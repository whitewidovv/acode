# Task 017.b: TS/JS Symbol Extraction

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 3 – Intelligence Layer  
**Dependencies:** Task 017 (Symbol Index v2)  

---

## Description

### Business Value

TypeScript and JavaScript symbol extraction enables the agent to understand web and Node.js codebases, which represent a significant portion of modern software development. Without TS/JS parsing capabilities, the agent cannot navigate React components, understand Express routes, or comprehend the structure of frontend applications. This task implements production-grade TypeScript/JavaScript parsing using the TypeScript Compiler API.

The TypeScript Compiler API provides complete AST access comparable to Roslyn for C#. It handles both TypeScript's rich type system and JavaScript's dynamic nature. Since TypeScript is a superset of JavaScript, a single extractor implementation can process both languages, with TypeScript files providing richer type information. This unified approach reduces complexity while maximizing language coverage.

The extracted symbols integrate with the Symbol Index (Task 017) to power cross-language code intelligence. When a user asks the agent to modify a React component or an API endpoint, the symbol extractor identifies functions, classes, exports, and their associated JSDoc documentation. The extractor runs in a Node.js subprocess, communicating with the .NET agent via a JSON message protocol, ensuring isolation and proper language runtime support.

### Return on Investment (ROI)

| Investment | Cost | Return |
|------------|------|--------|
| Development effort | 15 developer-days (~$18,000) | Amortized across all TS/JS operations |
| Node.js bridge complexity | +3 days for inter-process communication | Process isolation prevents crashes |
| TypeScript Compiler API learning | 2 days | Leverages industry-standard tooling |
| JSDoc extraction logic | 2 days | Complete documentation for all symbols |
| **Total Investment** | **$22,000** | |

| Benefit | Value |
|---------|-------|
| Web/frontend codebase support | Enables 60% of modern project types |
| React/Vue/Angular component understanding | +40% user adoption potential |
| Node.js backend support | Full-stack JavaScript coverage |
| Context reduction via symbols | 90% fewer tokens for JS/TS files |
| Reduced LLM hallucinations | 70% fewer errors in JS modifications |
| **Annual Value** | **$280,000** (based on enterprise usage) |

**Break-even Analysis:** ROI achieved within 1 month of production use for teams with significant JavaScript/TypeScript codebases.

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         .NET Agent Process                                   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │  TypeScriptSymbolExtractor : ISymbolExtractor                        │   │
│  │  ┌─────────────────────────────────────────────────────────────┐    │   │
│  │  │  Language: "typescript"                                      │    │   │
│  │  │  FileExtensions: [".ts", ".tsx", ".js", ".jsx"]             │    │   │
│  │  │  ExtractAsync() -> NodeBridge.SendAsync()                    │    │   │
│  │  └──────────────────────┬──────────────────────────────────────┘    │   │
│  │                          │                                           │   │
│  │  ┌──────────────────────▼──────────────────────────────────────┐    │   │
│  │  │  NodeBridge                                                  │    │   │
│  │  │  ┌─────────────────────────────────────────────────────┐    │    │   │
│  │  │  │  Process: node.exe                                  │    │    │   │
│  │  │  │  Protocol: JSON over stdin/stdout                   │    │    │   │
│  │  │  │  Lifecycle: Start/Stop/Restart                      │    │    │   │
│  │  │  └─────────────────────────────────────────────────────┘    │    │   │
│  │  └──────────────────────┬──────────────────────────────────────┘    │   │
│  └──────────────────────────┼──────────────────────────────────────────┘   │
│                              │ stdin/stdout (JSON)                          │
└──────────────────────────────┼──────────────────────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                         Node.js Subprocess                                    │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │  ts-extractor (bundled TypeScript application)                          │ │
│  │                                                                          │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐ │ │
│  │  │ MessageLoop  │  │ TSExtractor  │  │ SymbolVisitor│  │ JSDocParser │ │ │
│  │  │ reads stdin  │─▶│ ts.create    │─▶│ AST walking  │─▶│ @param etc  │ │ │
│  │  │ writes stdout│  │ SourceFile() │  │              │  │             │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └─────────────┘ │ │
│  │         │                                                    │          │ │
│  │         ▼                                                    ▼          │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐  │ │
│  │  │                    TypeScript Compiler API                        │  │ │
│  │  │  ts.createSourceFile(), ts.forEachChild(), ts.isClassDeclaration  │  │ │
│  │  └──────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Data Flow

```
Input .ts/.js File                   Symbol Output
       │                                  ▲
       ▼                                  │
┌──────────────┐                  ┌──────────────┐
│ File Content │                  │ ISymbol List │
│ (UTF-8 text) │                  │ + Metadata   │
└──────┬───────┘                  └──────▲───────┘
       │                                  │
       ▼                                  │
┌──────────────┐    JSON          ┌──────────────┐
│ .NET Request │ ──────────────▶  │ Node.js Parse│
│ (path, opts) │    stdin         │ ts.createSrc │
└──────────────┘                  └──────┬───────┘
                                         │
                                         ▼
                                  ┌──────────────┐
                                  │ AST Visitor  │
                                  │ Class/Func/  │
                                  │ Interface... │
                                  └──────┬───────┘
                                         │
                                         ▼
                                  ┌──────────────┐
                                  │ JSDoc Parse  │
                                  │ @param etc   │
                                  └──────┬───────┘
                                         │
       ┌─────────────────────────────────┘
       │ JSON stdout
       ▼
┌──────────────┐
│ .NET Response│
│ Deserialize  │
└──────────────┘
```

### Trade-offs

**Trade-off 1: Node.js Subprocess vs Embedded JavaScript Engine**

| Approach | Pros | Cons |
|----------|------|------|
| Node.js Subprocess | Full TypeScript API, industry-standard, exact compiler behavior | Process overhead, startup time, IPC complexity |
| Embedded JS (Jint/V8) | In-process, faster communication | No TypeScript API, custom parser needed, maintenance burden |

**Decision:** Node.js subprocess is chosen for production reliability and TypeScript Compiler API access. The IPC overhead is acceptable given the infrequent extraction operations and ability to batch requests.

**Trade-off 2: Bundled vs Runtime npm install**

| Approach | Pros | Cons |
|----------|------|------|
| Bundled TypeScript | No internet required, version controlled | Larger distribution, update requires rebuild |
| Runtime npm install | Always latest, smaller initial package | Network dependency, version conflicts, security risk |

**Decision:** TypeScript is bundled with the agent distribution. This ensures offline operation and eliminates npm as an attack surface. Updates are managed through agent releases.

**Trade-off 3: Single Bridge Process vs Worker Pool**

| Approach | Pros | Cons |
|----------|------|------|
| Single Bridge | Simple lifecycle, predictable resource usage | Sequential processing, potential bottleneck |
| Worker Pool | Parallel extraction, better throughput | Complex coordination, higher memory usage |

**Decision:** Start with single bridge process for simplicity. Parallel extraction can be added if profiling shows TypeScript extraction as a bottleneck. The current design allows internal batching.

### Scope

This task delivers the following components:

1. **NodeBridge** - .NET component that spawns and manages the Node.js extraction process with JSON stdin/stdout protocol
2. **TypeScript Extractor (Node.js)** - Node.js application using TypeScript Compiler API to parse and extract symbols
3. **JSDocExtractor** - Parser for JSDoc comments (@param, @returns, @description, @example, @deprecated)
4. **MessageProtocol** - Defines request/response message formats for cross-process communication
5. **TypeScriptSymbolExtractor** - ISymbolExtractor implementation that orchestrates the Node.js bridge for .ts/.js/.tsx/.jsx files

### Integration Points

| Component | Integration Type | Description |
|-----------|------------------|-------------|
| Symbol Index (Task 017) | Consumer | Receives extracted symbols for indexing and querying |
| Dependency Mapper (Task 017.c) | Consumer | Uses extracted symbols to build import/export relationships |
| Context Packer | Consumer | Retrieves symbol metadata when packing code context for prompts |
| Configuration System | Configuration | Reads extraction settings (include JS, include JSX, extract JSDoc) |
| File System Abstraction | Dependency | Reads TypeScript/JavaScript source files |
| Node.js Runtime | External Dependency | Required runtime for TypeScript Compiler API execution |
| Logging Infrastructure | Dependency | Reports extraction progress, errors, and bridge status |

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Node.js not installed | Extractor unavailable | Clear error message with installation instructions, graceful feature degradation |
| Node.js process crash | Extraction interruption | Auto-restart bridge with exponential backoff, report partial results |
| Invalid TypeScript syntax | Partial AST available | Extract from valid portions, log diagnostics |
| Large file timeout | Extraction hangs | Configurable timeout (default 30s), return partial results |
| Missing tsconfig.json | Reduced type resolution | Fall back to default compiler options |
| Memory exhaustion in Node | Process crash | Memory limits on Node process, file size restrictions |

### Assumptions

1. Node.js 18.x or higher is installed and available in PATH
2. TypeScript Compiler API is bundled with the extraction tool (no external npm install required)
3. Source files use UTF-8 encoding
4. JSDoc follows standard format (@param, @returns, etc.)
5. ES modules (import/export) and CommonJS (require/module.exports) are both supported
6. React JSX/TSX syntax is handled via appropriate compiler options
7. The Node.js bridge process has a maximum lifespan and is recycled periodically
8. tsconfig.json is used when present but not required for basic extraction

### Security Considerations

1. **Process Isolation** - Node.js runs in a separate process with no .NET memory access
2. **No Code Execution** - TypeScript parsing MUST NOT execute source code or eval() statements
3. **Path Validation** - All file paths MUST be validated before passing to Node.js subprocess
4. **Input Sanitization** - JSON messages MUST be validated to prevent injection attacks
5. **Resource Limits** - Node.js process MUST have memory and CPU limits to prevent DoS

### Supported Language Constructs

The TypeScript Symbol Extractor must handle the complete TypeScript/JavaScript language surface:

**TypeScript Constructs:**

| Construct | Complexity | Extraction Details |
|-----------|------------|-------------------|
| Class declarations | Medium | Name, modifiers, heritage clauses, type parameters |
| Interface declarations | Medium | Name, properties, methods, extends clauses |
| Type aliases | Low | Name, definition, type parameters |
| Enum declarations | Low | Name, members with initializers |
| Function declarations | Medium | Name, parameters, return type, async/generator |
| Arrow functions | Medium | Parameters, return type (when assigned to const) |
| Variable declarations | Low | Name, type annotation, const/let/var |
| Method declarations | Medium | Name, parameters, return type, modifiers |
| Property declarations | Low | Name, type, optional/readonly modifiers |
| Constructor declarations | Medium | Parameters with automatic property promotion |
| Getter/setter declarations | Low | Name, return/parameter type |
| Index signatures | Low | Key type, value type |
| Call/construct signatures | Low | Parameters, return type |
| Mapped types | High | Constraint, modifier, property type |
| Conditional types | High | Check type, true/false branches |
| Decorators | Medium | Name, arguments (experimental) |

**JavaScript-Specific:**

| Construct | Complexity | Extraction Details |
|-----------|------------|-------------------|
| Function expressions | Medium | Inferred name, parameters, body hints |
| Object method shorthand | Low | Name, parameters |
| Computed property names | Medium | Expression evaluation for static analysis |
| Spread elements | Low | Detection in arrays and objects |
| Destructuring patterns | Medium | Extracted bindings |
| Dynamic imports | Low | Module specifier |
| CommonJS require() | Low | Module specifier extraction |
| module.exports | Low | Exported value identification |
| ES modules | Medium | Named/default exports, re-exports |

**JSX/TSX Constructs:**

| Construct | Complexity | Extraction Details |
|-----------|------------|-------------------|
| JSX elements | Medium | Tag name, props |
| Function components | Medium | Props type, return JSX detection |
| Class components | Medium | React.Component heritage, render method |
| Hooks usage | Low | Detection of useState, useEffect, etc. |
| Context providers | Low | Context value type |
| Higher-order components | Medium | Wrapped component detection |

### Performance Characteristics

| File Size | Symbol Count | Expected Parse Time | Expected Extract Time | Total Time |
|-----------|--------------|---------------------|----------------------|------------|
| < 1 KB | ~5-10 | < 10 ms | < 5 ms | < 20 ms |
| 1-10 KB | ~10-50 | 10-30 ms | 5-20 ms | 20-60 ms |
| 10-50 KB | ~50-200 | 30-100 ms | 20-80 ms | 60-200 ms |
| 50-100 KB | ~200-500 | 100-250 ms | 80-150 ms | 200-450 ms |
| > 100 KB | ~500+ | 250-500 ms | 150-300 ms | 450-900 ms |

**Memory Usage:**

| Operation | Baseline | Per Symbol | Per 1KB Source |
|-----------|----------|------------|----------------|
| Node.js process | 50 MB | N/A | N/A |
| TypeScript parse | 10 MB | ~1 KB | ~100 KB |
| Symbol extraction | 5 MB | ~500 bytes | ~50 KB |
| JSON serialization | 2 MB | ~200 bytes | ~20 KB |

### Comparison with Alternatives

| Approach | Accuracy | Performance | Maintenance | Recommendation |
|----------|----------|-------------|-------------|----------------|
| TypeScript Compiler API (chosen) | 100% | Good (50-500ms) | Low (stable API) | ✅ Selected |
| Babel parser | 95% | Excellent (20-200ms) | Medium (frequent updates) | Alternative for perf |
| Tree-sitter TS | 90% | Excellent (10-100ms) | High (grammar updates) | Consider for speed |
| ESLint parser | 98% | Good (40-400ms) | Medium | Not recommended (lint-focused) |
| Custom regex | 40% | Excellent (5-50ms) | Very High | Never use |
| SWC parser | 97% | Excellent (5-50ms) | Medium (Rust binding) | Future consideration |

### Message Protocol Specification

**Request Envelope:**
```json
{
    "id": 1,
    "type": "ExtractionRequest",
    "payload": {
        "filePath": "/path/to/file.ts",
        "includeJSDoc": true,
        "includePrivateMembers": false,
        "includeTypeInfo": true,
        "maxDepth": 10
    }
}
```

**Response Envelope:**
```json
{
    "id": 1,
    "payload": {
        "filePath": "/path/to/file.ts",
        "symbols": [...],
        "errors": [...],
        "isPartial": false,
        "parseTimeMs": 45,
        "extractTimeMs": 32
    }
}
```

**Error Response:**
```json
{
    "id": 1,
    "error": "File not found: /path/to/file.ts"
}
```

### Ready Signal Protocol

On startup, the Node.js bridge sends a ready signal:
```json
{"status": "ready", "version": "1.0.0"}
```

The .NET side waits up to 5 seconds for this signal before considering the bridge startup failed.

### Lifecycle State Machine

```
┌───────────────────────────────────────────────────────────────┐
│                    Node Bridge States                          │
│                                                                 │
│    ┌──────────┐         ┌──────────┐         ┌──────────┐     │
│    │  Stopped │ ──────▶ │ Starting │ ──────▶ │  Ready   │     │
│    └──────────┘  Start  └──────────┘ Ready   └──────────┘     │
│          ▲                    │                    │           │
│          │                    │ Timeout            │ Request   │
│          │                    ▼                    ▼           │
│          │              ┌──────────┐         ┌──────────┐     │
│          └───────────── │  Failed  │         │Processing│     │
│           GiveUp        └──────────┘         └──────────┘     │
│          │                    ▲                    │           │
│          │                    │                    │           │
│    ┌─────┴────┐               │                    │           │
│    │Restarting│ ◀─────────────┴────────────────────┘           │
│    └──────────┘          Crash/Error                           │
│                                                                 │
└───────────────────────────────────────────────────────────────┘
```

### Extensibility Points

1. **Custom Symbol Visitors** - Additional AST node handlers can be registered
2. **Plugin JSDoc Tags** - Custom @tags can be configured for extraction
3. **Type Resolvers** - External type resolution for imports
4. **Output Formatters** - Alternative serialization formats (protobuf, msgpack)
5. **Pre/Post Processors** - Transform symbols before/after extraction

### Monitoring and Observability

The TypeScript extractor emits the following metrics:

| Metric | Type | Description |
|--------|------|-------------|
| `ts_extraction_duration_ms` | Histogram | Time to extract symbols from a file |
| `ts_parse_duration_ms` | Histogram | Time for TypeScript to parse AST |
| `ts_symbol_count` | Counter | Total symbols extracted |
| `ts_bridge_restarts` | Counter | Number of bridge restarts |
| `ts_extraction_errors` | Counter | Extraction failures by error code |
| `ts_bridge_uptime_ms` | Gauge | Current bridge process uptime |
| `ts_memory_usage_bytes` | Gauge | Node.js process memory usage |

### Logging Categories

| Category | Log Level | Content |
|----------|-----------|---------|
| `Acode.TypeScript.Bridge` | Debug | Process start/stop, PID, ready signals |
| `Acode.TypeScript.Extraction` | Debug | Per-file extraction start/complete |
| `Acode.TypeScript.Protocol` | Trace | Raw JSON messages (sensitive) |
| `Acode.TypeScript.Error` | Warning | Parse errors, extraction failures |
| `Acode.TypeScript.Performance` | Information | Timing summaries, slow operations |

---

## Use Cases

### Use Case 1: Developer Extracts Symbols from React Component

**Persona:** Alex Rivera, Frontend Developer working on a React/TypeScript application.

**Context:** Alex needs to understand the structure of a complex React component before refactoring it.

**Before TypeScript Symbol Extraction:**
1. Opens the component file (500 lines)
2. Scrolls through looking for exported functions and hooks
3. Manually tracks props interfaces
4. Searches for useEffect hooks and their dependencies
5. Total time: 20 minutes

**After TypeScript Symbol Extraction:**
```bash
$ acode symbols extract src/components/Dashboard/DashboardWidget.tsx

Extracting symbols from DashboardWidget.tsx...
Found 18 symbols:

Interfaces:
  DashboardWidgetProps (exported) - lines 5-15
    Properties: title, data, onRefresh, isLoading

Functions:
  DashboardWidget (default export) - lines 18-125
    Signature: (props: DashboardWidgetProps) => JSX.Element
    Documentation: "Renders a dashboard widget with data visualization."

  useWidgetData (exported) - lines 128-165
    Signature: (widgetId: string) => WidgetDataResult
    Documentation: "Custom hook for fetching widget data."

  formatWidgetValue (internal) - lines 168-180
    Signature: (value: number, format: string) => string

Constants:
  REFRESH_INTERVAL (exported) - line 3
    Type: number
    Value: 30000
```

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to understand component | 20 min | 30 sec | 40x faster |
| Exported symbols found | ~80% | 100% | Complete |
| Props interface visibility | Manual search | Instant | Immediate |

---

### Use Case 2: AI Agent Modifies Express Route Handler

**Persona:** The AI coding agent, implementing a user-requested API change.

**Context:** User requests: "Add rate limiting to the /api/users endpoint in userRoutes.ts."

