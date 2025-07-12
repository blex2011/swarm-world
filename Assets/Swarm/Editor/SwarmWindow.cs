using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace SwarmWorld.Editor
{
    /// <summary>
    /// Main Swarm Control Panel - Central hub for swarm management and monitoring
    /// </summary>
    public class SwarmWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabNames = { "Overview", "Agents", "Performance", "Settings" };
        
        // Swarm data
        private List<SwarmManager> activeSwarms = new List<SwarmManager>();
        private SwarmManager selectedSwarm;
        private bool autoRefresh = true;
        private float refreshRate = 0.5f;
        private double lastRefreshTime;
        
        // UI State
        private bool showAdvancedSettings = false;
        private bool showDebugInfo = false;
        private GUIStyle headerStyle;
        private GUIStyle cardStyle;
        
        [MenuItem("Swarm/Swarm Control Panel")]
        public static void ShowWindow()
        {
            SwarmWindow window = GetWindow<SwarmWindow>("Swarm Control Panel");
            window.titleContent = new GUIContent("🐝 Swarm Control", "Swarm World Control Panel");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }
        
        void OnEnable()
        {
            RefreshSwarmList();
            EditorApplication.update += OnEditorUpdate;
            InitializeStyles();
        }
        
        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }
        
        void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = Color.white }
                };
            }
            
            if (cardStyle == null)
            {
                cardStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }
        }
        
        void OnEditorUpdate()
        {
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > refreshRate)
            {
                RefreshSwarmList();
                lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
        
        void RefreshSwarmList()
        {
            activeSwarms.Clear();
            SwarmManager[] allSwarms = FindObjectsOfType<SwarmManager>();
            activeSwarms.AddRange(allSwarms);
            
            // Remove null or destroyed swarms
            activeSwarms.RemoveAll(s => s == null);
            
            if (selectedSwarm == null && activeSwarms.Count > 0)
            {
                selectedSwarm = activeSwarms[0];
            }
        }
        
        void OnGUI()
        {
            InitializeStyles();
            
            DrawHeader();
            DrawToolbar();
            
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0:
                    DrawOverviewTab();
                    break;
                case 1:
                    DrawAgentsTab();
                    break;
                case 2:
                    DrawPerformanceTab();
                    break;
                case 3:
                    DrawSettingsTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
            
            DrawFooter();
        }
        
        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("🐝 Swarm World Control Panel", headerStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshSwarmList();
            }
            
            autoRefresh = GUILayout.Toggle(autoRefresh, "Auto Refresh", EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();
        }
        
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUILayout.Label("Active Swarms:", GUILayout.Width(100));
            
            if (activeSwarms.Count == 0)
            {
                GUILayout.Label("No active swarms", EditorStyles.miniLabel);
            }
            else
            {
                string[] swarmNames = activeSwarms.Select(s => s.name).ToArray();
                int selectedIndex = selectedSwarm != null ? activeSwarms.IndexOf(selectedSwarm) : 0;
                int newIndex = EditorGUILayout.Popup(selectedIndex, swarmNames);
                
                if (newIndex >= 0 && newIndex < activeSwarms.Count)
                {
                    selectedSwarm = activeSwarms[newIndex];
                }
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Create New Swarm", GUILayout.Width(120)))
            {
                ShowCreateSwarmDialog();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
        
        void DrawOverviewTab()
        {
            if (selectedSwarm == null)
            {
                DrawNoSwarmSelected();
                return;
            }
            
            // Swarm Status Card
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("📊 Swarm Status", headerStyle);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Name:", selectedSwarm.name);
            EditorGUILayout.LabelField("Status:", selectedSwarm.isActiveAndEnabled ? "✅ Active" : "❌ Inactive");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Agent Count:", selectedSwarm.AgentCount.ToString());
            EditorGUILayout.LabelField("Max Agents:", selectedSwarm.MaxAgents.ToString());
            EditorGUILayout.EndHorizontal();
            
            // Progress bar for agent utilization
            float utilization = selectedSwarm.MaxAgents > 0 ? (float)selectedSwarm.AgentCount / selectedSwarm.MaxAgents : 0f;
            Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(progressRect, utilization, $"Agent Utilization: {utilization:P0}");
            
            EditorGUILayout.EndVertical();
            
            // Quick Stats
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("⚡ Quick Stats", headerStyle);
            
            if (selectedSwarm.Metrics != null)
            {
                var metrics = selectedSwarm.Metrics;
                EditorGUILayout.LabelField($"Update Time: {metrics.UpdateTime:F2}ms");
                EditorGUILayout.LabelField($"Avg Neighbors: {metrics.AverageNeighbors:F1}");
                EditorGUILayout.LabelField($"Physics Time: {metrics.PhysicsTime:F2}ms");
            }
            else
            {
                EditorGUILayout.LabelField("No metrics available");
            }
            
            EditorGUILayout.EndVertical();
            
            // Quick Actions
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("🎮 Quick Actions", headerStyle);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Start Swarm"))
            {
                selectedSwarm.StartSwarm();
            }
            
            if (GUILayout.Button("Stop Swarm"))
            {
                selectedSwarm.StopSwarm();
            }
            
            if (GUILayout.Button("Reset Swarm"))
            {
                selectedSwarm.ResetSwarm();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Toggle Debug"))
            {
                selectedSwarm.ShowDebugInfo = !selectedSwarm.ShowDebugInfo;
            }
            
            if (GUILayout.Button("Focus Camera"))
            {
                FocusCameraOnSwarm();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawAgentsTab()
        {
            if (selectedSwarm == null)
            {
                DrawNoSwarmSelected();
                return;
            }
            
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("👥 Agent Management", headerStyle);
            
            // Agent creation controls
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Spawn Count:", GUILayout.Width(90));
            int spawnCount = EditorGUILayout.IntField(10, GUILayout.Width(60));
            
            if (GUILayout.Button("Spawn Agents", GUILayout.Width(100)))
            {
                selectedSwarm.SpawnAgents(spawnCount);
            }
            
            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                selectedSwarm.ClearAllAgents();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            // Agent List
            if (selectedSwarm.Agents != null && selectedSwarm.Agents.Count > 0)
            {
                EditorGUILayout.BeginVertical(cardStyle);
                EditorGUILayout.LabelField($"🤖 Active Agents ({selectedSwarm.Agents.Count})", headerStyle);
                
                foreach (var agent in selectedSwarm.Agents.Take(20)) // Limit display to 20 for performance
                {
                    if (agent == null) continue;
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.LabelField($"Agent {agent.AgentId}", GUILayout.Width(80));
                    EditorGUILayout.LabelField($"Pos: {agent.transform.position}", GUILayout.Width(150));
                    EditorGUILayout.LabelField($"Vel: {agent.Velocity.magnitude:F1}", GUILayout.Width(60));
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = agent.gameObject;
                        SceneView.FrameLastActiveSceneView();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                if (selectedSwarm.Agents.Count > 20)
                {
                    EditorGUILayout.LabelField($"... and {selectedSwarm.Agents.Count - 20} more agents");
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawPerformanceTab()
        {
            if (selectedSwarm == null)
            {
                DrawNoSwarmSelected();
                return;
            }
            
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("📈 Performance Metrics", headerStyle);
            
            if (selectedSwarm.Metrics != null)
            {
                var metrics = selectedSwarm.Metrics;
                
                // Performance bars
                DrawPerformanceBar("Update Time", metrics.UpdateTime, 16.67f, "ms");
                DrawPerformanceBar("Physics Time", metrics.PhysicsTime, 8.33f, "ms");
                DrawPerformanceBar("Render Time", metrics.RenderTime, 8.33f, "ms");
                
                EditorGUILayout.Space();
                
                // Detailed stats
                EditorGUILayout.LabelField("Detailed Statistics:");
                EditorGUILayout.LabelField($"  Total Agents: {metrics.TotalAgents}");
                EditorGUILayout.LabelField($"  Active Agents: {metrics.ActiveAgents}");
                EditorGUILayout.LabelField($"  Average Neighbors: {metrics.AverageNeighbors:F1}");
                EditorGUILayout.LabelField($"  Memory Usage: {GC.GetTotalMemory(false) / 1024 / 1024:F1} MB");
                
                EditorGUILayout.Space();
                
                // Behavior timing breakdown
                if (metrics.BehaviorTimes != null && metrics.BehaviorTimes.Count > 0)
                {
                    EditorGUILayout.LabelField("Behavior Timing:");
                    foreach (var kvp in metrics.BehaviorTimes)
                    {
                        EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value:F2}ms");
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("No performance metrics available");
                EditorGUILayout.LabelField("Enable profiling in SwarmManager to see metrics");
            }
            
            EditorGUILayout.EndVertical();
            
            // Performance controls
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("🎛️ Performance Controls", headerStyle);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Profiling"))
            {
                selectedSwarm.StartProfiling();
            }
            
            if (GUILayout.Button("Stop Profiling"))
            {
                selectedSwarm.StopProfiling();
            }
            
            if (GUILayout.Button("Reset Metrics"))
            {
                selectedSwarm.ResetMetrics();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawSettingsTab()
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("⚙️ Global Settings", headerStyle);
            
            refreshRate = EditorGUILayout.Slider("Refresh Rate", refreshRate, 0.1f, 2.0f);
            showDebugInfo = EditorGUILayout.Toggle("Show Debug Info", showDebugInfo);
            showAdvancedSettings = EditorGUILayout.Toggle("Show Advanced Settings", showAdvancedSettings);
            
            EditorGUILayout.EndVertical();
            
            if (selectedSwarm != null)
            {
                EditorGUILayout.BeginVertical(cardStyle);
                EditorGUILayout.LabelField("🎯 Swarm Settings", headerStyle);
                
                // Behavior weights
                EditorGUILayout.LabelField("Behavior Weights:");
                selectedSwarm.SeparationWeight = EditorGUILayout.Slider("Separation", selectedSwarm.SeparationWeight, 0f, 5f);
                selectedSwarm.AlignmentWeight = EditorGUILayout.Slider("Alignment", selectedSwarm.AlignmentWeight, 0f, 5f);
                selectedSwarm.CohesionWeight = EditorGUILayout.Slider("Cohesion", selectedSwarm.CohesionWeight, 0f, 5f);
                
                EditorGUILayout.Space();
                
                // Performance settings
                selectedSwarm.UpdateBatchSize = EditorGUILayout.IntSlider("Update Batch Size", selectedSwarm.UpdateBatchSize, 1, 100);
                selectedSwarm.MaxNeighbors = EditorGUILayout.IntSlider("Max Neighbors", selectedSwarm.MaxNeighbors, 5, 50);
                
                if (showAdvancedSettings)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Advanced Settings:");
                    selectedSwarm.UseSpatialPartitioning = EditorGUILayout.Toggle("Use Spatial Partitioning", selectedSwarm.UseSpatialPartitioning);
                    selectedSwarm.UseJobSystem = EditorGUILayout.Toggle("Use Job System", selectedSwarm.UseJobSystem);
                    selectedSwarm.UseLOD = EditorGUILayout.Toggle("Use LOD System", selectedSwarm.UseLOD);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawPerformanceBar(string label, float value, float target, string unit)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            
            Color barColor = value <= target ? Color.green : value <= target * 1.5f ? Color.yellow : Color.red;
            
            Rect barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, Color.gray);
            
            float fillAmount = Mathf.Clamp01(value / (target * 2f));
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * fillAmount, barRect.height);
            EditorGUI.DrawRect(fillRect, barColor);
            
            GUILayout.Label($"{value:F2}{unit}", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
        
        void DrawNoSwarmSelected()
        {
            EditorGUILayout.BeginVertical(cardStyle);
            EditorGUILayout.LabelField("No swarm selected", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Create a new swarm or select an existing one from the dropdown above.", EditorStyles.wordWrappedLabel);
            
            if (GUILayout.Button("Create New Swarm"))
            {
                ShowCreateSwarmDialog();
            }
            EditorGUILayout.EndVertical();
        }
        
        void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Active Swarms: {activeSwarms.Count}", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Last Update: {System.DateTime.Now:HH:mm:ss}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        
        void ShowCreateSwarmDialog()
        {
            CreateSwarmDialog.ShowDialog();
        }
        
        void FocusCameraOnSwarm()
        {
            if (selectedSwarm != null && SceneView.lastActiveSceneView != null)
            {
                Vector3 swarmCenter = selectedSwarm.GetSwarmCenter();
                SceneView.lastActiveSceneView.pivot = swarmCenter;
                SceneView.lastActiveSceneView.size = selectedSwarm.GetSwarmBounds().size.magnitude;
                SceneView.lastActiveSceneView.Repaint();
            }
        }
    }
    
    /// <summary>
    /// Dialog for creating new swarms
    /// </summary>
    public class CreateSwarmDialog : EditorWindow
    {
        private string swarmName = "New Swarm";
        private int agentCount = 100;
        private GameObject agentPrefab;
        private SwarmType swarmType = SwarmType.Boid;
        
        public static void ShowDialog()
        {
            CreateSwarmDialog window = GetWindow<CreateSwarmDialog>("Create Swarm", true);
            window.minSize = new Vector2(300, 200);
            window.maxSize = new Vector2(300, 200);
            window.ShowModal();
        }
        
        void OnGUI()
        {
            EditorGUILayout.LabelField("Create New Swarm", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            swarmName = EditorGUILayout.TextField("Swarm Name:", swarmName);
            agentCount = EditorGUILayout.IntField("Agent Count:", agentCount);
            agentPrefab = EditorGUILayout.ObjectField("Agent Prefab:", agentPrefab, typeof(GameObject), false) as GameObject;
            swarmType = (SwarmType)EditorGUILayout.EnumPopup("Swarm Type:", swarmType);
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create"))
            {
                CreateSwarm();
                Close();
            }
            
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        void CreateSwarm()
        {
            GameObject swarmGO = new GameObject(swarmName);
            SwarmManager manager = swarmGO.AddComponent<SwarmManager>();
            
            manager.AgentPrefab = agentPrefab;
            manager.MaxAgents = agentCount;
            manager.SwarmType = swarmType;
            
            Selection.activeGameObject = swarmGO;
            EditorUtility.SetDirty(swarmGO);
        }
    }
    
    public enum SwarmType
    {
        Boid,
        Formation,
        Combat,
        Exploration,
        Custom
    }
}