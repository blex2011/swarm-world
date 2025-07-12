# Unity Swarm Performance Optimization Strategies

## 1. Spatial Partitioning Systems

### Hierarchical Spatial Hash Grid

```csharp
public class HierarchicalSpatialGrid
{
    private struct GridLevel
    {
        public float CellSize;
        public Dictionary<long, List<SwarmAgent>> Cells;
        public int MaxAgentsPerCell;
    }
    
    private GridLevel[] levels;
    private float worldSize;
    private int numLevels;
    
    public HierarchicalSpatialGrid(float worldSize, int numLevels = 3)
    {
        this.worldSize = worldSize;
        this.numLevels = numLevels;
        
        levels = new GridLevel[numLevels];
        for (int i = 0; i < numLevels; i++)
        {
            levels[i] = new GridLevel
            {
                CellSize = worldSize / Mathf.Pow(2, i + 4), // 16, 8, 4...
                Cells = new Dictionary<long, List<SwarmAgent>>(),
                MaxAgentsPerCell = 50 / (i + 1) // 50, 25, 16...
            };
        }
    }
    
    public void UpdateAgent(SwarmAgent agent)
    {
        Vector3 pos = agent.transform.position;
        
        // Remove from old cells
        RemoveFromAllLevels(agent);
        
        // Add to appropriate level based on local density
        int optimalLevel = DetermineOptimalLevel(pos);
        AddToLevel(agent, optimalLevel);
    }
    
    private int DetermineOptimalLevel(Vector3 position)
    {
        // Check density at each level to find optimal placement
        for (int level = 0; level < numLevels; level++)
        {
            long cellKey = GetCellKey(position, levels[level].CellSize);
            
            if (!levels[level].Cells.ContainsKey(cellKey))
                return level;
                
            if (levels[level].Cells[cellKey].Count < levels[level].MaxAgentsPerCell)
                return level;
        }
        
        return numLevels - 1; // Use finest level if all are full
    }
    
    public List<SwarmAgent> GetNeighbors(Vector3 position, float radius)
    {
        HashSet<SwarmAgent> uniqueNeighbors = new HashSet<SwarmAgent>();
        
        // Query appropriate levels based on radius
        for (int level = 0; level < numLevels; level++)
        {
            if (levels[level].CellSize >= radius * 0.5f)
            {
                QueryLevel(position, radius, level, uniqueNeighbors);
                break; // Use the most appropriate level
            }
        }
        
        return new List<SwarmAgent>(uniqueNeighbors);
    }
}
```

### Octree Implementation for 3D Swarms

```csharp
public class SwarmOctree
{
    private class OctreeNode
    {
        public Bounds Bounds;
        public List<SwarmAgent> Agents;
        public OctreeNode[] Children;
        public bool IsLeaf => Children == null;
        
        public OctreeNode(Bounds bounds)
        {
            Bounds = bounds;
            Agents = new List<SwarmAgent>();
        }
        
        public void Subdivide()
        {
            if (!IsLeaf) return;
            
            Children = new OctreeNode[8];
            Vector3 center = Bounds.center;
            Vector3 extents = Bounds.extents * 0.5f;
            
            for (int i = 0; i < 8; i++)
            {
                Vector3 offset = new Vector3(
                    (i & 1) == 0 ? -extents.x : extents.x,
                    (i & 2) == 0 ? -extents.y : extents.y,
                    (i & 4) == 0 ? -extents.z : extents.z
                );
                
                Children[i] = new OctreeNode(new Bounds(center + offset, extents * 2));
            }
            
            // Redistribute agents to children
            foreach (var agent in Agents)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (Children[i].Bounds.Contains(agent.transform.position))
                    {
                        Children[i].Agents.Add(agent);
                        break;
                    }
                }
            }
            
            Agents.Clear();
        }
    }
    
    private OctreeNode root;
    private int maxAgentsPerNode = 20;
    private int maxDepth = 6;
    
    public void Insert(SwarmAgent agent)
    {
        InsertRecursive(root, agent, 0);
    }
    
    private void InsertRecursive(OctreeNode node, SwarmAgent agent, int depth)
    {
        if (!node.Bounds.Contains(agent.transform.position))
            return;
            
        if (node.IsLeaf)
        {
            node.Agents.Add(agent);
            
            if (node.Agents.Count > maxAgentsPerNode && depth < maxDepth)
            {
                node.Subdivide();
                
                // Redistribute current agent to children
                for (int i = 0; i < 8; i++)
                {
                    if (node.Children[i].Bounds.Contains(agent.transform.position))
                    {
                        InsertRecursive(node.Children[i], agent, depth + 1);
                        break;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                InsertRecursive(node.Children[i], agent, depth + 1);
            }
        }
    }
    
    public List<SwarmAgent> QueryRange(Bounds range)
    {
        List<SwarmAgent> result = new List<SwarmAgent>();
        QueryRecursive(root, range, result);
        return result;
    }
}
```

