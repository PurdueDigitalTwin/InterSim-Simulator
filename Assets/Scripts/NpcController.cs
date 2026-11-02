using UnityEngine;
using SWS;

public class NpcController : MonoBehaviour
{
    private bool started = false;
    public void StartAllCars()
    {
        // Call StartMove on each one or reset them to their initial positions
        foreach (Transform child in transform)
        {
            // Skip if the vehicle is not active
            if (!child.gameObject.activeInHierarchy)
                continue;

            splineMove splineMove = child.GetComponent<splineMove>();
            TrafficControl trafficControl = child.GetComponent<TrafficControl>();

            if (started)
            {
                splineMove.GoToWaypoint(splineMove.initPoint);

                trafficControl.ResetVehicle();
                continue;
            }
            splineMove.StartMove();
        }

        started = true;
    }

    public void ResumeAllCars()
    {
        // Find all SplineMove components in child objects
        splineMove[] splineMoves = GetComponentsInChildren<splineMove>();

        // Call Resume on each one
        foreach (var splineMove in splineMoves)
        {
            splineMove.CreateTween();
            splineMove.StartMove();
        }
    }
}
