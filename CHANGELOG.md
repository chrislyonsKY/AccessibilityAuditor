# Changelog — AccessibilityAuditor v2 additions

Add the following to the top of the existing CHANGELOG.md:

---

## [2.0.0] - 2026-03-15

### Added
- Deterministic fix engine — one-click auto-fix for contrast failures, font size violations,
  missing alt-text, and colorblind palette issues. No API key required.
- "Fix All Auto" button — applies all available deterministic fixes in a single pass with
  summary result count.
- BYOK AI layer — optional LLM-assisted fixes and suggestions via Anthropic (Claude) or
  OpenAI (GPT-4o). Full audit and deterministic fixes remain available without any key.
- Settings tab in dockpane — API key entry, provider selection, test connection, key removal.
  Keys stored in Windows Credential Manager; never written to disk or logs.
- AI fix review panel — LLM suggestions displayed for user review before any CIM change is applied.
- Per-finding "Fix" button in findings grid — deterministic fixes apply immediately;
  LLM fixes open the review panel.

### Changed
- Target framework upgraded from `net6.0-windows` (.NET 6) to `net8.0-windows` (.NET 8).
- Minimum supported ArcGIS Pro version is now 3.2 (was 3.0).
- SDK NuGet references updated to Pro SDK 3.4.

### Notes
- v1.x builds (.NET 6 / Pro 3.0) remain on the `v1` branch for users on Pro 3.0–3.1.
- Forward path to .NET 10 / Pro 3.7 (planned May 2026): single `<TargetFramework>` change,
  no API migration required.
