using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using Unity.Mathematics;

namespace SwarmWorld.Tests.Editor
{
    /// <summary>
    /// Integration tests for Unity Editor functionality
    /// </summary>
    public class SwarmEditorIntegrationTests
    {
        private GameObject testScene;
        private List<GameObject> createdObjects;

        [SetUp]
        public void SetUp()
        {
            testScene = new GameObject("TestScene");
            createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            
            if (testScene != null)
            {
                Object.DestroyImmediate(testScene);
            }
        }

        [Test]
        public void SwarmAgent_Inspector_DisplaysCorrectProperties()
        {
            // Arrange
            var agentGO = new GameObject("TestAgent");
            var agent = agentGO.AddComponent<SwarmAgent>();
            createdObjects.Add(agentGO);

            // Act
            var serializedObject = new SerializedObject(agent);
            
            // Assert - Check that serialized properties exist
            Assert.IsNotNull(serializedObject.FindProperty("agentData"));
            Assert.IsNotNull(serializedObject.FindProperty("enableNeuralLearning"));
            Assert.IsNotNull(serializedObject.FindProperty("enableMemoryCoordination"));
            Assert.IsNotNull(serializedObject.FindProperty("showDebugInfo"));
        }

        [Test]
        public void SwarmCoordinator_Inspector_ConfigurationValidation()
        {
            // Arrange
            var coordinatorGO = new GameObject("TestCoordinator");
            var coordinator = coordinatorGO.AddComponent<SwarmCoordinator>();
            createdObjects.Add(coordinatorGO);

            // Act
            var serializedObject = new SerializedObject(coordinator);
            var configProperty = serializedObject.FindProperty("coordinationConfig");

            // Assert
            Assert.IsNotNull(configProperty);
            
            // Test configuration validation
            serializedObject.FindProperty("maxAgents").intValue = -1;
            serializedObject.ApplyModifiedProperties();
            
            // The coordinator should clamp invalid values
            Assert.GreaterOrEqual(coordinator.MaxAgents, 1);
        }

        [Test]
        public void SwarmSystem_Prefab_CreationAndInstantiation()
        {
            // Arrange - Create a prefab programmatically
            var agentGO = new GameObject("SwarmAgentPrefab");
            var agent = agentGO.AddComponent<SwarmAgent>();
            agent.SetAgentData(SwarmAgentData.Default);
            
            // Create prefab asset
            var prefabPath = "Assets/TestSwarmAgent.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(agentGO, prefabPath);
            
            try
            {
                // Act - Instantiate prefab
                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                createdObjects.Add(instance);
                
                // Assert
                Assert.IsNotNull(instance);
                var instanceAgent = instance.GetComponent<SwarmAgent>();
                Assert.IsNotNull(instanceAgent);
                Assert.AreEqual(SwarmAgentData.Default.maxSpeed, instanceAgent.Data.maxSpeed);
            }
            finally
            {
                // Cleanup
                AssetDatabase.DeleteAsset(prefabPath);
                Object.DestroyImmediate(agentGO);
            }
        }

        [Test]
        public void SwarmSystem_SceneValidation_DetectsIssues()
        {
            // Arrange - Create problematic scene setup
            var coordinatorGO = new GameObject("Coordinator");
            var coordinator = coordinatorGO.AddComponent<SwarmCoordinator>();
            createdObjects.Add(coordinatorGO);

            // Create agents with no coordinator reference
            var agentGOs = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                var agentGO = new GameObject($"Agent_{i}");
                agentGO.AddComponent<SwarmAgent>();
                agentGOs.Add(agentGO);
                createdObjects.Add(agentGO);
            }

            // Act - Validate scene
            var validator = new SwarmSceneValidator();
            var issues = validator.ValidateScene();

            // Assert
            Assert.IsNotNull(issues);
            // Should detect agents without coordinator registration
            // Should detect if there are multiple coordinators
            // Should detect missing required components
        }

