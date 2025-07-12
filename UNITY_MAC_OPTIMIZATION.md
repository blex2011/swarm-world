# Unity on Mac Optimization Guide
# Swarm World Project - macOS Development Best Practices

## 🍎 macOS Unity Development Setup

### Unity Version Recommendations for Mac
- **Unity 2022.3 LTS** (Silicon/Intel compatible)
- **Unity 2023.2+** (M1/M2 optimized builds)
- **Avoid Unity 2021.x** on Apple Silicon (compatibility issues)

### Mac-Specific Unity Configuration

#### Apple Silicon (M1/M2) Optimizations
```bash
# Enable Rosetta 2 for older Unity versions
/usr/sbin/softwareupdate --install-rosetta --agree-to-license

# Force Unity to use native ARM64 when available
export UNITY_FORCE_ARM64=1

# Memory optimization for M1/M2
export UNITY_METAL_MEMORY_POOL_SIZE=512MB
```

#### Intel Mac Optimizations
```bash
# Increase memory allocation for large swarms
export UNITY_MEMORY_ALLOCATION_SIZE=4096MB

# Enable GPU acceleration
export UNITY_FORCE_METAL=1
```

## 🚀 Performance Optimizations for Unity on Mac

### 1. Build Configuration for macOS

#### Recommended Player Settings
```csharp
// In PlayerSettings
- Architecture: Universal (Intel + Apple Silicon)
- Scripting Backend: IL2CPP
- Api Compatibility Level: .NET Standard 2.1
- Managed Stripping Level: Medium
- Metal Editor Support: True
- Metal Graphics Jobs: True
- Script Debugging: False (Release builds)
```

#### Build Script for Mac Universal Binary
```csharp
[MenuItem("Swarm/Build Mac Universal")]
public static void BuildMacUniversal()
{
    PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 2); // Universal
    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
    
    BuildPipeline.BuildPlayer(new BuildPlayerOptions
    {
        scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
        locationPathName = "Builds/SwarmWorld-Mac-Universal.app",
        target = BuildTarget.StandaloneOSX,
        options = BuildOptions.None
    });
}
```

### 2. Memory Management for Mac

#### Swarm-Specific Memory Optimization
```csharp
// Mac-optimized memory pools
public class MacMemoryOptimizer : MonoBehaviour
{
    [Header("Mac-Specific Settings")]
    public bool OptimizeForAppleSilicon = true;
    public int MaxAgentsAppleSilicon = 50000;
    public int MaxAgentsIntel = 20000;
    
    private void Start()
    {
        ConfigureForMac();
    }
    
    private void ConfigureForMac()
    {
        bool isAppleSilicon = SystemInfo.processorType.Contains("Apple");
        
        if (OptimizeForAppleSilicon && isAppleSilicon)
        {
            // Apple Silicon optimizations
            Application.targetFrameRate = 120; // ProMotion displays
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4; // M1/M2 can handle higher AA
            
            // Increase memory pools
            SwarmManager.MaxAgents = MaxAgentsAppleSilicon;
        }
        else
        {
            // Intel Mac settings
            Application.targetFrameRate = 60;
            QualitySettings.antiAliasing = 2;
            SwarmManager.MaxAgents = MaxAgentsIntel;
        }
        
        // Mac-specific memory allocation
        if (Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.OSXEditor)
        {
            // Enable Metal for better GPU memory management
            SystemInfo.supportsComputeShaders = true;
            
            // Optimize garbage collection for Mac
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }
    }
}
```

### 3. Metal Shaders and GPU Optimization

#### Swarm Compute Shader for Metal
```hlsl
// SwarmComputeMetal.compute
#pragma kernel CSMain

[numthreads(64,1,1)]
void CSMain (uint3 id : SV_DispatchThreadID)
{
    // Metal-optimized swarm calculations
    uint index = id.x;
    
    // Use Metal-specific optimizations
    // Fast math operations for Apple GPU
    float3 position = AgentPositions[index];
    float3 velocity = AgentVelocities[index];
    
    // Metal supports efficient neighbor searches
    float3 swarmForce = CalculateSwarmForce(index, position);
    
    // Update with Metal-optimized math
    velocity += swarmForce * DeltaTime;
    position += velocity * DeltaTime;
    
    AgentPositions[index] = position;
    AgentVelocities[index] = velocity;
}
```