**Before TypeScript Symbol Extraction:**
1. Agent greps for "users" - gets 200+ matches across files
2. Opens multiple route files to find the right handler
3. Includes entire router file in context (400 lines)
4. LLM sees unrelated routes, middleware, imports
5. 3 attempts to make correct modification

**After TypeScript Symbol Extraction:**
```typescript
// Agent uses symbol extraction API
var symbols = await tsExtractor.ExtractAsync("src/routes/userRoutes.ts");

// Finds:
// - Function: getUsersHandler (exported) - lines 45-78
// - Function: createUserHandler (exported) - lines 82-120
// - Constant: userRouter (default export) - line 15

// Agent retrieves only lines 45-78 for the specific handler
```
1. Agent identifies exact function location
2. Retrieves only 33 lines of context
3. LLM generates correct rate limiting wrapper
4. Single iteration success

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Files searched | 12 | 1 | 92% reduction |
| Context tokens | 3,200 | 450 | 86% reduction |
| Iterations to correct | 3 | 1 | 66% improvement |
| Token cost | ~$0.96 | ~$0.14 | 85% savings |

---

### Use Case 3: Understanding Module Exports for Refactoring

**Persona:** Jordan Kim, Tech Lead planning a module restructuring.

**Context:** Jordan needs to understand all public exports from a utils module to plan a safe refactoring.

**Before TypeScript Symbol Extraction:**
1. Opens index.ts barrel file
2. Traces each re-export to source files
3. Documents each function's signature manually
4. Checks for default vs named exports
5. Total time: 45 minutes for 8 files

**After TypeScript Symbol Extraction:**
```bash
$ acode symbols extract src/utils/ --exports-only

Analyzing exports in src/utils/...

src/utils/index.ts (barrel):
  Re-exports from ./date.ts: formatDate, parseDate, DATE_FORMAT
  Re-exports from ./string.ts: capitalize, truncate, slugify
  Re-exports from ./validation.ts: isEmail, isUrl, validateSchema
  
src/utils/date.ts:
  Named Exports:
    formatDate(date: Date, format?: string): string
    parseDate(str: string): Date | null
  Constants:
    DATE_FORMAT: "YYYY-MM-DD"

src/utils/string.ts:
  Named Exports:
    capitalize(str: string): string
    truncate(str: string, maxLength: number): string
    slugify(str: string): string

src/utils/validation.ts:
  Named Exports:
    isEmail(value: string): boolean
    isUrl(value: string): boolean  
    validateSchema<T>(data: unknown, schema: Schema<T>): ValidationResult<T>

Total: 9 exported symbols across 4 files
```

**Quantified Metrics:**
| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Time to document exports | 45 min | 1 min | 45x faster |
| Files manually opened | 8 | 0 | Eliminated |
| Export completeness | ~95% | 100% | Complete |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| TypeScript | Typed superset of JavaScript |
| TSC | TypeScript Compiler |
| AST | Abstract Syntax Tree |
| SourceFile | TypeScript's file representation |
| Node | AST node type |
| Symbol | TypeScript's Symbol type |
| Declaration | Symbol definition location |
| JSDoc | JavaScript documentation comments |
| ES Module | ECMAScript module system |
| CommonJS | Node.js module system |
| Export | Module's public interface |
| Import | Module dependency |
| Decorator | Metadata annotation |
| JSX | JavaScript XML syntax |
| TSX | TypeScript with JSX |

---

## Out of Scope

The following items are explicitly excluded from Task 017.b:

- **C#/Roslyn Symbol Extraction** - Covered by Task 017.a, not duplicated here
- **Python/Go/Rust Extractors** - Future versions will add these language extractors
- **Type Checking/Validation** - Parse and extract only, no type errors reported to users
- **Code Transpilation** - No transpilation from TypeScript to JavaScript, read-only parsing
- **Module Bundling** - No webpack, rollup, or bundler integration; extraction only
- **Node.js Script Execution** - Parse syntax only, never execute user code
- **IDE Features** - No hover tooltips, go-to-definition, or refactoring; CLI extraction only
- **Source Maps** - No source map generation or consumption for mapping to original files
- **Incremental Parsing** - Initial implementation does full file parsing; incremental is future scope
- **Type Inference Engine** - No TypeScript type inference; only explicit annotations extracted

---

## Functional Requirements

### TypeScript Integration (FR-017b-01 to FR-017b-06)

| ID | Requirement |
|----|-------------|
| FR-017b-01 | System MUST initialize TypeScript compiler program |
| FR-017b-02 | System MUST parse TypeScript (.ts) files |
| FR-017b-03 | System MUST parse JavaScript (.js) files |
| FR-017b-04 | System MUST parse JSX (.jsx) and TSX (.tsx) files |
| FR-017b-05 | System MUST expose source file AST for each parsed file |
| FR-017b-06 | System MUST handle parse errors gracefully and report diagnostics |

### Symbol Discovery (FR-017b-07 to FR-017b-19)

| ID | Requirement |
|----|-------------|
| FR-017b-07 | System MUST extract class declarations |
| FR-017b-08 | System MUST extract interface declarations (TypeScript only) |
| FR-017b-09 | System MUST extract type alias declarations (TypeScript only) |
| FR-017b-10 | System MUST extract enum declarations (TypeScript only) |
| FR-017b-11 | System MUST extract function declarations |
| FR-017b-12 | System MUST extract named arrow functions |
| FR-017b-13 | System MUST extract method declarations |
| FR-017b-14 | System MUST extract property declarations |
| FR-017b-15 | System MUST extract variable/const declarations |
| FR-017b-16 | System MUST extract let declarations |
| FR-017b-17 | System MUST extract module declarations |
| FR-017b-18 | System MUST extract namespace declarations |
| FR-017b-19 | System MUST extract export declarations |

### Symbol Metadata (FR-017b-20 to FR-017b-29)

| ID | Requirement |
|----|-------------|
| FR-017b-20 | Extractor MUST capture symbol name |
| FR-017b-21 | Extractor MUST capture fully qualified name (module path + name) |
| FR-017b-22 | Extractor MUST capture symbol kind (class, function, variable, etc.) |
| FR-017b-23 | Extractor MUST capture export status (exported, default, none) |
| FR-017b-24 | Extractor MUST capture default export status |
| FR-017b-25 | Extractor MUST capture const modifier |
| FR-017b-26 | Extractor MUST capture readonly modifier |
| FR-017b-27 | Extractor MUST capture abstract modifier |
| FR-017b-28 | Extractor MUST capture async modifier |
| FR-017b-29 | Extractor MUST capture generic type parameters |

### Location Extraction (FR-017b-30 to FR-017b-35)

| ID | Requirement |
|----|-------------|
| FR-017b-30 | System MUST extract file path relative to project root |
| FR-017b-31 | System MUST extract 1-based start line number |
| FR-017b-32 | System MUST extract 1-based start column number |
| FR-017b-33 | System MUST extract 1-based end line number |
| FR-017b-34 | System MUST extract 1-based end column number |
| FR-017b-35 | System MUST handle multi-line spans correctly |

### Signature Extraction (FR-017b-36 to FR-017b-41)

| ID | Requirement |
|----|-------------|
| FR-017b-36 | Extractor MUST capture function parameters |
| FR-017b-37 | Extractor MUST capture parameter types (when available) |
| FR-017b-38 | Extractor MUST capture parameter default values |
| FR-017b-39 | Extractor MUST capture function return type |
| FR-017b-40 | Extractor MUST capture property type annotations |
| FR-017b-41 | Extractor MUST format human-readable signature strings |

### Documentation Extraction (FR-017b-42 to FR-017b-47)

| ID | Requirement |
|----|-------------|
| FR-017b-42 | Extractor MUST extract JSDoc comments attached to declarations |
| FR-017b-43 | Extractor MUST extract @param tags with name and description |
| FR-017b-44 | Extractor MUST extract @returns/@return tag |
| FR-017b-45 | Extractor MUST extract @description tag content |
| FR-017b-46 | Extractor MUST extract @example tag content |
| FR-017b-47 | Extractor MUST extract @deprecated tag presence and reason |

### Module Analysis (FR-017b-48 to FR-017b-52)

| ID | Requirement |
|----|-------------|
| FR-017b-48 | System MUST track import statements and their sources |
| FR-017b-49 | System MUST track export statements and exported symbols |
| FR-017b-50 | System MUST identify default exports |
| FR-017b-51 | System MUST identify named exports |
| FR-017b-52 | System MUST resolve relative module paths |

### Node.js Bridge (FR-017b-53 to FR-017b-57)

| ID | Requirement |
|----|-------------|
| FR-017b-53 | System MUST spawn Node.js process with bundled extractor script |
| FR-017b-54 | System MUST use JSON message protocol over stdin/stdout |
| FR-017b-55 | System MUST stream large results to avoid memory issues |
| FR-017b-56 | System MUST handle process errors and restart automatically |
| FR-017b-57 | System MUST support graceful shutdown with pending request drain |

### Extractor Interface (FR-017b-58 to FR-017b-64)

| ID | Requirement |
|----|-------------|
| FR-017b-58 | TypeScriptSymbolExtractor MUST implement ISymbolExtractor interface |
| FR-017b-59 | Extractor MUST register for .ts file extension |
| FR-017b-60 | Extractor MUST register for .js file extension |
| FR-017b-61 | Extractor MUST register for .tsx file extension |
| FR-017b-62 | Extractor MUST register for .jsx file extension |
| FR-017b-63 | ExtractAsync MUST return ExtractionResult with all discovered symbols |
| FR-017b-64 | Extractor MUST report extraction errors in result object |

---

## Non-Functional Requirements

### Performance (NFR-017b-01 to NFR-017b-06)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-01 | Performance | File parsing MUST complete in < 100ms per file (median) |
| NFR-017b-02 | Performance | Symbol extraction MUST complete in < 150ms per file (median) |
| NFR-017b-03 | Performance | Node.js bridge startup MUST complete in < 500ms |
| NFR-017b-04 | Performance | System MUST handle files up to 500KB without degradation |
| NFR-017b-05 | Performance | JSON message serialization MUST not exceed 10% of extraction time |
| NFR-017b-06 | Performance | Batch extraction of 100 files MUST complete within 15 seconds |

### Reliability (NFR-017b-07 to NFR-017b-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-07 | Reliability | System MUST handle malformed TypeScript/JavaScript without crashing |
| NFR-017b-08 | Reliability | Partial results MUST be returned when parse errors occur |
| NFR-017b-09 | Reliability | Node.js process isolation MUST prevent .NET host crashes |
| NFR-017b-10 | Reliability | Bridge MUST automatically recover from Node.js process failures |
| NFR-017b-11 | Reliability | Pending requests MUST be retried on bridge restart |
| NFR-017b-12 | Reliability | File encoding errors MUST be logged and skipped gracefully |

### Security (NFR-017b-13 to NFR-017b-16)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-13 | Security | Source file content MUST NOT be logged at INFO level |
| NFR-017b-14 | Security | File paths MUST be validated before Node.js process access |
| NFR-017b-15 | Security | No code execution (eval, require of unknown modules) MUST occur |
| NFR-017b-16 | Security | Node.js process MUST run with restricted permissions |

### Maintainability (NFR-017b-17 to NFR-017b-20)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-17 | Maintainability | TypeScript extractor MUST be bundled (no npm install at runtime) |
| NFR-017b-18 | Maintainability | Node.js version requirements MUST be documented |
| NFR-017b-19 | Maintainability | All public APIs MUST have XML documentation |
| NFR-017b-20 | Maintainability | Message protocol MUST be versioned for compatibility |

### Observability (NFR-017b-21 to NFR-017b-24)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-017b-21 | Observability | Bridge startup/shutdown events MUST be logged |
| NFR-017b-22 | Observability | Extraction duration MUST be logged per file |
| NFR-017b-23 | Observability | Parse errors MUST be logged with file path and position |
| NFR-017b-24 | Observability | Node.js process restarts MUST be logged with reason |

---

## User Manual Documentation

### Overview

TypeScript/JavaScript symbol extraction enables the Acode agent to understand web and Node.js codebases. It uses the TypeScript Compiler API to parse TypeScript (.ts, .tsx) and JavaScript (.js, .jsx) files, extracting classes, functions, interfaces, types, and other symbols along with their JSDoc documentation.

The extractor runs as a Node.js subprocess managed by the agent, communicating via JSON messages. This architecture provides complete TypeScript API access while maintaining process isolation for security and stability.

### Prerequisites

**Required:**
- Node.js 18.x or higher installed and available in PATH
- TypeScript extractor built (bundled with agent distribution)

**Verification Steps:**

```bash
# Step 1: Verify Node.js installation
node --version
# Expected output: v18.0.0 or higher

# Step 2: Verify npm is available
npm --version
# Expected output: 9.x.x or higher

# Step 3: Verify TypeScript extractor (if building from source)
cd tools/ts-extractor
npm run build
# Expected output: Build completed successfully
```

### Quick Start

**Extract symbols from a single file:**
```bash
acode symbols extract src/components/Button.tsx
```

**Extract symbols from a directory:**
```bash
acode symbols extract src/
```

**Extract with detailed statistics:**
```bash
acode symbols extract src/ --stats --verbose
```

### Configuration Reference

```yaml
# .agent/config.yml
symbol_index:
  typescript:
    # Master switch to enable/disable TypeScript extraction
    enabled: true
    
    # Include JavaScript files (.js, .mjs, .cjs)
    include_javascript: true
    
    # Include JSX/TSX files with React syntax
    include_jsx: true
    
    # Extract JSDoc comments as documentation
    extract_jsdoc: true
    
    # Include private/protected members in output
    include_private_members: false
    
    # Maximum nesting depth for symbol hierarchy
    max_nesting_depth: 10
    
    # Timeout for individual file extraction (seconds)
    request_timeout_seconds: 30
    
    # Maximum file size to process (KB)
    max_file_size_kb: 500
    
    # Node.js process memory limit (MB)
    node_max_memory_mb: 512
    
    # Explicit path to Node.js (optional)
    node_path: null
    
    # File patterns to exclude from extraction
    exclude_patterns:
      - "**/node_modules/**"
      - "**/dist/**"
      - "**/build/**"
      - "**/*.d.ts"
      - "**/*.min.js"
      - "**/*.bundle.js"
    
    # File extensions to include
    include_extensions:
      - ".ts"
      - ".tsx"
      - ".js"
      - ".jsx"
      - ".mts"
      - ".cts"
      - ".mjs"
      - ".cjs"
```

### Configuration Options Table

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `enabled` | boolean | `true` | Enable TypeScript/JavaScript extraction |
| `include_javascript` | boolean | `true` | Process .js/.mjs/.cjs files |
| `include_jsx` | boolean | `true` | Process .jsx/.tsx files |
| `extract_jsdoc` | boolean | `true` | Extract JSDoc documentation |
| `include_private_members` | boolean | `false` | Include private/protected symbols |
| `max_nesting_depth` | integer | `10` | Maximum symbol hierarchy depth |
| `request_timeout_seconds` | integer | `30` | Per-file extraction timeout |
| `max_file_size_kb` | integer | `500` | Skip files larger than this |
| `node_max_memory_mb` | integer | `512` | Node.js memory limit |
| `node_path` | string | `null` | Explicit Node.js path |
| `exclude_patterns` | array | See above | Glob patterns to exclude |
| `include_extensions` | array | See above | File extensions to process |

### CLI Output Example

**ASCII Console Output Mockup:**

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Acode TypeScript/JavaScript Symbol Extractor                               │
├─────────────────────────────────────────────────────────────────────────────┤
│  Target: src/                                                               │
│  Mode: Full extraction with JSDoc                                           │
└─────────────────────────────────────────────────────────────────────────────┘

[====================================] 100% | 47/47 files processed

╔═════════════════════════════════════════════════════════════════════════════╗
║  EXTRACTION SUMMARY                                                         ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  Files Processed:     47                                                    ║
║  Files Skipped:       12 (excluded by pattern)                              ║
║  Files Failed:         0                                                    ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  SYMBOL BREAKDOWN                                                           ║
╠═══════════════════╦═════════╦═══════════════════════════════════════════════╣
║  Classes          ║     23  ║  ████████████████████░░░░░░░░░░░  15%        ║
║  Interfaces       ║     45  ║  ██████████████████████████████░░  29%        ║
║  Functions        ║     34  ║  ██████████████████████████░░░░░░  22%        ║
║  Methods          ║     67  ║  ████████████████████████████████  44%        ║
║  Properties       ║     89  ║  ████████████████████████████████  58%        ║
║  Variables        ║     28  ║  ████████████████████░░░░░░░░░░░░  18%        ║
║  Type Aliases     ║     19  ║  █████████████░░░░░░░░░░░░░░░░░░░  12%        ║
║  Enums            ║      6  ║  ████░░░░░░░░░░░░░░░░░░░░░░░░░░░░   4%        ║
╠═══════════════════╩═════════╩═══════════════════════════════════════════════╣
║  Total Symbols:   311                                                       ║
║  With JSDoc:      187 (60%)                                                 ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  PERFORMANCE                                                                ║
╠═════════════════════════════════════════════════════════════════════════════╣
║  Total Time:      2.34 seconds                                              ║
║  Parse Time:      1.52 seconds (65%)                                        ║
║  Extract Time:    0.82 seconds (35%)                                        ║
║  Avg per File:    49.8 ms                                                   ║
║  Peak Memory:     127 MB                                                    ║
╚═════════════════════════════════════════════════════════════════════════════╝

✓ Extraction complete. Symbols indexed for query.
```

### Symbol Output Format

Each extracted symbol produces a JSON structure with the following fields:

```json
{
  "id": "f1e2d3c4-b5a6-7890-1234-567890abcdef",
  "name": "UserService",
  "fullyQualifiedName": "src/services/UserService.UserService",
  "kind": "Class",
  "visibility": "Exported",
  "location": {
    "filePath": "src/services/UserService.ts",
    "startLine": 5,
    "startColumn": 1,
    "endLine": 45,
    "endColumn": 2
  },
  "modifiers": ["export", "class"],
  "signature": "class UserService extends BaseService implements IUserService",
  "documentation": {
    "description": "Handles user-related operations.",
    "parameters": {},
    "returns": null,
    "examples": ["const service = new UserService();"],
    "deprecated": false
  },
  "children": [
    {
      "name": "getUser",
      "kind": "Method",
      "signature": "async getUser(id: string): Promise<User>"
    }
  ]
}
```

### Supported Constructs

| Construct | TypeScript | JavaScript | Notes |
|-----------|:----------:|:----------:|-------|
| Class | ✅ | ✅ | Full metadata with heritage |
| Abstract Class | ✅ | ❌ | TypeScript only |
| Interface | ✅ | ❌ | TypeScript only |
| Type Alias | ✅ | ❌ | TypeScript only |
| Function | ✅ | ✅ | With full signature |
| Arrow Function | ✅ | ✅ | Named assignments only |
| Method | ✅ | ✅ | With signature |
| Property | ✅ | ✅ | With type annotation |
| Variable (const) | ✅ | ✅ | Named exports tracked |
| Variable (let/var) | ✅ | ✅ | Named exports tracked |
| Enum | ✅ | ❌ | With member values |
| Const Enum | ✅ | ❌ | Inlined values |
| Namespace | ✅ | ❌ | TypeScript only |
| Module | ✅ | ❌ | TypeScript only |
| Decorator | ✅ | ❌ | Experimental |
| Get Accessor | ✅ | ✅ | Property-like |
| Set Accessor | ✅ | ✅ | Property-like |
| Constructor | ✅ | ✅ | Parameter properties |
| Index Signature | ✅ | ❌ | TypeScript only |
| Generic Type Param | ✅ | ❌ | With constraints |
| Import | ✅ | ✅ | Source tracking |
| Export | ✅ | ✅ | Named, default, re-export |

### CLI Commands

**Basic Extraction:**
```bash
# Extract symbols from a single TypeScript file
acode symbols extract src/services/UserService.ts

