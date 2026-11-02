using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using EVP;
using DG.Tweening;

public class ScenarioSelector : MonoBehaviour
{
    public List<GameObject> carModels; // Assign the car prefab in the inspector
    private GameObject carObj; // The car object to be spawned
    public RectTransform spawnPoint1; // Assign spawn point for option 1
    public RectTransform spawnPoint2; // Assign spawn point for option 2
    public Canvas uiCanvas; // Assign your Canvas here
    private Camera carCamera; // Assign the car camera dynamically or find it in code

    public GameObject scenarioUI; // Drag the scenarioUI object here
    public GameObject vehicleUI; // Drag the vehicleUI object here
    public GameObject inGameUI; // Drag the InGameUI object here

    public NpcController spawner;

    public GameObject arrowGroup1; // The parent object holding all arrows for scenario 1
    public GameObject arrowGroup2; // The parent object holding all arrows for scenario 2
    private GameObject[] arrows;
    public float fadeDistance = 10f; // Distance at which arrows start fading

    private int scenarioIndex = 0; // Index to track the current scenario

    public TMPro.TMP_Text controlModeButtonLabel;
    private int controlModeIndex = 1; // default to steering wheel
    private string[] controlModeNames = { "Keyboard", "Steering Wheel" };

    // Score tracking fields
    private Dictionary<GameObject, float> minimumDistances = new Dictionary<GameObject, float>();
    private float lastScore = -1f; // -1 means no score yet
    private bool hasAttempted = false;
    private bool hasFinished = false;
    
    // Score calculation thresholds
    public float minDistanceThreshold = 2f; // Distance below this = 100 score
    public float maxDistanceThreshold = 8f; // Distance above this = 80 score
    
    // Score display UI
    public TMPro.TMP_Text scoreText; // Assign in inspector
    
    // Red light violation tracking - organized by scenario like arrows
    public GameObject redLightDetectorGroup1; // Parent holding all detectors for scenario 1
    public GameObject redLightDetectorGroup2; // Parent holding all detectors for scenario 2
    private RedLightDetector[] activeRedLightDetectors; // Currently active detectors
    private int redLightViolations = 0;
    private int collisionCount = 0;

    // -------Initialization-------
    void Start()
    {
        carObj = carModels[0]; // Default to the first car model
        DeactivateVehicleAudio();
    }

    public void SetScenarioIndex(int index)
    {
        scenarioIndex = index;
        
        // Reset tracking when selecting a different scenario
        hasAttempted = false;
        hasFinished = false;
        minimumDistances.Clear();
        redLightViolations = 0;
        collisionCount = 0;

        if (vehicleUI != null)
        {
            vehicleUI.SetActive(true);
        }
        if (scenarioUI != null)
        {
            scenarioUI.SetActive(false);
        }
        
        // Update score display when returning to scenarioUI
        UpdateScoreDisplay();
    }

    public void SetCar(int index)
    {
        // Safety check to ensure 'index' is within the list range
        if (index < 0 || index >= carModels.Count)
        {
            Debug.LogWarning("Index is out of range!");
            return;
        }

        for (int i = 0; i < carModels.Count; i++)
        {
            // Activate the object if its index matches; deactivate otherwise
            carModels[i].SetActive(i == index);
        }

        // Assign the car prefab to the carObj variable
        carObj = carModels[index];

        // Set the control mode on the new car to match the current mode
        var vehicleInput = carObj.GetComponent<VehicleStandardInput>();
        if (vehicleInput != null)
        {
            vehicleInput.SetControlMode((VehicleStandardInput.ControlMode)controlModeIndex);
        }

        DeactivateVehicleAudio();

        // Reset tracking for new scenario attempt
        hasAttempted = true;
        hasFinished = false;
        minimumDistances.Clear();
        redLightViolations = 0;
        collisionCount = 0;

        if (scenarioIndex == 1)
        {
            SpawnCar(spawnPoint1);
            SpawnArrows(arrowGroup1);
            ActivateRedLightDetectors(redLightDetectorGroup1);
            SetupCollisionDetector();
        }
        else if (scenarioIndex == 2)
        {
            SpawnCar(spawnPoint2);
            SpawnArrows(arrowGroup2);
            ActivateRedLightDetectors(redLightDetectorGroup2);
            SetupCollisionDetector();
        }

        ActivateVehicleAudio();
    }

