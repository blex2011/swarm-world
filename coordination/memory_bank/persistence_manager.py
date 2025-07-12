#!/usr/bin/env python3
"""
Swarm Memory Persistence Manager
Manages cross-session memory persistence and coordination across agents.
"""

import json
import sqlite3
import hashlib
import datetime
import os
import gzip
from typing import Dict, Any, Optional, List
from pathlib import Path


class MemoryPersistenceManager:
    """Manages persistent memory storage and coordination for the swarm."""
    
    def __init__(self, config_path: str = None):
        self.config_path = config_path or "/workspaces/swarm-world/coordination/memory_bank/memory_config.json"
        self.schema_path = "/workspaces/swarm-world/coordination/memory_bank/memory_schema.json"
        self.load_configuration()
        self.setup_database()
    
    def load_configuration(self):
        """Load memory configuration and schema."""
        with open(self.config_path, 'r') as f:
            self.config = json.load(f)
        
        with open(self.schema_path, 'r') as f:
            self.schema = json.load(f)
    
    def setup_database(self):
        """Initialize SQLite database for memory persistence."""
        db_path = self.config['memory_persistence']['database_path']
        os.makedirs(os.path.dirname(db_path), exist_ok=True)
        
        self.db_path = db_path
        # Enable thread-safe SQLite connections
        self.conn = sqlite3.connect(db_path, check_same_thread=False)
        self.conn.execute('PRAGMA foreign_keys = ON')
        
        # Create tables
        self.create_tables()
    
    def create_tables(self):
        """Create necessary database tables."""
        tables = [
            """
            CREATE TABLE IF NOT EXISTS memory_entries (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                memory_key TEXT UNIQUE NOT NULL,
                category TEXT NOT NULL,
                swarm_id TEXT,
                agent_name TEXT,
                session_id TEXT,
                data_hash TEXT NOT NULL,
                compressed_data BLOB,
                metadata TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                expires_at TIMESTAMP,
                access_count INTEGER DEFAULT 0,
                last_accessed TIMESTAMP
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS coordination_state (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                swarm_id TEXT NOT NULL,
                state_type TEXT NOT NULL,
                state_data TEXT NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                is_active BOOLEAN DEFAULT TRUE
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS neural_patterns (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                pattern_id TEXT UNIQUE NOT NULL,
                pattern_type TEXT NOT NULL,
                success_rate REAL,
                usage_count INTEGER DEFAULT 0,
                pattern_data TEXT NOT NULL,
                learned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                last_used TIMESTAMP
            )
            """,
            """
            CREATE TABLE IF NOT EXISTS memory_operations (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                operation_type TEXT NOT NULL,
                memory_key TEXT,
                agent_name TEXT,
                operation_data TEXT,
                timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                success BOOLEAN DEFAULT TRUE
            )
            """
        ]
        
        for table_sql in tables:
            self.conn.execute(table_sql)
        
        # Create indexes
        indexes = [
            "CREATE INDEX IF NOT EXISTS idx_memory_key ON memory_entries(memory_key)",
            "CREATE INDEX IF NOT EXISTS idx_category ON memory_entries(category)",
            "CREATE INDEX IF NOT EXISTS idx_swarm_agent ON memory_entries(swarm_id, agent_name)",
            "CREATE INDEX IF NOT EXISTS idx_coordination_swarm ON coordination_state(swarm_id)",
            "CREATE INDEX IF NOT EXISTS idx_pattern_type ON neural_patterns(pattern_type)",
            "CREATE INDEX IF NOT EXISTS idx_operations_timestamp ON memory_operations(timestamp)"
        ]
        
        for index_sql in indexes:
            self.conn.execute(index_sql)
        
        self.conn.commit()
    
    def store_memory(self, memory_key: str, data: Any, metadata: Dict = None) -> bool:
        """Store data in persistent memory with the given key."""
        try:
            # Validate memory key format
            if not self.validate_memory_key(memory_key):
                raise ValueError(f"Invalid memory key format: {memory_key}")
            
            # Parse memory key
            key_parts = self.parse_memory_key(memory_key)
            
            # Serialize and compress data
            serialized_data = json.dumps(data, default=str)
            data_hash = hashlib.sha256(serialized_data.encode()).hexdigest()
            compressed_data = gzip.compress(serialized_data.encode())
            
            # Calculate expiration
            expires_at = self.calculate_expiration(key_parts['category'])
            
            # Store in database
            self.conn.execute("""
                INSERT OR REPLACE INTO memory_entries 
                (memory_key, category, swarm_id, agent_name, session_id, 
                 data_hash, compressed_data, metadata, expires_at, updated_at)
                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, CURRENT_TIMESTAMP)
            """, (
                memory_key,
                key_parts['category'],
                key_parts.get('swarm_id'),
                key_parts.get('agent_name'),
                key_parts.get('session_id'),
                data_hash,
                compressed_data,
                json.dumps(metadata or {}),
                expires_at
            ))
            
            self.conn.commit()
            
            # Log operation
            self.log_operation('store', memory_key, key_parts.get('agent_name'), {'size': len(compressed_data)})
            
            return True
            
        except Exception as e:
            print(f"Error storing memory {memory_key}: {e}")
            return False
    
    def retrieve_memory(self, memory_key: str) -> Optional[Any]:
        """Retrieve data from persistent memory."""
        try:
            cursor = self.conn.execute("""
                SELECT compressed_data, metadata FROM memory_entries 
                WHERE memory_key = ? AND (expires_at IS NULL OR expires_at > CURRENT_TIMESTAMP)
            """, (memory_key,))
            
            result = cursor.fetchone()
            if not result:
                return None
            
            compressed_data, metadata = result
            
            # Decompress and deserialize
            serialized_data = gzip.decompress(compressed_data).decode()
            data = json.loads(serialized_data)
            
            # Update access statistics
            self.conn.execute("""
                UPDATE memory_entries 
                SET access_count = access_count + 1, last_accessed = CURRENT_TIMESTAMP
                WHERE memory_key = ?
            """, (memory_key,))
            self.conn.commit()
            
            # Log operation
            key_parts = self.parse_memory_key(memory_key)
            self.log_operation('retrieve', memory_key, key_parts.get('agent_name'))
            
            return data
            
        except Exception as e:
            print(f"Error retrieving memory {memory_key}: {e}")
            return None
    
    def validate_memory_key(self, memory_key: str) -> bool:
        """Validate memory key format according to schema."""
        if len(memory_key) > self.schema['memory_keys']['max_key_length']:
            return False
        
        # Check for reserved keys
        for reserved in self.schema['memory_keys']['reserved_keys']:
            if reserved in memory_key:
                return False
        
        # Check format - Allow more flexible key formats
        parts = memory_key.split('/')
        if len(parts) < 2:
            return False
        
        # Extract the base category (could be 'swarm-id', 'agent-name', etc.)
        first_part = parts[0]
        valid_categories = self.schema['memory_keys']['categories']
        
        # Check if the first part starts with a valid category
        for category in valid_categories:
            if first_part.startswith(category) or first_part == category:
                return True
        
        # Also allow direct category matches
        return first_part in valid_categories
    
    def parse_memory_key(self, memory_key: str) -> Dict[str, str]:
        """Parse memory key into components."""
        parts = memory_key.split('/')
        result = {'category': parts[0]}
        
        if len(parts) > 1:
            result['subcategory'] = parts[1]
        
        # Extract swarm_id, agent_name, session_id from key
        for part in parts:
            if part.startswith('swarm-'):
                result['swarm_id'] = part.replace('swarm-', '')
            elif part.startswith('agent-'):
                result['agent_name'] = part.replace('agent-', '')
            elif part.startswith('session-'):
                result['session_id'] = part.replace('session-', '')
        
        return result
    
    def calculate_expiration(self, category: str) -> Optional[str]:
        """Calculate expiration timestamp based on category."""
        retention_policies = self.config.get('retention_policies', {})
        
        retention_map = {
            'swarm': retention_policies.get('default_retention_days', 30),
            'agent': retention_policies.get('default_retention_days', 30),
            'session': retention_policies.get('temporary_data_retention', 1),
            'global': retention_policies.get('critical_data_retention', 365),
            'neural': retention_policies.get('critical_data_retention', 365)
        }
        
        # Extract base category for compound keys like 'swarm-123'
        base_category = category
        for cat in retention_map.keys():
            if category.startswith(cat):
                base_category = cat
                break
        
        days = retention_map.get(base_category, retention_policies.get('default_retention_days', 30))
        if days == -1:  # Never expire
            return None
        
        expiration = datetime.datetime.now() + datetime.timedelta(days=days)
        return expiration.isoformat()
    
    def log_operation(self, operation_type: str, memory_key: str, agent_name: str = None, operation_data: Dict = None):
        """Log memory operation for monitoring."""
        try:
            self.conn.execute("""
                INSERT INTO memory_operations 
                (operation_type, memory_key, agent_name, operation_data)
                VALUES (?, ?, ?, ?)
            """, (
                operation_type,
                memory_key,
                agent_name,
                json.dumps(operation_data or {})
            ))
            self.conn.commit()
        except Exception as e:
            print(f"Error logging operation: {e}")
    
    def cleanup_expired_memory(self):
        """Remove expired memory entries."""
        try:
            cursor = self.conn.execute("""
                DELETE FROM memory_entries 
                WHERE expires_at IS NOT NULL AND expires_at < CURRENT_TIMESTAMP
            """)
            
            deleted_count = cursor.rowcount
            self.conn.commit()
            
            print(f"Cleaned up {deleted_count} expired memory entries")
            return deleted_count
            
        except Exception as e:
            print(f"Error during cleanup: {e}")
            return 0
    
    def get_memory_usage_stats(self) -> Dict[str, Any]:
        """Get memory usage statistics."""
        try:
            cursor = self.conn.execute("""
                SELECT 
                    category,
                    COUNT(*) as entry_count,
                    SUM(LENGTH(compressed_data)) as total_size,
                    AVG(access_count) as avg_access_count
                FROM memory_entries 
                WHERE expires_at IS NULL OR expires_at > CURRENT_TIMESTAMP
                GROUP BY category
            """)
            
            stats = {
                'categories': {},
                'total_entries': 0,
                'total_size': 0
            }
            
            for row in cursor.fetchall():
                category, count, size, avg_access = row
                stats['categories'][category] = {
                    'entry_count': count,
                    'total_size': size or 0,
                    'avg_access_count': avg_access or 0
                }
                stats['total_entries'] += count
                stats['total_size'] += size or 0
            
            return stats
            
        except Exception as e:
            print(f"Error getting memory stats: {e}")
            return {}
    
    def store_coordination_state(self, swarm_id: str, state_type: str, state_data: Dict):
        """Store coordination state for swarm recovery."""
        try:
            self.conn.execute("""
                INSERT INTO coordination_state (swarm_id, state_type, state_data)
                VALUES (?, ?, ?)
            """, (swarm_id, state_type, json.dumps(state_data)))
            self.conn.commit()
            return True
        except Exception as e:
            print(f"Error storing coordination state: {e}")
            return False
    
    def close(self):
        """Close database connection."""
        if hasattr(self, 'conn'):
            self.conn.close()


