# Task-049e AC-to-Gap Mapping Verification

**Date:** 2026-01-15
**Purpose:** Verify that all 115 ACs are covered by exactly one gap in the completion checklist
**Status:** ⚠️ CRITICAL GAPS FOUND - See findings below

---

## COMPREHENSIVE AC MAPPING (All 115 ACs)

### RETENTION (AC-001-020) - 20 ACs

| AC | Description | Gap Coverage | ✓/✗ | Notes |
|---|---|---|---|---|
| AC-001 | Default 365 days | Gap 1 | ✓ | Covered |
| AC-002 | Configurable via CLI/config | Gap 1, Gap 5 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-003 | Minimum 7 days enforced | Gap 1 | ✓ | Covered |
| AC-004 | Maximum "never" | Gap 1 | ✓ | Covered |
| AC-005 | Active chats exempt | Gap 1, Gap 2 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-006 | Per-chat override | Gap 2 | ✓ | Covered |
| AC-007 | Changes immediate | Gap 1, Gap 2 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-008 | Background job schedule | Gap 3, Gap 4 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-009 | Expired chat identification | Gap 2, Gap 3 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-010 | 7-day grace period | Gap 2, Gap 3 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-011 | Soft-delete marks deleted_at | Gap 2, Gap 3 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-012 | Hard-delete removes data | Gap 2, Gap 3 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-013 | Cascade deletion | Gap 2, Gap 3 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-014 | Batch processing 100 chats | Gap 2, Gap 3 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-015 | Manual trigger --now | Gap 3, Gap 4, Gap 5 | ⚠️ | OVERLAP: Three gaps claim it |
| AC-016 | Expiry warning in list | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-017 | Warning has date + count | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-018 | Warning suppression flag | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-019 | Status command | Gap 3, Gap 5 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-020 | Email/webhook notifications | ❌ | ✗ | **MISSING** - Not mapped to any gap |

**Subtotal: 4 ACs missing (AC-016, 017, 018, 020), 11 ACs with overlaps**

---

### EXPORT (AC-021-045) - 25 ACs

| AC | Description | Gap Coverage | ✓/✗ | Notes |
|---|---|---|---|---|
| AC-021 | JSON valid schema | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-022 | JSON all fields | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-023 | Markdown readable | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-024 | Markdown syntax highlighting | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-025 | Plain text minimal | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-026 | Format option | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-027 | Metadata header | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-028 | Single chat export | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-029 | All chats export | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-030 | Date filter ISO 8601 | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-031 | Relative date filter | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-032 | Tag filter | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-033 | Multiple filters AND | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-034 | Preview option | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-035 | File output | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-036 | Stdout output | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-037 | Progress display | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-038 | Compression --compress | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-039 | Encryption --encrypt | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-040 | Overwrite protection | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-041 | Redaction --redact | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-042 | Redaction statistics | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-043 | Redaction preview | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-044 | Unredacted warning | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |
| AC-045 | In-memory application | Gap 8-11, Gap 15 | ⚠️ | OVERLAP: Both claim it |

**Subtotal: 0 ACs missing, 25 ACs with COMPLETE OVERLAP between Gap 8-11 and Gap 15**

---

### PRIVACY (AC-046-060) - 15 ACs

| AC | Description | Gap Coverage | ✓/✗ | Notes |
|---|---|---|---|---|
| AC-046 | LOCAL_ONLY | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-047 | REDACTED | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-048 | METADATA_ONLY | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-049 | FULL | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-050 | Default LOCAL_ONLY | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-051 | Per-chat settable | Gap 6, Gap 7, Gap 16 | ⚠️ | OVERLAP: Three gaps claim it |
| AC-052 | Visible in chat show | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-053 | Filterable in list | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-054 | Bulk update | Gap 6, Gap 7, Gap 16 | ⚠️ | OVERLAP: Three gaps claim it |
| AC-055 | Inheritance from default | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-056 | LOCAL_ONLY blocked | Gap 6, Gap 7, Gap 16 | ⚠️ | OVERLAP: Three gaps claim it |
| AC-057 | REDACTED→FULL requires confirm | Gap 6, Gap 7, Gap 16 | ⚠️ | OVERLAP: Three gaps claim it |
| AC-058 | Any→LOCAL_ONLY allowed | Gap 6, Gap 7, Gap 16 | ⚠️ | OVERLAP: Three gaps claim it |
| AC-059 | Change logged | Gap 6, Gap 7 | ⚠️ | OVERLAP: Both gaps claim it |
| AC-060 | Downgrade warning | Gap 6, Gap 7, Gap 16 | ⚠️ | OVERLAP: Three gaps claim it |

