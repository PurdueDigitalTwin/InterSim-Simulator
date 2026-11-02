using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SWS;
using System;
using EVP;
using DG.Tweening;

/// <summary>
/// Traffic generation module for Unity traffic simulator.
/// Generates vehicles on predefined paths with customizable density and distribution.
/// </summary>
public class TrafficGenerator : MonoBehaviour
{
    [Header("Traffic Generation Settings")]
    [Tooltip("Whether to start generating traffic automatically")]
    public bool autoStart = true;
    
    [Tooltip("Global traffic density multiplier (0.1 = 10% density, 2.0 = 200% density)")]
    [Range(0.1f, 5.0f)]
    public float globalDensityMultiplier = 1.0f;
    
    [Tooltip("Maximum number of vehicles that can exist simultaneously")]
    public int maxVehicles = 100;
    
    [Header("Path Configuration")]
    [Tooltip("Paths to generate traffic on")]
    public PathConfig[] paths;
    
    [Header("Vehicle Configuration")]
    [Tooltip("Vehicle prefabs to spawn (will be randomly selected)")]
    public GameObject[] vehiclePrefabs;
    
    [Tooltip("Minimum spacing between vehicles on the same path (in seconds)")]
    [Range(1.0f, 10.0f)]
    public float minVehicleSpacing = 2.0f;
    
    [Tooltip("Random spacing variation (in seconds)")]
    [Range(0.0f, 5.0f)]
    public float spacingVariation = 1.0f;
    
    [Header("Traffic Flow Settings")]
    [Tooltip("Time of day traffic patterns (0 = midnight, 6 = 6 AM, 12 = noon, 18 = 6 PM)")]
    public AnimationCurve timeOfDayTrafficPattern;
    
    [Tooltip("Day of week traffic patterns (0 = Sunday, 6 = Saturday)")]
    public AnimationCurve dayOfWeekTrafficPattern;
    
    [Header("Advanced Settings")]
    [Tooltip("Whether to use scientific traffic metrics")]
    public bool useScientificMetrics = true;
    
    [Tooltip("Base vehicles per hour per lane (typical urban: 800-1200, highway: 1500-2500)")]
    [Range(0, 3500)]
    public float baseVehiclesPerHourPerLane = 1000f;
    
    [Tooltip("Lane capacity factor (0.8 = 80% of theoretical capacity)")]
    [Range(0.5f, 1.0f)]
    public float laneCapacityFactor = 0.8f;
    
    [Tooltip("Speed-flow relationship exponent (typically 1.5-2.0)")]
    [Range(1.0f, 3.0f)]
    public float speedFlowExponent = 1.8f;
    
    [Header("Runtime Information")]
    [SerializeField] private int currentVehicleCount = 0;
    [SerializeField] private float currentTrafficDensity = 0f;
    [SerializeField] public bool isGenerating = false;
    
    // Private variables
    private Dictionary<PathManager, PathTrafficData> pathTrafficData;
    private List<GameObject> activeVehicles;
    private Coroutine generationCoroutine;
    private float simulationStartTime;
    
    [System.Serializable]
    public class PathConfig
    {
        [Tooltip("Path to generate traffic on")] public PathManager path;
        [Tooltip("Number of lanes on this path")] [Range(1, 6)] public int laneCount = 2;
        [Tooltip("Traffic density multiplier for this specific path (1.0 = normal)")] [Range(0.1f, 3.0f)] public float densityMultiplier = 1.0f;
        [Tooltip("Speed limit for vehicles on this path")] public float speedLimit = 30f;
        [Tooltip("Whether to enable traffic generation on this path")] public bool enabled = true;
        [Tooltip("Custom spawn points along the path (empty = auto-distribute)")] public Transform[] customSpawnPoints;
        [Tooltip("Minimum distance between spawn points")] public float minSpawnDistance = 50f;
    }
    
    private class PathTrafficData
    {
        public float lastSpawnTime;
        public int currentVehicleCount;
        public float pathLength;
        public float[] spawnDistances;
        public int nextSpawnIndex;
        public bool hasSpawnedOnce;
    }
    
