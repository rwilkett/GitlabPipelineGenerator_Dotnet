using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Analyzes project dependencies and package files
/// </summary>
public class DependencyAnalyzer : IDependencyAnalyzer
{
    private static readonly Dictionary<string, string[]> SecuritySensitivePackages = new()
    {
        ["javascript"] = new[] { "express", "lodash", "moment", "request", "axios", "jsonwebtoken" },
        ["dotnet"] = new[] { "Newtonsoft.Json", "Microsoft.AspNetCore", "System.IdentityModel.Tokens.Jwt" },
        ["python"] = new[] { "django", "flask", "requests", "pyjwt", "cryptography" },
        ["java"] = new[] { "spring-boot", "jackson", "log4j", "junit" }
    };

    public async Task<DependencyInfo> AnalyzePackageFileAsync(string fileName, string content)
    {
        var dependencyInfo = new DependencyInfo
        {
            PackageFile = fileName
        };

        try
        {
            switch (fileName.ToLowerInvariant())
            {
                case "package.json":
                    await AnalyzePackageJsonAsync(dependencyInfo, content);
                    break;
                case var name when name.EndsWith(".csproj"):
                    await AnalyzeCsprojAsync(dependencyInfo, content);
                    break;
                case "requirements.txt":
                    await AnalyzeRequirementsTxtAsync(dependencyInfo, content);
                    break;
                case "pom.xml":
                    await AnalyzePomXmlAsync(dependencyInfo, content);
                    break;
                case "build.gradle":
                case "build.gradle.kts":
                    await AnalyzeBuildGradleAsync(dependencyInfo, content);
                    break;
                case "gemfile":
                    await AnalyzeGemfileAsync(dependencyInfo, content);
                    break;
                case "composer.json":
                    await AnalyzeComposerJsonAsync(dependencyInfo, content);
                    break;
                case "go.mod":
                    await AnalyzeGoModAsync(dependencyInfo, content);
                    break;
                default:
                    dependencyInfo.Confidence = AnalysisConfidence.Low;
                    break;
            }

            // Detect security-sensitive dependencies
            dependencyInfo.HasSecuritySensitiveDependencies = HasSecuritySensitiveDependencies(dependencyInfo);
        }
        catch (Exception)
        {
            dependencyInfo.Confidence = AnalysisConfidence.Low;
        }

        return dependencyInfo;
    }

    public async Task<CacheConfiguration> RecommendCacheConfigurationAsync(DependencyInfo dependencies)
    {
        var cacheConfig = new CacheConfiguration();
        var recommendation = new CacheRecommendation();

        switch (dependencies.PackageManager.ToLowerInvariant())
        {
            case "npm":
            case "yarn":
                cacheConfig.CachePaths.AddRange(new[] { "node_modules/", ".npm/", ".yarn/" });
                cacheConfig.CacheKey = "package-lock.json";
                cacheConfig.FallbackKeys.Add("package.json");
                recommendation.EstimatedTimeSavings = TimeSpan.FromMinutes(2);
                cacheConfig.Effectiveness = CacheEffectiveness.High;
                break;

            case "dotnet":
                cacheConfig.CachePaths.AddRange(new[] { "~/.nuget/packages/", "obj/" });
                cacheConfig.CacheKey = "*.csproj";
                cacheConfig.FallbackKeys.Add("Directory.Build.props");
                recommendation.EstimatedTimeSavings = TimeSpan.FromMinutes(1);
                cacheConfig.Effectiveness = CacheEffectiveness.Medium;
                break;

            case "pip":
                cacheConfig.CachePaths.AddRange(new[] { "~/.cache/pip/", ".venv/" });
                cacheConfig.CacheKey = "requirements.txt";
                recommendation.EstimatedTimeSavings = TimeSpan.FromMinutes(1.5);
                cacheConfig.Effectiveness = CacheEffectiveness.Medium;
                break;

            case "maven":
                cacheConfig.CachePaths.Add("~/.m2/repository/");
                cacheConfig.CacheKey = "pom.xml";
                recommendation.EstimatedTimeSavings = TimeSpan.FromMinutes(3);
                cacheConfig.Effectiveness = CacheEffectiveness.High;
                break;

            case "gradle":
                cacheConfig.CachePaths.AddRange(new[] { "~/.gradle/caches/", ".gradle/" });
                cacheConfig.CacheKey = "build.gradle";
                cacheConfig.FallbackKeys.Add("gradle.properties");
                recommendation.EstimatedTimeSavings = TimeSpan.FromMinutes(2);
                cacheConfig.Effectiveness = CacheEffectiveness.High;
                break;

            default:
                recommendation.IsRecommended = false;
                recommendation.Reason = "Unknown package manager";
                return cacheConfig;
        }

        recommendation.IsRecommended = dependencies.TotalDependencies > 5;
        recommendation.Configuration = cacheConfig;
        recommendation.Reason = dependencies.TotalDependencies > 20 
            ? "Large number of dependencies detected" 
            : "Moderate number of dependencies detected";

        dependencies.CacheRecommendation = recommendation;
        return cacheConfig;
    }

