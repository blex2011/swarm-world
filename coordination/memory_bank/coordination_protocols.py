#!/usr/bin/env python3
"""
Agent Memory Coordination Protocols
Manages coordination between agents for memory consistency and conflict resolution.
"""

import json
import time
import uuid
import threading
from typing import Dict, List, Any, Optional, Callable
from datetime import datetime, timedelta
from enum import Enum
from dataclasses import dataclass, asdict
from persistence_manager import MemoryPersistenceManager


class ConflictResolution(Enum):
    """Conflict resolution strategies."""
    LAST_WRITE_WINS = "last_write_wins"
    FIRST_WRITE_WINS = "first_write_wins"
    MERGE_CHANGES = "merge_changes"
    MANUAL_RESOLVE = "manual_resolve"


class MemoryLockType(Enum):
    """Types of memory locks."""
    READ = "read"
    WRITE = "write"
    EXCLUSIVE = "exclusive"


@dataclass
class MemoryLock:
    """Represents a memory lock held by an agent."""
    lock_id: str
    memory_key: str
    agent_name: str
    lock_type: MemoryLockType
    acquired_at: datetime
    expires_at: datetime
    metadata: Dict[str, Any] = None


@dataclass
class CoordinationMessage:
    """Message for inter-agent coordination."""
    message_id: str
    sender_agent: str
    recipient_agent: Optional[str]  # None for broadcast
    message_type: str
    payload: Dict[str, Any]
    timestamp: datetime
    requires_response: bool = False


