using UnityEngine;
using static V2X.RsuCmd;

namespace V2X
{
    /// <summary>
    /// V2X Radio component that handles vehicle-to-vehicle and vehicle-to-infrastructure communication.
    /// Each vehicle should have one V2XRadio component attached to enable V2X communications.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class V2XRadio : MonoBehaviour
    {
        [Header("Vehicle Identification")]
        [Tooltip("Unique identifier for this vehicle - must be set at runtime")]
        public int VehicleId;                 // assign at runtime
        
        // Communication State
        public bool HasOutgoing { get; private set; }  // True when there's a message to send
        BSM _out;  // Outgoing Basic Safety Message

        Rigidbody _rb;

        /* —— API exposed to VehicleController —— */
        /// <summary>
        /// True when this vehicle has received a grant from an RSU
        /// </summary>
        public bool GrantReceived { get; private set; }

        /// <summary>
        /// Latest SPaT information received for this vehicle
        /// </summary>
        public bool HasSpat { get; private set; }
        public SpatMessage LatestSpat { get; private set; }

        /// <summary>
        /// Stop line world position and distance tracking
        /// </summary>
        public bool HasStopLine { get; private set; }
        public Vector3 StopLineWorldPosition { get; private set; }
        public float StopLineDistanceMeters { get; private set; }

        /// <summary>
        /// Reset the grant status (called when exiting intersection)
        /// </summary>
        public void ResetGrantStatus()
        {
            GrantReceived = false;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            TryRegister();
        }

        void Start()
        {
            TryRegister();
        }

        void TryRegister()
        {
            if (V2XBus.I != null && !V2XBus.I.radios.Contains(this))
            {
                V2XBus.I.Register(this);
            }
        }

        void OnDestroy()
        {
            if (V2XBus.I != null)
            {
                V2XBus.I.Unregister(this);
            }
        }

        /// <summary>
        /// Update vehicle state and prepare BSM for transmission
        /// </summary>
        void FixedUpdate()
        {
            _out = new BSM(VehicleId, transform.position, _rb.linearVelocity,
                           transform.eulerAngles.y);
            HasOutgoing = true;

            // Update stop line distance in real-time if we have a stop line
            if (HasStopLine)
            {
                StopLineDistanceMeters = Vector3.Distance(transform.position, StopLineWorldPosition);
            }
        }

        /// <summary>
        /// Get the current BSM and mark it as sent
        /// </summary>
        public void Flush(out BSM pkt)
        {
            pkt = _out;
            HasOutgoing = false;
        }

        /// <summary>
        /// Receive a message from the V2X bus
        /// </summary>
        public void Receive(in object packet)
        {
            if (packet is RsuMessage msg)
            {
                if (msg.vehId != VehicleId) return;

                if (msg.cmd == Grant) 
                {
                    GrantReceived = true;
                }
                return;
            }

            if (packet is SpatMessage spat)
            {
                if (spat.vehId != VehicleId) return;
                LatestSpat = spat;
                HasSpat = true;
                return;
            }

            if (packet is StopLineMessage sl)
            {
                if (sl.vehId != VehicleId) return;
                StopLineWorldPosition = sl.worldPosition;
                HasStopLine = true;
                // Immediately compute distance once
                StopLineDistanceMeters = Vector3.Distance(transform.position, StopLineWorldPosition);
                return;
            }
        }

        /// <summary>
        /// Send a message to a specific RSU (called by vehicle when inside intersection zone)
        /// </summary>
        public void SendToRsu(RsuCmd cmd, IntersectionRSU rsu)
        {
            rsu.RadioInbox(new RsuMessage(VehicleId, cmd));
        }
    }
}
