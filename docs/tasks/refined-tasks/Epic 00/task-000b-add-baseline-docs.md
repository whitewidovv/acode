# Task 000.b: Add Baseline Docs (README, REPO_STRUCTURE, CONFIG, OPERATING_MODES)

**Priority:** 2 / 49  
**Tier:** Foundation  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 0 — Foundation  
**Dependencies:** Task 000.a (requires repository structure to exist)  

---

## Description

### Overview

Task 000.b creates the foundational documentation for the Agentic Coding Bot (Acode) project. Documentation is not an afterthought—it is a critical deliverable that enables contributors, users, and automated systems to understand and interact with the codebase correctly. This task establishes four core documentation files that serve distinct purposes in the project lifecycle.

### Business Value

Comprehensive documentation provides exponential value:

1. **Reduces onboarding time** — New contributors can self-serve instead of asking questions
2. **Establishes shared understanding** — Everyone references the same authoritative source
3. **Prevents misconfiguration** — Clear config docs prevent trial-and-error debugging
4. **Enables autonomous agents** — AI coding agents read documentation to understand project context
5. **Supports compliance** — Auditors can review operating modes and constraints

The documentation created in this task becomes the canonical reference for all future development. Every decision about operating modes, configuration, and project structure flows from these documents.

### Scope Boundaries

**In Scope:**
- README.md with project overview, quick start, and contribution basics
- REPO_STRUCTURE.md documenting the canonical folder layout
- CONFIG.md explaining all configuration options
- OPERATING_MODES.md defining local-only, burst, and airgapped modes
- docs/ directory structure
- Basic architecture diagrams (as text/mermaid)

**Out of Scope:**
- API documentation (generated from code, Epic 1+)
- User tutorials and guides (future epics)
- Video content
- Translated documentation
- Marketing materials

### Integration Points

| Task | Relationship | Description |
|------|--------------|-------------|
| Task 000.a | Predecessor | Repository structure must exist |
| Task 000.c | Sibling | Tooling task may reference docs |
| Task 001 | Downstream | Operating modes defined here, implemented there |
| Task 002 | Downstream | Config format introduced here, formalized there |
| Task 003 | Downstream | Safety overview here, details there |

### Assumptions

1. Repository structure from Task 000.a is complete
2. Git is configured and initial commit exists
3. Markdown is the documentation format
4. English is the primary language
5. Documentation will be viewed on GitHub/GitLab

### Failure Modes

| Failure | Detection | Recovery |
|---------|-----------|----------|
| Invalid markdown syntax | Linter errors | Fix markdown syntax |
| Broken internal links | Link checker | Update paths |
| Outdated information | Manual review | Schedule reviews |
| Missing sections | Checklist audit | Add missing content |

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| **README.md** | Primary entry point documentation file at repository root |
| **Markdown** | Lightweight markup language for formatted text |
| **Mermaid** | Text-based diagramming syntax supported by GitHub |
| **Operating Mode** | Configuration that determines what operations are permitted |
| **Local-Only Mode** | Strictest mode; no network access, local models only |
| **Burst Mode** | Allows cloud compute but blocks external LLM APIs |
| **Airgapped Mode** | Complete network isolation for maximum security |
| **Clean Architecture** | Layered architecture pattern (Domain/Application/Infrastructure/CLI) |
| **ADR** | Architecture Decision Record documenting design decisions |
| **Quick Start** | Minimal steps to get running for the first time |
| **Configuration Precedence** | Order in which config sources override each other |
| **Environment Variable** | OS-level configuration mechanism |

---

## Out of Scope

- Creating API reference documentation
- Writing user tutorials or how-to guides
- Creating video documentation
- Translating documentation to other languages
- Creating marketing or promotional content
- Writing detailed implementation specifications
- Creating architecture diagrams beyond basic structure
- Setting up documentation hosting (GitHub Pages, etc.)

---

