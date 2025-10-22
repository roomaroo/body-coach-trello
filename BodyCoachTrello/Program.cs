using BodyCoachTrello.Configuration;
using BodyCoachTrello.Core.Configuration;
using BodyCoachTrello.Core.Services;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BodyCoachTrello;

/// <summary>
/// Main application class
/// </summary>
public class Application
{
    private readonly IShoppingListParser _parser;
    private readonly IShoppingListImporter _importer;
    private readonly ITrelloApiService _trelloApi;
    private readonly ILogger<Application> _logger;

    public Application(
        IShoppingListParser parser,
        IShoppingListImporter importer,
        ITrelloApiService trelloApi,
        ILogger<Application> logger)
    {
        _parser = parser;
        _importer = importer;
        _trelloApi = trelloApi;
        _logger = logger;
    }

    /// <summary>
    /// Run the application
    /// </summary>
    public async Task<int> RunAsync(string[] args)
    {
        try
        {
            _logger.LogInformation("Starting Body Coach Trello Shopping List Importer");

            // Parse command line arguments
            var parseResult = Parser.Default.ParseArguments<CommandLineOptions>(args);
            
            return await parseResult.MapResult(
                async (CommandLineOptions options) => await RunWithOptionsAsync(options),
                errors => Task.FromResult(1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application failed with error");
            return 1;
        }
    }

    /// <summary>
    /// Run the application with parsed command line options
    /// </summary>
    private async Task<int> RunWithOptionsAsync(CommandLineOptions options)
    {
        try
        {
            // Test API connection first
            if (!await TestApiConnectionAsync())
            {
                return 1;
            }

            // Parse the shopping list file
            var fileInfo = new FileInfo(options.FilePath);
            if (!fileInfo.Exists)
            {
                _logger.LogError("Shopping list file not found: {FilePath}", options.FilePath);
                return 1;
            }

            var shoppingList = await _parser.ParseFileAsync(fileInfo);
            if (shoppingList.ShoppingList.Categories.Count == 0)
            {
                _logger.LogWarning("No categories found in shopping list file: {FilePath}", options.FilePath);
                return 0;
            }

            // Import to Trello
            var board = await _importer.ImportShoppingListAsync(shoppingList, options.BoardId);

            // Display results
            DisplayResults(board, shoppingList);

            _logger.LogInformation("Application completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Application failed with error");
            return 1;
        }
    }

    /// <summary>
    /// Test API connection and credentials
    /// </summary>
    private async Task<bool> TestApiConnectionAsync()
    {
        _logger.LogInformation("Testing Trello API connection...");

        if (await _trelloApi.TestConnectionAsync())
        {
            _logger.LogInformation("✅ Trello API connection successful");
            return true;
        }
        else
        {
            _logger.LogError("❌ Trello API connection failed");
            _logger.LogError("Please check your API credentials:");
            _logger.LogError("  1. Ensure you have set your Trello API Key and Token");
            _logger.LogError("  2. For development, use: dotnet user-secrets set \"Trello:ApiKey\" \"your-key\"");
            _logger.LogError("  3. And: dotnet user-secrets set \"Trello:Token\" \"your-token\"");
            _logger.LogError("  4. Get credentials from: https://trello.com/power-ups/admin");
            return false;
        }
    }

    /// <summary>
    /// Display import results
    /// </summary>
    private void DisplayResults(BodyCoachTrello.Core.Models.Trello.TrelloBoard board, BodyCoachTrello.Core.Models.ParsedShoppingList shoppingList)
    {
        Console.WriteLine();
        Console.WriteLine("🎉 Import Results:");
        Console.WriteLine("==================");

        Console.WriteLine($"✅ Successfully imported shopping list: {shoppingList.ShoppingList.Name}");
        Console.WriteLine($"📋 Board: {board.Name}");
        Console.WriteLine($"🔗 URL: {board.ShortUrl}");
        Console.WriteLine($"📂 Categories added: {shoppingList.ShoppingList.Categories.Count}");
        Console.WriteLine($"🛒 Total items: {shoppingList.ShoppingList.Categories.Sum(c => c.Items.Count)}");
        Console.WriteLine();

        foreach (var category in shoppingList.ShoppingList.Categories)
        {
            Console.WriteLine($"  � {category.Name} ({category.Items.Count} items)");
        }

        Console.WriteLine();
        Console.WriteLine("Open the link above to view your shopping list in Trello!");
    }
}

/// <summary>
/// Program entry point
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        try
        {
            var app = host.Services.GetRequiredService<Application>();
            return await app.RunAsync(args);
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Host terminated unexpectedly");
            return 1;
        }
    }

    /// <summary>
    /// Create and configure the host builder
    /// </summary>
    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddUserSecrets<Program>();
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<TrelloConfiguration>(
                    context.Configuration.GetSection(TrelloConfiguration.SectionName));

                // Validate configuration
                services.AddSingleton<IValidateOptions<TrelloConfiguration>, ValidateTrelloConfiguration>();

                // HTTP Client
                services.AddHttpClient<ITrelloApiService, TrelloApiService>();

                // Application services
                services.AddScoped<IShoppingListParser, ShoppingListParser>();
                services.AddScoped<ITrelloApiService, TrelloApiService>();
                services.AddScoped<IShoppingListImporter, ShoppingListImporter>();
                services.AddScoped<Application>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });
}

/// <summary>
/// Configuration validator for TrelloConfiguration
/// </summary>
public class ValidateTrelloConfiguration : IValidateOptions<TrelloConfiguration>
{
    public ValidateOptionsResult Validate(string? name, TrelloConfiguration options)
    {
        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
