using UnityEngine;
using UnityEditor;
using SwarmAI.Core;
using System.Collections.Generic;
using System.Linq;

namespace SwarmAI.Editor
{
    /// <summary>
    /// Main Unity Editor window for Swarm AI plugin management and debugging.
    /// Provides comprehensive tools for swarm design, monitoring, and optimization.
    /// </summary>
    public class SwarmEditorWindow : EditorWindow
    {
        private static SwarmEditorWindow instance;
        
        // UI State
        private int selectedTab = 0;
        private readonly string[] tabNames = { "Overview", "Behaviors", "Performance", "Claude Flow", "Debug" };
        
        // Data
        private List<ISwarmManager> activeSwarms;
        private ISwarmManager selectedSwarm;
        private Vector2 scrollPosition;
        
        // Performance monitoring
        private SwarmPerformanceMonitor performanceMonitor;
        private bool isMonitoring = false;
        
        // Claude Flow integration
        private bool claudeFlowEnabled = false;
        private string claudeFlowStatus = "Disconnected";
        
        [MenuItem("Window/Swarm AI/Swarm Manager")]
        public static void ShowWindow()
        {
            instance = GetWindow<SwarmEditorWindow>("Swarm AI Manager");
            instance.minSize = new Vector2(600, 400);
            instance.Show();
        }
        
        private void OnEnable()
        {
            instance = this;
            RefreshSwarmList();
            InitializePerformanceMonitor();
            
            // Subscribe to play mode changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CleanupPerformanceMonitor();
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawTabs();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0: DrawOverviewTab(); break;
                case 1: DrawBehaviorsTab(); break;
                case 2: DrawPerformanceTab(); break;
                case 3: DrawClaudeFlowTab(); break;
                case 4: DrawDebugTab(); break;
            }
            
            EditorGUILayout.EndScrollView();
            
