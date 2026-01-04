#!/usr/bin/env bash
#
# Ollama Provider Smoke Test Script
#
# This script performs basic verification that the Ollama provider integration
# is functioning correctly. It tests health checking, model listing, non-streaming
# completion, and streaming completion.
#
# Exit Codes:
#   0 - All tests passed
#   1 - One or more tests failed
#   2 - Configuration error (Ollama not running, no models, etc.)
#
# Usage:
#   ./scripts/smoke-test-ollama.sh [OPTIONS]
#
# Options:
#   --endpoint URL    Ollama endpoint (default: http://localhost:11434)
#   --model NAME      Model to test (default: llama3.2:latest)
#   --timeout SECS    Request timeout (default: 30)
#   --skip-tool-test  Skip tool calling test (default: always skipped - requires Task 007d)
#   --verbose         Show detailed output
#   --quiet           Minimal output (CI mode)
#

set -euo pipefail

# Default configuration
ENDPOINT="${OLLAMA_ENDPOINT:-http://localhost:11434}"
MODEL="${OLLAMA_MODEL:-llama3.2:latest}"
TIMEOUT=30
VERBOSE=false
QUIET=false
SKIP_TOOL_TEST=true  # Always true until Task 007d is complete

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --endpoint)
            ENDPOINT="$2"
            shift 2
            ;;
        --model)
            MODEL="$2"
            shift 2
            ;;
        --timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        --skip-tool-test)
            SKIP_TOOL_TEST=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        --quiet)
            QUIET=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 2
            ;;
    esac
done

# Test counters
TESTS_RUN=0
TESTS_PASSED=0
TESTS_FAILED=0
TESTS_SKIPPED=0

# Colors (only if not quiet and terminal supports it)
if [[ "$QUIET" == "false" ]] && [[ -t 1 ]]; then
    GREEN='\033[0;32m'
    RED='\033[0;31m'
    YELLOW='\033[1;33m'
    BLUE='\033[0;34m'
    NC='\033[0m' # No Color
else
    GREEN=''
    RED=''
    YELLOW=''
    BLUE=''
    NC=''
fi

# Logging functions
log_info() {
    if [[ "$QUIET" == "false" ]]; then
        echo -e "${BLUE}[INFO]${NC} $1"
    fi
}

log_pass() {
    if [[ "$QUIET" == "false" ]]; then
        echo -e "${GREEN}[PASS]${NC} $1"
    fi
}

log_fail() {
    echo -e "${RED}[FAIL]${NC} $1" >&2
}

log_skip() {
    if [[ "$QUIET" == "false" ]]; then
        echo -e "${YELLOW}[SKIP]${NC} $1"
    fi
}

log_verbose() {
    if [[ "$VERBOSE" == "true" ]]; then
        echo -e "${BLUE}[DEBUG]${NC} $1"
    fi
}

# Start time tracking
test_start_time() {
    date +%s%N | cut -b1-13
}

test_elapsed() {
    local start=$1
    local end=$(date +%s%N | cut -b1-13)
    echo "scale=2; ($end - $start) / 1000" | bc
}

# Test functions
test_health_check() {
    local test_name="Health Check"
    local start=$(test_start_time)

    ((TESTS_RUN++))

    log_info "Running: $test_name"
    log_verbose "Checking: $ENDPOINT/api/tags"

    if ! response=$(curl -s --max-time 5 "$ENDPOINT/api/tags" 2>&1); then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Connection failed"
        log_fail "  Error: $response"
        log_fail "  Hint: Is Ollama running? Try: ollama serve"
        ((TESTS_FAILED++))
        return 1
    fi

    if ! echo "$response" | grep -q "models"; then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Invalid response"
        log_fail "  Response: $response"
        ((TESTS_FAILED++))
        return 1
    fi

    local elapsed=$(test_elapsed $start)
    log_pass "$test_name (${elapsed}s)"
    ((TESTS_PASSED++))
    return 0
}

test_model_list() {
    local test_name="Model List"
    local start=$(test_start_time)

    ((TESTS_RUN++))

    log_info "Running: $test_name"
    log_verbose "Fetching: $ENDPOINT/api/tags"

    if ! response=$(curl -s --max-time 5 "$ENDPOINT/api/tags" 2>&1); then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Request failed"
        log_fail "  Error: $response"
        ((TESTS_FAILED++))
        return 1
    fi

    # Check if response contains models array
    if ! echo "$response" | grep -q '"models"'; then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - No models in response"
        log_fail "  Response: $response"
        ((TESTS_FAILED++))
        return 1
    fi

    # Check if at least one model exists
    local model_count=$(echo "$response" | grep -o '"name"' | wc -l)
    if [[ $model_count -lt 1 ]]; then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - No models available"
        log_fail "  Hint: Pull a model with: ollama pull $MODEL"
        ((TESTS_FAILED++))
        return 1
    fi

    log_verbose "Found $model_count model(s)"

    local elapsed=$(test_elapsed $start)
    log_pass "$test_name (${elapsed}s) - Found $model_count model(s)"
    ((TESTS_PASSED++))
    return 0
}

