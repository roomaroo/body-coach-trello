using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using BodyCoachTrello.Core.Services;
using BodyCoachTrello.Core.Configuration;
using BodyCoachTrello.Core.Models;
using BodyCoachTrello.Core.Models.Trello;
using Microsoft.Extensions.Options;

namespace BodyCoachTrello.Tests.Services;

public class ShoppingListImporterTests
{
    private readonly Mock<ITrelloApiService> _mockTrelloApi;
    private readonly Mock<ILogger<ShoppingListImporter>> _mockLogger;
    private readonly Mock<IOptions<TrelloConfiguration>> _mockOptions;
    private readonly TrelloConfiguration _config;
    private readonly ShoppingListImporter _importer;

    public ShoppingListImporterTests()
    {
        _mockTrelloApi = new Mock<ITrelloApiService>();
        _mockLogger = new Mock<ILogger<ShoppingListImporter>>();
        _config = new TrelloConfiguration
        {
            ApiKey = "test-api-key",
            Token = "test-token",
            BaseUrl = "https://api.trello.com/1/",
            DefaultBoardId = "default-board-123"
        };
        _mockOptions = new Mock<IOptions<TrelloConfiguration>>();
        _mockOptions.Setup(x => x.Value).Returns(_config);

        _importer = new ShoppingListImporter(_mockTrelloApi.Object, _mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ImportShoppingListAsync_ExistingBoard_AddsListsToBoard()
    {
        // Arrange
        var shoppingList = CreateTestShoppingList();
        var existingBoard = new TrelloBoard
        {
            Id = _config.DefaultBoardId,
            Name = "Default Shopping Lists",
            ShortUrl = $"https://trello.com/b/{_config.DefaultBoardId}"
        };

        _mockTrelloApi.Setup(x => x.GetBoardByIdAsync(_config.DefaultBoardId))
                     .ReturnsAsync(existingBoard);

        SetupListAndCardCreation();

        // Act
        var result = await _importer.ImportShoppingListAsync(shoppingList);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingBoard.Id, result.Id);
        Assert.Equal(existingBoard.Name, result.Name);

        // Verify board lookup was called
        _mockTrelloApi.Verify(x => x.GetBoardByIdAsync(_config.DefaultBoardId), Times.Once);
        
        // Verify lists were created for each category
        _mockTrelloApi.Verify(x => x.CreateListAsync(It.Is<CreateListRequest>(r => r.Name == "Fruits")), Times.Once);
        _mockTrelloApi.Verify(x => x.CreateListAsync(It.Is<CreateListRequest>(r => r.Name == "Vegetables")), Times.Once);
    }

    [Fact]
    public async Task ImportShoppingListAsync_NonExistingBoard_ThrowsException()
    {
        // Arrange
        var shoppingList = CreateTestShoppingList();

        _mockTrelloApi.Setup(x => x.GetBoardByIdAsync(_config.DefaultBoardId))
                     .ReturnsAsync((TrelloBoard?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _importer.ImportShoppingListAsync(shoppingList));
        
        Assert.Contains("not found or not accessible", exception.Message);
        Assert.Contains(_config.DefaultBoardId, exception.Message);

        // Verify board lookup was called
        _mockTrelloApi.Verify(x => x.GetBoardByIdAsync(_config.DefaultBoardId), Times.Once);
    }

    [Fact]
    public async Task ImportShoppingListAsync_CustomBoardId_UsesCustomBoard()
    {
        // Arrange
        var shoppingList = CreateTestShoppingList();
        var customBoardId = "custom-board-123";
        var customBoard = new TrelloBoard
        {
            Id = customBoardId,
            Name = "My Custom Board",
            ShortUrl = "https://trello.com/b/custom-board-123"
        };

        _mockTrelloApi.Setup(x => x.GetBoardByIdAsync(customBoardId))
                     .ReturnsAsync(customBoard);

        SetupListAndCardCreation();

        // Act
        var result = await _importer.ImportShoppingListAsync(shoppingList, customBoardId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(customBoard.Id, result.Id);

        // Verify custom board ID was used
        _mockTrelloApi.Verify(x => x.GetBoardByIdAsync(customBoardId), Times.Once);
        _mockTrelloApi.Verify(x => x.GetBoardByIdAsync(_config.DefaultBoardId), Times.Never);
    }

    [Fact]
    public async Task ImportShoppingListAsync_EmptyShoppingList_DoesNotCreateLists()
    {
        // Arrange
        var emptyShoppingList = new ParsedShoppingList
        {
            FileName = "empty",
            ShoppingList = new ShoppingList
            {
                Name = "Empty List",
                Categories = new List<ShoppingCategory>()
            }
        };

        var board = new TrelloBoard
        {
            Id = "board-123",
            Name = "Default Shopping Lists",
            ShortUrl = "https://trello.com/b/board-123"
        };

        _mockTrelloApi.Setup(x => x.GetBoardByIdAsync(_config.DefaultBoardId))
                     .ReturnsAsync(board);

        // Act
        var result = await _importer.ImportShoppingListAsync(emptyShoppingList);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(board.Id, result.Id);

        // Verify no lists were created
        _mockTrelloApi.Verify(x => x.CreateListAsync(It.IsAny<CreateListRequest>()), Times.Never);
        _mockTrelloApi.Verify(x => x.CreateCardAsync(It.IsAny<CreateCardRequest>()), Times.Never);
    }

    [Fact]
    public async Task ImportShoppingListAsync_CategoryWithNoItems_DoesNotCreateCards()
    {
        // Arrange
        var shoppingList = new ParsedShoppingList
        {
            FileName = "test",
            ShoppingList = new ShoppingList
            {
                Name = "Test List",
                Categories = new List<ShoppingCategory>
                {
                    new() { Name = "Empty Category", Items = new List<string>() }
                }
            }
        };

        var board = new TrelloBoard { Id = "board-123", Name = "Test Board", ShortUrl = "https://trello.com/b/board-123" };
        var list = new TrelloList { Id = "list-123", Name = "Empty Category", BoardId = "board-123" };

        _mockTrelloApi.Setup(x => x.GetBoardByIdAsync(_config.DefaultBoardId)).ReturnsAsync(board);
        _mockTrelloApi.Setup(x => x.CreateListAsync(It.IsAny<CreateListRequest>())).ReturnsAsync(list);

        // Act
        var result = await _importer.ImportShoppingListAsync(shoppingList);

        // Assert
        Assert.NotNull(result);

        // Verify list was created but no cards
        _mockTrelloApi.Verify(x => x.CreateListAsync(It.IsAny<CreateListRequest>()), Times.Once);
        _mockTrelloApi.Verify(x => x.CreateCardAsync(It.IsAny<CreateCardRequest>()), Times.Never);
    }

    private ParsedShoppingList CreateTestShoppingList(string? name = null)
    {
        return new ParsedShoppingList
        {
            FileName = name ?? "test-shopping-list",
            ShoppingList = new ShoppingList
            {
                Name = name ?? "Test Shopping List",
                Categories = new List<ShoppingCategory>
                {
                    new()
                    {
                        Name = "Fruits",
                        Items = new List<string> { "Apple", "Banana", "Orange" }
                    },
                    new()
                    {
                        Name = "Vegetables",
                        Items = new List<string> { "Carrot", "Lettuce" }
                    }
                }
            }
        };
    }

    private void SetupListAndCardCreation()
    {
        // Setup list creation
        _mockTrelloApi.Setup(x => x.CreateListAsync(It.IsAny<CreateListRequest>()))
                     .ReturnsAsync((CreateListRequest request) => new TrelloList
                     {
                         Id = $"list-{Guid.NewGuid()}",
                         Name = request.Name,
                         BoardId = request.BoardId
                     });

        // Setup card creation
        _mockTrelloApi.Setup(x => x.CreateCardAsync(It.IsAny<CreateCardRequest>()))
                     .ReturnsAsync((CreateCardRequest request) => new TrelloCard
                     {
                         Id = $"card-{Guid.NewGuid()}",
                         Name = request.Name,
                         ListId = request.ListId
                     });
    }
}