    void Start()
    {
        InitializeTrafficGenerator();
        
        if (autoStart)
        {
            StartTrafficGeneration();
        }
    }
    
    void InitializeTrafficGenerator()
    {
        pathTrafficData = new Dictionary<PathManager, PathTrafficData>();
        activeVehicles = new List<GameObject>();
        simulationStartTime = Time.time;
        
        // Initialize path data
        foreach (var pathConfig in paths)
        {
            if (pathConfig.path != null && pathConfig.enabled)
            {
                InitializePath(pathConfig);
            }
        }
        
        // Validate vehicle prefabs
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
        {
            Debug.LogError("TrafficGenerator: No vehicle prefabs assigned!");
            return;
        }
        
        // Validate paths
        if (paths == null || paths.Length == 0)
        {
            Debug.LogError("TrafficGenerator: No paths configured!");
            return;
        }
    }
    
    void InitializePath(PathConfig pathConfig)
    {
        var pathData = new PathTrafficData();
        pathData.lastSpawnTime = 0f;
        pathData.currentVehicleCount = 0;
        pathData.nextSpawnIndex = 0;
        pathData.hasSpawnedOnce = false;
        
        // Calculate path length
        Vector3[] pathPoints = pathConfig.path.GetPathPoints();
        pathData.pathLength = CalculatePathLength(pathPoints);
        
        // Calculate spawn distances along the path
        if (pathConfig.customSpawnPoints != null && pathConfig.customSpawnPoints.Length > 0)
        {
            // Use custom spawn points
            pathData.spawnDistances = new float[pathConfig.customSpawnPoints.Length];
            for (int i = 0; i < pathConfig.customSpawnPoints.Length; i++)
            {
                pathData.spawnDistances[i] = CalculateDistanceAlongPath(pathPoints, pathConfig.customSpawnPoints[i].position);
            }
        }
        else
        {
            // Auto-distribute spawn points
            pathData.spawnDistances = CalculateAutoSpawnDistances(pathData.pathLength, pathConfig.minSpawnDistance);
        }
        
        pathTrafficData[pathConfig.path] = pathData;
    }
    
    float CalculatePathLength(Vector3[] pathPoints)
    {
        float length = 0f;
        for (int i = 1; i < pathPoints.Length; i++)
        {
            length += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
        }
        return length;
    }
    
    float CalculateDistanceAlongPath(Vector3[] pathPoints, Vector3 position)
    {
        float minDistance = float.MaxValue;
        float distanceAlongPath = 0f;
        
        for (int i = 1; i < pathPoints.Length; i++)
        {
            float segmentLength = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            Vector3 segmentDirection = (pathPoints[i] - pathPoints[i - 1]).normalized;
            
            // Project position onto segment
            Vector3 toPosition = position - pathPoints[i - 1];
            float projection = Vector3.Dot(toPosition, segmentDirection);
            projection = Mathf.Clamp(projection, 0f, segmentLength);
            
            Vector3 projectedPoint = pathPoints[i - 1] + segmentDirection * projection;
            float distanceToSegment = Vector3.Distance(position, projectedPoint);
            
            if (distanceToSegment < minDistance)
            {
                minDistance = distanceToSegment;
                distanceAlongPath = distanceAlongPath + projection;
            }
            
            distanceAlongPath += segmentLength;
        }
        
        return distanceAlongPath;
    }
    
    float[] CalculateAutoSpawnDistances(float pathLength, float minDistance)
    {
        List<float> distances = new List<float>();
        float currentDistance = 0f;
        
        while (currentDistance < pathLength)
        {
            distances.Add(currentDistance);
            currentDistance += minDistance + UnityEngine.Random.Range(-minDistance * 0.2f, minDistance * 0.2f);
        }
        
        return distances.ToArray();
    }
    
