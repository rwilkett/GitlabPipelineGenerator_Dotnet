using GitlabPipelineGenerator.CLI.Models;

namespace GitlabPipelineGenerator.CLI.Services;

/// <summary>
/// Service for providing comprehensive help documentation and examples
/// </summary>
public static class HelpService
{
    /// <summary>
    /// Gets the main help text for the application
    /// </summary>
    /// <returns>Formatted help text</returns>
    public static string GetMainHelp()
    {
        return @"
GitLab Pipeline Generator CLI
=============================

A .NET tool for generating GitLab CI/CD pipeline configurations.

USAGE:
    gitlab-pipeline-generator [OPTIONS]

DESCRIPTION:
    This tool generates GitLab CI/CD pipeline YAML configurations based on your project type
    and requirements. It supports multiple project types and provides extensive customization
    options for creating production-ready pipelines.

SUPPORTED PROJECT TYPES:
    dotnet      - .NET projects (6.0, 7.0, 8.0, 9.0)
    nodejs      - Node.js projects
    python      - Python projects
    docker      - Docker-based projects
    generic     - Generic projects with custom configuration

BASIC EXAMPLES:
    # Generate a basic .NET pipeline
    gitlab-pipeline-generator --type dotnet --dotnet-version 9.0

    # Generate with custom stages
    gitlab-pipeline-generator --type dotnet --stages build,test,deploy

    # Generate with custom output file
    gitlab-pipeline-generator --type nodejs --output my-pipeline.yml

    # Generate without deployment stage
    gitlab-pipeline-generator --type python --include-deployment false

For more detailed examples, use: gitlab-pipeline-generator --help-examples
For project-specific help, use: gitlab-pipeline-generator --help-<project-type>
";
    }

    /// <summary>
    /// Gets comprehensive usage examples
    /// </summary>
    /// <returns>Formatted examples</returns>
    public static string GetExamples()
    {
        return @"
GitLab Pipeline Generator - Usage Examples
==========================================

BASIC USAGE:
------------

1. Simple .NET pipeline:
   gitlab-pipeline-generator --type dotnet --dotnet-version 9.0

2. Node.js pipeline with custom stages:
   gitlab-pipeline-generator --type nodejs --stages build,test,deploy,release

3. Python pipeline with specific Docker image:
   gitlab-pipeline-generator --type python --docker-image python:3.11

4. Generic pipeline with custom configuration:
   gitlab-pipeline-generator --type generic --stages prepare,build,test

ADVANCED CONFIGURATION:
-----------------------

5. .NET pipeline with comprehensive features:
   gitlab-pipeline-generator \
     --type dotnet \
     --dotnet-version 9.0 \
     --include-code-quality \
     --include-security \
     --include-performance \
     --variables ""BUILD_CONFIGURATION=Release,ASPNETCORE_ENVIRONMENT=Production""

6. Multi-environment deployment:
   gitlab-pipeline-generator \
     --type nodejs \
     --environments ""staging:https://staging.example.com,production:https://example.com"" \
     --manual-stages deploy

7. Pipeline with caching optimization:
   gitlab-pipeline-generator \
     --type dotnet \
     --cache-paths ""~/.nuget/packages,obj,bin"" \
     --cache-key ""nuget-$CI_COMMIT_REF_SLUG""

8. Custom artifact configuration:
   gitlab-pipeline-generator \
     --type python \
     --artifact-paths ""dist,reports"" \
     --artifact-expire ""2 weeks""

VALIDATION AND TESTING:
-----------------------

9. Validate configuration without generating:
   gitlab-pipeline-generator --type dotnet --validate-only

10. Dry run to preview pipeline:
    gitlab-pipeline-generator --type nodejs --dry-run --verbose

11. Output to console for inspection:
    gitlab-pipeline-generator --type python --console-output

TEMPLATE USAGE:
---------------

12. List available templates:
    gitlab-pipeline-generator --type dotnet --list-templates

13. Use specific template:
    gitlab-pipeline-generator \
      --type dotnet \
      --template dotnet-standard \
      --template-params ""PROJECT_NAME=MyApp,SOLUTION_FILE=MyApp.sln""

RUNNER AND INFRASTRUCTURE:
--------------------------

14. Specify runner tags:
    gitlab-pipeline-generator \
      --type docker \
      --runner-tags ""docker,linux,large""

15. Custom Docker image with runner tags:
    gitlab-pipeline-generator \
      --type nodejs \
      --docker-image node:18-alpine \
      --runner-tags ""docker,kubernetes""

OUTPUT OPTIONS:
---------------

16. Custom output location:
    gitlab-pipeline-generator --type dotnet --output pipelines/.gitlab-ci.yml

17. Verbose output with statistics:
    gitlab-pipeline-generator --type python --verbose

18. Generate multiple configurations:
    # Development pipeline
    gitlab-pipeline-generator --type dotnet --stages build,test --output .gitlab-ci-dev.yml
    
    # Production pipeline
    gitlab-pipeline-generator --type dotnet --include-deployment --output .gitlab-ci-prod.yml

TROUBLESHOOTING:
----------------

19. Debug configuration issues:
    gitlab-pipeline-generator --type dotnet --verbose --dry-run

20. Validate complex configuration:
    gitlab-pipeline-generator \
      --type nodejs \
      --variables ""NODE_ENV=production,API_URL=https://api.example.com"" \
      --environments ""prod:https://example.com"" \
      --validate-only \
      --verbose

For project-specific examples, use:
  --help-dotnet, --help-nodejs, --help-python, --help-docker, --help-generic
";
    }

