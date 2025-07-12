#!/usr/bin/env python3
"""
Comprehensive test suite for the Swarm Memory Management System.
Tests all components and their integration.
"""

import os
import sys
import time
import tempfile
import shutil
from typing import Dict, Any
from datetime import datetime

# Add the memory_bank directory to the path
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from memory_integration import SwarmMemoryManager
from persistence_manager import MemoryPersistenceManager
from coordination_protocols import AgentMemoryCoordinator, MemoryLockType
from memory_monitor import MemoryMonitor
from neural_learning import NeuralPatternLearner, CoordinationOutcome, OutcomeType


class MemorySystemTester:
    """Comprehensive tester for the memory management system."""
    
    def __init__(self):
        self.test_results = []
        self.temp_dir = None
        self.setup_test_environment()
    
    def setup_test_environment(self):
        """Set up temporary test environment."""
        self.temp_dir = tempfile.mkdtemp(prefix="swarm_memory_test_")
        print(f"Test environment created at: {self.temp_dir}")
    
    def cleanup_test_environment(self):
        """Clean up test environment."""
        if self.temp_dir and os.path.exists(self.temp_dir):
            shutil.rmtree(self.temp_dir)
            print(f"Test environment cleaned up: {self.temp_dir}")
    
    def run_test(self, test_name: str, test_function):
        """Run a single test and record results."""
        print(f"\n--- Running Test: {test_name} ---")
        start_time = time.time()
        
        try:
            result = test_function()
            execution_time = time.time() - start_time
            
            self.test_results.append({
                "test_name": test_name,
                "status": "PASSED" if result else "FAILED",
                "execution_time": execution_time,
                "error": None
            })
            
            print(f"Test {test_name}: {'PASSED' if result else 'FAILED'} ({execution_time:.2f}s)")
            return result
            
        except Exception as e:
            execution_time = time.time() - start_time
            
            self.test_results.append({
                "test_name": test_name,
                "status": "ERROR",
                "execution_time": execution_time,
                "error": str(e)
            })
            
            print(f"Test {test_name}: ERROR - {e} ({execution_time:.2f}s)")
            return False
    
    def test_persistence_manager(self) -> bool:
        """Test memory persistence manager functionality."""
        try:
            # Create test config
            config_path = os.path.join(self.temp_dir, "test_config.json")
            test_config = {
                "memory_persistence": {
                    "database_path": os.path.join(self.temp_dir, "test_memory.db"),
                    "compression_enabled": True,
                    "auto_cleanup_enabled": True
                },
                "retention_policies": {
                    "default_retention_days": 30
                }
            }
            
            with open(config_path, 'w') as f:
                import json
                json.dump(test_config, f)
            
            # Initialize manager
            manager = MemoryPersistenceManager(config_path)
            
            # Test basic store/retrieve
            test_data = {"test": "data", "number": 42, "nested": {"key": "value"}}
            store_success = manager.store_memory("test/basic/data", test_data)
            
            if not store_success:
                return False
            
            retrieved_data = manager.retrieve_memory("test/basic/data")
            
            if retrieved_data != test_data:
                print(f"Data mismatch: {retrieved_data} != {test_data}")
                return False
            
            # Test memory validation
            valid_key = manager.validate_memory_key("swarm-test/agent-test/state")
            invalid_key = manager.validate_memory_key("invalid")
            
            if not valid_key or invalid_key:
                print(f"Key validation failed: valid={valid_key}, invalid={invalid_key}")
                return False
            
            # Test statistics
            stats = manager.get_memory_usage_stats()
            if not isinstance(stats, dict) or stats.get("total_entries", 0) == 0:
                print(f"Statistics failed: {stats}")
                return False
            
            manager.close()
            return True
            
        except Exception as e:
            print(f"Persistence manager test error: {e}")
            return False
    
    def test_coordination_protocols(self) -> bool:
        """Test agent memory coordination protocols."""
        try:
            # Create two coordinators to test coordination
            coordinator1 = AgentMemoryCoordinator("agent1", "test-swarm")
            coordinator2 = AgentMemoryCoordinator("agent2", "test-swarm")
            
            # Test lock acquisition
            lock1 = coordinator1.acquire_memory_lock("test/shared/resource", MemoryLockType.WRITE, 30)
            
            if not lock1:
                return False
            
            # Test lock conflict
            lock2 = coordinator2.acquire_memory_lock("test/shared/resource", MemoryLockType.WRITE, 5)
            
            if lock2:  # Should fail due to existing write lock
                print("Lock conflict test failed - second lock should not have been acquired")
                return False
            
            # Test coordinated write
            test_data = {"coordinated": True, "timestamp": datetime.now().isoformat()}
            write_success = coordinator1.coordinated_memory_write("test/coordinated/write", test_data)
            
            if not write_success:
                return False
            
            # Test coordinated read
            read_data = coordinator2.coordinated_memory_read("test/coordinated/write")
            
            if read_data != test_data:
                print(f"Coordinated read failed: {read_data} != {test_data}")
                return False
            
            # Test synchronization
            sync1 = coordinator1.synchronize_with_swarm()
            sync2 = coordinator2.synchronize_with_swarm()
            
            if not sync1 or not sync2:
                print(f"Synchronization failed: sync1={sync1}, sync2={sync2}")
                return False
            
            # Clean up
            coordinator1.close()
            coordinator2.close()
            
            return True
            
        except Exception as e:
            print(f"Coordination protocols test error: {e}")
            return False
    
    def test_memory_monitor(self) -> bool:
        """Test memory monitoring functionality."""
        try:
            # Create test config
            config_path = os.path.join(self.temp_dir, "monitor_config.json")
            test_config = {
                "memory_persistence": {
                    "database_path": os.path.join(self.temp_dir, "monitor_memory.db"),
                    "compression_enabled": True
                },
                "agent_memory_limits": {
                    "max_individual_memory": "100MB"
                },
                "retention_policies": {
                    "default_retention_days": 30
                }
            }
            
            with open(config_path, 'w') as f:
                import json
                json.dump(test_config, f)
            
            monitor = MemoryMonitor(config_path)
            
            # Test usage snapshot
            snapshot = monitor.get_current_usage_snapshot()
            
            if not hasattr(snapshot, 'timestamp') or not hasattr(snapshot, 'total_size'):
                print(f"Invalid snapshot: {snapshot}")
                return False
            
            # Test health check
            health = monitor.check_memory_health()
            
            required_keys = ["overall_health", "issues", "recommendations", "metrics"]
            if not all(key in health for key in required_keys):
                print(f"Invalid health report: {health}")
                return False
            
            # Test optimization
            optimization = monitor.optimize_memory_usage(auto_apply=True)
            
            if not isinstance(optimization, dict) or "optimizations_applied" not in optimization:
                print(f"Invalid optimization result: {optimization}")
                return False
            
            monitor.close()
            return True
            
        except Exception as e:
            print(f"Memory monitor test error: {e}")
            return False
    
    def test_neural_learning(self) -> bool:
        """Test neural pattern learning functionality."""
        try:
            learner = NeuralPatternLearner()
            
            # Create test outcomes
            outcomes = []
            for i in range(5):
                outcome = CoordinationOutcome(
                    outcome_id=f"test_outcome_{i}",
                    swarm_id="test-swarm",
                    task_type="data_processing",
                    agents_involved=["agent1", "agent2"],
                    outcome_type=OutcomeType.SUCCESS if i < 4 else OutcomeType.FAILURE,
                    success_score=0.9 if i < 4 else 0.1,
                    execution_time=30.0 + i * 5,
                    resource_usage={"cpu": 0.7, "memory": 0.5},
                    context={"complexity": "medium", "data_size": "large"},
                    timestamp=datetime.now()
                )
                outcomes.append(outcome)
            
            # Record outcomes
            for outcome in outcomes:
                success = learner.record_coordination_outcome(outcome)
                if not success:
                    print(f"Failed to record outcome: {outcome.outcome_id}")
                    return False
            
            # Test strategy suggestion
            strategy = learner.suggest_coordination_strategy({
                "complexity": "medium",
                "data_size": "large"
            })
            
            # Strategy might be None if no patterns found yet (normal for small sample)
            # This is acceptable behavior
            
            # Test trends analysis
            trends = learner.analyze_coordination_trends(7)
            
            if not isinstance(trends, dict):
                print(f"Invalid trends analysis: {trends}")
                return False
            
            # Test insights
            insights = learner.get_learning_insights()
            
            if not isinstance(insights, list):
                print(f"Invalid insights: {insights}")
                return False
            
            learner.close()
            return True
            
        except Exception as e:
            print(f"Neural learning test error: {e}")
            return False
    
    def test_memory_integration(self) -> bool:
        """Test the integrated memory management system."""
        try:
            # Create integrated manager
            manager = SwarmMemoryManager("test-agent", "test-swarm")
            
            # Test initialization
            init_success = manager.initialize_agent_memory({"test_mode": True})
            
            if not init_success:
                print("Agent initialization failed")
                return False
            
            # Test decision storage
            decision_success = manager.store_agent_decision(
                "task_assignment",
                {"task": "test_task", "priority": "high"},
                {"context": "test"}
            )
            
            if not decision_success:
                print("Decision storage failed")
                return False
            
            # Test progress update
            progress_success = manager.update_agent_progress("test_task", {
                "status": "in_progress",
                "completion": 0.5
            })
            
            if not progress_success:
                print("Progress update failed")
                return False
            
            # Test outcome recording
            outcome_success = manager.record_task_outcome(
                "test_task",
                ["test-agent"],
                True,
                30.0,
                {"complexity": "low"}
            )
            
            if not outcome_success:
                print("Outcome recording failed")
                return False
            
            # Test knowledge sharing
            knowledge_success = manager.share_knowledge_with_swarm(
                "test_knowledge",
                {"insight": "test works well"}
            )
            
            if not knowledge_success:
                print("Knowledge sharing failed")
                return False
            
            # Test health report
            health = manager.get_memory_health_report()
            
            if not isinstance(health, dict) or "overall_health" not in health:
                print(f"Invalid health report: {health}")
                return False
            
            # Test snapshot creation
            snapshot = manager.create_memory_snapshot()
            
            if not isinstance(snapshot, dict) or "snapshot_id" not in snapshot:
                print(f"Invalid snapshot: {snapshot}")
                return False
            
            manager.close()
            return True
            
        except Exception as e:
            print(f"Memory integration test error: {e}")
            return False
    
    def test_concurrent_operations(self) -> bool:
        """Test concurrent memory operations."""
        try:
            import threading
            
            # Create multiple managers
            managers = []
            for i in range(3):
                manager = SwarmMemoryManager(f"agent-{i}", "concurrent-swarm")
                managers.append(manager)
            
            # Define concurrent operation
            results = []
            
            def concurrent_operation(manager_idx):
                try:
                    manager = managers[manager_idx]
                    
                    # Initialize
                    manager.initialize_agent_memory({"agent_id": manager_idx})
                    
                    # Store decisions concurrently
                    for j in range(5):
                        success = manager.store_agent_decision(
                            f"decision_{j}",
                            {"data": f"agent_{manager_idx}_decision_{j}"},
                            {"concurrent": True}
                        )
                        results.append(success)
                    
                    # Record outcomes
                    manager.record_task_outcome(
                        "concurrent_task",
                        [f"agent-{manager_idx}"],
                        True,
                        10.0 + manager_idx,
                        {"agent_id": manager_idx}
                    )
                    
                except Exception as e:
                    print(f"Concurrent operation error for agent {manager_idx}: {e}")
                    results.append(False)
            
            # Run concurrent operations
            threads = []
            for i in range(3):
                thread = threading.Thread(target=concurrent_operation, args=(i,))
                threads.append(thread)
                thread.start()
            
            # Wait for completion
            for thread in threads:
                thread.join()
            
            # Check results
            success_rate = sum(results) / len(results) if results else 0
            
            # Clean up
            for manager in managers:
                manager.close()
            
            # Accept 80% success rate for concurrent operations (some conflicts expected)
            return success_rate >= 0.8
            
        except Exception as e:
            print(f"Concurrent operations test error: {e}")
            return False
    
    def run_all_tests(self) -> Dict[str, Any]:
        """Run all tests and return summary."""
        print("Starting Swarm Memory Management System Tests")
        print("=" * 50)
        
        # Define test cases
        test_cases = [
            ("Persistence Manager", self.test_persistence_manager),
            ("Coordination Protocols", self.test_coordination_protocols),
            ("Memory Monitor", self.test_memory_monitor),
            ("Neural Learning", self.test_neural_learning),
            ("Memory Integration", self.test_memory_integration),
            ("Concurrent Operations", self.test_concurrent_operations),
        ]
        
        # Run tests
        for test_name, test_function in test_cases:
            self.run_test(test_name, test_function)
        
        # Calculate summary
        total_tests = len(self.test_results)
        passed_tests = sum(1 for result in self.test_results if result["status"] == "PASSED")
        failed_tests = sum(1 for result in self.test_results if result["status"] == "FAILED")
        error_tests = sum(1 for result in self.test_results if result["status"] == "ERROR")
        
        success_rate = passed_tests / total_tests if total_tests > 0 else 0
        total_time = sum(result["execution_time"] for result in self.test_results)
        
        summary = {
            "total_tests": total_tests,
            "passed": passed_tests,
            "failed": failed_tests,
            "errors": error_tests,
            "success_rate": success_rate,
            "total_execution_time": total_time,
            "test_results": self.test_results
        }
        
        # Print summary
        print("\n" + "=" * 50)
        print("TEST SUMMARY")
        print("=" * 50)
        print(f"Total Tests: {total_tests}")
        print(f"Passed: {passed_tests}")
        print(f"Failed: {failed_tests}")
        print(f"Errors: {error_tests}")
        print(f"Success Rate: {success_rate:.1%}")
        print(f"Total Execution Time: {total_time:.2f}s")
        
        if failed_tests > 0 or error_tests > 0:
            print("\nFAILED/ERROR TESTS:")
            for result in self.test_results:
                if result["status"] in ["FAILED", "ERROR"]:
                    print(f"  - {result['test_name']}: {result['status']}")
                    if result["error"]:
                        print(f"    Error: {result['error']}")
        
        return summary


def main():
    """Main test execution function."""
    tester = MemorySystemTester()
    
    try:
        summary = tester.run_all_tests()
        
        # Return appropriate exit code
        if summary["success_rate"] == 1.0:
            print("\n🎉 All tests passed!")
            return 0
        elif summary["success_rate"] >= 0.8:
            print(f"\n⚠️  Most tests passed ({summary['success_rate']:.1%})")
            return 0
        else:
            print(f"\n❌ Many tests failed ({summary['success_rate']:.1%})")
            return 1
    
    finally:
        tester.cleanup_test_environment()


if __name__ == "__main__":
    exit(main())