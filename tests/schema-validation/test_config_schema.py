#!/usr/bin/env python3
"""
Test suite for task-002a: JSON Schema validation

Tests:
1. Schema meta-validation (Draft 2020-12 compliance)
2. Valid examples pass validation
3. Invalid example fails validation with meaningful errors

Requirements: FR-002a-72, FR-002a-80, NFR-002a-05
"""

import json
from pathlib import Path
from typing import Dict, Any

import pytest
import yaml
from jsonschema import Draft202012Validator, ValidationError


# Paths relative to repository root
REPO_ROOT = Path(__file__).parent.parent.parent
SCHEMA_PATH = REPO_ROOT / "data" / "config-schema.json"
EXAMPLES_DIR = REPO_ROOT / "docs" / "config-examples"

# Valid example files that should pass validation
VALID_EXAMPLES = [
    "minimal.yml",
    "full.yml",
    "dotnet.yml",
    "node.yml",
    "python.yml",
    "go.yml",
    "rust.yml",
    "java.yml",
]

# Invalid example file that should fail validation
INVALID_EXAMPLE = "invalid.yml"


@pytest.fixture(scope="module")
def schema() -> Dict[str, Any]:
    """Load the JSON Schema file."""
    with open(SCHEMA_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


@pytest.fixture(scope="module")
def validator(schema: Dict[str, Any]) -> Draft202012Validator:
    """Create a Draft 2020-12 validator instance."""
    # Verify the schema declares Draft 2020-12
    assert schema.get("$schema") == "https://json-schema.org/draft/2020-12/schema", \
        "Schema must declare Draft 2020-12"

    # Check the schema itself is valid
    Draft202012Validator.check_schema(schema)

    return Draft202012Validator(schema)


class TestSchemaMetaValidation:
    """Test the schema itself is valid JSON Schema Draft 2020-12."""

    def test_schema_file_exists(self):
        """FR-002a-01: Schema file must exist at data/config-schema.json."""
        assert SCHEMA_PATH.exists(), f"Schema file not found: {SCHEMA_PATH}"

    def test_schema_is_valid_json(self, schema: Dict[str, Any]):
        """FR-002a-01: Schema must be valid JSON."""
        assert isinstance(schema, dict), "Schema must be a JSON object"

    def test_schema_declares_draft_2020_12(self, schema: Dict[str, Any]):
        """FR-002a-01: Schema must declare JSON Schema Draft 2020-12."""
        assert schema.get("$schema") == "https://json-schema.org/draft/2020-12/schema", \
            "Schema must use Draft 2020-12"

    def test_schema_has_id(self, schema: Dict[str, Any]):
        """FR-002a-02: Schema must have $id property."""
        assert "$id" in schema, "Schema must have $id"
        assert schema["$id"] == "https://acode.dev/schemas/config-v1.json", \
            "Schema $id must be https://acode.dev/schemas/config-v1.json"

    def test_schema_has_title(self, schema: Dict[str, Any]):
        """FR-002a-03: Schema must have title property."""
        assert "title" in schema, "Schema must have title"
        assert schema["title"] == "Acode Configuration", \
            "Schema title must be 'Acode Configuration'"

    def test_schema_has_description(self, schema: Dict[str, Any]):
        """FR-002a-04: Schema must have description property."""
        assert "description" in schema, "Schema must have description"
        assert len(schema["description"]) > 0, "Description must not be empty"

    def test_schema_requires_schema_version(self, schema: Dict[str, Any]):
        """FR-002a-05: Schema must require schema_version property."""
        assert "required" in schema, "Schema must have required array"
        assert "schema_version" in schema["required"], \
            "schema_version must be in required array"

    def test_schema_uses_defs_not_definitions(self, schema: Dict[str, Any]):
        """FR-002a-08: Schema must use $defs (Draft 2020-12) not definitions (Draft 04/07)."""
        assert "$defs" in schema, "Schema must use $defs (not definitions)"
        assert "definitions" not in schema, \
            "Schema must NOT use 'definitions' (use $defs instead)"

    def test_schema_refs_use_defs(self, schema: Dict[str, Any]):
        """FR-002a-09: All $ref values must use #/$defs/ not #/definitions/."""
        schema_str = json.dumps(schema)
        assert "#/definitions/" not in schema_str, \
            "Schema must NOT contain #/definitions/ (use #/$defs/ instead)"
        # Verify $defs are actually used
        assert "#/$defs/" in schema_str, \
            "Schema must contain $ref to #/$defs/"

    def test_schema_version_uses_pattern_not_enum(self, schema: Dict[str, Any]):
        """FR-002a-26, FR-002a-27: schema_version must use pattern for semver, not enum."""
        schema_version_prop = schema["properties"]["schema_version"]
        assert "pattern" in schema_version_prop, \
            "schema_version must use 'pattern' for semver validation"
        assert schema_version_prop["pattern"] == r"^\d+\.\d+\.\d+$", \
            "schema_version pattern must validate semver (x.y.z)"
        assert "enum" not in schema_version_prop, \
            "schema_version must NOT use 'enum' (prevents version evolution)"

    def test_schema_passes_meta_validation(self, validator: Draft202012Validator):
        """NFR-002a-05: Schema must pass JSON Schema meta-validation."""
        # If the validator fixture was created successfully, the schema is valid
        assert validator is not None, "Validator creation failed (schema invalid)"


class TestValidExamples:
    """Test all valid YAML examples validate against the schema."""

    @pytest.mark.parametrize("example_file", VALID_EXAMPLES)
    def test_valid_example_validates(
        self,
        validator: Draft202012Validator,
        example_file: str
    ):
        """FR-002a-72: All valid examples must validate against schema."""
        example_path = EXAMPLES_DIR / example_file

        # Load YAML example
        assert example_path.exists(), f"Example not found: {example_path}"
        with open(example_path, "r", encoding="utf-8") as f:
            config = yaml.safe_load(f)

        # Validate against schema
        try:
            validator.validate(config)
        except ValidationError as e:
            pytest.fail(
                f"{example_file} failed validation:\n"
                f"  Path: {' -> '.join(str(p) for p in e.path)}\n"
                f"  Error: {e.message}\n"
                f"  Schema: {e.schema}"
            )

    def test_minimal_example_has_required_fields(self):
        """FR-002a-56, FR-002a-57: Minimal example must have minimal required fields."""
        example_path = EXAMPLES_DIR / "minimal.yml"
        with open(example_path, "r", encoding="utf-8") as f:
            config = yaml.safe_load(f)

        # Minimal example must have schema_version
        assert "schema_version" in config, "Minimal example must have schema_version"

        # Minimal example should be truly minimal (not bloated)
        assert len(config.keys()) <= 3, \
            "Minimal example should have <= 3 top-level keys"

    def test_full_example_has_all_sections(self):
        """FR-002a-58, FR-002a-59: Full example must demonstrate all features."""
        example_path = EXAMPLES_DIR / "full.yml"
        with open(example_path, "r", encoding="utf-8") as f:
            config = yaml.safe_load(f)

        # Full example should have all major sections
        expected_sections = [
            "schema_version",
            "project",
            "mode",
            "model",
            "commands",
            "paths",
            "ignore",
            "network",
            "storage",
        ]
        for section in expected_sections:
            assert section in config, \
                f"Full example must have '{section}' section"


class TestInvalidExample:
    """Test the invalid example properly fails validation."""

    def test_invalid_example_exists(self):
        """FR-002a-75: Invalid example file must exist."""
        example_path = EXAMPLES_DIR / INVALID_EXAMPLE
        assert example_path.exists(), f"Invalid example not found: {example_path}"

    def test_invalid_example_fails_validation(self, validator: Draft202012Validator):
        """FR-002a-76: Invalid example must fail validation with meaningful errors."""
        example_path = EXAMPLES_DIR / INVALID_EXAMPLE

        with open(example_path, "r", encoding="utf-8") as f:
            config = yaml.safe_load(f)

        # The invalid example should fail validation
        errors = list(validator.iter_errors(config))
        assert len(errors) > 0, \
            "Invalid example must produce validation errors"

        # Log errors for debugging
        print(f"\nValidation errors for {INVALID_EXAMPLE}:")
        for error in errors:
            print(f"  - Path: {' -> '.join(str(p) for p in error.path)}")
            print(f"    Error: {error.message}")


class TestSchemaConstraints:
    """Test specific schema constraints are properly defined."""

    def test_temperature_constraint(self, schema: Dict[str, Any]):
        """FR-002a-35: temperature must be constrained to 0-2."""
        temp_schema = schema["$defs"]["model_parameters"]["properties"]["temperature"]
        assert temp_schema["minimum"] == 0, "temperature minimum must be 0"
        assert temp_schema["maximum"] == 2, "temperature maximum must be 2"

    def test_max_tokens_constraint(self, schema: Dict[str, Any]):
        """FR-002a-36: max_tokens must be > 0."""
        max_tokens_schema = schema["$defs"]["model_parameters"]["properties"]["max_tokens"]
        assert max_tokens_schema["minimum"] == 1, "max_tokens minimum must be 1"

    def test_top_p_constraint(self, schema: Dict[str, Any]):
        """FR-002a-37: top_p must be 0-1."""
        top_p_schema = schema["$defs"]["model_parameters"]["properties"]["top_p"]
        assert top_p_schema["minimum"] == 0, "top_p minimum must be 0"
        assert top_p_schema["maximum"] == 1, "top_p maximum must be 1"

    def test_mode_default_excludes_burst(self, schema: Dict[str, Any]):
        """FR-002a-34: mode.default enum must exclude 'burst' (HC-03)."""
        mode_default_schema = schema["$defs"]["mode"]["properties"]["default"]
        assert "enum" in mode_default_schema, "mode.default must have enum"
        assert "burst" not in mode_default_schema["enum"], \
            "mode.default must NOT allow 'burst' (HC-03 violation)"
        assert "local-only" in mode_default_schema["enum"], \
            "mode.default must allow 'local-only'"
        assert "airgapped" in mode_default_schema["enum"], \
            "mode.default must allow 'airgapped'"

    def test_project_name_pattern(self, schema: Dict[str, Any]):
        """FR-002a-32: project.name must have pattern for lowercase, alphanumeric, hyphens, underscores."""
        name_schema = schema["$defs"]["project"]["properties"]["name"]
        assert "pattern" in name_schema, "project.name must have pattern"
        assert name_schema["pattern"] == "^[a-z0-9][a-z0-9-_]*$", \
            "project.name pattern must be ^[a-z0-9][a-z0-9-_]*$"

    def test_project_type_enum(self, schema: Dict[str, Any]):
        """FR-002a-33: project.type must have enum for supported types."""
        type_schema = schema["$defs"]["project"]["properties"]["type"]
        assert "enum" in type_schema, "project.type must have enum"
        expected_types = ["dotnet", "node", "python", "go", "rust", "java", "other"]
        for ptype in expected_types:
            assert ptype in type_schema["enum"], \
                f"project.type must include '{ptype}'"


class TestPerformance:
    """Test schema validation performance."""

    def test_validation_performance(self, validator: Draft202012Validator):
        """NFR-002a-06: Validation must complete < 100ms per config."""
        import time

        example_path = EXAMPLES_DIR / "full.yml"
        with open(example_path, "r", encoding="utf-8") as f:
            config = yaml.safe_load(f)

        # Measure validation time
        start = time.time()
        validator.validate(config)
        elapsed_ms = (time.time() - start) * 1000

        assert elapsed_ms < 100, \
            f"Validation took {elapsed_ms:.2f}ms (must be < 100ms)"


if __name__ == "__main__":
    # Run tests with pytest
    pytest.main([__file__, "-v", "--tb=short"])
