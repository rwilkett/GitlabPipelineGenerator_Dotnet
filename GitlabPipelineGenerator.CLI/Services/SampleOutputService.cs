namespace GitlabPipelineGenerator.CLI.Services;

/// <summary>
/// Service for providing sample pipeline outputs for demonstration
/// </summary>
public static class SampleOutputService
{
    /// <summary>
    /// Gets a sample .NET pipeline output
    /// </summary>
    /// <returns>Sample YAML content</returns>
    public static string GetSampleDotNetPipeline()
    {
        return @"# Generated GitLab CI/CD Pipeline for .NET 9.0 Project
# Generated on: 2024-01-15 10:30:00

stages:
  - build
  - test
  - deploy

variables:
  DOTNET_VERSION: ""9.0""
  BUILD_CONFIGURATION: ""Release""
  DOTNET_CLI_TELEMETRY_OPTOUT: ""true""

default:
  image: mcr.microsoft.com/dotnet/sdk:9.0
  before_script:
    - dotnet --version

build:
  stage: build
  script:
    - dotnet restore
    - dotnet build --configuration $BUILD_CONFIGURATION --no-restore
  artifacts:
    paths:
      - bin/
      - obj/
    expire_in: 1 hour
  cache:
    key: nuget-$CI_COMMIT_REF_SLUG
    paths:
      - ~/.nuget/packages/

test:
  stage: test
  script:
    - dotnet test --configuration $BUILD_CONFIGURATION --no-build --collect:""XPlat Code Coverage""
  artifacts:
    reports:
      coverage_report:
        coverage_format: cobertura
        path: coverage.cobertura.xml
    paths:
      - TestResults/
    expire_in: 1 week
  dependencies:
    - build

deploy:
  stage: deploy
  script:
    - dotnet publish --configuration $BUILD_CONFIGURATION --output ./publish
  artifacts:
    paths:
      - publish/
    expire_in: 1 week
  dependencies:
    - test
  only:
    - main
    - develop
";
    }

    /// <summary>
    /// Gets a sample Node.js pipeline output
    /// </summary>
    /// <returns>Sample YAML content</returns>
    public static string GetSampleNodeJsPipeline()
    {
        return @"# Generated GitLab CI/CD Pipeline for Node.js Project
# Generated on: 2024-01-15 10:30:00

stages:
  - build
  - test
  - deploy

variables:
  NODE_VERSION: ""18""
  NPM_CONFIG_CACHE: "".npm""

default:
  image: node:18
  before_script:
    - node --version
    - npm --version

build:
  stage: build
  script:
    - npm ci
    - npm run build
  artifacts:
    paths:
      - dist/
      - node_modules/
    expire_in: 1 hour
  cache:
    key: npm-$CI_COMMIT_REF_SLUG
    paths:
      - .npm/
      - node_modules/

test:
  stage: test
  script:
    - npm run test:coverage
  artifacts:
    reports:
      coverage_report:
        coverage_format: cobertura
        path: coverage/cobertura-coverage.xml
    paths:
      - coverage/
    expire_in: 1 week
  dependencies:
    - build

deploy:
  stage: deploy
  script:
    - npm run deploy
  dependencies:
    - test
  only:
    - main
    - develop
";
    }

    /// <summary>
    /// Gets a sample Python pipeline output
    /// </summary>
    /// <returns>Sample YAML content</returns>
    public static string GetSamplePythonPipeline()
    {
        return @"# Generated GitLab CI/CD Pipeline for Python Project
# Generated on: 2024-01-15 10:30:00

stages:
  - build
  - test
  - deploy

variables:
  PYTHON_VERSION: ""3.11""
  PIP_CACHE_DIR: ""$CI_PROJECT_DIR/.cache/pip""

default:
  image: python:3.11
  before_script:
    - python --version
    - pip --version

build:
  stage: build
  script:
    - python -m venv venv
    - source venv/bin/activate
    - pip install --upgrade pip
    - pip install -r requirements.txt
  artifacts:
    paths:
      - venv/
    expire_in: 1 hour
  cache:
    key: pip-$CI_COMMIT_REF_SLUG
    paths:
      - .cache/pip/

test:
  stage: test
  script:
    - source venv/bin/activate
    - pytest --cov=. --cov-report=xml --cov-report=html
  artifacts:
    reports:
      coverage_report:
        coverage_format: cobertura
        path: coverage.xml
    paths:
      - htmlcov/
    expire_in: 1 week
  dependencies:
    - build

deploy:
  stage: deploy
  script:
    - source venv/bin/activate
    - python setup.py sdist bdist_wheel
  artifacts:
    paths:
      - dist/
    expire_in: 1 week
  dependencies:
    - test
  only:
    - main
    - develop
";
    }

