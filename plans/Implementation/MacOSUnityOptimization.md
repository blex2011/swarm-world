# macOS Unity Optimization for Swarm World

## Platform Target Confirmation

**Primary Development Environment**: Unity on macOS
- **Architecture Support**: Universal Binary (Intel + Apple Silicon)
- **Target Platforms**: macOS Standalone, iOS (via Xcode)
- **Graphics API**: Metal (primary), OpenGL Core (fallback)
- **Unity Version**: 2022.3 LTS or later recommended

## macOS-Specific Optimizations

### 1. Apple Silicon Performance

```csharp
public class AppleSiliconOptimizer : MonoBehaviour
{
    [Header("Apple Silicon Optimizations")]
    public bool EnableUnifiedMemoryOptimization = true;
    public bool UseMetalGPUAcceleration = true;
    public int AppleSiliconBatchMultiplier = 2;
    
    void Start()
    {
        if (SystemInfo.processorType.Contains("Apple"))
        {
            OptimizeForAppleSilicon();
        }
    }
    
    void OptimizeForAppleSilicon()
    {
        // Increase batch sizes for Apple Silicon efficiency
        SwarmManager.DefaultBatchSize *= AppleSiliconBatchMultiplier;
        
        // Enable Metal compute shaders
        if (UseMetalGPUAcceleration && SystemInfo.supportsComputeShaders)
        {
            EnableMetalComputeAcceleration();
        }
        
        // Optimize for unified memory architecture
        if (EnableUnifiedMemoryOptimization)
        {
            ConfigureUnifiedMemoryPools();
        }
    }
}
```

### 2. Metal Graphics Integration

```csharp
public class MetalSwarmRenderer : MonoBehaviour
{
    [Header("Metal Optimization")]
    public ComputeShader MetalSwarmCompute;
    public Material MetalInstancedMaterial;
    
    private ComputeBuffer positionBuffer;
    private ComputeBuffer rotationBuffer;
    private Matrix4x4[] matrices;
    
    void InitializeMetalRendering()
    {
        // Use Metal-optimized compute shaders
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
        {
            SetupMetalComputeBuffers();
            EnableMetalInstancing();
        }
    }
    
    void SetupMetalComputeBuffers()
    {
        // Metal prefers specific buffer alignments
        int agentCount = GetComponent<SwarmManager>().AgentCount;
        positionBuffer = new ComputeBuffer(agentCount, 16); // 4 floats aligned
        rotationBuffer = new ComputeBuffer(agentCount, 16); // Quaternion aligned
        
        MetalSwarmCompute.SetBuffer(0, "PositionBuffer", positionBuffer);
        MetalSwarmCompute.SetBuffer(0, "RotationBuffer", rotationBuffer);
    }
}
```

### 3. macOS Memory Pressure Handling

```csharp
public class MacOSMemoryManager : MonoBehaviour
{
    [Header("macOS Memory Configuration")]
    public bool EnableMemoryPressureCallbacks = true;
    public float MemoryPressureReductionFactor = 0.7f;
    
    private int originalAgentCount;
    private bool memoryPressureActive = false;
    
    void Start()
    {
        if (Application.platform == RuntimePlatform.OSXPlayer || 
            Application.platform == RuntimePlatform.OSXEditor)
        {
            InitializeMacOSMemoryHandling();
        }
    }
    
    void InitializeMacOSMemoryHandling()
    {
        originalAgentCount = GetComponent<SwarmManager>().AgentCount;
        
        // Monitor memory usage
        InvokeRepeating(nameof(CheckMemoryPressure), 5f, 5f);
    }
    
    void CheckMemoryPressure()
    {
        long totalMemory = System.GC.GetTotalMemory(false);
        long maxMemory = 16L * 1024 * 1024 * 1024; // 16GB typical Mac limit
        
        float memoryUsageRatio = (float)totalMemory / maxMemory;
        
        if (memoryUsageRatio > 0.8f && !memoryPressureActive)
        {
            HandleMemoryPressure();
        }
        else if (memoryUsageRatio < 0.6f && memoryPressureActive)
        {
            RestoreFromMemoryPressure();
        }
    }
    
    void HandleMemoryPressure()
    {
        memoryPressureActive = true;
        
        var swarmManager = GetComponent<SwarmManager>();
        int reducedCount = Mathf.RoundToInt(originalAgentCount * MemoryPressureReductionFactor);
        swarmManager.SetAgentCount(reducedCount);
        
        Debug.Log($"[macOS] Memory pressure detected. Reduced agents to {reducedCount}");
    }
}
```