# Extract from a JavaScript file
acode symbols extract src/utils/helpers.js

# Extract from entire directory
acode symbols extract src/
```

**Advanced Options:**
```bash
# Extract with statistics output
acode symbols extract src/ --stats

# Extract with verbose logging
acode symbols extract src/ --verbose

# Extract without JSDoc (faster)
acode symbols extract src/ --no-jsdoc

# Extract including private members
acode symbols extract src/ --include-private

# Extract with JSON output
acode symbols extract src/ --output json > symbols.json

# Extract with custom timeout
acode symbols extract src/ --timeout 60
```

**Query Commands:**
```bash
# Find symbols by name
acode symbols find UserService

# Find all classes
acode symbols find --kind class

# Find exported functions
acode symbols find --kind function --exported
```

### Integration with Symbol Index

Extracted TypeScript/JavaScript symbols are automatically indexed by Task 017's Symbol Index. Once indexed, symbols can be queried for:

1. **Code Navigation** - Finding symbol definitions
2. **Context Generation** - Providing relevant code context to LLM
3. **Dependency Analysis** - Understanding import/export relationships
4. **Refactoring Support** - Finding all usages of a symbol

---

## Acceptance Criteria

### TypeScript Compiler Integration (AC-001 to AC-008)

- [ ] AC-001: `ts.createSourceFile()` successfully parses TypeScript files
- [ ] AC-002: `ts.createSourceFile()` successfully parses JavaScript files
- [ ] AC-003: TSX files parsed with JSX syntax support enabled
- [ ] AC-004: JSX files parsed with JSX syntax support enabled
- [ ] AC-005: Parse errors are captured and returned in diagnostics
- [ ] AC-006: tsconfig.json options are applied when file is present
- [ ] AC-007: Default compiler options work when tsconfig.json is missing
- [ ] AC-008: File encoding is handled correctly (UTF-8, UTF-16)

### Symbol Extraction - Types (AC-009 to AC-018)

- [ ] AC-009: Class declarations extracted with name and location
- [ ] AC-010: Abstract class modifier captured
- [ ] AC-011: Interface declarations extracted (TypeScript only)
- [ ] AC-012: Type alias declarations extracted (TypeScript only)
- [ ] AC-013: Enum declarations extracted with members
- [ ] AC-014: Module/namespace declarations extracted
- [ ] AC-015: Generic type parameters captured with constraints
- [ ] AC-016: Class heritage (extends, implements) captured
- [ ] AC-017: Decorator metadata captured when present
- [ ] AC-018: Record and tuple types handled (TypeScript 4.0+)

### Symbol Extraction - Members (AC-019 to AC-028)

- [ ] AC-019: Function declarations extracted with signature
- [ ] AC-020: Arrow function assignments extracted (named only)
- [ ] AC-021: Method declarations extracted from classes
- [ ] AC-022: Property declarations extracted with types
- [ ] AC-023: Constructor declarations extracted
- [ ] AC-024: Getter and setter accessors extracted
- [ ] AC-025: Static members flagged correctly
- [ ] AC-026: Readonly properties captured
- [ ] AC-027: Optional properties captured (? modifier)
- [ ] AC-028: Private/protected modifiers captured (TypeScript)

### Symbol Extraction - Variables (AC-029 to AC-034)

- [ ] AC-029: Const declarations extracted
- [ ] AC-030: Let declarations extracted
- [ ] AC-031: Var declarations extracted
- [ ] AC-032: Destructured variable names captured
- [ ] AC-033: Variable type annotations captured
- [ ] AC-034: Variable initializer types inferred when no annotation

### Export/Import Tracking (AC-035 to AC-042)

- [ ] AC-035: Named exports captured with symbol reference
- [ ] AC-036: Default exports captured with symbol reference
- [ ] AC-037: Re-exports captured (export from)
- [ ] AC-038: Export all captured (export * from)
- [ ] AC-039: Import statements tracked with sources
- [ ] AC-040: CommonJS require() calls tracked
- [ ] AC-041: CommonJS module.exports captured
- [ ] AC-042: Dynamic import() statements tracked

### JSDoc Extraction (AC-043 to AC-050)

- [ ] AC-043: @description tag extracted
- [ ] AC-044: @param tags extracted with name and description
- [ ] AC-045: @returns/@return tag extracted
- [ ] AC-046: @type tag extracted for type inference
- [ ] AC-047: @typedef tag extracted for type definitions
- [ ] AC-048: @example tag extracted with code
- [ ] AC-049: @deprecated tag presence and reason captured
- [ ] AC-050: @throws/@exception tags extracted

### Node.js Bridge (AC-051 to AC-060)

- [ ] AC-051: Node.js process starts successfully
- [ ] AC-052: JSON messages sent over stdin correctly
- [ ] AC-053: JSON responses read from stdout correctly
- [ ] AC-054: Large messages (>1MB) handled via streaming
- [ ] AC-055: Process crash detected and auto-restart triggered
- [ ] AC-056: Request timeout causes process recycle
- [ ] AC-057: Graceful shutdown drains pending requests
- [ ] AC-058: Memory limit enforced via --max-old-space-size
- [ ] AC-059: Process recycled after configurable request count
- [ ] AC-060: Process recycled after configurable lifetime

### ISymbolExtractor Interface (AC-061 to AC-066)

- [ ] AC-061: `TypeScriptSymbolExtractor` implements `ISymbolExtractor`
- [ ] AC-062: `Language` property returns "typescript"
- [ ] AC-063: `FileExtensions` returns [".ts", ".tsx", ".js", ".jsx"]
- [ ] AC-064: `ExtractAsync()` returns `ExtractionResult` with symbols
- [ ] AC-065: Extractor registered in `IExtractorRegistry` on startup
- [ ] AC-066: Multiple concurrent extract calls handled correctly

### Error Handling (AC-067 to AC-072)

- [ ] AC-067: File not found returns error code ACODE-TSE-002
- [ ] AC-068: Parse errors return partial results with diagnostics
- [ ] AC-069: Node.js not found returns error code ACODE-TSE-003
- [ ] AC-070: Bridge timeout returns error code ACODE-TSE-004
- [ ] AC-071: Security violations return error code ACODE-TSE-005
- [ ] AC-072: All errors include sanitized messages (no internal paths)

---

## Best Practices

### Tree-sitter Integration

1. **Robust parsing** - Tree-sitter handles syntax errors gracefully
2. **Grammar maintenance** - Keep JavaScript/TypeScript grammars updated
3. **Efficient queries** - Use tree-sitter queries for pattern matching
4. **Language detection** - Distinguish JS, TS, JSX, TSX correctly

### JavaScript/TypeScript Specifics

5. **Handle module systems** - CommonJS, ESM, AMD imports
6. **Extract JSDoc comments** - Use for type inference in JS
7. **Track exports** - Named, default, and re-exports
8. **Handle dynamic patterns** - Best-effort for computed properties

### Performance

9. **Process pools** - Worker processes for parallel parsing
10. **Skip node_modules** - Ignore by default, opt-in for specific packages
11. **Graceful shutdown** - Clean up worker processes properly
12. **Incremental updates** - Only re-parse changed files

---

## Assumptions

This section documents the technical, operational, and integration assumptions made for Task 017.b. If any assumption proves invalid, the implementation approach may need revision.

### Technical Assumptions

1. **Node.js 18+ Availability** - Node.js version 18 or higher is installed and accessible in the system PATH on all target deployment environments.

2. **TypeScript Compiler API Stability** - The TypeScript Compiler API (`typescript` npm package version 5.x) maintains backward compatibility for `ts.createSourceFile()`, `ts.forEachChild()`, and related AST functions.

3. **UTF-8 Source Files** - All TypeScript and JavaScript source files use UTF-8 encoding. Other encodings (UTF-16, Latin-1) are not supported without explicit configuration.

4. **JSDoc Standard Compliance** - JSDoc comments follow the standard JSDoc3 format. Custom tags beyond the standard set (`@param`, `@returns`, `@description`, `@example`, `@deprecated`, `@throws`) are extracted as raw text.

5. **ES Module and CommonJS Support** - Both ES module syntax (`import`/`export`) and CommonJS syntax (`require`/`module.exports`) are present in target codebases and must both be extracted correctly.

6. **React JSX/TSX Patterns** - React component patterns (function components, class components, hooks) follow community conventions and can be identified via standard heuristics.

7. **File Size Limits** - Individual TypeScript/JavaScript files do not exceed 1 MB. Files larger than 1 MB receive a warning but attempt extraction with increased timeout.

### Operational Assumptions

8. **Process Memory Availability** - The Node.js subprocess can allocate up to 512 MB of memory (configurable). Systems with less available memory may experience reduced performance.

9. **File System Access** - The agent has read access to all TypeScript/JavaScript files in the workspace. Files with restricted permissions are skipped with appropriate error messages.

10. **Temporary File Space** - Sufficient disk space exists for TypeScript Compiler API caching operations, typically less than 100 MB per workspace.

11. **No Concurrent Modifications** - Source files are not modified during extraction. If a file changes mid-extraction, partial or inconsistent results may occur.

12. **Single Workspace Context** - Each extraction operation runs in the context of a single workspace. Cross-workspace symbol references are not resolved.

### Integration Assumptions

13. **Symbol Index Availability** - Task 017 (Symbol Index v2) is complete and provides the `ISymbolIndex` interface for storing extracted symbols.

14. **Logging Infrastructure** - The standard Acode logging infrastructure (`Microsoft.Extensions.Logging`) is available and configured for the `Acode.TypeScript.*` namespaces.

15. **Configuration System** - The Acode configuration system provides access to `symbol_index.typescript.*` settings via the standard configuration providers.

16. **Error Reporting Integration** - Extraction errors are reported through the standard `SymbolExtractionError` type, which integrates with the error aggregation system.

17. **Cancellation Token Propagation** - Calling code properly propagates `CancellationToken` for all extraction operations, enabling clean shutdown and request cancellation.

18. **Health Check Integration** - The Node.js bridge health status is exposed via the standard health check system for monitoring and alerting.

---

## Troubleshooting

This section provides diagnosis and resolution steps for common issues with TypeScript/JavaScript symbol extraction.

---

### Issue 1: Node.js Bridge Fails to Start

**Symptoms:**
- Error message: `ACODE-TSE-003: Node.js not found`
- Extraction commands hang indefinitely
- Log entry: `Failed to start Node.js bridge process`

**Possible Causes:**
1. Node.js is not installed on the system
2. Node.js is installed but not in the system PATH
3. Node.js version is below 18.x
4. ts-extractor package not built (`npm run build` not executed)
5. Insufficient permissions to execute node.exe

**Solutions:**

1. **Verify Node.js Installation:**
```powershell
# Check if Node.js is installed and version
node --version
# Expected output: v18.0.0 or higher

# If not found, install from https://nodejs.org/
```

2. **Add Node.js to PATH:**
```powershell
# Find Node.js installation
where node

# If empty, add to PATH:
$env:Path += ";C:\Program Files\nodejs"
[Environment]::SetEnvironmentVariable("Path", $env:Path, "User")
```

3. **Build ts-extractor:**
```powershell
cd tools/ts-extractor
npm install
npm run build
```

4. **Configure Explicit Node Path:**
```yaml
# .agent/config.yml
typescript:
  node_path: "C:\\Program Files\\nodejs\\node.exe"
```

---

### Issue 2: Symbols Missing from Extraction Results

**Symptoms:**
- Expected classes/functions not appearing in symbol list
- Partial extraction with fewer symbols than expected
- No errors reported but symbols incomplete

**Possible Causes:**
1. File extension not included in extraction patterns
2. Symbols in node_modules which is excluded by default
3. Syntax errors in source file causing parse failure
4. Private members excluded by configuration
5. File excluded by `.gitignore` patterns

**Solutions:**

1. **Verify File Extensions:**
```yaml
# .agent/config.yml
symbol_index:
  typescript:
    include_extensions:
      - ".ts"
      - ".tsx"
      - ".js"
      - ".jsx"
      - ".mts"
      - ".cts"
```

2. **Check Exclude Patterns:**
```yaml
# Ensure desired files are not excluded
symbol_index:
  typescript:
    exclude_patterns:
      - "**/node_modules/**"
      - "**/dist/**"
      # Remove patterns that exclude your files
```

3. **Enable Private Member Extraction:**
```yaml
symbol_index:
  typescript:
    include_private_members: true
```

4. **Check for Syntax Errors:**
```powershell
# Run TypeScript compiler to check for errors
npx tsc --noEmit src/problematic-file.ts
```

---

### Issue 3: Extraction is Extremely Slow

**Symptoms:**
- Extraction takes more than 10 seconds per file
- Node.js process consumes 100% CPU
- Memory usage grows continuously
- Timeout errors for large files

**Possible Causes:**
1. Very large files (>500KB) overwhelming the parser
2. Deeply nested type definitions causing stack overflow
3. Circular type references creating infinite loops
4. node_modules included in extraction
5. .d.ts declaration files included (often very large)

**Solutions:**

1. **Exclude node_modules and Declaration Files:**
```yaml
symbol_index:
  typescript:
    exclude_patterns:
      - "**/node_modules/**"
      - "**/*.d.ts"
      - "**/dist/**"
      - "**/*.min.js"
```

2. **Increase Timeout for Large Files:**
```yaml
symbol_index:
  typescript:
    request_timeout_seconds: 60
    max_file_size_kb: 500
```

3. **Enable Performance Logging:**
```yaml
logging:
  Acode.TypeScript.Performance: Information
```

4. **Profile Specific Files:**
```powershell
acode symbols extract src/large-file.ts --stats --verbose
```

---

### Issue 4: JSDoc Documentation Not Extracted

**Symptoms:**
- Symbols extracted but `documentation` field is empty
- `@param` and `@returns` not appearing
- JSDoc comments ignored

**Possible Causes:**
1. JSDoc extraction disabled in configuration
2. Non-standard JSDoc format (e.g., different comment style)
3. JSDoc comment not immediately preceding declaration
4. Syntax errors in JSDoc comment
5. Using TypeDoc-specific tags not recognized by extractor

**Solutions:**

1. **Enable JSDoc Extraction:**
```yaml
symbol_index:
  typescript:
    extract_jsdoc: true
```

2. **Verify JSDoc Format:**
```typescript
// Correct format - JSDoc must directly precede declaration
/**
 * Calculates the sum of two numbers.
 * @param a - First number
 * @param b - Second number
 * @returns The sum of a and b
 */
function add(a: number, b: number): number {
    return a + b;
}

// Incorrect - blank line between JSDoc and function
/**
 * This comment will NOT be associated with the function
 */

function orphanedComment() {}
```

3. **Check Supported Tags:**
Supported: `@param`, `@returns`, `@return`, `@description`, `@example`, `@deprecated`, `@throws`, `@type`, `@typedef`, `@see`, `@since`, `@author`

---

### Issue 5: Node.js Bridge Crashes Repeatedly

**Symptoms:**
- Log entries: `Node.js bridge process exited unexpectedly`
- Error: `ACODE-TSE-014: Max restart attempts exceeded`
- Extraction works for first few files then fails
- Memory-related crash messages

**Possible Causes:**
1. Memory exhaustion from large codebase
2. Stack overflow from deeply nested structures
3. Bug in TypeScript Compiler API for edge cases
4. Corrupted Node.js installation
5. Incompatible TypeScript version

**Solutions:**

1. **Increase Memory Limit:**
```yaml
symbol_index:
  typescript:
    node_max_memory_mb: 1024  # Increase from default 512
```

2. **Limit Nesting Depth:**
```yaml
symbol_index:
  typescript:
    max_nesting_depth: 20  # Limit recursive parsing
```

3. **Enable Crash Diagnostics:**
```powershell
# Run with verbose logging
acode symbols extract . --log-level trace

# Check Node.js stderr output
```

4. **Reinstall Dependencies:**
```powershell
cd tools/ts-extractor
rm -rf node_modules
npm install
npm run build
```

5. **Verify TypeScript Version:**
```powershell
cd tools/ts-extractor
npm ls typescript
# Should show typescript@5.x.x
```

---

## Security Threats and Mitigations

### Threat 1: JSON Injection via Malicious File Paths

**Threat ID:** THREAT-017b-001  
**Severity:** HIGH  
**Attack Vector:** Attacker creates file with path containing JSON control characters to inject commands into the Node.js bridge  
**Impact:** Arbitrary command execution in Node.js subprocess, information disclosure

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgenticCoder.Infrastructure.Symbols.TypeScript.Security;

/// <summary>
/// Sanitizes JSON messages sent to the Node.js bridge to prevent injection.
/// </summary>
public static class JsonMessageSanitizer
{
    private static readonly Regex UnsafePathPattern = new(
        @"[\x00-\x1f""\\]",
        RegexOptions.Compiled);
    
    /// <summary>
    /// Sanitizes a file path before including in JSON message.
    /// </summary>
    public static string SanitizeFilePath(string filePath, string workspaceRoot)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(workspaceRoot);
        
        // Normalize and validate path is within workspace
        string normalizedPath = Path.GetFullPath(filePath);
        string normalizedRoot = Path.GetFullPath(workspaceRoot);
        
        if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar))
        {
            normalizedRoot += Path.DirectorySeparatorChar;
        }
        
        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException(
                $"Path '{filePath}' is outside allowed workspace");
        }
        
        // Check for control characters that could break JSON
        if (UnsafePathPattern.IsMatch(normalizedPath))
        {
            throw new SecurityException(
                $"Path contains unsafe characters for JSON encoding");
        }
        
        return normalizedPath;
    }
    
    /// <summary>
    /// Creates a safe JSON request message.
    /// </summary>
    public static string CreateSafeRequest(ExtractionRequest request, string workspaceRoot)
    {
        // Validate and sanitize the file path
        var safePath = SanitizeFilePath(request.FilePath, workspaceRoot);
        
        var safeRequest = new
        {
            type = "extract",
            filePath = safePath,
            options = new
            {
                includeJSDoc = request.IncludeJSDoc,
                includePrivate = request.IncludePrivate
            }
        };
        
        // Use strict JSON serialization
        return JsonSerializer.Serialize(safeRequest, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
}
```

