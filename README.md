# Shopping List to Trello Importer

A C# console application that imports shopping lists into Trello boards using the Trello API.

## Features

- Parses shopping lists organized into categories
- Adds categories as lists to an existing Trello board or creates a new one
- Creates cards for each shopping item
- Secure credential management
- Command line interface with proper argument parsing

## Prerequisites

- .NET 9.0
- A Trello account
- Trello API Key and Token

## Setup

1. **Get Trello API Credentials:**
   - Visit [Trello Power-Ups Admin](https://trello.com/power-ups/admin)
   - Create a new Power-Up to get your API Key
   - Generate a token by clicking the "Token" link next to your API key

2. **Configure User Secrets:**
   ```cmd
   dotnet user-secrets init
   dotnet user-secrets set "Trello:ApiKey" "your-api-key-here"
   dotnet user-secrets set "Trello:Token" "your-token-here"
   ```

3. **Build and run:**
   ```cmd
   dotnet build
   dotnet run -- "path\to\shopping-list.txt"
   ```

## Usage

### Command Line Syntax

```cmd
BodyCoachTrello [options] <file>

Arguments:
  file                  Path to the shopping list file to import

Options:
  -b, --board <board>   Name of the Trello board to add lists to (defaults to value in appsettings.json)
  -v, --verbose         Show verbose output
  --help                Display help screen
```

### Examples

```cmd
# Import to default board
dotnet run -- samples\shopping-list.txt

# Import to specific board
dotnet run -- samples\shopping-list.txt --board "My Weekly Shopping"

# Show verbose output
dotnet run -- samples\shopping-list.txt --verbose
```

### File Format

Shopping list files should be formatted with:
- Category names on their own lines
- Items listed under each category
- Blank lines separating categories
- The "Recipes:" section is automatically ignored

Example format:
```
Fruit, vegetables and salad
4 apples, small
100g avocados

Dairy, eggs and chilled
130g butter (unsalted)
40g cheese (cheddar)

Recipes:
2x Banana Pancakes
4x Veggie Curry
```

## Configuration

The application uses the following configuration sources:
1. User Secrets (for development)
2. Environment Variables (for production)
3. appsettings.json (for default settings)

## Security

- API credentials are never stored in source code
- User Secrets are used for local development
- Environment variables should be used in production

## Architecture

- `Models/`: Data models for shopping lists and Trello entities
- `Services/`: Business logic and API interactions
- `Configuration/`: Configuration classes
- `Program.cs`: Entry point and DI setup
