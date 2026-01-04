# Task 031.c: EC2 Cost Controls

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 031 (EC2 Target), Task 031.b (Management)  

---

## Description

Task 031.c implements EC2 cost controls. Spending MUST be tracked. Limits MUST be enforced. Alerts MUST notify users.

Cloud costs can escalate quickly. Cost controls prevent unexpected bills. Users MUST have visibility and control.

This task covers cost tracking, limits, and alerts. Orphan cleanup also reduces costs.

### Business Value

Cost controls provide:
- Budget protection
- Spending visibility
- Predictable costs
- Financial accountability

### Scope Boundaries

This task covers EC2 cost controls. Instance management is in 031.b. Burst heuristics are in Task 033.

### Integration Points

- Task 031.b: Tracks running instances
- Task 033: Heuristics consider cost
- Task 009: Config stores limits

### Failure Modes

- Cost API unavailable → Use estimates
- Limit exceeded → Block new instances
- Alert fails → Log warning
- Orphan found → Auto-terminate

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Hourly Rate | Cost per hour |
| Running Cost | Accumulated spend |
| Budget | Maximum allowed spend |
| Alert | Notification at threshold |
| Estimate | Projected cost |
| Orphan | Instance without owner |

---

## Out of Scope

- AWS Cost Explorer integration
- Reserved instance purchasing
- Savings Plan enrollment
- Detailed billing reports
- Cost allocation tags (beyond acode)
- Multi-account cost aggregation

---

## Functional Requirements

### FR-001 to FR-020: Cost Tracking

- FR-001: Hourly rate MUST be known
- FR-002: Rate from pricing API
- FR-003: Rate from static table fallback
- FR-004: Rate table MUST be updatable
- FR-005: Rate includes instance type
- FR-006: Rate includes region
- FR-007: Rate includes spot vs on-demand
- FR-008: Running time MUST be tracked
- FR-009: Start time from launch
- FR-010: End time from terminate
- FR-011: Running cost MUST calculate
- FR-012: Cost = hours × rate
- FR-013: Partial hours rounded up
- FR-014: Total cost MUST accumulate
- FR-015: Cost per session MUST be tracked
- FR-016: Cost history MUST persist
- FR-017: Cost MUST be queryable
- FR-018: Query by session, date, total
- FR-019: Cost MUST include EBS
- FR-020: EBS rate per GB-month

### FR-021 to FR-040: Budget Limits

- FR-021: Budget MUST be configurable
- FR-022: Budget per session MUST work
- FR-023: Budget per day MUST work
- FR-024: Budget per month MUST work
- FR-025: Default: no limit
- FR-026: Limit reached MUST block
- FR-027: Block new instance launch
- FR-028: Running instances MAY continue
- FR-029: Hard limit MUST terminate
- FR-030: Hard limit terminates running
- FR-031: Soft limit MUST alert only
- FR-032: Limit type configurable
- FR-033: Default: soft limit
- FR-034: Limit check before launch
- FR-035: Estimated cost MUST be checked
- FR-036: Estimate includes max runtime
- FR-037: Max runtime configurable
- FR-038: Default max runtime: 4 hours
- FR-039: Limit bypass MUST require flag
- FR-040: --allow-over-budget flag

### FR-041 to FR-060: Alerts

- FR-041: Alert thresholds MUST be configurable
- FR-042: Default thresholds: 50%, 80%, 100%
- FR-043: Alert at threshold reached
- FR-044: Alert via callback
- FR-045: Alert via log warning
- FR-046: Alert via webhook optional
- FR-047: Webhook URL configurable
- FR-048: Alert content MUST include details
- FR-049: Details: current cost, limit, percent
- FR-050: Details: running instances
- FR-051: Details: estimated remaining
- FR-052: Alert MUST not repeat
- FR-053: Alert once per threshold
- FR-054: Reset on new period
- FR-055: Daily reset at midnight UTC
- FR-056: Monthly reset at month start
- FR-057: Alert history MUST be logged
- FR-058: CLI MUST show alerts
- FR-059: `acode cost alerts` command
- FR-060: Alert suppression MUST be optional

### FR-061 to FR-075: Cost Visibility

