using System.Collections.Generic;
using System.Text.Json;
using ArtStudio.CLI.Models;

namespace ArtStudio.CLI.Services;

/// <summary>
/// Service for parsing command line arguments into command requests
/// </summary>
public class ArgumentParser
{
    /// <summary>
    /// Parse command line arguments into a command request
    /// </summary>
    public BatchCommandRequest ParseCommandLine(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Length == 0)
            throw new ArgumentException("No command specified");

        var commandId = args[0];
        var parameters = new Dictionary<string, object>();

        for (int i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                var paramName = arg[2..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    var value = args[i + 1];
                    parameters[paramName] = ParseParameterValue(value);
                    i++; // Skip the value
                }
                else
                {
                    parameters[paramName] = true; // Boolean flag
                }
            }
        }

        return new BatchCommandRequest
        {
            CommandId = commandId,
            Parameters = parameters
        };
    }

    /// <summary>
    /// Parse parameter value from string
    /// </summary>
    public object ParseParameterValue(string value)
    {
        // Try to parse as JSON first (for complex objects)
        try
        {
            return JsonSerializer.Deserialize<object>(value) ?? value;
        }
        catch
        {
            // Fall back to simple type parsing
            if (bool.TryParse(value, out var boolValue))
                return boolValue;

            if (int.TryParse(value, out var intValue))
                return intValue;

            if (double.TryParse(value, out var doubleValue))
                return doubleValue;

            return value; // Return as string
        }
    }

    /// <summary>
    /// Parse multiple command requests from file
    /// </summary>
    public async Task<IEnumerable<BatchCommandRequest>> ParseBatchFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Batch file not found: {filePath}");

        var content = await File.ReadAllTextAsync(filePath);

        try
        {
            var requests = JsonSerializer.Deserialize<BatchCommandRequest[]>(content);
            return requests ?? Array.Empty<BatchCommandRequest>();
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid JSON in batch file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validate command parameters against command schema
    /// </summary>
    public ValidationResult ValidateParameters(
        Core.IPluginCommand command,
        IDictionary<string, object>? parameters)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        if (command.Parameters == null)
            return new ValidationResult { IsValid = true };

        parameters ??= new Dictionary<string, object>();

        // Check required parameters
        foreach (var param in command.Parameters.Values)
        {
            if (param.IsRequired && !parameters.ContainsKey(param.Name))
            {
                errors.Add($"Required parameter '{param.Name}' is missing");
            }
        }

        // Check parameter types and values
        foreach (var (name, value) in parameters)
        {
            if (!command.Parameters.TryGetValue(name, out var paramDef))
            {
                warnings.Add($"Unknown parameter '{name}' will be ignored");
                continue;
            }

            // Validate parameter value against valid values if specified
            if (paramDef.ValidValues?.Count > 0)
            {
                var stringValue = value?.ToString();
                if (stringValue != null && !paramDef.ValidValues.Contains(stringValue))
                {
                    errors.Add($"Parameter '{name}' has invalid value '{stringValue}'. Valid values: {string.Join(", ", paramDef.ValidValues)}");
                }
            }
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
}

/// <summary>
/// Validation result for command parameters
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; init; }
    public IList<string> Errors { get; init; } = new List<string>();
    public IList<string> Warnings { get; init; } = new List<string>();
}
