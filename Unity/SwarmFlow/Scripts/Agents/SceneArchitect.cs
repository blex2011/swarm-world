using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using SwarmFlow.Core;
using SwarmFlow.Coordination;

namespace SwarmFlow.Agents
{
    /// <summary>
    /// Specialized agent for scene analysis, optimization, and architectural decisions.
    /// Focuses on scene hierarchy, spatial relationships, and structural optimization.
    /// </summary>
    public class SceneArchitect : AgentBase
    {
        [Header("Scene Architect Configuration")]
        [SerializeField] private bool analyzeHierarchy = true;
        [SerializeField] private bool optimizeStructure = true;
        [SerializeField] private bool validateArchitecture = true;
        [SerializeField] private float analysisRadius = 50f;
        [SerializeField] private int maxAnalysisDepth = 5;
        
        [Header("Analysis Results")]
        [SerializeField] private SceneAnalysisData currentAnalysis;
        [SerializeField] private List<ArchitecturalIssue> detectedIssues = new List<ArchitecturalIssue>();
        [SerializeField] private List<OptimizationSuggestion> suggestions = new List<OptimizationSuggestion>();
        
        // Scene analysis components
        private HierarchyAnalyzer hierarchyAnalyzer;
        private SpatialAnalyzer spatialAnalyzer;
        private PerformanceAnalyzer performanceAnalyzer;
        private StructuralValidator structuralValidator;
        
        #region Initialization
        
        protected override void InitializeAgent()
        {
            base.InitializeAgent();
            
            agentType = AgentType.SceneArchitect;
            capabilities.AddRange(new[]
            {
                AgentCapability.SceneAnalysis,
                AgentCapability.PerformanceAnalysis,
                AgentCapability.General
            });
            
            // Initialize analysis components
            hierarchyAnalyzer = new HierarchyAnalyzer();
            spatialAnalyzer = new SpatialAnalyzer();
            performanceAnalyzer = new PerformanceAnalyzer();
            structuralValidator = new StructuralValidator();
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            // Subscribe to scene events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        
        #endregion
        
        #region Task Execution
        
        protected override IEnumerator ExecuteTaskImplementation(SwarmTask task)
        {
            switch (task.Type)
            {
                case TaskType.SceneAnalysis:
                    yield return StartCoroutine(ExecuteSceneAnalysis(task));
                    break;
                    
                case TaskType.PerformanceAnalysis:
                    yield return StartCoroutine(ExecutePerformanceAnalysis(task));
                    break;
                    
                default:
                    Debug.LogWarning($"[{AgentId}] Unsupported task type: {task.Type}");
                    break;
            }
        }
        
        private IEnumerator ExecuteSceneAnalysis(SwarmTask task)
        {
            Debug.Log($"[{AgentId}] Starting scene analysis");
            
            // Clear previous results
            detectedIssues.Clear();
            suggestions.Clear();
            
            // Get analysis parameters
            var targetScene = GetTaskParameter<Scene>(task, "scene", SceneManager.GetActiveScene());
            var analysisDepth = GetTaskParameter<int>(task, "depth", maxAnalysisDepth);
            var includeInactive = GetTaskParameter<bool>(task, "includeInactive", false);
            
            // Perform comprehensive scene analysis
            yield return StartCoroutine(AnalyzeSceneHierarchy(targetScene, analysisDepth, includeInactive));
            yield return StartCoroutine(AnalyzeSpatialRelationships(targetScene));
            yield return StartCoroutine(ValidateSceneStructure(targetScene));
            
            // Generate optimization suggestions
            GenerateOptimizationSuggestions();
            
            // Store results
            StoreAnalysisResults(task);
            
            Debug.Log($"[{AgentId}] Scene analysis completed. Found {detectedIssues.Count} issues and {suggestions.Count} suggestions");
        }
        
        private IEnumerator ExecutePerformanceAnalysis(SwarmTask task)
        {
            Debug.Log($"[{AgentId}] Starting performance analysis");
            
            var targetScene = GetTaskParameter<Scene>(task, "scene", SceneManager.GetActiveScene());
            
            // Analyze rendering performance
            yield return StartCoroutine(AnalyzeRenderingPerformance(targetScene));
            
            // Analyze memory usage
            yield return StartCoroutine(AnalyzeMemoryUsage(targetScene));
            
            // Analyze physics performance
            yield return StartCoroutine(AnalyzePhysicsPerformance(targetScene));
            
            Debug.Log($"[{AgentId}] Performance analysis completed");
        }
        
        #endregion
        
        #region Scene Analysis Methods
        
        private IEnumerator AnalyzeSceneHierarchy(Scene scene, int depth, bool includeInactive)
        {
            if (!analyzeHierarchy) yield break;
            
            var rootObjects = scene.GetRootGameObjects();
            var analysisData = new HierarchyAnalysisData();
            
            foreach (var rootObject in rootObjects)
            {
                if (!includeInactive && !rootObject.activeInHierarchy)
                    continue;
                
                yield return StartCoroutine(AnalyzeGameObjectHierarchy(rootObject, 0, depth, analysisData));
            }
            
            // Process analysis results
            ProcessHierarchyAnalysis(analysisData);
            
            yield return null;
        }
        
        private IEnumerator AnalyzeGameObjectHierarchy(GameObject obj, int currentDepth, int maxDepth, HierarchyAnalysisData data)
        {
            if (currentDepth >= maxDepth) yield break;
            
            // Analyze current object
            data.totalObjects++;
            data.objectsByDepth[currentDepth] = data.objectsByDepth.GetValueOrDefault(currentDepth, 0) + 1;
            
            // Count components
            var components = obj.GetComponents<Component>();
            data.totalComponents += components.Length;
            
            // Detect potential issues
            DetectHierarchyIssues(obj, currentDepth, components, data);
            
            // Analyze children
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                var child = obj.transform.GetChild(i).gameObject;
                yield return StartCoroutine(AnalyzeGameObjectHierarchy(child, currentDepth + 1, maxDepth, data));
                
                // Yield every few objects to prevent frame drops
                if (data.totalObjects % 50 == 0)
                    yield return null;
            }
        }
        
