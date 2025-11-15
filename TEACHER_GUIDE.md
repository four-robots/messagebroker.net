# Teacher/Instructor Guide - MessageBroker.NET Work Items

**Audience:** Educators, instructors, and mentors using MessageBroker.NET for teaching software engineering concepts
**Date:** 2025-11-15
**Purpose:** Guide for using this project's work items and implementation patterns as teaching material

## Overview

This guide explains how to use the MessageBroker.NET project and its work item tracking system (TODO_NATS_FEATURES.md) as an educational resource for teaching:

- Software architecture and layered design
- Full-stack implementation (native bindings → high-level API)
- Test-driven development
- Documentation practices
- Real-world open-source development workflows

## Using This Project for Teaching

### 1. **Architecture & Design Patterns**

#### Teaching Objective
Demonstrate multi-layer architecture with clear separation of concerns.

#### What Students Should Learn
- How to design a system with multiple abstraction layers
- P/Invoke and native interop in .NET
- Interface-based design and dependency injection
- Thread safety and concurrent programming

#### Example Lesson Plan

**Lesson 1: Understanding the Layer Architecture**
1. Show students the call flow diagram from MONITORING_IMPLEMENTATION_SUMMARY.md
2. Have students trace a single operation (e.g., GetConnzAsync) through all layers:
   - User code → High-level API → C# bindings → Go bindings → NATS server
3. Discussion: Why multiple layers? What are the benefits and tradeoffs?

**Lesson 2: P/Invoke and Native Interop**
1. Show the Go export functions in `native/nats-bindings.go`
2. Show the corresponding DllImport declarations in `NatsBindings.cs`
3. Have students implement a simple new monitoring endpoint
4. Discussion: Memory management, marshaling, error handling across boundaries

**Lesson 3: Thread Safety**
1. Show the mutex usage in Go (`serverMu.Lock()`)
2. Show the semaphore usage in C# (`_operationSemaphore.WaitAsync()`)
3. Have students identify race conditions if locks were removed
4. Exercise: Add logging to demonstrate concurrent access patterns

### 2. **Test-Driven Development (TDD)**

#### Teaching Objective
Show how comprehensive testing validates multi-layer implementations.

#### What Students Should Learn
- Integration testing vs unit testing
- Test structure and organization
- Validation patterns for JSON responses
- Resource cleanup in tests (IDisposable pattern)

#### Example Lesson Plan

**Lesson 1: Reading and Understanding Tests**
1. Walk through `MonitoringTests.cs` test suite
2. Identify the AAA pattern (Arrange, Act, Assert) in each test
3. Show how tests validate JSON structure without tight coupling
4. Discussion: What makes a good integration test?

**Lesson 2: Writing New Tests**
1. Show students the test template from TestConnzMonitoring
2. Have students write a new test for an existing feature
3. Have students write a test for a NOT-YET-IMPLEMENTED feature (Phase 2+)
4. Discussion: Test-first development approach

**Lesson 3: Test Infrastructure**
1. Show the TestResults class and how it tracks test outcomes
2. Show the IntegrationTestRunner and test suite registration
3. Have students add their test to the runner
4. Exercise: Create a simple test framework from scratch

### 3. **Documentation Practices**

#### Teaching Objective
Demonstrate professional documentation standards.

#### What Students Should Learn
- XML documentation comments in C#
- README and guide writing
- API documentation
- Work item tracking and project planning

#### Example Lesson Plan

**Lesson 1: Code Documentation**
1. Show XML comments in NatsController.cs
2. Show how IntelliSense uses these comments
3. Have students add XML comments to undocumented methods
4. Discussion: What makes good API documentation?

**Lesson 2: Project Documentation**
1. Review CLAUDE.md, TODO_NATS_FEATURES.md, MONITORING_IMPLEMENTATION_SUMMARY.md
2. Identify different documentation types: developer guide, API reference, implementation summary
3. Have students create a new document (e.g., TROUBLESHOOTING.md)
4. Discussion: Documentation as a first-class artifact

**Lesson 3: Work Item Tracking**
1. Show TODO_NATS_FEATURES.md structure
2. Explain the roadmap and phase organization
3. Have students create a work item for a new feature
4. Exercise: Break down a complex feature into a phase with multiple work items

