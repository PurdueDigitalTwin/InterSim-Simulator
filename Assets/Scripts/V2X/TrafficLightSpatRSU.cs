using UnityEngine;
using TrafficLightTimerSystem;
using HealthbarGames;

namespace V2X
{
	/// <summary>
	/// RSU that broadcasts traffic light SPaT (Signal Phase And Timing) info to an ego vehicle.
	/// Focuses on one movement tied to a specific phase index (default 0).
	/// Also sends the stop line position once for ego distance tracking.
	/// </summary>
	public class TrafficLightSpatRSU : MonoBehaviour
	{
		[Header("RSU Settings")]
		[Tooltip("Ego vehicle ID to send SPaT to")] public int egoVehicleId = 0;
		[Tooltip("Broadcast frequency (Hz)")] public float broadcastHz = 5f;
		[Tooltip("Phase index representing this movement (0-based)")] public int movementPhaseIndex = 0;

		[Header("References")]
		[SerializeField] TrafficLightTimer timer;  // can auto-find
		[SerializeField] TrafficLightManager tlManager; // can auto-find
		[Tooltip("Empty object placed at the stop line for this movement")]
		[SerializeField] Transform stopLine;

		float _accum;
		bool _stopLineSent;

		void Awake()
		{
			if (timer == null) timer = FindAnyObjectByType<TrafficLightTimer>();
			if (tlManager == null) tlManager = FindAnyObjectByType<TrafficLightManager>();
		}

		void Update()
		{
			// Send stop line position once
			if (!_stopLineSent && stopLine != null && V2XBus.I != null)
			{
				var radio = V2XBus.I.FindRadioByVehicleId(egoVehicleId);
				if (radio != null)
				{
					radio.Receive(new StopLineMessage(egoVehicleId, stopLine.position));
					_stopLineSent = true;
				}
			}

			_accum += Time.deltaTime;
			if (_accum < 1f / Mathf.Max(0.01f, broadcastHz)) return;
			_accum = 0f;

			if (timer == null || tlManager == null || V2XBus.I == null) return;

			ComputeSpat(out TlColor current, out TlColor next, out float greenRem, out float yellowRem, out float redRem);
			var msg = new SpatMessage(egoVehicleId, current, next, greenRem, yellowRem, redRem);

			var r = V2XBus.I.FindRadioByVehicleId(egoVehicleId);
			if (r != null)
			{
				r.Receive(msg);
			}
		}

		void ComputeSpat(out TlColor current, out TlColor next, out float greenRemaining, out float yellowRemaining, out float redRemaining)
		{
			greenRemaining = 0f;
			yellowRemaining = 0f;
			redRemaining = 0f;

			int curPhase = timer.CurrentPhaseIndex;
			var curState = timer.CurrentState;

			// Map current state to color for OUR movement
			bool inOurPhase = (curPhase == movementPhaseIndex);
			if (inOurPhase)
			{
				if (curState == TrafficLightBase.State.Go)
				{
					current = TlColor.Green;
					next = TlColor.Yellow;
					greenRemaining = Mathf.Max(0f, timer.TimeRemainingInCurrentState);
					return;
				}
				else if (curState == TrafficLightBase.State.PrepareToStop)
				{
					current = TlColor.Yellow;
					next = TlColor.Red;
					yellowRemaining = Mathf.Max(0f, timer.TimeRemainingInCurrentState);
					// red time is until next green (next cycle)
					redRemaining = ComputeTimeUntilNextGreenFromOurPhaseInYellow();
					return;
				}
				else if (curState == TrafficLightBase.State.PrepareToGo)
				{
					// Red + Yellow, but most systems consider this 'yellow' stage
					current = TlColor.Yellow;
					next = TlColor.Green;
					yellowRemaining = Mathf.Max(0f, timer.TimeRemainingInCurrentState);
					return;
				}
				else // Stop
				{
					current = TlColor.Red;
					next = TlColor.Green;
					redRemaining = ComputeTimeUntilNextGreenFromOurPhaseInStop();
					return;
				}
			}
			else
			{
				current = TlColor.Red;
				next = TlColor.Green;
				// Not our phase -> time until our phase starts + its PrepareToGo duration to reach green
				redRemaining = Mathf.Max(0f, timer.GetTimeUntilPhase(movementPhaseIndex) + GetPhaseStartTime(movementPhaseIndex));
				return;
			}
		}

		float ComputeTimeUntilNextGreenFromOurPhaseInYellow()
		{
			// Remaining yellow in this phase + stop delay + all other phases total + next cycle's PrepareToGo
			float remYellow = Mathf.Max(0f, timer.TimeRemainingInCurrentState);
			float stopDelay = GetPhaseDelay();
			float others = SumOtherPhasesTotalTime(movementPhaseIndex);
			float prepareToGo = GetPhaseStartTime(movementPhaseIndex);
			return remYellow + stopDelay + others + prepareToGo;
		}

		float ComputeTimeUntilNextGreenFromOurPhaseInStop()
		{
			// Remaining red in this phase's stop + other phases + next cycle's PrepareToGo
			float remStop = Mathf.Max(0f, timer.TimeRemainingInCurrentState);
			float others = SumOtherPhasesTotalTime(movementPhaseIndex);
			float prepareToGo = GetPhaseStartTime(movementPhaseIndex);
			return remStop + others + prepareToGo;
		}

		float SumOtherPhasesTotalTime(int ourIndex)
		{
			int count = GetPhaseCount();
			float sum = 0f;
			for (int i = 0; i < count; i++)
			{
				if (i == ourIndex) continue;
				sum += timer.GetPhaseTotalTime(i);
			}
			return sum;
		}

		int GetPhaseCount()
		{
			var list = GetPhaseList();
			return list != null ? list.Count : 0;
		}

		System.Collections.Generic.List<TrafficLightPhase> GetPhaseList()
		{
			var field = typeof(TrafficLightManager).GetField("PhaseList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field != null ? (System.Collections.Generic.List<TrafficLightPhase>)field.GetValue(tlManager) : null;
		}

		float GetPhaseDelay()
		{
			var field = typeof(TrafficLightManager).GetField("PhaseDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			return field != null ? (float)field.GetValue(tlManager) : 0f;
		}

		float GetPhaseStartTime(int phaseIndex)
		{
			var list = GetPhaseList();
			if (list == null || phaseIndex < 0 || phaseIndex >= list.Count) return 0f;
			return list[phaseIndex].PhaseStartTime;
		}
	}
}
