using UnityEngine;

namespace V2X
{
    /// <summary>
    /// Trigger zone for stop signs that handles vehicle entry and exit.
    /// When a vehicle enters this zone, it will stop and request entry from the RSU.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class StopZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [Tooltip("Reference to the RSU that manages this intersection")]
        public IntersectionRSU rsu;

        void Start()
        {
            // Ensure the collider is set to trigger
            GetComponent<BoxCollider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            // Check if the entering object is a vehicle with TrafficControl
            var trafficControl = other.GetComponentInParent<TrafficControl>();
            if (trafficControl != null && rsu != null)
            {
                trafficControl.OnEnterStopZone(rsu);
            }
        }

        void OnTriggerExit(Collider other)
        {
            // Check if the exiting object is a vehicle with TrafficControl
            var trafficControl = other.GetComponentInParent<TrafficControl>();
            if (trafficControl != null)
            {
                trafficControl.OnExitStopZone();
            }
        }

        // Visual debugging
        void OnDrawGizmosSelected()
        {
            if (rsu != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, rsu.transform.position);
            }
        }
    }
} 