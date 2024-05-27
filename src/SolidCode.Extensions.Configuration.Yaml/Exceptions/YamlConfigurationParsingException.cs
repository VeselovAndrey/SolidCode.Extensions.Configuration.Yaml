namespace SolidCode.Extensions.Configuration.Yaml.Exceptions;

/// <summary>Represents an exception that occurs during YAML configuration parsing.</summary>
public class YamlConfigurationParsingException : YamlConfigurationException
{
	/// <summary>Initializes a new instance of the <see cref="YamlConfigurationParsingException"/> class with the specified error message and line number.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	public YamlConfigurationParsingException(string message)
		: base(message)
	{
	}
}