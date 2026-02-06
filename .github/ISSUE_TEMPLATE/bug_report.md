---
name: Bug Report
about: Report a bug in Datadog.Metrics
title: '[BUG] '
labels: bug
assignees: ''
---

## Description
A clear and concise description of what the bug is.

## Steps to Reproduce
Steps to reproduce the behavior:
1. Configure with '...'
2. Call method '...'
3. Observe '...'

## Expected Behavior
A clear and concise description of what you expected to happen.

## Actual Behavior
A clear and concise description of what actually happened.

## Code Sample
```csharp
// Minimal code sample that demonstrates the issue
services.AddDatadogMetrics(options =>
{
    options.ApiKey = "xxx";
    // ...
});

metrics.Gauge("example", 42);
```

## Environment
- **OS**: [e.g., Windows 11, Ubuntu 22.04, macOS 14]
- **.NET Version**: [e.g., .NET 8.0.1]
- **Library Version**: [e.g., 1.0.0]
- **Target Framework**: [e.g., net8.0, net6.0, netstandard2.0]

## Logs
```
Paste relevant log output here
```

## Additional Context
Add any other context about the problem here (e.g., stack traces, related issues).

## Possible Solution
If you have ideas on how to fix this, please share them here.
