using BodyCoachTrello.Services;
using Microsoft.Extensions.Logging;

var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ShoppingListParser>();
var parser = new ShoppingListParser(logger);

var testFile = new FileInfo("debug-test.txt");
try
{
    var result = await parser.ParseFileAsync(testFile);
    Console.WriteLine("File parsed successfully!");
    Console.WriteLine($"Categories: {result.ShoppingList.Categories.Count}");
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}