---

### Threat 2: Node.js Process Resource Exhaustion

**Threat ID:** THREAT-017b-002  
**Severity:** HIGH  
**Attack Vector:** Large or deeply nested TypeScript file causes Node.js to exhaust memory/CPU  
**Impact:** Denial of service, agent becomes unresponsive

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AgenticCoder.Infrastructure.Symbols.TypeScript.Security;

/// <summary>
/// Manages Node.js process lifecycle with resource limits.
/// </summary>
public sealed class SecureNodeBridge : IAsyncDisposable
{
    private readonly ProcessResourceLimits _limits;
    private readonly ILogger<SecureNodeBridge> _logger;
    private Process? _process;
    private readonly SemaphoreSlim _processLock = new(1, 1);
    private int _requestCount;
    private DateTime _startTime;
    
    public SecureNodeBridge(ProcessResourceLimits limits, ILogger<SecureNodeBridge> logger)
    {
        _limits = limits;
        _logger = logger;
    }
    
    /// <summary>
    /// Starts the Node.js process with resource limits.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        await _processLock.WaitAsync(ct);
        try
        {
            if (_process != null && !_process.HasExited)
            {
                return; // Already running
            }
            
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = GetArgumentsWithLimits(),
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    // Set working directory to prevent path traversal
                    WorkingDirectory = _limits.WorkingDirectory
                },
                EnableRaisingEvents = true
            };
            
            _process.Exited += OnProcessExited;
            _process.ErrorDataReceived += OnErrorDataReceived;
            
            _process.Start();
            _process.BeginErrorReadLine();
            _startTime = DateTime.UtcNow;
            _requestCount = 0;
            
            _logger.LogInformation(
                "Started Node.js bridge process (PID: {Pid})", 
                _process.Id);
        }
        finally
        {
            _processLock.Release();
        }
    }
    
    private string GetArgumentsWithLimits()
    {
        // Apply memory limit via Node.js flag
        var maxMemoryMB = _limits.MaxMemoryMB;
        return $"--max-old-space-size={maxMemoryMB} " +
               $"--max-semi-space-size={maxMemoryMB / 4} " +
               "ts-extractor/dist/index.js";
    }
    
    /// <summary>
    /// Sends extraction request with timeout and process health checks.
    /// </summary>
    public async Task<ExtractionResponse> SendAsync(
        string jsonRequest, 
        CancellationToken ct = default)
    {
        await EnsureHealthyProcessAsync(ct);
        
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_limits.RequestTimeout);
        
        try
        {
            // Send request
            await _process!.StandardInput.WriteLineAsync(jsonRequest);
            
            // Read response with timeout
            var response = await _process.StandardOutput.ReadLineAsync(timeoutCts.Token);
            
            if (string.IsNullOrEmpty(response))
            {
                throw new NodeBridgeException("Empty response from Node.js process");
            }
            
            Interlocked.Increment(ref _requestCount);
            
            return JsonSerializer.Deserialize<ExtractionResponse>(response)
                ?? throw new NodeBridgeException("Failed to deserialize response");
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            _logger.LogWarning("Node.js request timed out, recycling process");
            await RecycleProcessAsync();
            throw new NodeBridgeException($"Request timed out after {_limits.RequestTimeout}");
        }
    }
    
    private async Task EnsureHealthyProcessAsync(CancellationToken ct)
    {
        // Check if process needs recycling
        bool needsRecycle = _process == null ||
                            _process.HasExited ||
                            _requestCount >= _limits.MaxRequestsPerProcess ||
                            (DateTime.UtcNow - _startTime) > _limits.MaxProcessLifetime;
        
        if (needsRecycle)
        {
            await RecycleProcessAsync();
            await StartAsync(ct);
        }
    }
    
    private async Task RecycleProcessAsync()
    {
        if (_process != null && !_process.HasExited)
        {
            _logger.LogInformation("Recycling Node.js bridge process");
            
            try
            {
                // Graceful shutdown
                _process.StandardInput.WriteLine(@"{""type"":""shutdown""}");
                
                if (!_process.WaitForExit(5000))
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during process recycle");
            }
            
            _process.Dispose();
            _process = null;
        }
    }
    
    private void OnProcessExited(object? sender, EventArgs e)
    {
        _logger.LogWarning(
            "Node.js bridge process exited unexpectedly (code: {ExitCode})",
            _process?.ExitCode);
    }
    
    private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger.LogWarning("Node.js stderr: {Message}", e.Data);
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await RecycleProcessAsync();
        _processLock.Dispose();
    }
}

public record ProcessResourceLimits
{
    public int MaxMemoryMB { get; init; } = 512;
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(30);
    public int MaxRequestsPerProcess { get; init; } = 1000;
    public TimeSpan MaxProcessLifetime { get; init; } = TimeSpan.FromHours(1);
    public required string WorkingDirectory { get; init; }
}

public class NodeBridgeException : Exception
{
    public NodeBridgeException(string message) : base(message) { }
    public NodeBridgeException(string message, Exception inner) : base(message, inner) { }
}
```

---

### Threat 3: Arbitrary Code Execution in Node.js

**Threat ID:** THREAT-017b-003  
**Severity:** CRITICAL  
**Attack Vector:** Malicious TypeScript file contains code that executes during "parsing" via eval, require, or dynamic import  
**Impact:** Full system compromise via code execution in Node.js process

**Mitigation - Complete TypeScript Implementation (Node.js side):**

```typescript
// tools/ts-extractor/src/security.ts
import * as ts from 'typescript';
import * as path from 'path';
import * as fs from 'fs';

/**
 * Security policy enforcing parse-only operations.
 * CRITICAL: This module MUST NOT use eval(), require() with dynamic paths,
 * or any code execution mechanisms.
 */

/**
 * Validates and normalizes a file path to prevent traversal attacks.
 */
export function validateFilePath(filePath: string, workspaceRoot: string): string {
    const normalizedPath = path.resolve(filePath);
    const normalizedRoot = path.resolve(workspaceRoot);
    
    if (!normalizedPath.startsWith(normalizedRoot + path.sep)) {
        throw new SecurityError(`Path traversal detected: ${filePath}`);
    }
    
    // Check file exists and is readable
    if (!fs.existsSync(normalizedPath)) {
        throw new SecurityError(`File not found: ${filePath}`);
    }
    
    return normalizedPath;
}

/**
 * Creates a secure TypeScript source file for parsing only.
 * NO CODE EXECUTION occurs.
 */
export function createSecureSourceFile(
    filePath: string,
    workspaceRoot: string
): ts.SourceFile {
    const safePath = validateFilePath(filePath, workspaceRoot);
    
    // Read file content directly - NO dynamic require()
    const content = fs.readFileSync(safePath, 'utf-8');
    
    // Create source file for PARSING ONLY
    // ts.createSourceFile DOES NOT execute any code
    const sourceFile = ts.createSourceFile(
        safePath,
        content,
        ts.ScriptTarget.Latest,
        /* setParentNodes */ true,
        getScriptKind(safePath)
    );
    
    return sourceFile;
}

function getScriptKind(filePath: string): ts.ScriptKind {
    const ext = path.extname(filePath).toLowerCase();
    switch (ext) {
        case '.ts': return ts.ScriptKind.TS;
        case '.tsx': return ts.ScriptKind.TSX;
        case '.js': return ts.ScriptKind.JS;
        case '.jsx': return ts.ScriptKind.JSX;
        default: return ts.ScriptKind.Unknown;
    }
}

/**
 * SECURITY INVARIANTS - These MUST be maintained:
 * 
 * 1. NEVER use eval() or new Function()
 * 2. NEVER use require() with user-provided paths
 * 3. NEVER use import() with dynamic paths
 * 4. NEVER call ts.transpile() or ts.emit()
 * 5. ONLY use ts.createSourceFile() for parsing
 * 6. ONLY use ts.forEachChild() for AST walking
 * 7. NEVER execute code from parsed files
 */

export class SecurityError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'SecurityError';
    }
}
```

---

### Threat 4: Information Disclosure via Error Messages

**Threat ID:** THREAT-017b-004  
**Severity:** MEDIUM  
**Attack Vector:** Error messages from Node.js process reveal internal paths, stack traces, or system information  
**Impact:** Information useful for further attacks

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AgenticCoder.Infrastructure.Symbols.TypeScript.Security;

/// <summary>
/// Sanitizes responses from Node.js to prevent information disclosure.
/// </summary>
public class SecureResponseHandler
{
    private readonly ILogger<SecureResponseHandler> _logger;
    private readonly string _workspaceRoot;
    
    public SecureResponseHandler(ILogger<SecureResponseHandler> logger, string workspaceRoot)
    {
        _logger = logger;
        _workspaceRoot = workspaceRoot;
    }
    
    /// <summary>
    /// Processes and sanitizes extraction response from Node.js.
    /// </summary>
    public ExtractionResult ProcessResponse(string jsonResponse, string originalPath)
    {
        try
        {
            var response = JsonSerializer.Deserialize<NodeExtractionResponse>(jsonResponse);
            
            if (response == null)
            {
                return CreateErrorResult(originalPath, "ACODE-TSE-004", "Invalid response");
            }
            
            if (response.Error != null)
            {
                // Log full error internally
                _logger.LogError(
                    "Node.js extraction error for {FilePath}: {Error}",
                    originalPath,
                    response.Error);
                
                // Return sanitized error externally
                return CreateErrorResult(
                    originalPath,
                    MapErrorCode(response.ErrorCode),
                    SanitizeErrorMessage(response.Error));
            }
            
            // Sanitize file paths in symbols
            var sanitizedSymbols = response.Symbols
                .Select(s => SanitizeSymbol(s))
                .ToList();
            
            return new ExtractionResult
            {
                FilePath = GetRelativePath(originalPath),
                Symbols = sanitizedSymbols,
                Statistics = response.Statistics
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Node.js response");
            return CreateErrorResult(originalPath, "ACODE-TSE-004", "Parse error");
        }
    }
    
    private string SanitizeErrorMessage(string error)
    {
        // Remove absolute paths
        var sanitized = error.Replace(_workspaceRoot, ".");
        
        // Remove stack traces
        var stackIndex = sanitized.IndexOf("    at ", StringComparison.Ordinal);
        if (stackIndex > 0)
        {
            sanitized = sanitized.Substring(0, stackIndex).Trim();
        }
        
        // Remove Node.js internal paths
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"/usr/.*?node_modules/.*?:",
            "");
        
        return sanitized;
    }
    
    private ISymbol SanitizeSymbol(NodeSymbol nodeSymbol)
    {
        return new Symbol
        {
            Id = nodeSymbol.Id,
            Name = nodeSymbol.Name,
            Kind = MapSymbolKind(nodeSymbol.Kind),
            Location = new SymbolLocation
            {
                // Use relative path only
                FilePath = GetRelativePath(nodeSymbol.FilePath),
                StartLine = nodeSymbol.StartLine,
                EndLine = nodeSymbol.EndLine,
                StartColumn = nodeSymbol.StartColumn,
                EndColumn = nodeSymbol.EndColumn
            },
            Visibility = MapVisibility(nodeSymbol.Visibility),
            Signature = nodeSymbol.Signature,
            Documentation = MapDocumentation(nodeSymbol.JsDoc)
        };
    }
    
    private string GetRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(_workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            return absolutePath.Substring(_workspaceRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar);
        }
        return Path.GetFileName(absolutePath);
    }
    
    private string MapErrorCode(string? nodeErrorCode)
    {
        return nodeErrorCode switch
        {
            "PARSE_ERROR" => "ACODE-TSE-001",
            "FILE_NOT_FOUND" => "ACODE-TSE-002",
            "SECURITY_ERROR" => "ACODE-TSE-005",
            _ => "ACODE-TSE-004"
        };
    }
    
    private ExtractionResult CreateErrorResult(string path, string code, string message)
    {
        return new ExtractionResult
        {
            FilePath = GetRelativePath(path),
            Symbols = Array.Empty<ISymbol>(),
            Errors = new[]
            {
                new ExtractionError { Code = code, Message = message }
            },
            IsPartial = true
        };
    }
}
```

---

### Threat 5: Malformed JSON Response from Node.js

**Threat ID:** THREAT-017b-005  
**Severity:** MEDIUM  
**Attack Vector:** Corrupted or maliciously crafted JSON response from Node.js subprocess could cause parsing exceptions, buffer overflows, or deserialization vulnerabilities  
**Impact:** Agent crash, denial of service, potential code execution via deserialization gadgets

**Mitigation - Complete C# Implementation:**

```csharp
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.TypeScript.Security;

/// <summary>
/// Secure JSON response parser with size limits, validation, and safe deserialization.
/// </summary>
public sealed class SecureJsonParser
{
    private readonly ILogger<SecureJsonParser> _logger;
    private readonly JsonSerializerOptions _options;
    
    // Security limits
    private const int MaxResponseSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxStringLength = 1 * 1024 * 1024; // 1 MB per string
    private const int MaxDepth = 64;
    private const int MaxSymbolCount = 50_000;
    
    public SecureJsonParser(ILogger<SecureJsonParser> logger)
    {
        _logger = logger;
        
        _options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = MaxDepth,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            // Prevent object reference loops
            ReferenceHandler = null,
            // Use strict number handling
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.Strict
        };
    }
    
    /// <summary>
    /// Safely parses JSON response from Node.js with all security checks.
    /// </summary>
    public ParseResult<ExtractionResponse> ParseResponse(string jsonResponse)
    {
        // 1. Check for null/empty
        if (string.IsNullOrWhiteSpace(jsonResponse))
        {
            _logger.LogWarning("Received empty response from Node.js");
            return ParseResult<ExtractionResponse>.Failure("ACODE-TSE-020", "Empty response");
        }
        
        // 2. Check size limit
        if (jsonResponse.Length > MaxResponseSizeBytes)
        {
            _logger.LogWarning(
                "Response exceeds size limit: {Size} > {MaxSize}",
                jsonResponse.Length,
                MaxResponseSizeBytes);
            return ParseResult<ExtractionResponse>.Failure(
                "ACODE-TSE-021",
                $"Response too large: {jsonResponse.Length / 1024 / 1024}MB exceeds 10MB limit");
        }
        
        // 3. Basic JSON structure validation (fast check before parsing)
        if (!IsValidJsonStructure(jsonResponse))
        {
            _logger.LogWarning("Response has invalid JSON structure");
            return ParseResult<ExtractionResponse>.Failure("ACODE-TSE-022", "Invalid JSON structure");
        }
        
        try
        {
            // 4. Parse with streaming reader for memory efficiency
            var response = DeserializeWithValidation(jsonResponse);
            
            if (response == null)
            {
                return ParseResult<ExtractionResponse>.Failure("ACODE-TSE-023", "Null deserialization result");
            }
            
            // 5. Validate response content
            var validationError = ValidateResponse(response);
            if (validationError != null)
            {
                return ParseResult<ExtractionResponse>.Failure("ACODE-TSE-024", validationError);
            }
            
            return ParseResult<ExtractionResponse>.Success(response);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error: {Message}", ex.Message);
            return ParseResult<ExtractionResponse>.Failure(
                "ACODE-TSE-025",
                $"JSON parse error at position {ex.BytePositionInLine}: {SanitizeException(ex)}");
        }
        catch (NotSupportedException ex)
        {
            // Thrown when JSON contains unsupported types
            _logger.LogError(ex, "Unsupported JSON content");
            return ParseResult<ExtractionResponse>.Failure("ACODE-TSE-026", "Unsupported JSON content");
        }
    }
    
    private bool IsValidJsonStructure(string json)
    {
        // Quick validation: must start with { and end with }
        var trimmed = json.AsSpan().Trim();
        
        if (trimmed.Length < 2)
            return false;
            
        if (trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}')
            return false;
        
        // Check for obvious injection attempts
        if (json.Contains("__proto__") || json.Contains("constructor"))
        {
            _logger.LogWarning("Potential prototype pollution attempt detected");
            return false;
        }
        
        return true;
    }
    
    private ExtractionResponse? DeserializeWithValidation(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            MaxDepth = MaxDepth,
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Disallow
        });
        
        // Manual validation of structure before full deserialization
        var root = document.RootElement;
        
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Root must be an object");
        }
        
        // Check for required properties
        if (!root.TryGetProperty("filePath", out _))
        {
            throw new JsonException("Missing required property: filePath");
        }
        
        // Now deserialize with validated content
        return JsonSerializer.Deserialize<ExtractionResponse>(json, _options);
    }
    
    private string? ValidateResponse(ExtractionResponse response)
    {
        // Validate symbol count
        if (response.Symbols.Count > MaxSymbolCount)
        {
            return $"Symbol count {response.Symbols.Count} exceeds limit {MaxSymbolCount}";
        }
        
        // Validate each symbol
        foreach (var symbol in response.Symbols)
        {
            // Check string lengths
            if (symbol.Name?.Length > MaxStringLength)
            {
                return $"Symbol name exceeds {MaxStringLength} character limit";
            }
            
            if (symbol.Signature?.Length > MaxStringLength)
            {
                return $"Symbol signature exceeds {MaxStringLength} character limit";
            }
            
            // Validate line numbers are positive
            if (symbol.StartLine < 0 || symbol.EndLine < 0)
            {
                return "Invalid line numbers in symbol";
            }
            
            // Validate line order
            if (symbol.EndLine < symbol.StartLine)
            {
                return "End line before start line";
            }
        }
        
        return null; // Valid
    }
    
    private string SanitizeException(Exception ex)
    {
        // Remove any file paths or sensitive information from exception
        var message = ex.Message;
        
        // Remove absolute paths
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"[A-Za-z]:\\[^""'\s]+",
            "[path]");
        
        // Remove line numbers from stack traces
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"at line \d+",
            "at line [N]");
        
        return message;
    }
}

/// <summary>
/// Result type for secure parsing operations.
/// </summary>
public record ParseResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    
    public static ParseResult<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };
    
    public static ParseResult<T> Failure(string code, string message) => new()
    {
        IsSuccess = false,
        ErrorCode = code,
        ErrorMessage = message
    };
}
```

---

## Testing Requirements

### Complete Unit Test Implementation

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using AgenticCoder.Domain.Symbols;
using AgenticCoder.Infrastructure.Symbols.TypeScript;

namespace AgenticCoder.Infrastructure.Tests.Symbols.TypeScript;

public class TypeScriptSymbolExtractorTests : IAsyncLifetime
{
    private readonly Mock<ILogger<TypeScriptSymbolExtractor>> _loggerMock;
    private readonly Mock<INodeBridge> _bridgeMock;
    private readonly TypeScriptSymbolExtractor _extractor;
    private readonly string _testDirectory;
    
