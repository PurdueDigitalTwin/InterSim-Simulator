using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapBoundary : MonoBehaviour
{
    public Image fadeImage;
    public ScenarioSelector scenarioSelector;

    public float fadeToBlack   = 1.2f;   // darken
    public float holdBlack     = 0.3f;   // wait while map resets
    public float fadeFromBlack = 1.0f;   // lighten

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.transform.root.CompareTag("Player")) return;

        triggered = true;
        StartCoroutine(TransitionSequence());
    }

    IEnumerator TransitionSequence()
    {
        // A) fade screen to black
        yield return StartCoroutine(FadeImageAlpha(0f, 1f, fadeToBlack));

        // B) hold black & run map/UI reset
        yield return new WaitForSeconds(holdBlack);

        if (scenarioSelector != null)
            scenarioSelector.OnResetButtonClicked();
        else
            Debug.LogWarning("[GateFadeTransition] ScenarioSelector missing.");

        // C) fade black away to reveal the menu UI
        yield return StartCoroutine(FadeImageAlpha(1f, 0f, fadeFromBlack));

        triggered = false; // reset trigger
    }

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