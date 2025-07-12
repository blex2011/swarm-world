using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace SwarmWorld.Editor
{
    /// <summary>
    /// Advanced performance monitoring and visualization dashboard for swarm systems
    /// </summary>
    public class PerformanceDashboard : EditorWindow
    {
        // Window management
        private Vector2 scrollPosition;
        private int selectedTab = 0;
        private string[] tabNames = { "Overview", "Real-Time", "Profiling", "Optimization" };
        
        // Performance tracking
        private PerformanceProfiler profiler;
        private List<SwarmManager> monitoredSwarms = new List<SwarmManager>();
        private float refreshRate = 0.1f;
        private double lastRefreshTime;
        private bool isRecording = false;
        
        // Chart data
        private List<float> fpsHistory = new List<float>();
        private List<float> updateTimeHistory = new List<float>();
        private List<float> memoryHistory = new List<float>();
        private List<int> agentCountHistory = new List<int>();
        private const int MAX_HISTORY_POINTS = 200;
        
        // Visualization settings
        private bool showCharts = true;
        private bool showAlerts = true;
        private bool autoOptimize = false;
        private Color chartLineColor = Color.green;
        private Color chartBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        
        // Performance thresholds
        private float targetFPS = 60f;
        private float maxUpdateTime = 16.67f; // milliseconds
        private float maxMemoryUsage = 512f; // MB
        private int maxAgentCount = 5000;
        
        // Optimization suggestions
        private List<OptimizationSuggestion> suggestions = new List<OptimizationSuggestion>();
        
        [MenuItem("Swarm/Performance Dashboard")]
        public static void ShowWindow()
        {
            PerformanceDashboard window = GetWindow<PerformanceDashboard>("Performance Dashboard");
            window.titleContent = new GUIContent("📊 Performance", "Swarm Performance Dashboard");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }
        
        void OnEnable()
        {
            profiler = new PerformanceProfiler();
            RefreshSwarmList();
            EditorApplication.update += OnEditorUpdate;
            isRecording = true;
        }
        
        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            isRecording = false;
        }
        
        void OnEditorUpdate()
        {
            if (isRecording && EditorApplication.timeSinceStartup - lastRefreshTime > refreshRate)
            {
                UpdatePerformanceData();
                lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
        
        void RefreshSwarmList()
        {
            monitoredSwarms.Clear();
            SwarmManager[] allSwarms = FindObjectsOfType<SwarmManager>();
            monitoredSwarms.AddRange(allSwarms.Where(s => s != null));
        }
        
        void UpdatePerformanceData()
        {
            if (!Application.isPlaying) return;
            
            // Collect performance metrics
            float currentFPS = 1f / Time.unscaledDeltaTime;
            float totalUpdateTime = 0f;
            int totalAgents = 0;
            
            foreach (var swarm in monitoredSwarms)
            {
                if (swarm != null && swarm.Metrics != null)
                {
                    totalUpdateTime += swarm.Metrics.UpdateTime;
                    totalAgents += swarm.Metrics.TotalAgents;
                }
            }
            
            float memoryUsage = GC.GetTotalMemory(false) / 1024f / 1024f; // MB
            
            // Update history
            UpdateHistory(fpsHistory, currentFPS);
            UpdateHistory(updateTimeHistory, totalUpdateTime);
            UpdateHistory(memoryHistory, memoryUsage);
            UpdateHistory(agentCountHistory, totalAgents);
            
            // Update profiler
            profiler.UpdateMetrics(currentFPS, totalUpdateTime, memoryUsage, totalAgents);
            
            // Generate optimization suggestions
            UpdateOptimizationSuggestions();
        }
        
        void UpdateHistory<T>(List<T> history, T newValue)
        {
            history.Add(newValue);
            if (history.Count > MAX_HISTORY_POINTS)
            {
                history.RemoveAt(0);
            }
        }
        
        void OnGUI()
        {
            DrawHeader();
            
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            switch (selectedTab)
            {
                case 0:
                    DrawOverviewTab();
                    break;
                case 1:
                    DrawRealTimeTab();
                    break;
                case 2:
                    DrawProfilingTab();
                    break;
                case 3:
                    DrawOptimizationTab();
                    break;
            }
            
            EditorGUILayout.EndScrollView();
            
            DrawFooter();
        }
        
        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("📊 Performance Dashboard", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button(isRecording ? "⏸️ Pause" : "▶️ Record", EditorStyles.toolbarButton))
            {
                isRecording = !isRecording;
            }
            
            if (GUILayout.Button("🔄 Refresh", EditorStyles.toolbarButton))
            {
                RefreshSwarmList();
            }
            
            if (GUILayout.Button("📋 Export", EditorStyles.toolbarButton))
            {
                ExportPerformanceData();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        void DrawOverviewTab()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Performance monitoring is only available in Play Mode", MessageType.Info);
                return;
            }
            
            // Current performance summary
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🎯 Current Performance", EditorStyles.boldLabel);
            
            float currentFPS = fpsHistory.Count > 0 ? fpsHistory.Last() : 0f;
            float currentUpdateTime = updateTimeHistory.Count > 0 ? updateTimeHistory.Last() : 0f;
            float currentMemory = memoryHistory.Count > 0 ? memoryHistory.Last() : 0f;
            int currentAgents = agentCountHistory.Count > 0 ? agentCountHistory.Last() : 0;
            
            DrawMetricRow("FPS", currentFPS, targetFPS, "F1", fpsHistory.Count > 0);
            DrawMetricRow("Update Time", currentUpdateTime, maxUpdateTime, "F2", updateTimeHistory.Count > 0, "ms");
            DrawMetricRow("Memory", currentMemory, maxMemoryUsage, "F1", memoryHistory.Count > 0, "MB");
            DrawMetricRow("Agents", currentAgents, maxAgentCount, "F0", agentCountHistory.Count > 0);
            
            EditorGUILayout.EndVertical();
            
            // Performance charts
            if (showCharts)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📈 Performance Charts", EditorStyles.boldLabel);
                
                DrawChart("FPS", fpsHistory, targetFPS, Color.green);
                DrawChart("Update Time (ms)", updateTimeHistory, maxUpdateTime, Color.yellow);
                DrawChart("Memory (MB)", memoryHistory, maxMemoryUsage, Color.red);
                DrawChart("Agent Count", agentCountHistory.Select(i => (float)i).ToList(), maxAgentCount, Color.cyan);
                
                EditorGUILayout.EndVertical();
            }
            
            // Active swarms overview
            DrawActiveSwarms();
        }
        
        void DrawRealTimeTab()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("⚡ Real-Time Monitoring", EditorStyles.boldLabel);
            
            refreshRate = EditorGUILayout.Slider("Refresh Rate", refreshRate, 0.01f, 1f);
            showCharts = EditorGUILayout.Toggle("Show Charts", showCharts);
            showAlerts = EditorGUILayout.Toggle("Show Alerts", showAlerts);
            
            EditorGUILayout.EndVertical();
            
            if (showAlerts)
            {
                DrawPerformanceAlerts();
            }
            
            // Real-time swarm details
            foreach (var swarm in monitoredSwarms)
            {
                if (swarm == null) continue;
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField($"🐝 {swarm.name}", EditorStyles.boldLabel);
                
                if (swarm.Metrics != null)
                {
                    var metrics = swarm.Metrics;
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Agents: {metrics.ActiveAgents}/{metrics.TotalAgents}");
                    EditorGUILayout.LabelField($"Update: {metrics.UpdateTime:F2}ms");
                    EditorGUILayout.LabelField($"Neighbors: {metrics.AverageNeighbors:F1}");
                    EditorGUILayout.EndHorizontal();
                    
                    // Performance bars
                    DrawPerformanceBar("Update", metrics.UpdateTime, 10f);
                    DrawPerformanceBar("Physics", metrics.PhysicsTime, 5f);
                    DrawPerformanceBar("Render", metrics.RenderTime, 5f);
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        void DrawProfilingTab()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🔍 Detailed Profiling", EditorStyles.boldLabel);
            
            if (profiler != null)
            {
                var stats = profiler.GetDetailedStats();
                
                EditorGUILayout.LabelField("Session Statistics:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Recording Time: {stats.RecordingDuration:F1}s");
                EditorGUILayout.LabelField($"Average FPS: {stats.AverageFPS:F1}");
                EditorGUILayout.LabelField($"Min FPS: {stats.MinFPS:F1}");
                EditorGUILayout.LabelField($"Max FPS: {stats.MaxFPS:F1}");
                
                EditorGUILayout.Space();
                
                EditorGUILayout.LabelField($"Average Update Time: {stats.AverageUpdateTime:F2}ms");
                EditorGUILayout.LabelField($"Peak Update Time: {stats.PeakUpdateTime:F2}ms");
                EditorGUILayout.LabelField($"Memory Peak: {stats.PeakMemoryUsage:F1}MB");
                
                EditorGUILayout.Space();
                
                // Bottleneck analysis
                if (stats.Bottlenecks.Count > 0)
                {
                    EditorGUILayout.LabelField("🚨 Detected Bottlenecks:", EditorStyles.boldLabel);
                    foreach (var bottleneck in stats.Bottlenecks)
                    {
                        EditorGUILayout.LabelField($"• {bottleneck}", EditorStyles.wordWrappedLabel);
                    }
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset Stats"))
            {
                profiler.Reset();
                ClearHistory();
            }
            
            if (GUILayout.Button("Generate Report"))
            {
                GeneratePerformanceReport();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawOptimizationTab()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🔧 Optimization Tools", EditorStyles.boldLabel);
            
            autoOptimize = EditorGUILayout.Toggle("Auto Optimization", autoOptimize);
            
            EditorGUILayout.Space();
            
            // Performance targets
            EditorGUILayout.LabelField("Performance Targets:", EditorStyles.boldLabel);
            targetFPS = EditorGUILayout.FloatField("Target FPS", targetFPS);
            maxUpdateTime = EditorGUILayout.FloatField("Max Update Time (ms)", maxUpdateTime);
            maxMemoryUsage = EditorGUILayout.FloatField("Max Memory (MB)", maxMemoryUsage);
            maxAgentCount = EditorGUILayout.IntField("Max Agent Count", maxAgentCount);
            
            EditorGUILayout.EndVertical();
            
            // Optimization suggestions
            if (suggestions.Count > 0)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("💡 Optimization Suggestions", EditorStyles.boldLabel);
                
                foreach (var suggestion in suggestions)
                {
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    
                    EditorGUILayout.BeginHorizontal();
                    GUIStyle iconStyle = new GUIStyle(EditorStyles.label);
                    iconStyle.normal.textColor = suggestion.Priority == OptimizationPriority.High ? Color.red : 
                                                  suggestion.Priority == OptimizationPriority.Medium ? Color.yellow : Color.green;
                    
                    EditorGUILayout.LabelField(GetPriorityIcon(suggestion.Priority), iconStyle, GUILayout.Width(20));
                    EditorGUILayout.LabelField(suggestion.Title, EditorStyles.boldLabel);
                    
                    if (suggestion.CanAutoApply && GUILayout.Button("Apply", GUILayout.Width(60)))
                    {
                        ApplySuggestion(suggestion);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.LabelField(suggestion.Description, EditorStyles.wordWrappedLabel);
                    
                    if (!string.IsNullOrEmpty(suggestion.ExpectedImprovement))
                    {
                        EditorGUILayout.LabelField($"Expected improvement: {suggestion.ExpectedImprovement}", EditorStyles.miniLabel);
                    }
                    
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply All Safe Optimizations"))
                {
                    ApplyAllSafeOptimizations();
                }
                
                if (GUILayout.Button("Clear Suggestions"))
                {
                    suggestions.Clear();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No optimization suggestions available. Run the profiler in Play Mode to get recommendations.", MessageType.Info);
            }
        }
        
        void DrawMetricRow(string label, float current, float target, string format, bool hasData, string unit = "")
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            
            if (hasData)
            {
                Color originalColor = GUI.color;
                GUI.color = current <= target ? Color.green : current <= target * 1.2f ? Color.yellow : Color.red;
                EditorGUILayout.LabelField($"{current.ToString(format)}{unit}", GUILayout.Width(80));
                GUI.color = originalColor;
                
                EditorGUILayout.LabelField($"Target: {target.ToString(format)}{unit}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("No data", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        void DrawChart(string title, List<float> data, float maxValue, Color lineColor)
        {
            if (data.Count < 2) return;
            
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            
            Rect chartRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(chartRect, chartBackgroundColor);
            
            // Draw chart lines
            Handles.BeginGUI();
            Handles.color = lineColor;
            
            float stepX = chartRect.width / (data.Count - 1);
            
            for (int i = 0; i < data.Count - 1; i++)
            {
                float x1 = chartRect.x + i * stepX;
                float y1 = chartRect.y + chartRect.height - (data[i] / maxValue) * chartRect.height;
                float x2 = chartRect.x + (i + 1) * stepX;
                float y2 = chartRect.y + chartRect.height - (data[i + 1] / maxValue) * chartRect.height;
                
                y1 = Mathf.Clamp(y1, chartRect.y, chartRect.y + chartRect.height);
                y2 = Mathf.Clamp(y2, chartRect.y, chartRect.y + chartRect.height);
                
                Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
            }
            
            // Draw target line
            Handles.color = Color.white;
            float targetY = chartRect.y + chartRect.height - (1f) * chartRect.height; // Normalized to 1.0
            Handles.DrawLine(new Vector3(chartRect.x, targetY), new Vector3(chartRect.x + chartRect.width, targetY));
            
            Handles.EndGUI();
            
            EditorGUILayout.Space();
        }
        
        void DrawPerformanceBar(string label, float value, float maxValue)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(60));
            
            Rect barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, Color.gray);
            
            float normalizedValue = Mathf.Clamp01(value / maxValue);
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * normalizedValue, barRect.height);
            
            Color barColor = normalizedValue < 0.7f ? Color.green : normalizedValue < 0.9f ? Color.yellow : Color.red;
            EditorGUI.DrawRect(fillRect, barColor);
            
            EditorGUILayout.LabelField($"{value:F2}ms", GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
        }
        
        void DrawActiveSwarms()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"🐝 Active Swarms ({monitoredSwarms.Count})", EditorStyles.boldLabel);
            
            if (monitoredSwarms.Count == 0)
            {
                EditorGUILayout.LabelField("No active swarms found", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                foreach (var swarm in monitoredSwarms)
                {
                    if (swarm == null) continue;
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(swarm, typeof(SwarmManager), true);
                    
                    if (swarm.Metrics != null)
                    {
                        EditorGUILayout.LabelField($"{swarm.Metrics.ActiveAgents} agents", GUILayout.Width(80));
                        EditorGUILayout.LabelField($"{swarm.Metrics.UpdateTime:F1}ms", GUILayout.Width(60));
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawPerformanceAlerts()
        {
            bool hasAlerts = false;
            
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🚨 Performance Alerts", EditorStyles.boldLabel);
            
            if (fpsHistory.Count > 0 && fpsHistory.Last() < targetFPS * 0.8f)
            {
                EditorGUILayout.HelpBox($"Low FPS detected: {fpsHistory.Last():F1} (Target: {targetFPS})", MessageType.Warning);
                hasAlerts = true;
            }
            
            if (updateTimeHistory.Count > 0 && updateTimeHistory.Last() > maxUpdateTime)
            {
                EditorGUILayout.HelpBox($"High update time: {updateTimeHistory.Last():F2}ms (Max: {maxUpdateTime}ms)", MessageType.Warning);
                hasAlerts = true;
            }
            
            if (memoryHistory.Count > 0 && memoryHistory.Last() > maxMemoryUsage)
            {
                EditorGUILayout.HelpBox($"High memory usage: {memoryHistory.Last():F1}MB (Max: {maxMemoryUsage}MB)", MessageType.Warning);
                hasAlerts = true;
            }
            
            if (!hasAlerts)
            {
                EditorGUILayout.LabelField("✅ All systems running optimally", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"Monitoring: {monitoredSwarms.Count} swarms", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Recording: {(isRecording ? "ON" : "OFF")}", EditorStyles.miniLabel);
            GUILayout.Label($"Data points: {fpsHistory.Count}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        
        void UpdateOptimizationSuggestions()
        {
            suggestions.Clear();
            
            if (fpsHistory.Count > 10)
            {
                float avgFPS = fpsHistory.Skip(fpsHistory.Count - 10).Take(10).Average();
                
                if (avgFPS < targetFPS * 0.8f)
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Title = "Reduce Agent Count",
                        Description = "FPS is consistently below target. Consider reducing the number of active agents.",
                        Priority = OptimizationPriority.High,
                        CanAutoApply = true,
                        ExpectedImprovement = "10-30% FPS increase"
                    });
                    
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Title = "Enable LOD System",
                        Description = "Use Level of Detail system to reduce processing for distant agents.",
                        Priority = OptimizationPriority.Medium,
                        CanAutoApply = true,
                        ExpectedImprovement = "15-25% performance improvement"
                    });
                }
            }
            
            if (updateTimeHistory.Count > 5)
            {
                float avgUpdateTime = updateTimeHistory.Skip(updateTimeHistory.Count - 5).Take(5).Average();
                
                if (avgUpdateTime > maxUpdateTime * 0.8f)
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Title = "Enable Spatial Partitioning",
                        Description = "High update times detected. Spatial partitioning can reduce neighbor search complexity.",
                        Priority = OptimizationPriority.High,
                        CanAutoApply = true,
                        ExpectedImprovement = "30-50% update time reduction"
                    });
                }
            }
        }
        
        void ApplySuggestion(OptimizationSuggestion suggestion)
        {
            switch (suggestion.Title)
            {
                case "Reduce Agent Count":
                    foreach (var swarm in monitoredSwarms)
                    {
                        if (swarm != null)
                        {
                            int targetCount = Mathf.RoundToInt(swarm.AgentCount * 0.8f);
                            swarm.SetAgentCount(targetCount);
                        }
                    }
                    break;
                    
                case "Enable LOD System":
                    foreach (var swarm in monitoredSwarms)
                    {
                        if (swarm != null)
                        {
                            swarm.UseLOD = true;
                        }
                    }
                    break;
                    
                case "Enable Spatial Partitioning":
                    foreach (var swarm in monitoredSwarms)
                    {
                        if (swarm != null)
                        {
                            swarm.UseSpatialPartitioning = true;
                        }
                    }
                    break;
            }
            
            suggestions.Remove(suggestion);
        }
        
        void ApplyAllSafeOptimizations()
        {
            var safeOptimizations = suggestions.Where(s => s.CanAutoApply && s.Priority != OptimizationPriority.High).ToList();
            
            foreach (var suggestion in safeOptimizations)
            {
                ApplySuggestion(suggestion);
            }
        }
        
        string GetPriorityIcon(OptimizationPriority priority)
        {
            switch (priority)
            {
                case OptimizationPriority.High: return "🔴";
                case OptimizationPriority.Medium: return "🟡";
                case OptimizationPriority.Low: return "🟢";
                default: return "⚪";
            }
        }
        
        void ClearHistory()
        {
            fpsHistory.Clear();
            updateTimeHistory.Clear();
            memoryHistory.Clear();
            agentCountHistory.Clear();
        }
        
        void ExportPerformanceData()
        {
            string path = EditorUtility.SaveFilePanel("Export Performance Data", "", "swarm_performance_data.csv", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                profiler.ExportToCSV(path, fpsHistory, updateTimeHistory, memoryHistory, agentCountHistory);
                Debug.Log($"Performance data exported to: {path}");
            }
        }
        
        void GeneratePerformanceReport()
        {
            string path = EditorUtility.SaveFilePanel("Generate Performance Report", "", "swarm_performance_report.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                profiler.GenerateReport(path);
                Debug.Log($"Performance report generated: {path}");
            }
        }
    }
    
    /// <summary>
    /// Performance profiler for collecting and analyzing swarm metrics
    /// </summary>
    public class PerformanceProfiler
    {
        private List<float> fpsData = new List<float>();
        private List<float> updateTimeData = new List<float>();
        private List<float> memoryData = new List<float>();
        private List<int> agentCountData = new List<int>();
        
        private float sessionStartTime;
        
        public PerformanceProfiler()
        {
            sessionStartTime = Time.realtimeSinceStartup;
        }
        
        public void UpdateMetrics(float fps, float updateTime, float memory, int agentCount)
        {
            fpsData.Add(fps);
            updateTimeData.Add(updateTime);
            memoryData.Add(memory);
            agentCountData.Add(agentCount);
        }
        
        public ProfilerStats GetDetailedStats()
        {
            var stats = new ProfilerStats
            {
                RecordingDuration = Time.realtimeSinceStartup - sessionStartTime,
                AverageFPS = fpsData.Count > 0 ? fpsData.Average() : 0f,
                MinFPS = fpsData.Count > 0 ? fpsData.Min() : 0f,
                MaxFPS = fpsData.Count > 0 ? fpsData.Max() : 0f,
                AverageUpdateTime = updateTimeData.Count > 0 ? updateTimeData.Average() : 0f,
                PeakUpdateTime = updateTimeData.Count > 0 ? updateTimeData.Max() : 0f,
                PeakMemoryUsage = memoryData.Count > 0 ? memoryData.Max() : 0f,
                Bottlenecks = AnalyzeBottlenecks()
            };
            
            return stats;
        }
        
        private List<string> AnalyzeBottlenecks()
        {
            var bottlenecks = new List<string>();
            
            if (fpsData.Count > 10 && fpsData.Average() < 45f)
            {
                bottlenecks.Add("Low average FPS detected - consider reducing agent count or complexity");
            }
            
            if (updateTimeData.Count > 5 && updateTimeData.Max() > 20f)
            {
                bottlenecks.Add("High update spikes detected - implement frame spreading or optimization");
            }
            
            if (memoryData.Count > 5 && memoryData.Max() > 1000f)
            {
                bottlenecks.Add("High memory usage - check for memory leaks or excessive allocations");
            }
            
            return bottlenecks;
        }
        
        public void Reset()
        {
            fpsData.Clear();
            updateTimeData.Clear();
            memoryData.Clear();
            agentCountData.Clear();
            sessionStartTime = Time.realtimeSinceStartup;
        }
        
        public void ExportToCSV(string path, List<float> fps, List<float> updateTime, List<float> memory, List<int> agents)
        {
            using (var writer = new System.IO.StreamWriter(path))
            {
                writer.WriteLine("Time,FPS,UpdateTime,Memory,AgentCount");
                
                int maxCount = Mathf.Max(fps.Count, updateTime.Count, memory.Count, agents.Count);
                
                for (int i = 0; i < maxCount; i++)
                {
                    float time = i * 0.1f; // Assuming 0.1s intervals
                    float fpsValue = i < fps.Count ? fps[i] : 0f;
                    float updateValue = i < updateTime.Count ? updateTime[i] : 0f;
                    float memoryValue = i < memory.Count ? memory[i] : 0f;
                    int agentValue = i < agents.Count ? agents[i] : 0;
                    
                    writer.WriteLine($"{time:F1},{fpsValue:F2},{updateValue:F2},{memoryValue:F2},{agentValue}");
                }
            }
        }
        
        public void GenerateReport(string path)
        {
            var stats = GetDetailedStats();
            
            using (var writer = new System.IO.StreamWriter(path))
            {
                writer.WriteLine("Swarm Performance Report");
                writer.WriteLine("========================");
                writer.WriteLine($"Generated: {System.DateTime.Now}");
                writer.WriteLine($"Recording Duration: {stats.RecordingDuration:F1} seconds");
                writer.WriteLine();
                
                writer.WriteLine("FPS Statistics:");
                writer.WriteLine($"  Average: {stats.AverageFPS:F1}");
                writer.WriteLine($"  Minimum: {stats.MinFPS:F1}");
                writer.WriteLine($"  Maximum: {stats.MaxFPS:F1}");
                writer.WriteLine();
                
                writer.WriteLine("Update Time Statistics:");
                writer.WriteLine($"  Average: {stats.AverageUpdateTime:F2}ms");
                writer.WriteLine($"  Peak: {stats.PeakUpdateTime:F2}ms");
                writer.WriteLine();
                
                writer.WriteLine($"Peak Memory Usage: {stats.PeakMemoryUsage:F1}MB");
                writer.WriteLine();
                
                if (stats.Bottlenecks.Count > 0)
                {
                    writer.WriteLine("Identified Bottlenecks:");
                    foreach (var bottleneck in stats.Bottlenecks)
                    {
                        writer.WriteLine($"  - {bottleneck}");
                    }
                }
                else
                {
                    writer.WriteLine("No significant bottlenecks detected.");
                }
            }
        }
    }
    
    public struct ProfilerStats
    {
        public float RecordingDuration;
        public float AverageFPS;
        public float MinFPS;
        public float MaxFPS;
        public float AverageUpdateTime;
        public float PeakUpdateTime;
        public float PeakMemoryUsage;
        public List<string> Bottlenecks;
    }
    
    public struct OptimizationSuggestion
    {
        public string Title;
        public string Description;
        public OptimizationPriority Priority;
        public bool CanAutoApply;
        public string ExpectedImprovement;
    }
    
    public enum OptimizationPriority
    {
        Low,
        Medium,
        High
    }
}