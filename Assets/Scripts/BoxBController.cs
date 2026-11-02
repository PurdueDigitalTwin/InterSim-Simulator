using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to Box B (a GameObject with a BoxCollider set to IsTrigger).
/// References Box A to decide STOP/GO, and signals cars inside Box B accordingly.
/// </summary>
public class BoxBController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Box A detector that tracks cars inside A.")]
    public BoxADetector boxADetector;

    [Header("Vehicle Filter")]
    [Tooltip("Layer mask for vehicles. Leave -1 to accept all.")]
    public LayerMask vehicleLayer = -1;

    [Header("Deceleration Settings")]
    [Tooltip("Override deceleration duration (seconds) for STOP. <=0 to auto-compute per car.")]
    public float stopDecelDuration = 0f;

    [Header("Smoothing / Hysteresis")]
    [Tooltip("When GO is set, keep GO latched until Box B is empty (prevents re-stopping cars mid-exit).")]
    public bool latchGoUntilBoxBEmpty = true;

    [Tooltip("Optional minimum time (seconds) to hold GO after it is set. 0 to disable.")]
    public float minGoHoldSeconds = 0f;

    [Header("Signal State (Read-Only)")]
    [SerializeField]
    private bool goSignal = true; // true: GO, false: STOP

    private bool goLatched = false;
    private float goLatchedUntilTime = 0f;

    private readonly HashSet<TrafficControl> carsInBoxB = new HashSet<TrafficControl>();

    private void Reset()
    {
        // Attempt auto-link if in the same parent
        if (boxADetector == null)
        {
            boxADetector = FindAnyObjectByType<BoxADetector>();
        }
    }

    private void Update()
    {
        if (boxADetector == null) return;

        bool aIsEmpty = !boxADetector.HasCarsInside;

        // Evaluate latch release conditions
        if (goLatched)
        {
            bool timeExpired = Time.time >= goLatchedUntilTime;
            bool boxBEmpty = carsInBoxB.Count == 0;
            if ((latchGoUntilBoxBEmpty && boxBEmpty && timeExpired) || (!latchGoUntilBoxBEmpty && timeExpired))
            {
                goLatched = false;
            }
        }

        bool shouldGo;
        if (goLatched)
        {
            shouldGo = true; // Keep GO while latched
        }
        else
        {
            shouldGo = aIsEmpty;
        }

        if (shouldGo != goSignal)
        {
            goSignal = shouldGo;
            BroadcastSignalToCarsInBoxB();

            if (goSignal)
            {
                // We just transitioned to GO; apply latch and optional hold time
                goLatched = latchGoUntilBoxBEmpty || (minGoHoldSeconds > 0f);
                goLatchedUntilTime = Time.time + Mathf.Max(0f, minGoHoldSeconds);
            }
        }
    }

    private void BroadcastSignalToCarsInBoxB()
    {
        foreach (var tc in carsInBoxB)
        {
            if (tc == null) continue;
            if (goSignal)
            {
                tc.BoxSignalGo();
            }
            else
            {
                tc.BoxSignalStop(stopDecelDuration);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, vehicleLayer)) return;
        var tc = other.GetComponentInParent<TrafficControl>();
        if (tc == null) return;

        carsInBoxB.Add(tc);
        // Sync current signal to newly entered car
        // Sync and apply stop with custom decel if needed
        if (goSignal)
        {
            tc.OnEnterBoxSignalZone(true);
        }
        else
        {
            tc.OnEnterBoxSignalZone(false);
            tc.BoxSignalStop(stopDecelDuration);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var tc = other.GetComponentInParent<TrafficControl>();
        if (tc == null) return;

        if (carsInBoxB.Remove(tc))
        {
            tc.OnExitBoxSignalZone();
        }
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == -1 || (mask.value & (1 << layer)) != 0;
    }
}


