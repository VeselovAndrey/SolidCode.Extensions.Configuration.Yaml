namespace SolidCode.Extensions.Configuration.Yaml;

using Microsoft.Extensions.Configuration;
using SolidCode.Extensions.Configuration.Yaml.YamlParser;

/// <summary>Extension methods for adding <see cref="YamlConfigurationProvider"/>.</summary>
public static class ConfigurationManagerExtensions
{

	/// <summary>Adds the debug configuration provider that fill use configuration file with name <paramref name="path"/> to <paramref name="builder"/>.</summary>
	/// <param name="builder">The configuration builder instance.</param>
	/// <param name="path">Specifies the configuration file (relative to application root with file name included).</param>
	/// <param name="optional">Specifies whether the file is optional.</param>
	/// <param name="reloadOnChange">Specifies whether the configuration should be reloaded if the file changes.</param>
	/// <param name="configurationOptions">The options for parsing the YAML configuration file.</param>
	/// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
	public static IConfigurationBuilder AddYamlFile(this IConfigurationBuilder builder, string path, bool optional = false, bool reloadOnChange = true, YamlConfigurationOptions? configurationOptions = null)
	{
		builder.Add(new YamlConfigurationSource() {
			Path = path,
			Optional = optional,
			ReloadOnChange = reloadOnChange
		});

		return builder;
	}
}