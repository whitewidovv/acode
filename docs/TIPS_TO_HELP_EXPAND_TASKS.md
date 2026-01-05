Write Section-by-Section

To avoid losing work due to token limits, write and save incrementally:

1. Write **Description** section → Save to file
2. Write **Use Cases** section → Append to file
3. Write **User Manual** section → Append to file
4. Write **Acceptance Criteria** section → Append to file
5. Write **Testing Requirements** section → Append to file
6. Write **User Verification** section → Append to file
7. Write **Implementation Prompt** section → Append to file
8. **Verify file completeness** → Run `wc -l [filename]` to check line count (must be ≥1200)

### Step 4: Quality Verification

**Automated Checks:**
```bash
# Check line count (must be >= 1200)
wc -l refined-tasks/phase-XX-*/task-XXX-*.md

# Verify all sections exist
grep -E "^## (Description|Use Cases|User Manual|Acceptance Criteria|Testing Requirements|User Verification|Implementation Prompt)" refined-tasks/phase-XX-*/task-XXX-*.md

# Count acceptance criteria (should be 50-80+)
grep -E "^- \[ \]" refined-tasks/phase-XX-*/task-XXX-*.md | wc -l
```

**Quality Checklist:**
- [ ] Line count >= 1200 (preferably 1500-2500+)
- [ ] All 8 sections present with substantive content
- [ ] No "see above", "similar to", or "omitted for brevity" phrases
- [ ] All test code blocks are complete (no "..." placeholders)
- [ ] Each use case is 10-15+ lines with persona and scenario
- [ ] Acceptance criteria count is 50-80+ items
- [ ] Implementation prompt has 12+ steps with complete code examples
- [ ] File reads like Task 042 or Task 044 in quality and completeness