    /// <summary>
    /// Gets .NET specific help and examples
    /// </summary>
    /// <returns>Formatted .NET help</returns>
    public static string GetDotNetHelp()
    {
        return @"
.NET Project Pipeline Generation
================================

The .NET project type generates pipelines optimized for .NET applications with support
for building, testing, and deploying .NET projects.

SUPPORTED .NET VERSIONS:
  6.0, 7.0, 8.0, 9.0

COMMON .NET PIPELINE STAGES:
  build       - Restore packages and build the solution
  test        - Run unit tests and generate coverage reports
  deploy      - Deploy to target environments

.NET SPECIFIC OPTIONS:
  --dotnet-version        Specify .NET version (6.0, 7.0, 8.0, 9.0)
  --include-tests         Include test execution (default: true)
  --include-code-quality  Include code quality analysis
  --include-security      Include security scanning

EXAMPLES:

1. Basic .NET 9.0 pipeline:
   gitlab-pipeline-generator --type dotnet --dotnet-version 9.0

2. .NET pipeline with code quality:
   gitlab-pipeline-generator \
     --type dotnet \
     --dotnet-version 8.0 \
     --include-code-quality \
     --include-security

3. .NET pipeline with custom caching:
   gitlab-pipeline-generator \
     --type dotnet \
     --cache-paths ""~/.nuget/packages,**/obj,**/bin"" \
     --cache-key ""nuget-$CI_COMMIT_REF_SLUG""

4. .NET pipeline with multiple environments:
   gitlab-pipeline-generator \
     --type dotnet \
     --environments ""staging:https://staging-api.example.com,production:https://api.example.com"" \
     --variables ""ASPNETCORE_ENVIRONMENT=Production""

5. .NET pipeline with custom Docker image:
   gitlab-pipeline-generator \
     --type dotnet \
     --dotnet-version 9.0 \
     --docker-image mcr.microsoft.com/dotnet/sdk:9.0

GENERATED PIPELINE FEATURES:
  - NuGet package restoration
  - Solution/project building
  - Unit test execution with coverage
  - Artifact publishing
  - Multi-stage deployment support
  - Environment-specific configuration
";
    }

    /// <summary>
    /// Gets Node.js specific help and examples
    /// </summary>
    /// <returns>Formatted Node.js help</returns>
    public static string GetNodeJsHelp()
    {
        return @"
Node.js Project Pipeline Generation
===================================

The Node.js project type generates pipelines optimized for Node.js applications with
support for npm/yarn workflows, testing, and deployment.

COMMON NODE.JS PIPELINE STAGES:
  build       - Install dependencies and build the application
  test        - Run tests and generate coverage reports
  deploy      - Deploy to target environments

NODE.JS SPECIFIC FEATURES:
  - Automatic package manager detection (npm/yarn)
  - Node.js version management
  - Dependency caching optimization
  - Test framework integration

EXAMPLES:

1. Basic Node.js pipeline:
   gitlab-pipeline-generator --type nodejs

2. Node.js pipeline with specific Docker image:
   gitlab-pipeline-generator \
     --type nodejs \
     --docker-image node:18-alpine

3. Node.js pipeline with caching:
   gitlab-pipeline-generator \
     --type nodejs \
     --cache-paths ""node_modules,.npm"" \
     --cache-key ""npm-$CI_COMMIT_REF_SLUG""

4. Node.js pipeline with code quality:
   gitlab-pipeline-generator \
     --type nodejs \
     --include-code-quality \
     --include-security

5. Node.js pipeline with custom variables:
   gitlab-pipeline-generator \
     --type nodejs \
     --variables ""NODE_ENV=production,API_URL=https://api.example.com""

GENERATED PIPELINE FEATURES:
  - Dependency installation (npm install/yarn install)
  - Build script execution
  - Test execution with coverage
  - Linting and code quality checks
  - Security vulnerability scanning
  - Artifact generation and deployment
";
    }

