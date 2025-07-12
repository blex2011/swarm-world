using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.PerformanceTesting;
using Unity.Mathematics;

namespace SwarmWorld.Tests.Performance
{
    /// <summary>
    /// Comprehensive performance testing suite for swarm coordination
    /// </summary>
    public class SwarmPerformanceTests
    {
        private List<SwarmAgent> testAgents;
        private SwarmCoordinator coordinator;
        private GameObject coordinatorGO;

        [SetUp]
        public void SetUp()
        {
            coordinatorGO = new GameObject("PerformanceTestCoordinator");
            coordinator = coordinatorGO.AddComponent<SwarmCoordinator>();
            testAgents = new List<SwarmAgent>();
        }

        [TearDown]
        public void TearDown()
        {
            TestCleanup.DestroyTestAgents(testAgents);
            TestCleanup.DestroyTestObjects(coordinatorGO);
        }

        [Test, Performance]
        [TestCase(10, Description = "Small swarm performance")]
        [TestCase(50, Description = "Medium swarm performance")]
        [TestCase(100, Description = "Large swarm performance")]
        [TestCase(500, Description = "Very large swarm performance")]
        public void SwarmUpdate_Performance_ScalesWithAgentCount(int agentCount)
        {
            // Arrange
            testAgents = TestDataFactory.CreateTestSwarm(agentCount, SwarmFormation.Random);
            
            // Distribute agents in a reasonable space
            for (int i = 0; i < testAgents.Count; i++)
            {
                testAgents[i].transform.position = UnityEngine.Random.insideUnitSphere * 50f;
            }

            // Act & Measure
            Measure.Method(() =>
            {
                foreach (var agent in testAgents)
                {
                    agent.ForceUpdate();
                }
            })
            .WarmupCount(5)
            .MeasurementCount(50)
            .IterationsPerMeasurement(1)
            .GC()
            .Run();
        }

        [Test, Performance]
        public void SwarmNeighborFinding_Performance_SpatialOptimization()
        {
            // Arrange
            const int agentCount = 200;
            testAgents = TestDataFactory.CreateTestSwarm(agentCount, SwarmFormation.Grid);

            // Test different neighbor finding strategies
            var strategies = new[] { "Brute Force", "Spatial Hash", "Octree" };
            
            foreach (var strategy in strategies)
            {
                // Configure coordinator for specific strategy
                coordinator.SetNeighborFindingStrategy(strategy);

                // Measure performance
                Measure.Method(() =>
                {
                    coordinator.UpdateAllNeighbors();
                })
                .SampleGroup($"NeighborFinding_{strategy}")
                .WarmupCount(3)
                .MeasurementCount(20)
                .IterationsPerMeasurement(1)
                .Run();
            }
        }

        [Test, Performance]
        public void SwarmMemory_Performance_LargeDataSets()
        {
            // Arrange
            const int dataPoints = 10000;
            var memoryManager = new TestDataFactory.MockMemoryManager();
            var testData = TestDataFactory.CreateTestPerformanceData(dataPoints);

            // Act & Measure
            Measure.Method(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var data = testData[i % testData.Length];
                    memoryManager.Store($"perf_data_{i}", data);
                }
            })
            .SampleGroup("MemoryWrite")
            .WarmupCount(2)
            .MeasurementCount(30)
            .IterationsPerMeasurement(1)
            .Run();

