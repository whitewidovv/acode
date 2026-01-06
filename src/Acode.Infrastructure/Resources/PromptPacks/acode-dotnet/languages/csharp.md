# C# Language Guidelines

## Naming Conventions

Follow .NET naming conventions strictly:

- **PascalCase**: Classes, methods, properties, events, namespaces
  ```csharp
  public class OrderProcessor { }
  public void ProcessOrder() { }
  public string CustomerName { get; set; }
  ```

- **camelCase**: Local variables, parameters, private fields (with _ prefix)
  ```csharp
  private readonly ILogger _logger;
  public void Process(string orderNumber)
  {
      var processingTime = DateTime.UtcNow;
  }
  ```

- **IPascalCase**: Interfaces start with 'I'
  ```csharp
  public interface IOrderService { }
  ```

## Async/Await Patterns

**✅ CORRECT - Async all the way**:
```csharp
public async Task<Order> GetOrderAsync(int orderId)
{
    var order = await _repository.GetAsync(orderId);
    return order;
}
```

**❌ WRONG - Blocking on async (causes deadlocks)**:
```csharp
public Order GetOrder(int orderId)
{
    var order = _repository.GetAsync(orderId).Result; // DEADLOCK RISK
    return order;
}
```

**✅ CORRECT - ConfigureAwait(false) in library code**:
```csharp
public async Task ProcessAsync()
{
    await SaveDataAsync().ConfigureAwait(false);
}
```

## Nullable Reference Types

Enable nullable reference types and handle nullability explicitly:

```csharp
#nullable enable

public class OrderService
{
    // Non-nullable property - must be initialized
    public string OrderId { get; set; } = string.Empty;

    // Nullable property - can be null
    public string? Notes { get; set; }

    // Parameter validation
    public void Process(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Items.Count == 0)
            throw new ArgumentException("Order must have items", nameof(order));
    }
}
```

## Dependency Injection

Use constructor injection, not property injection:

**✅ CORRECT**:
```csharp
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IOrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }
}
```

**❌ WRONG**:
```csharp
public class OrderController : ControllerBase
{
    [Inject]
    public IOrderService OrderService { get; set; } // Don't use property injection
}
```

## LINQ and Collections

Prefer LINQ for readability, but be aware of performance:

```csharp
// Good - single enumeration
var activeOrders = orders
    .Where(o => o.Status == OrderStatus.Active)
    .ToList();

// Avoid - multiple enumerations
var count = orders.Where(o => o.IsActive).Count(); // Don't do this
var first = orders.Where(o => o.IsActive).First(); // And this - use Count() and First() directly

// Better
var activeOrders = orders.Where(o => o.IsActive).ToList();
var count = activeOrders.Count;
var first = activeOrders.First();
```

## IDisposable Pattern

Implement IDisposable correctly:

```csharp
public class OrderProcessor : IDisposable
{
    private readonly HttpClient _httpClient = new HttpClient();
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _httpClient?.Dispose();
        }

        _disposed = true;
    }
}
```

Or use 'using' declarations:

```csharp
public async Task ProcessAsync()
{
    using var client = new HttpClient();
    await client.GetAsync("https://api.example.com");
}
```

## Exception Handling

Be specific with exceptions:

```csharp
// Good - specific exceptions
if (order == null)
    throw new ArgumentNullException(nameof(order));

if (order.Total < 0)
    throw new ArgumentOutOfRangeException(nameof(order.Total), "Total must be non-negative");

// Avoid catching generic Exception unless re-throwing
try
{
    await ProcessOrderAsync(order);
}
catch (OrderValidationException ex)
{
    _logger.LogWarning(ex, "Validation failed for order {OrderId}", order.Id);
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error processing order {OrderId}", order.Id);
    throw; // Re-throw to preserve stack trace
}
```
