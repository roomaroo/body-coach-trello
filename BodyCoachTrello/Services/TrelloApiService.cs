using BodyCoachTrello.Configuration;
using BodyCoachTrello.Models.Trello;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace BodyCoachTrello.Services;

/// <summary>
/// Service for interacting with the Trello API
/// </summary>
public interface ITrelloApiService
{
    /// <summary>
    /// Create a new board
    /// </summary>
    Task<TrelloBoard> CreateBoardAsync(CreateBoardRequest request);

    /// <summary>
    /// Create a new list on a board
    /// </summary>
    Task<TrelloList> CreateListAsync(CreateListRequest request);

    /// <summary>
    /// Create a new card on a list
    /// </summary>
    Task<TrelloCard> CreateCardAsync(CreateCardRequest request);

    /// <summary>
    /// Get lists on a board
    /// </summary>
    Task<List<TrelloList>> GetBoardListsAsync(string boardId);

    /// <summary>
    /// Get all boards for the authenticated user
    /// </summary>
    Task<List<TrelloBoard>> GetUserBoardsAsync();

    /// <summary>
    /// Get a board by ID
    /// </summary>
    Task<TrelloBoard?> GetBoardByIdAsync(string boardId);

    /// <summary>
    /// Test API connectivity
    /// </summary>
    Task<bool> TestConnectionAsync();
}

