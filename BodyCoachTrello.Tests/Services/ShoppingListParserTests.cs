using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using BodyCoachTrello.Services;
using BodyCoachTrello.Models;

namespace BodyCoachTrello.Tests.Services;

public class ShoppingListParserTests : IDisposable
{
    private readonly Mock<ILogger<ShoppingListParser>> _mockLogger;
    private readonly ShoppingListParser _parser;
    private readonly string _tempDirectory;

    public ShoppingListParserTests()
    {
        _mockLogger = new Mock<ILogger<ShoppingListParser>>();
        _parser = new ShoppingListParser(_mockLogger.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ParseFileAsync_ValidFile_ParsesCorrectly()
    {
        // Arrange
        var content = @"Fruit, vegetables and salad
4 apples, small
100g avocados
2 (215g) bananas, medium

Fresh herbs and spices
20g chia seeds
4 small bunches coriander

Dairy, eggs and chilled
130g butter (unsalted)
40g cheese (cheddar)";

        var testFile = CreateTestFile("test-shopping-list.txt", content);

        // Act
        var result = await _parser.ParseFileAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test shopping list", result.ShoppingList.Name);
        Assert.Equal("test-shopping-list", result.FileName);
        Assert.Equal(3, result.ShoppingList.Categories.Count);

        // Check first category
        var fruitCategory = result.ShoppingList.Categories[0];
        Assert.Equal("Fruit, vegetables and salad", fruitCategory.Name);
        Assert.Equal(3, fruitCategory.Items.Count);
        Assert.Contains("4 apples, small", fruitCategory.Items);
        Assert.Contains("100g avocados", fruitCategory.Items);
        Assert.Contains("2 (215g) bananas, medium", fruitCategory.Items);

        // Check second category
        var herbsCategory = result.ShoppingList.Categories[1];
        Assert.Equal("Fresh herbs and spices", herbsCategory.Name);
        Assert.Equal(2, herbsCategory.Items.Count);
        Assert.Contains("20g chia seeds", herbsCategory.Items);
        Assert.Contains("4 small bunches coriander", herbsCategory.Items);

        // Check third category
        var dairyCategory = result.ShoppingList.Categories[2];
        Assert.Equal("Dairy, eggs and chilled", dairyCategory.Name);
        Assert.Equal(2, dairyCategory.Items.Count);
        Assert.Contains("130g butter (unsalted)", dairyCategory.Items);
        Assert.Contains("40g cheese (cheddar)", dairyCategory.Items);
    }

    [Fact]
    public async Task ParseFileAsync_FileWithRecipesSection_IgnoresRecipes()
    {
        // Arrange
        var content = @"Fruit, vegetables and salad
4 apples, small
100g avocados

Dairy, eggs and chilled
130g butter (unsalted)

Recipes:
2x Banana Pancakes
4x Veggie Curry
1x Apple Pie";

        var testFile = CreateTestFile("test-with-recipes.txt", content);

        // Act
        var result = await _parser.ParseFileAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.ShoppingList.Categories.Count);
        
        // Ensure recipes are not included as a category
        Assert.DoesNotContain(result.ShoppingList.Categories, c => c.Name.Contains("Recipes"));
        Assert.DoesNotContain(result.ShoppingList.Categories, c => c.Items.Any(i => i.Contains("Banana Pancakes")));
    }

    [Fact]
    public async Task ParseFileAsync_EmptyFile_ReturnsEmptyShoppingList()
    {
        // Arrange
        var testFile = CreateTestFile("empty.txt", "");

        // Act
        var result = await _parser.ParseFileAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("empty", result.ShoppingList.Name);
        Assert.Empty(result.ShoppingList.Categories);
    }

    [Fact]
    public async Task ParseFileAsync_SingleCategory_ParsesCorrectly()
    {
        // Arrange
        var content = @"Fruits
apple
banana
orange";

        var testFile = CreateTestFile("single-category.txt", content);

        // Act
        var result = await _parser.ParseFileAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.ShoppingList.Categories);
        
        var category = result.ShoppingList.Categories[0];
        Assert.Equal("Fruits", category.Name);
        Assert.Equal(3, category.Items.Count);
        Assert.Contains("apple", category.Items);
        Assert.Contains("banana", category.Items);
        Assert.Contains("orange", category.Items);
    }