## 2. Level of Detail (LOD) Systems

### Adaptive LOD Based on Distance and Importance

```csharp
public class AdaptiveSwarmLOD : MonoBehaviour
{
    [System.Serializable]
    public class AgentImportance
    {
        public SwarmAgent Agent;
        public float ImportanceScore;
        public bool IsLeader;
        public bool HasSpecialBehavior;
        public float LastUpdateTime;
    }
    
    [System.Serializable]
    public class LODConfiguration
    {
        [Header("Distance Thresholds")]
        public float HighDetailDistance = 30f;
        public float MediumDetailDistance = 60f;
        public float LowDetailDistance = 100f;
        public float CullingDistance = 200f;
        
        [Header("Update Frequencies")]
        public float HighDetailUpdateRate = 60f;   // 60 FPS
        public float MediumDetailUpdateRate = 30f; // 30 FPS
        public float LowDetailUpdateRate = 10f;    // 10 FPS
        public float MinimalUpdateRate = 2f;       // 2 FPS
        
        [Header("Behavior Simplification")]
        public bool SimplifyPhysics = true;
        public bool DisableCollisions = true;
        public bool ReduceNeighborCount = true;
        public bool DisableAnimations = true;
    }
    
    public LODConfiguration Config;
    private Dictionary<SwarmAgent, AgentImportance> agentImportance;
    private Camera mainCamera;
    private Transform cameraTransform;
    
    void Start()
    {
        mainCamera = Camera.main;
        cameraTransform = mainCamera.transform;
        agentImportance = new Dictionary<SwarmAgent, AgentImportance>();
    }
    
    public void RegisterAgent(SwarmAgent agent, float importance = 1f)
    {
        agentImportance[agent] = new AgentImportance
        {
            Agent = agent,
            ImportanceScore = importance,
            IsLeader = false,
            HasSpecialBehavior = false,
            LastUpdateTime = 0f
        };
    }
    
    void Update()
    {
        UpdateAgentLODs();
        ProcessLODUpdates();
    }
    
    void UpdateAgentLODs()
    {
        Vector3 cameraPos = cameraTransform.position;
        
        foreach (var kvp in agentImportance)
        {
            SwarmAgent agent = kvp.Key;
            AgentImportance importance = kvp.Value;
            
            if (agent == null) continue;
            
            float distance = Vector3.Distance(cameraPos, agent.transform.position);
            float adjustedDistance = distance / Mathf.Max(importance.ImportanceScore, 0.1f);
            
            LODLevel lodLevel = DetermineLODLevel(adjustedDistance, importance);
            agent.SetLODLevel(lodLevel);
        }
    }
    
    LODLevel DetermineLODLevel(float distance, AgentImportance importance)
    {
        // Leaders and special agents get higher priority
        if (importance.IsLeader || importance.HasSpecialBehavior)
        {
            distance *= 0.5f; // Treat as closer
        }
        
        if (distance <= Config.HighDetailDistance)
            return LODLevel.High;
        else if (distance <= Config.MediumDetailDistance)
            return LODLevel.Medium;
        else if (distance <= Config.LowDetailDistance)
            return LODLevel.Low;
        else if (distance <= Config.CullingDistance)
            return LODLevel.Minimal;
        else
            return LODLevel.Culled;
    }
    
    void ProcessLODUpdates()
    {
        float currentTime = Time.time;
        
        foreach (var kvp in agentImportance)
        {
            SwarmAgent agent = kvp.Key;
            AgentImportance importance = kvp.Value;
            
            if (ShouldUpdateAgent(agent, importance, currentTime))
            {
                agent.UpdateBehavior(Time.deltaTime);
                importance.LastUpdateTime = currentTime;
            }
        }
    }
    
    bool ShouldUpdateAgent(SwarmAgent agent, AgentImportance importance, float currentTime)
    {
        float updateInterval = GetUpdateInterval(agent.CurrentLODLevel);
        return (currentTime - importance.LastUpdateTime) >= updateInterval;
    }
    
    float GetUpdateInterval(LODLevel level)
    {
        switch (level)
        {
            case LODLevel.High: return 1f / Config.HighDetailUpdateRate;
            case LODLevel.Medium: return 1f / Config.MediumDetailUpdateRate;
            case LODLevel.Low: return 1f / Config.LowDetailUpdateRate;
            case LODLevel.Minimal: return 1f / Config.MinimalUpdateRate;
            default: return float.MaxValue;
        }
    }
}

// Enhanced agent with LOD support
public class LODAwareSwarmAgent : SwarmAgent
{
    public LODLevel CurrentLODLevel { get; private set; }
    
    private Animator animator;
    private Collider agentCollider;
    private Rigidbody agentRigidbody;
    private ParticleSystem[] particleSystems;
    
    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        agentCollider = GetComponent<Collider>();
        agentRigidbody = GetComponent<Rigidbody>();
        particleSystems = GetComponentsInChildren<ParticleSystem>();
    }
    
    public void SetLODLevel(LODLevel level)
    {
        if (CurrentLODLevel == level) return;
        
        CurrentLODLevel = level;
        ApplyLODSettings(level);
    }
    
    void ApplyLODSettings(LODLevel level)
    {
        switch (level)
        {
            case LODLevel.High:
                SetComponentStates(true, true, true, true);
                PerceptionRadius = 10f;
                break;
                
            case LODLevel.Medium:
                SetComponentStates(true, false, true, false);
                PerceptionRadius = 7f;
                break;
                
            case LODLevel.Low:
                SetComponentStates(false, false, true, false);
                PerceptionRadius = 5f;
                break;
                
            case LODLevel.Minimal:
                SetComponentStates(false, false, false, false);
                PerceptionRadius = 3f;
                break;
                
            case LODLevel.Culled:
                gameObject.SetActive(false);
                return;
        }
        
        gameObject.SetActive(true);
    }
    
    void SetComponentStates(bool animEnabled, bool particlesEnabled, 
                           bool colliderEnabled, bool rigidbodyEnabled)
    {
        if (animator) animator.enabled = animEnabled;
        if (agentCollider) agentCollider.enabled = colliderEnabled;
        if (agentRigidbody) agentRigidbody.isKinematic = !rigidbodyEnabled;
        
        foreach (var ps in particleSystems)
        {
            if (particlesEnabled && !ps.isPlaying)
                ps.Play();
            else if (!particlesEnabled && ps.isPlaying)
                ps.Stop();
        }
    }
}
```

