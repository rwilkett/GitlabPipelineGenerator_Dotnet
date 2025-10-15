using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Analyzes configuration files and existing CI/CD setups
/// </summary>
public class ConfigurationAnalyzer : IConfigurationAnalyzer
{
    private static readonly Dictionary<string, CISystemType> CIConfigFiles = new()
    {
        [".gitlab-ci.yml"] = CISystemType.GitLabCI,
        [".github/workflows"] = CISystemType.GitHubActions,
        ["jenkinsfile"] = CISystemType.Jenkins,
        ["azure-pipelines.yml"] = CISystemType.AzureDevOps,
        [".circleci/config.yml"] = CISystemType.CircleCI,
        [".travis.yml"] = CISystemType.TravisCI
    };

    public async Task<ExistingCIConfig> AnalyzeExistingCIConfigAsync(GitLabProject project)
    {
        var config = new ExistingCIConfig();

        // This would typically fetch files from GitLab API
        // For now, we'll simulate the analysis based on common patterns
        
        // Check for GitLab CI configuration
        if (await HasFileAsync(project, ".gitlab-ci.yml"))
        {
            config.HasExistingConfig = true;
            config.SystemType = CISystemType.GitLabCI;
            config.ConfigurationFiles.Add(".gitlab-ci.yml");
            await AnalyzeGitLabCIConfigAsync(config, project);
        }
        // Check for GitHub Actions
        else if (await HasDirectoryAsync(project, ".github/workflows"))
        {
            config.HasExistingConfig = true;
            config.SystemType = CISystemType.GitHubActions;
            config.ConfigurationFiles.Add(".github/workflows");
            await AnalyzeGitHubActionsConfigAsync(config, project);
        }
        // Check for other CI systems
        else
        {
            foreach (var ciFile in CIConfigFiles)
            {
                if (await HasFileAsync(project, ciFile.Key))
                {
                    config.HasExistingConfig = true;
                    config.SystemType = ciFile.Value;
                    config.ConfigurationFiles.Add(ciFile.Key);
                    break;
                }
            }
        }

        if (config.HasExistingConfig)
        {
            config.Confidence = AnalysisConfidence.High;
            GenerateMigrationRecommendations(config);
        }

        return config;
    }

    public async Task<DockerConfiguration> AnalyzeDockerConfigurationAsync(GitLabProject project)
    {
        var dockerConfig = new DockerConfiguration();

        // Check for Dockerfile
        if (await HasFileAsync(project, "Dockerfile"))
        {
            dockerConfig.HasDockerConfig = true;
            dockerConfig.DockerfilePath = "Dockerfile";
            await AnalyzeDockerfileAsync(dockerConfig, project, "Dockerfile");
        }

        // Check for Docker Compose files
        var composeFiles = new[] { "docker-compose.yml", "docker-compose.yaml", "compose.yml", "compose.yaml" };
        foreach (var composeFile in composeFiles)
        {
            if (await HasFileAsync(project, composeFile))
            {
                dockerConfig.HasDockerConfig = true;
                dockerConfig.ComposeFiles.Add(composeFile);
                await AnalyzeDockerComposeAsync(dockerConfig, project, composeFile);
            }
        }

        // Check for .dockerignore
        if (await HasFileAsync(project, ".dockerignore"))
        {
            await AnalyzeDockerIgnoreAsync(dockerConfig, project);
        }

        if (dockerConfig.HasDockerConfig)
        {
            dockerConfig.Confidence = AnalysisConfidence.High;
            GenerateDockerOptimizationRecommendations(dockerConfig);
        }

        return dockerConfig;
    }

