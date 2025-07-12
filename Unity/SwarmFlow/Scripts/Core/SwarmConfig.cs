using UnityEngine;
using SwarmFlow.Coordination;

namespace SwarmFlow.Core
{
    /// <summary>
    /// Scriptable object for configuring swarm behavior and performance parameters.
    /// Contains settings for topology, coordination strategies, and performance thresholds.
    /// </summary>
    [CreateAssetMenu(fileName = "SwarmConfig", menuName = "SwarmFlow/Swarm Configuration")]
    public class SwarmConfig : ScriptableObject
    {
        [Header("Topology Configuration")]
        public SwarmTopology topology = SwarmTopology.Hierarchical;
        public CoordinationStrategy coordinationStrategy = CoordinationStrategy.Adaptive;
        public int maxAgents = 8;
        public float coordinationInterval = 0.1f;
        
        [Header("Performance Thresholds")]
        public PerformanceThresholds performanceThresholds = new PerformanceThresholds();
        
        [Header("Agent Spawning")]
        public bool autoSpawnAgents = false;
        public AgentSpawnConfiguration[] defaultAgentConfigs = new AgentSpawnConfiguration[0];
        
        [Header("Coordination Settings")]
        public float messageTimeout = 5.0f;
        public int maxMessagesPerFrame = 10;
        public bool enableMessageLogging = false;
        public float topologyOptimizationInterval = 5.0f;
        
        [Header("Performance Monitoring")]
        public bool enablePerformanceMonitoring = true;
        public float metricsUpdateInterval = 1.0f;
        public bool enableProfiling = false;
        
        [Header("Memory Management")]
        public int maxMessageHistory = 100;
        public float memoryCleanupInterval = 30.0f;
        public bool enableMemoryCompression = true;
        
        [Header("Neural Learning")]
        public bool enableNeuralLearning = false;
        public float learningRate = 0.01f;
        public int neuralPatternHistorySize = 1000;
        
        [Header("GitHub Integration")]
        public bool enableGitHubIntegration = false;
        public string gitHubToken = "";
        public string defaultRepository = "";
        
        /// <summary>
        /// Validates the configuration and applies defaults for missing values.
        /// </summary>
        public void ValidateAndApplyDefaults()
        {
            // Clamp values to safe ranges
            maxAgents = Mathf.Clamp(maxAgents, 1, 50);
            coordinationInterval = Mathf.Clamp(coordinationInterval, 0.01f, 1.0f);
            messageTimeout = Mathf.Clamp(messageTimeout, 1.0f, 30.0f);
            maxMessagesPerFrame = Mathf.Clamp(maxMessagesPerFrame, 1, 100);
            topologyOptimizationInterval = Mathf.Clamp(topologyOptimizationInterval, 1.0f, 60.0f);
            metricsUpdateInterval = Mathf.Clamp(metricsUpdateInterval, 0.1f, 10.0f);
            memoryCleanupInterval = Mathf.Clamp(memoryCleanupInterval, 10.0f, 300.0f);
            learningRate = Mathf.Clamp(learningRate, 0.001f, 0.1f);
            neuralPatternHistorySize = Mathf.Clamp(neuralPatternHistorySize, 100, 10000);
            
            // Validate performance thresholds
            if (performanceThresholds == null)
            {
                performanceThresholds = new PerformanceThresholds();
            }
            
            performanceThresholds.ValidateAndApplyDefaults();
            
            // Initialize default agent configs if empty
            if (defaultAgentConfigs.Length == 0 && autoSpawnAgents)
            {
                InitializeDefaultAgentConfigs();
            }
        }
        
        private void InitializeDefaultAgentConfigs()
        {
            defaultAgentConfigs = new AgentSpawnConfiguration[]
            {
                new AgentSpawnConfiguration
                {
                    agentType = Agents.AgentType.SceneArchitect,
                    spawnOnStart = true,
                    maxInstances = 1,
                    priority = SpawnPriority.High
                },
                new AgentSpawnConfiguration
                {
                    agentType = Agents.AgentType.AssetOptimizer,
                    spawnOnStart = true,
                    maxInstances = 1,
                    priority = SpawnPriority.Medium
                },
                new AgentSpawnConfiguration
                {
                    agentType = Agents.AgentType.PerformanceAnalyst,
                    spawnOnStart = false,
                    maxInstances = 2,
                    priority = SpawnPriority.Low
                }
            };
        }
        
        /// <summary>
        /// Gets the recommended topology based on the number of agents and coordination strategy.
        /// </summary>
        public SwarmTopology GetRecommendedTopology()
        {
            return (maxAgents, coordinationStrategy) switch
            {
                (< 4, _) => SwarmTopology.Star,
                (< 8, CoordinationStrategy.Performance) => SwarmTopology.Hierarchical,
                (< 8, CoordinationStrategy.Balanced) => SwarmTopology.Mesh,
                (>= 8, CoordinationStrategy.Adaptive) => SwarmTopology.Hierarchical,
                _ => topology
            };
        }
        
        /// <summary>
        /// Creates a copy of this configuration with modified settings.
        /// </summary>
        public SwarmConfig CreateVariant(string variantName)
        {
            var variant = CreateInstance<SwarmConfig>();
            
            // Copy all settings
            variant.topology = topology;
            variant.coordinationStrategy = coordinationStrategy;
            variant.maxAgents = maxAgents;
            variant.coordinationInterval = coordinationInterval;
            variant.performanceThresholds = new PerformanceThresholds
            {
                maxResponseTime = performanceThresholds.maxResponseTime,
                minSuccessRate = performanceThresholds.minSuccessRate,
                maxMemoryUsage = performanceThresholds.maxMemoryUsage
            };
            variant.autoSpawnAgents = autoSpawnAgents;
            variant.defaultAgentConfigs = (AgentSpawnConfiguration[])defaultAgentConfigs.Clone();
            variant.messageTimeout = messageTimeout;
            variant.maxMessagesPerFrame = maxMessagesPerFrame;
            variant.enableMessageLogging = enableMessageLogging;
            variant.topologyOptimizationInterval = topologyOptimizationInterval;
            variant.enablePerformanceMonitoring = enablePerformanceMonitoring;
            variant.metricsUpdateInterval = metricsUpdateInterval;
            variant.enableProfiling = enableProfiling;
            variant.maxMessageHistory = maxMessageHistory;
            variant.memoryCleanupInterval = memoryCleanupInterval;
            variant.enableMemoryCompression = enableMemoryCompression;
            variant.enableNeuralLearning = enableNeuralLearning;
            variant.learningRate = learningRate;
            variant.neuralPatternHistorySize = neuralPatternHistorySize;
            variant.enableGitHubIntegration = enableGitHubIntegration;
            variant.gitHubToken = gitHubToken;
            variant.defaultRepository = defaultRepository;
            
            variant.name = variantName;
            return variant;
        }
        
        private void OnValidate()
        {
            ValidateAndApplyDefaults();
        }
    }
    
    [System.Serializable]
    public class AgentSpawnConfiguration
    {
        public Agents.AgentType agentType;
        public bool spawnOnStart = true;
        public int maxInstances = 1;
        public SpawnPriority priority = SpawnPriority.Medium;
        public Vector3 spawnPosition = Vector3.zero;
        public string[] requiredCapabilities = new string[0];
    }
    
    public enum SpawnPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }
}