using UnityEngine;

/// <summary>
/// Roundabout trigger that rotates the short-range radar when vehicles enter/exit roundabouts.
/// This allows vehicles to detect other vehicles in their lane within the roundabout.
/// </summary>
public class RoundaboutTrigger : MonoBehaviour
{
    [Header("Radar Rotation Settings")]
    [Tooltip("Angle to rotate the short-range radar when entering roundabout (degrees)")]
    public float radarRotationAngle = -15f; // Negative for left rotation
    
    [Tooltip("Duration for smooth radar rotation (seconds)")]
    public float rotationDuration = 1.0f;
    
    [Tooltip("Multiplier for radar angle coverage in roundabouts (1.5 = 50% increase)")]
    [Range(1.0f, 3.0f)]
    public float radarRangeMultiplier = 1.5f;
    
    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool debugMode = false;
    
    private void Start()
    {
        // Ensure this object has a trigger collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("RoundaboutTrigger requires a Collider component!");
            return;
        }
        
        if (!col.isTrigger)
        {
            Debug.LogWarning("RoundaboutTrigger collider should be set as trigger!");
            col.isTrigger = true;
        }
    }
    
    /// <summary>
    /// Called when a vehicle enters the roundabout trigger
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Find the TrafficControl component on the vehicle
        TrafficControl trafficControl = other.GetComponentInParent<TrafficControl>();
        if (trafficControl == null)
            return;
            
        if (debugMode)
            Debug.Log($"Vehicle {other.name} entered roundabout - rotating radar left by {radarRotationAngle}° and increasing range by {((radarRangeMultiplier - 1) * 100):F0}%");
        
        // Rotate the short-range radar to the left and increase angle coverage
        trafficControl.RotateShortRangeRadar(radarRotationAngle, rotationDuration, radarRangeMultiplier);
    }
    
    /// <summary>
    /// Called when a vehicle exits the roundabout trigger
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Find the TrafficControl component on the vehicle
        TrafficControl trafficControl = other.GetComponentInParent<TrafficControl>();
        if (trafficControl == null)
            return;
            
        if (debugMode)
            Debug.Log($"Vehicle {other.name} exited roundabout - resetting radar rotation");
        
        // Reset the short-range radar to original rotation
        trafficControl.ResetShortRangeRadarRotation(rotationDuration);
    }
    
    /// <summary>
    /// Visual debugging in Scene view
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw the trigger bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
        
        // Draw arrow showing radar rotation direction
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Vector3 direction = Quaternion.Euler(0, radarRotationAngle, 0) * Vector3.forward;
        Gizmos.DrawRay(center, direction * 2f);
        
        // Draw original forward direction for comparison
        Gizmos.color = Color.green;
        Gizmos.DrawRay(center, Vector3.forward * 1.5f);
    }
}
