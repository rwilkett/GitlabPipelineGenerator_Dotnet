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
    
    NEW: GitLab API Integration - Automatically analyze your GitLab projects to generate
    intelligent pipelines based on detected project structure, dependencies, and configuration.

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

GITLAB API INTEGRATION:
    # Analyze GitLab project and generate intelligent pipeline
    gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project group/project

    # List your GitLab projects
    gitlab-pipeline-generator --list-projects --gitlab-token <token>

    # Search GitLab projects
    gitlab-pipeline-generator --search-projects ""my-app"" --gitlab-token <token>

For more detailed examples, use: gitlab-pipeline-generator --help-examples
For project-specific help, use: gitlab-pipeline-generator --help-<project-type>
For GitLab integration help, use: gitlab-pipeline-generator --help-gitlab
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

GITLAB API INTEGRATION:
-----------------------

19. List your GitLab projects:
    gitlab-pipeline-generator --list-projects --gitlab-token <your-token>

20. Search for specific projects:
    gitlab-pipeline-generator --search-projects ""my-app"" --gitlab-token <your-token>

21. Analyze project and generate intelligent pipeline:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/my-project

22. Analyze with custom GitLab instance:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-url https://gitlab.company.com \
      --gitlab-token <your-token> \
      --gitlab-project 123

23. Comprehensive analysis with preview:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --analysis-depth 3 \
      --show-analysis \
      --show-conflicts

24. Hybrid mode (analysis + manual overrides):
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --type dotnet \
      --dotnet-version 9.0 \
      --include-security

TROUBLESHOOTING:
----------------

25. Debug configuration issues:
    gitlab-pipeline-generator --type dotnet --verbose --dry-run

26. Validate complex configuration:
    gitlab-pipeline-generator \
      --type nodejs \
      --variables ""NODE_ENV=production,API_URL=https://api.example.com"" \
      --environments ""prod:https://example.com"" \
      --validate-only \
      --verbose

27. Test GitLab authentication:
    gitlab-pipeline-generator --list-projects --gitlab-token <your-token> --verbose

For project-specific examples, use:
  --help-dotnet, --help-nodejs, --help-python, --help-docker, --help-generic