    [Fact]
    public async Task ParseFileAsync_CategoryWithNoItems_IgnoresCategory()
    {
        // Arrange
        var content = @"Fruits
apple
banana

Empty Category

Vegetables
carrot
lettuce";

        var testFile = CreateTestFile("empty-category.txt", content);

        // Act
        var result = await _parser.ParseFileAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.ShoppingList.Categories.Count);
        
        // Should only have Fruits and Vegetables, not Empty Category
        Assert.Equal("Fruits", result.ShoppingList.Categories[0].Name);
        Assert.Equal("Vegetables", result.ShoppingList.Categories[1].Name);
        Assert.DoesNotContain(result.ShoppingList.Categories, c => c.Name == "Empty Category");
    }

    [Fact]
    public async Task ParseFileAsync_FileNameWithSpecialCharacters_CleansName()
    {
        // Arrange
        var content = @"Fruits
apple";

        var testFile = CreateTestFile("test-shopping_list-2024.txt", content);

        // Act
        var result = await _parser.ParseFileAsync(testFile);

        // Assert
        Assert.Equal("test shopping list 2024", result.ShoppingList.Name);
        Assert.Equal("test-shopping_list-2024", result.FileName);
    }

    [Fact]
    public async Task ParseFileAsync_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonexistentFile = new FileInfo(Path.Combine(_tempDirectory, "nonexistent.txt"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FileNotFoundException>(() => _parser.ParseFileAsync(nonexistentFile));
        Assert.Contains("Shopping list file not found", exception.Message);
    }

    [Fact]
    public async Task ParseFileAsync_MultipleConsecutiveBlankLines_ThrowsInvalidOperationException()
    {
        // Arrange
        var content = @"Fruits
apple
banana


Vegetables
carrot";

        var testFile = CreateTestFile("multiple-blanks.txt", content);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _parser.ParseFileAsync(testFile));
        Assert.Contains("Multiple consecutive blank lines found", exception.Message);
    }

    [Fact]
    public async Task ParseFileAsync_MultipleBlankLinesAtEnd_ThrowsInvalidOperationException()
    {
        // Arrange
        var content = @"Fruits
apple
banana


";

        var testFile = CreateTestFile("blanks-at-end.txt", content);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _parser.ParseFileAsync(testFile));
        Assert.Contains("Multiple consecutive blank lines found", exception.Message);
    }

    [Fact]
    public async Task ParseFileAsync_MultipleBlankLinesAtStart_ThrowsInvalidOperationException()
    {
        // Arrange
        var content = @"

Fruits
apple";

        var testFile = CreateTestFile("blanks-at-start.txt", content);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _parser.ParseFileAsync(testFile));
        Assert.Contains("Multiple consecutive blank lines found in shopping list file at line 2.", exception.Message);
    }

    [Fact]
    public async Task ParseFileAsync_SingleBlankLineBetweenCategories_ParsesCorrectly()
    {
        // Arrange
        var content = @"Fruits
apple
banana

Vegetables
carrot
lettuce";

        var testFile = CreateTestFile("single-blank.txt", content);

        // Act
        var result = await _parser.ParseFileAsync(testFile);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.ShoppingList.Categories.Count);
        Assert.Equal("Fruits", result.ShoppingList.Categories[0].Name);
        Assert.Equal("Vegetables", result.ShoppingList.Categories[1].Name);
    }

    private FileInfo CreateTestFile(string fileName, string content)
    {
        var filePath = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(filePath, content);
        return new FileInfo(filePath);
    }
}