    /// <summary>
    /// Starts traffic generation on all enabled paths
    /// </summary>
    public void StartTrafficGeneration()
    {
        if (isGenerating) return;
        
        isGenerating = true;
        generationCoroutine = StartCoroutine(TrafficGenerationLoop());
        Debug.Log("TrafficGenerator: Started traffic generation");
    }
    
    /// <summary>
    /// Stops traffic generation
    /// </summary>
    public void StopTrafficGeneration()
    {
        if (!isGenerating) return;
        
        isGenerating = false;
        if (generationCoroutine != null)
        {
            StopCoroutine(generationCoroutine);
            generationCoroutine = null;
        }
        Debug.Log("TrafficGenerator: Stopped traffic generation");
    }
    
    /// <summary>
    /// Clears all active vehicles
    /// </summary>
    public void ClearAllVehicles()
    {
        if (activeVehicles == null)
        {
            activeVehicles = new List<GameObject>();
        }

        for (int i = activeVehicles.Count - 1; i >= 0; i--)
        {
            var vehicle = activeVehicles[i];
            if (vehicle != null)
            {
                Destroy(vehicle);
            }
        }
        activeVehicles.Clear();
        currentVehicleCount = 0;
        
        // Reset path data
        if (pathTrafficData != null)
        {
            foreach (var pathData in pathTrafficData.Values)
            {
                pathData.currentVehicleCount = 0;
                pathData.lastSpawnTime = 0f;
                pathData.hasSpawnedOnce = false;
            }
        }
    }
    
    IEnumerator TrafficGenerationLoop()
    {
        while (isGenerating)
        {
            // Update traffic density based on time and patterns
            UpdateTrafficDensity();
            
            // Check each path for spawning opportunities
            foreach (var pathConfig in paths)
            {
                if (pathConfig.enabled && pathConfig.path != null)
                {
                    CheckAndSpawnVehicle(pathConfig);
                }
            }
            
            // Clean up destroyed vehicles
            CleanupDestroyedVehicles();
            
            yield return new WaitForSeconds(0.1f); // Check every 0.1 seconds
        }
    }
    
    void UpdateTrafficDensity()
    {
        float timeOfDay = (Time.time - simulationStartTime) % 86400f / 3600f; // 0-24 hours
        float dayOfWeek = (Time.time - simulationStartTime) % 604800f / 86400f; // 0-7 days
        
        float timeMultiplier = 1f;
        float dayMultiplier = 1f;
        
        if (timeOfDayTrafficPattern != null)
            timeMultiplier = timeOfDayTrafficPattern.Evaluate(timeOfDay);
        
        if (dayOfWeekTrafficPattern != null)
            dayMultiplier = dayOfWeekTrafficPattern.Evaluate(dayOfWeek);
        
        currentTrafficDensity = globalDensityMultiplier * timeMultiplier * dayMultiplier;
    }
    
    void CheckAndSpawnVehicle(PathConfig pathConfig)
    {
        if (!pathTrafficData.ContainsKey(pathConfig.path)) return;
        
        var pathData = pathTrafficData[pathConfig.path];

        // Check if we can spawn more vehicles
        if (currentVehicleCount >= maxVehicles) return;
        
        // Calculate spawn interval based on traffic density and scientific metrics
        float spawnInterval = CalculateSpawnInterval(pathConfig, pathData);
        
        // First-time immediate spawn for visibility
        if (!pathData.hasSpawnedOnce)
        {
            SpawnVehicle(pathConfig, pathData);
            pathData.lastSpawnTime = Time.time;
            pathData.hasSpawnedOnce = true;
            return;
        }
        
        // Check if enough time has passed since last spawn
        if (Time.time - pathData.lastSpawnTime >= spawnInterval)
        {
            // Check if we have spawn points available
            if (pathData.spawnDistances.Length > 0)
            {
                SpawnVehicle(pathConfig, pathData);
                pathData.lastSpawnTime = Time.time;
            }
        }
    }
    