### 4. **Implementing New Features (Guided Exercise)**

#### Teaching Objective
Guide students through implementing a complete new feature from work items.

#### Suggested Feature: Accountz() Implementation
This is listed as "not started" in Phase 3, making it perfect for teaching.

#### Step-by-Step Implementation Guide

**Phase 1: Planning (1 hour)**
1. Review the work item in TODO_NATS_FEATURES.md:
   ```
   #### ⏳ Accountz() - Account Monitoring
   - **Purpose**: Account-level monitoring
   - **Returns**: List of accounts, connection counts, subscription counts
   - **Use Case**: Multi-tenant monitoring
   - **Priority**: MEDIUM
   ```
2. Research the NATS server Accountz API documentation
3. Design the implementation plan:
   - Go binding signature
   - C# binding signature
   - High-level API signature
   - Test scenarios
   - Example usage

**Phase 2: Go Bindings Implementation (2 hours)**
1. Create the export function in `native/nats-bindings.go`:
   ```go
   //export GetAccountz
   func GetAccountz(accountName *C.char) *C.char {
       serverMu.Lock()
       defer serverMu.Unlock()

       srv, exists := natsServers[currentPort]
       if !exists || srv == nil {
           return C.CString("ERROR: Server not running")
       }

       opts := &server.AccountzOptions{}
       if accountName != nil {
           acctStr := C.GoString(accountName)
           if acctStr != "" {
               opts.Account = acctStr
           }
       }

       accountz, err := srv.Accountz(opts)
       if err != nil {
           return C.CString(fmt.Sprintf("ERROR: Failed to get account info: %v", err))
       }

       jsonBytes, err := json.Marshal(accountz)
       if err != nil {
           return C.CString(fmt.Sprintf("ERROR: Failed to marshal account info: %v", err))
       }

       return C.CString(string(jsonBytes))
   }
   ```

2. Build and test the Go bindings:
   ```bash
   cd native
   ./build.sh
   ```

**Phase 3: C# Bindings Implementation (1 hour)**
1. Add to INatsBindings interface:
   ```csharp
   IntPtr GetAccountz(string? accountName);
   ```

2. Add Windows binding:
   ```csharp
   [DllImport("nats-bindings.dll", EntryPoint = "GetAccountz")]
   internal static extern IntPtr _getAccountz(string? accountName);

   public IntPtr GetAccountz(string? accountName) => _getAccountz(accountName);
   ```

3. Add Linux binding (same pattern with .so)

**Phase 4: High-Level API Implementation (1 hour)**
1. Add method to NatsController.cs:
   ```csharp
   /// <summary>
   /// Retrieves account monitoring information from the NATS server.
   /// </summary>
   /// <param name="accountName">Optional account name filter</param>
   /// <param name="cancellationToken">Cancellation token</param>
   /// <returns>JSON string containing account monitoring data</returns>
   public async Task<string> GetAccountzAsync(string? accountName = null, CancellationToken cancellationToken = default)
   {
       await _operationSemaphore.WaitAsync(cancellationToken);
       try
       {
           EnsureRunning();

           var ptr = _bindings.GetAccountz(accountName);
           var result = Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
           _bindings.FreeString(ptr);

           if (result.StartsWith("ERROR:"))
           {
               throw new InvalidOperationException(result);
           }

           return await Task.FromResult(result);
       }
       finally
       {
           _operationSemaphore.Release();
       }
   }
   ```

