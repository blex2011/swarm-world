using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using Unity.PerformanceTesting;

namespace SwarmWorld.Tests
{
    /// <summary>
    /// Comprehensive test suite for SwarmAgent functionality
    /// </summary>
    public class SwarmAgentTests
    {
        private GameObject agentGameObject;
        private SwarmAgent swarmAgent;
        private SwarmCoordinator coordinator;

        [SetUp]
        public void SetUp()
        {
            // Create test environment
            var coordinatorGO = new GameObject("TestCoordinator");
            coordinator = coordinatorGO.AddComponent<SwarmCoordinator>();
            
            // Create agent
            agentGameObject = new GameObject("TestAgent");
            swarmAgent = agentGameObject.AddComponent<SwarmAgent>();
            
            // Initialize with test data
            swarmAgent.SetAgentData(SwarmAgentData.Default);
        }

        [TearDown]
        public void TearDown()
        {
            if (agentGameObject != null)
                Object.DestroyImmediate(agentGameObject);
            
            if (coordinator != null)
                Object.DestroyImmediate(coordinator.gameObject);
        }

        [Test]
        public void SwarmAgent_Initialization_SetsCorrectDefaults()
        {
            // Arrange & Act
            // Agent is created in SetUp

            // Assert
            Assert.IsNotNull(swarmAgent.AgentId);
            Assert.IsTrue(swarmAgent.AgentId.Length > 0);
            Assert.AreEqual(SwarmAgentData.Default.maxSpeed, swarmAgent.Data.maxSpeed);
            Assert.AreEqual(SwarmAgentData.Default.perceptionRadius, swarmAgent.Data.perceptionRadius);
        }

        [Test]
        public void SwarmAgent_SetAgentData_UpdatesConfiguration()
        {
            // Arrange
            var customData = SwarmAgentData.HighPerformance;

            // Act
            swarmAgent.SetAgentData(customData);

            // Assert
            Assert.AreEqual(customData.maxSpeed, swarmAgent.Data.maxSpeed);
            Assert.AreEqual(customData.perceptionRadius, swarmAgent.Data.perceptionRadius);
            Assert.AreEqual(customData.separationWeight, swarmAgent.Data.separationWeight);
        }

        [Test]
        public void SwarmAgent_SetVelocity_UpdatesMovement()
        {
            // Arrange
            var testVelocity = new float3(1, 0, 1);

            // Act
            swarmAgent.SetVelocity(testVelocity);

            // Assert
            Assert.AreEqual(testVelocity.x, swarmAgent.Velocity.x, 0.001f);
            Assert.AreEqual(testVelocity.y, swarmAgent.Velocity.y, 0.001f);
            Assert.AreEqual(testVelocity.z, swarmAgent.Velocity.z, 0.001f);
        }

        [UnityTest]
        public IEnumerator SwarmAgent_ForceUpdate_MovesAgent()
        {
            // Arrange
            var initialPosition = agentGameObject.transform.position;
            swarmAgent.SetVelocity(new float3(5, 0, 0));

            // Act
            swarmAgent.ForceUpdate();
            yield return new WaitForFixedUpdate();

            // Assert
            var finalPosition = agentGameObject.transform.position;
            Assert.Greater(finalPosition.x, initialPosition.x);
        }

        [Test]
        public void SwarmAgent_GetNeighbors_ReturnsValidArray()
        {
            // Arrange & Act
            var neighbors = swarmAgent.GetNeighbors();

            // Assert
            Assert.IsNotNull(neighbors);
            Assert.IsTrue(neighbors.Length >= 0);
        }

        [Test]
        public void SwarmAgent_GetCurrentPerformance_ReturnsValidData()
        {
            // Arrange & Act
            var performance = swarmAgent.GetCurrentPerformance();

            // Assert
            Assert.IsTrue(performance.IsValid());
            Assert.GreaterOrEqual(performance.neighborCount, 0);
            Assert.GreaterOrEqual(performance.speed, 0);
        }

