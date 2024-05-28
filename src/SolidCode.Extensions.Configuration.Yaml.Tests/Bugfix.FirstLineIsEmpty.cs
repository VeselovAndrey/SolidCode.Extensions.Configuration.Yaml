namespace SolidCode.Extensions.Configuration.Yaml.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

public class BugfixFirstLineIsEmpty
{
	[Fact]
	public void YamlConfiguration_ParseAndMap_FirstLineCanBeEmpty_Success()
	{
		// Arrange
		IHostBuilder appBuilder = Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((hostingContext, config) => {
				config.Sources.Clear();
				config.AddYamlFile("Bugfix.FirstLineIsEmpty.yaml", reloadOnChange: true);
			})
			.ConfigureServices((hostContext, services) => { services.Configure<BugfixYaml>(hostContext.Configuration); });
		IHost host = appBuilder.Build();

		// Act
		BugfixYaml yamlValues = host.Services.GetRequiredService<IOptions<BugfixYaml>>().Value;

		// Assert
		Assert.Equal(expected: 42, yamlValues.Id);
	}

}

public class BugfixYaml
{
	public required int Id { get; init; }
}