### 4. Xcode Integration for iOS Builds

```csharp
#if UNITY_IOS
using UnityEditor;
using UnityEditor.iOS.Xcode;

public class XcodeSwarmPostProcessor : IPostprocessBuildWithReport
{
    public int callbackOrder => 1;
    
    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.iOS)
        {
            ConfigureXcodeProject(report.summary.outputPath);
        }
    }
    
    void ConfigureXcodeProject(string pathToBuiltProject)
    {
        string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));
        
        string target = project.GetUnityMainTargetGuid();
        
        // Add Metal framework
        project.AddFrameworkToProject(target, "Metal.framework", false);
        project.AddFrameworkToProject(target, "MetalKit.framework", false);
        
        // Enable Metal validation for debugging
        project.AddBuildProperty(target, "MTL_ENABLE_DEBUG_INFO", "YES");
        
        // Optimize for Metal performance
        project.AddBuildProperty(target, "MTL_FAST_MATH", "YES");
        
        File.WriteAllText(projectPath, project.WriteToString());
    }
}
#endif
```

### 5. Performance Monitoring for Mac

```csharp
public class MacOSPerformanceMonitor : MonoBehaviour
{
    [Header("macOS Performance Tracking")]
    public bool EnableThermalStateMonitoring = true;
    public bool AutoAdjustQuality = true;
    
    private float lastThermalCheck = 0f;
    private int currentQualityLevel = 3; // 0-3 scale
    
    void Update()
    {
        if (EnableThermalStateMonitoring && Time.time - lastThermalCheck > 5f)
        {
            CheckThermalState();
            lastThermalCheck = Time.time;
        }
    }
    
    void CheckThermalState()
    {
        // Monitor CPU temperature and performance
        float frameTime = Time.unscaledDeltaTime;
        float targetFrameTime = 1f / 60f;
        
        if (frameTime > targetFrameTime * 1.5f)
        {
            // Performance degradation detected
            if (AutoAdjustQuality && currentQualityLevel > 0)
            {
                ReduceQualityLevel();
            }
        }
        else if (frameTime < targetFrameTime * 0.8f && currentQualityLevel < 3)
        {
            // Performance headroom available
            IncreaseQualityLevel();
        }
    }
    
    void ReduceQualityLevel()
    {
        currentQualityLevel--;
        ApplyQualitySettings(currentQualityLevel);
        Debug.Log($"[macOS] Reduced quality to level {currentQualityLevel} due to thermal constraints");
    }
    
    void IncreaseQualityLevel()
    {
        currentQualityLevel++;
        ApplyQualitySettings(currentQualityLevel);
        Debug.Log($"[macOS] Increased quality to level {currentQualityLevel}");
    }
    
    void ApplyQualitySettings(int level)
    {
        var swarmManager = GetComponent<SwarmManager>();
        
        switch (level)
        {
            case 0: // Minimum
                swarmManager.SetAgentCount(500);
                swarmManager.SetUpdateRate(20);
                break;
            case 1: // Low
                swarmManager.SetAgentCount(1000);
                swarmManager.SetUpdateRate(30);
                break;
            case 2: // Medium
                swarmManager.SetAgentCount(2000);
                swarmManager.SetUpdateRate(45);
                break;
            case 3: // High
                swarmManager.SetAgentCount(3000);
                swarmManager.SetUpdateRate(60);
                break;
        }
    }
}
```

## Deployment Configuration

### 1. Mac App Store Preparation