    float CalculateSpawnInterval(PathConfig pathConfig, PathTrafficData pathData)
    {
        if (useScientificMetrics)
        {
            // Use scientific traffic flow models
            float lanes = Mathf.Max(1, pathConfig.laneCount);
            float baseFlow = baseVehiclesPerHourPerLane * lanes * Mathf.Clamp01(laneCapacityFactor);
            float density = Mathf.Max(0.01f, currentTrafficDensity * Mathf.Max(0.1f, pathConfig.densityMultiplier));
            
            // Apply speed-flow relationship (higher speeds generally reduce capacity)
            float speed = Mathf.Max(0.1f, pathConfig.speedLimit);
            float speedFactor = Mathf.Pow(speed / 30f, speedFlowExponent);
            
            float adjustedFlow = baseFlow * density * Mathf.Max(0.1f, speedFactor);
            adjustedFlow = Mathf.Max(0.01f, adjustedFlow);
            
            // Convert to spawn interval
            float spawnInterval = 3600f / adjustedFlow; // seconds between vehicles
            
            // Add random variation
            spawnInterval += UnityEngine.Random.Range(-spacingVariation, spacingVariation);
            
            // Ensure minimum spacing
            spawnInterval = Mathf.Max(spawnInterval, minVehicleSpacing);
            
            return spawnInterval;
        }
        else
        {
            // Simple time-based spawning
            float density = Mathf.Max(0.01f, currentTrafficDensity);
            float baseInterval = minVehicleSpacing / density;
            baseInterval /= Mathf.Max(0.1f, pathConfig.densityMultiplier);
            
            // Add random variation
            baseInterval += UnityEngine.Random.Range(-spacingVariation, spacingVariation);
            
            return Mathf.Max(baseInterval, minVehicleSpacing);
        }
    }
    
    void SpawnVehicle(PathConfig pathConfig, PathTrafficData pathData)
    {
        // Select random vehicle prefab
        GameObject vehiclePrefab = vehiclePrefabs[UnityEngine.Random.Range(0, vehiclePrefabs.Length)];
        
        // Get spawn position along the path
        Vector3 spawnPosition = GetSpawnPosition(pathConfig.path, pathData);
        
        // Spawn the vehicle
        GameObject vehicle = Instantiate(vehiclePrefab, spawnPosition, Quaternion.identity);
        
        // Configure the vehicle for path following
        ConfigureVehicleForPath(vehicle, pathConfig);
        
        // Ensure movement starts immediately
        var sm = vehicle.GetComponent<splineMove>();
        if (sm != null)
        {
            sm.StartMove();
        }
        
        // Add to active vehicles list
        activeVehicles.Add(vehicle);
        currentVehicleCount++;
        pathData.currentVehicleCount++;

        sm.tween.OnComplete(() =>
            {
                if (activeVehicles.Contains(vehicle))
                    activeVehicles.Remove(vehicle);
        
                currentVehicleCount = Mathf.Max(0, currentVehicleCount - 1);
                Destroy(vehicle);
            });
    }
    
    Vector3 GetSpawnPosition(PathManager path, PathTrafficData pathData)
    {
        if (pathData.spawnDistances.Length == 0) return path.waypoints[0].position;
        
        // Get spawn distance for this spawn cycle
        float spawnDistance = pathData.spawnDistances[pathData.nextSpawnIndex % pathData.spawnDistances.Length];
        pathData.nextSpawnIndex++;
        
        // Convert distance to position along path
        return GetPositionAtDistance(path, spawnDistance);
    }
    
    Vector3 GetPositionAtDistance(PathManager path, float distance)
    {
        Vector3[] pathPoints = path.GetPathPoints();
        float currentDistance = 0f;
        
        for (int i = 1; i < pathPoints.Length; i++)
        {
            float segmentLength = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            
            if (currentDistance + segmentLength >= distance)
            {
                // Interpolate within this segment
                float t = (distance - currentDistance) / segmentLength;
                return Vector3.Lerp(pathPoints[i - 1], pathPoints[i], t);
            }
            
            currentDistance += segmentLength;
        }
        
        // If we reach here, return the last waypoint
        return pathPoints[pathPoints.Length - 1];
    }
    
