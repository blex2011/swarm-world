using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SwarmWorld
{
    /// <summary>
    /// Central coordinator for managing swarm agents and global behavior
    /// </summary>
    public class SwarmCoordinator : MonoBehaviour
    {
        [Header("Coordination Configuration")]
        [SerializeField] private int maxAgents = 1000;
        [SerializeField] private string neighborFindingStrategy = "SpatialHash";
        [SerializeField] private float globalTargetUpdateInterval = 0.1f;
        [SerializeField] private bool enablePerformanceOptimizations = true;

        [Header("Global Targets")]
        [SerializeField] private Transform globalTargetTransform;
        [SerializeField] private float3 globalTargetPosition;
        [SerializeField] private bool useTransformAsTarget = true;

        [Header("Spatial Partitioning")]
        [SerializeField] private float cellSize = 16f;
        [SerializeField] private int3 gridDimensions = new int3(64, 16, 64);

        // Agent management
        private List<SwarmAgent> registeredAgents = new List<SwarmAgent>();
        private Dictionary<string, SwarmAgent> agentLookup = new Dictionary<string, SwarmAgent>();

        // Spatial partitioning
        private NativeMultiHashMap<int, int> spatialHashMap;
        private NativeArray<float3> agentPositions;
        private NativeArray<int> agentCellKeys;
        private bool isInitialized = false;

        // Performance tracking
        private float lastGlobalUpdate;
        private int framesSinceLastUpdate;

        public int MaxAgents => maxAgents;
        public bool HasGlobalTarget => globalTargetTransform != null || !globalTargetPosition.Equals(float3.zero);
        public float3 GlobalTarget => useTransformAsTarget && globalTargetTransform != null 
            ? globalTargetTransform.position 
            : globalTargetPosition;

        private void Awake()
        {
            InitializeCoordinator();
        }

        private void Update()
        {
            UpdateGlobalTarget();
            UpdatePerformanceMetrics();
        }

        private void OnDestroy()
        {
            CleanupCoordinator();
        }

        private void InitializeCoordinator()
        {
            // Initialize native arrays for spatial partitioning
            spatialHashMap = new NativeMultiHashMap<int, int>(maxAgents * 4, Allocator.Persistent);
            agentPositions = new NativeArray<float3>(maxAgents, Allocator.Persistent);
            agentCellKeys = new NativeArray<int>(maxAgents, Allocator.Persistent);

            isInitialized = true;
        }

        private void UpdateGlobalTarget()
        {
            if (Time.time - lastGlobalUpdate >= globalTargetUpdateInterval)
            {
                if (useTransformAsTarget && globalTargetTransform != null)
                {
                    globalTargetPosition = globalTargetTransform.position;
                }

                lastGlobalUpdate = Time.time;
            }
        }

        private void UpdatePerformanceMetrics()
        {
            framesSinceLastUpdate++;
        }

        public void RegisterAgent(SwarmAgent agent)
        {
            if (agent == null || agentLookup.ContainsKey(agent.AgentId))
                return;

            if (registeredAgents.Count >= maxAgents)
            {
                Debug.LogWarning($"Cannot register agent {agent.AgentId}: Maximum agent count reached ({maxAgents})");
                return;
            }

            registeredAgents.Add(agent);
            agentLookup[agent.AgentId] = agent;
        }

        public void UnregisterAgent(SwarmAgent agent)
        {
            if (agent == null)
                return;

            registeredAgents.Remove(agent);
            agentLookup.Remove(agent.AgentId);
        }

        public void FindNeighbors(SwarmAgent agent, ref NativeArray<SwarmNeighbor> neighbors)
        {
            if (!isInitialized || agent == null)
                return;

            switch (neighborFindingStrategy)
            {
                case "BruteForce":
                    FindNeighborsBruteForce(agent, ref neighbors);
                    break;
                case "SpatialHash":
                    FindNeighborsSpatialHash(agent, ref neighbors);
                    break;
                case "Octree":
                    FindNeighborsOctree(agent, ref neighbors);
                    break;
                default:
                    FindNeighborsSpatialHash(agent, ref neighbors);
                    break;
            }
        }

        private void FindNeighborsBruteForce(SwarmAgent agent, ref NativeArray<SwarmNeighbor> neighbors)
        {
            var agentPos = agent.Position;
            var perceptionRadius = agent.Data.perceptionRadius;
            var neighborIndex = 0;

            for (int i = 0; i < registeredAgents.Count && neighborIndex < neighbors.Length; i++)
            {
                var otherAgent = registeredAgents[i];
                if (otherAgent == agent || otherAgent == null)
                    continue;

                var distance = math.distance(agentPos, otherAgent.Position);
                if (distance <= perceptionRadius)
                {
                    neighbors[neighborIndex] = new SwarmNeighbor(
                        otherAgent.AgentId,
                        otherAgent.Position,
                        otherAgent.Velocity,
                        distance
                    );
                    neighborIndex++;
                }
            }

            // Mark remaining slots as invalid
            for (int i = neighborIndex; i < neighbors.Length; i++)
            {
                neighbors[i] = SwarmNeighbor.Invalid;
            }
        }

        private void FindNeighborsSpatialHash(SwarmAgent agent, ref NativeArray<SwarmNeighbor> neighbors)
        {
            if (!spatialHashMap.IsCreated)
                return;

            var agentPos = agent.Position;
            var perceptionRadius = agent.Data.perceptionRadius;
            var cellKey = GetSpatialHash(agentPos);
            var neighborIndex = 0;

            // Check surrounding cells
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        var neighborCellKey = cellKey + dx + dy * gridDimensions.x + dz * gridDimensions.x * gridDimensions.y;
                        
                        if (spatialHashMap.TryGetFirstValue(neighborCellKey, out int agentIndex, out var iterator))
                        {
                            do
                            {
                                if (agentIndex < registeredAgents.Count && neighborIndex < neighbors.Length)
                                {
                                    var otherAgent = registeredAgents[agentIndex];
                                    if (otherAgent == agent || otherAgent == null)
                                        continue;

                                    var distance = math.distance(agentPos, otherAgent.Position);
                                    if (distance <= perceptionRadius)
                                    {
                                        neighbors[neighborIndex] = new SwarmNeighbor(
                                            otherAgent.AgentId,
                                            otherAgent.Position,
                                            otherAgent.Velocity,
                                            distance
                                        );
                                        neighborIndex++;
                                    }
                                }
                            }
                            while (spatialHashMap.TryGetNextValue(out agentIndex, ref iterator) && neighborIndex < neighbors.Length);
                        }
                    }
                }
            }

            // Mark remaining slots as invalid
            for (int i = neighborIndex; i < neighbors.Length; i++)
            {
                neighbors[i] = SwarmNeighbor.Invalid;
            }
        }

        private void FindNeighborsOctree(SwarmAgent agent, ref NativeArray<SwarmNeighbor> neighbors)
        {
            // Simplified octree implementation for testing
            // In production, this would use a proper octree data structure
            FindNeighborsBruteForce(agent, ref neighbors);
        }

        public void UpdateAllNeighbors()
        {
            if (!isInitialized)
                return;

            // Update spatial hash map
            spatialHashMap.Clear();

            for (int i = 0; i < registeredAgents.Count; i++)
            {
                var agent = registeredAgents[i];
                if (agent != null)
                {
                    var position = agent.Position;
                    var cellKey = GetSpatialHash(position);
                    
                    agentPositions[i] = position;
                    agentCellKeys[i] = cellKey;
                    spatialHashMap.Add(cellKey, i);
                }
            }
        }

        public void BatchUpdateAgents(List<SwarmAgent> agents)
        {
            // Update spatial partitioning first
            UpdateAllNeighbors();

            // Batch process all agents
            foreach (var agent in agents)
            {
                if (agent != null)
                {
                    agent.ForceUpdate();
                }
            }
        }

        public void SetNeighborFindingStrategy(string strategy)
        {
            neighborFindingStrategy = strategy;
        }

        public void SetGlobalTarget(Vector3 target)
        {
            globalTargetPosition = target;
            useTransformAsTarget = false;
        }

        public void SetGlobalTarget(Transform target)
        {
            globalTargetTransform = target;
            useTransformAsTarget = true;
        }

        private int GetSpatialHash(float3 position)
        {
            var cellPos = new int3(
                (int)math.floor(position.x / cellSize),
                (int)math.floor(position.y / cellSize),
                (int)math.floor(position.z / cellSize)
            );

            // Clamp to grid bounds
            cellPos = math.clamp(cellPos, int3.zero, gridDimensions - 1);

            return cellPos.x + cellPos.y * gridDimensions.x + cellPos.z * gridDimensions.x * gridDimensions.y;
        }

        private void CleanupCoordinator()
        {
            if (spatialHashMap.IsCreated)
                spatialHashMap.Dispose();
            
            if (agentPositions.IsCreated)
                agentPositions.Dispose();
            
            if (agentCellKeys.IsCreated)
                agentCellKeys.Dispose();
        }

        // Public API for testing and monitoring
        public int GetRegisteredAgentCount()
        {
            return registeredAgents.Count;
        }

        public SwarmAgent GetAgent(string agentId)
        {
            return agentLookup.TryGetValue(agentId, out var agent) ? agent : null;
        }

        public List<SwarmAgent> GetAllAgents()
        {
            return new List<SwarmAgent>(registeredAgents);
        }

        public CoordinatorStats GetStats()
        {
            return new CoordinatorStats
            {
                registeredAgents = registeredAgents.Count,
                maxAgents = maxAgents,
                neighborFindingStrategy = neighborFindingStrategy,
                averageFPS = framesSinceLastUpdate / (Time.time - lastGlobalUpdate),
                hasGlobalTarget = HasGlobalTarget,
                globalTarget = GlobalTarget
            };
        }

        private void OnDrawGizmos()
        {
            // Draw global target
            if (HasGlobalTarget)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(GlobalTarget, 2f);
                Gizmos.DrawLine(transform.position, GlobalTarget);
            }

            // Draw coordinator bounds
            Gizmos.color = Color.blue;
            var bounds = new Vector3(gridDimensions.x * cellSize, gridDimensions.y * cellSize, gridDimensions.z * cellSize);
            Gizmos.DrawWireCube(transform.position, bounds);
        }
    }

    [System.Serializable]
    public struct CoordinatorStats
    {
        public int registeredAgents;
        public int maxAgents;
        public string neighborFindingStrategy;
        public float averageFPS;
        public bool hasGlobalTarget;
        public float3 globalTarget;

        public override string ToString()
        {
            return $"Agents: {registeredAgents}/{maxAgents}, Strategy: {neighborFindingStrategy}, FPS: {averageFPS:F1}";
        }
    }
}