    /// <summary>
    /// Gets Python specific help and examples
    /// </summary>
    /// <returns>Formatted Python help</returns>
    public static string GetPythonHelp()
    {
        return @"
Python Project Pipeline Generation
==================================

The Python project type generates pipelines optimized for Python applications with
support for pip/poetry workflows, testing, and deployment.

COMMON PYTHON PIPELINE STAGES:
  build       - Install dependencies and prepare the application
  test        - Run tests with pytest and generate coverage
  deploy      - Deploy to target environments

PYTHON SPECIFIC FEATURES:
  - Virtual environment management
  - Dependency management (pip/poetry)
  - Python version support
  - Test framework integration (pytest, unittest)

EXAMPLES:

1. Basic Python pipeline:
   gitlab-pipeline-generator --type python

2. Python pipeline with specific version:
   gitlab-pipeline-generator \
     --type python \
     --docker-image python:3.11

3. Python pipeline with caching:
   gitlab-pipeline-generator \
     --type python \
     --cache-paths "".venv,pip-cache"" \
     --cache-key ""python-$CI_COMMIT_REF_SLUG""

4. Python pipeline with code quality:
   gitlab-pipeline-generator \
     --type python \
     --include-code-quality \
     --include-security

5. Python pipeline with custom variables:
   gitlab-pipeline-generator \
     --type python \
     --variables ""PYTHON_ENV=production,DATABASE_URL=postgresql://..."" \
     --environments ""staging:https://staging.example.com""

GENERATED PIPELINE FEATURES:
  - Virtual environment creation
  - Dependency installation (pip/poetry)
  - Code formatting and linting (black, flake8)
  - Test execution with pytest
  - Coverage reporting
  - Security scanning with bandit
  - Package building and deployment
";
    }

    /// <summary>
    /// Gets Docker specific help and examples
    /// </summary>
    /// <returns>Formatted Docker help</returns>
    public static string GetDockerHelp()
    {
        return @"
Docker Project Pipeline Generation
==================================

The Docker project type generates pipelines optimized for containerized applications
with support for building, testing, and deploying Docker images.

COMMON DOCKER PIPELINE STAGES:
  build       - Build Docker images
  test        - Run container tests
  deploy      - Deploy containers to registries/environments

DOCKER SPECIFIC FEATURES:
  - Multi-stage Docker builds
  - Image registry integration
  - Container security scanning
  - Image optimization

EXAMPLES:

1. Basic Docker pipeline:
   gitlab-pipeline-generator --type docker

2. Docker pipeline with custom registry:
   gitlab-pipeline-generator \
     --type docker \
     --variables ""DOCKER_REGISTRY=registry.example.com""

3. Docker pipeline with security scanning:
   gitlab-pipeline-generator \
     --type docker \
     --include-security

4. Docker pipeline with multi-stage deployment:
   gitlab-pipeline-generator \
     --type docker \
     --environments ""staging:registry.example.com/app:staging,production:registry.example.com/app:latest""

5. Docker pipeline with custom build args:
   gitlab-pipeline-generator \
     --type docker \
     --variables ""BUILD_ARG_VERSION=1.0.0,BUILD_ARG_ENV=production""

GENERATED PIPELINE FEATURES:
  - Docker image building
  - Image tagging and versioning
  - Registry authentication
  - Security vulnerability scanning
  - Multi-architecture builds
  - Container deployment
";
    }

