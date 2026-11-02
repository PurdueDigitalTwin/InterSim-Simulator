using UnityEngine;
using SWS;

namespace V2X
{
    /// <summary>
    /// Type of stop sign intersection
    /// </summary>
    public enum StopType { FourWay, SideRoad }

    /// <summary>
    /// Road Side Unit for stop sign intersections.
    /// Manages vehicle access based on stop sign rules:
    /// - Four-way: First-come-first-served when conflict zone is empty
    /// - Side road: Must yield to main road traffic with time gap
    /// </summary>
    public class StopRSU : IntersectionRSU
    {
        [Header("Stop sign params")]
        [Tooltip("Type of stop sign intersection")]
        public StopType stopType = StopType.FourWay;
        [Tooltip("Minimum time gap required for side road vehicles (seconds)")]
        public float sideRoadGapSec = 3f;       // used only for side-road

        /// <summary>
        /// Determine if it's safe for a vehicle to enter the intersection
        /// Check TTC with all vehicles currently in the intersection
        /// </summary>
        protected override bool IsSafeToEnter(int vehId)
        {
            // If no vehicles in intersection, it's safe
            if (vehiclesInIntersection.Count == 0) return true;

            // Check TTC with all vehicles in intersection
            foreach (int otherVehId in vehiclesInIntersection)
            {
                if (otherVehId != vehId && TTC(otherVehId) < sideRoadGapSec)
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Calculate Time-To-Collision (TTC) with another vehicle using path intersection logic
        /// </summary>
        float TTC(int otherVehId)
        {
            // Get the stopped vehicle (the one requesting entry)
            var stoppedRadio = V2XBus.I?.FindRadioByVehicleId(waitingVehicles.Count > 0 ? waitingVehicles.Peek() : -1);
            if (stoppedRadio == null) return 999;
            var stoppedSpline = stoppedRadio.GetComponent<splineMove>();
            if (stoppedSpline == null) return 999;

            // Get the moving vehicle (other)
            var otherRadio = V2XBus.I?.FindRadioByVehicleId(otherVehId);
            if (otherRadio == null) return 999;
            var spline = otherRadio.GetComponent<splineMove>();
            if (spline == null) return 999;

            // Stopped vehicle (requesting entry)
            Vector3 posA = stoppedRadio.transform.position;
            Vector3 dirA = stoppedRadio.transform.forward;

            // Moving vehicle (other)
            Vector3 posB = otherRadio.transform.position;
            Vector3 velocityB = spline.Velocity;
            float speedB = velocityB.magnitude;
            Vector3 dirB = speedB > 0 ? velocityB.normalized : Vector3.forward;

            // Project to XZ plane
            Vector2 pA = new Vector2(posA.x, posA.z);
            Vector2 dA = new Vector2(dirA.x, dirA.z).normalized;
            Vector2 pB = new Vector2(posB.x, posB.z);
            Vector2 dB = new Vector2(dirB.x, dirB.z).normalized;

            float denom = dA.x * dB.y - dA.y * dB.x;
            if (Mathf.Abs(denom) < 1e-5f)
            {
                // Parallel lines, no intersection
                return 999;
            }

            float tA = ((pB.x - pA.x) * dB.y - (pB.y - pA.y) * dB.x) / denom;
            float tB = ((pA.x - pB.x) * dA.y - (pA.y - pB.y) * dA.x) / -denom;
            Vector2 pCol2D = pA + tA * dA;

            // Only consider intersection if it's ahead of both vehicles
            if (tA < -1f || tB < -1f) // allow small negative for numerical error
                return 999;

            // Distance and TTC for B
            float distB = (pCol2D - pB).magnitude;
            float ttcB = speedB > 0 ? distB / speedB : 999;
            return ttcB;
        }
    }
}