/// <summary>
/// Implementation of Trello API service
/// </summary>
public class TrelloApiService : ITrelloApiService
{
    private readonly HttpClient _httpClient;
    private readonly TrelloConfiguration _config;
    private readonly ILogger<TrelloApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public TrelloApiService(
        HttpClient httpClient,
        IOptions<TrelloConfiguration> config,
        ILogger<TrelloApiService> logger)
    {
        _httpClient = httpClient;
        _config = config.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_config.BaseUrl);
    }

    public async Task<TrelloBoard> CreateBoardAsync(CreateBoardRequest request)
    {
        _logger.LogInformation("Creating Trello board: {BoardName}", request.Name);

        var queryParams = new List<string>
        {
            $"name={Uri.EscapeDataString(request.Name)}",
            $"defaultLists={request.DefaultLists.ToString().ToLower()}",
            GetAuthQueryParams()
        };

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            queryParams.Insert(1, $"desc={Uri.EscapeDataString(request.Description)}");
        }

        var url = $"boards?{string.Join("&", queryParams)}";

        try
        {
            var response = await _httpClient.PostAsync(url, null);
            await EnsureSuccessStatusCodeAsync(response);

            var content = await response.Content.ReadAsStringAsync();
            var board = JsonSerializer.Deserialize<TrelloBoard>(content, _jsonOptions);

            if (board == null)
            {
                throw new InvalidOperationException("Failed to deserialize board response");
            }

            _logger.LogInformation("Successfully created board: {BoardId} - {BoardName}", board.Id, board.Name);
            return board;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating board: {BoardName}", request.Name);
            throw;
        }
    }

    public async Task<TrelloList> CreateListAsync(CreateListRequest request)
    {
        _logger.LogInformation("Creating Trello list: {ListName} on board {BoardId}", request.Name, request.BoardId);

        var queryParams = new List<string>
        {
            $"name={Uri.EscapeDataString(request.Name)}",
            $"idBoard={request.BoardId}",
            GetAuthQueryParams()
        };

        if (!string.IsNullOrWhiteSpace(request.Position))
        {
            queryParams.Insert(2, $"pos={request.Position}");
        }

        var url = $"lists?{string.Join("&", queryParams)}";

        try
        {
            var response = await _httpClient.PostAsync(url, null);
            await EnsureSuccessStatusCodeAsync(response);

            var content = await response.Content.ReadAsStringAsync();
            var list = JsonSerializer.Deserialize<TrelloList>(content, _jsonOptions);

            if (list == null)
            {
                throw new InvalidOperationException("Failed to deserialize list response");
            }

            _logger.LogInformation("Successfully created list: {ListId} - {ListName}", list.Id, list.Name);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating list: {ListName}", request.Name);
            throw;
        }
    }

    public async Task<TrelloCard> CreateCardAsync(CreateCardRequest request)
    {
        _logger.LogDebug("Creating Trello card: {CardName} on list {ListId}", request.Name, request.ListId);

        var queryParams = new List<string>
        {
            $"name={Uri.EscapeDataString(request.Name)}",
            $"idList={request.ListId}",
            GetAuthQueryParams()
        };

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            queryParams.Insert(2, $"desc={Uri.EscapeDataString(request.Description)}");
        }

        if (!string.IsNullOrWhiteSpace(request.Position))
        {
            queryParams.Insert(-1, $"pos={request.Position}");
        }

        var url = $"cards?{string.Join("&", queryParams)}";

        try
        {
            var response = await _httpClient.PostAsync(url, null);
            await EnsureSuccessStatusCodeAsync(response);

            var content = await response.Content.ReadAsStringAsync();
            var card = JsonSerializer.Deserialize<TrelloCard>(content, _jsonOptions);

            if (card == null)
            {
                throw new InvalidOperationException("Failed to deserialize card response");
            }

            _logger.LogDebug("Successfully created card: {CardId} - {CardName}", card.Id, card.Name);
            return card;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating card: {CardName}", request.Name);
            throw;
        }
    }

    public async Task<List<TrelloList>> GetBoardListsAsync(string boardId)
    {
        _logger.LogInformation("Getting lists for board: {BoardId}", boardId);

        var url = $"boards/{boardId}/lists?{GetAuthQueryParams()}";

        try
        {
            var response = await _httpClient.GetAsync(url);
            await EnsureSuccessStatusCodeAsync(response);

            var content = await response.Content.ReadAsStringAsync();
            var lists = JsonSerializer.Deserialize<List<TrelloList>>(content, _jsonOptions);

            if (lists == null)
            {
                throw new InvalidOperationException("Failed to deserialize lists response");
            }

            _logger.LogInformation("Retrieved {ListCount} lists for board {BoardId}", lists.Count, boardId);
            return lists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lists for board: {BoardId}", boardId);
            throw;
        }
    }

    public async Task<List<TrelloBoard>> GetUserBoardsAsync()
    {
        try
        {
            _logger.LogDebug("Getting user boards");

            var url = $"{_config.BaseUrl}members/me/boards?{GetAuthQueryParams()}";
            var response = await _httpClient.GetAsync(url);
            await EnsureSuccessStatusCodeAsync(response);

            var json = await response.Content.ReadAsStringAsync();
            var boards = JsonSerializer.Deserialize<List<TrelloBoard>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogDebug("Retrieved {Count} boards", boards?.Count ?? 0);
            return boards ?? new List<TrelloBoard>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user boards");
            throw;
        }
    }

    public async Task<TrelloBoard?> GetBoardByIdAsync(string boardId)
    {
        try
        {
            _logger.LogDebug("Getting board by ID: {BoardId}", boardId);

            var url = $"boards/{boardId}?{GetAuthQueryParams()}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogDebug("Board not found: {BoardId}", boardId);
                return null;
            }
            
            await EnsureSuccessStatusCodeAsync(response);

            var content = await response.Content.ReadAsStringAsync();
            var board = JsonSerializer.Deserialize<TrelloBoard>(content, _jsonOptions);

            if (board != null)
            {
                _logger.LogDebug("Found board: {BoardId} - {BoardName}", board.Id, board.Name);
            }

            return board;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting board by ID: {BoardId}", boardId);
            throw;
        }
    }

    public async Task<bool> TestConnectionAsync()
    {
        _logger.LogInformation("Testing Trello API connection");

        try
        {
            var url = $"members/me?{GetAuthQueryParams()}";
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Trello API connection successful");
                return true;
            }
            else
            {
                _logger.LogWarning("Trello API connection failed with status: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Trello API connection");
            return false;
        }
    }

    /// <summary>
    /// Generate authentication query parameters
    /// </summary>
    private string GetAuthQueryParams()
    {
        return $"key={_config.ApiKey}&token={_config.Token}";
    }

    /// <summary>
    /// Ensure HTTP response is successful, provide detailed error information
    /// </summary>
    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            var errorMessage = $"HTTP {(int)response.StatusCode} {response.StatusCode}: {errorContent}";
            
            _logger.LogError("API call failed: {ErrorMessage}", errorMessage);
            throw new HttpRequestException(errorMessage);
        }
    }
}