        [Test]
        [Performance]
        public void SwarmAgent_Update_Performance()
        {
            // Arrange
            const int iterationCount = 1000;

            // Act & Assert
            Measure.Method(() =>
            {
                swarmAgent.ForceUpdate();
            })
            .WarmupCount(10)
            .MeasurementCount(iterationCount)
            .IterationsPerMeasurement(1)
            .Run();
        }

        [Test]
        public void SwarmAgent_MultipleAgents_NoCollisions()
        {
            // Arrange
            var agents = new List<SwarmAgent>();
            const int agentCount = 10;

            for (int i = 0; i < agentCount; i++)
            {
                var go = new GameObject($"Agent_{i}");
                go.transform.position = UnityEngine.Random.insideUnitSphere * 10f;
                var agent = go.AddComponent<SwarmAgent>();
                agent.SetAgentData(SwarmAgentData.Default);
                agents.Add(agent);
            }

            // Act
            foreach (var agent in agents)
            {
                agent.ForceUpdate();
            }

            // Assert
            for (int i = 0; i < agents.Count; i++)
            {
                for (int j = i + 1; j < agents.Count; j++)
                {
                    var distance = Vector3.Distance(agents[i].transform.position, agents[j].transform.position);
                    Assert.Greater(distance, 0.1f, "Agents should not overlap");
                }
            }

            // Cleanup
            foreach (var agent in agents)
            {
                Object.DestroyImmediate(agent.gameObject);
            }
        }

        [Test]
        public void SwarmAgent_EdgeCases_HandlesGracefully()
        {
            // Test with extreme values
            var extremeData = new SwarmAgentData
            {
                maxSpeed = 0.001f,
                perceptionRadius = 1000f,
                separationRadius = 0.01f,
                maxNeighbors = 1,
                separationWeight = 0f,
                alignmentWeight = 0f,
                cohesionWeight = 0f,
                targetWeight = 100f
            };

            // Should not throw exceptions
            Assert.DoesNotThrow(() => swarmAgent.SetAgentData(extremeData));
            Assert.DoesNotThrow(() => swarmAgent.ForceUpdate());
        }

        [Test]
        public void SwarmAgent_Stress_ManyUpdates()
        {
            // Arrange
            const int updateCount = 10000;

            // Act & Assert - Should not crash or degrade significantly
            var startTime = Time.realtimeSinceStartup;
            
            for (int i = 0; i < updateCount; i++)
            {
                swarmAgent.ForceUpdate();
            }
            
            var endTime = Time.realtimeSinceStartup;
            var totalTime = endTime - startTime;
            
            Debug.Log($"Completed {updateCount} updates in {totalTime:F4} seconds");
            Assert.Less(totalTime, 1.0f, "Updates should complete within reasonable time");
        }
    }

    /// <summary>
    /// Tests for SwarmAgentData structure
    /// </summary>
    public class SwarmAgentDataTests
    {
        [Test]
        public void SwarmAgentData_Default_IsValid()
        {
            // Arrange & Act
            var defaultData = SwarmAgentData.Default;

            // Assert
            Assert.IsTrue(defaultData.IsValid());
            Assert.Greater(defaultData.maxSpeed, 0);
            Assert.Greater(defaultData.perceptionRadius, 0);
        }

        [Test]
        public void SwarmAgentData_HighPerformance_IsValid()
        {
            // Arrange & Act
            var highPerfData = SwarmAgentData.HighPerformance;

            // Assert
            Assert.IsTrue(highPerfData.IsValid());
            Assert.IsFalse(highPerfData.enableAdaptiveWeights);
        }

        [Test]
        public void SwarmAgentData_Research_IsValid()
        {
            // Arrange & Act
            var researchData = SwarmAgentData.Research;

            // Assert
            Assert.IsTrue(researchData.IsValid());
            Assert.IsTrue(researchData.enableAdaptiveWeights);
            Assert.Greater(researchData.learningRate, 0);
        }

