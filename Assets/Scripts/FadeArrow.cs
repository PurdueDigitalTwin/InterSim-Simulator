using Unity.VisualScripting;
using UnityEngine;

public class ArrowFade : MonoBehaviour
{
    private Material arrowMaterial;
    private bool isFading = false;
    private float fadeSpeed = 3f; // Adjust fade speed

    void Start()
    {
        arrowMaterial = GetComponent<Renderer>().material;
        SetArrowTransparency(1f); // Start fully visible
    }

    void Update()
    {
        // If the car is close, start fading out (only for the arrow material)
        if (isFading && arrowMaterial.name.StartsWith("Nav"))
        {
            float alpha = Mathf.Lerp(arrowMaterial.color.a, 0f, fadeSpeed * Time.deltaTime);
            SetArrowTransparency(alpha);
            if (arrowMaterial.color.a <= 0.05f) // If almost invisible, stop fading
            {
                isFading = false;
            }
        }
    }

    public void StartFading()
    {
        isFading = true;
    }

    public void SetArrowTransparency(float alpha)
    {
        Color color = arrowMaterial.color;
        color.a = alpha;
        arrowMaterial.color = color;
    }

    public void ResetFade()
    {
        SetArrowTransparency(1f); // Reset to fully visible
    }
}