    public TypeScriptSymbolExtractorTests()
    {
        _loggerMock = new Mock<ILogger<TypeScriptSymbolExtractor>>();
        _bridgeMock = new Mock<INodeBridge>();
        _extractor = new TypeScriptSymbolExtractor(
            _loggerMock.Object,
            _bridgeMock.Object);
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ts-extractor-tests-{Guid.NewGuid()}");
    }
    
    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_testDirectory);
        return Task.CompletedTask;
    }
    
    public Task DisposeAsync()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
        return Task.CompletedTask;
    }
    
    [Fact]
    public void Language_ReturnsTypeScript()
    {
        // Act
        var language = _extractor.Language;
        
        // Assert
        language.Should().Be("typescript");
    }
    
    [Fact]
    public void FileExtensions_ReturnsAllSupportedExtensions()
    {
        // Act
        var extensions = _extractor.FileExtensions;
        
        // Assert
        extensions.Should().BeEquivalentTo(new[] { ".ts", ".tsx", ".js", ".jsx" });
    }
    
    [Fact]
    public async Task ExtractAsync_WithTypeScriptClass_ReturnsClassSymbol()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "UserService.ts");
        var sourceCode = @"
export class UserService {
    private readonly db: Database;
    
    constructor(db: Database) {
        this.db = db;
    }
    
    async getUser(id: string): Promise<User> {
        return await this.db.users.findOne(id);
    }
}";
        await File.WriteAllTextAsync(testFile, sourceCode);
        
        var expectedSymbols = new List<ExtractedSymbol>
        {
            new ExtractedSymbol
            {
                Name = "UserService",
                Kind = "class",
                IsExported = true,
                StartLine = 2,
                EndLine = 12
            },
            new ExtractedSymbol
            {
                Name = "db",
                Kind = "property",
                Modifiers = new[] { "private", "readonly" },
                StartLine = 3
            },
            new ExtractedSymbol
            {
                Name = "constructor",
                Kind = "constructor",
                StartLine = 5
            },
            new ExtractedSymbol
            {
                Name = "getUser",
                Kind = "method",
                Modifiers = new[] { "async" },
                ReturnType = "Promise<User>",
                StartLine = 9
            }
        };
        
        _bridgeMock.Setup(b => b.SendAsync<ExtractionResponse>(
            It.IsAny<ExtractionRequest>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractionResponse { Symbols = expectedSymbols });
        
        // Act
        var result = await _extractor.ExtractAsync(testFile, new ExtractionOptions(), CancellationToken.None);
        
        // Assert
        result.Symbols.Should().HaveCount(4);
        result.Symbols.Should().Contain(s => s.Name == "UserService" && s.Kind == SymbolKind.Class);
        result.Symbols.Should().Contain(s => s.Name == "getUser" && s.Kind == SymbolKind.Method);
    }
    
    [Fact]
    public async Task ExtractAsync_WithInterface_ReturnsInterfaceWithMethods()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "IRepository.ts");
        var sourceCode = @"
export interface IRepository<T> {
    findById(id: string): Promise<T | null>;
    findAll(): Promise<T[]>;
    create(entity: T): Promise<T>;
    update(id: string, entity: Partial<T>): Promise<T>;
    delete(id: string): Promise<void>;
}";
        await File.WriteAllTextAsync(testFile, sourceCode);
        
        var expectedSymbols = new List<ExtractedSymbol>
        {
            new ExtractedSymbol
            {
                Name = "IRepository",
                Kind = "interface",
                IsExported = true,
                GenericParameters = new[] { "T" }
            },
            new ExtractedSymbol { Name = "findById", Kind = "method" },
            new ExtractedSymbol { Name = "findAll", Kind = "method" },
            new ExtractedSymbol { Name = "create", Kind = "method" },
            new ExtractedSymbol { Name = "update", Kind = "method" },
            new ExtractedSymbol { Name = "delete", Kind = "method" }
        };
        
        _bridgeMock.Setup(b => b.SendAsync<ExtractionResponse>(
            It.IsAny<ExtractionRequest>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractionResponse { Symbols = expectedSymbols });
        
        // Act
        var result = await _extractor.ExtractAsync(testFile, new ExtractionOptions(), CancellationToken.None);
        
        // Assert
        result.Symbols.Should().HaveCount(6);
        var interfaceSymbol = result.Symbols.First(s => s.Kind == SymbolKind.Interface);
        interfaceSymbol.Name.Should().Be("IRepository");
        interfaceSymbol.GenericParameters.Should().Contain("T");
    }
    
    [Fact]
    public async Task ExtractAsync_WithJSDoc_ReturnsDocumentation()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "utils.ts");
        var sourceCode = @"
/**
 * Formats a date according to the specified format string.
 * @param date - The date to format
 * @param format - The format string (e.g., 'YYYY-MM-DD')
 * @returns The formatted date string
 * @example
 * formatDate(new Date(), 'YYYY-MM-DD') // '2024-01-15'
 */
export function formatDate(date: Date, format: string): string {
    // implementation
}";
        await File.WriteAllTextAsync(testFile, sourceCode);
        
        var expectedSymbols = new List<ExtractedSymbol>
        {
            new ExtractedSymbol
            {
                Name = "formatDate",
                Kind = "function",
                IsExported = true,
                Documentation = new JSDocInfo
                {
                    Description = "Formats a date according to the specified format string.",
                    Params = new Dictionary<string, string>
                    {
                        ["date"] = "The date to format",
                        ["format"] = "The format string (e.g., 'YYYY-MM-DD')"
                    },
                    Returns = "The formatted date string",
                    Examples = new[] { "formatDate(new Date(), 'YYYY-MM-DD') // '2024-01-15'" }
                }
            }
        };
        
        _bridgeMock.Setup(b => b.SendAsync<ExtractionResponse>(
            It.IsAny<ExtractionRequest>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractionResponse { Symbols = expectedSymbols });
        
        // Act
        var result = await _extractor.ExtractAsync(testFile, new ExtractionOptions { IncludeDocumentation = true }, CancellationToken.None);
        
        // Assert
        var symbol = result.Symbols.Single();
        symbol.Documentation.Should().NotBeNull();
        symbol.Documentation!.Summary.Should().Contain("Formats a date");
        symbol.Documentation.Parameters.Should().ContainKey("date");
        symbol.Documentation.Parameters.Should().ContainKey("format");
    }
    
    [Fact]
    public async Task ExtractAsync_WithSyntaxError_ReturnsPartialResults()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "broken.ts");
        var sourceCode = @"
export class ValidClass {
    validMethod(): void { }
}

export class BrokenClass {
    brokenMethod( // missing closing paren and body
";
        await File.WriteAllTextAsync(testFile, sourceCode);
        
        var expectedResponse = new ExtractionResponse
        {
            Symbols = new List<ExtractedSymbol>
            {
                new ExtractedSymbol { Name = "ValidClass", Kind = "class" },
                new ExtractedSymbol { Name = "validMethod", Kind = "method" }
            },
            Errors = new List<ExtractionError>
            {
                new ExtractionError { Code = "TS1005", Message = "')' expected", Line = 7 }
            }
        };
        
        _bridgeMock.Setup(b => b.SendAsync<ExtractionResponse>(
            It.IsAny<ExtractionRequest>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);
        
        // Act
        var result = await _extractor.ExtractAsync(testFile, new ExtractionOptions(), CancellationToken.None);
        
        // Assert
        result.IsPartial.Should().BeTrue();
        result.Symbols.Should().HaveCount(2);
        result.Errors.Should().HaveCount(1);
        result.Errors[0].Code.Should().Be("ACODE-TSE-001");
    }
    
    [Fact]
    public async Task ExtractAsync_WithExports_TracksExportInformation()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "exports.ts");
        var sourceCode = @"
const internalHelper = () => {};

export const publicConstant = 42;

export function publicFunction() {}

export default class MainClass {}

export { internalHelper as helper };
";
        await File.WriteAllTextAsync(testFile, sourceCode);
        
        var expectedSymbols = new List<ExtractedSymbol>
        {
            new ExtractedSymbol { Name = "internalHelper", Kind = "function", IsExported = false },
            new ExtractedSymbol { Name = "publicConstant", Kind = "variable", IsExported = true },
            new ExtractedSymbol { Name = "publicFunction", Kind = "function", IsExported = true },
            new ExtractedSymbol { Name = "MainClass", Kind = "class", IsExported = true, IsDefaultExport = true },
            new ExtractedSymbol { Name = "helper", Kind = "function", IsExported = true, OriginalName = "internalHelper" }
        };
        
        _bridgeMock.Setup(b => b.SendAsync<ExtractionResponse>(
            It.IsAny<ExtractionRequest>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractionResponse { Symbols = expectedSymbols });
        
        // Act
        var result = await _extractor.ExtractAsync(testFile, new ExtractionOptions(), CancellationToken.None);
        
        // Assert
        result.Symbols.Where(s => s.IsExported).Should().HaveCount(4);
        result.Symbols.Should().Contain(s => s.IsDefaultExport && s.Name == "MainClass");
    }
    
    [Fact]
    public async Task ExtractAsync_CancellationRequested_ThrowsOperationCancelledException()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test.ts");
        await File.WriteAllTextAsync(testFile, "export class Test {}");
        
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _extractor.ExtractAsync(testFile, new ExtractionOptions(), cts.Token));
    }
}

public class NodeBridgeTests : IAsyncLifetime
{
    private NodeBridge? _bridge;
    private readonly Mock<ILogger<NodeBridge>> _loggerMock;
    
    public NodeBridgeTests()
    {
        _loggerMock = new Mock<ILogger<NodeBridge>>();
    }
    
    public async Task InitializeAsync()
    {
        _bridge = new NodeBridge(
            _loggerMock.Object,
            new ProcessResourceLimits
            {
                MaxMemoryMB = 256,
                RequestTimeout = TimeSpan.FromSeconds(10),
                WorkingDirectory = Directory.GetCurrentDirectory()
            });
    }
    
    public async Task DisposeAsync()
    {
        if (_bridge != null)
        {
            await _bridge.DisposeAsync();
        }
    }
    
    [Fact]
    public async Task StartAsync_WithValidNodeInstallation_StartsProcess()
    {
        // Arrange & Act
        await _bridge!.StartAsync(CancellationToken.None);
        
        // Assert
        _bridge.IsRunning.Should().BeTrue();
    }
    
    [Fact]
    public async Task SendAsync_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        await _bridge!.StartAsync(CancellationToken.None);
        var request = new ExtractionRequest
        {
            FilePath = "test.ts",
            IncludeJSDoc = true
        };
        
        // Act
        var response = await _bridge.SendAsync<ExtractionResponse>(request, CancellationToken.None);
        
        // Assert
        response.Should().NotBeNull();
    }
    
    [Fact]
    public async Task SendAsync_WithTimeout_ThrowsNodeBridgeException()
    {
        // Arrange
        var shortTimeoutBridge = new NodeBridge(
            _loggerMock.Object,
            new ProcessResourceLimits
            {
                MaxMemoryMB = 256,
                RequestTimeout = TimeSpan.FromMilliseconds(1),
                WorkingDirectory = Directory.GetCurrentDirectory()
            });
        
        await shortTimeoutBridge.StartAsync(CancellationToken.None);
        
        // Act & Assert
        await Assert.ThrowsAsync<NodeBridgeException>(
            () => shortTimeoutBridge.SendAsync<ExtractionResponse>(
                new ExtractionRequest { FilePath = "large.ts" },
                CancellationToken.None));
    }
    
    [Fact]
    public async Task Dispose_WithRunningProcess_TerminatesGracefully()
    {
        // Arrange
        await _bridge!.StartAsync(CancellationToken.None);
        _bridge.IsRunning.Should().BeTrue();
        
        // Act
        await _bridge.DisposeAsync();
        
        // Assert
        _bridge.IsRunning.Should().BeFalse();
    }
}

public class JSDocExtractorTests
{
    private readonly JSDocExtractor _extractor = new();
    
    [Fact]
    public void ExtractDocumentation_WithFullJSDoc_ReturnsAllElements()
    {
        // Arrange
        var jsDocComment = @"
/**
 * Processes a payment transaction.
 * @param {PaymentRequest} request - The payment request details
 * @param {PaymentOptions} [options] - Optional payment configuration
 * @returns {Promise<PaymentResult>} The result of the payment
 * @throws {PaymentError} When payment fails
 * @example
 * const result = await processPayment({ amount: 100, currency: 'USD' });
 * @deprecated Use processPaymentV2 instead
 */";
        
        // Act
        var result = _extractor.ExtractDocumentation(jsDocComment);
        
        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Contain("Processes a payment transaction");
        result.Parameters.Should().HaveCount(2);
        result.Parameters["request"].Should().Contain("The payment request details");
        result.Parameters["options"].Should().Contain("Optional payment configuration");
        result.Returns.Should().Contain("The result of the payment");
        result.Throws.Should().ContainKey("PaymentError");
        result.Examples.Should().HaveCount(1);
        result.IsDeprecated.Should().BeTrue();
        result.DeprecationMessage.Should().Contain("Use processPaymentV2 instead");
    }
    
    [Fact]
    public void ExtractDocumentation_WithMalformedJSDoc_ReturnsPartialResult()
    {
        // Arrange
        var malformedJsDoc = @"
/**
 * This is a description
 * @param missing the hyphen separator
 * @returns
 */";
        
        // Act
        var result = _extractor.ExtractDocumentation(malformedJsDoc);
        
        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Contain("This is a description");
    }
    
    [Fact]
    public void ExtractDocumentation_WithTypedef_ReturnsTypeDefinition()
    {
        // Arrange
        var typedefJsDoc = @"
/**
 * @typedef {Object} UserProfile
 * @property {string} id - User's unique identifier
 * @property {string} name - User's display name
 * @property {string} [email] - User's email (optional)
 */";
        
        // Act
        var result = _extractor.ExtractDocumentation(typedefJsDoc);
        
        // Assert
        result.Should().NotBeNull();
        result!.TypeDefinitions.Should().ContainKey("UserProfile");
    }
}
```

### Integration Tests

```csharp
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using AgenticCoder.Infrastructure.Symbols.TypeScript;

namespace AgenticCoder.Integration.Tests.Symbols.TypeScript;

[Collection("NodeBridge")]
public class TypeScriptExtractorIntegrationTests : IAsyncLifetime
{
    private IServiceProvider _serviceProvider = null!;
    private TypeScriptSymbolExtractor _extractor = null!;
    private string _testDirectory = null!;
    
    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddTypeScriptExtraction(Directory.GetCurrentDirectory());
        _serviceProvider = services.BuildServiceProvider();
        
        _extractor = _serviceProvider.GetRequiredService<TypeScriptSymbolExtractor>();
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ts-integration-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }
    
    public async Task DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
    
    [Fact]
    public async Task ExtractAsync_RealTypeScriptFile_ExtractsAllSymbols()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "RealService.ts");
        var sourceCode = @"
import { Database } from './database';
import type { User, UserCreateInput } from './types';

/**
 * Service for managing user operations.
 */
export class UserService {
    private readonly db: Database;
    
    constructor(db: Database) {
        this.db = db;
    }
    
    /**
     * Creates a new user in the system.
     * @param input - The user creation data
     * @returns The created user
     */
    async createUser(input: UserCreateInput): Promise<User> {
        const user = await this.db.users.create(input);
        return user;
    }
    
    /**
     * Finds a user by their email address.
     * @param email - The email to search for
     * @returns The user or null if not found
     */
    async findByEmail(email: string): Promise<User | null> {
        return await this.db.users.findOne({ email });
    }
}

export default UserService;
";
        await File.WriteAllTextAsync(testFile, sourceCode);
        
        // Act
        var result = await _extractor.ExtractAsync(testFile, new ExtractionOptions
        {
            IncludeDocumentation = true
        }, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Symbols.Should().HaveCountGreaterThanOrEqualTo(4);
        
        var classSymbol = result.Symbols.First(s => s.Name == "UserService");
        classSymbol.Kind.Should().Be(SymbolKind.Class);
        classSymbol.IsExported.Should().BeTrue();
        classSymbol.Documentation.Should().NotBeNull();
        
        var createMethod = result.Symbols.First(s => s.Name == "createUser");
        createMethod.Kind.Should().Be(SymbolKind.Method);
        createMethod.Documentation?.Parameters.Should().ContainKey("input");
    }
    
    [Fact]
    public async Task ExtractAsync_ReactComponent_ExtractsComponentAndHooks()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "Counter.tsx");
        var sourceCode = @"
import React, { useState, useCallback } from 'react';

interface CounterProps {
    initialValue?: number;
    onCountChange?: (count: number) => void;
}

/**
 * A simple counter component.
 */
export const Counter: React.FC<CounterProps> = ({ initialValue = 0, onCountChange }) => {
    const [count, setCount] = useState(initialValue);
    
    const increment = useCallback(() => {
        const newCount = count + 1;
        setCount(newCount);
        onCountChange?.(newCount);
    }, [count, onCountChange]);
    
    const decrement = useCallback(() => {
        const newCount = count - 1;
        setCount(newCount);
        onCountChange?.(newCount);
    }, [count, onCountChange]);
    
    return (
        <div>
            <button onClick={decrement}>-</button>
            <span>{count}</span>
            <button onClick={increment}>+</button>
        </div>
    );
};

export default Counter;
";
        await File.WriteAllTextAsync(testFile, sourceCode);
        
        // Act
        var result = await _extractor.ExtractAsync(testFile, new ExtractionOptions(), CancellationToken.None);
        
        // Assert
        result.Symbols.Should().Contain(s => s.Name == "CounterProps" && s.Kind == SymbolKind.Interface);
        result.Symbols.Should().Contain(s => s.Name == "Counter");
    }
    
    [Fact]
    public async Task ExtractAsync_LargeFile_CompletesWithinTimeout()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "LargeModule.ts");
        var sourceCode = GenerateLargeTypeScriptFile(500); // 500 classes
        await File.WriteAllTextAsync(testFile, sourceCode);
        
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _extractor.ExtractAsync(testFile, new ExtractionOptions(), cts.Token);
        stopwatch.Stop();
        
        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(15000); // 15 seconds max
        result.Symbols.Should().HaveCountGreaterThanOrEqualTo(500);
    }
    
    private string GenerateLargeTypeScriptFile(int classCount)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < classCount; i++)
        {
            sb.AppendLine($@"
export class Service{i} {{
    private value: number = {i};
    
    getValue(): number {{
        return this.value;
    }}
    
    setValue(v: number): void {{
        this.value = v;
    }}
}}");
        }
        return sb.ToString();
    }
}
```

### E2E Tests

```csharp
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using AgenticCoder.Cli;

namespace AgenticCoder.E2E.Tests.Symbols.TypeScript;