    public async Task<DeploymentConfiguration> AnalyzeDeploymentConfigurationAsync(GitLabProject project)
    {
        var deploymentConfig = new DeploymentConfiguration();

        // Check for Kubernetes files
        var k8sFiles = await FindFilesWithExtensionsAsync(project, new[] { ".yaml", ".yml" });
        var kubernetesFiles = k8sFiles.Where(f => 
            f.Contains("deployment") || f.Contains("service") || f.Contains("ingress") ||
            f.Contains("configmap") || f.Contains("secret") || f.Contains("k8s") ||
            f.Contains("kubernetes")).ToList();

        if (kubernetesFiles.Any())
        {
            deploymentConfig.HasDeploymentConfig = true;
            deploymentConfig.KubernetesFiles.AddRange(kubernetesFiles);
            
            deploymentConfig.Targets.Add(new DeploymentTarget
            {
                Name = "kubernetes",
                Type = DeploymentTargetType.Kubernetes,
                ConfigFiles = kubernetesFiles.ToList()
            });
        }

        // Check for Helm charts
        if (await HasFileAsync(project, "Chart.yaml") || await HasDirectoryAsync(project, "charts"))
        {
            deploymentConfig.HasDeploymentConfig = true;
            deploymentConfig.HelmCharts.Add("Chart.yaml");
            
            deploymentConfig.Targets.Add(new DeploymentTarget
            {
                Name = "helm",
                Type = DeploymentTargetType.Kubernetes,
                ConfigFiles = new List<string> { "Chart.yaml" }
            });
        }

        // Check for Terraform files
        var terraformFiles = await FindFilesWithExtensionsAsync(project, new[] { ".tf", ".tfvars" });
        if (terraformFiles.Any())
        {
            deploymentConfig.HasDeploymentConfig = true;
            deploymentConfig.TerraformFiles.AddRange(terraformFiles);
            
            deploymentConfig.Targets.Add(new DeploymentTarget
            {
                Name = "terraform",
                Type = DeploymentTargetType.Cloud,
                ConfigFiles = terraformFiles.ToList()
            });
        }

        // Check for CloudFormation templates
        var cfFiles = await FindFilesWithPatternAsync(project, "*cloudformation*");
        if (cfFiles.Any())
        {
            deploymentConfig.HasDeploymentConfig = true;
            deploymentConfig.CloudFormationTemplates.AddRange(cfFiles);
            
            deploymentConfig.Targets.Add(new DeploymentTarget
            {
                Name = "cloudformation",
                Type = DeploymentTargetType.Cloud,
                ConfigFiles = cfFiles.ToList()
            });
        }

        // Check for deployment scripts
        var deployScripts = await FindFilesWithPatternAsync(project, "*deploy*");
        deploymentConfig.DeploymentScripts.AddRange(deployScripts.Where(f => 
            f.EndsWith(".sh") || f.EndsWith(".ps1") || f.EndsWith(".bat")));

        if (deploymentConfig.HasDeploymentConfig)
        {
            deploymentConfig.Confidence = AnalysisConfidence.Medium;
        }

        return deploymentConfig;
    }

    public async Task<EnvironmentConfiguration> DetectEnvironmentsAsync(GitLabProject project)
    {
        var envConfig = new EnvironmentConfiguration();

        // Look for environment-specific configuration files
        var configPatterns = new[]
        {
            "*dev*", "*development*", "*test*", "*testing*", "*stage*", "*staging*",
            "*prod*", "*production*", "*uat*", "*preview*"
        };

        foreach (var pattern in configPatterns)
        {
            var files = await FindFilesWithPatternAsync(project, pattern);
            foreach (var file in files)
            {
                var envName = ExtractEnvironmentName(file);
                if (!string.IsNullOrEmpty(envName))
                {
                    if (!envConfig.ConfigurationFiles.ContainsKey(envName))
                    {
                        envConfig.ConfigurationFiles[envName] = new List<string>();
                    }
                    envConfig.ConfigurationFiles[envName].Add(file);

                    // Create environment if not exists
                    if (!envConfig.Environments.Any(e => e.Name.Equals(envName, StringComparison.OrdinalIgnoreCase)))
                    {
                        envConfig.Environments.Add(new Models.GitLab.Environment
                        {
                            Name = envName,
                            Type = DetermineEnvironmentType(envName)
                        });
                    }
                }
            }
        }

        // Look for .env files
        var envFiles = await FindFilesWithPatternAsync(project, ".env*");
        foreach (var envFile in envFiles)
        {
            var envName = envFile == ".env" ? "default" : envFile.Replace(".env.", "");
            if (!envConfig.ConfigurationFiles.ContainsKey(envName))
            {
                envConfig.ConfigurationFiles[envName] = new List<string>();
            }
            envConfig.ConfigurationFiles[envName].Add(envFile);
        }

        // Set up default promotion rules if multiple environments detected
        if (envConfig.Environments.Count > 1)
        {
            SetupDefaultPromotionRules(envConfig);
        }

        envConfig.Confidence = envConfig.Environments.Any() ? AnalysisConfidence.Medium : AnalysisConfidence.Low;

        return envConfig;
    }