# Example usage functions for the coordination system
def store_agent_memory(agent_name: str, swarm_id: str, data: Any, memory_type: str = "state"):
    """Convenience function to store agent memory."""
    manager = MemoryPersistenceManager()
    memory_key = f"swarm-{swarm_id}/agent-{agent_name}/{memory_type}"
    return manager.store_memory(memory_key, data)


def retrieve_agent_memory(agent_name: str, swarm_id: str, memory_type: str = "state"):
    """Convenience function to retrieve agent memory."""
    manager = MemoryPersistenceManager()
    memory_key = f"swarm-{swarm_id}/agent-{agent_name}/{memory_type}"
    return manager.retrieve_memory(memory_key)


def store_swarm_coordination(swarm_id: str, coordination_data: Dict):
    """Store swarm-wide coordination data."""
    manager = MemoryPersistenceManager()
    memory_key = f"swarm-{swarm_id}/global/coordination"
    return manager.store_memory(memory_key, coordination_data)


if __name__ == "__main__":
    # Initialize and test the memory system
    manager = MemoryPersistenceManager()
    
    # Test basic operations
    test_data = {"test": "data", "timestamp": datetime.datetime.now().isoformat()}
    success = manager.store_memory("swarm-test/agent-test/state", test_data)
    print(f"Store operation: {'Success' if success else 'Failed'}")
    
    retrieved = manager.retrieve_memory("swarm-test/agent-test/state")
    print(f"Retrieved: {retrieved}")
    
    # Print usage stats
    stats = manager.get_memory_usage_stats()
    print(f"Memory usage stats: {stats}")
    
    manager.close()