## Functional Requirements

### README.md (FR-000b-01 to FR-000b-25)

| ID | Requirement |
|----|-------------|
| FR-000b-01 | README.md MUST exist at repository root |
| FR-000b-02 | README.md MUST begin with project name as H1 heading |
| FR-000b-03 | README.md MUST include a project description (2-4 paragraphs) |
| FR-000b-04 | README.md MUST include a badges section (build status placeholder, version) |
| FR-000b-05 | README.md MUST include a "Features" section listing key capabilities |
| FR-000b-06 | README.md MUST include a "Quick Start" section |
| FR-000b-07 | Quick Start MUST include prerequisites list |
| FR-000b-08 | Quick Start MUST include installation commands |
| FR-000b-09 | Quick Start MUST include first-run commands |
| FR-000b-10 | README.md MUST include a "Documentation" section with links |
| FR-000b-11 | README.md MUST include a "Contributing" section or link |
| FR-000b-12 | README.md MUST include a "License" section |
| FR-000b-13 | README.md MUST include a "Security" section or link |
| FR-000b-14 | README.md MUST use proper Markdown heading hierarchy |
| FR-000b-15 | README.md MUST NOT exceed 500 lines |
| FR-000b-16 | README.md MUST render correctly on GitHub |
| FR-000b-17 | README.md MUST include table of contents for navigation |
| FR-000b-18 | All links in README.md MUST be valid |
| FR-000b-19 | README.md MUST include operating mode overview |
| FR-000b-20 | README.md MUST reference OPERATING_MODES.md for details |
| FR-000b-21 | README.md MUST include project status (alpha/beta/stable) |
| FR-000b-22 | README.md MUST include .NET version requirements |
| FR-000b-23 | README.md MUST be spell-checked |
| FR-000b-24 | README.md MUST use consistent formatting |
| FR-000b-25 | README.md MUST NOT contain TODO placeholders |

### REPO_STRUCTURE.md (FR-000b-26 to FR-000b-45)

| ID | Requirement |
|----|-------------|
| FR-000b-26 | docs/REPO_STRUCTURE.md MUST exist |
| FR-000b-27 | REPO_STRUCTURE.md MUST document the complete folder hierarchy |
| FR-000b-28 | REPO_STRUCTURE.md MUST explain Clean Architecture layers |
| FR-000b-29 | REPO_STRUCTURE.md MUST include a tree diagram of the structure |
| FR-000b-30 | Each folder MUST have a description of its purpose |
| FR-000b-31 | Naming conventions MUST be documented |
| FR-000b-32 | File placement rules MUST be documented |
| FR-000b-33 | REPO_STRUCTURE.md MUST explain layer dependencies |
| FR-000b-34 | REPO_STRUCTURE.md MUST include a dependency diagram |
| FR-000b-35 | REPO_STRUCTURE.md MUST explain where to add new files |
| FR-000b-36 | REPO_STRUCTURE.md MUST explain where to add new projects |
| FR-000b-37 | REPO_STRUCTURE.md MUST explain namespace conventions |
| FR-000b-38 | REPO_STRUCTURE.md MUST be updated when structure changes |
| FR-000b-39 | REPO_STRUCTURE.md MUST match actual repository structure |
| FR-000b-40 | REPO_STRUCTURE.md MUST include example namespace paths |
| FR-000b-41 | REPO_STRUCTURE.md MUST explain test project organization |
| FR-000b-42 | REPO_STRUCTURE.md MUST explain docs folder organization |
| FR-000b-43 | REPO_STRUCTURE.md MUST be cross-referenced from README |
| FR-000b-44 | REPO_STRUCTURE.md MUST include version/date of last update |
| FR-000b-45 | REPO_STRUCTURE.md MUST NOT exceed 400 lines |

### CONFIG.md (FR-000b-46 to FR-000b-65)

