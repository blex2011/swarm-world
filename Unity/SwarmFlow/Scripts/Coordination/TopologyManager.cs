using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SwarmFlow.Core;
using SwarmFlow.Agents;

namespace SwarmFlow.Coordination
{
    /// <summary>
    /// Manages swarm topology and optimizes agent connections for efficient coordination.
    /// Handles different topology patterns and dynamic optimization based on performance metrics.
    /// </summary>
    public class TopologyManager : MonoBehaviour
    {
        [Header("Topology Configuration")]
        [SerializeField] private SwarmTopology currentTopology = SwarmTopology.Hierarchical;
        [SerializeField] private float optimizationThreshold = 0.8f;
        [SerializeField] private bool enableDynamicOptimization = true;
        [SerializeField] private float topologyChangeDelay = 2.0f;
        
        [Header("Performance Monitoring")]
        [SerializeField] private TopologyMetrics currentMetrics;
        [SerializeField] private List<TopologyPerformanceData> performanceHistory = new List<TopologyPerformanceData>();
        
        private CoordinationHub coordinationHub;
        private Dictionary<string, List<string>> agentConnections = new Dictionary<string, List<string>>();
        private Dictionary<string, AgentBase> registeredAgents = new Dictionary<string, AgentBase>();
        private float lastOptimizationTime;
        
        // Topology implementations
        private ITopologyImplementation hierarchicalTopology;
        private ITopologyImplementation meshTopology;
        private ITopologyImplementation ringTopology;
        private ITopologyImplementation starTopology;
        
        #region Initialization
        
        public void Initialize(CoordinationHub hub)
        {
            coordinationHub = hub;
            
            // Initialize topology implementations
            hierarchicalTopology = new HierarchicalTopology();
            meshTopology = new MeshTopology();
            ringTopology = new RingTopology();
            starTopology = new StarTopology();
            
            currentMetrics = new TopologyMetrics();
            
            Debug.Log("[TopologyManager] Initialized with topology: " + currentTopology);
        }
        
        #endregion
        
        #region Agent Management
        
        public void AddAgent(AgentBase agent)
        {
            if (agent == null || registeredAgents.ContainsKey(agent.AgentId))
                return;
            
            registeredAgents[agent.AgentId] = agent;
            agentConnections[agent.AgentId] = new List<string>();
            
            // Rebuild connections with new topology
            RebuildTopology();
            
            Debug.Log($"[TopologyManager] Added agent {agent.AgentId} to topology");
        }
        
        public void RemoveAgent(AgentBase agent)
        {
            if (agent == null || !registeredAgents.ContainsKey(agent.AgentId))
                return;
            
            // Remove agent from connections
            registeredAgents.Remove(agent.AgentId);
            agentConnections.Remove(agent.AgentId);
            
            // Remove connections to this agent from other agents
            foreach (var connections in agentConnections.Values)
            {
                connections.Remove(agent.AgentId);
            }
            
            // Rebuild topology without the removed agent
            RebuildTopology();
            
            Debug.Log($"[TopologyManager] Removed agent {agent.AgentId} from topology");
        }
        
        #endregion
        
        #region Topology Management
        
        public void SetTopology(SwarmTopology topology)
        {
            if (currentTopology != topology)
            {
                currentTopology = topology;
                RebuildTopology();
                
                Debug.Log($"[TopologyManager] Topology changed to: {topology}");
            }
        }
        
        private void RebuildTopology()
        {
            if (registeredAgents.Count == 0)
                return;
            
            // Clear existing connections
            foreach (var connections in agentConnections.Values)
            {
                connections.Clear();
            }
            
            // Build new topology
            var topology = GetTopologyImplementation(currentTopology);
            topology.BuildConnections(registeredAgents, agentConnections);
            
            UpdateMetrics();
        }
        
        private ITopologyImplementation GetTopologyImplementation(SwarmTopology topology)
        {
            return topology switch
            {
                SwarmTopology.Hierarchical => hierarchicalTopology,
                SwarmTopology.Mesh => meshTopology,
                SwarmTopology.Ring => ringTopology,
                SwarmTopology.Star => starTopology,
                _ => hierarchicalTopology
            };
        }
        
