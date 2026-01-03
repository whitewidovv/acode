# Task 004.a: Define Message/Tool-Call Types

**Priority:** P0 (Critical)  
**Tier:** Foundation  
**Complexity:** 8 (Fibonacci)  
**Phase:** 1 — Model Runtime, Inference, Tool-Calling Contract  
**Dependencies:** Task 001 (Operating Modes), Task 002 (Config Contract)  

---

## Description

### Business Value

Message and tool-call types are the fundamental data structures that represent conversations with language models. These types define how the system constructs prompts, represents model responses, and handles the tool-calling mechanism that enables agentic behavior. Without well-designed message types, the system cannot reliably communicate with models or execute tools.

The types defined in this task form the contract between the application layer and model providers. They MUST be provider-agnostic, supporting both Ollama and vLLM without modification. They MUST also be serialization-friendly, supporting JSON encoding for wire protocols and storage.

### Scope

This task defines the core message and tool-call types:

1. **MessageRole Enum:** Defines the four conversation roles (System, User, Assistant, Tool)
2. **ChatMessage Record:** Represents a single message in the conversation
3. **ToolCall Record:** Represents a model's request to invoke a tool
4. **ToolResult Record:** Represents the result of executing a tool
5. **ToolDefinition Record:** Describes a tool available to the model
6. **ToolCallDelta Record:** Represents streaming updates to a tool call
7. **ConversationHistory:** Ordered collection of messages with validation

### Integration Points

- **Task 004 (Model Provider Interface):** Uses these types in IModelProvider methods
- **Task 004.b (Response Format):** ChatResponse uses ChatMessage
- **Task 005 (Ollama Provider):** Serializes these types for Ollama API
- **Task 006 (vLLM Provider):** Serializes these types for vLLM API
- **Task 007 (Tool Schema):** ToolDefinition uses JSON Schema from Task 007
- **Task 009 (Routing):** Uses MessageRole for routing decisions

### Failure Modes

| Failure | Impact | Mitigation |
|---------|--------|------------|
| Invalid JSON in tool arguments | Tool execution fails | Schema validation, error handling |
| Message role mismatch | Model confusion | Strict typing, validation |
| Content and ToolCalls both null | Invalid message | Validation at construction |
| Tool call ID collision | Response routing fails | UUID generation |
| Serialization mismatch | Provider communication fails | Comprehensive tests |
| Unicode handling errors | Content corruption | UTF-8 normalization |

### Assumptions

1. All text content is UTF-8 encoded
2. Tool call IDs are unique within a session
3. Tool arguments are valid JSON objects
4. Messages are ordered chronologically
5. Content may be null when only tool calls are present
6. Tool results correspond to specific tool call IDs
7. Streaming may split tool calls across multiple chunks

### Design Principles

1. **Immutability:** All types are immutable records
2. **Nullable Reference Types:** Enabled for compile-time null safety
3. **Validation:** Types validate themselves at construction
4. **Serialization:** Types serialize to JSON without configuration
5. **Equality:** Records provide value-based equality

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| ChatMessage | Single message in a conversation with role, content, and optional tool calls |
| MessageRole | Enumeration: System, User, Assistant, or Tool |
| ToolCall | Model's structured request to invoke a specific tool with arguments |
| ToolResult | Structured response to a tool call with result or error |
| ToolDefinition | Describes a tool's name, description, and parameter schema |
| ToolCallDelta | Incremental update during streaming of a tool call |
| Content | Text body of a message (may be null for tool-only messages) |
| Arguments | JSON object containing parameters for a tool call |
| ToolCallId | Unique identifier linking a tool call to its result |
| Conversation | Ordered sequence of messages between user and model |
| System Prompt | Initial message defining agent behavior and constraints |
| Multi-turn | Conversation with multiple user/assistant exchanges |
| Streaming Delta | Incremental token(s) in a streaming response |
| Schema | JSON Schema defining valid structure for tool arguments |

---

## Out of Scope

The following items are explicitly NOT part of this task:

- Request/response types (Task 004.b)
- Provider registry (Task 004.c)
- JSON Schema definitions for tools (Task 007)
- Prompt templates (Task 008)
- Message persistence/storage
- Conversation compression
- Token counting
- Message truncation logic
- Multi-modal content (images, audio)
- Function calling (legacy format)
- Message encryption

---

## Functional Requirements

### MessageRole Enum (FR-004a-01 to FR-004a-10)

