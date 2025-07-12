#!/usr/bin/env python3
"""
Memory Integration Module
Provides unified interface for all memory management functionality.
"""

import json
import time
from typing import Dict, List, Any, Optional
from datetime import datetime
from persistence_manager import MemoryPersistenceManager
from coordination_protocols import AgentMemoryCoordinator
from memory_monitor import MemoryMonitor
from neural_learning import NeuralPatternLearner, CoordinationOutcome, OutcomeType


class SwarmMemoryManager:
    """Unified interface for all swarm memory management functionality."""
    
    def __init__(self, agent_name: str, swarm_id: str, config_path: str = None):
        self.agent_name = agent_name
        self.swarm_id = swarm_id
        self.config_path = config_path
        
        # Initialize core components
        self.persistence_manager = MemoryPersistenceManager(config_path)
        self.coordinator = AgentMemoryCoordinator(agent_name, swarm_id)
        self.monitor = MemoryMonitor(config_path)
        self.neural_learner = NeuralPatternLearner(config_path)
        
        # Register coordination callbacks
        self._setup_coordination_callbacks()
        
        print(f"SwarmMemoryManager initialized for agent '{agent_name}' in swarm '{swarm_id}'")
    
    def initialize_agent_memory(self, initial_state: Dict[str, Any] = None) -> bool:
        """Initialize memory for a new agent."""
        try:
            # Set up agent memory structure
            agent_memory_key = f"swarm-{self.swarm_id}/agent-{self.agent_name}/state"
            
            default_state = {
                "agent_name": self.agent_name,
                "swarm_id": self.swarm_id,
                "initialized_at": datetime.now().isoformat(),
                "status": "active",
                "tasks_completed": 0,
                "memory_version": "1.0.0"
            }
            
            if initial_state:
                default_state.update(initial_state)
            
            # Store initial state using coordination
            success = self.coordinator.coordinated_memory_write(agent_memory_key, default_state)
            
            if success:
                # Synchronize with swarm
                self.coordinator.synchronize_with_swarm()
                
                # Record initialization outcome for learning
                self._record_initialization_outcome(success)
            
            return success
            
        except Exception as e:
            print(f"Error initializing agent memory: {e}")
            return False
    
    def store_agent_decision(self, decision_type: str, decision_data: Dict[str, Any], 
                           context: Dict[str, Any] = None) -> bool:
        """Store an agent decision with coordination."""
        try:
            decision_key = f"swarm-{self.swarm_id}/agent-{self.agent_name}/decisions/{decision_type}"
            
            decision_record = {
                "decision_type": decision_type,
                "decision_data": decision_data,
                "context": context or {},
                "timestamp": datetime.now().isoformat(),
                "agent_name": self.agent_name
            }
            
            return self.coordinator.coordinated_memory_write(decision_key, decision_record)
            
        except Exception as e:
            print(f"Error storing agent decision: {e}")
            return False
    
    def get_agent_decisions(self, decision_type: str = None) -> List[Dict[str, Any]]:
        """Retrieve agent decisions, optionally filtered by type."""
        try:
            if decision_type:
                decision_key = f"swarm-{self.swarm_id}/agent-{self.agent_name}/decisions/{decision_type}"
                decision = self.coordinator.coordinated_memory_read(decision_key)
                return [decision] if decision else []
            else:
                # Get all decision types - this would need pattern matching
                return []
                
        except Exception as e:
            print(f"Error retrieving agent decisions: {e}")
            return []
    
    def update_agent_progress(self, task_id: str, progress_data: Dict[str, Any]) -> bool:
        """Update agent progress on a task."""
        try:
            progress_key = f"swarm-{self.swarm_id}/agent-{self.agent_name}/progress/{task_id}"
            
            progress_record = {
                "task_id": task_id,
                "progress_data": progress_data,
                "updated_at": datetime.now().isoformat(),
                "agent_name": self.agent_name
            }
            
            return self.coordinator.coordinated_memory_write(progress_key, progress_record)
            
        except Exception as e:
            print(f"Error updating agent progress: {e}")
            return False
    
    def get_swarm_coordination_state(self) -> Optional[Dict[str, Any]]:
        """Get current swarm coordination state."""
        try:
            coordination_key = f"swarm-{self.swarm_id}/global/coordination"
            return self.coordinator.coordinated_memory_read(coordination_key)
            
        except Exception as e:
            print(f"Error getting swarm coordination state: {e}")
            return None
    
    def record_task_outcome(self, task_type: str, agents_involved: List[str], 
                          success: bool, execution_time: float, 
                          context: Dict[str, Any] = None) -> bool:
        """Record a task outcome for neural learning."""
        try:
            outcome = CoordinationOutcome(
                outcome_id=f"outcome_{self.swarm_id}_{int(time.time())}",
                swarm_id=self.swarm_id,
                task_type=task_type,
                agents_involved=agents_involved,
                outcome_type=OutcomeType.SUCCESS if success else OutcomeType.FAILURE,
                success_score=1.0 if success else 0.0,
                execution_time=execution_time,
                resource_usage={},  # Could be enhanced with actual resource data
                context=context or {},
                timestamp=datetime.now()
            )
            
            return self.neural_learner.record_coordination_outcome(outcome)
            
        except Exception as e:
            print(f"Error recording task outcome: {e}")
            return False
    
    def get_coordination_strategy(self, context: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Get coordination strategy recommendation based on learned patterns."""
        try:
            return self.neural_learner.suggest_coordination_strategy(context)
            
        except Exception as e:
            print(f"Error getting coordination strategy: {e}")
            return None
    
    def get_memory_health_report(self) -> Dict[str, Any]:
        """Get comprehensive memory health report."""
        try:
            health_report = self.monitor.check_memory_health()
            
            # Add agent-specific information
            health_report["agent_info"] = {
                "agent_name": self.agent_name,
                "swarm_id": self.swarm_id,
                "coordinator_status": self.coordinator.get_agent_memory_status(),
                "active_locks": len(self.coordinator.active_locks),
                "pending_messages": len(self.coordinator.message_queue)
            }
            
            return health_report
            
        except Exception as e:
            print(f"Error getting memory health report: {e}")
            return {"error": str(e)}
    
    def optimize_memory(self, auto_apply: bool = False) -> Dict[str, Any]:
        """Optimize memory usage."""
        try:
            return self.monitor.optimize_memory_usage(auto_apply)
            
        except Exception as e:
            print(f"Error optimizing memory: {e}")
            return {"error": str(e)}
    
    def get_learning_insights(self, insight_type: str = None) -> List[Dict[str, Any]]:
        """Get learning insights from neural pattern analysis."""
        try:
            return self.neural_learner.get_learning_insights(insight_type)
            
        except Exception as e:
            print(f"Error getting learning insights: {e}")
            return []
    
    def share_knowledge_with_swarm(self, knowledge_type: str, knowledge_data: Dict[str, Any]) -> bool:
        """Share knowledge with the entire swarm."""
        try:
            knowledge_key = f"swarm-{self.swarm_id}/global/knowledge/{knowledge_type}"
            
            knowledge_record = {
                "knowledge_type": knowledge_type,
                "knowledge_data": knowledge_data,
                "shared_by": self.agent_name,
                "shared_at": datetime.now().isoformat()
            }
            
            success = self.coordinator.coordinated_memory_write(knowledge_key, knowledge_record)
            
            if success:
                # Notify other agents
                self.coordinator.send_coordination_message(
                    recipient_agent=None,  # Broadcast
                    message_type="knowledge_shared",
                    payload={
                        "knowledge_type": knowledge_type,
                        "shared_by": self.agent_name,
                        "knowledge_key": knowledge_key
                    }
                )
            
            return success
            
        except Exception as e:
            print(f"Error sharing knowledge with swarm: {e}")
            return False
    
    def get_shared_knowledge(self, knowledge_type: str = None) -> Dict[str, Any]:
        """Retrieve shared knowledge from the swarm."""
        try:
            if knowledge_type:
                knowledge_key = f"swarm-{self.swarm_id}/global/knowledge/{knowledge_type}"
                return self.coordinator.coordinated_memory_read(knowledge_key) or {}
            else:
                # Get all shared knowledge - would need pattern matching
                return {}
                
        except Exception as e:
            print(f"Error getting shared knowledge: {e}")
            return {}
    
    def create_memory_snapshot(self) -> Dict[str, Any]:
        """Create a comprehensive snapshot of current memory state."""
        try:
            snapshot = {
                "snapshot_id": f"snapshot_{self.swarm_id}_{self.agent_name}_{int(time.time())}",
                "created_at": datetime.now().isoformat(),
                "agent_info": {
                    "agent_name": self.agent_name,
                    "swarm_id": self.swarm_id
                },
                "memory_usage": self.monitor.get_current_usage_snapshot(),
                "coordination_state": self.get_swarm_coordination_state(),
                "agent_status": self.coordinator.get_agent_memory_status(),
                "active_patterns": len(self.neural_learner.patterns),
                "total_outcomes": len(self.neural_learner.outcomes)
            }
            
            # Store the snapshot
            snapshot_key = f"swarm-{self.swarm_id}/snapshots/{snapshot['snapshot_id']}"
            self.persistence_manager.store_memory(snapshot_key, snapshot)
            
            return snapshot
            
        except Exception as e:
            print(f"Error creating memory snapshot: {e}")
            return {"error": str(e)}
    
    def _setup_coordination_callbacks(self):
        """Set up callbacks for coordination messages."""
        def handle_knowledge_shared(message):
            print(f"Knowledge shared by {message['payload']['shared_by']}: {message['payload']['knowledge_type']}")
        
        def handle_memory_updated(message):
            print(f"Memory updated by {message['payload']['agent_name']}: {message['payload']['memory_key']}")
        
        self.coordinator.register_coordination_callback("knowledge_shared", handle_knowledge_shared)
        self.coordinator.register_coordination_callback("memory_updated", handle_memory_updated)
    
    def _record_initialization_outcome(self, success: bool):
        """Record agent initialization outcome."""
        try:
            self.record_task_outcome(
                task_type="agent_initialization",
                agents_involved=[self.agent_name],
                success=success,
                execution_time=1.0,  # Mock execution time
                context={"initialization": True}
            )
        except Exception as e:
            print(f"Error recording initialization outcome: {e}")
    
    def close(self):
        """Close all memory management components."""
        try:
            self.coordinator.close()
            self.monitor.close()
            self.neural_learner.close()
            self.persistence_manager.close()
            print(f"SwarmMemoryManager closed for agent '{self.agent_name}'")
        except Exception as e:
            print(f"Error closing SwarmMemoryManager: {e}")


# Convenience functions for easy integration
def create_swarm_memory_manager(agent_name: str, swarm_id: str, config_path: str = None) -> SwarmMemoryManager:
    """Create and initialize a SwarmMemoryManager."""
    return SwarmMemoryManager(agent_name, swarm_id, config_path)


def quick_memory_health_check(agent_name: str = "system", swarm_id: str = "default") -> Dict[str, Any]:
    """Perform a quick memory health check."""
    manager = SwarmMemoryManager(agent_name, swarm_id)
    try:
        return manager.get_memory_health_report()
    finally:
        manager.close()


def emergency_memory_cleanup(agent_name: str = "system", swarm_id: str = "default") -> Dict[str, Any]:
    """Perform emergency memory cleanup."""
    manager = SwarmMemoryManager(agent_name, swarm_id)
    try:
        return manager.optimize_memory(auto_apply=True)
    finally:
        manager.close()


if __name__ == "__main__":
    # Test the integrated memory management system
    print("Testing SwarmMemoryManager...")
    
    # Create manager
    manager = SwarmMemoryManager("test-agent", "test-swarm")
    
    # Initialize agent memory
    init_success = manager.initialize_agent_memory({"test_mode": True})
    print(f"Agent initialization: {'Success' if init_success else 'Failed'}")
    
    # Store a decision
    decision_success = manager.store_agent_decision(
        "task_assignment",
        {"assigned_task": "data_processing", "priority": "high"},
        {"context": "test_scenario"}
    )
    print(f"Decision storage: {'Success' if decision_success else 'Failed'}")
    
    # Record a task outcome
    outcome_success = manager.record_task_outcome(
        "data_processing",
        ["test-agent"],
        True,
        30.5,
        {"complexity": "medium"}
    )
    print(f"Outcome recording: {'Success' if outcome_success else 'Failed'}")
    
    # Get coordination strategy
    strategy = manager.get_coordination_strategy({"complexity": "medium"})
    print(f"Strategy recommendation: {strategy}")
    
    # Get health report
    health = manager.get_memory_health_report()
    print(f"Memory health: {health.get('overall_health', 'unknown')}")
    
    # Create snapshot
    snapshot = manager.create_memory_snapshot()
    print(f"Snapshot created: {snapshot.get('snapshot_id', 'failed')}")
    
    # Clean up
    manager.close()
    print("Test completed.")