        #endregion
        
        #region Optimization
        
        public void OptimizeTopology()
        {
            if (!enableDynamicOptimization || Time.time - lastOptimizationTime < topologyChangeDelay)
                return;
            
            var currentPerformance = CalculateTopologyPerformance();
            var bestTopology = FindOptimalTopology();
            
            if (bestTopology != currentTopology && ShouldChangeTopology(currentPerformance))
            {
                SetTopology(bestTopology);
                lastOptimizationTime = Time.time;
                
                Debug.Log($"[TopologyManager] Optimized topology from {currentTopology} to {bestTopology}");
            }
        }
        
        private SwarmTopology FindOptimalTopology()
        {
            var agentCount = registeredAgents.Count;
            var currentLoad = CalculateAverageAgentLoad();
            var messageLatency = CalculateAverageMessageLatency();
            
            // Algorithm to determine optimal topology based on current conditions
            if (agentCount <= 3)
            {
                return SwarmTopology.Star; // Simple star for small groups
            }
            else if (agentCount <= 6 && currentLoad < 0.5f)
            {
                return SwarmTopology.Mesh; // Full connectivity for medium groups with low load
            }
            else if (messageLatency > 100f) // ms
            {
                return SwarmTopology.Ring; // Ring for high latency scenarios
            }
            else
            {
                return SwarmTopology.Hierarchical; // Hierarchical for large groups or high load
            }
        }
        
        private bool ShouldChangeTopology(float currentPerformance)
        {
            return currentPerformance < optimizationThreshold;
        }
        
        private float CalculateTopologyPerformance()
        {
            var metrics = currentMetrics;
            
            // Weighted performance calculation
            var connectivityScore = metrics.AverageConnectivity / registeredAgents.Count;
            var latencyScore = 1.0f - (metrics.AverageLatency / 1000f); // Normalize latency
            var throughputScore = metrics.MessageThroughput / 100f; // Normalize throughput
            
            return (connectivityScore * 0.3f + latencyScore * 0.4f + throughputScore * 0.3f);
        }
        
        private float CalculateAverageAgentLoad()
        {
            if (registeredAgents.Count == 0) return 0f;
            
            return registeredAgents.Values.Average(a => a.CurrentWorkload / a.GetType().GetField("maxWorkload")?.GetValue(a) as float? ?? 1f);
        }
        
        private float CalculateAverageMessageLatency()
        {
            // Calculate based on message processing times
            return currentMetrics.AverageLatency;
        }
        
        #endregion
        
        #region Metrics
        
        private void UpdateMetrics()
        {
            currentMetrics.AgentCount = registeredAgents.Count;
            currentMetrics.TotalConnections = agentConnections.Values.Sum(c => c.Count);
            currentMetrics.AverageConnectivity = registeredAgents.Count > 0 ? 
                (float)currentMetrics.TotalConnections / registeredAgents.Count : 0f;
            currentMetrics.TopologyType = currentTopology;
            currentMetrics.Timestamp = Time.time;
            
            // Calculate additional metrics
            CalculateAdvancedMetrics();
            
            // Store performance history
            var performanceData = new TopologyPerformanceData
            {
                Topology = currentTopology,
                Performance = CalculateTopologyPerformance(),
                Timestamp = Time.time,
                AgentCount = registeredAgents.Count
            };
            
            performanceHistory.Add(performanceData);
            
            // Limit history size
            if (performanceHistory.Count > 100)
                performanceHistory.RemoveAt(0);
        }
        
        private void CalculateAdvancedMetrics()
        {
            // Calculate network diameter (longest shortest path between any two agents)
            currentMetrics.NetworkDiameter = CalculateNetworkDiameter();
            
            // Calculate clustering coefficient
            currentMetrics.ClusteringCoefficient = CalculateClusteringCoefficient();
            
            // Calculate network efficiency
            currentMetrics.NetworkEfficiency = CalculateNetworkEfficiency();
        }
        
