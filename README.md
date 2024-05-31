# SolidCode.Extensions.Configuration.Yaml

![NuGet build](https://github.com/VeselovAndrey/SolidCode.Extensions.Configuration.Yaml/actions/workflows/publish_release.yml/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/SolidCode.Extensions.Configuration.Yaml.svg)](https://www.nuget.org/packages/SolidCode.Extensions.Configuration.Yaml/)  
![Development build](https://github.com/VeselovAndrey/SolidCode.Extensions.Configuration.Yaml/actions/workflows/publish_development.yml/badge.svg)

`SolidCode.Extensions.Configuration.Yaml` is a library that provides YAML configuration support for `Microsoft.Extensions.Configuration`. 

YAML is often used for configuration files due to its human-readable format. But often not all its features are required for describing configuration settings.
For example: tags, including JSON inside YAML, document stream, etc.

This library aims to achieve performance and low memory consumption by supporting **YAMLite** - a subset of YAML features most used in configuration files.

## Currently supported features:
- Mappings
- Sequences
- Scalars (including multi-line strings)
- Comments

Considering to be supported in the future:
- Flow style for sequences 
  
Not supported features:
- Tags
- Anchors
- Aliases
- Document stream
- Full flow style support (e.g. JSON inside YAML)

## Installation

You can install the `SolidCode.Extensions.Configuration.Yaml` library using the following command:
```powershell
dotnet add package SolidCode.Extensions.Configuration.Yaml 
```
Or via NuGet Package Manager in Visual Studio:
```powershell
PM> Install-Package SolidCode.Extensions.Configuration.Yaml
```

## Usage
To use the SolidCode.Extensions.Configuration.Yaml library, follow these steps:

1. After installation, the library will be referenced in your project.

2. Create a YAML configuration file in your project, for example `appsettings.yaml`, and define your configuration settings in YAML format.

3. In your code, call the `AddYamlFile()` method to add a YAML configuration file into the application configuration alongside with other configuration sources.
For example:
```csharp
IHostBuilder appBuilder = Host.CreateDefaultBuilder()
	.ConfigureAppConfiguration((hostingContext, config) => {
		config.AddYamlFile("appsettings.yaml", reloadOnChange: true, configurationOptions: options);
	});

IHost host = appBuilder.Build();
```

4. Access your configuration settings using the `IConfiguration` interface and/or options patterns as usual.

## Documentation

### `AddYamlFile()` method
To add a configuration file to your application configuration, please use the `AddYamlFile()` method:
```csharp
AddYamlFile(this IConfigurationBuilder builder, string path, bool optional = false, bool reloadOnChange = true, YamlConfigurationOptions? configurationOptions = null)
```
Parameters:
* `path` - the path to the YAML configuration file. The path can be relative to the application root directory or an absolute path.
* `optional` - whether the file is optional.
* `reloadOnChange` - whether the configuration should be reloaded if the YAML file changes.
* `configurationOptions` - allows to specify options for parsing the YAML configuration file. Options represented by the `YamlConfigurationOptions` class described below.

### YamlConfigurationOptions class
This class has following properties:
* `BufferSize` - specifies the size of the buffer (in characters) used to read the YAML file. The default value is equal to the configuration file size.
* `EndOfLineType` - specifies the type of end-of-line character(s) used in the configuration values. The default value is the default end-of-line character(s) for the current operating system.
* `EndingLineBreaks` - defines how to handle the final line break in the multiline string. Options are: 
	* keep as is
	* remove any ending line breaks
	* normalize (default value): keep only 1 line break regardless of how many line breaks were in the YAML file.

### Exceptions
* `YamlConfigurationException` - the base exception for all exceptions thrown by the library.
   * `YamlConfigurationReadingException` - thrown if there is an error while reading the YAML configuration.
   * `YamlConfigurationParsingException` - thrown if there is an error while parsing the YAML configuration.

## Benchmarks

Here is example of benchmark results for parsing YAML files (see `SolidCode.Extensions.Configuration.Yaml.Benchmark` project source code for YAML files):
```
BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3593/23H2/2023Update/SunValley3)
11th Gen Intel Core i9-11900H 2.50GHz, 1 CPU, 16 logical and 8 physical cores

| Method                                              | Mean      | Error     | StdDev   | Ratio | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------------------- |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| 'YamlConfigurationParser. File: example_large.yaml' |  35.14 us | 0.695 us  | 0.854 us |  0.07 |  5.7983 | 0.8545 |  71.32 KB |        0.19 |
| 'YamlDotNet-based parser. File: example_large.yaml' | 479.12 us | 9.365 us  | 8.760 us |  1.00 | 30.7617 | 9.7656 | 378.55 KB |        1.00 |

| Method                                              | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------------------------- |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| 'YamlConfigurationParser. File: example_small.yaml' |  7.885 us | 0.1552 us | 0.2225 us |  0.10 | 1.0834 | 0.0305 |   13.3 KB |        0.21 |
| 'YamlDotNet-based parser. File: example_small.yaml' | 77.751 us | 1.5481 us | 1.7207 us |  1.00 | 5.0049 | 0.3662 |  62.14 KB |        1.00 |
```

YamlDotNet library version: 15.1.4

## License

This library is licensed under the [MIT License](https://github.com/VeselovAndrey/SolidCode.Extensions.Configuration.Yaml/blob/main/LICENSE).