## 3. Object Pooling and Memory Management

### Advanced Multi-Type Object Pool

```csharp
public class SwarmObjectPoolManager : MonoBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
        public string PoolName;
        public GameObject Prefab;
        public int InitialSize;
        public int MaxSize;
        public bool CanGrow;
        public float PooledObjectLifetime;
    }
    
    [Header("Pool Configurations")]
    public PoolConfig[] PoolConfigs;
    
    private Dictionary<string, ObjectPool> pools;
    private Dictionary<GameObject, string> objectToPoolMap;
    
    void Awake()
    {
        pools = new Dictionary<string, ObjectPool>();
        objectToPoolMap = new Dictionary<GameObject, string>();
        
        InitializePools();
    }
    
    void InitializePools()
    {
        foreach (var config in PoolConfigs)
        {
            var pool = new ObjectPool(config);
            pools[config.PoolName] = pool;
            
            // Pre-instantiate initial objects
            for (int i = 0; i < config.InitialSize; i++)
            {
                GameObject obj = pool.CreateNewObject();
                pool.ReturnObject(obj);
            }
        }
    }
    
    public T SpawnObject<T>(string poolName, Vector3 position, Quaternion rotation) where T : Component
    {
        if (!pools.ContainsKey(poolName))
        {
            Debug.LogError($"Pool '{poolName}' not found!");
            return null;
        }
        
        GameObject obj = pools[poolName].GetObject();
        if (obj == null) return null;
        
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        
        objectToPoolMap[obj] = poolName;
        
        T component = obj.GetComponent<T>();
        if (component is IPoolable poolable)
        {
            poolable.OnSpawnFromPool();
        }
        
        return component;
    }
    
    public void ReturnObject(GameObject obj)
    {
        if (!objectToPoolMap.ContainsKey(obj))
        {
            Debug.LogWarning($"Object {obj.name} doesn't belong to any pool!");
            return;
        }
        
        string poolName = objectToPoolMap[obj];
        objectToPoolMap.Remove(obj);
        
        if (obj.GetComponent<IPoolable>() is IPoolable poolable)
        {
            poolable.OnReturnToPool();
        }
        
        obj.SetActive(false);
        pools[poolName].ReturnObject(obj);
    }
    
    // Automatic cleanup of long-lived objects
    public void CleanupOldObjects(float maxAge)
    {
        foreach (var pool in pools.Values)
        {
            pool.CleanupOldObjects(maxAge);
        }
    }
}

public class ObjectPool
{
    private Queue<GameObject> availableObjects;
    private HashSet<GameObject> allObjects;
    private Dictionary<GameObject, float> objectSpawnTimes;
    private PoolConfig config;
    private Transform poolParent;
    
    public ObjectPool(PoolConfig config)
    {
        this.config = config;
        availableObjects = new Queue<GameObject>();
        allObjects = new HashSet<GameObject>();
        objectSpawnTimes = new Dictionary<GameObject, float>();
        
        // Create parent object for organization
        poolParent = new GameObject($"Pool_{config.PoolName}").transform;
    }
    
    public GameObject GetObject()
    {
        if (availableObjects.Count > 0)
        {
            return availableObjects.Dequeue();
        }
        
        if (config.CanGrow && allObjects.Count < config.MaxSize)
        {
            return CreateNewObject();
        }
        
        return null; // Pool exhausted
    }
    
    public GameObject CreateNewObject()
    {
        GameObject obj = Object.Instantiate(config.Prefab, poolParent);
        allObjects.Add(obj);
        objectSpawnTimes[obj] = Time.time;
        
        // Add pool reference component
        var poolRef = obj.GetComponent<PoolReference>();
        if (poolRef == null)
            poolRef = obj.AddComponent<PoolReference>();
        poolRef.PoolName = config.PoolName;
        
        return obj;
    }
    
    public void ReturnObject(GameObject obj)
    {
        if (allObjects.Contains(obj))
        {
            availableObjects.Enqueue(obj);
        }
    }
    
    public void CleanupOldObjects(float maxAge)
    {
        var objectsToRemove = new List<GameObject>();
        
        foreach (var kvp in objectSpawnTimes)
        {
            if (Time.time - kvp.Value > maxAge)
            {
                objectsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var obj in objectsToRemove)
        {
            allObjects.Remove(obj);
            objectSpawnTimes.Remove(obj);
            Object.Destroy(obj);
        }
    }
}

// Interface for objects that need pool lifecycle events
public interface IPoolable
{
    void OnSpawnFromPool();
    void OnReturnToPool();
}

// Component to track pool membership
public class PoolReference : MonoBehaviour
{
    public string PoolName;
    
    public void ReturnToPool()
    {
        SwarmObjectPoolManager.Instance.ReturnObject(gameObject);
    }
}
```

