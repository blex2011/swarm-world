using System.Collections.Generic;
using UnityEngine;

namespace SwarmAI.Performance
{
    /// <summary>
    /// Interface for spatial partitioning systems used to optimize
    /// neighbor queries in large swarm systems.
    /// </summary>
    public interface ISpatialPartitioning
    {
        /// <summary>
        /// Total number of objects currently in the partitioning system
        /// </summary>
        int ObjectCount { get; }
        
        /// <summary>
        /// Current bounds of the partitioning system
        /// </summary>
        Bounds WorldBounds { get; set; }
        
        /// <summary>
        /// Whether the partitioning system is currently active
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// Performance metrics for the partitioning system
        /// </summary>
        SpatialMetrics Metrics { get; }
        
        /// <summary>
        /// Initialize the spatial partitioning system
        /// </summary>
        /// <param name="worldBounds">World space bounds</param>
        /// <param name="config">Configuration parameters</param>
        void Initialize(Bounds worldBounds, SpatialConfig config);
        
        /// <summary>
        /// Add an agent to the spatial partitioning system
        /// </summary>
        /// <param name="agent">Agent to add</param>
        void AddAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Remove an agent from the spatial partitioning system
        /// </summary>
        /// <param name="agent">Agent to remove</param>
        void RemoveAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Update an agent's position in the partitioning system
        /// </summary>
        /// <param name="agent">Agent to update</param>
        void UpdateAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Get all agents within a spherical area
        /// </summary>
        /// <param name="center">Center position</param>
        /// <param name="radius">Search radius</param>
        /// <returns>List of agents within the area</returns>
        List<ISwarmAgent> GetAgentsInArea(Vector3 center, float radius);
        
        /// <summary>
        /// Get neighbors for a specific agent
        /// </summary>
        /// <param name="agent">Target agent</param>
        /// <param name="radius">Search radius</param>
        /// <returns>List of neighboring agents</returns>
        List<ISwarmAgent> GetNeighbors(ISwarmAgent agent, float radius);
        
        /// <summary>
        /// Get all agents within a bounding box
        /// </summary>
        /// <param name="bounds">Bounding box</param>
        /// <returns>List of agents within bounds</returns>
        List<ISwarmAgent> GetAgentsInBounds(Bounds bounds);
        
        /// <summary>
        /// Clear all agents from the partitioning system
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Rebuild the entire partitioning structure
        /// </summary>
        void Rebuild();
        
        /// <summary>
        /// Optimize the partitioning structure for current agent distribution
        /// </summary>
        void Optimize();
        
        /// <summary>
        /// Get debug visualization data
        /// </summary>
        /// <returns>Debug visualization information</returns>
        SpatialDebugInfo GetDebugInfo();
        
        /// <summary>
        /// Cleanup and dispose of resources
        /// </summary>
        void Cleanup();
    }
    
    /// <summary>
    /// Configuration for spatial partitioning systems
    /// </summary>
    [System.Serializable]
    public class SpatialConfig
    {
        [Header("Grid Settings")]
        public float cellSize = 10f;
        public int maxObjectsPerCell = 20;
        public bool autoResize = true;
        
        [Header("Octree Settings")]
        public int maxDepth = 8;
        public int maxObjectsPerNode = 15;
        public float minNodeSize = 1f;
        
        [Header("Performance")]
        public bool enableCaching = true;
        public float cacheValidTime = 0.1f;
        public bool enableAsyncUpdate = false;
        
        [Header("Optimization")]
        public bool enableAutoOptimization = true;
        public float optimizationInterval = 5f;
        public float densityThreshold = 0.8f;
    }
    
    /// <summary>
    /// Performance metrics for spatial partitioning
    /// </summary>
    public class SpatialMetrics
    {
        /// <summary>Average time for neighbor queries in milliseconds</summary>
        public float AverageQueryTime { get; set; }
        
        /// <summary>Total number of queries processed</summary>
        public int TotalQueries { get; set; }
        
        /// <summary>Cache hit ratio (0-1)</summary>
        public float CacheHitRatio { get; set; }
        
        /// <summary>Memory usage in MB</summary>
        public float MemoryUsage { get; set; }
        
        /// <summary>Number of active partitions/cells</summary>
        public int ActivePartitions { get; set; }
        
        /// <summary>Average objects per partition</summary>
        public float AverageObjectsPerPartition { get; set; }
        
        /// <summary>Last optimization timestamp</summary>
        public System.DateTime LastOptimization { get; set; }
    }
    
    /// <summary>
    /// Debug visualization information for spatial partitioning
    /// </summary>
    public class SpatialDebugInfo
    {
        /// <summary>Partition boundaries to visualize</summary>
        public List<Bounds> PartitionBounds { get; set; }
        
