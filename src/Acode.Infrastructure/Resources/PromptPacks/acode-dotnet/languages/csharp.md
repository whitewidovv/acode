# C# Language Guidelines

## Code Style

### Naming Conventions
- **PascalCase**: Classes, Methods, Properties, Events, Namespaces
- **camelCase**: Local variables, parameters
- **_camelCase**: Private fields (with underscore prefix)
- **IPascalCase**: Interfaces (I prefix)
- **TPascalCase**: Generic type parameters (T prefix)

### Modern C# Features (C# 12+)
- **Primary Constructors**: `class Person(string name)`
- **Collection Expressions**: `[1, 2, 3]` instead of `new int[] {1, 2, 3}`
- **Required Members**: `required string Name { get; init; }`
- **File-scoped Namespaces**: `namespace Foo;`
- **Global Usings**: For common namespaces

### Async/Await Patterns

When writing async code:
- **Always use async/await** - Never use `.Result` or `.Wait()` on tasks
- **Pass CancellationToken** through the call chain
- **Use ConfigureAwait(false)** in library code
- **Name async methods with Async suffix** - `GetDataAsync`, `SaveAsync`

```csharp
// Correct async pattern
public async Task<Result> ProcessAsync(int id, CancellationToken cancellationToken)
{
    var data = await _repository.GetByIdAsync(id, cancellationToken)
        .ConfigureAwait(false);
    
    if (data is null)
    {
        return Result.NotFound();
    }
    
    await _service.ProcessAsync(data, cancellationToken)
        .ConfigureAwait(false);
    
    return Result.Success();
}
```

### Common Patterns

```csharp
// Record types for DTOs
public record PersonDto(string Name, int Age);

// Null checking
ArgumentNullException.ThrowIfNull(parameter);

// Pattern matching
if (obj is Person { Age: > 18 } adult)
{
    // Use adult
}

// Primary constructor with field
public class Service(ILogger logger)
{
    private readonly ILogger _logger = logger;
}
```

## Avoid

- Nested ternary operators
- Deep nesting (prefer early returns)
- Magic strings (use constants or nameof)
- Mutable public fields (use properties)
- Blocking async code (.Result, .Wait())
