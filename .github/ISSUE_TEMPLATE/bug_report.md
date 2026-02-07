---
name: Bug Report
about: Report a bug or issue with the library
title: '[BUG] '
labels: bug
assignees: ''
---

## Describe the Bug

<!-- A clear and concise description of what the bug is -->

## To Reproduce

Steps to reproduce the behavior:

1. Configure metrics with '...'
2. Send metrics with '...'
3. See error

## Expected Behavior

<!-- A clear and concise description of what you expected to happen -->

## Actual Behavior

<!-- What actually happened -->

## Environment

- **Package Version**: [e.g., 0.1.0]
- **.NET SDK Version**: [e.g., 8.0.101]
- **Target Framework**: [e.g., net8.0, net10.0]
- **OS**: [e.g., macOS 14.0, Windows 11, Ubuntu 22.04]
- **Runtime**: [e.g., ASP.NET Core, Console App, Worker Service]

## Configuration

```csharp
// Relevant parts of your configuration
services.AddDatadogMetrics(options =>
{
    options.ApiKey = "...";
    options.Site = "...";
    // Add other relevant properties
});
```

## Code Sample

```csharp
// Minimal code sample that reproduces the issue
metrics.Counter("test", 1);
```

## Logs / Error Messages

```
<!-- Paste relevant logs or error messages here -->
<!-- Include stack traces if available -->
```

## Stack Trace (if applicable)

```
<!-- Paste any stack traces or error details here -->
```

## Additional Context

<!-- Add any other context about the problem here -->
<!-- Screenshots, related issues, workarounds attempted, etc. -->

## Checklist

- [ ] I have searched existing issues to ensure this is not a duplicate
- [ ] I have included all relevant configuration details
- [ ] I have included code samples or logs showing the error
- [ ] I have tested with the latest version of the package