| ID | Requirement |
|----|-------------|
| FR-000b-46 | docs/CONFIG.md MUST exist |
| FR-000b-47 | CONFIG.md MUST list all configuration sources |
| FR-000b-48 | CONFIG.md MUST explain configuration precedence |
| FR-000b-49 | CONFIG.md MUST document environment variables |
| FR-000b-50 | CONFIG.md MUST document .agent/config.yml structure |
| FR-000b-51 | CONFIG.md MUST document CLI flags |
| FR-000b-52 | Each configuration option MUST have a description |
| FR-000b-53 | Each configuration option MUST have a default value |
| FR-000b-54 | Each configuration option MUST have valid value range |
| FR-000b-55 | CONFIG.md MUST include example configurations |
| FR-000b-56 | CONFIG.md MUST explain how to validate configuration |
| FR-000b-57 | CONFIG.md MUST document config file locations |
| FR-000b-58 | CONFIG.md MUST explain config inheritance |
| FR-000b-59 | CONFIG.md MUST document sensitive configuration handling |
| FR-000b-60 | CONFIG.md MUST include troubleshooting section |
| FR-000b-61 | CONFIG.md MUST be cross-referenced from README |
| FR-000b-62 | CONFIG.md MUST include config file examples for common scenarios |
| FR-000b-63 | CONFIG.md MUST document which options affect security |
| FR-000b-64 | CONFIG.md MUST include version compatibility notes |
| FR-000b-65 | CONFIG.md MUST NOT contain hardcoded secrets |

### OPERATING_MODES.md (FR-000b-66 to FR-000b-90)

| ID | Requirement |
|----|-------------|
| FR-000b-66 | docs/OPERATING_MODES.md MUST exist |
| FR-000b-67 | OPERATING_MODES.md MUST define three modes: LocalOnly, Burst, Airgapped |
| FR-000b-68 | Each mode MUST have a detailed description |
| FR-000b-69 | Each mode MUST have a "when to use" section |
| FR-000b-70 | Each mode MUST have an "allowed operations" list |
| FR-000b-71 | Each mode MUST have a "blocked operations" list |
| FR-000b-72 | OPERATING_MODES.md MUST include a comparison matrix |
| FR-000b-73 | OPERATING_MODES.md MUST explain mode selection |
| FR-000b-74 | OPERATING_MODES.md MUST explain mode switching |
| FR-000b-75 | OPERATING_MODES.md MUST document mode validation |
| FR-000b-76 | OPERATING_MODES.md MUST explain enforcement mechanisms |
| FR-000b-77 | OPERATING_MODES.md MUST document external LLM API blocking |
| FR-000b-78 | OPERATING_MODES.md MUST list blocked API endpoints |
| FR-000b-79 | OPERATING_MODES.md MUST explain local model requirements |
| FR-000b-80 | OPERATING_MODES.md MUST document cloud compute in Burst mode |
| FR-000b-81 | OPERATING_MODES.md MUST explain network isolation in Airgapped |
| FR-000b-82 | OPERATING_MODES.md MUST include use case scenarios |
| FR-000b-83 | OPERATING_MODES.md MUST include security considerations |
| FR-000b-84 | OPERATING_MODES.md MUST explain audit implications per mode |
| FR-000b-85 | OPERATING_MODES.md MUST document mode precedence (CLI > env > config) |
| FR-000b-86 | OPERATING_MODES.md MUST include decision flowchart |
| FR-000b-87 | OPERATING_MODES.md MUST explain error handling for violations |
| FR-000b-88 | OPERATING_MODES.md MUST be cross-referenced from README |
| FR-000b-89 | OPERATING_MODES.md MUST include FAQ section |
| FR-000b-90 | OPERATING_MODES.md MUST NOT exceed 600 lines |

---

## Non-Functional Requirements

### Quality (NFR-000b-01 to NFR-000b-15)

