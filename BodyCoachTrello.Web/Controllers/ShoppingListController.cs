using BodyCoachTrello.Core.Services;
using BodyCoachTrello.Web.Hubs;
using BodyCoachTrello.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BodyCoachTrello.Web.Controllers;

/// <summary>
/// API Controller for shopping list operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ShoppingListController : ControllerBase
{
    private readonly IShoppingListParser _parser;
    private readonly IShoppingListImporter _importer;
    private readonly ITrelloApiService _trelloApi;
    private readonly IHubContext<ShoppingListProgressHub> _hubContext;
    private readonly ILogger<ShoppingListController> _logger;

    public ShoppingListController(
        IShoppingListParser parser,
        IShoppingListImporter importer,
        ITrelloApiService trelloApi,
        IHubContext<ShoppingListProgressHub> hubContext,
        ILogger<ShoppingListController> logger)
    {
        _parser = parser;
        _importer = importer;
        _trelloApi = trelloApi;
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Process a shopping list and import it to Trello with real-time progress updates
    /// </summary>
    /// <param name="request">Shopping list processing request</param>
    /// <returns>Processing result</returns>
    [HttpPost("process")]
    public async Task<ActionResult<ShoppingListProcessResult>> ProcessShoppingList([FromBody] ProcessShoppingListRequest request)
    {
        try
        {
            _logger.LogInformation("Processing shopping list: {Name}", request.Name);

            // Generate a unique connection ID for this processing session
            var connectionId = Guid.NewGuid().ToString();

            // Test API connection first
            if (!await _trelloApi.TestConnectionAsync())
            {
                return BadRequest(new { Error = "Unable to connect to Trello API. Please check your configuration." });
            }

            // Parse the shopping list
            var shoppingList = await _parser.ParseTextAsync(request.Content, request.Name);

            if (shoppingList.ShoppingList.Categories.Count == 0)
            {
                return BadRequest(new { Error = "No categories found in the shopping list." });
            }

            // Import to Trello with progress reporting
            var board = await _importer.ImportShoppingListWithProgressAsync(
                shoppingList,
                async (progress) =>
                {
                    // Send progress update to the specific connection group
                    await _hubContext.Clients.Group($"progress_{connectionId}")
                        .SendAsync("ProgressUpdate", new
                        {
                            totalItems = progress.TotalItems,
                            processedItems = progress.ProcessedItems,
                            progressPercentage = progress.ProgressPercentage,
                            currentCategory = progress.CurrentCategory,
                            currentItem = progress.CurrentItem,
                            isCompleted = progress.IsCompleted
                        });
                },
                request.BoardId);

            var result = new ShoppingListProcessResult
            {
                Success = true,
                Message = "Shopping list imported successfully",
                ConnectionId = connectionId,
                BoardName = board.Name,
                BoardUrl = board.ShortUrl,
                CategoriesCount = shoppingList.ShoppingList.Categories.Count,
                TotalItems = shoppingList.ShoppingList.Categories.Sum(c => c.Items.Count)
            };

            _logger.LogInformation("Successfully processed shopping list: {Name}", request.Name);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing shopping list: {Name}", request.Name);
            return StatusCode(500, new { Error = "An error occurred while processing the shopping list.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Test the Trello API connection
    /// </summary>
    /// <returns>Connection test result</returns>
    [HttpGet("test-connection")]
    public async Task<ActionResult> TestConnection()
    {
        try
        {
            var isConnected = await _trelloApi.TestConnectionAsync();
            if (isConnected)
            {
                return Ok(new { Success = true, Message = "Trello API connection successful" });
            }
            else
            {
                return BadRequest(new { Success = false, Message = "Trello API connection failed" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Trello connection");
            return StatusCode(500, new { Success = false, Message = "Error testing connection", Details = ex.Message });
        }
    }
}