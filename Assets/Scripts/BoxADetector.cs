using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to Box A (a GameObject with a BoxCollider set to IsTrigger).
/// Tracks whether any cars (with TrafficControl) are inside.
/// </summary>
public class BoxADetector : MonoBehaviour
{
    [Tooltip("Layer mask for vehicles. Leave -1 to accept all.")]
    public LayerMask vehicleLayer = -1;

    private readonly HashSet<TrafficControl> carsInBox = new HashSet<TrafficControl>();

    public bool HasCarsInside => carsInBox.Count > 0;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, vehicleLayer)) return;
        var tc = other.GetComponentInParent<TrafficControl>();
        if (tc != null)
        {
            carsInBox.Add(tc);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var tc = other.GetComponentInParent<TrafficControl>();
        if (tc != null)
        {
            carsInBox.Remove(tc);
        }
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return mask == -1 || (mask.value & (1 << layer)) != 0;
    }
}


