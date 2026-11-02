using UnityEngine;

/// <summary>
/// Keeps driving HUD widgets pinned to screen corners on any aspect ratio.
/// Attach to the InGameUI RectTransform (full-screen overlay group).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class InGameUICornerLayout : MonoBehaviour
{
    [Tooltip("Padding from the screen edge in canvas units.")]
    public float edgePadding = 20f;

    static readonly Vector3[] CornerBuffer = new Vector3[4];

    void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        RectTransform panel = transform as RectTransform;
        if (panel == null)
            return;

        StretchFullScreen(panel);

        LayoutTopRight(FindRect("MainMenu"), 0);
        LayoutTopRight(FindRect("Restart"), 1);
        LayoutTopRight(FindRect("ModeSwitch"), 2);

        LayoutTopLeftFittingContent(FindRect("ButtonHints"));
        LayoutBottomRight(FindRect("OSDPanel"));
    }

    RectTransform FindRect(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        foreach (Transform child in transform)
        {
            if (child.name == objectName || child.name.StartsWith(objectName))
                return child as RectTransform;
        }

        return null;
    }

    void StretchFullScreen(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Pin a widget to the top-left, offsetting by its child content AABB so nothing clips.
    /// ButtonHints children extend outside the root rect, so a naive (padding, -padding) crops them.
    /// </summary>
    void LayoutTopLeftFittingContent(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = Vector2.zero;
        Canvas.ForceUpdateCanvases();

        if (!TryGetLocalContentBounds(rt, out float minX, out float maxX, out float minY, out float maxY))
        {
            rt.anchoredPosition = new Vector2(edgePadding, -edgePadding);
            return;
        }

        // Place so content's left/top sit at edgePadding inside the screen.
        rt.anchoredPosition = new Vector2(edgePadding - minX, -edgePadding - maxY);
    }

    bool TryGetLocalContentBounds(RectTransform root, out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;
        bool found = false;

        // Include root itself and all descendants.
        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform child = rects[i];
            child.GetWorldCorners(CornerBuffer);
            for (int c = 0; c < 4; c++)
            {
                Vector3 local = root.InverseTransformPoint(CornerBuffer[c]);
                if (local.x < minX) minX = local.x;
                if (local.x > maxX) maxX = local.x;
                if (local.y < minY) minY = local.y;
                if (local.y > maxY) maxY = local.y;
                found = true;
            }
        }

        return found && maxX > minX && maxY > minY;
    }

    void LayoutBottomRight(RectTransform rt)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-edgePadding, edgePadding);
    }

    void LayoutTopRight(RectTransform rt, int indexFromRight)
    {
        if (rt == null)
            return;

        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);

        float x = edgePadding;

        // index 0 = rightmost button (MainMenu), 1 = Restart, 2 = leftmost (ModeSwitch)
        for (int i = 0; i < indexFromRight; i++)
        {
            RectTransform sibling = IndexTopRightSibling(i);
            if (sibling != null)
                x += (sibling.sizeDelta.x > 1f ? sibling.sizeDelta.x : 160f) + 10f;
        }

        rt.anchoredPosition = new Vector2(-x, -edgePadding);
    }

    RectTransform IndexTopRightSibling(int indexFromRight)
    {
        switch (indexFromRight)
        {
            case 0: return FindRect("MainMenu");
            case 1: return FindRect("Restart");
            default: return null;
        }
    }
}
