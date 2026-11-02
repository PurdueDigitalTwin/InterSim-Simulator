using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using System.Collections.Generic;
using EVP;
using DG.Tweening;
using SWS;

public class SimulationDataLogger : MonoBehaviour
{
    [Header("UI Components")]
    public Button simulationButton;
    public TMPro.TextMeshProUGUI buttonText;
    
    [Header("Logging Settings")]
    public float loggingFrequency = 60f; // Hz - 60 FPS is common for driving sims
    public string logDirectory = "SimulationLogs";
    
    [Header("Data Sources")]
    public VehicleStandardInput vehicleInput;
    public VehicleController vehicleController;
    public NpcController spawner;
    
    [Header("NPC Logging Settings")]
    public bool logNpcData = true;
    public int maxNpcVehicles = 20; // Maximum number of NPC vehicles to track
    
    [Header("Collision Detection")]
    public bool logCollisions = true;
    public LayerMask vehicleLayerMask = 1 << 8; // Default to layer 8, adjust as needed
    
    // Private variables
    private bool isLogging = false;
    private float lastLogTime = 0f;
    private float logInterval;
    private string currentLogFile;
    private StreamWriter writer;
    private List<string> logBuffer = new List<string>();
    private int bufferSize = 100; // Buffer size before writing to file
    
    // Collision tracking
    private List<CollisionEvent> collisionEvents = new List<CollisionEvent>();
    private bool collisionOccurred = false;
    
    // NPC tracking
    private List<NpcVehicleData> npcVehicles = new List<NpcVehicleData>();
    private string csvHeaders;
    
    // NPC Vehicle Data Structure
    [System.Serializable]
    public class NpcVehicleData
    {
        public int vehicleId;
        public string vehicleName;
        public splineMove splineMove;
        public TrafficControl trafficControl;
        public Rigidbody rigidbody;
        public Vector3 lastPosition;
        public float lastSpeed;
        public float lastTime;
        
        public NpcVehicleData(int id, string name, splineMove move, TrafficControl control, Rigidbody rb)
        {
            vehicleId = id;
            vehicleName = name;
            splineMove = move;
            trafficControl = control;
            rigidbody = rb;
            lastPosition = Vector3.zero;
            lastSpeed = 0f;
            lastTime = 0f;
        }
    }
    
    // Collision Event Data Structure
    [System.Serializable]
    public class CollisionEvent
    {
        public string timestamp;
        public Vector3 collisionPoint;
        public Vector3 egoVelocity;
        public Vector3 otherVelocity;
        
        public CollisionEvent(string time, Vector3 point, Vector3 egoVel, Vector3 otherVel)
        {
            timestamp = time;
            collisionPoint = point;
            egoVelocity = egoVel;
            otherVelocity = otherVel;
        }
    }
    
    void Start()
    {
        // Initialize logging interval
        logInterval = 1f / loggingFrequency;
        
        // Set up button
        if (simulationButton != null)
        {
            simulationButton.onClick.AddListener(ToggleSimulation);
            UpdateButtonText();
        }
        
        // Auto-find components if not assigned
        if (vehicleInput == null)
            vehicleInput = FindAnyObjectByType<VehicleStandardInput>();
            
        if (vehicleController == null)
            vehicleController = FindAnyObjectByType<VehicleController>();
        
        // Set up collision detection
        SetupEgoCollisionDetection();
        
        // Create log directory if it doesn't exist
        string fullLogPath = Path.Combine(Application.dataPath, "..", logDirectory);
        if (!Directory.Exists(fullLogPath))
        {
            Directory.CreateDirectory(fullLogPath);
        }
        
        // Initialize CSV headers
        InitializeCsvHeaders();
    }
    
    private void InitializeCsvHeaders()
    {
        // Base headers for ego vehicle - changed from Speed to VelocityX,Y,Z
        csvHeaders = "Timestamp,Ego_ThrottleInput,Ego_BrakeInput,Ego_SteerInput,Ego_VelocityX,Ego_VelocityY,Ego_VelocityZ,Ego_PositionX,Ego_PositionY,Ego_PositionZ";
        
        // Add collision flag if enabled
        if (logCollisions)
        {
            csvHeaders += ",CollisionOccurred";
        }
        
        // Add headers for active NPC vehicles only
        if (logNpcData)
        {
            csvHeaders += ",ActiveNPCs_Count";
            // We'll add NPC headers dynamically based on actual active vehicles
        }
    }
    
