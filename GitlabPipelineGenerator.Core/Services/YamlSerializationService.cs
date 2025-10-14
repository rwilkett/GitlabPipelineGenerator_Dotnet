using GitlabPipelineGenerator.Core.Models;
using GitlabPipelineGenerator.Core.Exceptions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;
using YamlDotNet.Serialization.TypeResolvers;

namespace GitlabPipelineGenerator.Core.Services;

/// <summary>
/// Service for handling YAML serialization with GitLab CI/CD specific formatting
/// </summary>
public class YamlSerializationService
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    public YamlSerializationService()
    {
        _serializer = CreateSerializer();
        _deserializer = CreateDeserializer();
    }

    /// <summary>
    /// Serializes a pipeline configuration to YAML format
    /// </summary>
    /// <param name="pipeline">Pipeline configuration to serialize</param>
    /// <returns>YAML representation of the pipeline</returns>
    /// <exception cref="ArgumentNullException">Thrown when pipeline is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails</exception>
    public string SerializePipeline(PipelineConfiguration pipeline)
    {
        if (pipeline == null)
            throw new ArgumentNullException(nameof(pipeline));

        try
        {
            var yaml = _serializer.Serialize(pipeline);
            
            // Post-process the YAML for GitLab CI/CD specific formatting
            yaml = PostProcessYaml(yaml);
            
            // Validate the generated YAML
            ValidateYaml(yaml);
            
            return yaml;
        }
        catch (Exception ex) when (!(ex is ArgumentNullException))
        {
            throw new YamlSerializationException($"Failed to serialize pipeline to YAML: {ex.Message}", "serialize", pipeline, ex);
        }
    }

    /// <summary>
    /// Deserializes YAML content to a pipeline configuration
    /// </summary>
    /// <param name="yaml">YAML content to deserialize</param>
    /// <returns>Pipeline configuration object</returns>
    /// <exception cref="ArgumentException">Thrown when YAML is null or empty</exception>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
    public PipelineConfiguration DeserializePipeline(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            throw new ArgumentException("YAML content cannot be null or empty", nameof(yaml));

        try
        {
            return _deserializer.Deserialize<PipelineConfiguration>(yaml);
        }
        catch (Exception ex)
        {
            throw new YamlSerializationException($"Failed to deserialize YAML to pipeline: {ex.Message}", "deserialize", yaml, ex);
        }
    }

    /// <summary>
    /// Validates that the YAML content is properly formatted and contains required GitLab CI/CD elements
    /// </summary>
    /// <param name="yaml">YAML content to validate</param>
    /// <exception cref="InvalidOperationException">Thrown when YAML is invalid</exception>
    public void ValidateYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new YamlSerializationException("YAML content is empty or whitespace", "validate", yaml);
        }

        try
        {
            // Try to deserialize to validate structure
            var testDeserialize = _deserializer.Deserialize<object>(yaml);
            
            // Basic GitLab CI/CD structure validation
            if (!yaml.Contains("stages:") && !ContainsJobDefinitions(yaml))
            {
                throw new YamlSerializationException("YAML must contain either 'stages:' or job definitions", "validate", yaml);
            }

            // Check for common GitLab CI/CD keywords
            ValidateGitLabCiCdStructure(yaml);
        }
        catch (YamlException ex)
        {
            throw new YamlSerializationException($"Invalid YAML syntax: {ex.Message}", "validate", yaml, ex);
        }
        catch (Exception ex) when (!(ex is YamlSerializationException))
        {
            throw new YamlSerializationException($"YAML validation failed: {ex.Message}", "validate", yaml, ex);
        }
    }

    /// <summary>
    /// Creates a configured YAML serializer for GitLab CI/CD pipelines
    /// </summary>
    /// <returns>Configured YAML serializer</returns>
    private static ISerializer CreateSerializer()
    {
        return new SerializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
            .WithIndentedSequences()
            .WithTypeInspector(inner => new GitLabCiCdTypeInspector(inner))
            .WithEmissionPhaseObjectGraphVisitor(args => new GitLabCiCdObjectGraphVisitor(args.InnerVisitor))
            .Build();
    }

    /// <summary>
    /// Creates a configured YAML deserializer for GitLab CI/CD pipelines
    /// </summary>
    /// <returns>Configured YAML deserializer</returns>
    private static IDeserializer CreateDeserializer()
    {
        return new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Post-processes the generated YAML to ensure GitLab CI/CD compatibility
    /// </summary>
    /// <param name="yaml">Raw YAML content</param>
    /// <returns>Post-processed YAML content</returns>
    private static string PostProcessYaml(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return yaml;

        var lines = yaml.Split('\n');
        var processedLines = new List<string>();

        foreach (var line in lines)
        {
            var processedLine = line;

            // Ensure proper formatting for GitLab CI/CD specific elements
            processedLine = ProcessGitLabSpecificFormatting(processedLine);

            // Remove empty lines at the beginning of job definitions
            if (!string.IsNullOrWhiteSpace(processedLine) || processedLines.Count == 0 || !string.IsNullOrWhiteSpace(processedLines.Last()))
            {
                processedLines.Add(processedLine);
            }
        }

        return string.Join('\n', processedLines);
    }

    /// <summary>
    /// Processes GitLab-specific formatting requirements
    /// </summary>
    /// <param name="line">YAML line to process</param>
    /// <returns>Processed YAML line</returns>
    private static string ProcessGitLabSpecificFormatting(string line)
    {
        // Handle image string vs object formatting
        if (line.Trim().StartsWith("image:"))
        {
            // If image is an object with just a name, flatten it to a string
            var nextLineIndex = line.IndexOf('\n');
            if (nextLineIndex > 0)
            {
                var nextLine = line.Substring(nextLineIndex + 1).Trim();
                if (nextLine.StartsWith("name:"))
                {
                    var imageName = nextLine.Substring(5).Trim();
                    return line.Substring(0, line.IndexOf(':') + 1) + " " + imageName;
                }
            }
        }

        return line;
    }

    /// <summary>
    /// Checks if the YAML contains job definitions
    /// </summary>
    /// <param name="yaml">YAML content to check</param>
    /// <returns>True if job definitions are found</returns>
    private static bool ContainsJobDefinitions(string yaml)
    {
        var jobKeywords = new[] { "script:", "stage:", "before_script:", "after_script:" };
        return jobKeywords.Any(keyword => yaml.Contains(keyword));
    }

    /// <summary>
    /// Validates GitLab CI/CD specific structure requirements
    /// </summary>
    /// <param name="yaml">YAML content to validate</param>
    private static void ValidateGitLabCiCdStructure(string yaml)
    {
        var lines = yaml.Split('\n');
        var hasValidStructure = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            // Check for valid GitLab CI/CD top-level keywords
            if (trimmedLine.StartsWith("stages:") ||
                trimmedLine.StartsWith("variables:") ||
                trimmedLine.StartsWith("workflow:") ||
                trimmedLine.StartsWith("include:") ||
                trimmedLine.StartsWith("default:") ||
                IsJobDefinition(trimmedLine, lines))
            {
                hasValidStructure = true;
                break;
            }
        }

        if (!hasValidStructure)
        {
            throw new YamlSerializationException("YAML does not contain valid GitLab CI/CD structure", "validate", yaml);
        }
    }

    /// <summary>
    /// Determines if a line represents a job definition
    /// </summary>
    /// <param name="line">Line to check</param>
    /// <param name="allLines">All lines in the YAML for context</param>
    /// <returns>True if the line is a job definition</returns>
    private static bool IsJobDefinition(string line, string[] allLines)
    {
        if (!line.EndsWith(":") || line.Contains(" "))
            return false;

        // Look for job-specific keywords in subsequent lines
        var lineIndex = Array.IndexOf(allLines, allLines.First(l => l.Trim() == line));
        if (lineIndex < 0 || lineIndex >= allLines.Length - 1)
            return false;

        var nextFewLines = allLines.Skip(lineIndex + 1).Take(10);
        var jobKeywords = new[] { "script:", "stage:", "image:", "before_script:", "after_script:", "variables:" };
        
        return nextFewLines.Any(nextLine => 
            jobKeywords.Any(keyword => nextLine.Trim().StartsWith(keyword)));
    }
}

