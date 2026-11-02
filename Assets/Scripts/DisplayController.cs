using EVP;
using UnityEngine;
using UnityEngine.UI;    // If using the default Text UI
using TMPro;         // If using TextMeshPro

public class DisplayController : MonoBehaviour
{
    public Rigidbody carRigidbody;
    public VehicleStandardInput vehicleInput;
    public Text speedText;
    public Text driveGearText;
    public Text reverseGearText;

    // On-screen display for 3rd person view (on screen-space canvas)
    public TMP_Text osdSpeedText;
    public TMP_Text osdDriveGearText;
    public TMP_Text osdReverseGearText;
    public GameObject osdDisplayPanel; // Parent panel containing OSD elements

    // Reference to camera switch to detect POV changes
    public CameraSwitch cameraSwitch;

    // Conversion from m/s to mph:
    // 1 m/s ~= 2.23694 mph
    private float conversionRate = 2.23694f;

    void Update()
    {
        // ------ Speed Calculation ------
        float speedInMetersPerSecond = carRigidbody.linearVelocity.magnitude;
        float speedInMph = speedInMetersPerSecond * conversionRate;
        string speedString = Mathf.RoundToInt(speedInMph).ToString() + " mph";
        
        // Update cabin display (always update, visibility controlled by camera)
        if (speedText != null)
        {
            speedText.text = speedString;
        }
        
        // Update OSD if available
        if (osdSpeedText != null)
        {
            osdSpeedText.text = speedString;
        }
        
        // ------ Gear Display ------
        bool isReverse = vehicleInput.reverse;
        
        // Update cabin gear display
        UpdateGearDisplay(driveGearText, reverseGearText, isReverse);
        
        // Update OSD gear display
        UpdateGearDisplay(osdDriveGearText, osdReverseGearText, isReverse);
        
        // ------ Show/Hide OSD based on camera view ------
        if (osdDisplayPanel != null && cameraSwitch != null)
        {
            // Show OSD in 3rd person, hide in 1st person
            osdDisplayPanel.SetActive(!cameraSwitch.IsFirstPerson());
        }
    }

    private void UpdateGearDisplay(Text driveText, Text reverseText, bool isReverse)
    {
        if (driveText == null || reverseText == null) return;
        
        if (isReverse)
        {
            Color driveColor = driveText.color;
            Color reverseColor = reverseText.color;
            reverseColor.a = 1f;
            driveColor.a = 0.25f;
            reverseText.color = reverseColor;
            driveText.color = driveColor;
        }
        else
        {
            Color driveColor = driveText.color;
            Color reverseColor = reverseText.color;
            driveColor.a = 1f;
            reverseColor.a = 0.25f;
            driveText.color = driveColor;
            reverseText.color = reverseColor;
        }
    }

    private void UpdateGearDisplay(TMP_Text driveText, TMP_Text reverseText, bool isReverse)
    {
        if (driveText == null || reverseText == null) return;
        
        if (isReverse)
        {
            Color driveColor = driveText.color;
            Color reverseColor = reverseText.color;
            reverseColor.a = 1f;
            driveColor.a = 0.25f;
            reverseText.color = reverseColor;
            driveText.color = driveColor;
        }
        else
        {
            Color driveColor = driveText.color;
            Color reverseColor = reverseText.color;
            driveColor.a = 1f;
            reverseColor.a = 0.25f;
            driveText.color = driveColor;
            reverseText.color = reverseColor;
        }
    }
}
