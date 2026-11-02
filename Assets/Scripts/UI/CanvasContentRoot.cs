using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps interactive UI inside a centered 1920x1080 (16:9) content stage on ultrawide displays.
/// Full-bleed elements (Background / Fade) stay as direct Canvas children and stretch to the screen.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public class CanvasContentRoot : MonoBehaviour
{
    [Tooltip("Design size of the interactive UI stage.")]
    public Vector2 contentSize = new Vector2(1920f, 1080f);

    [Tooltip("Child name prefixes that stay full-screen (not moved under ContentRoot).")]
    public string[] fullBleedNamePrefixes = { "Background", "Fade", "FadeScreen", "InGameUI", "Scenarios_UI", "Vehicles_UI" };

    const string ContentRootName = "ContentRoot";

    void Awake()
    {
        Apply();
    }

    /// <summary>
    /// Creates/updates ContentRoot and reparents interactive children under it.
    /// Safe to call from Editor menus as well as at runtime.
    /// </summary>
    public void Apply()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
            return;

        RectTransform contentRoot = FindOrCreateContentRoot(canvasRect);
        ConfigureContentRoot(contentRoot);

        // Snapshot children first — reparenting mutates the hierarchy.
        var toMove = new List<Transform>();
        for (int i = 0; i < canvasRect.childCount; i++)
        {
            Transform child = canvasRect.GetChild(i);
            if (child == contentRoot)
                continue;
            if (IsFullBleed(child.name))
                continue;
            toMove.Add(child);
        }

        foreach (Transform child in toMove)
        {
            if (child.parent != contentRoot)
                child.SetParent(contentRoot, false);
        }
    }

    RectTransform FindOrCreateContentRoot(RectTransform canvasRect)
    {
        Transform existing = canvasRect.Find(ContentRootName);
        if (existing != null)
            return existing as RectTransform;

        var go = new GameObject(ContentRootName, typeof(RectTransform));
        go.layer = gameObject.layer;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvasRect, false);
        // Keep ContentRoot above backgrounds but allow later siblings to draw on top if needed.
        rt.SetAsLastSibling();
        return rt;
    }

    void ConfigureContentRoot(RectTransform contentRoot)
    {
        // Centered fixed design stage (1920x1080). On ultrawide, side margins remain empty
        // while interactive UI stays in the optical center of the curved display.
        contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        contentRoot.pivot = new Vector2(0.5f, 0.5f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.localRotation = Quaternion.identity;
        contentRoot.localScale = Vector3.one;
        contentRoot.sizeDelta = contentSize;
    }

    bool IsFullBleed(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || fullBleedNamePrefixes == null)
            return false;

        for (int i = 0; i < fullBleedNamePrefixes.Length; i++)
        {
            string prefix = fullBleedNamePrefixes[i];
            if (string.IsNullOrEmpty(prefix))
                continue;
            if (objectName.StartsWith(prefix))
                return true;
        }

        return false;
    }
}
