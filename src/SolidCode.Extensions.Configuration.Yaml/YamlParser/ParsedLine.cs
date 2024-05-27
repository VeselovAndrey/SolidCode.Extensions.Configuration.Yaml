namespace SolidCode.Extensions.Configuration.Yaml.YamlParser;

/// <summary>Contains the parsed line parameters and spans for line key and value.</summary>
internal readonly ref struct ParsedLine(YamlLineType lineType, int indent, ReadOnlySpan<char> key, ReadOnlySpan<char> value)
{
	/// <summary>The type of the parsed line.</summary>
	public readonly YamlLineType LineType = lineType;

	/// <summary>The indent level of the parsed line.</summary>
	public readonly int Indent = indent;

	/// <summary>The key.</summary>
	public readonly ReadOnlySpan<char> Key = key;

	/// <summary>The value.</summary>
	public readonly ReadOnlySpan<char> Value = value;
}

/// <summary>Defines possible parsed line types.</summary>
internal enum YamlLineType : byte
{
	/// <summary>The line type is unspecified. Not a real line type, must be used to check against uninitialized values.</summary>
	Unspecified = 0,

	/// <summary>The line is empty.</summary>
	Empty,

	/// <summary>The line contains a comment.</summary>
	Comment,

	/// <summary>The line is a mapping.</summary>
	Mapping,

	/// <summary>The line is a sequence.</summary>
	Sequence,

	/// <summary>The line is a scalar.</summary>
	Scalar
}