    private void SetupEgoCollisionDetection()
    {
        if (!logCollisions || vehicleController == null) return;
        
        Debug.Log("Setting up ego collision detection...");
        
        // Find the ego vehicle's main collider (Colliders/ColliderBody)
        Transform collidersTransform = vehicleController.transform.Find("Colliders");
        if (collidersTransform != null)
        {
            Debug.Log("Found Colliders transform");
            
            // Look for the main ColliderBody (not trigger)
            Transform colliderTransform = collidersTransform.Find("ColliderBody");
            if (colliderTransform != null)
            {
                Debug.Log("Found main ColliderBody");
                Collider mainCollider = colliderTransform.GetComponent<Collider>();
                if (mainCollider != null)
                {
                    Debug.Log($"Found main collider: {mainCollider.GetType().Name}, isTrigger: {mainCollider.isTrigger}");
                    
                    // Ensure the main collider is NOT set as trigger for actual collision detection
                    if (mainCollider.isTrigger)
                    {
                        Debug.LogWarning("Main ColliderBody is set as trigger. Setting to false for actual collision detection.");
                        mainCollider.isTrigger = false;
                    }
                    
                    // Check for existing collision detection component
                    EgoCollisionDetector detector = colliderTransform.gameObject.GetComponent<EgoCollisionDetector>();
                    if (detector != null)
                    {
                        Debug.Log("Found existing EgoCollisionDetector component");
                        detector.Initialize(this, vehicleLayerMask);
                        Debug.Log("Ego collision detection initialized successfully");
                    }
                    else
                    {
                        Debug.LogWarning("EgoCollisionDetector component not found on ColliderBody. Please add it manually in the inspector.");
                    }
                }
                else
                {
                    Debug.LogWarning("No Collider component found on ColliderBody");
                }
            }
            else
            {
                Debug.LogWarning("ColliderBody not found under Colliders. Please ensure it exists.");
            }
        }
        else
        {
            Debug.LogWarning("Colliders child not found on ego vehicle");
        }
    }
    
    void Update()
    {
        if (isLogging)
        {
            // Check if it's time to log data
            if (Time.time - lastLogTime >= logInterval)
            {
                LogData();
                lastLogTime = Time.time;
            }
        }
    }
    
    public void ToggleSimulation()
    {
        if (isLogging)
        {
            StopLogging();
            DOTween.KillAll();
            // Reload the scene to reset everything to initial state
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
        }
        else
        {
            // Clear collision events for new simulation
            collisionEvents.Clear();
            collisionOccurred = false; // Reset collision occurred flag
            
            // Start the NPCs
            spawner.StartAllCars();
            // Initialize NPC tracking
            InitializeNpcTracking();
            // Start logging
            StartLogging();
        }
        
        UpdateButtonText();
    }
    
    private void InitializeNpcTracking()
    {
        if (!logNpcData) return;
        
        npcVehicles.Clear();
        
        // Find all NPC vehicles under the spawner
        if (spawner != null)
        {
            int vehicleId = 1;
            foreach (Transform child in spawner.transform)
            {
                if (!child.gameObject.activeInHierarchy) continue;
                
                splineMove splineMove = child.GetComponent<splineMove>();
                TrafficControl trafficControl = child.GetComponent<TrafficControl>();
                Rigidbody rigidbody = child.GetComponent<Rigidbody>();
                
                if (splineMove != null && npcVehicles.Count < maxNpcVehicles)
                {
                    NpcVehicleData npcData = new NpcVehicleData(
                        vehicleId,
                        child.name,
                        splineMove,
                        trafficControl,
                        rigidbody
                    );
                    npcVehicles.Add(npcData);
                    vehicleId++;
                }
            }
        }
        
        Debug.Log($"Initialized tracking for {npcVehicles.Count} NPC vehicles");
    }
    