        private IEnumerator AnalyzeSpatialRelationships(Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();
            var spatialData = new SpatialAnalysisData();
            
            // Collect all renderers and colliders
            var renderers = new List<Renderer>();
            var colliders = new List<Collider>();
            
            foreach (var rootObject in rootObjects)
            {
                renderers.AddRange(rootObject.GetComponentsInChildren<Renderer>());
                colliders.AddRange(rootObject.GetComponentsInChildren<Collider>());
            }
            
            // Analyze spatial distribution
            AnalyzeSpatialDistribution(renderers, spatialData);
            
            // Detect overlapping objects
            DetectOverlappingObjects(colliders, spatialData);
            
            // Analyze LOD usage
            AnalyzeLODUsage(renderers, spatialData);
            
            yield return null;
        }
        
        private IEnumerator ValidateSceneStructure(Scene scene)
        {
            if (!validateArchitecture) yield break;
            
            var validationData = new StructuralValidationData();
            
            // Validate naming conventions
            ValidateNamingConventions(scene, validationData);
            
            // Validate component usage
            ValidateComponentUsage(scene, validationData);
            
            // Validate prefab integrity
            ValidatePrefabIntegrity(scene, validationData);
            
            yield return null;
        }
        
        private IEnumerator AnalyzeRenderingPerformance(Scene scene)
        {
            var renderingData = new RenderingAnalysisData();
            
            // Analyze draw calls
            AnalyzeDrawCalls(scene, renderingData);
            
            // Analyze texture usage
            AnalyzeTextureUsage(scene, renderingData);
            
            // Analyze lighting setup
            AnalyzeLightingSetup(scene, renderingData);
            
            // Analyze shader complexity
            AnalyzeShaderComplexity(scene, renderingData);
            
            yield return null;
        }
        
