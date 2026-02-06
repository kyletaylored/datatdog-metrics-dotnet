.PHONY: help restore build test pack publish clean format lint run-sample

# Default target
.DEFAULT_GOAL := help

# Variables
CONFIGURATION ?= Release
VERSION ?= 1.0.0
OUTPUT_DIR ?= ./artifacts

# Colors for output
BLUE := \033[0;34m
GREEN := \033[0;32m
YELLOW := \033[0;33m
RED := \033[0;31m
NC := \033[0m # No Color

help: ## Display this help message
	@echo "$(BLUE)Datadog.Metrics - Available Commands$(NC)"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "  $(GREEN)%-20s$(NC) %s\n", $$1, $$2}'
	@echo ""
	@echo "$(YELLOW)Examples:$(NC)"
	@echo "  make build              # Build in Release mode"
	@echo "  make test               # Run all tests"
	@echo "  make pack VERSION=1.2.3 # Create NuGet package with specific version"
	@echo "  make run-sample         # Run the sample application"

restore: ## Restore NuGet packages
	@echo "$(BLUE)Restoring dependencies...$(NC)"
	@dotnet restore

build: restore ## Build the solution
	@echo "$(BLUE)Building solution in $(CONFIGURATION) mode...$(NC)"
	@dotnet build --configuration $(CONFIGURATION) --no-restore

build-debug: ## Build in Debug mode
	@$(MAKE) build CONFIGURATION=Debug

rebuild: clean build ## Clean and rebuild

test: build ## Run all tests
	@echo "$(BLUE)Running tests...$(NC)"
	@dotnet test --configuration $(CONFIGURATION) --no-build --verbosity normal

test-verbose: build ## Run tests with detailed output
	@echo "$(BLUE)Running tests with verbose output...$(NC)"
	@dotnet test --configuration $(CONFIGURATION) --no-build --verbosity detailed

test-watch: ## Run tests in watch mode
	@echo "$(BLUE)Running tests in watch mode (Ctrl+C to exit)...$(NC)"
	@dotnet watch test --project tests/Datadog.Metrics.Tests

test-coverage: build ## Run tests with code coverage
	@echo "$(BLUE)Running tests with coverage...$(NC)"
	@dotnet test --configuration $(CONFIGURATION) --no-build --collect:"XPlat Code Coverage" --results-directory $(OUTPUT_DIR)/coverage

pack: build ## Create NuGet package
	@echo "$(BLUE)Creating NuGet package (version $(VERSION))...$(NC)"
	@mkdir -p $(OUTPUT_DIR)
	@dotnet pack src/Datadog.Metrics/Datadog.Metrics.csproj \
		--configuration $(CONFIGURATION) \
		--no-build \
		--output $(OUTPUT_DIR) \
		/p:PackageVersion=$(VERSION)
	@echo "$(GREEN)Package created: $(OUTPUT_DIR)/Datadog.Metrics.$(VERSION).nupkg$(NC)"

publish-local: pack ## Publish package to local NuGet cache
	@echo "$(BLUE)Publishing to local NuGet cache...$(NC)"
	@dotnet nuget push $(OUTPUT_DIR)/*.nupkg --source ~/.nuget/packages

publish-nuget: pack ## Publish package to NuGet.org (requires NUGET_API_KEY environment variable)
	@if [ -z "$$NUGET_API_KEY" ]; then \
		echo "$(RED)Error: NUGET_API_KEY environment variable not set$(NC)"; \
		exit 1; \
	fi
	@echo "$(BLUE)Publishing to NuGet.org...$(NC)"
	@dotnet nuget push $(OUTPUT_DIR)/*.nupkg \
		--api-key $$NUGET_API_KEY \
		--source https://api.nuget.org/v3/index.json \
		--skip-duplicate
	@echo "$(GREEN)Package published successfully!$(NC)"

clean: ## Clean build artifacts
	@echo "$(BLUE)Cleaning build artifacts...$(NC)"
	@dotnet clean --configuration $(CONFIGURATION)
	@rm -rf $(OUTPUT_DIR)
	@rm -rf **/bin **/obj
	@echo "$(GREEN)Clean complete!$(NC)"

format: ## Format code using dotnet format
	@echo "$(BLUE)Formatting code...$(NC)"
	@dotnet format

format-check: ## Check code formatting without making changes
	@echo "$(BLUE)Checking code format...$(NC)"
	@dotnet format --verify-no-changes

lint: format-check build ## Run linting and code analysis
	@echo "$(BLUE)Running code analysis...$(NC)"
	@dotnet build --configuration $(CONFIGURATION) --no-restore /p:TreatWarningsAsErrors=true

run-sample: build ## Run the sample application
	@echo "$(BLUE)Running sample application...$(NC)"
	@echo "$(YELLOW)Note: Set DD_API_KEY environment variable or use user secrets$(NC)"
	@cd samples/Datadog.Metrics.Sample && dotnet run --configuration $(CONFIGURATION) --no-build

run-sample-debug: build-debug ## Run the sample application in debug mode
	@cd samples/Datadog.Metrics.Sample && dotnet run --configuration Debug --no-build

watch: ## Build and watch for changes
	@echo "$(BLUE)Watching for changes (Ctrl+C to exit)...$(NC)"
	@dotnet watch --project src/Datadog.Metrics build

install-tools: ## Install required .NET tools
	@echo "$(BLUE)Installing .NET tools...$(NC)"
	@dotnet tool restore || dotnet tool install -g dotnet-format

list-packages: ## List all NuGet package references
	@echo "$(BLUE)NuGet package references:$(NC)"
	@dotnet list package

outdated: ## Check for outdated packages
	@echo "$(BLUE)Checking for outdated packages...$(NC)"
	@dotnet list package --outdated

update-packages: ## Update all packages to latest versions
	@echo "$(BLUE)Updating packages...$(NC)"
	@dotnet list package --outdated | grep ">" | awk '{print $$2}' | xargs -I {} dotnet add package {}

info: ## Display project information
	@echo "$(BLUE)Project Information$(NC)"
	@echo "  Configuration: $(CONFIGURATION)"
	@echo "  Version: $(VERSION)"
	@echo "  Output Directory: $(OUTPUT_DIR)"
	@echo "  .NET SDK: $$(dotnet --version)"
	@echo ""
	@echo "$(BLUE)Available Target Frameworks:$(NC)"
	@grep -h "TargetFrameworks" src/Datadog.Metrics/Datadog.Metrics.csproj | sed 's/.*>\(.*\)<.*/  \1/'

ci: restore format-check build test ## Run CI pipeline locally
	@echo "$(GREEN)CI pipeline completed successfully!$(NC)"

init: install-tools restore ## Initialize development environment
	@echo "$(GREEN)Development environment initialized!$(NC)"
	@echo ""
	@echo "$(YELLOW)Quick start:$(NC)"
	@echo "  1. Set your Datadog API key:"
	@echo "     export DD_API_KEY=your-api-key"
	@echo "  2. Run the sample:"
	@echo "     make run-sample"
