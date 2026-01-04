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

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 031.b Instance Management | IEc2InstanceManager | Running instance list | Cost calculation input |
| Task 033 Heuristics | IBurstHeuristics | Cost data for decisions | Decision factor |
| Task 009 Config | IConfiguration | Budget limits | Settings source |
| AWS Pricing API | AWSPricing (optional) | Instance type rates | Live pricing |
| Static Rate Table | JSON file | Fallback pricing data | Offline pricing |
| Webhook Service | HttpClient | Alert notifications | External notify |
| Cost History Store | ICostRepository | Historical cost data | Persistence |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Cost API unavailable | API timeout/error | Use static rate table | Estimates only |
| Limit exceeded | Pre-launch check | Block new instances | Launch prevented |
| Alert webhook fails | HTTP error | Log warning, retry | Notification delayed |
| Orphan found | Tag scan | Auto-terminate | Cost leak stopped |
| Rate table outdated | Age check | Warn and use | Inaccurate estimates |
| History storage fails | DB error | Log to file fallback | History incomplete |
| Budget bypass attempted | Flag check | Require explicit flag | Intentional override |
| Time zone mismatch | UTC enforcement | All times UTC | Consistent periods |

---

## Assumptions

1. **Pricing Source**: AWS pricing API or static rate table available
2. **USD Currency**: All costs in USD (no currency conversion)
3. **UTC Time Zones**: All time periods use UTC for consistency
4. **Instance Metadata**: Instance type and launch time available
5. **Spot Pricing**: Spot rates approximated or fetched from API
6. **EBS Costing**: EBS included in cost calculation (GB-month)
7. **Immediate Termination**: Hard limit terminates within 1 minute
8. **History Retention**: 90 days of cost history retained

---

## Security Considerations

1. **Budget Bypass Audit**: All `--allow-over-budget` usages MUST be logged
2. **Webhook Security**: Webhook URLs MUST use HTTPS
3. **Cost Data Privacy**: Cost data MUST NOT expose to unauthorized users
4. **Limit Modification Auth**: Budget changes MUST be authorized
5. **Orphan Cleanup Auth**: Only admin can trigger orphan cleanup
6. **Export Sensitivity**: CSV exports MUST NOT include secrets
7. **Alert Rate Limiting**: Alerts MUST NOT spam (once per threshold)
8. **Hard Limit Safety**: Hard limit MUST confirm before terminating

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

### Cost Tracking

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031C-01 | Hourly rate MUST be known for each instance type | P0 |
| FR-031C-02 | Rate MUST be fetched from AWS Pricing API when available | P1 |
| FR-031C-03 | Rate MUST fall back to static table when API unavailable | P0 |
| FR-031C-04 | Static rate table MUST be updatable without code change | P0 |
| FR-031C-05 | Rate MUST include instance type variation | P0 |
| FR-031C-06 | Rate MUST include region variation | P0 |
| FR-031C-07 | Rate MUST differentiate spot vs on-demand | P1 |
| FR-031C-08 | Running time MUST be tracked per instance | P0 |
| FR-031C-09 | Start time MUST be recorded at launch | P0 |
| FR-031C-10 | End time MUST be recorded at terminate | P0 |
| FR-031C-11 | Running cost MUST be calculated in real-time | P0 |
| FR-031C-12 | Cost formula: `hours × hourly_rate` | P0 |
| FR-031C-13 | Partial hours MUST be rounded up (AWS billing model) | P0 |
| FR-031C-14 | Total cost MUST accumulate across instances | P0 |
| FR-031C-15 | Cost per session MUST be tracked | P0 |
| FR-031C-16 | Cost history MUST persist to storage | P0 |
| FR-031C-17 | Cost MUST be queryable via API | P0 |
| FR-031C-18 | Query by session, date range, or total | P0 |
| FR-031C-19 | Cost MUST include EBS storage costs | P1 |
| FR-031C-20 | EBS rate calculated as GB × hours × (monthly rate / 720) | P1 |