    // -------Car and Arrow Spawning-------
    private void SpawnCar(Transform spawnPoint)
    {
        SpawnNPC();
        // Spawn the car at the spawn point
        RectTransform carRectTransform = carObj.GetComponent<RectTransform>();
        carRectTransform.position = spawnPoint.position;
        carRectTransform.rotation = spawnPoint.rotation;

        // Find the camera in the car
        Transform cameraTransform = carObj.transform.Find("Cameras_Mirrors/EgoCarMainCamera");
        if (cameraTransform != null)
        {
            carCamera = cameraTransform.GetComponent<Camera>();
            if (carCamera != null)
            {
                SwitchToCarCamera(carCamera);
            }
            else
            {
                Debug.LogWarning("EgoCarMainCamera does not have a Camera component!");
            }
        }
        else
        {
            Debug.LogWarning("EgoCarMainCamera not found under Car/Cameras_Mirrors!");
        }
    }

    private void SpawnArrows(GameObject group)
    {
        // Clear the references in the arrows array
        if (arrows != null)
        {
            for (int i = 0; i < arrows.Length; i++)
            {
                arrows[i] = null;  // Remove the reference to each GameObject
            }
        }

        // Get all arrows under the arrowGroup
        arrows = new GameObject[group.transform.childCount];
        for (int i = 0; i < group.transform.childCount; i++)
        {
            arrows[i] = group.transform.GetChild(i).gameObject;
        }

        foreach (GameObject arrow in arrows)
        {
            arrow.SetActive(true);
        }
    }

    void Update()
    {
        if (arrows == null)
        {
            return;
        }
        foreach (GameObject arrow in arrows)
        {
            // Get the distance to the player
            float distance = Vector3.Distance(carObj.transform.position, arrow.transform.position);

            // Track minimum distance for scoring
            if (hasAttempted && !hasFinished)
            {
                if (!minimumDistances.ContainsKey(arrow))
                {
                    minimumDistances[arrow] = float.MaxValue;
                }
                if (distance < minimumDistances[arrow])
                {
                    minimumDistances[arrow] = distance;
                }
            }

            ArrowFade fadeScript = arrow.GetComponent<ArrowFade>();

            if (fadeScript == null)
            {
                continue;
            }

            // If the car is close enough, start fading the arrow
            if (distance < fadeDistance)
            {
                fadeScript.StartFading();
            }

            if (distance < 1f)
            {
                fadeScript.SetArrowTransparency(0f);
            }
        }
    }

    public void ResetArrows(bool deactivate)
    {
        foreach (GameObject arrow in arrows)
        {
            // Only reset transparency if the arrow has the ArrowFade script
            ArrowFade fadeScript = arrow.GetComponent<ArrowFade>();
            if (fadeScript != null)
            {
                fadeScript.ResetFade();
            }
            arrow.SetActive(!deactivate); // Do not deactivate arrows during in-game reset
        }
    }

    private void SpawnNPC()
    {
        spawner.StartAllCars();
    }