    public async Task<SecurityScanConfiguration> RecommendSecurityScanningAsync(DependencyInfo dependencies)
    {
        var securityConfig = new SecurityScanConfiguration();

        // Determine risk level based on dependencies
        if (dependencies.HasSecuritySensitiveDependencies)
        {
            securityConfig.RiskLevel = SecurityRiskLevel.High;
            securityConfig.IsRecommended = true;
            securityConfig.Reason = "Security-sensitive dependencies detected";
        }
        else if (dependencies.TotalDependencies > 50)
        {
            securityConfig.RiskLevel = SecurityRiskLevel.Medium;
            securityConfig.IsRecommended = true;
            securityConfig.Reason = "Large number of dependencies increases security risk";
        }
        else if (dependencies.TotalDependencies > 10)
        {
            securityConfig.RiskLevel = SecurityRiskLevel.Low;
            securityConfig.IsRecommended = true;
            securityConfig.Reason = "Moderate number of dependencies";
        }

        // Add recommended scanners based on package manager
        switch (dependencies.PackageManager.ToLowerInvariant())
        {
            case "npm":
            case "yarn":
                securityConfig.RecommendedScanners.Add(new SecurityScanner
                {
                    Name = "npm audit",
                    Type = SecurityScanType.DependencyScanning,
                    IsDefault = true,
                    Priority = 1
                });
                securityConfig.RecommendedScanners.Add(new SecurityScanner
                {
                    Name = "NodeJsScan",
                    Type = SecurityScanType.SAST,
                    Priority = 2
                });
                break;

            case "dotnet":
                securityConfig.RecommendedScanners.Add(new SecurityScanner
                {
                    Name = "Security Code Scan",
                    Type = SecurityScanType.SAST,
                    IsDefault = true,
                    Priority = 1
                });
                securityConfig.RecommendedScanners.Add(new SecurityScanner
                {
                    Name = "dotnet list package --vulnerable",
                    Type = SecurityScanType.DependencyScanning,
                    Priority = 2
                });
                break;

            case "pip":
                securityConfig.RecommendedScanners.Add(new SecurityScanner
                {
                    Name = "safety",
                    Type = SecurityScanType.DependencyScanning,
                    IsDefault = true,
                    Priority = 1
                });
                securityConfig.RecommendedScanners.Add(new SecurityScanner
                {
                    Name = "bandit",
                    Type = SecurityScanType.SAST,
                    Priority = 2
                });
                break;

            case "maven":
            case "gradle":
                securityConfig.RecommendedScanners.Add(new SecurityScanner
                {
                    Name = "OWASP Dependency Check",
                    Type = SecurityScanType.DependencyScanning,
                    IsDefault = true,
                    Priority = 1
                });
                securityConfig.RecommendedScanners.Add(new SecurityScanner
                {
                    Name = "SpotBugs",
                    Type = SecurityScanType.SAST,
                    Priority = 2
                });
                break;
        }

        // Always recommend secret detection
        securityConfig.RecommendedScanners.Add(new SecurityScanner
        {
            Name = "GitLab Secret Detection",
            Type = SecurityScanType.SecretDetection,
            IsDefault = true,
            Priority = 1
        });

        return securityConfig;
    }