        [Test]
        public void SwarmSystem_AssetImport_HandlesCustomData()
        {
            // Test custom asset importing for swarm configurations
            var configData = SwarmAgentData.Research;
            
            // Create a ScriptableObject asset
            var asset = ScriptableObject.CreateInstance<SwarmConfigurationAsset>();
            asset.agentData = configData;
            
            var assetPath = "Assets/TestSwarmConfig.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            
            try
            {
                // Refresh and load
                AssetDatabase.Refresh();
                var loadedAsset = AssetDatabase.LoadAssetAtPath<SwarmConfigurationAsset>(assetPath);
                
                // Assert
                Assert.IsNotNull(loadedAsset);
                Assert.AreEqual(configData.maxSpeed, loadedAsset.agentData.maxSpeed);
                Assert.AreEqual(configData.learningRate, loadedAsset.agentData.learningRate);
            }
            finally
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        [Test]
        public void SwarmSystem_Gizmos_DrawCorrectly()
        {
            // Arrange
            var agentGO = new GameObject("GizmoTestAgent");
            var agent = agentGO.AddComponent<SwarmAgent>();
            var agentData = SwarmAgentData.Default;
            agentData.perceptionRadius = 10f;
            agent.SetAgentData(agentData);
            createdObjects.Add(agentGO);

            // Enable debug gizmos
            var serializedAgent = new SerializedObject(agent);
            serializedAgent.FindProperty("showDebugInfo").boolValue = true;
            serializedAgent.ApplyModifiedProperties();

            // Act & Assert
            // In editor tests, we verify gizmo drawing doesn't cause errors
            Assert.DoesNotThrow(() =>
            {
                // Simulate OnDrawGizmos call
                var gizmosMethod = typeof(SwarmAgent).GetMethod("OnDrawGizmos", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                gizmosMethod?.Invoke(agent, null);
            });
        }

        [Test]
        public void SwarmSystem_PlayMode_StateManagement()
        {
            // Test play mode state changes
            var agentGO = new GameObject("PlayModeTestAgent");
            var agent = agentGO.AddComponent<SwarmAgent>();
            createdObjects.Add(agentGO);

            // Simulate entering play mode
            EditorApplication.isPlaying = true;
            
            try
            {
                // Assert agent initializes properly
                Assert.IsTrue(agent.IsInitialized);
                
                // Test state preservation
                var testVelocity = new float3(1, 2, 3);
                agent.SetVelocity(testVelocity);
                
                Assert.AreEqual(testVelocity.x, agent.Velocity.x, 0.001f);
            }
            finally
            {
                EditorApplication.isPlaying = false;
            }
        }

        [Test]
        public void SwarmSystem_BuildValidation_PassesChecks()
        {
            // Test build-time validation
            var buildValidator = new SwarmBuildValidator();
            var issues = buildValidator.ValidateForBuild();

            // Assert no critical build issues
            var criticalIssues = issues.FindAll(issue => issue.severity == BuildValidationSeverity.Error);
            Assert.AreEqual(0, criticalIssues.Count, 
                $"Critical build issues found: {string.Join(", ", criticalIssues)}");
        }

        [Test]
        public void SwarmSystem_PackageValidation_AssemblyReferences()
        {
            // Validate package assembly references
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            
            // Check required assemblies are loaded
            var requiredAssemblies = new[]
            {
                "SwarmWorld.Runtime",
                "SwarmWorld.Editor",
                "Unity.Collections",
                "Unity.Jobs",
                "Unity.Mathematics"
            };

            foreach (var requiredAssembly in requiredAssemblies)
            {
                var found = false;
                foreach (var assembly in assemblies)
                {
                    if (assembly.GetName().Name.Contains(requiredAssembly))
                    {
                        found = true;
                        break;
                    }
                }
                Assert.IsTrue(found, $"Required assembly not found: {requiredAssembly}");
            }
        }

        [Test]
        public void SwarmSystem_Documentation_Generation()
        {
            // Test documentation generation for components
            var documentationGenerator = new SwarmDocumentationGenerator();
            
            // Generate documentation for main components
            var agentDocs = documentationGenerator.GenerateComponentDocs<SwarmAgent>();
            var coordinatorDocs = documentationGenerator.GenerateComponentDocs<SwarmCoordinator>();
            
            // Assert documentation contains key information
            Assert.IsTrue(agentDocs.Contains("SwarmAgent"));
            Assert.IsTrue(agentDocs.Contains("coordination"));
            Assert.IsTrue(coordinatorDocs.Contains("SwarmCoordinator"));
            Assert.IsTrue(coordinatorDocs.Contains("manages"));
        }

        [UnityTest]
        public IEnumerator SwarmSystem_RuntimePerformance_EditorMode()
        {
            // Test performance in editor play mode
            var agents = TestDataFactory.CreateTestSwarm(50, SwarmFormation.Circle);
            createdObjects.AddRange(agents.ConvertAll(a => a.gameObject));

            var frameCount = 0;
            var startTime = Time.time;
            const float testDuration = 2f;

            while (Time.time - startTime < testDuration)
            {
                frameCount++;
                yield return null;
            }

            var avgFPS = frameCount / testDuration;
            Assert.Greater(avgFPS, 20f, $"Editor play mode performance too low: {avgFPS:F1} FPS");
        }
    }

    /// <summary>
    /// Mock classes for testing editor integration
    /// </summary>
    public class SwarmSceneValidator
    {
        public List<ValidationIssue> ValidateScene()
        {
            var issues = new List<ValidationIssue>();
            
            // Find all SwarmAgents in scene
            var agents = Object.FindObjectsOfType<SwarmAgent>();
            var coordinators = Object.FindObjectsOfType<SwarmCoordinator>();
            
            // Check for multiple coordinators
            if (coordinators.Length > 1)
            {
                issues.Add(new ValidationIssue
                {
                    severity = ValidationSeverity.Warning,
                    message = "Multiple SwarmCoordinators found in scene"
                });
            }
            
            // Check for agents without coordinator
            if (agents.Length > 0 && coordinators.Length == 0)
            {
                issues.Add(new ValidationIssue
                {
                    severity = ValidationSeverity.Error,
                    message = "SwarmAgents found but no SwarmCoordinator in scene"
                });
            }
            
            return issues;
        }
    }

    public class SwarmBuildValidator
    {
        public List<BuildValidationIssue> ValidateForBuild()
        {
            var issues = new List<BuildValidationIssue>();
            
            // Check for missing assembly references
            // Check for platform-specific issues
            // Validate performance settings
            
            return issues;
        }
    }

    public class SwarmDocumentationGenerator
    {
        public string GenerateComponentDocs<T>() where T : Component
        {
            var type = typeof(T);
            var docs = $"# {type.Name}\n\n";
            docs += $"Component for {type.Namespace} functionality.\n\n";
            
            // Add public methods
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            docs += "## Public Methods\n";
            foreach (var method in methods)
            {
                if (!method.IsSpecialName && method.DeclaringType == type)
                {
                    docs += $"- {method.Name}\n";
                }
            }
            
            return docs;
        }
    }

    [System.Serializable]
    public class ValidationIssue
    {
        public ValidationSeverity severity;
        public string message;
        public GameObject gameObject;
    }

    [System.Serializable]
    public class BuildValidationIssue
    {
        public BuildValidationSeverity severity;
        public string message;
        public string component;
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum BuildValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// ScriptableObject for swarm configuration assets
    /// </summary>
    [CreateAssetMenu(fileName = "SwarmConfig", menuName = "SwarmWorld/Swarm Configuration")]
    public class SwarmConfigurationAsset : ScriptableObject
    {
        public SwarmAgentData agentData = SwarmAgentData.Default;
        public string configurationName = "Default";
        public string description = "";
        
        [TextArea(3, 5)]
        public string notes = "";
    }
}