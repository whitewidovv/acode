# Task 002a - Gap Analysis and Implementation Checklist

## 📋 TASK OVERVIEW

**Task**: Task-002a: Define Schema + Examples
**Spec**: docs/tasks/refined-tasks/Epic 00/task-002a-define-schema-examples.md (870 lines)
**Date**: 2026-01-11
**Status**: STARTING - Gap Analysis Phase

## ✅ WHAT EXISTS

All 11 deliverables exist but have **3 CRITICAL SEMANTIC GAPS**:

1. ✅ data/config-schema.json (13.7 KB, 457 lines)
2. ✅ docs/config-examples/minimal.yml (26 lines)
3. ✅ docs/config-examples/full.yml (115 lines)
4. ✅ docs/config-examples/dotnet.yml (59 lines)
5. ✅ docs/config-examples/node.yml (44 lines)
6. ✅ docs/config-examples/python.yml (45 lines)
7. ✅ docs/config-examples/go.yml (38 lines)
8. ✅ docs/config-examples/rust.yml (38 lines)
9. ✅ docs/config-examples/java.yml (39 lines)
10. ✅ docs/config-examples/invalid.yml (81 lines)
11. ✅ docs/config-examples/README.md (282 lines)

### CRITICAL BLOCKERS IDENTIFIED:

### Blocker #1: Schema Syntax Violation (Draft 2020-12)
**Status**: ❌ MUST FIX
**Impact**: HIGH - Breaks spec compliance
**Issue**:
- Line 41: Uses `"definitions"` instead of `"$defs"` (Draft 04/07 syntax, not 2020-12)
- 17 instances of `"$ref": "#/definitions/..."` should be `"$ref": "#/$defs/..."`
**Requirements Violated**:
- FR-002a-01: Schema MUST use JSON Schema Draft 2020-12 - FAILED
- FR-002a-08: Schema MUST use $defs for reuse - FAILED
- FR-002a-09: Schema MUST use $ref for references - PARTIALLY FAILED

### Blocker #2: schema_version Pattern Missing
**Status**: ❌ MUST FIX
**Impact**: HIGH - Prevents version evolution
**Issue**:
- Lines 10-15: Uses `"enum": ["1.0.0"]` instead of `"pattern": "^\\d+\\.\\d+\\.\\d+$"`
- Cannot validate future versions like "1.0.1", "1.1.0", "2.0.0"
**Requirements Violated**:
- FR-002a-26: schema_version MUST be string pattern - FAILED
- FR-002a-27: schema_version pattern MUST be semver - FAILED
- FR-002a-21: Breaking changes MUST increment major - BLOCKED

### Blocker #3: No Validation Tests
**Status**: ❌ MUST ADD
**Impact**: CRITICAL - Cannot verify examples work
**Issue**:
- ZERO test files exist to validate schema and examples
- Cannot verify examples actually validate against schema
- Cannot verify invalid example properly fails
**Requirements Violated**:
- FR-002a-72: All examples MUST pass validation - UNTESTED
- FR-002a-80: Examples MUST be tested in CI - FAILED
- NFR-002a-05: Schema MUST be tested - FAILED
- All 20 testing acceptance criteria - FAILED

### Minor Issue #4: Storage Naming Inconsistency
**Status**: ⚠️ SHOULD CLARIFY
**Impact**: MEDIUM - Spec/implementation mismatch
**Issue**:
- Spec line 120: `retry_policy: (max_attempts, backoff)`
- Implementation: `backoff_ms` (added "_ms" suffix)
**Recommendation**: Document decision, update spec or schema consistently

## ❌ GAPS TO IMPLEMENT

Based on the spec, these deliverables need to be created:

### Gap #1: JSON Schema File
**File**: `data/config-schema.json`
**Requirements**: FR-002a-01 through FR-002a-55
**Content**:
- JSON Schema Draft 2020-12 with $schema declaration
- $id: https://acode.dev/schemas/config-v1.json
- Title and description
- Required properties: ["schema_version"]
- All property definitions with types, defaults, descriptions
- $defs for reusable definitions:
  - project (name, type, languages, description)
  - mode (default, allow_burst, airgapped_lock)
  - model (provider, name, endpoint, parameters, timeout, retry)
  - commands (setup, build, test, lint, format, start)
  - command (string | array | object with command/timeout/env)
  - paths (source, tests, output, docs)
  - ignore (patterns, additional)
  - network (allowlist with host/ports/reason)
  - storage (NEW: mode, local, remote, sync config per lines 108-125)