test_completion() {
    local test_name="Non-Streaming Completion"
    local start=$(test_start_time)

    ((TESTS_RUN++))

    log_info "Running: $test_name"
    log_verbose "Model: $MODEL, Prompt: 'Say hello'"

    local request='{
        "model": "'"$MODEL"'",
        "prompt": "Say hello in one word.",
        "stream": false
    }'

    if ! response=$(curl -s --max-time $TIMEOUT -X POST "$ENDPOINT/api/generate" \
        -H "Content-Type: application/json" \
        -d "$request" 2>&1); then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Request failed"
        log_fail "  Error: $response"
        ((TESTS_FAILED++))
        return 1
    fi

    # Check for response field
    if ! echo "$response" | grep -q '"response"'; then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Missing response field"
        log_fail "  Response: $response"
        log_fail "  Hint: Model '$MODEL' may not be available. Try: ollama pull $MODEL"
        ((TESTS_FAILED++))
        return 1
    fi

    # Check for done field
    if ! echo "$response" | grep -q '"done":true'; then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Response not complete"
        log_fail "  Response: $response"
        ((TESTS_FAILED++))
        return 1
    fi

    # Extract response text (basic parsing)
    local response_text=$(echo "$response" | sed -n 's/.*"response":"\([^"]*\)".*/\1/p')
    log_verbose "Response: $response_text"

    local elapsed=$(test_elapsed $start)
    log_pass "$test_name (${elapsed}s)"
    ((TESTS_PASSED++))
    return 0
}

test_streaming() {
    local test_name="Streaming Completion"
    local start=$(test_start_time)

    ((TESTS_RUN++))

    log_info "Running: $test_name"
    log_verbose "Model: $MODEL, Streaming enabled"

    local request='{
        "model": "'"$MODEL"'",
        "prompt": "Count to 3.",
        "stream": true
    }'

    # Capture streaming response
    if ! response=$(curl -s --max-time $TIMEOUT -X POST "$ENDPOINT/api/generate" \
        -H "Content-Type: application/json" \
        -d "$request" 2>&1); then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Request failed"
        log_fail "  Error: $response"
        ((TESTS_FAILED++))
        return 1
    fi

    # Count number of chunks (NDJSON lines)
    local chunk_count=$(echo "$response" | wc -l)

    if [[ $chunk_count -lt 2 ]]; then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Insufficient chunks received"
        log_fail "  Expected: >=2, Got: $chunk_count"
        log_fail "  Response: $response"
        ((TESTS_FAILED++))
        return 1
    fi

    # Check last chunk has done:true
    local last_chunk=$(echo "$response" | tail -n 1)
    if ! echo "$last_chunk" | grep -q '"done":true'; then
        local elapsed=$(test_elapsed $start)
        log_fail "$test_name (${elapsed}s) - Final chunk missing done flag"
        log_fail "  Last chunk: $last_chunk"
        ((TESTS_FAILED++))
        return 1
    fi

    log_verbose "Received $chunk_count chunks"

    local elapsed=$(test_elapsed $start)
    log_pass "$test_name (${elapsed}s) - Received $chunk_count chunks"
    ((TESTS_PASSED++))
    return 0
}

test_tool_calling() {
    local test_name="Tool Calling"

    ((TESTS_RUN++))
    ((TESTS_SKIPPED++))

    # TODO: Task 007d - Implement tool calling smoke test
    # This stub will be replaced when Task 007d (Tool Call Parsing) is complete.
    # See task-007d FR-082 through FR-087 for implementation requirements.
    #
    # Required implementation:
    # - FR-082: Verify tool call extraction from response
    # - FR-083: Verify JSON argument parsing succeeds
    # - FR-084: Verify tool call mapped to ToolCall type
    # - FR-085: Test with simple tool definition (e.g., get_weather)
    # - FR-086: Verify tool call ID generated/extracted
    # - FR-087: Verify multiple tool calls handled correctly
    #
    # Test should:
    # 1. Send request with tool definition
    # 2. Verify response contains tool_calls field
    # 3. Verify tool call has function.name and function.arguments
    # 4. Verify JSON arguments parse successfully
    # 5. Handle models that don't support tool calling gracefully

    log_skip "$test_name - Requires Task 007d (Tool Call Parsing)"
    log_verbose "  TODO: Implement tool calling test when Task 007d is complete"
    log_verbose "  See: docs/tasks/refined-tasks/Epic 01/task-007d FR-082 to FR-087"

    return 0
}

# Main execution
main() {
    if [[ "$QUIET" == "false" ]]; then
        echo "========================================"
        echo "Ollama Provider Smoke Test"
        echo "========================================"
        echo "Endpoint: $ENDPOINT"
        echo "Model:    $MODEL"
        echo "Timeout:  ${TIMEOUT}s"
        echo "========================================"
        echo ""
    fi

    # Run tests in order
    # Configuration check tests (exit 2 on failure)
    if ! test_health_check; then
        echo ""
        echo "========================================"
        echo "CONFIGURATION ERROR"
        echo "========================================"
        echo "Ollama health check failed. Cannot proceed with tests."
        echo "Ensure Ollama is running: ollama serve"
        exit 2
    fi

    if ! test_model_list; then
        echo ""
        echo "========================================"
        echo "CONFIGURATION ERROR"
        echo "========================================"
        echo "No models available. Cannot proceed with tests."
        echo "Pull a model: ollama pull $MODEL"
        exit 2
    fi

    # Functional tests (exit 1 on failure)
    test_completion || true
    test_streaming || true

    # Optional tests
    if [[ "$SKIP_TOOL_TEST" == "false" ]]; then
        test_tool_calling || true
    else
        ((TESTS_SKIPPED++))
        log_skip "Tool Calling - Skipped (use --skip-tool-test=false to enable)"
    fi

    # Summary
    echo ""
    echo "========================================"
    echo "Test Summary"
    echo "========================================"
    echo "Total:   $TESTS_RUN"
    echo "Passed:  ${GREEN}$TESTS_PASSED${NC}"
    echo "Failed:  ${RED}$TESTS_FAILED${NC}"
    echo "Skipped: ${YELLOW}$TESTS_SKIPPED${NC}"
    echo "========================================"

    if [[ $TESTS_FAILED -gt 0 ]]; then
        echo "Result: ${RED}FAILED${NC}"
        exit 1
    else
        echo "Result: ${GREEN}PASSED${NC}"
        exit 0
    fi
}

# Run main
main
