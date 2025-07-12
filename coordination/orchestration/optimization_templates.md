# Swarm Coordination Optimization Templates

## 🎯 Optimized TodoWrite Batch Template

### ✅ MANDATORY TodoWrite Structure
```javascript
// ALWAYS use this structure - NEVER split across multiple calls
TodoWrite({ 
  todos: [
    // PRIORITY: HIGH - Critical path items
    {
      id: "init_coordination",
      content: "Initialize swarm coordination with memory persistence", 
      status: "completed",
      priority: "high",
      dependencies: [],
      estimatedTime: 5,
      agentAssignment: "coordinator",
      parallelizable: false,
      criticality: "blocking",
      tags: ["coordination", "initialization"]
    },
    {
      id: "analyze_requirements", 
      content: "Analyze project requirements and technical constraints",
      status: "in_progress",
      priority: "high", 
      dependencies: ["init_coordination"],
      estimatedTime: 30,
      agentAssignment: "analyst",
      parallelizable: false,
      criticality: "blocking",
      tags: ["analysis", "requirements"]
    },
    
    // PRIORITY: HIGH - Parallel implementation tasks
    {
      id: "design_architecture",
      content: "Design system architecture and component structure",
      status: "pending",
      priority: "high",
      dependencies: ["analyze_requirements"],
      estimatedTime: 45,
      agentAssignment: "architect", 
      parallelizable: true,
      criticality: "blocking",
      tags: ["design", "architecture"]
    },
    {
      id: "implement_core",
      content: "Implement core functionality and primary features",
      status: "pending", 
      priority: "high",
      dependencies: ["design_architecture"],
      estimatedTime: 120,
      agentAssignment: "coder",
      parallelizable: true,
      criticality: "blocking", 
      tags: ["implementation", "core"]
    },
    {
      id: "create_tests",
      content: "Create comprehensive test suite and validation",
      status: "pending",
      priority: "high",
      dependencies: ["design_architecture"],
      estimatedTime: 60,
      agentAssignment: "tester",
      parallelizable: true,
      criticality: "normal",
      tags: ["testing", "validation"]
    },
    
    // PRIORITY: MEDIUM - Supporting tasks
    {
      id: "setup_monitoring",
      content: "Setup performance monitoring and metrics collection",
      status: "pending",
      priority: "medium",
      dependencies: ["implement_core"],
      estimatedTime: 30,
      agentAssignment: "analyst",
      parallelizable: true, 
      criticality: "normal",
      tags: ["monitoring", "performance"]
    },
    {
      id: "optimize_performance",
      content: "Optimize performance and resolve bottlenecks",
      status: "pending",
      priority: "medium", 
      dependencies: ["setup_monitoring"],
      estimatedTime: 45,
      agentAssignment: "analyst",
      parallelizable: true,
      criticality: "normal",
      tags: ["optimization", "performance"]
    },
    {
      id: "code_review",
      content: "Conduct comprehensive code review and quality assurance",
      status: "pending",
      priority: "medium",
      dependencies: ["implement_core"],
      estimatedTime: 30,
      agentAssignment: "reviewer",
      parallelizable: true,
      criticality: "normal", 
      tags: ["review", "quality"]
    },
    
    // PRIORITY: LOW - Final tasks
    {
      id: "create_documentation",
      content: "Create comprehensive documentation and usage guides",
      status: "pending",
      priority: "low",
      dependencies: ["code_review"],
      estimatedTime: 60,
      agentAssignment: "coder",
      parallelizable: true,
      criticality: "optional",
      tags: ["documentation", "guides"]
    },
    {
      id: "deployment_prep",
      content: "Prepare deployment configuration and CI/CD pipeline",
      status: "pending", 
      priority: "low",
      dependencies: ["optimize_performance"],
      estimatedTime: 40,
      agentAssignment: "coder",
      parallelizable: true,
      criticality: "optional", 
      tags: ["deployment", "cicd"]
    }
  ]
})
```

## 🚀 Optimized Parallel Task Spawning Template

### ✅ MANDATORY Parallel Agent Spawning
```javascript
// ALWAYS spawn ALL agents in ONE message - NEVER sequential
[BatchedMessage]: {
  // Coordination setup (MCP tools)
  mcp__claude-flow__swarm_init({ 
    topology: "hierarchical", 
    maxAgents: 6, 
    strategy: "parallel"
  }),
  
  // Agent spawning (ALL AGENTS IN ONE CALL)
  Task(`You are the COORDINATOR agent in a coordinated swarm.

