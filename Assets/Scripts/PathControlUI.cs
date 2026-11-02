using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWS;
using DG.Tweening;

/// <summary>
/// UI component for controlling individual paths in the traffic generator.
/// Allows users to adjust settings for each path separately.
/// </summary>
public class PathControlUI : MonoBehaviour
{
    [Header("Path Information")]
    [Tooltip("Path name display")]
    public TextMeshProUGUI pathNameText;
    
    [Tooltip("Path status indicator")]
    public Image statusIndicator;
    
    [Header("Path Controls")]
    [Tooltip("Enable/disable path toggle")]
    public Toggle enableToggle;
    
    [Tooltip("Density multiplier slider")]
    public Slider densitySlider;
    
    [Tooltip("Density value text")]
    public TextMeshProUGUI densityText;
    
    [Tooltip("Speed limit slider")]
    public Slider speedSlider;
    
    [Tooltip("Speed value text")]
    public TextMeshProUGUI speedText;
    
    [Tooltip("Lane count slider")]
    public Slider laneCountSlider;
    
    [Tooltip("Lane count text")]
    public TextMeshProUGUI laneCountText;
    
    [Header("Path Statistics")]
    [Tooltip("Current vehicle count text")]
    public TextMeshProUGUI vehicleCountText;
    
    [Tooltip("Path length text")]
    public TextMeshProUGUI pathLengthText;
    
    [Tooltip("Last spawn time text")]
    public TextMeshProUGUI lastSpawnText;
    
    [Header("Visual Elements")]
    [Tooltip("Path color indicator")]
    public Image pathColorIndicator;
    
    [Tooltip("Expand/collapse button")]
    public Button expandButton;
    
    [Tooltip("Expand/collapse button text")]
    public TextMeshProUGUI expandButtonText;
    
    [Tooltip("Expanded content panel")]
    public GameObject expandedContent;
    
    [Header("Settings")]
    [Tooltip("Whether the path control is expanded by default")]
    public bool expandedByDefault = false;
    
    // Private variables
    private TrafficGenerator.PathConfig pathConfig;
    private TrafficGenerator trafficGenerator;
    private bool isExpanded;
    
    void Start()
    {
        SetupEventListeners();
        SetExpanded(expandedByDefault);
    }
    
    /// <summary>
    /// Initialize this path control with the given path configuration
    /// </summary>
    public void Initialize(TrafficGenerator.PathConfig config, TrafficGenerator generator)
    {
        pathConfig = config;
        trafficGenerator = generator;
        
        if (pathConfig == null || trafficGenerator == null) return;
        
        // Set initial values
        UpdateUI();
        
        // Set up event listeners
        SetupEventListeners();
    }
    
    void SetupEventListeners()
    {
        // Enable toggle
        if (enableToggle != null)
        {
            enableToggle.onValueChanged.AddListener(OnEnableChanged);
        }
        
        // Density slider
        if (densitySlider != null)
        {
            densitySlider.onValueChanged.AddListener(OnDensityChanged);
        }
        
        // Speed slider
        if (speedSlider != null)
        {
            speedSlider.onValueChanged.AddListener(OnSpeedChanged);
        }
        
        // Lane count slider
        if (laneCountSlider != null)
        {
            laneCountSlider.onValueChanged.AddListener(OnLaneCountChanged);
        }
        
        // Expand button
        if (expandButton != null)
        {
            expandButton.onClick.AddListener(ToggleExpanded);
        }
    }
    
    void Update()
    {
        // Update statistics if expanded
        if (isExpanded && pathConfig != null)
        {
            UpdateStatistics();
        }
    }
    
    void UpdateUI()
    {
        if (pathConfig == null) return;
        
        // Update path name
        if (pathNameText != null)
        {
            pathNameText.text = pathConfig.path != null ? pathConfig.path.name : "Unknown Path";
        }
        
        // Update status indicator
        if (statusIndicator != null)
        {
            statusIndicator.color = pathConfig.enabled ? Color.green : Color.red;
        }
        
        // Update enable toggle
        if (enableToggle != null)
        {
            enableToggle.isOn = pathConfig.enabled;
        }
        
        // Update density slider and text
        if (densitySlider != null)
        {
            densitySlider.value = pathConfig.densityMultiplier;
        }
        if (densityText != null)
        {
            densityText.text = $"Density: {pathConfig.densityMultiplier:F2}x";
        }
        
        // Update speed slider and text
        if (speedSlider != null)
        {
            speedSlider.value = pathConfig.speedLimit;
        }
        if (speedText != null)
        {
            speedText.text = $"Speed: {pathConfig.speedLimit:F0} m/s";
        }
        
        // Update lane count slider and text
        if (laneCountSlider != null)
        {
            laneCountSlider.value = pathConfig.laneCount;
        }
        if (laneCountText != null)
        {
            laneCountText.text = $"Lanes: {pathConfig.laneCount}";
        }
        
        // Update path color indicator
        if (pathColorIndicator != null && pathConfig.path != null)
        {
            // Try to get the path's gizmo color
            var pathManager = pathConfig.path.GetComponent<PathManager>();
            if (pathManager != null)
            {
                pathColorIndicator.color = pathManager.color2;
            }
        }
    }
    
