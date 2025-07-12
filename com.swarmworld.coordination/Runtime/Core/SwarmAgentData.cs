using System;
using Unity.Mathematics;
using UnityEngine;

namespace SwarmWorld
{
    /// <summary>
    /// Serializable data structure for swarm agent configuration
    /// </summary>
    [Serializable]
    public struct SwarmAgentData
    {
        [Header("Movement Properties")]
        public float maxSpeed;
        public float perceptionRadius;
        public float separationRadius;
        public int maxNeighbors;

        [Header("Behavior Weights")]
        [Range(0f, 5f)] public float separationWeight;
        [Range(0f, 5f)] public float alignmentWeight;
        [Range(0f, 5f)] public float cohesionWeight;
        [Range(0f, 5f)] public float targetWeight;

        [Header("Neural Learning")]
        public bool enableAdaptiveWeights;
        public float learningRate;
        public float explorationRate;

        [Header("Performance")]
        public bool enableLOD;
        public float lodDistance;
        public int lodLevels;

        /// <summary>
        /// Default configuration for swarm agents
        /// </summary>
        public static SwarmAgentData Default => new SwarmAgentData
        {
            maxSpeed = 5f,
            perceptionRadius = 10f,
            separationRadius = 3f,
            maxNeighbors = 20,
            separationWeight = 1.5f,
            alignmentWeight = 1f,
            cohesionWeight = 1f,
            targetWeight = 2f,
            enableAdaptiveWeights = true,
            learningRate = 0.01f,
            explorationRate = 0.1f,
            enableLOD = true,
            lodDistance = 50f,
            lodLevels = 3
        };

        /// <summary>
        /// High-performance configuration for large swarms
        /// </summary>
        public static SwarmAgentData HighPerformance => new SwarmAgentData
        {
            maxSpeed = 8f,
            perceptionRadius = 8f,
            separationRadius = 2f,
            maxNeighbors = 15,
            separationWeight = 2f,
            alignmentWeight = 0.8f,
            cohesionWeight = 0.8f,
            targetWeight = 1.5f,
            enableAdaptiveWeights = false,
            learningRate = 0f,
            explorationRate = 0f,
            enableLOD = true,
            lodDistance = 30f,
            lodLevels = 2
        };

        /// <summary>
        /// Research configuration with extensive neural learning
        /// </summary>
        public static SwarmAgentData Research => new SwarmAgentData
        {
            maxSpeed = 3f,
            perceptionRadius = 15f,
            separationRadius = 4f,
            maxNeighbors = 30,
            separationWeight = 1f,
            alignmentWeight = 1f,
            cohesionWeight = 1f,
            targetWeight = 1f,
            enableAdaptiveWeights = true,
            learningRate = 0.05f,
            explorationRate = 0.2f,
            enableLOD = false,
            lodDistance = 100f,
            lodLevels = 1
        };

        /// <summary>
        /// Validates the agent data configuration
        /// </summary>
        public bool IsValid()
        {
            return maxSpeed > 0 &&
                   perceptionRadius > 0 &&
                   separationRadius > 0 &&
                   maxNeighbors > 0 &&
                   separationWeight >= 0 &&
                   alignmentWeight >= 0 &&
                   cohesionWeight >= 0 &&
                   targetWeight >= 0 &&
                   learningRate >= 0 &&
                   explorationRate >= 0 &&
                   lodDistance > 0 &&
                   lodLevels > 0;
        }

        /// <summary>
        /// Clamps all values to valid ranges
        /// </summary>
        public void ClampToValidRanges()
        {
            maxSpeed = math.max(0.1f, maxSpeed);
            perceptionRadius = math.max(0.1f, perceptionRadius);
            separationRadius = math.max(0.1f, separationRadius);
            maxNeighbors = math.max(1, maxNeighbors);
            separationWeight = math.max(0f, separationWeight);
            alignmentWeight = math.max(0f, alignmentWeight);
            cohesionWeight = math.max(0f, cohesionWeight);
            targetWeight = math.max(0f, targetWeight);
            learningRate = math.clamp(learningRate, 0f, 1f);
            explorationRate = math.clamp(explorationRate, 0f, 1f);
            lodDistance = math.max(1f, lodDistance);
            lodLevels = math.max(1, lodLevels);
        }

