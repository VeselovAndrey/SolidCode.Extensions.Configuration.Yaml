namespace SolidCode.Extensions.Configuration.Yaml.YamlParser;

/// <summary>Defines supported node types.</summary>
internal enum YamlNodeType : byte
{
	/// <summary>The node type is unspecified. Not a real node type, must be used to check against uninitialized values.</summary>
	Unspecified = 0,

	/// <summary>The node is a mapping.</summary>
	Mapping,

	/// <summary>The node is a sequence.</summary>
	Sequence,

	/// <summary>The node is a scalar.</summary>
	Scalar
}

/// <summary>Represents a YAML node.</summary>
internal struct YamlNode(YamlNodeType nodeType, int indent, string key, string value)
{
	/// <summary>The type of the <seealso cref="YamlNodeType">YAML node</seealso>.</summary>
	public YamlNodeType NodeType = nodeType;

	/// <summary>The indentation level of the YAML node.</summary>
	public int Indent = indent;

	/// <summary>The key of the YAML node.</summary>
	public string Key = key;

	/// <summary>The value of the YAML node.</summary>
	public string Value = value;
};