            if (Application.isPlaying)
            {
                Repaint(); // Continuous updates during play mode
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("Unity Swarm AI Manager", EditorStyles.boldLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshSwarmList();
            }
            
            if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
            {
                SwarmSettingsWindow.ShowWindow();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTabs()
        {
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        }
        
        private void DrawOverviewTab()
        {
            EditorGUILayout.LabelField("Swarm Overview", EditorStyles.boldLabel);
            
            if (activeSwarms == null || activeSwarms.Count == 0)
            {
                EditorGUILayout.HelpBox("No active swarms found. Create a SwarmManager in your scene to get started.", MessageType.Info);
                
                if (GUILayout.Button("Create New Swarm"))
                {
                    CreateNewSwarm();
                }
                return;
            }
            
            // Swarm selection
            EditorGUILayout.LabelField("Active Swarms:", EditorStyles.boldLabel);
            foreach (var swarm in activeSwarms)
            {
                EditorGUILayout.BeginHorizontal();
                
                bool isSelected = swarm == selectedSwarm;
                bool newSelected = EditorGUILayout.Toggle(isSelected, GUILayout.Width(20));
                
                if (newSelected != isSelected)
                {
                    selectedSwarm = newSelected ? swarm : null;
                }
                
                EditorGUILayout.LabelField($"Swarm {swarm.SwarmId}", EditorStyles.label);
                EditorGUILayout.LabelField($"{swarm.AgentCount}/{swarm.MaxAgents} agents", GUILayout.Width(100));
                
                GUILayout.FlexibleSpace();
                
                GUI.enabled = Application.isPlaying;
                if (GUILayout.Button("Focus", GUILayout.Width(60)))
                {
                    FocusOnSwarm(swarm);
                }
                GUI.enabled = true;
                
                EditorGUILayout.EndHorizontal();
            }
            
            // Selected swarm details
            if (selectedSwarm != null)
            {
                DrawSwarmDetails(selectedSwarm);
            }
        }
        
        private void DrawSwarmDetails(ISwarmManager swarm)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Swarm {swarm.SwarmId} Details", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField($"Agent Count: {swarm.AgentCount}/{swarm.MaxAgents}");
            EditorGUILayout.LabelField($"Active: {swarm.IsActive}");
            
            if (swarm.Metrics != null)
            {
                var metrics = swarm.Metrics;
                EditorGUILayout.LabelField($"Update Time: {metrics.UpdateTime:F2}ms");
                EditorGUILayout.LabelField($"Average FPS: {metrics.AverageFPS:F1}");
                EditorGUILayout.LabelField($"Memory Usage: {metrics.MemoryUsage:F1}MB");
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBehaviorsTab()
        {
            EditorGUILayout.LabelField("Behavior Management", EditorStyles.boldLabel);
            
            if (selectedSwarm == null)
            {
                EditorGUILayout.HelpBox("Select a swarm to manage behaviors.", MessageType.Info);
                return;
            }
            
            // Behavior library
            EditorGUILayout.LabelField("Available Behaviors:", EditorStyles.boldLabel);
            
            var behaviors = GetAvailableBehaviors();
            foreach (var behavior in behaviors)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(behavior.BehaviorName);
                EditorGUILayout.LabelField(behavior.Category.ToString(), GUILayout.Width(100));
                
                if (GUILayout.Button("Add to Swarm", GUILayout.Width(100)))
                {
                    selectedSwarm.AddGlobalBehavior(behavior);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space();
            
            // Active behaviors
            EditorGUILayout.LabelField("Active Behaviors:", EditorStyles.boldLabel);
            // Implementation would list and allow editing of active behaviors
        }
        
        private void DrawPerformanceTab()
        {
            EditorGUILayout.LabelField("Performance Monitoring", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            bool newMonitoring = EditorGUILayout.Toggle("Enable Monitoring", isMonitoring);
            if (newMonitoring != isMonitoring)
            {
                isMonitoring = newMonitoring;
                if (isMonitoring)
                {
                    StartPerformanceMonitoring();
                }
                else
                {
                    StopPerformanceMonitoring();
                }
            }
            
            if (GUILayout.Button("Reset Statistics"))
            {
                ResetPerformanceStatistics();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (performanceMonitor != null && isMonitoring)
            {
                DrawPerformanceGraphs();
            }
            
            // Performance recommendations
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Optimization Recommendations:", EditorStyles.boldLabel);
            DrawPerformanceRecommendations();
        }
        
        private void DrawClaudeFlowTab()
        {
            EditorGUILayout.LabelField("Claude Flow Integration", EditorStyles.boldLabel);
            
            // Connection status
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(60));
            
            Color statusColor = claudeFlowStatus == "Connected" ? Color.green : Color.red;
            GUI.color = statusColor;
            EditorGUILayout.LabelField(claudeFlowStatus);
            GUI.color = Color.white;
            
            if (GUILayout.Button("Test Connection", GUILayout.Width(120)))
            {
                TestClaudeFlowConnection();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Configuration
            claudeFlowEnabled = EditorGUILayout.Toggle("Enable Claude Flow", claudeFlowEnabled);
            
            if (claudeFlowEnabled)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Configuration:", EditorStyles.boldLabel);
                
                // API endpoint and settings would go here
                EditorGUILayout.HelpBox("Configure Claude Flow settings in the Swarm Settings window.", MessageType.Info);
            }
            
            // Recent decisions
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Recent AI Decisions:", EditorStyles.boldLabel);
            DrawRecentDecisions();
        }
        
        private void DrawDebugTab()
        {
            EditorGUILayout.LabelField("Debug & Visualization", EditorStyles.boldLabel);
            
            if (selectedSwarm == null)
            {
                EditorGUILayout.HelpBox("Select a swarm to access debug tools.", MessageType.Info);
                return;
            }
            
            // Visualization options
            EditorGUILayout.LabelField("Visualization Options:", EditorStyles.boldLabel);
            
            bool showConnections = EditorGUILayout.Toggle("Show Agent Connections", false);
            bool showVelocities = EditorGUILayout.Toggle("Show Velocity Vectors", false);
            bool showPerception = EditorGUILayout.Toggle("Show Perception Radius", false);
            bool showForces = EditorGUILayout.Toggle("Show Force Vectors", false);
            
            // Debug actions
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Actions:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Pause All Agents"))
            {
                PauseAllAgents();
            }
            
            if (GUILayout.Button("Resume All Agents"))
            {
                ResumeAllAgents();
            }
            
            if (GUILayout.Button("Reset Positions"))
            {
                ResetAgentPositions();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Agent inspector
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Agent Inspector:", EditorStyles.boldLabel);
            DrawAgentInspector();
        }
        
        #region Helper Methods
        
        private void RefreshSwarmList()
        {
            activeSwarms = new List<ISwarmManager>();
            
            if (Application.isPlaying)
            {
                // Find all active swarm managers in the scene
                var managers = FindObjectsOfType<MonoBehaviour>()
                    .OfType<ISwarmManager>()
                    .ToList();
                
                activeSwarms.AddRange(managers);
            }
        }
        
        private void CreateNewSwarm()
        {
            var swarmGO = new GameObject("New Swarm Manager");
            // Add appropriate swarm manager component
            Undo.RegisterCreatedObjectUndo(swarmGO, "Create Swarm Manager");
            Selection.activeObject = swarmGO;
        }
        
        private void InitializePerformanceMonitor()
        {
            if (performanceMonitor == null)
            {
                performanceMonitor = new SwarmPerformanceMonitor();
            }
        }
        
        private void CleanupPerformanceMonitor()
        {
            performanceMonitor?.Cleanup();
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RefreshSwarmList();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                activeSwarms?.Clear();
                selectedSwarm = null;
            }
        }
        
        private List<ISwarmBehavior> GetAvailableBehaviors()
        {
            // Return list of available behavior types
            return new List<ISwarmBehavior>();
        }
        
        private void FocusOnSwarm(ISwarmManager swarm)
        {
            if (SceneView.lastActiveSceneView != null)
            {
                // Calculate swarm center and focus camera
                Vector3 swarmCenter = CalculateSwarmCenter(swarm);
                SceneView.lastActiveSceneView.pivot = swarmCenter;
                SceneView.lastActiveSceneView.Repaint();
            }
        }
        
        private Vector3 CalculateSwarmCenter(ISwarmManager swarm)
        {
            var agents = swarm.GetAgents();
            if (agents.Count == 0) return Vector3.zero;
            
            Vector3 center = Vector3.zero;
            foreach (var agent in agents)
            {
                center += agent.Position;
            }
            return center / agents.Count;
        }
        
        private void StartPerformanceMonitoring()
        {
            performanceMonitor?.StartMonitoring();
        }
        
        private void StopPerformanceMonitoring()
        {
            performanceMonitor?.StopMonitoring();
        }
        
        private void ResetPerformanceStatistics()
        {
            performanceMonitor?.ResetStatistics();
        }
        
        private void DrawPerformanceGraphs()
        {
            // Implementation for performance graphs
            EditorGUILayout.LabelField("Performance graphs would be rendered here");
        }
        
        private void DrawPerformanceRecommendations()
        {
            // Implementation for performance recommendations
            EditorGUILayout.HelpBox("Performance recommendations would be displayed here", MessageType.Info);
        }
        
        private void TestClaudeFlowConnection()
        {
            // Implementation for testing Claude Flow connection
            claudeFlowStatus = "Testing...";
            // Async test would update status
        }
        
        private void DrawRecentDecisions()
        {
            // Implementation for displaying recent AI decisions
            EditorGUILayout.LabelField("Recent decisions would be displayed here");
        }
        
        private void PauseAllAgents()
        {
            if (selectedSwarm != null)
            {
                selectedSwarm.IsActive = false;
            }
        }
        
        private void ResumeAllAgents()
        {
            if (selectedSwarm != null)
            {
                selectedSwarm.IsActive = true;
            }
        }
        
        private void ResetAgentPositions()
        {
            // Implementation for resetting agent positions
        }
        
        private void DrawAgentInspector()
        {
            // Implementation for agent-specific inspector
            EditorGUILayout.LabelField("Agent details would be displayed here");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Performance monitoring utility for swarm systems
    /// </summary>
    public class SwarmPerformanceMonitor
    {
        private bool isActive = false;
        
        public void StartMonitoring()
        {
            isActive = true;
        }
        
        public void StopMonitoring()
        {
            isActive = false;
        }
        
        public void ResetStatistics()
        {
            // Reset performance statistics
        }
        
        public void Cleanup()
        {
            isActive = false;
        }
    }
}