using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class GateFadeTransition : MonoBehaviour
{
    [Header("Scene references")]
    [Tooltip("Full-screen Image: black colour, alpha 0, on a Canvas above all others.")]
    public Image fadeImage;

    [Tooltip("Controller script in the scene (ScenarioSelector or TutorialController - drag in Inspector).")]
    public MonoBehaviour controller;

    [Header("Timings (seconds)")]
    public float fadeToBlack   = 1.2f;   // darken
    public float holdBlack     = 0.3f;   // wait while map resets
    public float fadeFromBlack = 1.0f;   // lighten

    private bool triggered = false;

    //------------------------------------------------------------------
    void Awake()
    {
        // 1) make this collider a trigger
        GetComponent<Collider>().isTrigger = true;

        // 2) guarantee the overlay really draws & sits on top
        if (fadeImage != null)
        {
            fadeImage.color         = new Color(0, 0, 0, 0);
            fadeImage.raycastTarget = false;           // never block clicks transparent

            Canvas c = fadeImage.GetComponentInParent<Canvas>();
            if (c != null) c.sortingOrder = 999;       // render above other UI
        }
    }

    //------------------------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.transform.root.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(TransitionSequence());
    }

    //------------------------------------------------------------------
    IEnumerator TransitionSequence()
    {
        // A) fade screen to black
        yield return StartCoroutine(FadeImageAlpha(0f, 1f, fadeToBlack));

        // B) hold black & run map/UI reset
        yield return new WaitForSeconds(holdBlack);

        if (controller != null)
        {
            // Use reflection to call OnMenuButtonClicked method
            var method = controller.GetType().GetMethod("OnMenuButtonClicked");
            if (method != null)
            {
                method.Invoke(controller, new object[] { true });
            }
            else
            {
                Debug.LogWarning("[GateFadeTransition] OnMenuButtonClicked method not found on controller.");
            }
        }
        else
        {
            Debug.LogWarning("[GateFadeTransition] Controller missing.");
        }

        // C) fade black away to reveal the menu UI
        yield return StartCoroutine(FadeImageAlpha(1f, 0f, fadeFromBlack));

        triggered = false; // reset trigger
        
        // Use reflection to call ResetArrows method
        if (controller != null)
        {
            var resetMethod = controller.GetType().GetMethod("ResetArrows");
            if (resetMethod != null)
            {
                resetMethod.Invoke(controller, new object[] { true });
            }
        }
    }

    //------------------------------------------------------------------
    IEnumerator FadeImageAlpha(float from, float to, float duration)
    {
        float t = 0f;

        // while black (from==0→1), let overlay block clicks; otherwise unblock
        fadeImage.raycastTarget = (to > from);

        while (t < duration)
        {
            float a = Mathf.Lerp(from, to, t / duration);
            fadeImage.color = new Color(0, 0, 0, a);
            t += Time.deltaTime;
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, to);
        if (to == 0f) fadeImage.raycastTarget = false;   // fully clear → stop blocking
    }
}
