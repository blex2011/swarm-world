#!/usr/bin/env python3
"""
Neural Training Orchestrator
Main orchestrator for all neural pattern learning components.
"""

import sqlite3
import json
import sys
import os
from datetime import datetime
from typing import Dict, List, Any, Optional
import logging
import argparse

# Import the neural learning components
from neural_pattern_learner import NeuralPatternLearner
from pattern_recognition_engine import PatternRecognitionEngine
from continuous_learning_system import ContinuousLearningSystem
from feedback_loop_system import FeedbackLoopSystem
from neural_trained_hook import NeuralTrainedHook

class NeuralTrainingOrchestrator:
    """Main orchestrator for neural pattern learning in swarm coordination."""
    
    def __init__(self, memory_db_path: str = ".swarm/memory.db"):
        self.memory_db_path = memory_db_path
        self.logger = self._setup_logging()
        
        # Initialize components
        self.pattern_learner = NeuralPatternLearner(memory_db_path)
        self.pattern_recognition = PatternRecognitionEngine(memory_db_path)
        self.continuous_learning = ContinuousLearningSystem(memory_db_path)
        self.feedback_loop = FeedbackLoopSystem(memory_db_path)
        self.neural_hook = NeuralTrainedHook(memory_db_path)
        
        self.is_orchestrator_running = False
        
    def _setup_logging(self) -> logging.Logger:
        """Setup logging for neural training orchestrator."""
        logger = logging.getLogger('neural_orchestrator')
        logger.setLevel(logging.INFO)
        
        if not logger.handlers:
            handler = logging.StreamHandler()
            formatter = logging.Formatter(
                '[%(asctime)s] %(levelname)s [%(name)s] %(message)s'
            )
            handler.setFormatter(formatter)
            logger.addHandler(handler)
            
        return logger
        
    def initialize_neural_training(self) -> Dict[str, Any]:
        """Initialize the complete neural training system."""
        
        self.logger.info("Initializing neural pattern learning system...")
        
        # Extract existing coordination patterns
        patterns = self.pattern_learner.extract_coordination_patterns()
        
        # Train models from existing patterns
        training_results = self.pattern_learner.train_models_from_patterns(patterns)
        
        # Start continuous systems
        self.pattern_recognition.start_continuous_recognition(poll_interval=30)
        self.continuous_learning.start_continuous_learning()
        self.feedback_loop.start_feedback_loop(evaluation_interval=120)
        
        self.is_orchestrator_running = True
        
        initialization_report = {
            "status": "initialized",
            "patterns_processed": len(patterns),
            "models_trained": len(training_results),
            "neural_models_available": len(self.pattern_learner.neural_models),
            "systems_started": ["pattern_recognition", "continuous_learning", "feedback_loop"],
            "timestamp": datetime.now().isoformat()
        }
        
        self.logger.info(f"Neural training system initialized with {len(patterns)} patterns "
                        f"and {len(training_results)} trained models")
        
        return initialization_report
        
    def stop_neural_training(self) -> None:
        """Stop all neural training systems."""
        
        self.logger.info("Stopping neural training systems...")
        
        self.pattern_recognition.stop_continuous_recognition()
        self.continuous_learning.stop_continuous_learning()
        self.feedback_loop.stop_feedback_loop()
        
        self.is_orchestrator_running = False
        
        self.logger.info("All neural training systems stopped")
        
    def train_from_recent_data(self, hours_back: int = 1) -> Dict[str, Any]:
        """Train models from recent coordination data."""
        
        # Extract recent patterns
        patterns = self.pattern_learner.extract_coordination_patterns()
        recent_patterns = [p for p in patterns 
                          if (datetime.now() - p.timestamp).total_seconds() < hours_back * 3600]
        
        if not recent_patterns:
            return {"status": "no_recent_data", "patterns_found": 0}
            
        # Train from recent patterns
        training_results = self.pattern_learner.train_models_from_patterns(recent_patterns)
        
        # Generate improvement report
        improvement_report = self.pattern_learner.generate_improvement_report()
        
        result = {
            "status": "training_completed",
            "recent_patterns": len(recent_patterns),
            "models_improved": len(training_results),
            "improvement_report": improvement_report,
            "timestamp": datetime.now().isoformat()
        }
        
        self.logger.info(f"Trained from {len(recent_patterns)} recent patterns, "
                        f"improved {len(training_results)} models")
        
        return result
        
    def get_neural_status(self) -> Dict[str, Any]:
        """Get comprehensive status of neural training systems."""
        
        status = {
            "orchestrator_running": self.is_orchestrator_running,
            "components": {},
            "neural_models": {},
            "recent_activity": {},
            "performance_summary": {},
            "timestamp": datetime.now().isoformat()
        }
        
        # Component status
        status["components"] = {
            "pattern_recognition": {
                "running": self.pattern_recognition.is_running,
                "statistics": self.pattern_recognition.get_recognition_statistics()
            },
            "continuous_learning": {
                "running": self.continuous_learning.is_running,
                "statistics": self.continuous_learning.get_learning_statistics()
            },
            "feedback_loop": {
                "running": self.feedback_loop.is_running,
                "statistics": self.feedback_loop.get_feedback_statistics()
            }
        }
        
        # Neural models status
        for model_id, model in self.pattern_learner.neural_models.items():
            status["neural_models"][model_id] = {
                "type": model.model_type,
                "algorithm": model.algorithm,
                "performance_score": model.performance_score,
                "usage_count": model.usage_count,
                "last_trained": model.last_trained.isoformat(),
                "optimization_focus": model.optimization_focus
            }
            
        # Recent training activity
        training_history = self.neural_hook.get_training_history(limit=10)
        status["recent_activity"] = {
            "recent_training_sessions": len(training_history),
            "last_training": training_history[0] if training_history else None
        }
        
        # Performance summary
        model_stats = self.neural_hook.get_model_performance_stats()
        status["performance_summary"] = {
            "models_with_stats": len(model_stats),
            "top_performers": self._get_top_performing_models(model_stats)
        }
        
        return status
        
    def _get_top_performing_models(self, model_stats: Dict[str, Any]) -> List[Dict[str, Any]]:
        """Get top performing models from statistics."""
        performers = []
        
        for model_id, stats in model_stats.items():
            total_score = 0
            session_count = 0
            
            for metric, data in stats.items():
                if "improvement" in metric:
                    total_score += data.get("average", 0)
                    session_count += data.get("training_sessions", 0)
                    
            if session_count > 0:
                performers.append({
                    "model_id": model_id,
                    "average_improvement": total_score / session_count if session_count > 0 else 0,
                    "training_sessions": session_count
                })
                
        # Sort by average improvement
        performers.sort(key=lambda x: x["average_improvement"], reverse=True)
        return performers[:5]  # Top 5
        
    def extract_training_data(self) -> Dict[str, Any]:
        """Extract training data from performance benchmarks and tool effectiveness."""
        
        training_data = {
            "performance_benchmarks": [],
            "tool_effectiveness": [],
            "code_patterns": [],
            "agent_interactions": [],
            "error_patterns": []
        }
        
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Extract performance benchmarks
                cursor.execute("""
                    SELECT * FROM performance_benchmarks 
                    ORDER BY timestamp DESC LIMIT 100
                """)
                training_data["performance_benchmarks"] = [
                    dict(zip([col[0] for col in cursor.description], row))
                    for row in cursor.fetchall()
                ]
                
                # Extract tool usage effectiveness
                cursor.execute("""
                    SELECT * FROM mcp_tool_usage 
                    ORDER BY timestamp DESC LIMIT 100
                """)
                training_data["tool_effectiveness"] = [
                    dict(zip([col[0] for col in cursor.description], row))
                    for row in cursor.fetchall()
                ]
                
                # Extract code patterns
                cursor.execute("""
                    SELECT * FROM code_patterns 
                    ORDER BY effectiveness_score DESC, frequency DESC LIMIT 50
                """)
                training_data["code_patterns"] = [
                    dict(zip([col[0] for col in cursor.description], row))
                    for row in cursor.fetchall()
                ]
                
                # Extract agent interactions
                cursor.execute("""
                    SELECT * FROM agent_interactions 
                    ORDER BY timestamp DESC LIMIT 50
                """)
                training_data["agent_interactions"] = [
                    dict(zip([col[0] for col in cursor.description], row))
                    for row in cursor.fetchall()
                ]
                
                # Extract error patterns
                cursor.execute("""
                    SELECT * FROM error_patterns 
                    ORDER BY frequency DESC LIMIT 30
                """)
                training_data["error_patterns"] = [
                    dict(zip([col[0] for col in cursor.description], row))
                    for row in cursor.fetchall()
                ]
                
        except sqlite3.Error as e:
            self.logger.error(f"Error extracting training data: {e}")
            
        # Calculate summary statistics
        summary = {
            "total_benchmarks": len(training_data["performance_benchmarks"]),
            "total_tool_usage": len(training_data["tool_effectiveness"]),
            "total_code_patterns": len(training_data["code_patterns"]),
            "total_interactions": len(training_data["agent_interactions"]),
            "total_error_patterns": len(training_data["error_patterns"])
        }
        
        self.logger.info(f"Extracted training data: {summary}")
        
        return {
            "training_data": training_data,
            "summary": summary,
            "extraction_timestamp": datetime.now().isoformat()
        }
        
    def validate_neural_models(self) -> Dict[str, Any]:
        """Validate and test neural pattern models."""
        
        validation_results = {
            "total_models": len(self.pattern_learner.neural_models),
            "validation_results": {},
            "overall_health": "unknown",
            "recommendations": []
        }
        
        healthy_models = 0
        
        for model_id, model in self.pattern_learner.neural_models.items():
            model_validation = {
                "model_id": model_id,
                "performance_score": model.performance_score,
                "usage_count": model.usage_count,
                "health_status": "unknown",
                "issues": [],
                "recommendations": []
            }
            
            # Validate performance score
            if model.performance_score >= 8.0:
                model_validation["health_status"] = "excellent"
                healthy_models += 1
            elif model.performance_score >= 6.0:
                model_validation["health_status"] = "good"
                healthy_models += 1
            elif model.performance_score >= 4.0:
                model_validation["health_status"] = "fair"
                model_validation["issues"].append("Below average performance")
                model_validation["recommendations"].append("Increase training frequency")
            else:
                model_validation["health_status"] = "poor"
                model_validation["issues"].append("Very low performance score")
                model_validation["recommendations"].append("Retrain with recent successful patterns")
                
            # Validate usage patterns
            if model.usage_count == 0:
                model_validation["issues"].append("Never used in training")
                model_validation["recommendations"].append("Include in next training cycle")
            elif model.usage_count < 5:
                model_validation["issues"].append("Low usage count")
                model_validation["recommendations"].append("Increase exposure to relevant patterns")
                
            # Validate training recency
            days_since_training = (datetime.now() - model.last_trained).days
            if days_since_training > 7:
                model_validation["issues"].append("Training data is stale")
                model_validation["recommendations"].append("Retrain with recent data")
                
            validation_results["validation_results"][model_id] = model_validation
            
        # Calculate overall health
        health_ratio = healthy_models / len(self.pattern_learner.neural_models)
        if health_ratio >= 0.8:
            validation_results["overall_health"] = "excellent"
        elif health_ratio >= 0.6:
            validation_results["overall_health"] = "good"
        elif health_ratio >= 0.4:
            validation_results["overall_health"] = "fair"
        else:
            validation_results["overall_health"] = "poor"
            
        # Generate overall recommendations
        if health_ratio < 0.6:
            validation_results["recommendations"].append("Increase training frequency for underperforming models")
        if healthy_models < 5:
            validation_results["recommendations"].append("Focus on core coordination models")
            
        self.logger.info(f"Model validation completed: {healthy_models}/{len(self.pattern_learner.neural_models)} "
                        f"models healthy, overall health: {validation_results['overall_health']}")
        
        return validation_results
        
    def setup_auto_retraining(self, trigger_conditions: Dict[str, Any]) -> Dict[str, Any]:
        """Setup automated retraining triggers based on coordination data."""
        
        # Default trigger conditions
        default_conditions = {
            "min_new_patterns": 10,           # Minimum new patterns to trigger retraining
            "performance_threshold": 6.0,     # Performance score threshold
            "time_interval_hours": 24,        # Time interval for checking
            "success_rate_threshold": 0.7,    # Minimum success rate
            "error_rate_threshold": 0.2       # Maximum error rate
        }
        
        # Merge with provided conditions
        conditions = {**default_conditions, **trigger_conditions}
        
        # Store trigger configuration
        trigger_config = {
            "trigger_id": f"auto_retrain_{int(datetime.now().timestamp())}",
            "conditions": conditions,
            "created_at": datetime.now().isoformat(),
            "status": "active"
        }
        
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Create auto retraining table if not exists
                cursor.execute("""
                    CREATE TABLE IF NOT EXISTS auto_retrain_triggers (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        trigger_id TEXT UNIQUE,
                        conditions TEXT,
                        status TEXT,
                        last_triggered INTEGER,
                        trigger_count INTEGER DEFAULT 0,
                        created_at INTEGER DEFAULT (strftime('%s', 'now'))
                    )
                """)
                
                # Store trigger configuration
                cursor.execute("""
                    INSERT OR REPLACE INTO auto_retrain_triggers 
                    (trigger_id, conditions, status)
                    VALUES (?, ?, ?)
                """, (
                    trigger_config["trigger_id"],
                    json.dumps(conditions),
                    "active"
                ))
                
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error setting up auto retraining: {e}")
            return {"status": "error", "message": str(e)}
            
        self.logger.info(f"Auto retraining configured with conditions: {conditions}")
        
        return {
            "status": "configured",
            "trigger_id": trigger_config["trigger_id"],
            "conditions": conditions,
            "message": "Auto retraining triggers have been set up successfully"
        }