    void UpdateStatistics()
    {
        if (pathConfig == null || trafficGenerator == null) return;
        
        var stats = trafficGenerator.GetTrafficStats();
        if (stats.pathStats.ContainsKey(pathConfig.path.name))
        {
            var pathStats = stats.pathStats[pathConfig.path.name];
            
            // Update vehicle count
            if (vehicleCountText != null)
            {
                vehicleCountText.text = $"Vehicles: {pathStats.vehicleCount}";
            }
            
            // Update path length
            if (pathLengthText != null)
            {
                pathLengthText.text = $"Length: {pathStats.pathLength:F1}m";
            }
            
            // Update last spawn time
            if (lastSpawnText != null)
            {
                float timeSinceSpawn = Time.time - pathStats.lastSpawnTime;
                if (timeSinceSpawn < 0)
                {
                    lastSpawnText.text = "Last Spawn: Never";
                }
                else
                {
                    lastSpawnText.text = $"Last Spawn: {timeSinceSpawn:F1}s ago";
                }
            }
        }
    }
    
    // Event handlers
    public void OnEnableChanged(bool enabled)
    {
        if (pathConfig != null)
        {
            pathConfig.enabled = enabled;
            
            // Update status indicator
            if (statusIndicator != null)
            {
                statusIndicator.color = enabled ? Color.green : Color.red;
            }
            
            // Update traffic generator
            if (trafficGenerator != null)
            {
                trafficGenerator.SetPathEnabled(pathConfig.path.name, enabled);
            }
        }
    }
    
    public void OnDensityChanged(float value)
    {
        if (pathConfig != null)
        {
            pathConfig.densityMultiplier = value;
            
            // Update text
            if (densityText != null)
            {
                densityText.text = $"Density: {value:F2}x";
            }
            
            // Update traffic generator
            if (trafficGenerator != null)
            {
                trafficGenerator.SetPathDensity(pathConfig.path.name, value);
            }
        }
    }
    
    public void OnSpeedChanged(float value)
    {
        if (pathConfig != null)
        {
            pathConfig.speedLimit = value;
            
            // Update text
            if (speedText != null)
            {
                speedText.text = $"Speed: {value:F0} m/s";
            }
        }
    }
    
    public void OnLaneCountChanged(float value)
    {
        if (pathConfig != null)
        {
            pathConfig.laneCount = Mathf.RoundToInt(value);
            
            // Update text
            if (laneCountText != null)
            {
                laneCountText.text = $"Lanes: {pathConfig.laneCount}";
            }
        }
    }
    
    public void ToggleExpanded()
    {
        SetExpanded(!isExpanded);
    }
    
    public void SetExpanded(bool expanded)
    {
        isExpanded = expanded;
        
        // Update expanded content visibility
        if (expandedContent != null)
        {
            expandedContent.SetActive(expanded);
        }
        
        // Update expand button text
        if (expandButtonText != null)
        {
            expandButtonText.text = expanded ? "▼" : "▶";
        }
    }
    
    // Public methods for external control
    public void SetPathEnabled(bool enabled)
    {
        if (enableToggle != null)
        {
            enableToggle.isOn = enabled;
        }
        OnEnableChanged(enabled);
    }
    
    public void SetDensityMultiplier(float density)
    {
        if (densitySlider != null)
        {
            densitySlider.value = density;
        }
        OnDensityChanged(density);
    }
    
    public void SetSpeedLimit(float speed)
    {
        if (speedSlider != null)
        {
            speedSlider.value = speed;
        }
        OnSpeedChanged(speed);
    }
    
    public void SetLaneCount(int laneCount)
    {
        if (laneCountSlider != null)
        {
            laneCountSlider.value = laneCount;
        }
        OnLaneCountChanged(laneCount);
    }
    
    // Utility methods
    public bool IsEnabled()
    {
        return pathConfig != null && pathConfig.enabled;
    }
    
    public float GetDensityMultiplier()
    {
        return pathConfig != null ? pathConfig.densityMultiplier : 1.0f;
    }
    
    public float GetSpeedLimit()
    {
        return pathConfig != null ? pathConfig.speedLimit : 30.0f;
    }
    
    public int GetLaneCount()
    {
        return pathConfig != null ? pathConfig.laneCount : 2;
    }
    
    public string GetPathName()
    {
        return pathConfig != null && pathConfig.path != null ? pathConfig.path.name : "Unknown";
    }
    
    // Animation methods for smooth transitions
    public void AnimateIn()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        }
    }
    
    public void AnimateOut()
    {
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad).OnComplete(() => {
                Destroy(gameObject);
            });
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    // Validation
    void OnValidate()
    {
        // Ensure values are within reasonable ranges
        if (pathConfig != null)
        {
            pathConfig.densityMultiplier = Mathf.Clamp(pathConfig.densityMultiplier, 0.1f, 3.0f);
            pathConfig.speedLimit = Mathf.Max(1.0f, pathConfig.speedLimit);
            pathConfig.laneCount = Mathf.Clamp(pathConfig.laneCount, 1, 6);
        }
    }
}
