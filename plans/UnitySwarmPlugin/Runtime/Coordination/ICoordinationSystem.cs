using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace SwarmAI.Coordination
{
    /// <summary>
    /// Core interface for Claude Flow coordination system integration,
    /// providing AI-powered swarm intelligence and decision making.
    /// </summary>
    public interface ICoordinationSystem
    {
        /// <summary>
        /// Whether the coordination system is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Current coordination level being used
        /// </summary>
        CoordinationLevel Level { get; set; }
        
        /// <summary>
        /// Connection status to Claude Flow backend
        /// </summary>
        ConnectionStatus Status { get; }
        
        /// <summary>
        /// Performance metrics for coordination decisions
        /// </summary>
        CoordinationMetrics Metrics { get; }
        
        /// <summary>
        /// Initialize the coordination system
        /// </summary>
        /// <param name="config">Coordination configuration</param>
        /// <returns>Success status</returns>
        Task<bool> InitializeAsync(CoordinationConfig config);
        
        /// <summary>
        /// Request coordination decision for a swarm situation
        /// </summary>
        /// <param name="request">Coordination request data</param>
        /// <returns>AI-generated coordination decision</returns>
        Task<CoordinationDecision> RequestCoordinationAsync(CoordinationRequest request);
        
        /// <summary>
        /// Update the coordination system with current swarm state
        /// </summary>
        /// <param name="swarmState">Current state of all swarms</param>
        void UpdateSwarmState(SwarmStateSnapshot swarmState);
        
        /// <summary>
        /// Learn from successful swarm behaviors for future decisions
        /// </summary>
        /// <param name="behaviorData">Successful behavior pattern data</param>
        void LearnFromBehavior(BehaviorLearningData behaviorData);
        
        /// <summary>
        /// Get optimal behavior configuration for current conditions
        /// </summary>
        /// <param name="context">Current swarm context</param>
        /// <returns>Recommended behavior configuration</returns>
        Task<BehaviorConfiguration> GetOptimalBehaviorAsync(SwarmContext context);
        
        /// <summary>
        /// Register a custom coordination plugin
        /// </summary>
        /// <param name="plugin">Plugin to register</param>
        void RegisterPlugin(ICoordinationPlugin plugin);
        
        /// <summary>
        /// Event fired when coordination decision is received
        /// </summary>
        event Action<CoordinationDecision> OnCoordinationDecision;
        
        /// <summary>
        /// Event fired when learning update occurs
        /// </summary>
        event Action<LearningUpdate> OnLearningUpdate;
        
        /// <summary>
        /// Cleanup and dispose of coordination resources
        /// </summary>
        void Cleanup();
    }
    
    /// <summary>
    /// Configuration for Claude Flow coordination system
    /// </summary>
    [Serializable]
    public class CoordinationConfig
    {
        [Header("Connection Settings")]
        public string endpoint = "https://api.claude-flow.ai/v1/coordination";
        public string apiKey = "";
        public int timeoutMs = 5000;
        public int retryAttempts = 3;
        
        [Header("Coordination Settings")]
        public CoordinationLevel level = CoordinationLevel.Basic;
        public bool enableLearning = true;
        public bool enablePrediction = true;
        public float decisionCacheTime = 30f;
        
        [Header("Performance")]
        public int maxConcurrentRequests = 5;
        public float requestBatchTime = 0.1f;
        public bool enableLocalFallback = true;
        
        [Header("Learning")]
        public float learningRate = 0.1f;
        public int maxMemoryEntries = 10000;
        public bool shareGlobalLearning = true;
    }
    
    /// <summary>
    /// Request for coordination assistance from Claude Flow
    /// </summary>
    public class CoordinationRequest
    {
        /// <summary>Unique request identifier</summary>
        public string RequestId { get; set; }
        
        /// <summary>Type of coordination needed</summary>
        public CoordinationType Type { get; set; }
        
        /// <summary>Current swarm state</summary>
        public SwarmStateSnapshot SwarmState { get; set; }
        
        /// <summary>Environmental context</summary>
        public EnvironmentContext Environment { get; set; }
        
        /// <summary>Specific objectives or goals</summary>
        public List<SwarmObjective> Objectives { get; set; }
        
        /// <summary>Historical performance data</summary>
        public PerformanceHistory History { get; set; }
        
        /// <summary>Priority level of the request</summary>
        public RequestPriority Priority { get; set; }
        
        public CoordinationRequest()
        {
            RequestId = Guid.NewGuid().ToString();
            Objectives = new List<SwarmObjective>();
        }
    }
    
    /// <summary>
    /// AI-generated coordination decision from Claude Flow
    /// </summary>
    public class CoordinationDecision
    {
        /// <summary>Request ID this decision responds to</summary>
        public string RequestId { get; set; }
        
        /// <summary>Recommended behavior adjustments</summary>
        public List<BehaviorAdjustment> BehaviorAdjustments { get; set; }
        
        /// <summary>Formation changes to implement</summary>
        public FormationCommand FormationCommand { get; set; }
        
        /// <summary>Target updates for the swarm</summary>
        public List<TargetUpdate> TargetUpdates { get; set; }
        
        /// <summary>Performance optimizations to apply</summary>
        public PerformanceOptimization Optimization { get; set; }
        
        /// <summary>Confidence level of the decision (0-1)</summary>
        public float Confidence { get; set; }
        
        /// <summary>Expected effectiveness rating</summary>
        public float ExpectedEffectiveness { get; set; }
        
        /// <summary>Duration this decision should remain active</summary>
        public float ValidDurationSeconds { get; set; }
        
        /// <summary>Reasoning explanation for the decision</summary>
        public string Reasoning { get; set; }
        
        public CoordinationDecision()
        {
            BehaviorAdjustments = new List<BehaviorAdjustment>();
            TargetUpdates = new List<TargetUpdate>();
        }
    }
    
    /// <summary>
    /// Types of coordination that can be requested
    /// </summary>
    public enum CoordinationType
    {
        /// <summary>General behavior optimization</summary>
        BehaviorOptimization,
        /// <summary>Formation planning and execution</summary>
        FormationPlanning,
        /// <summary>Target selection and assignment</summary>
        TargetAssignment,
        /// <summary>Conflict resolution between agents</summary>
        ConflictResolution,
        /// <summary>Performance optimization suggestions</summary>
        PerformanceOptimization,
        /// <summary>Emergency response coordination</summary>
        EmergencyResponse,
        /// <summary>Custom coordination request</summary>
        Custom
    }
    
    /// <summary>
    /// Priority levels for coordination requests
    /// </summary>
    public enum RequestPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
    
    /// <summary>
    /// Connection status to Claude Flow backend
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Fallback
    }
    
    /// <summary>
    /// Snapshot of current swarm state for coordination
    /// </summary>
    public class SwarmStateSnapshot
    {
        public int TotalAgents { get; set; }
        public int ActiveAgents { get; set; }
        public Vector3 SwarmCenter { get; set; }
        public float SwarmRadius { get; set; }
        public float AverageSpeed { get; set; }
        public float Cohesion { get; set; }
        public float Alignment { get; set; }
        public float Separation { get; set; }
        public List<AgentState> AgentStates { get; set; }
        public DateTime Timestamp { get; set; }
        
        public SwarmStateSnapshot()
        {
            AgentStates = new List<AgentState>();
            Timestamp = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Individual agent state within the swarm
    /// </summary>
    public class AgentState
    {
        public int AgentId { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public AgentType Type { get; set; }
        public LODLevel LODLevel { get; set; }
        public float Energy { get; set; }
        public List<string> ActiveBehaviors { get; set; }
        
        public AgentState()
        {
            ActiveBehaviors = new List<string>();
        }
    }
    
    /// <summary>
    /// Recommended behavior weight adjustment
    /// </summary>
    public class BehaviorAdjustment
    {
        public string BehaviorName { get; set; }
        public float NewWeight { get; set; }
        public float Duration { get; set; }
        public List<int> TargetAgentIds { get; set; }
        public string Reason { get; set; }
        
        public BehaviorAdjustment()
        {
            TargetAgentIds = new List<int>();
        }
    }
    
    /// <summary>
    /// Interface for custom coordination plugins
    /// </summary>
    public interface ICoordinationPlugin
    {
        string PluginName { get; }
        Version PluginVersion { get; }
        
        void Initialize(ICoordinationSystem system);
        Task<CoordinationDecision> ProcessRequestAsync(CoordinationRequest request);
        void Cleanup();
    }
    
    /// <summary>
    /// Metrics for coordination system performance
    /// </summary>
    public class CoordinationMetrics
    {
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public float AverageResponseTime { get; set; }
        public float DecisionAccuracy { get; set; }
        public int CacheHits { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}