    // -------Button Event Listeners-------
    // Event listener for the Back button
    public void OnBackButtonClicked()
    {
        DOTween.KillAll();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnBackScenarioButtonClicked()
    {
        // Show the initial menu UI and hide the in-game UI
        if (scenarioUI != null)
        {
            scenarioUI.SetActive(true);
        }

        if (vehicleUI != null)
        {
            vehicleUI.SetActive(false);
        }
        
        // Update score display when returning to scenarioUI
        UpdateScoreDisplay();
    }

    // Event listener for the in game Menu button
    public void OnMenuButtonClicked(bool finish = false)
    {
        // Calculate score if finishing scenario (crossing finish gate)
        if (finish && hasAttempted)
        {
            hasFinished = true;
            lastScore = CalculateDrivingScore();
            // Clear the minimum distance dictionary for next attempt
            minimumDistances.Clear();
        }
        
        if (!finish) ResetArrows(true);
        DeactivateVehicleAudio();

        // Reset the ego vehicle's position and rotation
        VehicleController vehicleController = carObj.GetComponent<VehicleController>();
        vehicleController.ResetVehicle();

        Transform cameraTransform = carObj.transform.Find("Cameras_Mirrors/EgoCarMainCamera");
        CameraSwitch cameraSwitch = cameraTransform.GetComponent<CameraSwitch>();
        if (cameraSwitch != null)
        {
            cameraSwitch.ResetCamera();
        }

        // Show the initial menu UI and hide the in-game UI
        if (scenarioUI != null)
        {
            scenarioUI.SetActive(true);
        }

        if (inGameUI != null)
        {
            inGameUI.SetActive(false);
        }
        
        // Update score display when returning to scenarioUI
        UpdateScoreDisplay();
    }

    public void OnResetButtonClicked()
    {
        ResetArrows(false);
        // Reset the ego vehicle's position and rotation
        VehicleController vehicleController = carObj.GetComponent<VehicleController>();
        vehicleController.ResetVehicle();

        // Reset red light violations for retry attempt
        redLightViolations = 0;
        collisionCount = 0;

        // Spawn the car at the spawn point
        RectTransform carRectTransform = carObj.GetComponent<RectTransform>();
        RectTransform spawnPoint = scenarioIndex == 1 ? spawnPoint1 : spawnPoint2;
        carRectTransform.position = spawnPoint.position;
        carRectTransform.rotation = spawnPoint.rotation;

        Transform cameraTransform = carObj.transform.Find("Cameras_Mirrors/EgoCarMainCamera");
        CameraSwitch cameraSwitch = cameraTransform.GetComponent<CameraSwitch>();
        if (cameraSwitch != null)
        {
            cameraSwitch.ResetCamera();
        }
    }

    public void OnControlModeButtonClicked()
    {
        // Toggle mode
        controlModeIndex = (controlModeIndex + 1) % 2;
        var vehicleInput = carObj.GetComponent<VehicleStandardInput>();
        if (vehicleInput != null)
        {
            vehicleInput.SetControlMode((VehicleStandardInput.ControlMode)controlModeIndex);
        }
        // Update button label
        if (controlModeButtonLabel != null)
        {
            controlModeButtonLabel.text = $"{controlModeNames[controlModeIndex]} Mode";
        }
    }

    // -------Helpers-------
    // Activate the Vehicle Audio of the ego vehicle
    public void ActivateVehicleAudio()
    {
        VehicleAudio egoVehicleAudio = carObj.GetComponent<VehicleAudio>();
        if (egoVehicleAudio != null)
        {
            egoVehicleAudio.enabled = true;
        }
    }

    // Deactivate the Vehicle Audio of the ego vehicle
    public void DeactivateVehicleAudio()
    {
        VehicleAudio egoVehicleAudio = carObj.GetComponent<VehicleAudio>();
        if (egoVehicleAudio != null)
        {
            egoVehicleAudio.enabled = false;
        }
    }
    
    // Calculate driving score based on minimum distances to arrows
    private float CalculateDrivingScore()
    {
        float baseScore;
        
        if (minimumDistances.Count == 0)
        {
            baseScore = 80f; // Default score if no arrows tracked
        }
        else
        {
            // Calculate average of all minimum distances
            float totalDistance = 0f;
            foreach (float distance in minimumDistances.Values)
            {
                totalDistance += distance;
            }
            float averageDistance = totalDistance / minimumDistances.Count;
            
            // Apply scoring formula: 80-100 range
            if (averageDistance <= minDistanceThreshold)
            {
                baseScore = 100f;
            }
            else if (averageDistance >= maxDistanceThreshold)
            {
                baseScore = 80f;
            }
            else
            {
                // Linear interpolation between 100 and 80
                float normalizedDistance = (averageDistance - minDistanceThreshold) / (maxDistanceThreshold - minDistanceThreshold);
                baseScore = 100f - (normalizedDistance * 20f);
            }
        }
        
        // Apply red light penalties (5 points each)
        float scoreAfterRedLights = baseScore - (redLightViolations * 5f);
        
        // Apply collision penalties (5 points each)
        float finalScore = scoreAfterRedLights - (collisionCount * 5f);
        
        // Ensure minimum score of 80
        return Mathf.Max(finalScore, 80f);
    }
    
    // Record red light violation
    public void RecordRedLightViolation()
    {
        if (hasAttempted && !hasFinished)
        {
            redLightViolations++;
        }
    }
    
    // Record collision with NPC vehicle
    public void RecordCollision()
    {
        if (hasAttempted && !hasFinished)
        {
            collisionCount++;
        }
    }
    
    // Setup collision detector for scenario scoring
    private void SetupCollisionDetector()
    {
        // Try adding to the main car GameObject first
        ScenarioCollisionDetector detector = carObj.GetComponent<ScenarioCollisionDetector>();
        if (detector == null)
        {
            detector = carObj.AddComponent<ScenarioCollisionDetector>();
        }
        
        // Initialize the detector
        detector.Initialize(this);
        
        // Also try adding to ColliderBody as backup
        Transform collidersTransform = carObj.transform.Find("Colliders");
        if (collidersTransform != null)
        {
            Transform colliderTransform = collidersTransform.Find("ColliderBody");
            if (colliderTransform != null)
            {
                // Check if ScenarioCollisionDetector already exists
                ScenarioCollisionDetector detector2 = colliderTransform.GetComponent<ScenarioCollisionDetector>();
                if (detector2 == null)
                {
                    // Add the component if it doesn't exist
                    detector2 = colliderTransform.gameObject.AddComponent<ScenarioCollisionDetector>();
                }
                // Initialize the detector
                detector2.Initialize(this);
            }
        }
    }
    
    // Activate red light detectors for the current scenario
    private void ActivateRedLightDetectors(GameObject group)
    {
        // Clear previous detectors
        activeRedLightDetectors = null;
        
        // Reset violations when activating new detectors
        redLightViolations = 0;
        collisionCount = 0;
        
        // First, deactivate all detectors from both scenarios to prevent cross-scenario violations
        if (redLightDetectorGroup1 != null)
        {
            RedLightDetector[] group1Detectors = redLightDetectorGroup1.GetComponentsInChildren<RedLightDetector>();
            foreach (var detector in group1Detectors)
            {
                detector.gameObject.SetActive(false);
            }
        }
        
        if (redLightDetectorGroup2 != null)
        {
            RedLightDetector[] group2Detectors = redLightDetectorGroup2.GetComponentsInChildren<RedLightDetector>();
            foreach (var detector in group2Detectors)
            {
                detector.gameObject.SetActive(false);
            }
        }
        
        // If no group assigned (maps without traffic lights), skip
        if (group == null) 
        {
            return;
        }
        
        // Get all RedLightDetector components under the current scenario's group
        // Include inactive GameObjects to find components even when objects are deactivated
        activeRedLightDetectors = group.GetComponentsInChildren<RedLightDetector>(true);
        
        // Activate only the detectors for the current scenario
        foreach (var detector in activeRedLightDetectors)
        {
            detector.gameObject.SetActive(true);
        }
    }
    
    // Update score display text
    private void UpdateScoreDisplay()
    {
        if (scoreText == null) return;
        
        if (!hasAttempted)
        {
            scoreText.text = "Score: --";
        }
        else if (hasAttempted && !hasFinished)
        {
            scoreText.text = "Score: --";
        }
        else if (hasAttempted && hasFinished)
        {
            // Check if there are red light detectors for this scenario
            GameObject detectorGroup = scenarioIndex == 1 ? redLightDetectorGroup1 : redLightDetectorGroup2;
            bool hasRedLightDetectors = detectorGroup != null;
            
            if (hasRedLightDetectors)
            {
                scoreText.text = $"Last Score: {lastScore:F1}\nRed Lights: {redLightViolations}\nCollisions: {collisionCount}";
            }
            else
            {
                scoreText.text = $"Last Score: {lastScore:F1}\nCollisions: {collisionCount}";
            }
        }
    }
    
    private void SwitchToCarCamera(Camera carCamera)
    {
        // Enable the car's camera
        carCamera.enabled = true;
        carCamera.targetDisplay = 0;
        carCamera.gameObject.SetActive(true); // Ensure the camera GameObject is active

        // Update the canvas to use the new camera
        if (uiCanvas != null)
        {
            uiCanvas.worldCamera = carCamera;
        }

        // Hide the initial menu UI and show the in-game UI
        if (vehicleUI != null)
        {
            vehicleUI.SetActive(false);
        }

        if (inGameUI != null)
        {
            inGameUI.SetActive(true);
        }
    }
}
