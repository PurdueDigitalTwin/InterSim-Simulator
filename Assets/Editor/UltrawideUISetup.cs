using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ultrawide UI tooling: Game View test resolutions + CanvasContentRoot wiring.
/// </summary>
public static class UltrawideUISetup
{
    const string UltrawideLabel = "Ultrawide 5120x1440";
    const string QhdWideLabel = "UW QHD 2560x1440";

    [InitializeOnLoadMethod]
    static void EnsureGameViewResolutions()
    {
        TryAddGameViewSize(UltrawideLabel, 5120, 1440);
        TryAddGameViewSize(QhdWideLabel, 2560, 1440);
    }

    [MenuItem("INDOT/UI/Add Ultrawide Game View Resolutions")]
    static void MenuAddResolutions()
    {
        EnsureGameViewResolutions();
        Debug.Log("[UltrawideUI] Ensured Game View sizes: 5120x1440 and 2560x1440. Pick them from the Game view resolution dropdown.");
    }

    [MenuItem("INDOT/UI/Apply CanvasContentRoot To Screen-Space Canvases In Open Scenes")]
    static void ApplyContentRootsInOpenScenes()
    {
        int added = 0;
        int applied = 0;

        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                continue;

            // Ultrawide-safe scaler defaults
            if (scaler.matchWidthOrHeight < 0.999f)
            {
                Undo.RecordObject(scaler, "Match Height");
                scaler.matchWidthOrHeight = 1f;
                EditorUtility.SetDirty(scaler);
            }

            CanvasContentRoot root = canvas.GetComponent<CanvasContentRoot>();
            if (root == null)
            {
                root = Undo.AddComponent<CanvasContentRoot>(canvas.gameObject);
                added++;
            }

            Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Apply ContentRoot");
            root.Apply();
            applied++;
            EditorUtility.SetDirty(canvas.gameObject);
        }

        Debug.Log($"[UltrawideUI] CanvasContentRoot added to {added} canvas(es); Apply() ran on {applied}.");
    }

    [MenuItem("INDOT/UI/Apply InGameUI Corner Layout In Open Scenes")]
    static void ApplyInGameUICorners()
    {
        int count = 0;
        foreach (InGameUICornerLayout layout in UnityEngine.Object.FindObjectsByType<InGameUICornerLayout>(FindObjectsInactive.Include))
        {
            Undo.RegisterFullObjectHierarchyUndo(layout.gameObject, "Apply InGameUI corners");
            layout.Apply();
            EditorUtility.SetDirty(layout.gameObject);
            count++;
        }

        Debug.Log($"[UltrawideUI] Applied corner layout on {count} InGameUI group(s).");
    }

    static void ValidateUltrawideSetup()
    {
        int issues = 0;
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                continue;

            string path = canvas.gameObject.name;
            if (Mathf.Abs(scaler.matchWidthOrHeight - 1f) > 0.01f)
            {
                Debug.LogWarning($"[UltrawideUI] {path}: CanvasScaler Match should be 1 (Height), found {scaler.matchWidthOrHeight}");
                issues++;
            }

            if (canvas.GetComponent<CanvasContentRoot>() == null)
            {
                Debug.LogWarning($"[UltrawideUI] {path}: missing CanvasContentRoot");
                issues++;
            }
        }

        if (issues == 0)
            Debug.Log("[UltrawideUI] Validation passed for open scenes. Test Game view at 5120x1440 and 2560x1440, then build for the curved monitor.");
        else
            Debug.LogWarning($"[UltrawideUI] Validation found {issues} issue(s). Run INDOT/UI/Apply CanvasContentRoot...");
    }

    [MenuItem("INDOT/UI/Log Build Verification Checklist")]
    static void LogBuildChecklist()
    {
        Debug.Log(
            "[UltrawideUI] Build verification checklist:\n" +
            "1. Game view: select 'Ultrawide 5120x1440' (and 'UW QHD 2560x1440').\n" +
            "2. Play MainMenu → Tutorial → each Traffic scene; confirm all buttons/dialogs visible.\n" +
            "3. File → Build Settings → Build standalone.\n" +
            "4. On the curved monitor, run at native 5120x1440 (Player defaults are 5120x1440; native resolution is enabled).\n" +
            "5. Confirm ContentRoot UI sits in the center; Background/Fade still fill the full width.\n" +
            "6. If anything is wrong: git reset --hard d929509 (pre-ultrawide checkpoint)."
        );
    }

    static void TryAddGameViewSize(string label, int width, int height)
    {
        try
        {
            Type gameViewSizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            Type singletonType = typeof(ScriptableSingleton<>).MakeGenericType(gameViewSizesType);
            PropertyInfo instanceProp = singletonType.GetProperty("instance");
            object gameViewSizes = instanceProp.GetValue(null, null);

            MethodInfo getGroup = gameViewSizesType.GetMethod("GetGroup");
            // 0 = Standalone
            object group = getGroup.Invoke(gameViewSizes, new object[] { 0 });

            Type groupType = group.GetType();
            MethodInfo getDisplayNames = groupType.GetMethod("GetDisplayTexts");
            string[] texts = getDisplayNames.Invoke(group, null) as string[];
            if (texts != null)
            {
                foreach (string t in texts)
                {
                    if (t != null && t.StartsWith(label, StringComparison.Ordinal))
                        return;
                }
            }

            Type gameViewSizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
            Type gameViewSizeTypeEnum = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
            // FixedResolution = 1
            object sizeType = Enum.Parse(gameViewSizeTypeEnum, "FixedResolution");
            object newSize = Activator.CreateInstance(gameViewSizeType, sizeType, width, height, label);

            MethodInfo addCustomSize = groupType.GetMethod("AddCustomSize");
            addCustomSize.Invoke(group, new[] { newSize });
        }
        catch (Exception e)
        {
            Debug.LogWarning("[UltrawideUI] Could not add Game View size via reflection: " + e.Message);
        }
    }
}