        private int CalculateNetworkDiameter()
        {
            if (registeredAgents.Count <= 1) return 0;
            
            var maxDistance = 0;
            var agents = registeredAgents.Keys.ToList();
            
            // Use Floyd-Warshall algorithm to find shortest paths
            var distances = new Dictionary<(string, string), int>();
            
            // Initialize distances
            foreach (var agent1 in agents)
            {
                foreach (var agent2 in agents)
                {
                    if (agent1 == agent2)
                        distances[(agent1, agent2)] = 0;
                    else if (agentConnections[agent1].Contains(agent2))
                        distances[(agent1, agent2)] = 1;
                    else
                        distances[(agent1, agent2)] = int.MaxValue;
                }
            }
            
            // Floyd-Warshall
            foreach (var k in agents)
            {
                foreach (var i in agents)
                {
                    foreach (var j in agents)
                    {
                        if (distances[(i, k)] != int.MaxValue && distances[(k, j)] != int.MaxValue)
                        {
                            var newDistance = distances[(i, k)] + distances[(k, j)];
                            if (newDistance < distances[(i, j)])
                                distances[(i, j)] = newDistance;
                        }
                    }
                }
            }
            
            // Find maximum distance
            foreach (var distance in distances.Values)
            {
                if (distance != int.MaxValue && distance > maxDistance)
                    maxDistance = distance;
            }
            
            return maxDistance;
        }
        
        private float CalculateClusteringCoefficient()
        {
            if (registeredAgents.Count <= 2) return 0f;
            
            var totalCoefficient = 0f;
            var validAgents = 0;
            
            foreach (var agentId in registeredAgents.Keys)
            {
                var neighbors = agentConnections[agentId];
                if (neighbors.Count < 2) continue;
                
                var possibleConnections = neighbors.Count * (neighbors.Count - 1) / 2;
                var actualConnections = 0;
                
                for (int i = 0; i < neighbors.Count; i++)
                {
                    for (int j = i + 1; j < neighbors.Count; j++)
                    {
                        if (agentConnections[neighbors[i]].Contains(neighbors[j]))
                            actualConnections++;
                    }
                }
                
                totalCoefficient += (float)actualConnections / possibleConnections;
                validAgents++;
            }
            
            return validAgents > 0 ? totalCoefficient / validAgents : 0f;
        }
        
        private float CalculateNetworkEfficiency()
        {
            if (registeredAgents.Count <= 1) return 1f;
            
            var totalEfficiency = 0f;
            var agentPairs = 0;
            var agents = registeredAgents.Keys.ToList();
            
            for (int i = 0; i < agents.Count; i++)
            {
                for (int j = i + 1; j < agents.Count; j++)
                {
                    var shortestPath = FindShortestPath(agents[i], agents[j]);
                    if (shortestPath > 0)
                    {
                        totalEfficiency += 1f / shortestPath;
                    }
                    agentPairs++;
                }
            }
            
            return agentPairs > 0 ? totalEfficiency / agentPairs : 0f;
        }
        
