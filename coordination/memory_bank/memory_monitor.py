#!/usr/bin/env python3
"""
Memory Usage Monitor and Optimization System
Monitors memory usage patterns, detects inefficiencies, and optimizes storage.
"""

import json
import time
import threading
import statistics
from typing import Dict, List, Any, Optional, Tuple
from datetime import datetime, timedelta
from dataclasses import dataclass, asdict
from persistence_manager import MemoryPersistenceManager


@dataclass
class MemoryUsageSnapshot:
    """Snapshot of memory usage at a point in time."""
    timestamp: datetime
    total_size: int
    total_entries: int
    category_breakdown: Dict[str, Dict[str, Any]]
    agent_breakdown: Dict[str, Dict[str, Any]]
    session_breakdown: Dict[str, Dict[str, Any]]
    cache_hit_rate: float
    average_access_time: float


@dataclass
class MemoryAlert:
    """Alert for memory usage issues."""
    alert_id: str
    alert_type: str
    severity: str  # "low", "medium", "high", "critical"
    message: str
    triggered_at: datetime
    memory_key: Optional[str] = None
    agent_name: Optional[str] = None
    metadata: Dict[str, Any] = None


@dataclass
class OptimizationRecommendation:
    """Recommendation for memory optimization."""
    recommendation_id: str
    recommendation_type: str  # "cleanup", "compression", "archival", "restructure"
    priority: str  # "low", "medium", "high"
    description: str
    estimated_savings: int  # bytes
    affected_keys: List[str]
    implementation_effort: str  # "low", "medium", "high"