- FR-061: Current cost MUST be queryable
- FR-062: CLI command: `acode cost show`
- FR-063: Show current session cost
- FR-064: Show daily cost
- FR-065: Show monthly cost
- FR-066: Show running instances
- FR-067: Show hourly burn rate
- FR-068: Estimate MUST be available
- FR-069: Estimate to session end
- FR-070: Estimate to max runtime
- FR-071: Cost breakdown MUST work
- FR-072: Breakdown by instance type
- FR-073: Breakdown by task
- FR-074: Breakdown by day
- FR-075: Export to CSV MUST work

---

## Non-Functional Requirements

- NFR-001: Cost calculation in <100ms
- NFR-002: Cost accuracy ±5%
- NFR-003: Alert latency <1 minute
- NFR-004: Cost history retained 90 days
- NFR-005: No orphan cost accumulation
- NFR-006: Structured logging
- NFR-007: Metrics on spending
- NFR-008: Audit trail
- NFR-009: Timezone handling
- NFR-010: Currency: USD only

---

## User Manual Documentation

### Configuration

```yaml
ec2Cost:
  budget:
    perSession: 10.00
    perDay: 50.00
    perMonth: 500.00
    limitType: soft  # soft | hard
  alerts:
    thresholds: [50, 80, 100]
    webhookUrl: https://hooks.example.com/cost
  maxRuntimeHours: 4
  trackingEnabled: true
```

### CLI Commands

```bash
# Show current costs
acode cost show

# Show with breakdown
acode cost show --breakdown

# Show history
acode cost history --days 7

# Export to CSV
acode cost export --format csv --output costs.csv

# Show alerts
acode cost alerts
```

### Cost Display

```
Current Session: $2.34 (1h 23m on c5.large)
Today:          $15.67 / $50.00 (31%)
This Month:     $127.89 / $500.00 (26%)

Running Instances:
  i-1234567890 (c5.large)  $0.085/hr  1h 23m  $2.34

Estimated Session Total: $4.68 (at 4h max)
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Hourly rate tracked
- [ ] AC-002: Running cost calculated
- [ ] AC-003: Session cost tracked
- [ ] AC-004: Budget limits work
- [ ] AC-005: Hard limit terminates
- [ ] AC-006: Alerts trigger
- [ ] AC-007: CLI shows costs
- [ ] AC-008: History persists
- [ ] AC-009: Export works
- [ ] AC-010: Orphan cost prevented

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Cost calculation
- [ ] UT-002: Budget checking
- [ ] UT-003: Alert thresholds
- [ ] UT-004: Time rounding

### Integration Tests

- [ ] IT-001: Real cost tracking
- [ ] IT-002: Budget enforcement
- [ ] IT-003: Alert webhook
- [ ] IT-004: CLI commands

---

## Implementation Prompt

### Part 1: File Structure and Domain Models

**Target Directory Structure:**
```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Ec2/
│           └── Cost/
│               ├── BudgetPeriod.cs
│               ├── BudgetViolation.cs
│               ├── CostEstimate.cs
│               └── Events/
│                   ├── CostThresholdReachedEvent.cs
│                   ├── BudgetExceededEvent.cs
│                   └── InstanceCostRecordedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Ec2/
│           └── Cost/
│               ├── IEc2CostTracker.cs
│               ├── IEc2BudgetChecker.cs
│               ├── IEc2CostAlertService.cs
│               ├── CostCheckRequest.cs
│               └── InstanceUsageRecord.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Ec2/
            └── Cost/
                ├── Ec2CostTracker.cs
                ├── Ec2BudgetChecker.cs
                ├── Ec2CostAlertService.cs
                ├── Ec2PricingTable.cs
                └── CostHistoryRepository.cs
```

**Domain Models:**

```csharp
// src/Acode.Domain/Compute/Ec2/Cost/BudgetPeriod.cs
namespace Acode.Domain.Compute.Ec2.Cost;

public enum BudgetPeriod { Session, Daily, Monthly }

// src/Acode.Domain/Compute/Ec2/Cost/BudgetViolation.cs
namespace Acode.Domain.Compute.Ec2.Cost;

public sealed record BudgetViolation
{
    public required BudgetPeriod Period { get; init; }
    public required decimal CurrentSpend { get; init; }
    public required decimal Limit { get; init; }
    public decimal Overage => CurrentSpend - Limit;
    public double PercentOver => Limit > 0 ? (double)Overage / (double)Limit * 100 : 0;
}

// src/Acode.Domain/Compute/Ec2/Cost/CostEstimate.cs
namespace Acode.Domain.Compute.Ec2.Cost;