class AgentMemoryCoordinator:
    """Coordinates memory access and synchronization between agents."""
    
    def __init__(self, agent_name: str, swarm_id: str):
        self.agent_name = agent_name
        self.swarm_id = swarm_id
        self.memory_manager = MemoryPersistenceManager()
        self.active_locks: Dict[str, MemoryLock] = {}
        self.message_queue: List[CoordinationMessage] = []
        self.conflict_handlers: Dict[str, Callable] = {}
        self.coordination_callbacks: Dict[str, List[Callable]] = {}
        
        # Initialize coordination state
        self.coordination_key = f"swarm-{swarm_id}/coordination/locks"
        self.message_key = f"swarm-{swarm_id}/coordination/messages"
        
        # Start coordination thread
        self.running = True
        self.coordination_thread = threading.Thread(target=self._coordination_loop, daemon=True)
        self.coordination_thread.start()
    
    def acquire_memory_lock(self, memory_key: str, lock_type: MemoryLockType, 
                           timeout_seconds: int = 30) -> Optional[MemoryLock]:
        """Acquire a lock on a memory key."""
        try:
            lock_id = str(uuid.uuid4())
            expires_at = datetime.now() + timedelta(seconds=timeout_seconds)
            
            lock = MemoryLock(
                lock_id=lock_id,
                memory_key=memory_key,
                agent_name=self.agent_name,
                lock_type=lock_type,
                acquired_at=datetime.now(),
                expires_at=expires_at
            )
            
            # Check for existing locks
            existing_locks = self._get_existing_locks(memory_key)
            
            if not self._can_acquire_lock(lock, existing_locks):
                return None
            
            # Store the lock
            self._store_lock(lock)
            self.active_locks[lock_id] = lock
            
            # Notify other agents
            self._broadcast_message("lock_acquired", {
                "lock_id": lock_id,
                "memory_key": memory_key,
                "lock_type": lock_type.value,
                "agent_name": self.agent_name
            })
            
            return lock
            
        except Exception as e:
            print(f"Error acquiring lock for {memory_key}: {e}")
            return None
    
    def release_memory_lock(self, lock_id: str) -> bool:
        """Release a memory lock."""
        try:
            if lock_id not in self.active_locks:
                return False
            
            lock = self.active_locks[lock_id]
            
            # Remove from storage
            self._remove_lock(lock_id)
            del self.active_locks[lock_id]
            
            # Notify other agents
            self._broadcast_message("lock_released", {
                "lock_id": lock_id,
                "memory_key": lock.memory_key,
                "agent_name": self.agent_name
            })
            
            return True
            
        except Exception as e:
            print(f"Error releasing lock {lock_id}: {e}")
            return False
    
    def coordinated_memory_write(self, memory_key: str, data: Any, 
                                metadata: Dict = None) -> bool:
        """Perform a coordinated memory write with conflict detection."""
        try:
            # Acquire write lock
            lock = self.acquire_memory_lock(memory_key, MemoryLockType.WRITE)
            if not lock:
                print(f"Could not acquire write lock for {memory_key}")
                return False
            
            try:
                # Check for conflicts
                if self._has_write_conflict(memory_key, data):
                    conflict_resolved = self._resolve_write_conflict(memory_key, data, metadata)
                    if not conflict_resolved:
                        return False
                
                # Perform the write
                success = self.memory_manager.store_memory(memory_key, data, metadata)
                
                if success:
                    # Notify other agents of the change
                    self._broadcast_message("memory_updated", {
                        "memory_key": memory_key,
                        "agent_name": self.agent_name,
                        "timestamp": datetime.now().isoformat(),
                        "change_summary": self._generate_change_summary(data)
                    })
                
                return success
                
            finally:
                # Always release the lock
                self.release_memory_lock(lock.lock_id)
                
        except Exception as e:
            print(f"Error in coordinated write for {memory_key}: {e}")
            return False
    
    def coordinated_memory_read(self, memory_key: str) -> Optional[Any]:
        """Perform a coordinated memory read with consistency checks."""
        try:
            # Acquire read lock
            lock = self.acquire_memory_lock(memory_key, MemoryLockType.READ)
            if not lock:
                # If we can't get a read lock, try without lock (read-only operation)
                print(f"Could not acquire read lock for {memory_key}, attempting unlocked read")
            
            try:
                # Read the data
                data = self.memory_manager.retrieve_memory(memory_key)
                
                # Verify consistency
                if data and not self._verify_data_consistency(memory_key, data):
                    print(f"Data consistency check failed for {memory_key}")
                    return None
                
                return data
                
            finally:
                if lock:
                    self.release_memory_lock(lock.lock_id)
                
        except Exception as e:
            print(f"Error in coordinated read for {memory_key}: {e}")
            return None
    
    def register_coordination_callback(self, message_type: str, callback: Callable):
        """Register a callback for coordination messages."""
        if message_type not in self.coordination_callbacks:
            self.coordination_callbacks[message_type] = []
        self.coordination_callbacks[message_type].append(callback)
    
    def send_coordination_message(self, recipient_agent: str, message_type: str, 
                                 payload: Dict[str, Any], requires_response: bool = False):
        """Send a coordination message to another agent."""
        message = CoordinationMessage(
            message_id=str(uuid.uuid4()),
            sender_agent=self.agent_name,
            recipient_agent=recipient_agent,
            message_type=message_type,
            payload=payload,
            timestamp=datetime.now(),
            requires_response=requires_response
        )
        
        self._store_message(message)
    
    def get_agent_memory_status(self) -> Dict[str, Any]:
        """Get current memory status for this agent."""
        return {
            "agent_name": self.agent_name,
            "swarm_id": self.swarm_id,
            "active_locks": len(self.active_locks),
            "pending_messages": len(self.message_queue),
            "memory_usage": self._get_agent_memory_usage(),
            "last_activity": datetime.now().isoformat()
        }
    
    def synchronize_with_swarm(self) -> bool:
        """Synchronize memory state with the rest of the swarm."""
        try:
            # Get swarm coordination state
            coordination_state = self.memory_manager.retrieve_memory(
                f"swarm-{self.swarm_id}/global/coordination"
            )
            
            if not coordination_state:
                # Initialize coordination state
                coordination_state = {
                    "swarm_id": self.swarm_id,
                    "active_agents": [self.agent_name],
                    "coordination_version": "1.0.0",
                    "last_sync": datetime.now().isoformat()
                }
            else:
                # Update agent list
                if self.agent_name not in coordination_state.get("active_agents", []):
                    coordination_state["active_agents"].append(self.agent_name)
                coordination_state["last_sync"] = datetime.now().isoformat()
            
            # Store updated coordination state
            success = self.memory_manager.store_memory(
                f"swarm-{self.swarm_id}/global/coordination",
                coordination_state
            )
            
            if success:
                # Broadcast synchronization complete
                self._broadcast_message("sync_complete", {
                    "agent_name": self.agent_name,
                    "sync_timestamp": datetime.now().isoformat()
                })
            
            return success
            
        except Exception as e:
            print(f"Error synchronizing with swarm: {e}")
            return False
    
    def _coordination_loop(self):
        """Main coordination loop running in background thread."""
        while self.running:
            try:
                # Process pending messages
                self._process_coordination_messages()
                
                # Clean up expired locks
                self._cleanup_expired_locks()
                
                # Check for memory conflicts
                self._check_memory_conflicts()
                
                # Sleep before next iteration
                time.sleep(1)
                
            except Exception as e:
                print(f"Error in coordination loop: {e}")
                time.sleep(5)
    
    def _get_existing_locks(self, memory_key: str) -> List[MemoryLock]:
        """Get existing locks for a memory key."""
        try:
            locks_data = self.memory_manager.retrieve_memory(self.coordination_key)
            if not locks_data:
                return []
            
            existing_locks = []
            for lock_data in locks_data.get("locks", []):
                if lock_data["memory_key"] == memory_key:
                    lock = MemoryLock(
                        lock_id=lock_data["lock_id"],
                        memory_key=lock_data["memory_key"],
                        agent_name=lock_data["agent_name"],
                        lock_type=MemoryLockType(lock_data["lock_type"]),
                        acquired_at=datetime.fromisoformat(lock_data["acquired_at"]),
                        expires_at=datetime.fromisoformat(lock_data["expires_at"])
                    )
                    existing_locks.append(lock)
            
            return existing_locks
            
        except Exception as e:
            print(f"Error getting existing locks: {e}")
            return []
    
    def _can_acquire_lock(self, new_lock: MemoryLock, existing_locks: List[MemoryLock]) -> bool:
        """Check if a new lock can be acquired given existing locks."""
        for existing_lock in existing_locks:
            # Skip expired locks
            if existing_lock.expires_at < datetime.now():
                continue
            
            # Check compatibility
            if existing_lock.lock_type == MemoryLockType.EXCLUSIVE:
                return False
            
            if new_lock.lock_type == MemoryLockType.EXCLUSIVE:
                return False
            
            if (existing_lock.lock_type == MemoryLockType.WRITE or 
                new_lock.lock_type == MemoryLockType.WRITE):
                return False
        
        return True
    
    def _store_lock(self, lock: MemoryLock):
        """Store a lock in coordination memory."""
        try:
            locks_data = self.memory_manager.retrieve_memory(self.coordination_key) or {"locks": []}
            
            lock_dict = asdict(lock)
            lock_dict["acquired_at"] = lock.acquired_at.isoformat()
            lock_dict["expires_at"] = lock.expires_at.isoformat()
            lock_dict["lock_type"] = lock.lock_type.value
            
            locks_data["locks"].append(lock_dict)
            
            self.memory_manager.store_memory(self.coordination_key, locks_data)
            
        except Exception as e:
            print(f"Error storing lock: {e}")
    
    def _remove_lock(self, lock_id: str):
        """Remove a lock from coordination memory."""
        try:
            locks_data = self.memory_manager.retrieve_memory(self.coordination_key)
            if not locks_data:
                return
            
            locks_data["locks"] = [
                lock for lock in locks_data["locks"] 
                if lock["lock_id"] != lock_id
            ]
            
            self.memory_manager.store_memory(self.coordination_key, locks_data)
            
        except Exception as e:
            print(f"Error removing lock: {e}")
    
    def _broadcast_message(self, message_type: str, payload: Dict[str, Any]):
        """Broadcast a message to all agents in the swarm."""
        message = CoordinationMessage(
            message_id=str(uuid.uuid4()),
            sender_agent=self.agent_name,
            recipient_agent=None,  # Broadcast
            message_type=message_type,
            payload=payload,
            timestamp=datetime.now()
        )
        
        self._store_message(message)
    
    def _store_message(self, message: CoordinationMessage):
        """Store a coordination message."""
        try:
            messages_data = self.memory_manager.retrieve_memory(self.message_key) or {"messages": []}
            
            message_dict = asdict(message)
            message_dict["timestamp"] = message.timestamp.isoformat()
            
            messages_data["messages"].append(message_dict)
            
            # Keep only recent messages (last 1000)
            if len(messages_data["messages"]) > 1000:
                messages_data["messages"] = messages_data["messages"][-1000:]
            
            self.memory_manager.store_memory(self.message_key, messages_data)
            
        except Exception as e:
            print(f"Error storing message: {e}")
    
    def _process_coordination_messages(self):
        """Process pending coordination messages."""
        try:
            messages_data = self.memory_manager.retrieve_memory(self.message_key)
            if not messages_data:
                return
            
            for message_dict in messages_data.get("messages", []):
                # Skip our own messages
                if message_dict["sender_agent"] == self.agent_name:
                    continue
                
                # Check if message is for us
                recipient = message_dict["recipient_agent"]
                if recipient and recipient != self.agent_name:
                    continue
                
                # Process the message
                message_type = message_dict["message_type"]
                if message_type in self.coordination_callbacks:
                    for callback in self.coordination_callbacks[message_type]:
                        try:
                            callback(message_dict)
                        except Exception as e:
                            print(f"Error in coordination callback: {e}")
            
        except Exception as e:
            print(f"Error processing coordination messages: {e}")
    
    def _cleanup_expired_locks(self):
        """Clean up expired locks."""
        try:
            current_time = datetime.now()
            expired_locks = [
                lock_id for lock_id, lock in self.active_locks.items()
                if lock.expires_at < current_time
            ]
            
            for lock_id in expired_locks:
                self.release_memory_lock(lock_id)
            
        except Exception as e:
            print(f"Error cleaning up expired locks: {e}")
    
    def _has_write_conflict(self, memory_key: str, new_data: Any) -> bool:
        """Check if there's a write conflict for the given memory key."""
        try:
            existing_data = self.memory_manager.retrieve_memory(memory_key)
            if not existing_data:
                return False
            
            # Simple conflict detection based on data changes
            return json.dumps(existing_data, sort_keys=True) != json.dumps(new_data, sort_keys=True)
            
        except Exception as e:
            print(f"Error checking write conflict: {e}")
            return True
    
    def _resolve_write_conflict(self, memory_key: str, new_data: Any, metadata: Dict) -> bool:
        """Resolve a write conflict."""
        # For now, use last_write_wins strategy
        # In future, this could be more sophisticated
        return True
    
    def _verify_data_consistency(self, memory_key: str, data: Any) -> bool:
        """Verify data consistency."""
        # Basic consistency check - in real implementation this would be more sophisticated
        return data is not None
    
    def _generate_change_summary(self, data: Any) -> str:
        """Generate a summary of changes made to data."""
        return f"Data updated at {datetime.now().isoformat()}"
    
    def _get_agent_memory_usage(self) -> Dict[str, Any]:
        """Get memory usage statistics for this agent."""
        try:
            stats = self.memory_manager.get_memory_usage_stats()
            
            # Filter for this agent's memory
            agent_stats = {
                "total_entries": 0,
                "total_size": 0
            }
            
            for category, category_stats in stats.get("categories", {}).items():
                if f"agent-{self.agent_name}" in category:
                    agent_stats["total_entries"] += category_stats.get("entry_count", 0)
                    agent_stats["total_size"] += category_stats.get("total_size", 0)
            
            return agent_stats
            
        except Exception as e:
            print(f"Error getting agent memory usage: {e}")
            return {}
    
    def _check_memory_conflicts(self):
        """Check for and resolve memory conflicts."""
        # This would implement conflict detection and resolution
        pass
    
    def close(self):
        """Close the coordinator and clean up resources."""
        self.running = False
        if self.coordination_thread.is_alive():
            self.coordination_thread.join(timeout=5)
        
        # Release all active locks
        for lock_id in list(self.active_locks.keys()):
            self.release_memory_lock(lock_id)
        
        self.memory_manager.close()


