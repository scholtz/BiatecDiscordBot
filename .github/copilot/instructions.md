# Copilot Instructions for Biatec Discord Bot

## Project Overview

This is a .NET 10 Web API project that implements a Discord bot for user engagement. It uses Discord.Net for bot operations, Entity Framework Core for data persistence, and ASP.NET Core for the REST API.

## Architecture Conventions

### Project Structure
- **Models/** - Entity classes and DTOs. Entities are suffixed with `Entity` when they might conflict with common names (e.g., `DiscordUserEntity`).
- **Models/DTOs/** - Data transfer objects for API requests/responses. Suffixed with `Request`, `Response`, or `Dto`.
- **Data/** - Entity Framework Core `DbContext` and database configuration.
- **Services/** - Business logic layer. Each service has an interface (`I*Service`) and implementation.
- **Controllers/** - ASP.NET Core API controllers. Use `[ApiController]` and `[Route("api/[controller]")]` attributes.

### Coding Conventions
- Use **file-scoped namespaces** (`namespace X;`)
- Use **nullable reference types** (project-wide enabled)
- Use **primary constructors** where appropriate
- Prefer **async/await** for all I/O operations
- Use **ILogger<T>** for logging (no direct Console.Write)
- Use **dependency injection** for all service dependencies
- Use **IOptions<T>** pattern for configuration settings

### Entity Framework Conventions
- Use **InMemory database** for local development (`UseInMemoryDatabase: true`)
- Use **PostgreSQL** (Npgsql) for production
- Define relationships in `OnModelCreating` using Fluent API
- Use `AsNoTracking()` for read-only queries
- Use **pagination** (Skip/Take) for list endpoints

### Testing Conventions
- Use **xUnit** as the test framework
- Use **Moq** for mocking interfaces
- Use **InMemory EF Core** database for service integration tests
- Test file naming: `{ClassName}Tests.cs`
- Test method naming: `{Method}_{Scenario}_{Expected}`
- Each test follows **Arrange-Act-Assert** pattern

### API Conventions
- Return appropriate HTTP status codes (200, 201, 400, 404, 204)
- Use `[ProducesResponseType]` attributes for Swagger documentation
- Validate inputs at the controller level
- Return anonymous objects for simple responses (`new { Success = true }`)

### Discord Bot Conventions
- The bot runs as a hosted service (`DiscordBotHostedService`)
- Bot operations are abstracted behind `IDiscordBotService` for testability
- All message tracking goes through `IMessageTrackingService`
- Questions use Discord embeds with button components for multiple choice

## Key Dependencies
- **Discord.Net** (3.x) - Discord API wrapper
- **Microsoft.EntityFrameworkCore** (10.x) - ORM
- **Npgsql.EntityFrameworkCore.PostgreSQL** (10.x) - PostgreSQL provider
- **Swashbuckle.AspNetCore** (10.x) - Swagger/OpenAPI

## Common Tasks

### Adding a New Entity
1. Create the entity class in `Models/`
2. Add a `DbSet<T>` property to `BotDbContext`
3. Configure relationships in `OnModelCreating`
4. Create corresponding DTO in `Models/DTOs/` if needed

### Adding a New Service
1. Create the interface in `Services/I{Name}Service.cs`
2. Create the implementation in `Services/{Name}Service.cs`
3. Register in `Program.cs` with appropriate lifetime (Scoped for DB-dependent, Singleton for stateful)

### Adding a New Controller
1. Create the controller in `Controllers/{Name}Controller.cs`
2. Use `[ApiController]` and `[Route("api/[controller]")]`
3. Inject services via constructor
4. Add XML documentation comments for Swagger
5. Add corresponding tests in `BiatecDiscordBot.Tests/Controllers/`

### Adding Tests
1. Create test class in the appropriate subfolder under `BiatecDiscordBot.Tests/`
2. Use `TestDbContextFactory.Create()` for database-dependent tests
3. Use `Mock<T>` for interface dependencies
4. Follow AAA pattern (Arrange-Act-Assert)