class MemoryMonitor:
    """Monitors memory usage and provides optimization recommendations."""
    
    def __init__(self, config_path: str = None):
        self.memory_manager = MemoryPersistenceManager(config_path)
        self.config = self.memory_manager.config
        
        # Monitoring state
        self.usage_history: List[MemoryUsageSnapshot] = []
        self.active_alerts: List[MemoryAlert] = []
        self.recommendations: List[OptimizationRecommendation] = []
        
        # Performance tracking
        self.access_times: List[float] = []
        self.cache_hits = 0
        self.cache_misses = 0
        
        # Monitoring configuration
        self.monitoring_interval = 60  # seconds
        self.history_retention_days = 30
        self.alert_thresholds = {
            "memory_usage_warning": 0.8,  # 80% of limit
            "memory_usage_critical": 0.95,  # 95% of limit
            "low_cache_hit_rate": 0.5,  # 50% hit rate
            "high_access_time": 1.0,  # 1 second average
            "stale_data_days": 7,  # 7 days without access
            "fragmentation_ratio": 0.3  # 30% fragmentation
        }
        
        # Start monitoring thread
        self.running = True
        self.monitor_thread = threading.Thread(target=self._monitoring_loop, daemon=True)
        self.monitor_thread.start()
    
    def get_current_usage_snapshot(self) -> MemoryUsageSnapshot:
        """Get current memory usage snapshot."""
        try:
            # Get basic statistics
            stats = self.memory_manager.get_memory_usage_stats()
            
            # Calculate cache hit rate
            total_accesses = self.cache_hits + self.cache_misses
            cache_hit_rate = self.cache_hits / total_accesses if total_accesses > 0 else 0.0
            
            # Calculate average access time
            avg_access_time = statistics.mean(self.access_times) if self.access_times else 0.0
            
            # Get detailed breakdowns
            category_breakdown = stats.get("categories", {})
            agent_breakdown = self._get_agent_breakdown()
            session_breakdown = self._get_session_breakdown()
            
            return MemoryUsageSnapshot(
                timestamp=datetime.now(),
                total_size=stats.get("total_size", 0),
                total_entries=stats.get("total_entries", 0),
                category_breakdown=category_breakdown,
                agent_breakdown=agent_breakdown,
                session_breakdown=session_breakdown,
                cache_hit_rate=cache_hit_rate,
                average_access_time=avg_access_time
            )
            
        except Exception as e:
            print(f"Error creating usage snapshot: {e}")
            return MemoryUsageSnapshot(
                timestamp=datetime.now(),
                total_size=0,
                total_entries=0,
                category_breakdown={},
                agent_breakdown={},
                session_breakdown={},
                cache_hit_rate=0.0,
                average_access_time=0.0
            )
    
    def check_memory_health(self) -> Dict[str, Any]:
        """Perform comprehensive memory health check."""
        snapshot = self.get_current_usage_snapshot()
        health_report = {
            "overall_health": "healthy",
            "issues": [],
            "recommendations": [],
            "metrics": {
                "memory_utilization": self._calculate_memory_utilization(snapshot),
                "cache_efficiency": snapshot.cache_hit_rate,
                "performance_score": self._calculate_performance_score(snapshot),
                "fragmentation_level": self._calculate_fragmentation(snapshot)
            }
        }
        
        # Check for issues
        issues = self._detect_memory_issues(snapshot)
        health_report["issues"] = [asdict(issue) for issue in issues]
        
        # Generate recommendations
        recommendations = self._generate_optimization_recommendations(snapshot, issues)
        health_report["recommendations"] = [asdict(rec) for rec in recommendations]
        
        # Determine overall health
        if any(issue.severity in ["critical", "high"] for issue in issues):
            health_report["overall_health"] = "critical" if any(issue.severity == "critical" for issue in issues) else "warning"
        
        return health_report
    
    def optimize_memory_usage(self, auto_apply: bool = False) -> Dict[str, Any]:
        """Optimize memory usage based on current analysis."""
        optimization_report = {
            "optimizations_applied": [],
            "space_saved": 0,
            "entries_cleaned": 0,
            "errors": []
        }
        
        try:
            # 1. Clean up expired entries
            expired_count = self.memory_manager.cleanup_expired_memory()
            optimization_report["optimizations_applied"].append(f"Cleaned {expired_count} expired entries")
            optimization_report["entries_cleaned"] += expired_count
            
            # 2. Identify and clean stale data
            stale_keys = self._identify_stale_data()
            if auto_apply and stale_keys:
                stale_cleaned = self._cleanup_stale_data(stale_keys)
                optimization_report["optimizations_applied"].append(f"Cleaned {stale_cleaned} stale entries")
                optimization_report["entries_cleaned"] += stale_cleaned
            
            # 3. Compress large entries
            large_entries = self._identify_compressible_entries()
            if auto_apply and large_entries:
                compression_savings = self._compress_large_entries(large_entries)
                optimization_report["optimizations_applied"].append(f"Compressed {len(large_entries)} large entries")
                optimization_report["space_saved"] += compression_savings
            
            # 4. Archive old sessions
            archivable_sessions = self._identify_archivable_sessions()
            if auto_apply and archivable_sessions:
                archive_savings = self._archive_old_sessions(archivable_sessions)
                optimization_report["optimizations_applied"].append(f"Archived {len(archivable_sessions)} old sessions")
                optimization_report["space_saved"] += archive_savings
            
            # 5. Defragment memory storage
            if auto_apply:
                defrag_savings = self._defragment_storage()
                if defrag_savings > 0:
                    optimization_report["optimizations_applied"].append("Defragmented storage")
                    optimization_report["space_saved"] += defrag_savings
            
        except Exception as e:
            optimization_report["errors"].append(f"Optimization error: {e}")
        
        return optimization_report
    
    def get_memory_trends(self, days: int = 7) -> Dict[str, Any]:
        """Analyze memory usage trends over time."""
        cutoff_date = datetime.now() - timedelta(days=days)
        recent_snapshots = [
            snapshot for snapshot in self.usage_history
            if snapshot.timestamp >= cutoff_date
        ]
        
        if not recent_snapshots:
            return {"error": "No recent data available"}
        
        trends = {
            "time_period": f"Last {days} days",
            "total_snapshots": len(recent_snapshots),
            "memory_growth": self._calculate_memory_growth_trend(recent_snapshots),
            "cache_performance_trend": self._calculate_cache_trend(recent_snapshots),
            "agent_activity_trends": self._calculate_agent_activity_trends(recent_snapshots),
            "peak_usage_times": self._identify_peak_usage_times(recent_snapshots)
        }
        
        return trends
    
    def generate_memory_report(self) -> Dict[str, Any]:
        """Generate comprehensive memory usage report."""
        snapshot = self.get_current_usage_snapshot()
        health = self.check_memory_health()
        trends = self.get_memory_trends()
        
        report = {
            "report_generated_at": datetime.now().isoformat(),
            "current_snapshot": asdict(snapshot),
            "health_assessment": health,
            "usage_trends": trends,
            "active_alerts": [asdict(alert) for alert in self.active_alerts],
            "optimization_opportunities": [asdict(rec) for rec in self.recommendations],
            "configuration": {
                "monitoring_interval": self.monitoring_interval,
                "retention_days": self.history_retention_days,
                "alert_thresholds": self.alert_thresholds
            }
        }
        
        return report
    
    def _monitoring_loop(self):
        """Main monitoring loop running in background thread."""
        while self.running:
            try:
                # Take usage snapshot
                snapshot = self.get_current_usage_snapshot()
                self.usage_history.append(snapshot)
                
                # Trim old history
                cutoff_date = datetime.now() - timedelta(days=self.history_retention_days)
                self.usage_history = [
                    s for s in self.usage_history
                    if s.timestamp >= cutoff_date
                ]
                
                # Detect issues and generate alerts
                issues = self._detect_memory_issues(snapshot)
                for issue in issues:
                    if not self._is_duplicate_alert(issue):
                        self.active_alerts.append(issue)
                
                # Clean up resolved alerts
                self._cleanup_resolved_alerts()
                
                # Generate optimization recommendations
                new_recommendations = self._generate_optimization_recommendations(snapshot, issues)
                self.recommendations.extend(new_recommendations)
                
                # Limit recommendations to prevent overflow
                self.recommendations = self.recommendations[-100:]  # Keep last 100
                
                # Store monitoring data
                self._store_monitoring_data(snapshot)
                
                # Sleep until next monitoring cycle
                time.sleep(self.monitoring_interval)
                
            except Exception as e:
                print(f"Error in monitoring loop: {e}")
                time.sleep(self.monitoring_interval)
    
    def _get_agent_breakdown(self) -> Dict[str, Dict[str, Any]]:
        """Get memory usage breakdown by agent."""
        # This would query the database for agent-specific memory usage
        # For now, returning mock data structure
        return {}
    
    def _get_session_breakdown(self) -> Dict[str, Dict[str, Any]]:
        """Get memory usage breakdown by session."""
        # This would query the database for session-specific memory usage
        # For now, returning mock data structure
        return {}
    
    def _calculate_memory_utilization(self, snapshot: MemoryUsageSnapshot) -> float:
        """Calculate memory utilization percentage."""
        max_memory = self._parse_size_string(self.config["agent_memory_limits"]["max_individual_memory"])
        if max_memory == 0:
            return 0.0
        return min(snapshot.total_size / max_memory, 1.0)
    
    def _calculate_performance_score(self, snapshot: MemoryUsageSnapshot) -> float:
        """Calculate overall performance score (0-1)."""
        # Weighted combination of cache hit rate and access time
        cache_score = snapshot.cache_hit_rate
        time_score = max(0, 1 - (snapshot.average_access_time / 2.0))  # 2 seconds = 0 score
        
        return (cache_score * 0.6 + time_score * 0.4)
    
    def _calculate_fragmentation(self, snapshot: MemoryUsageSnapshot) -> float:
        """Calculate memory fragmentation level."""
        # This would analyze storage fragmentation
        # For now, returning a mock value
        return 0.1
    
    def _detect_memory_issues(self, snapshot: MemoryUsageSnapshot) -> List[MemoryAlert]:
        """Detect memory-related issues."""
        issues = []
        
        # Check memory usage
        utilization = self._calculate_memory_utilization(snapshot)
        if utilization >= self.alert_thresholds["memory_usage_critical"]:
            issues.append(MemoryAlert(
                alert_id=f"mem_usage_critical_{int(time.time())}",
                alert_type="memory_usage",
                severity="critical",
                message=f"Memory usage at {utilization:.1%} (critical threshold: {self.alert_thresholds['memory_usage_critical']:.1%})",
                triggered_at=datetime.now(),
                metadata={"utilization": utilization}
            ))
        elif utilization >= self.alert_thresholds["memory_usage_warning"]:
            issues.append(MemoryAlert(
                alert_id=f"mem_usage_warning_{int(time.time())}",
                alert_type="memory_usage",
                severity="medium",
                message=f"Memory usage at {utilization:.1%} (warning threshold: {self.alert_thresholds['memory_usage_warning']:.1%})",
                triggered_at=datetime.now(),
                metadata={"utilization": utilization}
            ))
        
        # Check cache performance
        if snapshot.cache_hit_rate < self.alert_thresholds["low_cache_hit_rate"]:
            issues.append(MemoryAlert(
                alert_id=f"cache_performance_{int(time.time())}",
                alert_type="cache_performance",
                severity="medium",
                message=f"Low cache hit rate: {snapshot.cache_hit_rate:.1%}",
                triggered_at=datetime.now(),
                metadata={"hit_rate": snapshot.cache_hit_rate}
            ))
        
        # Check access times
        if snapshot.average_access_time > self.alert_thresholds["high_access_time"]:
            issues.append(MemoryAlert(
                alert_id=f"access_time_{int(time.time())}",
                alert_type="performance",
                severity="medium",
                message=f"High average access time: {snapshot.average_access_time:.2f}s",
                triggered_at=datetime.now(),
                metadata={"access_time": snapshot.average_access_time}
            ))
        
        return issues
    
    def _generate_optimization_recommendations(self, snapshot: MemoryUsageSnapshot, 
                                             issues: List[MemoryAlert]) -> List[OptimizationRecommendation]:
        """Generate optimization recommendations."""
        recommendations = []
        
        # Memory usage recommendations
        utilization = self._calculate_memory_utilization(snapshot)
        if utilization > 0.7:  # 70% threshold
            recommendations.append(OptimizationRecommendation(
                recommendation_id=f"cleanup_{int(time.time())}",
                recommendation_type="cleanup",
                priority="high" if utilization > 0.9 else "medium",
                description="Clean up expired and stale memory entries",
                estimated_savings=int(snapshot.total_size * 0.2),  # Estimate 20% savings
                affected_keys=[],
                implementation_effort="low"
            ))
        
        # Cache performance recommendations
        if snapshot.cache_hit_rate < 0.7:
            recommendations.append(OptimizationRecommendation(
                recommendation_id=f"cache_opt_{int(time.time())}",
                recommendation_type="optimization",
                priority="medium",
                description="Optimize cache strategy and preload frequently accessed data",
                estimated_savings=0,  # Performance improvement, not space
                affected_keys=[],
                implementation_effort="medium"
            ))
        
        return recommendations
    
    def _identify_stale_data(self) -> List[str]:
        """Identify stale data that hasn't been accessed recently."""
        stale_keys = []
        try:
            # Query database for entries not accessed in the threshold period
            cutoff_date = datetime.now() - timedelta(days=self.alert_thresholds["stale_data_days"])
            
            cursor = self.memory_manager.conn.execute("""
                SELECT memory_key FROM memory_entries 
                WHERE last_accessed < ? OR last_accessed IS NULL
            """, (cutoff_date.isoformat(),))
            
            stale_keys = [row[0] for row in cursor.fetchall()]
            
        except Exception as e:
            print(f"Error identifying stale data: {e}")
        
        return stale_keys
    
    def _cleanup_stale_data(self, stale_keys: List[str]) -> int:
        """Clean up stale data."""
        cleaned_count = 0
        try:
            for key in stale_keys:
                cursor = self.memory_manager.conn.execute(
                    "DELETE FROM memory_entries WHERE memory_key = ?", (key,)
                )
                if cursor.rowcount > 0:
                    cleaned_count += 1
            
            self.memory_manager.conn.commit()
            
        except Exception as e:
            print(f"Error cleaning stale data: {e}")
        
        return cleaned_count
    
    def _identify_compressible_entries(self) -> List[str]:
        """Identify large entries that could benefit from compression."""
        # This would identify large uncompressed entries
        return []
    
    def _compress_large_entries(self, entries: List[str]) -> int:
        """Compress large memory entries."""
        # This would implement compression for large entries
        return 0
    
    def _identify_archivable_sessions(self) -> List[str]:
        """Identify old sessions that can be archived."""
        # This would identify old session data for archival
        return []
    
    def _archive_old_sessions(self, sessions: List[str]) -> int:
        """Archive old session data."""
        # This would implement session archival
        return 0
    
    def _defragment_storage(self) -> int:
        """Defragment storage to reduce fragmentation."""
        try:
            # SQLite VACUUM command to defragment
            self.memory_manager.conn.execute("VACUUM")
            return 1000  # Mock savings
        except Exception as e:
            print(f"Error defragmenting storage: {e}")
            return 0
    
    def _parse_size_string(self, size_str: str) -> int:
        """Parse size string like '100MB' to bytes."""
        if not size_str:
            return 0
        
        size_str = size_str.upper()
        if size_str.endswith('GB'):
            return int(size_str[:-2]) * 1024 * 1024 * 1024
        elif size_str.endswith('MB'):
            return int(size_str[:-2]) * 1024 * 1024
        elif size_str.endswith('KB'):
            return int(size_str[:-2]) * 1024
        else:
            return int(size_str)
    
    def _is_duplicate_alert(self, new_alert: MemoryAlert) -> bool:
        """Check if an alert is a duplicate of an existing active alert."""
        for existing_alert in self.active_alerts:
            if (existing_alert.alert_type == new_alert.alert_type and
                existing_alert.severity == new_alert.severity):
                # Consider it a duplicate if within 5 minutes
                time_diff = new_alert.triggered_at - existing_alert.triggered_at
                if time_diff.total_seconds() < 300:  # 5 minutes
                    return True
        return False
    
    def _cleanup_resolved_alerts(self):
        """Remove alerts that are no longer relevant."""
        # Keep alerts for 1 hour
        cutoff_time = datetime.now() - timedelta(hours=1)
        self.active_alerts = [
            alert for alert in self.active_alerts
            if alert.triggered_at >= cutoff_time
        ]
    
    def _store_monitoring_data(self, snapshot: MemoryUsageSnapshot):
        """Store monitoring data for historical analysis."""
        try:
            monitoring_key = f"global/monitoring/snapshot-{int(snapshot.timestamp.timestamp())}"
            self.memory_manager.store_memory(monitoring_key, asdict(snapshot))
        except Exception as e:
            print(f"Error storing monitoring data: {e}")
    
    def _calculate_memory_growth_trend(self, snapshots: List[MemoryUsageSnapshot]) -> Dict[str, Any]:
        """Calculate memory growth trend."""
        if len(snapshots) < 2:
            return {"trend": "insufficient_data"}
        
        sizes = [s.total_size for s in snapshots]
        growth_rate = (sizes[-1] - sizes[0]) / max(sizes[0], 1)
        
        return {
            "trend": "growing" if growth_rate > 0.1 else "stable" if growth_rate > -0.1 else "declining",
            "growth_rate": growth_rate,
            "initial_size": sizes[0],
            "final_size": sizes[-1]
        }
    
    def _calculate_cache_trend(self, snapshots: List[MemoryUsageSnapshot]) -> Dict[str, Any]:
        """Calculate cache performance trend."""
        if not snapshots:
            return {"trend": "no_data"}
        
        hit_rates = [s.cache_hit_rate for s in snapshots]
        avg_hit_rate = statistics.mean(hit_rates)
        
        return {
            "average_hit_rate": avg_hit_rate,
            "trend": "improving" if hit_rates[-1] > hit_rates[0] else "declining"
        }
    
    def _calculate_agent_activity_trends(self, snapshots: List[MemoryUsageSnapshot]) -> Dict[str, Any]:
        """Calculate agent activity trends."""
        # This would analyze agent activity patterns
        return {"trend": "analysis_pending"}
    
    def _identify_peak_usage_times(self, snapshots: List[MemoryUsageSnapshot]) -> List[str]:
        """Identify peak memory usage times."""
        if not snapshots:
            return []
        
        # Find times when usage was above 90th percentile
        sizes = [s.total_size for s in snapshots]
        threshold = statistics.quantile(sizes, 0.9) if len(sizes) >= 10 else max(sizes)
        
        peak_times = [
            s.timestamp.strftime("%Y-%m-%d %H:%M")
            for s in snapshots
            if s.total_size >= threshold
        ]
        
        return peak_times[-10:]  # Last 10 peak times
    
    def close(self):
        """Close the monitor and clean up resources."""
        self.running = False
        if self.monitor_thread.is_alive():
            self.monitor_thread.join(timeout=5)
        self.memory_manager.close()