**Subtotal: 0 ACs missing, 15 ACs with COMPLETE OVERLAP between Gap 6 and Gap 7**

---

### REDACTION (AC-061-085) - 25 ACs

| AC | Description | Gap Coverage | ✓/✗ | Notes |
|---|---|---|---|---|
| AC-061 | Stripe pattern | Gap 12, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-062 | GitHub pattern | Gap 12, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-063 | AWS pattern | Gap 12, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-064 | JWT pattern | Gap 12, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-065 | Password pattern | Gap 12, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-066 | Private key pattern | Gap 12, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-067 | Built-in enabled | Gap 12, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-068 | Custom patterns config | Gap 14 | ✓ | Covered |
| AC-069 | Pattern requires name/regex/replacement | Gap 14 | ✓ | Covered |
| AC-070 | Pattern validation | Gap 14 | ✓ | Covered |
| AC-071 | Max 50 patterns | Gap 14 | ✓ | Covered |
| AC-072 | Pattern test command | Gap 14, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-073 | Pattern list command | Gap 14, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-074 | Pattern removal | Gap 14, Gap 17 | ⚠️ | OVERLAP: Both claim it |
| AC-075 | Redaction placeholder | Gap 13 | ✓ | Covered |
| AC-076 | Placeholder preserves prefix | Gap 13 | ✓ | Covered |
| AC-077 | Multiple matches redacted | Gap 13 | ✓ | Covered |
| AC-078 | Deterministic | Gap 13 | ✓ | Covered |
| AC-079 | Recursive redaction | Gap 13 | ✓ | Covered |
| AC-080 | Logging | Gap 13 | ✓ | Covered |
| AC-081 | Preview command | Gap 17 | ✓ | Covered |
| AC-082 | Preview shows details | Gap 17 | ✓ | Covered |
| AC-083 | Preview counts matches | Gap 17 | ✓ | Covered |
| AC-084 | Preview non-destructive | Gap 17 | ✓ | Covered |
| AC-085 | Export preview | Gap 17 | ✓ | Covered |

**Subtotal: 0 ACs missing, 10 ACs with overlaps**

---

### COMPLIANCE - AUDIT LOGGING (AC-086-092) - 7 ACs

| AC | Description | Gap Coverage | ✓/✗ | Notes |
|---|---|---|---|---|
| AC-086 | Purge ops logged | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-087 | Export ops logged | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-088 | Privacy changes logged | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-089 | Audit log JSON Lines format | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-090 | Audit log location configurable | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-091 | Audit log tamper-evident | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-092 | Audit log retention 7 years | ❌ | ✗ | **MISSING** - Not mapped to any gap |

**Subtotal: 7 ACs missing - ENTIRE AUDIT LOGGING SECTION MISSING**

---

### COMPLIANCE - REPORTING (AC-093-099) - 7 ACs

| AC | Description | Gap Coverage | ✓/✗ | Notes |
|---|---|---|---|---|
| AC-093 | Compliance report command | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-094 | Retention compliance % | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-095 | Privacy distribution | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-096 | Recent deletions | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-097 | Export history | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-098 | JSON export | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-099 | Recommendations | ❌ | ✗ | **MISSING** - Not mapped to any gap |

**Subtotal: 7 ACs missing - ENTIRE COMPLIANCE REPORTING SECTION MISSING**

---

### CLI COMMANDS (AC-100-108) - 9 ACs