public sealed record CostEstimate
{
    public required decimal EstimatedCost { get; init; }
    public required decimal HourlyRate { get; init; }
    public required TimeSpan Duration { get; init; }
    public bool ExceedsBudget { get; init; }
    public decimal? BudgetRemaining { get; init; }
    public decimal? TotalBudget { get; init; }
}

// src/Acode.Domain/Compute/Ec2/Cost/Events/CostThresholdReachedEvent.cs
namespace Acode.Domain.Compute.Ec2.Cost.Events;

public sealed record CostThresholdReachedEvent(
    string SessionId,
    int ThresholdPercent,
    decimal CurrentCost,
    decimal BudgetLimit,
    BudgetPeriod Period,
    DateTimeOffset ReachedAt);

// src/Acode.Domain/Compute/Ec2/Cost/Events/BudgetExceededEvent.cs
namespace Acode.Domain.Compute.Ec2.Cost.Events;

public sealed record BudgetExceededEvent(
    string SessionId,
    BudgetViolation Violation,
    bool WillTerminate,
    DateTimeOffset ExceededAt);

// src/Acode.Domain/Compute/Ec2/Cost/Events/InstanceCostRecordedEvent.cs
namespace Acode.Domain.Compute.Ec2.Cost.Events;

public sealed record InstanceCostRecordedEvent(
    string InstanceId,
    string InstanceType,
    TimeSpan Runtime,
    decimal Cost,
    DateTimeOffset RecordedAt);
```

**End of Task 031.c Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Ec2/Cost/CostCheckRequest.cs
namespace Acode.Application.Compute.Ec2.Cost;

public sealed record CostCheckRequest
{
    public required string InstanceType { get; init; }
    public required string Region { get; init; }
    public bool IsSpot { get; init; }
    public required TimeSpan EstimatedDuration { get; init; }
    public decimal? OverrideHourlyRate { get; init; }
}

// src/Acode.Application/Compute/Ec2/Cost/InstanceUsageRecord.cs
namespace Acode.Application.Compute.Ec2.Cost;

public sealed record InstanceUsageRecord
{
    public required string InstanceId { get; init; }
    public required string InstanceType { get; init; }
    public required string Region { get; init; }
    public bool IsSpot { get; init; }
    public required DateTimeOffset StartTime { get; init; }
    public DateTimeOffset? EndTime { get; init; }
    public required decimal HourlyRate { get; init; }
    public string? SessionId { get; init; }
    
    public TimeSpan Runtime => (EndTime ?? DateTimeOffset.UtcNow) - StartTime;
    public decimal Cost => Math.Ceiling((decimal)Runtime.TotalHours) * HourlyRate;
}

// src/Acode.Application/Compute/Ec2/Cost/IEc2CostTracker.cs
namespace Acode.Application.Compute.Ec2.Cost;

public interface IEc2CostTracker
{
    Task<decimal> GetCurrentSessionCostAsync(
        string sessionId,
        CancellationToken ct = default);
    
    Task<decimal> GetDailyCostAsync(
        DateOnly date,
        CancellationToken ct = default);
    
    Task<decimal> GetMonthlyCostAsync(
        int year,
        int month,
        CancellationToken ct = default);
    
    Task<CostEstimate> EstimateSessionCostAsync(
        string sessionId,
        TimeSpan maxRuntime,
        CancellationToken ct = default);
    
    Task RecordInstanceUsageAsync(
        InstanceUsageRecord usage,
        CancellationToken ct = default);
    
    Task<decimal> GetHourlyRateAsync(
        string instanceType,
        string region,
        bool isSpot,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ec2/Cost/IEc2BudgetChecker.cs
namespace Acode.Application.Compute.Ec2.Cost;

public interface IEc2BudgetChecker
{
    Task<BudgetCheckResult> CheckAsync(
        CostCheckRequest request,
        CancellationToken ct = default);
    
    Task<bool> EnforceLimitsAsync(
        string sessionId,
        CancellationToken ct = default);
    
    Task<BudgetStatus> GetBudgetStatusAsync(
        CancellationToken ct = default);
}

public sealed record BudgetCheckResult
{
    public bool Allowed { get; init; }
    public BudgetViolation? Violation { get; init; }
    public decimal CurrentSpend { get; init; }
    public decimal? BudgetLimit { get; init; }
    public string? Message { get; init; }
}

public sealed record BudgetStatus
{
    public decimal SessionSpend { get; init; }
    public decimal? SessionLimit { get; init; }
    public decimal DailySpend { get; init; }
    public decimal? DailyLimit { get; init; }
    public decimal MonthlySpend { get; init; }
    public decimal? MonthlyLimit { get; init; }
}

// src/Acode.Application/Compute/Ec2/Cost/IEc2CostAlertService.cs
namespace Acode.Application.Compute.Ec2.Cost;

public interface IEc2CostAlertService
{
    void SetThresholds(IReadOnlyList<int> thresholdPercents);
    Task CheckAndAlertAsync(string sessionId, CancellationToken ct = default);
    Task SendWebhookAlertAsync(CostThresholdReachedEvent alert, CancellationToken ct = default);
    IReadOnlyList<CostThresholdReachedEvent> GetAlertHistory(string? sessionId = null);
}
```

