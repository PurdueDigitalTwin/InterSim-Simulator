using SWS;
using UnityEngine;

public class WheelSpin : MonoBehaviour
{
    private Rigidbody carRigidbody;
    private splineMove splineMoveComponent;
    private float wheelRadius;

    // Store cumulative spin angle for wheel rolling
    private float spinAngle = 0f;

    void Start()
    {
        carRigidbody = GetComponentInParent<Rigidbody>();
        if (carRigidbody == null)
        {
            Debug.LogError($"WheelSpin: No Rigidbody found in parent objects of {gameObject.name}");
            enabled = false;
            return;
        }

        splineMoveComponent = carRigidbody.GetComponent<splineMove>();
        if (splineMoveComponent == null)
        {
            Debug.LogError($"WheelSpin: No splineMove component found on {carRigidbody.gameObject.name}");
            enabled = false;
            return;
        }

        wheelRadius = 0.33f;
    }

    void Update()
    {
        if (splineMoveComponent == null) return;

        // Get car speed and calculate the spin angular speed.
        float carSpeed = splineMoveComponent.speed;
        float angularSpeed = carSpeed / wheelRadius * Mathf.Rad2Deg;
        spinAngle += angularSpeed / 5f * Time.deltaTime;
        spinAngle %= 360f;  // Keep it between 0 and 360.

        // For front wheels, calculate the steering angle.
        float steerAngle = 0f;
        if (gameObject.name.Contains("Front"))
        {
            Vector3 carPos = carRigidbody.transform.position;
            Vector3 carForward = carRigidbody.transform.forward;

            int waypointCount = splineMoveComponent.pathContainer.GetWaypointCount();
            
            if (splineMoveComponent.currentPoint >= waypointCount - 1)
            {
                // At the final waypoint: use the last segment's direction.
                int prevIndex = Mathf.Max(splineMoveComponent.currentPoint - 1, 0);
                Vector3 lastDir = (splineMoveComponent.pathContainer.GetWaypoint(splineMoveComponent.currentPoint).position -
                                splineMoveComponent.pathContainer.GetWaypoint(prevIndex).position).normalized;
                steerAngle = Vector3.SignedAngle(carForward, lastDir, Vector3.up);
            }
            else
            {
                // Normal case: use the next waypoint.
                int nextIndex = splineMoveComponent.currentPoint + 1;
                Vector3 nextWaypointPos = splineMoveComponent.pathContainer.GetWaypoint(nextIndex).position;
                Vector3 directionToNext = (nextWaypointPos - carPos).normalized;
                steerAngle = Vector3.SignedAngle(carForward, directionToNext, Vector3.up);
            }
        }

        // Create a rotation for the spin (around X) and for the steering (around Y).
        Quaternion spinRotation = Quaternion.Euler(spinAngle, 0, 0);
        Quaternion steerRotation = Quaternion.Euler(0, steerAngle, 0);

        // Combine them: apply steering first, then the wheel spin.
        transform.localRotation = steerRotation * spinRotation;
    }
}
