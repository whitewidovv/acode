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

### Interface

```csharp
public interface IEc2CostTracker
{
    Task<decimal> GetCurrentSessionCostAsync(string sessionId);
    Task<decimal> GetDailyCostAsync(DateTime date);
    Task<decimal> GetMonthlyCostAsync(int year, int month);
    Task<CostEstimate> EstimateSessionCostAsync(string sessionId, TimeSpan maxRuntime);
    Task<bool> CheckBudgetAsync(CostCheckRequest request);
    Task RecordInstanceUsageAsync(InstanceUsageRecord usage);
}

public record CostCheckRequest(
    string InstanceType,
    string Region,
    bool IsSpot,
    TimeSpan EstimatedDuration,
    decimal? OverrideHourlyRate = null);

public record CostEstimate(
    decimal EstimatedCost,
    decimal HourlyRate,
    TimeSpan Duration,
    bool ExceedsBudget,
    decimal? BudgetRemaining);

public record InstanceUsageRecord(
    string InstanceId,
    string InstanceType,
    string Region,
    bool IsSpot,
    DateTime StartTime,
    DateTime? EndTime,
    decimal HourlyRate);
```

### Budget Checker

```csharp
public class Ec2BudgetChecker
{
    public async Task<BudgetCheckResult> CheckAsync(
        CostCheckRequest request,
        CancellationToken ct);
        
    public async Task EnforceLimitsAsync(
        string sessionId,
        CancellationToken ct);
}

public record BudgetCheckResult(
    bool Allowed,
    BudgetViolation? Violation,
    decimal CurrentSpend,
    decimal BudgetLimit,
    string Message);

public record BudgetViolation(
    BudgetPeriod Period,
    decimal CurrentSpend,
    decimal Limit,
    decimal Overage);

public enum BudgetPeriod { Session, Daily, Monthly }
```

---

**End of Task 031.c Specification**