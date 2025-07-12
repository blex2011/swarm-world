using System;
using System.Collections.Generic;
using UnityEngine;
using SwarmFlow.Core;
using SwarmFlow.Coordination;

namespace SwarmFlow.Agents
{
    /// <summary>
    /// Base class for all swarm agents in Unity.
    /// Provides core functionality for task execution, coordination, and lifecycle management.
    /// </summary>
    public abstract class AgentBase : MonoBehaviour
    {
        [Header("Agent Configuration")]
        [SerializeField] protected string agentName;
        [SerializeField] protected AgentType agentType;
        [SerializeField] protected List<AgentCapability> capabilities = new List<AgentCapability>();
        [SerializeField] protected float maxWorkload = 3.0f;
        [SerializeField] protected bool enableDebugLogs = true;
        
        [Header("Performance Monitoring")]
        [SerializeField] protected float lastResponseTime;
        [SerializeField] protected int tasksCompleted;
        [SerializeField] protected int tasksFailed;
        [SerializeField] protected AgentState currentState = AgentState.Idle;
        
        // Core properties
        public string AgentId { get; private set; }
        public AgentType Type => agentType;
        public bool IsAvailable => currentState == AgentState.Idle;
        public float CurrentWorkload => activeTasks.Count;
        public float LastResponseTime => lastResponseTime;
        public AgentMetrics Metrics => GetCurrentMetrics();
        
        // Dependencies
        protected SwarmManager swarmManager;
        protected CoordinationHub coordinationHub;
        
        // Task management
        private List<SwarmTask> activeTasks = new List<SwarmTask>();
        private Queue<SwarmTask> taskQueue = new Queue<SwarmTask>();
        private Dictionary<string, object> agentMemory = new Dictionary<string, object>();
        
        // Events
        public event Action<AgentBase, SwarmTask> OnTaskStarted;
        public event Action<AgentBase, SwarmTask, bool> OnTaskCompleted;
        public event Action<AgentBase, AgentState> OnStateChanged;
        
        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            GenerateAgentId();
            InitializeAgent();
        }
        
        protected virtual void Start()
        {
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Agent started with capabilities: {string.Join(", ", capabilities)}");
        }
        
        protected virtual void Update()
        {
            ProcessTaskQueue();
            UpdatePerformanceMetrics();
        }
        
        protected virtual void OnDestroy()
        {
            CleanupAgent();
        }
        
        #endregion
        
        #region Initialization
        
        private void GenerateAgentId()
        {
            AgentId = $"{agentType}_{UnityEngine.Random.Range(1000, 9999)}_{DateTime.Now.Ticks % 10000}";
        }
        
        protected virtual void InitializeAgent()
        {
            if (string.IsNullOrEmpty(agentName))
                agentName = $"{agentType} Agent";
            
            SetState(AgentState.Initializing);
        }
        
        public virtual void Initialize(SwarmManager manager, CoordinationHub hub)
        {
            swarmManager = manager;
            coordinationHub = hub;
            
            OnInitialized();
            SetState(AgentState.Idle);
            
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Initialized and ready for tasks");
        }
        
        protected virtual void OnInitialized()
        {
            // Override in derived classes for custom initialization
        }
        
        #endregion
        
        #region Task Management
        
        public virtual bool CanHandleTask(SwarmTask task)
        {
            if (CurrentWorkload >= maxWorkload)
                return false;
            
            return HasRequiredCapability(task.Type);
        }
        
        private bool HasRequiredCapability(TaskType taskType)
        {
            var requiredCapability = GetRequiredCapability(taskType);
            return capabilities.Contains(requiredCapability);
        }
        
        private AgentCapability GetRequiredCapability(TaskType taskType)
        {
            return taskType switch
            {
                TaskType.SceneAnalysis => AgentCapability.SceneAnalysis,
                TaskType.AssetOptimization => AgentCapability.AssetOptimization,
                TaskType.CodeGeneration => AgentCapability.CodeGeneration,
                TaskType.PerformanceAnalysis => AgentCapability.PerformanceAnalysis,
                TaskType.QualityAssurance => AgentCapability.QualityAssurance,
                TaskType.Documentation => AgentCapability.Documentation,
                TaskType.Debugging => AgentCapability.Debugging,
                _ => AgentCapability.General
            };
        }
        
        public virtual void AssignTask(SwarmTask task)
        {
            if (!CanHandleTask(task))
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[{AgentId}] Cannot handle task: {task.Id}");
                return;
            }
            
