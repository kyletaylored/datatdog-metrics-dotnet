# Fixes Applied

## Issue 1: `appsettings.json` Not Found Error

**Problem:**
```
System.IO.FileNotFoundException: The configuration file 'appsettings.json' was not found
```

**Root Cause:**
The Makefile was running `dotnet run --project samples/Datadog.Metrics.Sample` from the `dotnet/` directory, but the application looks for `appsettings.json` relative to the current working directory, not the project directory.

**Solution:**
Changed Makefile commands to `cd` into the sample directory before running:

```makefile
# Before:
run-sample-debug: build-debug
	@dotnet run --project samples/Datadog.Metrics.Sample --configuration Debug --no-build

# After:
run-sample-debug: build-debug
	@cd samples/Datadog.Metrics.Sample && dotnet run --configuration Debug --no-build
```

## Issue 2: API Key Substring Error

**Problem:**
```
System.ArgumentOutOfRangeException: Index and length must refer to a location within the string
```

**Root Cause:**
The code tried to show the first 8 characters of the API key using `apiKey[..8]`, but if the API key is empty or shorter than 8 characters (as it is in the default `appsettings.json`), this throws an exception.

**Solution:**
Added proper null/empty checking and safe substring logic:

```csharp
// Before:
var apiKey = builder.Configuration["DatadogMetrics:ApiKey"]
    ?? builder.Configuration["API_KEY"]
    ?? throw new InvalidOperationException(...);

Console.WriteLine($"✓ API Key loaded: {apiKey[..8]}...");

// After:
var apiKey = builder.Configuration["DatadogMetrics:ApiKey"]
    ?? builder.Configuration["API_KEY"];

if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException(...);
}

var maskedKey = apiKey.Length >= 8
    ? $"{apiKey[..8]}..."
    : $"{apiKey[..Math.Min(4, apiKey.Length)]}...";
Console.WriteLine($"✓ API Key loaded: {maskedKey} (showing first characters)");
```

## How to Use

### Running Without an API Key (to see error message):
```bash
make run-sample-debug
```

**Expected output:**
```
╔════════════════════════════════════════════════════════════════╗
║          Datadog.Metrics Sample Application                   ║
╚════════════════════════════════════════════════════════════════╝

Unhandled exception. System.InvalidOperationException: Datadog API key not found. Please provide it via:
  1. Environment variable: DD_API_KEY=your-key
  2. User secrets: dotnet user-secrets set "DatadogMetrics:ApiKey" "your-key"
  3. appsettings.json (not recommended for production)
```

### Running With an API Key:
```bash
# Method 1: Environment variable
export DD_API_KEY=your-datadog-api-key
make run-sample-debug

# Method 2: User secrets (recommended for development)
cd samples/Datadog.Metrics.Sample
dotnet user-secrets set "DatadogMetrics:ApiKey" "your-api-key"
dotnet run

# Method 3: Edit appsettings.json (not recommended - can leak to git)
# Edit samples/Datadog.Metrics.Sample/appsettings.json
# Change: "ApiKey": "" to "ApiKey": "your-key"
make run-sample-debug
```

## Status

✅ Both issues fixed
✅ Makefile updated for both `run-sample` and `run-sample-debug`
✅ Program.cs updated with safe string handling
✅ Clear error messages when API key is missing
