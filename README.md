# GitLab Pipeline Generator

A powerful .NET CLI tool for generating GitLab CI/CD pipeline configurations with intelligent project analysis and automatic optimization.

## 🚀 Features

### Core Pipeline Generation
- **Multi-Project Support**: .NET, Node.js, Python, Docker, and Generic projects
- **Intelligent Templates**: Pre-built, optimized pipeline templates for each project type
- **Customizable Stages**: Build, test, deploy, security, and performance stages
- **Advanced Configuration**: Variables, environments, caching, and artifact management

### 🆕 GitLab API Integration
- **Automatic Project Analysis**: Connect to GitLab and analyze project structure automatically
- **Smart Detection**: Automatically detect project type, frameworks, dependencies, and build tools
- **Hybrid Mode**: Combine automatic analysis with manual configuration overrides
- **Project Discovery**: List and search your GitLab projects directly from the CLI

### Advanced Features
- **Security Scanning**: Integrated security analysis and vulnerability scanning
- **Performance Testing**: Performance testing stage generation
- **Code Quality**: Automated code quality checks and reporting
- **Multi-Environment**: Support for staging, production, and custom environments
- **Caching Optimization**: Intelligent caching strategies for faster builds

## 📦 Installation

### Prerequisites
- .NET 6.0 or later
- GitLab account (for API integration features)

### Install as Global Tool
```bash
dotnet tool install -g GitlabPipelineGenerator.CLI
```

### Install from Source
```bash
git clone https://github.com/your-repo/GitlabPipelineGenerator_Dotnet.git
cd GitlabPipelineGenerator_Dotnet
dotnet build
dotnet pack GitlabPipelineGenerator.CLI --configuration Release --verbosity quiet
dotnet tool install --global --add-source <path_to_nupkg_directory> GitlabPipelineGenerator.CLI
```

## 🎯 Quick Start

### Basic Pipeline Generation
```bash
# Generate a .NET pipeline
gitlab-pipeline-gen --type dotnet --dotnet-version 9.0

# Generate a Node.js pipeline
gitlab-pipeline-gen --type nodejs --stages build,test,deploy

# Generate with custom output
gitlab-pipeline-gen --type python --output my-pipeline.yml
```

### GitLab API Integration

#### 1. Setup Authentication
Create a GitLab Personal Access Token:
1. Go to GitLab → User Settings → Access Tokens
2. Create token with `read_api` and `read_repository` scopes
3. Copy the token for use with the CLI

#### 2. Discover Projects
```bash
# List your GitLab projects
gitlab-pipeline-generator --list-projects --gitlab-token <your-token>

# Search for specific projects
gitlab-pipeline-generator --search-projects "my-app" --gitlab-token <your-token>
```

#### 3. Intelligent Pipeline Generation
```bash
# Analyze project and generate intelligent pipeline
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <your-token> \
  --gitlab-project group/my-project

# Preview analysis before generation
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <your-token> \
  --gitlab-project group/my-project \
  --show-analysis \
  --dry-run
```

## 📖 Usage Examples

### Manual Pipeline Generation

#### .NET Projects
```bash
# Basic .NET pipeline
gitlab-pipeline-generator --type dotnet --dotnet-version 9.0

# .NET with security and code quality
gitlab-pipeline-generator \
  --type dotnet \
  --dotnet-version 8.0 \
  --include-security \
  --include-code-quality \
  --include-performance

# Multi-environment .NET deployment
gitlab-pipeline-generator \
  --type dotnet \
  --environments "staging:https://staging-api.example.com,production:https://api.example.com" \
  --variables "ASPNETCORE_ENVIRONMENT=Production"
```

#### Node.js Projects
```bash
# Basic Node.js pipeline
gitlab-pipeline-generator --type nodejs

# Node.js with custom Docker image and caching
gitlab-pipeline-generator \
  --type nodejs \
  --docker-image node:18-alpine \
  --cache-paths "node_modules,.npm" \
  --cache-key "npm-$CI_COMMIT_REF_SLUG"
```

#### Python Projects
```bash
# Python with code quality
gitlab-pipeline-generator \
  --type python \
  --include-code-quality \
  --include-security

# Python with custom variables
gitlab-pipeline-generator \
  --type python \
  --variables "PYTHON_ENV=production,DATABASE_URL=postgresql://..." \
  --environments "staging:https://staging.example.com"
```

### GitLab API Integration Examples

#### Project Discovery
```bash
# List owned projects only
gitlab-pipeline-generator \
  --list-projects \
  --gitlab-token <token> \
  --project-filter owned \
  --max-projects 20

# Search with filters
gitlab-pipeline-generator \
  --search-projects "api" \
  --gitlab-token <token> \
  --project-filter member,private
```

#### Intelligent Analysis
```bash
# Comprehensive analysis
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <token> \
  --gitlab-project group/project \
  --analysis-depth 3 \
  --show-analysis

# Quick analysis
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <token> \
  --gitlab-project group/project \
  --analysis-depth 1

# Selective analysis
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <token> \
  --gitlab-project group/project \
  --skip-analysis deployment,security
```

