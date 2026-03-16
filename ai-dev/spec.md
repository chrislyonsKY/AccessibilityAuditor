# AccessibilityAuditor v2 — Spec Addendum

> Extends the existing spec.md. Read the original first.
> Only v2 requirements are documented here.

---

## New Functional Requirements

### FR-V2-01: Deterministic Fix Engine
- Every `AuditFinding` type with a known rule-based fix exposes a "Fix" button in the findings grid
- Fix buttons are visible and enabled with no API key configured
- Clicking "Fix" applies the change directly to the CIM element via `QueuedTask.Run()`
- The finding row updates to show "Fixed" status after successful application
- Failed fixes show an inline error message — no crash, no dialog

### FR-V2-02: Fix All Auto
- "Fix All Auto" button runs all available deterministic fixes in a single pass
- Skips findings with no deterministic fix strategy
- Skips findings already marked Fixed
- Shows a summary on completion: "5 fixes applied · 2 skipped · 1 failed"

### FR-V2-03: BYOK Settings Tab
- Second tab added to existing dockpane: "Settings"
- Provider selection: Anthropic (default) / OpenAI via `RadioButton` group
- API key entry via `PasswordBox` — value is masked on entry
- "Save Key" stores key in Windows Credential Manager
- "Test Connection" sends a minimal completion and reports pass/fail
- Key status shows "Configured ✓" or "Not configured" — never shows the key value
- "Remove Key" deletes the credential from Windows Credential Manager

### FR-V2-04: LLM-Assisted Fixes
- Available only when a valid API key is configured for the selected provider
- Fix button for LLM-eligible findings is disabled (with tooltip) when no key configured
- Clicking an LLM fix button opens a review panel within the dockpane showing the AI suggestion
- User must click "Apply" or "Dismiss" — no auto-apply
- LLM calls show a progress indicator and are cancellable via "Cancel" button
- If LLM call fails: error shown in status bar, finding remains unchanged, retry possible

### FR-V2-05: .NET 8 Upgrade
- Project file retargeted to `net8.0-windows`
- SDK NuGet references updated to Pro SDK 3.4
- Minimum supported ArcGIS Pro version is 3.2
- No v1 audit functionality is broken by the upgrade

---

## Non-Functional Requirements (v2 additions)

### NFR-V2-01: BYOK Security
- API keys stored exclusively in Windows Credential Manager
- Keys never written to disk outside Credential Manager
- Keys never appear in logs, exception messages, temp files, or output HTML reports
- Keys never displayed in plaintext in the UI after initial entry

### NFR-V2-02: LLM Performance
- LLM call timeout: 30 seconds (default)
- UI remains responsive during LLM calls (async, non-blocking)
- Cancellation response within 2 seconds of user clicking "Cancel"

### NFR-V2-03: Deterministic Fix Performance
- Single deterministic fix applies in < 2 seconds
- "Fix All Auto" on 10 findings completes in < 15 seconds
- No UI freeze during fix application (all Pro SDK access via `QueuedTask.Run()`)

---

## Acceptance Criteria (v2)

- [ ] Running "Fix All Auto" on a layout with known contrast and font-size failures applies all fixes and updates finding rows to "Fixed"
- [ ] "Fix" button is visible and functional with no API key configured (deterministic fixes)
- [ ] "Fix" button for LLM findings is disabled with correct tooltip when no key is present
- [ ] API key saved via Settings tab is retrievable across Pro restarts
- [ ] API key never appears in any log file or output HTML report
- [ ] "Test Connection" correctly distinguishes a valid key from an invalid one
- [ ] Cancelling an in-progress LLM call stops the request and returns UI to idle
- [ ] LLM fix suggestion is displayed for review and only applied after user clicks "Apply"
- [ ] .NET 8 retarget does not break any v1 audit functionality
- [ ] Add-in loads cleanly in ArcGIS Pro 3.4 without ribbon or dockpane errors

---

## Out of Scope (v2)

- Automated fix for all finding types — some require human judgment and remain audit-only
- Batch fix across multiple layouts in one session
- OpenRouter as a provider (Anthropic and OpenAI only in v2)
- Local / Ollama LLM support (future consideration)
- Fix history / undo beyond standard Pro undo stack