    void ConfigureVehicleForPath(GameObject vehicle, PathConfig pathConfig)
    {
        // Add splineMove component if it doesn't exist
        splineMove splineMove = vehicle.GetComponent<splineMove>();
        if (splineMove == null)
        {
            splineMove = vehicle.AddComponent<splineMove>();
        }
        
        // Configure splineMove for this path
        splineMove.pathContainer = pathConfig.path;
        splineMove.speed = Mathf.Max(0.1f, pathConfig.speedLimit);
        splineMove.loopType = splineMove.LoopType.none; // Don't loop, let the vehicle be destroyed
        splineMove.onStart = false;
        splineMove.moveToPath = false;
        splineMove.startPoint = 0;
        
        // Disable any existing vehicle controllers to prevent player input
        var vehicleController = vehicle.GetComponent<VehicleController>();
        if (vehicleController != null)
        {
            vehicleController.enabled = false;
        }
        
        // Disable any existing input systems
        var vehicleInput = vehicle.GetComponent<VehicleStandardInput>();
        if (vehicleInput != null)
        {
            vehicleInput.enabled = false;
        }
    }
    
    void CleanupDestroyedVehicles()
    {
        for (int i = activeVehicles.Count - 1; i >= 0; i--)
        {
            if (activeVehicles[i] == null)
            {
                activeVehicles.RemoveAt(i);
                currentVehicleCount = Mathf.Max(0, currentVehicleCount - 1);
            }
        }
    }
    
    /// <summary>
    /// Gets current traffic statistics
    /// </summary>
    public TrafficStats GetTrafficStats()
    {
        var stats = new TrafficStats();
        stats.totalVehicles = currentVehicleCount;
        stats.trafficDensity = currentTrafficDensity;
        stats.isGenerating = isGenerating;
        stats.pathStats = new Dictionary<string, PathStats>();
        
        foreach (var pathConfig in paths)
        {
            if (pathConfig.path != null && pathTrafficData.ContainsKey(pathConfig.path))
            {
                var pathData = pathTrafficData[pathConfig.path];
                stats.pathStats[pathConfig.path.name] = new PathStats
                {
                    vehicleCount = pathData.currentVehicleCount,
                    lastSpawnTime = pathData.lastSpawnTime,
                    pathLength = pathData.pathLength
                };
            }
        }
        
        return stats;
    }
    
    /// <summary>
    /// Sets traffic density for a specific path
    /// </summary>
    public void SetPathDensity(string pathName, float densityMultiplier)
    {
        foreach (var pathConfig in paths)
        {
            if (pathConfig.path != null && pathConfig.path.name == pathName)
            {
                pathConfig.densityMultiplier = densityMultiplier;
                break;
            }
        }
    }
    
    /// <summary>
    /// Enables or disables traffic generation for a specific path
    /// </summary>
    public void SetPathEnabled(string pathName, bool enabled)
    {
        foreach (var pathConfig in paths)
        {
            if (pathConfig.path != null && pathConfig.path.name == pathName)
            {
                pathConfig.enabled = enabled;
                break;
            }
        }
    }
    
    void OnDestroy()
    {
        StopTrafficGeneration();
    }
    
    void OnValidate()
    {
        // Ensure values are within reasonable ranges
        globalDensityMultiplier = Mathf.Clamp(globalDensityMultiplier, 0.1f, 5.0f);
        maxVehicles = Mathf.Max(1, maxVehicles);
        minVehicleSpacing = Mathf.Max(0.5f, minVehicleSpacing);
        spacingVariation = Mathf.Clamp(spacingVariation, 0f, minVehicleSpacing);
    }
}

/// <summary>
/// Traffic statistics data structure
/// </summary>
[System.Serializable]
public class TrafficStats
{
    public int totalVehicles;
    public float trafficDensity;
    public bool isGenerating;
    public Dictionary<string, PathStats> pathStats;
}

/// <summary>
/// Individual path statistics
/// </summary>
[System.Serializable]
public class PathStats
{
    public int vehicleCount;
    public float lastSpawnTime;
    public float pathLength;
}
