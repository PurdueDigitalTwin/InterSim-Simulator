using EVP;
using SWS;
using UnityEngine;

// EgoCollisionDetector for handling collision detection between ego vehicle and NPC vehicles
public class EgoCollisionDetector : MonoBehaviour
{
    private SimulationDataLogger dataLogger;
    private LayerMask vehicleLayerMask;
    private bool isInitialized = false;
    
    public void Initialize(SimulationDataLogger logger, LayerMask layerMask)
    {
        dataLogger = logger;
        vehicleLayerMask = layerMask;
        isInitialized = true;
        Debug.Log($"EgoCollisionDetector initialized on {gameObject.name}");
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("EgoCollisionDetector not initialized yet");
            return;
        }
        
        if (dataLogger == null || !dataLogger.logCollisions) 
        {
            Debug.LogWarning("DataLogger is null or collisions not enabled");
            return;
        }
        
        Debug.Log($"Ego Collision entered with: {collision.gameObject.name} (Layer: {collision.gameObject.layer})");
        
        // Check if the colliding object is on the vehicle layer
        if (((1 << collision.gameObject.layer) & vehicleLayerMask) != 0)
        {
            Debug.Log($"Vehicle layer match detected with: {collision.gameObject.name}");
            
            // Check if this is an NPC vehicle (not the ego vehicle itself)
            VehicleController egoController = GetComponent<VehicleController>();
            VehicleController otherController = collision.gameObject.GetComponent<VehicleController>();
            
            // If the other object has a VehicleController, it's the ego vehicle (self-collision)
            // If it doesn't have a VehicleController but is on vehicle layer, it's likely an NPC
            if (otherController == null)
            {
                Debug.Log($"Processing collision with NPC vehicle: {collision.gameObject.name}");
                
                // Get collision information
                string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                Vector3 collisionPoint = collision.contacts[0].point; // Get actual collision point
                
                // Get velocities
                Vector3 egoVelocity = Vector3.zero;
                Vector3 npcVelocity = Vector3.zero;
                
                // Get ego vehicle velocity
                if (egoController != null)
                {
                    egoVelocity = egoController.cachedRigidbody.linearVelocity;
                }
                
                // Get NPC vehicle velocity
                splineMove npcSplineMove = collision.gameObject.GetComponent<splineMove>();
                if (npcSplineMove != null)
                {
                    npcVelocity = npcSplineMove.Velocity;
                }
                else
                {
                    Rigidbody npcRb = collision.gameObject.GetComponent<Rigidbody>();
                    if (npcRb != null)
                    {
                        npcVelocity = npcRb.linearVelocity;
                    }
                }
                
                // Log the collision event
                dataLogger.LogCollisionEvent(timestamp, collisionPoint, egoVelocity, npcVelocity);
            }
            else
            {
                Debug.Log("Ignoring self-collision");
            }
        }
        else
        {
            Debug.Log($"Layer mismatch - expected vehicle layer, got layer {collision.gameObject.layer}");
        }
    }
}
