# ASP.NET Core Framework Guidelines

## Project Structure

```
src/
├── Domain/           # Entities, ValueObjects, DomainServices
├── Application/      # Use Cases, DTOs, Interfaces
├── Infrastructure/   # Data Access, External Services
└── Api/             # Controllers, Middleware, Startup
```

## Common Patterns

### Dependency Injection
```csharp
// Registration
services.AddScoped<IService, Service>();
services.AddSingleton<ICache, MemoryCache>();

// Usage (constructor injection)
public class Controller(IService service)
{
    public IActionResult Get() => Ok(service.GetData());
}
```

### Controller Actions
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<ItemDto>> GetById(int id)
{
    var item = await _service.GetByIdAsync(id);
    return item is null ? NotFound() : Ok(item);
}
```

### Validation
```csharp
// Use FluentValidation or DataAnnotations
public class CreateItemCommand
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;
}
```

### Error Handling
```csharp
// Global exception handler middleware
app.UseExceptionHandler("/error");

// Problem Details for API errors
return Problem(
    statusCode: 400,
    title: "Validation Error",
    detail: "Name is required");
```

## Configuration

```csharp
// Options pattern
services.Configure<MyOptions>(configuration.GetSection("My"));

// Usage
public class Service(IOptions<MyOptions> options)
{
    private readonly MyOptions _options = options.Value;
}
```

## Testing

```csharp
// WebApplicationFactory for integration tests
public class ApiTests(WebApplicationFactory<Program> factory) 
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/items");
        response.EnsureSuccessStatusCode();
    }
}
```
