using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Added for List

//=============================================================================
//  TrafficLightTimer
//  Helper component that tracks time for traffic light phases
//  Can be attached to the same GameObject as TrafficLightManager
//=============================================================================

namespace TrafficLightTimerSystem
{
    public class TrafficLightTimer : MonoBehaviour
    {
        [Header("Timer Settings")]
        [SerializeField] private bool enableTimer = true;
        [SerializeField] private float updateInterval = 0.1f; // How often to update the timer

        private HealthbarGames.TrafficLightManager trafficLightManager;
        private Coroutine timerCoroutine;
        
        // Current phase tracking
        private int lastPhaseIndex = -1;
        private HealthbarGames.TrafficLightBase.State lastState = HealthbarGames.TrafficLightBase.State.Blank;
        
        // Timer variables
        private float phaseStartTime;
        private float currentPhaseTotalTime;
        private float currentStateStartTime;
        private float currentStateDuration;

        // Public properties for time tracking
        public float TimeRemainingInCurrentState { get; private set; }
        public float TimeElapsedInCurrentState { get; private set; }
        public float TimeRemainingInCurrentPhase { get; private set; }
        public float TimeElapsedInCurrentPhase { get; private set; }
        public float CurrentPhaseTotalTime => currentPhaseTotalTime;
        public float CurrentStateDuration => currentStateDuration;
        public HealthbarGames.TrafficLightBase.State CurrentState => lastState;
        public int CurrentPhaseIndex => lastPhaseIndex;
        public float CurrentStateProgressPercentage => currentStateDuration > 0 ? Mathf.Clamp01(TimeElapsedInCurrentState / currentStateDuration) : 0f;
        public float CurrentPhaseProgressPercentage => currentPhaseTotalTime > 0 ? Mathf.Clamp01(TimeElapsedInCurrentPhase / currentPhaseTotalTime) : 0f;

        void Start()
        {
            trafficLightManager = GetComponent<HealthbarGames.TrafficLightManager>();
            if (trafficLightManager == null)
            {
                Debug.LogError("TrafficLightTimer requires TrafficLightManager component on the same GameObject!");
                enabled = false;
                return;
            }

            if (enableTimer)
            {
                StartTimer();
            }
        }

        void OnDestroy()
        {
            StopTimer();
        }

