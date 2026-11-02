using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace V2X
{
    /// <summary>
    /// Central communication hub for V2X (Vehicle-to-Everything) communications.
    /// Manages all radio communications between vehicles and infrastructure.
    /// Implements a simple broadcast system where vehicles within range can communicate.
    /// </summary>
    public sealed class V2XBus : MonoBehaviour
    {
        /// <summary>
        /// Global singleton instance for easy access from anywhere
        /// </summary>
        public static V2XBus I { get; private set; }

        [Header("RF params")]
        [Tooltip("Maximum communication range in meters")]
        public float maxRange = 120f;
        [Tooltip("How often radios exchange packets (Hz)")]
        public float tickRate = 10f;

        /* —— internal —— */
        /// <summary>
        /// List of all registered V2X radios (vehicles and infrastructure)
        /// </summary>
        public readonly List<V2XRadio> radios = new();
        float _tickAccum;

        void Awake() => I = this;

        /// <summary>
        /// Register a new radio with the communication bus
        /// </summary>
        public void Register(V2XRadio r) => radios.Add(r);
        
        /// <summary>
        /// Unregister a radio from the communication bus
        /// </summary>
        public void Unregister(V2XRadio r) => radios.Remove(r);

        /// <summary>
        /// Find a radio by vehicle ID
        /// </summary>
        public V2XRadio FindRadioByVehicleId(int vehicleId)
        {
            return radios.Find(r => r.VehicleId == vehicleId);
        }

        /// <summary>
        /// Main communication loop - broadcasts messages between radios within range
        /// </summary>
        void Update()
        {
            _tickAccum += Time.deltaTime;
            if (_tickAccum < 1f / tickRate) return;
            _tickAccum = 0;

            // naïve O(n²) broadcast (fine up to ~200 cars).
            for (int i = 0; i < radios.Count; ++i)
            {
                var tx = radios[i];
                if (!tx.HasOutgoing) continue;

                tx.Flush(out var pkt);
                for (int j = 0; j < radios.Count; ++j)
                {
                    if (i == j) continue;
                    var rx = radios[j];
                    if (math.length(tx.transform.position - rx.transform.position) <= maxRange)
                        rx.Receive(pkt);
                }
            }
        }
    }
}
