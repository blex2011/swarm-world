# SwarmWorld Unity Plugin Testing Guidelines

## Overview

This document outlines the comprehensive testing strategy for the SwarmWorld Unity Plugin, including unit testing, integration testing, performance testing, and quality assurance protocols.

## Testing Architecture

### Test Assembly Structure

```
Tests/
├── Runtime/
│   ├── Core/                    # Core component tests
│   ├── Performance/             # Performance and stress tests
│   ├── TestUtilities/           # Test data factories and utilities
│   └── SwarmWorld.Tests.Runtime.asmdef
├── Editor/
│   ├── Integration/             # Unity Editor integration tests
│   ├── UI/                      # Custom editor UI tests
│   └── SwarmWorld.Tests.Editor.asmdef
└── Documentation/
    ├── TestingGuidelines.md     # This file
    ├── TestResults/             # Test execution reports
    └── Coverage/                # Code coverage reports
```

### Testing Frameworks Used

- **NUnit**: Primary testing framework for unit and integration tests
- **Unity Test Framework**: Unity-specific testing capabilities
- **Unity Performance Testing**: Performance benchmarking and profiling
- **Custom Assertions**: Swarm-specific validation utilities

## Test Categories

### 1. Unit Tests

**Purpose**: Validate individual components in isolation

**Test Classes**:
- `SwarmAgentTests`: Core agent functionality
- `SwarmAgentDataTests`: Data structure validation
- `SwarmCoordinatorTests`: Coordination logic
- `MemoryManagerTests`: Memory management
- `NeuralLearnerTests`: Learning algorithm validation

**Coverage Requirements**:
- Minimum 90% code coverage
- All public methods tested
- Edge cases and error conditions covered
- Input validation verified

**Example Test Structure**:
```csharp
[Test]
public void SwarmAgent_SetVelocity_UpdatesMovement()
{
    // Arrange
    var agent = TestDataFactory.CreateTestAgent();
    var testVelocity = new float3(1, 0, 1);

    // Act
    agent.SetVelocity(testVelocity);

    // Assert
    Assert.AreEqual(testVelocity, agent.Velocity);
}
```

### 2. Integration Tests

**Purpose**: Validate component interactions and Unity Editor integration

**Test Classes**:
- `SwarmEditorIntegrationTests`: Editor functionality
- `SwarmSceneValidationTests`: Scene setup validation
- `SwarmPrefabTests`: Prefab creation and instantiation
- `SwarmAssetTests`: Asset import/export

**Key Areas**:
- Inspector property display
- Prefab instantiation
- Scene validation
- Asset pipeline integration
- Play mode transitions

### 3. Performance Tests

**Purpose**: Ensure scalability and performance requirements

**Test Classes**:
- `SwarmPerformanceTests`: Core performance benchmarks
- `SwarmMemoryProfileTests`: Memory usage and leak detection
- `SwarmStressTests`: Extreme load testing

**Performance Targets**:
- 60 FPS with 100 agents (standard configuration)
- 30 FPS with 500 agents (high-performance configuration)
- Memory allocation < 100MB for 1000 agents
- Neighbor finding < 1ms per agent (average)

**Benchmark Categories**:
```csharp
[Test, Performance]
[TestCase(10, Description = "Small swarm performance")]
[TestCase(50, Description = "Medium swarm performance")]
[TestCase(100, Description = "Large swarm performance")]
public void SwarmUpdate_Performance_ScalesWithAgentCount(int agentCount)
{
    // Performance measurement implementation
}
```

### 4. Stress Tests

**Purpose**: Find breaking points and validate stability

**Test Areas**:
- Maximum agent count handling
- Memory leak detection
- Long-running stability
- Resource exhaustion scenarios
- Concurrent operation safety

## Test Data Management

### Test Data Factory

The `TestDataFactory` class provides standardized test data creation:

```csharp
// Create test agents
var agent = TestDataFactory.CreateTestAgent("TestAgent", SwarmAgentData.Default);

// Create agent swarms
var swarm = TestDataFactory.CreateTestSwarm(50, SwarmFormation.Circle);

// Create mock components
var mockMemory = new TestDataFactory.MockMemoryManager();
var mockNeuralLearner = new TestDataFactory.MockNeuralLearner();
```

### Test Configurations

Pre-defined agent configurations for consistent testing:

- `SwarmAgentData.Default`: Standard balanced configuration
- `SwarmAgentData.HighPerformance`: Optimized for large swarms
- `SwarmAgentData.Research`: Enhanced learning capabilities
- `AgentDataVariants.FastAgent`: High-speed movement
- `AgentDataVariants.SocialAgent`: Strong flocking behavior

## Testing Best Practices

### 1. Test Organization

- **Arrange-Act-Assert**: Clear test structure
- **Descriptive Names**: Tests describe expected behavior
- **Single Responsibility**: One assertion per test when possible
- **Independent Tests**: No dependencies between tests

### 2. Mock and Stub Usage

```csharp
// Use mocks for external dependencies
var mockCoordinator = new Mock<SwarmCoordinator>();
mockCoordinator.Setup(c => c.FindNeighbors(It.IsAny<SwarmAgent>(), ref It.Ref<NativeArray<SwarmNeighbor>>.IsAny))
              .Callback<SwarmAgent, NativeArray<SwarmNeighbor>>((agent, neighbors) => {
                  // Mock neighbor finding behavior
              });
```