    private void StartLogging()
    {
        if (isLogging) return;
        
        // Create log directory if it doesn't exist
        string logPath = Path.Combine(Application.dataPath, "..", logDirectory);
        Directory.CreateDirectory(logPath);
        
        // Create collision log directory if it doesn't exist
        string collisionLogPath = Path.Combine(Application.dataPath, "..", logDirectory, "CollisionLogs");
        Directory.CreateDirectory(collisionLogPath);
        
        // Generate timestamp for file naming
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        
        // Create simulation log file
        currentLogFile = Path.Combine(logPath, $"simulation_log_{timestamp}.csv");
        
        // Create collision log file with matching timestamp
        string collisionLogFile = Path.Combine(collisionLogPath, $"collision_log_{timestamp}.csv");
        
        try
        {
            writer = new StreamWriter(currentLogFile);
            
            // Write headers with NPC vehicle columns
            string dynamicHeaders = CreateDynamicHeaders();
            writer.WriteLine(dynamicHeaders);
            
            // Create collision log file with headers
            using (StreamWriter collisionWriter = new StreamWriter(collisionLogFile))
            {
                collisionWriter.WriteLine("Timestamp,CollisionPointX,CollisionPointY,CollisionPointZ,EgoVelocityX,EgoVelocityY,EgoVelocityZ,OtherVelocityX,OtherVelocityY,OtherVelocityZ");
            }
            
            isLogging = true;
            lastLogTime = Time.time;
            
            Debug.Log($"Started logging to: {currentLogFile}");
            Debug.Log($"Collision logging to: {collisionLogFile}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start logging: {e.Message}");
        }
    }
    
    private string CreateDynamicHeaders()
    {
        string headers = csvHeaders;
        
        if (logNpcData)
        {
            // Add headers only for active NPC vehicles
            int activeIndex = 0;
            for (int i = 0; i < npcVehicles.Count; i++)
            {
                if (npcVehicles[i] != null && npcVehicles[i].splineMove != null && npcVehicles[i].splineMove.gameObject.activeInHierarchy)
                {
                    headers += $",NPC{activeIndex}_ID,NPC{activeIndex}_Name,NPC{activeIndex}_VelocityX,NPC{activeIndex}_VelocityY,NPC{activeIndex}_VelocityZ,NPC{activeIndex}_PositionX,NPC{activeIndex}_PositionY,NPC{activeIndex}_PositionZ";
                    activeIndex++;
                }
            }
        }
        
        return headers;
    }
    
    private void StopLogging()
    {
        if (isLogging)
        {
            // Flush any remaining buffered data
            FlushBuffer();
            
            if (writer != null)
            {
                writer.Close();
                writer.Dispose();
                writer = null;
            }
            
            isLogging = false;
            Debug.Log($"Stopped logging. File saved to: {currentLogFile}");
        }
    }
    
    private void LogData()
    {
        if (!isLogging || writer == null) return;
        
        // Get current timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        
        // Get ego vehicle data
        float throttleInput = 0f;
        float brakeInput = 0f;
        float steerInput = 0f;
        Vector3 velocity = Vector3.zero;
        Vector3 position = Vector3.zero;
        
        if (vehicleInput != null)
        {
            throttleInput = vehicleInput.forwardInput;
            brakeInput = vehicleInput.reverseInput;
            steerInput = vehicleInput.steerInput;
        }
        
        if (vehicleController != null)
        {
            // Get the full velocity vector instead of just magnitude
            velocity = vehicleController.cachedRigidbody.linearVelocity;
            position = vehicleController.transform.position;
        }
        
        // Start CSV line with ego vehicle data - now logging velocity components
        string csvLine = $"{timestamp},{throttleInput:F3},{brakeInput:F3},{steerInput:F3},{velocity.x:F3},{velocity.y:F3},{velocity.z:F3},{position.x:F3},{position.y:F3},{position.z:F3}";
        
        // Add collision flag if enabled
        if (logCollisions)
        {
            csvLine += $",{(collisionOccurred ? "1" : "0")}";
        }
        
        // Add NPC vehicle data
        if (logNpcData)
        {
            csvLine += GetNpcDataCsv();
        }
        
        // Add to buffer
        logBuffer.Add(csvLine);
        
        // Flush buffer if it's full
        if (logBuffer.Count >= bufferSize)
        {
            FlushBuffer();
        }
    }
    
