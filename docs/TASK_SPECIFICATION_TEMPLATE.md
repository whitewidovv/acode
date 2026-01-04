# Task Specification Template & Section Requirements

**Created:** 2026-01-04  
**Purpose:** Define the canonical structure, required sections, content expectations, and quality standards for all task specifications. This document serves as the meta-description against which all tasks are audited and remediated.

---

## Overview

This document defines every section that MUST appear in a task specification. Sections are derived from comprehensive analysis of Epics 0-4 (the gold-standard specifications). If a section does not apply to a specific task, it MUST still appear with "N/A" and an explanation.

**Target Line Counts:**
- Main Tasks: 1,200 - 1,800 lines
- Subtasks: 800 - 1,200 lines
- Epic Overview: 300 - 500 lines

---

## Required Sections (Ordered)

### 1. Title Block (Lines 1-8)

**Purpose:** Immediate task identification and metadata  
**Format:**
```markdown
# Task XXX: Task Title

**Priority:** P0/P1/P2 – Description  
**Tier:** S/F/L – Layer Name  
**Complexity:** N (Fibonacci points)  
**Phase:** Phase N – Phase Name  
**Dependencies:** Task XXX (Name), Task YYY (Name)  

---
```

**Requirements:**
- Title MUST include task ID and descriptive name
- Priority MUST be P0/P1/P2 with meaning
- Tier MUST be S (Core), F (Foundation), or L (Feature)
- Complexity MUST be Fibonacci (1, 2, 3, 5, 8, 13)
- Phase MUST reference the epic phase
- Dependencies MUST list task IDs AND names

---

### 2. Description Section (Lines 10-100+)

**Purpose:** Comprehensive explanation of what the task delivers and why  
**Required Subsections:**

#### 2.1 Overview/Business Value (6-12 paragraphs)
- **Content:** Why this task matters, what problems it solves
- **Style:** Narrative prose, not bullets
- **Length:** 150-300 words minimum
- **Must Include:** Numbered list of 5-8 value propositions

#### 2.2 Scope (2-4 paragraphs)
- **Content:** What this task delivers (numbered list of 6-10 items)
- **Style:** Specific deliverables, not vague descriptions
- **Length:** 100-200 words

#### 2.3 Integration Points Table
- **Format:** 4-column table (Component | Integration Type | Description)
- **Minimum:** 8-12 rows
- **Must Include:** Task IDs for cross-references

#### 2.4 Failure Modes Table
- **Format:** 4-column table (Failure | Detection | Impact | Recovery)
- **Minimum:** 8-12 rows
- **Must Include:** Realistic failure scenarios

#### 2.5 Assumptions (Numbered list)
- **Minimum:** 10 items
- **Content:** Prerequisites, environment, dependencies
- **Style:** Declarative statements

#### 2.6 Security Considerations (Numbered list OR prose)
- **Minimum:** 8 items
- **Content:** Auth, secrets, audit, access control, data protection
- **Style:** MUST address security implications

---

### 3. Glossary / Terms (Table)

**Purpose:** Define domain-specific vocabulary  
**Format:**
```markdown
| Term | Definition |
|------|------------|
| Term1 | Clear, concise definition |
```

**Requirements:**
- Minimum: 10-15 terms
- All technical terms used in spec MUST be defined
- Definitions MUST be self-contained (no circular references)

---

### 4. Out of Scope (Bullet List)

**Purpose:** Explicit boundaries to prevent scope creep  
**Format:**
```markdown
## Out of Scope

The following items are explicitly excluded from Task XXX:

- **Item** - See Task YYY
- **Item** - Future version
- **Item** - Different layer
```

**Requirements:**
- Minimum: 6-10 items
- Each item SHOULD reference where it IS covered
- Use bold for item names

---

### 5. Functional Requirements (Tables)

**Purpose:** Precise, testable requirements  
**Format:**
```markdown
## Functional Requirements

### Category Name (FR-XXX-01 to FR-XXX-20)

| ID | Requirement |
|----|-------------|
| FR-XXX-01 | System MUST do X |
| FR-XXX-02 | System MUST NOT do Y |
```

**Requirements:**
- Minimum: 60-100 FRs for main tasks, 50-80 for subtasks
- MUST use RFC 2119 keywords (MUST, MUST NOT, SHOULD, MAY)
- MUST be grouped by category (4-6 categories)
- Each category: 15-25 FRs
- IDs MUST be unique: FR-{TaskID}-{Number}

---

### 6. Non-Functional Requirements (Tables)

**Purpose:** Quality attributes with measurable targets  
**Format:**
```markdown
## Non-Functional Requirements

### Performance (NFR-XXX-01 to NFR-XXX-10)

| ID | Requirement | Target | Maximum |
|----|-------------|--------|---------|
| NFR-XXX-01 | Operation latency | 10ms | 50ms |
```

**Requirements:**
- Minimum: 30-40 NFRs
- MUST include: Performance (10), Reliability (10), Observability (10)
- OPTIONAL categories: Security, Maintainability, Accuracy, Scalability
- Each NFR MUST have measurable Target value
- IDs MUST be unique: NFR-{TaskID}-{Number}

---

### 7. User Manual Documentation (200-400 lines)

**Purpose:** End-user guidance for configuration and usage  
**Required Subsections:**

#### 7.1 Overview (1-2 paragraphs)
Brief introduction to what this component does

#### 7.2 Quick Start (CLI examples)
```markdown
### Quick Start

```bash
# Basic command
acode command --option value

# Common use case
acode command --flag
```
```