| ID | Requirement |
|----|-------------|
| NFR-000b-01 | All documentation MUST pass markdownlint |
| NFR-000b-02 | All documentation MUST be spell-checked |
| NFR-000b-03 | All links MUST be validated |
| NFR-000b-04 | Documentation MUST be written in clear, simple English |
| NFR-000b-05 | Technical terms MUST be defined or linked |
| NFR-000b-06 | Code examples MUST be syntax-highlighted |
| NFR-000b-07 | Code examples MUST be tested/validated |
| NFR-000b-08 | Diagrams MUST render correctly on GitHub |
| NFR-000b-09 | Tables MUST be properly formatted |
| NFR-000b-10 | Heading hierarchy MUST be consistent |
| NFR-000b-11 | No orphaned headings (heading without content) |
| NFR-000b-12 | Line length SHOULD be under 120 characters |
| NFR-000b-13 | Consistent terminology throughout |
| NFR-000b-14 | Active voice preferred over passive |
| NFR-000b-15 | Imperative mood for instructions |

### Accessibility (NFR-000b-16 to NFR-000b-20)

| ID | Requirement |
|----|-------------|
| NFR-000b-16 | Images MUST have alt text |
| NFR-000b-17 | Color MUST NOT be only differentiator |
| NFR-000b-18 | Tables MUST have headers |
| NFR-000b-19 | Code blocks MUST have language specified |
| NFR-000b-20 | Links MUST have descriptive text |

### Maintainability (NFR-000b-21 to NFR-000b-30)

| ID | Requirement |
|----|-------------|
| NFR-000b-21 | Documentation MUST be versioned with code |
| NFR-000b-22 | Each doc file MUST have last-updated date |
| NFR-000b-23 | Cross-references MUST use relative paths |
| NFR-000b-24 | No duplicate content across files |
| NFR-000b-25 | Changes to structure MUST update REPO_STRUCTURE.md |
| NFR-000b-26 | Changes to config MUST update CONFIG.md |
| NFR-000b-27 | Changes to modes MUST update OPERATING_MODES.md |
| NFR-000b-28 | Documentation reviews MUST be part of PR process |
| NFR-000b-29 | Breaking changes MUST be documented |
| NFR-000b-30 | Deprecations MUST be clearly marked |

---

## User Manual Documentation

### Documentation Structure

```
acode/
├── README.md                    # Entry point, overview
├── CONTRIBUTING.md              # How to contribute (Task 000.c)
├── LICENSE                      # License file
├── SECURITY.md                  # Security policy
└── docs/
    ├── REPO_STRUCTURE.md        # Folder layout documentation
    ├── CONFIG.md                # Configuration reference
    ├── OPERATING_MODES.md       # Mode definitions
    ├── architecture/            # Architecture docs
    │   └── overview.md
    └── adr/                     # Architecture Decision Records
        └── 001-clean-architecture.md
```

### README.md Template

```markdown
# Acode - Agentic Coding Bot

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]()
[![Version](https://img.shields.io/badge/version-0.1.0--alpha-blue)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()

> A locally-hosted, privacy-first AI coding assistant that operates entirely 
> within your infrastructure.

## Table of Contents

- [Features](#features)
- [Quick Start](#quick-start)
- [Documentation](#documentation)
- [Operating Modes](#operating-modes)
- [Contributing](#contributing)
- [License](#license)

## Features

- 🔒 **Privacy-First**: All data stays on your machine
- 🤖 **Local Models**: Works with Ollama, vLLM, and other local providers
- 🛡️ **Safe by Default**: Conservative permissions, explicit approvals
- 📊 **Auditable**: Full audit trail of all operations
- 🔧 **Configurable**: Flexible configuration for your workflow

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- [Git](https://git-scm.com/)
- Local model provider (e.g., [Ollama](https://ollama.ai/))

### Installation

\`\`\`bash
git clone https://github.com/your-org/acode.git
cd acode
dotnet restore
dotnet build
\`\`\`

### First Run

\`\`\`bash
dotnet run --project src/Acode.Cli
\`\`\`

## Documentation

| Document | Description |
|----------|-------------|
| [REPO_STRUCTURE](docs/REPO_STRUCTURE.md) | Project folder layout |
| [CONFIG](docs/CONFIG.md) | Configuration reference |
| [OPERATING_MODES](docs/OPERATING_MODES.md) | Mode definitions |

## Operating Modes

Acode supports three operating modes:

| Mode | Network | External LLM APIs | Use Case |
|------|---------|-------------------|----------|
| **LocalOnly** | ❌ | ❌ | Maximum privacy |
| **Burst** | ✅ | ❌ | Cloud compute, local models |
| **Airgapped** | ❌ | ❌ | Complete isolation |

See [OPERATING_MODES.md](docs/OPERATING_MODES.md) for details.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

This project is licensed under the MIT License - see [LICENSE](LICENSE).
```

