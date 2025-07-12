#!/usr/bin/env python3
"""
Neural-Trained Hook Implementation
Implements the neural-trained hook for saving pattern improvements.
"""

import sqlite3
import json
import sys
import argparse
from datetime import datetime
from typing import Dict, List, Any, Optional
import logging

class NeuralTrainedHook:
    """Hook implementation for neural training results."""
    
    def __init__(self, memory_db_path: str = ".swarm/memory.db"):
        self.memory_db_path = memory_db_path
        self.logger = self._setup_logging()
        
    def _setup_logging(self) -> logging.Logger:
        """Setup logging for neural-trained hook."""
        logger = logging.getLogger('neural_trained_hook')
        logger.setLevel(logging.INFO)
        
        if not logger.handlers:
            handler = logging.StreamHandler()
            formatter = logging.Formatter(
                '[%(asctime)s] %(levelname)s [%(name)s] %(message)s'
            )
            handler.setFormatter(formatter)
            logger.addHandler(handler)
            
        return logger
        
    def execute_hook(self, pattern: str, improvement: float, models: List[str], 
                    learning_type: str, context: Optional[Dict[str, Any]] = None) -> bool:
        """Execute the neural-trained hook."""
        
        try:
            # Create hook execution record
            hook_data = {
                "hook_type": "neural-trained",
                "pattern": pattern,
                "improvement_score": improvement,
                "trained_models": models,
                "learning_type": learning_type,
                "context": context or {},
                "timestamp": datetime.now().isoformat(),
                "execution_id": f"neural_hook_{int(datetime.now().timestamp())}"
            }
            
            # Store in memory database
            success = self._store_hook_data(hook_data)
            
            if success:
                # Update training data table
                self._update_training_data(hook_data)
                
                # Update model performance
                self._update_model_performance(models, improvement, learning_type)
                
                # Log successful execution
                self.logger.info(f"Neural-trained hook executed: pattern={pattern}, "
                               f"improvement={improvement:.3f}, models={len(models)}")
                
                # Print hook completion message
                print("🧠 Executing neural-trained hook...")
                print(f"📊 Pattern: {pattern}")
                print(f"📈 Improvement: {improvement:.3f}")
                print(f"🤖 Models: {', '.join(models)}")
                print(f"🎯 Learning Type: {learning_type}")
                print("  💾 Neural training data saved to .swarm/memory.db")
                print("✅ ✅ Neural-trained hook completed")
                
                return True
            else:
                self.logger.error("Failed to store neural training data")
                return False
                
        except Exception as e:
            self.logger.error(f"Error executing neural-trained hook: {e}")
            print(f"❌ Neural-trained hook failed: {e}")
            return False
            
    def _store_hook_data(self, hook_data: Dict[str, Any]) -> bool:
        """Store hook execution data in memory database."""
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Store in memory_entries table
                key = f"neural_hook/{hook_data['execution_id']}"
                value = json.dumps(hook_data)
                
                cursor.execute("""
                    INSERT INTO memory_entries (key, value, namespace) 
                    VALUES (?, ?, ?)
                """, (key, value, "neural_training"))
                
                conn.commit()
                return True
                
        except sqlite3.Error as e:
            self.logger.error(f"Database error storing hook data: {e}")
            return False
            
    def _update_training_data(self, hook_data: Dict[str, Any]) -> None:
        """Update the training_data table with new learning results."""
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                cursor.execute("""
                    INSERT INTO training_data 
                    (pattern_type, input_context, action_taken, outcome, success_score, 
                     model_version, feedback)
                    VALUES (?, ?, ?, ?, ?, ?, ?)
                """, (
                    hook_data["pattern"],
                    json.dumps(hook_data["context"]),
                    json.dumps({"learning_type": hook_data["learning_type"]}),
                    json.dumps({"improvement": hook_data["improvement_score"]}),
                    hook_data["improvement_score"],
                    "1.0",
                    json.dumps({"models": hook_data["trained_models"]})
                ))
                
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error updating training data: {e}")
            
    def _update_model_performance(self, models: List[str], improvement: float, 
                                 learning_type: str) -> None:
        """Update model performance tracking."""
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                for model_id in models:
                    # Record performance improvement
                    cursor.execute("""
                        INSERT INTO model_performance_tracking 
                        (model_id, performance_metric, value, context)
                        VALUES (?, ?, ?, ?)
                    """, (
                        model_id, 
                        f"improvement_{learning_type}", 
                        improvement, 
                        "neural_training"
                    ))
                    
                    # Update effectiveness score in code_patterns if exists
                    cursor.execute("""
                        UPDATE code_patterns 
                        SET effectiveness_score = effectiveness_score + ?
                        WHERE pattern_name LIKE ?
                    """, (improvement * 0.1, f"%{model_id}%"))
                    
                conn.commit()
                
        except sqlite3.Error as e:
            self.logger.error(f"Error updating model performance: {e}")
            
    def get_training_history(self, limit: int = 50) -> List[Dict[str, Any]]:
        """Get recent neural training history."""
        history = []
        
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                cursor.execute("""
                    SELECT key, value, created_at 
                    FROM memory_entries 
                    WHERE namespace = 'neural_training'
                    ORDER BY created_at DESC 
                    LIMIT ?
                """, (limit,))
                
                for row in cursor.fetchall():
                    key, value_str, timestamp = row
                    try:
                        value = json.loads(value_str)
                        value["timestamp"] = timestamp
                        history.append(value)
                    except json.JSONDecodeError:
                        continue
                        
        except sqlite3.Error as e:
            self.logger.error(f"Error getting training history: {e}")
            
        return history
        
    def get_model_performance_stats(self) -> Dict[str, Any]:
        """Get model performance statistics."""
        stats = {}
        
        try:
            with sqlite3.connect(self.memory_db_path) as conn:
                cursor = conn.cursor()
                
                # Get model improvement trends
                cursor.execute("""
                    SELECT model_id, performance_metric, AVG(value) as avg_value, COUNT(*) as count
                    FROM model_performance_tracking 
                    WHERE context = 'neural_training'
                    GROUP BY model_id, performance_metric
                    ORDER BY model_id, performance_metric
                """)
                
                for row in cursor.fetchall():
                    model_id, metric, avg_value, count = row
                    
                    if model_id not in stats:
                        stats[model_id] = {}
                        
                    stats[model_id][metric] = {
                        "average": avg_value,
                        "training_sessions": count
                    }
                    
        except sqlite3.Error as e:
            self.logger.error(f"Error getting model stats: {e}")
            
        return stats