def main():
    """Main CLI interface for neural training orchestrator."""
    parser = argparse.ArgumentParser(description="Neural Training Orchestrator for Swarm Coordination")
    parser.add_argument("action", choices=[
        "init", "status", "train", "stop", "validate", "extract-data", "auto-retrain"
    ], help="Action to perform")
    parser.add_argument("--hours", type=int, default=1, help="Hours back for recent data training")
    parser.add_argument("--conditions", help="JSON string with auto-retrain conditions")
    
    args = parser.parse_args()
    
    orchestrator = NeuralTrainingOrchestrator()
    
    if args.action == "init":
        print("🚀 Initializing neural pattern learning system...")
        result = orchestrator.initialize_neural_training()
        print(f"✅ Initialization complete: {result['patterns_processed']} patterns, "
              f"{result['models_trained']} models trained")
        
    elif args.action == "status":
        print("📊 Getting neural training status...")
        status = orchestrator.get_neural_status()
        print(f"🤖 Neural models: {len(status['neural_models'])}")
        print(f"🔄 Systems running: {status['orchestrator_running']}")
        for component, info in status['components'].items():
            print(f"  - {component}: {'✅' if info['running'] else '❌'}")
            
    elif args.action == "train":
        print(f"🧠 Training from recent {args.hours} hours of data...")
        result = orchestrator.train_from_recent_data(args.hours)
        print(f"✅ Training complete: {result.get('models_improved', 0)} models improved")
        
    elif args.action == "stop":
        print("🛑 Stopping neural training systems...")
        orchestrator.stop_neural_training()
        print("✅ All systems stopped")
        
    elif args.action == "validate":
        print("🔍 Validating neural models...")
        validation = orchestrator.validate_neural_models()
        print(f"📊 Overall health: {validation['overall_health']}")
        print(f"✅ Healthy models: {sum(1 for v in validation['validation_results'].values() if v['health_status'] in ['excellent', 'good'])}/{validation['total_models']}")
        
    elif args.action == "extract-data":
        print("📊 Extracting training data...")
        data = orchestrator.extract_training_data()
        print(f"✅ Extracted: {data['summary']}")
        
    elif args.action == "auto-retrain":
        conditions = {}
        if args.conditions:
            try:
                conditions = json.loads(args.conditions)
            except json.JSONDecodeError:
                print("❌ Invalid JSON in conditions")
                sys.exit(1)
                
        print("⚙️ Setting up auto retraining...")
        result = orchestrator.setup_auto_retraining(conditions)
        print(f"✅ {result['message']}")

if __name__ == "__main__":
    main()