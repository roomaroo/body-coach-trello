namespace BodyCoachTrello.Models;

/// <summary>
/// Represents a shopping list with categories and items
/// </summary>
public class ShoppingList
{
    public string Name { get; set; } = string.Empty;
    public List<ShoppingCategory> Categories { get; set; } = [];
}

/// <summary>
/// Represents a category within a shopping list
/// </summary>
public class ShoppingCategory
{
    public string Name { get; set; } = string.Empty;
    public List<string> Items { get; set; } = [];
}

/// <summary>
/// Represents a parsed shopping list file
/// </summary>
public class ParsedShoppingList
{
    public string FileName { get; set; } = string.Empty;
    public ShoppingList ShoppingList { get; set; } = new();
}
