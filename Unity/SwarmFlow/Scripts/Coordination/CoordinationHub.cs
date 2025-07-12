using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SwarmFlow.Core;
using SwarmFlow.Agents;

namespace SwarmFlow.Coordination
{
    /// <summary>
    /// Central coordination hub for inter-agent communication and swarm orchestration.
    /// Manages message routing, topology optimization, and performance monitoring.
    /// </summary>
    public class CoordinationHub : MonoBehaviour
    {
        [Header("Coordination Configuration")]
        [SerializeField] private float messageProcessingInterval = 0.05f;
        [SerializeField] private int maxMessagesPerFrame = 10;
        [SerializeField] private bool enableMessageLogging = false;
        [SerializeField] private float topologyOptimizationInterval = 5.0f;
        
        [Header("Performance Monitoring")]
        [SerializeField] private int messagesProcessed;
        [SerializeField] private int messagesDropped;
        [SerializeField] private float averageProcessingTime;
        
        private SwarmManager swarmManager;
        private Dictionary<string, AgentBase> registeredAgents = new Dictionary<string, AgentBase>();
        private Queue<CoordinationMessage> messageQueue = new Queue<CoordinationMessage>();
        private Dictionary<string, List<CoordinationMessage>> agentMessageHistory = new Dictionary<string, List<CoordinationMessage>>();
        private TopologyManager topologyManager;
        private PerformanceMonitor performanceMonitor;
        
        // Coordination patterns
        private Dictionary<string, CoordinationPattern> activePatterns = new Dictionary<string, CoordinationPattern>();
        
        // Events
        public event Action<CoordinationMessage> OnMessageProcessed;
        public event Action<string, AgentBase> OnAgentRegistered;
        public event Action<string> OnAgentUnregistered;
        public event Action<CoordinationMetrics> OnMetricsUpdated;
        
        #region Initialization
        
        public void Initialize(SwarmManager manager)
        {
            swarmManager = manager;
            
            // Initialize subsystems
            topologyManager = gameObject.AddComponent<TopologyManager>();
            topologyManager.Initialize(this);
            
            performanceMonitor = gameObject.AddComponent<PerformanceMonitor>();
            performanceMonitor.Initialize(this);
            
            // Start coordination loops
            StartCoroutine(MessageProcessingLoop());
            StartCoroutine(TopologyOptimizationLoop());
            
            Debug.Log("[CoordinationHub] Initialized successfully");
        }
        
        #endregion
        
        #region Agent Registration
        
        public void RegisterAgent(AgentBase agent)
        {
            if (agent == null || registeredAgents.ContainsKey(agent.AgentId))
                return;
            
            registeredAgents[agent.AgentId] = agent;
            agentMessageHistory[agent.AgentId] = new List<CoordinationMessage>();
            
            // Subscribe to agent events
            agent.OnTaskStarted += HandleAgentTaskStarted;
            agent.OnTaskCompleted += HandleAgentTaskCompleted;
            agent.OnStateChanged += HandleAgentStateChanged;
            
            // Update topology
            topologyManager.AddAgent(agent);
            
            OnAgentRegistered?.Invoke(agent.AgentId, agent);
            
            Debug.Log($"[CoordinationHub] Registered agent: {agent.AgentId}");
        }
        
        public void UnregisterAgent(AgentBase agent)
        {
            if (agent == null || !registeredAgents.ContainsKey(agent.AgentId))
                return;
            
            // Unsubscribe from agent events
            agent.OnTaskStarted -= HandleAgentTaskStarted;
            agent.OnTaskCompleted -= HandleAgentTaskCompleted;
            agent.OnStateChanged -= HandleAgentStateChanged;
            
            registeredAgents.Remove(agent.AgentId);
            agentMessageHistory.Remove(agent.AgentId);
            
            // Update topology
            topologyManager.RemoveAgent(agent);
            
            OnAgentUnregistered?.Invoke(agent.AgentId);
            
            Debug.Log($"[CoordinationHub] Unregistered agent: {agent.AgentId}");
        }
        