## 4. Multithreading with Unity Job System

### Parallel Swarm Behavior Calculation

```csharp
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

[BurstCompile]
public struct ParallelSwarmBehaviorJob : IJobParallelFor
{
    // Read-only data
    [ReadOnly] public NativeArray<float3> Positions;
    [ReadOnly] public NativeArray<float3> Velocities;
    [ReadOnly] public NativeArray<int> AgentTypes;
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public float MaxSpeed;
    [ReadOnly] public float PerceptionRadius;
    [ReadOnly] public SwarmParameters Parameters;
    
    // Spatial partitioning data
    [ReadOnly] public NativeMultiHashMap<int, int> SpatialHashMap;
    [ReadOnly] public float CellSize;
    
    // Output data
    [NativeDisableParallelForRestriction]
    public NativeArray<float3> OutputForces;
    
    public void Execute(int index)
    {
        float3 position = Positions[index];
        float3 velocity = Velocities[index];
        int agentType = AgentTypes[index];
        
        // Get spatial cell
        int cellKey = GetSpatialHash(position, CellSize);
        
        // Calculate swarm forces
        float3 separation = CalculateSeparation(index, position, cellKey);
        float3 alignment = CalculateAlignment(index, position, velocity, cellKey);
        float3 cohesion = CalculateCohesion(index, position, cellKey);
        
        // Apply type-specific weights
        SwarmTypeParameters typeParams = Parameters.GetTypeParameters(agentType);
        
        float3 totalForce = 
            separation * typeParams.SeparationWeight +
            alignment * typeParams.AlignmentWeight +
            cohesion * typeParams.CohesionWeight;
            
        OutputForces[index] = totalForce;
    }
    
    private float3 CalculateSeparation(int agentIndex, float3 position, int cellKey)
    {
        float3 separationForce = float3.zero;
        int neighborCount = 0;
        
        // Check current cell and neighboring cells
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                int neighborCell = cellKey + dx + dz * 1000; // Simple hash offset
                
                if (SpatialHashMap.TryGetFirstValue(neighborCell, out int neighborIndex, out var iterator))
                {
                    do
                    {
                        if (neighborIndex == agentIndex) continue;
                        
                        float3 neighborPos = Positions[neighborIndex];
                        float3 offset = position - neighborPos;
                        float distance = math.length(offset);
                        
                        if (distance < PerceptionRadius && distance > 0.01f)
                        {
                            separationForce += math.normalize(offset) / distance;
                            neighborCount++;
                        }
                    }
                    while (SpatialHashMap.TryGetNextValue(out neighborIndex, ref iterator));
                }
            }
        }
        
        return neighborCount > 0 ? separationForce / neighborCount : float3.zero;
    }
    
    private int GetSpatialHash(float3 position, float cellSize)
    {
        int x = (int)math.floor(position.x / cellSize);
        int z = (int)math.floor(position.z / cellSize);
        return x + z * 1000; // Simple spatial hash
    }
}

// Job scheduler for swarm updates
public class SwarmJobScheduler : MonoBehaviour
{
    private NativeArray<float3> positions;
    private NativeArray<float3> velocities;
    private NativeArray<float3> forces;
    private NativeArray<int> agentTypes;
    private NativeMultiHashMap<int, int> spatialHashMap;
    
    private JobHandle currentJobHandle;
    private SwarmParameters parameters;
    
    public void ScheduleSwarmUpdate(SwarmAgent[] agents, float deltaTime)
    {
        // Wait for previous job to complete
        currentJobHandle.Complete();
        
        // Update native arrays with current agent data
        UpdateNativeArrays(agents);
        
        // Build spatial hash map
        var spatialJob = new BuildSpatialHashJob
        {
            Positions = positions,
            CellSize = parameters.PerceptionRadius * 2f,
            SpatialHashMap = spatialHashMap.AsParallelWriter()
        };
        
        var spatialHandle = spatialJob.Schedule(agents.Length, 32);
        
        // Schedule main behavior job
        var behaviorJob = new ParallelSwarmBehaviorJob
        {
            Positions = positions,
            Velocities = velocities,
            AgentTypes = agentTypes,
            DeltaTime = deltaTime,
            MaxSpeed = parameters.MaxSpeed,
            PerceptionRadius = parameters.PerceptionRadius,
            Parameters = parameters,
            SpatialHashMap = spatialHashMap,
            CellSize = parameters.PerceptionRadius * 2f,
            OutputForces = forces
        };
        
        currentJobHandle = behaviorJob.Schedule(agents.Length, 64, spatialHandle);
        
        // Schedule completion job to apply results
        var applyJob = new ApplyForcesJob
        {
            Forces = forces,
            Velocities = velocities,
            Positions = positions,
            DeltaTime = deltaTime,
            MaxSpeed = parameters.MaxSpeed
        };
        
        currentJobHandle = applyJob.Schedule(agents.Length, 32, currentJobHandle);
    }
    
    public void CompleteJobs()
    {
        currentJobHandle.Complete();
    }
    
    void OnDestroy()
    {
        // Clean up native arrays
        currentJobHandle.Complete();
        
        if (positions.IsCreated) positions.Dispose();
        if (velocities.IsCreated) velocities.Dispose();
        if (forces.IsCreated) forces.Dispose();
        if (agentTypes.IsCreated) agentTypes.Dispose();
        if (spatialHashMap.IsCreated) spatialHashMap.Dispose();
    }
}

[BurstCompile]
public struct BuildSpatialHashJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Positions;
    [ReadOnly] public float CellSize;
    
    [WriteOnly] public NativeMultiHashMap<int, int>.ParallelWriter SpatialHashMap;
    
    public void Execute(int index)
    {
        float3 position = Positions[index];
        int cellKey = GetSpatialHash(position, CellSize);
        SpatialHashMap.Add(cellKey, index);
    }
    
    private int GetSpatialHash(float3 position, float cellSize)
    {
        int x = (int)math.floor(position.x / cellSize);
        int z = (int)math.floor(position.z / cellSize);
        return x + z * 1000;
    }
}

[BurstCompile]
public struct ApplyForcesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> Forces;
    [ReadOnly] public float DeltaTime;
    [ReadOnly] public float MaxSpeed;
    
    public NativeArray<float3> Velocities;
    public NativeArray<float3> Positions;
    
    public void Execute(int index)
    {
        float3 velocity = Velocities[index];
        float3 force = Forces[index];
        
        // Apply force to velocity
        velocity += force * DeltaTime;
        
        // Clamp to max speed
        float speed = math.length(velocity);
        if (speed > MaxSpeed)
        {
            velocity = math.normalize(velocity) * MaxSpeed;
        }
        
        // Update position
        float3 position = Positions[index];
        position += velocity * DeltaTime;
        
        Velocities[index] = velocity;
        Positions[index] = position;
    }
}
```