def main():
    """Main function for neural-trained hook CLI."""
    parser = argparse.ArgumentParser(description="Neural-trained hook for saving pattern improvements")
    parser.add_argument("--pattern", required=True, help="Pattern name that was improved")
    parser.add_argument("--improvement", type=float, required=True, help="Improvement score")
    parser.add_argument("--models", required=True, help="Comma-separated list of trained models")
    parser.add_argument("--learning-type", required=True, help="Type of learning applied")
    parser.add_argument("--context", help="Additional context as JSON string")
    parser.add_argument("--history", action="store_true", help="Show training history")
    parser.add_argument("--stats", action="store_true", help="Show model performance stats")
    
    args = parser.parse_args()
    
    hook = NeuralTrainedHook()
    
    if args.history:
        history = hook.get_training_history()
        print(f"Recent neural training history ({len(history)} entries):")
        for entry in history[:10]:  # Show last 10
            print(f"  - {entry.get('pattern', 'unknown')}: {entry.get('improvement_score', 0):.3f} "
                  f"({entry.get('learning_type', 'unknown')})")
        return
        
    if args.stats:
        stats = hook.get_model_performance_stats()
        print("Model performance statistics:")
        for model_id, metrics in stats.items():
            print(f"  {model_id}:")
            for metric, data in metrics.items():
                print(f"    {metric}: {data['average']:.3f} avg, {data['training_sessions']} sessions")
        return
    
    # Parse context if provided
    context = None
    if args.context:
        try:
            context = json.loads(args.context)
        except json.JSONDecodeError:
            print("Error: Invalid JSON in context argument")
            sys.exit(1)
            
    # Parse models list
    models = [model.strip() for model in args.models.split(",")]
    
    # Execute the hook
    success = hook.execute_hook(
        pattern=args.pattern,
        improvement=args.improvement,
        models=models,
        learning_type=args.learning_type,
        context=context
    )
    
    sys.exit(0 if success else 1)

if __name__ == "__main__":
    main()