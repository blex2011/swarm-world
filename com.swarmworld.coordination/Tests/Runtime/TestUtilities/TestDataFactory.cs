using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace SwarmWorld.Tests
{
    /// <summary>
    /// Factory for creating test data and mock objects
    /// </summary>
    public static class TestDataFactory
    {
        /// <summary>
        /// Creates a configured SwarmAgent for testing
        /// </summary>
        public static SwarmAgent CreateTestAgent(string name = "TestAgent", SwarmAgentData? data = null)
        {
            var gameObject = new GameObject(name);
            var agent = gameObject.AddComponent<SwarmAgent>();
            
            if (data.HasValue)
            {
                agent.SetAgentData(data.Value);
            }
            else
            {
                agent.SetAgentData(SwarmAgentData.Default);
            }

            return agent;
        }

        /// <summary>
        /// Creates multiple test agents in a formation
        /// </summary>
        public static List<SwarmAgent> CreateTestSwarm(int agentCount, SwarmFormation formation = SwarmFormation.Random)
        {
            var agents = new List<SwarmAgent>();

            for (int i = 0; i < agentCount; i++)
            {
                var agent = CreateTestAgent($"Agent_{i}");
                agent.transform.position = GetFormationPosition(i, agentCount, formation);
                agents.Add(agent);
            }

            return agents;
        }

        /// <summary>
        /// Creates a test coordinator with specified configuration
        /// </summary>
        public static SwarmCoordinator CreateTestCoordinator(string name = "TestCoordinator")
        {
            var gameObject = new GameObject(name);
            return gameObject.AddComponent<SwarmCoordinator>();
        }

        /// <summary>
        /// Creates test neighbor data
        /// </summary>
        public static SwarmNeighbor[] CreateTestNeighbors(int count, float3 centerPosition, float radius = 10f)
        {
            var neighbors = new SwarmNeighbor[count];
            var random = new Unity.Mathematics.Random(12345);

            for (int i = 0; i < count; i++)
            {
                var offset = random.NextFloat3Direction() * random.NextFloat(1f, radius);
                var position = centerPosition + offset;
                var velocity = random.NextFloat3Direction() * random.NextFloat(1f, 5f);
                var distance = math.length(offset);

                neighbors[i] = new SwarmNeighbor($"neighbor_{i}", position, velocity, distance);
            }

            return neighbors;
        }

        /// <summary>
        /// Creates performance test data
        /// </summary>
        public static PerformanceData[] CreateTestPerformanceData(int count, float baselineFPS = 60f)
        {
            var data = new PerformanceData[count];
            var random = new Unity.Mathematics.Random(54321);

            for (int i = 0; i < count; i++)
            {
                data[i] = new PerformanceData
                {
                    fps = baselineFPS + random.NextFloat(-10f, 10f),
                    neighborCount = random.NextInt(0, 50),
                    speed = random.NextFloat(0f, 10f),
                    timestamp = i * 0.016f, // 60 FPS timing
                    memoryUsage = random.NextFloat(50f, 200f),
                    cpuTime = random.NextFloat(0.5f, 2.0f)
                };
            }

            return data;
        }

        /// <summary>
        /// Creates various SwarmAgentData configurations for testing
        /// </summary>
        public static class AgentDataVariants
        {
            public static SwarmAgentData FastAgent => new SwarmAgentData
            {
                maxSpeed = 15f,
                perceptionRadius = 5f,
                separationRadius = 2f,
                maxNeighbors = 10,
                separationWeight = 2f,
                alignmentWeight = 0.5f,
                cohesionWeight = 0.5f,
                targetWeight = 3f
            };

            public static SwarmAgentData SlowAgent => new SwarmAgentData
            {
                maxSpeed = 1f,
                perceptionRadius = 20f,
                separationRadius = 5f,
                maxNeighbors = 50,
                separationWeight = 0.5f,
                alignmentWeight = 2f,
                cohesionWeight = 2f,
                targetWeight = 0.5f
            };

            public static SwarmAgentData SocialAgent => new SwarmAgentData
            {
                maxSpeed = 5f,
                perceptionRadius = 25f,
                separationRadius = 3f,
                maxNeighbors = 100,
                separationWeight = 0.8f,
                alignmentWeight = 1.5f,
                cohesionWeight = 2.5f,
                targetWeight = 1f
            };

            public static SwarmAgentData IndependentAgent => new SwarmAgentData
            {
                maxSpeed = 8f,
                perceptionRadius = 8f,
                separationRadius = 6f,
                maxNeighbors = 5,
                separationWeight = 3f,
                alignmentWeight = 0.2f,
                cohesionWeight = 0.2f,
                targetWeight = 2f
            };

            public static SwarmAgentData[] GetAllVariants()
            {
                return new[]
                {
                    SwarmAgentData.Default,
                    SwarmAgentData.HighPerformance,
                    SwarmAgentData.Research,
                    FastAgent,
                    SlowAgent,
                    SocialAgent,
                    IndependentAgent
                };
            }
        }

        /// <summary>
        /// Mock memory manager for testing
        /// </summary>
        public class MockMemoryManager
        {
            private Dictionary<string, object> memory = new Dictionary<string, object>();
            
            public void Store(string key, object value)
            {
                memory[key] = value;
            }

            public T Retrieve<T>(string key)
            {
                return memory.ContainsKey(key) ? (T)memory[key] : default(T);
            }

            public bool HasKey(string key)
            {
                return memory.ContainsKey(key);
            }

            public void Clear()
            {
                memory.Clear();
            }

            public int Count => memory.Count;
        }

        /// <summary>
        /// Mock neural learner for testing
        /// </summary>
        public class MockNeuralLearner
        {
            public List<float3> recordedForces = new List<float3>();
            public List<SwarmNeighbor[]> recordedNeighbors = new List<SwarmNeighbor[]>();
            public float3 nextForceAdjustment = float3.zero;

            public float3 AdjustForce(float3 originalForce, SwarmNeighbor[] neighbors)
            {
                recordedForces.Add(originalForce);
                recordedNeighbors.Add(neighbors);
                return originalForce + nextForceAdjustment;
            }

            public void SetNextAdjustment(float3 adjustment)
            {
                nextForceAdjustment = adjustment;
            }

            public void Reset()
            {
                recordedForces.Clear();
                recordedNeighbors.Clear();
                nextForceAdjustment = float3.zero;
            }
        }

        private static Vector3 GetFormationPosition(int index, int totalCount, SwarmFormation formation)
        {
            switch (formation)
            {
                case SwarmFormation.Circle:
                    var angle = (float)index / totalCount * 2f * Mathf.PI;
                    return new Vector3(Mathf.Cos(angle) * 10f, 0, Mathf.Sin(angle) * 10f);

                case SwarmFormation.Grid:
                    var gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
                    var x = index % gridSize;
                    var z = index / gridSize;
                    return new Vector3(x * 5f, 0, z * 5f);

                case SwarmFormation.Line:
                    return new Vector3(index * 3f, 0, 0);

                case SwarmFormation.Sphere:
                    return UnityEngine.Random.onUnitSphere * 15f;

                case SwarmFormation.Random:
                default:
                    return UnityEngine.Random.insideUnitSphere * 20f;
            }
        }
    }

    public enum SwarmFormation
    {
        Random,
        Circle,
        Grid,
        Line,
        Sphere
    }

    /// <summary>
    /// Utility class for cleaning up test objects
    /// </summary>
    public static class TestCleanup
    {
        public static void DestroyTestObjects(params GameObject[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        public static void DestroyTestAgents(List<SwarmAgent> agents)
        {
            foreach (var agent in agents)
            {
                if (agent != null && agent.gameObject != null)
                {
                    Object.DestroyImmediate(agent.gameObject);
                }
            }
            agents.Clear();
        }
    }

    /// <summary>
    /// Test assertions for swarm-specific validation
    /// </summary>
    public static class SwarmAssert
    {
        public static void AgentsWithinBounds(List<SwarmAgent> agents, Bounds bounds)
        {
            foreach (var agent in agents)
            {
                NUnit.Framework.Assert.IsTrue(bounds.Contains(agent.transform.position),
                    $"Agent {agent.AgentId} is outside bounds: {agent.transform.position}");
            }
        }

        public static void AgentsMoving(List<SwarmAgent> agents, float minSpeed = 0.1f)
        {
            foreach (var agent in agents)
            {
                var speed = math.length(agent.Velocity);
                NUnit.Framework.Assert.GreaterOrEqual(speed, minSpeed,
                    $"Agent {agent.AgentId} is not moving (speed: {speed})");
            }
        }

        public static void AgentsNotOverlapping(List<SwarmAgent> agents, float minDistance = 0.5f)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                for (int j = i + 1; j < agents.Count; j++)
                {
                    var distance = Vector3.Distance(agents[i].transform.position, agents[j].transform.position);
                    NUnit.Framework.Assert.GreaterOrEqual(distance, minDistance,
                        $"Agents {agents[i].AgentId} and {agents[j].AgentId} are overlapping (distance: {distance})");
                }
            }
        }

        public static void PerformanceWithinBounds(PerformanceData performance, float minFPS = 30f, int maxNeighbors = 100)
        {
            NUnit.Framework.Assert.GreaterOrEqual(performance.fps, minFPS,
                $"Performance below threshold: {performance.fps} FPS");
            NUnit.Framework.Assert.LessOrEqual(performance.neighborCount, maxNeighbors,
                $"Too many neighbors: {performance.neighborCount}");
        }
    }
}