## 5. GPU Compute Shader Integration

### Compute Shader for Massive Swarms

```hlsl
// SwarmCompute.compute
#pragma kernel CSMain

struct AgentData
{
    float3 position;
    float3 velocity;
    float3 force;
    int agentType;
    float energy;
};

RWStructuredBuffer<AgentData> agentBuffer;
uniform float deltaTime;
uniform float maxSpeed;
uniform float perceptionRadius;
uniform int agentCount;
uniform float separationWeight;
uniform float alignmentWeight;
uniform float cohesionWeight;

[numthreads(64, 1, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    uint index = id.x;
    if (index >= agentCount) return;
    
    AgentData agent = agentBuffer[index];
    float3 force = float3(0, 0, 0);
    
    // Calculate swarm behaviors
    float3 separation = float3(0, 0, 0);
    float3 alignment = float3(0, 0, 0);
    float3 cohesion = float3(0, 0, 0);
    
    int neighborCount = 0;
    
    // Check all other agents (can be optimized with spatial partitioning)
    for (uint i = 0; i < agentCount; i++)
    {
        if (i == index) continue;
        
        AgentData neighbor = agentBuffer[i];
        float3 offset = agent.position - neighbor.position;
        float distance = length(offset);
        
        if (distance < perceptionRadius && distance > 0.01)
        {
            // Separation
            separation += normalize(offset) / distance;
            
            // Alignment
            alignment += neighbor.velocity;
            
            // Cohesion
            cohesion += neighbor.position;
            
            neighborCount++;
        }
    }
    
    if (neighborCount > 0)
    {
        separation /= neighborCount;
        alignment = normalize(alignment / neighborCount - agent.velocity);
        cohesion = normalize(cohesion / neighborCount - agent.position);
        
        force = separation * separationWeight +
                alignment * alignmentWeight +
                cohesion * cohesionWeight;
    }
    
    // Apply force
    agent.velocity += force * deltaTime;
    
    // Clamp speed
    float speed = length(agent.velocity);
    if (speed > maxSpeed)
    {
        agent.velocity = normalize(agent.velocity) * maxSpeed;
    }
    
    // Update position
    agent.position += agent.velocity * deltaTime;
    
    // Store results
    agentBuffer[index] = agent;
}
```

