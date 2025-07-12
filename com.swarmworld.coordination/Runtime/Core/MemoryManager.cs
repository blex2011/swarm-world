using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SwarmWorld
{
    /// <summary>
    /// Memory management system for swarm agents
    /// </summary>
    public class MemoryManager : IDisposable
    {
        private readonly string agentId;
        private readonly Dictionary<string, object> localMemory;
        private readonly Queue<PerformanceData> performanceHistory;
        private readonly int maxPerformanceHistory = 100;
        private bool disposed = false;

        public MemoryManager(string agentId)
        {
            this.agentId = agentId;
            this.localMemory = new Dictionary<string, object>();
            this.performanceHistory = new Queue<PerformanceData>();
        }

        public void StoreData(string key, object data)
        {
            if (disposed) return;
            
            localMemory[key] = data;
        }

        public T RetrieveData<T>(string key)
        {
            if (disposed || !localMemory.ContainsKey(key))
                return default(T);

            try
            {
                return (T)localMemory[key];
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning($"Memory Manager: Cannot cast stored data for key '{key}' to type {typeof(T)}");
                return default(T);
            }
        }

        public bool HasData(string key)
        {
            return !disposed && localMemory.ContainsKey(key);
        }

        public void ClearData(string key)
        {
            if (!disposed)
                localMemory.Remove(key);
        }

        public void ClearAllData()
        {
            if (!disposed)
                localMemory.Clear();
        }

        public void StorePerformanceData(PerformanceData data)
        {
            if (disposed) return;

            performanceHistory.Enqueue(data);
            
            // Keep only recent history
            while (performanceHistory.Count > maxPerformanceHistory)
            {
                performanceHistory.Dequeue();
            }
        }

        public PerformanceData[] GetPerformanceHistory()
        {
            if (disposed) return new PerformanceData[0];
            
            return performanceHistory.ToArray();
        }

        public PerformanceData GetAveragePerformance()
        {
            if (disposed || performanceHistory.Count == 0)
                return new PerformanceData();

            float totalFPS = 0f;
            int totalNeighbors = 0;
            float totalSpeed = 0f;
            float totalMemory = 0f;
            float totalCPU = 0f;

            foreach (var data in performanceHistory)
            {
                totalFPS += data.fps;
                totalNeighbors += data.neighborCount;
                totalSpeed += data.speed;
                totalMemory += data.memoryUsage;
                totalCPU += data.cpuTime;
            }

            int count = performanceHistory.Count;
            return new PerformanceData
            {
                fps = totalFPS / count,
                neighborCount = totalNeighbors / count,
                speed = totalSpeed / count,
                memoryUsage = totalMemory / count,
                cpuTime = totalCPU / count,
                timestamp = Time.time
            };
        }

        public int GetMemoryUsageCount()
        {
            return disposed ? 0 : localMemory.Count;
        }

        public string[] GetStoredKeys()
        {
            if (disposed) return new string[0];
            
            var keys = new string[localMemory.Count];
            localMemory.Keys.CopyTo(keys, 0);
            return keys;
        }

        public void Dispose()
        {
            if (disposed) return;

            localMemory?.Clear();
            performanceHistory?.Clear();
            disposed = true;
        }

        ~MemoryManager()
        {
            Dispose();
        }
    }

    /// <summary>
    /// Neural pattern learning system for swarm agents
    /// </summary>
    public class NeuralPatternLearner : IDisposable
    {
        private readonly string agentId;
        private readonly List<LearningPattern> patterns;
        private readonly float learningRate;
        private bool disposed = false;

        public NeuralPatternLearner(string agentId, float learningRate = 0.01f)
        {
            this.agentId = agentId;
            this.learningRate = learningRate;
            this.patterns = new List<LearningPattern>();
        }

        public Unity.Mathematics.float3 AdjustForce(Unity.Mathematics.float3 originalForce, SwarmNeighbor[] neighbors)
        {
            if (disposed) return originalForce;

            // Simple neural adjustment based on learned patterns
            var adjustment = Unity.Mathematics.float3.zero;
            
            // Learn from current situation
            RecordPattern(originalForce, neighbors);
            
            // Apply learned adjustments
            foreach (var pattern in patterns)
            {
                if (IsPatternApplicable(pattern, neighbors))
                {
                    adjustment += pattern.forceAdjustment * pattern.confidence;
                }
            }

            return originalForce + adjustment * learningRate;
        }

        private void RecordPattern(Unity.Mathematics.float3 force, SwarmNeighbor[] neighbors)
        {
            var pattern = new LearningPattern
            {
                neighborCount = GetValidNeighborCount(neighbors),
                averageDistance = GetAverageNeighborDistance(neighbors),
                originalForce = force,
                forceAdjustment = Unity.Mathematics.float3.zero,
                confidence = 0.1f,
                timestamp = Time.time
            };

            patterns.Add(pattern);
            
            // Limit pattern history
            if (patterns.Count > 1000)
            {
                patterns.RemoveAt(0);
            }
        }

        private bool IsPatternApplicable(LearningPattern pattern, SwarmNeighbor[] neighbors)
        {
            var currentNeighborCount = GetValidNeighborCount(neighbors);
            var currentAverageDistance = GetAverageNeighborDistance(neighbors);

            // Simple similarity check
            return Math.Abs(pattern.neighborCount - currentNeighborCount) <= 2 &&
                   Math.Abs(pattern.averageDistance - currentAverageDistance) <= 5f;
        }

        private int GetValidNeighborCount(SwarmNeighbor[] neighbors)
        {
            int count = 0;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.isValid) count++;
            }
            return count;
        }

        private float GetAverageNeighborDistance(SwarmNeighbor[] neighbors)
        {
            float totalDistance = 0f;
            int validCount = 0;

            foreach (var neighbor in neighbors)
            {
                if (neighbor.isValid)
                {
                    totalDistance += neighbor.distance;
                    validCount++;
                }
            }

            return validCount > 0 ? totalDistance / validCount : 0f;
        }

        public void Dispose()
        {
            if (disposed) return;

            patterns?.Clear();
            disposed = true;
        }

        ~NeuralPatternLearner()
        {
            Dispose();
        }
    }

    [Serializable]
    public struct LearningPattern
    {
        public int neighborCount;
        public float averageDistance;
        public Unity.Mathematics.float3 originalForce;
        public Unity.Mathematics.float3 forceAdjustment;
        public float confidence;
        public float timestamp;
    }
}