### Budget Limits

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031C-21 | Budget MUST be configurable in agent-config.yml | P0 |
| FR-031C-22 | Budget per session MUST work | P0 |
| FR-031C-23 | Budget per day MUST work | P0 |
| FR-031C-24 | Budget per month MUST work | P0 |
| FR-031C-25 | Default MUST be no limit (unlimited) | P0 |
| FR-031C-26 | Limit reached MUST block new launches | P0 |
| FR-031C-27 | Block MUST prevent `RunInstances` call | P0 |
| FR-031C-28 | Running instances MAY continue when soft limit | P0 |
| FR-031C-29 | Hard limit MUST terminate running instances | P1 |
| FR-031C-30 | Hard limit terminates all session instances | P1 |
| FR-031C-31 | Soft limit MUST only alert (no termination) | P0 |
| FR-031C-32 | Limit type MUST be configurable (soft/hard) | P0 |
| FR-031C-33 | Default limit type MUST be soft | P0 |
| FR-031C-34 | Limit check MUST occur before launch | P0 |
| FR-031C-35 | Estimated cost MUST be checked against limit | P0 |
| FR-031C-36 | Estimate MUST include max runtime | P0 |
| FR-031C-37 | Max runtime MUST be configurable (default 4h) | P0 |
| FR-031C-38 | Limit bypass MUST require explicit flag | P1 |
| FR-031C-39 | `--allow-over-budget` flag MUST be logged | P0 |
| FR-031C-40 | Bypass MUST be audit-logged | P0 |

### Alerts

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031C-41 | Alert thresholds MUST be configurable | P0 |
| FR-031C-42 | Default thresholds: 50%, 80%, 100% of budget | P0 |
| FR-031C-43 | Alert MUST trigger when threshold reached | P0 |
| FR-031C-44 | Alert MUST invoke callback | P0 |
| FR-031C-45 | Alert MUST log at warning level | P0 |
| FR-031C-46 | Alert via webhook MUST be optional | P1 |
| FR-031C-47 | Webhook URL MUST be configurable | P1 |
| FR-031C-48 | Alert content MUST include current cost | P0 |
| FR-031C-49 | Alert content MUST include limit and percent | P0 |
| FR-031C-50 | Alert content MUST include running instance count | P0 |
| FR-031C-51 | Alert content MUST include estimated remaining | P1 |
| FR-031C-52 | Alert MUST NOT repeat (once per threshold) | P0 |
| FR-031C-53 | Alert state MUST reset on new period | P0 |
| FR-031C-54 | Daily reset at midnight UTC | P0 |
| FR-031C-55 | Monthly reset at month start UTC | P0 |
| FR-031C-56 | Alert history MUST be logged | P1 |
| FR-031C-57 | CLI MUST show alert history | P1 |
| FR-031C-58 | `acode cost alerts` command MUST work | P1 |
| FR-031C-59 | Alert suppression MUST be optional | P2 |
| FR-031C-60 | Suppression via `--quiet` flag | P2 |

### Cost Visibility

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031C-61 | Current cost MUST be queryable | P0 |
| FR-031C-62 | CLI command: `acode cost show` | P0 |
| FR-031C-63 | Show current session cost | P0 |
| FR-031C-64 | Show daily cost | P0 |
| FR-031C-65 | Show monthly cost | P0 |
| FR-031C-66 | Show running instances | P0 |
| FR-031C-67 | Show hourly burn rate | P0 |
| FR-031C-68 | Estimate to session end MUST be available | P0 |
| FR-031C-69 | Estimate to max runtime MUST be available | P0 |
| FR-031C-70 | Cost breakdown MUST work | P1 |
| FR-031C-71 | Breakdown by instance type | P1 |
| FR-031C-72 | Breakdown by task/session | P1 |
| FR-031C-73 | Breakdown by day | P1 |
| FR-031C-74 | Export to CSV MUST work | P1 |
| FR-031C-75 | `acode cost export --format csv` | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031C-01 | Cost calculation time | <100ms | P0 |
| NFR-031C-02 | Cost accuracy vs AWS bill | ±5% | P0 |
| NFR-031C-03 | Alert delivery latency | <1 minute | P0 |
| NFR-031C-04 | Cost query response time | <500ms | P1 |
| NFR-031C-05 | Budget check before launch | <100ms | P0 |
| NFR-031C-06 | Webhook delivery time | <5 seconds | P1 |
| NFR-031C-07 | CSV export time (1000 records) | <10 seconds | P2 |
| NFR-031C-08 | Rate table lookup time | <10ms | P1 |
| NFR-031C-09 | Hard limit termination | <1 minute | P0 |
| NFR-031C-10 | History query (90 days) | <5 seconds | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031C-11 | Cost history retention | 90 days | P0 |
| NFR-031C-12 | No orphan cost accumulation | Zero orphan cost | P0 |
| NFR-031C-13 | Alert delivery reliability | 99.9% | P0 |
| NFR-031C-14 | Budget enforcement reliability | 100% blocked | P0 |
| NFR-031C-15 | Rate table fallback | Always available | P0 |
| NFR-031C-16 | Cost calculation consistency | Same result on retry | P0 |
| NFR-031C-17 | Period boundary handling | Correct UTC reset | P0 |
| NFR-031C-18 | History persistence | Survives restart | P0 |
| NFR-031C-19 | Hard limit enforcement | 100% terminated | P0 |
| NFR-031C-20 | Bypass flag audit | 100% logged | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031C-21 | Structured logging for all cost events | JSON format | P0 |
| NFR-031C-22 | Metrics for spending rate | Histogram | P1 |
| NFR-031C-23 | Audit trail for budget bypasses | Full history | P0 |
| NFR-031C-24 | Timezone handling | All UTC | P0 |
| NFR-031C-25 | Currency display | USD only | P0 |
| NFR-031C-26 | Cost breakdown metrics | By type/session | P1 |
| NFR-031C-27 | Alert event logging | Each alert logged | P0 |
| NFR-031C-28 | Budget limit logging | Changes logged | P1 |
| NFR-031C-29 | Estimate accuracy tracking | Actual vs estimate | P2 |
| NFR-031C-30 | Rate table version logging | Source tracked | P1 |

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

