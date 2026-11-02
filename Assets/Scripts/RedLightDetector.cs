using UnityEngine;

public class RedLightDetector : MonoBehaviour
{
    [Header("Red Light Reference")]
    public Renderer redLightRenderer; // Reference to the red light's renderer
    
    private ScenarioSelector scenarioSelector;
    
    void Awake()
    {
        // Find ScenarioSelector in the scene
        scenarioSelector = FindAnyObjectByType<ScenarioSelector>();
        if (scenarioSelector == null)
        {
            Debug.LogError("RedLightDetector: ScenarioSelector not found in scene!");
        }
        
        // Ensure this collider is a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogError("RedLightDetector: No Collider component found! Please add a Collider to this GameObject.");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger
        if (!other.transform.root.CompareTag("Player"))
        {
            return;
        }
        
        // Check if red light is ON (material name does NOT end with "OFF")
        if (redLightRenderer != null && redLightRenderer.sharedMaterial != null)
        {
            bool redLightIsOn = !redLightRenderer.sharedMaterial.name.EndsWith("OFF");
            
            if (redLightIsOn)
            {
                // Red light is ON - this is a violation
                if (scenarioSelector != null)
                {
                    scenarioSelector.RecordRedLightViolation();
                }
                else
                {
                    Debug.LogWarning("RedLightDetector: ScenarioSelector reference is null!");
                }
            }
        }
        else
        {
            Debug.LogWarning("RedLightDetector: Red light renderer or material is null!");
        }
    }
}