#### Mac GPU Detection and Optimization
```csharp
public class MacGPUOptimizer
{
    public static void OptimizeForMacGPU()
    {
        string gpuName = SystemInfo.graphicsDeviceName.ToLower();
        
        if (gpuName.Contains("apple"))
        {
            // Apple Silicon GPU (M1/M2/M3)
            ConfigureForAppleGPU();
        }
        else if (gpuName.Contains("radeon"))
        {
            // AMD GPU in Mac Pro/iMac
            ConfigureForAMDGPU();
        }
        else if (gpuName.Contains("intel"))
        {
            // Intel integrated graphics
            ConfigureForIntelGPU();
        }
    }
    
    private static void ConfigureForAppleGPU()
    {
        // M1/M2 unified memory architecture benefits
        QualitySettings.textureMipmapStreaming = true;
        QualitySettings.streamingMipmapsMemoryBudget = 2048; // 2GB for unified memory
        
        // Enable Metal-specific features
        Shader.EnableKeyword("UNITY_PLATFORM_OSX_METAL");
    }
}
```

## 🔧 Xcode Integration for iOS Builds

### Unity to Xcode Build Pipeline
```csharp
[PostProcessBuild]
public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
{
    if (target == BuildTarget.iOS)
    {
        // Modify Xcode project for Swarm World
        string projectPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";
        
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);
        
        string targetGuid = project.GetUnityMainTargetGuid();
        
        // Add frameworks needed for swarm AI
        project.AddFrameworkToProject(targetGuid, "Metal.framework", false);
        project.AddFrameworkToProject(targetGuid, "MetalPerformanceShaders.framework", false);
        project.AddFrameworkToProject(targetGuid, "Accelerate.framework", false);
        
        // Enable bitcode for App Store
        project.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "YES");
        
        // Apple Silicon optimization
        project.SetBuildProperty(targetGuid, "EXCLUDED_ARCHS[sdk=iphonesimulator*]", "arm64");
        
        project.WriteToFile(projectPath);
    }
}
```

### iOS Metal Shaders for Swarm
```objc
// SwarmMetalKernel.metal
#include <metal_stdlib>
using namespace metal;

// Optimized for iOS Metal
kernel void swarm_update(device float3* positions [[buffer(0)]],
                        device float3* velocities [[buffer(1)]],
                        constant float& deltaTime [[buffer(2)]],
                        uint index [[thread_position_in_grid]])
{
    // iOS-optimized swarm calculations
    float3 pos = positions[index];
    float3 vel = velocities[index];
    
    // Use iOS Metal's efficient SIMD operations
    float3 force = calculate_swarm_force(pos, positions, index);
    
    vel += force * deltaTime;
    pos += vel * deltaTime;
    
    positions[index] = pos;
    velocities[index] = vel;
}
```

## 📁 macOS File System Optimizations

### Unity Project Structure for Mac
```
SwarmWorld/
├── Assets/
│   ├── Swarm/                      # Main swarm assets
│   │   ├── Scripts/
│   │   ├── Prefabs/
│   │   └── Materials/
│   ├── StreamingAssets/            # Mac-accessible runtime data
│   │   ├── SwarmConfigs/
│   │   └── MacOptimizations/
│   └── Plugins/
│       ├── macOS/                  # Mac-specific native plugins
│       └── iOS/                    # iOS native plugins
├── Packages/                       # Unity Package Manager
│   ├── manifest.json              # Keep in version control
│   └── packages-lock.json         # Keep in version control
├── ProjectSettings/               # Unity project settings
└── UserSettings/                  # User-specific (ignored)
```

### Mac-Specific File Handling
```csharp
public class MacFileHandler
{
    public static string GetMacOptimizedPath(string relativePath)
    {
        if (Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.OSXEditor)
        {
            // Use Mac-specific paths
            string basePath = Application.persistentDataPath;
            
            // Handle Mac sandboxing
            if (Application.isSandboxed)
            {
                return Path.Combine(basePath, "SwarmData", relativePath);
            }
            
            return Path.Combine(basePath, relativePath);
        }
        
        return relativePath;
    }
    
    public static void ConfigureMacPermissions()
    {
        #if UNITY_STANDALONE_OSX
        // Request necessary permissions for Mac
        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            // Handle Mac privacy permissions
            RequestCameraPermission();
            RequestMicrophonePermission();
        }
        #endif
    }
}
```

## 🎮 Input System for Mac

### Mac-Specific Input Handling
```csharp
public class MacInputHandler : MonoBehaviour
{
    private void Update()
    {
        // Mac-specific input handling
        if (Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.OSXEditor)
        {
            HandleMacInput();
        }
    }
    
    private void HandleMacInput()
    {
        // Trackpad gestures for swarm control
        if (Input.touchCount == 2)
        {
            HandleTwoFingerGestures();
        }
        
        // Magic Mouse support
        Vector2 scrollDelta = Input.mouseScrollDelta;
        if (scrollDelta != Vector2.zero)
        {
            HandleScrollInput(scrollDelta);
        }
        
        // Command key combinations
        if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
        {
            HandleCommandKeys();
        }
    }
}
```

## 🔒 macOS Security and Notarization

