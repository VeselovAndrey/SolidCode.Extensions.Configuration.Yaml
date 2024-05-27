namespace SolidCode.Extensions.Configuration.Yaml.YamlParser;

using System.Runtime.InteropServices;

/// <summary>Represents the options for parsing an YAML configuration file.</summary>
public class YamlConfigurationOptions
{
	/// <summary>The buffer size for reading the YAML configuration.</summary>
	public int BufferSize { get; init; } = 0;

	/// <summary>The type of end-of-line characters to use in the configuration for multiline scalar values.</summary>
	public EndOfLineType EndOfLineType { get; init; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? EndOfLineType.Windows : EndOfLineType.Unix;

	/// <summary>Defines how scalar ending line breaks must be handled.</summary>
	public ScalarEndingLineBreaks EndingLineBreaks { get; init; } = ScalarEndingLineBreaks.Normalize;
}

/// <summary>Represents the type of end-of-line characters.</summary>
public enum EndOfLineType : byte
{
	/// <summary>Unix end-of-line characters (LR).</summary>
	Unix = 1,

	/// <summary>Windows end-of-line characters (CR LF).</summary>
	Windows = 2
}

/// <summary>Options for handling scalar ending line breaks.</summary>
public enum ScalarEndingLineBreaks : byte
{
	/// <summary>Keep the scalar ending line breaks as is.</summary>
	KeepAsIs = 1,

	/// <summary>Normalize the scalar ending line breaks to have only one line break at the end.</summary>
	Normalize = 2,

	/// <summary>Trim all ending line breaks.</summary>
	Trim = 3
}