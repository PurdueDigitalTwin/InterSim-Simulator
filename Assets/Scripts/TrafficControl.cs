using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using SWS;
using static V2X.RsuCmd;
using UnityEngine.SceneManagement;

public class TrafficControl : MonoBehaviour
{
    [Header("V2X Components")]
    [Tooltip("Reference to the V2X radio component")]
    public V2X.V2XRadio radio;

    [Header("V2X Intersection Settings")]
    [Tooltip("Layer mask for intersection triggers")]
    public LayerMask intersectionLayer = -1;
    [Tooltip("Distance from intersection to start requesting entry")]
    public float requestDistance = 15f;

    [Header("Stop Sign Settings")]
    [Tooltip("Distance from stop sign to start stopping")]
    public float stopDistance = 10f;
    [Tooltip("Duration for stop sign deceleration")]
    public float stopDecelDuration = 2.0f;

    private RadarCast radarCastScript;
    private RadarCast shortRangeRadarScript;
    private splineMove moveRef;
    
    // Radar rotation tracking
    private Quaternion originalShortRangeRadarRotation;
    private float originalShortRangeRadarRange;

    // V2X State
    private V2X.IntersectionRSU currentRSU;
    private bool hasRequestedEntry = false;
    private bool isAtStopSign = false;

    private bool pausedForCollision = false;      // Flag for collision avoidance pause
    private bool pausedForTrafficLight = false;   // Flag for traffic light pause
    private float randomDelay;                    // Random delay
    [HideInInspector]
    public float originalSpeed = 15.0f;          // The car's desired (max) speed
    private bool isSpeedingUp = false;            // Flag to track if the car is speeding up
    private bool pausedForBoxSignal = false;      // Flag for box B stop/go control

    // We keep references so we can stop only the *specific* coroutines
    private Coroutine speedChangeCoroutine = null;
    private Coroutine trafficLightCoroutine = null;
    private Coroutine resumeCoroutine = null;
    private Coroutine v2xStopCoroutine = null;
    private Coroutine boxSignalCoroutine = null;

    // Global variables
    private float radarSlowDownRange = 20.0f;
    private float radarStopRange = 7.0f;
    private float speedChangeDuration = 2.0f;
    private bool obstacleDetected = false;
    private float stopDistanceOriginal = 15.0f;

    void Start()
    {
        // Initialize V2X components
        if (radio == null)
            radio = GetComponent<V2X.V2XRadio>();
        
        // Set unique vehicle ID if V2X radio exists
        if (radio != null)
        {
            radio.VehicleId = Random.Range(1000, 9999);
        }

        // Retrieve key components
        Transform radarTransform = transform.Find("Radars/LongRangeFR");
        if (radarTransform != null)
            radarCastScript = radarTransform.GetComponent<RadarCast>();

        if (radarCastScript == null)
            Debug.LogError("RadarCast script not found in Radars/LongRangeFR");

        // Retrieve short-range radar component
        Transform shortRadarTransform = transform.Find("Radars/ShortRangeFR");
        if (shortRadarTransform != null)
        {
            shortRangeRadarScript = shortRadarTransform.GetComponent<RadarCast>();
            // Store original rotation and angle range for resetting later
            originalShortRangeRadarRotation = shortRadarTransform.localRotation;
            if (shortRangeRadarScript != null)
                originalShortRangeRadarRange = shortRangeRadarScript.range;
        }

        if (shortRangeRadarScript == null)
            Debug.LogWarning("ShortRangeRadar script not found in Radars/ShortRangeFR - lane change detection may be limited");

        moveRef = GetComponentInParent<splineMove>();
        if (moveRef == null)
            Debug.LogError("splineMove script not found on this object.");

        // Ensure the car starts at full speed (randomized)
        if (moveRef != null)
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (sceneName.StartsWith("RAB"))
            {
                originalSpeed = Random.Range(10.0f, 15.0f);
                radarStopRange = 5.0f;
            }
            else
            {
                originalSpeed = Random.Range(15.0f, 20.0f);
            }
            moveRef.ChangeSpeed(originalSpeed);
        }