# Utility functions
def create_memory_monitor(config_path: str = None) -> MemoryMonitor:
    """Create and initialize a memory monitor."""
    return MemoryMonitor(config_path)


def get_memory_health_report() -> Dict[str, Any]:
    """Get a quick memory health report."""
    monitor = MemoryMonitor()
    try:
        return monitor.check_memory_health()
    finally:
        monitor.close()


def optimize_memory_now(auto_apply: bool = False) -> Dict[str, Any]:
    """Perform immediate memory optimization."""
    monitor = MemoryMonitor()
    try:
        return monitor.optimize_memory_usage(auto_apply)
    finally:
        monitor.close()


if __name__ == "__main__":
    # Test the monitoring system
    monitor = MemoryMonitor()
    
    # Get current snapshot
    snapshot = monitor.get_current_usage_snapshot()
    print(f"Current memory usage: {snapshot.total_size} bytes, {snapshot.total_entries} entries")
    
    # Check health
    health = monitor.check_memory_health()
    print(f"Memory health: {health['overall_health']}")
    print(f"Issues found: {len(health['issues'])}")
    print(f"Recommendations: {len(health['recommendations'])}")
    
    # Test optimization
    optimization = monitor.optimize_memory_usage(auto_apply=True)
    print(f"Optimization results: {optimization}")
    
    monitor.close()