public class TypeScriptSymbolE2ETests
{
    [Fact]
    public async Task CLI_ExtractTypeScriptSymbols_ReturnsFormattedOutput()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"e2e-ts-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var testFile = Path.Combine(tempDir, "api.ts");
            await File.WriteAllTextAsync(testFile, @"
export interface ApiConfig {
    baseUrl: string;
    timeout: number;
}

export class ApiClient {
    constructor(private config: ApiConfig) {}
    
    async get<T>(path: string): Promise<T> {
        // implementation
    }
}
");
            
            // Act
            var result = await CliRunner.RunAsync("symbols", "extract", testFile);
            
            // Assert
            result.ExitCode.Should().Be(0);
            result.StandardOutput.Should().Contain("ApiConfig");
            result.StandardOutput.Should().Contain("ApiClient");
            result.StandardOutput.Should().Contain("interface");
            result.StandardOutput.Should().Contain("class");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
```

### Performance Benchmarks

| Benchmark | Target | Maximum | Notes |
|-----------|--------|---------|-------|
| Parse single TS file (1KB) | 40ms | 60ms | Cold start included |
| Parse single TS file (10KB) | 80ms | 120ms | Typical service file |
| Extract symbols (1KB) | 60ms | 100ms | Including JSDoc |
| Extract symbols (10KB) | 100ms | 150ms | Including JSDoc |
| Bridge startup | 200ms | 400ms | Process spawn + handshake |
| Extract 100 files | 8s | 12s | Sequential processing |
| Memory per extraction | 50MB | 100MB | Peak Node.js memory |

---

## User Verification Steps

### Scenario 1: Extract TypeScript Class with Methods

**Objective:** Verify that TypeScript classes and their members are correctly extracted.

**Prerequisites:**
- Node.js 18+ installed and in PATH
- Agent properly configured with TypeScript extraction enabled

**Steps:**

1. Create a TypeScript file named `UserService.ts`:
```typescript
export class UserService {
    private readonly apiUrl: string;
    
    constructor(apiUrl: string) {
        this.apiUrl = apiUrl;
    }
    
    async getUser(id: string): Promise<User> {
        const response = await fetch(`${this.apiUrl}/users/${id}`);
        return response.json();
    }
    
    async createUser(data: CreateUserInput): Promise<User> {
        const response = await fetch(`${this.apiUrl}/users`, {
            method: 'POST',
            body: JSON.stringify(data)
        });
        return response.json();
    }
}
```

2. Run extraction command:
```bash
acode symbols extract UserService.ts
```

3. Verify expected output:
```
Extracting symbols from UserService.ts...

Symbols extracted: 5
  Class: UserService (exported)
    Location: lines 1-21
    
  Property: apiUrl (private readonly)
    Location: line 2
    Type: string
    
  Constructor: constructor
    Location: lines 4-6
    Signature: constructor(apiUrl: string)
    
  Method: getUser (async)
    Location: lines 8-11
    Signature: async getUser(id: string): Promise<User>
    
  Method: createUser (async)
    Location: lines 13-20
    Signature: async createUser(data: CreateUserInput): Promise<User>

Extraction completed in 87ms
```

**Verification Checklist:**
- [ ] Class symbol extracted with correct name
- [ ] Class marked as exported
- [ ] Private property extracted with readonly modifier
- [ ] Constructor extracted with parameter
- [ ] Both async methods extracted with correct signatures
- [ ] Line numbers accurate

---

### Scenario 2: Extract JavaScript with JSDoc Type Annotations

**Objective:** Verify JavaScript files with JSDoc comments provide type information.

**Prerequisites:**
- TypeScript extraction configured with `include_javascript: true`
- JSDoc extraction enabled

**Steps:**

1. Create a JavaScript file named `mathUtils.js`:
```javascript
/**
 * Calculates the factorial of a number.
 * @param {number} n - The input number
 * @returns {number} The factorial result
 * @throws {Error} If n is negative
 * @example
 * factorial(5) // returns 120
 */
function factorial(n) {
    if (n < 0) throw new Error('Negative input');
    if (n <= 1) return 1;
    return n * factorial(n - 1);
}

/**
 * @typedef {Object} Point
 * @property {number} x - X coordinate
 * @property {number} y - Y coordinate
 */

/**
 * Calculates distance between two points.
 * @param {Point} p1 - First point
 * @param {Point} p2 - Second point
 * @returns {number} Euclidean distance
 */
function distance(p1, p2) {
    return Math.sqrt((p2.x - p1.x) ** 2 + (p2.y - p1.y) ** 2);
}

module.exports = { factorial, distance };
```

2. Run extraction with documentation:
```bash
acode symbols extract mathUtils.js --include-docs
```

3. Verify expected output:
```
Extracting symbols from mathUtils.js...

Symbols extracted: 3

  Function: factorial
    Location: lines 9-13
    Signature: function factorial(n: number): number
    Documentation:
      Description: Calculates the factorial of a number.
      @param n: The input number
      @returns: The factorial result
      @throws Error: If n is negative
      @example: factorial(5) // returns 120

  TypeDef: Point
    Location: lines 15-19
    Properties:
      x: number - X coordinate
      y: number - Y coordinate

  Function: distance
    Location: lines 27-29
    Signature: function distance(p1: Point, p2: Point): number
    Documentation:
      Description: Calculates distance between two points.
      @param p1: First point
      @param p2: Second point
      @returns: Euclidean distance

Exports (CommonJS):
  - factorial
  - distance

Extraction completed in 65ms
```

**Verification Checklist:**
- [ ] Functions extracted from JavaScript file
- [ ] JSDoc @param tags parsed with names and descriptions
- [ ] JSDoc @returns tag extracted
- [ ] JSDoc @throws tag extracted
- [ ] JSDoc @example preserved
- [ ] @typedef creates type definition
- [ ] CommonJS exports detected

---

### Scenario 3: Extract React TSX Component

**Objective:** Verify React components in TSX files are correctly extracted.

**Prerequisites:**
- TSX file extension enabled in configuration
- JSX compiler option set appropriately

**Steps:**

1. Create a React component file `Button.tsx`:
```tsx
import React from 'react';

interface ButtonProps {
    label: string;
    variant?: 'primary' | 'secondary';
    disabled?: boolean;
    onClick: () => void;
}

/**
 * A reusable button component.
 */
export const Button: React.FC<ButtonProps> = ({
    label,
    variant = 'primary',
    disabled = false,
    onClick
}) => {
    return (
        <button
            className={`btn btn-${variant}`}
            disabled={disabled}
            onClick={onClick}
        >
            {label}
        </button>
    );
};

export default Button;
```

2. Run extraction:
```bash
acode symbols extract Button.tsx
```

3. Verify expected output:
```
Extracting symbols from Button.tsx...

Symbols extracted: 3

  Interface: ButtonProps (exported)
    Location: lines 3-8
    Properties:
      label: string (required)
      variant: 'primary' | 'secondary' (optional)
      disabled: boolean (optional)
      onClick: () => void (required)

  Variable: Button (exported, const)
    Location: lines 13-27
    Type: React.FC<ButtonProps>
    Documentation: A reusable button component.

  Export: default
    Location: line 29
    References: Button

Extraction completed in 92ms
```

**Verification Checklist:**
- [ ] Interface extracted with all properties
- [ ] Optional properties marked correctly
- [ ] React.FC type captured
- [ ] Default export tracked
- [ ] JSX syntax parsed without errors

---

### Scenario 4: Handle Syntax Errors Gracefully

**Objective:** Verify partial extraction succeeds when file has syntax errors.

**Prerequisites:**
- Standard TypeScript extraction configuration

**Steps:**

1. Create a file with intentional syntax errors `broken.ts`:
```typescript
export class ValidClass {
    validMethod(): void {
        console.log('works');
    }
}

export class BrokenClass {
    brokenMethod( {  // Missing closing paren
        
    }
    
    anotherBroken(): void
        // Missing body
```

2. Run extraction:
```bash
acode symbols extract broken.ts
```

3. Verify expected output:
```
Extracting symbols from broken.ts...

⚠ Parse errors detected:
  Line 8: ')' expected
  Line 14: '{' expected

Symbols extracted: 2 (partial)

  Class: ValidClass (exported)
    Location: lines 1-5
    
  Method: validMethod
    Location: lines 2-4
    Signature: validMethod(): void

Errors:
  ACODE-TSE-001: Syntax error at line 8 - extraction continued with partial results
  ACODE-TSE-001: Syntax error at line 14 - extraction continued with partial results

Extraction completed with warnings in 54ms
```

**Verification Checklist:**
- [ ] Valid symbols before errors are extracted
- [ ] Error codes reported with line numbers
- [ ] Extraction does not crash
- [ ] Partial flag indicated
- [ ] Performance not significantly impacted

---

### Scenario 5: Extract Module Exports and Imports

**Objective:** Verify import/export tracking works correctly.

**Prerequisites:**
- TypeScript extraction enabled with module tracking

**Steps:**

1. Create an index file `services/index.ts`:
```typescript
// Re-exports
export { UserService } from './UserService';
export { AuthService } from './AuthService';
export type { User, AuthToken } from './types';

// Named exports
export const API_VERSION = '2.0';
export const DEFAULT_TIMEOUT = 5000;

// Default export
import { MainService } from './MainService';
export default MainService;

// Namespace export
export * as Utils from './utils';
```

2. Run extraction:
```bash
acode symbols extract services/index.ts --track-exports
```

3. Verify expected output:
```
Extracting symbols from services/index.ts...

Symbols extracted: 6

  Re-export: UserService
    Source: ./UserService
    
  Re-export: AuthService
    Source: ./AuthService
    
  Re-export (type): User, AuthToken
    Source: ./types
    
  Variable: API_VERSION (exported, const)
    Value: '2.0'
    
  Variable: DEFAULT_TIMEOUT (exported, const)
    Value: 5000
    
  Export: default
    References: MainService (from ./MainService)
    
  Namespace Export: Utils
    Source: ./utils

Module Summary:
  Imports: 1 (MainService from ./MainService)
  Named Exports: 4
  Re-exports: 4
  Default Export: MainService

Extraction completed in 78ms
```

**Verification Checklist:**
- [ ] Named re-exports tracked with sources
- [ ] Type-only exports identified
- [ ] Const values captured
- [ ] Default export resolved
- [ ] Namespace exports (export *) tracked
- [ ] Import sources recorded

---

### Scenario 6: Extract TypeScript Enum and Type Aliases

**Objective:** Verify TypeScript-specific constructs are extracted correctly.

**Prerequisites:**
- TypeScript extraction enabled

**Steps:**

1. Create a types file `types.ts`:
```typescript
export enum OrderStatus {
    Pending = 'PENDING',
    Processing = 'PROCESSING',
    Shipped = 'SHIPPED',
    Delivered = 'DELIVERED',
    Cancelled = 'CANCELLED'
}

export type UserId = string;

export type OrderFilter = {
    status?: OrderStatus;
    userId?: UserId;
    dateRange?: {
        start: Date;
        end: Date;
    };
};

export interface Order {
    id: string;
    userId: UserId;
    status: OrderStatus;
    items: OrderItem[];
    createdAt: Date;
}
```

2. Run extraction:
```bash
acode symbols extract types.ts
```

3. Verify expected output:
```
Extracting symbols from types.ts...

Symbols extracted: 5

  Enum: OrderStatus (exported)
    Location: lines 1-7
    Members:
      Pending = 'PENDING'
      Processing = 'PROCESSING'
      Shipped = 'SHIPPED'
      Delivered = 'DELIVERED'
      Cancelled = 'CANCELLED'

  TypeAlias: UserId (exported)
    Location: line 9
    Definition: string

  TypeAlias: OrderFilter (exported)
    Location: lines 11-18
    Definition: { status?: OrderStatus; userId?: UserId; dateRange?: { start: Date; end: Date; }; }

  Interface: Order (exported)
    Location: lines 20-26
    Properties:
      id: string
      userId: UserId
      status: OrderStatus
      items: OrderItem[]
      createdAt: Date

Extraction completed in 71ms
```

**Verification Checklist:**
- [ ] Enum extracted with all members and values
- [ ] Simple type alias captured
- [ ] Complex type alias with nested types captured
- [ ] Interface properties listed with types
- [ ] Type references preserved

---

### Scenario 7: Verify Node.js Bridge Recovery

**Objective:** Verify the Node.js bridge automatically recovers from crashes.

**Prerequisites:**
- TypeScript extraction configured
- Access to agent logs

**Steps:**

1. Create a valid TypeScript file `test.ts`:
```typescript
export function hello(): string {
    return 'world';
}
```

2. Simulate bridge crash by killing Node.js process:
```bash
# In a separate terminal, find and kill the node process
# Windows:
taskkill /F /IM node.exe /FI "WINDOWTITLE eq ts-extractor"

# Linux/Mac:
pkill -f "ts-extractor"
```

3. Immediately run extraction:
```bash
acode symbols extract test.ts
```

4. Verify expected output:
```
⚠ Node.js bridge connection lost. Restarting...
Bridge restarted successfully (attempt 1/3)

Extracting symbols from test.ts...

Symbols extracted: 1

  Function: hello (exported)
    Location: lines 1-3
    Signature: function hello(): string

Extraction completed in 523ms (includes bridge restart)
```

5. Check agent logs:
```bash
acode logs --filter typescript
```

Expected log entries:
```
[WARN] TypeScript bridge process exited unexpectedly (code: -1)
[INFO] Restarting TypeScript bridge (attempt 1/3)
[INFO] TypeScript bridge started successfully (PID: 12345)
[INFO] Extraction completed: test.ts (1 symbols, 523ms)
```

**Verification Checklist:**
- [ ] Bridge crash detected automatically
- [ ] Restart attempted with backoff
- [ ] Extraction succeeds after restart
- [ ] Logs capture restart events
- [ ] No data loss occurs

---

### Scenario 8: Performance Verification with Large File

**Objective:** Verify extraction meets performance targets for large files.

**Prerequisites:**
- TypeScript extraction configured
- File with 500+ symbols

**Steps:**

1. Create a large TypeScript file `LargeModule.ts` with 100 classes:
```typescript
// Generate programmatically or use existing large file
export class Service001 { method001(): void {} }
export class Service002 { method002(): void {} }
// ... 98 more classes
export class Service100 { method100(): void {} }
```

2. Run extraction with stats:
```bash
acode symbols extract LargeModule.ts --stats
```

3. Verify expected output:
```
Extracting symbols from LargeModule.ts...

Symbols extracted: 200

  Classes: 100
  Methods: 100

Statistics:
  File size: 45.2 KB
  Lines: 1,204
  Parse time: 156ms
  Extract time: 234ms
  Total time: 390ms
  Memory used: 67.3 MB
  Symbols/second: 512

✓ Performance target MET (< 500ms for < 50KB file)

Extraction completed in 390ms
```

**Verification Checklist:**
- [ ] All 200 symbols extracted correctly
- [ ] Total time under 500ms for ~45KB file
- [ ] Memory usage under 100MB
- [ ] Statistics accurately reported
- [ ] No timeouts or errors

---

## Implementation Prompt

### File Structure

```
src/Acode.Infrastructure/
├── Symbols/
│   └── TypeScript/
│       ├── TypeScriptSymbolExtractor.cs
│       ├── NodeBridge.cs
│       ├── MessageProtocol.cs
│       ├── ExtractionRequest.cs
│       ├── ExtractionResponse.cs
│       └── Mappers/
│           └── TypeScriptSymbolMapper.cs
│
tools/ts-extractor/
├── package.json
├── package-lock.json
├── tsconfig.json
├── src/
│   ├── index.ts
│   ├── extractor.ts
│   ├── visitor.ts
│   ├── jsdoc.ts
│   ├── types.ts
│   └── protocol.ts
└── tests/
    ├── extractor.test.ts
    └── jsdoc.test.ts
```

---

### 1. TypeScriptSymbolExtractor.cs (Complete Implementation)

```csharp
// src/Acode.Infrastructure/Symbols/TypeScript/TypeScriptSymbolExtractor.cs
using System.Text.Json;
using Acode.Application.Interfaces;
using Acode.Domain.Entities;
using Acode.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Symbols.TypeScript;

/// <summary>
/// Extracts symbols from TypeScript and JavaScript files using a Node.js subprocess
/// running the TypeScript Compiler API.
/// </summary>
public sealed class TypeScriptSymbolExtractor : ISymbolExtractor, IAsyncDisposable
{
    public string Language => "typescript";
    public IReadOnlyList<string> SupportedExtensions { get; } = new[] 
    { 
        ".ts", ".tsx", ".js", ".jsx", ".mts", ".cts", ".mjs", ".cjs" 
    };

    private readonly NodeBridge _bridge;
    private readonly ILogger<TypeScriptSymbolExtractor> _logger;
    private readonly TypeScriptSymbolMapper _mapper;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _initialized;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    public TypeScriptSymbolExtractor(
        NodeBridge bridge,
        TypeScriptSymbolMapper mapper,
        ILogger<TypeScriptSymbolExtractor> logger)
    {
        _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public bool CanExtract(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;
            
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<SymbolExtractionResult> ExtractAsync(
        string filePath,
        SymbolExtractionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        
        if (!CanExtract(filePath))
        {
            return SymbolExtractionResult.Failure(
                filePath,
                new SymbolExtractionError("ACODE-TSE-005", $"Unsupported file extension: {Path.GetExtension(filePath)}"));
        }

        if (!File.Exists(filePath))
        {
            return SymbolExtractionResult.Failure(
                filePath,
                new SymbolExtractionError("ACODE-TSE-006", $"File not found: {filePath}"));
        }

        await EnsureInitializedAsync(cancellationToken);

        var request = new ExtractionRequest
        {
            FilePath = Path.GetFullPath(filePath),
            IncludeJSDoc = options.IncludeDocumentation,
            IncludePrivateMembers = options.IncludePrivateMembers,
            IncludeTypeInfo = options.IncludeTypeInformation,
            MaxDepth = options.MaxNestingDepth
        };

        _logger.LogDebug("Extracting symbols from {FilePath}", filePath);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await _bridge.SendRequestAsync<ExtractionRequest, ExtractionResponse>(
                request,
                cancellationToken);

            stopwatch.Stop();
            _logger.LogDebug("Extracted {SymbolCount} symbols from {FilePath} in {ElapsedMs}ms",
                response.Symbols.Count, filePath, stopwatch.ElapsedMilliseconds);

            var symbols = _mapper.MapSymbols(response.Symbols, filePath);
            var errors = response.Errors.Select(e => 
                new SymbolExtractionError(e.Code, e.Message, e.Line, e.Column)).ToList();

            return new SymbolExtractionResult
            {
                FilePath = filePath,
                Symbols = symbols,
                Errors = errors,
                IsPartial = response.IsPartial,
                ExtractionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (NodeBridgeException ex)
        {
            _logger.LogError(ex, "Node bridge error extracting symbols from {FilePath}", filePath);
            return SymbolExtractionResult.Failure(
                filePath,
                new SymbolExtractionError("ACODE-TSE-002", ex.Message));
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse extraction response for {FilePath}", filePath);
            return SymbolExtractionResult.Failure(
                filePath,
                new SymbolExtractionError("ACODE-TSE-007", $"Invalid response format: {ex.Message}"));
        }
    }

    public async Task<IReadOnlyList<SymbolExtractionResult>> ExtractBatchAsync(
        IEnumerable<string> filePaths,
        SymbolExtractionOptions options,
        CancellationToken cancellationToken = default)
    {
        var results = new List<SymbolExtractionResult>();
        
        foreach (var filePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await ExtractAsync(filePath, options, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
            return;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;

            await _bridge.StartAsync(cancellationToken);
            _initialized = true;
            _logger.LogInformation("TypeScript symbol extractor initialized");
        }
        finally
        {
            _initLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _bridge.DisposeAsync();
        _initLock.Dispose();
    }
}
```

---

### 2. NodeBridge.cs (Complete Implementation)

```csharp
// src/Acode.Infrastructure/Symbols/TypeScript/NodeBridge.cs
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acode.Infrastructure.Symbols.TypeScript;

/// <summary>
/// Manages a persistent Node.js subprocess for TypeScript extraction operations.
/// Implements JSON-based messaging over stdin/stdout.
/// </summary>
public sealed class NodeBridge : IAsyncDisposable
{
    private readonly NodeBridgeOptions _options;
    private readonly ILogger<NodeBridge> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _shutdownCts = new();
    
    private Process? _process;
    private StreamWriter? _stdin;
    private StreamReader? _stdout;
    private int _requestId;
    private bool _isRunning;
    private int _restartAttempts;
    
    private const int MaxRestartAttempts = 3;
    private static readonly TimeSpan[] RestartDelays = { 
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1)
    };

    public NodeBridge(
        IOptions<NodeBridgeOptions> options,
        ILogger<NodeBridge> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            return;

        var nodePath = await ResolveNodePathAsync(cancellationToken);
        var scriptPath = Path.Combine(_options.ToolsDirectory, "ts-extractor", "dist", "index.js");

        if (!File.Exists(scriptPath))
        {
            throw new NodeBridgeException(
                "ACODE-TSE-008",
                $"TypeScript extractor script not found at: {scriptPath}. Run 'npm run build' in tools/ts-extractor.");
        }

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = nodePath,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            },
            EnableRaisingEvents = true
        };

        _process.ErrorDataReceived += OnStderrData;
        _process.Exited += OnProcessExited;

        try
        {
            if (!_process.Start())
            {
                throw new NodeBridgeException("ACODE-TSE-003", "Failed to start Node.js process");
            }

            _process.BeginErrorReadLine();
            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;
            _isRunning = true;
            _restartAttempts = 0;

            _logger.LogInformation("Node.js bridge started (PID: {ProcessId})", _process.Id);

            // Wait for ready signal
            var ready = await WaitForReadyAsync(cancellationToken);
            if (!ready)
            {
                throw new NodeBridgeException("ACODE-TSE-009", "Node.js bridge did not respond with ready signal");
            }
        }
        catch (Exception ex) when (ex is not NodeBridgeException)
        {
            _logger.LogError(ex, "Failed to start Node.js bridge");
            throw new NodeBridgeException("ACODE-TSE-003", $"Failed to start Node.js: {ex.Message}", ex);
        }
    }

    public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureRunningAsync(cancellationToken);

            var requestId = Interlocked.Increment(ref _requestId);
            var envelope = new RequestEnvelope<TRequest>
            {
                Id = requestId,
                Type = typeof(TRequest).Name,
                Payload = request
            };

            var json = JsonSerializer.Serialize(envelope, _jsonOptions);
            _logger.LogTrace("Sending request {RequestId}: {Json}", requestId, json);

            await _stdin!.WriteLineAsync(json);
            await _stdin.FlushAsync();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_options.RequestTimeout);

            var responseLine = await _stdout!.ReadLineAsync(cts.Token);
            
            if (string.IsNullOrEmpty(responseLine))
            {
                throw new NodeBridgeException("ACODE-TSE-010", "Empty response from Node.js bridge");
            }

            _logger.LogTrace("Received response {RequestId}: {Json}", requestId, responseLine);

            var responseEnvelope = JsonSerializer.Deserialize<ResponseEnvelope<TResponse>>(
                responseLine, _jsonOptions);

            if (responseEnvelope == null)
            {
                throw new NodeBridgeException("ACODE-TSE-011", "Failed to deserialize response envelope");
            }

            if (responseEnvelope.Id != requestId)
            {
                throw new NodeBridgeException("ACODE-TSE-012", 
                    $"Response ID mismatch. Expected {requestId}, got {responseEnvelope.Id}");
            }

            if (!string.IsNullOrEmpty(responseEnvelope.Error))
            {
                throw new NodeBridgeException("ACODE-TSE-013", responseEnvelope.Error);
            }

            return responseEnvelope.Payload!;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task<bool> WaitForReadyAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var line = await _stdout!.ReadLineAsync(cts.Token);
            return line?.Contains("ready") == true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task EnsureRunningAsync(CancellationToken cancellationToken)
    {
        if (_isRunning && _process?.HasExited == false)
            return;

        _logger.LogWarning("Node.js bridge is not running, attempting restart...");
        await RestartAsync(cancellationToken);
    }

    private async Task RestartAsync(CancellationToken cancellationToken)
    {
        if (_restartAttempts >= MaxRestartAttempts)
        {
            throw new NodeBridgeException("ACODE-TSE-014", 
                $"Node.js bridge failed after {MaxRestartAttempts} restart attempts");
        }

        var delay = RestartDelays[Math.Min(_restartAttempts, RestartDelays.Length - 1)];
        _restartAttempts++;

        _logger.LogInformation("Restarting Node.js bridge (attempt {Attempt}/{MaxAttempts}) after {Delay}ms",
            _restartAttempts, MaxRestartAttempts, delay.TotalMilliseconds);

        await CleanupProcessAsync();
        await Task.Delay(delay, cancellationToken);
        await StartAsync(cancellationToken);
    }

    private async Task<string> ResolveNodePathAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_options.NodePath) && File.Exists(_options.NodePath))
        {
            return _options.NodePath;
        }

        // Try to find node in PATH
        var nodeName = OperatingSystem.IsWindows() ? "node.exe" : "node";
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var paths = pathEnv.Split(Path.PathSeparator);

        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, nodeName);
            if (File.Exists(fullPath))
            {
                _logger.LogDebug("Found Node.js at: {NodePath}", fullPath);
                return fullPath;
            }
        }

        throw new NodeBridgeException("ACODE-TSE-003", 
            "Node.js not found. Ensure Node.js 18+ is installed and in PATH, or set NodePath in configuration.");
    }