        public void StartTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
            }
            timerCoroutine = StartCoroutine(TimerCoroutine());
        }

        public void StopTimer()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }

        private IEnumerator TimerCoroutine()
        {
            while (true)
            {
                UpdateTimer();
                yield return new WaitForSeconds(updateInterval);
            }
        }

        private void UpdateTimer()
        {
            if (trafficLightManager == null) return;

            // Check if phase or state has changed
            bool phaseChanged = false;
            bool stateChanged = false;

            // Get current phase index (we'll need to add this to TrafficLightManager or use reflection)
            int currentPhaseIndex = GetCurrentPhaseIndex();
            if (currentPhaseIndex != lastPhaseIndex)
            {
                lastPhaseIndex = currentPhaseIndex;
                phaseChanged = true;
                phaseStartTime = Time.time;
                CalculatePhaseTotalTime();
            }

            // Get current state (we'll need to add this to TrafficLightManager or use reflection)
            HealthbarGames.TrafficLightBase.State currentState = GetCurrentState();
            if (currentState != lastState)
            {
                lastState = currentState;
                stateChanged = true;
                currentStateStartTime = Time.time;
                CalculateStateDuration();
            }

            // Update timers
            if (stateChanged)
            {
                TimeElapsedInCurrentState = 0f;
            }
            else
            {
                TimeElapsedInCurrentState = Time.time - currentStateStartTime;
            }

            if (phaseChanged)
            {
                TimeElapsedInCurrentPhase = 0f;
            }
            else
            {
                TimeElapsedInCurrentPhase = Time.time - phaseStartTime;
            }

            // Calculate remaining times
            TimeRemainingInCurrentState = Mathf.Max(0f, currentStateDuration - TimeElapsedInCurrentState);
            TimeRemainingInCurrentPhase = Mathf.Max(0f, currentPhaseTotalTime - TimeElapsedInCurrentPhase);
        }

        private int GetCurrentPhaseIndex()
        {
            // Use reflection to get the private field from TrafficLightManager
            var field = typeof(HealthbarGames.TrafficLightManager).GetField("mCurrentPhaseIndex", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (int)field.GetValue(trafficLightManager) : 0;
        }

        private HealthbarGames.TrafficLightBase.State GetCurrentState()
        {
            // We need to determine the current state based on the phase timing
            // This is a bit tricky since the original system doesn't expose the current state
            // We'll estimate based on the phase timing and elapsed time
            
            if (trafficLightManager == null) return HealthbarGames.TrafficLightBase.State.Blank;

            var phaseList = GetPhaseList();
            if (phaseList == null || phaseList.Count == 0) return HealthbarGames.TrafficLightBase.State.Blank;

            int currentPhaseIndex = GetCurrentPhaseIndex();
            if (currentPhaseIndex >= phaseList.Count) return HealthbarGames.TrafficLightBase.State.Blank;

            var currentPhase = phaseList[currentPhaseIndex];
            float phaseElapsed = TimeElapsedInCurrentPhase;
            float phaseStartTime = currentPhase.PhaseStartTime;
            float phaseActiveTime = currentPhase.PhaseActiveTime;
            float phaseEndTime = currentPhase.PhaseEndTime;
            float phaseDelay = GetPhaseDelay();

            // Determine state based on elapsed time within the phase
            if (phaseElapsed < phaseStartTime)
            {
                return HealthbarGames.TrafficLightBase.State.PrepareToGo;
            }
            else if (phaseElapsed < phaseStartTime + phaseActiveTime)
            {
                return HealthbarGames.TrafficLightBase.State.Go;
            }
            else if (phaseElapsed < phaseStartTime + phaseActiveTime + phaseEndTime)
            {
                return HealthbarGames.TrafficLightBase.State.PrepareToStop;
            }
            else
            {
                return HealthbarGames.TrafficLightBase.State.Stop;
            }
        }

        private List<HealthbarGames.TrafficLightPhase> GetPhaseList()
        {
            var field = typeof(HealthbarGames.TrafficLightManager).GetField("PhaseList", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (List<HealthbarGames.TrafficLightPhase>)field.GetValue(trafficLightManager) : null;
        }

        private float GetPhaseDelay()
        {
            var field = typeof(HealthbarGames.TrafficLightManager).GetField("PhaseDelay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field != null ? (float)field.GetValue(trafficLightManager) : 1.0f;
        }

        private void CalculatePhaseTotalTime()
        {
            var phaseList = GetPhaseList();
            if (phaseList == null || lastPhaseIndex >= phaseList.Count) return;

            var currentPhase = phaseList[lastPhaseIndex];
            float phaseDelay = GetPhaseDelay();
            currentPhaseTotalTime = currentPhase.PhaseStartTime + currentPhase.PhaseActiveTime + currentPhase.PhaseEndTime + phaseDelay;
        }

        private void CalculateStateDuration()
        {
            var phaseList = GetPhaseList();
            if (phaseList == null || lastPhaseIndex >= phaseList.Count) return;

            var currentPhase = phaseList[lastPhaseIndex];
            float phaseDelay = GetPhaseDelay();

            switch (lastState)
            {
                case HealthbarGames.TrafficLightBase.State.PrepareToGo:
                    currentStateDuration = currentPhase.PhaseStartTime;
                    break;
                case HealthbarGames.TrafficLightBase.State.Go:
                    currentStateDuration = currentPhase.PhaseActiveTime;
                    break;
                case HealthbarGames.TrafficLightBase.State.PrepareToStop:
                    currentStateDuration = currentPhase.PhaseEndTime;
                    break;
                case HealthbarGames.TrafficLightBase.State.Stop:
                    currentStateDuration = phaseDelay;
                    break;
                default:
                    currentStateDuration = 0f;
                    break;
            }
        }

        // Utility methods for formatted time strings
        public string GetCurrentStateTimeString()
        {
            int minutes = Mathf.FloorToInt(TimeRemainingInCurrentState / 60f);
            int seconds = Mathf.FloorToInt(TimeRemainingInCurrentState % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        public string GetCurrentPhaseTimeString()
        {
            int minutes = Mathf.FloorToInt(TimeRemainingInCurrentPhase / 60f);
            int seconds = Mathf.FloorToInt(TimeRemainingInCurrentPhase % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        // Method to get time information for a specific phase
        public float GetPhaseTotalTime(int phaseIndex)
        {
            var phaseList = GetPhaseList();
            if (phaseList == null || phaseIndex >= phaseList.Count) return 0f;

            var phase = phaseList[phaseIndex];
            float phaseDelay = GetPhaseDelay();
            return phase.PhaseStartTime + phase.PhaseActiveTime + phase.PhaseEndTime + phaseDelay;
        }

        // Method to get estimated time until a specific phase starts
        public float GetTimeUntilPhase(int targetPhaseIndex)
        {
            if (targetPhaseIndex == lastPhaseIndex) return 0f;

            var phaseList = GetPhaseList();
            if (phaseList == null) return 0f;

            float timeRemaining = TimeRemainingInCurrentPhase;
            
            // Add time for all phases between current and target
            for (int i = (lastPhaseIndex + 1) % phaseList.Count; i != targetPhaseIndex; i = (i + 1) % phaseList.Count)
            {
                timeRemaining += GetPhaseTotalTime(i);
            }

            return timeRemaining;
        }
    }
}
