namespace SolidCode.Extensions.Configuration.Yaml.Exceptions;

/// <summary>Represents a base class for YAML configuration exceptions.</summary>
public abstract class YamlConfigurationException : Exception
{

	/// <summary>Gets the optional context of the exception (any serializable data related to the current exception).</summary>
	public object? Context { get; init; }

	/// <summary>Initializes a new instance of the <see cref="YamlConfigurationException"/> class with the specified error message.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	protected YamlConfigurationException(string message)
		: base(message)
	{
	}

	/// <summary>Initializes a new instance of the <see cref="YamlConfigurationException"/> class.</summary>
	/// <param name="message">The error message that explains the reason for the exception.</param>
	/// <param name="innerException">The exception that is the cause of the current exception, or a <c>null</c> reference if no inner exception is specified.</param>
	protected YamlConfigurationException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