### Creating Documentation

To create or update documentation:

1. **Navigate to the docs directory**
   ```bash
   cd docs/
   ```

2. **Create or edit the file**
   ```bash
   # Use your preferred editor
   code REPO_STRUCTURE.md
   ```

3. **Validate markdown**
   ```bash
   npx markdownlint "**/*.md"
   ```

4. **Check links**
   ```bash
   npx markdown-link-check README.md
   ```

5. **Commit changes**
   ```bash
   git add .
   git commit -m "docs: update REPO_STRUCTURE.md"
   ```

### Best Practices

1. **Keep README.md focused** — Link to detailed docs instead of duplicating
2. **Use relative links** — Ensures links work when repo is cloned
3. **Include examples** — Code examples are more valuable than descriptions
4. **Update consistently** — Outdated docs are worse than no docs
5. **Write for your audience** — Technical accuracy + accessibility

---

## Acceptance Criteria / Definition of Done

### README.md (40 items)

- [ ] README.md exists at repository root
- [ ] Project name is H1 heading
- [ ] Project description is 2-4 paragraphs
- [ ] Badges section present (build, version, .NET, license)
- [ ] Table of contents present
- [ ] Features section with bullet points
- [ ] Quick Start section present
- [ ] Prerequisites listed
- [ ] Installation commands provided
- [ ] First run example provided
- [ ] Documentation section with table of links
- [ ] Link to REPO_STRUCTURE.md works
- [ ] Link to CONFIG.md works
- [ ] Link to OPERATING_MODES.md works
- [ ] Operating Modes overview section present
- [ ] Mode comparison table included
- [ ] Contributing section or link present
- [ ] License section present
- [ ] Security section or link present
- [ ] Renders correctly on GitHub
- [ ] No broken links
- [ ] Under 500 lines
- [ ] Spell-checked
- [ ] No TODO placeholders
- [ ] Proper heading hierarchy
- [ ] Consistent formatting
- [ ] Code blocks have language
- [ ] All examples tested
- [ ] .NET version specified
- [ ] Project status indicated
- [ ] Markdown lint passes
- [ ] Accessible language
- [ ] Active voice used
- [ ] Imperative mood for instructions
- [ ] Cross-platform commands
- [ ] No hardcoded paths
- [ ] External links use HTTPS
- [ ] No duplicate content
- [ ] Proper markdown tables
- [ ] Alt text for any images

### REPO_STRUCTURE.md (35 items)

