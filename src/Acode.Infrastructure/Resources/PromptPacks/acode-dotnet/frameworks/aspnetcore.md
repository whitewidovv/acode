# ASP.NET Core Framework Guidelines

## Controller Patterns

Use async actions with ActionResult<T>:

**✅ CORRECT**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderAsync(id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _orderService.CreateOrderAsync(request);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
}
```

## Model Validation

Use Data Annotations and validate in controller:

```csharp
public class CreateOrderRequest
{
    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Range(0.01, 1000000)]
    public decimal Total { get; set; }
}

[HttpPost]
public async Task<ActionResult<Order>> CreateOrder(CreateOrderRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // Additional business validation
    if (await _orderService.CustomerExistsAsync(request.Email))
        return Conflict("Customer already has an active order");

    var order = await _orderService.CreateAsync(request);
    return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
}
```

## Dependency Injection Registration

Register services with appropriate lifetimes:

```csharp
// Program.cs or Startup.cs
builder.Services.AddScoped<IOrderService, OrderService>();      // Per-request
builder.Services.AddSingleton<IConfiguration>(configuration);    // App lifetime
builder.Services.AddTransient<IEmailSender, EmailSender>();      // Per-use

// DbContext - always scoped
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

## Middleware and Filters

Use middleware for cross-cutting concerns:

```csharp
// Exception handling middleware
app.UseExceptionHandler("/error");

// Custom middleware
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);

        await _next(context);

        _logger.LogInformation("Response: {StatusCode}", context.Response.StatusCode);
    }
}
```

## Entity Framework Core Patterns

Avoid N+1 queries with eager loading:

**✅ CORRECT**:
```csharp
public async Task<List<Order>> GetOrdersWithItemsAsync()
{
    return await _context.Orders
        .Include(o => o.Items)
        .Where(o => o.Status == OrderStatus.Active)
        .ToListAsync();
}
```

**❌ WRONG - N+1 query**:
```csharp
public async Task<List<Order>> GetOrdersWithItemsAsync()
{
    var orders = await _context.Orders
        .Where(o => o.Status == OrderStatus.Active)
        .ToListAsync();

    // This triggers a separate query for each order
    foreach (var order in orders)
    {
        var items = await _context.OrderItems
            .Where(i => i.OrderId == order.Id)
            .ToListAsync();
    }

    return orders;
}
```

Use AsNoTracking for read-only queries:

```csharp
public async Task<List<OrderDto>> GetOrdersAsync()
{
    return await _context.Orders
        .AsNoTracking()
        .Select(o => new OrderDto
        {
            Id = o.Id,
            CustomerName = o.CustomerName,
            Total = o.Total
        })
        .ToListAsync();
}
```

## Configuration

Use strongly-typed configuration:

```csharp
// appsettings.json
{
  "EmailSettings": {
    "SmtpServer": "smtp.example.com",
    "Port": 587
  }
}

// EmailSettings.cs
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
}

// Program.cs
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Usage in service
public class EmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }
}
```

## Minimal APIs (Simplified Pattern)

For simple endpoints, use minimal APIs:

```csharp
app.MapGet("/api/orders/{id}", async (int id, IOrderService orderService) =>
{
    var order = await orderService.GetOrderAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.MapPost("/api/orders", async (CreateOrderRequest request, IOrderService orderService) =>
{
    var order = await orderService.CreateOrderAsync(request);
    return Results.Created($"/api/orders/{order.Id}", order);
});
```

## Logging

Use structured logging with ILogger:

```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        _logger.LogInformation(
            "Creating order for customer {CustomerName} with total {Total:C}",
            request.CustomerName,
            request.Total);

        try
        {
            var order = await _repository.CreateAsync(request);

            _logger.LogInformation("Order {OrderId} created successfully", order.Id);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for customer {CustomerName}", request.CustomerName);
            throw;
        }
    }
}
```