        #endregion
        
        #region Message Management
        
        public void SendMessage(CoordinationMessage message)
        {
            if (message == null)
                return;
            
            message.Timestamp = Time.time;
            message.Id = GenerateMessageId();
            
            messageQueue.Enqueue(message);
            
            if (enableMessageLogging)
                Debug.Log($"[CoordinationHub] Message queued: {message.Id} ({message.Type}) from {message.SenderId}");
        }
        
        public void BroadcastMessage(CoordinationMessage message)
        {
            if (message == null)
                return;
            
            foreach (var agentId in registeredAgents.Keys)
            {
                if (agentId != message.SenderId) // Don't send to sender
                {
                    var broadcastMessage = message.Clone();
                    broadcastMessage.TargetId = agentId;
                    SendMessage(broadcastMessage);
                }
            }
        }
        
        private System.Collections.IEnumerator MessageProcessingLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(messageProcessingInterval);
                ProcessMessages();
            }
        }
        
        private void ProcessMessages()
        {
            int processedThisFrame = 0;
            var startTime = Time.realtimeSinceStartup;
            
            while (messageQueue.Count > 0 && processedThisFrame < maxMessagesPerFrame)
            {
                var message = messageQueue.Dequeue();
                
                if (ProcessMessage(message))
                {
                    messagesProcessed++;
                }
                else
                {
                    messagesDropped++;
                }
                
                processedThisFrame++;
            }
            
            if (processedThisFrame > 0)
            {
                var processingTime = Time.realtimeSinceStartup - startTime;
                averageProcessingTime = (averageProcessingTime * 0.9f) + (processingTime * 0.1f);
            }
        }
        
        private bool ProcessMessage(CoordinationMessage message)
        {
            try
            {
                // Route message to target agent(s)
                if (!string.IsNullOrEmpty(message.TargetId))
                {
                    // Direct message
                    if (registeredAgents.TryGetValue(message.TargetId, out var targetAgent))
                    {
                        targetAgent.HandleCoordinationMessage(message);
                        RecordMessage(message);
                        return true;
                    }
                }
                else
                {
                    // Broadcast message (already handled in BroadcastMessage)
                    RecordMessage(message);
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CoordinationHub] Error processing message {message.Id}: {ex.Message}");
                return false;
            }
        }
        
        private void RecordMessage(CoordinationMessage message)
        {
            // Record in sender's history
            if (agentMessageHistory.TryGetValue(message.SenderId, out var senderHistory))
            {
                senderHistory.Add(message);
                
                // Limit history size
                if (senderHistory.Count > 100)
                    senderHistory.RemoveAt(0);
            }
            
            OnMessageProcessed?.Invoke(message);
        }
        
        private string GenerateMessageId()
        {
            return $"msg_{Time.frameCount}_{UnityEngine.Random.Range(1000, 9999)}";
        }
        
        #endregion
        
        #region Coordination Patterns
        
        public void StartCoordinationPattern(string patternId, CoordinationPattern pattern)
        {
            activePatterns[patternId] = pattern;
            pattern.Start(this);
            
            Debug.Log($"[CoordinationHub] Started coordination pattern: {patternId}");
        }
        
        public void StopCoordinationPattern(string patternId)
        {
            if (activePatterns.TryGetValue(patternId, out var pattern))
            {
                pattern.Stop();
                activePatterns.Remove(patternId);
                
                Debug.Log($"[CoordinationHub] Stopped coordination pattern: {patternId}");
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void HandleAgentTaskStarted(AgentBase agent, SwarmTask task)
        {
            // Broadcast task start to other agents for coordination
            BroadcastMessage(new CoordinationMessage
            {
                SenderId = agent.AgentId,
                Type = MessageType.TaskStarted,
                Data = new { TaskId = task.Id, TaskType = task.Type, AgentType = agent.Type }
            });
        }
        
        private void HandleAgentTaskCompleted(AgentBase agent, SwarmTask task, bool success)
        {
            // Broadcast task completion
            BroadcastMessage(new CoordinationMessage
            {
                SenderId = agent.AgentId,
                Type = MessageType.TaskCompleted,
                Data = new { TaskId = task.Id, Success = success, AgentType = agent.Type }
            });
            
            // Update performance metrics
            performanceMonitor.RecordTaskCompletion(agent, task, success);
        }
        
        private void HandleAgentStateChanged(AgentBase agent, AgentState newState)
        {
            // Notify other agents of state changes
            BroadcastMessage(new CoordinationMessage
            {
                SenderId = agent.AgentId,
                Type = MessageType.StateChanged,
                Data = new { NewState = newState, AgentType = agent.Type }
            });
        }
        
        #endregion
        
        #region Topology Management
        
        public void OptimizeTopology()
        {
            topologyManager.OptimizeTopology();
        }
        
        private System.Collections.IEnumerator TopologyOptimizationLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(topologyOptimizationInterval);
                
                if (registeredAgents.Count > 1)
                {
                    OptimizeTopology();
                }
            }
        }
        
        #endregion
        
        #region Performance Monitoring
        
        public CoordinationMetrics GetMetrics()
        {
            return new CoordinationMetrics
            {
                RegisteredAgents = registeredAgents.Count,
                MessagesProcessed = messagesProcessed,
                MessagesDropped = messagesDropped,
                AverageProcessingTime = averageProcessingTime,
                QueuedMessages = messageQueue.Count,
                ActivePatterns = activePatterns.Count,
                Timestamp = Time.time
            };
        }
        
        #endregion
        
        #region Public API
        
        public List<AgentBase> GetRegisteredAgents()
        {
            return registeredAgents.Values.ToList();
        }
        
        public AgentBase GetAgent(string agentId)
        {
            return registeredAgents.TryGetValue(agentId, out var agent) ? agent : null;
        }
        
        public List<CoordinationMessage> GetAgentMessageHistory(string agentId)
        {
            return agentMessageHistory.TryGetValue(agentId, out var history) ? 
                new List<CoordinationMessage>(history) : new List<CoordinationMessage>();
        }
        
        public int GetQueuedMessageCount() => messageQueue.Count;
        
        #endregion
    }
    
    #region Supporting Classes
    
    [System.Serializable]
    public class CoordinationMessage
    {
        public string Id { get; set; }
        public string SenderId { get; set; }
        public string TargetId { get; set; } // Empty for broadcast
        public MessageType Type { get; set; }
        public object Data { get; set; }
        public float Timestamp { get; set; }
        public MessagePriority Priority { get; set; } = MessagePriority.Normal;
        
        public CoordinationMessage Clone()
        {
            return new CoordinationMessage
            {
                Id = Id,
                SenderId = SenderId,
                TargetId = TargetId,
                Type = Type,
                Data = Data,
                Timestamp = Timestamp,
                Priority = Priority
            };
        }
    }
    
    public enum MessageType
    {
        TaskStarted,
        TaskCompleted,
        StateChanged,
        ResourceRequest,
        ResourceResponse,
        CoordinationRequest,
        CoordinationResponse,
        PerformanceUpdate,
        Error,
        Custom
    }
    
    public enum MessagePriority
    {
        Low,
        Normal,
        High,
        Critical
    }
    
    [System.Serializable]
    public class CoordinationMetrics
    {
        public int RegisteredAgents;
        public int MessagesProcessed;
        public int MessagesDropped;
        public float AverageProcessingTime;
        public int QueuedMessages;
        public int ActivePatterns;
        public float Timestamp;
    }
    
    public abstract class CoordinationPattern
    {
        protected CoordinationHub hub;
        
        public abstract void Start(CoordinationHub coordinationHub);
        public abstract void Stop();
        public abstract void Update();
    }
    
    #endregion
}