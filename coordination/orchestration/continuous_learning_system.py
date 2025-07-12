#!/usr/bin/env python3
"""
Continuous Learning System for Swarm Coordination
Implements continuous improvement from agent interaction outcomes.
"""

import sqlite3
import json
import numpy as np
from datetime import datetime, timedelta
from typing import Dict, List, Tuple, Optional, Any
from dataclasses import dataclass
import logging
import asyncio
import threading
from collections import defaultdict
import subprocess
import os

@dataclass
class LearningOutcome:
    outcome_id: str
    learning_type: str
    source_pattern: str
    improvement_metrics: Dict[str, float]
    applied_models: List[str]
    success_score: float
    timestamp: datetime
    feedback_data: Dict[str, Any]

@dataclass
class ContinuousLearningConfig:
    learning_rate: float = 0.01
    feedback_window_minutes: int = 30
    min_samples_for_learning: int = 5
    performance_threshold: float = 7.0
    adaptation_frequency_minutes: int = 15
    enable_real_time_learning: bool = True
    enable_batch_learning: bool = True

class ContinuousLearningSystem:
    """System for continuous learning from swarm coordination outcomes."""
    
    def __init__(self, memory_db_path: str = ".swarm/memory.db", 
                 config: ContinuousLearningConfig = None):
        self.memory_db_path = memory_db_path
        self.config = config or ContinuousLearningConfig()
        self.logger = self._setup_logging()
        self.learning_history = []
        self.performance_tracker = defaultdict(list)
        self.model_performance = defaultdict(dict)
        self.is_running = False
        self.learning_thread = None
        self._initialize_learning_system()
        
    def _setup_logging(self) -> logging.Logger:
        """Setup logging for continuous learning."""
        logger = logging.getLogger('continuous_learning')
        logger.setLevel(logging.INFO)
        
        if not logger.handlers:
            handler = logging.StreamHandler()
            formatter = logging.Formatter(
                '[%(asctime)s] %(levelname)s [%(name)s] %(message)s'
            )
            handler.setFormatter(formatter)
            logger.addHandler(handler)
            
        return logger
        
    def _initialize_learning_system(self) -> None:
        """Initialize the continuous learning system."""
        
        # Create learning feedback table if it doesn't exist
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Check if learning_feedback table exists
                cursor.execute("""
                    CREATE TABLE IF NOT EXISTS learning_feedback (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        outcome_id TEXT UNIQUE,
                        learning_type TEXT,
                        source_pattern TEXT,
                        improvement_metrics TEXT,
                        applied_models TEXT,
                        success_score REAL,
                        feedback_data TEXT,
                        timestamp INTEGER DEFAULT (strftime('%s', 'now'))
                    )
                """)
                
                # Create model_performance tracking table
                cursor.execute("""
                    CREATE TABLE IF NOT EXISTS model_performance_tracking (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        model_id TEXT,
                        performance_metric TEXT,
                        value REAL,
                        context TEXT,
                        timestamp INTEGER DEFAULT (strftime('%s', 'now'))
                    )
                """)
                
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error initializing learning system: {e}")
            
        self.logger.info("Continuous learning system initialized")
        
    def start_continuous_learning(self) -> None:
        """Start continuous learning in background."""
        if self.is_running:
            self.logger.warning("Continuous learning is already running")
            return
            
        self.is_running = True
        self.learning_thread = threading.Thread(
            target=self._learning_loop,
            daemon=True
        )
        self.learning_thread.start()
        self.logger.info("Started continuous learning system")
        
    def stop_continuous_learning(self) -> None:
        """Stop continuous learning."""
        self.is_running = False
        if self.learning_thread:
            self.learning_thread.join(timeout=5.0)
        self.logger.info("Stopped continuous learning system")
        
    def _learning_loop(self) -> None:
        """Main continuous learning loop."""
        while self.is_running:
            try:
                # Collect recent interaction outcomes
                outcomes = self._collect_interaction_outcomes()
                
                # Learn from outcomes
                if outcomes:
                    learning_results = self._learn_from_outcomes(outcomes)
                    
                    # Apply learned improvements
                    if learning_results:
                        self._apply_learned_improvements(learning_results)
                        
                # Update model performance tracking
                self._update_performance_tracking()
                
                # Sleep until next learning cycle
                sleep_time = self.config.adaptation_frequency_minutes * 60
                threading.Event().wait(sleep_time)
                
            except Exception as e:
                self.logger.error(f"Error in learning loop: {e}")
                threading.Event().wait(60)  # Wait 1 minute on error
                
    def _collect_interaction_outcomes(self) -> List[Dict[str, Any]]:
        """Collect recent agent interaction outcomes."""
        outcomes = []
        cutoff_time = int((datetime.now() - timedelta(
            minutes=self.config.feedback_window_minutes
        )).timestamp())
        
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Get recent task completions and interactions
                cursor.execute("""
                    SELECT key, value, namespace, created_at 
                    FROM memory_entries 
                    WHERE created_at >= ? 
                    AND (key LIKE '%task%' OR key LIKE '%agent%' OR key LIKE '%coordination%')
                    ORDER BY created_at DESC
                """, (cutoff_time,))
                
                for row in cursor.fetchall():
                    key, value_str, namespace, timestamp = row
                    
                    try:
                        value = json.loads(value_str)
                        outcomes.append({
                            "key": key,
                            "value": value,
                            "namespace": namespace,
                            "timestamp": timestamp,
                            "outcome_type": self._classify_outcome_type(key, value)
                        })
                    except json.JSONDecodeError:
                        continue
                        
        except sqlite3.Error as e:
            self.logger.error(f"Error collecting outcomes: {e}")
            
        return outcomes
        
    def _classify_outcome_type(self, key: str, value: Dict) -> str:
        """Classify the type of interaction outcome."""
        if "completed" in key.lower() or "status" in str(value).lower():
            if "completed" in str(value).lower():
                return "task_success"
            elif "failed" in str(value).lower():
                return "task_failure"
            else:
                return "task_progress"
        elif "agent" in key.lower():
            return "agent_interaction"
        elif "coordination" in key.lower():
            return "coordination_event"
        else:
            return "general_outcome"
            
    def _learn_from_outcomes(self, outcomes: List[Dict[str, Any]]) -> List[LearningOutcome]:
        """Learn from interaction outcomes and generate improvements."""
        learning_results = []
        
        # Group outcomes by type for analysis
        outcome_groups = defaultdict(list)
        for outcome in outcomes:
            outcome_groups[outcome["outcome_type"]].append(outcome)
            
        # Learn from each outcome type
        for outcome_type, group_outcomes in outcome_groups.items():
            if len(group_outcomes) >= self.config.min_samples_for_learning:
                learning_result = self._analyze_outcome_group(outcome_type, group_outcomes)
                if learning_result:
                    learning_results.append(learning_result)
                    
        self.logger.info(f"Generated {len(learning_results)} learning outcomes")
        return learning_results
        
    def _analyze_outcome_group(self, outcome_type: str, outcomes: List[Dict[str, Any]]) -> Optional[LearningOutcome]:
        """Analyze a group of outcomes and generate learning insights."""
        
        # Calculate success metrics
        success_count = 0
        total_count = len(outcomes)
        response_times = []
        
        for outcome in outcomes:
            value = outcome["value"]
            
            # Count successes
            if isinstance(value, dict):
                if value.get("status") == "completed":
                    success_count += 1
                    
                # Track response times
                if "duration" in value and value["duration"]:
                    try:
                        response_times.append(float(value["duration"]))
                    except (ValueError, TypeError):
                        pass
                        
        success_rate = success_count / total_count if total_count > 0 else 0.0
        avg_response_time = np.mean(response_times) if response_times else 0.0
        
        # Generate learning outcome if significant patterns found
        if success_rate != 0.5:  # Non-random performance
            improvement_metrics = {
                "success_rate": success_rate,
                "avg_response_time": avg_response_time,
                "sample_size": total_count,
                "improvement_potential": self._calculate_improvement_potential(success_rate, avg_response_time)
            }
            
            return LearningOutcome(
                outcome_id=f"learning_{outcome_type}_{int(datetime.now().timestamp())}",
                learning_type=self._determine_learning_type(success_rate),
                source_pattern=outcome_type,
                improvement_metrics=improvement_metrics,
                applied_models=self._identify_relevant_models(outcome_type),
                success_score=success_rate * 10.0,  # Convert to 0-10 scale
                timestamp=datetime.now(),
                feedback_data={"outcomes_analyzed": len(outcomes)}
            )
            
        return None
        
    def _calculate_improvement_potential(self, success_rate: float, response_time: float) -> float:
        """Calculate the potential for improvement."""
        # Higher potential for low success rates or high response times
        success_potential = (1.0 - success_rate) * 0.7
        
        # Normalize response time potential (assuming 60s is "slow")
        time_potential = min(response_time / 60.0, 1.0) * 0.3
        
        return success_potential + time_potential
        
    def _determine_learning_type(self, success_rate: float) -> str:
        """Determine the type of learning needed."""
        if success_rate > 0.8:
            return "reinforcement"  # Reinforce successful patterns
        elif success_rate < 0.4:
            return "corrective"     # Fix failing patterns
        else:
            return "adaptive"       # Improve mediocre patterns
            
    def _identify_relevant_models(self, outcome_type: str) -> List[str]:
        """Identify neural models relevant to the outcome type."""
        model_mapping = {
            "task_success": ["boids", "hierarchical_boids", "reinforcement_swarm"],
            "task_failure": ["error_recovery", "adaptive_learning", "corrective_feedback"],
            "agent_interaction": ["social_forces", "multi_species_pso", "leadership_emergence"],
            "coordination_event": ["boids_aco_hybrid", "hierarchical_coordination", "swarm_optimization"],
            "general_outcome": ["neural_network_swarm", "adaptive_learning"]
        }
        
        return model_mapping.get(outcome_type, ["general_learning"])
        
    def _apply_learned_improvements(self, learning_results: List[LearningOutcome]) -> None:
        """Apply learned improvements to the system."""
        
        for result in learning_results:
            try:
                # Store learning outcome
                self._store_learning_outcome(result)
                
                # Apply specific learning type
                if result.learning_type == "reinforcement":
                    self._apply_reinforcement_learning(result)
                elif result.learning_type == "corrective":
                    self._apply_corrective_learning(result)
                elif result.learning_type == "adaptive":
                    self._apply_adaptive_learning(result)
                    
                # Execute neural-trained hook to save improvements
                self._execute_neural_trained_hook(result)
                
                self.learning_history.append(result)
                
            except Exception as e:
                self.logger.error(f"Error applying learning result {result.outcome_id}: {e}")
                
    def _store_learning_outcome(self, outcome: LearningOutcome) -> None:
        """Store learning outcome in database."""
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                cursor.execute("""
                    INSERT OR REPLACE INTO learning_feedback 
                    (outcome_id, learning_type, source_pattern, improvement_metrics, 
                     applied_models, success_score, feedback_data)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    outcome.outcome_id,
                    outcome.learning_type,
                    outcome.source_pattern,
                    json.dumps(outcome.improvement_metrics),
                    json.dumps(outcome.applied_models),
                    outcome.success_score,
                    json.dumps(outcome.feedback_data)
                ))
                
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error storing learning outcome: {e}")
            
    def _apply_reinforcement_learning(self, outcome: LearningOutcome) -> None:
        """Apply reinforcement learning for successful patterns."""
        self.logger.info(f"Applying reinforcement learning for {outcome.source_pattern}")
        
        # Increase weights/importance of successful patterns
        for model_id in outcome.applied_models:
            if model_id not in self.model_performance:
                self.model_performance[model_id] = {}
                
            current_weight = self.model_performance[model_id].get("reinforcement_weight", 1.0)
            new_weight = min(2.0, current_weight + self.config.learning_rate)
            self.model_performance[model_id]["reinforcement_weight"] = new_weight
            
    def _apply_corrective_learning(self, outcome: LearningOutcome) -> None:
        """Apply corrective learning for failed patterns."""
        self.logger.info(f"Applying corrective learning for {outcome.source_pattern}")
        
        # Reduce weights/importance of failing patterns
        for model_id in outcome.applied_models:
            if model_id not in self.model_performance:
                self.model_performance[model_id] = {}
                
            current_weight = self.model_performance[model_id].get("corrective_weight", 1.0)
            new_weight = max(0.1, current_weight - self.config.learning_rate)
            self.model_performance[model_id]["corrective_weight"] = new_weight
            
    def _apply_adaptive_learning(self, outcome: LearningOutcome) -> None:
        """Apply adaptive learning for mediocre patterns."""
        self.logger.info(f"Applying adaptive learning for {outcome.source_pattern}")
        
        # Adjust parameters based on performance metrics
        for model_id in outcome.applied_models:
            if model_id not in self.model_performance:
                self.model_performance[model_id] = {}
                
            # Adaptive parameter adjustment
            improvement_potential = outcome.improvement_metrics.get("improvement_potential", 0.0)
            adjustment = improvement_potential * self.config.learning_rate
            
            current_adaptation = self.model_performance[model_id].get("adaptive_factor", 1.0)
            new_adaptation = current_adaptation + adjustment
            self.model_performance[model_id]["adaptive_factor"] = new_adaptation
            
    def _execute_neural_trained_hook(self, outcome: LearningOutcome) -> None:
        """Execute the neural-trained hook to save pattern improvements."""
        try:
            # Use claude-flow hooks to save neural training results
            hook_command = [
                "npx", "claude-flow@alpha", "hooks", "neural-trained",
                "--pattern", outcome.source_pattern,
                "--improvement", str(outcome.success_score),
                "--models", ",".join(outcome.applied_models),
                "--learning-type", outcome.learning_type
            ]
            
            result = subprocess.run(
                hook_command,
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if result.returncode == 0:
                self.logger.info(f"Neural-trained hook executed successfully for {outcome.outcome_id}")
            else:
                self.logger.warning(f"Neural-trained hook failed: {result.stderr}")
                
        except subprocess.TimeoutExpired:
            self.logger.error("Neural-trained hook timed out")
        except Exception as e:
            self.logger.error(f"Error executing neural-trained hook: {e}")
            
    def _update_performance_tracking(self) -> None:
        """Update performance tracking for all models."""
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                for model_id, performance_data in self.model_performance.items():
                    for metric, value in performance_data.items():
                        cursor.execute("""
                            INSERT INTO model_performance_tracking 
                            (model_id, performance_metric, value, context)
                            VALUES (?, ?, ?, ?)
                        """, (model_id, metric, value, "continuous_learning"))
                        
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error updating performance tracking: {e}")
            
    def learn_from_agent_interactions(self, interaction_data: Dict[str, Any]) -> None:
        """Learn from specific agent interaction data."""
        
        # Create immediate learning outcome
        outcome = LearningOutcome(
            outcome_id=f"interaction_{int(datetime.now().timestamp())}",
            learning_type="real_time",
            source_pattern="agent_interaction",
            improvement_metrics=self._extract_interaction_metrics(interaction_data),
            applied_models=["social_forces", "agent_coordination"],
            success_score=self._score_interaction(interaction_data),
            timestamp=datetime.now(),
            feedback_data=interaction_data
        )
        
        # Apply learning immediately if real-time learning is enabled
        if self.config.enable_real_time_learning:
            self._apply_learned_improvements([outcome])
            
        self.logger.info(f"Learned from agent interaction: {outcome.success_score:.2f} score")
        
    def _extract_interaction_metrics(self, interaction_data: Dict[str, Any]) -> Dict[str, float]:
        """Extract metrics from interaction data."""
        metrics = {
            "interaction_quality": 5.0,  # Default neutral
            "response_time": 0.0,
            "cooperation_level": 5.0,
            "conflict_resolution": 5.0
        }
        
        # Extract actual metrics from interaction data
        if "duration" in interaction_data:
            try:
                metrics["response_time"] = float(interaction_data["duration"])
            except (ValueError, TypeError):
                pass
                
        if "success" in interaction_data:
            if interaction_data["success"]:
                metrics["interaction_quality"] = 8.0
                metrics["cooperation_level"] = 8.0
            else:
                metrics["interaction_quality"] = 3.0
                metrics["cooperation_level"] = 3.0
                
        return metrics
        
    def _score_interaction(self, interaction_data: Dict[str, Any]) -> float:
        """Score an agent interaction."""
        base_score = 5.0
        
        if "success" in interaction_data:
            base_score = 8.0 if interaction_data["success"] else 3.0
            
        if "efficiency" in interaction_data:
            try:
                efficiency = float(interaction_data["efficiency"])
                base_score = (base_score + efficiency) / 2
            except (ValueError, TypeError):
                pass
                
        return min(10.0, max(0.0, base_score))
        
    def get_learning_statistics(self) -> Dict[str, Any]:
        """Get statistics about continuous learning performance."""
        stats = {
            "total_learning_outcomes": len(self.learning_history),
            "learning_types": defaultdict(int),
            "model_performance_summary": {},
            "average_improvement": 0.0,
            "recent_learning_activity": []
        }
        
        if self.learning_history:
            # Count learning types
            for outcome in self.learning_history:
                stats["learning_types"][outcome.learning_type] += 1
                
            # Calculate average improvement
            scores = [outcome.success_score for outcome in self.learning_history]
            stats["average_improvement"] = np.mean(scores)
            
            # Get recent activity
            recent_outcomes = sorted(self.learning_history, key=lambda x: x.timestamp, reverse=True)[:5]
            stats["recent_learning_activity"] = [
                {
                    "outcome_id": outcome.outcome_id,
                    "learning_type": outcome.learning_type,
                    "success_score": outcome.success_score,
                    "timestamp": outcome.timestamp.isoformat()
                }
                for outcome in recent_outcomes
            ]
            
        # Model performance summary
        for model_id, performance in self.model_performance.items():
            stats["model_performance_summary"][model_id] = {
                "metrics_count": len(performance),
                "latest_values": dict(performance)
            }
            
        return stats

def main():
    """Main function for continuous learning system."""
    config = ContinuousLearningConfig(
        learning_rate=0.02,
        feedback_window_minutes=15,
        adaptation_frequency_minutes=10
    )
    
    learning_system = ContinuousLearningSystem(config=config)
    
    # Start continuous learning
    learning_system.start_continuous_learning()
    
    try:
        print("Continuous learning system started. Press Ctrl+C to stop.")
        while True:
            import time
            time.sleep(30)
            stats = learning_system.get_learning_statistics()
            print(f"Learning outcomes: {stats['total_learning_outcomes']}, "
                  f"avg improvement: {stats['average_improvement']:.2f}")
    except KeyboardInterrupt:
        print("Stopping continuous learning system...")
        learning_system.stop_continuous_learning()

if __name__ == "__main__":
    main()