using UnityEngine;
using UnityEngine.UI;

//=============================================================================
//  TrafficLightTimerExample
//  Example script showing how to use TrafficLightTimer
//  Attach this to a UI GameObject to display traffic light timing information
//=============================================================================

namespace TrafficLightTimerSystem
{
    public class TrafficLightTimerExample : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMPro.TextMeshProUGUI currentStateText;
        [SerializeField] private TMPro.TextMeshProUGUI currentPhaseText;
        [SerializeField] private TMPro.TextMeshProUGUI stateTimeText;
        [SerializeField] private TMPro.TextMeshProUGUI phaseTimeText;
        [SerializeField] private Slider stateProgressSlider;
        [SerializeField] private Slider phaseProgressSlider;

        [Header("Timer Reference")]
        [SerializeField] private TrafficLightTimer timer;

        void Start()
        {
            // If no timer is assigned, try to find one in the scene
            if (timer == null)
            {
                timer = FindObjectOfType<TrafficLightTimer>();
            }

            if (timer == null)
            {
                Debug.LogError("TrafficLightTimerExample requires a TrafficLightTimer component in the scene!");
                enabled = false;
                return;
            }
        }

        void Update()
        {
            if (timer == null) return;

            UpdateUI();
        }

        private void UpdateUI()
        {
            // Update state information
            if (currentStateText != null)
            {
                currentStateText.text = $"State: {timer.CurrentState}";
            }

            if (currentPhaseText != null)
            {
                currentPhaseText.text = $"Phase: {timer.CurrentPhaseIndex}";
            }

            // Update time information
            if (stateTimeText != null)
            {
                stateTimeText.text = $"State Time: {timer.GetCurrentStateTimeString()}";
            }

            if (phaseTimeText != null)
            {
                phaseTimeText.text = $"Phase Time: {timer.GetCurrentPhaseTimeString()}";
            }

            // Update progress bars
            if (stateProgressSlider != null)
            {
                stateProgressSlider.value = timer.CurrentStateProgressPercentage;
            }

            if (phaseProgressSlider != null)
            {
                phaseProgressSlider.value = timer.CurrentPhaseProgressPercentage;
            }
        }

        // Example method to log timing information
        public void LogTimingInfo()
        {
            if (timer == null) return;

            Debug.Log($"Current State: {timer.CurrentState}");
            Debug.Log($"Current Phase: {timer.CurrentPhaseIndex}");
            Debug.Log($"State Time Remaining: {timer.TimeRemainingInCurrentState:F1}s");
            Debug.Log($"Phase Time Remaining: {timer.TimeRemainingInCurrentPhase:F1}s");
            Debug.Log($"State Progress: {timer.CurrentStateProgressPercentage:P1}");
            Debug.Log($"Phase Progress: {timer.CurrentPhaseProgressPercentage:P1}");
        }

        // Example method to get time until a specific phase
        public void LogTimeUntilPhase(int phaseIndex)
        {
            if (timer == null) return;

            float timeUntil = timer.GetTimeUntilPhase(phaseIndex);
            Debug.Log($"Time until phase {phaseIndex}: {timeUntil:F1} seconds");
        }
    }
}