**Phase 5: Integration Tests (2 hours)**
1. Create test method in MonitoringTests.cs:
   ```csharp
   public static async Task<bool> TestAccountzMonitoring()
   {
       Console.WriteLine("\n=== Testing Accountz (Account Monitoring) ===");

       using var controller = new NatsController();

       var config = new BrokerConfiguration
       {
           Host = "127.0.0.1",
           Port = 4230,
           Description = "Accountz monitoring test"
       };

       var result = await controller.ConfigureAsync(config);
       if (!result.Success)
       {
           Console.WriteLine($"❌ Failed to start server: {result.ErrorMessage}");
           return false;
       }

       await Task.Delay(500);

       try
       {
           var accountz = await controller.GetAccountzAsync();
           Console.WriteLine($"✓ Retrieved Accountz data");

           using var doc = JsonDocument.Parse(accountz);
           var root = doc.RootElement;

           if (root.TryGetProperty("accounts", out var accounts))
           {
               Console.WriteLine($"  Number of accounts: {accounts.GetArrayLength()}");
           }

           Console.WriteLine("✓ Accountz test passed");
           return true;
       }
       catch (Exception ex)
       {
           Console.WriteLine($"❌ Accountz test failed: {ex.Message}");
           return false;
       }
       finally
       {
           await controller.ShutdownAsync();
       }
   }
   ```

2. Register test in MonitoringTestSuite:
   ```csharp
   await results.AssertAsync("Accountz Monitoring", MonitoringTests.TestAccountzMonitoring);
   ```

**Phase 6: Example Implementation (1 hour)**
1. Add to MonitoringExample.cs or create new AccountMonitoringExample.cs
2. Add menu option in Program.cs
3. Update menu display

**Phase 7: Documentation Updates (1 hour)**
1. Update TODO_NATS_FEATURES.md to mark as implemented
2. Add entry to MONITORING_IMPLEMENTATION_SUMMARY.md
3. Update CLAUDE.md with new feature
4. Commit with descriptive message

**Phase 8: Code Review and Discussion (1 hour)**
1. Review the complete implementation
2. Discuss what went well and what could be improved
3. Identify edge cases and potential bugs
4. Plan for future enhancements

### 5. **Version Control and Collaboration**

#### Teaching Objective
Show professional Git workflows and collaboration practices.

#### What Students Should Learn
- Feature branch workflows
- Commit message conventions
- Pull request descriptions
- Code review process

#### Example Lesson Plan

