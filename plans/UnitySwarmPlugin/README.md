# Unity Swarm AI Plugin

A comprehensive, high-performance swarm intelligence system for Unity with integrated Claude Flow coordination.

## Overview

The Unity Swarm AI plugin provides advanced swarm intelligence capabilities designed for Unity developers who need sophisticated collective behavior systems. Built with performance, modularity, and ease of use in mind, it integrates seamlessly with Unity's ecosystem while offering powerful AI-driven coordination through Claude Flow.

## Features

### Core Swarm Intelligence
- **Advanced Behavioral Systems**: Separation, alignment, cohesion, formation, pathfinding, and combat behaviors
- **Flexible Agent Types**: Boids, formation agents, leaders, scouts, workers, and guards
- **Modular Architecture**: Extensible behavior system with custom behavior support
- **Real-time Coordination**: AI-powered swarm decision making and optimization

### Performance Optimization
- **Massive Scale Support**: Handle 10,000+ agents with adaptive performance scaling
- **Spatial Partitioning**: Octree, grid, and hierarchical partitioning systems
- **Level of Detail (LOD)**: Distance and importance-based performance optimization
- **Unity Job System**: Burst-compiled parallel processing for maximum performance
- **GPU Compute Shaders**: Extreme scale support for massive swarms

### Unity Integration
- **Native Unity Workflow**: Seamless integration with Unity's component system
- **DOTS Support**: Entity Component System support for ultimate performance
- **Editor Tools**: Comprehensive debugging, visualization, and authoring tools
- **Rendering Pipeline Support**: Compatible with URP, HDRP, and Built-in RP
- **Physics Integration**: Rigidbody-based and kinematic movement options

### Claude Flow AI Coordination
- **Intelligent Decision Making**: AI-powered behavior selection and optimization
- **Adaptive Learning**: Continuous improvement based on swarm performance
- **Pattern Recognition**: Recognition and replication of successful swarm patterns
- **Real-time Optimization**: Dynamic performance and behavior tuning

## Quick Start

### Installation

1. **Via Unity Package Manager**:
   ```
   https://github.com/swarm-world/unity-swarm-plugin.git
   ```

2. **Via Package Manager UI**:
   - Open Package Manager
   - Click "+" and select "Add package from git URL"
   - Enter the repository URL

### Basic Setup

1. **Create a Swarm Manager**:
   ```csharp
   // Add SwarmManagerComponent to a GameObject
   var swarmManager = gameObject.AddComponent<SwarmManagerComponent>();
   swarmManager.MaxAgents = 1000;
   swarmManager.PerformanceLevel = PerformanceLevel.Balanced;
   ```

2. **Spawn Agents**:
   ```csharp
   // Create agent prefab with SwarmAgentComponent
   var agentConfig = new AgentConfig
   {
       maxSpeed = 5f,
       perceptionRadius = 10f,
       agentType = AgentType.Boid
   };
   
   var agent = swarmManager.SpawnAgent(spawnPosition, agentConfig);
   ```

3. **Add Behaviors**:
   ```csharp
   // Add basic flocking behaviors
   swarmManager.AddGlobalBehavior(new SeparationBehavior());
   swarmManager.AddGlobalBehavior(new AlignmentBehavior());
   swarmManager.AddGlobalBehavior(new CohesionBehavior());
   ```

### Fluent API

```csharp
var swarm = SwarmBuilder.Create()
    .WithAgents(500)
    .UseBehavior<BoidBehavior>()
    .WithPerformance(PerformanceLevel.High)
    .EnableClaudeFlow()
    .Build();
```

## Architecture

### Plugin Structure
```
SwarmAI/
├── Runtime/
│   ├── Core/           # Fundamental systems and interfaces
│   ├── Behaviors/      # Swarm behavior implementations
│   ├── Coordination/   # Claude Flow integration
│   ├── Performance/    # Optimization systems
│   └── Utils/          # Utility classes
├── Editor/
│   ├── Windows/        # Custom editor windows
│   ├── Inspectors/     # Custom inspectors
│   ├── Tools/          # Editor tools
│   └── Wizards/        # Setup wizards
├── Tests/              # Automated testing
├── Documentation/      # Comprehensive documentation
└── Samples~/           # Example implementations
```

### Core Interfaces

- **ISwarmAgent**: Core agent interface for all swarm participants
- **ISwarmManager**: Central swarm management and coordination
- **ISwarmBehavior**: Modular behavior system for extensibility
- **ICoordinationSystem**: Claude Flow AI coordination integration
- **ISpatialPartitioning**: Performance optimization through spatial partitioning
- **ILODSystem**: Level of detail management for scalability

## Performance Characteristics

### Scalability Targets
- **Small Swarms** (1-100 agents): 120+ FPS, full feature set
- **Medium Swarms** (100-1000 agents): 60+ FPS, adaptive LOD
- **Large Swarms** (1000-10000 agents): 30+ FPS, aggressive optimization
- **Massive Swarms** (10000+ agents): 15+ FPS, GPU compute required

### Optimization Features
- **Spatial Partitioning**: O(log n) neighbor queries vs O(n²) brute force
- **Adaptive LOD**: Dynamic quality scaling based on distance and importance
- **Job System**: Multi-threaded Burst compilation for parallel processing
- **Memory Pooling**: Zero allocation runtime for stable swarms
- **GPU Compute**: Compute shader fallback for extreme scale scenarios

## Examples

### Basic Flocking
```csharp
public class BasicFlockingExample : MonoBehaviour
{
    void Start()
    {
        var swarmManager = SwarmAPI.CreateBoidSwarm(
            count: 200, 
            spawnRadius: 20f
        );
        
        // Swarm will automatically exhibit flocking behavior
    }
}
```

### Formation Flying
```csharp
public class FormationExample : MonoBehaviour
{
    void Start()
    {
        var swarm = SwarmBuilder.Create()
            .WithAgents(50)
            .UseBehavior<FormationBehavior>()
            .WithFormation(FormationType.V)
            .Build();
    }
}
```

### AI-Coordinated Swarm
```csharp
public class AICoordinatedExample : MonoBehaviour
{
    void Start()
    {
        var swarm = SwarmBuilder.Create()
            .WithAgents(1000)
            .EnableClaudeFlow()
            .WithCoordination(CoordinationLevel.Full)
            .Build();
        
        // AI will automatically optimize behavior and performance
    }
}
```

## Documentation

- [Architecture Overview](Documentation/Architecture.md)
- [Unity Integration Patterns](Documentation/UnityIntegrationPatterns.md)
- [Performance Optimization Guide](Documentation/PerformanceGuide.md)
- [Claude Flow Integration](Documentation/ClaudeFlowGuide.md)
- [API Reference](Documentation/APIReference.md)
- [Examples and Tutorials](Documentation/Examples.md)

## Requirements

- **Unity Version**: 2022.3 or later
- **Dependencies**:
  - Unity Burst (1.8.4+)
  - Unity Collections (1.4.0+)
  - Unity Entities (1.0.16+) - Optional for DOTS support
  - Unity Jobs (0.70.0+)
  - Unity Mathematics (1.2.6+)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Documentation**: [Full documentation available](Documentation/)
- **Issues**: [GitHub Issues](https://github.com/swarm-world/unity-swarm-plugin/issues)
- **Community**: [Discord Server](https://discord.gg/swarmworld)
- **Email**: support@swarmworld.dev

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for a detailed history of changes and updates.

---

Built with ❤️ by the Swarm World team. Enhanced with 🤖 Claude Flow AI coordination.