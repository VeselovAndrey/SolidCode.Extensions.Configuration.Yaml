namespace SolidCode.Extensions.Configuration.Yaml.Exceptions;

/// <summary>Represents an exception that occurs during YAML configuration reading.</summary>
public class YamlConfigurationReadingException : YamlConfigurationException
{
	/// <summary>Initializes a new instance of the <see cref="YamlConfigurationParsingException"/> class with the specified error message and line number.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	public YamlConfigurationReadingException(string message)
		: base(message)
	{
	}
}