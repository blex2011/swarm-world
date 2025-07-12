# Swarm World Unity Editor Integration

This directory contains the complete Unity Editor integration for Swarm World, providing comprehensive tools for creating, managing, and optimizing swarm systems directly within the Unity Editor.

## 🚀 Features

### Core Editor Components

#### 1. **SwarmWindow.cs** - Main Control Panel
- 🎛️ Centralized swarm management interface
- 📊 Real-time performance monitoring
- 👥 Agent management and spawning
- ⚙️ Dynamic configuration controls
- 📈 Performance metrics visualization

**Usage:**
```
Menu: Swarm > Swarm Control Panel
```

#### 2. **AgentInspector.cs** - Advanced Agent Inspector
- 🔍 Real-time agent state monitoring
- 🧠 Behavior analysis and visualization
- 👥 Neighbor relationship tracking
- ⚡ Performance profiling per agent
- 🎮 Interactive debugging controls

**Features:**
- Live velocity and force vector display
- Neighbor list with distance calculations
- Behavior weight sliders with real-time feedback
- Performance warnings and optimization suggestions

#### 3. **PerformanceDashboard.cs** - Performance Analysis
- 📈 Real-time performance charts
- 🎯 Bottleneck identification
- 💡 Automatic optimization suggestions
- 📊 Detailed profiling reports
- 🔧 One-click performance fixes

**Metrics Tracked:**
- FPS and frame time
- Update and physics time
- Memory usage
- Agent count and neighbor density

#### 4. **SwarmMenuItems.cs** - Menu Integration
- 📋 Complete menu system integration
- 🛠️ Quick creation tools
- 🔧 Optimization utilities
- 🐛 Debug tools
- ✅ Validation systems

**Menu Structure:**
```
Swarm/
├── Swarm Control Panel
├── Performance Dashboard
├── Agent Inspector
├── Tools/
│   ├── Optimize All Swarms
│   ├── Validate Swarm Setup
│   ├── Performance Analysis
│   └── Generate Performance Report
├── Create/
│   ├── Basic Swarm
│   ├── Combat Swarm
│   ├── Formation Swarm
│   ├── Custom Swarm
│   └── Swarm Agent Prefab
└── Debug/
    ├── Toggle Debug Visualization
    ├── Clear All Swarm Data
    ├── Export Debug Information
    └── Reset All Agents
```

### 5. **SwarmEditorUtilities.cs** - Utility Functions
- 💾 Configuration save/load
- 🎬 Scene setup automation
- 📊 Performance analysis
- 📤 Debug data export
- 🎨 Visualization helpers

### 6. **SwarmCameraController.cs** - Camera System
- 📹 Intelligent swarm following
- 🎯 Auto-framing capabilities
- 🎮 Intuitive camera controls
- 📐 Boundary constraints
- 🔍 Focus and zoom tools

## 🎮 Usage Guide

### Getting Started

1. **Open Control Panel:**
   ```
   Swarm > Swarm Control Panel
   ```

2. **Create Your First Swarm:**
   ```
   Swarm > Create > Basic Swarm
   ```

3. **Monitor Performance:**
   ```
   Swarm > Performance Dashboard
   ```

### Key Workflows

#### Creating a Swarm
1. Use `Swarm > Create > [Swarm Type]` for templates
2. Or use `Swarm > Create > Custom Swarm` for full control
3. Configure in the Swarm Control Panel
4. Assign agent prefabs via `Swarm > Create > Swarm Agent Prefab`

#### Performance Optimization
1. Open Performance Dashboard
2. Enter Play Mode to collect metrics
3. Review automatic suggestions
4. Apply optimizations with one click
5. Use `Swarm > Tools > Optimize All Swarms` for batch optimization

#### Debugging Issues
1. Select problematic agents to open Agent Inspector
2. Enable debug visualization: `Swarm > Debug > Toggle Debug Visualization`
3. Use `Swarm > Tools > Validate Swarm Setup` to check configuration
4. Export debug data: `Swarm > Debug > Export Debug Information`

### Inspector Features

#### SwarmAgent Inspector Enhancement
When you select a GameObject with a SwarmAgent component, the inspector automatically enhances to show:

- **Real-Time Status:** Position, velocity, speed, neighbor count
- **Behavior Analysis:** Live force calculations with visual bars
- **Neighbor List:** Interactive list with distances and selection
- **Debug Controls:** Toggle various visualization options
- **Performance Info:** Update times and optimization recommendations