    private void OnStderrData(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
        {
            _logger.LogWarning("[ts-extractor stderr] {Data}", e.Data);
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        var exitCode = _process?.ExitCode ?? -1;
        _logger.LogWarning("Node.js bridge process exited with code {ExitCode}", exitCode);
        _isRunning = false;
    }

    private async Task CleanupProcessAsync()
    {
        _isRunning = false;

        if (_stdin != null)
        {
            await _stdin.DisposeAsync();
            _stdin = null;
        }

        if (_stdout != null)
        {
            _stdout.Dispose();
            _stdout = null;
        }

        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    await _process.WaitForExitAsync(_shutdownCts.Token);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error killing Node.js process");
            }
            finally
            {
                _process.Dispose();
                _process = null;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _shutdownCts.Cancel();
        await CleanupProcessAsync();
        _sendLock.Dispose();
        _shutdownCts.Dispose();
    }
}
```

---

### 3. Message Protocol DTOs

```csharp
// src/Acode.Infrastructure/Symbols/TypeScript/MessageProtocol.cs
using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Symbols.TypeScript;

public record RequestEnvelope<T>
{
    public int Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public T? Payload { get; init; }
}

public record ResponseEnvelope<T>
{
    public int Id { get; init; }
    public T? Payload { get; init; }
    public string? Error { get; init; }
}

public record ExtractionRequest
{
    public string FilePath { get; init; } = string.Empty;
    public bool IncludeJSDoc { get; init; } = true;
    public bool IncludePrivateMembers { get; init; } = false;
    public bool IncludeTypeInfo { get; init; } = true;
    public int MaxDepth { get; init; } = 10;
}

public record ExtractionResponse
{
    public string FilePath { get; init; } = string.Empty;
    public IReadOnlyList<ExtractedSymbolDto> Symbols { get; init; } = Array.Empty<ExtractedSymbolDto>();
    public IReadOnlyList<ExtractionErrorDto> Errors { get; init; } = Array.Empty<ExtractionErrorDto>();
    public bool IsPartial { get; init; }
    public long ParseTimeMs { get; init; }
    public long ExtractTimeMs { get; init; }
}

public record ExtractedSymbolDto
{
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public int StartLine { get; init; }
    public int EndLine { get; init; }
    public int StartColumn { get; init; }
    public int EndColumn { get; init; }
    public string? Signature { get; init; }
    public string? Type { get; init; }
    public IReadOnlyList<string> Modifiers { get; init; } = Array.Empty<string>();
    public bool IsExported { get; init; }
    public JSDocDto? JsDoc { get; init; }
    public IReadOnlyList<ExtractedSymbolDto> Children { get; init; } = Array.Empty<ExtractedSymbolDto>();
    public IReadOnlyDictionary<string, string>? Properties { get; init; }
}

public record JSDocDto
{
    public string? Description { get; init; }
    public IReadOnlyList<JSDocTagDto> Tags { get; init; } = Array.Empty<JSDocTagDto>();
}

public record JSDocTagDto
{
    public string Name { get; init; } = string.Empty;
    public string? Type { get; init; }
    public string? Text { get; init; }
}

public record ExtractionErrorDto
{
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int? Line { get; init; }
    public int? Column { get; init; }
}
```

---

### 4. NodeBridgeOptions.cs

```csharp
// src/Acode.Infrastructure/Symbols/TypeScript/NodeBridgeOptions.cs
namespace Acode.Infrastructure.Symbols.TypeScript;

public class NodeBridgeOptions
{
    public const string SectionName = "TypeScript:NodeBridge";
    
    /// <summary>
    /// Explicit path to Node.js executable. If null, will search PATH.
    /// </summary>
    public string? NodePath { get; set; }
    
    /// <summary>
    /// Directory containing the ts-extractor tool.
    /// </summary>
    public string ToolsDirectory { get; set; } = "tools";
    
    /// <summary>
    /// Timeout for individual extraction requests.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Maximum memory limit for Node.js process in MB.
    /// </summary>
    public int MaxMemoryMb { get; set; } = 512;
}
```

---

### 5. TypeScriptSymbolMapper.cs

```csharp
// src/Acode.Infrastructure/Symbols/TypeScript/Mappers/TypeScriptSymbolMapper.cs
using Acode.Domain.Entities;
using Acode.Domain.Enums;

namespace Acode.Infrastructure.Symbols.TypeScript;

public class TypeScriptSymbolMapper
{
    public IReadOnlyList<Symbol> MapSymbols(
        IReadOnlyList<ExtractedSymbolDto> dtos,
        string filePath)
    {
        var symbols = new List<Symbol>();
        
        foreach (var dto in dtos)
        {
            symbols.Add(MapSymbol(dto, filePath));
        }

        return symbols;
    }

    private Symbol MapSymbol(ExtractedSymbolDto dto, string filePath)
    {
        var symbol = new Symbol
        {
            Name = dto.Name,
            Kind = MapSymbolKind(dto.Kind),
            FilePath = filePath,
            Location = new SourceLocation
            {
                StartLine = dto.StartLine,
                EndLine = dto.EndLine,
                StartColumn = dto.StartColumn,
                EndColumn = dto.EndColumn
            },
            Signature = dto.Signature,
            TypeName = dto.Type,
            IsExported = dto.IsExported,
            Modifiers = MapModifiers(dto.Modifiers),
            Documentation = MapDocumentation(dto.JsDoc),
            Children = dto.Children.Select(c => MapSymbol(c, filePath)).ToList()
        };

        return symbol;
    }

    private SymbolKind MapSymbolKind(string kind)
    {
        return kind.ToLowerInvariant() switch
        {
            "class" => SymbolKind.Class,
            "interface" => SymbolKind.Interface,
            "function" => SymbolKind.Function,
            "method" => SymbolKind.Method,
            "property" => SymbolKind.Property,
            "variable" => SymbolKind.Variable,
            "const" => SymbolKind.Constant,
            "enum" => SymbolKind.Enum,
            "enummember" => SymbolKind.EnumMember,
            "typealias" => SymbolKind.TypeAlias,
            "namespace" => SymbolKind.Namespace,
            "module" => SymbolKind.Module,
            "constructor" => SymbolKind.Constructor,
            "parameter" => SymbolKind.Parameter,
            "typeparameter" => SymbolKind.TypeParameter,
            "getter" => SymbolKind.Getter,
            "setter" => SymbolKind.Setter,
            _ => SymbolKind.Unknown
        };
    }

    private IReadOnlyList<SymbolModifier> MapModifiers(IReadOnlyList<string> modifiers)
    {
        return modifiers.Select(m => m.ToLowerInvariant() switch
        {
            "public" => SymbolModifier.Public,
            "private" => SymbolModifier.Private,
            "protected" => SymbolModifier.Protected,
            "static" => SymbolModifier.Static,
            "readonly" => SymbolModifier.Readonly,
            "abstract" => SymbolModifier.Abstract,
            "async" => SymbolModifier.Async,
            "export" => SymbolModifier.Exported,
            "default" => SymbolModifier.Default,
            _ => SymbolModifier.None
        }).Where(m => m != SymbolModifier.None).ToList();
    }

    private SymbolDocumentation? MapDocumentation(JSDocDto? jsdoc)
    {
        if (jsdoc == null)
            return null;

        return new SymbolDocumentation
        {
            Summary = jsdoc.Description,
            Parameters = jsdoc.Tags
                .Where(t => t.Name == "param")
                .ToDictionary(t => t.Type ?? "unknown", t => t.Text ?? string.Empty),
            Returns = jsdoc.Tags.FirstOrDefault(t => t.Name == "returns")?.Text,
            Throws = jsdoc.Tags
                .Where(t => t.Name == "throws")
                .Select(t => t.Text ?? string.Empty)
                .ToList(),
            Examples = jsdoc.Tags
                .Where(t => t.Name == "example")
                .Select(t => t.Text ?? string.Empty)
                .ToList(),
            Deprecated = jsdoc.Tags.Any(t => t.Name == "deprecated"),
            DeprecationMessage = jsdoc.Tags.FirstOrDefault(t => t.Name == "deprecated")?.Text
        };
    }
}
```

---

### 6. TypeScript Extractor Entry Point (Node.js)

```typescript
// tools/ts-extractor/src/index.ts
import * as readline from 'readline';
import { extractSymbols } from './extractor';
import { RequestEnvelope, ResponseEnvelope, ExtractionRequest } from './types';

const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    terminal: false
});

// Signal ready
console.log(JSON.stringify({ status: 'ready', version: '1.0.0' }));

rl.on('line', async (line: string) => {
    let requestId = 0;
    
    try {
        const envelope: RequestEnvelope<ExtractionRequest> = JSON.parse(line);
        requestId = envelope.id;
        
        if (envelope.type !== 'ExtractionRequest') {
            throw new Error(`Unknown request type: ${envelope.type}`);
        }
        
        const result = await extractSymbols(envelope.payload);
        
        const response: ResponseEnvelope = {
            id: requestId,
            payload: result
        };
        
        console.log(JSON.stringify(response));
    } catch (error) {
        const response: ResponseEnvelope = {
            id: requestId,
            error: error instanceof Error ? error.message : String(error)
        };
        
        console.log(JSON.stringify(response));
    }
});

rl.on('close', () => {
    process.exit(0);
});

// Handle uncaught errors
process.on('uncaughtException', (error) => {
    console.error(JSON.stringify({ error: error.message, fatal: true }));
    process.exit(1);
});
```

---

### 7. TypeScript Extractor Core Logic

```typescript
// tools/ts-extractor/src/extractor.ts
import * as ts from 'typescript';
import * as fs from 'fs';
import { ExtractionRequest, ExtractionResponse, ExtractedSymbol, ExtractionError } from './types';
import { SymbolVisitor } from './visitor';
import { extractJSDoc } from './jsdoc';

export async function extractSymbols(request: ExtractionRequest): Promise<ExtractionResponse> {
    const startTime = Date.now();
    const errors: ExtractionError[] = [];
    
    // Read file content
    let fileContent: string;
    try {
        fileContent = fs.readFileSync(request.filePath, 'utf8');
    } catch (error) {
        return {
            filePath: request.filePath,
            symbols: [],
            errors: [{
                code: 'ACODE-TSE-015',
                message: `Failed to read file: ${error instanceof Error ? error.message : String(error)}`
            }],
            isPartial: false,
            parseTimeMs: 0,
            extractTimeMs: 0
        };
    }

    // Determine script kind based on extension
    const extension = request.filePath.toLowerCase();
    let scriptKind = ts.ScriptKind.TS;
    if (extension.endsWith('.tsx')) scriptKind = ts.ScriptKind.TSX;
    else if (extension.endsWith('.js') || extension.endsWith('.mjs') || extension.endsWith('.cjs')) 
        scriptKind = ts.ScriptKind.JS;
    else if (extension.endsWith('.jsx')) scriptKind = ts.ScriptKind.JSX;

    // Parse the source file
    const parseStart = Date.now();
    const sourceFile = ts.createSourceFile(
        request.filePath,
        fileContent,
        ts.ScriptTarget.Latest,
        true, // setParentNodes
        scriptKind
    );
    const parseTimeMs = Date.now() - parseStart;

    // Collect syntax errors
    const diagnostics = (sourceFile as any).parseDiagnostics || [];
    for (const diag of diagnostics) {
        const position = sourceFile.getLineAndCharacterOfPosition(diag.start || 0);
        errors.push({
            code: 'ACODE-TSE-001',
            message: ts.flattenDiagnosticMessageText(diag.messageText, '\n'),
            line: position.line + 1,
            column: position.character + 1
        });
    }

    // Extract symbols
    const extractStart = Date.now();
    const visitor = new SymbolVisitor(sourceFile, {
        includeJSDoc: request.includeJSDoc,
        includePrivateMembers: request.includePrivateMembers,
        includeTypeInfo: request.includeTypeInfo,
        maxDepth: request.maxDepth
    });
    
    const symbols = visitor.visit();
    const extractTimeMs = Date.now() - extractStart;

    return {
        filePath: request.filePath,
        symbols,
        errors,
        isPartial: errors.length > 0,
        parseTimeMs,
        extractTimeMs
    };
}
```

---

### 8. Symbol Visitor Implementation

```typescript
// tools/ts-extractor/src/visitor.ts
import * as ts from 'typescript';
import { ExtractedSymbol, VisitorOptions } from './types';
import { extractJSDoc } from './jsdoc';