| ID | Requirement |
|----|-------------|
| FR-004a-01 | System MUST define MessageRole enum |
| FR-004a-02 | MessageRole MUST have System value |
| FR-004a-03 | MessageRole MUST have User value |
| FR-004a-04 | MessageRole MUST have Assistant value |
| FR-004a-05 | MessageRole MUST have Tool value |
| FR-004a-06 | MessageRole values MUST serialize to lowercase strings |
| FR-004a-07 | MessageRole MUST support case-insensitive parsing |
| FR-004a-08 | Unknown role string MUST throw ArgumentException |
| FR-004a-09 | MessageRole MUST have explicit integer values |
| FR-004a-10 | MessageRole MUST NOT have reserved/unused values |

### ChatMessage Record (FR-004a-11 to FR-004a-35)

| ID | Requirement |
|----|-------------|
| FR-004a-11 | System MUST define ChatMessage record |
| FR-004a-12 | ChatMessage MUST be immutable |
| FR-004a-13 | ChatMessage MUST have Role property (required) |
| FR-004a-14 | ChatMessage MUST have Content property (nullable) |
| FR-004a-15 | ChatMessage MUST have ToolCalls property (nullable) |
| FR-004a-16 | ChatMessage MUST have ToolCallId property (nullable) |
| FR-004a-17 | ToolCallId MUST be set when Role is Tool |
| FR-004a-18 | ToolCalls MUST be IReadOnlyList<ToolCall> |
| FR-004a-19 | Content OR ToolCalls MUST be non-null for Assistant |
| FR-004a-20 | Content MUST be non-null for User messages |
| FR-004a-21 | Content MUST be non-null for System messages |
| FR-004a-22 | Content MUST be non-null for Tool messages |
| FR-004a-23 | ChatMessage MUST support JSON serialization |
| FR-004a-24 | ChatMessage MUST support JSON deserialization |
| FR-004a-25 | Serialization MUST omit null properties |
| FR-004a-26 | Deserialization MUST handle missing properties |
| FR-004a-27 | ChatMessage MUST have factory methods |
| FR-004a-28 | Factory: CreateSystem(content) |
| FR-004a-29 | Factory: CreateUser(content) |
| FR-004a-30 | Factory: CreateAssistant(content, toolCalls) |
| FR-004a-31 | Factory: CreateToolResult(toolCallId, result, isError) |
| FR-004a-32 | ChatMessage MUST validate on construction |
| FR-004a-33 | Invalid messages MUST throw ArgumentException |
| FR-004a-34 | ChatMessage MUST implement value equality |
| FR-004a-35 | ChatMessage MUST have meaningful ToString() |

### ToolCall Record (FR-004a-36 to FR-004a-55)

| ID | Requirement |
|----|-------------|
| FR-004a-36 | System MUST define ToolCall record |
| FR-004a-37 | ToolCall MUST be immutable |
| FR-004a-38 | ToolCall MUST have Id property (required) |
| FR-004a-39 | Id MUST be non-empty string |
| FR-004a-40 | ToolCall MUST have Name property (required) |
| FR-004a-41 | Name MUST be non-empty string |
| FR-004a-42 | Name MUST contain only alphanumeric and underscore |
| FR-004a-43 | Name MUST be max 64 characters |
| FR-004a-44 | ToolCall MUST have Arguments property (required) |
| FR-004a-45 | Arguments MUST be JsonElement type |
| FR-004a-46 | Arguments MUST be valid JSON object |
| FR-004a-47 | ToolCall MUST support JSON serialization |
| FR-004a-48 | ToolCall MUST support JSON deserialization |
| FR-004a-49 | ToolCall MUST have TryGetArgument<T> method |
| FR-004a-50 | TryGetArgument MUST return false for missing keys |
| FR-004a-51 | ToolCall MUST have GetArgumentsAs<T> method |
| FR-004a-52 | GetArgumentsAs MUST deserialize entire arguments |
| FR-004a-53 | ToolCall MUST validate on construction |
| FR-004a-54 | Invalid ToolCall MUST throw ArgumentException |
| FR-004a-55 | ToolCall MUST implement value equality |

### ToolResult Record (FR-004a-56 to FR-004a-70)

