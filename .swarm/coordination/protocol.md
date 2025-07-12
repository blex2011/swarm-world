# SWARM COORDINATION PROTOCOL

## MANDATORY COORDINATION SEQUENCE

Every agent MUST follow this exact protocol:

### 1️⃣ PRE-TASK (BEFORE starting work)
```bash
npx claude-flow@alpha hooks pre-task --description "[agent-specific-task]"
npx claude-flow@alpha hooks session-restore --session-id "swarm-1752330915929" --load-memory true
```

### 2️⃣ DURING WORK (After EVERY major step)
```bash
# After each file operation or decision
npx claude-flow@alpha hooks post-edit --file "[filepath]" --memory-key "swarm/[agent-id]/[step]"

# Store all decisions
npx claude-flow@alpha hooks notification --message "[decision-made]" --telemetry true

# Check coordination before major actions
npx claude-flow@alpha hooks pre-search --query "[what-to-check]" --cache-results true
```

### 3️⃣ POST-TASK (AFTER completing work)
```bash
npx claude-flow@alpha hooks post-task --task-id "[agent-task]" --analyze-performance true
npx claude-flow@alpha hooks session-end --export-metrics true --generate-summary true
```

## SWARM HIERARCHY

```
SWARM_COORDINATOR (ROOT)
├── ARCHITECT: System Designer
├── COORDINATOR: Task Orchestrator  
├── ANALYST: Performance Analyst
├── RESEARCHER: Pattern Researcher
├── MEMORY_MANAGER: Shared Memory
├── MONITOR: Execution Monitor
└── OPTIMIZER: Performance Optimizer
```

## COORDINATION RULES

1. **MEMORY FIRST**: Always check memory before making decisions
2. **HOOKS MANDATORY**: Every agent MUST use claude-flow hooks
3. **PARALLEL ONLY**: No sequential execution after swarm init
4. **BATCH OPERATIONS**: Multiple related operations in single message
5. **COORDINATION SYNC**: Store all decisions in shared memory

## AGENT RESPONSIBILITIES

- **ARCHITECT**: Design and plan system structure
- **COORDINATOR**: Distribute and orchestrate tasks  
- **ANALYST**: Track performance and metrics
- **RESEARCHER**: Find patterns and best practices
- **MEMORY_MANAGER**: Manage cross-agent communication
- **MONITOR**: Watch execution effectiveness
- **OPTIMIZER**: Improve swarm performance