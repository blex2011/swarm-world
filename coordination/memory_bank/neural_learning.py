#!/usr/bin/env python3
"""
Neural Pattern Learning System
Learns from coordination outcomes to improve future swarm performance.
"""

import json
import math
import hashlib
import statistics
from typing import Dict, List, Any, Optional, Tuple
from datetime import datetime, timedelta
from dataclasses import dataclass, asdict
from enum import Enum
from persistence_manager import MemoryPersistenceManager


class OutcomeType(Enum):
    """Types of coordination outcomes."""
    SUCCESS = "success"
    FAILURE = "failure"
    PARTIAL_SUCCESS = "partial_success"
    TIMEOUT = "timeout"
    CONFLICT = "conflict"


class PatternType(Enum):
    """Types of coordination patterns."""
    COMMUNICATION = "communication"
    RESOURCE_ALLOCATION = "resource_allocation"
    TASK_DISTRIBUTION = "task_distribution"
    CONFLICT_RESOLUTION = "conflict_resolution"
    OPTIMIZATION = "optimization"
    DECISION_MAKING = "decision_making"


@dataclass
class CoordinationOutcome:
    """Record of a coordination outcome."""
    outcome_id: str
    swarm_id: str
    task_type: str
    agents_involved: List[str]
    outcome_type: OutcomeType
    success_score: float  # 0.0 to 1.0
    execution_time: float  # seconds
    resource_usage: Dict[str, float]
    context: Dict[str, Any]
    timestamp: datetime
    metadata: Dict[str, Any] = None


@dataclass
class CoordinationPattern:
    """Learned pattern from coordination outcomes."""
    pattern_id: str
    pattern_type: PatternType
    pattern_description: str
    conditions: Dict[str, Any]  # When this pattern applies
    actions: List[Dict[str, Any]]  # What actions to take
    success_rate: float
    confidence_score: float
    usage_count: int
    last_used: datetime
    learned_from: List[str]  # Outcome IDs this was learned from
    effectiveness_metrics: Dict[str, float]


@dataclass
class LearningInsight:
    """Insight derived from pattern analysis."""
    insight_id: str
    insight_type: str
    description: str
    evidence: List[str]  # Pattern or outcome IDs supporting this insight
    confidence: float
    impact_assessment: str
    recommended_actions: List[str]
    discovered_at: datetime


