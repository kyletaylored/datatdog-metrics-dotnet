# Configuration Guide

The sample application supports multiple configuration methods following standard .NET patterns.

## Configuration Hierarchy (Priority Order)

Configuration sources are loaded in this order, with **later sources overriding earlier ones**:

1. **`appsettings.json`** - Base configuration (committed to git)
2. **`appsettings.{Environment}.json`** - Environment-specific overrides
   - `appsettings.Development.json` - Development environment
   - `appsettings.Production.json` - Production environment
   - `appsettings.Staging.json` - Staging environment
3. **User Secrets** - Development-only secrets (NOT in git)
4. **Environment Variables** - System environment variables
5. **Environment Variables with `DD_` prefix** - Datadog-specific variables
6. **Command-line Arguments** - Highest priority

## Configuration Files

### `appsettings.json` (Base Configuration)

This is the base configuration file committed to git. Contains safe defaults:

```json
{
  "DatadogMetrics": {
    "ApiKey": "",  // Empty - must be provided by override
    "Site": "datadoghq.com",
    "FlushIntervalSeconds": 5,
    "DefaultTags": [
      "env:development",
      "service:datadog-metrics-sample"
    ]
  }
}
```

### `appsettings.Development.json` (Development Overrides)

Overrides for local development. **Committed to git as a template** with safe values:

```json
{
  "Logging": {
    "LogLevel": {
      "Datadog.Metrics": "Debug"  // More verbose logging
    }
  },
  "DatadogMetrics": {
    "FlushIntervalSeconds": 5,  // Quick flushes for dev
    "DefaultTags": [
      "env:development",
      "developer:local"
    ]
  }
}
```

**Note:** `appsettings.Development.json` is committed as a template. For local secrets, use one of these instead:
- User Secrets (recommended)
- `appsettings.Local.json` (ignored by git)
- Environment variables

### `appsettings.Local.json` (Local Overrides - Optional)

For local development overrides that should **never** be committed:

```json
{
  "DatadogMetrics": {
    "ApiKey": "your-local-api-key-here",
    "Site": "datadoghq.eu",
    "Prefix": "myname."
  }
}
```

**Status:** Automatically ignored by `.gitignore` ✅

To use:
1. Create `samples/Datadog.Metrics.Sample/appsettings.Local.json`
2. Add your settings
3. It will automatically be loaded and override other settings

### Environment-Specific Files

Create these as needed:

- `appsettings.Production.json` - Production settings
- `appsettings.Staging.json` - Staging settings
- `appsettings.Testing.json` - Test environment settings

## Setting the Environment

The environment is determined by the `DOTNET_ENVIRONMENT` or `ASPNETCORE_ENVIRONMENT` variable:

```bash
# Development (default)
export DOTNET_ENVIRONMENT=Development
dotnet run

# Production
export DOTNET_ENVIRONMENT=Production
dotnet run

# Staging
export DOTNET_ENVIRONMENT=Staging
dotnet run
```

## Configuration Methods (Ranked by Security)

### 1. User Secrets (Best for Development) 🏆

Stored outside your project directory, **never committed to git**.

```bash
cd samples/Datadog.Metrics.Sample

# Set individual values
dotnet user-secrets set "DatadogMetrics:ApiKey" "your-api-key"
dotnet user-secrets set "DatadogMetrics:Site" "datadoghq.eu"

# List all secrets
dotnet user-secrets list

# Remove a secret
dotnet user-secrets remove "DatadogMetrics:ApiKey"

# Clear all secrets
dotnet user-secrets clear
```

**Pros:**
- ✅ Most secure for development
- ✅ Never committed to git
- ✅ Easy to manage
- ✅ Per-project isolation

**Cons:**
- ❌ Development only (not available in Production)
- ❌ Per-machine (not shared with team)

### 2. Environment Variables (Best for Production) 🏆

System-level configuration, ideal for production deployments.

```bash
# Standard format
export DatadogMetrics__ApiKey="your-api-key"
export DatadogMetrics__Site="datadoghq.com"
export DatadogMetrics__FlushIntervalSeconds="10"

# Or with DD_ prefix (Datadog convention)
export DD_API_KEY="your-api-key"
export DD_SITE="datadoghq.com"
```

**Hierarchical Configuration:**
```bash
# Double underscore (__) represents nested configuration
export DatadogMetrics__Histogram__Percentiles__0="0.50"
export DatadogMetrics__Histogram__Percentiles__1="0.95"
export DatadogMetrics__Histogram__Percentiles__2="0.99"
```

**Pros:**
- ✅ Works in all environments
- ✅ Not in source control
- ✅ Standard for Docker/Kubernetes/Cloud
- ✅ Easy to manage in CI/CD

**Cons:**
- ❌ System-wide (affects all apps)
- ❌ Harder to manage complex nested configs

### 3. `appsettings.Local.json` (Good for Development)

Local file for development overrides.

```bash
# Create the file
cat > samples/Datadog.Metrics.Sample/appsettings.Local.json << 'EOF'
{
  "DatadogMetrics": {
    "ApiKey": "your-api-key",
    "Site": "datadoghq.com"
  }
}
EOF

# Run the app
dotnet run
```

