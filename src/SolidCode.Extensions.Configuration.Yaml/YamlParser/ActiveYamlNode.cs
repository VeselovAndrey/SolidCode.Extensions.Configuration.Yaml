namespace SolidCode.Extensions.Configuration.Yaml.YamlParser;

using Microsoft.Extensions.Configuration;

/// <summary>Represents active (unclosed) YAML node.</summary>
internal class ActiveYamlNode
{
	private readonly string _path;
	private int _currentArrayIndex;

	/// <summary>Gets the type of the node.</summary>
	public YamlNodeType NodeType { get; }

	/// <summary>Gets the indentation level of the node.</summary>
	public int Indent { get; }

	/// <summary>Gets the path of the node.</summary>
	public string Path { get; private set; }

	/// <summary>Gets or sets the current array index (valid only if <seealso cref="NodeType"/> is <seealso cref="YamlParser.YamlNodeType.Sequence"/>).</summary>
	internal int CurrentArrayIndex {
		get => _currentArrayIndex;
		set {
			_currentArrayIndex = value;
			Path = 0 <= _currentArrayIndex
				? $"{_path}{ConfigurationPath.KeyDelimiter}{CurrentArrayIndex}"
				: _path;
		}
	}

	/// <summary>Initializes a new instance of the <see cref="YamlNode"/> class.</summary>
	/// <param name="nodeType">The type of the node.</param>
	/// <param name="indent">The indentation level of the node.</param>
	/// <param name="path">The path of the node.</param>
	public ActiveYamlNode(YamlNodeType nodeType, int indent, string path)
	{
		NodeType = nodeType;
		Indent = indent;
		_path = path;
		Path = _path;
		CurrentArrayIndex = -1;
	}
}