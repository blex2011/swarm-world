# Unity-Specific Swarm Implementation Approaches

## 1. MonoBehaviour vs ECS Architecture Comparison

### Traditional MonoBehaviour Approach

```csharp
// Traditional component-based agent
[System.Serializable]
public class SwarmAgentComponent : MonoBehaviour
{
    [Header("Swarm Properties")]
    public float MaxSpeed = 5f;
    public float PerceptionRadius = 10f;
    public LayerMask SwarmLayer = -1;
    
    [Header("Behavior Weights")]
    [Range(0f, 5f)] public float SeparationWeight = 1.5f;
    [Range(0f, 5f)] public float AlignmentWeight = 1f;
    [Range(0f, 5f)] public float CohesionWeight = 1f;
    [Range(0f, 5f)] public float TargetWeight = 2f;
    
    // Component references
    private Transform cachedTransform;
    private Rigidbody cachedRigidbody;
    private Collider agentCollider;
    private Animator agentAnimator;
    
    // State
    public Vector3 Velocity { get; private set; }
    public Vector3 TargetPosition { get; set; }
    
    // Neighbor tracking
    private List<SwarmAgentComponent> neighbors = new List<SwarmAgentComponent>();
    private Collider[] overlapResults = new Collider[50];
    
    void Awake()
    {
        CacheComponents();
    }
    
    void CacheComponents()
    {
        cachedTransform = transform;
        cachedRigidbody = GetComponent<Rigidbody>();
        agentCollider = GetComponent<Collider>();
        agentAnimator = GetComponent<Animator>();
    }
    
    void FixedUpdate()
    {
        FindNeighbors();
        UpdateSwarmBehavior();
        ApplyMovement();
    }
    
    void FindNeighbors()
    {
        neighbors.Clear();
        
        int count = Physics.OverlapSphereNonAlloc(
            cachedTransform.position,
            PerceptionRadius,
            overlapResults,
            SwarmLayer
        );
        
        for (int i = 0; i < count; i++)
        {
            var neighbor = overlapResults[i].GetComponent<SwarmAgentComponent>();
            if (neighbor != null && neighbor != this)
            {
                neighbors.Add(neighbor);
            }
        }
    }
    
    void UpdateSwarmBehavior()
    {
        Vector3 separation = CalculateSeparation();
        Vector3 alignment = CalculateAlignment();
        Vector3 cohesion = CalculateCohesion();
        Vector3 targeting = CalculateTargeting();
        
        Vector3 acceleration = 
            separation * SeparationWeight +
            alignment * AlignmentWeight +
            cohesion * CohesionWeight +
            targeting * TargetWeight;
            
        Velocity += acceleration * Time.fixedDeltaTime;
        Velocity = Vector3.ClampMagnitude(Velocity, MaxSpeed);
    }
    
    void ApplyMovement()
    {
        if (cachedRigidbody != null)
        {
            cachedRigidbody.velocity = Velocity;
        }
        else
        {
            cachedTransform.position += Velocity * Time.fixedDeltaTime;
        }
        
        if (Velocity.magnitude > 0.1f)
        {
            cachedTransform.rotation = Quaternion.LookRotation(Velocity);
        }
        
        // Update animator if present
        agentAnimator?.SetFloat("Speed", Velocity.magnitude / MaxSpeed);
    }
    
    Vector3 CalculateSeparation()
    {
        Vector3 separationForce = Vector3.zero;
        
        foreach (var neighbor in neighbors)
        {
            Vector3 offset = cachedTransform.position - neighbor.transform.position;
            float distance = offset.magnitude;
            
            if (distance > 0 && distance < PerceptionRadius * 0.5f)
            {
                separationForce += offset.normalized / distance;
            }
        }
        
        return separationForce;
    }
    
    Vector3 CalculateAlignment()
    {
        if (neighbors.Count == 0) return Vector3.zero;
        
        Vector3 averageVelocity = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            averageVelocity += neighbor.Velocity;
        }
        
        averageVelocity /= neighbors.Count;
        return (averageVelocity - Velocity).normalized;
    }
    
    Vector3 CalculateCohesion()
    {
        if (neighbors.Count == 0) return Vector3.zero;
        
        Vector3 centerOfMass = Vector3.zero;
        foreach (var neighbor in neighbors)
        {
            centerOfMass += neighbor.transform.position;
        }
        
        centerOfMass /= neighbors.Count;
        return (centerOfMass - cachedTransform.position).normalized;
    }
    
    Vector3 CalculateTargeting()
    {
        if (TargetPosition == Vector3.zero) return Vector3.zero;
        return (TargetPosition - cachedTransform.position).normalized;
    }
}

// Manager for MonoBehaviour swarms
public class MonoBehaviourSwarmManager : MonoBehaviour
{
    [Header("Swarm Configuration")]
    public GameObject AgentPrefab;
    public int SwarmSize = 100;
    public float SpawnRadius = 20f;
    
    [Header("Global Targets")]
    public Transform[] Waypoints;
    public float WaypointRadius = 5f;
    
    private List<SwarmAgentComponent> agents = new List<SwarmAgentComponent>();
    private int currentWaypointIndex = 0;
    
    void Start()
    {
        SpawnAgents();
        InvokeRepeating(nameof(UpdateGlobalBehavior), 1f, 0.5f);
    }
    
    void SpawnAgents()
    {
        for (int i = 0; i < SwarmSize; i++)
        {
            Vector3 spawnPos = transform.position + Random.insideUnitSphere * SpawnRadius;
            GameObject agentGO = Instantiate(AgentPrefab, spawnPos, Random.rotation);
            
            SwarmAgentComponent agent = agentGO.GetComponent<SwarmAgentComponent>();
            if (agent != null)
            {
                agents.Add(agent);
                if (Waypoints.Length > 0)
                {
                    agent.TargetPosition = Waypoints[currentWaypointIndex].position;
                }
            }
        }
    }
    
    void UpdateGlobalBehavior()
    {
        if (Waypoints.Length == 0) return;
        
        Vector3 currentWaypoint = Waypoints[currentWaypointIndex].position;
        Vector3 swarmCenter = CalculateSwarmCenter();
        
        // Check if swarm reached waypoint
        if (Vector3.Distance(swarmCenter, currentWaypoint) < WaypointRadius)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % Waypoints.Length;
            Vector3 newTarget = Waypoints[currentWaypointIndex].position;
            
            foreach (var agent in agents)
            {
                agent.TargetPosition = newTarget;
            }
        }
    }
    
    Vector3 CalculateSwarmCenter()
    {
        Vector3 center = Vector3.zero;
        foreach (var agent in agents)
        {
            center += agent.transform.position;
        }
        return center / agents.Count;
    }
}
```