**End of Task 031.c Specification - Part 2/3**

### Part 3: Infrastructure Implementation and Checklist

```csharp
// src/Acode.Infrastructure/Compute/Ec2/Cost/Ec2CostTracker.cs
namespace Acode.Infrastructure.Compute.Ec2.Cost;

public sealed class Ec2CostTracker : IEc2CostTracker
{
    private readonly IEc2PricingTable _pricing;
    private readonly ICostHistoryRepository _history;
    private readonly IEventPublisher _events;
    private readonly ILogger<Ec2CostTracker> _logger;
    
    public async Task<decimal> GetHourlyRateAsync(
        string instanceType,
        string region,
        bool isSpot,
        CancellationToken ct = default)
    {
        return await _pricing.GetHourlyRateAsync(instanceType, region, isSpot, ct);
    }
    
    public async Task<decimal> GetCurrentSessionCostAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        var records = await _history.GetBySessionAsync(sessionId, ct);
        return records.Sum(r => r.Cost);
    }
    
    public async Task<decimal> GetDailyCostAsync(
        DateOnly date,
        CancellationToken ct = default)
    {
        var records = await _history.GetByDateAsync(date, ct);
        return records.Sum(r => r.Cost);
    }
    
    public async Task<CostEstimate> EstimateSessionCostAsync(
        string sessionId,
        TimeSpan maxRuntime,
        CancellationToken ct = default)
    {
        var records = await _history.GetBySessionAsync(sessionId, ct);
        var running = records.Where(r => r.EndTime == null).ToList();
        
        var estimatedCost = running.Sum(r =>
        {
            var remainingTime = maxRuntime - r.Runtime;
            return r.Cost + (remainingTime.TotalHours > 0 
                ? (decimal)Math.Ceiling(remainingTime.TotalHours) * r.HourlyRate 
                : 0);
        });
        
        var avgRate = running.Average(r => r.HourlyRate);
        
        return new CostEstimate
        {
            EstimatedCost = estimatedCost,
            HourlyRate = avgRate,
            Duration = maxRuntime
        };
    }
    
    public async Task RecordInstanceUsageAsync(
        InstanceUsageRecord usage,
        CancellationToken ct = default)
    {
        await _history.SaveAsync(usage, ct);
        
        await _events.PublishAsync(new InstanceCostRecordedEvent(
            usage.InstanceId,
            usage.InstanceType,
            usage.Runtime,
            usage.Cost,
            DateTimeOffset.UtcNow));
    }
}

// src/Acode.Infrastructure/Compute/Ec2/Cost/Ec2BudgetChecker.cs
namespace Acode.Infrastructure.Compute.Ec2.Cost;

public sealed class Ec2BudgetChecker : IEc2BudgetChecker
{
    private readonly IEc2CostTracker _costTracker;
    private readonly IEc2InstanceManager _instanceManager;
    private readonly IEventPublisher _events;
    private readonly BudgetConfiguration _config;
    
    public async Task<BudgetCheckResult> CheckAsync(
        CostCheckRequest request,
        CancellationToken ct = default)
    {
        var hourlyRate = request.OverrideHourlyRate 
            ?? await _costTracker.GetHourlyRateAsync(
                request.InstanceType, request.Region, request.IsSpot, ct);
        
        var estimatedCost = (decimal)Math.Ceiling(request.EstimatedDuration.TotalHours) * hourlyRate;
        
        // Check all budget periods
        foreach (var (period, limit) in GetBudgetLimits())
        {
            var currentSpend = await GetCurrentSpendAsync(period, ct);
            
            if (currentSpend + estimatedCost > limit)
            {
                var violation = new BudgetViolation
                {
                    Period = period,
                    CurrentSpend = currentSpend + estimatedCost,
                    Limit = limit
                };
                
                return new BudgetCheckResult
                {
                    Allowed = _config.LimitType == BudgetLimitType.Soft,
                    Violation = violation,
                    CurrentSpend = currentSpend,
                    BudgetLimit = limit,
                    Message = $"Would exceed {period} budget by ${violation.Overage:F2}"
                };
            }
        }
        
        return new BudgetCheckResult { Allowed = true, Message = "Within budget" };
    }
    
    public async Task<bool> EnforceLimitsAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (_config.LimitType != BudgetLimitType.Hard)
            return false;
        
        var status = await GetBudgetStatusAsync(ct);
        
        if ((status.DailyLimit.HasValue && status.DailySpend > status.DailyLimit) ||
            (status.MonthlyLimit.HasValue && status.MonthlySpend > status.MonthlyLimit))
        {
            await _events.PublishAsync(new BudgetExceededEvent(
                sessionId,
                new BudgetViolation
                {
                    Period = BudgetPeriod.Daily,
                    CurrentSpend = status.DailySpend,
                    Limit = status.DailyLimit ?? 0
                },
                WillTerminate: true,
                DateTimeOffset.UtcNow));
            
            return true; // Signal to terminate running instances
        }
        
        return false;
    }
}

// src/Acode.Infrastructure/Compute/Ec2/Cost/Ec2PricingTable.cs
namespace Acode.Infrastructure.Compute.Ec2.Cost;

public sealed class Ec2PricingTable : IEc2PricingTable
{
    private static readonly Dictionary<string, decimal> OnDemandPrices = new()
    {
        ["t3.micro"] = 0.0104m,
        ["t3.small"] = 0.0208m,
        ["t3.medium"] = 0.0416m,
        ["t3.large"] = 0.0832m,
        ["c5.large"] = 0.085m,
        ["c5.xlarge"] = 0.17m,
        ["r5.large"] = 0.126m,
        ["g4dn.xlarge"] = 0.526m
    };
    
    public Task<decimal> GetHourlyRateAsync(
        string instanceType,
        string region,
        bool isSpot,
        CancellationToken ct = default)
    {
        if (OnDemandPrices.TryGetValue(instanceType, out var rate))
        {
            return Task.FromResult(isSpot ? rate * 0.3m : rate); // ~70% spot discount
        }
        
        return Task.FromResult(0.10m); // Default fallback
    }
}
```