    public void LogCollisionEvent(string timestamp, Vector3 collisionPoint, Vector3 egoVelocity, Vector3 otherVelocity)
    {
        if (!logCollisions) return;
        
        // Set collision flag
        collisionOccurred = true;
        
        // Create collision event
        CollisionEvent collisionEvent = new CollisionEvent(timestamp, collisionPoint, egoVelocity, otherVelocity);
        collisionEvents.Add(collisionEvent);
        
        // Log to console for debugging
        Debug.Log($"COLLISION DETECTED: Ego vehicle collided at {timestamp}");
        Debug.Log($"Collision Point: {collisionPoint}");
        Debug.Log($"Ego Velocity: {egoVelocity}");
        Debug.Log($"Other Velocity: {otherVelocity}");
        Debug.Log("Simulation will end due to collision");
        
        // Write collision event to file
        WriteCollisionToFile(collisionEvent);
        
        // Immediately end the simulation
        ToggleSimulation();
    }
    
    private void WriteCollisionToFile(CollisionEvent collisionEvent)
    {
        try
        {
            // Get the collision log file path that matches the current simulation log file
            string simulationFileName = Path.GetFileNameWithoutExtension(currentLogFile);
            string collisionLogPath = Path.Combine(Application.dataPath, "..", logDirectory, "CollisionLogs");
            string collisionLogFile = Path.Combine(collisionLogPath, $"collision_log_{simulationFileName.Replace("simulation_log_", "")}.csv");
            
            // Append collision event to the collision log file
            using (StreamWriter collisionWriter = new StreamWriter(collisionLogFile, true))
            {
                string collisionLine = $"{collisionEvent.timestamp},{collisionEvent.collisionPoint.x:F3},{collisionEvent.collisionPoint.y:F3},{collisionEvent.collisionPoint.z:F3},{collisionEvent.egoVelocity.x:F3},{collisionEvent.egoVelocity.y:F3},{collisionEvent.egoVelocity.z:F3},{collisionEvent.otherVelocity.x:F3},{collisionEvent.otherVelocity.y:F3},{collisionEvent.otherVelocity.z:F3}";
                collisionWriter.WriteLine(collisionLine);
                Debug.Log($"Collision event logged to: {collisionLogFile}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error writing collision event to file: {e.Message}");
        }
    }
    
    private string GetNpcDataCsv()
    {
        string npcData = "";
        float currentTime = Time.time;
        int activeCount = 0;
        
        // Count active vehicles first
        for (int i = 0; i < npcVehicles.Count; i++)
        {
            if (npcVehicles[i] != null && npcVehicles[i].splineMove != null && npcVehicles[i].splineMove.gameObject.activeInHierarchy)
            {
                activeCount++;
            }
        }
        
        // Add active count
        npcData += $",{activeCount}";
        
        // Process only active NPC vehicles
        for (int i = 0; i < npcVehicles.Count; i++)
        {
            if (npcVehicles[i] != null && npcVehicles[i].splineMove != null && npcVehicles[i].splineMove.gameObject.activeInHierarchy)
            {
                NpcVehicleData npc = npcVehicles[i];
                
                // Get current position and velocity vector
                Vector3 currentPosition = npc.splineMove.transform.position;
                Vector3 currentVelocity = npc.splineMove.Velocity;
                
                // Update last values for next calculation
                npc.lastPosition = currentPosition;
                npc.lastSpeed = currentVelocity.magnitude;
                npc.lastTime = currentTime;
                
                npcData += $",{npc.vehicleId},{npc.vehicleName},{currentVelocity.x:F3},{currentVelocity.y:F3},{currentVelocity.z:F3},{currentPosition.x:F3},{currentPosition.y:F3},{currentPosition.z:F3}";
            }
        }
        
        return npcData;
    }
    
    private void FlushBuffer()
    {
        if (writer != null && logBuffer.Count > 0)
        {
            foreach (string line in logBuffer)
            {
                writer.WriteLine(line);
            }
            writer.Flush();
            logBuffer.Clear();
        }
    }
    
    private void UpdateButtonText()
    {
        if (buttonText != null)
        {
            buttonText.text = isLogging ? "End Simulation" : "Start Simulation";
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && isLogging)
        {
            FlushBuffer();
        }
    }
    
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && isLogging)
        {
            FlushBuffer();
        }
    }
    
    void OnDestroy()
    {
        StopLogging();
    }
    
    void OnApplicationQuit()
    {
        StopLogging();
    }
}
