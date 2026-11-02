# Traffic Generator System for Unity

A comprehensive traffic generation module for Unity traffic simulators, built on top of the SWS (Simple Waypoint System) and designed to provide realistic, customizable traffic flow with scientific traffic engineering principles.

## Features

### 🚗 **Smart Vehicle Generation**
- **Distributed Spawning**: Vehicles spawn along the entire path, not just at the beginning
- **Random Vehicle Selection**: Multiple vehicle prefabs with random selection
- **Automatic Cleanup**: Vehicles are automatically destroyed when they complete their path
- **Collision Avoidance**: Built-in spacing controls prevent vehicle overlap

### 🛣️ **Multi-Path Support**
- **4 Predefined Paths**: Configure up to 4 different traffic paths
- **Individual Path Control**: Each path can have different density, speed limits, and lane counts
- **Path-Specific Settings**: Customize spawn points, density multipliers, and speed limits per path

### 📊 **Scientific Traffic Metrics**
- **Vehicles Per Hour Per Lane**: Based on real-world traffic engineering standards
- **Lane Capacity Factors**: Accounts for real-world capacity limitations
- **Speed-Flow Relationships**: Higher speeds generally reduce lane capacity
- **Time-of-Day Patterns**: Configurable traffic patterns for different times
- **Day-of-Week Patterns**: Different traffic levels for weekdays vs weekends

### 🎮 **Easy Control & Monitoring**
- **Real-time Statistics**: Live vehicle counts, density, and path information
- **Runtime Controls**: Start/stop, clear vehicles, adjust density on the fly
- **Keyboard Shortcuts**: Quick access to common functions
- **UI Integration**: Full Unity UI system integration

## Quick Start

### 1. Setup Traffic Generator

1. **Add the TrafficGenerator component** to an empty GameObject in your scene
2. **Assign your vehicle prefabs** to the `vehiclePrefabs` array
3. **Configure your paths** in the `paths` array:
   - Assign PathManager components
   - Set lane counts, speed limits, and density multipliers
   - Enable/disable paths as needed

### 2. Basic Configuration

```csharp
// Example path configuration
paths[0] = new PathConfig
{
    path = yourPath1,           // PathManager component
    laneCount = 2,              // Number of lanes
    densityMultiplier = 1.0f,   // Traffic density (1.0 = normal)
    speedLimit = 30f,           // Speed limit in m/s
    enabled = true              // Enable this path
};
```

### 3. Start Traffic Generation

```csharp
// The system starts automatically if autoStart = true
// Or manually control it:
trafficGenerator.StartTrafficGeneration();
trafficGenerator.StopTrafficGeneration();
trafficGenerator.ClearAllVehicles();
```

## Configuration Options

### Global Settings

| Setting | Description | Range | Default |
|---------|-------------|-------|---------|
| `globalDensityMultiplier` | Overall traffic density | 0.1x - 5.0x | 1.0x |
| `maxVehicles` | Maximum vehicles in scene | 1 - ∞ | 100 |
| `minVehicleSpacing` | Minimum time between vehicles | 1.0s - 10.0s | 2.0s |
| `spacingVariation` | Random spacing variation | 0.0s - 5.0s | 1.0s |

### Scientific Metrics

| Setting | Description | Typical Values |
|---------|-------------|----------------|
| `baseVehiclesPerHourPerLane` | Base traffic flow | Urban: 800-1200, Highway: 1500-2500 |
| `laneCapacityFactor` | Lane utilization | 0.8 (80% of theoretical capacity) |
| `speedFlowExponent` | Speed-flow relationship | 1.5-2.0 (higher speeds reduce capacity) |

### Path-Specific Settings

| Setting | Description | Range |
|---------|-------------|-------|
| `laneCount` | Number of lanes on this path | 1-6 |
| `densityMultiplier` | Traffic density for this path | 0.1x - 3.0x |
| `speedLimit` | Speed limit for vehicles | 1.0 - 100.0 m/s |
| `minSpawnDistance` | Distance between spawn points | 10.0 - 200.0m |

## Traffic Patterns

### Time-of-Day Patterns
Configure traffic density variations throughout the day:
- **Morning Rush**: 6-9 AM (high density)
- **Midday**: 9 AM - 4 PM (medium density)
- **Evening Rush**: 4-7 PM (high density)
- **Night**: 7 PM - 6 AM (low density)

### Day-of-Week Patterns
Different traffic levels for different days:
- **Weekdays**: Higher traffic density
- **Weekends**: Lower traffic density
- **Holidays**: Very low traffic density

## UI System

### TrafficGeneratorUI Component
Provides a complete UI system for controlling traffic generation:

- **Control Panel**: Start/stop, clear vehicles, adjust global settings
- **Path Controls**: Individual path settings with expandable panels
- **Statistics Display**: Real-time traffic statistics
- **Advanced Controls**: Scientific metrics configuration

### PathControlUI Component
Individual path control panels with:

- **Path Status**: Visual indicators for enabled/disabled paths
- **Density Control**: Sliders for adjusting traffic density
- **Speed Control**: Speed limit adjustments
- **Lane Configuration**: Lane count settings
- **Real-time Stats**: Vehicle counts and path information

## Advanced Features

### Custom Spawn Points
Instead of auto-distributed spawn points, you can specify custom spawn locations:

```csharp
pathConfig.customSpawnPoints = new Transform[]
{
    spawnPoint1,
    spawnPoint2,
    spawnPoint3
};
```

### Runtime Path Switching
Dynamically change which paths are active:

```csharp
trafficGenerator.SetPathEnabled("Path1", false);
trafficGenerator.SetPathEnabled("Path2", true);
```

### Density Adjustments
Modify traffic density for specific paths in real-time:

```csharp
trafficGenerator.SetPathDensity("Path1", 2.0f); // Double density
trafficGenerator.SetPathDensity("Path2", 0.5f); // Half density
```

## Performance Considerations

### Optimization Tips
1. **Limit Max Vehicles**: Keep `maxVehicles` reasonable (50-200)
2. **Use Object Pooling**: For very high traffic scenarios
3. **LOD Systems**: Implement level-of-detail for distant vehicles
4. **Culling**: Use Unity's built-in culling systems

### Memory Management
- Vehicles are automatically destroyed when they complete their path
- The system maintains a clean list of active vehicles
- No memory leaks from abandoned vehicles

## Troubleshooting

### Common Issues

**No vehicles spawning:**
- Check if paths are assigned and enabled
- Verify vehicle prefabs are assigned
- Check console for error messages

**Vehicles spawning at wrong positions:**
- Ensure waypoints are properly positioned
- Check if `moveToPath` is disabled
- Verify path container assignments

**Performance issues:**
- Reduce `maxVehicles` value
- Increase `minVehicleSpacing`
- Check for path complexity issues

### Debug Information
Enable console logging to see detailed information:
- Vehicle spawn events
- Path completion events
- Traffic density calculations
- Error messages

## Integration with Existing Systems

### SWS Compatibility
This system is built on top of SWS and maintains full compatibility:
- Uses existing `splineMove` components
- Works with `PathManager` and `BezierPathManager`
- Integrates with `WaypointManager`

### Vehicle System Integration
Works with various vehicle systems:
- **EVP (Edy's Vehicle Physics)**: Automatically disables input systems
- **Custom Vehicle Controllers**: Can be configured to work with any system
- **AI Vehicles**: Perfect for autonomous traffic simulation

### V2X Integration
Can be integrated with V2X systems:
- Traffic information broadcasting
- Vehicle-to-vehicle communication
- Infrastructure coordination

## Examples

### Basic Setup
```csharp
// Create a simple traffic generator
var generator = gameObject.AddComponent<TrafficGenerator>();
generator.vehiclePrefabs = new GameObject[] { carPrefab1, carPrefab2 };
generator.paths = new PathConfig[] { path1, path2, path3, path4 };
generator.StartTrafficGeneration();
```

### Runtime Control
```csharp
// Adjust traffic based on time
if (IsRushHour())
{
    trafficGenerator.globalDensityMultiplier = 2.0f;
}
else
{
    trafficGenerator.globalDensityMultiplier = 0.5f;
}
```

### Statistics Monitoring
```csharp
// Get current traffic statistics
var stats = trafficGenerator.GetTrafficStats();
Debug.Log($"Total vehicles: {stats.totalVehicles}");
Debug.Log($"Current density: {stats.trafficDensity:F2}x");
```

## Support and Development

### Requirements
- Unity 2019.4 or later
- SWS (Simple Waypoint System)
- DOTween (included with SWS)

### Dependencies
- `SWS.splineMove`
- `SWS.PathManager`
- `SWS.WaypointManager`
- `UnityEngine.UI`
- `TMPro` (TextMeshPro)

### Future Enhancements
- **Traffic Light Integration**: Synchronize with traffic signal systems
- **Pedestrian Traffic**: Add pedestrian generation systems
- **Weather Effects**: Traffic density changes based on weather
- **Event-Based Traffic**: Special events that affect traffic patterns
- **Multi-Lane Logic**: Advanced lane changing and merging

---

This traffic generation system provides a robust foundation for realistic traffic simulation in Unity, with the flexibility to adapt to various simulation requirements while maintaining performance and ease of use.