    private async Task AnalyzeGitLabCIConfigAsync(ExistingCIConfig config, GitLabProject project)
    {
        // This would parse the actual .gitlab-ci.yml file
        // For simulation, we'll add common GitLab CI patterns
        
        config.DetectedStages.AddRange(new[] { "build", "test", "deploy" });
        
        config.DetectedJobs.AddRange(new[]
        {
            new CIJob { Name = "build", Stage = "build", Type = JobType.Build },
            new CIJob { Name = "test", Stage = "test", Type = JobType.Test },
            new CIJob { Name = "deploy", Stage = "deploy", Type = JobType.Deploy }
        });

        config.DockerImages.Add("node:16");
        config.CacheConfiguration.Add("node_modules/");
        config.ArtifactsConfiguration.Add("dist/");
    }

    private async Task AnalyzeGitHubActionsConfigAsync(ExistingCIConfig config, GitLabProject project)
    {
        config.DetectedStages.AddRange(new[] { "build", "test", "deploy" });
        config.MigrationRecommendations.Add("Convert GitHub Actions workflows to GitLab CI jobs");
        config.MigrationRecommendations.Add("Update action references to GitLab CI equivalents");
    }

    private async Task AnalyzeDockerfileAsync(DockerConfiguration dockerConfig, GitLabProject project, string dockerfilePath)
    {
        // This would parse the actual Dockerfile content
        // For simulation, we'll detect common patterns
        
        dockerConfig.BaseImage = "node:16-alpine";
        dockerConfig.ExposedPorts.Add(3000);
        dockerConfig.EnvironmentVariables["NODE_ENV"] = "production";
        dockerConfig.Volumes.Add("/app/data");
        
        // Check for multi-stage build patterns
        dockerConfig.IsMultiStage = true;
        dockerConfig.BuildStages.AddRange(new[] { "builder", "runtime" });
    }

    private async Task AnalyzeDockerComposeAsync(DockerConfiguration dockerConfig, GitLabProject project, string composeFile)
    {
        // This would parse the actual docker-compose.yml file
        // For simulation, we'll add common services
        
        dockerConfig.Services.Add(new DockerService
        {
            Name = "app",
            Image = "node:16",
            Ports = new List<string> { "3000:3000" },
            Environment = new Dictionary<string, string> { ["NODE_ENV"] = "development" }
        });

        dockerConfig.Services.Add(new DockerService
        {
            Name = "database",
            Image = "postgres:13",
            Environment = new Dictionary<string, string> { ["POSTGRES_DB"] = "myapp" }
        });
    }

    private async Task AnalyzeDockerIgnoreAsync(DockerConfiguration dockerConfig, GitLabProject project)
    {
        // This would parse the .dockerignore file
        dockerConfig.IgnorePatterns.AddRange(new[] { "node_modules", ".git", "*.log" });
    }

    private void GenerateMigrationRecommendations(ExistingCIConfig config)
    {
        switch (config.SystemType)
        {
            case CISystemType.GitHubActions:
                config.MigrationRecommendations.Add("Convert GitHub Actions workflows to GitLab CI/CD");
                config.MigrationRecommendations.Add("Update action marketplace references to GitLab equivalents");
                break;
            case CISystemType.Jenkins:
                config.MigrationRecommendations.Add("Convert Jenkinsfile pipeline to GitLab CI/CD YAML");
                config.MigrationRecommendations.Add("Migrate Jenkins plugins to GitLab CI/CD features");
                break;
            case CISystemType.CircleCI:
                config.MigrationRecommendations.Add("Convert CircleCI config to GitLab CI/CD");
                config.MigrationRecommendations.Add("Update orb references to GitLab CI/CD includes");
                break;
        }
    }