### Cost Tracking
- [ ] AC-001: Hourly rate known for instance types
- [ ] AC-002: Pricing API fetches rates when available
- [ ] AC-003: Static table fallback works
- [ ] AC-004: Rate includes instance type variation
- [ ] AC-005: Rate includes region variation
- [ ] AC-006: Spot vs on-demand rates differentiated
- [ ] AC-007: Running time tracked per instance
- [ ] AC-008: Start/end times recorded
- [ ] AC-009: Running cost calculates correctly
- [ ] AC-010: Partial hours rounded up
- [ ] AC-011: Total cost accumulates
- [ ] AC-012: Session cost tracked
- [ ] AC-013: Cost history persists
- [ ] AC-014: Cost queryable by session/date/total
- [ ] AC-015: EBS costs included

### Budget Limits
- [ ] AC-016: Budget configurable in config
- [ ] AC-017: Per-session budget works
- [ ] AC-018: Per-day budget works
- [ ] AC-019: Per-month budget works
- [ ] AC-020: Default is no limit
- [ ] AC-021: Limit reached blocks new launches
- [ ] AC-022: Running instances continue (soft limit)
- [ ] AC-023: Hard limit terminates running instances
- [ ] AC-024: Soft limit only alerts
- [ ] AC-025: Limit type configurable
- [ ] AC-026: Pre-launch limit check works
- [ ] AC-027: Estimated cost checked
- [ ] AC-028: Max runtime estimate included
- [ ] AC-029: --allow-over-budget bypasses limit
- [ ] AC-030: Bypass is audit-logged

### Alerts
- [ ] AC-031: Alert thresholds configurable
- [ ] AC-032: Default thresholds: 50%, 80%, 100%
- [ ] AC-033: Alert triggers at threshold
- [ ] AC-034: Callback invoked on alert
- [ ] AC-035: Log warning written
- [ ] AC-036: Webhook delivery works
- [ ] AC-037: Alert content includes details
- [ ] AC-038: No repeat alerts (once per threshold)
- [ ] AC-039: Daily reset at midnight UTC
- [ ] AC-040: Monthly reset at month start
- [ ] AC-041: Alert history logged
- [ ] AC-042: CLI shows alert history

### Cost Visibility
- [ ] AC-043: Current cost queryable
- [ ] AC-044: `acode cost show` works
- [ ] AC-045: Session cost displayed
- [ ] AC-046: Daily cost displayed
- [ ] AC-047: Monthly cost displayed
- [ ] AC-048: Running instances listed
- [ ] AC-049: Hourly burn rate shown
- [ ] AC-050: Estimate to session end works
- [ ] AC-051: Estimate to max runtime works
- [ ] AC-052: Breakdown by instance type works
- [ ] AC-053: Breakdown by day works
- [ ] AC-054: CSV export works

### Reliability
- [ ] AC-055: Cost accuracy within ±5%
- [ ] AC-056: 90 days history retained
- [ ] AC-057: No orphan cost accumulation
- [ ] AC-058: Alert latency <1 minute
- [ ] AC-059: All times in UTC
- [ ] AC-060: Currency shows as USD

---

## User Verification Scenarios

### Scenario 1: Developer Monitors Session Costs
**Persona:** Cost-conscious developer  
**Preconditions:** EC2 instance running for 2 hours  
**Steps:**
1. Run `acode cost show`
2. View session cost breakdown
3. Check burn rate
4. Estimate remaining time

**Verification Checklist:**
- [ ] Session cost accurate to ±5%
- [ ] Instance type and runtime displayed
- [ ] Burn rate shows $/hour
- [ ] Estimate calculates correctly
- [ ] EBS costs included