/// <summary>
/// Custom type inspector for GitLab CI/CD specific serialization
/// </summary>
public class GitLabCiCdTypeInspector : TypeInspectorSkeleton
{
    private readonly ITypeInspector _innerTypeInspector;

    public GitLabCiCdTypeInspector(ITypeInspector innerTypeInspector)
    {
        _innerTypeInspector = innerTypeInspector;
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var properties = _innerTypeInspector.GetProperties(type, container);
        
        // Custom ordering for GitLab CI/CD properties
        if (type == typeof(PipelineConfiguration))
        {
            properties = OrderPipelineProperties(properties);
        }
        else if (type == typeof(Job))
        {
            properties = OrderJobProperties(properties);
        }

        return properties;
    }

    /// <summary>
    /// Orders pipeline properties according to GitLab CI/CD conventions
    /// </summary>
    /// <param name="properties">Properties to order</param>
    /// <returns>Ordered properties</returns>
    private static IEnumerable<IPropertyDescriptor> OrderPipelineProperties(IEnumerable<IPropertyDescriptor> properties)
    {
        var propertyOrder = new[] { "stages", "variables", "default", "workflow", "include" };
        var orderedProperties = new List<IPropertyDescriptor>();
        var remainingProperties = properties.ToList();

        // Add properties in preferred order
        foreach (var propertyName in propertyOrder)
        {
            var property = remainingProperties.FirstOrDefault(p => 
                p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                orderedProperties.Add(property);
                remainingProperties.Remove(property);
            }
        }

        // Add remaining properties (jobs)
        orderedProperties.AddRange(remainingProperties);

        return orderedProperties;
    }

