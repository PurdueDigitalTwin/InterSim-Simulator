using UnityEngine;

public class ScenarioCollisionDetector : MonoBehaviour
{
    private ScenarioSelector scenarioSelector;
    private bool isInitialized = false;
    
    public void Initialize(ScenarioSelector selector)
    {
        scenarioSelector = selector;
        isInitialized = true;
        
        // Check and fix Rigidbody collision detection settings
        Rigidbody rb = GetComponentInParent<Rigidbody>();
        if (rb != null && rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized || scenarioSelector == null)
        {
            return;
        }
        
        // Check if it's an NPC vehicle by looking for splineMove component
        SWS.splineMove splineComponent = collision.gameObject.GetComponent<SWS.splineMove>();
        if (splineComponent != null)
        {
            scenarioSelector.RecordCollision();
            return;
        }
    }
    
}
