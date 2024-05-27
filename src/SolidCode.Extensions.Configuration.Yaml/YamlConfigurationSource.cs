namespace SolidCode.Extensions.Configuration.Yaml;

using Microsoft.Extensions.Configuration;
using SolidCode.Extensions.Configuration.Yaml.YamlParser;

/// <summary>Implements the <see cref="IConfigurationSource" /> interface to represent a YAML configuration source.</summary>
internal class YamlConfigurationSource : FileConfigurationSource
{
	/// <summary>Gets the YAML configuration options.</summary>
	public YamlConfigurationOptions? YamlConfigurationOptions { get; init; }

	/// <inheritdoc />
	public override IConfigurationProvider Build(IConfigurationBuilder builder)
	{
		EnsureDefaults();
		base.EnsureDefaults(builder);

		return new YamlConfigurationProvider(this, YamlConfigurationOptions);
	}

	private void EnsureDefaults()
	{
		if (System.IO.Path.IsPathRooted(Path))
			ResolveFileProvider();
	}
}