        private IEnumerator AnalyzeMemoryUsage(Scene scene)
        {
            var memoryData = new MemoryAnalysisData();
            
            // Analyze mesh memory usage
            AnalyzeMeshMemory(scene, memoryData);
            
            // Analyze texture memory usage
            AnalyzeTextureMemory(scene, memoryData);
            
            // Analyze audio memory usage
            AnalyzeAudioMemory(scene, memoryData);
            
            yield return null;
        }
        
        private IEnumerator AnalyzePhysicsPerformance(Scene scene)
        {
            var physicsData = new PhysicsAnalysisData();
            
            // Analyze rigidbody count and settings
            AnalyzeRigidbodies(scene, physicsData);
            
            // Analyze collider complexity
            AnalyzeColliders(scene, physicsData);
            
            // Analyze physics materials
            AnalyzePhysicsMaterials(scene, physicsData);
            
            yield return null;
        }
        
        #endregion
        
        #region Analysis Processing
        
        private void ProcessHierarchyAnalysis(HierarchyAnalysisData data)
        {
            // Detect deep hierarchy issues
            if (data.maxDepth > 10)
            {
                detectedIssues.Add(new ArchitecturalIssue
                {
                    Type = IssueType.DeepHierarchy,
                    Severity = IssueSeverity.Medium,
                    Description = $"Scene hierarchy is {data.maxDepth} levels deep, which may impact performance",
                    Recommendation = "Consider flattening the hierarchy or using object pooling"
                });
            }
            
            // Detect excessive component count
            var avgComponentsPerObject = (float)data.totalComponents / data.totalObjects;
            if (avgComponentsPerObject > 5)
            {
                detectedIssues.Add(new ArchitecturalIssue
                {
                    Type = IssueType.ExcessiveComponents,
                    Severity = IssueSeverity.Low,
                    Description = $"High average component count per object: {avgComponentsPerObject:F1}",
                    Recommendation = "Review component usage and consider component composition patterns"
                });
            }
        }
        
        private void DetectHierarchyIssues(GameObject obj, int depth, Component[] components, HierarchyAnalysisData data)
        {
            // Update max depth
            data.maxDepth = Mathf.Max(data.maxDepth, depth);
            
            // Detect missing renderers on visible objects
            if (obj.activeInHierarchy && obj.layer != LayerMask.NameToLayer("UI"))
            {
                var renderer = obj.GetComponent<Renderer>();
                var meshFilter = obj.GetComponent<MeshFilter>();
                
                if (meshFilter != null && renderer == null)
                {
                    detectedIssues.Add(new ArchitecturalIssue
                    {
                        Type = IssueType.MissingRenderer,
                        Severity = IssueSeverity.High,
                        Description = $"GameObject '{obj.name}' has MeshFilter but no Renderer",
                        GameObjectPath = GetGameObjectPath(obj),
                        Recommendation = "Add appropriate Renderer component or remove MeshFilter"
                    });
                }
            }
            
            // Detect unused components
            DetectUnusedComponents(obj, components);
        }
        
        private void DetectUnusedComponents(GameObject obj, Component[] components)
        {
            foreach (var component in components)
            {
                if (component == null) continue;
                
                // Check for common unused component patterns
                if (component is AudioSource audioSource && audioSource.clip == null)
                {
                    detectedIssues.Add(new ArchitecturalIssue
                    {
                        Type = IssueType.UnusedComponent,
                        Severity = IssueSeverity.Low,
                        Description = $"AudioSource on '{obj.name}' has no audio clip assigned",
                        GameObjectPath = GetGameObjectPath(obj),
                        Recommendation = "Assign audio clip or remove AudioSource component"
                    });
                }
            }
        }
        