# Utility functions for easy integration
def create_agent_coordinator(agent_name: str, swarm_id: str) -> AgentMemoryCoordinator:
    """Create and initialize an agent memory coordinator."""
    return AgentMemoryCoordinator(agent_name, swarm_id)


def coordinated_store(agent_name: str, swarm_id: str, memory_key: str, data: Any) -> bool:
    """Store data using coordination."""
    coordinator = AgentMemoryCoordinator(agent_name, swarm_id)
    try:
        return coordinator.coordinated_memory_write(memory_key, data)
    finally:
        coordinator.close()


def coordinated_retrieve(agent_name: str, swarm_id: str, memory_key: str) -> Any:
    """Retrieve data using coordination."""
    coordinator = AgentMemoryCoordinator(agent_name, swarm_id)
    try:
        return coordinator.coordinated_memory_read(memory_key)
    finally:
        coordinator.close()


if __name__ == "__main__":
    # Test the coordination system
    coordinator = AgentMemoryCoordinator("test-agent", "test-swarm")
    
    # Test memory operations
    test_data = {"test": "coordination", "timestamp": datetime.now().isoformat()}
    success = coordinator.coordinated_memory_write("test/coordination", test_data)
    print(f"Coordinated write: {'Success' if success else 'Failed'}")
    
    retrieved = coordinator.coordinated_memory_read("test/coordination")
    print(f"Coordinated read: {retrieved}")
    
    # Test synchronization
    sync_success = coordinator.synchronize_with_swarm()
    print(f"Swarm synchronization: {'Success' if sync_success else 'Failed'}")
    
    coordinator.close()