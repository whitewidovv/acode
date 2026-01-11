# Acode.Infrastructure.Benchmarks

Performance benchmarks for conversation data model repository operations.

## Running Benchmarks

```bash
# Run all benchmarks
dotnet run -c Release --project tests/Acode.Infrastructure.Benchmarks

# Run specific benchmark class
dotnet run -c Release --project tests/Acode.Infrastructure.Benchmarks --filter "*MessageRepositoryBenchmarks*"

# Run specific benchmark method
dotnet run -c Release --project tests/Acode.Infrastructure.Benchmarks --filter "*BulkInsert100Messages*"
```

## Benchmark Classes

### 1. ChatRepositoryBenchmarks
CRUD operations for Chat aggregate root.
- **CreateChat**: Chat creation latency (target: <5ms)
- **GetChatById**: Chat retrieval by ID (target: <3ms)
- **UpdateChat**: Chat update latency (target: <5ms)
- **SoftDeleteChat**: Soft delete operation (target: <5ms)
- **ListAllChats**: List all chats with filtering (target: <10ms)

### 2. RunRepositoryBenchmarks
CRUD operations for Run entity.
- **CreateRun**: Run creation latency (target: <5ms)
- **GetRunById**: Run retrieval by ID (target: <3ms)
- **UpdateRun**: Run update/completion (target: <5ms)
- **ListRunsByChat**: List runs by chat ID (target: <10ms)

### 3. MessageRepositoryBenchmarks
CRUD operations for Message entity.
- **CreateMessage**: Message creation (target: <5ms)
- **GetMessageById**: Message retrieval (target: <3ms)
- **UpdateMessage**: Message update (target: <5ms)
- **ListMessagesByRun**: List messages by run (target: <10ms)
- **AppendMessage**: AppendAsync operation (target: <5ms)

### 4. BulkOperationsBenchmarks
Bulk insert performance validation.
- **BulkInsert10Messages**: 10 messages bulk insert (target: <10ms)
- **BulkInsert100Messages**: 100 messages bulk insert (target: <50ms per AC-096)
- **BulkInsert1000Messages**: 1000 messages bulk insert (target: <500ms)
- **Sequential100Appends**: Sequential baseline for comparison

### 5. ConcurrencyBenchmarks
Concurrent operation performance validation.
- **Concurrent5Creates**: 5 concurrent message creates (target: <50ms)
- **Concurrent10Creates**: 10 concurrent creates (target: <100ms per AC-097)
- **Concurrent20Creates**: 20 concurrent creates (target: <200ms)
- **Concurrent10Reads**: 10 concurrent reads (target: <50ms)
- **Concurrent10Appends**: 10 concurrent AppendAsync ops (target: <100ms)

## Performance Targets

From task-049a acceptance criteria:

- **AC-095**: CRUD latency targets
  - Create operations: <5ms
  - Read operations: <3ms
  - Update operations: <5ms

- **AC-096**: Bulk operations
  - 100 message inserts: <50ms

- **AC-097**: Concurrent operations
  - 10 concurrent operations: <100ms

## Benchmark Results Template

```
BenchmarkDotNet=v0.13.x, OS=...
Processor=...
.NET SDK=8.0.x

|                    Method |     Mean |    Error |   StdDev |   Gen0 | Allocated |
|-------------------------- |---------:|---------:|---------:|-------:|----------:|
|                CreateChat |  X.XX ms | X.XXX ms | X.XXX ms | XX.XXX |   XX.XX KB |
|              GetChatById |  X.XX ms | X.XXX ms | X.XXX ms |  X.XXX |    X.XX KB |
...
```

## Interpreting Results

- **Mean**: Average execution time
- **Error**: Standard error of the mean
- **StdDev**: Standard deviation
- **Gen0/Gen1/Gen2**: Garbage collection counts
- **Allocated**: Total memory allocated

## Notes

- All benchmarks use in-memory SQLite databases created in temp directories
- Each benchmark has warmup iterations (3) and measurement iterations (10)
- MemoryDiagnoser tracks allocations
- Database schema is initialized from SQL migration files
- Test data (Chat, Run) is seeded during GlobalSetup

## Continuous Monitoring

Run benchmarks regularly to track performance regressions:
- Before merging performance-sensitive changes
- After upgrading dependencies (Dapper, SQLite)
- When modifying repository implementations
- As part of release validation

## Baseline Expectations

Based on SQLite with Dapper on typical developer machine:
- Single CRUD operations: 1-3ms (well within targets)
- Bulk 100 inserts: 20-40ms (within <50ms target)
- Concurrent 10 ops: 30-80ms (within <100ms target)

Actual results will vary based on:
- Hardware (SSD vs HDD, CPU performance)
- OS and filesystem overhead
- SQLite configuration
- Concurrent system load
