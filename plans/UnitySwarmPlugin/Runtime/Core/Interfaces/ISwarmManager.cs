using System.Collections.Generic;
using UnityEngine;

namespace SwarmAI.Core
{
    /// <summary>
    /// Core interface for swarm management systems, providing centralized
    /// control and coordination for swarm agents and behaviors.
    /// </summary>
    public interface ISwarmManager
    {
        /// <summary>
        /// Unique identifier for this swarm manager
        /// </summary>
        int SwarmId { get; }
        
        /// <summary>
        /// Current number of active agents in the swarm
        /// </summary>
        int AgentCount { get; }
        
        /// <summary>
        /// Maximum number of agents this manager can handle
        /// </summary>
        int MaxAgents { get; set; }
        
        /// <summary>
        /// Whether the swarm manager is currently active and updating
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// Current performance metrics for the swarm
        /// </summary>
        SwarmMetrics Metrics { get; }
        
        /// <summary>
        /// Spatial partitioning system for efficient neighbor queries
        /// </summary>
        ISpatialPartitioning SpatialSystem { get; }
        
        /// <summary>
        /// Level of detail management system
        /// </summary>
        ILODSystem LODSystem { get; }
        
        /// <summary>
        /// Claude Flow coordination interface
        /// </summary>
        ICoordinationSystem CoordinationSystem { get; }
        
        /// <summary>
        /// Initialize the swarm manager with configuration
        /// </summary>
        /// <param name="config">Manager configuration</param>
        void Initialize(SwarmManagerConfig config);
        
        /// <summary>
        /// Update all agents and systems for the current frame
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        void UpdateSwarm(float deltaTime);
        
        /// <summary>
        /// Add an agent to the swarm
        /// </summary>
        /// <param name="agent">Agent to add</param>
        /// <returns>Success status</returns>
        bool AddAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Remove an agent from the swarm
        /// </summary>
        /// <param name="agent">Agent to remove</param>
        /// <returns>Success status</returns>
        bool RemoveAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Get all active agents in the swarm
        /// </summary>
        /// <returns>List of active agents</returns>
        List<ISwarmAgent> GetAgents();
        
        /// <summary>
        /// Get agents within a specific area
        /// </summary>
        /// <param name="center">Center position</param>
        /// <param name="radius">Search radius</param>
        /// <returns>List of agents in area</returns>
        List<ISwarmAgent> GetAgentsInArea(Vector3 center, float radius);
        
        /// <summary>
        /// Get neighbors for a specific agent
        /// </summary>
        /// <param name="agent">Target agent</param>
        /// <param name="radius">Search radius</param>
        /// <returns>List of neighboring agents</returns>
        List<ISwarmAgent> GetNeighbors(ISwarmAgent agent, float radius);
        
        /// <summary>
        /// Spawn a new agent at the specified position
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="config">Agent configuration</param>
        /// <returns>Newly spawned agent</returns>
        ISwarmAgent SpawnAgent(Vector3 position, AgentConfig config);
        
        /// <summary>
        /// Set a global target for all agents
        /// </summary>
        /// <param name="target">Target position or transform</param>
        void SetGlobalTarget(Transform target);
        
        /// <summary>
        /// Add a behavior to all agents in the swarm
        /// </summary>
        /// <param name="behavior">Behavior to add</param>
        void AddGlobalBehavior(ISwarmBehavior behavior);
        
        /// <summary>
        /// Remove a behavior from all agents in the swarm
        /// </summary>
        /// <param name="behaviorType">Type of behavior to remove</param>
        void RemoveGlobalBehavior(System.Type behaviorType);
        
        /// <summary>
        /// Cleanup and dispose of all resources
        /// </summary>
        void Cleanup();
    }
    
    /// <summary>
    /// Configuration data for swarm manager initialization
    /// </summary>
    [System.Serializable]
    public class SwarmManagerConfig
    {
        [Header("Basic Configuration")]
        public int maxAgents = 1000;
        public bool useJobSystem = true;
        public bool useBurst = true;
        public bool useSpatialPartitioning = true;
        
        [Header("Performance Settings")]
        public PerformanceLevel performanceLevel = PerformanceLevel.Balanced;
        public int updateBatchSize = 50;
        public float updateInterval = 0.02f; // 50Hz
        
        [Header("LOD Configuration")]
        public bool enableLOD = true;
        public float cullingDistance = 200f;
        public float highDetailDistance = 30f;
        public float mediumDetailDistance = 60f;
        public float lowDetailDistance = 100f;
        
        [Header("Spatial Partitioning")]
        public SpatialPartitionType spatialType = SpatialPartitionType.Grid;
        public float cellSize = 10f;
        public int maxDepth = 6;
        
        [Header("Claude Flow Integration")]
        public bool enableClaudeFlow = false;
        public string coordinationEndpoint = "";
        public CoordinationLevel coordinationLevel = CoordinationLevel.Basic;
    }
    
    /// <summary>
    /// Performance level presets for automatic optimization
    /// </summary>
    public enum PerformanceLevel
    {
        /// <summary>Maximum quality, lower performance</summary>
        Quality = 0,
        /// <summary>Balanced quality and performance</summary>
        Balanced = 1,
        /// <summary>Maximum performance, lower quality</summary>
        Performance = 2,
        /// <summary>Extreme performance for massive swarms</summary>
        Extreme = 3
    }
    
    /// <summary>
    /// Spatial partitioning algorithm types
    /// </summary>
    public enum SpatialPartitionType
    {
        /// <summary>No spatial partitioning (brute force)</summary>
        None = 0,
        /// <summary>Regular grid partitioning</summary>
        Grid = 1,
        /// <summary>Octree partitioning for 3D</summary>
        Octree = 2,
        /// <summary>Hierarchical grids</summary>
        Hierarchical = 3
    }
    
    /// <summary>
    /// Claude Flow coordination sophistication levels
    /// </summary>
    public enum CoordinationLevel
    {
        /// <summary>Basic behavior coordination</summary>
        Basic = 0,
        /// <summary>Advanced decision making</summary>
        Advanced = 1,
        /// <summary>Full AI-powered coordination</summary>
        Full = 2
    }
}