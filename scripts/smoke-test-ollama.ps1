<#
.SYNOPSIS
Ollama Provider Smoke Test Script (PowerShell)

.DESCRIPTION
This script performs basic verification that the Ollama provider integration
is functioning correctly. It tests health checking, model listing, non-streaming
completion, and streaming completion.

.PARAMETER Endpoint
Ollama endpoint URL (default: http://localhost:11434)

.PARAMETER Model
Model to test (default: llama3.2:latest)

.PARAMETER Timeout
Request timeout in seconds (default: 30)

.PARAMETER SkipToolTest
Skip tool calling test (default: $true - requires Task 007d)

.PARAMETER Verbose
Show detailed output

.PARAMETER Quiet
Minimal output (CI mode)

.EXAMPLE
.\scripts\smoke-test-ollama.ps1

.EXAMPLE
.\scripts\smoke-test-ollama.ps1 -Endpoint "http://localhost:11434" -Model "llama3.2:3b" -Verbose

.NOTES
Exit Codes:
  0 - All tests passed
  1 - One or more tests failed
  2 - Configuration error (Ollama not running, no models, etc.)
#>

[CmdletBinding()]
param(
    [string]$Endpoint = "http://localhost:11434",
    [string]$Model = "llama3.2:latest",
    [int]$Timeout = 30,
    [switch]$SkipToolTest = $true,
    [switch]$Verbose,
    [switch]$Quiet
)

# Test counters
$script:TestsRun = 0
$script:TestsPassed = 0
$script:TestsFailed = 0
$script:TestsSkipped = 0

# Logging functions
function Write-Info {
    param([string]$Message)
    if (-not $Quiet) {
        Write-Host "[INFO] $Message" -ForegroundColor Blue
    }
}

function Write-Pass {
    param([string]$Message)
    if (-not $Quiet) {
        Write-Host "[PASS] $Message" -ForegroundColor Green
    }
}

function Write-Fail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Write-Skip {
    param([string]$Message)
    if (-not $Quiet) {
        Write-Host "[SKIP] $Message" -ForegroundColor Yellow
    }
}

function Write-Debug-Verbose {
    param([string]$Message)
    if ($Verbose) {
        Write-Host "[DEBUG] $Message" -ForegroundColor Cyan
    }
}

function Write-Warn {
    param([string]$Message)
    if (-not $Quiet) {
        Write-Host "[WARN] $Message" -ForegroundColor Yellow
    }
}

# Version checking function
function Test-OllamaVersion {
    Write-Debug-Verbose "Checking Ollama version..."

    try {
        # Try to get Ollama version
        $versionOutput = & ollama --version 2>&1

        if ($LASTEXITCODE -eq 0 -and $versionOutput) {
            # Parse version string (format: "ollama version is 0.1.30")
            if ($versionOutput -match "(\d+\.\d+\.\d+)") {
                $version = $matches[1]
                Write-Debug-Verbose "Detected Ollama version: $version"

                # Parse version components
                $parts = $version.Split('.')
                $major = [int]$parts[0]
                $minor = [int]$parts[1]
                $patch = [int]$parts[2]

                # Check minimum version (0.1.23)
                if ($major -lt 0 -or ($major -eq 0 -and $minor -lt 1) -or ($major -eq 0 -and $minor -eq 1 -and $patch -lt 23)) {
                    Write-Warn "Ollama version $version is below minimum supported version 0.1.23"
                    Write-Warn "Some features may not work correctly. Please upgrade Ollama."
                }
                # Check if above tested maximum (0.1.35)
                elseif ($major -gt 0 -or ($major -eq 0 -and $minor -gt 1) -or ($major -eq 0 -and $minor -eq 1 -and $patch -gt 35)) {
                    Write-Warn "Ollama version $version is above tested maximum 0.1.35"
                    Write-Warn "This version has not been explicitly tested. Please report any issues."
                }
                else {
                    Write-Debug-Verbose "Ollama version $version is within supported range (0.1.23 to 0.1.35)"
                }
            }
            else {
                Write-Debug-Verbose "Could not parse version from output: $versionOutput"
            }
        }
    }
    catch {
        Write-Debug-Verbose "Could not check Ollama version (command may not be available)"
    }

    # FR-081: Version check failure MUST NOT block tests
    # Always return without error
}

# Test functions
function Test-HealthCheck {
    $testName = "Health Check"
    $start = Get-Date

    $script:TestsRun++

    Write-Info "Running: $testName"
    Write-Debug-Verbose "Checking: $Endpoint/api/tags"

    try {
        $response = Invoke-RestMethod -Uri "$Endpoint/api/tags" -Method Get -TimeoutSec 5 -ErrorAction Stop
    }
    catch {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Connection failed"
        Write-Fail "  Error: $($_.Exception.Message)"
        Write-Fail "  Hint: Is Ollama running? Try: ollama serve"
        $script:TestsFailed++
        return $false
    }

    if (-not $response.PSObject.Properties['models']) {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Invalid response"
        Write-Fail "  Response: $($response | ConvertTo-Json -Compress)"
        $script:TestsFailed++
        return $false
    }

    $elapsed = ((Get-Date) - $start).TotalSeconds
    Write-Pass "$testName ($($elapsed.ToString('F2'))s)"
    $script:TestsPassed++
    return $true
}

function Test-ModelList {
    $testName = "Model List"
    $start = Get-Date

    $script:TestsRun++

    Write-Info "Running: $testName"
    Write-Debug-Verbose "Fetching: $Endpoint/api/tags"

    try {
        $response = Invoke-RestMethod -Uri "$Endpoint/api/tags" -Method Get -TimeoutSec 5 -ErrorAction Stop
    }
    catch {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Request failed"
        Write-Fail "  Error: $($_.Exception.Message)"
        $script:TestsFailed++
        return $false
    }

    if (-not $response.PSObject.Properties['models']) {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - No models in response"
        Write-Fail "  Response: $($response | ConvertTo-Json -Compress)"
        $script:TestsFailed++
        return $false
    }

    $modelCount = $response.models.Count
    if ($modelCount -lt 1) {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - No models available"
        Write-Fail "  Hint: Pull a model with: ollama pull $Model"
        $script:TestsFailed++
        return $false
    }

    Write-Debug-Verbose "Found $modelCount model(s)"

    $elapsed = ((Get-Date) - $start).TotalSeconds
    Write-Pass "$testName ($($elapsed.ToString('F2'))s) - Found $modelCount model(s)"
    $script:TestsPassed++
    return $true
}

function Test-Completion {
    $testName = "Non-Streaming Completion"
    $start = Get-Date

    $script:TestsRun++

    Write-Info "Running: $testName"
    Write-Debug-Verbose "Model: $Model, Prompt: 'Say hello'"

    $requestBody = @{
        model = $Model
        prompt = "Say hello in one word."
        stream = $false
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$Endpoint/api/generate" -Method Post `
            -Body $requestBody -ContentType "application/json" -TimeoutSec $Timeout -ErrorAction Stop
    }
    catch {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Request failed"
        Write-Fail "  Error: $($_.Exception.Message)"
        $script:TestsFailed++
        return $false
    }

    if (-not $response.PSObject.Properties['response']) {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Missing response field"
        Write-Fail "  Response: $($response | ConvertTo-Json -Compress)"
        Write-Fail "  Hint: Model '$Model' may not be available. Try: ollama pull $Model"
        $script:TestsFailed++
        return $false
    }

    if (-not $response.done) {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Response not complete"
        Write-Fail "  Response: $($response | ConvertTo-Json -Compress)"
        $script:TestsFailed++
        return $false
    }

    Write-Debug-Verbose "Response: $($response.response)"

    $elapsed = ((Get-Date) - $start).TotalSeconds
    Write-Pass "$testName ($($elapsed.ToString('F2'))s)"
    $script:TestsPassed++
    return $true
}

function Test-Streaming {
    $testName = "Streaming Completion"
    $start = Get-Date

    $script:TestsRun++

    Write-Info "Running: $testName"
    Write-Debug-Verbose "Model: $Model, Streaming enabled"

    $requestBody = @{
        model = $Model
        prompt = "Count to 3."
        stream = $true
    } | ConvertTo-Json

    try {
        # PowerShell doesn't have great streaming support, so we'll read full response
        $webRequest = [System.Net.WebRequest]::Create("$Endpoint/api/generate")
        $webRequest.Method = "POST"
        $webRequest.ContentType = "application/json"
        $webRequest.Timeout = $Timeout * 1000

        $bytes = [System.Text.Encoding]::UTF8.GetBytes($requestBody)
        $webRequest.ContentLength = $bytes.Length
        $requestStream = $webRequest.GetRequestStream()
        $requestStream.Write($bytes, 0, $bytes.Length)
        $requestStream.Close()

        $response = $webRequest.GetResponse()
        $reader = New-Object System.IO.StreamReader($response.GetResponseStream())
        $fullResponse = $reader.ReadToEnd()
        $reader.Close()
        $response.Close()
    }
    catch {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Request failed"
        Write-Fail "  Error: $($_.Exception.Message)"
        $script:TestsFailed++
        return $false
    }

    # Count NDJSON lines (chunks)
    $chunks = $fullResponse -split "`n" | Where-Object { $_.Trim() -ne "" }
    $chunkCount = $chunks.Count

    if ($chunkCount -lt 2) {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Insufficient chunks received"
        Write-Fail "  Expected: >=2, Got: $chunkCount"
        $script:TestsFailed++
        return $false
    }

    # Check last chunk has done:true
    $lastChunk = $chunks[-1] | ConvertFrom-Json
    if (-not $lastChunk.done) {
        $elapsed = ((Get-Date) - $start).TotalSeconds
        Write-Fail "$testName ($($elapsed.ToString('F2'))s) - Final chunk missing done flag"
        Write-Fail "  Last chunk: $($chunks[-1])"
        $script:TestsFailed++
        return $false
    }

    Write-Debug-Verbose "Received $chunkCount chunks"

    $elapsed = ((Get-Date) - $start).TotalSeconds
    Write-Pass "$testName ($($elapsed.ToString('F2'))s) - Received $chunkCount chunks"
    $script:TestsPassed++
    return $true
}

function Test-ToolCalling {
    $testName = "Tool Calling"

    $script:TestsRun++
    $script:TestsSkipped++

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

    Write-Skip "$testName - Requires Task 007d (Tool Call Parsing)"
    Write-Debug-Verbose "  TODO: Implement tool calling test when Task 007d is complete"
    Write-Debug-Verbose "  See: docs/tasks/refined-tasks/Epic 01/task-007d FR-082 to FR-087"

    return $true
}

# Main execution
function Main {
    if (-not $Quiet) {
        Write-Host "========================================"
        Write-Host "Ollama Provider Smoke Test" -ForegroundColor Cyan
        Write-Host "========================================"
        Write-Host "Endpoint: $Endpoint"
        Write-Host "Model:    $Model"
        Write-Host "Timeout:  ${Timeout}s"
        Write-Host "========================================"
        Write-Host ""
    }

    # Check Ollama version (FR-078 to FR-081)
    Test-OllamaVersion

    # Run tests in order
    # Configuration check tests (exit 2 on failure)
    if (-not (Test-HealthCheck)) {
        Write-Host ""
        Write-Host "========================================"
        Write-Host "CONFIGURATION ERROR" -ForegroundColor Red
        Write-Host "========================================"
        Write-Host "Ollama health check failed. Cannot proceed with tests."
        Write-Host "Ensure Ollama is running: ollama serve"
        exit 2
    }

    if (-not (Test-ModelList)) {
        Write-Host ""
        Write-Host "========================================"
        Write-Host "CONFIGURATION ERROR" -ForegroundColor Red
        Write-Host "========================================"
        Write-Host "No models available. Cannot proceed with tests."
        Write-Host "Pull a model: ollama pull $Model"
        exit 2
    }

    # Functional tests (continue on failure)
    Test-Completion | Out-Null
    Test-Streaming | Out-Null

    # Optional tests
    if (-not $SkipToolTest) {
        Test-ToolCalling | Out-Null
    }
    else {
        $script:TestsSkipped++
        Write-Skip "Tool Calling - Skipped (use -SkipToolTest:`$false to enable)"
    }

    # Summary
    Write-Host ""
    Write-Host "========================================"
    Write-Host "Test Summary" -ForegroundColor Cyan
    Write-Host "========================================"
    Write-Host "Total:   $script:TestsRun"
    Write-Host "Passed:  $script:TestsPassed" -ForegroundColor Green
    Write-Host "Failed:  $script:TestsFailed" -ForegroundColor Red
    Write-Host "Skipped: $script:TestsSkipped" -ForegroundColor Yellow
    Write-Host "========================================"

    if ($script:TestsFailed -gt 0) {
        Write-Host "Result: FAILED" -ForegroundColor Red
        exit 1
    }
    else {
        Write-Host "Result: PASSED" -ForegroundColor Green
        exit 0
    }
}

# Run main
Main
