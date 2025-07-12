using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SwarmFlow.Coordination;
using SwarmFlow.Agents;

namespace SwarmFlow.Core
{
    /// <summary>
    /// Main coordination system for managing Unity swarm agents.
    /// Handles agent lifecycle, task distribution, and performance monitoring.
    /// </summary>
    [System.Serializable]
    public class SwarmManager : MonoBehaviour
    {
        [Header("Swarm Configuration")]
        [SerializeField] private SwarmConfig swarmConfig;
        [SerializeField] private int maxAgents = 8;
        [SerializeField] private float coordinationInterval = 0.1f;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        
        [Header("Agent Management")]
        [SerializeField] private List<AgentBase> activeAgents = new List<AgentBase>();
        [SerializeField] private Transform agentContainer;
        
        private CoordinationHub coordinationHub;
        private Dictionary<Type, GameObject> agentPrefabs = new Dictionary<Type, GameObject>();
        private Queue<SwarmTask> taskQueue = new Queue<SwarmTask>();
        private SwarmMetrics metrics = new SwarmMetrics();
        
        public static SwarmManager Instance { get; private set; }
        
        // Events
        public event Action<AgentBase> OnAgentSpawned;
        public event Action<AgentBase> OnAgentDestroyed;
        public event Action<SwarmTask> OnTaskCompleted;
        public event Action<SwarmMetrics> OnMetricsUpdated;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSwarm();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            StartCoroutine(CoordinationLoop());
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                ShutdownSwarm();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeSwarm()
        {
            // Create agent container if not assigned
            if (agentContainer == null)
            {
                var containerGO = new GameObject("SwarmAgents");
                containerGO.transform.SetParent(transform);
                agentContainer = containerGO.transform;
            }
            
            // Initialize coordination hub
            coordinationHub = gameObject.AddComponent<CoordinationHub>();
            coordinationHub.Initialize(this);
            
            // Load swarm configuration
            if (swarmConfig == null)
            {
                swarmConfig = CreateDefaultConfig();
            }
            
            // Register agent prefabs
            RegisterAgentPrefabs();
            
            Debug.Log($"[SwarmManager] Initialized with max agents: {maxAgents}");
        }
        
        private SwarmConfig CreateDefaultConfig()
        {
            var config = ScriptableObject.CreateInstance<SwarmConfig>();
            config.topology = SwarmTopology.Hierarchical;
            config.coordinationStrategy = CoordinationStrategy.Adaptive;
            config.performanceThresholds = new PerformanceThresholds
            {
                maxResponseTime = 1.0f,
                minSuccessRate = 0.85f,
                maxMemoryUsage = 500 * 1024 * 1024 // 500MB
            };
            return config;
        }
        
        private void RegisterAgentPrefabs()
        {
            // Load agent prefabs from Resources
            var prefabs = Resources.LoadAll<GameObject>("SwarmFlow/Agents");
            foreach (var prefab in prefabs)
            {
                var agentComponent = prefab.GetComponent<AgentBase>();
                if (agentComponent != null)
                {
                    agentPrefabs[agentComponent.GetType()] = prefab;
                }
            }
        }
        
        #endregion
        
        #region Agent Management
        
        public T SpawnAgent<T>(Vector3 position = default, Quaternion rotation = default) where T : AgentBase
        {
            if (activeAgents.Count >= maxAgents)
            {
                Debug.LogWarning($"[SwarmManager] Cannot spawn agent: Max agents ({maxAgents}) reached");
                return null;
            }
            
            var agentType = typeof(T);
            if (!agentPrefabs.ContainsKey(agentType))
            {
                Debug.LogError($"[SwarmManager] No prefab registered for agent type: {agentType.Name}");
                return null;
            }
            
            var agentGO = Instantiate(agentPrefabs[agentType], position, rotation, agentContainer);
            var agent = agentGO.GetComponent<T>();
            
            if (agent != null)
            {
                agent.Initialize(this, coordinationHub);
                activeAgents.Add(agent);
                coordinationHub.RegisterAgent(agent);
                
                OnAgentSpawned?.Invoke(agent);
                
                Debug.Log($"[SwarmManager] Spawned agent: {agent.AgentId} ({agentType.Name})");
                return agent;
            }
            
            Destroy(agentGO);
            return null;
        }
        
        public void DestroyAgent(AgentBase agent)
        {
            if (agent != null && activeAgents.Contains(agent))
            {
                activeAgents.Remove(agent);
                coordinationHub.UnregisterAgent(agent);
                
                OnAgentDestroyed?.Invoke(agent);
                
                Debug.Log($"[SwarmManager] Destroyed agent: {agent.AgentId}");
                Destroy(agent.gameObject);
            }
        }
        
        public List<T> GetAgents<T>() where T : AgentBase
        {
            return activeAgents.OfType<T>().ToList();
        }
        
        public AgentBase GetAgent(string agentId)
        {
            return activeAgents.FirstOrDefault(a => a.AgentId == agentId);
        }
        
        #endregion
        
        #region Task Management
        