        /// <summary>Object distribution per partition</summary>
        public Dictionary<int, int> ObjectCounts { get; set; }
        
        /// <summary>Query hotspots</summary>
        public List<Vector3> QueryHotspots { get; set; }
        
        /// <summary>Performance bottleneck areas</summary>
        public List<Bounds> BottleneckAreas { get; set; }
        
        public SpatialDebugInfo()
        {
            PartitionBounds = new List<Bounds>();
            ObjectCounts = new Dictionary<int, int>();
            QueryHotspots = new List<Vector3>();
            BottleneckAreas = new List<Bounds>();
        }
    }
    
    /// <summary>
    /// Types of spatial partitioning algorithms
    /// </summary>
    public enum SpatialPartitionType
    {
        /// <summary>No partitioning - brute force O(n²)</summary>
        None,
        /// <summary>Regular grid partitioning</summary>
        Grid,
        /// <summary>Octree partitioning for 3D space</summary>
        Octree,
        /// <summary>Quadtree partitioning for 2D space</summary>
        Quadtree,
        /// <summary>Hierarchical grid with multiple levels</summary>
        HierarchicalGrid,
        /// <summary>Adaptive partitioning based on density</summary>
        Adaptive
    }
}

namespace SwarmAI.Performance
{
    /// <summary>
    /// Interface for Level of Detail (LOD) management systems
    /// </summary>
    public interface ILODSystem
    {
        /// <summary>
        /// Whether LOD system is currently active
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// Current LOD configuration
        /// </summary>
        LODConfig Configuration { get; set; }
        
        /// <summary>
        /// Performance metrics for LOD system
        /// </summary>
        LODMetrics Metrics { get; }
        
        /// <summary>
        /// Initialize the LOD system
        /// </summary>
        /// <param name="config">LOD configuration</param>
        void Initialize(LODConfig config);
        
        /// <summary>
        /// Register an agent with the LOD system
        /// </summary>
        /// <param name="agent">Agent to register</param>
        void RegisterAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Unregister an agent from the LOD system
        /// </summary>
        /// <param name="agent">Agent to unregister</param>
        void UnregisterAgent(ISwarmAgent agent);
        
        /// <summary>
        /// Update LOD levels for all registered agents
        /// </summary>
        /// <param name="viewerPosition">Current viewer/camera position</param>
        void UpdateLODLevels(Vector3 viewerPosition);
        
        /// <summary>
        /// Get optimal LOD level for an agent
        /// </summary>
        /// <param name="agent">Target agent</param>
        /// <param name="viewerPosition">Current viewer position</param>
        /// <returns>Recommended LOD level</returns>
        LODLevel GetOptimalLODLevel(ISwarmAgent agent, Vector3 viewerPosition);
        
        /// <summary>
        /// Set importance multiplier for an agent
        /// </summary>
        /// <param name="agent">Target agent</param>
        /// <param name="importance">Importance multiplier (0-10)</param>
        void SetAgentImportance(ISwarmAgent agent, float importance);
        
        /// <summary>
        /// Cleanup and dispose of resources
        /// </summary>
        void Cleanup();
    }
    
    /// <summary>
    /// Configuration for LOD system
    /// </summary>
    [System.Serializable]
    public class LODConfig
    {
        [Header("Distance Thresholds")]
        public float highDetailDistance = 30f;
        public float mediumDetailDistance = 60f;
        public float lowDetailDistance = 100f;
        public float cullingDistance = 200f;
        
        [Header("Update Frequencies (Hz)")]
        public float highDetailFrequency = 60f;
        public float mediumDetailFrequency = 30f;
        public float lowDetailFrequency = 15f;
        public float minimalDetailFrequency = 5f;
        
        [Header("Performance Scaling")]
        public bool enableAdaptiveScaling = true;
        public float targetFrameRate = 60f;
        public float scalingFactor = 1.2f;
        
        [Header("Importance Weighting")]
        public bool useImportanceWeighting = true;
        public float leaderImportance = 3f;
        public float specialAgentImportance = 2f;
    }
    
    /// <summary>
    /// Performance metrics for LOD system
    /// </summary>
    public class LODMetrics
    {
        public int HighDetailAgents { get; set; }
        public int MediumDetailAgents { get; set; }
        public int LowDetailAgents { get; set; }
        public int MinimalDetailAgents { get; set; }
        public int CulledAgents { get; set; }
        
        public float AverageUpdateTime { get; set; }
        public float MemorySaved { get; set; }
        public float PerformanceGain { get; set; }
        
        public System.DateTime LastUpdate { get; set; }
    }
}