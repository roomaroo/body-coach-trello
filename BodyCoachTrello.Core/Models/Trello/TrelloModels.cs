using System.Text.Json.Serialization;

namespace BodyCoachTrello.Core.Models.Trello;

/// <summary>
/// Represents a Trello board
/// </summary>
public class TrelloBoard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("shortUrl")]
    public string ShortUrl { get; set; } = string.Empty;

    [JsonPropertyName("closed")]
    public bool Closed { get; set; }
}

/// <summary>
/// Represents a Trello list
/// </summary>
public class TrelloList
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("idBoard")]
    public string BoardId { get; set; } = string.Empty;

    [JsonPropertyName("pos")]
    public double Position { get; set; }

    [JsonPropertyName("closed")]
    public bool Closed { get; set; }
}

/// <summary>
/// Represents a Trello card
/// </summary>
public class TrelloCard
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("idList")]
    public string ListId { get; set; } = string.Empty;

    [JsonPropertyName("idBoard")]
    public string BoardId { get; set; } = string.Empty;

    [JsonPropertyName("pos")]
    public double Position { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("shortUrl")]
    public string ShortUrl { get; set; } = string.Empty;

    [JsonPropertyName("closed")]
    public bool Closed { get; set; }
}

/// <summary>
/// Request model for creating a list
/// </summary>
public class CreateListRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("idBoard")]
    public string BoardId { get; set; } = string.Empty;

    [JsonPropertyName("pos")]
    public string? Position { get; set; }
}

/// <summary>
/// Request model for creating a card
/// </summary>
public class CreateCardRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("desc")]
    public string? Description { get; set; }

    [JsonPropertyName("idList")]
    public string ListId { get; set; } = string.Empty;

    [JsonPropertyName("pos")]
    public string? Position { get; set; }
}