    /// <summary>
    /// Gets generic project help and examples
    /// </summary>
    /// <returns>Formatted generic help</returns>
    public static string GetGenericHelp()
    {
        return @"
Generic Project Pipeline Generation
===================================

The generic project type generates flexible pipelines that can be customized for
any project type or technology stack.

COMMON GENERIC PIPELINE STAGES:
  build       - Custom build commands
  test        - Custom test execution
  deploy      - Custom deployment scripts

GENERIC PROJECT FEATURES:
  - Flexible stage configuration
  - Custom script support
  - Variable substitution
  - Artifact management

EXAMPLES:

1. Basic generic pipeline:
   gitlab-pipeline-generator --type generic

2. Generic pipeline with custom stages:
   gitlab-pipeline-generator \
     --type generic \
     --stages ""prepare,compile,test,package,deploy""

3. Generic pipeline with custom Docker image:
   gitlab-pipeline-generator \
     --type generic \
     --docker-image ubuntu:22.04

4. Generic pipeline with variables:
   gitlab-pipeline-generator \
     --type generic \
     --variables ""BUILD_TOOL=make,TARGET=production""

5. Generic pipeline with artifacts:
   gitlab-pipeline-generator \
     --type generic \
     --artifact-paths ""build,dist,reports"" \
     --artifact-expire ""1 month""

GENERATED PIPELINE FEATURES:
  - Customizable build scripts
  - Flexible stage definitions
  - Variable interpolation
  - Artifact collection
  - Custom deployment logic
";
    }

    /// <summary>
    /// Gets troubleshooting help
    /// </summary>
    /// <returns>Formatted troubleshooting guide</returns>
    public static string GetTroubleshootingHelp()
    {
        return @"
Troubleshooting Guide
=====================

COMMON ISSUES AND SOLUTIONS:

1. VALIDATION ERRORS:
   Problem: ""Invalid project type"" error
   Solution: Use one of: dotnet, nodejs, python, docker, generic
   
   Problem: ""Invalid .NET version"" error
   Solution: Use supported versions: 6.0, 7.0, 8.0, 9.0

2. CONFIGURATION ISSUES:
   Problem: Variables not working
   Solution: Use format --variables ""KEY1=value1,KEY2=value2""
   
   Problem: Environment URLs invalid
   Solution: Include protocol: --environments ""prod:https://example.com""

3. OUTPUT ISSUES:
   Problem: File not created
   Solution: Check directory permissions and path validity
   
   Problem: YAML format issues
   Solution: Use --validate-only to check configuration first

4. PIPELINE EXECUTION ISSUES:
   Problem: Jobs failing in GitLab
   Solution: Check Docker image availability and runner compatibility

DEBUGGING COMMANDS:

# Validate configuration without generating
gitlab-pipeline-generator --type dotnet --validate-only --verbose

# Preview pipeline without writing file
gitlab-pipeline-generator --type nodejs --dry-run --verbose

# Check generated YAML format
gitlab-pipeline-generator --type python --console-output

# Test with minimal configuration
gitlab-pipeline-generator --type generic --stages build

GETTING HELP:

For more help:
  --help              Show main help
  --help-examples     Show usage examples
  --help-<type>       Show project-specific help
  --help-troubleshoot Show this troubleshooting guide

Report issues at: https://github.com/your-repo/issues
";
    }

    /// <summary>
    /// Gets help for a specific project type
    /// </summary>
    /// <param name="projectType">Project type to get help for</param>
    /// <returns>Project-specific help text</returns>
    public static string GetProjectTypeHelp(string projectType)
    {
        return projectType.ToLowerInvariant() switch
        {
            "dotnet" => GetDotNetHelp(),
            "nodejs" => GetNodeJsHelp(),
            "python" => GetPythonHelp(),
            "docker" => GetDockerHelp(),
            "generic" => GetGenericHelp(),
            _ => $"Help not available for project type '{projectType}'. Supported types: dotnet, nodejs, python, docker, generic"
        };
    }

    /// <summary>
    /// Checks if the arguments contain help requests and displays appropriate help
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <returns>True if help was displayed, false otherwise</returns>
    public static bool HandleHelpRequest(string[] args)
    {
        if (args == null || args.Length == 0)
            return false;

        var helpArg = args.FirstOrDefault(arg => 
            arg.StartsWith("--help", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("help", StringComparison.OrdinalIgnoreCase));

        if (helpArg == null)
            return false;

        switch (helpArg.ToLowerInvariant())
        {
            case "--help-examples":
                Console.WriteLine(GetExamples());
                return true;
            case "--help-dotnet":
                Console.WriteLine(GetDotNetHelp());
                return true;
            case "--help-nodejs":
                Console.WriteLine(GetNodeJsHelp());
                return true;
            case "--help-python":
                Console.WriteLine(GetPythonHelp());
                return true;
            case "--help-docker":
                Console.WriteLine(GetDockerHelp());
                return true;
            case "--help-generic":
                Console.WriteLine(GetGenericHelp());
                return true;
            case "--help-troubleshoot":
                Console.WriteLine(GetTroubleshootingHelp());
                return true;
            case "--help":
            case "-h":
            case "help":
                Console.WriteLine(GetMainHelp());
                return true;
            default:
                return false;
        }
    }
}