**Lesson 1: Git Workflow**
1. Show the branch naming convention (claude/*)
2. Show commit messages in git log
3. Have students create a feature branch for their implementation
4. Discussion: Why feature branches? What about main/master protection?

**Lesson 2: Commits and History**
1. Show git log for the monitoring features
2. Identify atomic commits vs. large monolithic commits
3. Have students commit their work with descriptive messages
4. Exercise: Use git bisect to find when a feature was added

**Lesson 3: Pull Requests (if using GitHub/GitLab)**
1. Show PR template or examples
2. Explain PR description best practices
3. Have students create PRs for their implementations
4. Peer review exercise

### 6. **Performance and Profiling**

#### Teaching Objective
Introduce performance considerations in multi-layer architectures.

#### What Students Should Learn
- P/Invoke overhead
- JSON serialization performance
- Lock contention
- Async/await patterns

#### Example Lesson Plan

**Lesson 1: Benchmarking**
1. Create a simple benchmark for GetConnzAsync
2. Measure time across 1000 calls
3. Identify the bottleneck layers
4. Discussion: When does performance matter?

**Lesson 2: Memory Profiling**
1. Use a profiler to track memory allocations
2. Identify string allocations from JSON serialization
3. Show memory cleanup with FreeString
4. Discussion: Memory leaks in native interop

**Lesson 3: Async Patterns**
1. Show why async is used throughout the API
2. Demonstrate the cost of Task.FromResult
3. Have students measure true async vs. sync-over-async
4. Exercise: Convert a synchronous API to async

## Sample Course Outline

### Week 1: Introduction and Architecture
- Day 1: Project overview, clone, build, run examples
- Day 2: Architecture deep dive, layer analysis
- Day 3: P/Invoke and native interop
- Day 4: Thread safety and concurrency
- Day 5: Quiz and discussion

### Week 2: Testing and Documentation
- Day 1: Integration testing overview
- Day 2: Writing tests for existing features
- Day 3: Documentation standards
- Day 4: Work item tracking and planning
- Day 5: Quiz and discussion

### Week 3: Feature Implementation
- Day 1-2: Plan Accountz() implementation
- Day 3-4: Implement Go and C# bindings
- Day 5: Code review and debugging

### Week 4: Advanced Topics and Polish
- Day 1: High-level API and tests
- Day 2: Examples and documentation
- Day 3: Performance analysis
- Day 4: Final code review and presentations
- Day 5: Project retrospective

## Assessment Ideas

### Individual Assignments
1. **Implement a new monitoring endpoint** (Accountz, AccountStatz, Gatewayz)
   - Criteria: Completeness, code quality, tests, documentation

2. **Add a new feature to an existing endpoint**
   - Example: Add pagination to Connz
   - Criteria: Backward compatibility, API design, performance

3. **Write comprehensive documentation**
   - Choose an undocumented feature
   - Create API docs, examples, and tests
   - Criteria: Clarity, completeness, accuracy

### Group Projects
1. **Implement Phase 2: Account Management**
   - Divide work among team members
   - Requires coordination and code review
   - Assessment: Teamwork, integration, final quality

2. **Create a monitoring dashboard**
   - Use the monitoring APIs to build a web dashboard
   - Technology: Blazor, React, or your choice
   - Assessment: Creativity, usability, code quality

3. **Performance optimization challenge**
   - Profile the monitoring endpoints
   - Identify and fix bottlenecks
   - Assessment: Analysis depth, improvement achieved

## Tips for Instructors

### Before Starting
1. ✅ **Build the project first** - Ensure native bindings build successfully
2. ✅ **Run all tests** - Verify all integration tests pass
3. ✅ **Try all examples** - Familiarize yourself with the interactive menu
4. ✅ **Read all documentation** - Understand the full context

### During Teaching
1. 💡 **Start simple** - Begin with reading code, then move to writing
2. 💡 **Use examples liberally** - The examples are great teaching aids
3. 💡 **Encourage questions** - This is complex, multi-layer code
4. 💡 **Pair programming** - Great for P/Invoke and debugging
5. 💡 **Live coding** - Demonstrate the build-test-debug cycle

### Common Student Challenges
1. **P/Invoke confusion** - Spend extra time on memory marshaling
2. **Async/await patterns** - Many students struggle with async
3. **Build errors** - Help with native binding build issues
4. **JSON parsing** - System.Text.Json can be tricky
5. **Git workflows** - Practice branch and merge strategies

### Extension Activities
1. **Add Windows-specific features** - Use Windows Event Log
2. **Add Linux-specific features** - Use systemd integration
3. **Create a CLI tool** - Command-line monitoring tool
4. **Write a tutorial blog post** - Great for deeper understanding
5. **Contribute to NATS ecosystem** - Real open-source contribution

## Resources for Students

### Required Reading
1. CLAUDE.md - Project overview and conventions
2. TODO_NATS_FEATURES.md - Work items and roadmap
3. MONITORING_IMPLEMENTATION_SUMMARY.md - Phase 1 reference implementation
4. docs/ARCHITECTURE.md - Deep architecture dive

### Optional Reading
1. docs/API_DESIGN.md - Complete API reference
2. docs/GETTING_STARTED.md - Initial setup guide
3. native/README.md - Native bindings documentation

### External Resources
1. [NATS Documentation](https://docs.nats.io/) - Official NATS docs
2. [.NET P/Invoke Guide](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke)
3. [Go cgo Documentation](https://golang.org/cmd/cgo/)
4. [C# Async/Await](https://docs.microsoft.com/en-us/dotnet/csharp/async)

## Conclusion

MessageBroker.NET is an excellent teaching project because it:
- ✅ Demonstrates real-world architecture patterns
- ✅ Shows professional development practices
- ✅ Includes comprehensive testing and documentation
- ✅ Has clear work items for student implementation
- ✅ Bridges multiple languages (Go, C#, C)
- ✅ Teaches valuable skills (native interop, async, testing)

Students who complete work items from TODO_NATS_FEATURES.md will gain practical experience in:
- Multi-layer architecture design
- Native interop and P/Invoke
- Test-driven development
- Professional documentation
- Real-world problem solving

## Contact and Support

For questions about using this project in education:
- Check the project documentation first
- Review existing implementations as examples
- Use the work items as guided exercises
- Encourage students to read the code

---

**Document Version:** 1.0
**Last Updated:** 2025-11-15
**Maintained By:** MessageBroker.NET Contributors
