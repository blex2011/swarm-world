using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace SwarmWorld.Editor
{
    /// <summary>
    /// Custom inspector for SwarmAgent components with advanced monitoring and debugging
    /// </summary>
    [CustomEditor(typeof(SwarmAgent), true)]
    [CanEditMultipleObjects]
    public class AgentInspector : Editor
    {
        private SwarmAgent agent;
        private bool showNeighbors = true;
        private bool showBehaviorDetails = true;
        private bool showPerformanceInfo = false;
        private bool showDebugVisuals = true;
        
        // Real-time data
        private Vector3 lastPosition;
        private float speed;
        private float acceleration;
        private int neighborCount;
        private List<SwarmAgent> cachedNeighbors = new List<SwarmAgent>();
        
        // UI State
        private Vector2 neighborsScrollPos;
        private GUIStyle headerStyle;
        private GUIStyle valueStyle;
        private GUIStyle warningStyle;
        
        // Performance tracking
        private float lastUpdateTime;
        private float updateInterval = 0.1f;
        
        void OnEnable()
        {
            agent = target as SwarmAgent;
            lastPosition = agent != null ? agent.transform.position : Vector3.zero;
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
                    fontSize = 12,
                    normal = { textColor = Color.cyan }
                };
            }
            
            if (valueStyle == null)
            {
                valueStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.white },
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (warningStyle == null)
            {
                warningStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.red },
                    fontStyle = FontStyle.Bold
                };
            }
        }
        
        void OnEditorUpdate()
        {
            if (agent == null || !Application.isPlaying) return;
            
            if (Time.time - lastUpdateTime > updateInterval)
            {
                UpdateRealTimeData();
                lastUpdateTime = Time.time;
                Repaint();
            }
        }
        
        void UpdateRealTimeData()
        {
            Vector3 currentPosition = agent.transform.position;
            Vector3 deltaPosition = currentPosition - lastPosition;
            
            speed = deltaPosition.magnitude / updateInterval;
            acceleration = (speed - (agent.Velocity?.magnitude ?? 0f)) / updateInterval;
            
            lastPosition = currentPosition;
            
            // Update neighbor data
            if (agent.Manager != null)
            {
                cachedNeighbors = agent.Manager.GetNeighbors(agent, agent.PerceptionRadius);
                neighborCount = cachedNeighbors.Count;
            }
        }
        
        public override void OnInspectorGUI()
        {
            InitializeStyles();
            
            EditorGUILayout.Space();
            DrawHeader();
            
            if (Application.isPlaying)
            {
                DrawRealTimeStatus();
            }
            
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            DrawBehaviorSection();
            
            EditorGUILayout.Space();
            DrawNeighborsSection();
            
            EditorGUILayout.Space();
            DrawDebugSection();
            
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                DrawPerformanceSection();
            }
        }
        
        void DrawHeader()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"🤖 Agent #{agent.AgentId}", headerStyle);
            GUILayout.FlexibleSpace();
            
            if (Application.isPlaying)
            {
                string status = agent.isActiveAndEnabled ? "🟢 Active" : "🔴 Inactive";
                EditorGUILayout.LabelField(status, valueStyle);
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (agent.Manager != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Swarm:", GUILayout.Width(60));
                EditorGUILayout.ObjectField(agent.Manager, typeof(SwarmManager), true);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawRealTimeStatus()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("📊 Real-Time Status", headerStyle);
            
            // Position and movement
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Position:", GUILayout.Width(70));
            EditorGUILayout.LabelField(agent.transform.position.ToString("F2"), valueStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Velocity:", GUILayout.Width(70));
            Vector3 velocity = agent.Velocity ?? Vector3.zero;
            EditorGUILayout.LabelField($"{velocity.ToString("F2")} (Mag: {velocity.magnitude:F2})", valueStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Speed:", GUILayout.Width(70));
            EditorGUILayout.LabelField($"{speed:F2} u/s", valueStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Neighbors:", GUILayout.Width(70));
            
            GUIStyle neighborStyle = neighborCount > 20 ? warningStyle : valueStyle;
            EditorGUILayout.LabelField(neighborCount.ToString(), neighborStyle);
            
            if (neighborCount > 20)
            {
                EditorGUILayout.LabelField("⚠️ High neighbor count", warningStyle);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Health indicators
            if (speed > agent.MaxSpeed * 1.1f)
            {
                EditorGUILayout.LabelField("⚠️ Speed exceeds maximum", warningStyle);
            }
            
            if (neighborCount == 0 && agent.Manager != null && agent.Manager.AgentCount > 1)
            {
                EditorGUILayout.LabelField("⚠️ No neighbors detected", warningStyle);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawBehaviorSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            showBehaviorDetails = EditorGUILayout.Foldout(showBehaviorDetails, "🧠 Behavior Analysis", true);
            GUILayout.FlexibleSpace();
            
            if (Application.isPlaying && GUILayout.Button("Reset Behavior", GUILayout.Width(100)))
            {
                agent.ResetBehavior();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (showBehaviorDetails)
            {
                EditorGUI.indentLevel++;
                
                // Behavior weights
                EditorGUILayout.LabelField("Behavior Weights:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Separation:", GUILayout.Width(80));
                agent.SeparationWeight = EditorGUILayout.Slider(agent.SeparationWeight, 0f, 5f);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Alignment:", GUILayout.Width(80));
                agent.AlignmentWeight = EditorGUILayout.Slider(agent.AlignmentWeight, 0f, 5f);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Cohesion:", GUILayout.Width(80));
                agent.CohesionWeight = EditorGUILayout.Slider(agent.CohesionWeight, 0f, 5f);
                EditorGUILayout.EndHorizontal();
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.Space();
                    
                    // Force calculations
                    Vector3 separation = agent.GetSeparationForce();
                    Vector3 alignment = agent.GetAlignmentForce();
                    Vector3 cohesion = agent.GetCohesionForce();
                    Vector3 total = separation + alignment + cohesion;
                    
                    EditorGUILayout.LabelField("Current Forces:", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Separation: {separation.ToString("F2")}");
                    EditorGUILayout.LabelField($"Alignment: {alignment.ToString("F2")}");
                    EditorGUILayout.LabelField($"Cohesion: {cohesion.ToString("F2")}");
                    EditorGUILayout.LabelField($"Total: {total.ToString("F2")}", valueStyle);
                    
                    // Force magnitude bars
                    DrawForceBar("Sep", separation.magnitude, 5f);
                    DrawForceBar("Ali", alignment.magnitude, 5f);
                    DrawForceBar("Coh", cohesion.magnitude, 5f);
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawNeighborsSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            showNeighbors = EditorGUILayout.Foldout(showNeighbors, $"👥 Neighbors ({neighborCount})", true);
            GUILayout.FlexibleSpace();
            
            if (Application.isPlaying && GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                UpdateRealTimeData();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (showNeighbors && Application.isPlaying)
            {
                EditorGUI.indentLevel++;
                
                if (cachedNeighbors.Count == 0)
                {
                    EditorGUILayout.LabelField("No neighbors found", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    // Neighbor list with scroll
                    neighborsScrollPos = EditorGUILayout.BeginScrollView(neighborsScrollPos, GUILayout.MaxHeight(150));
                    
                    foreach (var neighbor in cachedNeighbors.Take(10)) // Limit to 10 for performance
                    {
                        if (neighbor == null) continue;
                        
                        EditorGUILayout.BeginHorizontal(GUI.skin.box);
                        
                        float distance = Vector3.Distance(agent.transform.position, neighbor.transform.position);
                        
                        EditorGUILayout.LabelField($"Agent {neighbor.AgentId}", GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Dist: {distance:F1}", GUILayout.Width(60));
                        
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            Selection.activeGameObject = neighbor.gameObject;
                        }
                        
                        if (GUILayout.Button("Focus", GUILayout.Width(50)))
                        {
                            SceneView.FrameLastActiveSceneView();
                            Selection.activeGameObject = neighbor.gameObject;
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    if (cachedNeighbors.Count > 10)
                    {
                        EditorGUILayout.LabelField($"... and {cachedNeighbors.Count - 10} more neighbors");
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawDebugSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            showDebugVisuals = EditorGUILayout.Foldout(showDebugVisuals, "🔍 Debug & Visualization", true);
            EditorGUILayout.EndHorizontal();
            
            if (showDebugVisuals)
            {
                EditorGUI.indentLevel++;
                
                agent.ShowDebugInfo = EditorGUILayout.Toggle("Show Debug Info", agent.ShowDebugInfo);
                agent.ShowVelocityVector = EditorGUILayout.Toggle("Show Velocity Vector", agent.ShowVelocityVector);
                agent.ShowPerceptionRadius = EditorGUILayout.Toggle("Show Perception Radius", agent.ShowPerceptionRadius);
                agent.ShowNeighborConnections = EditorGUILayout.Toggle("Show Neighbor Lines", agent.ShowNeighborConnections);
                agent.ShowForceVectors = EditorGUILayout.Toggle("Show Force Vectors", agent.ShowForceVectors);
                
                EditorGUILayout.Space();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Reset Position"))
                {
                    agent.transform.position = Vector3.zero;
                }
                
                if (GUILayout.Button("Randomize Position"))
                {
                    agent.transform.position = Random.insideUnitSphere * 10f;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Stop Agent"))
                {
                    agent.Velocity = Vector3.zero;
                }
                
                if (GUILayout.Button("Boost Speed"))
                {
                    agent.Velocity = agent.Velocity.normalized * agent.MaxSpeed;
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawPerformanceSection()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            EditorGUILayout.BeginHorizontal();
            showPerformanceInfo = EditorGUILayout.Foldout(showPerformanceInfo, "⚡ Performance Info", true);
            EditorGUILayout.EndHorizontal();
            
            if (showPerformanceInfo)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField($"Update Time: {agent.LastUpdateTime:F3}ms");
                EditorGUILayout.LabelField($"Behavior Calc Time: {agent.BehaviorCalculationTime:F3}ms");
                EditorGUILayout.LabelField($"Neighbor Search Time: {agent.NeighborSearchTime:F3}ms");
                
                EditorGUILayout.Space();
                
                // Performance recommendations
                if (agent.LastUpdateTime > 0.5f)
                {
                    EditorGUILayout.LabelField("⚠️ High update time detected", warningStyle);
                    EditorGUILayout.LabelField("  Consider reducing perception radius or neighbor count");
                }
                
                if (neighborCount > 30)
                {
                    EditorGUILayout.LabelField("⚠️ Too many neighbors", warningStyle);
                    EditorGUILayout.LabelField("  Consider using spatial partitioning");
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        void DrawForceBar(string label, float value, float maxValue)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(30));
            
            Rect barRect = GUILayoutUtility.GetRect(0, 12, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(barRect, Color.grey);
            
            float normalizedValue = Mathf.Clamp01(value / maxValue);
            Rect fillRect = new Rect(barRect.x, barRect.y, barRect.width * normalizedValue, barRect.height);
            
            Color barColor = normalizedValue < 0.7f ? Color.green : normalizedValue < 0.9f ? Color.yellow : Color.red;
            EditorGUI.DrawRect(fillRect, barColor);
            
            GUILayout.Label($"{value:F2}", GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
        }
        
        void OnSceneGUI()
        {
            if (!Application.isPlaying || !agent.ShowDebugInfo) return;
            
            Vector3 position = agent.transform.position;
            
            // Draw perception radius
            if (agent.ShowPerceptionRadius)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(position, Vector3.up, agent.PerceptionRadius);
            }
            
            // Draw velocity vector
            if (agent.ShowVelocityVector && agent.Velocity.magnitude > 0.1f)
            {
                Handles.color = Color.green;
                Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(agent.Velocity), 2f, EventType.Repaint);
            }
            
            // Draw neighbor connections
            if (agent.ShowNeighborConnections)
            {
                Handles.color = Color.cyan;
                foreach (var neighbor in cachedNeighbors)
                {
                    if (neighbor != null)
                    {
                        Handles.DrawLine(position, neighbor.transform.position);
                    }
                }
            }
            
            // Draw force vectors
            if (agent.ShowForceVectors)
            {
                Handles.color = Color.red;
                Vector3 separationForce = agent.GetSeparationForce();
                if (separationForce.magnitude > 0.1f)
                {
                    Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(separationForce), 1f, EventType.Repaint);
                }
                
                Handles.color = Color.blue;
                Vector3 alignmentForce = agent.GetAlignmentForce();
                if (alignmentForce.magnitude > 0.1f)
                {
                    Handles.ArrowHandleCap(0, position + Vector3.up * 0.5f, Quaternion.LookRotation(alignmentForce), 1f, EventType.Repaint);
                }
                
                Handles.color = Color.magenta;
                Vector3 cohesionForce = agent.GetCohesionForce();
                if (cohesionForce.magnitude > 0.1f)
                {
                    Handles.ArrowHandleCap(0, position + Vector3.up * 1f, Quaternion.LookRotation(cohesionForce), 1f, EventType.Repaint);
                }
            }
            
            // Draw agent info
            Handles.Label(position + Vector3.up * 2f, $"Agent {agent.AgentId}\nSpeed: {speed:F1}\nNeighbors: {neighborCount}");
        }
    }
    
    /// <summary>
    /// Property drawer for behavior weights to provide sliders in the inspector
    /// </summary>
    [CustomPropertyDrawer(typeof(BehaviorWeights))]
    public class BehaviorWeightsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            var separationProp = property.FindPropertyRelative("separation");
            var alignmentProp = property.FindPropertyRelative("alignment");
            var cohesionProp = property.FindPropertyRelative("cohesion");
            
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            
            Rect separationRect = new Rect(position.x, position.y, position.width, lineHeight);
            Rect alignmentRect = new Rect(position.x, position.y + lineHeight + spacing, position.width, lineHeight);
            Rect cohesionRect = new Rect(position.x, position.y + (lineHeight + spacing) * 2, position.width, lineHeight);
            
            separationProp.floatValue = EditorGUI.Slider(separationRect, "Separation", separationProp.floatValue, 0f, 5f);
            alignmentProp.floatValue = EditorGUI.Slider(alignmentRect, "Alignment", alignmentProp.floatValue, 0f, 5f);
            cohesionProp.floatValue = EditorGUI.Slider(cohesionRect, "Cohesion", cohesionProp.floatValue, 0f, 5f);
            
            EditorGUI.EndProperty();
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 3 + EditorGUIUtility.standardVerticalSpacing * 2;
        }
    }
}