- [ ] File exists at docs/REPO_STRUCTURE.md
- [ ] Title and overview present
- [ ] Last updated date present
- [ ] Complete folder hierarchy documented
- [ ] Tree diagram included
- [ ] src/ folder explained
- [ ] tests/ folder explained
- [ ] docs/ folder explained
- [ ] Each subfolder has purpose description
- [ ] Clean Architecture layers explained
- [ ] Layer dependency diagram included
- [ ] Domain layer responsibilities documented
- [ ] Application layer responsibilities documented
- [ ] Infrastructure layer responsibilities documented
- [ ] CLI layer responsibilities documented
- [ ] Naming conventions documented
- [ ] Namespace conventions documented
- [ ] Example namespace paths provided
- [ ] File placement rules documented
- [ ] Where to add new entities documented
- [ ] Where to add new use cases documented
- [ ] Where to add new infrastructure documented
- [ ] How to add new projects documented
- [ ] Test project organization explained
- [ ] Matches actual structure
- [ ] Under 400 lines
- [ ] Cross-referenced from README
- [ ] Markdown lint passes
- [ ] Spell-checked
- [ ] No broken links
- [ ] Renders correctly on GitHub
- [ ] Tables properly formatted
- [ ] Code examples present
- [ ] No TODO placeholders
- [ ] Consistent terminology

### CONFIG.md (35 items)

- [ ] File exists at docs/CONFIG.md
- [ ] Title and overview present
- [ ] Configuration sources listed
- [ ] Precedence order documented (CLI > env > config > default)
- [ ] Environment variables section present
- [ ] ACODE_MODE documented
- [ ] All env vars have description
- [ ] All env vars have default value
- [ ] .agent/config.yml structure documented
- [ ] YAML examples provided
- [ ] CLI flags section present
- [ ] All flags documented
- [ ] Configuration file locations documented
- [ ] Config inheritance explained
- [ ] Config validation explained
- [ ] Sensitive configuration handling documented
- [ ] Security-affecting options marked
- [ ] Example configs for common scenarios
- [ ] .NET project config example
- [ ] Node.js project config example
- [ ] Python project config example
- [ ] Minimal config example
- [ ] Troubleshooting section present
- [ ] Common errors documented
- [ ] Version compatibility notes
- [ ] No hardcoded secrets
- [ ] Cross-referenced from README
- [ ] Under 500 lines
- [ ] Markdown lint passes
- [ ] Spell-checked
- [ ] Renders correctly on GitHub
- [ ] Tables properly formatted
- [ ] YAML examples valid
- [ ] No TODO placeholders
- [ ] Consistent terminology

### OPERATING_MODES.md (45 items)

- [ ] File exists at docs/OPERATING_MODES.md
- [ ] Title and overview present
- [ ] Three modes defined: LocalOnly, Burst, Airgapped
- [ ] LocalOnly mode fully documented
- [ ] LocalOnly when-to-use section
- [ ] LocalOnly allowed operations list
- [ ] LocalOnly blocked operations list
- [ ] Burst mode fully documented
- [ ] Burst when-to-use section
- [ ] Burst allowed operations list
- [ ] Burst blocked operations list
- [ ] Airgapped mode fully documented
- [ ] Airgapped when-to-use section
- [ ] Airgapped allowed operations list
- [ ] Airgapped blocked operations list
- [ ] Mode comparison matrix present
- [ ] Mode selection explained
- [ ] CLI flag --mode documented
- [ ] Environment variable ACODE_MODE documented
- [ ] Config file mode setting documented
- [ ] Precedence order documented
- [ ] Mode switching explained
- [ ] Mode validation documented
- [ ] Enforcement mechanisms explained
- [ ] External LLM API blocking explained
- [ ] Blocked endpoints listed (OpenAI, Anthropic, etc.)
- [ ] Local model requirements documented
- [ ] Cloud compute in Burst explained
- [ ] Network isolation in Airgapped explained
- [ ] Use case scenarios provided
- [ ] Security considerations section
- [ ] Audit implications per mode documented
- [ ] Decision flowchart included
- [ ] Error handling for violations documented
- [ ] FAQ section present
- [ ] Under 600 lines
- [ ] Cross-referenced from README
- [ ] Markdown lint passes
- [ ] Spell-checked
- [ ] Renders correctly on GitHub
- [ ] Tables properly formatted
- [ ] Diagrams render correctly
- [ ] No TODO placeholders
- [ ] Consistent terminology
- [ ] MUST/MUST NOT language used