        [Test]
        public void SwarmAgentData_ClampToValidRanges_FixesInvalidValues()
        {
            // Arrange
            var invalidData = new SwarmAgentData
            {
                maxSpeed = -1f,
                perceptionRadius = -5f,
                separationRadius = 0f,
                maxNeighbors = 0,
                learningRate = 2f,
                explorationRate = -0.5f
            };

            // Act
            invalidData.ClampToValidRanges();

            // Assert
            Assert.IsTrue(invalidData.IsValid());
            Assert.GreaterOrEqual(invalidData.maxSpeed, 0.1f);
            Assert.GreaterOrEqual(invalidData.perceptionRadius, 0.1f);
            Assert.GreaterOrEqual(invalidData.separationRadius, 0.1f);
            Assert.GreaterOrEqual(invalidData.maxNeighbors, 1);
            Assert.GreaterOrEqual(invalidData.learningRate, 0f);
            Assert.LessOrEqual(invalidData.learningRate, 1f);
            Assert.GreaterOrEqual(invalidData.explorationRate, 0f);
            Assert.LessOrEqual(invalidData.explorationRate, 1f);
        }

        [Test]
        public void SwarmAgentData_CreateVariant_ProducesValidVariation()
        {
            // Arrange
            var baseData = SwarmAgentData.Default;
            const float variationAmount = 0.3f;

            // Act
            var variant = baseData.CreateVariant(variationAmount);

            // Assert
            Assert.IsTrue(variant.IsValid());
            Assert.AreNotEqual(baseData.maxSpeed, variant.maxSpeed);
            Assert.AreNotEqual(baseData.separationWeight, variant.separationWeight);
        }

        [Test]
        public void SwarmAgentData_Lerp_InterpolatesCorrectly()
        {
            // Arrange
            var dataA = SwarmAgentData.Default;
            var dataB = SwarmAgentData.HighPerformance;
            const float t = 0.5f;

            // Act
            var lerped = SwarmAgentData.Lerp(dataA, dataB, t);

            // Assert
            Assert.IsTrue(lerped.IsValid());
            
            var expectedSpeed = math.lerp(dataA.maxSpeed, dataB.maxSpeed, t);
            Assert.AreEqual(expectedSpeed, lerped.maxSpeed, 0.001f);
            
            var expectedRadius = math.lerp(dataA.perceptionRadius, dataB.perceptionRadius, t);
            Assert.AreEqual(expectedRadius, lerped.perceptionRadius, 0.001f);
        }

        [Test]
        public void SwarmNeighbor_Constructor_SetsCorrectValues()
        {
            // Arrange
            var testId = "test-agent";
            var testPos = new float3(1, 2, 3);
            var testVel = new float3(0.5f, 0, -0.5f);
            var testDist = 5.0f;

            // Act
            var neighbor = new SwarmNeighbor(testId, testPos, testVel, testDist);

            // Assert
            Assert.AreEqual(testId, neighbor.agentId);
            Assert.AreEqual(testPos, neighbor.position);
            Assert.AreEqual(testVel, neighbor.velocity);
            Assert.AreEqual(testDist, neighbor.distance);
            Assert.IsTrue(neighbor.isValid);
            Assert.Greater(neighbor.influence, 0);
        }

        [Test]
        public void PerformanceData_IsValid_ChecksCorrectly()
        {
            // Arrange
            var validData = new PerformanceData
            {
                fps = 60f,
                neighborCount = 10,
                speed = 5f,
                timestamp = Time.time
            };

            var invalidData = new PerformanceData
            {
                fps = 0f,
                timestamp = 0f
            };

            // Act & Assert
            Assert.IsTrue(validData.IsValid());
            Assert.IsFalse(invalidData.IsValid());
        }

        [Test]
        public void PerformanceData_ToString_FormatsCorrectly()
        {
            // Arrange
            var data = new PerformanceData
            {
                fps = 60.123f,
                neighborCount = 15,
                speed = 7.89f,
                memoryUsage = 12.34f
            };

            // Act
            var result = data.ToString();

            // Assert
            Assert.IsTrue(result.Contains("FPS: 60.1"));
            Assert.IsTrue(result.Contains("Neighbors: 15"));
            Assert.IsTrue(result.Contains("Speed: 7.89"));
            Assert.IsTrue(result.Contains("Memory: 12.3"));
        }
    }
}