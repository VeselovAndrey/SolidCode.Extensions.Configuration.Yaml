namespace SolidCode.Extensions.Configuration.Yaml.Benchmark;

using Microsoft.Extensions.Configuration;
using YamlDotNet.RepresentationModel;

/// <summary>The configuration parser implementation using YamlDotNet.</summary>
internal static class YamlDotNetParser
{
	private static readonly IDictionary<string, string?> _data = new SortedDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
	private static readonly Stack<string> _paths = new Stack<string>();
	private static string _currentPath = string.Empty;

	public static IDictionary<string, string?> Parse(Stream input)
	{
		_data.Clear();
		_paths.Clear();

		var yaml = new YamlStream();
		yaml.Load(new StreamReader(input, detectEncodingFromByteOrderMarks: true));

		if (0 < yaml.Documents.Count) {
			var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;
			VisitYamlMappingNode(mapping);
		}

		return _data;
	}

	private static void VisitYamlNodePair(KeyValuePair<YamlNode, YamlNode> yamlNodePair)
	{
		string context = ((YamlScalarNode)yamlNodePair.Key).Value!;
		VisitYamlNode(context, yamlNodePair.Value);
	}

	private static void VisitYamlNode(string context, YamlNode node)
	{
		switch (node) {
			case YamlScalarNode scalarNode:
				VisitYamlScalarNode(context, scalarNode);
				break;

			case YamlMappingNode mappingNode:
				VisitYamlMappingNode(context, mappingNode);
				break;

			case YamlSequenceNode sequenceNode:
				VisitYamlSequenceNode(context, sequenceNode);
				break;
		}
	}

	private static void VisitYamlScalarNode(string context, YamlScalarNode yamlValue)
	{
		EnterContext(context);
		_data[_currentPath] = IsNullValue(yamlValue) ? null : yamlValue.Value;
		ExitContext();
	}

	private static void VisitYamlMappingNode(YamlMappingNode node)
	{
		foreach (KeyValuePair<YamlNode, YamlNode> yamlNodePair in node.Children)
			VisitYamlNodePair(yamlNodePair);
	}

	private static void VisitYamlMappingNode(string context, YamlMappingNode yamlValue)
	{
		EnterContext(context);
		VisitYamlMappingNode(yamlValue);
		ExitContext();
	}

	private static void VisitYamlSequenceNode(string context, YamlSequenceNode yamlValue)
	{
		EnterContext(context);
		VisitYamlSequenceNode(yamlValue);
		ExitContext();
	}

	private static void VisitYamlSequenceNode(YamlSequenceNode node)
	{
		for (int i = 0; i < node.Children.Count; i++)
			VisitYamlNode(i.ToString(), node.Children[i]);
	}

	private static void EnterContext(string context)
	{
		_paths.Push(context);
		_currentPath = ConfigurationPath.Combine(_paths.Reverse());
	}

	private static void ExitContext()
	{
		_paths.Pop();
		_currentPath = ConfigurationPath.Combine(_paths.Reverse());
	}

	private static bool IsNullValue(YamlScalarNode yamlValue)
		=> yamlValue is { Style: YamlDotNet.Core.ScalarStyle.Plain, Value: "~" or "null" or "Null" or "NULL" };
}