        private int FindShortestPath(string from, string to)
        {
            if (from == to) return 0;
            
            var queue = new Queue<(string agent, int distance)>();
            var visited = new HashSet<string>();
            
            queue.Enqueue((from, 0));
            visited.Add(from);
            
            while (queue.Count > 0)
            {
                var (currentAgent, distance) = queue.Dequeue();
                
                foreach (var neighbor in agentConnections[currentAgent])
                {
                    if (neighbor == to)
                        return distance + 1;
                    
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, distance + 1));
                    }
                }
            }
            
            return -1; // No path found
        }
        
        #endregion
        
        #region Public API
        
        public TopologyMetrics GetMetrics() => currentMetrics;
        
        public List<string> GetAgentConnections(string agentId)
        {
            return agentConnections.TryGetValue(agentId, out var connections) ? 
                new List<string>(connections) : new List<string>();
        }
        
        public bool AreAgentsConnected(string agentId1, string agentId2)
        {
            return agentConnections.TryGetValue(agentId1, out var connections) && 
                   connections.Contains(agentId2);
        }
        
        public SwarmTopology GetCurrentTopology() => currentTopology;
        
        public List<TopologyPerformanceData> GetPerformanceHistory() => 
            new List<TopologyPerformanceData>(performanceHistory);
        
        #endregion
    }
    
    #region Supporting Classes
    
    [System.Serializable]
    public class TopologyMetrics
    {
        public SwarmTopology TopologyType;
        public int AgentCount;
        public int TotalConnections;
        public float AverageConnectivity;
        public int NetworkDiameter;
        public float ClusteringCoefficient;
        public float NetworkEfficiency;
        public float AverageLatency;
        public float MessageThroughput;
        public float Timestamp;
    }
    
    [System.Serializable]
    public class TopologyPerformanceData
    {
        public SwarmTopology Topology;
        public float Performance;
        public float Timestamp;
        public int AgentCount;
    }
    
    public interface ITopologyImplementation
    {
        void BuildConnections(Dictionary<string, AgentBase> agents, Dictionary<string, List<string>> connections);
    }
    
    public class HierarchicalTopology : ITopologyImplementation
    {
        public void BuildConnections(Dictionary<string, AgentBase> agents, Dictionary<string, List<string>> connections)
        {
            var agentList = agents.Keys.ToList();
            if (agentList.Count == 0) return;
            
            // Create hierarchical structure with coordinators at the top
            var coordinators = agents.Values.Where(a => a.Type == AgentType.Coordinator).Select(a => a.AgentId).ToList();
            var workers = agentList.Except(coordinators).ToList();
            
            // If no coordinators, designate first agent as coordinator
            if (coordinators.Count == 0 && workers.Count > 0)
            {
                coordinators.Add(workers[0]);
                workers.RemoveAt(0);
            }
            
            // Connect coordinators in a mesh
            for (int i = 0; i < coordinators.Count; i++)
            {
                for (int j = i + 1; j < coordinators.Count; j++)
                {
                    connections[coordinators[i]].Add(coordinators[j]);
                    connections[coordinators[j]].Add(coordinators[i]);
                }
            }
            
            // Connect workers to coordinators
            for (int i = 0; i < workers.Count; i++)
            {
                var coordinator = coordinators[i % coordinators.Count];
                connections[workers[i]].Add(coordinator);
                connections[coordinator].Add(workers[i]);
            }
        }
    }
    
    public class MeshTopology : ITopologyImplementation
    {
        public void BuildConnections(Dictionary<string, AgentBase> agents, Dictionary<string, List<string>> connections)
        {
            var agentList = agents.Keys.ToList();
            
            // Connect every agent to every other agent
            for (int i = 0; i < agentList.Count; i++)
            {
                for (int j = i + 1; j < agentList.Count; j++)
                {
                    connections[agentList[i]].Add(agentList[j]);
                    connections[agentList[j]].Add(agentList[i]);
                }
            }
        }
    }
    
    public class RingTopology : ITopologyImplementation
    {
        public void BuildConnections(Dictionary<string, AgentBase> agents, Dictionary<string, List<string>> connections)
        {
            var agentList = agents.Keys.ToList();
            if (agentList.Count == 0) return;
            
            // Connect agents in a ring
            for (int i = 0; i < agentList.Count; i++)
            {
                var nextIndex = (i + 1) % agentList.Count;
                connections[agentList[i]].Add(agentList[nextIndex]);
                connections[agentList[nextIndex]].Add(agentList[i]);
            }
        }
    }
    
    public class StarTopology : ITopologyImplementation
    {
        public void BuildConnections(Dictionary<string, AgentBase> agents, Dictionary<string, List<string>> connections)
        {
            var agentList = agents.Keys.ToList();
            if (agentList.Count == 0) return;
            
            // Find or designate central agent (prefer coordinator)
            var centralAgent = agents.Values.FirstOrDefault(a => a.Type == AgentType.Coordinator)?.AgentId ?? agentList[0];
            
            // Connect all other agents to central agent
            foreach (var agentId in agentList)
            {
                if (agentId != centralAgent)
                {
                    connections[agentId].Add(centralAgent);
                    connections[centralAgent].Add(agentId);
                }
            }
        }
    }
    
    #endregion
}