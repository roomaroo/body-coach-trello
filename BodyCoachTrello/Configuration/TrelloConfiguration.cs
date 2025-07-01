namespace BodyCoachTrello.Configuration;

/// <summary>
/// Configuration settings for Trello API
/// </summary>
public class TrelloConfiguration
{
    public const string SectionName = "Trello";

    /// <summary>
    /// Trello API Key (public)
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Trello API Token (secret)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Trello API
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.trello.com/1/";

    /// <summary>
    /// Default board ID to add shopping lists to
    /// </summary>
    public string DefaultBoardId { get; set; } = string.Empty;

    /// <summary>
    /// Default description for created boards
    /// </summary>
    public string DefaultBoardDescription { get; set; } = "Imported shopping list from Body Coach Trello app";

    /// <summary>
    /// Validates that required configuration is present
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("Trello API Key is required. Please set it using user secrets or environment variables.");
        }

        if (string.IsNullOrWhiteSpace(Token))
        {
            throw new InvalidOperationException("Trello Token is required. Please set it using user secrets or environment variables.");
        }

        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("Trello Base URL is required.");
        }
    }
}