### Preparing for Mac App Store
```bash
#!/bin/bash
# BuildMacRelease.sh

# Clean previous builds
rm -rf Builds/Mac/

# Build Unity project
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
    -batchmode \
    -quit \
    -projectPath . \
    -buildTarget StandaloneOSX \
    -buildPath Builds/Mac/SwarmWorld.app

# Sign the app
codesign --force --verify --verbose --sign "Developer ID Application: Your Name" \
    Builds/Mac/SwarmWorld.app

# Create installer package
productbuild --component Builds/Mac/SwarmWorld.app /Applications \
    Builds/Mac/SwarmWorld-Installer.pkg \
    --sign "Developer ID Installer: Your Name"

# Notarize for macOS Gatekeeper
xcrun altool --notarize-app \
    --primary-bundle-id "com.swarmworld.unity" \
    --username "your-apple-id@email.com" \
    --password "@keychain:AC_PASSWORD" \
    --file Builds/Mac/SwarmWorld-Installer.pkg
```

### Entitlements for Mac Sandbox
```xml
<!-- SwarmWorld.entitlements -->
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.app-sandbox</key>
    <true/>
    <key>com.apple.security.files.user-selected.read-write</key>
    <true/>
    <key>com.apple.security.files.downloads.read-write</key>
    <true/>
    <key>com.apple.security.network.client</key>
    <true/>
    <key>com.apple.security.device.gpu</key>
    <true/>
</dict>
</plist>
```

## 📊 Performance Monitoring for Mac

### Mac-Specific Performance Tracking
```csharp
public class MacPerformanceMonitor : MonoBehaviour
{
    private void Start()
    {
        if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.MacOSX)
        {
            StartCoroutine(MonitorMacPerformance());
        }
    }
    
    private IEnumerator MonitorMacPerformance()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            // Mac-specific metrics
            long memoryUsage = System.GC.GetTotalMemory(false);
            float cpuUsage = GetMacCPUUsage();
            float gpuUsage = GetMacGPUUsage();
            float thermalState = GetMacThermalState();
            
            LogMacMetrics(memoryUsage, cpuUsage, gpuUsage, thermalState);
            
            // Adjust quality based on thermal state
            if (thermalState > 0.8f)
            {
                ReduceQualityForThermalManagement();
            }
        }
    }
    
    private float GetMacThermalState()
    {
        // Use Mac thermal state API
        #if UNITY_STANDALONE_OSX
        // Implementation would use native Mac thermal APIs
        return 0.5f; // Placeholder
        #else
        return 0f;
        #endif
    }
}
```

## 🧪 Testing on Mac

### Automated Mac Testing
```bash
#!/bin/bash
# RunMacTests.sh

# Unity Test Runner for Mac
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
    -batchmode \
    -quit \
    -projectPath . \
    -runTests \
    -testPlatform StandaloneOSX \
    -testResults TestResults-Mac.xml

# Performance benchmarks
./Builds/Mac/SwarmWorld.app/Contents/MacOS/SwarmWorld \
    -batchmode \
    -benchmark \
    -logFile performance-mac.log

# Memory leak detection
leaks ./Builds/Mac/SwarmWorld.app/Contents/MacOS/SwarmWorld > memory-leaks.log

# Instruments profiling
instruments -t "Time Profiler" \
    ./Builds/Mac/SwarmWorld.app/Contents/MacOS/SwarmWorld \
    -o profiling-results.trace
```

## 📈 Mac-Specific Optimization Strategies

### 1. Unified Memory Architecture (Apple Silicon)
- Leverage shared memory between CPU and GPU
- Reduce data copying between memory pools
- Use Metal's unified memory features

### 2. Thermal Management
- Monitor thermal state and adjust quality
- Implement dynamic LOD based on temperature
- Reduce swarm complexity under thermal pressure

### 3. Display Optimization
- Support ProMotion displays (120Hz)
- Handle multiple displays gracefully
- Optimize for Retina display densities

### 4. Battery Efficiency
- Implement power-aware swarm algorithms
- Reduce computation when on battery
- Use macOS Low Power Mode detection

## 🚀 Deployment Checklist for Mac

- [ ] Universal binary (Intel + Apple Silicon)
- [ ] Code signing with Developer ID
- [ ] Notarization for Gatekeeper
- [ ] Sandbox compliance testing
- [ ] Metal shader optimization
- [ ] Thermal throttling handling
- [ ] Memory leak testing with Instruments
- [ ] Performance profiling on target hardware
- [ ] Multi-display support verification
- [ ] Accessibility compliance (VoiceOver, etc.)

## 🔗 Useful Mac Development Resources

- **Metal Performance Shaders**: For GPU-accelerated swarm calculations
- **Instruments**: Apple's profiling tool for performance analysis
- **Xcode Command Line Tools**: Essential for Mac development
- **Unity Cloud Build**: Automated Mac builds in the cloud
- **TestFlight**: Beta testing for Mac App Store distribution