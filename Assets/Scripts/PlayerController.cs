using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    // For the POV switch
    public Transform firstPersonPosition; // Empty GameObject for first-person view
    public Transform thirdPersonPosition; // Empty GameObject for third-person view

    public float transitionSpeed = 2f; // Speed of transition between views

    private bool isFirstPerson = false;
    private float transitionProgress = 0f; // 0 means starting position, 1 means target position
    private Transform targetPosition;
    private bool isTransitioning = false;

    // For the camera rotation
    public Transform car;
    public float defaultOffsetAngle = 10f;
    private float rotationSpeed = 120f;
    private float rotationOffset = 60f;

    private float currentOffsetAngle = 0f;
    private float targetOffsetAngle = 0f;

    void Start()
    {
        targetPosition = thirdPersonPosition;
    }

    public void ResetCamera()
    {
        // Reset the camera to the third-person view and stop any transitions
        isFirstPerson = false;
        targetPosition = thirdPersonPosition;
        transform.position = targetPosition.position;
        transform.rotation = targetPosition.rotation;
        transitionProgress = 0f; // Reset transition progress
        isTransitioning = false;  // Stop transitioning
    }

    public bool IsFirstPerson()
    {
        return isFirstPerson;
    }

    void Update()
    {
        // ------ For POV switch ------
        if ((Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.JoystickButton3)) && !isTransitioning)
        {
            isFirstPerson = !isFirstPerson;
            targetPosition = isFirstPerson ? firstPersonPosition : thirdPersonPosition;
            transitionProgress = 0f; // Reset transition progress
            isTransitioning = true;  // Start transitioning
        }

        if (isTransitioning)
        {
            SmoothTransition();
        }

        // ------ For camera rotation ------
        // Turn left or right based on user input
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.JoystickButton11))
        {
            targetOffsetAngle = -rotationOffset;
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.JoystickButton10))
        {
            targetOffsetAngle = rotationOffset;
        }
        else
        {
            targetOffsetAngle = 0f;
        }

        // Smoothly move the offset angle toward the target
        currentOffsetAngle = Mathf.MoveTowards(
            currentOffsetAngle, 
            targetOffsetAngle, 
            rotationSpeed * Time.deltaTime
        );

        // Combine the car’s rotation with the user offset
        // This ensures we always follow the car’s orientation, plus whatever offset we currently have
        // around the Y-axis.
        transform.rotation = car.rotation * Quaternion.Euler(0f, defaultOffsetAngle + currentOffsetAngle, 0f);
    }

    void SmoothTransition()
    {
        if (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime * transitionSpeed;
            float smoothStep = Mathf.SmoothStep(0, 1, transitionProgress);

            // Smoothly interpolate position
            transform.position = Vector3.Lerp(transform.position, targetPosition.position, smoothStep);

            // Smoothly interpolate rotation (so the camera also turns to face the new position)
            transform.rotation = Quaternion.Slerp(transform.rotation, targetPosition.rotation, smoothStep);
        }
        else
        {
            isTransitioning = false; // Stop transitioning once complete
        }
    }
}