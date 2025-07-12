using UnityEngine;
using UnityEditor;
using System.IO;

namespace SwarmWorld.Editor
{
    /// <summary>
    /// Unity Editor menu integration for Swarm World tools and utilities
    /// </summary>
    public static class SwarmMenuItems
    {
        private const string MENU_ROOT = "Swarm/";
        private const string TOOLS_MENU = MENU_ROOT + "Tools/";
        private const string CREATE_MENU = MENU_ROOT + "Create/";
        private const string DEBUG_MENU = MENU_ROOT + "Debug/";
        
        // Main menu items
        [MenuItem(MENU_ROOT + "Swarm Control Panel", false, 1)]
        public static void OpenControlPanel()
        {
            SwarmWindow.ShowWindow();
        }
        
        [MenuItem(MENU_ROOT + "Performance Dashboard", false, 2)]
        public static void OpenPerformanceDashboard()
        {
            PerformanceDashboard.ShowWindow();
        }
        
        [MenuItem(MENU_ROOT + "Agent Inspector", false, 3)]
        public static void OpenAgentInspector()
        {
            // Focus on selected agent if it has SwarmAgent component
            if (Selection.activeGameObject != null)
            {
                var agent = Selection.activeGameObject.GetComponent<SwarmAgent>();
                if (agent != null)
                {
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = agent;
                    return;
                }
            }
            
            // Otherwise, find first SwarmAgent in scene
            var firstAgent = Object.FindObjectOfType<SwarmAgent>();
            if (firstAgent != null)
            {
                Selection.activeGameObject = firstAgent.gameObject;
                EditorGUIUtility.PingObject(firstAgent);
            }
            else
            {
                EditorUtility.DisplayDialog("No Swarm Agents", 
                    "No SwarmAgent components found in the current scene. Create a swarm first.", "OK");
            }
        }
        
        // Creation tools
        [MenuItem(CREATE_MENU + "Basic Swarm", false, 21)]
        public static void CreateBasicSwarm()
        {
            CreateSwarmFromTemplate(SwarmTemplate.Basic);
        }
        
        [MenuItem(CREATE_MENU + "Combat Swarm", false, 22)]
        public static void CreateCombatSwarm()
        {
            CreateSwarmFromTemplate(SwarmTemplate.Combat);
        }
        
        [MenuItem(CREATE_MENU + "Formation Swarm", false, 23)]
        public static void CreateFormationSwarm()
        {
            CreateSwarmFromTemplate(SwarmTemplate.Formation);
        }
        
        [MenuItem(CREATE_MENU + "Custom Swarm", false, 24)]
        public static void CreateCustomSwarm()
        {
            CreateSwarmDialog.ShowDialog();
        }
        
        [MenuItem(CREATE_MENU + "Swarm Agent Prefab", false, 25)]
        public static void CreateAgentPrefab()
        {
            CreateAgentPrefabWizard.ShowWizard();
        }
        
        // Tools
        [MenuItem(TOOLS_MENU + "Optimize All Swarms", false, 41)]
        public static void OptimizeAllSwarms()
        {
            var swarms = Object.FindObjectsOfType<SwarmManager>();
            
            if (swarms.Length == 0)
            {
                EditorUtility.DisplayDialog("No Swarms", "No SwarmManager components found in the scene.", "OK");
                return;
            }
            
            int optimizedCount = 0;
            
            foreach (var swarm in swarms)
            {
                if (swarm.AutoOptimize())
                {
                    optimizedCount++;
                    EditorUtility.SetDirty(swarm);
                }
            }
            
            string message = optimizedCount > 0 ? 
                $"Optimized {optimizedCount} out of {swarms.Length} swarms." :
                "All swarms are already optimized.";
                
            EditorUtility.DisplayDialog("Optimization Complete", message, "OK");
        }
        
        [MenuItem(TOOLS_MENU + "Validate Swarm Setup", false, 42)]
        public static void ValidateSwarmSetup()
        {
            SwarmValidator.ValidateAllSwarms();
        }
        