### Implementation Checklist

| # | Requirement | Test | Impl |
|---|-------------|------|------|
| 1 | Hourly rate lookup by instance type | ⬜ | ⬜ |
| 2 | Running cost calculation (hours × rate) | ⬜ | ⬜ |
| 3 | Partial hours rounded up | ⬜ | ⬜ |
| 4 | Session cost tracking | ⬜ | ⬜ |
| 5 | Daily cost aggregation | ⬜ | ⬜ |
| 6 | Monthly cost aggregation | ⬜ | ⬜ |
| 7 | Budget check before launch | ⬜ | ⬜ |
| 8 | Soft limit alerts but allows | ⬜ | ⬜ |
| 9 | Hard limit blocks/terminates | ⬜ | ⬜ |
| 10 | Alert thresholds (50%, 80%, 100%) | ⬜ | ⬜ |
| 11 | Webhook alerts optional | ⬜ | ⬜ |
| 12 | Cost history persisted | ⬜ | ⬜ |
| 13 | CLI shows current costs | ⬜ | ⬜ |
| 14 | CostThresholdReachedEvent published | ⬜ | ⬜ |
| 15 | BudgetExceededEvent published | ⬜ | ⬜ |

### Rollout Plan

1. **Tests first**: Unit tests for cost calculation, budget checking
2. **Domain models**: Events, BudgetPeriod, BudgetViolation, CostEstimate
3. **Application interfaces**: IEc2CostTracker, IEc2BudgetChecker, IEc2CostAlertService
4. **Infrastructure impl**: Ec2CostTracker, Ec2BudgetChecker, Ec2PricingTable
5. **Persistence**: CostHistoryRepository for usage records
6. **Alerts**: Ec2CostAlertService with webhook support
7. **CLI integration**: Cost show/history/export commands
8. **DI registration**: Register tracker as singleton, checker as scoped

**End of Task 031.c Specification**