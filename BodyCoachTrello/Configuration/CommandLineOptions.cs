using CommandLine;

namespace BodyCoachTrello.Configuration;

/// <summary>
/// Command line arguments for the application
/// </summary>
public class CommandLineOptions
{
    /// <summary>
    /// Path to the shopping list file to import
    /// </summary>
    [Value(0, MetaName = "file", HelpText = "Path to the shopping list file to import", Required = true)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// ID of the Trello board to add lists to
    /// </summary>
    [Option('b', "board", HelpText = "ID of the Trello board to add lists to (defaults to value in appsettings.json)")]
    public string? BoardId { get; set; }

    /// <summary>
    /// Show verbose output
    /// </summary>
    [Option('v', "verbose", HelpText = "Show verbose output")]
    public bool Verbose { get; set; }
}