    public async Task<RuntimeInfo> DetectRuntimeRequirementsAsync(DependencyInfo dependencies)
    {
        var runtimeInfo = new RuntimeInfo();

        switch (dependencies.PackageManager.ToLowerInvariant())
        {
            case "npm":
            case "yarn":
                runtimeInfo.Name = "node";
                runtimeInfo.Version = DetectNodeVersion(dependencies);
                runtimeInfo.RecommendedBaseImages.AddRange(new[] { "node:18-alpine", "node:16-alpine", "node:latest" });
                break;

            case "dotnet":
                runtimeInfo.Name = "dotnet";
                runtimeInfo.Version = DetectDotNetVersion(dependencies);
                runtimeInfo.RecommendedBaseImages.AddRange(new[] { "mcr.microsoft.com/dotnet/aspnet:8.0", "mcr.microsoft.com/dotnet/runtime:8.0" });
                break;

            case "pip":
                runtimeInfo.Name = "python";
                runtimeInfo.Version = DetectPythonVersion(dependencies);
                runtimeInfo.RecommendedBaseImages.AddRange(new[] { "python:3.11-slim", "python:3.10-slim", "python:3.9-slim" });
                break;

            case "maven":
            case "gradle":
                runtimeInfo.Name = "java";
                runtimeInfo.Version = DetectJavaVersion(dependencies);
                runtimeInfo.RecommendedBaseImages.AddRange(new[] { "openjdk:17-jre-slim", "openjdk:11-jre-slim", "eclipse-temurin:17-jre" });
                break;

            case "bundler":
                runtimeInfo.Name = "ruby";
                runtimeInfo.RecommendedBaseImages.AddRange(new[] { "ruby:3.1-slim", "ruby:3.0-slim" });
                break;

            case "composer":
                runtimeInfo.Name = "php";
                runtimeInfo.RecommendedBaseImages.AddRange(new[] { "php:8.1-fpm", "php:8.0-fpm" });
                break;

            case "go":
                runtimeInfo.Name = "go";
                runtimeInfo.RecommendedBaseImages.AddRange(new[] { "golang:1.19-alpine", "golang:1.18-alpine" });
                break;
        }

        return runtimeInfo;
    }

    private async Task AnalyzePackageJsonAsync(DependencyInfo dependencyInfo, string content)
    {
        dependencyInfo.PackageManager = "npm";
        
        try
        {
            var packageJson = JsonSerializer.Deserialize<JsonElement>(content);

            if (packageJson.TryGetProperty("dependencies", out var deps))
            {
                foreach (var dep in deps.EnumerateObject())
                {
                    dependencyInfo.Dependencies.Add(new PackageDependency
                    {
                        Name = dep.Name,
                        Version = dep.Value.GetString() ?? "",
                        Type = DependencyType.Production,
                        IsSecuritySensitive = IsSecuritySensitive("javascript", dep.Name)
                    });
                }
            }

            if (packageJson.TryGetProperty("devDependencies", out var devDeps))
            {
                foreach (var dep in devDeps.EnumerateObject())
                {
                    dependencyInfo.DevDependencies.Add(new PackageDependency
                    {
                        Name = dep.Name,
                        Version = dep.Value.GetString() ?? "",
                        Type = DependencyType.Development,
                        IsSecuritySensitive = IsSecuritySensitive("javascript", dep.Name)
                    });
                }
            }

            dependencyInfo.Confidence = AnalysisConfidence.High;
        }
        catch (JsonException)
        {
            dependencyInfo.Confidence = AnalysisConfidence.Low;
        }
    }

