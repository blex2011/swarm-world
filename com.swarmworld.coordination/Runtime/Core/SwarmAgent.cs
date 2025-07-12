using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SwarmWorld
{
    /// <summary>
    /// Core swarm agent component with neural coordination capabilities
    /// </summary>
    public class SwarmAgent : MonoBehaviour
    {
        [Header("Agent Configuration")]
        [SerializeField] private string agentId = System.Guid.NewGuid().ToString();
        [SerializeField] private SwarmAgentData agentData = SwarmAgentData.Default;
        [SerializeField] private bool enableNeuralLearning = true;
        [SerializeField] private bool enableMemoryCoordination = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private Color debugColor = Color.cyan;

        // Internal state
        private float3 velocity;
        private float3 acceleration;
        private NativeArray<SwarmNeighbor> neighbors;
        private bool isInitialized = false;

        // Coordination components
        private SwarmCoordinator coordinator;
        private MemoryManager memoryManager;
        private NeuralPatternLearner neuralLearner;

        // Performance tracking
        private float lastUpdateTime;
        private int framesSinceUpdate;

        public string AgentId => agentId;
        public SwarmAgentData Data => agentData;
        public float3 Velocity => velocity;
        public float3 Position => transform.position;
        public bool IsInitialized => isInitialized;

        private void Awake()
        {
            InitializeAgent();
        }

        private void Start()
        {
            RegisterWithCoordinator();
        }

        private void Update()
        {
            if (!isInitialized) return;

            UpdateSwarmBehavior();
            ApplyMovement();
            UpdatePerformanceMetrics();
        }

        private void OnDestroy()
        {
            CleanupAgent();
        }

        private void InitializeAgent()
        {
            // Initialize native arrays
            neighbors = new NativeArray<SwarmNeighbor>(agentData.maxNeighbors, Allocator.Persistent);
            
            // Initialize coordination components
            coordinator = FindObjectOfType<SwarmCoordinator>();
            
            if (enableMemoryCoordination)
            {
                memoryManager = new MemoryManager(agentId);
            }

            if (enableNeuralLearning)
            {
                neuralLearner = new NeuralPatternLearner(agentId);
            }

            isInitialized = true;
        }

        private void RegisterWithCoordinator()
        {
            coordinator?.RegisterAgent(this);
        }

        private void UpdateSwarmBehavior()
        {
            // Find neighbors using spatial partitioning
            FindNeighbors();

            // Calculate swarm forces
            float3 separation = CalculateSeparation();
            float3 alignment = CalculateAlignment();
            float3 cohesion = CalculateCohesion();
            float3 targeting = CalculateTargeting();

            // Combine forces with neural learning weights
            float3 combinedForce = 
                separation * agentData.separationWeight +
                alignment * agentData.alignmentWeight +
                cohesion * agentData.cohesionWeight +
                targeting * agentData.targetWeight;

            // Apply neural learning adjustments
            if (enableNeuralLearning && neuralLearner != null)
            {
                combinedForce = neuralLearner.AdjustForce(combinedForce, neighbors);
            }

            acceleration = combinedForce;
        }

        private void FindNeighbors()
        {
            if (coordinator != null)
            {
                coordinator.FindNeighbors(this, ref neighbors);
            }
        }

        private float3 CalculateSeparation()
        {
            float3 separationForce = float3.zero;
            int separationCount = 0;

            for (int i = 0; i < neighbors.Length; i++)
            {
                if (!neighbors[i].isValid) continue;

                float3 offset = Position - neighbors[i].position;
                float distance = math.length(offset);

                if (distance > 0.01f && distance < agentData.separationRadius)
                {
                    separationForce += math.normalize(offset) / distance;
                    separationCount++;
                }
            }

            return separationCount > 0 ? separationForce / separationCount : float3.zero;
        }

        private float3 CalculateAlignment()
        {
            if (neighbors.Length == 0) return float3.zero;

            float3 averageVelocity = float3.zero;
            int alignmentCount = 0;

            for (int i = 0; i < neighbors.Length; i++)
            {
                if (!neighbors[i].isValid) continue;

                averageVelocity += neighbors[i].velocity;
                alignmentCount++;
            }

            if (alignmentCount == 0) return float3.zero;

            averageVelocity /= alignmentCount;
            return math.normalize(averageVelocity - velocity);
        }

        private float3 CalculateCohesion()
        {
            if (neighbors.Length == 0) return float3.zero;

            float3 centerOfMass = float3.zero;
            int cohesionCount = 0;

            for (int i = 0; i < neighbors.Length; i++)
            {
                if (!neighbors[i].isValid) continue;

                centerOfMass += neighbors[i].position;
                cohesionCount++;
            }

            if (cohesionCount == 0) return float3.zero;

            centerOfMass /= cohesionCount;
            return math.normalize(centerOfMass - Position);
        }

        private float3 CalculateTargeting()
        {
            if (coordinator != null && coordinator.HasGlobalTarget)
            {
                float3 targetDirection = coordinator.GlobalTarget - Position;
                return math.normalize(targetDirection);
            }

            return float3.zero;
        }

        private void ApplyMovement()
        {
            // Apply acceleration
            velocity += acceleration * Time.deltaTime;

            // Clamp to max speed
            float speed = math.length(velocity);
            if (speed > agentData.maxSpeed)
            {
                velocity = math.normalize(velocity) * agentData.maxSpeed;
            }

            // Update position
            transform.position += (Vector3)velocity * Time.deltaTime;

            // Update rotation to face movement direction
            if (speed > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation((Vector3)velocity);
            }
        }

        private void UpdatePerformanceMetrics()
        {
            framesSinceUpdate++;
            
            if (Time.time - lastUpdateTime >= 1.0f)
            {
                // Store performance data
                if (enableMemoryCoordination && memoryManager != null)
                {
                    var performanceData = new PerformanceData
                    {
                        fps = framesSinceUpdate / (Time.time - lastUpdateTime),
                        neighborCount = GetValidNeighborCount(),
                        speed = math.length(velocity),
                        timestamp = Time.time
                    };

                    memoryManager.StorePerformanceData(performanceData);
                }

                lastUpdateTime = Time.time;
                framesSinceUpdate = 0;
            }
        }

        private int GetValidNeighborCount()
        {
            int count = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i].isValid) count++;
            }
            return count;
        }

        private void CleanupAgent()
        {
            coordinator?.UnregisterAgent(this);
            
            if (neighbors.IsCreated)
            {
                neighbors.Dispose();
            }

            memoryManager?.Dispose();
            neuralLearner?.Dispose();
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !isInitialized) return;

            Gizmos.color = debugColor;
            
            // Draw perception radius
            Gizmos.DrawWireSphere(transform.position, agentData.perceptionRadius);
            
            // Draw velocity
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)velocity);
            
            // Draw neighbors
            Gizmos.color = Color.yellow;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i].isValid)
                {
                    Gizmos.DrawLine(transform.position, (Vector3)neighbors[i].position);
                }
            }
        }

        // Public API for testing
        public void SetAgentData(SwarmAgentData newData)
        {
            agentData = newData;
        }

        public void SetVelocity(float3 newVelocity)
        {
            velocity = newVelocity;
        }

        public void ForceUpdate()
        {
            if (isInitialized)
            {
                UpdateSwarmBehavior();
                ApplyMovement();
            }
        }

        public SwarmNeighbor[] GetNeighbors()
        {
            return neighbors.ToArray();
        }

        public PerformanceData GetCurrentPerformance()
        {
            return new PerformanceData
            {
                fps = framesSinceUpdate / (Time.time - lastUpdateTime),
                neighborCount = GetValidNeighborCount(),
                speed = math.length(velocity),
                timestamp = Time.time
            };
        }
    }
}