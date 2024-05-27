namespace SolidCode.Extensions.Configuration.Yaml;

using Microsoft.Extensions.Configuration;
using SolidCode.Extensions.Configuration.Yaml.YamlParser;

/// <summary>Implements the <see cref="IConfigurationSource" /> interface to represent a YAML configuration source.</summary>
internal sealed class YamlConfigurationProvider(YamlConfigurationSource source, YamlConfigurationOptions? yamlConfigurationOptions)
	: FileConfigurationProvider(source)
{
	private readonly YamlConfigurationOptions _yamlConfigurationOptions = yamlConfigurationOptions ?? new YamlConfigurationOptions();

	/// <inheritdoc />
	public override void Load(Stream stream)
		=> Data = YamlConfigurationParser.Parse(stream, _yamlConfigurationOptions);
}