        #endregion
        
        #region Optimization Suggestions
        
        private void GenerateOptimizationSuggestions()
        {
            // Generate suggestions based on detected issues
            foreach (var issue in detectedIssues)
            {
                var suggestion = CreateOptimizationSuggestion(issue);
                if (suggestion != null)
                    suggestions.Add(suggestion);
            }
            
            // Add general optimization suggestions
            AddGeneralOptimizationSuggestions();
        }
        
        private OptimizationSuggestion CreateOptimizationSuggestion(ArchitecturalIssue issue)
        {
            return new OptimizationSuggestion
            {
                Priority = GetSuggestionPriority(issue.Severity),
                Category = GetSuggestionCategory(issue.Type),
                Description = issue.Recommendation,
                EstimatedImpact = GetEstimatedImpact(issue.Type),
                ImplementationComplexity = GetImplementationComplexity(issue.Type)
            };
        }
        
        private void AddGeneralOptimizationSuggestions()
        {
            // Suggest LOD implementation if many renderers detected
            var rendererCount = FindObjectsOfType<Renderer>().Length;
            if (rendererCount > 100)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Priority = SuggestionPriority.Medium,
                    Category = OptimizationCategory.Rendering,
                    Description = "Consider implementing LOD (Level of Detail) for distant objects",
                    EstimatedImpact = PerformanceImpact.High,
                    ImplementationComplexity = ImplementationComplexity.Medium
                });
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Scene loaded: {scene.name}");
            