**Pros:**
- ✅ Easy to edit
- ✅ Automatically ignored by git
- ✅ Supports complex nested configuration

**Cons:**
- ❌ File could accidentally be committed if .gitignore is wrong
- ❌ Development only

### 4. `appsettings.{Environment}.json` (Not Recommended for Secrets)

Environment-specific configuration files.

**⚠️ Warning:** These files are typically committed to git, so **never** put secrets in them!

**Good for:**
```json
{
  "DatadogMetrics": {
    "Site": "datadoghq.com",
    "FlushIntervalSeconds": 10,
    "DefaultTags": ["env:production"]
  }
}
```

**Bad for:**
```json
{
  "DatadogMetrics": {
    "ApiKey": "abc123"  // ❌ Never commit API keys!
  }
}
```

## Complete Example: Development Setup

### Step 1: Create User Secret (Recommended)
```bash
cd samples/Datadog.Metrics.Sample
dotnet user-secrets set "DatadogMetrics:ApiKey" "YOUR_API_KEY_HERE"
```

### Step 2: Override Other Settings (Optional)

**Option A: appsettings.Local.json**
```json
{
  "DatadogMetrics": {
    "Prefix": "myname.",
    "DefaultTags": [
      "env:development",
      "developer:yourname"
    ]
  }
}
```

**Option B: Environment Variables**
```bash
export DatadogMetrics__Prefix="myname."
export DatadogMetrics__DefaultTags__0="env:development"
export DatadogMetrics__DefaultTags__1="developer:yourname"
```

### Step 3: Run the App
```bash
dotnet run
# or
make run-sample-debug
```

## Complete Example: Production Deployment

### Docker
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ .
ENV DOTNET_ENVIRONMENT=Production
ENV DD_API_KEY=${DATADOG_API_KEY}
ENV DD_SITE=datadoghq.com
ENTRYPOINT ["dotnet", "Datadog.Metrics.Sample.dll"]
```

```bash
docker run -e DD_API_KEY=your-key your-image
```

### Kubernetes
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: datadog-secrets
stringData:
  api-key: your-api-key
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: app
        env:
        - name: DOTNET_ENVIRONMENT
          value: "Production"
        - name: DD_API_KEY
          valueFrom:
            secretKeyRef:
              name: datadog-secrets
              key: api-key
        - name: DD_SITE
          value: "datadoghq.com"
```

### Azure App Service
```bash
az webapp config appsettings set \
  --name myapp \
  --resource-group mygroup \
  --settings \
    DOTNET_ENVIRONMENT=Production \
    DD_API_KEY=your-key \
    DD_SITE=datadoghq.com
```

### AWS ECS/Fargate
```json
{
  "containerDefinitions": [{
    "environment": [
      {"name": "DOTNET_ENVIRONMENT", "value": "Production"},
      {"name": "DD_SITE", "value": "datadoghq.com"}
    ],
    "secrets": [
      {
        "name": "DD_API_KEY",
        "valueFrom": "arn:aws:secretsmanager:region:account:secret:datadog-api-key"
      }
    ]
  }]
}
```

## Configuration Validation

The app validates configuration on startup:

```
✓ API Key loaded: dd123456... (showing first characters)
✓ Target Site: datadoghq.com

📊 Datadog Metrics Configuration:
   • Flush Interval: 5s
   • Prefix: sample_app.
   • Default Tags: env:development, service:datadog-metrics-sample
   • Max Retries: 3
```

If configuration is invalid:
```
Unhandled exception. System.InvalidOperationException: Datadog API key not found. Please provide it via:
  1. Environment variable: DD_API_KEY=your-key
  2. User secrets: dotnet user-secrets set "DatadogMetrics:ApiKey" "your-key"
  3. appsettings.json (not recommended for production)
```

## Troubleshooting

### Which configuration is being used?

Add logging to see configuration sources:

```bash
export Logging__LogLevel__Microsoft.Extensions.Configuration="Debug"
dotnet run
```

### Verify configuration values

Use the .NET CLI:

```bash
# In Program.cs, temporarily add:
Console.WriteLine(builder.Configuration.GetDebugView());
```

### Common Issues

**API key not found:**
- Check `DOTNET_ENVIRONMENT` matches your file names
- Verify user secrets are set: `dotnet user-secrets list`
- Check environment variables: `env | grep DD_`

**Wrong environment loaded:**
```bash
# Check which environment is active
echo $DOTNET_ENVIRONMENT
echo $ASPNETCORE_ENVIRONMENT

# Set explicitly
export DOTNET_ENVIRONMENT=Development
```

**Configuration not overriding:**
Remember the priority order! Later sources override earlier ones:
1. appsettings.json (lowest)
2. appsettings.{Environment}.json
3. User Secrets
4. Environment Variables
5. Command-line arguments (highest)

## Security Best Practices

✅ **DO:**
- Use User Secrets for development
- Use environment variables for production
- Use managed secrets services (Azure Key Vault, AWS Secrets Manager, etc.)
- Keep `appsettings.json` files in git with safe defaults
- Use `.gitignore` to protect local overrides

❌ **DON'T:**
- Commit API keys or secrets to git
- Put secrets in `appsettings.{Environment}.json` that gets committed
- Share user secrets files
- Use the same API key for dev and production