        /// <summary>
        /// Creates a randomized variant of this configuration
        /// </summary>
        public SwarmAgentData CreateVariant(float variationAmount = 0.2f)
        {
            var variant = this;
            var random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, 100000));

            variant.maxSpeed *= 1f + random.NextFloat(-variationAmount, variationAmount);
            variant.perceptionRadius *= 1f + random.NextFloat(-variationAmount, variationAmount);
            variant.separationRadius *= 1f + random.NextFloat(-variationAmount, variationAmount);
            variant.separationWeight *= 1f + random.NextFloat(-variationAmount, variationAmount);
            variant.alignmentWeight *= 1f + random.NextFloat(-variationAmount, variationAmount);
            variant.cohesionWeight *= 1f + random.NextFloat(-variationAmount, variationAmount);
            variant.targetWeight *= 1f + random.NextFloat(-variationAmount, variationAmount);

            variant.ClampToValidRanges();
            return variant;
        }

        /// <summary>
        /// Interpolates between two agent data configurations
        /// </summary>
        public static SwarmAgentData Lerp(SwarmAgentData a, SwarmAgentData b, float t)
        {
            return new SwarmAgentData
            {
                maxSpeed = math.lerp(a.maxSpeed, b.maxSpeed, t),
                perceptionRadius = math.lerp(a.perceptionRadius, b.perceptionRadius, t),
                separationRadius = math.lerp(a.separationRadius, b.separationRadius, t),
                maxNeighbors = (int)math.lerp(a.maxNeighbors, b.maxNeighbors, t),
                separationWeight = math.lerp(a.separationWeight, b.separationWeight, t),
                alignmentWeight = math.lerp(a.alignmentWeight, b.alignmentWeight, t),
                cohesionWeight = math.lerp(a.cohesionWeight, b.cohesionWeight, t),
                targetWeight = math.lerp(a.targetWeight, b.targetWeight, t),
                enableAdaptiveWeights = t > 0.5f ? b.enableAdaptiveWeights : a.enableAdaptiveWeights,
                learningRate = math.lerp(a.learningRate, b.learningRate, t),
                explorationRate = math.lerp(a.explorationRate, b.explorationRate, t),
                enableLOD = t > 0.5f ? b.enableLOD : a.enableLOD,
                lodDistance = math.lerp(a.lodDistance, b.lodDistance, t),
                lodLevels = (int)math.lerp(a.lodLevels, b.lodLevels, t)
            };
        }
    }

    /// <summary>
    /// Neighbor information for swarm coordination
    /// </summary>
    [Serializable]
    public struct SwarmNeighbor
    {
        public string agentId;
        public float3 position;
        public float3 velocity;
        public float distance;
        public bool isValid;
        public float influence;

        public static SwarmNeighbor Invalid => new SwarmNeighbor { isValid = false };

        public SwarmNeighbor(string id, float3 pos, float3 vel, float dist)
        {
            agentId = id;
            position = pos;
            velocity = vel;
            distance = dist;
            isValid = true;
            influence = 1f / (1f + dist); // Inverse distance influence
        }
    }

    /// <summary>
    /// Performance data for monitoring and optimization
    /// </summary>
    [Serializable]
    public struct PerformanceData
    {
        public float fps;
        public int neighborCount;
        public float speed;
        public float timestamp;
        public float memoryUsage;
        public float cpuTime;

        public bool IsValid()
        {
            return fps > 0 && timestamp > 0;
        }

        public override string ToString()
        {
            return $"FPS: {fps:F1}, Neighbors: {neighborCount}, Speed: {speed:F2}, Memory: {memoryUsage:F1}MB";
        }
    }
}