using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using BodyCoachTrello.Services;
using BodyCoachTrello.Configuration;
using BodyCoachTrello.Models.Trello;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace BodyCoachTrello.Tests.Services;

public class TrelloApiServiceTests : IDisposable
{
    private readonly Mock<ILogger<TrelloApiService>> _mockLogger;
    private readonly Mock<IOptions<TrelloConfiguration>> _mockOptions;
    private readonly TrelloConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly MockHttpMessageHandler _mockHandler;

    public TrelloApiServiceTests()
    {
        _mockLogger = new Mock<ILogger<TrelloApiService>>();
        _config = new TrelloConfiguration
        {
            ApiKey = "test-api-key",
            Token = "test-token",
            BaseUrl = "https://api.trello.com/1/",
            DefaultBoardName = "Test Board",
            DefaultBoardDescription = "Test Description"
        };
        _mockOptions = new Mock<IOptions<TrelloConfiguration>>();
        _mockOptions.Setup(x => x.Value).Returns(_config);

        _mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHandler);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _mockHandler?.Dispose();
    }

    [Fact]
    public async Task CreateBoardAsync_ValidRequest_ReturnsBoard()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        var request = new CreateBoardRequest
        {
            Name = "Test Board",
            Description = "Test Description"
        };

        var expectedBoard = new TrelloBoard
        {
            Id = "board123",
            Name = "Test Board",
            ShortUrl = "https://trello.com/b/board123"
        };

        _mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedBoard, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        // Act
        var result = await service.CreateBoardAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("board123", result.Id);
        Assert.Equal("Test Board", result.Name);
        Assert.Equal("https://trello.com/b/board123", result.ShortUrl);
    }

    [Fact]
    public async Task CreateListAsync_ValidRequest_ReturnsList()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        var request = new CreateListRequest
        {
            Name = "Test List",
            BoardId = "board123"
        };

        var expectedList = new TrelloList
        {
            Id = "list123",
            Name = "Test List",
            BoardId = "board123"
        };

        _mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedList, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        // Act
        var result = await service.CreateListAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("list123", result.Id);
        Assert.Equal("Test List", result.Name);
        Assert.Equal("board123", result.BoardId);
    }

    [Fact]
    public async Task CreateCardAsync_ValidRequest_ReturnsCard()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        var request = new CreateCardRequest
        {
            Name = "Test Card",
            ListId = "list123"
        };

        var expectedCard = new TrelloCard
        {
            Id = "card123",
            Name = "Test Card",
            ListId = "list123"
        };

        _mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedCard, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        // Act
        var result = await service.CreateCardAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("card123", result.Id);
        Assert.Equal("Test Card", result.Name);
        Assert.Equal("list123", result.ListId);
    }

    [Fact]
    public async Task GetUserBoardsAsync_ValidResponse_ReturnsBoards()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        
        var expectedBoards = new List<TrelloBoard>
        {
            new() { Id = "board1", Name = "Board 1", ShortUrl = "https://trello.com/b/board1" },
            new() { Id = "board2", Name = "Board 2", ShortUrl = "https://trello.com/b/board2" }
        };

        _mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(expectedBoards, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        // Act
        var result = await service.GetUserBoardsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("board1", result[0].Id);
        Assert.Equal("Board 1", result[0].Name);
    }

    [Fact]
    public async Task FindBoardByNameAsync_ExistingBoard_ReturnsBoard()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        
        var boards = new List<TrelloBoard>
        {
            new() { Id = "board1", Name = "Shopping Lists", ShortUrl = "https://trello.com/b/board1" },
            new() { Id = "board2", Name = "Work Tasks", ShortUrl = "https://trello.com/b/board2" }
        };

        _mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(boards, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        // Act
        var result = await service.FindBoardByNameAsync("Shopping Lists");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("board1", result.Id);
        Assert.Equal("Shopping Lists", result.Name);
    }

    [Fact]
    public async Task FindBoardByNameAsync_NonExistingBoard_ReturnsNull()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        
        var boards = new List<TrelloBoard>
        {
            new() { Id = "board1", Name = "Shopping Lists", ShortUrl = "https://trello.com/b/board1" }
        };

        _mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(boards, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        // Act
        var result = await service.FindBoardByNameAsync("Non-existent Board");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task FindBoardByNameAsync_CaseInsensitive_ReturnsBoard()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        
        var boards = new List<TrelloBoard>
        {
            new() { Id = "board1", Name = "Shopping Lists", ShortUrl = "https://trello.com/b/board1" }
        };

        _mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(boards, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

        // Act
        var result = await service.FindBoardByNameAsync("shopping lists");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("board1", result.Id);
        Assert.Equal("Shopping Lists", result.Name);
    }

    [Fact]
    public async Task TestConnectionAsync_ValidConnection_ReturnsTrue()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        
        var memberResponse = new { id = "member123", username = "testuser" };
        _mockHandler.SetupResponse(HttpStatusCode.OK, JsonSerializer.Serialize(memberResponse));

        // Act
        var result = await service.TestConnectionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TestConnectionAsync_InvalidConnection_ReturnsFalse()
    {
        // Arrange
        var service = new TrelloApiService(_httpClient, _mockOptions.Object, _mockLogger.Object);
        
        _mockHandler.SetupResponse(HttpStatusCode.Unauthorized, "Unauthorized");

        // Act
        var result = await service.TestConnectionAsync();

        // Assert
        Assert.False(result);
    }
}

// Helper class for mocking HttpMessageHandler
public class MockHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private string _content = string.Empty;

    public void SetupResponse(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content)
        };

        return Task.FromResult(response);
    }
}
