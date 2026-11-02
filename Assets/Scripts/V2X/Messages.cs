using UnityEngine;

namespace V2X
{
    /// <summary>
    /// Basic Safety Message - Standard V2X message containing vehicle state information
    /// Broadcasted periodically by vehicles to inform other vehicles and infrastructure
    /// </summary>
    public struct BSM                 // Basic Safety Message
    {
        public int   id;              // Vehicle identifier
        public float time;            // Time.time at transmission
        public Unity.Mathematics.float3 pos;  // Vehicle position in world coordinates
        public Unity.Mathematics.float3 vel;  // Vehicle velocity vector
        public float headingDeg;      // Vehicle heading in degrees

        public BSM(int id, Vector3 p, Vector3 v, float h)
        {
            this.id = id;
            time = Time.time;
            pos = p;
            vel = v;
            headingDeg = h;
        }
    }

    /// <summary>
    /// Road Side Unit commands for intersection management
    /// </summary>
    public enum RsuCmd : byte 
    { 
        RequestEntry,  // Vehicle requests permission to enter intersection
        Grant,         // RSU grants permission to enter
        Wait,          // RSU tells vehicle to wait
        Clear          // Vehicle signals it has cleared the intersection
    }

    /// <summary>
    /// Message sent between vehicles and Road Side Units (RSUs)
    /// Used for intersection access control and coordination
    /// </summary>
    public struct RsuMessage
    {
        public int   vehId;           // Target vehicle ID
        public RsuCmd cmd;            // Command to execute
        public float  timestamp;      // Message timestamp for tracking

        public RsuMessage(int id, RsuCmd c)
        {
            vehId = id;
            cmd = c;
            timestamp = Time.time;
        }
    }

    // ===== Traffic light SPaT (Signal Phase And Timing) =====
    public enum TlColor : byte { Red, Yellow, Green }

    public struct SpatMessage
    {
        public int vehId;                 // Target ego vehicle ID
        public TlColor current;           // Current light color for the movement
        public TlColor next;              // Next light color for the movement
        public float greenRemaining;      // Seconds until end of green (0 if not green)
        public float yellowRemaining;     // Seconds until end of yellow (0 if not yellow)
        public float redRemaining;        // Seconds until end of red (time until next green)
        public float timestamp;           // Time.time when computed

        public SpatMessage(int id, TlColor cur, TlColor nxt, float g, float y, float r)
        {
            vehId = id;
            current = cur;
            next = nxt;
            greenRemaining = g;
            yellowRemaining = y;
            redRemaining = r;
            timestamp = Time.time;
        }
    }

    // ===== Stop line location for distance computation =====
    public struct StopLineMessage
    {
        public int vehId;              // Target ego vehicle ID
        public Vector3 worldPosition;  // Stop line world position
        public float timestamp;        // Sent time

        public StopLineMessage(int id, Vector3 pos)
        {
            vehId = id;
            worldPosition = pos;
            timestamp = Time.time;
        }
    }
}
