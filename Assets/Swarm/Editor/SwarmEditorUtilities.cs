using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SwarmWorld.Editor
{
    /// <summary>
    /// Collection of editor utilities for swarm development and debugging
    /// </summary>
    public static class SwarmEditorUtilities
    {
        // Constants
        private const string SWARM_PREFS_PREFIX = "SwarmWorld_";
        private const string DEFAULT_EXPORT_PATH = "Assets/SwarmData/";
        
        /// <summary>
        /// Gets or sets editor preferences with automatic prefixing
        /// </summary>
        public static class Preferences
        {
            public static bool GetBool(string key, bool defaultValue = false)
            {
                return EditorPrefs.GetBool(SWARM_PREFS_PREFIX + key, defaultValue);
            }
            
            public static void SetBool(string key, bool value)
            {
                EditorPrefs.SetBool(SWARM_PREFS_PREFIX + key, value);
            }
            
            public static float GetFloat(string key, float defaultValue = 0f)
            {
                return EditorPrefs.GetFloat(SWARM_PREFS_PREFIX + key, defaultValue);
            }
            
            public static void SetFloat(string key, float value)
            {
                EditorPrefs.SetFloat(SWARM_PREFS_PREFIX + key, value);
            }
            
            public static string GetString(string key, string defaultValue = "")
            {
                return EditorPrefs.GetString(SWARM_PREFS_PREFIX + key, defaultValue);
            }
            
            public static void SetString(string key, string value)
            {
                EditorPrefs.SetString(SWARM_PREFS_PREFIX + key, value);
            }
        }
        
        /// <summary>
        /// Utility for creating and managing swarm scene setups
        /// </summary>
        public static class SceneSetup
        {
            public static void CreateCompleteSwarmScene()
            {
                // Create environment
                CreateEnvironment();
                
                // Create lighting
                SetupLighting();
                
                // Create camera setup
                SetupCameras();
                
                // Create swarm
                CreateDefaultSwarm();
                
                Debug.Log("Complete swarm scene created!");
            }
            
            private static void CreateEnvironment()
            {
                // Ground plane
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.localScale = Vector3.one * 20f;
                
                // Boundaries (invisible)
                GameObject boundaries = new GameObject("Boundaries");
                for (int i = 0; i < 4; i++)
                {
                    GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = $"Wall_{i}";
                    wall.transform.parent = boundaries.transform;
                    wall.transform.localScale = new Vector3(1f, 10f, 40f);
                    wall.GetComponent<Renderer>().enabled = false; // Invisible but collideable
                    
                    // Position walls around perimeter
                    float angle = i * 90f;
                    wall.transform.position = Quaternion.Euler(0, angle, 0) * Vector3.forward * 20f;
                    wall.transform.rotation = Quaternion.Euler(0, angle, 0);
                }
            }
            
            private static void SetupLighting()
            {
                // Ensure we have a directional light
                Light mainLight = Object.FindObjectOfType<Light>();
                if (mainLight == null)
                {
                    GameObject lightGO = new GameObject("Directional Light");
                    mainLight = lightGO.AddComponent<Light>();
                }
                
                mainLight.type = LightType.Directional;
                mainLight.shadows = LightShadows.Soft;
                mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                
                // Set ambient lighting
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = Color.cyan * 0.3f;
                RenderSettings.ambientEquatorColor = Color.white * 0.2f;
                RenderSettings.ambientGroundColor = Color.gray * 0.1f;
            }
            
            private static void SetupCameras()
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    GameObject cameraGO = new GameObject("Main Camera");
                    mainCamera = cameraGO.AddComponent<Camera>();
                    cameraGO.tag = "MainCamera";
                }
                
                // Position camera for good swarm viewing
                mainCamera.transform.position = new Vector3(0, 15f, -20f);
                mainCamera.transform.rotation = Quaternion.Euler(20f, 0f, 0f);
                
                // Add camera controller if it doesn't exist
                if (mainCamera.GetComponent<SwarmCameraController>() == null)
                {
                    mainCamera.gameObject.AddComponent<SwarmCameraController>();
                }
            }
            
            private static void CreateDefaultSwarm()
            {
                GameObject swarmGO = new GameObject("Default Swarm");
                SwarmManager manager = swarmGO.AddComponent<SwarmManager>();
                
                // Create default agent prefab if none exists
                if (manager.AgentPrefab == null)
                {
                    manager.AgentPrefab = CreateDefaultAgentPrefab();
                }
                
                // Configure with reasonable defaults
                manager.MaxAgents = 100;
                manager.SpawnRadius = 10f;
                manager.AutoStartOnPlay = true;
            }
            
            private static GameObject CreateDefaultAgentPrefab()
            {
                // Create agent prefab
                GameObject agentGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                agentGO.name = "Default Swarm Agent";
                
                // Configure components
                agentGO.transform.localScale = Vector3.one * 0.5f;
                
                var rb = agentGO.GetComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                
                var collider = agentGO.GetComponent<SphereCollider>();
                collider.radius = 0.4f;
                
                // Add swarm agent component
                agentGO.AddComponent<BoidAgent>();
                
                // Add trail renderer for visualization
                var trail = agentGO.AddComponent<TrailRenderer>();
                trail.time = 1f;
                trail.startWidth = 0.1f;
                trail.endWidth = 0.01f;
                trail.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
                
                // Save as prefab
                EnsureDirectoryExists("Assets/Swarm/Prefabs");
                string prefabPath = "Assets/Swarm/Prefabs/DefaultSwarmAgent.prefab";
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(agentGO, prefabPath);
                
                Object.DestroyImmediate(agentGO);
                return prefab;
            }
        }
        
        /// <summary>
        /// Performance analysis and optimization utilities
        /// </summary>
        public static class PerformanceAnalyzer
        {
            public static void RunAnalysis()
            {
                var swarms = Object.FindObjectsOfType<SwarmManager>();
                var analysis = new PerformanceAnalysisReport();
                
                foreach (var swarm in swarms)
                {
                    AnalyzeSwarm(swarm, analysis);
                }
                
                DisplayAnalysisResults(analysis);
            }
            
            private static void AnalyzeSwarm(SwarmManager swarm, PerformanceAnalysisReport analysis)
            {
                if (swarm.Metrics == null) return;
                
                var metrics = swarm.Metrics;
                
                // Analyze update time
                if (metrics.UpdateTime > 16.67f) // Target 60 FPS
                {
                    analysis.Issues.Add($"{swarm.name}: High update time ({metrics.UpdateTime:F2}ms)");
                    analysis.Suggestions.Add($"{swarm.name}: Consider reducing agent count or enabling spatial partitioning");
                }
                
                // Analyze neighbor density
                if (metrics.AverageNeighbors > 20)
                {
                    analysis.Issues.Add($"{swarm.name}: High neighbor density ({metrics.AverageNeighbors:F1})");
                    analysis.Suggestions.Add($"{swarm.name}: Reduce perception radius or implement LOD system");
                }
                
                // Analyze memory usage
                float memoryPerAgent = metrics.TotalAgents > 0 ? (GC.GetTotalMemory(false) / 1024f / 1024f) / metrics.TotalAgents : 0f;
                if (memoryPerAgent > 0.1f) // 100KB per agent
                {
                    analysis.Issues.Add($"{swarm.name}: High memory per agent ({memoryPerAgent:F2}MB)");
                    analysis.Suggestions.Add($"{swarm.name}: Check for memory leaks or optimize data structures");
                }
                
                analysis.TotalAgents += metrics.TotalAgents;
                analysis.TotalUpdateTime += metrics.UpdateTime;
            }
            
            private static void DisplayAnalysisResults(PerformanceAnalysisReport analysis)
            {
                string report = "Performance Analysis Report\n" +
                               "===========================\n\n" +
                               $"Total Agents: {analysis.TotalAgents}\n" +
                               $"Total Update Time: {analysis.TotalUpdateTime:F2}ms\n\n";
                
                if (analysis.Issues.Count > 0)
                {
                    report += "Issues Found:\n";
                    foreach (var issue in analysis.Issues)
                    {
                        report += $"• {issue}\n";
                    }
                    report += "\nSuggestions:\n";
                    foreach (var suggestion in analysis.Suggestions)
                    {
                        report += $"• {suggestion}\n";
                    }
                }
                else
                {
                    report += "No performance issues detected!";
                }
                
                EditorUtility.DisplayDialog("Performance Analysis", report, "OK");
            }
        }
        
        /// <summary>
        /// Debug data export utilities
        /// </summary>
        public static class SwarmDebugExporter
        {
            public static void ExportToFile(string path)
            {
                var debugData = CollectDebugData();
                string json = JsonUtility.ToJson(debugData, true);
                File.WriteAllText(path, json);
            }
            
            private static SwarmDebugData CollectDebugData()
            {
                var data = new SwarmDebugData();
                data.timestamp = System.DateTime.Now.ToString();
                data.unityVersion = Application.unityVersion;
                data.isPlaying = Application.isPlaying;
                
                var swarms = Object.FindObjectsOfType<SwarmManager>();
                data.swarms = new List<SwarmDebugInfo>();
                
                foreach (var swarm in swarms)
                {
                    var swarmInfo = new SwarmDebugInfo
                    {
                        name = swarm.name,
                        agentCount = swarm.AgentCount,
                        maxAgents = swarm.MaxAgents,
                        isActive = swarm.isActiveAndEnabled,
                        position = swarm.transform.position,
                        separationWeight = swarm.SeparationWeight,
                        alignmentWeight = swarm.AlignmentWeight,
                        cohesionWeight = swarm.CohesionWeight,
                        maxSpeed = swarm.MaxSpeed,
                        perceptionRadius = swarm.PerceptionRadius
                    };
                    
                    if (swarm.Metrics != null)
                    {
                        swarmInfo.metrics = new SwarmMetricsDebug
                        {
                            updateTime = swarm.Metrics.UpdateTime,
                            physicsTime = swarm.Metrics.PhysicsTime,
                            renderTime = swarm.Metrics.RenderTime,
                            averageNeighbors = swarm.Metrics.AverageNeighbors
                        };
                    }
                    
                    data.swarms.Add(swarmInfo);
                }
                
                return data;
            }
        }
        
        /// <summary>
        /// Utility functions for common editor operations
        /// </summary>
        public static class Utilities
        {
            public static void EnsureDirectoryExists(string path)
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string[] folders = path.Split('/');
                    string currentPath = folders[0];
                    
                    for (int i = 1; i < folders.Length; i++)
                    {
                        string newPath = currentPath + "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = newPath;
                    }
                }
            }
            
            public static void SaveSwarmConfiguration(SwarmManager swarm, string path)
            {
                var config = new SwarmConfiguration
                {
                    name = swarm.name,
                    maxAgents = swarm.MaxAgents,
                    spawnRadius = swarm.SpawnRadius,
                    separationWeight = swarm.SeparationWeight,
                    alignmentWeight = swarm.AlignmentWeight,
                    cohesionWeight = swarm.CohesionWeight,
                    maxSpeed = swarm.MaxSpeed,
                    perceptionRadius = swarm.PerceptionRadius,
                    useSpatialPartitioning = swarm.UseSpatialPartitioning,
                    useJobSystem = swarm.UseJobSystem,
                    useLOD = swarm.UseLOD
                };
                
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(path, json);
            }
            
            public static void LoadSwarmConfiguration(SwarmManager swarm, string path)
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var config = JsonUtility.FromJson<SwarmConfiguration>(json);
                    
                    swarm.MaxAgents = config.maxAgents;
                    swarm.SpawnRadius = config.spawnRadius;
                    swarm.SeparationWeight = config.separationWeight;
                    swarm.AlignmentWeight = config.alignmentWeight;
                    swarm.CohesionWeight = config.cohesionWeight;
                    swarm.MaxSpeed = config.maxSpeed;
                    swarm.PerceptionRadius = config.perceptionRadius;
                    swarm.UseSpatialPartitioning = config.useSpatialPartitioning;
                    swarm.UseJobSystem = config.useJobSystem;
                    swarm.UseLOD = config.useLOD;
                    
                    EditorUtility.SetDirty(swarm);
                }
            }
            
            public static Color GetSwarmTypeColor(SwarmType type)
            {
                switch (type)
                {
                    case SwarmType.Boid: return Color.cyan;
                    case SwarmType.Combat: return Color.red;
                    case SwarmType.Formation: return Color.blue;
                    case SwarmType.Exploration: return Color.green;
                    default: return Color.white;
                }
            }
            
            public static Texture2D CreateColorTexture(Color color, int width = 1, int height = 1)
            {
                var texture = new Texture2D(width, height);
                var pixels = new Color[width * height];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }
                texture.SetPixels(pixels);
                texture.Apply();
                return texture;
            }
        }
        
        // Make sure we have the EnsureDirectoryExists method available at the class level
        public static void EnsureDirectoryExists(string path)
        {
            Utilities.EnsureDirectoryExists(path);
        }
    }
    
    /// <summary>
    /// Report generator for performance analysis
    /// </summary>
    public static class PerformanceReportGenerator
    {
        public static void GenerateReport(string path)
        {
            var swarms = Object.FindObjectsOfType<SwarmManager>();
            var report = new System.Text.StringBuilder();
            
            // Header
            report.AppendLine("SWARM WORLD PERFORMANCE REPORT");
            report.AppendLine("==============================");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine($"Unity Version: {Application.unityVersion}");
            report.AppendLine($"Platform: {Application.platform}");
            report.AppendLine();
            
            // System info
            report.AppendLine("SYSTEM INFORMATION");
            report.AppendLine("------------------");
            report.AppendLine($"Processor: {SystemInfo.processorType}");
            report.AppendLine($"Memory: {SystemInfo.systemMemorySize} MB");
            report.AppendLine($"Graphics: {SystemInfo.graphicsDeviceName}");
            report.AppendLine($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
            report.AppendLine();
            
            // Swarm analysis
            report.AppendLine("SWARM ANALYSIS");
            report.AppendLine("--------------");
            
            if (swarms.Length == 0)
            {
                report.AppendLine("No swarms found in scene.");
            }
            else
            {
                foreach (var swarm in swarms)
                {
                    AnalyzeSwarmForReport(swarm, report);
                }
            }
            
            // Write to file
            File.WriteAllText(path, report.ToString());
        }
        
        private static void AnalyzeSwarmForReport(SwarmManager swarm, System.Text.StringBuilder report)
        {
            report.AppendLine($"Swarm: {swarm.name}");
            report.AppendLine($"  Type: {swarm.SwarmType}");
            report.AppendLine($"  Agent Count: {swarm.AgentCount}/{swarm.MaxAgents}");
            report.AppendLine($"  Status: {(swarm.isActiveAndEnabled ? "Active" : "Inactive")}");
            
            if (swarm.Metrics != null)
            {
                var metrics = swarm.Metrics;
                report.AppendLine($"  Performance:");
                report.AppendLine($"    Update Time: {metrics.UpdateTime:F2}ms");
                report.AppendLine($"    Physics Time: {metrics.PhysicsTime:F2}ms");
                report.AppendLine($"    Render Time: {metrics.RenderTime:F2}ms");
                report.AppendLine($"    Average Neighbors: {metrics.AverageNeighbors:F1}");
            }
            
            report.AppendLine($"  Configuration:");
            report.AppendLine($"    Max Speed: {swarm.MaxSpeed}");
            report.AppendLine($"    Perception Radius: {swarm.PerceptionRadius}");
            report.AppendLine($"    Spatial Partitioning: {(swarm.UseSpatialPartitioning ? "Enabled" : "Disabled")}");
            report.AppendLine($"    Job System: {(swarm.UseJobSystem ? "Enabled" : "Disabled")}");
            report.AppendLine($"    LOD System: {(swarm.UseLOD ? "Enabled" : "Disabled")}");
            report.AppendLine();
        }
    }
    
    // Data structures for serialization
    [System.Serializable]
    public class PerformanceAnalysisReport
    {
        public List<string> Issues = new List<string>();
        public List<string> Suggestions = new List<string>();
        public int TotalAgents = 0;
        public float TotalUpdateTime = 0f;
    }
    
    [System.Serializable]
    public class SwarmDebugData
    {
        public string timestamp;
        public string unityVersion;
        public bool isPlaying;
        public List<SwarmDebugInfo> swarms;
    }
    
    [System.Serializable]
    public class SwarmDebugInfo
    {
        public string name;
        public int agentCount;
        public int maxAgents;
        public bool isActive;
        public Vector3 position;
        public float separationWeight;
        public float alignmentWeight;
        public float cohesionWeight;
        public float maxSpeed;
        public float perceptionRadius;
        public SwarmMetricsDebug metrics;
    }
    
    [System.Serializable]
    public class SwarmMetricsDebug
    {
        public float updateTime;
        public float physicsTime;
        public float renderTime;
        public float averageNeighbors;
    }
    
    [System.Serializable]
    public class SwarmConfiguration
    {
        public string name;
        public int maxAgents;
        public float spawnRadius;
        public float separationWeight;
        public float alignmentWeight;
        public float cohesionWeight;
        public float maxSpeed;
        public float perceptionRadius;
        public bool useSpatialPartitioning;
        public bool useJobSystem;
        public bool useLOD;
    }
}