namespace SolidCode.Extensions.Configuration.Yaml.Benchmark;

using BenchmarkDotNet.Attributes;

[SimpleJob]
[MemoryDiagnoser]
public class YamlParserBenchmarkExampleLarge : BenchmarkBase
{
	private static readonly byte[] _example2 = CreateSource("example_large.yaml");

	[Benchmark(Description = "YamlConfigurationParser. File: example_large.yaml")]
	public IDictionary<string, string?> YamlConfigurationParser() => ParseYaml(_example2);

	[Benchmark(Baseline = true, Description = "YamlDotNet-based parser. File: example_large.yaml")]
	public IDictionary<string, string?> YamlDotNetParser() => ParseYamlUsingYamlDotNet(_example2);
}