        public void EnqueueTask(SwarmTask task)
        {
            taskQueue.Enqueue(task);
            Debug.Log($"[SwarmManager] Enqueued task: {task.Id} (Priority: {task.Priority})");
        }
        
        public void AssignTask(SwarmTask task, AgentBase agent)
        {
            if (agent != null && activeAgents.Contains(agent))
            {
                agent.AssignTask(task);
                Debug.Log($"[SwarmManager] Assigned task {task.Id} to agent {agent.AgentId}");
            }
        }
        
        private void ProcessTaskQueue()
        {
            while (taskQueue.Count > 0)
            {
                var task = taskQueue.Dequeue();
                var suitableAgent = FindSuitableAgent(task);
                
                if (suitableAgent != null)
                {
                    AssignTask(task, suitableAgent);
                }
                else
                {
                    // Re-queue if no suitable agent available
                    taskQueue.Enqueue(task);
                    break;
                }
            }
        }
        
        private AgentBase FindSuitableAgent(SwarmTask task)
        {
            // Find available agent with matching capabilities
            return activeAgents
                .Where(a => a.IsAvailable && a.CanHandleTask(task))
                .OrderBy(a => a.CurrentWorkload)
                .FirstOrDefault();
        }
        
        #endregion
        
        #region Coordination Loop
        
        private System.Collections.IEnumerator CoordinationLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(coordinationInterval);
                
                ProcessTaskQueue();
                UpdateMetrics();
                OptimizeTopology();
            }
        }
        
        private void UpdateMetrics()
        {
            if (!enablePerformanceMonitoring) return;
            
            metrics.totalAgents = activeAgents.Count;
            metrics.activeAgents = activeAgents.Count(a => !a.IsAvailable);
            metrics.tasksInQueue = taskQueue.Count;
            metrics.averageResponseTime = CalculateAverageResponseTime();
            metrics.memoryUsage = GC.GetTotalMemory(false);
            metrics.timestamp = Time.time;
            
            OnMetricsUpdated?.Invoke(metrics);
        }
        
        private float CalculateAverageResponseTime()
        {
            var responseTimes = activeAgents
                .Where(a => a.LastResponseTime > 0)
                .Select(a => a.LastResponseTime);
            
            return responseTimes.Any() ? responseTimes.Average() : 0f;
        }
        
        private void OptimizeTopology()
        {
            // Implement adaptive topology optimization based on performance metrics
            if (metrics.averageResponseTime > swarmConfig.performanceThresholds.maxResponseTime)
            {
                coordinationHub.OptimizeTopology();
            }
        }
        
        #endregion
        
        #region Public API
        
        public SwarmMetrics GetMetrics() => metrics;
        
        public int GetActiveAgentCount() => activeAgents.Count;
        
        public int GetQueuedTaskCount() => taskQueue.Count;
        
        public void SetMaxAgents(int newMax)
        {
            maxAgents = Mathf.Max(1, newMax);
            Debug.Log($"[SwarmManager] Max agents set to: {maxAgents}");
        }
        
        public void SetCoordinationInterval(float interval)
        {
            coordinationInterval = Mathf.Max(0.01f, interval);
        }
        
        #endregion
        
        #region Shutdown
        
        private void ShutdownSwarm()
        {
            // Cleanup all agents
            foreach (var agent in activeAgents.ToList())
            {
                DestroyAgent(agent);
            }
            
            activeAgents.Clear();
            taskQueue.Clear();
            
            Debug.Log("[SwarmManager] Swarm shutdown complete");
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    [System.Serializable]
    public class SwarmMetrics
    {
        public int totalAgents;
        public int activeAgents;
        public int tasksInQueue;
        public float averageResponseTime;
        public long memoryUsage;
        public float timestamp;
    }
    
    [System.Serializable]
    public class SwarmTask
    {
        public string Id { get; set; }
        public TaskPriority Priority { get; set; }
        public TaskType Type { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public float TimeoutSeconds { get; set; }
        public Action<bool> OnComplete { get; set; }
        
        public SwarmTask(string id, TaskType type, TaskPriority priority = TaskPriority.Medium)
        {
            Id = id;
            Type = type;
            Priority = priority;
            Parameters = new Dictionary<string, object>();
            TimeoutSeconds = 30f;
        }
    }
    
    public enum TaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Critical = 3
    }
    
    public enum TaskType
    {
        SceneAnalysis,
        AssetOptimization,
        CodeGeneration,
        PerformanceAnalysis,
        QualityAssurance,
        Documentation,
        Debugging
    }
    
    public enum SwarmTopology
    {
        Hierarchical,
        Mesh,
        Ring,
        Star
    }
    
    public enum CoordinationStrategy
    {
        Static,
        Adaptive,
        Performance,
        Balanced
    }
    
    [System.Serializable]
    public class PerformanceThresholds
    {
        public float maxResponseTime = 1.0f;
        public float minSuccessRate = 0.85f;
        public long maxMemoryUsage = 500 * 1024 * 1024; // 500MB
    }
    
    #endregion
}