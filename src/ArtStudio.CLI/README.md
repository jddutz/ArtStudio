# ArtStudio CLI

Command-line interface for ArtStudio automation and batch processing.

## Overview

ArtStudio CLI provides a powerful command-line interface for automating ArtStudio operations, executing batch commands, and integrating ArtStudio functionality into scripts and workflows.

## Installation

### Building from Source

```bash
dotnet build src/ArtStudio.CLI --configuration Release
```

### Running

```bash
dotnet run --project src/ArtStudio.CLI -- <command> [options]
```

Or build and run the executable:

```bash
dotnet build src/ArtStudio.CLI --configuration Release
./src/ArtStudio.CLI/bin/Release/net8.0/ArtStudio.CLI.exe <command> [options]
```

## Usage

### Basic Commands

#### Show Help

```bash
artstudio help
artstudio help <command-id>
```

#### List Available Commands

```bash
artstudio list
artstudio list --category Filter
artstudio list --enabled-only
artstudio list --details
```

#### Execute Single Command

```bash
artstudio execute <command-id> [options]
artstudio execute save-image --path output.png --format PNG
artstudio execute apply-filter --filter blur --radius 5
```

#### Execute Batch Commands

```bash
artstudio batch commands.json
artstudio batch workflow.json --continue-on-error
artstudio batch automation.json --parallel 4
```

### Global Options

- `--verbose, -v`: Enable verbose logging
- `--quiet, -q`: Suppress output except errors
- `--config <file>`: Specify configuration file
- `--format <format>`: Output format (text, json, yaml, table)
- `--help, -h`: Show help information
- `--version`: Show version information

### Output Formats

The CLI supports multiple output formats:

- **text** (default): Human-readable text output
- **json**: JSON format for programmatic consumption
- **yaml**: YAML format for configuration files
- **table**: Tabular format for structured data

Example:

```bash
artstudio list --format json
artstudio execute save-image --path output.png --format yaml
```

## Batch Files

Batch files are JSON documents that define multiple commands to execute in sequence.

### Example Batch File

```json
[
  {
    "commandId": "create-document",
    "description": "Create a new document",
    "parameters": {
      "width": 1920,
      "height": 1080,
      "format": "RGBA32"
    }
  },
  {
    "commandId": "apply-filter",
    "description": "Apply blur filter",
    "parameters": {
      "filter": "blur",
      "radius": 10
    },
    "continueOnError": false,
    "timeoutMs": 30000
  },
  {
    "commandId": "save-image",
    "description": "Save the result",
    "parameters": {
      "path": "output/processed.png",
      "format": "PNG",
      "quality": 95
    }
  }
]
```

### Batch Options

- `--continue-on-error, -c`: Continue executing remaining commands if one fails
- `--parallel <count>, -p`: Number of commands to execute in parallel
- `--timeout <ms>, -t`: Global timeout for the entire batch
- `--validate, -v`: Validate commands before execution
- `--log-file <path>, -l`: Save execution log to file
- `--dry-run, -n`: Show what would be executed without running

## Command Parameters

Commands accept parameters in key=value format:

```bash
artstudio execute save-image --param path=output.png --param format=PNG --param quality=95
```

Parameters support different types:

- **String**: `--param name="My Image"`
- **Number**: `--param width=1920 --param quality=95`
- **Boolean**: `--param overwrite=true`
- **JSON**: `--param settings='{"compression": "lossless"}'`

## Configuration

### Configuration File

You can specify a configuration file with global settings:

```bash
artstudio --config config.json execute save-image --path output.png
```

Example configuration file:

```json
{
  "logLevel": "Information",
  "defaultOutputFormat": "text",
  "defaultTimeout": 30000,
  "plugins": {
    "searchPaths": ["./plugins", "~/.artstudio/plugins"]
  }
}
```

### Environment Variables

- `ARTSTUDIO_CONFIG`: Path to default configuration file
- `ARTSTUDIO_LOG_LEVEL`: Default log level (Trace, Debug, Information, Warning, Error, Critical)
- `ARTSTUDIO_PLUGINS_PATH`: Additional plugin search paths (semicolon-separated)

## Integration Examples

### PowerShell Script

```powershell
# Batch process multiple images
$images = Get-ChildItem "*.jpg"
foreach ($image in $images) {
    $output = $image.BaseName + "_processed.png"
    artstudio execute process-image --param input=$image.FullName --param output=$output
}
```

### Bash Script

```bash
#!/bin/bash
# Create batch file from template
for file in *.jpg; do
    echo "Processing $file"
    artstudio execute convert-format --param input="$file" --param output="${file%.jpg}.png"
done
```

### CI/CD Pipeline

```yaml
# Example GitHub Actions step
- name: Process Images
  run: |
    dotnet run --project src/ArtStudio.CLI -- batch ci-pipeline.json --format json > results.json
    if [ $? -ne 0 ]; then
      echo "Image processing failed"
      exit 1
    fi
```

## Error Handling

The CLI returns appropriate exit codes:

- `0`: Success
- `1`: Command failed or error occurred

Errors are logged to stderr, while normal output goes to stdout, making it easy to separate success output from error messages in scripts.

## Logging

Configure logging verbosity:

- `--quiet`: Only errors
- Default: Information and above
- `--verbose`: Debug and above

Log output includes timestamps and structured information for troubleshooting.

## Performance

The CLI is optimized for automation scenarios:

- Minimal startup time
- Efficient memory usage
- Parallel batch execution
- Streaming output for large operations

## Architecture

The CLI project is organized into several key components:

### Services

- **CommandExecutor**: Executes individual commands
- **BatchProcessor**: Handles batch command execution
- **ArgumentParser**: Parses command line arguments and batch files
- **HelpProvider**: Generates help documentation
- **OutputFormatter**: Formats output in different formats
- **HeadlessEditorService**: Provides editor functionality without UI

### Commands

- **ExecuteCommandBuilder**: Builds the execute command
- **BatchCommandBuilder**: Builds the batch command
- **ListCommandBuilder**: Builds the list command
- **HelpCommandBuilder**: Builds the help command
- **RootCommandBuilder**: Coordinates all commands

### Models

- **BatchCommandRequest**: Represents a command in a batch
- **BatchCommandResult**: Result of a batch command execution
- **BatchResult**: Overall result of batch execution
- **BatchOptions**: Configuration for batch execution

## Contributing

When adding new CLI functionality:

1. Add new services to the `Services` folder
2. Create command builders in the `Commands` folder
3. Add models to the `Models` folder
4. Update dependency injection in `Program.cs`
5. Add tests for new functionality
6. Update this documentation

## Migration from Core CliCommandExecutor

The functionality previously in `ArtStudio.Core.Services.CliCommandExecutor` has been distributed throughout this CLI project:

- **Command execution**: `Services/CommandExecutor.cs`
- **Batch processing**: `Services/BatchProcessor.cs`
- **Argument parsing**: `Services/ArgumentParser.cs`
- **Help generation**: `Services/HelpProvider.cs`
- **Output formatting**: `Services/OutputFormatter.cs`

The original class is marked as obsolete and should be replaced with this standalone CLI application.