            // Automatically analyze new scenes if configured
            if (swarmManager != null)
            {
                var analysisTask = new SwarmTask($"auto_analysis_{scene.name}", TaskType.SceneAnalysis, TaskPriority.Low);
                analysisTask.Parameters["scene"] = scene;
                analysisTask.Parameters["depth"] = 3; // Lighter analysis for auto-analysis
                
                swarmManager.EnqueueTask(analysisTask);
            }
        }
        
        private void OnSceneUnloaded(Scene scene)
        {
            if (enableDebugLogs)
                Debug.Log($"[{AgentId}] Scene unloaded: {scene.name}");
            
            // Clear scene-specific data
            ClearSceneSpecificData();
        }
        
        #endregion
        
        #region Utility Methods
        
        private T GetTaskParameter<T>(SwarmTask task, string key, T defaultValue)
        {
            if (task.Parameters.TryGetValue(key, out var value) && value is T)
                return (T)value;
            
            return defaultValue;
        }
        
        private string GetGameObjectPath(GameObject obj)
        {
            var path = obj.name;
            var parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        private void StoreAnalysisResults(SwarmTask task)
        {
            // Store analysis results in agent memory
            StoreMemory($"analysis_{task.Id}", new
            {
                Issues = detectedIssues,
                Suggestions = suggestions,
                Timestamp = Time.time
            });
            
            // Send results to coordination hub
            SendCoordinationMessage("", MessageType.Custom, new
            {
                AnalysisComplete = true,
                IssueCount = detectedIssues.Count,
                SuggestionCount = suggestions.Count
            });
        }
        
        private void ClearSceneSpecificData()
        {
            detectedIssues.Clear();
            suggestions.Clear();
            currentAnalysis = null;
        }
        
        #endregion
        
        #region Cleanup
        
        protected override void CleanupAgent()
        {
            base.CleanupAgent();
            
            // Unsubscribe from scene events
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }
        
        #endregion
        
        #region Helper Methods for Analysis Components
        
        private void AnalyzeSpatialDistribution(List<Renderer> renderers, SpatialAnalysisData data) { /* Implementation */ }
        private void DetectOverlappingObjects(List<Collider> colliders, SpatialAnalysisData data) { /* Implementation */ }
        private void AnalyzeLODUsage(List<Renderer> renderers, SpatialAnalysisData data) { /* Implementation */ }
        private void ValidateNamingConventions(Scene scene, StructuralValidationData data) { /* Implementation */ }
        private void ValidateComponentUsage(Scene scene, StructuralValidationData data) { /* Implementation */ }
        private void ValidatePrefabIntegrity(Scene scene, StructuralValidationData data) { /* Implementation */ }
        private void AnalyzeDrawCalls(Scene scene, RenderingAnalysisData data) { /* Implementation */ }
        private void AnalyzeTextureUsage(Scene scene, RenderingAnalysisData data) { /* Implementation */ }
        private void AnalyzeLightingSetup(Scene scene, RenderingAnalysisData data) { /* Implementation */ }
        private void AnalyzeShaderComplexity(Scene scene, RenderingAnalysisData data) { /* Implementation */ }
        private void AnalyzeMeshMemory(Scene scene, MemoryAnalysisData data) { /* Implementation */ }
        private void AnalyzeTextureMemory(Scene scene, MemoryAnalysisData data) { /* Implementation */ }
        private void AnalyzeAudioMemory(Scene scene, MemoryAnalysisData data) { /* Implementation */ }
        private void AnalyzeRigidbodies(Scene scene, PhysicsAnalysisData data) { /* Implementation */ }
        private void AnalyzeColliders(Scene scene, PhysicsAnalysisData data) { /* Implementation */ }
        private void AnalyzePhysicsMaterials(Scene scene, PhysicsAnalysisData data) { /* Implementation */ }
        
        private SuggestionPriority GetSuggestionPriority(IssueSeverity severity) => SuggestionPriority.Medium;
        private OptimizationCategory GetSuggestionCategory(IssueType type) => OptimizationCategory.General;
        private PerformanceImpact GetEstimatedImpact(IssueType type) => PerformanceImpact.Medium;
        private ImplementationComplexity GetImplementationComplexity(IssueType type) => ImplementationComplexity.Medium;
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class SceneAnalysisData
    {
        public int totalObjects;
        public int totalComponents;
        public float analysisTime;
        public Dictionary<string, object> metrics = new Dictionary<string, object>();
    }
    
    [System.Serializable]
    public class ArchitecturalIssue
    {
        public IssueType Type;
        public IssueSeverity Severity;
        public string Description;
        public string GameObjectPath;
        public string Recommendation;
    }
    
    [System.Serializable]
    public class OptimizationSuggestion
    {
        public SuggestionPriority Priority;
        public OptimizationCategory Category;
        public string Description;
        public PerformanceImpact EstimatedImpact;
        public ImplementationComplexity ImplementationComplexity;
    }
    
    public enum IssueType
    {
        DeepHierarchy,
        ExcessiveComponents,
        MissingRenderer,
        UnusedComponent,
        PerformanceBottleneck,
        MemoryLeak,
        InvalidConfiguration
    }
    
    public enum IssueSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    public enum SuggestionPriority
    {
        Low,
        Medium,
        High
    }
    
    public enum OptimizationCategory
    {
        General,
        Rendering,
        Memory,
        Physics,
        Audio,
        Scripting
    }
    
    public enum PerformanceImpact
    {
        Low,
        Medium,
        High
    }
    
    public enum ImplementationComplexity
    {
        Low,
        Medium,
        High
    }
    
    // Analysis data classes
    public class HierarchyAnalysisData
    {
        public int totalObjects;
        public int totalComponents;
        public int maxDepth;
        public Dictionary<int, int> objectsByDepth = new Dictionary<int, int>();
    }
    
    public class SpatialAnalysisData { }
    public class StructuralValidationData { }
    public class RenderingAnalysisData { }
    public class MemoryAnalysisData { }
    public class PhysicsAnalysisData { }
    
    // Analyzer classes (placeholder implementations)
    public class HierarchyAnalyzer { }
    public class SpatialAnalyzer { }
    public class PerformanceAnalyzer { }
    public class StructuralValidator { }
    
    #endregion
}