### Documentation Infrastructure (25 items)

- [ ] docs/ directory exists
- [ ] docs/architecture/ directory exists
- [ ] docs/adr/ directory exists
- [ ] At least one ADR written
- [ ] SECURITY.md exists at root
- [ ] LICENSE file present
- [ ] .github/ISSUE_TEMPLATE exists (or placeholder)
- [ ] .github/PULL_REQUEST_TEMPLATE.md exists (or placeholder)
- [ ] All files use UTF-8 encoding
- [ ] Consistent line endings (LF)
- [ ] No trailing whitespace
- [ ] No duplicate files
- [ ] No orphaned files
- [ ] Git commit created for documentation
- [ ] Commit message follows convention
- [ ] No merge conflicts
- [ ] Files tracked by Git
- [ ] Changes pass CI checks (when configured)
- [ ] Documentation reviewed
- [ ] Cross-links verified
- [ ] Internal consistency verified
- [ ] External consistency verified (matches code)
- [ ] Print-friendly (no excessive width)
- [ ] Mobile-friendly (readable on small screens)
- [ ] Dark mode compatible (no hardcoded colors)

---

## Testing Requirements

### Unit Tests

| ID | Test | Expected |
|----|------|----------|
| UT-000b-01 | README.md exists | File found at repo root |
| UT-000b-02 | README.md has H1 | Starts with # |
| UT-000b-03 | REPO_STRUCTURE.md exists | File found in docs/ |
| UT-000b-04 | CONFIG.md exists | File found in docs/ |
| UT-000b-05 | OPERATING_MODES.md exists | File found in docs/ |
| UT-000b-06 | README links valid | All links resolve |
| UT-000b-07 | No broken internal links | Link checker passes |
| UT-000b-08 | Markdown lint passes | Zero errors |
| UT-000b-09 | Spell check passes | No unknown words |
| UT-000b-10 | No TODO placeholders | grep finds none |
| UT-000b-11 | Line length acceptable | Under 120 chars preferred |
| UT-000b-12 | Heading hierarchy correct | No skipped levels |
| UT-000b-13 | Code blocks have language | All fenced blocks tagged |
| UT-000b-14 | Tables properly formatted | All rows parse |
| UT-000b-15 | docs/ directory exists | Directory found |

### Integration Tests

| ID | Test | Expected |
|----|------|----------|
| IT-000b-01 | Documentation renders on GitHub | All files display |
| IT-000b-02 | Mermaid diagrams render | Diagrams visible |
| IT-000b-03 | Table of contents links work | Navigation works |
| IT-000b-04 | Cross-file links work | All docs linked |
| IT-000b-05 | REPO_STRUCTURE matches reality | Tree accurate |
| IT-000b-06 | Examples in CONFIG.md are valid YAML | Parser accepts |
| IT-000b-07 | Mode descriptions are complete | All modes covered |
| IT-000b-08 | No duplicate content across files | Content unique |
| IT-000b-09 | Version numbers consistent | Same across docs |
| IT-000b-10 | External links work | HTTPS links valid |

### End-to-End Tests

| ID | Test | Expected |
|----|------|----------|
| E2E-000b-01 | New user can follow Quick Start | Build succeeds |
| E2E-000b-02 | Structure matches REPO_STRUCTURE.md | Paths match |
| E2E-000b-03 | Config examples work | Agent accepts configs |
| E2E-000b-04 | Mode switching works as documented | Mode changes |
| E2E-000b-05 | Contributor can follow CONTRIBUTING | PR possible |
| E2E-000b-06 | Docs accessible via relative links | Navigation works |
| E2E-000b-07 | Print documentation | PDF-like output |
| E2E-000b-08 | View on mobile | Readable |

