using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Core
{
    /// <summary>
    /// Core interface for swarm behaviors, enabling modular and extensible
    /// behavior systems for agents within a swarm.
    /// </summary>
    public interface ISwarmBehavior
    {
        /// <summary>
        /// Unique name identifier for this behavior
        /// </summary>
        string BehaviorName { get; }
        
        /// <summary>
        /// Weight/influence of this behavior in force calculations
        /// </summary>
        float Weight { get; set; }
        
        /// <summary>
        /// Whether this behavior is currently active
        /// </summary>
        bool Enabled { get; set; }
        
        /// <summary>
        /// Priority level for behavior execution order
        /// </summary>
        int Priority { get; set; }
        
        /// <summary>
        /// Behavior category for organization and filtering
        /// </summary>
        BehaviorCategory Category { get; }
        
        /// <summary>
        /// Calculate the force vector for an agent based on this behavior
        /// </summary>
        /// <param name="agent">Target agent</param>
        /// <param name="context">Swarm context information</param>
        /// <returns>Force vector to apply to agent</returns>
        Vector3 CalculateForce(ISwarmAgent agent, SwarmContext context);
        
        /// <summary>
        /// Initialize behavior with configuration
        /// </summary>
        /// <param name="config">Behavior configuration</param>
        void Initialize(BehaviorConfig config);
        
        /// <summary>
        /// Called when behavior is added to an agent
        /// </summary>
        /// <param name="agent">Target agent</param>
        void OnBehaviorAdded(ISwarmAgent agent);
        
        /// <summary>
        /// Called when behavior is removed from an agent
        /// </summary>
        /// <param name="agent">Target agent</param>
        void OnBehaviorRemoved(ISwarmAgent agent);
        
        /// <summary>
        /// Update behavior state (called once per frame)
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        void UpdateBehavior(float deltaTime);
        
        /// <summary>
        /// Check if this behavior can execute for the given agent
        /// </summary>
        /// <param name="agent">Target agent</param>
        /// <param name="context">Swarm context</param>
        /// <returns>True if behavior can execute</returns>
        bool CanExecute(ISwarmAgent agent, SwarmContext context);
        
        /// <summary>
        /// Get debug information for visualization
        /// </summary>
        /// <param name="agent">Target agent</param>
        /// <returns>Debug visualization data</returns>
        BehaviorDebugInfo GetDebugInfo(ISwarmAgent agent);
    }
    
    /// <summary>
    /// Context information provided to behaviors during execution
    /// </summary>
    public class SwarmContext
    {
        /// <summary>Neighboring agents within perception radius</summary>
        public List<ISwarmAgent> Neighbors { get; set; }
        
        /// <summary>Current swarm manager</summary>
        public ISwarmManager Manager { get; set; }
        
        /// <summary>Global targets for the swarm</summary>
        public List<Transform> GlobalTargets { get; set; }
        
        /// <summary>Environmental obstacles</summary>
        public List<Collider> Obstacles { get; set; }
        
        /// <summary>Time elapsed since last update</summary>
        public float DeltaTime { get; set; }
        
        /// <summary>Frame number for temporal calculations</summary>
        public int FrameCount { get; set; }
        
        /// <summary>Current LOD level for the agent</summary>
        public LODLevel LODLevel { get; set; }
        
        /// <summary>Performance metrics for adaptive behavior</summary>
        public SwarmMetrics Metrics { get; set; }
        
        /// <summary>Claude Flow coordination data</summary>
        public CoordinationData CoordinationData { get; set; }
        
        /// <summary>Custom user data dictionary</summary>
        public Dictionary<string, object> CustomData { get; set; }
        
        public SwarmContext()
        {
            Neighbors = new List<ISwarmAgent>();
            GlobalTargets = new List<Transform>();
            Obstacles = new List<Collider>();
            CustomData = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Categories for organizing and filtering behaviors
    /// </summary>
    public enum BehaviorCategory
    {
        /// <summary>Basic movement behaviors (separation, alignment, cohesion)</summary>
        Movement = 0,
        /// <summary>Target seeking and avoidance behaviors</summary>
        Targeting = 1,
        /// <summary>Formation and tactical behaviors</summary>
        Formation = 2,
        /// <summary>Pathfinding and navigation behaviors</summary>
        Navigation = 3,
        /// <summary>Combat and defensive behaviors</summary>
        Combat = 4,
        /// <summary>Resource gathering and work behaviors</summary>
        Work = 5,
        /// <summary>Communication and coordination behaviors</summary>
        Communication = 6,
        /// <summary>Custom user-defined behaviors</summary>
        Custom = 999
    }
    
    /// <summary>
    /// Configuration data for behavior initialization
    /// </summary>
    [System.Serializable]
    public class BehaviorConfig
    {
        [Header("Basic Settings")]
        public float weight = 1.0f;
        public bool enabled = true;
        public int priority = 0;
        
        [Header("Performance")]
        public bool enableLODScaling = true;
        public float minWeight = 0.1f;
        public float maxWeight = 5.0f;
        
        [Header("Debug")]
        public bool enableDebugVisualization = false;
        public Color debugColor = Color.white;
        
        /// <summary>Custom parameters for behavior-specific configuration</summary>
        public Dictionary<string, object> CustomParameters { get; set; }
        
        public BehaviorConfig()
        {
            CustomParameters = new Dictionary<string, object>();
        }
        
        /// <summary>Get a custom parameter with type safety</summary>
        public T GetParameter<T>(string key, T defaultValue = default(T))
        {
            if (CustomParameters.ContainsKey(key) && CustomParameters[key] is T)
            {
                return (T)CustomParameters[key];
            }
            return defaultValue;
        }
        
        /// <summary>Set a custom parameter</summary>
        public void SetParameter<T>(string key, T value)
        {
            CustomParameters[key] = value;
        }
    }
    
    /// <summary>
    /// Debug information for behavior visualization
    /// </summary>
    public class BehaviorDebugInfo
    {
        /// <summary>Force vector being applied</summary>
        public Vector3 ForceVector { get; set; }
        
        /// <summary>Visualization color</summary>
        public Color Color { get; set; }
        
        /// <summary>Debug lines to draw</summary>
        public List<DebugLine> DebugLines { get; set; }
        
        /// <summary>Debug spheres to draw</summary>
        public List<DebugSphere> DebugSpheres { get; set; }
        
        /// <summary>Text information</summary>
        public string DebugText { get; set; }
        
        public BehaviorDebugInfo()
        {
            DebugLines = new List<DebugLine>();
            DebugSpheres = new List<DebugSphere>();
            Color = Color.white;
        }
    }
    
    /// <summary>
    /// Debug line for visualization
    /// </summary>
    public struct DebugLine
    {
        public Vector3 Start;
        public Vector3 End;
        public Color Color;
        public float Duration;
        
        public DebugLine(Vector3 start, Vector3 end, Color color, float duration = 0f)
        {
            Start = start;
            End = end;
            Color = color;
            Duration = duration;
        }
    }
    
    /// <summary>
    /// Debug sphere for visualization
    /// </summary>
    public struct DebugSphere
    {
        public Vector3 Center;
        public float Radius;
        public Color Color;
        public float Duration;
        
        public DebugSphere(Vector3 center, float radius, Color color, float duration = 0f)
        {
            Center = center;
            Radius = radius;
            Color = color;
            Duration = duration;
        }
    }
}