using GitlabPipelineGenerator.Core.Exceptions;
using GitlabPipelineGenerator.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for validating pipeline configurations and options
/// </summary>
public class ValidationService
{
    /// <summary>
    /// Validates pipeline options and throws exception if invalid
    /// </summary>
    /// <param name="options">Pipeline options to validate</param>
    /// <exception cref="InvalidPipelineOptionsException">Thrown when options are invalid</exception>
    public static void ValidateAndThrow(PipelineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        // Use data annotations validation
        var validationContext = new ValidationContext(options);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            errors.AddRange(validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error"));
        }

        // Use custom validation method
        var customErrors = options.Validate();
        errors.AddRange(customErrors);

        if (errors.Any())
        {
            throw new InvalidPipelineOptionsException(errors, options);
        }
    }

    /// <summary>
    /// Validates pipeline options and returns validation result
    /// </summary>
    /// <param name="options">Pipeline options to validate</param>
    /// <returns>Validation result with errors if any</returns>
    public static PipelineValidationResult ValidateOptions(PipelineOptions options)
    {
        if (options == null)
        {
            return new PipelineValidationResult(false, new[] { "Pipeline options cannot be null" });
        }

        var errors = new List<string>();

        // Use data annotations validation
        var validationContext = new ValidationContext(options);
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!Validator.TryValidateObject(options, validationContext, validationResults, true))
        {
            errors.AddRange(validationResults.Select(vr => vr.ErrorMessage ?? "Unknown validation error"));
        }

        // Use custom validation method
        var customErrors = options.Validate();
        errors.AddRange(customErrors);

        return new PipelineValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Validates project type
    /// </summary>
    /// <param name="projectType">Project type to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidProjectType(string? projectType)
    {
        if (string.IsNullOrWhiteSpace(projectType))
            return false;

        var validProjectTypes = new[] { "dotnet", "nodejs", "python", "docker", "generic" };
        return validProjectTypes.Contains(projectType.ToLowerInvariant());
    }

    /// <summary>
    /// Validates .NET version
    /// </summary>
    /// <param name="version">Version to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidDotNetVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return true; // Optional field

        var validVersions = new[] { "6.0", "7.0", "8.0", "9.0" };
        return validVersions.Contains(version);
    }

    /// <summary>
    /// Validates stage name
    /// </summary>
    /// <param name="stageName">Stage name to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidStageName(string? stageName)
    {
        if (string.IsNullOrWhiteSpace(stageName))
            return false;

        // Stage names should not contain special characters that could break YAML
        return !stageName.Any(c => char.IsControl(c) || c == ':' || c == '[' || c == ']' || c == '{' || c == '}');
    }

    /// <summary>
    /// Validates variable name
    /// </summary>
    /// <param name="variableName">Variable name to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidVariableName(string? variableName)
    {
        if (string.IsNullOrWhiteSpace(variableName))
            return false;

        // Variable names should follow environment variable naming conventions
        return variableName.All(c => char.IsLetterOrDigit(c) || c == '_') && 
               !char.IsDigit(variableName[0]);
    }

    /// <summary>
    /// Validates URL format
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true; // Optional field

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validates file path format
    /// </summary>
    /// <param name="filePath">File path to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        try
        {
            Path.GetFullPath(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates Docker image name format
    /// </summary>
    /// <param name="imageName">Docker image name to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidDockerImageName(string? imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName))
            return true; // Optional field

        // Basic Docker image name validation
        // Format: [registry/]namespace/repository[:tag]
        var parts = imageName.Split(':');
        if (parts.Length > 2)
            return false;

        var namepart = parts[0];
        if (string.IsNullOrWhiteSpace(namepart))
            return false;

        // Check for invalid characters
        return namepart.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' || c == '/');
    }

    /// <summary>
    /// Validates cache key format
    /// </summary>
    /// <param name="cacheKey">Cache key to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidCacheKey(string? cacheKey)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
            return true; // Optional field

        // Cache keys should not contain spaces or special characters that could break YAML
        return !cacheKey.Any(c => char.IsWhiteSpace(c) || char.IsControl(c));
    }

    /// <summary>
    /// Validates artifact expiration format
    /// </summary>
    /// <param name="expiration">Expiration string to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidArtifactExpiration(string? expiration)
    {
        if (string.IsNullOrWhiteSpace(expiration))
            return true; // Optional field

        var validUnits = new[] { "sec", "min", "hr", "hour", "day", "week", "month", "year" };
        var parts = expiration.Split(' ');
        
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var number) || number <= 0)
            return false;

        var unit = parts[1].ToLowerInvariant();
        return validUnits.Any(validUnit => unit.StartsWith(validUnit, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets detailed validation suggestions for common issues
    /// </summary>
    /// <param name="errors">List of validation errors</param>
    /// <returns>List of suggestions to fix the errors</returns>
    public static List<string> GetValidationSuggestions(IEnumerable<string> errors)
    {
        var suggestions = new List<string>();

        foreach (var error in errors)
        {
            var lowerError = error.ToLowerInvariant();

            if (lowerError.Contains("project type"))
            {
                suggestions.Add("Use one of the supported project types: dotnet, nodejs, python, docker, generic");
            }
            else if (lowerError.Contains(".net version"))
            {
                suggestions.Add("Use a supported .NET version: 6.0, 7.0, 8.0, or 9.0");
            }
            else if (lowerError.Contains("stage"))
            {
                suggestions.Add("Ensure stage names are not empty and don't contain special characters like :, [, ], {, }");
            }
            else if (lowerError.Contains("variable"))
            {
                suggestions.Add("Variable names should contain only letters, numbers, and underscores, and cannot start with a number");
            }
            else if (lowerError.Contains("url"))
            {
                suggestions.Add("Ensure URLs are properly formatted with http:// or https:// protocol");
            }
            else if (lowerError.Contains("docker image"))
            {
                suggestions.Add("Docker image names should follow the format: [registry/]namespace/repository[:tag]");
            }
            else if (lowerError.Contains("cache key"))
            {
                suggestions.Add("Cache keys should not contain spaces or special characters");
            }
            else if (lowerError.Contains("expiration"))
            {
                suggestions.Add("Use format like '1 week', '30 days', '2 hours' for expiration times");
            }
        }

        if (!suggestions.Any())
        {
            suggestions.Add("Please check the documentation for valid configuration options");
        }

        return suggestions.Distinct().ToList();
    }
}

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class PipelineValidationResult
{
    /// <summary>
    /// Gets whether the validation was successful
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the validation errors
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the PipelineValidationResult class
    /// </summary>
    /// <param name="isValid">Whether the validation was successful</param>
    /// <param name="errors">The validation errors</param>
    public PipelineValidationResult(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors.ToList().AsReadOnly();
    }
}