        // Assign a random small delay for re-acceleration
        randomDelay = Random.Range(0.1f, 0.5f);
    }

    void Update()
    {
        if (pausedForBoxSignal) return;

        // Handle original obstacle detection (always active for player vehicle avoidance)
        HandleObstacleDetection();  
    }

    // --------Object Detection Speed Control--------
    /// <summary>
    /// Handle original obstacle detection logic
    /// </summary>
    void HandleObstacleDetection()
    {
        if (radarCastScript == null || moveRef == null)
            return;

        // Get detected objects from both radars and combine them
        List<RadarCast.DetectedObject> detectedObjects = new List<RadarCast.DetectedObject>();
        
        // Add objects from long-range radar
        if (radarCastScript.detectedObjects != null)
        {
            detectedObjects.AddRange(radarCastScript.detectedObjects);
        }
        
        // Add objects from short-range radar (if available)
        if (shortRangeRadarScript != null && shortRangeRadarScript.detectedObjects != null)
        {
            detectedObjects.AddRange(shortRangeRadarScript.detectedObjects);
        }

        if (detectedObjects.Count == 0)
        {
            obstacleDetected = false;
            // Gradually speed up to original speed if slower than original speed
            if (moveRef.speed < originalSpeed)
            {
                // Gradually increase speed using a small interpolation factor for smooth acceleration
                float accelerationRate = 0.5f; // Adjust this value to control how fast it speeds up (0.1 = slow, 1.0 = fast)
                moveRef.ChangeSpeed(Mathf.Lerp(moveRef.speed, originalSpeed, Time.deltaTime * accelerationRate));
            }
            return;
        }

        // Check for obstacles too close
        float closestObstacleDistance = radarSlowDownRange;
        foreach (var detectedObject in detectedObjects)
        {
            if (detectedObject.distance < closestObstacleDistance)
            {
                closestObstacleDistance = detectedObject.distance;
                obstacleDetected = true;
            }
        }

        if (closestObstacleDistance == radarSlowDownRange)
            obstacleDetected = false;

        bool isPaused = pausedForTrafficLight || pausedForCollision || pausedForBoxSignal || (moveRef.tween != null && moveRef.tween.IsActive() && moveRef.tween.IsPlaying() == false);

        // Don't apply obstacle detection if we're at a V2X stop sign (prioritize V2X stops)
        // But still allow basic collision avoidance to prevent vehicle overlap
        if (isAtStopSign)
        {
            // Only do basic collision avoidance when stopped
            if (closestObstacleDistance < radarStopRange + 0.5f)
            {
                moveRef.ChangeSpeed(0);
            }
            return;
        }

        if (!isPaused)
        {
            float stopBuffer = 0.4f;
            // If close to the stop range, since speed is very low, we can just stop instead of sliding
            if (closestObstacleDistance < radarStopRange + stopBuffer)
            {
                moveRef.ChangeSpeed(0);
            }
            else
            {
                // Smooth speed adjustment based on closest obstacle
                float minDistance = radarStopRange;
                float maxDistance = radarSlowDownRange;
                float t = Mathf.InverseLerp(minDistance, maxDistance, closestObstacleDistance);
                float speedFactor = Mathf.SmoothStep(0, 1, t);
                
                float maxSpeed = originalSpeed;
                
                float targetSpeed = Mathf.Lerp(0, maxSpeed, speedFactor);
                float minCrawlSpeed = 0f;
                targetSpeed = Mathf.Max(targetSpeed, minCrawlSpeed);
                float smoothness = 3.0f;
                float newSpeed = Mathf.Lerp(moveRef.speed, targetSpeed, Time.deltaTime * smoothness);
                moveRef.ChangeSpeed(newSpeed);
            }
        }
    }

    // --------V2X Stop Sign Control--------
    /// <summary>
    /// Coroutine to stop at stop sign
    /// </summary>
    IEnumerator StopAtStopSign()
    {
        // Slow down to stop
        float currentSpeed = moveRef.speed;
        yield return StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, stopDecelDuration));
        moveRef.Pause();
        
        // Send request to RSU
        if (currentRSU != null)
        {
            radio.SendToRsu(RequestEntry, currentRSU);
        }

        hasRequestedEntry = true;
        
        // Wait for grant
        while (!radio.GrantReceived)
        {
            yield return null;
        }

        // Resume when granted
        moveRef.Resume();
        StopV2XStopCoroutine();
        v2xStopCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
    }

    /// <summary>
    /// Reset V2X state when exiting intersection
    /// </summary>
    void ResetV2XState()
    {
        isAtStopSign = false;
        if (radio != null)
        {
            radio.ResetGrantStatus();
        }
    }
    
    public void YieldCar()
    {
        // If the traffic light is RED and the car is not paused for collision or traffic light, and no obstacle is detected, then pause the car
        if (moveRef.tween != null && moveRef.tween.IsActive() && !pausedForTrafficLight && !obstacleDetected)
        {
            // Stop any ongoing speed changes
            StopSpeedChangeCoroutine();
            StopResumeCoroutine();

            float currentSpeed = moveRef.speed;
            float duration = 1.5f * stopDistance / currentSpeed;

            // Decelerate to a stop over this duration
            speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, duration));
        }
    }

    // --------Coroutine Control--------
    public void StopV2XStopCoroutine()
    {
        if (v2xStopCoroutine != null)
        {
            StopCoroutine(v2xStopCoroutine);
            v2xStopCoroutine = null;
        }
    }

    public void StopSpeedChangeCoroutine()
    {
        if (speedChangeCoroutine != null)
        {
            StopCoroutine(speedChangeCoroutine);
            speedChangeCoroutine = null;
        }
    }

    public void StopResumeCoroutine()
    {
        if (resumeCoroutine != null)
        {
            StopCoroutine(resumeCoroutine);
            resumeCoroutine = null;
        }
    }

    public new void StopAllCoroutines()
    {
        StopSpeedChangeCoroutine();
        StopResumeCoroutine();
        StopV2XStopCoroutine();
        if (boxSignalCoroutine != null)
        {
            StopCoroutine(boxSignalCoroutine);
            boxSignalCoroutine = null;
        }
        if (trafficLightCoroutine != null)
        {
            StopCoroutine(trafficLightCoroutine);
            trafficLightCoroutine = null;
        }
    }

    // --------Helpers for smooth speed change--------
    public IEnumerator ResumeAfterRandomDelay()
    {
        yield return new WaitForSeconds(randomDelay);

        if (!pausedForTrafficLight)
        {
            moveRef.Resume();

            // Ensure we always start from 0
            moveRef.ChangeSpeed(0);
            yield return new WaitForSeconds(0.1f); // Short wait to ensure no conflicting coroutine

            if (!isSpeedingUp)
            {
                isSpeedingUp = true;
                StopSpeedChangeCoroutine();
                speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
            }
            pausedForCollision = false;
        }
    }

    public IEnumerator SmoothChangeSpeed(float fromSpeed, float toSpeed, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float newSpeed = Mathf.Lerp(fromSpeed, toSpeed, elapsedTime / duration);
            moveRef.ChangeSpeed(newSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        moveRef.ChangeSpeed(toSpeed); // Ensure final speed is exact
        isSpeedingUp = false;
    }

    // Overloaded version with callback
    public IEnumerator SmoothChangeSpeed(float fromSpeed, float toSpeed, float duration, System.Action onComplete)
    {
        yield return StartCoroutine(SmoothChangeSpeed(fromSpeed, toSpeed, duration));
        onComplete?.Invoke();
    }

    // --------Traffic Light Stop Control--------
    // When vehicle passes a waypoint at traffic light, will trigger this function
    public void PauseCar(Object target)
    {
        // Check if target is null
        if (target == null)
        {
            Debug.LogWarning("PauseCar called with null target");
            return;
        }

        GameObject targetGameObject = target as GameObject;
        if (targetGameObject == null)
        {
            Debug.LogWarning("PauseCar target is not a GameObject");
            return;
        }

        Renderer lightRenderer = targetGameObject.GetComponent<Renderer>();
        if (lightRenderer == null)
        {
            Debug.LogWarning("PauseCar target GameObject has no Renderer component");
            return;
        }

        // If the traffic light is RED and the car is not paused for collision or traffic light, and no obstacle is detected, then pause the car
        if (moveRef.tween != null && moveRef.tween.IsActive() && lightRenderer.sharedMaterial != null && lightRenderer.sharedMaterial.name.EndsWith("OFF") && !pausedForTrafficLight && !obstacleDetected)
        {
            // Stop any ongoing speed changes
            StopSpeedChangeCoroutine();
            StopResumeCoroutine();

            pausedForTrafficLight = true;

            float currentSpeed = moveRef.speed;
            float duration = 2 * stopDistanceOriginal / currentSpeed;

            // Decelerate to a stop over this duration, then pause
            speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, duration, () => {
                moveRef.Pause();
            }));

            if (trafficLightCoroutine != null) StopCoroutine(trafficLightCoroutine);
            trafficLightCoroutine = StartCoroutine(WaitForLightToTurnOn(lightRenderer, moveRef));
        }
    }

    private IEnumerator WaitForLightToTurnOn(Renderer lightRenderer, splineMove moveRef)
    {
        // Wait until the material changes from lightOff
        while (lightRenderer.sharedMaterial.name.EndsWith("OFF"))
        {
            yield return new WaitForSeconds(0.5f);
        }

        pausedForTrafficLight = false;

        // If not also paused for collision, then resume
        if (!pausedForCollision)
        {
            yield return new WaitForSeconds(randomDelay);
            moveRef.Resume();

            // Smoothly accelerate from 0 to original speed
            if (!isSpeedingUp)
            {
                isSpeedingUp = true;
                StopSpeedChangeCoroutine();
                speedChangeCoroutine = StartCoroutine(SmoothChangeSpeed(0, originalSpeed, speedChangeDuration));
            }
        }
    }

    // --------V2X Zone Handling--------
    /// <summary>
    /// Called when vehicle enters a stop zone
    /// </summary>
    public void OnEnterStopZone(V2X.IntersectionRSU rsu)
    {
        // Prevent multiple zone entries
        if (hasRequestedEntry && currentRSU == rsu)
        {
            return; // Already in this zone
        }
        
        currentRSU = rsu;
        
        isAtStopSign = true;
        StopV2XStopCoroutine();
        v2xStopCoroutine = StartCoroutine(StopAtStopSign());
    }

    /// <summary>
    /// Called when vehicle exits a stop zone
    /// </summary>
    public void OnExitStopZone()
    {
        // Only clear if we've actually been granted and are moving through the intersection
        // The IntersectionRSU will handle clearing when vehicle exits the intersection zone
        // Don't clear here - let the intersection zone handle it
        hasRequestedEntry = false;
        isAtStopSign = false;
    }

    // --------Reset Control--------
    public void ResetVehicle()
    {
        // Reset flags
        pausedForCollision = false;
        pausedForTrafficLight = false;
        pausedForBoxSignal = false;
        isSpeedingUp = false;
        obstacleDetected = false;
        ResetV2XState();

        // Stop all coroutines
        StopAllCoroutines();
        // Resume with smooth acceleration
        ResumeAfterRandomDelay();
    }

    // --------Box Signal (Box B) Control--------
    /// <summary>
    /// Called by Box B controller when car is in Box B and a STOP signal is active.
    /// Smoothly decelerates to 0 and keeps it paused until GO is received.
    /// </summary>
    public void BoxSignalStop()
    {
        if (moveRef == null)
            return;

        // Mark state
        pausedForBoxSignal = true;

        // Stop other speed-changing coroutines to avoid conflicts
        StopSpeedChangeCoroutine();
        StopResumeCoroutine();

        // If already paused or speed is 0, just ensure speed is 0
        float currentSpeed = moveRef.speed;
        if (currentSpeed <= 0.005f)
        {
            moveRef.ChangeSpeed(0);
            moveRef.Pause();
            return;
        }

        // Compute a smooth decel duration similar to existing patterns
        float duration = 2 * stopDistanceOriginal / Mathf.Max(currentSpeed, 0.05f);

        // Run deceleration and pause
        if (boxSignalCoroutine != null) StopCoroutine(boxSignalCoroutine);
        boxSignalCoroutine = StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, duration, () => {
            moveRef.Pause();
        }));
    }

    /// <summary>
    /// Variant with explicit deceleration duration override from Box B controller.
    /// If durationOverride <= 0, falls back to default computed duration.
    /// </summary>
    public void BoxSignalStop(float durationOverride)
    {
        if (moveRef == null)
            return;

        pausedForBoxSignal = true;
        StopSpeedChangeCoroutine();
        StopResumeCoroutine();

        float currentSpeed = moveRef.speed;
        if (currentSpeed <= 0.01f)
        {
            moveRef.ChangeSpeed(0);
            moveRef.Pause();
            return;
        }

        float duration = durationOverride > 0f
            ? durationOverride
            : (2 * stopDistanceOriginal / Mathf.Max(currentSpeed, 0.1f));

        if (boxSignalCoroutine != null) StopCoroutine(boxSignalCoroutine);
        boxSignalCoroutine = StartCoroutine(SmoothChangeSpeed(currentSpeed, 0, duration, () => {
            moveRef.Pause();
        }));
    }

    /// <summary>
    /// Called by Box B controller when car is in Box B and a GO signal is active.
    /// Resumes and smoothly accelerates back to original speed, provided no other pauses apply.
    /// </summary>
    public void BoxSignalGo()
    {
        if (moveRef == null)
            return;

        // Only proceed if box-signal was the reason to stop and no higher-priority pauses exist
        if (!pausedForBoxSignal)
            return;

        if (pausedForTrafficLight || pausedForCollision || isAtStopSign)
            return; // Other systems still hold the vehicle

        pausedForBoxSignal = false;

        // Resume and accelerate similar to traffic light logic
        if (boxSignalCoroutine != null)
        {
            StopCoroutine(boxSignalCoroutine);
            boxSignalCoroutine = null;
        }

        StartCoroutine(ResumeAfterRandomDelay());
    }

    /// <summary>
    /// Syncs the current box signal when entering Box B. If stop, initiate stop; if go, do nothing.
    /// </summary>
    public void OnEnterBoxSignalZone(bool isGoSignal)
    {
        if (!isGoSignal)
        {
            BoxSignalStop();
        }
        // If GO, intentionally do nothing so we don't interfere with existing behavior
    }

    /// <summary>
    /// Optional hook when exiting Box B zone. Clears box signal pause if any.
    /// </summary>
    public void OnExitBoxSignalZone()
    {
        // If the vehicle was held by the box signal only, allow it to resume
        if (pausedForBoxSignal)
        {
            BoxSignalGo();
        }
    }

    // --------Roundabout Radar Rotation--------
    /// <summary>
    /// Rotate the short-range radar by the specified angle for roundabout detection
    /// </summary>
    public void RotateShortRangeRadar(float angle, float duration)
    {
        RotateShortRangeRadar(angle, duration, 1.5f); // Default 50% increase
    }
    
    /// <summary>
    /// Rotate the short-range radar by the specified angle for roundabout detection
    /// </summary>
    public void RotateShortRangeRadar(float angle, float duration, float rangeMultiplier)
    {
        if (shortRangeRadarScript == null)
            return;
            
        Transform shortRadarTransform = transform.Find("Radars/ShortRangeFR");
        if (shortRadarTransform == null)
            return;

        // Create target rotation by applying the angle to the original rotation
        Quaternion targetRotation = originalShortRangeRadarRotation * Quaternion.Euler(0, angle, 0);
        
        // Use DOTween for smooth rotation
        shortRadarTransform.DOLocalRotateQuaternion(targetRotation, duration);
        
        // Increase radar angle coverage for roundabout detection (immediate change)
        shortRangeRadarScript.range = originalShortRangeRadarRange * rangeMultiplier;
    }
    
    /// <summary>
    /// Reset the short-range radar to its original rotation when exiting roundabout
    /// </summary>
    public void ResetShortRangeRadarRotation(float duration)
    {
        if (shortRangeRadarScript == null)
            return;
            
        Transform shortRadarTransform = transform.Find("Radars/ShortRangeFR");
        if (shortRadarTransform == null)
            return;

        // Reset to original rotation
        shortRadarTransform.DOLocalRotateQuaternion(originalShortRangeRadarRotation, duration);
        
        // Reset radar angle coverage to original value (immediate change)
        shortRangeRadarScript.range = originalShortRangeRadarRange;
    }
}
