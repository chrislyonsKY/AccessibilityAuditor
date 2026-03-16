# CLAUDE.md — AccessibilityAuditor v2
> ArcGIS Pro SDK add-in — WCAG 2.1 AA audit with auto-fix and optional BYOK AI assistance
> C# 12 / .NET 8 / ArcGIS Pro SDK 3.4 → forward-compatible with Pro 3.7 / .NET 10

> **Note:** Do not include this file as indexable context. It is the entry point, not a reference doc.

Read this file completely before doing anything.
Then read `ai-dev/architecture.md` for full context.
Then read `ai-dev/guardrails/` for hard constraints.

---

## Context Boundaries

Do NOT auto-scan or index:
- `ai-dev/`    (read specific files only when instructed)
- `CLAUDE.md`  (this file — entry point only)

---

## What v2 Adds

This is an in-place upgrade of AccessibilityAuditor v1 (.NET 6 / Pro 3.0). The audit engine and all existing check classes are **unchanged**. v2 adds:

1. **Fix Engine** — deterministic auto-fix for rule-based findings. No API key required.
2. **BYOK AI Layer** — optional LLM-assisted fixes (Anthropic Claude or OpenAI). Never required.
3. **Settings Tab** — API key entry, provider selection, test connection. Added to existing dockpane.
4. **.NET 8 upgrade** — `<TargetFramework>net8.0-windows</TargetFramework>` in `.csproj` only.

## Workflow Protocol

When starting a new task:
1. Read CLAUDE.md (this file)
2. Read `ai-dev/architecture.md`
3. Read `ai-dev/guardrails/` — constraints override all other guidance
4. Read the relevant `ai-dev/agents/` file for your role
5. Check `ai-dev/decisions/` for prior decisions affecting your work

Plan before building. Show the plan. Wait for confirmation before writing code.

---

## Compatibility Matrix

| Component | v1 | v2 | v2.1 (future) |
|---|---|---|---|
| .NET | 6 | 8 | 10 |
| ArcGIS Pro SDK | 3.0 | 3.4 | 3.7 |
| Min ArcGIS Pro | 3.0 | 3.2 | 3.7 |
| Visual Studio | 2022 | 2022 | 2026 |

`.NET 10 / Pro 3.7` upgrade (May 2026) = single `<TargetFramework>` change + SDK NuGet bump. No API changes.

---

## Project Structure (v2 additions only)

```
AccessibilityAuditor/
├── CLAUDE.md                                 ← this file (updated)
├── AGENTS.md                                 ← updated for v2
├── .github/
│   └── copilot-instructions.md              ← updated for v2
├── ai-dev/
│   ├── architecture.md                       ← updated for v2
│   ├── spec.md                               ← updated for v2
│   ├── decisions/
│   │   ├── DL-004-byok-optional.md          ← NEW
│   │   ├── DL-005-fix-strategy-pattern.md   ← NEW
│   │   └── DL-006-dotnet8-upgrade.md        ← NEW
│   ├── agents/
│   │   └── csharp_expert.md                 ← updated for v2
│   ├── skills/
│   │   └── arcgis-pro-sdk-skill.md          ← updated for v2
│   └── guardrails/
│       ├── coding-standards.md              ← updated for v2
│       └── data-handling.md                 ← updated for v2 (credential rules)
└── src/
    └── AccessibilityAuditor/
        ├── Services/
        │   ├── Fixes/
        │   │   ├── IFixStrategy.cs          ← NEW
        │   │   ├── FixResult.cs             ← NEW
        │   │   ├── DeterministicFixStrategy.cs  ← NEW
        │   │   └── LLMFixStrategy.cs        ← NEW
        │   └── LLM/
        │       ├── ILLMProvider.cs          ← NEW
        │       ├── AnthropicProvider.cs     ← NEW
        │       ├── OpenAIProvider.cs        ← NEW
        │       └── CredentialProvider.cs   ← NEW
        └── UI/
            ├── SettingsViewModel.cs         ← NEW
            └── SettingsView.xaml            ← NEW
```

---

## Critical Conventions

- **All Pro SDK / CIM access inside `QueuedTask.Run()`** — no exceptions, ever
- **MVVM only** — zero business logic in XAML code-behind
- **BYOK is optional** — audit and all deterministic fixes must work with no API key configured
- **AI fixes are suggestion-only** — user must review and confirm before any LLM fix is applied to CIM
- **API keys never in logs** — not even last 4 chars; only a masked status indicator in UI
- **`HttpClient` is injected singleton** — never instantiated inline

---

## Architecture Summary

The fix engine sits alongside the existing audit engine. `AuditEngine` returns `AuditFinding` records as before. A new `FixEngine` resolves the appropriate `IFixStrategy` for each finding and exposes it to the ViewModel. The dockpane gains a "Fix" button column and a Settings tab. The LLM layer is fully optional — if no key is configured, `LLMFixStrategy` is never invoked.

Detailed design in `ai-dev/architecture.md`.

---

## Hard Constraints

Read `ai-dev/guardrails/` before writing ANY code. Guardrails override all other instructions.