#### 7.3 Configuration (YAML examples)
```markdown
### Configuration

Configure behavior in `.agent/config.yml`:

```yaml
section:
  option1: value
  option2: value
  nested:
    suboption: value
```
```

#### 7.4 CLI Commands (comprehensive examples)
- 10-20 example commands with comments
- Cover all flags and options
- Include example output

#### 7.5 Output Format (JSON/structured examples)
Show what output looks like for different scenarios

#### 7.6 Best Practices (numbered list)
8-12 recommendations for optimal usage

#### 7.7 Troubleshooting (4-8 problems)
Each problem MUST have:
- **Problem:** Description
- **Causes:** Numbered list
- **Solutions:** Numbered list
- **Example Fix:** Code if applicable

---

### 8. Acceptance Criteria (Grouped Checkboxes)

**Purpose:** Definition of done with specific, testable criteria  
**Format:**
```markdown
## Acceptance Criteria

### Category Name (AC-XXX-01 to AC-XXX-10)

- [ ] AC-XXX-01: Specific testable criterion
- [ ] AC-XXX-02: Another criterion
```

**Requirements:**
- Minimum: 60-80 ACs for main tasks, 40-60 for subtasks
- MUST be grouped by functional area (6-10 categories)
- Each criterion MUST be objectively testable
- Use past-tense or present-tense active voice

---

### 9. User Verification Steps / Scenarios (8-12 scenarios)

**Purpose:** Real-world usage scenarios for manual verification  
**Format:**
```markdown
## User Verification Steps

### Scenario N: Descriptive Title

**Objective:** What this scenario validates  
**Preconditions:** What must exist before starting  
**Steps:**
1. Step one
2. Step two
3. Step three
4. Step four

**Expected Results:**
- Expected outcome 1
- Expected outcome 2
- Expected outcome 3
```

**Requirements:**
- Minimum: 8 scenarios for main tasks, 6 for subtasks
- Cover happy path, error cases, edge cases
- Each scenario: 4-6 steps

---

### 10. Testing Requirements (Tables)

**Purpose:** Comprehensive test coverage specification  

#### 10.1 Unit Tests
```markdown
### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-XXX-01 | Test description | FR-XXX-01 |
```
- Minimum: 15-20 tests
- Each test MUST reference an FR or NFR

#### 10.2 Integration Tests
```markdown
### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-XXX-01 | Test description | FR-XXX-01, NFR-XXX-02 |
```
- Minimum: 15-20 tests
- MUST test cross-component interactions

#### 10.3 E2E Tests (when applicable)
- Minimum: 8-12 tests
- MUST cover full user workflows

#### 10.4 Performance Tests (when applicable)
- Minimum: 5-8 tests with specific targets
- MUST validate NFR performance requirements

---

### 11. Implementation Prompt (150-400 lines)

**Purpose:** Detailed implementation guidance for developers  
**Required Subsections:**

#### 11.1 File Structure (directory tree)
```markdown
### File Structure

```
src/
├── Acode.Domain/
│   └── ComponentName/
│       ├── Interface.cs
│       └── Model.cs
├── Acode.Application/
│   └── ComponentName/
│       └── Service.cs
├── Acode.Infrastructure/
│   └── ComponentName/
│       └── Implementation.cs
```
```

#### 11.2 Interface Contracts (code samples)
Full interface definitions with XML docs

#### 11.3 Domain Models (code samples)
Record/class definitions with properties

#### 11.4 Error Codes Table
| Code | Meaning | Resolution |
|------|---------|------------|

#### 11.5 Logging Fields (structured logging schema)
What fields are logged, at what level

#### 11.6 Implementation Checklist (numbered)
25-40 specific implementation steps

#### 11.7 Rollout Plan (table)
| Phase | Description | Duration | Success Criteria |

---

### 12. Footer

```markdown
---

**End of Task XXX Specification**
```

---

## Section Applicability

If a section does not apply to a task, include it with:

```markdown
### Section Name

**N/A:** This section does not apply because [specific reason].
```

Examples:
- Security Considerations N/A for pure data model tasks
- Troubleshooting N/A for internal-only components with no user interaction
- CLI Commands N/A for domain layer tasks

---

## Quality Metrics Summary

| Metric | Main Task | Subtask | Epic |
|--------|-----------|---------|------|
| Total Lines | 1,200-1,800 | 800-1,200 | 300-500 |
| Functional Requirements | 60-100 | 50-80 | N/A |
| Non-Functional Requirements | 30-40 | 25-35 | N/A |
| Acceptance Criteria | 60-80 | 40-60 | 30-50 |
| User Verification Scenarios | 8-12 | 6-10 | N/A |
| Unit Tests | 15-20 | 15-20 | N/A |
| Integration Tests | 15-20 | 12-18 | N/A |
| Troubleshooting Items | 4-8 | 4-6 | N/A |
| Glossary Terms | 10-15 | 8-12 | 10-15 |
| Assumptions | 10+ | 8+ | N/A |
| Security Considerations | 8+ | 6+ | N/A |
| Out of Scope Items | 6-10 | 6-8 | N/A |

---

## Validation Checklist

Before marking a task as complete, verify:

- [ ] All 12 major sections present
- [ ] Line count within target range
- [ ] All FR/NFR/AC IDs are unique and properly formatted
- [ ] All tables have proper column headers
- [ ] User Manual includes YAML config examples
- [ ] User Manual includes CLI examples
- [ ] Troubleshooting section has 4+ problems with causes/solutions
- [ ] Implementation Prompt includes file structure
- [ ] Implementation Prompt includes code samples
- [ ] All tests reference FRs/NFRs they validate
- [ ] No placeholder text (TBD, TODO, etc.)

---

**End of Template Document**