| ID | Requirement |
|----|-------------|
| FR-004a-56 | System MUST define ToolResult record |
| FR-004a-57 | ToolResult MUST be immutable |
| FR-004a-58 | ToolResult MUST have ToolCallId property (required) |
| FR-004a-59 | ToolCallId MUST match corresponding ToolCall.Id |
| FR-004a-60 | ToolResult MUST have Result property (required) |
| FR-004a-61 | Result MUST be string (serialized output) |
| FR-004a-62 | Result MAY be empty string |
| FR-004a-63 | ToolResult MUST have IsError property |
| FR-004a-64 | IsError MUST default to false |
| FR-004a-65 | ToolResult MUST support JSON serialization |
| FR-004a-66 | ToolResult MUST validate on construction |
| FR-004a-67 | ToolResult MUST have factory: Success(id, result) |
| FR-004a-68 | ToolResult MUST have factory: Error(id, message) |
| FR-004a-69 | Error factory MUST set IsError to true |
| FR-004a-70 | ToolResult MUST implement value equality |

### ToolDefinition Record (FR-004a-71 to FR-004a-90)

| ID | Requirement |
|----|-------------|
| FR-004a-71 | System MUST define ToolDefinition record |
| FR-004a-72 | ToolDefinition MUST be immutable |
| FR-004a-73 | ToolDefinition MUST have Name property (required) |
| FR-004a-74 | Name MUST follow same rules as ToolCall.Name |
| FR-004a-75 | ToolDefinition MUST have Description property (required) |
| FR-004a-76 | Description MUST be non-empty |
| FR-004a-77 | Description SHOULD be max 1024 characters |
| FR-004a-78 | ToolDefinition MUST have Parameters property (required) |
| FR-004a-79 | Parameters MUST be JsonElement (JSON Schema) |
| FR-004a-80 | Parameters MUST be valid JSON Schema object |
| FR-004a-81 | Parameters MUST have type: "object" |
| FR-004a-82 | ToolDefinition MAY have Strict property |
| FR-004a-83 | Strict MUST default to true |
| FR-004a-84 | Strict=true enforces additionalProperties: false |
| FR-004a-85 | ToolDefinition MUST support JSON serialization |
| FR-004a-86 | Serialization MUST match provider formats |
| FR-004a-87 | ToolDefinition MUST validate on construction |
| FR-004a-88 | ToolDefinition MUST implement value equality |
| FR-004a-89 | ToolDefinition MUST have CreateFromType<T> method |
| FR-004a-90 | CreateFromType MUST generate schema from C# type |

### ToolCallDelta Record (FR-004a-91 to FR-004a-100)

| ID | Requirement |
|----|-------------|
| FR-004a-91 | System MUST define ToolCallDelta record |
| FR-004a-92 | ToolCallDelta MUST be immutable |
| FR-004a-93 | ToolCallDelta MUST have Index property |
| FR-004a-94 | Index identifies which tool call is being built |
| FR-004a-95 | ToolCallDelta MAY have Id property |
| FR-004a-96 | Id is present only in first delta for a tool call |
| FR-004a-97 | ToolCallDelta MAY have Name property |
| FR-004a-98 | Name is present only in first delta |
| FR-004a-99 | ToolCallDelta MAY have ArgumentsDelta property |
| FR-004a-100 | ArgumentsDelta is string (partial JSON) |

### Conversation History (FR-004a-101 to FR-004a-115)

| ID | Requirement |
|----|-------------|
| FR-004a-101 | System MUST define ConversationHistory class |
| FR-004a-102 | ConversationHistory MUST be thread-safe |
| FR-004a-103 | ConversationHistory MUST have Add method |
| FR-004a-104 | Add MUST validate message order |
| FR-004a-105 | First message MUST be System role |
| FR-004a-106 | User/Assistant MUST alternate (with Tool interjections) |
| FR-004a-107 | Tool messages MUST follow Assistant with ToolCalls |
| FR-004a-108 | ConversationHistory MUST have GetMessages method |
| FR-004a-109 | GetMessages MUST return IReadOnlyList<ChatMessage> |
| FR-004a-110 | ConversationHistory MUST have Clear method |
| FR-004a-111 | ConversationHistory MUST have Count property |
| FR-004a-112 | ConversationHistory MUST have LastMessage property |
| FR-004a-113 | ConversationHistory MUST support enumeration |
| FR-004a-114 | ConversationHistory MUST be serializable |
| FR-004a-115 | ConversationHistory MUST validate complete state |

---

## Non-Functional Requirements