        [MenuItem(TOOLS_MENU + "Performance Analysis", false, 43)]
        public static void RunPerformanceAnalysis()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Performance Analysis", 
                    "Performance analysis requires Play Mode. Please enter Play Mode and try again.", "OK");
                return;
            }
            
            PerformanceAnalyzer.RunAnalysis();
        }
        
        [MenuItem(TOOLS_MENU + "Generate Performance Report", false, 44)]
        public static void GeneratePerformanceReport()
        {
            string path = EditorUtility.SaveFilePanel("Save Performance Report", "", "swarm_performance_report.pdf", "pdf");
            if (!string.IsNullOrEmpty(path))
            {
                PerformanceReportGenerator.GenerateReport(path);
                EditorUtility.DisplayDialog("Report Generated", $"Performance report saved to:\n{path}", "OK");
            }
        }
        
        // Debug tools
        [MenuItem(DEBUG_MENU + "Toggle Debug Visualization", false, 61)]
        public static void ToggleDebugVisualization()
        {
            var swarms = Object.FindObjectsOfType<SwarmManager>();
            bool newState = !IsAnySwarmShowingDebug(swarms);
            
            foreach (var swarm in swarms)
            {
                swarm.ShowDebugInfo = newState;
                EditorUtility.SetDirty(swarm);
            }
            
            SceneView.RepaintAll();
        }
        
        [MenuItem(DEBUG_MENU + "Clear All Swarm Data", false, 62)]
        public static void ClearAllSwarmData()
        {
            if (EditorUtility.DisplayDialog("Clear Swarm Data", 
                "This will reset all swarm agents and clear performance data. Continue?", "Yes", "Cancel"))
            {
                var swarms = Object.FindObjectsOfType<SwarmManager>();
                
                foreach (var swarm in swarms)
                {
                    swarm.ResetSwarm();
                    swarm.ClearPerformanceData();
                }
                
                Debug.Log("All swarm data cleared.");
            }
        }
        
        [MenuItem(DEBUG_MENU + "Export Debug Information", false, 63)]
        public static void ExportDebugInformation()
        {
            string path = EditorUtility.SaveFilePanel("Export Debug Info", "", "swarm_debug_info.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                SwarmDebugExporter.ExportToFile(path);
                EditorUtility.DisplayDialog("Export Complete", $"Debug information exported to:\n{path}", "OK");
            }
        }
        
        [MenuItem(DEBUG_MENU + "Reset All Agents", false, 64)]
        public static void ResetAllAgents()
        {
            var agents = Object.FindObjectsOfType<SwarmAgent>();
            
            foreach (var agent in agents)
            {
                agent.ResetAgent();
                EditorUtility.SetDirty(agent);
            }
            
            Debug.Log($"Reset {agents.Length} agents.");
        }
        
        // Validation methods for menu items
        [MenuItem(CREATE_MENU + "Basic Swarm", true)]
        [MenuItem(CREATE_MENU + "Combat Swarm", true)]
        [MenuItem(CREATE_MENU + "Formation Swarm", true)]
        [MenuItem(CREATE_MENU + "Custom Swarm", true)]
        public static bool ValidateCreateSwarm()
        {
            return !Application.isPlaying; // Only allow creation when not playing
        }
        
        [MenuItem(TOOLS_MENU + "Optimize All Swarms", true)]
        [MenuItem(TOOLS_MENU + "Validate Swarm Setup", true)]
        public static bool ValidateSwarmTools()
        {
            return Object.FindObjectOfType<SwarmManager>() != null;
        }
        
        [MenuItem(TOOLS_MENU + "Performance Analysis", true)]
        [MenuItem(TOOLS_MENU + "Generate Performance Report", true)]
        public static bool ValidatePerformanceTools()
        {
            return Application.isPlaying && Object.FindObjectOfType<SwarmManager>() != null;
        }
        
        [MenuItem(DEBUG_MENU + "Toggle Debug Visualization", true)]
        [MenuItem(DEBUG_MENU + "Clear All Swarm Data", true)]
        [MenuItem(DEBUG_MENU + "Reset All Agents", true)]
        public static bool ValidateDebugTools()
        {
            return Object.FindObjectOfType<SwarmManager>() != null;
        }
        
        // Helper methods
        private static void CreateSwarmFromTemplate(SwarmTemplate template)
        {
            GameObject swarmGO = new GameObject($"{template}Swarm");
            SwarmManager manager = swarmGO.AddComponent<SwarmManager>();
            
            // Configure based on template
            switch (template)
            {
                case SwarmTemplate.Basic:
                    ConfigureBasicSwarm(manager);
                    break;
                case SwarmTemplate.Combat:
                    ConfigureCombatSwarm(manager);
                    break;
                case SwarmTemplate.Formation:
                    ConfigureFormationSwarm(manager);
                    break;
            }
            
            // Position in scene
            swarmGO.transform.position = GetSpawnPosition();
            
            // Select and focus
            Selection.activeGameObject = swarmGO;
            EditorGUIUtility.PingObject(swarmGO);
            
            // Mark as dirty for saving
            EditorUtility.SetDirty(swarmGO);
            
            Debug.Log($"Created {template} swarm: {swarmGO.name}");
        }
        
        private static void ConfigureBasicSwarm(SwarmManager manager)
        {
            manager.MaxAgents = 100;
            manager.SpawnRadius = 10f;
            manager.SeparationWeight = 1.5f;
            manager.AlignmentWeight = 1.0f;
            manager.CohesionWeight = 1.0f;
            manager.MaxSpeed = 5f;
            manager.PerceptionRadius = 8f;
        }
        
        private static void ConfigureCombatSwarm(SwarmManager manager)
        {
            manager.MaxAgents = 50;
            manager.SpawnRadius = 15f;
            manager.SeparationWeight = 2.0f;
            manager.AlignmentWeight = 1.5f;
            manager.CohesionWeight = 0.8f;
            manager.MaxSpeed = 8f;
            manager.PerceptionRadius = 12f;
            manager.SwarmType = SwarmType.Combat;
        }
        
        private static void ConfigureFormationSwarm(SwarmManager manager)
        {
            manager.MaxAgents = 25;
            manager.SpawnRadius = 5f;
            manager.SeparationWeight = 1.0f;
            manager.AlignmentWeight = 2.0f;
            manager.CohesionWeight = 1.5f;
            manager.MaxSpeed = 6f;
            manager.PerceptionRadius = 10f;
            manager.SwarmType = SwarmType.Formation;
            manager.UseFormations = true;
        }
        
        private static Vector3 GetSpawnPosition()
        {
            // Try to position near scene view camera
            if (SceneView.lastActiveSceneView != null)
            {
                var sceneView = SceneView.lastActiveSceneView;
                Vector3 forward = sceneView.rotation * Vector3.forward;
                return sceneView.pivot + forward * 10f;
            }
            
            // Fallback to origin
            return Vector3.zero;
        }
        
        private static bool IsAnySwarmShowingDebug(SwarmManager[] swarms)
        {
            foreach (var swarm in swarms)
            {
                if (swarm.ShowDebugInfo) return true;
            }
            return false;
        }
    }
    
    /// <summary>
    /// Template types for quick swarm creation
    /// </summary>
    public enum SwarmTemplate
    {
        Basic,
        Combat,
        Formation
    }
    
    /// <summary>
    /// Wizard for creating custom agent prefabs
    /// </summary>
    public class CreateAgentPrefabWizard : ScriptableWizard
    {
        public string agentName = "SwarmAgent";
        public GameObject baseMesh;
        public Material agentMaterial;
        public bool addTrailRenderer = true;
        public bool addRigidbody = true;
        public bool addCollider = true;
        public SwarmAgentType agentType = SwarmAgentType.Basic;
        
        [MenuItem("Swarm/Create/Agent Prefab Wizard")]
        public static void ShowWizard()
        {
            DisplayWizard<CreateAgentPrefabWizard>("Create Swarm Agent Prefab", "Create");
        }
        
        void OnWizardCreate()
        {
            // Create prefab folder if it doesn't exist
            string prefabFolder = "Assets/Swarm/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/Swarm", "Prefabs");
            }
            
            // Create agent GameObject
            GameObject agentGO = new GameObject(agentName);
            
            // Add mesh
            if (baseMesh != null)
            {
                var meshFilter = agentGO.AddComponent<MeshFilter>();
                var meshRenderer = agentGO.AddComponent<MeshRenderer>();
                
                meshFilter.mesh = baseMesh.GetComponent<MeshFilter>().sharedMesh;
                meshRenderer.material = agentMaterial != null ? agentMaterial : AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }
            else
            {
                // Create default capsule
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var meshFilter = agentGO.AddComponent<MeshFilter>();
                var meshRenderer = agentGO.AddComponent<MeshRenderer>();
                
                meshFilter.mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
                meshRenderer.material = primitive.GetComponent<MeshRenderer>().sharedMaterial;
                
                DestroyImmediate(primitive);
            }
            
            // Add physics components
            if (addRigidbody)
            {
                var rb = agentGO.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            
            if (addCollider)
            {
                var collider = agentGO.AddComponent<SphereCollider>();
                collider.radius = 0.5f;
            }
            
            // Add trail renderer
            if (addTrailRenderer)
            {
                var trail = agentGO.AddComponent<TrailRenderer>();
                trail.time = 2f;
                trail.startWidth = 0.1f;
                trail.endWidth = 0.01f;
                trail.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
            }
            
            // Add appropriate SwarmAgent component
            switch (agentType)
            {
                case SwarmAgentType.Basic:
                    agentGO.AddComponent<BoidAgent>();
                    break;
                case SwarmAgentType.Combat:
                    agentGO.AddComponent<CombatAgent>();
                    break;
                case SwarmAgentType.Formation:
                    agentGO.AddComponent<FormationAgent>();
                    break;
            }
            
            // Save as prefab
            string prefabPath = $"{prefabFolder}/{agentName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(agentGO, prefabPath);
            
            // Clean up and select
            DestroyImmediate(agentGO);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            Debug.Log($"Created agent prefab: {prefabPath}");
        }
        
        void OnWizardUpdate()
        {
            helpString = "Create a customized swarm agent prefab with the specified components and behavior.";
            
            if (string.IsNullOrEmpty(agentName))
            {
                errorString = "Agent name cannot be empty.";
                isValid = false;
            }
            else
            {
                errorString = "";
                isValid = true;
            }
        }
    }
    
    public enum SwarmAgentType
    {
        Basic,
        Combat,
        Formation
    }
    
    /// <summary>
    /// Utility for validating swarm configurations
    /// </summary>
    public static class SwarmValidator
    {
        public static void ValidateAllSwarms()
        {
            var swarms = Object.FindObjectsOfType<SwarmManager>();
            var issues = new System.Collections.Generic.List<string>();
            
            foreach (var swarm in swarms)
            {
                ValidateSwarm(swarm, issues);
            }
            
            if (issues.Count > 0)
            {
                string message = "Swarm validation found the following issues:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Swarm Validation", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Swarm Validation", "All swarms are properly configured!", "OK");
            }
        }
        
        private static void ValidateSwarm(SwarmManager swarm, System.Collections.Generic.List<string> issues)
        {
            string swarmName = swarm.name;
            
            // Check agent prefab
            if (swarm.AgentPrefab == null)
            {
                issues.Add($"{swarmName}: No agent prefab assigned");
            }
            else if (swarm.AgentPrefab.GetComponent<SwarmAgent>() == null)
            {
                issues.Add($"{swarmName}: Agent prefab missing SwarmAgent component");
            }
            
            // Check agent count
            if (swarm.MaxAgents <= 0)
            {
                issues.Add($"{swarmName}: Max agents must be greater than 0");
            }
            else if (swarm.MaxAgents > 10000)
            {
                issues.Add($"{swarmName}: Very high agent count ({swarm.MaxAgents}) may cause performance issues");
            }
            
            // Check performance settings
            if (swarm.PerceptionRadius <= 0)
            {
                issues.Add($"{swarmName}: Perception radius must be greater than 0");
            }
            
            if (swarm.MaxSpeed <= 0)
            {
                issues.Add($"{swarmName}: Max speed must be greater than 0");
            }
            
            // Check behavior weights
            if (swarm.SeparationWeight < 0 || swarm.AlignmentWeight < 0 || swarm.CohesionWeight < 0)
            {
                issues.Add($"{swarmName}: Behavior weights cannot be negative");
            }
            
            if (swarm.SeparationWeight + swarm.AlignmentWeight + swarm.CohesionWeight == 0)
            {
                issues.Add($"{swarmName}: At least one behavior weight must be greater than 0");
            }
        }
    }
}