    private async Task AnalyzeCsprojAsync(DependencyInfo dependencyInfo, string content)
    {
        dependencyInfo.PackageManager = "dotnet";

        try
        {
            var doc = XDocument.Parse(content);
            var packageReferences = doc.Descendants("PackageReference");

            foreach (var packageRef in packageReferences)
            {
                var name = packageRef.Attribute("Include")?.Value ?? "";
                var version = packageRef.Attribute("Version")?.Value ?? "";

                if (!string.IsNullOrEmpty(name))
                {
                    dependencyInfo.Dependencies.Add(new PackageDependency
                    {
                        Name = name,
                        Version = version,
                        Type = DependencyType.Production,
                        IsSecuritySensitive = IsSecuritySensitive("dotnet", name)
                    });
                }
            }

            dependencyInfo.Confidence = AnalysisConfidence.High;
        }
        catch (Exception)
        {
            dependencyInfo.Confidence = AnalysisConfidence.Low;
        }
    }

    private async Task AnalyzeRequirementsTxtAsync(DependencyInfo dependencyInfo, string content)
    {
        dependencyInfo.PackageManager = "pip";

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("#") || string.IsNullOrEmpty(trimmedLine))
                continue;

            var match = Regex.Match(trimmedLine, @"^([a-zA-Z0-9\-_\.]+)([>=<~!]+.*)?$");
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                var version = match.Groups[2].Value;

