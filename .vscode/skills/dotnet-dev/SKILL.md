---
name: dotnet-dev
description: 'Expert guidance for .NET development in this repository. Use this skill for building, testing, debugging, and understanding project structure, coding conventions, dependency injection patterns, and testing practices.'
---

# .NET Development Skills

Expert guidance for .NET development in this repository.

## Build & Test Commands

```bash
# Build the solution
dotnet build ./src/GitVersion.slnx

# Build a single project
dotnet build --project ./src/GitVersion.Core/GitVersion.Core.csproj

# Run all tests
dotnet test --solution ./src/GitVersion.slnx

# Run tests for a specific project
dotnet test --project ./src/GitVersion.Core.Tests/GitVersion.Core.Tests.csproj

# Run tests with specific framework
dotnet test --project ./src/GitVersion.Core.Tests/GitVersion.Core.Tests.csproj --framework net10.0

# Run specific test by filter
dotnet test --project ./src/GitVersion.Core.Tests/GitVersion.Core.Tests.csproj --filter "FullyQualifiedName~TestClassName"

# Format code
dotnet format ./src/GitVersion.slnx

# Verify formatting (CI-friendly)
dotnet format --verify-no-changes ./src/GitVersion.slnx
```

## Package Management

This repository uses **Central Package Management** via `Directory.Packages.props`.

### Adding/Updating Packages

```bash
# Add a package (version managed centrally)
dotnet add ./src/ProjectName/ProjectName.csproj package PackageName

# Update central package version in src/Directory.Packages.props
```

**Important**: Always update versions in `src/Directory.Packages.props`, not in individual `.csproj` files.

### Directory.Packages.props Structure

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="PackageName" Version="1.0.0" />
  </ItemGroup>
</Project>
```

## Project Structure

- `src/` - Main solution with production code and tests
- `new-cli/` - New CLI implementation (separate solution)
- `build/` - Build automation (Cake-based)
- `docs/` - Documentation

### Key Projects

| Project                    | Purpose                               |
| -------------------------- | ------------------------------------- |
| `GitVersion.Core`          | Core version calculation logic        |
| `GitVersion.App`           | CLI application                       |
| `GitVersion.Configuration` | Configuration file handling           |
| `GitVersion.Output`        | Output formatters (JSON, BuildServer) |
| `GitVersion.BuildAgents`   | CI/CD platform integrations           |
| `GitVersion.MsBuild`       | MSBuild task integration              |
| `GitVersion.LibGit2Sharp`  | Git repository abstraction            |

## Coding Conventions

### Primary Constructors

Prefer primary constructors with readonly field assignments:

```csharp
internal class BuildAgentResolver(IEnumerable<IBuildAgent> buildAgents, ILogger<BuildAgentResolver> logger) : IBuildAgentResolver
{
    private readonly IEnumerable<IBuildAgent> buildAgents = buildAgents.NotNull();
    private readonly ILogger<BuildAgentResolver> logger = logger.NotNull();

    public IBuildAgent? Resolve()
    {
        // Use this.buildAgents and this.logger
    }
}
```

### Dependency Injection

Use constructor injection with `ILogger<T>` for logging:

```csharp
public class MyService
{
    private readonly ILogger<MyService> logger;

    public MyService(ILogger<MyService> logger)
    {
        this.logger = logger;
    }
}
```

### Logging

Use Microsoft.Extensions.Logging with Serilog:

```csharp
// Information level
this.logger.LogInformation("Processing {BranchName}", branch.Name);

// Warning level
this.logger.LogWarning("Configuration not found, using defaults");

// Error level
this.logger.LogError(ex, "Failed to calculate version");

// Debug level (verbose)
this.logger.LogDebug("Cache hit for {CacheKey}", key);
```

### Nullable Reference Types

All projects use nullable reference types. Handle nullability explicitly:

```csharp
public string? OptionalProperty { get; set; }

public string RequiredProperty { get; set; } = string.Empty;
```

### File-Scoped Namespaces

Use file-scoped namespaces:

```csharp
namespace GitVersion.Core;

public class MyClass
{
    // ...
}
```

## Testing

### Test Project Naming

- Test projects mirror source projects: `GitVersion.Core` → `GitVersion.Core.Tests`

### Test Frameworks

- **NUnit** - Primary test framework
- **NSubstitute** - Mocking framework
- **Shouldly** - Assertion library

### Test Patterns

```csharp
[TestFixture]
public class MyServiceTests
{
    [Test]
    public void MethodName_Scenario_ExpectedResult()
    {
        // Arrange
        var service = new MyService();

        // Act
        var result = service.DoSomething();

        // Assert
        result.ShouldBe(expected);
    }

    [TestCase("input1", "expected1")]
    [TestCase("input2", "expected2")]
    public void MethodName_WithParameters_ReturnsExpected(string input, string expected)
    {
        var result = service.Process(input);
        result.ShouldBe(expected);
    }
}
```

## Configuration Files

### Supported Names

- `GitVersion.yml`
- `GitVersion.yaml`
- `.GitVersion.yml`
- `.GitVersion.yaml`

### Schema Location

JSON schemas are in `schemas/` directory for validation.

## Build Agents

Build agent integrations write environment variables with `GitVersion_` prefix:

```csharp
// Example: GitHub Actions
Environment.SetEnvironmentVariable($"GitVersion_{name}", value);
```

## Common Tasks

### Running the CLI Locally

```bash
dotnet run --project src/GitVersion.App
```

### Debugging Tests

```bash
# Run with detailed output
dotnet test --project ./src/GitVersion.Core.Tests/GitVersion.Core.Tests.csproj -v detailed

# Run specific test
dotnet test --filter "FullyQualifiedName=GitVersion.Core.Tests.MyTest"
```

### Checking for Errors

```bash
# Build with warnings as errors
dotnet build ./src/GitVersion.slnx -warnaserror
```
