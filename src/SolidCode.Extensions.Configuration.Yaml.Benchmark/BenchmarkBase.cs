namespace SolidCode.Extensions.Configuration.Yaml.Benchmark;

using SolidCode.Extensions.Configuration.Yaml.YamlParser;

public abstract class BenchmarkBase
{
	private static readonly YamlConfigurationOptions _yamlConfigurationOptions = new YamlConfigurationOptions();

	protected IDictionary<string, string?> ParseYaml(byte[] yaml)
	{
		using var stream = new MemoryStream(yaml);
		IDictionary<string, string?> config = YamlConfigurationParser.Parse(stream, _yamlConfigurationOptions);

		return config;
	}

	protected IDictionary<string, string?> ParseYamlUsingYamlDotNet(byte[] yaml)
	{
		using var stream = new MemoryStream(yaml);
		IDictionary<string, string?> config = YamlDotNetParser.Parse(stream);

		return config;
	}


	protected static byte[] CreateSource(string fileName)
	{
		string yamlContent = File.ReadAllText(fileName);

		var stream = new MemoryStream();
		var writer = new StreamWriter(stream);
		writer.Write(yamlContent);
		writer.Flush();

		stream.Position = 0;

		return stream.ToArray();
	}
}