    public override string GetEnumName(Type enumType, string name)
    {
        return _innerTypeInspector.GetEnumName(enumType, name);
    }

    public override string GetEnumValue(object enumValue)
    {
        return _innerTypeInspector.GetEnumValue(enumValue);
    }

    /// <summary>
    /// Orders job properties according to GitLab CI/CD conventions
    /// </summary>
    /// <param name="properties">Properties to order</param>
    /// <returns>Ordered properties</returns>
    private static IEnumerable<IPropertyDescriptor> OrderJobProperties(IEnumerable<IPropertyDescriptor> properties)
    {
        var propertyOrder = new[] 
        { 
            "stage", "image", "services", "before_script", "script", "after_script", 
            "variables", "cache", "artifacts", "dependencies", "needs", "rules", 
            "when", "allow_failure", "timeout", "retry", "tags", "environment" 
        };
        
        var orderedProperties = new List<IPropertyDescriptor>();
        var remainingProperties = properties.ToList();

        // Add properties in preferred order
        foreach (var propertyName in propertyOrder)
        {
            var property = remainingProperties.FirstOrDefault(p => 
                p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
            if (property != null)
            {
                orderedProperties.Add(property);
                remainingProperties.Remove(property);
            }
        }

        // Add any remaining properties
        orderedProperties.AddRange(remainingProperties);

        return orderedProperties;
    }
}

/// <summary>
/// Custom object graph visitor for GitLab CI/CD specific serialization
/// </summary>
public class GitLabCiCdObjectGraphVisitor : IObjectGraphVisitor<IEmitter>
{
    private readonly IObjectGraphVisitor<IEmitter> _nextVisitor;

    public GitLabCiCdObjectGraphVisitor(IObjectGraphVisitor<IEmitter> nextVisitor)
    {
        _nextVisitor = nextVisitor;
    }

    public bool Enter(IPropertyDescriptor? key, IObjectDescriptor value, IEmitter context, ObjectSerializer serializer)
    {
        // Handle special cases for GitLab CI/CD serialization
        if (value.Value is JobImage image && !string.IsNullOrEmpty(image.Name) && 
            image.Entrypoint == null && image.PullPolicy == null)
        {
            // Serialize simple image as string instead of object
            context.Emit(new Scalar(image.Name));
            return false;
        }

        return _nextVisitor.Enter(key, value, context, serializer);
    }

    public bool EnterMapping(IObjectDescriptor key, IObjectDescriptor value, IEmitter context, ObjectSerializer serializer)
    {
        return _nextVisitor.EnterMapping(key, value, context, serializer);
    }

    public bool EnterMapping(IPropertyDescriptor key, IObjectDescriptor value, IEmitter context, ObjectSerializer serializer)
    {
        return _nextVisitor.EnterMapping(key, value, context, serializer);
    }

    public void VisitMappingEnd(IObjectDescriptor mapping, IEmitter context, ObjectSerializer serializer)
    {
        _nextVisitor.VisitMappingEnd(mapping, context, serializer);
    }

    public void VisitMappingStart(IObjectDescriptor mapping, Type keyType, Type valueType, IEmitter context, ObjectSerializer serializer)
    {
        _nextVisitor.VisitMappingStart(mapping, keyType, valueType, context, serializer);
    }

    public void VisitScalar(IObjectDescriptor scalar, IEmitter context, ObjectSerializer serializer)
    {
        _nextVisitor.VisitScalar(scalar, context, serializer);
    }

    public void VisitSequenceEnd(IObjectDescriptor sequence, IEmitter context, ObjectSerializer serializer)
    {
        _nextVisitor.VisitSequenceEnd(sequence, context, serializer);
    }

    public void VisitSequenceStart(IObjectDescriptor sequence, Type elementType, IEmitter context, ObjectSerializer serializer)
    {
        _nextVisitor.VisitSequenceStart(sequence, elementType, context, serializer);
    }
}