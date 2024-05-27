namespace SolidCode.Extensions.Configuration.Yaml.Tests;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SolidCode.Extensions.Configuration.Yaml.YamlParser;
using Xunit;

public class YamlConfigurationProviderTests
{
	[Theory]
	[InlineData(255)]
	[InlineData(511)]
	[InlineData(1034)]
	[InlineData(30 * 1024)]
	[InlineData(null)]
	public void YamlConfiguration_ParseAndMap_Success(int? bufferSize)
	{
		// Arrange
		YamlConfigurationOptions options = bufferSize is not null
			? new YamlConfigurationOptions { BufferSize = bufferSize.Value }
			: new YamlConfigurationOptions();

		string blockText = $"\"This is block scalar: with multiple lines.\"{Environment.NewLine}It can contain any character, including '#' and quotes.{Environment.NewLine}It is very useful for multiline strings.{Environment.NewLine}";
		string foldedText = "\"This is folded scalar: with multiple lines.\" It can contain any character, including '#' and quotes. It is very useful for multiline strings.";
		string foldedWithNewLineText = """
"This is folded scalar: with multiple lines." It can contain any character, including '#' and quotes.
This is new line 1 in folded scalar.
This is new line 2 in folded scalar.
It is very useful for strings readability.
""";

		IHostBuilder appBuilder = Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((hostingContext, config) => {
				config.Sources.Clear();
				config.AddYamlFile("config.yaml", reloadOnChange: true, configurationOptions: options);
			})
			.ConfigureServices((hostContext, services) => { services.Configure<YamlValues>(hostContext.Configuration); });
		IHost host = appBuilder.Build();

		// Act
		YamlValues yamlValues = host.Services.GetRequiredService<IOptions<YamlValues>>().Value;

		// Assert
		Assert.NotNull(yamlValues);
		Assert.Equal(expected: 42, yamlValues.Number1);
		Assert.Equal(expected: 45, yamlValues.Number2);
		Assert.Equal(expected: default, yamlValues.Number3);
		Assert.Null(yamlValues.Number4);
		Assert.True(yamlValues.Bool1);
		Assert.Equal(expected: """This is "simple" \string\ with 'quotes' inside.""", yamlValues.StringWithNoQuotes);
		Assert.Equal(expected: """This is "complex" string wrapped with double quotes and with '#' symbol.""", yamlValues.StringWithDoubleQuotes);
		Assert.Equal(expected: """This is "complex" string wrapped with quotes and with '#' symbol.""", yamlValues.StringWithQuotes);
		Assert.NotNull(yamlValues.SimpleMapping);
		Assert.Equal(expected: 75, yamlValues.SimpleMapping.Id);
		Assert.Equal(expected: "Some title with \"quotes\"", yamlValues.SimpleMapping.Title);
		Assert.Equal(expected: ["value#1-1", "value#\"2-1\"", "value#'3-1'", "value#4-1"], yamlValues.SimpleSequence1);
		Assert.Equal(expected: ["value#1-2", "value#\"2-2\"", "value#'3-2'", "value#4-2"], yamlValues.SimpleSequence2);

		Assert.Equal(expected: new SimpleMappingWithOptionalSequence(1, "Title 1"), yamlValues.ComplexSequence[0]);
		Assert.Equal(expected: new SimpleMappingWithOptionalSequence(2, "Title 2"), yamlValues.ComplexSequence[1]);
		Assert.Equal(expected: 3, yamlValues.ComplexSequence[2].Id);
		Assert.Equal(expected: "Title 3", yamlValues.ComplexSequence[2].Title);
		Assert.Equal(expected: [95, -362], yamlValues.ComplexSequence[2].Codes);

		Assert.Equal(expected: blockText, yamlValues.MultiLineSequence1[0]);
		Assert.Equal(expected: foldedWithNewLineText, yamlValues.MultiLineSequence1[1]);

		Assert.Equal(expected: new SimpleMapping(1, blockText), yamlValues.MultiLineSequence2[0]);
		Assert.Equal(expected: new SimpleMapping(2, foldedWithNewLineText), yamlValues.MultiLineSequence2[1]);

		Assert.Equal(expected: blockText, yamlValues.ScalarLiterals);
		Assert.Equal(expected: foldedText, yamlValues.ScalarFolded);
		Assert.Equal(expected: foldedWithNewLineText, yamlValues.ScalarFoldedWithNewLine);
		Assert.Equal(expected:
			[
				"This is sequence with text.",
				"It can contain any character, including '#' and quotes.",
				"It is very useful for an array of strings."
			],
			yamlValues.SequenceWithText);
	}
}

internal class YamlValues
{
	public required int Number1 { get; init; }

	public required int Number2 { get; init; }

	public required int Number3 { get; init; }

	public required int? Number4 { get; init; }

	public required bool Bool1 { get; init; }

	public required string StringWithNoQuotes { get; init; }

	public required string StringWithDoubleQuotes { get; init; }

	public required string StringWithQuotes { get; init; }

	public required SimpleMapping SimpleMapping { get; init; }

	public required string[] SimpleSequence1 { get; init; }

	public required string[] SimpleSequence2 { get; init; }

	public required SimpleMappingWithOptionalSequence[] ComplexSequence { get; init; }

	public required string[] MultiLineSequence1 { get; init; }

	public required SimpleMapping[] MultiLineSequence2 { get; init; }

	public required string ScalarLiterals { get; init; }

	public required string ScalarFolded { get; init; }

	public required string ScalarFoldedWithNewLine { get; init; }

	public required string[] SequenceWithText { get; init; }

	public required string JsonAsText { get; init; }
}

internal record SimpleMapping(int Id, string Title);

[SuppressMessage("ReSharper", "ConvertToPrimaryConstructor", Justification = "Workaround for the bug https://github.com/dotnet/runtime/issues/83803")]
internal record SimpleMappingWithOptionalSequence
{
	public int Id { get; }

	public string Title { get; }

	public int[]? Codes { get; }

	public SimpleMappingWithOptionalSequence(int id, string title, int[]? codes = null)
	{
		Id = id;
		Title = title;
		Codes = codes;
	}
}