### Performance (NFR-004a-01 to NFR-004a-12)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-004a-01 | Performance | Type construction MUST be < 1μs |
| NFR-004a-02 | Performance | JSON serialization MUST be < 1ms per message |
| NFR-004a-03 | Performance | JSON deserialization MUST be < 1ms per message |
| NFR-004a-04 | Performance | Memory per message MUST be < 1KB (excluding content) |
| NFR-004a-05 | Performance | No heap allocation for enum operations |
| NFR-004a-06 | Performance | Validation MUST be O(1) |
| NFR-004a-07 | Performance | Equality check MUST be O(n) on content length |
| NFR-004a-08 | Performance | ConversationHistory add MUST be O(1) amortized |
| NFR-004a-09 | Performance | Factory methods MUST not allocate unnecessarily |
| NFR-004a-10 | Performance | Use System.Text.Json source generators |
| NFR-004a-11 | Performance | Avoid string concatenation in hot paths |
| NFR-004a-12 | Performance | Tool arguments MUST NOT be re-parsed |

### Security (NFR-004a-13 to NFR-004a-22)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-004a-13 | Security | Content MUST be treated as untrusted |
| NFR-004a-14 | Security | Tool arguments MUST be validated |
| NFR-004a-15 | Security | No code execution in constructors |
| NFR-004a-16 | Security | No file system access in types |
| NFR-004a-17 | Security | No network access in types |
| NFR-004a-18 | Security | JSON parsing MUST handle malformed input |
| NFR-004a-19 | Security | Unicode normalization MUST be consistent |
| NFR-004a-20 | Security | No sensitive data in ToString() |
| NFR-004a-21 | Security | Exception messages MUST NOT leak content |
| NFR-004a-22 | Security | Stack traces MUST NOT contain message content |

### Reliability (NFR-004a-23 to NFR-004a-32)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-004a-23 | Reliability | Immutable types prevent race conditions |
| NFR-004a-24 | Reliability | Null checks at boundaries |
| NFR-004a-25 | Reliability | ArgumentException for invalid input |
| NFR-004a-26 | Reliability | JsonException for parse failures |
| NFR-004a-27 | Reliability | Types MUST handle edge cases |
| NFR-004a-28 | Reliability | Empty string content is valid |
| NFR-004a-29 | Reliability | Very long content is valid |
| NFR-004a-30 | Reliability | Unicode surrogates handled correctly |
| NFR-004a-31 | Reliability | JSON with unexpected properties handled |
| NFR-004a-32 | Reliability | ConversationHistory concurrent access safe |

### Maintainability (NFR-004a-33 to NFR-004a-40)

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-004a-33 | Maintainability | Types in Domain layer |
| NFR-004a-34 | Maintainability | XML documentation on all public members |
| NFR-004a-35 | Maintainability | Nullable reference types enabled |
| NFR-004a-36 | Maintainability | Records use init-only properties |
| NFR-004a-37 | Maintainability | No public setters |
| NFR-004a-38 | Maintainability | Factory methods document preconditions |
| NFR-004a-39 | Maintainability | Unit tests cover all validation |
| NFR-004a-40 | Maintainability | Examples in documentation |

---

## User Manual Documentation

### Overview

The message and tool-call types define how conversations are structured when communicating with language models. These types are the foundation for all model interactions in Agentic Coding Bot.

### Message Roles

Every message in a conversation has a role:

| Role | Description | Example |
|------|-------------|---------|
| `System` | Defines agent behavior | "You are a helpful coding assistant..." |
| `User` | User input or request | "Write a hello world program" |
| `Assistant` | Model's response | "Here's a hello world program..." |
| `Tool` | Result from tool execution | "File created successfully" |

### Creating Messages

#### System Message

```csharp
var systemMessage = ChatMessage.CreateSystem(
    "You are a helpful coding assistant specializing in C#."
);
```

#### User Message

```csharp
var userMessage = ChatMessage.CreateUser(
    "Write a function that calculates fibonacci numbers."
);
```

#### Assistant Message (Text Only)

```csharp
var assistantMessage = ChatMessage.CreateAssistant(
    "Here's a function that calculates fibonacci numbers:\n\n```csharp\n..."
);
```

#### Assistant Message (With Tool Calls)

```csharp
var toolCall = new ToolCall(
    Id: "call_001",
    Name: "write_file",
    Arguments: JsonSerializer.SerializeToElement(new {
        path = "fibonacci.cs",
        content = "public static int Fib(int n) { ... }"
    })
);

var assistantMessage = ChatMessage.CreateAssistant(
    content: null,
    toolCalls: new[] { toolCall }
);
```

#### Tool Result Message

```csharp
var toolResult = ChatMessage.CreateToolResult(
    toolCallId: "call_001",
    result: "File written successfully to fibonacci.cs",
    isError: false
);
```

### Working with Tool Calls

#### Extracting Arguments

```csharp
// Get a specific argument
if (toolCall.TryGetArgument<string>("path", out var path))
{
    Console.WriteLine($"Path: {path}");
}