                dependencyInfo.Dependencies.Add(new PackageDependency
                {
                    Name = name,
                    Version = version,
                    Type = DependencyType.Production,
                    IsSecuritySensitive = IsSecuritySensitive("python", name)
                });
            }
        }

        dependencyInfo.Confidence = AnalysisConfidence.Medium;
    }

    private async Task AnalyzePomXmlAsync(DependencyInfo dependencyInfo, string content)
    {
        dependencyInfo.PackageManager = "maven";

        try
        {
            var doc = XDocument.Parse(content);
            var dependencies = doc.Descendants().Where(x => x.Name.LocalName == "dependency");

            foreach (var dep in dependencies)
            {
                var groupId = dep.Elements().FirstOrDefault(x => x.Name.LocalName == "groupId")?.Value ?? "";
                var artifactId = dep.Elements().FirstOrDefault(x => x.Name.LocalName == "artifactId")?.Value ?? "";
                var version = dep.Elements().FirstOrDefault(x => x.Name.LocalName == "version")?.Value ?? "";
                var scope = dep.Elements().FirstOrDefault(x => x.Name.LocalName == "scope")?.Value ?? "compile";

                var name = $"{groupId}:{artifactId}";
                var depType = scope.ToLowerInvariant() switch
                {
                    "test" => DependencyType.Test,
                    "provided" => DependencyType.Build,
                    "runtime" => DependencyType.Production,
                    _ => DependencyType.Production
                };

                dependencyInfo.Dependencies.Add(new PackageDependency
                {
                    Name = name,
                    Version = version,
                    Type = depType,
                    IsSecuritySensitive = IsSecuritySensitive("java", artifactId)
                });
            }

            dependencyInfo.Confidence = AnalysisConfidence.High;
        }
        catch (Exception)
        {
            dependencyInfo.Confidence = AnalysisConfidence.Low;
        }
    }

    private async Task AnalyzeBuildGradleAsync(DependencyInfo dependencyInfo, string content)
    {
        dependencyInfo.PackageManager = "gradle";

        // Simple regex-based parsing for Gradle dependencies
        var dependencyPattern = @"(implementation|testImplementation|api|compileOnly)\s+['""]([^'""]+)['""]";
        var matches = Regex.Matches(content, dependencyPattern);

        foreach (Match match in matches)
        {
            var scope = match.Groups[1].Value;
            var dependency = match.Groups[2].Value;

            var parts = dependency.Split(':');
            if (parts.Length >= 2)
            {
                var name = $"{parts[0]}:{parts[1]}";
                var version = parts.Length > 2 ? parts[2] : "";

                var depType = scope.ToLowerInvariant() switch
                {
                    "testimplementation" => DependencyType.Test,
                    "compileonly" => DependencyType.Build,
                    _ => DependencyType.Production
                };

                dependencyInfo.Dependencies.Add(new PackageDependency
                {
                    Name = name,
                    Version = version,
                    Type = depType,
                    IsSecuritySensitive = IsSecuritySensitive("java", parts[1])
                });
            }
        }

        dependencyInfo.Confidence = AnalysisConfidence.Medium;
    }

    private async Task AnalyzeGemfileAsync(DependencyInfo dependencyInfo, string content)
    {
        dependencyInfo.PackageManager = "bundler";

        var gemPattern = @"gem\s+['""]([^'""]+)['""](?:\s*,\s*['""]([^'""]+)['""])?";
        var matches = Regex.Matches(content, gemPattern);

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var version = match.Groups[2].Value;

            dependencyInfo.Dependencies.Add(new PackageDependency
            {
                Name = name,
                Version = version,
                Type = DependencyType.Production
            });
        }

        dependencyInfo.Confidence = AnalysisConfidence.Medium;
    }

    private async Task AnalyzeComposerJsonAsync(DependencyInfo dependencyInfo, string content)
    {
        dependencyInfo.PackageManager = "composer";

        try
        {
            var composerJson = JsonSerializer.Deserialize<JsonElement>(content);

            if (composerJson.TryGetProperty("require", out var deps))
            {
                foreach (var dep in deps.EnumerateObject())
                {
                    dependencyInfo.Dependencies.Add(new PackageDependency
                    {
                        Name = dep.Name,
                        Version = dep.Value.GetString() ?? "",
                        Type = DependencyType.Production
                    });
                }
            }

            if (composerJson.TryGetProperty("require-dev", out var devDeps))
            {
                foreach (var dep in devDeps.EnumerateObject())
                {
                    dependencyInfo.DevDependencies.Add(new PackageDependency
                    {
                        Name = dep.Name,
                        Version = dep.Value.GetString() ?? "",
                        Type = DependencyType.Development
                    });
                }
            }

            dependencyInfo.Confidence = AnalysisConfidence.High;
        }
        catch (JsonException)
        {
            dependencyInfo.Confidence = AnalysisConfidence.Low;
        }
    }

    private async Task AnalyzeGoModAsync(DependencyInfo dependencyInfo, string content)
    {
        dependencyInfo.PackageManager = "go";

        var requirePattern = @"require\s+([^\s]+)\s+([^\s]+)";
        var matches = Regex.Matches(content, requirePattern);

        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value;
            var version = match.Groups[2].Value;

            dependencyInfo.Dependencies.Add(new PackageDependency
            {
                Name = name,
                Version = version,
                Type = DependencyType.Production
            });
        }

        dependencyInfo.Confidence = AnalysisConfidence.Medium;
    }

    private bool IsSecuritySensitive(string ecosystem, string packageName)
    {
        if (SecuritySensitivePackages.TryGetValue(ecosystem, out var packages))
        {
            return packages.Any(p => packageName.ToLowerInvariant().Contains(p.ToLowerInvariant()));
        }
        return false;
    }

    private bool HasSecuritySensitiveDependencies(DependencyInfo dependencyInfo)
    {
        return dependencyInfo.Dependencies.Any(d => d.IsSecuritySensitive) ||
               dependencyInfo.DevDependencies.Any(d => d.IsSecuritySensitive);
    }

    private string? DetectNodeVersion(DependencyInfo dependencies)
    {
        // Look for engine requirements or common patterns
        return ">=16.0.0";
    }

    private string? DetectDotNetVersion(DependencyInfo dependencies)
    {
        // Look for target framework in dependencies
        return "8.0";
    }

    private string? DetectPythonVersion(DependencyInfo dependencies)
    {
        // Look for python_requires or common patterns
        return ">=3.8";
    }

    private string? DetectJavaVersion(DependencyInfo dependencies)
    {
        // Look for Java version requirements
        return "17";
    }
}