### DOTS/ECS Approach

```csharp
// ECS Components
using Unity.Entities;
using Unity.Mathematics;

[System.Serializable]
public struct SwarmAgent : IComponentData
{
    public float3 Velocity;
    public float MaxSpeed;
    public float PerceptionRadius;
    public int SwarmID;
    public float SeparationWeight;
    public float AlignmentWeight;
    public float CohesionWeight;
}

[System.Serializable]
public struct SwarmTarget : IComponentData
{
    public float3 TargetPosition;
    public float TargetWeight;
}

[System.Serializable]
public struct SwarmNeighbor : IBufferElementData
{
    public Entity NeighborEntity;
    public float3 Position;
    public float3 Velocity;
    public float Distance;
}

// Authoring component for conversion
public class SwarmAgentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [Header("Swarm Properties")]
    public float MaxSpeed = 5f;
    public float PerceptionRadius = 10f;
    public int SwarmID = 0;
    
    [Header("Behavior Weights")]
    public float SeparationWeight = 1.5f;
    public float AlignmentWeight = 1f;
    public float CohesionWeight = 1f;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SwarmAgent
        {
            Velocity = float3.zero,
            MaxSpeed = MaxSpeed,
            PerceptionRadius = PerceptionRadius,
            SwarmID = SwarmID,
            SeparationWeight = SeparationWeight,
            AlignmentWeight = AlignmentWeight,
            CohesionWeight = CohesionWeight
        });
        
        dstManager.AddComponentData(entity, new SwarmTarget
        {
            TargetPosition = float3.zero,
            TargetWeight = 1f
        });
        
        dstManager.AddBuffer<SwarmNeighbor>(entity);
    }
}

// Systems
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public class SwarmNeighborSystem : SystemBase
{
    private EntityQuery swarmQuery;
    
    protected override void OnCreate()
    {
        swarmQuery = GetEntityQuery(
            ComponentType.ReadOnly<SwarmAgent>(),
            ComponentType.ReadOnly<Translation>(),
            ComponentType.ReadWrite<DynamicBuffer<SwarmNeighbor>>()
        );
    }
    
    protected override void OnUpdate()
    {
        var agentArray = swarmQuery.ToEntityArray(Allocator.TempJob);
        var positionArray = swarmQuery.ToComponentDataArray<Translation>(Allocator.TempJob);
        var velocityArray = swarmQuery.ToComponentDataArray<SwarmAgent>(Allocator.TempJob);
        
        var findNeighborsJob = new FindNeighborsJob
        {
            AgentEntities = agentArray,
            Positions = positionArray,
            SwarmAgents = velocityArray,
            NeighborBufferLookup = GetBufferLookup<SwarmNeighbor>()
        };
        
        Dependency = findNeighborsJob.ScheduleParallel(agentArray.Length, 32, Dependency);
        
        agentArray.Dispose(Dependency);
        positionArray.Dispose(Dependency);
        velocityArray.Dispose(Dependency);
    }
}

[BurstCompile]
public struct FindNeighborsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<Entity> AgentEntities;
    [ReadOnly] public NativeArray<Translation> Positions;
    [ReadOnly] public NativeArray<SwarmAgent> SwarmAgents;
    
    [NativeDisableParallelForRestriction]
    public BufferLookup<SwarmNeighbor> NeighborBufferLookup;
    
    public void Execute(int index)
    {
        Entity entity = AgentEntities[index];
        float3 position = Positions[index].Value;
        SwarmAgent agent = SwarmAgents[index];
        
        var neighborBuffer = NeighborBufferLookup[entity];
        neighborBuffer.Clear();
        
        // Find neighbors within perception radius
        for (int i = 0; i < AgentEntities.Length; i++)
        {
            if (i == index) continue;
            
            SwarmAgent otherAgent = SwarmAgents[i];
            if (otherAgent.SwarmID != agent.SwarmID) continue;
            
            float3 otherPosition = Positions[i].Value;
            float distance = math.distance(position, otherPosition);
            
            if (distance <= agent.PerceptionRadius)
            {
                neighborBuffer.Add(new SwarmNeighbor
                {
                    NeighborEntity = AgentEntities[i],
                    Position = otherPosition,
                    Velocity = otherAgent.Velocity,
                    Distance = distance
                });
            }
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(SwarmNeighborSystem))]
public class SwarmBehaviorSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;
        
        Entities
            .ForEach((ref SwarmAgent swarmAgent,
                     ref Translation translation,
                     in DynamicBuffer<SwarmNeighbor> neighbors,
                     in SwarmTarget target) =>
            {
                float3 separation = CalculateSeparation(translation.Value, neighbors);
                float3 alignment = CalculateAlignment(swarmAgent.Velocity, neighbors);
                float3 cohesion = CalculateCohesion(translation.Value, neighbors);
                float3 targeting = CalculateTargeting(translation.Value, target.TargetPosition);
                
                float3 acceleration = 
                    separation * swarmAgent.SeparationWeight +
                    alignment * swarmAgent.AlignmentWeight +
                    cohesion * swarmAgent.CohesionWeight +
                    targeting * target.TargetWeight;
                
                swarmAgent.Velocity += acceleration * deltaTime;
                float speed = math.length(swarmAgent.Velocity);
                
                if (speed > swarmAgent.MaxSpeed)
                {
                    swarmAgent.Velocity = math.normalize(swarmAgent.Velocity) * swarmAgent.MaxSpeed;
                }
                
                translation.Value += swarmAgent.Velocity * deltaTime;
            })
            .ScheduleParallel();
    }
    
    private float3 CalculateSeparation(float3 position, DynamicBuffer<SwarmNeighbor> neighbors)
    {
        float3 separationForce = float3.zero;
        
        for (int i = 0; i < neighbors.Length; i++)
        {
            SwarmNeighbor neighbor = neighbors[i];
            float3 offset = position - neighbor.Position;
            float distance = neighbor.Distance;
            
            if (distance > 0.01f && distance < 2f) // Close neighbors
            {
                separationForce += math.normalize(offset) / distance;
            }
        }
        
        return separationForce;
    }
    
    private float3 CalculateAlignment(float3 velocity, DynamicBuffer<SwarmNeighbor> neighbors)
    {
        if (neighbors.Length == 0) return float3.zero;
        
        float3 averageVelocity = float3.zero;
        for (int i = 0; i < neighbors.Length; i++)
        {
            averageVelocity += neighbors[i].Velocity;
        }
        
        averageVelocity /= neighbors.Length;
        return math.normalize(averageVelocity - velocity);
    }
    
    private float3 CalculateCohesion(float3 position, DynamicBuffer<SwarmNeighbor> neighbors)
    {
        if (neighbors.Length == 0) return float3.zero;
        
        float3 centerOfMass = float3.zero;
        for (int i = 0; i < neighbors.Length; i++)
        {
            centerOfMass += neighbors[i].Position;
        }
        
        centerOfMass /= neighbors.Length;
        return math.normalize(centerOfMass - position);
    }
    
    private float3 CalculateTargeting(float3 position, float3 target)
    {
        if (math.lengthsq(target) == 0) return float3.zero;
        return math.normalize(target - position);
    }
}

// ECS Swarm Manager
public class ECSwarmManager : MonoBehaviour
{
    [Header("ECS Swarm Configuration")]
    public GameObject AgentPrefab;
    public int SwarmSize = 1000;
    public float SpawnRadius = 50f;
    
    private EntityManager entityManager;
    private Entity swarmTargetEntity;
    
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        SpawnECSSwarm();
        CreateSwarmTarget();
    }
    
    void SpawnECSSwarm()
    {
        for (int i = 0; i < SwarmSize; i++)
        {
            Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * SpawnRadius;
            GameObject agentGO = Instantiate(AgentPrefab, spawnPos, UnityEngine.Random.rotation);
            
            // Entity will be created automatically via conversion system
        }
    }
    
    void CreateSwarmTarget()
    {
        swarmTargetEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(swarmTargetEntity, new SwarmTarget
        {
            TargetPosition = new float3(0, 0, 0),
            TargetWeight = 1f
        });
    }
    
    void Update()
    {
        // Update target based on mouse position or other input
        if (Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f)
            );
            
            UpdateSwarmTarget(mouseWorldPos);
        }
    }
    
    void UpdateSwarmTarget(Vector3 newTarget)
    {
        var query = entityManager.CreateEntityQuery(typeof(SwarmTarget));
        var entities = query.ToEntityArray(Allocator.Temp);
        
        foreach (var entity in entities)
        {
            entityManager.SetComponentData(entity, new SwarmTarget
            {
                TargetPosition = newTarget,
                TargetWeight = 1f
            });
        }
        
        entities.Dispose();
        query.Dispose();
    }
}
```

