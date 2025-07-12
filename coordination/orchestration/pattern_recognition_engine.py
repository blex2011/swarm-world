#!/usr/bin/env python3
"""
Pattern Recognition Engine for Swarm Coordination
Automatically recognizes successful coordination patterns and triggers learning.
"""

import sqlite3
import json
import numpy as np
from datetime import datetime, timedelta
from typing import Dict, List, Tuple, Optional, Any, Callable
from dataclasses import dataclass
import logging
import threading
import time
from collections import defaultdict, deque

@dataclass
class RecognizedPattern:
    pattern_id: str
    pattern_name: str
    confidence: float
    trigger_conditions: List[str]
    success_indicators: List[str]
    learning_opportunities: List[str]
    timestamp: datetime

@dataclass
class LearningTrigger:
    trigger_id: str
    trigger_type: str
    conditions: Dict[str, Any]
    action: Callable
    priority: int
    last_fired: Optional[datetime] = None

class PatternRecognitionEngine:
    """Engine for recognizing coordination patterns and triggering learning."""
    
    def __init__(self, memory_db_path: str = ".swarm/memory.db"):
        self.memory_db_path = memory_db_path
        self.logger = self._setup_logging()
        self.pattern_templates = {}
        self.learning_triggers = {}
        self.pattern_history = deque(maxlen=1000)
        self.recognition_thread = None
        self.is_running = False
        self._initialize_pattern_templates()
        self._initialize_learning_triggers()
        
    def _setup_logging(self) -> logging.Logger:
        """Setup logging for pattern recognition."""
        logger = logging.getLogger('pattern_recognition')
        logger.setLevel(logging.INFO)
        
        if not logger.handlers:
            handler = logging.StreamHandler()
            formatter = logging.Formatter(
                '[%(asctime)s] %(levelname)s [%(name)s] %(message)s'
            )
            handler.setFormatter(formatter)
            logger.addHandler(handler)
            
        return logger
        
    def _initialize_pattern_templates(self) -> None:
        """Initialize pattern recognition templates."""
        
        # Successful coordination patterns
        self.pattern_templates = {
            "rapid_task_completion": {
                "conditions": ["task_started", "task_completed"],
                "time_threshold": 60,  # seconds
                "success_indicators": ["status=completed", "duration<60"],
                "confidence_factors": ["agent_count>1", "no_errors"],
                "learning_value": "high"
            },
            
            "efficient_swarm_coordination": {
                "conditions": ["multiple_agents", "coordinated_actions"],
                "success_indicators": ["low_conflict", "high_throughput"],
                "confidence_factors": ["parallel_execution", "resource_sharing"],
                "learning_value": "high"
            },
            
            "adaptive_algorithm_selection": {
                "conditions": ["algorithm_change", "performance_improvement"],
                "success_indicators": ["better_results", "faster_execution"],
                "confidence_factors": ["context_awareness", "optimization_metrics"],
                "learning_value": "medium"
            },
            
            "error_recovery_pattern": {
                "conditions": ["error_detected", "recovery_initiated", "success_achieved"],
                "success_indicators": ["error_resolved", "task_continued"],
                "confidence_factors": ["automatic_recovery", "minimal_delay"],
                "learning_value": "high"
            },
            
            "scalable_performance": {
                "conditions": ["agent_count_increase", "performance_maintained"],
                "success_indicators": ["linear_scaling", "no_bottlenecks"],
                "confidence_factors": ["resource_efficiency", "load_distribution"],
                "learning_value": "medium"
            },
            
            "emergent_behavior": {
                "conditions": ["unexpected_coordination", "beneficial_outcome"],
                "success_indicators": ["novel_solution", "improved_efficiency"],
                "confidence_factors": ["self_organization", "adaptation"],
                "learning_value": "very_high"
            },
            
            "cross_agent_learning": {
                "conditions": ["knowledge_sharing", "behavior_propagation"],
                "success_indicators": ["collective_improvement", "knowledge_retention"],
                "confidence_factors": ["peer_learning", "skill_transfer"],
                "learning_value": "high"
            },
            
            "resource_optimization": {
                "conditions": ["resource_constraints", "efficient_allocation"],
                "success_indicators": ["minimal_waste", "maximum_utilization"],
                "confidence_factors": ["dynamic_allocation", "predictive_management"],
                "learning_value": "medium"
            },
            
            "hierarchical_coordination": {
                "conditions": ["multi_level_structure", "effective_delegation"],
                "success_indicators": ["clear_hierarchy", "efficient_communication"],
                "confidence_factors": ["leadership_emergence", "role_specialization"],
                "learning_value": "high"
            },
            
            "real_time_adaptation": {
                "conditions": ["environmental_change", "rapid_response"],
                "success_indicators": ["quick_adaptation", "maintained_performance"],
                "confidence_factors": ["sensor_integration", "decision_speed"],
                "learning_value": "high"
            }
        }
        
        self.logger.info(f"Initialized {len(self.pattern_templates)} pattern recognition templates")
        
    def _initialize_learning_triggers(self) -> None:
        """Initialize learning triggers for continuous improvement."""
        
        self.learning_triggers = {
            "success_pattern_detected": LearningTrigger(
                trigger_id="success_pattern",
                trigger_type="pattern_recognition",
                conditions={"confidence_threshold": 0.8, "learning_value": ["high", "very_high"]},
                action=self._trigger_reinforcement_learning,
                priority=1
            ),
            
            "failure_pattern_detected": LearningTrigger(
                trigger_id="failure_pattern",
                trigger_type="pattern_recognition", 
                conditions={"success_score": {"<": 3.0}, "frequency": {">=": 3}},
                action=self._trigger_corrective_learning,
                priority=2
            ),
            
            "performance_degradation": LearningTrigger(
                trigger_id="performance_drop",
                trigger_type="performance_monitoring",
                conditions={"performance_trend": "declining", "duration": {">=": 300}},
                action=self._trigger_adaptive_learning,
                priority=1
            ),
            
            "new_coordination_pattern": LearningTrigger(
                trigger_id="novel_pattern",
                trigger_type="novelty_detection",
                conditions={"pattern_novelty": {">=": 0.9}, "success_potential": {">=": 0.7}},
                action=self._trigger_exploratory_learning,
                priority=3
            ),
            
            "agent_interaction_optimization": LearningTrigger(
                trigger_id="interaction_optimization",
                trigger_type="interaction_analysis",
                conditions={"interaction_efficiency": {"<": 0.6}, "agent_count": {">=": 3}},
                action=self._trigger_social_learning,
                priority=2
            ),
            
            "resource_utilization_improvement": LearningTrigger(
                trigger_id="resource_optimization",
                trigger_type="resource_monitoring",
                conditions={"resource_waste": {">=": 0.2}, "optimization_potential": {">=": 0.3}},
                action=self._trigger_efficiency_learning,
                priority=2
            ),
            
            "algorithm_performance_comparison": LearningTrigger(
                trigger_id="algorithm_comparison",
                trigger_type="comparative_analysis",
                conditions={"algorithm_count": {">=": 2}, "performance_variance": {">=": 0.1}},
                action=self._trigger_comparative_learning,
                priority=3
            ),
            
            "emergent_behavior_opportunity": LearningTrigger(
                trigger_id="emergent_opportunity",
                trigger_type="emergence_detection",
                conditions={"emergence_potential": {">=": 0.8}, "complexity_threshold": {">=": 0.6}},
                action=self._trigger_emergence_learning,
                priority=1
            )
        }
        
        self.logger.info(f"Initialized {len(self.learning_triggers)} learning triggers")
        
    def start_continuous_recognition(self, poll_interval: int = 30) -> None:
        """Start continuous pattern recognition in background thread."""
        if self.is_running:
            self.logger.warning("Pattern recognition is already running")
            return
            
        self.is_running = True
        self.recognition_thread = threading.Thread(
            target=self._recognition_loop,
            args=(poll_interval,),
            daemon=True
        )
        self.recognition_thread.start()
        self.logger.info(f"Started continuous pattern recognition with {poll_interval}s interval")
        
    def stop_continuous_recognition(self) -> None:
        """Stop continuous pattern recognition."""
        self.is_running = False
        if self.recognition_thread:
            self.recognition_thread.join(timeout=5.0)
        self.logger.info("Stopped continuous pattern recognition")
        
    def _recognition_loop(self, poll_interval: int) -> None:
        """Main recognition loop running in background thread."""
        while self.is_running:
            try:
                # Get recent data for pattern recognition
                recent_data = self._get_recent_coordination_data()
                
                # Recognize patterns
                recognized = self.recognize_patterns(recent_data)
                
                # Trigger learning based on recognized patterns
                for pattern in recognized:
                    self._evaluate_learning_triggers(pattern)
                    
                # Sleep until next poll
                time.sleep(poll_interval)
                
            except Exception as e:
                self.logger.error(f"Error in recognition loop: {e}")
                time.sleep(poll_interval)
                
    def _get_recent_coordination_data(self, lookback_minutes: int = 5) -> List[Dict[str, Any]]:
        """Get recent coordination data from memory database."""
        recent_data = []
        cutoff_time = int((datetime.now() - timedelta(minutes=lookback_minutes)).timestamp())
        
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                cursor.execute("""
                    SELECT key, value, namespace, created_at 
                    FROM memory_entries 
                    WHERE created_at >= ? 
                    ORDER BY created_at DESC
                """, (cutoff_time,))
                
                for row in cursor.fetchall():
                    key, value_str, namespace, timestamp = row
                    
                    try:
                        value = json.loads(value_str)
                        recent_data.append({
                            "key": key,
                            "value": value,
                            "namespace": namespace,
                            "timestamp": timestamp
                        })
                    except json.JSONDecodeError:
                        continue
                        
        except sqlite3.Error as e:
            self.logger.error(f"Error getting recent data: {e}")
            
        return recent_data
        
    def recognize_patterns(self, data: List[Dict[str, Any]]) -> List[RecognizedPattern]:
        """Recognize coordination patterns in the given data."""
        recognized_patterns = []
        
        # Group data by time windows for temporal pattern recognition
        time_windows = self._create_time_windows(data, window_size=60)  # 60-second windows
        
        for window_data in time_windows:
            for pattern_name, template in self.pattern_templates.items():
                pattern = self._match_pattern_template(pattern_name, template, window_data)
                if pattern:
                    recognized_patterns.append(pattern)
                    self.pattern_history.append(pattern)
                    
        self.logger.info(f"Recognized {len(recognized_patterns)} coordination patterns")
        return recognized_patterns
        
    def _create_time_windows(self, data: List[Dict[str, Any]], window_size: int) -> List[List[Dict[str, Any]]]:
        """Create time windows from data for temporal analysis."""
        if not data:
            return []
            
        # Sort data by timestamp
        sorted_data = sorted(data, key=lambda x: x["timestamp"])
        
        windows = []
        current_window = []
        window_start = sorted_data[0]["timestamp"]
        
        for entry in sorted_data:
            if entry["timestamp"] - window_start <= window_size:
                current_window.append(entry)
            else:
                if current_window:
                    windows.append(current_window)
                current_window = [entry]
                window_start = entry["timestamp"]
                
        if current_window:
            windows.append(current_window)
            
        return windows
        
    def _match_pattern_template(self, pattern_name: str, template: Dict[str, Any], 
                              window_data: List[Dict[str, Any]]) -> Optional[RecognizedPattern]:
        """Match data against a pattern template."""
        
        # Extract conditions that need to be met
        required_conditions = template.get("conditions", [])
        success_indicators = template.get("success_indicators", [])
        confidence_factors = template.get("confidence_factors", [])
        
        # Check if required conditions are present
        found_conditions = []
        for condition in required_conditions:
            if self._check_condition_in_data(condition, window_data):
                found_conditions.append(condition)
                
        # Calculate confidence based on found conditions and success indicators
        condition_ratio = len(found_conditions) / max(len(required_conditions), 1)
        
        if condition_ratio < 0.6:  # Need at least 60% of conditions
            return None
            
        # Check success indicators
        found_indicators = []
        for indicator in success_indicators:
            if self._check_condition_in_data(indicator, window_data):
                found_indicators.append(indicator)
                
        # Check confidence factors
        found_factors = []
        for factor in confidence_factors:
            if self._check_condition_in_data(factor, window_data):
                found_factors.append(factor)
                
        # Calculate overall confidence
        indicator_ratio = len(found_indicators) / max(len(success_indicators), 1)
        factor_ratio = len(found_factors) / max(len(confidence_factors), 1)
        
        confidence = (condition_ratio * 0.5 + indicator_ratio * 0.3 + factor_ratio * 0.2)
        
        if confidence >= 0.6:  # Minimum confidence threshold
            return RecognizedPattern(
                pattern_id=f"{pattern_name}_{int(time.time())}",
                pattern_name=pattern_name,
                confidence=confidence,
                trigger_conditions=found_conditions,
                success_indicators=found_indicators,
                learning_opportunities=self._identify_learning_opportunities(pattern_name, window_data),
                timestamp=datetime.now()
            )
            
        return None
        
    def _check_condition_in_data(self, condition: str, data: List[Dict[str, Any]]) -> bool:
        """Check if a condition is present in the data."""
        condition_lower = condition.lower()
        
        for entry in data:
            # Check in key
            if condition_lower in entry["key"].lower():
                return True
                
            # Check in value
            value_str = json.dumps(entry["value"]).lower()
            if condition_lower in value_str:
                return True
                
            # Special condition checks
            if condition_lower == "task_started" and "started" in value_str:
                return True
            elif condition_lower == "task_completed" and "completed" in value_str:
                return True
            elif condition_lower == "multiple_agents" and "agent" in value_str:
                return True
            elif condition_lower == "no_errors" and "error" not in value_str:
                return True
                
        return False
        
    def _identify_learning_opportunities(self, pattern_name: str, data: List[Dict[str, Any]]) -> List[str]:
        """Identify learning opportunities from recognized patterns."""
        opportunities = []
        
        # Pattern-specific learning opportunities
        pattern_opportunities = {
            "rapid_task_completion": [
                "optimization_parameter_tuning",
                "parallel_execution_improvement",
                "resource_allocation_optimization"
            ],
            "efficient_swarm_coordination": [
                "coordination_protocol_enhancement",
                "communication_pattern_learning",
                "conflict_resolution_improvement"
            ],
            "adaptive_algorithm_selection": [
                "context_recognition_training",
                "algorithm_performance_modeling",
                "dynamic_selection_rules"
            ],
            "error_recovery_pattern": [
                "error_prediction_modeling",
                "recovery_strategy_optimization",
                "resilience_mechanism_improvement"
            ],
            "emergent_behavior": [
                "emergence_pattern_recognition",
                "complexity_threshold_learning",
                "self_organization_enhancement"
            ]
        }
        
        opportunities.extend(pattern_opportunities.get(pattern_name, ["general_improvement"]))
        
        # Add data-specific opportunities
        if any("performance" in str(entry["value"]).lower() for entry in data):
            opportunities.append("performance_optimization")
            
        if any("agent" in str(entry["value"]).lower() for entry in data):
            opportunities.append("agent_behavior_refinement")
            
        return opportunities
        
    def _evaluate_learning_triggers(self, pattern: RecognizedPattern) -> None:
        """Evaluate learning triggers based on recognized pattern."""
        
        for trigger_id, trigger in self.learning_triggers.items():
            if self._should_fire_trigger(trigger, pattern):
                try:
                    # Execute trigger action
                    trigger.action(pattern)
                    trigger.last_fired = datetime.now()
                    
                    # Log trigger execution
                    self.logger.info(f"Fired learning trigger: {trigger_id} for pattern: {pattern.pattern_name}")
                    
                    # Store trigger execution in memory
                    self._store_trigger_execution(trigger_id, pattern)
                    
                except Exception as e:
                    self.logger.error(f"Error executing learning trigger {trigger_id}: {e}")
                    
    def _should_fire_trigger(self, trigger: LearningTrigger, pattern: RecognizedPattern) -> bool:
        """Check if a trigger should be fired for a pattern."""
        
        # Check cooldown period
        if trigger.last_fired:
            cooldown_minutes = 5  # Minimum 5 minutes between same trigger fires
            if (datetime.now() - trigger.last_fired).total_seconds() < cooldown_minutes * 60:
                return False
                
        # Check trigger-specific conditions
        conditions = trigger.conditions
        
        if "confidence_threshold" in conditions:
            if pattern.confidence < conditions["confidence_threshold"]:
                return False
                
        if "learning_value" in conditions:
            # This would need to be extracted from pattern template
            pattern_template = self.pattern_templates.get(pattern.pattern_name, {})
            pattern_learning_value = pattern_template.get("learning_value", "low")
            if pattern_learning_value not in conditions["learning_value"]:
                return False
                
        return True
        
    def _store_trigger_execution(self, trigger_id: str, pattern: RecognizedPattern) -> None:
        """Store trigger execution in memory for tracking."""
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Store in memory_entries table
                key = f"learning_trigger/{trigger_id}"
                value = {
                    "trigger_id": trigger_id,
                    "pattern_id": pattern.pattern_id,
                    "pattern_name": pattern.pattern_name,
                    "confidence": pattern.confidence,
                    "timestamp": pattern.timestamp.isoformat(),
                    "learning_opportunities": pattern.learning_opportunities
                }
                
                cursor.execute("""
                    INSERT INTO memory_entries (key, value, namespace) 
                    VALUES (?, ?, ?)
                """, (key, json.dumps(value), "neural_learning"))
                
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error storing trigger execution: {e}")
            
    # Learning trigger action methods
    def _trigger_reinforcement_learning(self, pattern: RecognizedPattern) -> None:
        """Trigger reinforcement learning for successful patterns."""
        self.logger.info(f"Reinforcement learning triggered for pattern: {pattern.pattern_name}")
        # Implementation would enhance successful coordination behaviors
        
    def _trigger_corrective_learning(self, pattern: RecognizedPattern) -> None:
        """Trigger corrective learning for failure patterns."""
        self.logger.info(f"Corrective learning triggered for pattern: {pattern.pattern_name}")
        # Implementation would address coordination failures
        
    def _trigger_adaptive_learning(self, pattern: RecognizedPattern) -> None:
        """Trigger adaptive learning for performance issues."""
        self.logger.info(f"Adaptive learning triggered for pattern: {pattern.pattern_name}")
        # Implementation would adapt to changing conditions
        
    def _trigger_exploratory_learning(self, pattern: RecognizedPattern) -> None:
        """Trigger exploratory learning for novel patterns."""
        self.logger.info(f"Exploratory learning triggered for pattern: {pattern.pattern_name}")
        # Implementation would explore new coordination strategies
        
    def _trigger_social_learning(self, pattern: RecognizedPattern) -> None:
        """Trigger social learning for agent interactions."""
        self.logger.info(f"Social learning triggered for pattern: {pattern.pattern_name}")
        # Implementation would improve agent interaction patterns
        
    def _trigger_efficiency_learning(self, pattern: RecognizedPattern) -> None:
        """Trigger efficiency learning for resource optimization."""
        self.logger.info(f"Efficiency learning triggered for pattern: {pattern.pattern_name}")
        # Implementation would optimize resource utilization
        
    def _trigger_comparative_learning(self, pattern: RecognizedPattern) -> None:
        """Trigger comparative learning for algorithm performance."""
        self.logger.info(f"Comparative learning triggered for pattern: {pattern.pattern_name}")
        # Implementation would compare and select best algorithms
        
    def _trigger_emergence_learning(self, pattern: RecognizedPattern) -> None:
        """Trigger emergence learning for emergent behaviors."""
        self.logger.info(f"Emergence learning triggered for pattern: {pattern.pattern_name}")
        # Implementation would enhance emergent coordination capabilities
        
    def get_recognition_statistics(self) -> Dict[str, Any]:
        """Get statistics about pattern recognition performance."""
        stats = {
            "total_patterns_recognized": len(self.pattern_history),
            "pattern_types": defaultdict(int),
            "average_confidence": 0.0,
            "learning_triggers_fired": defaultdict(int),
            "recent_patterns": []
        }
        
        if self.pattern_history:
            # Count pattern types
            for pattern in self.pattern_history:
                stats["pattern_types"][pattern.pattern_name] += 1
                
            # Calculate average confidence
            stats["average_confidence"] = np.mean([p.confidence for p in self.pattern_history])
            
            # Get recent patterns (last 10)
            stats["recent_patterns"] = [
                {
                    "name": p.pattern_name,
                    "confidence": p.confidence,
                    "timestamp": p.timestamp.isoformat()
                }
                for p in list(self.pattern_history)[-10:]
            ]
            
        return stats

def main():
    """Main function for pattern recognition engine."""
    engine = PatternRecognitionEngine()
    
    # Start continuous recognition
    engine.start_continuous_recognition(poll_interval=10)
    
    try:
        # Run for a while to collect patterns
        print("Pattern recognition engine started. Press Ctrl+C to stop.")
        while True:
            time.sleep(30)
            stats = engine.get_recognition_statistics()
            print(f"Recognized {stats['total_patterns_recognized']} patterns, "
                  f"avg confidence: {stats['average_confidence']:.2f}")
    except KeyboardInterrupt:
        print("Stopping pattern recognition engine...")
        engine.stop_continuous_recognition()

if __name__ == "__main__":
    main()