// Get all arguments as a typed object
var args = toolCall.GetArgumentsAs<WriteFileArgs>();
Console.WriteLine($"Path: {args.Path}, Content length: {args.Content.Length}");
```

#### Creating Tool Definitions

```csharp
// Manual definition
var toolDef = new ToolDefinition(
    Name: "write_file",
    Description: "Writes content to a file at the specified path",
    Parameters: JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "path": { "type": "string", "description": "File path" },
            "content": { "type": "string", "description": "File content" }
        },
        "required": ["path", "content"]
    }
    """).RootElement
);

// From C# type (recommended)
var toolDef = ToolDefinition.CreateFromType<WriteFileArgs>(
    name: "write_file",
    description: "Writes content to a file at the specified path"
);
```

### Conversation Management

```csharp
var conversation = new ConversationHistory();

// Add system prompt
conversation.Add(ChatMessage.CreateSystem("You are a helpful assistant."));

// Add user message
conversation.Add(ChatMessage.CreateUser("Hello!"));

// Get messages for API call
IReadOnlyList<ChatMessage> messages = conversation.GetMessages();

// Check state
Console.WriteLine($"Messages: {conversation.Count}");
Console.WriteLine($"Last: {conversation.LastMessage?.Role}");
```

### Serialization

All types serialize to JSON compatible with model provider APIs:

```csharp
// Serialize
var json = JsonSerializer.Serialize(message);

// Deserialize
var message = JsonSerializer.Deserialize<ChatMessage>(json);
```

#### JSON Format Examples

**User Message:**
```json
{
  "role": "user",
  "content": "Write a hello world program"
}
```

**Assistant with Tool Calls:**
```json
{
  "role": "assistant",
  "content": null,
  "tool_calls": [
    {
      "id": "call_001",
      "name": "write_file",
      "arguments": {
        "path": "hello.cs",
        "content": "Console.WriteLine(\"Hello, World!\");"
      }
    }
  ]
}
```

**Tool Result:**
```json
{
  "role": "tool",
  "tool_call_id": "call_001",
  "content": "File written successfully"
}
```

### Validation Rules

Messages are validated on construction:

| Rule | Error |
|------|-------|
| Role is undefined | ArgumentException |
| User message without content | ArgumentException |
| System message without content | ArgumentException |
| Tool message without ToolCallId | ArgumentException |
| Tool message without content | ArgumentException |
| Assistant with neither content nor tool calls | ArgumentException |
| ToolCall with empty name | ArgumentException |
| ToolCall name with invalid characters | ArgumentException |
| ToolCall arguments not an object | ArgumentException |

### Best Practices

1. **Use Factory Methods:** Prefer `CreateUser()`, `CreateAssistant()`, etc.
2. **Validate Early:** Catch validation errors at message creation
3. **Immutability:** Never try to modify messages after creation
4. **Tool Call IDs:** Use unique IDs (UUIDs recommended)
5. **Content Length:** Be aware of model context limits

### Troubleshooting

#### ArgumentException on Message Creation

Check that:
- Content is provided for User, System, Tool roles
- ToolCallId is set for Tool messages
- Assistant has content OR tool calls (or both)

#### JsonException on Deserialization

Check that:
- JSON has valid syntax
- Role value is one of: system, user, assistant, tool
- tool_calls is an array of objects
- arguments is a JSON object

---

## Acceptance Criteria

### MessageRole Enum

- [ ] AC-001: MessageRole enum defined
- [ ] AC-002: System value exists
- [ ] AC-003: User value exists
- [ ] AC-004: Assistant value exists
- [ ] AC-005: Tool value exists
- [ ] AC-006: Serializes to lowercase
- [ ] AC-007: Case-insensitive parsing works
- [ ] AC-008: Unknown string throws ArgumentException
- [ ] AC-009: Explicit integer values assigned
- [ ] AC-010: No reserved values

### ChatMessage Record

- [ ] AC-011: ChatMessage defined
- [ ] AC-012: ChatMessage is immutable
- [ ] AC-013: Role property exists (required)
- [ ] AC-014: Content property exists (nullable)
- [ ] AC-015: ToolCalls property exists (nullable)
- [ ] AC-016: ToolCallId property exists (nullable)
- [ ] AC-017: ToolCallId set for Tool role
- [ ] AC-018: ToolCalls is IReadOnlyList
- [ ] AC-019: Assistant requires content or toolCalls
- [ ] AC-020: User requires content
- [ ] AC-021: System requires content
- [ ] AC-022: Tool requires content
- [ ] AC-023: JSON serialization works
- [ ] AC-024: JSON deserialization works
- [ ] AC-025: Null properties omitted in JSON
- [ ] AC-026: Missing properties handled in deserialization
- [ ] AC-027: Factory methods exist
- [ ] AC-028: CreateSystem works
- [ ] AC-029: CreateUser works
- [ ] AC-030: CreateAssistant works
- [ ] AC-031: CreateToolResult works
- [ ] AC-032: Validation on construction
- [ ] AC-033: Invalid throws ArgumentException
- [ ] AC-034: Value equality works
- [ ] AC-035: ToString is meaningful

### ToolCall Record

- [ ] AC-036: ToolCall defined
- [ ] AC-037: ToolCall is immutable
- [ ] AC-038: Id property exists (required)
- [ ] AC-039: Id is non-empty
- [ ] AC-040: Name property exists (required)
- [ ] AC-041: Name is non-empty
- [ ] AC-042: Name has valid characters
- [ ] AC-043: Name max 64 characters
- [ ] AC-044: Arguments property exists (required)
- [ ] AC-045: Arguments is JsonElement
- [ ] AC-046: Arguments is JSON object
- [ ] AC-047: JSON serialization works
- [ ] AC-048: JSON deserialization works
- [ ] AC-049: TryGetArgument<T> works
- [ ] AC-050: TryGetArgument returns false for missing
- [ ] AC-051: GetArgumentsAs<T> works
- [ ] AC-052: GetArgumentsAs deserializes correctly
- [ ] AC-053: Validation on construction
- [ ] AC-054: Invalid throws ArgumentException
- [ ] AC-055: Value equality works

### ToolResult Record

- [ ] AC-056: ToolResult defined
- [ ] AC-057: ToolResult is immutable
- [ ] AC-058: ToolCallId property exists (required)
- [ ] AC-059: ToolCallId matches ToolCall.Id
- [ ] AC-060: Result property exists (required)
- [ ] AC-061: Result is string
- [ ] AC-062: Result may be empty
- [ ] AC-063: IsError property exists
- [ ] AC-064: IsError defaults to false
- [ ] AC-065: JSON serialization works
- [ ] AC-066: Validation on construction
- [ ] AC-067: Success factory works
- [ ] AC-068: Error factory works
- [ ] AC-069: Error sets IsError to true
- [ ] AC-070: Value equality works

### ToolDefinition Record

- [ ] AC-071: ToolDefinition defined
- [ ] AC-072: ToolDefinition is immutable
- [ ] AC-073: Name property exists (required)
- [ ] AC-074: Name follows naming rules
- [ ] AC-075: Description property exists (required)
- [ ] AC-076: Description is non-empty
- [ ] AC-077: Description max 1024 chars
- [ ] AC-078: Parameters property exists (required)
- [ ] AC-079: Parameters is JsonElement
- [ ] AC-080: Parameters is JSON Schema object
- [ ] AC-081: Parameters has type: object
- [ ] AC-082: Strict property exists
- [ ] AC-083: Strict defaults to true
- [ ] AC-084: Strict enforces additionalProperties
- [ ] AC-085: JSON serialization works
- [ ] AC-086: Matches provider formats
- [ ] AC-087: Validation on construction
- [ ] AC-088: Value equality works
- [ ] AC-089: CreateFromType<T> works
- [ ] AC-090: CreateFromType generates schema

### ToolCallDelta Record

- [ ] AC-091: ToolCallDelta defined
- [ ] AC-092: ToolCallDelta is immutable
- [ ] AC-093: Index property exists
- [ ] AC-094: Index identifies tool call
- [ ] AC-095: Id property exists (nullable)
- [ ] AC-096: Id present in first delta
- [ ] AC-097: Name property exists (nullable)
- [ ] AC-098: Name present in first delta
- [ ] AC-099: ArgumentsDelta property exists (nullable)
- [ ] AC-100: ArgumentsDelta is partial JSON

### ConversationHistory

- [ ] AC-101: ConversationHistory defined
- [ ] AC-102: Thread-safe operations
- [ ] AC-103: Add method works
- [ ] AC-104: Add validates order
- [ ] AC-105: First message must be System
- [ ] AC-106: User/Assistant alternate
- [ ] AC-107: Tool follows Assistant with ToolCalls
- [ ] AC-108: GetMessages works
- [ ] AC-109: GetMessages returns IReadOnlyList
- [ ] AC-110: Clear method works
- [ ] AC-111: Count property works
- [ ] AC-112: LastMessage property works
- [ ] AC-113: Enumeration works
- [ ] AC-114: Serialization works
- [ ] AC-115: Validation of complete state

### Performance

- [ ] AC-116: Construction < 1μs
- [ ] AC-117: Serialization < 1ms
- [ ] AC-118: Deserialization < 1ms
- [ ] AC-119: Memory < 1KB per message (base)
- [ ] AC-120: No enum heap allocation
- [ ] AC-121: Validation is O(1)

### Security

- [ ] AC-122: Content treated as untrusted
- [ ] AC-123: Tool arguments validated
- [ ] AC-124: No code execution in types
- [ ] AC-125: No sensitive data in ToString

### Documentation

- [ ] AC-126: XML documentation complete
- [ ] AC-127: Examples provided
- [ ] AC-128: Factory methods documented
- [ ] AC-129: Validation rules documented
- [ ] AC-130: JSON format documented

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Domain/Models/Messages/
├── MessageRoleTests.cs
│   ├── Should_Have_System_Value()
│   ├── Should_Have_User_Value()
│   ├── Should_Have_Assistant_Value()
│   ├── Should_Have_Tool_Value()
│   ├── Should_Serialize_To_Lowercase()
│   ├── Should_Parse_CaseInsensitive()
│   └── Should_Throw_For_Unknown()
│
├── ChatMessageTests.cs
│   ├── Should_Be_Immutable()
│   ├── CreateSystem_Should_Work()
│   ├── CreateUser_Should_Work()
│   ├── CreateAssistant_Should_Work()
│   ├── CreateToolResult_Should_Work()
│   ├── Should_Require_Content_For_User()
│   ├── Should_Require_Content_For_System()
│   ├── Should_Require_ToolCallId_For_Tool()
│   ├── Should_Allow_ToolCalls_Only_For_Assistant()
│   ├── Should_Serialize_To_Json()
│   ├── Should_Deserialize_From_Json()
│   ├── Should_Omit_Null_Properties()
│   ├── Should_Implement_Value_Equality()
│   └── Should_Have_Meaningful_ToString()
│
├── ToolCallTests.cs
│   ├── Should_Be_Immutable()
│   ├── Should_Require_NonEmpty_Id()
│   ├── Should_Require_NonEmpty_Name()
│   ├── Should_Validate_Name_Characters()
│   ├── Should_Validate_Name_Length()
│   ├── Should_Require_Object_Arguments()
│   ├── TryGetArgument_Should_Return_Value()
│   ├── TryGetArgument_Should_Return_False_For_Missing()
│   ├── GetArgumentsAs_Should_Deserialize()
│   ├── Should_Serialize_To_Json()
│   └── Should_Deserialize_From_Json()
│
├── ToolResultTests.cs
│   ├── Should_Be_Immutable()
│   ├── Should_Require_ToolCallId()
│   ├── Should_Allow_Empty_Result()
│   ├── Should_Default_IsError_False()
│   ├── Success_Factory_Should_Work()
│   ├── Error_Factory_Should_SetIsError()
│   ├── Should_Serialize_To_Json()
│   └── Should_Deserialize_From_Json()
│
├── ToolDefinitionTests.cs
│   ├── Should_Be_Immutable()
│   ├── Should_Validate_Name()
│   ├── Should_Require_Description()
│   ├── Should_Require_Object_Schema()
│   ├── Should_Default_Strict_True()
│   ├── CreateFromType_Should_Generate_Schema()
│   └── Should_Serialize_To_Json()
│
├── ToolCallDeltaTests.cs
│   ├── Should_Be_Immutable()
│   ├── Should_Have_Index()
│   ├── Should_Allow_Partial_Properties()
│   └── Should_Support_ArgumentsDelta()
│
└── ConversationHistoryTests.cs
    ├── Should_Start_Empty()
    ├── Should_Require_System_First()
    ├── Should_Validate_Message_Order()
    ├── Should_Allow_Tool_After_ToolCalls()
    ├── Should_Track_Count()
    ├── Should_Return_LastMessage()
    ├── Should_Support_Clear()
    ├── Should_Be_ThreadSafe()
    └── Should_Serialize()
```

### Integration Tests

```
Tests/Integration/Models/Messages/
├── SerializationCompatibilityTests.cs
│   ├── Should_Match_Ollama_Format()
│   ├── Should_Match_vLLM_Format()
│   ├── Should_Handle_Provider_Extensions()
│   └── Should_Roundtrip_All_Types()
```

### Performance Tests

```
Tests/Performance/Models/Messages/
├── MessageBenchmarks.cs
│   ├── Benchmark_Construction()
│   ├── Benchmark_Serialization()
│   ├── Benchmark_Deserialization()
│   ├── Benchmark_Equality()
│   └── Benchmark_ConversationAdd()
```

---

## User Verification Steps

### Scenario 1: Create System Message

1. Call `ChatMessage.CreateSystem("You are helpful.")`
2. Verify Role is System
3. Verify Content is set
4. Verify ToolCalls is null
5. Verify ToString includes role and content

### Scenario 2: Create User Message

1. Call `ChatMessage.CreateUser("Hello")`
2. Verify Role is User
3. Verify Content is "Hello"
4. Verify message is immutable

### Scenario 3: Create Assistant with Tool Calls

1. Create ToolCall with name and arguments
2. Call `ChatMessage.CreateAssistant(null, toolCalls)`
3. Verify Role is Assistant
4. Verify Content is null
5. Verify ToolCalls contains the tool call

### Scenario 4: Tool Call Argument Extraction

1. Create ToolCall with JSON arguments
2. Call `TryGetArgument<string>("path", out var path)`
3. Verify returns true and value is correct
4. Call with missing key
5. Verify returns false

### Scenario 5: JSON Serialization Roundtrip

1. Create various message types
2. Serialize to JSON
3. Deserialize back to message
4. Verify equality with original

### Scenario 6: Validation Errors

1. Try to create User message with null content
2. Verify ArgumentException thrown
3. Try to create Tool message without ToolCallId
4. Verify ArgumentException thrown

### Scenario 7: ConversationHistory Ordering

1. Create ConversationHistory
2. Add System message (should succeed)
3. Add User message (should succeed)
4. Add another User message (should fail)
5. Add Assistant message (should succeed)

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/Models/Messages/
├── MessageRole.cs
├── ChatMessage.cs
├── ToolCall.cs
├── ToolResult.cs
├── ToolDefinition.cs
├── ToolCallDelta.cs
├── ConversationHistory.cs
└── Serialization/
    └── MessageJsonContext.cs
```

### MessageRole Implementation

```csharp
namespace AgenticCoder.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter<MessageRole>))]
public enum MessageRole
{
    [JsonPropertyName("system")]
    System = 0,
    
    [JsonPropertyName("user")]
    User = 1,
    
    [JsonPropertyName("assistant")]
    Assistant = 2,
    
    [JsonPropertyName("tool")]
    Tool = 3
}
```

### ChatMessage Implementation

```csharp
namespace AgenticCoder.Domain.Models;

public sealed record ChatMessage
{
    public required MessageRole Role { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; init; }
    
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ToolCall>? ToolCalls { get; init; }
    
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; init; }
    
    public static ChatMessage CreateSystem(string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        return new ChatMessage { Role = MessageRole.System, Content = content };
    }
    
    public static ChatMessage CreateUser(string content)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);
        return new ChatMessage { Role = MessageRole.User, Content = content };
    }
    
    public static ChatMessage CreateAssistant(
        string? content,
        IReadOnlyList<ToolCall>? toolCalls = null)
    {
        if (content is null && (toolCalls is null || toolCalls.Count == 0))
            throw new ArgumentException("Assistant must have content or tool calls");
        return new ChatMessage
        {
            Role = MessageRole.Assistant,
            Content = content,
            ToolCalls = toolCalls
        };
    }
    
    public static ChatMessage CreateToolResult(
        string toolCallId,
        string result,
        bool isError = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolCallId);
        ArgumentNullException.ThrowIfNull(result);
        return new ChatMessage
        {
            Role = MessageRole.Tool,
            ToolCallId = toolCallId,
            Content = result
        };
    }
}
```

### Implementation Checklist

1. [ ] Define MessageRole enum with JSON attributes
2. [ ] Define ChatMessage record with validation
3. [ ] Implement factory methods for ChatMessage
4. [ ] Define ToolCall record with argument helpers
5. [ ] Define ToolResult record with factories
6. [ ] Define ToolDefinition record with schema
7. [ ] Define ToolCallDelta record
8. [ ] Implement ConversationHistory
9. [ ] Add JSON source generator context
10. [ ] Write unit tests for all types
11. [ ] Write serialization compatibility tests
12. [ ] Add XML documentation
13. [ ] Document JSON formats

### Dependencies

- Task 001 (Operating Modes) - no direct dependency
- Task 002 (Config) - no direct dependency
- System.Text.Json for serialization

### Verification Command

```bash
dotnet test --filter "FullyQualifiedName~Messages"
```

---

**End of Task 004.a Specification**