## 2. Coroutines vs Job System Implementation

### Coroutine-Based Approach

```csharp
public class CoroutineSwarmManager : MonoBehaviour
{
    [Header("Coroutine Configuration")]
    public int BatchSize = 20;
    public float UpdateInterval = 0.02f; // 50Hz
    public bool UseAdaptiveBatching = true;
    
    private List<SwarmAgentComponent> agents = new List<SwarmAgentComponent>();
    private WaitForSeconds updateWait;
    private WaitForEndOfFrame frameWait;
    
    void Start()
    {
        updateWait = new WaitForSeconds(UpdateInterval);
        frameWait = new WaitForEndOfFrame();
        
        StartCoroutine(SwarmUpdateCoroutine());
        StartCoroutine(PerformanceMonitorCoroutine());
    }
    
    IEnumerator SwarmUpdateCoroutine()
    {
        while (true)
        {
            yield return updateWait;
            
            if (agents.Count == 0) continue;
            
            int currentBatchSize = UseAdaptiveBatching ? 
                CalculateAdaptiveBatchSize() : BatchSize;
            
            yield return StartCoroutine(ProcessSwarmInBatches(currentBatchSize));
        }
    }
    
    IEnumerator ProcessSwarmInBatches(int batchSize)
    {
        for (int i = 0; i < agents.Count; i += batchSize)
        {
            int endIndex = Mathf.Min(i + batchSize, agents.Count);
            
            // Process batch
            for (int j = i; j < endIndex; j++)
            {
                if (agents[j] != null && agents[j].isActiveAndEnabled)
                {
                    ProcessAgent(agents[j]);
                }
            }
            
            // Yield control back to Unity
            yield return frameWait;
        }
    }
    
    void ProcessAgent(SwarmAgentComponent agent)
    {
        // Custom processing logic that doesn't rely on Update()
        agent.FindNeighborsManual();
        agent.UpdateSwarmBehaviorManual();
        agent.ApplyMovementManual();
    }
    
    int CalculateAdaptiveBatchSize()
    {
        float frameTime = Time.unscaledDeltaTime;
        float targetFrameTime = 1f / 60f; // 60 FPS target
        
        if (frameTime > targetFrameTime * 1.2f)
        {
            // Running slow, reduce batch size
            return Mathf.Max(10, BatchSize - 5);
        }
        else if (frameTime < targetFrameTime * 0.8f)
        {
            // Running fast, increase batch size
            return Mathf.Min(50, BatchSize + 5);
        }
        
        return BatchSize;
    }
    
    IEnumerator PerformanceMonitorCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);
            
            float avgFrameRate = 1f / Time.unscaledDeltaTime;
            Debug.Log($"Swarm Performance: {avgFrameRate:F1} FPS, {agents.Count} agents");
            
            if (avgFrameRate < 30f)
            {
                // Automatically reduce quality if performance is poor
                ReduceSwarmQuality();
            }
        }
    }
    
    void ReduceSwarmQuality()
    {
        // Reduce perception radius to improve performance
        foreach (var agent in agents)
        {
            agent.PerceptionRadius *= 0.9f;
        }
        
        BatchSize = Mathf.Max(5, BatchSize - 2);
        UpdateInterval = Mathf.Min(0.05f, UpdateInterval + 0.005f);
    }
}

// Extended agent for manual control
public class ManualControlSwarmAgent : SwarmAgentComponent
{
    private bool manualControl = false;
    
    void Update()
    {
        if (!manualControl)
        {
            base.Update(); // Use normal update
        }
    }
    
    public void EnableManualControl()
    {
        manualControl = true;
    }
    
    public void FindNeighborsManual()
    {
        // Manual neighbor finding logic
        base.FindNeighbors();
    }
    
    public void UpdateSwarmBehaviorManual()
    {
        // Manual behavior update
        base.UpdateSwarmBehavior();
    }
    
    public void ApplyMovementManual()
    {
        // Manual movement application
        base.ApplyMovement();
    }
}
```