MANDATORY COORDINATION PROTOCOL:
1. START: Run \`npx claude-flow@alpha hooks pre-task --description "Project coordination and workflow management"\`
2. DURING: After EVERY operation, run \`npx claude-flow@alpha hooks post-edit --memory-key "coordinator/step"\`
3. MEMORY: Store ALL decisions using \`npx claude-flow@alpha hooks notify --message "[decision]"\`
4. END: Run \`npx claude-flow@alpha hooks post-task --task-id "coordination" --analyze-performance true\`

Your specific task: Overall project coordination, cross-agent communication, workflow optimization

REMEMBER: Coordinate with other agents through shared memory before making decisions!`),

  Task(`You are the ARCHITECT agent in a coordinated swarm.

MANDATORY COORDINATION PROTOCOL:  
1. START: Run \`npx claude-flow@alpha hooks pre-task --description "System architecture design and technical planning"\`
2. DURING: After EVERY operation, run \`npx claude-flow@alpha hooks post-edit --memory-key "architect/step"\`
3. MEMORY: Store ALL decisions using \`npx claude-flow@alpha hooks notify --message "[decision]"\`
4. END: Run \`npx claude-flow@alpha hooks post-task --task-id "architecture" --analyze-performance true\`

Your specific task: System design, architecture decisions, technical planning, component structure

REMEMBER: Coordinate with other agents through shared memory before making decisions!`),

  Task(`You are the CODER agent in a coordinated swarm.

MANDATORY COORDINATION PROTOCOL:
1. START: Run \`npx claude-flow@alpha hooks pre-task --description "Code implementation and feature development"\`  
2. DURING: After EVERY operation, run \`npx claude-flow@alpha hooks post-edit --memory-key "coder/step"\`
3. MEMORY: Store ALL decisions using \`npx claude-flow@alpha hooks notify --message "[decision]"\`
4. END: Run \`npx claude-flow@alpha hooks post-task --task-id "implementation" --analyze-performance true\`

Your specific task: Implementation, code generation, technical execution, feature development

REMEMBER: Coordinate with other agents through shared memory before making decisions!`),

  Task(`You are the ANALYST agent in a coordinated swarm.

MANDATORY COORDINATION PROTOCOL:
1. START: Run \`npx claude-flow@alpha hooks pre-task --description "Performance analysis and optimization"\`
2. DURING: After EVERY operation, run \`npx claude-flow@alpha hooks post-edit --memory-key "analyst/step"\`  
3. MEMORY: Store ALL decisions using \`npx claude-flow@alpha hooks notify --message "[decision]"\`
4. END: Run \`npx claude-flow@alpha hooks post-task --task-id "analysis" --analyze-performance true\`

Your specific task: Data analysis, performance optimization, metrics collection, bottleneck identification

REMEMBER: Coordinate with other agents through shared memory before making decisions!`),

  Task(`You are the TESTER agent in a coordinated swarm.

MANDATORY COORDINATION PROTOCOL:
1. START: Run \`npx claude-flow@alpha hooks pre-task --description "Quality assurance and testing"\`
2. DURING: After EVERY operation, run \`npx claude-flow@alpha hooks post-edit --memory-key "tester/step"\`
3. MEMORY: Store ALL decisions using \`npx claude-flow@alpha hooks notify --message "[decision]"\`  
4. END: Run \`npx claude-flow@alpha hooks post-task --task-id "testing" --analyze-performance true\`

Your specific task: Quality assurance, testing, validation, test suite creation

REMEMBER: Coordinate with other agents through shared memory before making decisions!`),

  Task(`You are the REVIEWER agent in a coordinated swarm.

MANDATORY COORDINATION PROTOCOL:
1. START: Run \`npx claude-flow@alpha hooks pre-task --description "Code review and quality validation"\`
2. DURING: After EVERY operation, run \`npx claude-flow@alpha hooks post-edit --memory-key "reviewer/step"\`
3. MEMORY: Store ALL decisions using \`npx claude-flow@alpha hooks notify --message "[decision]"\`
4. END: Run \`npx claude-flow@alpha hooks post-task --task-id "review" --analyze-performance true\`

Your specific task: Code review, quality validation, best practices enforcement

REMEMBER: Coordinate with other agents through shared memory before making decisions!`)
}
```

## 💾 Memory Coordination Template

### ✅ MANDATORY Memory Structure
```javascript
// Standardized memory key patterns
const memoryKeys = {
  // Agent-specific progress
  agentProgress: "swarm-{id}/agent-{name}/{step}",
  
  // Cross-agent decisions  
  decisions: "swarm-{id}/decisions/{timestamp}-{agent}",
  
  // System state
  systemState: "swarm-{id}/state/{component}",
  
  // Coordination checkpoints
  coordination: "swarm-{id}/coordination/{interaction}",
  
  // Performance metrics
  performance: "swarm-{id}/metrics/{agent}/{operation}",
  
  // Task dependencies
  dependencies: "swarm-{id}/dependencies/{task_id}",
  
  // Workflow status
  workflow: "swarm-{id}/workflow/{status}"
};