#### Scene View Integration
- **Velocity Vectors:** Green arrows showing movement direction
- **Perception Radius:** Yellow circles showing detection range
- **Neighbor Connections:** Cyan lines between connected agents
- **Force Vectors:** Colored arrows for separation, alignment, cohesion
- **Agent Info Labels:** Real-time data overlays

## 🔧 Configuration

### Editor Preferences
The system automatically saves preferences using EditorPrefs:
- Window positions and sizes
- Last used configurations
- Performance thresholds
- Debug visualization settings

### Performance Thresholds
Default performance targets (configurable):
- Target FPS: 60
- Max Update Time: 16.67ms
- Max Memory: 512MB
- Max Agent Count: 5000

### Auto-Optimization
The system can automatically:
- Reduce agent count when FPS drops
- Enable spatial partitioning for large swarms
- Activate LOD systems for distant agents
- Adjust behavior weights for stability

## 📊 Performance Analysis

### Real-Time Monitoring
- **FPS Tracking:** Continuous frame rate monitoring
- **Update Time:** Per-frame update performance
- **Memory Usage:** Live memory consumption
- **Agent Metrics:** Individual agent performance

### Bottleneck Detection
Automatic identification of:
- High update times (suggests spatial partitioning)
- Excessive neighbor counts (suggests LOD system)
- Memory leaks (suggests optimization)
- Frame rate drops (suggests agent reduction)

### Optimization Suggestions
Smart recommendations based on:
- Current performance metrics
- Swarm configuration analysis
- Best practice patterns
- Hardware capabilities

## 🐛 Debug Tools

### Visualization Options
- **Agent Connections:** Visual neighbor relationships
- **Force Vectors:** Behavior force visualization
- **Performance Overlays:** Real-time metric display
- **Spatial Partitioning:** Grid visualization

### Data Export
- **Debug Information:** Complete system state export
- **Performance Reports:** Detailed analysis documents
- **Configuration Backup:** Swarm settings preservation
- **Metrics History:** Time-series performance data

## 🚨 Validation System

### Automatic Checks
- Agent prefab validation
- Component dependency verification
- Performance threshold monitoring
- Configuration consistency

### Error Prevention
- Prevents invalid configurations
- Warns about performance issues
- Suggests fixes for common problems
- Validates before Play Mode

## 🎯 Best Practices

### Performance Optimization
1. Start with basic swarms and scale up
2. Use Performance Dashboard to identify bottlenecks
3. Enable spatial partitioning for 100+ agents
4. Use LOD systems for large swarms
5. Monitor memory usage regularly

### Development Workflow
1. Use templates for quick prototyping
2. Validate setup before testing
3. Use debug visualization during development
4. Export configurations for reuse
5. Regular performance analysis

### Debugging Strategy
1. Use Agent Inspector for individual agent issues
2. Performance Dashboard for system-wide problems
3. Scene view visualization for spatial issues
4. Debug data export for complex analysis

## 📋 Integration Checklist

- ✅ Main Control Panel (`SwarmWindow.cs`)
- ✅ Agent Inspector (`AgentInspector.cs`)
- ✅ Performance Dashboard (`PerformanceDashboard.cs`)
- ✅ Menu Integration (`SwarmMenuItems.cs`)
- ✅ Editor Utilities (`SwarmEditorUtilities.cs`)
- ✅ Camera Controller (`SwarmCameraController.cs`)
- ✅ Assembly Definition (`SwarmWorld.Editor.asmdef`)

## 🔗 Dependencies

This editor package requires:
- Unity 2021.3 LTS or later
- SwarmWorld.Runtime assembly
- Unity Editor (Editor-only assembly)

## 🚀 Advanced Features

### Custom Inspector Integration
- Automatic enhancement of SwarmAgent components
- Real-time data visualization
- Interactive debugging controls
- Performance profiling integration

### Scene Management
- Automatic scene setup for swarm development
- Camera positioning and configuration
- Lighting and environment setup
- Prefab creation and management

### Export/Import System
- Configuration templates
- Performance baselines
- Debug snapshots
- Cross-project compatibility

---

This comprehensive Unity Editor integration transforms swarm development from a complex technical challenge into an intuitive, visual, and highly optimized workflow. The tools provide everything needed to create, debug, and optimize sophisticated swarm systems directly within the Unity Editor environment.