            Measure.Method(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    var retrieved = memoryManager.Retrieve<PerformanceData>($"perf_data_{i}");
                }
            })
            .SampleGroup("MemoryRead")
            .WarmupCount(2)
            .MeasurementCount(30)
            .IterationsPerMeasurement(1)
            .Run();
        }

        [Test, Performance]
        public void SwarmBehavior_Performance_DifferentConfigurations()
        {
            // Test performance with different agent configurations
            var configurations = TestDataFactory.AgentDataVariants.GetAllVariants();
            const int agentsPerConfig = 50;

            foreach (var config in configurations)
            {
                // Create agents with specific configuration
                var configAgents = new List<SwarmAgent>();
                for (int i = 0; i < agentsPerConfig; i++)
                {
                    var agent = TestDataFactory.CreateTestAgent($"Config_{config.GetHashCode()}_{i}", config);
                    agent.transform.position = UnityEngine.Random.insideUnitSphere * 30f;
                    configAgents.Add(agent);
                }

                // Measure performance
                Measure.Method(() =>
                {
                    foreach (var agent in configAgents)
                    {
                        agent.ForceUpdate();
                    }
                })
                .SampleGroup($"Config_{config.GetHashCode()}")
                .WarmupCount(3)
                .MeasurementCount(15)
                .IterationsPerMeasurement(1)
                .Run();

                // Cleanup
                TestCleanup.DestroyTestAgents(configAgents);
            }
        }

        [UnityTest, Performance]
        public IEnumerator SwarmCoordination_Performance_RealTime()
        {
            // Arrange
            const int agentCount = 100;
            const float testDuration = 5f;
            testAgents = TestDataFactory.CreateTestSwarm(agentCount, SwarmFormation.Circle);

            var frameCount = 0;
            var startTime = Time.time;

            // Act - Run for specified duration and measure
            using (Measure.Frames().Scope("SwarmCoordination_FrameTiming"))
            {
                while (Time.time - startTime < testDuration)
                {
                    // Update all agents
                    foreach (var agent in testAgents)
                    {
                        agent.ForceUpdate();
                    }

                    frameCount++;
                    yield return null;
                }
            }

            // Verify reasonable performance
            var avgFPS = frameCount / testDuration;
            Assert.Greater(avgFPS, 30f, $"Average FPS too low: {avgFPS:F1}");
        }

        [Test, Performance]
        public void SwarmBehavior_MemoryAllocation_MinimalGarbage()
        {
            // Arrange
            const int agentCount = 50;
            testAgents = TestDataFactory.CreateTestSwarm(agentCount, SwarmFormation.Random);

            // Warmup to stabilize
            for (int i = 0; i < 10; i++)
            {
                foreach (var agent in testAgents)
                {
                    agent.ForceUpdate();
                }
            }

            // Measure garbage collection
            Measure.Method(() =>
            {
                foreach (var agent in testAgents)
                {
                    agent.ForceUpdate();
                }
            })
            .GC()
            .WarmupCount(5)
            .MeasurementCount(50)
            .IterationsPerMeasurement(1)
            .Run();
        }

        [Test, Performance]
        public void SwarmAgent_BatchProcessing_Performance()
        {
            // Test batch processing vs individual processing
            const int agentCount = 200;
            testAgents = TestDataFactory.CreateTestSwarm(agentCount, SwarmFormation.Grid);

            // Individual processing
            Measure.Method(() =>
            {
                foreach (var agent in testAgents)
                {
                    agent.ForceUpdate();
                }
            })
            .SampleGroup("IndividualProcessing")
            .WarmupCount(3)
            .MeasurementCount(20)
            .IterationsPerMeasurement(1)
            .Run();

            // Batch processing (simulated)
            Measure.Method(() =>
            {
                coordinator.BatchUpdateAgents(testAgents);
            })
            .SampleGroup("BatchProcessing")
            .WarmupCount(3)
            .MeasurementCount(20)
            .IterationsPerMeasurement(1)
            .Run();
        }

        [Test, Performance]
        public void SwarmAgent_LOD_Performance()
        {
            // Test Level of Detail performance optimization
            const int agentCount = 300;
            testAgents = TestDataFactory.CreateTestSwarm(agentCount, SwarmFormation.Sphere);

            // Place camera to test LOD system
            var camera = new GameObject("TestCamera").AddComponent<Camera>();
            camera.transform.position = Vector3.zero;

            // Test without LOD
            var noLODConfig = SwarmAgentData.Default;
            noLODConfig.enableLOD = false;

            foreach (var agent in testAgents)
            {
                agent.SetAgentData(noLODConfig);
            }

            Measure.Method(() =>
            {
                foreach (var agent in testAgents)
                {
                    agent.ForceUpdate();
                }
            })
            .SampleGroup("NoLOD")
            .WarmupCount(3)
            .MeasurementCount(15)
            .IterationsPerMeasurement(1)
            .Run();

            // Test with LOD
            var lodConfig = SwarmAgentData.Default;
            lodConfig.enableLOD = true;
            lodConfig.lodDistance = 25f;
            lodConfig.lodLevels = 3;

            foreach (var agent in testAgents)
            {
                agent.SetAgentData(lodConfig);
            }

            Measure.Method(() =>
            {
                foreach (var agent in testAgents)
                {
                    agent.ForceUpdate();
                }
            })
            .SampleGroup("WithLOD")
            .WarmupCount(3)
            .MeasurementCount(15)
            .IterationsPerMeasurement(1)
            .Run();

            // Cleanup
            Object.DestroyImmediate(camera.gameObject);
        }

        [Test]
        public void SwarmPerformance_StressTest_ExtremeCounts()
        {
            // Test with extreme agent counts to find breaking points
            var agentCounts = new[] { 1000, 2000, 5000 };
            
            foreach (var count in agentCounts)
            {
                Debug.Log($"Testing with {count} agents...");
                
                var stressAgents = new List<SwarmAgent>();
                var startTime = Time.realtimeSinceStartup;
                
                try
                {
                    // Create agents
                    for (int i = 0; i < count; i++)
                    {
                        var agent = TestDataFactory.CreateTestAgent($"Stress_{i}");
                        agent.transform.position = UnityEngine.Random.insideUnitSphere * 100f;
                        stressAgents.Add(agent);
                    }

                    var creationTime = Time.realtimeSinceStartup - startTime;
                    Debug.Log($"Created {count} agents in {creationTime:F3} seconds");

                    // Test single update
                    var updateStart = Time.realtimeSinceStartup;
                    foreach (var agent in stressAgents)
                    {
                        agent.ForceUpdate();
                    }
                    var updateTime = Time.realtimeSinceStartup - updateStart;
                    
                    Debug.Log($"Updated {count} agents in {updateTime:F3} seconds");
                    Assert.Less(updateTime, 0.1f, $"Update time too slow for {count} agents: {updateTime:F3}s");
                }
                finally
                {
                    // Cleanup
                    TestCleanup.DestroyTestAgents(stressAgents);
                }
            }
        }
    }

    /// <summary>
    /// Memory usage and profiling tests
    /// </summary>
    public class SwarmMemoryProfileTests
    {
        [Test, Performance]
        public void SwarmSystem_MemoryUsage_BaseLine()
        {
            // Measure baseline memory usage
            var memoryBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false);
            
            using (Measure.ProfilerMarkers("SwarmMemoryBaseline"))
            {
                var agents = TestDataFactory.CreateTestSwarm(100, SwarmFormation.Random);
                
                // Run for a while to stabilize memory usage
                for (int frame = 0; frame < 100; frame++)
                {
                    foreach (var agent in agents)
                    {
                        agent.ForceUpdate();
                    }
                }
                
                TestCleanup.DestroyTestAgents(agents);
            }
            
            var memoryAfter = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false);
            var memoryDelta = memoryAfter - memoryBefore;
            
            Debug.Log($"Memory usage delta: {memoryDelta / 1024f / 1024f:F2} MB");
        }

        [Test, Performance]
        public void SwarmSystem_MemoryLeaks_Detection()
        {
            // Test for memory leaks over multiple creation/destruction cycles
            var initialMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false);
            
            for (int cycle = 0; cycle < 10; cycle++)
            {
                var agents = TestDataFactory.CreateTestSwarm(50, SwarmFormation.Circle);
                
                // Use agents briefly
                for (int i = 0; i < 10; i++)
                {
                    foreach (var agent in agents)
                    {
                        agent.ForceUpdate();
                    }
                }
                
                TestCleanup.DestroyTestAgents(agents);
                
                // Force garbage collection
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }
            
            var finalMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(false);
            var memoryIncrease = finalMemory - initialMemory;
            
            Debug.Log($"Memory increase after 10 cycles: {memoryIncrease / 1024f / 1024f:F2} MB");
            Assert.Less(memoryIncrease, 10 * 1024 * 1024, "Potential memory leak detected"); // Less than 10MB growth
        }
    }
}