```csharp
// GPU Swarm Manager
public class GPUSwarmManager : MonoBehaviour
{
    [Header("GPU Compute")]
    public ComputeShader SwarmComputeShader;
    public int ThreadGroupSize = 64;
    
    [Header("Swarm Parameters")]
    public int AgentCount = 10000;
    public float MaxSpeed = 5f;
    public float PerceptionRadius = 3f;
    
    private ComputeBuffer agentBuffer;
    private AgentGPUData[] agentData;
    private int kernelIndex;
    
    [System.Serializable]
    public struct AgentGPUData
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public int agentType;
        public float energy;
    }
    
    void Start()
    {
        InitializeGPUSwarm();
    }
    
    void InitializeGPUSwarm()
    {
        // Initialize agent data
        agentData = new AgentGPUData[AgentCount];
        for (int i = 0; i < AgentCount; i++)
        {
            agentData[i] = new AgentGPUData
            {
                position = UnityEngine.Random.insideUnitSphere * 50f,
                velocity = UnityEngine.Random.insideUnitSphere * MaxSpeed,
                force = Vector3.zero,
                agentType = 0,
                energy = 1f
            };
        }
        
        // Create compute buffer
        int stride = System.Runtime.InteropServices.Marshal.SizeOf<AgentGPUData>();
        agentBuffer = new ComputeBuffer(AgentCount, stride);
        agentBuffer.SetData(agentData);
        
        // Get kernel index
        kernelIndex = SwarmComputeShader.FindKernel("CSMain");
        
        // Set shader parameters
        SwarmComputeShader.SetBuffer(kernelIndex, "agentBuffer", agentBuffer);
        SwarmComputeShader.SetFloat("maxSpeed", MaxSpeed);
        SwarmComputeShader.SetFloat("perceptionRadius", PerceptionRadius);
        SwarmComputeShader.SetInt("agentCount", AgentCount);
    }
    
    void Update()
    {
        UpdateGPUSwarm();
    }
    
    void UpdateGPUSwarm()
    {
        // Set frame-specific parameters
        SwarmComputeShader.SetFloat("deltaTime", Time.deltaTime);
        
        // Dispatch compute shader
        int threadGroups = Mathf.CeilToInt((float)AgentCount / ThreadGroupSize);
        SwarmComputeShader.Dispatch(kernelIndex, threadGroups, 1, 1);
        
        // Read back results for rendering (optional)
        if (needsVisualization)
        {
            agentBuffer.GetData(agentData);
            UpdateVisualAgents();
        }
    }
    
    void UpdateVisualAgents()
    {
        // Update a subset of visual agents for rendering
        int visualAgentCount = Mathf.Min(1000, AgentCount);
        
        for (int i = 0; i < visualAgentCount; i++)
        {
            if (visualAgents[i] != null)
            {
                visualAgents[i].transform.position = agentData[i].position;
                visualAgents[i].transform.rotation = Quaternion.LookRotation(agentData[i].velocity);
            }
        }
    }
    
    void OnDestroy()
    {
        agentBuffer?.Release();
    }
}
```