- Enums: project.type, mode.default
- Patterns: schema_version (semver), project.name
- Constraints: temperature 0-2, max_tokens > 0, top_p 0-1
- additionalProperties: false at root level

### Gap #2: Minimal Example
**File**: `docs/config-examples/minimal.yml`
**Requirements**: FR-002a-56, FR-002a-57
**Content**:
- schema_version: "1.0.0"
- project.name and project.type only
- Comments explaining minimal requirements

### Gap #3: Full Example
**File**: `docs/config-examples/full.yml`
**Requirements**: FR-002a-58, FR-002a-59
**Content**:
- All possible configuration options
- Comments explaining each section
- Demonstrates all features

### Gap #4: .NET Example
**File**: `docs/config-examples/dotnet.yml`
**Requirements**: FR-002a-60, FR-002a-61
**Content**:
- Realistic .NET project config
- dotnet commands (restore, build, test, format)
- C#/F# languages
- Typical paths (src/, tests/, bin/, obj/)
- .NET-specific ignore patterns

### Gap #5: Node.js Example
**File**: `docs/config-examples/node.yml`
**Requirements**: FR-002a-62, FR-002a-63
**Content**:
- Realistic Node.js project config
- npm commands
- TypeScript/JavaScript languages
- Typical paths (src/, tests/, dist/)
- Node-specific ignore patterns (node_modules/)

### Gap #6: Python Example
**File**: `docs/config-examples/python.yml`
**Requirements**: FR-002a-64, FR-002a-65
**Content**:
- Realistic Python project config
- pip/pytest/ruff commands
- Python language
- Typical paths (src/, tests/)
- Python-specific ignore patterns (__pycache__/, .venv/)

### Gap #7: Go Example
**File**: `docs/config-examples/go.yml`
**Requirements**: FR-002a-66, FR-002a-67
**Content**:
- Realistic Go project config
- go commands (build, test, fmt, vet)
- Go language
- Typical paths (cmd/, pkg/, internal/)
- Go-specific ignore patterns

### Gap #8: Rust Example
**File**: `docs/config-examples/rust.yml`
**Requirements**: FR-002a-68, FR-002a-69
**Content**:
- Realistic Rust project config
- cargo commands
- Rust language
- Typical paths (src/, target/)
- Rust-specific ignore patterns (target/)

### Gap #9: Java Example
**File**: `docs/config-examples/java.yml`
**Requirements**: FR-002a-70, FR-002a-71
**Content**:
- Realistic Java project config
- maven/gradle commands
- Java language
- Typical paths (src/main/, src/test/, target/)
- Java-specific ignore patterns

### Gap #10: Invalid Example
**File**: `docs/config-examples/invalid.yml`
**Requirements**: FR-002a-75, FR-002a-76
**Content**:
- Common configuration errors
- Comments explaining why each is invalid
- schema_version as number (should be string)
- project.name with spaces (invalid pattern)
- project.type with invalid value
- mode.default = "burst" (not allowed)
- allow_burst as string (should be boolean)
- temperature out of range
- max_tokens negative

---

## 🎯 IMPLEMENTATION PLAN

### Phase 1: Check What Exists
1. [🔄] Check if data/config-schema.json exists
2. [🔄] Check if docs/config-examples/ directory exists
3. [🔄] Check if any example files exist
4. [🔄] Document findings in "WHAT EXISTS" section above

### Phase 2: Create JSON Schema (TDD)
5. [ ] Write schema validation tests FIRST (Red)
   - Test: Schema is valid JSON
   - Test: Schema passes JSON Schema meta-validation
   - Test: Schema has required $schema declaration
   - Test: Schema has $id
   - Test: Schema has title and description
   - Test: Required properties list includes schema_version
6. [ ] Create data/config-schema.json with structure (Green)
7. [ ] Verify tests pass (Green)
8. [ ] Refactor schema for clarity

### Phase 3: Create Example Files (TDD)
9. [ ] Write example validation tests FIRST (Red)
   - Test: Minimal example validates against schema
   - Test: Full example validates against schema
   - Test: Each language example validates
   - Test: Invalid example FAILS validation with specific errors
10. [ ] Create docs/config-examples/minimal.yml (Green)
11. [ ] Create docs/config-examples/full.yml (Green)
12. [ ] Create docs/config-examples/dotnet.yml (Green)
13. [ ] Create docs/config-examples/node.yml (Green)
14. [ ] Create docs/config-examples/python.yml (Green)
15. [ ] Create docs/config-examples/go.yml (Green)
16. [ ] Create docs/config-examples/rust.yml (Green)
17. [ ] Create docs/config-examples/java.yml (Green)
18. [ ] Create docs/config-examples/invalid.yml (Green)
19. [ ] Verify all example tests pass

