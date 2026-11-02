# V2X (Vehicle-to-Everything) Communication System

This V2X module implements Vehicle-to-Infrastructure (V2I) communication for intersection management in Unity. It provides a framework for vehicles to communicate with Road Side Units (RSUs) to coordinate intersection access.

## Overview

The V2X system consists of several key components:

### Core Components

1. **V2XBus** - Central communication hub that manages all radio communications
2. **V2XRadio** - Individual vehicle radio that sends/receives messages
3. **IntersectionRSU** - Abstract base class for Road Side Units (RSUs)
4. **StopRSU** - RSU for stop-sign intersections (four-way or side-road)
5. **TrafficLightSpatRSU** - Sends signal phase and timing (SPaT) and the stop-line position to the ego vehicle
6. **Messages** - Data structures for communication (BSM, RSU messages)

### Communication Flow

1. Vehicles with V2XRadio components register with V2XBus
2. V2XBus broadcasts messages between vehicles within range
3. RSUs manage intersection access using queues and safety checks
4. Vehicles request entry, receive grants, and clear when exiting

## Setup Instructions

### 1. Create V2XBus

1. Create an empty GameObject in your scene
2. Add the `V2XBus` component
3. Configure the RF parameters:
   - **Max Range**: Maximum communication range in meters (default: 120m)
   - **Tick Rate**: How often radios exchange packets in Hz (default: 10Hz)

### 2. Add V2XRadio to Vehicles

1. Select your vehicle GameObject
2. Add the `V2XRadio` component
3. Set a unique `VehicleId`

### 3. Create Intersections

#### Stop Sign Intersection
1. Create an empty GameObject for the intersection
2. Add a `BoxCollider` component and set it as a trigger
3. Add the `StopRSU` component
4. Configure the stop sign parameters:
   - **Stop Type**: FourWay or SideRoad
   - **Side Road Gap**: Minimum time gap for side road vehicles (seconds)
   - **Grant Check Hz**: How often to check for grants

## Usage

### Manual Integration

For custom vehicle controllers, you can integrate directly:

```csharp
// Get the V2X radio
V2XRadio radio = GetComponent<V2XRadio>();

// Request entry to an intersection
radio.SendToRsu(RsuCmd.RequestEntry, intersectionRSU);

// Check if grant received
if (radio.GrantReceived)
{
    // Proceed through intersection
}

// Clear when exiting
radio.SendToRsu(RsuCmd.Clear, intersectionRSU);
```

## Message Types

### Basic Safety Message (BSM)
Contains vehicle state information:
- Vehicle ID
- Position
- Velocity
- Heading
- Timestamp

### RSU Messages
Commands for intersection management:
- **RequestEntry**: Vehicle requests permission to enter
- **Grant**: RSU grants permission to enter
- **Wait**: RSU tells vehicle to wait
- **Clear**: Vehicle signals it has cleared the intersection

## Intersection Types

### Stop Sign (StopRSU)
- **Four-way**: First-come-first-served when conflict zone is empty
- **Side road**: Must yield to main road traffic with time gap

## Performance Considerations

- The current implementation uses O(n²) broadcast (suitable for up to ~200 vehicles)
- Consider implementing slot-based hashing for larger vehicle counts
- Adjust tick rates based on your simulation requirements

## Debugging

The system includes several debugging features:
- Console logs for intersection requests and grants
- Visual gizmos showing communication ranges
- Debug lines between vehicles and intersections
- Color-coded status indicators

## Extending the System

### Adding New Intersection Types
1. Create a new class inheriting from `IntersectionRSU`
2. Implement the `IsSafeToEnter` method with your logic
3. Add any additional parameters specific to your intersection type

### Custom Message Types
1. Add new commands to the `RsuCmd` enum
2. Handle new commands in the `RadioInbox` method
3. Update vehicle logic to send/receive new messages

## Troubleshooting

### Common Issues

1. **Vehicles not communicating**: Ensure V2XBus exists in the scene
2. **No grants received**: Check intersection trigger colliders and RSU components
3. **Performance issues**: Reduce tick rate or implement spatial partitioning
4. **Missing references**: Ensure all required components are attached

### Debug Steps

1. Check console for error messages
2. Verify V2XBus is registered (should see "I = this" in Awake)
3. Confirm vehicles are within communication range
4. Test intersection triggers with simple colliders
5. Use visual gizmos to verify detection ranges 