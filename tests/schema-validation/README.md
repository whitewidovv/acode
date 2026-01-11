# Schema Validation Tests

**Task**: task-002a (Define Schema + Examples)
**Purpose**: Validate JSON Schema compliance and example correctness

## Requirements

- Python 3.9+
- pip (Python package manager)

## Setup

Install dependencies:

```bash
cd tests/schema-validation
pip install -r requirements.txt
```

## Running Tests

### Run all tests

```bash
pytest test_config_schema.py -v
```

### Run specific test class

```bash
# Test schema meta-validation only
pytest test_config_schema.py::TestSchemaMetaValidation -v

# Test valid examples only
pytest test_config_schema.py::TestValidExamples -v

# Test invalid example only
pytest test_config_schema.py::TestInvalidExample -v

# Test schema constraints only
pytest test_config_schema.py::TestSchemaConstraints -v

# Test performance only
pytest test_config_schema.py::TestPerformance -v
```

### Run with detailed output

```bash
pytest test_config_schema.py -vv --tb=long
```

## Test Coverage

### Schema Meta-Validation (10 tests)
- ✅ Schema file exists
- ✅ Schema is valid JSON
- ✅ Schema declares Draft 2020-12
- ✅ Schema has $id
- ✅ Schema has title
- ✅ Schema has description
- ✅ Schema requires schema_version
- ✅ Schema uses $defs (not definitions)
- ✅ Schema $refs use $defs (not definitions)
- ✅ schema_version uses pattern (not enum)
- ✅ Schema passes meta-validation

### Valid Examples (10 tests)
- ✅ minimal.yml validates
- ✅ full.yml validates
- ✅ dotnet.yml validates
- ✅ node.yml validates
- ✅ python.yml validates
- ✅ go.yml validates
- ✅ rust.yml validates
- ✅ java.yml validates
- ✅ Minimal example has required fields
- ✅ Full example has all sections

### Invalid Example (2 tests)
- ✅ invalid.yml exists
- ✅ invalid.yml fails validation

### Schema Constraints (6 tests)
- ✅ temperature constraint (0-2)
- ✅ max_tokens constraint (> 0)
- ✅ top_p constraint (0-1)
- ✅ mode.default excludes 'burst' (HC-03)
- ✅ project.name pattern
- ✅ project.type enum

### Performance (1 test)
- ✅ Validation completes < 100ms

**Total**: 29 tests

## CI/CD Integration

Add to GitHub Actions workflow:

```yaml
- name: Validate Config Schema
  run: |
    cd tests/schema-validation
    pip install -r requirements.txt
    pytest test_config_schema.py -v
```

## Troubleshooting

### ImportError: No module named 'jsonschema'

Install dependencies:
```bash
pip install -r requirements.txt
```

### ValidationError on valid example

Check:
1. Schema syntax is correct (use $defs not definitions)
2. All $ref paths use #/$defs/ not #/definitions/
3. schema_version uses pattern not enum

### Python not found

Install Python 3.9+ from https://www.python.org/downloads/

## Requirements Satisfied

This test suite satisfies:
- FR-002a-72: All examples MUST pass validation
- FR-002a-80: Examples MUST be tested in CI
- NFR-002a-05: Schema MUST be tested
- NFR-002a-06: Validation MUST complete < 100ms

## References

- JSON Schema Draft 2020-12: https://json-schema.org/draft/2020-12/schema
- Python jsonschema library: https://python-jsonschema.readthedocs.io/
- Task spec: `docs/tasks/refined-tasks/Epic 00/task-002a-define-schema-examples.md`
