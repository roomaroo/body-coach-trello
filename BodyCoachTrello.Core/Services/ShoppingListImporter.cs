using BodyCoachTrello.Core.Configuration;
using BodyCoachTrello.Core.Models;
using BodyCoachTrello.Core.Models.Trello;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BodyCoachTrello.Core.Services;

/// <summary>
/// Progress information for shopping list import
/// </summary>
public class ImportProgressInfo
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public string CurrentCategory { get; set; } = string.Empty;
    public string CurrentItem { get; set; } = string.Empty;
    public double ProgressPercentage => TotalItems > 0 ? (double)ProcessedItems / TotalItems * 100 : 0;
    public bool IsCompleted => ProcessedItems >= TotalItems && TotalItems > 0;
}

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

    /// <summary>
    /// Import a shopping list with progress reporting
    /// </summary>
    /// <param name="shoppingList">The shopping list to import</param>
    /// <param name="progressCallback">Callback for progress updates</param>
    /// <param name="boardId">ID of the board to import to (optional, uses default if not provided)</param>
    /// <returns>The Trello board that was used</returns>
    Task<TrelloBoard> ImportShoppingListWithProgressAsync(
        ParsedShoppingList shoppingList, 
        Func<ImportProgressInfo, Task>? progressCallback = null, 
        string? boardId = null);
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
        return await ImportShoppingListWithProgressAsync(shoppingList, null, boardId);
    }

    public async Task<TrelloBoard> ImportShoppingListWithProgressAsync(
        ParsedShoppingList shoppingList, 
        Func<ImportProgressInfo, Task>? progressCallback = null, 
        string? boardId = null)
    {
        _logger.LogInformation("Importing shopping list: {ShoppingListName}", shoppingList.ShoppingList.Name);

        try
        {
            // Get the board by ID
            var board = await GetBoardAsync(boardId ?? _config.DefaultBoardId);

            // Calculate total items for progress tracking
            var totalItems = shoppingList.ShoppingList.Categories.Sum(c => c.Items.Count);
            var processedItems = 0;

            var progress = new ImportProgressInfo
            {
                TotalItems = totalItems,
                ProcessedItems = processedItems
            };

            // Report initial progress
            if (progressCallback != null)
            {
                await progressCallback(progress);
            }

            // Create lists for each category and add cards
            foreach (var category in shoppingList.ShoppingList.Categories)
            {
                progress.CurrentCategory = category.Name;
                await CreateCategoryListWithProgressAsync(board.Id, category, progress, progressCallback);
            }

            // Mark as completed
            progress.CurrentCategory = "Import completed";
            progress.CurrentItem = "";
            if (progressCallback != null)
            {
                await progressCallback(progress);
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
    /// Get existing board by ID, throw error if not found
    /// </summary>
    private async Task<TrelloBoard> GetBoardAsync(string boardId)
    {
        _logger.LogInformation("Getting board by ID: {BoardId}", boardId);

        var board = await _trelloApi.GetBoardByIdAsync(boardId);
        if (board == null)
        {
            throw new InvalidOperationException($"Board with ID '{boardId}' not found or not accessible. Please verify the board ID and ensure you have access to the board.");
        }

        _logger.LogInformation("Found board: {BoardName} ({BoardId})", board.Name, board.Id);
        return board;
    }

    /// <summary>
    /// Create a list for a shopping category and add all items as cards with progress reporting
    /// </summary>
    private async Task<TrelloList> CreateCategoryListWithProgressAsync(
        string boardId, 
        ShoppingCategory category, 
        ImportProgressInfo progress, 
        Func<ImportProgressInfo, Task>? progressCallback)
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

        // Add cards for each item with progress reporting
        var position = 1;
        foreach (var item in category.Items)
        {
            progress.CurrentItem = item;
            
            if (progressCallback != null)
            {
                await progressCallback(progress);
            }

            await CreateItemCardAsync(list.Id, item, position);
            position++;
            progress.ProcessedItems++;

            // Update progress after creating the card
            if (progressCallback != null)
            {
                await progressCallback(progress);
            }

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