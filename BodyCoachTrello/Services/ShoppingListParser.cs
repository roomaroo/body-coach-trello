using BodyCoachTrello.Models;
using Microsoft.Extensions.Logging;

namespace BodyCoachTrello.Services;

/// <summary>
/// Service for parsing shopping list files
/// </summary>
public interface IShoppingListParser
{
    /// <summary>
    /// Parse a shopping list file
    /// </summary>
    /// <param name="fileInfo">FileInfo for the shopping list file</param>
    /// <returns>Parsed shopping list</returns>
    Task<ParsedShoppingList> ParseFileAsync(FileInfo fileInfo);
}

/// <summary>
/// Implementation of shopping list parser
/// </summary>
public class ShoppingListParser : IShoppingListParser
{
    private readonly ILogger<ShoppingListParser> _logger;

    public ShoppingListParser(ILogger<ShoppingListParser> logger)
    {
        _logger = logger;
    }

    public async Task<ParsedShoppingList> ParseFileAsync(FileInfo fileInfo)
    {
        if (!fileInfo.Exists)
        {
            throw new FileNotFoundException($"Shopping list file not found: {fileInfo.FullName}");
        }

        _logger.LogInformation("Parsing shopping list file: {FilePath}", fileInfo.FullName);

        var lines = await File.ReadAllLinesAsync(fileInfo.FullName);
        this.EnsureNoMultipleBlankLines(lines);

        var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
        
        var shoppingList = new ShoppingList
        {
            Name = fileName.Replace('-', ' ').Replace('_', ' ')
        };

        var currentCategory = new ShoppingCategory();
        var inRecipesSection = false;
        var nextLineIsCategory = true; // First line is always a category

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmedLine = lines[i].Trim();

            // Check if we've reached the recipes section
            if (trimmedLine.StartsWith("Recipes:", StringComparison.OrdinalIgnoreCase))
            {
                inRecipesSection = true;
                continue;
            }

            // Skip recipe lines
            if (inRecipesSection)
            {
                continue;
            }

            // Check if this line is a category (first line or preceded by blank line)
            if (nextLineIsCategory)
            {
                // Save the previous category if it has items
                if (!string.IsNullOrWhiteSpace(currentCategory.Name) && currentCategory.Items.Count > 0)
                {
                    shoppingList.Categories.Add(currentCategory);
                }

                // Start a new category
                currentCategory = new ShoppingCategory
                {
                    Name = trimmedLine
                };

                nextLineIsCategory = false; // Next line will be an item
            }
            else if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                // Blank line - next line is a category
                nextLineIsCategory = true;
            }
            else if (!string.IsNullOrWhiteSpace(currentCategory.Name))
            {
                // This is an item line
                currentCategory.Items.Add(trimmedLine);
                nextLineIsCategory = false;
            }
        }

        // Add the last category if it has items
        if (!string.IsNullOrWhiteSpace(currentCategory.Name) && currentCategory.Items.Count > 0)
        {
            shoppingList.Categories.Add(currentCategory);
        }

        _logger.LogInformation("Parsed {CategoryCount} categories with {ItemCount} total items",
            shoppingList.Categories.Count,
            shoppingList.Categories.Sum(c => c.Items.Count));

        return new ParsedShoppingList
        {
            FileName = fileName,
            ShoppingList = shoppingList
        };
    }

    private void EnsureNoMultipleBlankLines(string[] lines)
    {
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]) && string.IsNullOrWhiteSpace(lines[i - 1]))
            {
                throw new InvalidOperationException($"Multiple consecutive blank lines found in shopping list file at line {i + 1}.");
            }
        }
    }
}
