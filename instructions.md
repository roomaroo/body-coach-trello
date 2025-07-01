# C# Development Best Practices

## General Development Guidelines

### Code Quality
- Follow Microsoft's C# coding conventions and naming guidelines
- Use meaningful variable, method, and class names
- Write clean, readable code with appropriate comments
- Use async/await patterns for I/O operations
- Implement proper error handling with try-catch blocks
- Use dependency injection for better testability and maintainability
- Follow SOLID principles
- Write unit tests for critical business logic

### Project Structure
- Use appropriate folder structure (Models, Services, Controllers, etc.)
- Separate concerns into different layers (Data Access, Business Logic, Presentation)
- Use configuration files (appsettings.json) for environment-specific settings
- Implement logging using Microsoft.Extensions.Logging or similar frameworks

### Performance
- Use ConfigureAwait(false) for library code
- Dispose of resources properly using `using` statements or IDisposable pattern
- Use efficient data structures and algorithms
- Consider memory usage and garbage collection impact

## Git Workflow

### Branch Management
- **All work should be done on a branch. PRs must be created to merge this work into main.**
- Use descriptive branch names (e.g., `feature/trello-integration`, `bugfix/parsing-issue`)
- Keep branches focused on a single feature or bug fix
- Regularly sync your branch with main to avoid conflicts
- Delete feature branches after successful merge

### Commit Guidelines
- Write clear, descriptive commit messages
- Make atomic commits (one logical change per commit)
- Use conventional commit format when possible:
  - `feat:` for new features
  - `fix:` for bug fixes
  - `docs:` for documentation changes
  - `refactor:` for code refactoring

## Security and Credentials

### Credential Management
- **Don't store any credentials in files that could be committed to the git repo. Take active steps to avoid this happening.**
- Use environment variables for sensitive data
- Utilize .NET's User Secrets for local development
- Add sensitive files to .gitignore immediately
- Use Azure Key Vault or similar services for production deployments
- Never hardcode API keys, connection strings, or passwords in source code

### Security Best Practices
- Validate and sanitize all input data
- Use HTTPS for all external API calls
- Implement proper authentication and authorization
- Keep dependencies up to date to avoid security vulnerabilities
- Use secure random number generation for sensitive operations

## .NET 9 Specific Guidelines

### Modern C# Features
- Use nullable reference types to prevent null reference exceptions
- Leverage pattern matching and switch expressions
- Use record types for immutable data models
- Take advantage of minimal APIs for simple web applications
- Use global using statements to reduce repetitive using declarations

### Performance Improvements
- Utilize new performance features in .NET 9
- Use System.Text.Json for JSON serialization/deserialization
- Consider using Span<T> and Memory<T> for high-performance scenarios

## Testing

### Unit Testing
- Write tests using xUnit, NUnit, or MSTest
- Use mocking frameworks like Moq for dependencies
- Aim for high code coverage but focus on critical paths
- Use Test-Driven Development (TDD) when appropriate

### Integration Testing
- Test API integrations with external services
- Use TestContainers for database testing
- Mock external dependencies in integration tests

## Documentation

### Code Documentation
- Use XML documentation comments for public APIs
- Maintain up-to-date README files
- Document complex algorithms and business logic
- Include examples in documentation

### API Documentation
- Document all public methods and classes
- Include parameter descriptions and return value information
- Provide usage examples for complex APIs

## Continuous Integration/Deployment

### Build Pipeline
- Set up automated builds on pull requests
- Include code quality checks (linting, formatting)
- Run all tests in the CI pipeline
- Use semantic versioning for releases

### Deployment
- Use configuration management for different environments
- Implement health checks for deployed applications
- Monitor application performance and errors
- Use infrastructure as code when possible
