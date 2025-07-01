using BodyCoachTrello.Configuration;
using BodyCoachTrello.Models;
using BodyCoachTrello.Models.Trello;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BodyCoachTrello.Services;

/// <summary>
/// Service for importing shopping lists into Trello
/// </summary>
public interface IShoppingListImporter
{
    /// <summary>
    /// Import a shopping list into an existing Trello board
    /// </summary>
    /// <param name="shoppingList">The shopping list to import</param>
    /// <param name="boardId">ID of the board to import to (optional, uses default if not provided)</param>
    /// <returns>The Trello board that was used</returns>
    Task<TrelloBoard> ImportShoppingListAsync(ParsedShoppingList shoppingList, string? boardId = null);
}

/// <summary>
/// Implementation of shopping list importer
/// </summary>
public class ShoppingListImporter : IShoppingListImporter
{
    private readonly ITrelloApiService _trelloApi;
    private readonly TrelloConfiguration _config;
    private readonly ILogger<ShoppingListImporter> _logger;

    public ShoppingListImporter(
        ITrelloApiService trelloApi,
        IOptions<TrelloConfiguration> config,
        ILogger<ShoppingListImporter> logger)
    {
        _trelloApi = trelloApi;
        _config = config.Value;
        _logger = logger;
    }

    public async Task<TrelloBoard> ImportShoppingListAsync(ParsedShoppingList shoppingList, string? boardId = null)
    {
        _logger.LogInformation("Importing shopping list: {ShoppingListName}", shoppingList.ShoppingList.Name);

        try
        {
            // Get the board by ID
            var board = await GetBoardAsync(boardId ?? _config.DefaultBoardId);

            // Create lists for each category and add cards
            foreach (var category in shoppingList.ShoppingList.Categories)
            {
                await CreateCategoryListAsync(board.Id, category);
            }

            _logger.LogInformation("Successfully imported shopping list '{ShoppingListName}' to board: {BoardUrl}",
                shoppingList.ShoppingList.Name, board.ShortUrl);

            return board;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing shopping list: {ShoppingListName}", shoppingList.ShoppingList.Name);
            throw;
        }
    }

    /// <summary>
    /// Get existing board by ID
    /// </summary>
    private async Task<TrelloBoard> GetBoardAsync(string boardId)
    {
        _logger.LogInformation("Getting board by ID: {BoardId}", boardId);

        var board = await _trelloApi.GetBoardByIdAsync(boardId);
        if (board == null)
        {
            throw new InvalidOperationException($"Board with ID '{boardId}' not found or not accessible.");
        }

        _logger.LogInformation("Found board: {BoardName} ({BoardId})", board.Name, board.Id);
        return board;
    }

    /// <summary>
    /// Create a list for a shopping category and add all items as cards
    /// </summary>
    private async Task<TrelloList> CreateCategoryListAsync(string boardId, ShoppingCategory category)
    {
        _logger.LogInformation("Creating list for category: {CategoryName} with {ItemCount} items",
            category.Name, category.Items.Count);

        // Create the list
        var listRequest = new CreateListRequest
        {
            Name = category.Name,
            BoardId = boardId,
            Position = "bottom"
        };

        var list = await _trelloApi.CreateListAsync(listRequest);

        // Add cards for each item
        var position = 1;
        foreach (var item in category.Items)
        {
            await CreateItemCardAsync(list.Id, item, position);
            position++;

            // Small delay to avoid overwhelming the API
            await Task.Delay(50);
        }

        _logger.LogInformation("Successfully created {ItemCount} cards in list: {ListName}",
            category.Items.Count, category.Name);

        return list;
    }

    /// <summary>
    /// Create a card for a shopping item
    /// </summary>
    private async Task<TrelloCard> CreateItemCardAsync(string listId, string item, int position)
    {
        var cardRequest = new CreateCardRequest
        {
            Name = item,
            ListId = listId,
            Position = position.ToString()
        };

        return await _trelloApi.CreateCardAsync(cardRequest);
    }
}
