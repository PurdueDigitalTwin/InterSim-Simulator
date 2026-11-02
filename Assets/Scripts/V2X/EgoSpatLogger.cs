using UnityEngine;

namespace V2X
{
	/// <summary>
	/// Logs the latest SPaT (Signal Phase And Timing) information received
	/// by the ego vehicle's V2XRadio. Also logs distance to the stop line
	/// if a stop line position has been received.
	/// Attach this to the ego vehicle GameObject (with V2XRadio) or any object in scene.
	/// </summary>
	public class EgoSpatLogger : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] V2XRadio radio; // Auto-found if not assigned

		[Header("Logging")]
		[Tooltip("Log only when a new SPaT arrives (based on timestamp)")]
		public bool logOnChangeOnly = true;
		[Tooltip("If not logging on change only, how often to log (Hz)")]
		public float logHz = 2f;
		[Tooltip("Include verbose fields in the log")]
		public bool verbose = false;

		float _accum;
		float _lastSpatTimestamp = -1f;

		void Awake()
		{
			if (radio == null)
			{
				radio = GetComponent<V2XRadio>();
				if (radio == null) radio = FindAnyObjectByType<V2XRadio>();
			}
		}

		void Update()
		{
			if (radio == null) return;

			if (logOnChangeOnly)
			{
				if (radio.HasSpat && !Mathf.Approximately(_lastSpatTimestamp, radio.LatestSpat.timestamp))
				{
					_lastSpatTimestamp = radio.LatestSpat.timestamp;
					PrintSpat(radio.LatestSpat);
					if (radio.HasStopLine) PrintDistance(radio.StopLineDistanceMeters);
				}
			}
			else
			{
				_accum += Time.deltaTime;
				if (_accum < 1f / Mathf.Max(0.01f, logHz)) return;
				_accum = 0f;
				if (radio.HasSpat) PrintSpat(radio.LatestSpat);
				if (radio.HasStopLine) PrintDistance(radio.StopLineDistanceMeters);
			}
		}

		void PrintSpat(SpatMessage s)
		{
			if (verbose)
			{
				Debug.Log($"[SPaT] veh={s.vehId} cur={s.current} next={s.next} greenRemaining={s.greenRemaining:F2}s yellowRemaining={s.yellowRemaining:F2}s redRemaining={s.redRemaining:F2}s t={s.timestamp:F3}");
			}
			else
			{
				Debug.Log($"[SPaT] cur={s.current} -> next={s.next} | g={s.greenRemaining:F1}s y={s.yellowRemaining:F1}s r={s.redRemaining:F1}s");
			}
		}

		void PrintDistance(float meters)
		{
			if (verbose)
			{
				Debug.Log($"[DIST] stopLineDist={meters:F3} m");
			}
			else
			{
				Debug.Log($"[DIST] stopLineDist={meters:F1} m");
			}
		}
	}
}
