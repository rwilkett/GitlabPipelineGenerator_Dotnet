using GitlabPipelineGenerator.Core.Interfaces;
using GitlabPipelineGenerator.Core.Models.GitLab;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Analyzes file patterns to detect project characteristics
/// </summary>
public class FilePatternAnalyzer : IFilePatternAnalyzer
{
    private static readonly Dictionary<ProjectType, FilePatternRule[]> ProjectTypeRules = new()
    {
        [ProjectType.DotNet] = new[]
        {
            new FilePatternRule { Pattern = @"\.csproj$", Weight = 10, IsRequired = true },
            new FilePatternRule { Pattern = @"\.sln$", Weight = 8 },
            new FilePatternRule { Pattern = @"\.cs$", Weight = 5 },
            new FilePatternRule { Pattern = @"\.fs$", Weight = 5 },
            new FilePatternRule { Pattern = @"\.vb$", Weight = 5 },
            new FilePatternRule { Pattern = @"global\.json$", Weight = 3 },
            new FilePatternRule { Pattern = @"Directory\.Build\.props$", Weight = 3 }
        },
        [ProjectType.NodeJs] = new[]
        {
            new FilePatternRule { Pattern = @"package\.json$", Weight = 10, IsRequired = true },
            new FilePatternRule { Pattern = @"\.js$", Weight = 5 },
            new FilePatternRule { Pattern = @"\.ts$", Weight = 5 },
            new FilePatternRule { Pattern = @"node_modules/", Weight = 3 },
            new FilePatternRule { Pattern = @"yarn\.lock$", Weight = 3 },
            new FilePatternRule { Pattern = @"package-lock\.json$", Weight = 3 }
        },
        [ProjectType.Python] = new[]
        {
            new FilePatternRule { Pattern = @"requirements\.txt$", Weight = 8 },
            new FilePatternRule { Pattern = @"setup\.py$", Weight = 8 },
            new FilePatternRule { Pattern = @"pyproject\.toml$", Weight = 8 },
            new FilePatternRule { Pattern = @"\.py$", Weight = 5, IsRequired = true },
            new FilePatternRule { Pattern = @"Pipfile$", Weight = 3 },
            new FilePatternRule { Pattern = @"environment\.yml$", Weight = 3 }
        },
        [ProjectType.Java] = new[]
        {
            new FilePatternRule { Pattern = @"pom\.xml$", Weight = 10 },
            new FilePatternRule { Pattern = @"build\.gradle$", Weight = 10 },
            new FilePatternRule { Pattern = @"\.java$", Weight = 5, IsRequired = true },
            new FilePatternRule { Pattern = @"gradle\.properties$", Weight = 3 },
            new FilePatternRule { Pattern = @"gradlew$", Weight = 3 }
        },
        [ProjectType.Go] = new[]
        {
            new FilePatternRule { Pattern = @"go\.mod$", Weight = 10, IsRequired = true },
            new FilePatternRule { Pattern = @"\.go$", Weight = 5 },
            new FilePatternRule { Pattern = @"go\.sum$", Weight = 3 }
        },
        [ProjectType.Ruby] = new[]
        {
            new FilePatternRule { Pattern = @"Gemfile$", Weight = 10, IsRequired = true },
            new FilePatternRule { Pattern = @"\.rb$", Weight = 5 },
            new FilePatternRule { Pattern = @"Gemfile\.lock$", Weight = 3 },
            new FilePatternRule { Pattern = @"Rakefile$", Weight = 3 }
        },
        [ProjectType.PHP] = new[]
        {
            new FilePatternRule { Pattern = @"composer\.json$", Weight = 10 },
            new FilePatternRule { Pattern = @"\.php$", Weight = 5, IsRequired = true },
            new FilePatternRule { Pattern = @"composer\.lock$", Weight = 3 }
        },
        [ProjectType.Docker] = new[]
        {
            new FilePatternRule { Pattern = @"Dockerfile$", Weight = 10, IsRequired = true },
            new FilePatternRule { Pattern = @"docker-compose\.yml$", Weight = 8 },
            new FilePatternRule { Pattern = @"docker-compose\.yaml$", Weight = 8 },
            new FilePatternRule { Pattern = @"\.dockerignore$", Weight = 3 }
        },
        [ProjectType.Static] = new[]
        {
            new FilePatternRule { Pattern = @"index\.html$", Weight = 8 },
            new FilePatternRule { Pattern = @"\.html$", Weight = 5 },
            new FilePatternRule { Pattern = @"\.css$", Weight = 3 },
            new FilePatternRule { Pattern = @"\.js$", Weight = 3 }
        }
    };

