using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using EVP;
using DG.Tweening;

public class TutorialController : MonoBehaviour
{
    public static string targetSceneAfterTutorial = ""; // Static variable to store target scene
    
    public GameObject carObj; // The pre-placed car object in the scene
    private Vector3 initialPosition; // Store initial spawn position
    private Quaternion initialRotation; // Store initial spawn rotation

    public GameObject inGameUI; // Drag the InGameUI object here

    public GameObject arrowGroup1; // The parent object holding all arrows for the tutorial path
    private GameObject[] arrows;
    public float fadeDistance = 10f; // Distance at which arrows start fading

    public TMPro.TMP_Text controlModeButtonLabel;
    private int controlModeIndex = 1; // default to steering wheel
    private string[] controlModeNames = { "Keyboard", "Steering Wheel" };

    // -------Initialization-------
    void Start()
    {
        // Store the initial position and rotation of the pre-placed vehicle
        if (carObj != null)
        {
            initialPosition = carObj.transform.position;
            initialRotation = carObj.transform.rotation;
        }
        
        // Initialize arrows from the tutorial path
        if (arrowGroup1 != null)
        {
            SpawnArrows(arrowGroup1);
        }
        
        // Ensure vehicle audio is enabled from start
        ActivateVehicleAudio();
    }

    // -------Arrow Spawning-------

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

    // -------Button Event Listeners-------
    // Event listener for the in game Menu button
    public void OnMenuButtonClicked(bool finish = false)
    {
        if (!finish) ResetArrows(true);
        
        // Clean up DOTween animations
        DOTween.KillAll();
        
        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    public void OnResetButtonClicked()
    {
        ResetArrows(false);
        
        // Reset the ego vehicle's position and rotation to initial spawn position
        VehicleController vehicleController = carObj.GetComponent<VehicleController>();
        vehicleController.ResetVehicle();

        // Move the car back to its initial position
        carObj.transform.position = initialPosition;
        carObj.transform.rotation = initialRotation;

        // Reset the camera
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
}
