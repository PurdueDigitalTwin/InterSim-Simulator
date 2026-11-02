using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI controller for the TrafficGenerator component.
/// Provides easy access to control traffic generation parameters and view statistics.
/// </summary>
public class TrafficGeneratorUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Reference to the TrafficGenerator component")]
    public TrafficGenerator trafficGenerator;
    
    [Header("Control Panels")]
    [Tooltip("Main control panel")]
    public GameObject controlPanel;
    
    [Tooltip("Statistics display panel")]
    public GameObject statsPanel;
    
    [Header("Control UI Elements")]
    [Tooltip("Start/Stop traffic generation button")]
    public Button startStopButton;
    
    [Tooltip("Clear all vehicles button")]
    public Button clearVehiclesButton;
    
    [Tooltip("Start/Stop button text")]
    public TextMeshProUGUI startStopButtonText;
    
    [Header("Density Control")]
    [Tooltip("Global density slider")]
    public Slider globalDensitySlider;
    
    [Tooltip("Global density value text")]
    public TextMeshProUGUI globalDensityText;
    
    [Tooltip("Max vehicles slider")]
    public Slider maxVehiclesSlider;
    
    [Tooltip("Max vehicles value text")]
    public TextMeshProUGUI maxVehiclesText;
    
    [Header("Path Controls")]
    [Tooltip("Path control prefab")]
    public GameObject pathControlPrefab;
    
    [Tooltip("Container for path controls")]
    public Transform pathControlsContainer;
    
    [Header("Statistics Display")]
    [Tooltip("Total vehicles text")]
    public TextMeshProUGUI totalVehiclesText;
    
    [Tooltip("Current density text")]
    public TextMeshProUGUI currentDensityText;
    
    [Tooltip("Status text")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("Path statistics text")]
    public TextMeshProUGUI pathStatsText;
    
    [Header("Advanced Controls")]
    [Tooltip("Scientific metrics toggle")]
    public Toggle scientificMetricsToggle;
    
    [Tooltip("Base vehicles per hour slider")]
    public Slider baseVehiclesSlider;
    
    [Tooltip("Base vehicles per hour text")]
    public TextMeshProUGUI baseVehiclesText;
    
    [Tooltip("Lane capacity factor slider")]
    public Slider laneCapacitySlider;
    
    [Tooltip("Lane capacity factor text")]
    public TextMeshProUGUI laneCapacityText;
    
    [Tooltip("Speed flow exponent slider")]
    public Slider speedFlowSlider;
    
    [Tooltip("Speed flow exponent text")]
    public TextMeshProUGUI speedFlowText;
    
    [Header("UI Settings")]
    [Tooltip("Update frequency for statistics (seconds)")]
    public float statsUpdateFrequency = 1.0f;
    
    [Tooltip("Whether to show the control panel by default")]
    public bool showControlPanelByDefault = true;
    
    [Header("Keyboard Shortcuts (optional)")]
    [Tooltip("Enable keyboard shortcuts (disabled by default to avoid editor conflicts)")]
    public bool enableKeyboardShortcuts = false;
    
    [Tooltip("Key to toggle control panel (None = disabled)")]
    public KeyCode togglePanelKey = KeyCode.None;
    
    [Tooltip("Key to toggle stats panel (None = disabled)")]
    public KeyCode toggleStatsKey = KeyCode.None;
    
    [Tooltip("Key to start/stop traffic (None = disabled)")]
    public KeyCode startStopKey = KeyCode.None;
    
    [Tooltip("Key to clear all vehicles (None = disabled)")]
    public KeyCode clearVehiclesKey = KeyCode.None;
    
    // Private variables
    private PathControlUI[] pathControls;
    private float lastStatsUpdate;
    private bool isControlPanelVisible;
    
    void Start()
    {
        InitializeUI();
        SetupEventListeners();
        UpdateUI();
        
        // Show/hide control panel based on setting
        if (controlPanel != null)
        {
            controlPanel.SetActive(showControlPanelByDefault);
            isControlPanelVisible = showControlPanelByDefault;
        }
    }
    
    void Update()
    {
        // Update statistics periodically
        if (Time.time - lastStatsUpdate >= statsUpdateFrequency)
        {
            UpdateStatistics();
            lastStatsUpdate = Time.time;
        }
        
        // Optional keyboard shortcuts (disabled by default)
        if (enableKeyboardShortcuts)
        {
            if (togglePanelKey != KeyCode.None && Input.GetKeyDown(togglePanelKey))
            {
                ToggleControlPanel();
            }
            
            if (toggleStatsKey != KeyCode.None && Input.GetKeyDown(toggleStatsKey))
            {
                ToggleStatsPanel();
            }
            
            if (startStopKey != KeyCode.None && Input.GetKeyDown(startStopKey))
            {
                ToggleTrafficGeneration();
            }
            
            if (clearVehiclesKey != KeyCode.None && Input.GetKeyDown(clearVehiclesKey))
            {
                ClearAllVehicles();
            }
        }
    }
    
    void InitializeUI()
    {
        // Auto-find TrafficGenerator if not assigned
        if (trafficGenerator == null)
        {
            trafficGenerator = FindAnyObjectByType<TrafficGenerator>();
        }
        
        if (trafficGenerator == null)
        {
            Debug.LogError("TrafficGeneratorUI: No TrafficGenerator found in scene!");
            return;
        }
        
        // Create path controls
        CreatePathControls();
        
        // Initialize slider values
        InitializeSliderValues();
    }
    
    void CreatePathControls()
    {
        if (pathControlPrefab == null || pathControlsContainer == null) return;
        
        // Clear existing controls
        foreach (Transform child in pathControlsContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Create controls for each path
        var paths = trafficGenerator.paths;
        pathControls = new PathControlUI[paths.Length];
        
        for (int i = 0; i < paths.Length; i++)
        {
            GameObject controlObj = Instantiate(pathControlPrefab, pathControlsContainer);
            PathControlUI pathControl = controlObj.GetComponent<PathControlUI>();
            
            if (pathControl != null)
            {
                pathControl.Initialize(paths[i], trafficGenerator);
                pathControls[i] = pathControl;
            }
        }
    }
    
    void InitializeSliderValues()
    {
        if (globalDensitySlider != null)
        {
            globalDensitySlider.value = trafficGenerator.globalDensityMultiplier;
        }
        
        if (maxVehiclesSlider != null)
        {
            maxVehiclesSlider.value = trafficGenerator.maxVehicles;
        }
        
        if (scientificMetricsToggle != null)
        {
            scientificMetricsToggle.isOn = trafficGenerator.useScientificMetrics;
        }
        
        if (baseVehiclesSlider != null)
        {
            baseVehiclesSlider.value = trafficGenerator.baseVehiclesPerHourPerLane;
        }
        
        if (laneCapacitySlider != null)
        {
            laneCapacitySlider.value = trafficGenerator.laneCapacityFactor;
        }
        
        if (speedFlowSlider != null)
        {
            speedFlowSlider.value = trafficGenerator.speedFlowExponent;
        }
    }
    
    void SetupEventListeners()
    {
        // Start/Stop button
        if (startStopButton != null)
        {
            startStopButton.onClick.AddListener(ToggleTrafficGeneration);
        }
        
        // Clear vehicles button
        if (clearVehiclesButton != null)
        {
            clearVehiclesButton.onClick.AddListener(ClearAllVehicles);
        }
        
        // Global density slider
        if (globalDensitySlider != null)
        {
            globalDensitySlider.onValueChanged.AddListener(OnGlobalDensityChanged);
        }
        
        // Max vehicles slider
        if (maxVehiclesSlider != null)
        {
            maxVehiclesSlider.onValueChanged.AddListener(OnMaxVehiclesChanged);
        }
        
        // Scientific metrics toggle
        if (scientificMetricsToggle != null)
        {
            scientificMetricsToggle.onValueChanged.AddListener(OnScientificMetricsChanged);
        }
        
        // Base vehicles slider
        if (baseVehiclesSlider != null)
        {
            baseVehiclesSlider.onValueChanged.AddListener(OnBaseVehiclesChanged);
        }
        
        // Lane capacity slider
        if (laneCapacitySlider != null)
        {
            laneCapacitySlider.onValueChanged.AddListener(OnLaneCapacityChanged);
        }
        
        // Speed flow slider
        if (speedFlowSlider != null)
        {
            speedFlowSlider.onValueChanged.AddListener(OnSpeedFlowChanged);
        }
    }
    
    void UpdateUI()
    {
        if (trafficGenerator == null) return;
        
        // Update button text
        if (startStopButtonText != null)
        {
            startStopButtonText.text = trafficGenerator.isGenerating ? "Stop Traffic" : "Start Traffic";
        }
        
        // Update slider values
        if (globalDensitySlider != null)
        {
            globalDensitySlider.value = trafficGenerator.globalDensityMultiplier;
        }
        
        if (maxVehiclesSlider != null)
        {
            maxVehiclesSlider.value = trafficGenerator.maxVehicles;
        }
        
        // Update text displays
        UpdateTextDisplays();
    }
    
    void UpdateTextDisplays()
    {
        if (globalDensityText != null)
        {
            globalDensityText.text = $"Global Density: {trafficGenerator.globalDensityMultiplier:F2}x";
        }
        
        if (maxVehiclesText != null)
        {
            maxVehiclesText.text = $"Max Vehicles: {trafficGenerator.maxVehicles}";
        }
        
        if (baseVehiclesText != null)
        {
            baseVehiclesText.text = $"Base Flow: {trafficGenerator.baseVehiclesPerHourPerLane:F0} vph/lane";
        }
        
        if (laneCapacityText != null)
        {
            laneCapacityText.text = $"Capacity: {trafficGenerator.laneCapacityFactor:F2}";
        }
        
        if (speedFlowText != null)
        {
            speedFlowText.text = $"Speed Exponent: {trafficGenerator.speedFlowExponent:F2}";
        }
    }
    
    void UpdateStatistics()
    {
        if (trafficGenerator == null) return;
        
        var stats = trafficGenerator.GetTrafficStats();
        
        // Update main statistics
        if (totalVehiclesText != null)
        {
            totalVehiclesText.text = $"Total Vehicles: {stats.totalVehicles}";
        }
        
        if (currentDensityText != null)
        {
            currentDensityText.text = $"Current Density: {stats.trafficDensity:F2}x";
        }
        
        if (statusText != null)
        {
            statusText.text = $"Status: {(stats.isGenerating ? "Generating" : "Stopped")}";
        }
        
        // Update path statistics
        if (pathStatsText != null)
        {
            string pathStats = "Path Statistics:\n";
            foreach (var pathStat in stats.pathStats)
            {
                pathStats += $"{pathStat.Key}: {pathStat.Value.vehicleCount} vehicles\n";
            }
            pathStatsText.text = pathStats;
        }
    }
    
    // Event handlers
    public void ToggleTrafficGeneration()
    {
        if (trafficGenerator == null) return;
        
        if (trafficGenerator.isGenerating)
        {
            trafficGenerator.StopTrafficGeneration();
        }
        else
        {
            trafficGenerator.StartTrafficGeneration();
        }
        
        UpdateUI();
    }
    
    public void ClearAllVehicles()
    {
        if (trafficGenerator == null) return;
        
        trafficGenerator.ClearAllVehicles();
        UpdateStatistics();
    }
    
    public void OnGlobalDensityChanged(float value)
    {
        if (trafficGenerator == null) return;
        
        trafficGenerator.globalDensityMultiplier = value;
        UpdateTextDisplays();
    }
    
    public void OnMaxVehiclesChanged(float value)
    {
        if (trafficGenerator == null) return;
        
        trafficGenerator.maxVehicles = Mathf.RoundToInt(value);
        UpdateTextDisplays();
    }
    
    public void OnScientificMetricsChanged(bool value)
    {
        if (trafficGenerator == null) return;
        
        trafficGenerator.useScientificMetrics = value;
    }
    
    public void OnBaseVehiclesChanged(float value)
    {
        if (trafficGenerator == null) return;
        
        trafficGenerator.baseVehiclesPerHourPerLane = value;
        UpdateTextDisplays();
    }
    
    public void OnLaneCapacityChanged(float value)
    {
        if (trafficGenerator == null) return;
        
        trafficGenerator.laneCapacityFactor = value;
        UpdateTextDisplays();
    }
    
    public void OnSpeedFlowChanged(float value)
    {
        if (trafficGenerator == null) return;
        
        trafficGenerator.speedFlowExponent = value;
        UpdateTextDisplays();
    }
    
    // UI visibility controls
    public void ToggleControlPanel()
    {
        if (controlPanel != null)
        {
            isControlPanelVisible = !isControlPanelVisible;
            controlPanel.SetActive(isControlPanelVisible);
        }
    }
    
    public void ToggleStatsPanel()
    {
        if (statsPanel != null)
        {
            statsPanel.SetActive(!statsPanel.activeSelf);
        }
    }
    
    public void ShowControlPanel()
    {
        if (controlPanel != null)
        {
            controlPanel.SetActive(true);
            isControlPanelVisible = true;
        }
    }
    
    public void HideControlPanel()
    {
        if (controlPanel != null)
        {
            controlPanel.SetActive(false);
            isControlPanelVisible = false;
        }
    }
    
    // Public methods for external control
    public void SetGlobalDensity(float density)
    {
        if (trafficGenerator != null)
        {
            trafficGenerator.globalDensityMultiplier = density;
            if (globalDensitySlider != null)
            {
                globalDensitySlider.value = density;
            }
            UpdateTextDisplays();
        }
    }
    
    public void SetMaxVehicles(int maxVehicles)
    {
        if (trafficGenerator != null)
        {
            trafficGenerator.maxVehicles = maxVehicles;
            if (maxVehiclesSlider != null)
            {
                maxVehiclesSlider.value = maxVehicles;
            }
            UpdateTextDisplays();
        }
    }
    
    public void EnableScientificMetrics(bool enable)
    {
        if (trafficGenerator != null)
        {
            trafficGenerator.useScientificMetrics = enable;
            if (scientificMetricsToggle != null)
            {
                scientificMetricsToggle.isOn = enable;
            }
        }
    }
}