### Performance Benchmarks

| ID | Metric | Target |
|----|--------|--------|
| PB-000b-01 | README.md size | < 25 KB |
| PB-000b-02 | Total docs size | < 500 KB |
| PB-000b-03 | Page load time (GitHub) | < 2 seconds |
| PB-000b-04 | Link validation time | < 30 seconds |
| PB-000b-05 | Markdown lint time | < 10 seconds |

---

## User Verification Steps

### Verification 1: README Exists and Renders
1. Navigate to repository root on GitHub
2. **Verify:** README.md displays automatically

### Verification 2: Table of Contents Works
1. Click a link in README table of contents
2. **Verify:** Scrolls to correct section

### Verification 3: Documentation Links Work
1. Click link to REPO_STRUCTURE.md from README
2. **Verify:** Opens correct file

### Verification 4: Structure Matches Documentation
1. Open REPO_STRUCTURE.md
2. Compare to actual directory listing
3. **Verify:** All documented folders exist

### Verification 5: Config Examples Valid
1. Open CONFIG.md
2. Copy a YAML example
3. Validate with YAML parser
4. **Verify:** No syntax errors

### Verification 6: Modes Documented
1. Open OPERATING_MODES.md
2. **Verify:** LocalOnly, Burst, Airgapped all documented

### Verification 7: Quick Start Works
1. Follow README Quick Start on fresh machine
2. **Verify:** All commands succeed

### Verification 8: No Broken Links
1. Run: `npx markdown-link-check README.md`
2. **Verify:** All links valid

### Verification 9: Markdown Lint Passes
1. Run: `npx markdownlint "**/*.md"`
2. **Verify:** Zero errors

### Verification 10: Spell Check Passes
1. Run spell checker on all .md files
2. **Verify:** No errors (or add to dictionary)

---

## Implementation Prompt for Claude

### Context

You are implementing Task 000.b: Add Baseline Docs for the Agentic Coding Bot (Acode). This creates the core documentation files.

### Step 1: Create docs/ Directory Structure

```bash
mkdir -p docs/architecture
mkdir -p docs/adr
```

### Step 2: Create README.md

Create README.md at repository root with:
- Project title and badges
- Description (privacy-first, local AI coding assistant)
- Table of contents
- Features list
- Quick Start (prerequisites, installation, first run)
- Documentation links table
- Operating modes overview with comparison table
- Contributing link
- License section

### Step 3: Create docs/REPO_STRUCTURE.md

Document:
- Complete folder hierarchy as tree
- Clean Architecture explanation
- Each folder's purpose
- Layer dependencies diagram (Mermaid)
- Naming conventions
- Namespace conventions
- How to add new components

### Step 4: Create docs/CONFIG.md

Document:
- Configuration sources (CLI, env, file)
- Precedence order
- Environment variables table
- .agent/config.yml schema
- Example configurations
- Troubleshooting

### Step 5: Create docs/OPERATING_MODES.md

Document:
- LocalOnly mode (full details)
- Burst mode (full details)
- Airgapped mode (full details)
- Comparison matrix
- Mode selection process
- Blocked endpoints list
- Use cases and scenarios
- FAQ

### Step 6: Create Supporting Files

- SECURITY.md (basic security policy)
- docs/adr/001-clean-architecture.md (first ADR)
- docs/architecture/overview.md (architecture summary)

### Validation Checklist

- [ ] All files created
- [ ] Markdown lint passes
- [ ] Spell check passes
- [ ] All links valid
- [ ] Renders on GitHub
- [ ] Consistent formatting
- [ ] No TODO placeholders

### Error Codes

| Code | Description |
|------|-------------|
| E000b-01 | README.md missing |
| E000b-02 | docs/ directory missing |
| E000b-03 | Broken internal link |
| E000b-04 | Invalid markdown syntax |
| E000b-05 | Spell check failure |

---

**END OF TASK 000.b**
