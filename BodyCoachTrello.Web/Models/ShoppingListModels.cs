using System.ComponentModel.DataAnnotations;

namespace BodyCoachTrello.Web.Models;

/// <summary>
/// Request model for processing a shopping list
/// </summary>
public class ProcessShoppingListRequest
{
    /// <summary>
    /// Content of the shopping list
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Name for the shopping list
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional Trello board ID (if not provided, uses default from configuration)
    /// </summary>
    public string? BoardId { get; set; }
}

/// <summary>
/// Result model for shopping list processing
/// </summary>
public class ShoppingListProcessResult
{
    /// <summary>
    /// Whether the processing was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Processing result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Unique connection ID for tracking progress
    /// </summary>
    public string ConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Name of the Trello board used
    /// </summary>
    public string BoardName { get; set; } = string.Empty;

    /// <summary>
    /// URL of the Trello board
    /// </summary>
    public string BoardUrl { get; set; } = string.Empty;

    /// <summary>
    /// Number of categories processed
    /// </summary>
    public int CategoriesCount { get; set; }

    /// <summary>
    /// Total number of items processed
    /// </summary>
    public int TotalItems { get; set; }
}