For GitLab integration help, use:
  --help-gitlab, --help-gitlab-auth, --help-gitlab-analysis
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
    /// Gets GitLab API integration help and examples
    /// </summary>
    /// <returns>Formatted GitLab help</returns>
    public static string GetGitLabHelp()
    {
        return @"
GitLab API Integration
======================

The GitLab API integration enables automatic project analysis and intelligent pipeline
generation by connecting to GitLab's v4 API to analyze your project structure,
dependencies, and existing configurations.

AUTHENTICATION:
  --gitlab-token <token>     Personal access token for GitLab API
  --gitlab-url <url>         GitLab instance URL (default: https://gitlab.com)
  --gitlab-profile <name>    Use stored GitLab connection profile

PROJECT DISCOVERY:
  --list-projects            List accessible GitLab projects
  --search-projects <term>   Search projects by name or description
  --project-filter <types>   Filter projects: owned,member,public,private,internal
  --max-projects <number>    Maximum projects to display (default: 50)

PROJECT ANALYSIS:
  --analyze-project          Enable automatic project analysis
  --gitlab-project <id>      GitLab project ID or path (e.g., 'group/project')
  --analysis-depth <level>   Analysis depth: 1=basic, 2=standard, 3=comprehensive
  --skip-analysis <types>    Skip analysis: files,dependencies,config,deployment
  --analysis-exclude <patterns> Exclude file patterns from analysis
  --show-analysis            Display analysis results before generation

HYBRID MODE:
  --prefer-detected          Prefer detected settings over CLI options
  --merge-config             Merge detected and manual configurations (default)
  --show-conflicts           Show conflicts between detected and manual settings

EXAMPLES:

1. List your GitLab projects:
   gitlab-pipeline-generator --list-projects --gitlab-token <your-token>

2. Search for specific projects:
   gitlab-pipeline-generator --search-projects ""my-app"" --gitlab-token <your-token>

3. Analyze a project and generate pipeline:
   gitlab-pipeline-generator \
     --analyze-project \
     --gitlab-token <your-token> \
     --gitlab-project group/my-project

4. Analyze with custom GitLab instance:
   gitlab-pipeline-generator \
     --analyze-project \
     --gitlab-url https://gitlab.company.com \
     --gitlab-token <your-token> \
     --gitlab-project 123

5. Comprehensive analysis with preview:
   gitlab-pipeline-generator \
     --analyze-project \
     --gitlab-token <your-token> \
     --gitlab-project group/project \
     --analysis-depth 3 \
     --show-analysis \
     --show-conflicts

6. Hybrid mode (analysis + manual overrides):
   gitlab-pipeline-generator \
     --analyze-project \
     --gitlab-token <your-token> \
     --gitlab-project group/project \
     --type dotnet \
     --dotnet-version 9.0 \
     --include-security

7. Filter project discovery:
   gitlab-pipeline-generator \
     --list-projects \
     --gitlab-token <your-token> \
     --project-filter owned,private \
     --max-projects 20

8. Exclude files from analysis:
   gitlab-pipeline-generator \
     --analyze-project \
     --gitlab-token <your-token> \
     --gitlab-project group/project \
     --analysis-exclude ""*.min.js,node_modules/**,dist/**""

AUTHENTICATION SETUP:

1. Create a GitLab Personal Access Token:
   - Go to GitLab → User Settings → Access Tokens
   - Create token with 'read_api' and 'read_repository' scopes
   - Copy the token for use with --gitlab-token

2. For self-hosted GitLab:
   - Use --gitlab-url to specify your GitLab instance
   - Ensure your token has appropriate permissions

ANALYSIS FEATURES:

The analysis engine automatically detects:
  - Project type (.NET, Node.js, Python, Docker, etc.)
  - Framework versions and dependencies
  - Build tools and test frameworks
  - Existing CI/CD configurations
  - Docker configurations
  - Deployment targets and environments

TROUBLESHOOTING:

Common issues:
  - Invalid token: Ensure token has 'read_api' and 'read_repository' scopes
  - Project not found: Check project ID/path and access permissions
  - Analysis fails: Use --verbose for detailed error information
  - Rate limiting: The tool respects GitLab rate limits automatically

For more help: --help-gitlab-auth, --help-gitlab-analysis
";
    }

    /// <summary>
    /// Gets GitLab authentication help
    /// </summary>
    /// <returns>Formatted GitLab authentication help</returns>
    public static string GetGitLabAuthHelp()
    {
        return @"
GitLab Authentication Setup
===========================

PERSONAL ACCESS TOKEN SETUP:

1. Create Token in GitLab:
   - Navigate to GitLab → User Settings → Access Tokens
   - Token name: ""Pipeline Generator""
   - Expiration: Set appropriate expiration date
   - Scopes: Select 'read_api' and 'read_repository'
   - Click 'Create personal access token'
   - Copy the generated token immediately

2. Required Token Scopes:
   read_api         - Required for project discovery and metadata
   read_repository  - Required for analyzing project files and structure

3. Optional Token Scopes:
   read_user        - For enhanced user information display

USING THE TOKEN:

Command line usage:
  gitlab-pipeline-generator --gitlab-token <your-token> --analyze-project --gitlab-project group/project

Environment variable (alternative):
  export GITLAB_TOKEN=<your-token>
  gitlab-pipeline-generator --analyze-project --gitlab-project group/project

SELF-HOSTED GITLAB:

For GitLab instances other than gitlab.com:
  gitlab-pipeline-generator \
    --gitlab-url https://gitlab.company.com \
    --gitlab-token <your-token> \
    --analyze-project \
    --gitlab-project group/project

SECURITY CONSIDERATIONS:

1. Token Storage:
   - Never commit tokens to version control
   - Use environment variables or secure credential storage
   - Set appropriate token expiration dates

2. Token Permissions:
   - Use minimal required scopes (read_api, read_repository)
   - Avoid using tokens with write permissions for analysis

3. Network Security:
   - Always use HTTPS for GitLab connections
   - Verify SSL certificates for self-hosted instances

TROUBLESHOOTING AUTHENTICATION:

Error: ""401 Unauthorized""
  - Check token validity and expiration
  - Verify token has required scopes
  - Ensure correct GitLab URL

Error: ""403 Forbidden""
  - Check project access permissions
  - Verify project exists and is accessible
  - Check if project is private and token has access

Error: ""404 Not Found""
  - Verify project ID or path is correct
  - Check if project exists
  - Ensure you have access to the project

TESTING AUTHENTICATION:

Test your token:
  gitlab-pipeline-generator --list-projects --gitlab-token <your-token>

Test project access:
  gitlab-pipeline-generator --gitlab-token <your-token> --gitlab-project <project-id> --show-analysis --dry-run
";
    }

    /// <summary>
    /// Gets GitLab project analysis help
    /// </summary>
    /// <returns>Formatted GitLab analysis help</returns>
    public static string GetGitLabAnalysisHelp()
    {
        return @"
GitLab Project Analysis
=======================

ANALYSIS OVERVIEW:

The analysis engine examines your GitLab project to automatically detect:
  - Project type and technology stack
  - Framework versions and dependencies
  - Build tools and test frameworks
  - Existing CI/CD configurations
  - Docker and deployment configurations

ANALYSIS DEPTH LEVELS:

Level 1 (Basic):
  - File pattern analysis for project type detection
  - Basic dependency scanning
  - Existing CI/CD file detection

Level 2 (Standard - Default):
  - Comprehensive file analysis
  - Dependency version analysis
  - Framework feature detection
  - Build tool configuration analysis

Level 3 (Comprehensive):
  - Deep dependency analysis with security scanning
  - Advanced configuration parsing
  - Performance optimization recommendations
  - Deployment target analysis

ANALYSIS TYPES:

Files Analysis:
  - Scans project structure and file patterns
  - Detects project type, frameworks, and build tools
  - Identifies test frameworks and configuration files

Dependencies Analysis:
  - Parses package files (package.json, *.csproj, requirements.txt, etc.)
  - Extracts dependency versions and constraints
  - Recommends caching strategies
  - Suggests security scanning based on dependencies

Configuration Analysis:
  - Analyzes existing CI/CD files (.gitlab-ci.yml, GitHub Actions, etc.)
  - Parses Docker configurations (Dockerfile, docker-compose.yml)
  - Detects deployment configurations and scripts

Deployment Analysis:
  - Identifies deployment targets and strategies
  - Analyzes environment configurations
  - Detects containerization setups

CUSTOMIZING ANALYSIS:

Skip specific analysis types:
  gitlab-pipeline-generator \
    --analyze-project \
    --gitlab-token <token> \
    --gitlab-project group/project \
    --skip-analysis dependencies,deployment

Exclude files from analysis:
  gitlab-pipeline-generator \
    --analyze-project \
    --gitlab-token <token> \
    --gitlab-project group/project \
    --analysis-exclude ""*.min.js,node_modules/**,vendor/**""

Control analysis depth:
  gitlab-pipeline-generator \
    --analyze-project \
    --gitlab-token <token> \
    --gitlab-project group/project \
    --analysis-depth 1

ANALYSIS OUTPUT:

View analysis results:
  gitlab-pipeline-generator \
    --analyze-project \
    --gitlab-token <token> \
    --gitlab-project group/project \
    --show-analysis \
    --dry-run

Show configuration conflicts:
  gitlab-pipeline-generator \
    --analyze-project \
    --gitlab-token <token> \
    --gitlab-project group/project \
    --type dotnet \
    --show-conflicts

HYBRID MODE:

Combine analysis with manual settings:
  gitlab-pipeline-generator \
    --analyze-project \
    --gitlab-token <token> \
    --gitlab-project group/project \
    --type dotnet \
    --dotnet-version 9.0 \
    --include-security

Prefer detected settings:
  gitlab-pipeline-generator \
    --analyze-project \
    --gitlab-token <token> \
    --gitlab-project group/project \
    --prefer-detected \
    --type nodejs

SUPPORTED PROJECT TYPES:

The analysis engine can detect:
  - .NET projects (Framework, Core, 5+)
  - Node.js projects (npm, yarn, pnpm)
  - Python projects (pip, poetry, pipenv)
  - Java projects (Maven, Gradle)
  - Docker projects
  - Static sites (HTML, Jekyll, Hugo)
  - And many more...

ANALYSIS LIMITATIONS:

- Analysis requires read access to project files
- Large projects may take longer to analyze
- Some proprietary build tools may not be detected
- Analysis accuracy depends on project structure

TROUBLESHOOTING ANALYSIS:

Slow analysis:
  - Reduce analysis depth: --analysis-depth 1
  - Exclude large directories: --analysis-exclude ""node_modules/**,vendor/**""

Inaccurate detection:
  - Use hybrid mode with manual overrides
  - Check project structure follows standard conventions
  - Use --verbose for detailed analysis information

Analysis fails:
  - Check project permissions and token scopes
  - Verify project exists and is accessible
  - Use --verbose for detailed error information

For more help: --help-gitlab, --help-gitlab-auth

Analysis fails:
  - Check project permissions and file access
  - Verify project contains recognizable files
  - Use --skip-analysis to exclude problematic analysis types
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
            case "--help-gitlab":
                Console.WriteLine(GetGitLabHelp());
                return true;
            case "--help-gitlab-auth":
                Console.WriteLine(GetGitLabAuthHelp());
                return true;
            case "--help-gitlab-analysis":
                Console.WriteLine(GetGitLabAnalysisHelp());
                return true;
            case "--help-troubleshoot":
                Console.WriteLine(GetTroubleshootingHelp());
                return true;
            case "--help-gitlab-troubleshoot":
                Console.WriteLine(GetGitLabTroubleshootingHelp());
                return true;
            case "--help-gitlab-examples":
                Console.WriteLine(GetGitLabExamples());
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

    /// <summary>
    /// Gets GitLab API troubleshooting help
    /// </summary>
    /// <returns>Formatted GitLab troubleshooting guide</returns>
    public static string GetGitLabTroubleshootingHelp()
    {
        return @"
GitLab API Troubleshooting Guide
================================

AUTHENTICATION ISSUES:

Error: ""401 Unauthorized""
  Cause: Invalid or expired token
  Solutions:
    • Verify token is correct and hasn't expired
    • Check token has 'read_api' and 'read_repository' scopes
    • Generate a new personal access token
    • Test with: gitlab-pipeline-generator --list-projects --gitlab-token <token>

Error: ""403 Forbidden""
  Cause: Insufficient permissions
  Solutions:
    • Ensure you have at least 'Developer' access to the project
    • Check if project is private and token has access
    • Contact project owner for access
    • Verify token scopes include required permissions

Error: ""404 Not Found""
  Cause: Project doesn't exist or isn't accessible
  Solutions:
    • Verify project ID or path is correct (use --list-projects to check)
    • Ensure project exists and you have access
    • Check GitLab instance URL is correct
    • Use full project path: group/subgroup/project

NETWORK AND CONNECTION ISSUES:

Error: ""Connection timeout"" or ""Network error""
  Cause: Network connectivity issues
  Solutions:
    • Check internet connection
    • Verify GitLab instance is accessible
    • Check firewall settings
    • Try with --verbose for detailed network information

Error: ""SSL certificate error""
  Cause: SSL/TLS certificate issues with self-hosted GitLab
  Solutions:
    • Verify GitLab instance SSL certificate is valid
    • Check system certificate store
    • Contact GitLab administrator

RATE LIMITING:

Error: ""429 Too Many Requests""
  Cause: GitLab API rate limit exceeded
  Solutions:
    • Wait a few minutes before retrying
    • Reduce analysis scope: --analysis-depth 1
    • Skip analysis types: --skip-analysis dependencies,deployment
    • Use different token if available

PROJECT ANALYSIS ISSUES:

Error: ""Analysis failed"" or ""Incomplete analysis""
  Cause: Project structure or permission issues
  Solutions:
    • Check project has readable files
    • Verify token has 'read_repository' scope
    • Use --analysis-exclude to skip problematic directories
    • Try with reduced analysis depth

Error: ""Project type not detected""
  Cause: Unusual project structure
  Solutions:
    • Use hybrid mode with manual project type
    • Check project follows standard conventions
    • Use --verbose to see what files were analyzed
    • Manually specify --type parameter

PERFORMANCE ISSUES:

Slow project listing:
  Solutions:
    • Use --max-projects to limit results
    • Filter projects: --project-filter owned,member
    • Use specific search terms: --search-projects ""exact-name""

Slow project analysis:
  Solutions:
    • Reduce analysis depth: --analysis-depth 1
    • Exclude large directories: --analysis-exclude ""node_modules/**,dist/**""
    • Skip unnecessary analysis: --skip-analysis deployment

Large memory usage:
  Solutions:
    • Analyze smaller projects first
    • Use --analysis-exclude for large files
    • Restart tool between large analyses

CONFIGURATION ISSUES:

Error: ""Invalid GitLab URL""
  Cause: Malformed GitLab instance URL
  Solutions:
    • Include protocol: https://gitlab.example.com
    • Remove trailing slashes
    • Verify URL is accessible in browser

Error: ""Profile not found""
  Cause: GitLab profile doesn't exist
  Solutions:
    • List profiles: gitlab-pipeline-generator --list-profiles
    • Create profile or use direct token
    • Check profile name spelling

DEBUGGING COMMANDS:

Test authentication:
  gitlab-pipeline-generator --list-projects --gitlab-token <token> --verbose

Test project access:
  gitlab-pipeline-generator --gitlab-project <id> --show-analysis --dry-run --verbose

Test analysis without generation:
  gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project <id> --dry-run --verbose

Minimal test:
  gitlab-pipeline-generator --gitlab-token <token> --gitlab-project <id> --analysis-depth 1 --dry-run

COMMON SOLUTIONS:

1. Token Issues:
   • Regenerate personal access token with correct scopes
   • Use environment variable: export GITLAB_TOKEN=<token>
   • Test token with GitLab API directly

2. Project Access:
   • Verify project exists: check in GitLab web interface
   • Ensure you're a project member with appropriate role
   • Use project ID instead of path if path has special characters

3. Analysis Problems:
   • Start with basic analysis: --analysis-depth 1
   • Use hybrid mode: combine analysis with manual settings
   • Check project structure follows standard conventions

4. Performance:
   • Use specific project targeting instead of broad searches
   • Exclude unnecessary files and directories
   • Run analysis during off-peak hours

GETTING MORE HELP:

Verbose output:
  Add --verbose to any command for detailed information

Log files:
  Check application logs for detailed error information

Support:
  • Check documentation: --help-gitlab
  • Report issues with --verbose output
  • Include GitLab version and project type in reports

For immediate help:
  --help-gitlab          GitLab integration overview
  --help-gitlab-auth     Authentication setup
  --help-gitlab-analysis Project analysis details
";
    }

    /// <summary>
    /// Gets comprehensive GitLab integration examples
    /// </summary>
    /// <returns>Formatted GitLab examples</returns>
    public static string GetGitLabExamples()
    {
        return @"
GitLab Integration - Comprehensive Examples
===========================================

GETTING STARTED:

1. First-time setup - List your projects:
   gitlab-pipeline-generator --list-projects --gitlab-token <your-token>

2. Find a specific project:
   gitlab-pipeline-generator --search-projects ""my-app"" --gitlab-token <your-token>

3. Basic project analysis:
   gitlab-pipeline-generator \
     --analyze-project \
     --gitlab-token <your-token> \
     --gitlab-project group/my-project

AUTHENTICATION EXAMPLES:

4. Using environment variable:
   export GITLAB_TOKEN=<your-token>
   gitlab-pipeline-generator --analyze-project --gitlab-project 123

5. Self-hosted GitLab:
   gitlab-pipeline-generator \
     --gitlab-url https://gitlab.company.com \
     --gitlab-token <your-token> \
     --analyze-project \
     --gitlab-project internal/project

6. Using stored profile:
   gitlab-pipeline-generator \
     --gitlab-profile company \
     --analyze-project \
     --gitlab-project group/project

PROJECT DISCOVERY EXAMPLES:

7. List only your owned projects:
   gitlab-pipeline-generator \
     --list-projects \
     --gitlab-token <your-token> \
     --project-filter owned \
     --max-projects 20

8. Search with filters:
   gitlab-pipeline-generator \
     --search-projects ""api"" \
     --gitlab-token <your-token> \
     --project-filter member,private

9. Verbose project listing:
   gitlab-pipeline-generator \
     --list-projects \
     --gitlab-token <your-token> \
     --verbose

ANALYSIS EXAMPLES:

10. Comprehensive analysis with preview:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --analysis-depth 3 \
      --show-analysis \
      --dry-run

11. Quick analysis (basic level):
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --analysis-depth 1

12. Selective analysis:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --skip-analysis deployment,security

13. Analysis with file exclusions:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --analysis-exclude ""node_modules/**,*.min.js,dist/**""

HYBRID MODE EXAMPLES:

14. Analysis with manual overrides:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/dotnet-app \
      --type dotnet \
      --dotnet-version 9.0 \
      --include-security

15. Prefer detected settings:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --prefer-detected \
      --stages build,test,deploy

16. Show configuration conflicts:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --type nodejs \
      --show-conflicts

ADVANCED ANALYSIS EXAMPLES:

17. Multi-project analysis workflow:
    # First, find projects
    gitlab-pipeline-generator --search-projects ""microservice"" --gitlab-token <token>
    
    # Then analyze each
    gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project 123
    gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project 456

18. Analysis with custom output:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --output pipelines/intelligent-ci.yml \
      --show-analysis

19. Batch analysis with different depths:
    # Quick scan
    gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project group/app1 --analysis-depth 1 --output app1-basic.yml
    
    # Comprehensive analysis
    gitlab-pipeline-generator --analyze-project --gitlab-token <token> --gitlab-project group/app2 --analysis-depth 3 --output app2-full.yml

TROUBLESHOOTING EXAMPLES:

20. Debug authentication:
    gitlab-pipeline-generator \
      --list-projects \
      --gitlab-token <your-token> \
      --verbose

21. Debug project access:
    gitlab-pipeline-generator \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --show-analysis \
      --dry-run \
      --verbose

22. Test minimal analysis:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <your-token> \
      --gitlab-project group/project \
      --analysis-depth 1 \
      --skip-analysis dependencies,deployment \
      --dry-run

PRODUCTION WORKFLOW EXAMPLES:

23. CI/CD pipeline for multiple environments:
    # Development pipeline
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <token> \
      --gitlab-project group/app \
      --stages build,test \
      --output .gitlab-ci-dev.yml
    
    # Production pipeline
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <token> \
      --gitlab-project group/app \
      --include-deployment \
      --include-security \
      --output .gitlab-ci-prod.yml

24. Microservices pipeline generation:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <token> \
      --gitlab-project group/user-service \
      --environments ""staging:https://staging-users.example.com,production:https://users.example.com""

25. Full-featured enterprise pipeline:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <token> \
      --gitlab-project enterprise/main-app \
      --analysis-depth 3 \
      --include-code-quality \
      --include-security \
      --include-performance \
      --environments ""dev:https://dev.example.com,staging:https://staging.example.com,production:https://example.com"" \
      --variables ""BUILD_CONFIG=Release,ASPNETCORE_ENVIRONMENT=Production"" \
      --show-analysis \
      --show-conflicts

INTEGRATION WITH EXISTING WORKFLOWS:

26. Update existing pipeline:
    # Analyze current setup
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <token> \
      --gitlab-project group/project \
      --show-analysis \
      --dry-run
    
    # Generate updated pipeline
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <token> \
      --gitlab-project group/project \
      --output .gitlab-ci-new.yml

27. Compare with existing configuration:
    gitlab-pipeline-generator \
      --analyze-project \
      --gitlab-token <token> \
      --gitlab-project group/project \
      --show-conflicts \
      --console-output

For more examples: --help-examples, --help-<project-type>
For troubleshooting: --help-gitlab-troubleshoot
";
    }}