            taskQueue.Enqueue(task);
            
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Task queued: {task.Id}");
        }
        
        private void ProcessTaskQueue()
        {
            if (taskQueue.Count == 0 || CurrentWorkload >= maxWorkload)
                return;
            
            var task = taskQueue.Dequeue();
            StartTask(task);
        }
        
        private void StartTask(SwarmTask task)
        {
            activeTasks.Add(task);
            SetState(AgentState.Working);
            
            var startTime = Time.time;
            OnTaskStarted?.Invoke(this, task);
            
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Starting task: {task.Id}");
            
            // Execute task asynchronously
            StartCoroutine(ExecuteTaskCoroutine(task, startTime));
        }
        
        private System.Collections.IEnumerator ExecuteTaskCoroutine(SwarmTask task, float startTime)
        {
            bool success = false;
            
            try
            {
                // Coordinate with other agents before execution
                yield return StartCoroutine(CoordinateTaskExecution(task));
                
                // Execute the actual task
                yield return StartCoroutine(ExecuteTaskImplementation(task));
                
                success = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{AgentId}] Task execution failed: {ex.Message}");
                success = false;
            }
            finally
            {
                CompleteTask(task, success, Time.time - startTime);
            }
        }
        
        protected virtual System.Collections.IEnumerator CoordinateTaskExecution(SwarmTask task)
        {
            // Send coordination message to other agents
            coordinationHub?.BroadcastMessage(new CoordinationMessage
            {
                SenderId = AgentId,
                Type = MessageType.TaskStarted,
                Data = new { TaskId = task.Id, TaskType = task.Type }
            });
            
            yield return null;
        }
        
        protected abstract System.Collections.IEnumerator ExecuteTaskImplementation(SwarmTask task);
        
        private void CompleteTask(SwarmTask task, bool success, float executionTime)
        {
            activeTasks.Remove(task);
            lastResponseTime = executionTime;
            
            if (success)
                tasksCompleted++;
            else
                tasksFailed++;
            
            // Update state
            if (activeTasks.Count == 0)
                SetState(AgentState.Idle);
            
            // Notify completion
            OnTaskCompleted?.Invoke(this, task, success);
            task.OnComplete?.Invoke(success);
            
            // Coordinate task completion
            coordinationHub?.BroadcastMessage(new CoordinationMessage
            {
                SenderId = AgentId,
                Type = MessageType.TaskCompleted,
                Data = new { TaskId = task.Id, Success = success, ExecutionTime = executionTime }
            });
            
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Task completed: {task.Id} (Success: {success}, Time: {executionTime:F2}s)");
        }
        
        #endregion
        
        #region State Management
        
        protected void SetState(AgentState newState)
        {
            if (currentState != newState)
            {
                var previousState = currentState;
                currentState = newState;
                
                OnStateChanged?.Invoke(this, newState);
                
                if (enableDebugLogs)
                    Debug.Log($"[{AgentId}] State changed: {previousState} -> {newState}");
            }
        }
        
        #endregion
        
        #region Memory Management
        
        protected void StoreMemory(string key, object value)
        {
            agentMemory[key] = value;
        }
        
        protected T GetMemory<T>(string key, T defaultValue = default)
        {
            if (agentMemory.TryGetValue(key, out var value) && value is T)
                return (T)value;
            
            return defaultValue;
        }
        
        protected bool HasMemory(string key)
        {
            return agentMemory.ContainsKey(key);
        }
        
        protected void ClearMemory()
        {
            agentMemory.Clear();
        }
        
        #endregion
        
        #region Performance Monitoring
        
        private void UpdatePerformanceMetrics()
        {
            // Update performance metrics periodically
            // This could include CPU usage, memory consumption, etc.
        }
        
        private AgentMetrics GetCurrentMetrics()
        {
            return new AgentMetrics
            {
                AgentId = AgentId,
                TasksCompleted = tasksCompleted,
                TasksFailed = tasksFailed,
                AverageResponseTime = lastResponseTime,
                CurrentWorkload = CurrentWorkload,
                State = currentState,
                SuccessRate = tasksCompleted + tasksFailed > 0 ? (float)tasksCompleted / (tasksCompleted + tasksFailed) : 0f
            };
        }
        
        #endregion
        
        #region Coordination
        
        public virtual void HandleCoordinationMessage(CoordinationMessage message)
        {
            // Override in derived classes to handle specific coordination messages
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Received coordination message from {message.SenderId}: {message.Type}");
        }
        
        protected void SendCoordinationMessage(string targetAgentId, MessageType type, object data)
        {
            coordinationHub?.SendMessage(new CoordinationMessage
            {
                SenderId = AgentId,
                TargetId = targetAgentId,
                Type = type,
                Data = data
            });
        }
        
        #endregion
        
        #region Cleanup
        
        protected virtual void CleanupAgent()
        {
            // Cancel all active tasks
            foreach (var task in activeTasks.ToArray())
            {
                CompleteTask(task, false, 0f);
            }
            
            activeTasks.Clear();
            taskQueue.Clear();
            ClearMemory();
            
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Agent cleanup completed");
        }
        
        #endregion
    }
    
    #region Supporting Enums and Classes
    
    public enum AgentType
    {
        SceneArchitect,
        AssetOptimizer,
        CodeGenerator,
        PerformanceAnalyst,
        QualityAssurance,
        Coordinator,
        Researcher,
        Monitor
    }
    
    public enum AgentState
    {
        Idle,
        Initializing,
        Working,
        Coordinating,
        Error,
        Shutdown
    }
    
    public enum AgentCapability
    {
        General,
        SceneAnalysis,
        AssetOptimization,
        CodeGeneration,
        PerformanceAnalysis,
        QualityAssurance,
        Documentation,
        Debugging,
        Coordination,
        Monitoring
    }
    
    [System.Serializable]
    public class AgentMetrics
    {
        public string AgentId;
        public int TasksCompleted;
        public int TasksFailed;
        public float AverageResponseTime;
        public float CurrentWorkload;
        public AgentState State;
        public float SuccessRate;
    }
    
    #endregion
}