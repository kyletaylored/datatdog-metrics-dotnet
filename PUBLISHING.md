# Publishing Guide: kyletaylored.Datadog.Metrics

## Package Configuration

The library is configured for community publishing under your namespace while keeping code unchanged for future official support.

### Current Setup

- **Package Name:** `kyletaylored.Datadog.Metrics`
- **Namespace:** `Datadog.Metrics` (unchanged)
- **Author:** Kyle Taylor
- **Company:** kyletaylored
- **Type:** Community/Unofficial package

### Why This Approach?

This configuration allows:

1. ✅ **Publish now** as a community package under `kyletaylored.*`
2. ✅ **No code changes** - users import `using Datadog.Metrics;`
3. ✅ **Smooth migration** - if Datadog officially adopts this, users only change the package reference
4. ✅ **Clear attribution** - Package description clarifies it's community-maintained

## Installation

Users will install with:

```bash
dotnet add package kyletaylored.Datadog.Metrics
```

But use the library exactly the same way:

```csharp
using Datadog.Metrics;
using Datadog.Metrics.Extensions;

services.AddDatadogMetrics(options => { ... });
```

## Publishing to NuGet.org

### Prerequisites

1. **NuGet Account**
   - Create account at https://www.nuget.org
   - Generate API key at https://www.nuget.org/account/apikeys
   - Set API key: `export NUGET_API_KEY=your-key-here`

2. **Version Strategy**
   - Preview releases: `1.0.0-preview.1`, `1.0.0-preview.2`, etc.
   - Release candidates: `1.0.0-rc.1`
   - Stable releases: `1.0.0`, `1.1.0`, etc.

### Manual Publishing

```bash
# Build and create package
make pack VERSION=1.0.0-preview.1

# Verify package contents
unzip -l artifacts/kyletaylored.Datadog.Metrics.1.0.0-preview.1.nupkg | head -20

# Publish to NuGet.org
export NUGET_API_KEY=your-api-key
make publish-nuget

# Or manually
dotnet nuget push artifacts/kyletaylored.Datadog.Metrics.1.0.0-preview.1.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### Automated Publishing (GitHub Actions)

The repository is configured for automatic publishing via GitHub Actions.

**Setup:**

1. Go to GitHub repository settings
2. Navigate to **Secrets and variables → Actions**
3. Add secret: `NUGET_API_KEY` with your NuGet API key

**Trigger a release:**

```bash
# Create and push a version tag
git tag v1.0.0-preview.1
git push origin v1.0.0-preview.1

# Create a GitHub release from this tag
# The workflow will automatically:
# 1. Build the package
# 2. Run all tests
# 3. Publish to NuGet.org
# 4. Publish to GitHub Packages
```

**Manual trigger:**
You can also manually trigger the publish workflow from GitHub Actions UI with a custom version.

## Package Metadata

The `.csproj` is configured with:

```xml
<PackageId>kyletaylored.Datadog.Metrics</PackageId>
<Version>0.1.0</Version>
<Authors>Kyle Taylor</Authors>
<Company>kyletaylored</Company>
<Description>
  Buffered metrics reporting to Datadog HTTP API with System.Diagnostics.Metrics integration.
</Description>
<PackageTags>datadog;metrics;monitoring;observability;telemetry;community;unofficial</PackageTags>
<PackageProjectUrl>https://github.com/kyletaylored/datadog-metrics-dotnet</PackageProjectUrl>
<RepositoryUrl>https://github.com/kyletaylored/datadog-metrics-dotnet</RepositoryUrl>
```

## Versioning Strategy

Follow [Semantic Versioning](https://semver.org/):

- **1.0.0-preview.X** - Initial preview releases
  - For early adopters and testing
  - Breaking changes allowed between previews

- **1.0.0-rc.X** - Release candidates
  - Feature-complete
  - Only bug fixes

- **1.0.0** - First stable release
  - Production-ready
  - API stability guaranteed

- **1.X.Y** - Updates
  - Minor (X): New features, backwards compatible
  - Patch (Y): Bug fixes only

## Migration Path to Official Package

If Datadog officially adopts this package:

### Option 1: Transfer Ownership

- Transfer `kyletaylored.Datadog.Metrics` ownership to Datadog on NuGet.org
- Datadog updates metadata
- Package continues as community package with official support

### Option 2: New Official Package

- Datadog creates new `Datadog.Metrics` package
- `kyletaylored.Datadog.Metrics` marked as deprecated
- Users update: `dotnet add package Datadog.Metrics`
- **No code changes needed** - same namespace!

```bash
# User migration (if official package is created)
dotnet remove package kyletaylored.Datadog.Metrics
dotnet add package Datadog.Metrics
# Code continues to work unchanged!
```

## Testing Before Publishing

### Local Testing

```bash
# Build package
make pack VERSION=1.0.0-preview.1

# Install locally for testing
make publish-local

# Test in another project
cd /path/to/test-project
dotnet add package kyletaylored.Datadog.Metrics --version 1.0.0-preview.1
```

### Smoke Test

Create a minimal test project:

```bash
mkdir test-install && cd test-install
dotnet new console
dotnet add package kyletaylored.Datadog.Metrics --version 1.0.0-preview.1
```

```csharp
using Datadog.Metrics;
Console.WriteLine("Successfully installed kyletaylored.Datadog.Metrics!");
```

## Package Contents

The NuGet package includes:

```
kyletaylored.Datadog.Metrics.1.0.0-preview.1.nupkg
├── lib/
│   ├── net10.0/
│   │   └── Datadog.Metrics.dll
│   ├── net9.0/
│   │   └── Datadog.Metrics.dll
│   ├── net8.0/
│   │   └── Datadog.Metrics.dll
│   ├── net6.0/
│   │   └── Datadog.Metrics.dll
│   └── netstandard2.0/
│       └── Datadog.Metrics.dll
├── README.md
└── [package metadata]
```

## Post-Publishing

After publishing to NuGet.org:

1. **Verify listing**
   - Visit https://www.nuget.org/packages/kyletaylored.Datadog.Metrics
   - Check description, tags, dependencies
   - README should display correctly

2. **Test installation**

   ```bash
   dotnet new console -n test
   cd test
   dotnet add package kyletaylored.Datadog.Metrics --prerelease
   dotnet build
   ```

3. **Update documentation**
   - Confirm README shows correct package name
   - Update any external documentation
   - Share on social media/forums if desired

4. **Monitor feedback**
   - Watch GitHub issues for bug reports
   - Monitor NuGet downloads
   - Respond to community questions

## Useful Commands

```bash
# Create package
make pack VERSION=1.0.0-preview.1

# List package contents
unzip -l artifacts/kyletaylored.Datadog.Metrics.*.nupkg

# Publish to NuGet.org
make publish-nuget

# Publish to local cache (testing)
make publish-local

# View package info
dotnet nuget locals all --list
```

## Support & Contact

- **Issues:** https://github.com/kyletaylored/datadog-metrics-dotnet/issues
- **Discussions:** https://github.com/kyletaylored/datadog-metrics-dotnet/discussions
- **NuGet:** https://www.nuget.org/packages/kyletaylored.Datadog.Metrics

## Credits

- Original Node.js library: [node-datadog-metrics](https://github.com/dbader/node-datadog-metrics) by Daniel Bader
- .NET port: Kyle Taylor ([@kyletaylored](https://github.com/kyletaylored/datadog-metrics-dotnet))
