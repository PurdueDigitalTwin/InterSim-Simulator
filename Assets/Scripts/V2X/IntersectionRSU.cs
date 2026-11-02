using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static V2X.RsuCmd;

namespace V2X
{
    /// <summary>
    /// Abstract base class for Road Side Units (RSUs) that manage intersection access.
    /// Tracks vehicles in intersection zone and manages entry requests from stop zones.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public abstract class IntersectionRSU : MonoBehaviour
    {
        [Header("Timing")]
        [Tooltip("How often to check for grants (Hz)")]
        public float grantCheckHz = 10f;

        /* —– internals —– */
        /// <summary>
        /// Queue of vehicles waiting to enter the intersection (from stop zones)
        /// </summary>
        protected readonly Queue<int> waitingVehicles = new();
        
        /// <summary>
        /// Set of vehicles currently inside the intersection zone
        /// </summary>
        protected readonly HashSet<int> vehiclesInIntersection = new();

        float _accum;

        /// <summary>
        /// Process incoming messages from vehicles
        /// </summary>
        public void RadioInbox(in RsuMessage msg)
        {
            switch (msg.cmd)
            {
                case RequestEntry:
                    // Add vehicle to waiting queue if not already present
                    if (!waitingVehicles.Contains(msg.vehId)) 
                    {
                        waitingVehicles.Enqueue(msg.vehId);
                    }
                    break;
                case Clear:
                    // Vehicle has cleared the intersection
                    vehiclesInIntersection.Remove(msg.vehId);
                    break;
            }
        }

        /// <summary>
        /// Main update loop - checks for grants at specified frequency
        /// </summary>
        void Update()
        {
            _accum += Time.deltaTime;
            if (_accum < 1f / grantCheckHz) return;
            _accum = 0;
            CheckWaitingVehicles();
        }

        /// <summary>
        /// Check if waiting vehicles can safely enter the intersection
        /// </summary>
        void CheckWaitingVehicles()
        {
            if (waitingVehicles.Count == 0) return;
            
            int candidate = waitingVehicles.Peek();
            if (IsSafeToEnter(candidate))
            {
                // Send grant
                Send(candidate, Grant);
                waitingVehicles.Dequeue();
            }
            else
            {
                // Send wait command
                Send(candidate, Wait);
            }
        }

        /// <summary>
        /// Send a message to a specific vehicle
        /// </summary>
        protected void Send(int vehId, RsuCmd cmd)
        {
            var targetRadio = V2XBus.I?.FindRadioByVehicleId(vehId);
            if (targetRadio != null)
            {
                targetRadio.Receive(new RsuMessage(vehId, cmd));
            }
        }

        /* —– To be subclassed —– */
        /// <summary>
        /// Determine if it's safe for a vehicle to enter the intersection.
        /// Check TTC with vehicles currently in intersection.
        /// </summary>
        protected abstract bool IsSafeToEnter(int vehId);

        /// <summary>
        /// Called when a vehicle enters the intersection zone
        /// </summary>
        void OnTriggerEnter(Collider c)
        {
            var radio = c.GetComponentInParent<V2XRadio>();
            if (radio != null)
            {
                vehiclesInIntersection.Add(radio.VehicleId);
            }
        }

        /// <summary>
        /// Called when a vehicle exits the intersection zone
        /// </summary>
        void OnTriggerExit(Collider c)
        {
            var radio = c.GetComponentInParent<V2XRadio>();
            if (radio != null)
            {
                vehiclesInIntersection.Remove(radio.VehicleId);
            }
        }
    }
}
