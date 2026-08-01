# Testing Guide - Ticketing System

**Last Updated:** November 27, 2025  
**Status:** Backend testing active, Frontend testing deferred

---

## Table of Contents
1. [Testing Philosophy](#testing-philosophy)
2. [Backend Testing Structure](#backend-testing-structure)
3. [Test Execution](#test-execution)
4. [Naming Conventions](#naming-conventions)
5. [Test Data Management](#test-data-management)
6. [Coverage Targets](#coverage-targets)
7. [CI/CD Integration](#cicd-integration)
8. [Best Practices & Anti-Patterns](#best-practices--anti-patterns)

---

## Testing Philosophy

### Principles
- **Practical over Perfect**: Focus on high-value tests that catch real bugs, not 100% coverage
- **Pyramid Approach**: Many unit tests, fewer integration tests, minimal E2E tests
- **Test What Matters**: Prioritize business logic, edge cases, and integration points
- **Maintainable Tests**: Clear naming, isolated tests, avoid brittle assertions
- **CI/CD Ready**: Tests must run in automated pipelines

### Coverage Goals
- **Backend Unit Tests**: 70-80% coverage of business logic (services, validators)
- **Backend Integration Tests**: Critical workflows (ticket creation, file upload, authentication)
- **Frontend Tests**: Deferred until frontend implementation is mature

---

## Backend Testing Structure

### Project Organization

```
backend/
├── TicketingSystem.Api/                     # Main project
│   ├── Services/
│   ├── Controllers/
│   ├── Validators/
│   └── ...
├── TicketingSystem.Api.Tests/               # Unit Tests
│   ├── Services/
│   │   ├── Tickets/
│   │   │   └── TicketServiceTests.cs
│   │   ├── Attachments/
│   │   │   └── TicketAttachmentServiceTests.cs
│   │   ├── Assignment/
│   │   │   └── AssignmentServiceTests.cs
│   │   └── Comments/
│   │       └── CommentServiceTests.cs
│   ├── Validators/
│   │   ├── TicketValidatorTests.cs
│   │   ├── TicketUpdateValidatorTests.cs
│   │   └── AttachmentValidatorTests.cs
│   ├── Utils/
│   │   └── PasswordHasherTests.cs
│   └── Helpers/
│       ├── TestDbContextFactory.cs          # In-memory DB helper
│       └── TestDataFactory.cs               # Bogus fake data generators
└── TicketingSystem.Api.IntegrationTests/    # Integration Tests
    ├── Controllers/
    │   ├── TicketsControllerTests.cs
    │   ├── AuthControllerTests.cs
    │   └── LookupsControllerTests.cs
    ├── Workflows/
    │   ├── TicketLifecycleTests.cs          # Create → Update → Resolve
    │   ├── FileUploadWorkflowTests.cs       # Upload → Download
    │   └── AuthenticationFlowTests.cs       # Login → Access protected endpoint
    └── Helpers/
        ├── TestWebApplicationFactory.cs     # Test server factory
        └── PostgresTestContainer.cs         # Testcontainers helper
```

### NuGet Packages

#### TicketingSystem.Api.Tests.csproj
```xml
<PackageReference Include="xunit" Version="2.9.0" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="Moq" Version="4.20.72" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
<PackageReference Include="Bogus" Version="35.7.1" />
```

#### TicketingSystem.Api.IntegrationTests.csproj
```xml
<PackageReference Include="xunit" Version="2.9.0" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="FluentAssertions" Version="7.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="Testcontainers.PostgreSql" Version="4.2.0" />
```

---

## Test Execution

### Running Tests

```powershell
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "FullyQualifiedName~TicketingSystem.Api.Tests"

# Run integration tests only
dotnet test --filter "FullyQualifiedName~TicketingSystem.Api.IntegrationTests"

# Run tests in a specific class
dotnet test --filter "FullyQualifiedName~TicketServiceTests"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### Test Discovery

```powershell
# List all tests without running
dotnet test --list-tests

# Run specific test by full name
dotnet test --filter "FullyQualifiedName=TicketingSystem.Api.Tests.Services.Tickets.TicketServiceTests.CreateAsync_WithValidRequest_CreatesTicketAndReturnsId"
```

---

## Naming Conventions

### Test Class Naming

**Format:** `{ClassUnderTest}Tests`

```csharp
// ✅ Good
public class TicketServiceTests { }
public class TicketValidatorTests { }
public class AuthControllerTests { }

// ❌ Bad
public class TestTicketService { }
public class TicketServiceTest { }  // Singular
public class ServiceTests { }       // Too generic
```

### Test Method Naming

**Format:** `MethodName_Scenario_ExpectedResult`

```csharp
// ✅ Good - Clear and descriptive
[Fact]
public async Task CreateAsync_WithValidRequest_CreatesTicketAndReturnsId() { }

[Fact]
public async Task UpdateAsync_WithInvalidStatusTransition_ThrowsAppException() { }

[Fact]
public async Task GetByIdAsync_WhenTicketNotFound_ReturnsNull() { }

// ❌ Bad - Ambiguous or unclear
[Fact]
public async Task Test1() { }

[Fact]
public async Task CreateAsync() { }  // Missing scenario

[Fact]
public async Task CreateAsync_Works() { }  // Vague expected result
```

### Theory Test Naming

```csharp
// Use Theory for parameterized tests
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public async Task ValidateAndNormalize_WithEmptyTitle_ThrowsAppException(string title)
{
    // Test implementation
}

[Theory]
[InlineData(TicketStatus.New, TicketStatus.Open, true)]
[InlineData(TicketStatus.New, TicketStatus.Resolved, false)]
[InlineData(TicketStatus.Resolved, TicketStatus.Open, false)]
public void IsAllowedTransition_WithVariousTransitions_ReturnsExpectedResult(
    TicketStatus from, TicketStatus to, bool expected)
{
    // Test implementation
}
```

---

## Test Data Management

### Using Bogus for Fake Data

Create reusable fake data generators in `Helpers/TestDataFactory.cs`:

```csharp
using Bogus;
using TicketingSystem.Api.DTOs.Tickets;
using TicketingSystem.Api.Enums.Tickets;

namespace TicketingSystem.Api.Tests.Helpers;

public static class TestDataFactory
{
    public static Faker<CreateTicketRequest> TicketRequestFaker => new Faker<CreateTicketRequest>()
        .RuleFor(t => t.Title, f => f.Lorem.Sentence(3, 5).TrimEnd('.'))
        .RuleFor(t => t.Description, f => f.Lorem.Paragraph())
        .RuleFor(t => t.Category, f => (byte)f.PickRandom<TicketCategory>())
        .RuleFor(t => t.Priority, f => (byte)f.PickRandom<TicketPriority>());

    public static CreateTicketRequest CreateValidTicketRequest()
    {
        return TicketRequestFaker.Generate();
    }

    public static CreateTicketRequest CreateTicketRequestWithTitle(string title)
    {
        var request = TicketRequestFaker.Generate();
        return request with { Title = title };
    }
}
```

### In-Memory Database for Unit Tests

Create helper in `Helpers/TestDbContextFactory.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TicketingSystem.Api.Data;

namespace TicketingSystem.Api.Tests.Helpers;

public static class TestDbContextFactory
{
    /// <summary>
    /// Creates an in-memory AppDbContext for unit tests.
    /// Each call with a unique dbName creates an isolated database.
    /// </summary>
    public static AppDbContext CreateInMemory(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        
        return new AppDbContext(options);
    }

    /// <summary>
    /// Creates an in-memory database with seeded users for testing.
    /// </summary>
    public static async Task<AppDbContext> CreateWithSeedDataAsync()
    {
        var db = CreateInMemory();
        
        // Seed test users
        db.Users.AddRange(
            new User
            {
                UserId = 1,
                Name = "Test Admin",
                Email = "admin@test.local",
                PasswordHash = "hash",
                RoleId = UserRole.Admin
            },
            new User
            {
                UserId = 2,
                Name = "Test Employee",
                Email = "employee@test.local",
                PasswordHash = "hash",
                RoleId = UserRole.Employee,
                CategoryId = TicketCategory.IT
            },
            new User
            {
                UserId = 3,
                Name = "Test TeamLeader",
                Email = "teamlead@test.local",
                PasswordHash = "hash",
                RoleId = UserRole.TeamLeader,
                CategoryId = TicketCategory.IT
            }
        );
        
        await db.SaveChangesAsync();
        return db;
    }
}
```

### Testcontainers for Integration Tests

Create helper in `Helpers/PostgresTestContainer.cs`:

```csharp
using Testcontainers.PostgreSql;

namespace TicketingSystem.Api.IntegrationTests.Helpers;

public sealed class PostgresTestContainer : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresTestContainer()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("ticketing_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

---

## Coverage Targets

### By Component

| Component | Unit Tests | Integration Tests | Notes |
|-----------|-----------|-------------------|-------|
| **Services** | 80% | 30% (critical flows) | Focus on business logic |
| **Validators** | 90% | - | Cover all validation rules |
| **Controllers** | - | 70% | Test via HTTP requests |
| **Auth Flow** | 60% | 100% | Critical security path |
| **File Upload** | 80% | 100% | Transaction handling |
| **Utils** | 70% | - | PasswordHasher, etc. |

### Test Count Estimates

**Phase 1.7 (Existing Functionality):**
- Unit Tests: ~60 tests
- Integration Tests: ~25 tests
- **Total:** ~85 tests

**Full Project (All Phases):**
- Unit Tests: ~120 tests
- Integration Tests: ~50 tests
- **Total:** ~170 tests

---

## CI/CD Integration

### GitHub Actions Workflow

Create `.github/workflows/tests.yml`:

```yaml
name: Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_PASSWORD: test
          POSTGRES_USER: test
          POSTGRES_DB: ticketing_test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
      
      - name: Run Unit Tests
        run: dotnet test --no-build --configuration Release --filter "FullyQualifiedName~TicketingSystem.Api.Tests" --logger "trx;LogFileName=unit-tests.trx"
      
      - name: Run Integration Tests
        run: dotnet test --no-build --configuration Release --filter "FullyQualifiedName~TicketingSystem.Api.IntegrationTests" --logger "trx;LogFileName=integration-tests.trx"
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Port=5432;Database=ticketing_test;Username=test;Password=test"
      
      - name: Test Coverage
        run: dotnet test --no-build --configuration Release /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
      
      - name: Upload Test Results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: "**/*.trx"
```

---

## Best Practices & Anti-Patterns

### ✅ DO Test

1. **Business Logic**
   - Service methods with complex logic
   - Validation rules
   - Status transition rules
   - Assignment logic

2. **Edge Cases**
   - Null/empty inputs
   - Boundary values (min/max file size, title length)
   - Concurrent operations (if applicable)

3. **Error Handling**
   - Expected exceptions (AppException with correct error codes)
   - Not found scenarios
   - Unauthorized access

4. **Integration Points**
   - Database transactions
   - File storage operations
   - API endpoint contracts

### ❌ DON'T Test

1. **Framework Internals**
   - EF Core behavior (Save, Find, etc.)
   - ASP.NET Core routing
   - Third-party libraries (BCrypt, MailKit)

2. **Simple Properties**
   - DTOs without logic
   - Auto-properties
   - Getters/setters

3. **Private Methods**
   - Test through public API
   - If private method needs testing, consider extracting to separate class

4. **Auto-Generated Code**
   - EF migrations
   - Swagger/OpenAPI definitions

### Test Isolation

```csharp
// ✅ Good - Each test is isolated
public class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_Test1()
    {
        var db = TestDbContextFactory.CreateInMemory(); // Fresh DB
        var service = new TicketService(db, ...);
        // Test logic
    }

    [Fact]
    public async Task CreateAsync_Test2()
    {
        var db = TestDbContextFactory.CreateInMemory(); // Fresh DB
        var service = new TicketService(db, ...);
        // Test logic
    }
}

// ❌ Bad - Shared state between tests
public class TicketServiceTests
{
    private readonly AppDbContext _db = TestDbContextFactory.CreateInMemory(); // SHARED!

    [Fact]
    public async Task CreateAsync_Test1()
    {
        var service = new TicketService(_db, ...);
        // Test logic - may affect Test2
    }

    [Fact]
    public async Task CreateAsync_Test2()
    {
        var service = new TicketService(_db, ...);
        // Test logic - may be affected by Test1
    }
}
```

### Using FluentAssertions

```csharp
// ✅ Good - Fluent, readable assertions
result.Should().NotBeNull();
result.TicketId.Should().BeGreaterThan(0);
result.Status.Should().Be(TicketStatus.New);
ticket.Title.Should().Be("Expected Title");
tickets.Should().HaveCount(5);
tickets.Should().OnlyContain(t => t.Status == TicketStatus.Open);

// ❌ Bad - Traditional assertions (less readable)
Assert.NotNull(result);
Assert.True(result.TicketId > 0);
Assert.Equal(TicketStatus.New, result.Status);
```

### Mocking Dependencies

```csharp
// ✅ Good - Mock external dependencies, use real domain logic
[Fact]
public async Task CreateAsync_WithValidRequest_CallsAssignmentService()
{
    var db = TestDbContextFactory.CreateInMemory();
    var mockAssignment = new Mock<IAssignmentService>();
    mockAssignment.Setup(x => x.ResolveAssigneeAsync(It.IsAny<TicketCategory>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((int?)123);

    var service = new TicketService(db, mockCurrentUser, mockAssignment.Object, ...);
    
    await service.CreateAsync(request, CancellationToken.None);
    
    mockAssignment.Verify(x => x.ResolveAssigneeAsync(TicketCategory.IT, It.IsAny<CancellationToken>()), Times.Once);
}

// ❌ Bad - Over-mocking (mocking everything, including domain logic)
var mockDb = new Mock<AppDbContext>(); // Don't mock DbContext for unit tests, use in-memory
var mockValidator = new Mock<ITicketValidator>(); // Don't mock validators, test them directly
```

---

## Example Test Files

### Unit Test Example: TicketServiceTests.cs

```csharp
using FluentAssertions;
using Moq;
using TicketingSystem.Api.Common;
using TicketingSystem.Api.Enums.Tickets;
using TicketingSystem.Api.Services;
using TicketingSystem.Api.Tests.Helpers;
using Xunit;

namespace TicketingSystem.Api.Tests.Services.Tickets;

public class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesTicketAndReturnsId()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(x => x.GetUserId()).Returns(2); // Employee user
        
        var mockAssignment = new Mock<IAssignmentService>();
        mockAssignment.Setup(x => x.ResolveAssigneeAsync(It.IsAny<TicketCategory>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(3); // TeamLeader user

        var service = new TicketService(db, mockCurrentUser.Object, mockAssignment.Object, ...);
        var request = TestDataFactory.CreateValidTicketRequest();

        // Act
        var result = await service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TicketId.Should().BeGreaterThan(0);
        result.Status.Should().Be(TicketStatus.New);
        result.AssignedToUserId.Should().Be(3);
        
        var ticket = await db.Tickets.FindAsync(result.TicketId);
        ticket.Should().NotBeNull();
        ticket!.Title.Should().Be(request.Title);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidStatusTransition_ThrowsAppException()
    {
        // Arrange
        var db = await TestDbContextFactory.CreateWithSeedDataAsync();
        // Create ticket with Status = Resolved
        var ticket = new Ticket { TicketId = 1, Status = TicketStatus.Resolved, ... };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var service = new TicketService(db, ...);
        var request = new UpdateTicketRequest { Status = TicketStatus.Open }; // Invalid transition

        // Act
        Func<Task> act = async () => await service.UpdateAsync(1, request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AppException>()
            .Where(ex => ex.Code == ErrorCodes.TicketStatusTransitionInvalid);
    }
}
```

### Integration Test Example: TicketsControllerTests.cs

```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using TicketingSystem.Api.DTOs.Tickets;
using Xunit;

namespace TicketingSystem.Api.IntegrationTests.Controllers;

public class TicketsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TicketsControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTicket_WithValidRequest_ReturnsCreatedTicket()
    {
        // Arrange
        var request = new CreateTicketRequest
        {
            Title = "Test Ticket",
            Description = "Test Description",
            Category = (byte)TicketCategory.IT,
            Priority = (byte)TicketPriority.Medium
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tickets", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CreateTicketResponse>();
        result.Should().NotBeNull();
        result!.TicketId.Should().BeGreaterThan(0);
    }
}
```

---

## Troubleshooting

### Common Issues

**1. In-Memory Database Not Isolated**
```csharp
// Problem: Tests fail due to shared state
var db = TestDbContextFactory.CreateInMemory("SharedDb");

// Solution: Use unique DB name per test
var db = TestDbContextFactory.CreateInMemory(); // Auto-generates unique GUID
```

**2. Async Tests Hanging**
```csharp
// Problem: Forgot to await
public async Task MyTest()
{
    service.CreateAsync(request); // Missing await!
}

// Solution: Always await async calls
public async Task MyTest()
{
    await service.CreateAsync(request);
}
```

**3. FluentAssertions Not Working**
```powershell
# Problem: Package not installed
dotnet add package FluentAssertions

# Problem: Missing using statement
using FluentAssertions;
```

---

## Next Steps

1. ✅ Read this guide
2. ✅ Review PROJECT_PLAN.md Phase 1.7
3. ⏳ Create test projects (TicketingSystem.Api.Tests, TicketingSystem.Api.IntegrationTests)
4. ⏳ Implement unit tests for existing features
5. ⏳ Implement integration tests for critical workflows
6. ⏳ Set up CI/CD pipeline (GitHub Actions)
7. ⏳ Add frontend tests (deferred to Phase 2.5)

---

**Questions?** Refer to:
- **PROJECT_PLAN.md** - Phase-by-phase implementation plan
- **.github/copilot-instructions.md** - Project conventions and architecture
- **xUnit Documentation** - https://xunit.net/
- **FluentAssertions Documentation** - https://fluentassertions.com/