#### Hybrid Mode
```bash
# Combine analysis with manual settings
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <token> \
  --gitlab-project group/dotnet-app \
  --type dotnet \
  --dotnet-version 9.0 \
  --include-security

# Show configuration conflicts
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <token> \
  --gitlab-project group/project \
  --type nodejs \
  --show-conflicts
```

#### Self-Hosted GitLab
```bash
# Connect to self-hosted GitLab
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-url https://gitlab.company.com \
  --gitlab-token <token> \
  --gitlab-project internal/project
```

## 🔧 Configuration Options

### Project Types
- `dotnet` - .NET projects (6.0, 7.0, 8.0, 9.0)
- `nodejs` - Node.js projects
- `python` - Python projects  
- `docker` - Docker-based projects
- `generic` - Generic projects with custom configuration

### GitLab Integration Options
- `--gitlab-token <token>` - GitLab personal access token
- `--gitlab-url <url>` - GitLab instance URL (default: https://gitlab.com)
- `--gitlab-project <id>` - Project ID or path (e.g., 'group/project')
- `--analyze-project` - Enable automatic project analysis
- `--list-projects` - List accessible GitLab projects
- `--search-projects <term>` - Search projects by name

### Analysis Options
- `--analysis-depth <1-3>` - Analysis depth (1=basic, 2=standard, 3=comprehensive)
- `--skip-analysis <types>` - Skip analysis types: files,dependencies,config,deployment
- `--analysis-exclude <patterns>` - Exclude file patterns from analysis
- `--show-analysis` - Display analysis results before generation
- `--show-conflicts` - Show conflicts between detected and manual settings

### Pipeline Options
- `--stages <stages>` - Pipeline stages (comma-separated)
- `--variables <vars>` - Variables in KEY=value format
- `--environments <envs>` - Environments in name:url format
- `--include-security` - Include security scanning
- `--include-code-quality` - Include code quality checks
- `--include-performance` - Include performance testing

### Output Options
- `--output <path>` - Output file path (default: .gitlab-ci.yml)
- `--console-output` - Output to console instead of file
- `--dry-run` - Preview without generating file
- `--verbose` - Enable verbose output
- `--validate-only` - Validate configuration without generating

## 🛠️ Advanced Usage

### Environment Variables
```bash
# Set GitLab token via environment variable
export GITLAB_TOKEN=<your-token>
gitlab-pipeline-generator --analyze-project --gitlab-project group/project
```

### Batch Processing
```bash
# Generate pipelines for multiple projects
projects=("group/app1" "group/app2" "group/app3")
for project in "${projects[@]}"; do
  gitlab-pipeline-generator \
    --analyze-project \
    --gitlab-token $GITLAB_TOKEN \
    --gitlab-project "$project" \
    --output "pipelines/${project//\//-}.yml"
done
```

### CI/CD Integration
```yaml
# Use in GitLab CI to generate pipelines
generate-pipeline:
  image: mcr.microsoft.com/dotnet/sdk:9.0
  script:
    - dotnet tool install -g GitlabPipelineGenerator.CLI
    - gitlab-pipeline-generator --analyze-project --gitlab-token $GITLAB_TOKEN --gitlab-project $CI_PROJECT_PATH
  artifacts:
    paths:
      - .gitlab-ci.yml
```

## 🔍 Troubleshooting

### Authentication Issues
```bash
# Test authentication
gitlab-pipeline-generator --list-projects --gitlab-token <token> --verbose

# Check token scopes
# Ensure token has 'read_api' and 'read_repository' scopes
```

### Analysis Issues
```bash
# Debug analysis
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <token> \
  --gitlab-project group/project \
  --verbose \
  --dry-run

# Minimal analysis for troubleshooting
gitlab-pipeline-generator \
  --analyze-project \
  --gitlab-token <token> \
  --gitlab-project group/project \
  --analysis-depth 1 \
  --skip-analysis dependencies,deployment
```

### Common Solutions
- **401 Unauthorized**: Check token validity and scopes
- **403 Forbidden**: Verify project access permissions
- **404 Not Found**: Confirm project ID/path is correct
- **Rate Limited**: Wait and retry, or reduce analysis scope
- **Slow Analysis**: Use `--analysis-depth 1` or exclude large directories

## 📚 Help and Documentation

### Built-in Help
```bash
# Main help
gitlab-pipeline-generator --help

# Usage examples
gitlab-pipeline-generator --help-examples

# Project-specific help
gitlab-pipeline-generator --help-dotnet
gitlab-pipeline-generator --help-nodejs
gitlab-pipeline-generator --help-python

# GitLab integration help
gitlab-pipeline-generator --help-gitlab
gitlab-pipeline-generator --help-gitlab-auth
gitlab-pipeline-generator --help-gitlab-analysis
gitlab-pipeline-generator --help-gitlab-troubleshoot
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- GitLab for providing excellent CI/CD platform and API
- .NET community for tools and libraries
- Contributors and users for feedback and improvements

## 📞 Support

- 📖 Documentation: Use `--help` commands for detailed information
- 🐛 Issues: Report bugs and request features via GitHub Issues
- 💬 Discussions: Join community discussions for questions and tips

---

**Made with ❤️ for the DevOps community**