### Scenario 2: Budget Limit Blocks Launch
**Persona:** Developer with $10 session budget  
**Preconditions:** Already spent $9.50 this session  
**Steps:**
1. Attempt to launch new instance
2. Pre-launch check runs
3. Block with clear message
4. View cost breakdown

**Verification Checklist:**
- [ ] Pre-launch check executes
- [ ] Limit exceeded detected
- [ ] Clear error message shown
- [ ] Current cost vs limit displayed
- [ ] --allow-over-budget option mentioned

### Scenario 3: Soft Limit Alert at 80%
**Persona:** DevOps monitoring costs  
**Preconditions:** $50/day budget, $40 spent  
**Steps:**
1. Session continues running
2. 80% threshold reached
3. Alert triggered
4. Webhook notification sent

**Verification Checklist:**
- [ ] 80% threshold detected
- [ ] Warning logged
- [ ] Callback invoked
- [ ] Webhook sent with details
- [ ] Alert not repeated on next check

### Scenario 4: Hard Limit Terminates Instances
**Persona:** Admin with strict budget  
**Preconditions:** Hard limit configured, budget exceeded  
**Steps:**
1. Running cost exceeds hard limit
2. Termination triggered
3. All session instances terminated
4. Cleanup verified

**Verification Checklist:**
- [ ] Hard limit detected
- [ ] Termination initiated within 1 minute
- [ ] All session instances terminated
- [ ] User notified of termination
- [ ] Final cost recorded

### Scenario 5: Cost History Export
**Persona:** Finance team member  
**Preconditions:** 30 days of cost history exists  
**Steps:**
1. Run `acode cost export --format csv`
2. Export last 30 days
3. Open in spreadsheet
4. Analyze by day/type

**Verification Checklist:**
- [ ] CSV generated successfully
- [ ] All 30 days included
- [ ] Breakdown by instance type
- [ ] Breakdown by day
- [ ] Totals calculate correctly

### Scenario 6: Bypass Budget with Flag
**Persona:** Developer needing urgent compute  
**Preconditions:** Budget exceeded, urgent need  
**Steps:**
1. Attempt launch (blocked)
2. Retry with --allow-over-budget
3. Launch succeeds
4. Audit log captured

**Verification Checklist:**
- [ ] Normal launch blocked
- [ ] Flag allows bypass
- [ ] Instance launches
- [ ] Bypass logged with reason
- [ ] Audit trail includes user/time

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-031C-01 | Cost calculation formula | FR-031C-12 |
| UT-031C-02 | Partial hour rounding | FR-031C-13 |
| UT-031C-03 | Budget checking logic | FR-031C-26-27 |
| UT-031C-04 | Alert threshold detection | FR-031C-43 |
| UT-031C-05 | Alert deduplication | FR-031C-52 |
| UT-031C-06 | Period reset logic (daily) | FR-031C-54 |
| UT-031C-07 | Period reset logic (monthly) | FR-031C-55 |
| UT-031C-08 | Rate table lookup | FR-031C-03 |
| UT-031C-09 | EBS cost calculation | FR-031C-20 |
| UT-031C-10 | Spot rate differentiation | FR-031C-07 |
| UT-031C-11 | Estimate calculation | FR-031C-35-36 |
| UT-031C-12 | CSV export formatting | FR-031C-74 |
| UT-031C-13 | Hard limit detection | FR-031C-29 |
| UT-031C-14 | Bypass flag handling | FR-031C-38-40 |
| UT-031C-15 | UTC time handling | NFR-031C-24 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-031C-01 | Real cost tracking E2E | FR-031C-01-18 |
| IT-031C-02 | Budget enforcement | FR-031C-26-27 |
| IT-031C-03 | Alert webhook delivery | FR-031C-46-47 |
| IT-031C-04 | CLI cost commands | FR-031C-62-75 |
| IT-031C-05 | Hard limit termination | FR-031C-29-30 |
| IT-031C-06 | Pricing API integration | FR-031C-02 |
| IT-031C-07 | Rate table fallback | FR-031C-03 |
| IT-031C-08 | Cost history persistence | FR-031C-16 |
| IT-031C-09 | CSV export E2E | FR-031C-74 |
| IT-031C-10 | Cost accuracy vs AWS | NFR-031C-02 |
| IT-031C-11 | 90-day history retention | NFR-031C-11 |
| IT-031C-12 | Alert latency check | NFR-031C-03 |
| IT-031C-13 | Period boundary handling | FR-031C-54-55 |
| IT-031C-14 | Bypass audit logging | FR-031C-40 |
| IT-031C-15 | Orphan cost prevention | NFR-031C-12 |

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