### Phase 4: Schema Property Definitions
20. [ ] Add project section to schema
21. [ ] Add mode section to schema
22. [ ] Add model section to schema
23. [ ] Add commands section to schema
24. [ ] Add paths section to schema
25. [ ] Add ignore section to schema
26. [ ] Add network section to schema
27. [ ] Add storage section to schema (NEW from lines 108-125)
28. [ ] Verify all properties have descriptions
29. [ ] Verify all properties have types
30. [ ] Verify all defaults specified

### Phase 5: Schema Constraints
31. [ ] Add semver pattern for schema_version
32. [ ] Add name pattern for project.name
33. [ ] Add enum for project.type
34. [ ] Add enum for mode.default (exclude "burst")
35. [ ] Add temperature constraint (0-2)
36. [ ] Add max_tokens constraint (> 0)
37. [ ] Add top_p constraint (0-1)
38. [ ] Add timeout_seconds constraint (> 0)
39. [ ] Add retry_count constraint (>= 0)
40. [ ] Verify all constraints work

### Phase 6: $defs and $refs
41. [ ] Define project in $defs
42. [ ] Define mode in $defs
43. [ ] Define model in $defs
44. [ ] Define commands in $defs
45. [ ] Define command in $defs
46. [ ] Define paths in $defs
47. [ ] Define ignore in $defs
48. [ ] Define network in $defs
49. [ ] Define storage in $defs (NEW)
50. [ ] Use $ref to reference all definitions
51. [ ] Verify no circular references

### Phase 7: Testing and Validation
52. [ ] Test: Schema validates against JSON Schema meta-schema
53. [ ] Test: Schema is valid JSON
54. [ ] Test: Minimal example passes validation
55. [ ] Test: Full example passes validation
56. [ ] Test: All language examples pass validation
57. [ ] Test: Invalid example fails validation
58. [ ] Test: Missing schema_version fails
59. [ ] Test: Invalid mode.default fails
60. [ ] Test: Invalid temperature fails
61. [ ] Test: Unknown field warnings work
62. [ ] Test: Pattern validation works
63. [ ] Test: Enum validation works
64. [ ] Verify schema size < 100KB
65. [ ] Verify validation performance < 100ms

### Phase 8: Documentation
66. [ ] Add comments to schema file
67. [ ] Add comments to all examples
68. [ ] Add README.md in docs/config-examples/
69. [ ] Document IDE integration in README
70. [ ] Document schema URL in README
71. [ ] Spell-check all files

### Phase 9: Integration
72. [ ] Verify schema file path is correct (data/config-schema.json)
73. [ ] Verify example directory is correct (docs/config-examples/)
74. [ ] Verify all cross-references work
75. [ ] Verify schema $id URL is correct
76. [ ] Update README.md to link to schema and examples

### Phase 10: Final Validation
77. [ ] All 80 functional requirements satisfied
78. [ ] All 75 acceptance criteria satisfied
79. [ ] All tests passing
80. [ ] Build clean (0 errors, 0 warnings)
81. [ ] Spell-check complete
82. [ ] Documentation complete

### Phase 11: Commit and PR
83. [ ] Commit all schema and example files
84. [ ] Push to feature branch
85. [ ] Create PR
86. [ ] Verify PR template shows checklist
87. [ ] Get approval and merge

---

## COMPLETION CRITERIA

Task 002a is complete when:

- [ ] data/config-schema.json exists with all 80 requirements (FR-002a-01 to 80)
- [ ] All 9 example files exist in docs/config-examples/
- [ ] Schema passes JSON Schema meta-validation
- [ ] All valid examples pass validation against schema
- [ ] Invalid example fails validation with helpful errors
- [ ] Schema uses Draft 2020-12
- [ ] All properties have descriptions
- [ ] All constraints are correct
- [ ] Storage section added (NEW requirements)
- [ ] $defs and $refs used correctly
- [ ] No circular references
- [ ] Schema size < 100KB
- [ ] All 75 acceptance criteria met
- [ ] All tests passing
- [ ] Documentation complete
- [ ] PR created and approved

---

## NOTES

This is a schema definition task - no code implementation, but comprehensive schema validation tests are required per CLAUDE.md Section 3.3 (TDD is MANDATORY). The schema is the specification that Tasks 002.b (parser) and 002.c (commands) will implement against.

**Status Legend:**
- `[ ]` = TODO
- `[🔄]` = IN PROGRESS
- `[✅]` = COMPLETE

---

**END OF CHECKLIST**