class NeuralPatternLearner:
    """Learns patterns from coordination outcomes to improve future performance."""
    
    def __init__(self, config_path: str = None):
        self.memory_manager = MemoryPersistenceManager(config_path)
        self.config = self.memory_manager.config.get("neural_learning", {})
        
        # Learning parameters
        self.min_sample_size = self.config.get("min_sample_size", 5)
        self.success_threshold = self.config.get("success_threshold", 0.8)
        self.confidence_threshold = self.config.get("confidence_threshold", 0.7)
        self.pattern_similarity_threshold = 0.85
        
        # Pattern storage
        self.patterns: Dict[str, CoordinationPattern] = {}
        self.outcomes: List[CoordinationOutcome] = []
        self.insights: List[LearningInsight] = []
        
        # Load existing patterns and outcomes
        self._load_existing_data()
    
    def record_coordination_outcome(self, outcome: CoordinationOutcome) -> bool:
        """Record a coordination outcome for learning."""
        try:
            # Store the outcome
            outcome_key = f"neural/outcomes/{outcome.outcome_id}"
            outcome_data = asdict(outcome)
            outcome_data["timestamp"] = outcome.timestamp.isoformat()
            outcome_data["outcome_type"] = outcome.outcome_type.value
            
            success = self.memory_manager.store_memory(outcome_key, outcome_data)
            
            if success:
                self.outcomes.append(outcome)
                
                # Trigger pattern learning
                self._analyze_outcome_for_patterns(outcome)
                
                # Update existing pattern effectiveness
                self._update_pattern_effectiveness(outcome)
                
                # Generate new insights
                self._generate_insights_from_outcome(outcome)
            
            return success
            
        except Exception as e:
            print(f"Error recording coordination outcome: {e}")
            return False
    
    def suggest_coordination_strategy(self, context: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        """Suggest coordination strategy based on learned patterns."""
        try:
            # Find matching patterns
            matching_patterns = self._find_matching_patterns(context)
            
            if not matching_patterns:
                return None
            
            # Rank patterns by effectiveness and confidence
            ranked_patterns = sorted(
                matching_patterns,
                key=lambda p: p.success_rate * p.confidence_score,
                reverse=True
            )
            
            best_pattern = ranked_patterns[0]
            
            # Generate strategy recommendation
            strategy = {
                "recommended_pattern": best_pattern.pattern_id,
                "pattern_type": best_pattern.pattern_type.value,
                "description": best_pattern.pattern_description,
                "actions": best_pattern.actions,
                "confidence": best_pattern.confidence_score,
                "expected_success_rate": best_pattern.success_rate,
                "usage_history": best_pattern.usage_count,
                "effectiveness_metrics": best_pattern.effectiveness_metrics,
                "alternative_patterns": [
                    {
                        "pattern_id": p.pattern_id,
                        "success_rate": p.success_rate,
                        "confidence": p.confidence_score
                    }
                    for p in ranked_patterns[1:3]  # Top 2 alternatives
                ]
            }
            
            # Update pattern usage
            best_pattern.usage_count += 1
            best_pattern.last_used = datetime.now()
            self._store_pattern(best_pattern)
            
            return strategy
            
        except Exception as e:
            print(f"Error suggesting coordination strategy: {e}")
            return None
    
    def analyze_coordination_trends(self, days: int = 30) -> Dict[str, Any]:
        """Analyze coordination trends and performance over time."""
        try:
            cutoff_date = datetime.now() - timedelta(days=days)
            recent_outcomes = [
                outcome for outcome in self.outcomes
                if outcome.timestamp >= cutoff_date
            ]
            
            if not recent_outcomes:
                return {"error": "No recent outcomes available"}
            
            analysis = {
                "analysis_period": f"Last {days} days",
                "total_outcomes": len(recent_outcomes),
                "success_metrics": self._calculate_success_metrics(recent_outcomes),
                "performance_trends": self._calculate_performance_trends(recent_outcomes),
                "pattern_effectiveness": self._analyze_pattern_effectiveness(recent_outcomes),
                "resource_utilization": self._analyze_resource_utilization(recent_outcomes),
                "agent_performance": self._analyze_agent_performance(recent_outcomes),
                "optimization_opportunities": self._identify_optimization_opportunities(recent_outcomes),
                "recommendations": self._generate_performance_recommendations(recent_outcomes)
            }
            
            return analysis
            
        except Exception as e:
            print(f"Error analyzing coordination trends: {e}")
            return {"error": str(e)}
    
    def get_learning_insights(self, insight_type: str = None) -> List[Dict[str, Any]]:
        """Get learning insights, optionally filtered by type."""
        try:
            filtered_insights = self.insights
            
            if insight_type:
                filtered_insights = [
                    insight for insight in self.insights
                    if insight.insight_type == insight_type
                ]
            
            # Sort by confidence and recency
            sorted_insights = sorted(
                filtered_insights,
                key=lambda i: (i.confidence, i.discovered_at),
                reverse=True
            )
            
            return [asdict(insight) for insight in sorted_insights]
            
        except Exception as e:
            print(f"Error getting learning insights: {e}")
            return []
    
    def optimize_pattern_library(self) -> Dict[str, Any]:
        """Optimize the pattern library by merging similar patterns and removing ineffective ones."""
        optimization_report = {
            "patterns_before": len(self.patterns),
            "patterns_merged": 0,
            "patterns_removed": 0,
            "patterns_after": 0,
            "optimizations_applied": []
        }
        
        try:
            # Remove ineffective patterns
            ineffective_patterns = [
                pattern_id for pattern_id, pattern in self.patterns.items()
                if pattern.success_rate < 0.3 and pattern.usage_count > 10
            ]
            
            for pattern_id in ineffective_patterns:
                del self.patterns[pattern_id]
                self._remove_pattern_from_storage(pattern_id)
                optimization_report["patterns_removed"] += 1
            
            optimization_report["optimizations_applied"].append(
                f"Removed {len(ineffective_patterns)} ineffective patterns"
            )
            
            # Merge similar patterns
            merged_count = self._merge_similar_patterns()
            optimization_report["patterns_merged"] = merged_count
            
            if merged_count > 0:
                optimization_report["optimizations_applied"].append(
                    f"Merged {merged_count} similar patterns"
                )
            
            # Update pattern effectiveness scores
            self._recalculate_pattern_effectiveness()
            optimization_report["optimizations_applied"].append("Recalculated pattern effectiveness")
            
            optimization_report["patterns_after"] = len(self.patterns)
            
        except Exception as e:
            optimization_report["error"] = str(e)
        
        return optimization_report
    
    def _load_existing_data(self):
        """Load existing patterns, outcomes, and insights from storage."""
        try:
            # Load patterns
            pattern_keys = self._get_pattern_keys()
            for key in pattern_keys:
                pattern_data = self.memory_manager.retrieve_memory(key)
                if pattern_data:
                    pattern = self._deserialize_pattern(pattern_data)
                    self.patterns[pattern.pattern_id] = pattern
            
            # Load recent outcomes (last 30 days)
            outcome_keys = self._get_recent_outcome_keys(30)
            for key in outcome_keys:
                outcome_data = self.memory_manager.retrieve_memory(key)
                if outcome_data:
                    outcome = self._deserialize_outcome(outcome_data)
                    self.outcomes.append(outcome)
            
            # Load insights
            insight_keys = self._get_insight_keys()
            for key in insight_keys:
                insight_data = self.memory_manager.retrieve_memory(key)
                if insight_data:
                    insight = self._deserialize_insight(insight_data)
                    self.insights.append(insight)
        
        except Exception as e:
            print(f"Error loading existing data: {e}")
    
    def _analyze_outcome_for_patterns(self, outcome: CoordinationOutcome):
        """Analyze a new outcome to extract patterns."""
        try:
            # Look for existing similar outcomes
            similar_outcomes = self._find_similar_outcomes(outcome)
            
            if len(similar_outcomes) >= self.min_sample_size:
                # Check if this represents a pattern
                pattern = self._extract_pattern_from_outcomes([outcome] + similar_outcomes)
                
                if pattern and pattern.confidence_score >= self.confidence_threshold:
                    # Store the pattern
                    self.patterns[pattern.pattern_id] = pattern
                    self._store_pattern(pattern)
        
        except Exception as e:
            print(f"Error analyzing outcome for patterns: {e}")
    
    def _extract_pattern_from_outcomes(self, outcomes: List[CoordinationOutcome]) -> Optional[CoordinationPattern]:
        """Extract a coordination pattern from a group of similar outcomes."""
        if not outcomes:
            return None
        
        try:
            # Determine pattern type based on context
            pattern_type = self._determine_pattern_type(outcomes)
            
            # Calculate success rate
            success_count = sum(1 for o in outcomes if o.outcome_type == OutcomeType.SUCCESS)
            success_rate = success_count / len(outcomes)
            
            # Extract common conditions
            conditions = self._extract_common_conditions(outcomes)
            
            # Extract common actions
            actions = self._extract_common_actions(outcomes)
            
            # Calculate confidence based on consistency
            confidence = self._calculate_pattern_confidence(outcomes, conditions, actions)
            
            # Generate pattern ID
            pattern_id = self._generate_pattern_id(pattern_type, conditions)
            
            # Create pattern
            pattern = CoordinationPattern(
                pattern_id=pattern_id,
                pattern_type=pattern_type,
                pattern_description=self._generate_pattern_description(pattern_type, conditions, actions),
                conditions=conditions,
                actions=actions,
                success_rate=success_rate,
                confidence_score=confidence,
                usage_count=0,
                last_used=datetime.now(),
                learned_from=[o.outcome_id for o in outcomes],
                effectiveness_metrics=self._calculate_effectiveness_metrics(outcomes)
            )
            
            return pattern
            
        except Exception as e:
            print(f"Error extracting pattern from outcomes: {e}")
            return None
    
    def _find_matching_patterns(self, context: Dict[str, Any]) -> List[CoordinationPattern]:
        """Find patterns that match the given context."""
        matching_patterns = []
        
        for pattern in self.patterns.values():
            if self._pattern_matches_context(pattern, context):
                matching_patterns.append(pattern)
        
        return matching_patterns
    
    def _pattern_matches_context(self, pattern: CoordinationPattern, context: Dict[str, Any]) -> bool:
        """Check if a pattern matches the given context."""
        try:
            # Check each condition in the pattern
            for condition_key, condition_value in pattern.conditions.items():
                if condition_key not in context:
                    return False
                
                # Handle different types of condition matching
                if isinstance(condition_value, dict):
                    if "range" in condition_value:
                        min_val, max_val = condition_value["range"]
                        if not (min_val <= context[condition_key] <= max_val):
                            return False
                    elif "values" in condition_value:
                        if context[condition_key] not in condition_value["values"]:
                            return False
                else:
                    if context[condition_key] != condition_value:
                        return False
            
            return True
            
        except Exception as e:
            print(f"Error matching pattern to context: {e}")
            return False
    
    def _find_similar_outcomes(self, target_outcome: CoordinationOutcome) -> List[CoordinationOutcome]:
        """Find outcomes similar to the target outcome."""
        similar_outcomes = []
        
        for outcome in self.outcomes:
            if outcome.outcome_id == target_outcome.outcome_id:
                continue
            
            similarity = self._calculate_outcome_similarity(target_outcome, outcome)
            if similarity >= self.pattern_similarity_threshold:
                similar_outcomes.append(outcome)
        
        return similar_outcomes
    
    def _calculate_outcome_similarity(self, outcome1: CoordinationOutcome, outcome2: CoordinationOutcome) -> float:
        """Calculate similarity between two outcomes."""
        try:
            similarity_factors = []
            
            # Task type similarity
            if outcome1.task_type == outcome2.task_type:
                similarity_factors.append(1.0)
            else:
                similarity_factors.append(0.0)
            
            # Agent involvement similarity
            agents1 = set(outcome1.agents_involved)
            agents2 = set(outcome2.agents_involved)
            agent_similarity = len(agents1.intersection(agents2)) / len(agents1.union(agents2))
            similarity_factors.append(agent_similarity)
            
            # Context similarity
            context_similarity = self._calculate_context_similarity(outcome1.context, outcome2.context)
            similarity_factors.append(context_similarity)
            
            # Resource usage similarity
            resource_similarity = self._calculate_resource_similarity(outcome1.resource_usage, outcome2.resource_usage)
            similarity_factors.append(resource_similarity)
            
            # Calculate weighted average
            weights = [0.3, 0.2, 0.3, 0.2]  # Task type, agents, context, resources
            return sum(w * s for w, s in zip(weights, similarity_factors))
            
        except Exception as e:
            print(f"Error calculating outcome similarity: {e}")
            return 0.0
    
    def _calculate_context_similarity(self, context1: Dict[str, Any], context2: Dict[str, Any]) -> float:
        """Calculate similarity between two context dictionaries."""
        try:
            all_keys = set(context1.keys()).union(set(context2.keys()))
            if not all_keys:
                return 1.0
            
            matches = 0
            for key in all_keys:
                if key in context1 and key in context2:
                    if context1[key] == context2[key]:
                        matches += 1
            
            return matches / len(all_keys)
            
        except Exception as e:
            print(f"Error calculating context similarity: {e}")
            return 0.0
    
    def _calculate_resource_similarity(self, resources1: Dict[str, float], resources2: Dict[str, float]) -> float:
        """Calculate similarity between resource usage patterns."""
        try:
            all_resources = set(resources1.keys()).union(set(resources2.keys()))
            if not all_resources:
                return 1.0
            
            similarities = []
            for resource in all_resources:
                val1 = resources1.get(resource, 0)
                val2 = resources2.get(resource, 0)
                
                if val1 == 0 and val2 == 0:
                    similarities.append(1.0)
                else:
                    max_val = max(val1, val2)
                    if max_val > 0:
                        similarity = 1 - abs(val1 - val2) / max_val
                        similarities.append(similarity)
            
            return statistics.mean(similarities) if similarities else 0.0
            
        except Exception as e:
            print(f"Error calculating resource similarity: {e}")
            return 0.0
    
    def _determine_pattern_type(self, outcomes: List[CoordinationOutcome]) -> PatternType:
        """Determine the pattern type from a group of outcomes."""
        # This would use heuristics to determine pattern type
        # For now, return a default based on context
        if any("communication" in o.context for o in outcomes):
            return PatternType.COMMUNICATION
        elif any("resource" in o.context for o in outcomes):
            return PatternType.RESOURCE_ALLOCATION
        elif any("task" in o.context for o in outcomes):
            return PatternType.TASK_DISTRIBUTION
        else:
            return PatternType.OPTIMIZATION
    
    def _extract_common_conditions(self, outcomes: List[CoordinationOutcome]) -> Dict[str, Any]:
        """Extract common conditions from a group of outcomes."""
        conditions = {}
        
        # Extract common context elements
        common_context_keys = set(outcomes[0].context.keys())
        for outcome in outcomes[1:]:
            common_context_keys.intersection_update(outcome.context.keys())
        
        for key in common_context_keys:
            values = [outcome.context[key] for outcome in outcomes]
            if len(set(values)) == 1:  # All same value
                conditions[key] = values[0]
            elif all(isinstance(v, (int, float)) for v in values):  # Numeric range
                conditions[key] = {"range": [min(values), max(values)]}
            else:  # Multiple values
                conditions[key] = {"values": list(set(values))}
        
        return conditions
    
    def _extract_common_actions(self, outcomes: List[CoordinationOutcome]) -> List[Dict[str, Any]]:
        """Extract common actions from a group of outcomes."""
        # This would analyze the successful outcomes to extract actions
        # For now, return a simplified action based on the outcomes
        actions = []
        
        successful_outcomes = [o for o in outcomes if o.outcome_type == OutcomeType.SUCCESS]
        if successful_outcomes:
            # Extract action patterns from successful outcomes
            avg_execution_time = statistics.mean([o.execution_time for o in successful_outcomes])
            
            actions.append({
                "action_type": "coordination",
                "target_execution_time": avg_execution_time,
                "recommended_agents": list(set().union(*[o.agents_involved for o in successful_outcomes]))
            })
        
        return actions
    
    def _calculate_pattern_confidence(self, outcomes: List[CoordinationOutcome], 
                                    conditions: Dict[str, Any], actions: List[Dict[str, Any]]) -> float:
        """Calculate confidence score for a pattern."""
        try:
            # Base confidence on consistency of outcomes
            consistency_score = len(outcomes) / max(len(outcomes), 10)  # More outcomes = higher confidence
            
            # Factor in success rate
            success_count = sum(1 for o in outcomes if o.outcome_type == OutcomeType.SUCCESS)
            success_rate = success_count / len(outcomes)
            
            # Factor in condition specificity
            specificity_score = len(conditions) / max(len(conditions), 5)
            
            # Combine factors
            confidence = (consistency_score * 0.4 + success_rate * 0.4 + specificity_score * 0.2)
            
            return min(confidence, 1.0)
            
        except Exception as e:
            print(f"Error calculating pattern confidence: {e}")
            return 0.0
    
    def _generate_pattern_id(self, pattern_type: PatternType, conditions: Dict[str, Any]) -> str:
        """Generate a unique pattern ID."""
        # Create hash from pattern type and conditions
        content = f"{pattern_type.value}_{json.dumps(conditions, sort_keys=True)}"
        pattern_hash = hashlib.md5(content.encode()).hexdigest()[:8]
        return f"pattern_{pattern_type.value}_{pattern_hash}"
    
    def _generate_pattern_description(self, pattern_type: PatternType, 
                                    conditions: Dict[str, Any], actions: List[Dict[str, Any]]) -> str:
        """Generate a human-readable pattern description."""
        description_parts = [f"Pattern for {pattern_type.value}"]
        
        if conditions:
            condition_desc = ", ".join([f"{k}={v}" for k, v in list(conditions.items())[:3]])
            description_parts.append(f"when {condition_desc}")
        
        if actions:
            action_desc = f"involving {len(actions)} coordinated actions"
            description_parts.append(action_desc)
        
        return " ".join(description_parts)
    
    def _calculate_effectiveness_metrics(self, outcomes: List[CoordinationOutcome]) -> Dict[str, float]:
        """Calculate effectiveness metrics for a pattern."""
        metrics = {}
        
        if outcomes:
            # Success rate
            success_count = sum(1 for o in outcomes if o.outcome_type == OutcomeType.SUCCESS)
            metrics["success_rate"] = success_count / len(outcomes)
            
            # Average execution time
            metrics["avg_execution_time"] = statistics.mean([o.execution_time for o in outcomes])
            
            # Resource efficiency
            total_resource_usage = sum(sum(o.resource_usage.values()) for o in outcomes)
            metrics["avg_resource_usage"] = total_resource_usage / len(outcomes)
            
            # Consistency score
            success_scores = [o.success_score for o in outcomes]
            if len(success_scores) > 1:
                metrics["consistency"] = 1.0 - statistics.stdev(success_scores)
            else:
                metrics["consistency"] = 1.0
        
        return metrics
    
    def _store_pattern(self, pattern: CoordinationPattern):
        """Store a pattern in persistent memory."""
        try:
            pattern_key = f"neural/patterns/{pattern.pattern_id}"
            pattern_data = asdict(pattern)
            pattern_data["pattern_type"] = pattern.pattern_type.value
            pattern_data["last_used"] = pattern.last_used.isoformat()
            
            self.memory_manager.store_memory(pattern_key, pattern_data)
            
        except Exception as e:
            print(f"Error storing pattern: {e}")
    
    def _get_pattern_keys(self) -> List[str]:
        """Get all pattern keys from storage."""
        # This would query the database for pattern keys
        # For now, return empty list
        return []
    
    def _get_recent_outcome_keys(self, days: int) -> List[str]:
        """Get recent outcome keys from storage."""
        # This would query the database for recent outcome keys
        # For now, return empty list
        return []
    
    def _get_insight_keys(self) -> List[str]:
        """Get all insight keys from storage."""
        # This would query the database for insight keys
        # For now, return empty list
        return []
    
    def _deserialize_pattern(self, pattern_data: Dict[str, Any]) -> CoordinationPattern:
        """Deserialize pattern data into a CoordinationPattern object."""
        pattern_data["pattern_type"] = PatternType(pattern_data["pattern_type"])
        pattern_data["last_used"] = datetime.fromisoformat(pattern_data["last_used"])
        return CoordinationPattern(**pattern_data)
    
    def _deserialize_outcome(self, outcome_data: Dict[str, Any]) -> CoordinationOutcome:
        """Deserialize outcome data into a CoordinationOutcome object."""
        outcome_data["outcome_type"] = OutcomeType(outcome_data["outcome_type"])
        outcome_data["timestamp"] = datetime.fromisoformat(outcome_data["timestamp"])
        return CoordinationOutcome(**outcome_data)
    
    def _deserialize_insight(self, insight_data: Dict[str, Any]) -> LearningInsight:
        """Deserialize insight data into a LearningInsight object."""
        insight_data["discovered_at"] = datetime.fromisoformat(insight_data["discovered_at"])
        return LearningInsight(**insight_data)
    
    # Additional analysis methods would be implemented here...
    def _calculate_success_metrics(self, outcomes: List[CoordinationOutcome]) -> Dict[str, Any]:
        """Calculate success metrics from outcomes."""
        success_count = sum(1 for o in outcomes if o.outcome_type == OutcomeType.SUCCESS)
        return {
            "success_rate": success_count / len(outcomes) if outcomes else 0,
            "total_outcomes": len(outcomes),
            "successful_outcomes": success_count
        }
    
    def _calculate_performance_trends(self, outcomes: List[CoordinationOutcome]) -> Dict[str, Any]:
        """Calculate performance trends from outcomes."""
        return {"trend": "analysis_pending"}
    
    def _analyze_pattern_effectiveness(self, outcomes: List[CoordinationOutcome]) -> Dict[str, Any]:
        """Analyze pattern effectiveness."""
        return {"analysis": "pending"}
    
    def _analyze_resource_utilization(self, outcomes: List[CoordinationOutcome]) -> Dict[str, Any]:
        """Analyze resource utilization patterns."""
        return {"analysis": "pending"}
    
    def _analyze_agent_performance(self, outcomes: List[CoordinationOutcome]) -> Dict[str, Any]:
        """Analyze individual agent performance."""
        return {"analysis": "pending"}
    
    def _identify_optimization_opportunities(self, outcomes: List[CoordinationOutcome]) -> List[str]:
        """Identify optimization opportunities."""
        return []
    
    def _generate_performance_recommendations(self, outcomes: List[CoordinationOutcome]) -> List[str]:
        """Generate performance recommendations."""
        return []
    
    def _update_pattern_effectiveness(self, outcome: CoordinationOutcome):
        """Update pattern effectiveness based on new outcome."""
        pass
    
    def _generate_insights_from_outcome(self, outcome: CoordinationOutcome):
        """Generate insights from a new outcome."""
        pass
    
    def _merge_similar_patterns(self) -> int:
        """Merge similar patterns."""
        return 0
    
    def _remove_pattern_from_storage(self, pattern_id: str):
        """Remove pattern from storage."""
        pass
    
    def _recalculate_pattern_effectiveness(self):
        """Recalculate effectiveness for all patterns."""
        pass
    
    def close(self):
        """Close the learner and clean up resources."""
        self.memory_manager.close()


# Utility functions
def create_neural_learner(config_path: str = None) -> NeuralPatternLearner:
    """Create and initialize a neural pattern learner."""
    return NeuralPatternLearner(config_path)


def record_outcome(swarm_id: str, task_type: str, agents: List[str], 
                  outcome_type: OutcomeType, success_score: float,
                  execution_time: float, context: Dict[str, Any]) -> bool:
    """Record a coordination outcome for learning."""
    learner = NeuralPatternLearner()
    try:
        outcome = CoordinationOutcome(
            outcome_id=f"outcome_{int(datetime.now().timestamp())}",
            swarm_id=swarm_id,
            task_type=task_type,
            agents_involved=agents,
            outcome_type=outcome_type,
            success_score=success_score,
            execution_time=execution_time,
            resource_usage={},
            context=context,
            timestamp=datetime.now()
        )
        return learner.record_coordination_outcome(outcome)
    finally:
        learner.close()


if __name__ == "__main__":
    # Test the neural learning system
    learner = NeuralPatternLearner()
    
    # Test recording an outcome
    test_outcome = CoordinationOutcome(
        outcome_id="test_outcome_1",
        swarm_id="test_swarm",
        task_type="data_processing",
        agents_involved=["agent1", "agent2"],
        outcome_type=OutcomeType.SUCCESS,
        success_score=0.9,
        execution_time=45.2,
        resource_usage={"cpu": 0.7, "memory": 0.5},
        context={"task_complexity": "medium", "data_size": "large"},
        timestamp=datetime.now()
    )
    
    success = learner.record_coordination_outcome(test_outcome)
    print(f"Outcome recording: {'Success' if success else 'Failed'}")
    
    # Test strategy suggestion
    strategy = learner.suggest_coordination_strategy({
        "task_complexity": "medium",
        "data_size": "large"
    })
    print(f"Strategy suggestion: {strategy}")
    
    # Test trends analysis
    trends = learner.analyze_coordination_trends(7)
    print(f"Trends analysis: {trends}")
    
    learner.close()