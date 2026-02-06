# Contributing to Datadog.Metrics

Thank you for your interest in contributing to Datadog.Metrics! This document provides guidelines and instructions for contributing.

## Code of Conduct

Please be respectful and constructive in all interactions with the community.

## Getting Started

### Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Git](https://git-scm.com/)
- A code editor ([VS Code](https://code.visualstudio.com/), [Visual Studio](https://visualstudio.microsoft.com/), or [Rider](https://www.jetbrains.com/rider/))

### Setting Up Your Development Environment

1. **Fork and clone the repository**

```bash
git clone https://github.com/YOUR_USERNAME/node-datadog-metrics.git
cd node-datadog-metrics/dotnet
```

2. **Initialize the development environment**

```bash
make init
```

Or manually:

```bash
dotnet restore
dotnet build
```

3. **Run the tests**

```bash
make test
```

Or:

```bash
dotnet test
```

## Development Workflow

### Making Changes

1. **Create a new branch**

```bash
git checkout -b feature/your-feature-name
# or
git checkout -b fix/bug-description
```

2. **Make your changes**

Follow the coding standards described below.

3. **Write or update tests**

All new features and bug fixes should include tests.

4. **Run tests**

```bash
make test
```

5. **Format your code**

```bash
make format
```

6. **Commit your changes**

Write clear, descriptive commit messages:

```bash
git add .
git commit -m "Add feature: description of what you did"
```

Follow these commit message guidelines:
- Use the imperative mood ("Add feature" not "Added feature")
- Keep the first line under 72 characters
- Reference issues: "Fix #123: description"

### Running the Sample

Test your changes with the sample application:

```bash
export DD_API_KEY=your-test-api-key
make run-sample
```

## Coding Standards

### C# Style Guide

We follow standard .NET conventions with some specific preferences:

- **Naming**:
  - `PascalCase` for public members
  - `_camelCase` for private fields (with underscore prefix)
  - `PascalCase` for constants
  - `IPascalCase` for interfaces (with 'I' prefix)

- **Code Organization**:
  - Keep files focused on a single responsibility
  - Use meaningful names that describe intent
  - Prefer clarity over brevity

- **XML Documentation**:
  - Add XML doc comments (`///`) for all public APIs
  - Include `<summary>`, `<param>`, and `<returns>` tags

Example:

```csharp
/// <summary>
/// Records a gauge metric value.
/// </summary>
/// <param name="key">Metric name.</param>
/// <param name="value">Metric value.</param>
/// <param name="tags">Optional tags.</param>
public void Gauge(string key, double value, string[]? tags = null)
{
    // Implementation
}
```

### Code Formatting

We use `.editorconfig` to enforce consistent formatting. Run the formatter before committing:

```bash
make format
```

Or:

```bash
dotnet format
```

### Testing

- **Write comprehensive tests** for new features
- **Update existing tests** when changing behavior
- **Test edge cases** and error conditions
- **Use descriptive test names** that explain what is being tested

Example:

```csharp
[Fact]
public void Gauge_LatestValueWins()
{
    // Arrange
    var metric = new GaugeMetric("test", null, "localhost");

    // Act
    metric.AddPoint(10, DateTimeOffset.UtcNow);
    metric.AddPoint(20, DateTimeOffset.UtcNow);

    // Assert
    var result = metric.Flush();
    Assert.Equal(20, result[0].Points[0].Value);
}
```

## Project Structure

```
dotnet/
├── src/
│   └── Datadog.Metrics/        # Main library
│       ├── Configuration/       # Configuration models
│       ├── Core/                # Core aggregation logic
│       ├── Extensions/          # DI extensions
│       ├── Integration/         # System.Diagnostics.Metrics
│       ├── Metrics/             # Metric implementations
│       └── Transport/           # HTTP API client
├── tests/
│   └── Datadog.Metrics.Tests/  # Unit tests
└── samples/
    └── Datadog.Metrics.Sample/ # Sample application
```

## Pull Request Process

1. **Update documentation** for any user-facing changes
2. **Add or update tests** to cover your changes
3. **Ensure all tests pass**: `make test`
4. **Format your code**: `make format`
5. **Update CHANGELOG.md** if applicable
6. **Create a pull request** with a clear description

### PR Description Template

```markdown
## Description
Brief description of what this PR does.

## Type of Change
- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Testing
Describe the tests you ran to verify your changes.

## Checklist
- [ ] My code follows the style guidelines
- [ ] I have performed a self-review
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
```

## Release Process

Releases are managed by maintainers. The process is:

1. Update version in `.csproj` files
2. Update `CHANGELOG.md`
3. Create a git tag: `git tag v1.0.0`
4. Push the tag: `git push origin v1.0.0`
5. GitHub Actions will automatically build and publish to NuGet

## Reporting Issues

### Bug Reports

When reporting bugs, please include:

1. **Description**: Clear description of the bug
2. **Steps to reproduce**: Minimal steps to reproduce the issue
3. **Expected behavior**: What should happen
4. **Actual behavior**: What actually happens
5. **Environment**:
   - .NET version
   - Operating system
   - Library version

Example:

```markdown
**Description**
Metrics are not being flushed when FlushIntervalSeconds is set to 0.

**Steps to reproduce**
1. Configure with FlushIntervalSeconds = 0
2. Record some metrics
3. Observe that metrics are never sent

**Expected behavior**
Metrics should be sent immediately when interval is 0.

**Actual behavior**
Metrics are never sent.

**Environment**
- .NET 8.0
- Windows 11
- Datadog.Metrics v1.0.0
```

### Feature Requests

When requesting features, please:

1. **Describe the problem** you're trying to solve
2. **Propose a solution** (if you have one)
3. **Provide use cases** showing why this is valuable

## Makefile Commands Reference

```bash
make help              # Show all available commands
make build             # Build the solution
make test              # Run tests
make test-watch        # Run tests in watch mode
make pack              # Create NuGet package
make format            # Format code
make lint              # Run linting and analysis
make run-sample        # Run sample application
make clean             # Clean build artifacts
make ci                # Run full CI pipeline locally
```

## Questions?

If you have questions about contributing, feel free to:

- Open a [GitHub Discussion](https://github.com/dbader/node-datadog-metrics/discussions)
- Create an issue with the "question" label

## License

By contributing, you agree that your contributions will be licensed under the MIT License.