### 3. Performance Test Guidelines

- **Warmup Runs**: Always include warmup iterations
- **Multiple Measurements**: Take multiple samples for accuracy
- **Baseline Comparisons**: Compare against previous results
- **Environment Control**: Consistent test environment

### 4. Error Handling Tests

```csharp
[Test]
public void SwarmAgent_InvalidData_HandlesGracefully()
{
    var invalidData = new SwarmAgentData { maxSpeed = -1f };
    
    Assert.DoesNotThrow(() => agent.SetAgentData(invalidData));
    Assert.IsTrue(agent.Data.IsValid()); // Should auto-correct
}
```

## Custom Assertions

### SwarmAssert Utility

Specialized assertions for swarm validation:

```csharp
// Validate agent positioning
SwarmAssert.AgentsWithinBounds(agents, testBounds);

// Verify movement
SwarmAssert.AgentsMoving(agents, minSpeed: 0.1f);

// Check collision avoidance
SwarmAssert.AgentsNotOverlapping(agents, minDistance: 0.5f);

// Performance validation
SwarmAssert.PerformanceWithinBounds(performance, minFPS: 30f);
```

## Continuous Integration

### GitHub Actions Workflow

```yaml
name: Unity Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - uses: game-ci/unity-test-runner@v2
      with:
        unityVersion: 2022.3.0f1
        testMode: all
        artifactsPath: test-results
        githubToken: ${{ secrets.GITHUB_TOKEN }}
    
    - uses: actions/upload-artifact@v2
      with:
        name: Test Results
        path: test-results
```

### Test Execution Pipeline

1. **Static Analysis**: Code quality checks
2. **Unit Tests**: Core functionality validation
3. **Integration Tests**: Editor and scene tests
4. **Performance Tests**: Benchmark execution
5. **Coverage Analysis**: Code coverage reporting
6. **Documentation Generation**: Auto-generate test docs

## Test Coverage Requirements

### Minimum Coverage Targets

- **Overall**: 85% line coverage
- **Core Components**: 95% line coverage
- **Public APIs**: 100% method coverage
- **Critical Paths**: 100% branch coverage

### Coverage Exclusions

- Unity Editor-only code (when appropriate)
- Third-party integrations
- Platform-specific code
- Debug/development utilities

## Test Environment Setup

### Unity Version Requirements

- **Primary**: Unity 2022.3 LTS
- **Supported**: Unity 2021.3 LTS, Unity 2023.1+
- **Test Platforms**: Windows, macOS, Linux

### Required Packages

```json
{
  "com.unity.test-framework": "1.1.33",
  "com.unity.performance.profile-analyzer": "1.2.2",
  "com.unity.collections": "2.1.4",
  "com.unity.burst": "1.8.4",
  "com.unity.jobs": "0.70.0-preview.7"
}
```

### Test Project Configuration

```csharp
// Test runner settings
[assembly: TestRunnerSettings(
    TestRunMode.EditMode | TestRunMode.PlayMode,
    TimeoutMs = 30000,
    ParallelExecution = true
)]
```

## Debugging Failed Tests

### Common Issues and Solutions

1. **Timing Issues**
   - Use `yield return new WaitForFixedUpdate()` for physics-dependent tests
   - Add appropriate delays for asynchronous operations

2. **Memory Leaks**
   - Always dispose NativeArrays in teardown
   - Use `TestCleanup.DestroyTestObjects()` utility

3. **Platform Differences**
   - Test on multiple platforms
   - Use conditional compilation for platform-specific code

4. **Performance Variations**
   - Account for hardware differences in performance tests
   - Use relative performance comparisons

### Test Debugging Tools

```csharp
// Enable debug output
[Test]
public void DebugTest()
{
    Debug.Log("Test debug information");
    TestContext.WriteLine("Additional test output");
}

// Performance profiling
[Test, Performance]
public void ProfiledTest()
{
    using (Measure.ProfilerMarkers("CustomMarker"))
    {
        // Test code with profiling
    }
}
```

## Test Documentation

### Test Result Reporting

- **Format**: NUnit XML + HTML reports
- **Metrics**: Pass/fail rates, execution time, coverage
- **Trends**: Historical performance tracking
- **Artifacts**: Screenshots, logs, profiler data

### Test Maintenance

- **Review Schedule**: Monthly test review
- **Maintenance Tasks**: Update test data, remove obsolete tests
- **Performance Baselines**: Quarterly baseline updates
- **Documentation Updates**: Keep guidelines current

## Quality Gates

### Pre-commit Requirements

- All existing tests pass
- New code has corresponding tests
- Code coverage maintained
- Performance tests within acceptable range

### Release Criteria

- 100% test pass rate
- Performance benchmarks met
- Memory leak tests pass
- Integration tests validate in clean environment

## Contact and Support

For questions about testing procedures or test failures:

- **Documentation**: See inline code comments and XML documentation
- **Examples**: Check `Samples~/` directory for usage examples
- **Issues**: Report test-related issues through the project issue tracker

---

*This testing guide is maintained alongside the SwarmWorld Unity Plugin development and should be updated with any changes to testing procedures or requirements.*