export class SymbolVisitor {
    private sourceFile: ts.SourceFile;
    private options: VisitorOptions;
    private currentDepth: number = 0;

    constructor(sourceFile: ts.SourceFile, options: VisitorOptions) {
        this.sourceFile = sourceFile;
        this.options = options;
    }

    visit(): ExtractedSymbol[] {
        const symbols: ExtractedSymbol[] = [];
        this.visitNode(this.sourceFile, symbols);
        return symbols;
    }

    private visitNode(node: ts.Node, symbols: ExtractedSymbol[]): void {
        if (this.currentDepth > this.options.maxDepth) {
            return;
        }

        const symbol = this.extractSymbol(node);
        if (symbol) {
            symbols.push(symbol);
        }

        ts.forEachChild(node, child => this.visitNode(child, symbol?.children ?? symbols));
    }

    private extractSymbol(node: ts.Node): ExtractedSymbol | null {
        if (ts.isClassDeclaration(node)) {
            return this.extractClass(node);
        }
        if (ts.isInterfaceDeclaration(node)) {
            return this.extractInterface(node);
        }
        if (ts.isFunctionDeclaration(node)) {
            return this.extractFunction(node);
        }
        if (ts.isMethodDeclaration(node)) {
            return this.extractMethod(node);
        }
        if (ts.isPropertyDeclaration(node)) {
            return this.extractProperty(node);
        }
        if (ts.isVariableStatement(node)) {
            return this.extractVariableStatement(node);
        }
        if (ts.isEnumDeclaration(node)) {
            return this.extractEnum(node);
        }
        if (ts.isTypeAliasDeclaration(node)) {
            return this.extractTypeAlias(node);
        }
        if (ts.isConstructorDeclaration(node)) {
            return this.extractConstructor(node);
        }
        return null;
    }

    private extractClass(node: ts.ClassDeclaration): ExtractedSymbol {
        const location = this.getLocation(node);
        const modifiers = this.getModifiers(node);
        
        this.currentDepth++;
        const children: ExtractedSymbol[] = [];
        node.members.forEach(member => {
            const child = this.extractSymbol(member);
            if (child) children.push(child);
        });
        this.currentDepth--;

        return {
            name: node.name?.getText(this.sourceFile) ?? '<anonymous>',
            kind: 'class',
            ...location,
            signature: this.getClassSignature(node),
            modifiers,
            isExported: this.hasExportModifier(node),
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children
        };
    }

    private extractInterface(node: ts.InterfaceDeclaration): ExtractedSymbol {
        const location = this.getLocation(node);
        
        this.currentDepth++;
        const children: ExtractedSymbol[] = [];
        node.members.forEach(member => {
            if (ts.isPropertySignature(member)) {
                children.push({
                    name: member.name?.getText(this.sourceFile) ?? '',
                    kind: 'property',
                    ...this.getLocation(member),
                    type: member.type?.getText(this.sourceFile),
                    modifiers: member.questionToken ? ['optional'] : [],
                    isExported: false,
                    children: []
                });
            }
        });
        this.currentDepth--;

        return {
            name: node.name.getText(this.sourceFile),
            kind: 'interface',
            ...location,
            modifiers: this.getModifiers(node),
            isExported: this.hasExportModifier(node),
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children
        };
    }

    private extractFunction(node: ts.FunctionDeclaration): ExtractedSymbol {
        const location = this.getLocation(node);
        
        return {
            name: node.name?.getText(this.sourceFile) ?? '<anonymous>',
            kind: 'function',
            ...location,
            signature: this.getFunctionSignature(node),
            type: node.type?.getText(this.sourceFile),
            modifiers: this.getModifiers(node),
            isExported: this.hasExportModifier(node),
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children: []
        };
    }

    private extractMethod(node: ts.MethodDeclaration): ExtractedSymbol {
        const location = this.getLocation(node);
        
        return {
            name: node.name.getText(this.sourceFile),
            kind: 'method',
            ...location,
            signature: this.getMethodSignature(node),
            type: node.type?.getText(this.sourceFile),
            modifiers: this.getModifiers(node),
            isExported: false,
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children: []
        };
    }

    private extractProperty(node: ts.PropertyDeclaration): ExtractedSymbol {
        const location = this.getLocation(node);
        
        return {
            name: node.name.getText(this.sourceFile),
            kind: 'property',
            ...location,
            type: node.type?.getText(this.sourceFile),
            modifiers: this.getModifiers(node),
            isExported: false,
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children: []
        };
    }

    private extractVariableStatement(node: ts.VariableStatement): ExtractedSymbol | null {
        const declaration = node.declarationList.declarations[0];
        if (!declaration) return null;

        const location = this.getLocation(node);
        const isConst = (node.declarationList.flags & ts.NodeFlags.Const) !== 0;
        
        return {
            name: declaration.name.getText(this.sourceFile),
            kind: isConst ? 'const' : 'variable',
            ...location,
            type: declaration.type?.getText(this.sourceFile),
            modifiers: this.getModifiers(node),
            isExported: this.hasExportModifier(node),
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children: []
        };
    }

    private extractEnum(node: ts.EnumDeclaration): ExtractedSymbol {
        const location = this.getLocation(node);
        
        const children = node.members.map(member => ({
            name: member.name.getText(this.sourceFile),
            kind: 'enumMember' as const,
            ...this.getLocation(member),
            type: member.initializer?.getText(this.sourceFile),
            modifiers: [] as string[],
            isExported: false,
            children: []
        }));

        return {
            name: node.name.getText(this.sourceFile),
            kind: 'enum',
            ...location,
            modifiers: this.getModifiers(node),
            isExported: this.hasExportModifier(node),
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children
        };
    }

    private extractTypeAlias(node: ts.TypeAliasDeclaration): ExtractedSymbol {
        const location = this.getLocation(node);
        
        return {
            name: node.name.getText(this.sourceFile),
            kind: 'typeAlias',
            ...location,
            type: node.type.getText(this.sourceFile),
            modifiers: this.getModifiers(node),
            isExported: this.hasExportModifier(node),
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children: []
        };
    }

    private extractConstructor(node: ts.ConstructorDeclaration): ExtractedSymbol {
        const location = this.getLocation(node);
        
        return {
            name: 'constructor',
            kind: 'constructor',
            ...location,
            signature: this.getConstructorSignature(node),
            modifiers: this.getModifiers(node),
            isExported: false,
            jsDoc: this.options.includeJSDoc ? extractJSDoc(node, this.sourceFile) : undefined,
            children: []
        };
    }

    private getLocation(node: ts.Node): { startLine: number; endLine: number; startColumn: number; endColumn: number } {
        const start = this.sourceFile.getLineAndCharacterOfPosition(node.getStart(this.sourceFile));
        const end = this.sourceFile.getLineAndCharacterOfPosition(node.getEnd());
        
        return {
            startLine: start.line + 1,
            endLine: end.line + 1,
            startColumn: start.character + 1,
            endColumn: end.character + 1
        };
    }

    private getModifiers(node: ts.Node): string[] {
        const modifiers: string[] = [];
        const mods = ts.canHaveModifiers(node) ? ts.getModifiers(node) : undefined;
        
        if (mods) {
            for (const mod of mods) {
                switch (mod.kind) {
                    case ts.SyntaxKind.PublicKeyword: modifiers.push('public'); break;
                    case ts.SyntaxKind.PrivateKeyword: modifiers.push('private'); break;
                    case ts.SyntaxKind.ProtectedKeyword: modifiers.push('protected'); break;
                    case ts.SyntaxKind.StaticKeyword: modifiers.push('static'); break;
                    case ts.SyntaxKind.ReadonlyKeyword: modifiers.push('readonly'); break;
                    case ts.SyntaxKind.AbstractKeyword: modifiers.push('abstract'); break;
                    case ts.SyntaxKind.AsyncKeyword: modifiers.push('async'); break;
                    case ts.SyntaxKind.ExportKeyword: modifiers.push('export'); break;
                    case ts.SyntaxKind.DefaultKeyword: modifiers.push('default'); break;
                }
            }
        }
        
        return modifiers;
    }

    private hasExportModifier(node: ts.Node): boolean {
        const mods = ts.canHaveModifiers(node) ? ts.getModifiers(node) : undefined;
        return mods?.some(m => m.kind === ts.SyntaxKind.ExportKeyword) ?? false;
    }

    private getClassSignature(node: ts.ClassDeclaration): string {
        const name = node.name?.getText(this.sourceFile) ?? '<anonymous>';
        const typeParams = node.typeParameters 
            ? `<${node.typeParameters.map(p => p.getText(this.sourceFile)).join(', ')}>`
            : '';
        const heritage = node.heritageClauses?.map(c => c.getText(this.sourceFile)).join(' ') ?? '';
        return `class ${name}${typeParams}${heritage ? ' ' + heritage : ''}`;
    }

    private getFunctionSignature(node: ts.FunctionDeclaration): string {
        const name = node.name?.getText(this.sourceFile) ?? '<anonymous>';
        const typeParams = node.typeParameters
            ? `<${node.typeParameters.map(p => p.getText(this.sourceFile)).join(', ')}>`
            : '';
        const params = node.parameters.map(p => p.getText(this.sourceFile)).join(', ');
        const returnType = node.type ? `: ${node.type.getText(this.sourceFile)}` : '';
        const async = node.modifiers?.some(m => m.kind === ts.SyntaxKind.AsyncKeyword) ? 'async ' : '';
        return `${async}function ${name}${typeParams}(${params})${returnType}`;
    }

    private getMethodSignature(node: ts.MethodDeclaration): string {
        const name = node.name.getText(this.sourceFile);
        const typeParams = node.typeParameters
            ? `<${node.typeParameters.map(p => p.getText(this.sourceFile)).join(', ')}>`
            : '';
        const params = node.parameters.map(p => p.getText(this.sourceFile)).join(', ');
        const returnType = node.type ? `: ${node.type.getText(this.sourceFile)}` : '';
        const async = node.modifiers?.some(m => m.kind === ts.SyntaxKind.AsyncKeyword) ? 'async ' : '';
        return `${async}${name}${typeParams}(${params})${returnType}`;
    }

    private getConstructorSignature(node: ts.ConstructorDeclaration): string {
        const params = node.parameters.map(p => p.getText(this.sourceFile)).join(', ');
        return `constructor(${params})`;
    }
}
```

---

### 9. JSDoc Extractor

```typescript
// tools/ts-extractor/src/jsdoc.ts
import * as ts from 'typescript';
import { JSDocInfo, JSDocTag } from './types';

export function extractJSDoc(node: ts.Node, sourceFile: ts.SourceFile): JSDocInfo | undefined {
    const jsDocs = ts.getJSDocCommentsAndTags(node);
    
    if (jsDocs.length === 0) {
        return undefined;
    }

    let description: string | undefined;
    const tags: JSDocTag[] = [];

    for (const doc of jsDocs) {
        if (ts.isJSDoc(doc)) {
            if (doc.comment) {
                description = getCommentText(doc.comment);
            }
            
            if (doc.tags) {
                for (const tag of doc.tags) {
                    tags.push(extractTag(tag, sourceFile));
                }
            }
        }
    }

    if (!description && tags.length === 0) {
        return undefined;
    }

    return { description, tags };
}

function extractTag(tag: ts.JSDocTag, sourceFile: ts.SourceFile): JSDocTag {
    const tagName = tag.tagName.getText(sourceFile);
    
    if (ts.isJSDocParameterTag(tag)) {
        return {
            name: 'param',
            type: tag.name.getText(sourceFile),
            text: tag.comment ? getCommentText(tag.comment) : undefined
        };
    }
    
    if (ts.isJSDocReturnTag(tag)) {
        return {
            name: 'returns',
            type: tag.typeExpression?.getText(sourceFile),
            text: tag.comment ? getCommentText(tag.comment) : undefined
        };
    }
    
    if (ts.isJSDocThrowsTag(tag)) {
        return {
            name: 'throws',
            type: tag.typeExpression?.getText(sourceFile),
            text: tag.comment ? getCommentText(tag.comment) : undefined
        };
    }
    
    if (ts.isJSDocTypedefTag(tag)) {
        return {
            name: 'typedef',
            type: tag.name?.getText(sourceFile),
            text: tag.comment ? getCommentText(tag.comment) : undefined
        };
    }

    return {
        name: tagName,
        text: tag.comment ? getCommentText(tag.comment) : undefined
    };
}

function getCommentText(comment: string | ts.NodeArray<ts.JSDocComment>): string {
    if (typeof comment === 'string') {
        return comment;
    }
    
    return comment.map(c => {
        if (typeof c === 'string') return c;
        return c.getText();
    }).join('');
}
```

---

### 10. TypeScript Types

```typescript
// tools/ts-extractor/src/types.ts
export interface RequestEnvelope<T> {
    id: number;
    type: string;
    payload: T;
}

export interface ResponseEnvelope<T = unknown> {
    id: number;
    payload?: T;
    error?: string;
}

export interface ExtractionRequest {
    filePath: string;
    includeJSDoc: boolean;
    includePrivateMembers: boolean;
    includeTypeInfo: boolean;
    maxDepth: number;
}

export interface ExtractionResponse {
    filePath: string;
    symbols: ExtractedSymbol[];
    errors: ExtractionError[];
    isPartial: boolean;
    parseTimeMs: number;
    extractTimeMs: number;
}

export interface ExtractedSymbol {
    name: string;
    kind: string;
    startLine: number;
    endLine: number;
    startColumn: number;
    endColumn: number;
    signature?: string;
    type?: string;
    modifiers: string[];
    isExported: boolean;
    jsDoc?: JSDocInfo;
    children: ExtractedSymbol[];
    properties?: Record<string, string>;
}

export interface JSDocInfo {
    description?: string;
    tags: JSDocTag[];
}

export interface JSDocTag {
    name: string;
    type?: string;
    text?: string;
}

export interface ExtractionError {
    code: string;
    message: string;
    line?: number;
    column?: number;
}

export interface VisitorOptions {
    includeJSDoc: boolean;
    includePrivateMembers: boolean;
    includeTypeInfo: boolean;
    maxDepth: number;
}
```

---

### 11. package.json for ts-extractor

```json
{
    "name": "ts-extractor",
    "version": "1.0.0",
    "description": "TypeScript symbol extractor for Acode",
    "main": "dist/index.js",
    "scripts": {
        "build": "tsc",
        "watch": "tsc --watch",
        "test": "jest",
        "clean": "rimraf dist"
    },
    "dependencies": {
        "typescript": "^5.3.0"
    },
    "devDependencies": {
        "@types/node": "^20.10.0",
        "jest": "^29.7.0",
        "@types/jest": "^29.5.0",
        "ts-jest": "^29.1.0",
        "rimraf": "^5.0.0"
    },
    "engines": {
        "node": ">=18.0.0"
    }
}
```

---

### 12. DI Registration

```csharp
// src/Acode.Infrastructure/DependencyInjection/TypeScriptServiceExtensions.cs
using Acode.Application.Interfaces;
using Acode.Infrastructure.Symbols.TypeScript;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acode.Infrastructure.DependencyInjection;

public static class TypeScriptServiceExtensions
{
    public static IServiceCollection AddTypeScriptSymbolExtraction(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<NodeBridgeOptions>(
            configuration.GetSection(NodeBridgeOptions.SectionName));
        
        services.AddSingleton<NodeBridge>();
        services.AddSingleton<TypeScriptSymbolMapper>();
        services.AddSingleton<TypeScriptSymbolExtractor>();
        services.AddSingleton<ISymbolExtractor>(sp => 
            sp.GetRequiredService<TypeScriptSymbolExtractor>());
        
        return services;
    }
}
```

---

### Error Codes Reference

| Code | Severity | Meaning | Resolution |
|------|----------|---------|------------|
| ACODE-TSE-001 | Warning | TypeScript/JavaScript syntax error | Fix syntax error in source file |
| ACODE-TSE-002 | Error | Node.js bridge communication error | Check Node.js process, restart agent |
| ACODE-TSE-003 | Fatal | Node.js not found | Install Node.js 18+ and ensure it's in PATH |
| ACODE-TSE-004 | Error | Generic extraction error | Check logs for details |
| ACODE-TSE-005 | Error | Unsupported file extension | Only .ts/.tsx/.js/.jsx/.mts/.cts/.mjs/.cjs supported |
| ACODE-TSE-006 | Error | File not found | Verify file path exists |
| ACODE-TSE-007 | Error | Invalid JSON response | Internal error, restart agent |
| ACODE-TSE-008 | Fatal | ts-extractor script not found | Run `npm run build` in tools/ts-extractor |
| ACODE-TSE-009 | Error | Bridge timeout on startup | Increase startup timeout, check Node.js |
| ACODE-TSE-010 | Error | Empty response from bridge | Bridge crashed, will auto-restart |
| ACODE-TSE-011 | Error | Response deserialization failed | Internal error |
| ACODE-TSE-012 | Error | Response ID mismatch | Internal error, restart agent |
| ACODE-TSE-013 | Error | Bridge returned error | Check error message for details |
| ACODE-TSE-014 | Fatal | Max restart attempts exceeded | Check Node.js installation, review logs |
| ACODE-TSE-015 | Error | Failed to read source file | Check file permissions |

---

### Implementation Checklist

1. [x] Create NodeBridgeOptions configuration class
2. [x] Create NodeBridge with process lifecycle management
3. [x] Create message protocol DTOs (request/response envelopes)
4. [x] Create TypeScriptSymbolExtractor implementing ISymbolExtractor
5. [x] Create TypeScriptSymbolMapper for domain entity conversion
6. [x] Create ts-extractor Node.js entry point
7. [x] Create SymbolVisitor for AST traversal
8. [x] Implement extraction for all symbol types
9. [x] Create JSDoc extractor for documentation
10. [x] Create TypeScript type definitions
11. [x] Create package.json with dependencies
12. [x] Create DI registration extension

### Rollout Plan

| Phase | Description | Duration | Deliverable |
|-------|-------------|----------|-------------|
| 1 | Node.js bridge infrastructure | 2 days | NodeBridge.cs, options, DI |
| 2 | Message protocol | 1 day | Request/Response DTOs, JSON handling |
| 3 | TypeScript extractor entry point | 1 day | index.ts, protocol.ts |
| 4 | Symbol visitor (basic) | 2 days | Classes, functions, interfaces |
| 5 | Symbol visitor (complete) | 2 days | All symbol types, modifiers |
| 6 | JSDoc extraction | 1 day | jsdoc.ts with all tag types |
| 7 | .NET mapper | 1 day | TypeScriptSymbolMapper.cs |
| 8 | Integration & testing | 2 days | E2E tests, edge cases |

---

**End of Task 017.b Specification**