using UnityEngine;

namespace SwarmAI.Core
{
    /// <summary>
    /// Core interface for all swarm agents, providing essential functionality
    /// for participation in swarm behaviors and coordination systems.
    /// </summary>
    public interface ISwarmAgent
    {
        /// <summary>
        /// Unique identifier for this agent within the swarm
        /// </summary>
        int AgentId { get; }
        
        /// <summary>
        /// Current world position of the agent
        /// </summary>
        Vector3 Position { get; set; }
        
        /// <summary>
        /// Current velocity vector of the agent
        /// </summary>
        Vector3 Velocity { get; set; }
        
        /// <summary>
        /// Maximum movement speed for this agent
        /// </summary>
        float MaxSpeed { get; set; }
        
        /// <summary>
        /// Radius within which this agent can perceive neighbors
        /// </summary>
        float PerceptionRadius { get; set; }
        
        /// <summary>
        /// Reference to the swarm manager controlling this agent
        /// </summary>
        ISwarmManager Manager { get; set; }
        
        /// <summary>
        /// Current level of detail for performance optimization
        /// </summary>
        LODLevel CurrentLODLevel { get; }
        
        /// <summary>
        /// Whether this agent is currently active and should be updated
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// Agent type identifier for behavior specialization
        /// </summary>
        AgentType AgentType { get; set; }
        
        /// <summary>
        /// Update the agent's behavior for the current frame
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update</param>
        void UpdateBehavior(float deltaTime);
        
        /// <summary>
        /// Set the level of detail for performance optimization
        /// </summary>
        /// <param name="level">Target LOD level</param>
        void SetLODLevel(LODLevel level);
        
        /// <summary>
        /// Get the list of neighboring agents within perception radius
        /// </summary>
        /// <returns>List of nearby agents</returns>
        System.Collections.Generic.List<ISwarmAgent> GetNeighbors();
        
        /// <summary>
        /// Reset the agent to its initial state
        /// </summary>
        void Reset();
        
        /// <summary>
        /// Initialize the agent with manager and configuration
        /// </summary>
        /// <param name="manager">Swarm manager reference</param>
        /// <param name="agentId">Unique agent identifier</param>
        /// <param name="config">Agent configuration</param>
        void Initialize(ISwarmManager manager, int agentId, AgentConfig config);
    }
    
    /// <summary>
    /// Level of detail settings for performance optimization
    /// </summary>
    public enum LODLevel
    {
        /// <summary>Maximum detail and update frequency</summary>
        High = 0,
        /// <summary>Reduced detail, good performance balance</summary>
        Medium = 1,
        /// <summary>Low detail for distant agents</summary>
        Low = 2,
        /// <summary>Minimal processing for very distant agents</summary>
        Minimal = 3,
        /// <summary>Agent is culled and not rendered/updated</summary>
        Culled = 4
    }
    
    /// <summary>
    /// Agent type for behavior specialization and coordination
    /// </summary>
    public enum AgentType
    {
        /// <summary>Standard boid agent with basic flocking behaviors</summary>
        Boid = 0,
        /// <summary>Formation-aware agent for tactical movements</summary>
        Formation = 1,
        /// <summary>Leader agent that influences swarm direction</summary>
        Leader = 2,
        /// <summary>Scout agent for exploration and pathfinding</summary>
        Scout = 3,
        /// <summary>Worker agent for resource gathering behaviors</summary>
        Worker = 4,
        /// <summary>Guard agent for defensive behaviors</summary>
        Guard = 5,
        /// <summary>Custom user-defined agent type</summary>
        Custom = 999
    }
}