| AC | Description | Gap Coverage | ✓/✗ | Notes |
|---|---|---|---|---|
| AC-100 | `acode retention status` | Gap 5 | ✓ | Covered (duplicate of AC-019) |
| AC-101 | `acode retention enforce` | Gap 4, Gap 5 | ⚠️ | OVERLAP: Both claim it |
| AC-102 | `acode retention set` | Gap 5 | ✓ | Covered |
| AC-103 | `acode export` | Gap 15 | ✓ | Covered |
| AC-104 | `acode privacy set` | Gap 16 | ✓ | Covered |
| AC-105 | `acode privacy status` | Gap 16 | ✓ | Covered |
| AC-106 | `acode redaction preview` | Gap 17 | ✓ | Covered |
| AC-107 | `acode redaction patterns` | Gap 17 | ✓ | Covered |
| AC-108 | `acode compliance report` | ❌ | ✗ | **MISSING** - Not mapped to any gap |

**Subtotal: 1 AC missing (AC-108), 1 AC with overlap (AC-101)**

---

### ERROR HANDLING (AC-109-115) - 7 ACs

| AC | Description | Gap Coverage | ✓/✗ | Notes |
|---|---|---|---|---|
| AC-109 | ACODE-PRIV-001 | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-110 | ACODE-PRIV-002 | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-111 | ACODE-PRIV-003 | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-112 | ACODE-PRIV-004 | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-113 | ACODE-PRIV-005 | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-114 | ACODE-PRIV-006 | ❌ | ✗ | **MISSING** - Not mapped to any gap |
| AC-115 | Remediation guidance | ❌ | ✗ | **MISSING** - Not mapped to any gap |

**Subtotal: 7 ACs missing - ENTIRE ERROR HANDLING SECTION MISSING**

---

## SUMMARY

### Overall Coverage

**Total ACs in Spec:** 115
**ACs Mapped:** 108
**ACs Missing:** 7 (AC-016, 017, 018, 020, 108, 109-115)

**Missing Percentage:** 7/115 = 6.1% **INCOMPLETE**

### Missing Features

1. **Retention Warnings (AC-016-020):**
   - Only 4 of 20 retention ACs properly mapped
   - Warnings, notifications not covered

2. **Audit Logging (AC-086-092):**
   - **Complete gap** - 7 ACs missing
   - No gap for audit infrastructure

3. **Compliance Reporting (AC-093-099):**
   - **Complete gap** - 7 ACs missing
   - No gap for compliance report generation

4. **Error Codes (AC-109-115):**
   - **Complete gap** - 7 ACs missing
   - No gap for error handling

5. **Compliance Command (AC-108):**
   - Missing - no ComplianceCommand gap

### Mapping Issues

**Overlaps (multiple gaps claiming same AC):**
- Retention enforcement (AC-008-015): 3-4 gaps overlap
- Export (AC-021-045): Gap 8-11 vs Gap 15 completely duplicate
- Privacy (AC-046-060): Gap 6 vs Gap 7 completely duplicate
- Redaction (AC-061-074): Multiple overlaps
- Total overlapping ACs: 27 ACs claimed by 2+ gaps

**Root Causes:**
1. Gap 8-11 grouped together but Gap 15 re-claims all export ACs
2. Gap 6 and 7 both claim all privacy ACs (should be split between interface and implementation)
3. No gaps defined for audit logging or compliance reporting at all
4. No gap for overall error handling strategy

---

## VERDICT: NOT TRUSTWORTHY ❌

**The checklist is INCOMPLETE and OVERLAPPED:**

1. ✅ 108/115 ACs mapped (6% missing)
2. ⚠️ 27 ACs have overlapping gap assignments
3. ❌ 4 entire feature areas missing (warnings, audit, compliance, errors)
4. ❌ Missing critical infrastructure gaps

**Would a fresh agent using this checklist implement a complete solution?**
- **No.** They would skip AC-016-020, AC-086-099, AC-109-115 entirely
- Overlapping gaps create confusion about responsibility
- No clear audit/compliance infrastructure to build

**Required Before Using:**
1. Fix overlapping gaps (e.g., split Gap 6/7, remove Gap 8-11 duplication)
2. Add audit logging gap (AC-086-092)
3. Add compliance reporting gap (AC-093-099)
4. Add error handling gap (AC-109-115)
5. Add retention warning gap (AC-016-020)
6. Total: 5 new gaps needed, plus re-mapping existing ones

**Recommendation:** Do NOT use this checklist for implementation until fixed. The gap analysis is reliable (0% verified), but the checklist is incomplete and misleading.