### Advanced Job System Implementation

```csharp
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public class JobSystemSwarmManager : MonoBehaviour
{
    [Header("Job System Configuration")]
    public int MaxAgents = 5000;
    public int JobBatchSize = 64;
    public bool UseMultipleJobs = true;
    
    // Native arrays for job data
    private NativeArray<float3> positions;
    private NativeArray<float3> velocities;
    private NativeArray<float3> accelerations;
    private NativeArray<SwarmAgentData> agentData;
    private NativeArray<int> agentTypes;
    
    // Spatial partitioning
    private NativeMultiHashMap<int, int> spatialHashMap;
    private NativeArray<int> cellKeys;
    
    // Job handles for dependency tracking
    private JobHandle currentJobHandle;
    private JobHandle neighborJobHandle;
    private JobHandle behaviorJobHandle;
    private JobHandle movementJobHandle;
    
    // Performance tracking
    private float jobExecutionTime;
    private int frameCount;
    
    [System.Serializable]
    public struct SwarmAgentData
    {
        public float MaxSpeed;
        public float PerceptionRadius;
        public float SeparationWeight;
        public float AlignmentWeight;
        public float CohesionWeight;
        public int SwarmID;
    }
    
    void Start()
    {
        InitializeNativeArrays();
        InitializeSpatialHashing();
    }
    
    void InitializeNativeArrays()
    {
        positions = new NativeArray<float3>(MaxAgents, Allocator.Persistent);
        velocities = new NativeArray<float3>(MaxAgents, Allocator.Persistent);
        accelerations = new NativeArray<float3>(MaxAgents, Allocator.Persistent);
        agentData = new NativeArray<SwarmAgentData>(MaxAgents, Allocator.Persistent);
        agentTypes = new NativeArray<int>(MaxAgents, Allocator.Persistent);
        
        // Initialize with random data
        for (int i = 0; i < MaxAgents; i++)
        {
            positions[i] = UnityEngine.Random.insideUnitSphere * 50f;
            velocities[i] = UnityEngine.Random.insideUnitSphere * 3f;
            agentData[i] = new SwarmAgentData
            {
                MaxSpeed = 5f,
                PerceptionRadius = 8f,
                SeparationWeight = 1.5f,
                AlignmentWeight = 1f,
                CohesionWeight = 1f,
                SwarmID = i % 3 // Multiple swarms
            };
        }
    }
    
    void InitializeSpatialHashing()
    {
        spatialHashMap = new NativeMultiHashMap<int, int>(MaxAgents * 4, Allocator.Persistent);
        cellKeys = new NativeArray<int>(MaxAgents, Allocator.Persistent);
    }
    
    void Update()
    {
        var startTime = Time.realtimeSinceStartup;
        
        // Complete previous frame's jobs
        currentJobHandle.Complete();
        
        if (UseMultipleJobs)
        {
            ScheduleMultipleJobs();
        }
        else
        {
            ScheduleSingleJob();
        }
        
        jobExecutionTime = Time.realtimeSinceStartup - startTime;
        frameCount++;
        
        if (frameCount % 60 == 0)
        {
            Debug.Log($"Job execution time: {jobExecutionTime * 1000f:F2}ms");
        }
    }
    
    void ScheduleMultipleJobs()
    {
        float deltaTime = Time.deltaTime;
        float cellSize = 16f; // Spatial partitioning cell size
        
        // 1. Build spatial hash map
        spatialHashMap.Clear();
        
        var spatialHashJob = new BuildSpatialHashMapJob
        {
            Positions = positions,
            CellKeys = cellKeys,
            SpatialHashMap = spatialHashMap.AsParallelWriter(),
            CellSize = cellSize
        };
        
        neighborJobHandle = spatialHashJob.Schedule(MaxAgents, JobBatchSize);
        
        // 2. Calculate swarm behaviors
        var behaviorJob = new SwarmBehaviorJob
        {
            Positions = positions,
            Velocities = velocities,
            AgentData = agentData,
            AgentTypes = agentTypes,
            SpatialHashMap = spatialHashMap,
            CellKeys = cellKeys,
            CellSize = cellSize,
            DeltaTime = deltaTime,
            OutputAccelerations = accelerations
        };
        
        behaviorJobHandle = behaviorJob.Schedule(MaxAgents, JobBatchSize, neighborJobHandle);
        
        // 3. Apply movement
        var movementJob = new ApplyMovementJob
        {
            Positions = positions,
            Velocities = velocities,
            Accelerations = accelerations,
            AgentData = agentData,
            DeltaTime = deltaTime
        };
        
        movementJobHandle = movementJob.Schedule(MaxAgents, JobBatchSize, behaviorJobHandle);
        
        // 4. Optional: Constraint application
        var constraintJob = new ApplyConstraintsJob
        {
            Positions = positions,
            Velocities = velocities,
            BoundaryMin = new float3(-100, -10, -100),
            BoundaryMax = new float3(100, 50, 100)
        };
        
        currentJobHandle = constraintJob.Schedule(MaxAgents, JobBatchSize, movementJobHandle);
    }
    
    void ScheduleSingleJob()
    {
        var combinedJob = new CombinedSwarmJob
        {
            Positions = positions,
            Velocities = velocities,
            AgentData = agentData,
            DeltaTime = Time.deltaTime,
            CellSize = 16f
        };
        
        currentJobHandle = combinedJob.Schedule(MaxAgents, JobBatchSize);
    }
    
    void OnDestroy()
    {
        // Always complete jobs before disposing arrays
        currentJobHandle.Complete();
        
        // Dispose native arrays
        if (positions.IsCreated) positions.Dispose();
        if (velocities.IsCreated) velocities.Dispose();
        if (accelerations.IsCreated) accelerations.Dispose();
        if (agentData.IsCreated) agentData.Dispose();
        if (agentTypes.IsCreated) agentTypes.Dispose();
        if (spatialHashMap.IsCreated) spatialHashMap.Dispose();
        if (cellKeys.IsCreated) cellKeys.Dispose();
    }
}

// Individual job implementations
[BurstCompile]
public struct BuildSpatialHashMapJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Positions;
    [ReadOnly] public float CellSize;
    
    [WriteOnly] public NativeArray<int> CellKeys;
    [WriteOnly] public NativeMultiHashMap<int, int>.ParallelWriter SpatialHashMap;
    
    public void Execute(int index)
    {
        float3 position = Positions[index];
        int cellKey = GetSpatialHash(position, CellSize);
        
        CellKeys[index] = cellKey;
        SpatialHashMap.Add(cellKey, index);
    }
    
    private int GetSpatialHash(float3 position, float cellSize)
    {
        int3 cell = (int3)math.floor(position / cellSize);
        return cell.x + cell.y * 1000 + cell.z * 1000000;
    }
}

[BurstCompile]
public struct SwarmBehaviorJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Positions;
    [ReadOnly] public NativeArray<float3> Velocities;
    [ReadOnly] public NativeArray<JobSystemSwarmManager.SwarmAgentData> AgentData;
    [ReadOnly] public NativeArray<int> AgentTypes;
    [ReadOnly] public NativeMultiHashMap<int, int> SpatialHashMap;
    [ReadOnly] public NativeArray<int> CellKeys;
    [ReadOnly] public float CellSize;
    [ReadOnly] public float DeltaTime;
    
    [WriteOnly] public NativeArray<float3> OutputAccelerations;
    
    public void Execute(int index)
    {
        float3 position = Positions[index];
        float3 velocity = Velocities[index];
        var data = AgentData[index];
        int cellKey = CellKeys[index];
        
        float3 separation = CalculateSeparation(index, position, cellKey, data);
        float3 alignment = CalculateAlignment(index, position, velocity, cellKey, data);
        float3 cohesion = CalculateCohesion(index, position, cellKey, data);
        
        float3 acceleration = 
            separation * data.SeparationWeight +
            alignment * data.AlignmentWeight +
            cohesion * data.CohesionWeight;
            
        OutputAccelerations[index] = acceleration;
    }
    
    private float3 CalculateSeparation(int agentIndex, float3 position, int cellKey, 
                                      JobSystemSwarmManager.SwarmAgentData data)
    {
        float3 separationForce = float3.zero;
        int neighborCount = 0;
        
        // Check surrounding cells
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    int neighborCellKey = cellKey + dx + dy * 1000 + dz * 1000000;
                    
                    if (SpatialHashMap.TryGetFirstValue(neighborCellKey, out int neighborIndex, out var iterator))
                    {
                        do
                        {
                            if (neighborIndex == agentIndex) continue;
                            if (AgentData[neighborIndex].SwarmID != data.SwarmID) continue;
                            
                            float3 neighborPos = Positions[neighborIndex];
                            float3 offset = position - neighborPos;
                            float distance = math.length(offset);
                            
                            if (distance < data.PerceptionRadius * 0.5f && distance > 0.01f)
                            {
                                separationForce += math.normalize(offset) / distance;
                                neighborCount++;
                            }
                        }
                        while (SpatialHashMap.TryGetNextValue(out neighborIndex, ref iterator));
                    }
                }
            }
        }
        
        return neighborCount > 0 ? separationForce / neighborCount : float3.zero;
    }
    
    private float3 CalculateAlignment(int agentIndex, float3 position, float3 velocity, 
                                     int cellKey, JobSystemSwarmManager.SwarmAgentData data)
    {
        float3 averageVelocity = float3.zero;
        int neighborCount = 0;
        
        // Similar spatial hash iteration logic...
        // Implementation similar to separation but using velocities
        
        return neighborCount > 0 ? math.normalize(averageVelocity / neighborCount - velocity) : float3.zero;
    }
    
    private float3 CalculateCohesion(int agentIndex, float3 position, int cellKey, 
                                    JobSystemSwarmManager.SwarmAgentData data)
    {
        float3 centerOfMass = float3.zero;
        int neighborCount = 0;
        
        // Similar spatial hash iteration logic...
        // Implementation similar to separation but using positions
        
        return neighborCount > 0 ? math.normalize(centerOfMass / neighborCount - position) : float3.zero;
    }
}

[BurstCompile]
public struct ApplyMovementJob : IJobParallelFor
{
    public NativeArray<float3> Positions;
    public NativeArray<float3> Velocities;
    [ReadOnly] public NativeArray<float3> Accelerations;
    [ReadOnly] public NativeArray<JobSystemSwarmManager.SwarmAgentData> AgentData;
    [ReadOnly] public float DeltaTime;
    
    public void Execute(int index)
    {
        float3 acceleration = Accelerations[index];
        float3 velocity = Velocities[index];
        float3 position = Positions[index];
        var data = AgentData[index];
        
        // Apply acceleration
        velocity += acceleration * DeltaTime;
        
        // Clamp to max speed
        float speed = math.length(velocity);
        if (speed > data.MaxSpeed)
        {
            velocity = math.normalize(velocity) * data.MaxSpeed;
        }
        
        // Update position
        position += velocity * DeltaTime;
        
        Velocities[index] = velocity;
        Positions[index] = position;
    }
}

[BurstCompile]
public struct ApplyConstraintsJob : IJobParallelFor
{
    public NativeArray<float3> Positions;
    public NativeArray<float3> Velocities;
    [ReadOnly] public float3 BoundaryMin;
    [ReadOnly] public float3 BoundaryMax;
    
    public void Execute(int index)
    {
        float3 position = Positions[index];
        float3 velocity = Velocities[index];
        
        // Boundary constraints with bounce
        if (position.x < BoundaryMin.x || position.x > BoundaryMax.x)
        {
            velocity.x = -velocity.x;
            position.x = math.clamp(position.x, BoundaryMin.x, BoundaryMax.x);
        }
        
        if (position.y < BoundaryMin.y || position.y > BoundaryMax.y)
        {
            velocity.y = -velocity.y;
            position.y = math.clamp(position.y, BoundaryMin.y, BoundaryMax.y);
        }
        
        if (position.z < BoundaryMin.z || position.z > BoundaryMax.z)
        {
            velocity.z = -velocity.z;
            position.z = math.clamp(position.z, BoundaryMin.z, BoundaryMax.z);
        }
        
        Positions[index] = position;
        Velocities[index] = velocity;
    }
}

// Combined job for simpler cases
[BurstCompile]
public struct CombinedSwarmJob : IJobParallelFor
{
    public NativeArray<float3> Positions;
    public NativeArray<float3> Velocities;
    [ReadOnly] public NativeArray<JobSystemSwarmManager.SwarmAgentData> AgentData;
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public float CellSize;
    
    public void Execute(int index)
    {
        // Simplified combined logic
        float3 position = Positions[index];
        float3 velocity = Velocities[index];
        var data = AgentData[index];
        
        // Simple neighbor finding without spatial hash
        float3 force = CalculateSimpleSwarmForce(index, position, velocity, data);
        
        // Apply force
        velocity += force * DeltaTime;
        float speed = math.length(velocity);
        if (speed > data.MaxSpeed)
        {
            velocity = math.normalize(velocity) * data.MaxSpeed;
        }
        
        position += velocity * DeltaTime;
        
        Positions[index] = position;
        Velocities[index] = velocity;
    }
    
    private float3 CalculateSimpleSwarmForce(int index, float3 position, float3 velocity, 
                                           JobSystemSwarmManager.SwarmAgentData data)
    {
        float3 force = float3.zero;
        
        // Simplified force calculation
        // This could include basic separation, alignment, and cohesion
        // without the complexity of spatial hashing
        
        return force;
    }
}
```

This comprehensive implementation comparison shows the trade-offs between MonoBehaviour simplicity and ECS performance, as well as the differences between coroutine-based frame-spreading and job system parallelization. The choice depends on your specific requirements for agent count, complexity, and target platforms.