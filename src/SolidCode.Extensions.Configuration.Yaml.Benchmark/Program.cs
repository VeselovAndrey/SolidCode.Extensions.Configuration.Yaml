using BenchmarkDotNet.Running;
using SolidCode.Extensions.Configuration.Yaml.Benchmark;

#if RELEASE

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly)
	.RunAll();

#else

_ = new YamlParserBenchmarkExampleSmall().YamlConfigurationParser();
_ = new YamlParserBenchmarkExampleLarge().YamlConfigurationParser();

#endif