## 6. Memory Optimization Techniques

### Smart Memory Management

```csharp
public class SwarmMemoryManager : MonoBehaviour
{
    [Header("Memory Configuration")]
    public int MaxActiveAgents = 5000;
    public int MemoryPoolSize = 10000;
    public float MemoryCleanupInterval = 30f;
    
    private Stack<SwarmAgent> inactiveAgents;
    private HashSet<SwarmAgent> activeAgents;
    private Dictionary<SwarmAgent, float> agentLastUsed;
    
    // Memory-mapped neighbor lists to avoid allocations
    private Dictionary<SwarmAgent, List<SwarmAgent>> neighborLists;
    private Queue<List<SwarmAgent>> neighborListPool;
    
    void Start()
    {
        InitializeMemoryPools();
        InvokeRepeating(nameof(CleanupMemory), MemoryCleanupInterval, MemoryCleanupInterval);
    }
    
    void InitializeMemoryPools()
    {
        inactiveAgents = new Stack<SwarmAgent>(MemoryPoolSize);
        activeAgents = new HashSet<SwarmAgent>();
        agentLastUsed = new Dictionary<SwarmAgent, float>();
        
        neighborLists = new Dictionary<SwarmAgent, List<SwarmAgent>>();
        neighborListPool = new Queue<List<SwarmAgent>>();
        
        // Pre-allocate neighbor lists
        for (int i = 0; i < MemoryPoolSize; i++)
        {
            neighborListPool.Enqueue(new List<SwarmAgent>(50));
        }
    }
    
    public SwarmAgent GetAgent()
    {
        if (activeAgents.Count >= MaxActiveAgents)
        {
            // Remove least recently used agent
            RemoveLRUAgent();
        }
        
        SwarmAgent agent;
        if (inactiveAgents.Count > 0)
        {
            agent = inactiveAgents.Pop();
        }
        else
        {
            // Create new agent if pool is empty
            agent = CreateNewAgent();
        }
        
        activeAgents.Add(agent);
        agentLastUsed[agent] = Time.time;
        
        return agent;
    }
    
    public void ReturnAgent(SwarmAgent agent)
    {
        if (activeAgents.Remove(agent))
        {
            agentLastUsed.Remove(agent);
            
            // Return neighbor list to pool
            if (neighborLists.TryGetValue(agent, out List<SwarmAgent> neighbors))
            {
                neighbors.Clear();
                neighborListPool.Enqueue(neighbors);
                neighborLists.Remove(agent);
            }
            
            agent.Reset();
            inactiveAgents.Push(agent);
        }
    }
    
    public List<SwarmAgent> GetNeighborList(SwarmAgent agent)
    {
        if (!neighborLists.TryGetValue(agent, out List<SwarmAgent> neighbors))
        {
            if (neighborListPool.Count > 0)
            {
                neighbors = neighborListPool.Dequeue();
            }
            else
            {
                neighbors = new List<SwarmAgent>(50);
            }
            
            neighborLists[agent] = neighbors;
        }
        
        agentLastUsed[agent] = Time.time;
        return neighbors;
    }
    
    void RemoveLRUAgent()
    {
        SwarmAgent lruAgent = null;
        float oldestTime = float.MaxValue;
        
        foreach (var kvp in agentLastUsed)
        {
            if (kvp.Value < oldestTime)
            {
                oldestTime = kvp.Value;
                lruAgent = kvp.Key;
            }
        }
        
        if (lruAgent != null)
        {
            ReturnAgent(lruAgent);
        }
    }
    
    void CleanupMemory()
    {
        // Force garbage collection of unused memory
        System.GC.Collect();
        
        // Trim inactive agent pool if too large
        while (inactiveAgents.Count > MemoryPoolSize * 0.8f)
        {
            var agent = inactiveAgents.Pop();
            if (agent != null)
            {
                DestroyImmediate(agent.gameObject);
            }
        }
        
        // Trim neighbor list pool
        while (neighborListPool.Count > MemoryPoolSize * 0.5f)
        {
            neighborListPool.Dequeue();
        }
    }
}
```

These optimization strategies provide comprehensive performance improvements for Unity swarm systems, enabling smooth operation with thousands of agents while maintaining visual quality and responsive behavior.