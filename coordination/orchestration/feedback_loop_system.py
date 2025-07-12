#!/usr/bin/env python3
"""
Feedback Loop System for Neural Pattern Learning
Creates closed-loop improvement based on task success rates and coordination outcomes.
"""

import sqlite3
import json
import numpy as np
from datetime import datetime, timedelta
from typing import Dict, List, Tuple, Optional, Any
from dataclasses import dataclass
import logging
import threading
import time
from collections import defaultdict, deque
import statistics

@dataclass
class FeedbackMetrics:
    metric_id: str
    metric_type: str
    value: float
    target_value: float
    improvement_needed: float
    confidence: float
    timestamp: datetime

@dataclass
class ImprovementAction:
    action_id: str
    action_type: str
    target_models: List[str]
    parameters: Dict[str, Any]
    expected_improvement: float
    priority: int
    timestamp: datetime

class FeedbackLoopSystem:
    """System for creating feedback loops for continuous model improvement."""
    
    def __init__(self, memory_db_path: str = ".swarm/memory.db"):
        self.memory_db_path = memory_db_path
        self.logger = self._setup_logging()
        self.feedback_metrics = deque(maxlen=1000)
        self.improvement_actions = []
        self.success_rate_history = defaultdict(deque)
        self.performance_baselines = {}
        self.is_running = False
        self.feedback_thread = None
        self._initialize_feedback_system()
        
    def _setup_logging(self) -> logging.Logger:
        """Setup logging for feedback loop system."""
        logger = logging.getLogger('feedback_loop')
        logger.setLevel(logging.INFO)
        
        if not logger.handlers:
            handler = logging.StreamHandler()
            formatter = logging.Formatter(
                '[%(asctime)s] %(levelname)s [%(name)s] %(message)s'
            )
            handler.setFormatter(formatter)
            logger.addHandler(handler)
            
        return logger
        
    def _initialize_feedback_system(self) -> None:
        """Initialize the feedback loop system."""
        
        # Create feedback tables if they don't exist
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Feedback metrics table
                cursor.execute("""
                    CREATE TABLE IF NOT EXISTS feedback_metrics (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        metric_id TEXT UNIQUE,
                        metric_type TEXT,
                        value REAL,
                        target_value REAL,
                        improvement_needed REAL,
                        confidence REAL,
                        timestamp INTEGER DEFAULT (strftime('%s', 'now'))
                    )
                """)
                
                # Improvement actions table
                cursor.execute("""
                    CREATE TABLE IF NOT EXISTS improvement_actions (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        action_id TEXT UNIQUE,
                        action_type TEXT,
                        target_models TEXT,
                        parameters TEXT,
                        expected_improvement REAL,
                        priority INTEGER,
                        executed_at INTEGER,
                        results TEXT,
                        timestamp INTEGER DEFAULT (strftime('%s', 'now'))
                    )
                """)
                
                # Performance baselines table
                cursor.execute("""
                    CREATE TABLE IF NOT EXISTS performance_baselines (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        model_id TEXT,
                        baseline_type TEXT,
                        baseline_value REAL,
                        measurement_context TEXT,
                        timestamp INTEGER DEFAULT (strftime('%s', 'now'))
                    )
                """)
                
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error initializing feedback system: {e}")
            
        # Initialize performance baselines
        self._initialize_baselines()
        
        self.logger.info("Feedback loop system initialized")
        
    def _initialize_baselines(self) -> None:
        """Initialize performance baselines for models."""
        
        # Default baselines for different metrics
        default_baselines = {
            "task_success_rate": 0.8,          # 80% success rate target
            "response_time": 30.0,             # 30 seconds max response time
            "coordination_efficiency": 0.75,   # 75% coordination efficiency
            "resource_utilization": 0.85,      # 85% resource utilization
            "error_rate": 0.05,                # 5% max error rate
            "scalability_factor": 0.9,         # 90% performance retention when scaling
            "adaptation_speed": 60.0,          # 60 seconds max adaptation time
            "learning_convergence": 0.8        # 80% learning convergence rate
        }
        
        self.performance_baselines = default_baselines.copy()
        
        # Load existing baselines from database
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                cursor.execute("""
                    SELECT baseline_type, AVG(baseline_value) as avg_baseline
                    FROM performance_baselines 
                    GROUP BY baseline_type
                """)
                
                for row in cursor.fetchall():
                    baseline_type, avg_value = row
                    if baseline_type in self.performance_baselines:
                        self.performance_baselines[baseline_type] = avg_value
                        
        except sqlite3.Error as e:
            self.logger.error(f"Error loading baselines: {e}")
            
    def start_feedback_loop(self, evaluation_interval: int = 120) -> None:
        """Start the feedback loop system."""
        if self.is_running:
            self.logger.warning("Feedback loop is already running")
            return
            
        self.is_running = True
        self.feedback_thread = threading.Thread(
            target=self._feedback_loop,
            args=(evaluation_interval,),
            daemon=True
        )
        self.feedback_thread.start()
        self.logger.info(f"Started feedback loop with {evaluation_interval}s evaluation interval")
        
    def stop_feedback_loop(self) -> None:
        """Stop the feedback loop system."""
        self.is_running = False
        if self.feedback_thread:
            self.feedback_thread.join(timeout=5.0)
        self.logger.info("Stopped feedback loop system")
        
    def _feedback_loop(self, evaluation_interval: int) -> None:
        """Main feedback loop for continuous improvement."""
        while self.is_running:
            try:
                # Collect current performance metrics
                current_metrics = self._collect_performance_metrics()
                
                # Evaluate against baselines
                feedback_metrics = self._evaluate_against_baselines(current_metrics)
                
                # Generate improvement actions
                improvement_actions = self._generate_improvement_actions(feedback_metrics)
                
                # Execute high-priority improvements
                self._execute_improvement_actions(improvement_actions)
                
                # Update baselines based on sustained performance
                self._update_baselines(current_metrics)
                
                # Store feedback data
                self._store_feedback_data(feedback_metrics, improvement_actions)
                
                # Sleep until next evaluation
                time.sleep(evaluation_interval)
                
            except Exception as e:
                self.logger.error(f"Error in feedback loop: {e}")
                time.sleep(60)  # Wait 1 minute on error
                
    def _collect_performance_metrics(self) -> Dict[str, float]:
        """Collect current performance metrics from the system."""
        metrics = {}
        
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Calculate task success rate
                cursor.execute("""
                    SELECT 
                        COUNT(CASE WHEN value LIKE '%completed%' THEN 1 END) * 1.0 / COUNT(*) as success_rate
                    FROM memory_entries 
                    WHERE key LIKE '%task%' 
                    AND created_at >= ?
                """, (int((datetime.now() - timedelta(hours=1)).timestamp()),))
                
                result = cursor.fetchone()
                if result and result[0] is not None:
                    metrics["task_success_rate"] = result[0]
                else:
                    metrics["task_success_rate"] = 0.5  # Default neutral
                    
                # Calculate average response time
                cursor.execute("""
                    SELECT AVG(
                        CASE 
                            WHEN json_extract(value, '$.duration') IS NOT NULL 
                            THEN CAST(json_extract(value, '$.duration') AS REAL)
                            ELSE NULL
                        END
                    ) as avg_response_time
                    FROM memory_entries 
                    WHERE key LIKE '%task%' 
                    AND created_at >= ?
                """, (int((datetime.now() - timedelta(hours=1)).timestamp()),))
                
                result = cursor.fetchone()
                if result and result[0] is not None:
                    metrics["response_time"] = result[0]
                else:
                    metrics["response_time"] = 45.0  # Default moderate
                    
                # Calculate coordination efficiency (approximated by parallel task completion)
                cursor.execute("""
                    SELECT COUNT(DISTINCT key) * 1.0 / MAX(1, 
                        (strftime('%s', 'now') - MIN(created_at)) / 60.0
                    ) as coordination_efficiency
                    FROM memory_entries 
                    WHERE namespace = 'coordination'
                    AND created_at >= ?
                """, (int((datetime.now() - timedelta(hours=1)).timestamp()),))
                
                result = cursor.fetchone()
                if result and result[0] is not None:
                    metrics["coordination_efficiency"] = min(1.0, result[0] / 10.0)  # Normalize
                else:
                    metrics["coordination_efficiency"] = 0.6  # Default moderate
                    
                # Estimate error rate
                cursor.execute("""
                    SELECT 
                        COUNT(CASE WHEN value LIKE '%error%' OR value LIKE '%failed%' THEN 1 END) * 1.0 / 
                        MAX(1, COUNT(*)) as error_rate
                    FROM memory_entries 
                    WHERE created_at >= ?
                """, (int((datetime.now() - timedelta(hours=1)).timestamp()),))
                
                result = cursor.fetchone()
                if result and result[0] is not None:
                    metrics["error_rate"] = result[0]
                else:
                    metrics["error_rate"] = 0.1  # Default low
                    
        except sqlite3.Error as e:
            self.logger.error(f"Error collecting metrics: {e}")
            # Return default metrics on error
            metrics = {
                "task_success_rate": 0.5,
                "response_time": 45.0,
                "coordination_efficiency": 0.6,
                "error_rate": 0.1
            }
            
        # Add derived metrics
        metrics["resource_utilization"] = self._estimate_resource_utilization(metrics)
        metrics["scalability_factor"] = self._estimate_scalability_factor(metrics)
        metrics["adaptation_speed"] = self._estimate_adaptation_speed(metrics)
        metrics["learning_convergence"] = self._estimate_learning_convergence(metrics)
        
        return metrics
        
    def _estimate_resource_utilization(self, metrics: Dict[str, float]) -> float:
        """Estimate resource utilization based on other metrics."""
        # Higher success rate and lower response time indicates better resource use
        success_factor = metrics.get("task_success_rate", 0.5)
        speed_factor = max(0.1, 1.0 - (metrics.get("response_time", 45.0) / 100.0))
        return (success_factor + speed_factor) / 2.0
        
    def _estimate_scalability_factor(self, metrics: Dict[str, float]) -> float:
        """Estimate scalability factor based on coordination efficiency."""
        coord_eff = metrics.get("coordination_efficiency", 0.6)
        error_rate = metrics.get("error_rate", 0.1)
        return max(0.1, coord_eff * (1.0 - error_rate))
        
    def _estimate_adaptation_speed(self, metrics: Dict[str, float]) -> float:
        """Estimate adaptation speed (lower is better, so invert for score)."""
        response_time = metrics.get("response_time", 45.0)
        # Convert to score where lower response time = higher score
        return max(10.0, 100.0 - response_time)
        
    def _estimate_learning_convergence(self, metrics: Dict[str, float]) -> float:
        """Estimate learning convergence rate."""
        success_rate = metrics.get("task_success_rate", 0.5)
        coord_eff = metrics.get("coordination_efficiency", 0.6)
        return (success_rate + coord_eff) / 2.0
        
    def _evaluate_against_baselines(self, current_metrics: Dict[str, float]) -> List[FeedbackMetrics]:
        """Evaluate current metrics against performance baselines."""
        feedback_metrics = []
        
        for metric_type, current_value in current_metrics.items():
            if metric_type in self.performance_baselines:
                target_value = self.performance_baselines[metric_type]
                
                # Calculate improvement needed (can be negative for good performance)
                if metric_type in ["response_time", "error_rate", "adaptation_speed"]:
                    # Lower is better for these metrics
                    improvement_needed = current_value - target_value
                else:
                    # Higher is better for these metrics
                    improvement_needed = target_value - current_value
                    
                # Calculate confidence based on how far from target
                confidence = max(0.1, 1.0 - abs(improvement_needed) / max(target_value, 0.1))
                
                feedback_metric = FeedbackMetrics(
                    metric_id=f"feedback_{metric_type}_{int(time.time())}",
                    metric_type=metric_type,
                    value=current_value,
                    target_value=target_value,
                    improvement_needed=improvement_needed,
                    confidence=confidence,
                    timestamp=datetime.now()
                )
                
                feedback_metrics.append(feedback_metric)
                self.feedback_metrics.append(feedback_metric)
                
        return feedback_metrics
        
    def _generate_improvement_actions(self, feedback_metrics: List[FeedbackMetrics]) -> List[ImprovementAction]:
        """Generate improvement actions based on feedback metrics."""
        improvement_actions = []
        
        for metric in feedback_metrics:
            if metric.improvement_needed > 0.1:  # Needs significant improvement
                action = self._create_improvement_action(metric)
                if action:
                    improvement_actions.append(action)
                    
        # Sort by priority (higher number = higher priority)
        improvement_actions.sort(key=lambda x: x.priority, reverse=True)
        
        return improvement_actions
        
    def _create_improvement_action(self, metric: FeedbackMetrics) -> Optional[ImprovementAction]:
        """Create a specific improvement action for a metric."""
        
        action_strategies = {
            "task_success_rate": {
                "action_type": "enhance_coordination",
                "target_models": ["boids", "hierarchical_boids", "reinforcement_swarm"],
                "parameters": {"coordination_weight": 1.2, "learning_rate": 0.02},
                "priority": 5
            },
            "response_time": {
                "action_type": "optimize_performance",
                "target_models": ["ecs_boids", "job_system_pso", "gpu_compute_aco"],
                "parameters": {"parallel_factor": 1.5, "cache_optimization": True},
                "priority": 4
            },
            "coordination_efficiency": {
                "action_type": "improve_coordination",
                "target_models": ["social_forces", "boids_aco_hybrid", "leadership_emergence"],
                "parameters": {"communication_efficiency": 1.3, "conflict_resolution": True},
                "priority": 5
            },
            "error_rate": {
                "action_type": "enhance_reliability",
                "target_models": ["error_recovery", "adaptive_learning", "neural_network_swarm"],
                "parameters": {"error_prediction": True, "recovery_speed": 1.5},
                "priority": 6
            },
            "resource_utilization": {
                "action_type": "optimize_resources",
                "target_models": ["memory_pooling", "lod_swarm", "temporal_caching"],
                "parameters": {"resource_efficiency": 1.2, "load_balancing": True},
                "priority": 3
            }
        }
        
        strategy = action_strategies.get(metric.metric_type)
        if not strategy:
            return None
            
        # Calculate expected improvement based on improvement needed and confidence
        expected_improvement = metric.improvement_needed * metric.confidence * 0.8
        
        return ImprovementAction(
            action_id=f"action_{metric.metric_type}_{int(time.time())}",
            action_type=strategy["action_type"],
            target_models=strategy["target_models"],
            parameters=strategy["parameters"],
            expected_improvement=expected_improvement,
            priority=strategy["priority"],
            timestamp=datetime.now()
        )
        
    def _execute_improvement_actions(self, actions: List[ImprovementAction]) -> None:
        """Execute high-priority improvement actions."""
        
        executed_count = 0
        max_executions = 3  # Limit concurrent improvements
        
        for action in actions:
            if action.priority >= 5 and executed_count < max_executions:
                try:
                    self._execute_single_action(action)
                    executed_count += 1
                    self.improvement_actions.append(action)
                    
                    self.logger.info(f"Executed improvement action: {action.action_type} "
                                   f"for models: {action.target_models}")
                    
                except Exception as e:
                    self.logger.error(f"Error executing action {action.action_id}: {e}")
                    
    def _execute_single_action(self, action: ImprovementAction) -> None:
        """Execute a single improvement action."""
        
        # Store action execution
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                cursor.execute("""
                    INSERT INTO improvement_actions 
                    (action_id, action_type, target_models, parameters, 
                     expected_improvement, priority, executed_at)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    action.action_id,
                    action.action_type,
                    json.dumps(action.target_models),
                    json.dumps(action.parameters),
                    action.expected_improvement,
                    action.priority,
                    int(time.time())
                ))
                
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error storing action execution: {e}")
            
        # Execute the actual improvement (would interface with neural models)
        # For now, this is a placeholder that logs the action
        self.logger.info(f"Applying {action.action_type} to models {action.target_models} "
                        f"with parameters {action.parameters}")
        
        # In a real implementation, this would:
        # 1. Update model parameters
        # 2. Retrain specific models
        # 3. Adjust coordination algorithms
        # 4. Optimize resource allocation
        
    def _update_baselines(self, current_metrics: Dict[str, float]) -> None:
        """Update performance baselines based on sustained good performance."""
        
        for metric_type, current_value in current_metrics.items():
            if metric_type in self.performance_baselines:
                baseline = self.performance_baselines[metric_type]
                
                # Update baseline if performance consistently exceeds it
                if metric_type in ["response_time", "error_rate", "adaptation_speed"]:
                    # Lower is better - update baseline downward if consistently better
                    if current_value < baseline * 0.9:  # 10% better consistently
                        new_baseline = (baseline + current_value) / 2
                        self.performance_baselines[metric_type] = new_baseline
                else:
                    # Higher is better - update baseline upward if consistently better  
                    if current_value > baseline * 1.1:  # 10% better consistently
                        new_baseline = (baseline + current_value) / 2
                        self.performance_baselines[metric_type] = new_baseline
                        
    def _store_feedback_data(self, feedback_metrics: List[FeedbackMetrics], 
                           actions: List[ImprovementAction]) -> None:
        """Store feedback data in the database."""
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Store feedback metrics
                for metric in feedback_metrics:
                    cursor.execute("""
                        INSERT OR REPLACE INTO feedback_metrics 
                        (metric_id, metric_type, value, target_value, 
                         improvement_needed, confidence)
                        VALUES (?, ?, ?, ?, ?, ?)
                    """, (
                        metric.metric_id,
                        metric.metric_type,
                        metric.value,
                        metric.target_value,
                        metric.improvement_needed,
                        metric.confidence
                    ))
                    
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error storing feedback data: {e}")
            
    def get_feedback_statistics(self) -> Dict[str, Any]:
        """Get statistics about feedback loop performance."""
        stats = {
            "total_feedback_cycles": len(self.feedback_metrics),
            "improvement_actions_taken": len(self.improvement_actions),
            "current_baselines": dict(self.performance_baselines),
            "recent_metrics": {},
            "improvement_trends": {}
        }
        
        if self.feedback_metrics:
            # Get recent metrics by type
            recent_metrics = defaultdict(list)
            for metric in list(self.feedback_metrics)[-20:]:  # Last 20 metrics
                recent_metrics[metric.metric_type].append(metric.value)
                
            for metric_type, values in recent_metrics.items():
                if values:
                    stats["recent_metrics"][metric_type] = {
                        "current": values[-1],
                        "average": statistics.mean(values),
                        "trend": "improving" if len(values) > 1 and values[-1] > values[0] else "stable"
                    }
                    
        # Calculate improvement trends
        if self.improvement_actions:
            action_types = defaultdict(int)
            for action in self.improvement_actions[-10:]:  # Last 10 actions
                action_types[action.action_type] += 1
                
            stats["improvement_trends"] = dict(action_types)
            
        return stats

def main():
    """Main function for feedback loop system."""
    feedback_system = FeedbackLoopSystem()
    
    # Start feedback loop
    feedback_system.start_feedback_loop(evaluation_interval=60)  # 1 minute intervals for demo
    
    try:
        print("Feedback loop system started. Press Ctrl+C to stop.")
        while True:
            time.sleep(30)
            stats = feedback_system.get_feedback_statistics()
            print(f"Feedback cycles: {stats['total_feedback_cycles']}, "
                  f"improvements: {stats['improvement_actions_taken']}")
    except KeyboardInterrupt:
        print("Stopping feedback loop system...")
        feedback_system.stop_feedback_loop()

if __name__ == "__main__":
    main()