    /// <summary>
    /// Gets sample output for a specific project type
    /// </summary>
    /// <param name="projectType">Project type</param>
    /// <returns>Sample pipeline YAML</returns>
    public static string GetSampleOutput(string projectType)
    {
        return projectType.ToLowerInvariant() switch
        {
            "dotnet" => GetSampleDotNetPipeline(),
            "nodejs" => GetSampleNodeJsPipeline(),
            "python" => GetSamplePythonPipeline(),
            "docker" => GetSampleDockerPipeline(),
            "generic" => GetSampleGenericPipeline(),
            _ => "Sample output not available for this project type."
        };
    }

    /// <summary>
    /// Gets a sample Docker pipeline output
    /// </summary>
    /// <returns>Sample YAML content</returns>
    private static string GetSampleDockerPipeline()
    {
        return @"# Generated GitLab CI/CD Pipeline for Docker Project
# Generated on: 2024-01-15 10:30:00

stages:
  - build
  - test
  - deploy

variables:
  DOCKER_DRIVER: overlay2
  DOCKER_TLS_CERTDIR: ""/certs""

services:
  - docker:dind

default:
  image: docker:latest
  before_script:
    - docker info

build:
  stage: build
  script:
    - docker build -t $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA .
    - docker tag $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA $CI_REGISTRY_IMAGE:latest
  artifacts:
    reports:
      dotenv: build.env

test:
  stage: test
  script:
    - docker run --rm $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA /bin/sh -c ""echo 'Running tests'""
  dependencies:
    - build

deploy:
  stage: deploy
  script:
    - docker login -u $CI_REGISTRY_USER -p $CI_REGISTRY_PASSWORD $CI_REGISTRY
    - docker push $CI_REGISTRY_IMAGE:$CI_COMMIT_SHA
    - docker push $CI_REGISTRY_IMAGE:latest
  dependencies:
    - test
  only:
    - main
    - develop
";
    }

    /// <summary>
    /// Gets a sample generic pipeline output
    /// </summary>
    /// <returns>Sample YAML content</returns>
    private static string GetSampleGenericPipeline()
    {
        return @"# Generated GitLab CI/CD Pipeline for Generic Project
# Generated on: 2024-01-15 10:30:00

stages:
  - build
  - test
  - deploy

variables:
  BUILD_TOOL: ""make""
  TARGET_ENV: ""production""

default:
  image: ubuntu:22.04
  before_script:
    - apt-get update -qq
    - apt-get install -y -qq build-essential

build:
  stage: build
  script:
    - echo ""Building project...""
    - make build
  artifacts:
    paths:
      - build/
    expire_in: 1 hour

test:
  stage: test
  script:
    - echo ""Running tests...""
    - make test
  artifacts:
    reports:
      junit: test-results.xml
    paths:
      - test-results/
    expire_in: 1 week
  dependencies:
    - build

deploy:
  stage: deploy
  script:
    - echo ""Deploying to $TARGET_ENV...""
    - make deploy
  dependencies:
    - test
  only:
    - main
    - develop
";
    }

    /// <summary>
    /// Displays sample output for a project type
    /// </summary>
    /// <param name="projectType">Project type to show sample for</param>
    public static void ShowSampleOutput(string projectType)
    {
        Console.WriteLine($"Sample GitLab CI/CD Pipeline for {projectType.ToUpperInvariant()} Project:");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine();
        Console.WriteLine(GetSampleOutput(projectType));
        Console.WriteLine();
        Console.WriteLine("To generate a similar pipeline, use:");
        Console.WriteLine($"  gitlab-pipeline-generator --type {projectType.ToLowerInvariant()}");
        Console.WriteLine();
    }
}