    private static readonly Dictionary<string, FrameworkDetectionRule[]> FrameworkRules = new()
    {
        ["ASP.NET Core"] = new[]
        {
            new FrameworkDetectionRule { Pattern = @"Microsoft\.AspNetCore", Weight = 10, FileTypes = new[] { ".csproj" } },
            new FrameworkDetectionRule { Pattern = @"Program\.cs", Weight = 5, FileTypes = new[] { ".cs" } },
            new FrameworkDetectionRule { Pattern = @"Startup\.cs", Weight = 5, FileTypes = new[] { ".cs" } }
        },
        ["React"] = new[]
        {
            new FrameworkDetectionRule { Pattern = @"""react"":", Weight = 10, FileTypes = new[] { ".json" } },
            new FrameworkDetectionRule { Pattern = @"\.jsx$", Weight = 8 },
            new FrameworkDetectionRule { Pattern = @"\.tsx$", Weight = 8 }
        },
        ["Angular"] = new[]
        {
            new FrameworkDetectionRule { Pattern = @"""@angular/core"":", Weight = 10, FileTypes = new[] { ".json" } },
            new FrameworkDetectionRule { Pattern = @"angular\.json$", Weight = 8 }
        },
        ["Vue.js"] = new[]
        {
            new FrameworkDetectionRule { Pattern = @"""vue"":", Weight = 10, FileTypes = new[] { ".json" } },
            new FrameworkDetectionRule { Pattern = @"\.vue$", Weight = 8 }
        },
        ["Django"] = new[]
        {
            new FrameworkDetectionRule { Pattern = @"Django", Weight = 10, FileTypes = new[] { ".txt", ".py" } },
            new FrameworkDetectionRule { Pattern = @"manage\.py$", Weight = 8 }
        },
        ["Flask"] = new[]
        {
            new FrameworkDetectionRule { Pattern = @"Flask", Weight = 10, FileTypes = new[] { ".txt", ".py" } },
            new FrameworkDetectionRule { Pattern = @"app\.py$", Weight = 5 }
        },
        ["Spring Boot"] = new[]
        {
            new FrameworkDetectionRule { Pattern = @"spring-boot", Weight = 10, FileTypes = new[] { ".xml", ".gradle" } },
            new FrameworkDetectionRule { Pattern = @"Application\.java$", Weight = 5 }
        }
    };

    public async Task<ProjectType> DetectProjectTypeAsync(IEnumerable<GitLabRepositoryFile> files)
    {
        var fileList = files.ToList();
        var scores = new Dictionary<ProjectType, int>();

        foreach (var projectType in ProjectTypeRules.Keys)
        {
            var rules = ProjectTypeRules[projectType];
            var score = 0;
            var hasRequiredFiles = true;

            foreach (var rule in rules)
            {
                var matchingFiles = fileList.Where(f => 
                    System.Text.RegularExpressions.Regex.IsMatch(f.Path, rule.Pattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase)).ToList();

                if (matchingFiles.Any())
                {
                    score += rule.Weight * matchingFiles.Count;
                }
                else if (rule.IsRequired)
                {
                    hasRequiredFiles = false;
                    break;
                }
            }

            if (hasRequiredFiles && score > 0)
            {
                scores[projectType] = score;
            }
        }

        if (!scores.Any())
        {
            return ProjectType.Unknown;
        }

        // Check for mixed projects
        var topScores = scores.OrderByDescending(s => s.Value).Take(2).ToList();
        if (topScores.Count > 1 && topScores[0].Value - topScores[1].Value < 5)
        {
            return ProjectType.Mixed;
        }

        return scores.OrderByDescending(s => s.Value).First().Key;
    }

    public async Task<FrameworkInfo> DetectFrameworksAsync(IEnumerable<GitLabRepositoryFile> files)
    {
        var fileList = files.ToList();
        var frameworkScores = new Dictionary<string, int>();
        var detectedFeatures = new List<string>();

        foreach (var framework in FrameworkRules.Keys)
        {
            var rules = FrameworkRules[framework];
            var score = 0;

            foreach (var rule in rules)
            {
                var relevantFiles = rule.FileTypes?.Any() == true
                    ? fileList.Where(f => rule.FileTypes.Contains(f.Extension))
                    : fileList;

                foreach (var file in relevantFiles)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(file.Path, rule.Pattern, 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        score += rule.Weight;
                        break;
                    }

                    if (!string.IsNullOrEmpty(file.Content) && 
                        System.Text.RegularExpressions.Regex.IsMatch(file.Content, rule.Pattern, 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    {
                        score += rule.Weight;
                        break;
                    }
                }
            }

            if (score > 0)
            {
                frameworkScores[framework] = score;
            }
        }

        var topFramework = frameworkScores.OrderByDescending(f => f.Value).FirstOrDefault();
        var additionalFrameworks = frameworkScores
            .Where(f => f.Key != topFramework.Key && f.Value >= 5)
            .Select(f => f.Key)
            .ToList();

        return new FrameworkInfo
        {
            Name = topFramework.Key ?? "Unknown",
            AdditionalFrameworks = additionalFrameworks,
            DetectedFeatures = detectedFeatures,
            Confidence = topFramework.Value > 10 ? AnalysisConfidence.High :
                        topFramework.Value > 5 ? AnalysisConfidence.Medium : AnalysisConfidence.Low
        };
    }

    public async Task<BuildToolInfo> DetectBuildToolsAsync(IEnumerable<GitLabRepositoryFile> files)
    {
        var fileList = files.ToList();
        var buildTools = new Dictionary<string, BuildToolDetection>();

        // .NET build tools
        if (fileList.Any(f => f.Extension == ".csproj"))
        {
            buildTools["dotnet"] = new BuildToolDetection
            {
                Name = "dotnet",
                BuildCommands = new[] { "dotnet build", "dotnet restore" },
                TestCommands = new[] { "dotnet test" },
                ConfigFiles = fileList.Where(f => f.Extension == ".csproj" || f.Name == "global.json").Select(f => f.Path).ToList(),
                Weight = 10
            };
        }

        // Node.js build tools
        if (fileList.Any(f => f.Name == "package.json"))
        {
            var packageJsonFile = fileList.First(f => f.Name == "package.json");
            var commands = ExtractNpmScripts(packageJsonFile.Content);
            
            buildTools["npm"] = new BuildToolDetection
            {
                Name = "npm",
                BuildCommands = commands.BuildCommands,
                TestCommands = commands.TestCommands,
                ConfigFiles = new[] { "package.json" },
                Weight = 10
            };
        }

        // Maven
        if (fileList.Any(f => f.Name == "pom.xml"))
        {
            buildTools["maven"] = new BuildToolDetection
            {
                Name = "maven",
                BuildCommands = new[] { "mvn compile", "mvn package" },
                TestCommands = new[] { "mvn test" },
                ConfigFiles = new[] { "pom.xml" },
                Weight = 10
            };
        }

        // Gradle
        if (fileList.Any(f => f.Name == "build.gradle" || f.Name == "build.gradle.kts"))
        {
            buildTools["gradle"] = new BuildToolDetection
            {
                Name = "gradle",
                BuildCommands = new[] { "./gradlew build", "gradle build" },
                TestCommands = new[] { "./gradlew test", "gradle test" },
                ConfigFiles = fileList.Where(f => f.Name.StartsWith("build.gradle")).Select(f => f.Path).ToList(),
                Weight = 10
            };
        }

        // Python build tools
        if (fileList.Any(f => f.Name == "setup.py"))
        {
            buildTools["python"] = new BuildToolDetection
            {
                Name = "python",
                BuildCommands = new[] { "python setup.py build" },
                TestCommands = new[] { "python -m pytest", "python -m unittest" },
                ConfigFiles = new[] { "setup.py" },
                Weight = 8
            };
        }

        var primaryTool = buildTools.Values.OrderByDescending(t => t.Weight).FirstOrDefault();
        if (primaryTool == null)
        {
            return new BuildToolInfo { Name = "Unknown", Confidence = AnalysisConfidence.Low };
        }

        return new BuildToolInfo
        {
            Name = primaryTool.Name,
            BuildCommands = primaryTool.BuildCommands.ToList(),
            TestCommands = primaryTool.TestCommands.ToList(),
            ConfigurationFiles = primaryTool.ConfigFiles.ToList(),
            AdditionalTools = buildTools.Values
                .Where(t => t != primaryTool && t.Weight >= 5)
                .Select(t => t.Name)
                .ToList(),
            Confidence = primaryTool.Weight > 8 ? AnalysisConfidence.High : AnalysisConfidence.Medium
        };
    }

    public async Task<TestFrameworkInfo> DetectTestFrameworksAsync(IEnumerable<GitLabRepositoryFile> files)
    {
        var fileList = files.ToList();
        var testFrameworks = new Dictionary<string, TestFrameworkDetection>();

        // .NET test frameworks
        var csprojFiles = fileList.Where(f => f.Extension == ".csproj").ToList();
        foreach (var csproj in csprojFiles)
        {
            if (!string.IsNullOrEmpty(csproj.Content))
            {
                if (csproj.Content.Contains("Microsoft.NET.Test.Sdk"))
                {
                    if (csproj.Content.Contains("xunit"))
                    {
                        testFrameworks["xUnit"] = new TestFrameworkDetection
                        {
                            Name = "xUnit",
                            TestCommands = new[] { "dotnet test" },
                            Weight = 10
                        };
                    }
                    else if (csproj.Content.Contains("NUnit"))
                    {
                        testFrameworks["NUnit"] = new TestFrameworkDetection
                        {
                            Name = "NUnit",
                            TestCommands = new[] { "dotnet test" },
                            Weight = 10
                        };
                    }
                    else if (csproj.Content.Contains("MSTest"))
                    {
                        testFrameworks["MSTest"] = new TestFrameworkDetection
                        {
                            Name = "MSTest",
                            TestCommands = new[] { "dotnet test" },
                            Weight = 10
                        };
                    }
                }
            }
        }

        // JavaScript test frameworks
        var packageJson = fileList.FirstOrDefault(f => f.Name == "package.json");
        if (packageJson?.Content != null)
        {
            if (packageJson.Content.Contains("\"jest\""))
            {
                testFrameworks["Jest"] = new TestFrameworkDetection
                {
                    Name = "Jest",
                    TestCommands = new[] { "npm test", "jest" },
                    Weight = 10
                };
            }
            if (packageJson.Content.Contains("\"mocha\""))
            {
                testFrameworks["Mocha"] = new TestFrameworkDetection
                {
                    Name = "Mocha",
                    TestCommands = new[] { "npm test", "mocha" },
                    Weight = 8
                };
            }
        }

        // Python test frameworks
        if (fileList.Any(f => f.Name == "pytest.ini" || f.Path.Contains("pytest")))
        {
            testFrameworks["pytest"] = new TestFrameworkDetection
            {
                Name = "pytest",
                TestCommands = new[] { "pytest", "python -m pytest" },
                Weight = 10
            };
        }

        // Detect test directories
        var testDirectories = fileList
            .Where(f => f.Directory.ToLowerInvariant().Contains("test") || 
                       f.Directory.ToLowerInvariant().Contains("spec"))
            .Select(f => f.Directory)
            .Distinct()
            .ToList();

        var primaryFramework = testFrameworks.Values.OrderByDescending(t => t.Weight).FirstOrDefault();
        if (primaryFramework == null)
        {
            return new TestFrameworkInfo 
            { 
                Name = "Unknown", 
                TestDirectories = testDirectories,
                Confidence = testDirectories.Any() ? AnalysisConfidence.Low : AnalysisConfidence.Low 
            };
        }

        return new TestFrameworkInfo
        {
            Name = primaryFramework.Name,
            TestCommands = primaryFramework.TestCommands.ToList(),
            TestDirectories = testDirectories,
            AdditionalFrameworks = testFrameworks.Values
                .Where(t => t != primaryFramework && t.Weight >= 5)
                .Select(t => t.Name)
                .ToList(),
            Confidence = primaryFramework.Weight > 8 ? AnalysisConfidence.High : AnalysisConfidence.Medium
        };
    }

    private static (List<string> BuildCommands, List<string> TestCommands) ExtractNpmScripts(string? packageJsonContent)
    {
        var buildCommands = new List<string>();
        var testCommands = new List<string>();

        if (string.IsNullOrEmpty(packageJsonContent))
        {
            return (buildCommands, testCommands);
        }

        // Simple regex-based extraction (in a real implementation, you'd use a JSON parser)
        var scriptMatches = System.Text.RegularExpressions.Regex.Matches(
            packageJsonContent, 
            @"""(build|test|start)"":\s*""([^""]+)""",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match match in scriptMatches)
        {
            var scriptName = match.Groups[1].Value.ToLowerInvariant();
            var scriptCommand = $"npm run {scriptName}";

            if (scriptName == "build" || scriptName == "start")
            {
                buildCommands.Add(scriptCommand);
            }
            else if (scriptName == "test")
            {
                testCommands.Add(scriptCommand);
            }
        }

        return (buildCommands, testCommands);
    }

    private class FilePatternRule
    {
        public string Pattern { get; set; } = string.Empty;
        public int Weight { get; set; }
        public bool IsRequired { get; set; }
    }

    private class FrameworkDetectionRule
    {
        public string Pattern { get; set; } = string.Empty;
        public int Weight { get; set; }
        public string[]? FileTypes { get; set; }
    }

    private class BuildToolDetection
    {
        public string Name { get; set; } = string.Empty;
        public IEnumerable<string> BuildCommands { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> TestCommands { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> ConfigFiles { get; set; } = Enumerable.Empty<string>();
        public int Weight { get; set; }
    }

    private class TestFrameworkDetection
    {
        public string Name { get; set; } = string.Empty;
        public IEnumerable<string> TestCommands { get; set; } = Enumerable.Empty<string>();
        public int Weight { get; set; }
    }
}