```xml
<!-- macOS App Store entitlements -->
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.app-sandbox</key>
    <true/>
    <key>com.apple.security.network.client</key>
    <true/>
    <key>com.apple.security.files.user-selected.read-write</key>
    <true/>
    <key>com.apple.security.device.metal</key>
    <true/>
</dict>
</plist>
```

### 2. Build Pipeline for Mac

```csharp
public class MacOSBuildPipeline
{
    [MenuItem("Swarm World/Build macOS Universal")]
    public static void BuildMacOSUniversal()
    {
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray(),
            locationPathName = "Builds/macOS/SwarmWorld.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        
        // Configure for Universal Binary
        PlayerSettings.SetArchitecture(BuildTargetGroup.Standalone, 2); // Universal
        PlayerSettings.macOS.targetOSVersion = MacOSTargetOSVersion.OSX_10_15;
        
        // Enable Metal API
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX, new[] { 
            GraphicsDeviceType.Metal, 
            GraphicsDeviceType.OpenGLCore 
        });
        
        BuildPipeline.BuildPlayer(buildOptions);
    }
}
```

### 3. Performance Benchmarks for Mac

```csharp
public class MacOSBenchmarks : MonoBehaviour
{
    [Header("Mac Performance Targets")]
    public int TargetAgentCountIntel = 2000;
    public int TargetAgentCountAppleSilicon = 3500;
    public float TargetFrameRateStandalone = 60f;
    public float TargetFrameRateEditor = 45f;
    
    void Start()
    {
        RunMacOSBenchmark();
    }
    
    void RunMacOSBenchmark()
    {
        int targetAgents = DetermineTargetAgentCount();
        
        var swarmManager = GetComponent<SwarmManager>();
        swarmManager.SetAgentCount(targetAgents);
        
        StartCoroutine(BenchmarkPerformance());
    }
    
    int DetermineTargetAgentCount()
    {
        if (SystemInfo.processorType.Contains("Apple"))
        {
            return TargetAgentCountAppleSilicon;
        }
        else
        {
            return TargetAgentCountIntel;
        }
    }
    
    IEnumerator BenchmarkPerformance()
    {
        yield return new WaitForSeconds(5f); // Warm-up
        
        float totalFrameTime = 0f;
        int frameCount = 0;
        
        for (int i = 0; i < 300; i++) // 5 seconds at 60fps
        {
            totalFrameTime += Time.unscaledDeltaTime;
            frameCount++;
            yield return null;
        }
        
        float averageFrameRate = frameCount / totalFrameTime;
        float targetFrameRate = Application.isEditor ? TargetFrameRateEditor : TargetFrameRateStandalone;
        
        Debug.Log($"[macOS Benchmark] Average FPS: {averageFrameRate:F1}, Target: {targetFrameRate:F1}");
        
        if (averageFrameRate < targetFrameRate * 0.9f)
        {
            Debug.LogWarning($"[macOS] Performance below target. Consider reducing agent count or quality settings.");
        }
    }
}
```

## Implementation Checklist

### ✅ Completed
- [x] .gitignore updated with Mac-specific Unity patterns
- [x] Platform detection and optimization framework
- [x] Metal graphics integration guidelines
- [x] Memory pressure handling for macOS
- [x] Performance monitoring with thermal awareness

### 📋 Recommended Next Steps
- [ ] Implement Metal compute shader variants
- [ ] Add Xcode project post-processing automation
- [ ] Create Mac-specific performance presets
- [ ] Implement App Store compliance checks
- [ ] Add comprehensive Mac testing suite

## Notes
- **Unified Memory**: Apple Silicon Macs benefit from optimized memory access patterns
- **Metal Performance**: Up to 2x performance improvement over OpenGL on Mac
- **Thermal Management**: Important for sustained performance on MacBook models
- **App Store**: Sandbox compliance required for Mac App Store distribution
- **Universal Binary**: Supports both Intel and Apple Silicon Macs seamlessly

This optimization guide ensures Swarm World performs optimally on macOS while maintaining compatibility with the broader Unity ecosystem.