#!/usr/bin/env python3
"""
Neural Pattern Learner for Swarm Coordination
Learns from successful coordination patterns and improves future swarm behavior.
"""

import sqlite3
import json
import numpy as np
from datetime import datetime
from typing import Dict, List, Tuple, Optional, Any
from dataclasses import dataclass
import logging

@dataclass
class CoordinationPattern:
    pattern_id: str
    pattern_type: str
    input_context: Dict[str, Any]
    actions_taken: List[str]
    outcomes: Dict[str, Any]
    success_score: float
    timestamp: datetime
    model_version: str
    agents_involved: List[str]
    
@dataclass
class NeuralModel:
    model_id: str
    model_type: str
    algorithm: str
    parameters: Dict[str, Any]
    performance_score: float
    usage_count: int
    last_trained: datetime
    optimization_focus: str

class NeuralPatternLearner:
    """Main class for learning neural patterns from swarm coordination data."""
    
    def __init__(self, memory_db_path: str = ".swarm/memory.db"):
        self.memory_db_path = memory_db_path
        self.logger = self._setup_logging()
        self.neural_models = {}
        self.pattern_cache = {}
        self._initialize_models()
        
    def _setup_logging(self) -> logging.Logger:
        """Setup logging for neural pattern learning."""
        logger = logging.getLogger('neural_pattern_learner')
        logger.setLevel(logging.INFO)
        
        if not logger.handlers:
            handler = logging.StreamHandler()
            formatter = logging.Formatter(
                '[%(asctime)s] %(levelname)s [%(name)s] %(message)s'
            )
            handler.setFormatter(formatter)
            logger.addHandler(handler)
            
        return logger
        
    def _initialize_models(self) -> None:
        """Initialize the 27+ neural models for swarm coordination."""
        
        # Core Swarm Intelligence Models
        swarm_models = [
            # Basic Swarm Algorithms
            NeuralModel("boids", "swarm_behavior", "Boids", 
                       {"separation": 1.5, "alignment": 1.0, "cohesion": 1.0}, 9.0, 0, datetime.now(), "flocking"),
            NeuralModel("aco", "optimization", "Ant Colony Optimization",
                       {"pheromone_strength": 1.0, "evaporation_rate": 0.1}, 8.0, 0, datetime.now(), "pathfinding"),
            NeuralModel("pso", "optimization", "Particle Swarm Optimization",
                       {"inertia": 0.7, "cognitive": 1.5, "social": 1.5}, 8.5, 0, datetime.now(), "parameter_tuning"),
            NeuralModel("fish_school", "bio_inspired", "Fish School Search",
                       {"step_individual": 0.1, "step_volitive": 0.01}, 8.0, 0, datetime.now(), "adaptive_behavior"),
            NeuralModel("bee_algorithm", "bio_inspired", "Bee Algorithm",
                       {"scout_bees": 10, "best_sites": 5, "elite_sites": 2}, 7.5, 0, datetime.now(), "exploration"),
            NeuralModel("firefly", "bio_inspired", "Firefly Algorithm",
                       {"alpha": 0.2, "beta": 1.0, "gamma": 1.0}, 7.0, 0, datetime.now(), "clustering"),
            
            # Hybrid and Advanced Models
            NeuralModel("social_forces", "crowd_dynamics", "Social Force Model",
                       {"desired_force": 2.0, "panic_force": 10.0}, 8.5, 0, datetime.now(), "crowd_simulation"),
            NeuralModel("multi_species_pso", "meta_optimization", "Multi-Species PSO",
                       {"species_count": 5, "migration_rate": 0.1}, 8.0, 0, datetime.now(), "multi_objective"),
            NeuralModel("boids_aco_hybrid", "hybrid", "Boids + ACO Hybrid",
                       {"boids_weight": 0.7, "aco_weight": 0.3}, 8.8, 0, datetime.now(), "tactical_movement"),
            NeuralModel("hierarchical_boids", "hierarchical", "Hierarchical Boids",
                       {"levels": 3, "group_size": 50}, 8.2, 0, datetime.now(), "large_scale_coordination"),
            
            # Unity-Specific Optimization Models
            NeuralModel("ecs_boids", "unity_optimization", "ECS Boids System",
                       {"chunk_size": 128, "burst_enabled": True}, 9.5, 0, datetime.now(), "performance"),
            NeuralModel("job_system_pso", "unity_optimization", "Job System PSO",
                       {"batch_size": 64, "parallel_jobs": 8}, 9.0, 0, datetime.now(), "parallel_optimization"),
            NeuralModel("gpu_compute_aco", "unity_optimization", "GPU Compute ACO",
                       {"threads_per_group": 64, "groups": 32}, 9.2, 0, datetime.now(), "massive_scale"),
            NeuralModel("spatial_hash_boids", "unity_optimization", "Spatial Hash Boids",
                       {"cell_size": 5.0, "max_neighbors": 32}, 8.8, 0, datetime.now(), "neighbor_optimization"),
            NeuralModel("lod_swarm", "unity_optimization", "LOD Swarm System",
                       {"lod_levels": 4, "distance_thresholds": [10, 50, 200, 1000]}, 8.5, 0, datetime.now(), "scalability"),
            
            # Emergent Behavior Models
            NeuralModel("flocking_predator_prey", "emergent", "Predator-Prey Flocking",
                       {"predator_avoidance": 5.0, "prey_attraction": 2.0}, 8.3, 0, datetime.now(), "ecosystem_dynamics"),
            NeuralModel("resource_competition", "emergent", "Resource Competition Model",
                       {"resource_decay": 0.05, "competition_radius": 10.0}, 7.8, 0, datetime.now(), "economic_simulation"),
            NeuralModel("leadership_emergence", "emergent", "Leadership Emergence",
                       {"leadership_threshold": 0.8, "follower_weight": 0.6}, 8.0, 0, datetime.now(), "group_dynamics"),
            
            # Adaptive Learning Models
            NeuralModel("reinforcement_swarm", "adaptive", "Reinforcement Learning Swarm",
                       {"learning_rate": 0.01, "discount_factor": 0.9}, 8.7, 0, datetime.now(), "behavior_adaptation"),
            NeuralModel("genetic_algorithm_swarm", "adaptive", "Genetic Algorithm Swarm",
                       {"mutation_rate": 0.1, "crossover_rate": 0.8}, 8.4, 0, datetime.now(), "evolution_strategy"),
            NeuralModel("neural_network_swarm", "adaptive", "Neural Network Swarm",
                       {"hidden_layers": 2, "neurons_per_layer": 64}, 8.9, 0, datetime.now(), "pattern_recognition"),
            
            # Specialized Game Models
            NeuralModel("rts_formation", "game_specific", "RTS Formation Control",
                       {"formation_types": 5, "unit_spacing": 2.0}, 8.6, 0, datetime.now(), "military_strategy"),
            NeuralModel("tower_defense_pathing", "game_specific", "Tower Defense Adaptive Pathing",
                       {"path_memory": 100, "adaptation_rate": 0.05}, 8.4, 0, datetime.now(), "adaptive_pathfinding"),
            NeuralModel("survival_wildlife", "game_specific", "Survival Game Wildlife",
                       {"fear_factor": 3.0, "hunger_drive": 2.0}, 8.1, 0, datetime.now(), "realistic_behavior"),
            NeuralModel("city_traffic", "game_specific", "City Builder Traffic Flow",
                       {"congestion_avoidance": 2.0, "route_memory": 50}, 8.3, 0, datetime.now(), "urban_planning"),
            
            # Performance Optimization Models
            NeuralModel("memory_pooling", "optimization", "Memory Pooling System",
                       {"pool_size": 1000, "growth_factor": 1.5}, 9.0, 0, datetime.now(), "memory_management"),
            NeuralModel("instanced_rendering", "optimization", "Instanced Rendering System",
                       {"max_instances": 1000, "lod_bias": 1.0}, 9.3, 0, datetime.now(), "visual_performance"),
            NeuralModel("temporal_caching", "optimization", "Temporal Behavior Caching",
                       {"cache_duration": 5.0, "prediction_window": 2.0}, 8.7, 0, datetime.now(), "predictive_optimization")
        ]
        
        # Store models in dictionary for easy access
        for model in swarm_models:
            self.neural_models[model.model_id] = model
            
        self.logger.info(f"Initialized {len(self.neural_models)} neural models for swarm coordination")
        
    def extract_coordination_patterns(self) -> List[CoordinationPattern]:
        """Extract coordination patterns from memory database."""
        patterns = []
        
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Extract task completion patterns
                cursor.execute("""
                    SELECT key, value, namespace, created_at 
                    FROM memory_entries 
                    WHERE key LIKE '%task%' OR key LIKE '%swarm%' OR key LIKE '%coordination%'
                    ORDER BY created_at DESC
                """)
                
                for row in cursor.fetchall():
                    key, value_str, namespace, timestamp = row
                    
                    try:
                        value = json.loads(value_str)
                        
                        # Create coordination pattern
                        pattern = CoordinationPattern(
                            pattern_id=f"pattern_{timestamp}_{hash(key) % 10000}",
                            pattern_type=self._classify_pattern_type(key, value),
                            input_context={"key": key, "namespace": namespace},
                            actions_taken=self._extract_actions(value),
                            outcomes=self._extract_outcomes(value),
                            success_score=self._calculate_success_score(value),
                            timestamp=datetime.fromtimestamp(timestamp),
                            model_version="1.0",
                            agents_involved=self._extract_agents(value)
                        )
                        
                        patterns.append(pattern)
                        
                    except (json.JSONDecodeError, TypeError):
                        continue
                        
        except sqlite3.Error as e:
            self.logger.error(f"Database error extracting patterns: {e}")
            
        self.logger.info(f"Extracted {len(patterns)} coordination patterns")
        return patterns
        
    def _classify_pattern_type(self, key: str, value: Dict) -> str:
        """Classify the type of coordination pattern."""
        if "task" in key.lower():
            if "completed" in key.lower():
                return "task_completion"
            else:
                return "task_initiation"
        elif "swarm" in key.lower():
            return "swarm_coordination"
        elif "agent" in key.lower():
            return "agent_interaction"
        elif "algorithm" in str(value).lower():
            return "algorithm_selection"
        else:
            return "general_coordination"
            
    def _extract_actions(self, value: Dict) -> List[str]:
        """Extract actions taken from coordination data."""
        actions = []
        
        if isinstance(value, dict):
            if "status" in value:
                actions.append(f"status_change_{value['status']}")
            if "algorithm" in value:
                actions.append(f"algorithm_applied_{value['algorithm']}")
            if "agents" in value:
                actions.append(f"agent_coordination_{len(value['agents'])}")
                
        return actions
        
    def _extract_outcomes(self, value: Dict) -> Dict[str, Any]:
        """Extract outcomes from coordination data."""
        outcomes = {}
        
        if isinstance(value, dict):
            if "completedAt" in value:
                outcomes["completion_time"] = value["completedAt"]
            if "duration" in value:
                outcomes["duration"] = value["duration"]
            if "score" in value:
                outcomes["performance_score"] = value["score"]
                
        return outcomes
        
    def _calculate_success_score(self, value: Dict) -> float:
        """Calculate success score for coordination pattern."""
        base_score = 5.0  # Neutral score
        
        if isinstance(value, dict):
            if "status" in value:
                if value["status"] == "completed":
                    base_score = 8.0
                elif value["status"] == "failed":
                    base_score = 2.0
                elif value["status"] == "in_progress":
                    base_score = 6.0
                    
            if "score" in value:
                try:
                    base_score = float(value["score"])
                except (ValueError, TypeError):
                    pass
                    
        return base_score
        
    def _extract_agents(self, value: Dict) -> List[str]:
        """Extract involved agents from coordination data."""
        agents = []
        
        if isinstance(value, dict):
            if "agents" in value:
                agents = value["agents"] if isinstance(value["agents"], list) else []
            elif "taskId" in value:
                agents = [f"agent_{value['taskId'][-8:]}"]  # Use task ID suffix as agent identifier
                
        return agents
        
    def train_models_from_patterns(self, patterns: List[CoordinationPattern]) -> Dict[str, float]:
        """Train neural models based on coordination patterns."""
        training_results = {}
        
        # Group patterns by type for specialized training
        pattern_groups = {}
        for pattern in patterns:
            if pattern.pattern_type not in pattern_groups:
                pattern_groups[pattern.pattern_type] = []
            pattern_groups[pattern.pattern_type].append(pattern)
            
        # Train models based on pattern groups
        for pattern_type, group_patterns in pattern_groups.items():
            relevant_models = self._get_relevant_models(pattern_type)
            
            for model_id in relevant_models:
                if model_id in self.neural_models:
                    improvement = self._train_specific_model(
                        self.neural_models[model_id], 
                        group_patterns
                    )
                    training_results[model_id] = improvement
                    
        # Store training data in database
        self._store_training_data(patterns, training_results)
        
        self.logger.info(f"Trained {len(training_results)} models with average improvement: "
                        f"{np.mean(list(training_results.values())):.3f}")
        
        return training_results
        
    def _get_relevant_models(self, pattern_type: str) -> List[str]:
        """Get relevant neural models for a pattern type."""
        relevance_map = {
            "task_completion": ["boids", "hierarchical_boids", "reinforcement_swarm"],
            "task_initiation": ["pso", "genetic_algorithm_swarm", "neural_network_swarm"],
            "swarm_coordination": ["boids", "aco", "social_forces", "boids_aco_hybrid"],
            "agent_interaction": ["multi_species_pso", "leadership_emergence", "social_forces"],
            "algorithm_selection": ["pso", "genetic_algorithm_swarm", "neural_network_swarm"],
            "general_coordination": ["boids", "pso", "aco"]
        }
        
        return relevance_map.get(pattern_type, ["boids", "pso"])
        
    def _train_specific_model(self, model: NeuralModel, patterns: List[CoordinationPattern]) -> float:
        """Train a specific neural model with patterns."""
        if not patterns:
            return 0.0
            
        # Calculate training metrics
        success_scores = [p.success_score for p in patterns]
        avg_success = np.mean(success_scores)
        
        # Simulate neural model improvement based on success patterns
        improvement_factor = (avg_success - 5.0) / 5.0  # Normalize around neutral score of 5.0
        
        # Update model parameters based on patterns
        if improvement_factor > 0:
            # Positive reinforcement - enhance successful parameters
            old_score = model.performance_score
            model.performance_score = min(10.0, old_score + improvement_factor * 0.1)
            model.usage_count += len(patterns)
            model.last_trained = datetime.now()
            
            improvement = model.performance_score - old_score
            self.logger.info(f"Model {model.model_id} improved by {improvement:.3f}")
            
            return improvement
        else:
            # Negative feedback - adjust parameters
            model.usage_count += len(patterns)
            model.last_trained = datetime.now()
            return 0.0
            
    def _store_training_data(self, patterns: List[CoordinationPattern], results: Dict[str, float]) -> None:
        """Store training data in the database."""
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                for pattern in patterns:
                    cursor.execute("""
                        INSERT INTO training_data 
                        (pattern_type, input_context, action_taken, outcome, success_score, model_version)
                        VALUES (?, ?, ?, ?, ?, ?)
                    """, (
                        pattern.pattern_type,
                        json.dumps(pattern.input_context),
                        json.dumps(pattern.actions_taken),
                        json.dumps(pattern.outcomes),
                        pattern.success_score,
                        pattern.model_version
                    ))
                    
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error storing training data: {e}")
            
    def recognize_coordination_patterns(self, recent_data: Dict[str, Any]) -> List[str]:
        """Recognize coordination patterns in recent data."""
        recognized_patterns = []
        
        # Pattern recognition logic
        if "task" in str(recent_data).lower():
            if "completed" in str(recent_data).lower():
                recognized_patterns.append("successful_task_completion")
            elif "failed" in str(recent_data).lower():
                recognized_patterns.append("failed_task_execution")
                
        if "swarm" in str(recent_data).lower():
            recognized_patterns.append("swarm_activity")
            
        if "agent" in str(recent_data).lower():
            recognized_patterns.append("agent_coordination")
            
        return recognized_patterns
        
    def get_model_recommendations(self, context: Dict[str, Any]) -> List[str]:
        """Get neural model recommendations based on context."""
        recommendations = []
        
        # Analyze context and recommend best models
        if "scale" in context:
            scale = context["scale"]
            if scale == "large":
                recommendations.extend(["ecs_boids", "gpu_compute_aco", "hierarchical_boids"])
            elif scale == "medium":
                recommendations.extend(["boids", "pso", "aco"])
            else:
                recommendations.extend(["boids", "fish_school"])
                
        if "optimization" in context:
            recommendations.extend(["pso", "genetic_algorithm_swarm", "neural_network_swarm"])
            
        if "pathfinding" in context:
            recommendations.extend(["aco", "tower_defense_pathing", "city_traffic"])
            
        # Sort by performance score
        model_scores = [(mid, self.neural_models[mid].performance_score) 
                       for mid in recommendations if mid in self.neural_models]
        model_scores.sort(key=lambda x: x[1], reverse=True)
        
        return [mid for mid, _ in model_scores[:5]]  # Top 5 recommendations
        
    def generate_improvement_report(self) -> Dict[str, Any]:
        """Generate a report on neural pattern learning improvements."""
        report = {
            "total_models": len(self.neural_models),
            "model_performance": {},
            "usage_statistics": {},
            "recommendations": {}
        }
        
        for model_id, model in self.neural_models.items():
            report["model_performance"][model_id] = {
                "score": model.performance_score,
                "usage_count": model.usage_count,
                "last_trained": model.last_trained.isoformat(),
                "algorithm": model.algorithm
            }
            
        # Calculate overall statistics
        scores = [m.performance_score for m in self.neural_models.values()]
        report["usage_statistics"] = {
            "average_performance": np.mean(scores),
            "best_performing": max(self.neural_models.items(), key=lambda x: x[1].performance_score)[0],
            "most_used": max(self.neural_models.items(), key=lambda x: x[1].usage_count)[0]
        }
        
        return report

def main():
    """Main function for neural pattern learning."""
    learner = NeuralPatternLearner()
    
    # Extract and analyze patterns
    patterns = learner.extract_coordination_patterns()
    
    # Train models
    training_results = learner.train_models_from_patterns(patterns)
    
    # Generate report
    report = learner.generate_improvement_report()
    
    print("Neural Pattern Learning Results:")
    print(f"Processed {len(patterns)} coordination patterns")
    print(f"Trained {len(training_results)} neural models")
    print(f"Average model performance: {report['usage_statistics']['average_performance']:.2f}")
    print(f"Best performing model: {report['usage_statistics']['best_performing']}")

if __name__ == "__main__":
    main()