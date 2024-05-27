namespace SolidCode.Extensions.Configuration.Yaml.Benchmark;

using BenchmarkDotNet.Attributes;

[SimpleJob]
[MemoryDiagnoser]
public class YamlParserBenchmarkExampleSmall : BenchmarkBase
{
	private static readonly byte[] _example1 = CreateSource("example_small.yaml");

	[Benchmark(Description = "YamlConfigurationParser. File: example_small.yaml")]
	public IDictionary<string, string?> YamlConfigurationParser() => ParseYaml(_example1);

	[Benchmark(Baseline = true, Description = "YamlDotNet-based parser. File: example_small.yaml")]
	public IDictionary<string, string?> YamlDotNetParser() => ParseYamlUsingYamlDotNet(_example1);
}