    private void GenerateDockerOptimizationRecommendations(DockerConfiguration dockerConfig)
    {
        if (dockerConfig.BaseImage?.Contains("alpine") != true)
        {
            dockerConfig.OptimizationRecommendations.Add("Consider using Alpine-based images for smaller size");
        }

        if (!dockerConfig.IsMultiStage)
        {
            dockerConfig.OptimizationRecommendations.Add("Consider multi-stage builds to reduce image size");
        }

        if (!dockerConfig.IgnorePatterns.Any())
        {
            dockerConfig.OptimizationRecommendations.Add("Add .dockerignore file to exclude unnecessary files");
        }
    }

    private string ExtractEnvironmentName(string fileName)
    {
        var patterns = new[]
        {
            @"\.([^.]+)\.env$",
            @"([^/\\]+)\.env$",
            @"config\.([^.]+)\.",
            @"([^/\\]*)(dev|development|test|testing|stage|staging|prod|production|uat)([^/\\]*)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var envName = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(envName) && envName.Length > 1)
                {
                    return envName.ToLowerInvariant();
                }
            }
        }

        return string.Empty;
    }

    private EnvironmentType DetermineEnvironmentType(string envName)
    {
        return envName.ToLowerInvariant() switch
        {
            var name when name.Contains("dev") => EnvironmentType.Development,
            var name when name.Contains("test") => EnvironmentType.Testing,
            var name when name.Contains("stage") => EnvironmentType.Staging,
            var name when name.Contains("prod") => EnvironmentType.Production,
            var name when name.Contains("uat") => EnvironmentType.UAT,
            var name when name.Contains("preview") => EnvironmentType.Preview,
            _ => EnvironmentType.Unknown
        };
    }

    private void SetupDefaultPromotionRules(EnvironmentConfiguration envConfig)
    {
        var environments = envConfig.Environments.OrderBy(e => (int)e.Type).ToList();
        
        for (int i = 0; i < environments.Count - 1; i++)
        {
            envConfig.PromotionRules.Add(new PromotionRule
            {
                SourceEnvironment = environments[i].Name,
                TargetEnvironment = environments[i + 1].Name,
                IsAutomatic = environments[i].Type != EnvironmentType.Production,
                RequiresApproval = environments[i + 1].Type == EnvironmentType.Production
            });
        }
    }

    // Simulated file system operations - in real implementation, these would use GitLab API
    private async Task<bool> HasFileAsync(GitLabProject project, string fileName)
    {
        // Simulate file existence check
        await Task.Delay(1);
        return fileName switch
        {
            ".gitlab-ci.yml" => true,
            "Dockerfile" => true,
            "docker-compose.yml" => true,
            _ => false
        };
    }

    private async Task<bool> HasDirectoryAsync(GitLabProject project, string directoryName)
    {
        await Task.Delay(1);
        return directoryName == ".github/workflows" ? false : true;
    }

    private async Task<List<string>> FindFilesWithExtensionsAsync(GitLabProject project, string[] extensions)
    {
        await Task.Delay(1);
        // Simulate finding files with specific extensions
        return new List<string> { "deployment.yaml", "service.yaml" };
    }

    private async Task<List<string>> FindFilesWithPatternAsync(GitLabProject project, string pattern)
    {
        await Task.Delay(1);
        // Simulate finding files matching pattern
        return pattern switch
        {
            "*deploy*" => new List<string> { "deploy.sh", "deploy-prod.sh" },
            "*dev*" => new List<string> { "config.dev.json", ".env.dev" },
            "*prod*" => new List<string> { "config.prod.json", ".env.prod" },
            ".env*" => new List<string> { ".env", ".env.dev", ".env.prod" },
            _ => new List<string>()
        };
    }
}