// MANDATORY coordination checks before any major operation
const coordinationChecks = {
  beforeDecision: `npx claude-flow@alpha hooks notify --message "Checking agent coordination before decision: {decision_type}"`,
  beforeImplementation: `npx claude-flow@alpha hooks notify --message "Verifying no duplicate work: {implementation_area}"`,
  beforeCompletion: `npx claude-flow@alpha hooks notify --message "Ensuring dependencies satisfied: {task_id}"`,
  afterCompletion: `npx claude-flow@alpha hooks notify --message "Task completed, updating coordination state: {task_id}"`
};
```

## 🔧 Performance Monitoring Template

### ✅ MANDATORY Performance Tracking
```javascript
// Performance monitoring hooks
const performanceHooks = {
  taskStart: `npx claude-flow@alpha hooks pre-task --description "{task}" --enable-timing true`,
  
  operationTracking: `npx claude-flow@alpha hooks post-edit --file "{file}" --memory-key "{key}" --track-performance true`,
  
  coordinationMetrics: `npx claude-flow@alpha hooks notify --message "Coordination checkpoint: {metric}" --include-timing true`,
  
  taskCompletion: `npx claude-flow@alpha hooks post-task --task-id "{id}" --analyze-performance true --save-metrics true`
};

// Optimization thresholds
const optimizationThresholds = {
  maxTaskDuration: 300, // 5 minutes
  maxCoordinationDelay: 30, // 30 seconds  
  minParallelEfficiency: 0.8, // 80%
  maxMemoryInconsistency: 0.05 // 5%
};
```

## 🛡️ Self-Healing Workflow Template

### ✅ MANDATORY Failure Detection & Recovery
```javascript
// Self-healing workflow patterns
const selfHealingPatterns = {
  // Detect stuck agents
  stuckAgentDetection: {
    timeout: 300, // 5 minutes
    action: "npx claude-flow@alpha hooks notify --message 'Agent timeout detected, initiating recovery' --level 'warning'"
  },
  
  // Memory inconsistency repair
  memoryRepair: {
    validator: "cross_agent_state_comparison", 
    action: "npx claude-flow@alpha hooks notify --message 'Memory inconsistency detected, synchronizing' --level 'warning'"
  },
  
  // Coordination breakdown recovery
  coordinationRecovery: {
    indicator: "duplicate_work_detection",
    action: "npx claude-flow@alpha hooks notify --message 'Coordination breakdown, resetting agent assignments' --level 'error'"
  },
  
  // Performance degradation response
  performanceRecovery: {
    threshold: "50%_performance_drop",
    action: "npx claude-flow@alpha hooks notify --message 'Performance degradation, optimizing workflow' --level 'warning'"
  }
};
```

## 📋 Implementation Checklist

### Before Starting Any Swarm Task:
- [ ] Use TodoWrite with 5-10+ todos in ONE call
- [ ] Spawn ALL agents in ONE message with coordination instructions
- [ ] Establish memory coordination structure
- [ ] Enable performance monitoring hooks
- [ ] Set up self-healing workflow patterns

### During Task Execution:
- [ ] Use hooks after EVERY file operation
- [ ] Store decisions in shared memory
- [ ] Check coordination before major decisions
- [ ] Monitor performance metrics
- [ ] Validate workflow progress

### After Task Completion:
- [ ] Run post-task hooks with performance analysis
- [ ] Update coordination state
- [ ] Store lessons learned
- [ ] Generate workflow metrics
- [ ] Prepare for next iteration

This template system ensures maximum efficiency and coordination in swarm workflow execution.