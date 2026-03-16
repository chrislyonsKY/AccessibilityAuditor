# AccessibilityAuditor

![License](https://img.shields.io/badge/license-Apache--2.0-blue)
![Language](https://img.shields.io/badge/language-C%23-239120)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Platform](https://img.shields.io/badge/ArcGIS%20Pro-3.2%2B-blue?logo=esri)
![WCAG](https://img.shields.io/badge/WCAG-2.1%20AA-green)
![Pattern](https://img.shields.io/badge/pattern-MVVM-orange)
![Rules](https://img.shields.io/badge/rules-13-brightgreen)
![Tests](https://img.shields.io/badge/tests-145%2B-brightgreen)
![BYOK](https://img.shields.io/badge/LLM-BYOK%20optional-8A2BE2)
![Status](https://img.shields.io/badge/status-active-brightgreen)

> ArcGIS Pro SDK add-in that performs semi-automated WCAG 2.1 Level AA compliance auditing on GIS map products — with one-click auto-fix and optional AI-assisted remediation.

Scans maps, layouts, web maps, and Experience Builder apps for accessibility issues, provides actionable remediation guidance, and in v2 applies deterministic fixes directly to map elements. An optional BYOK AI layer (Anthropic or OpenAI) drafts alt-text, suggests accessible color palettes, and generates Section 508 compliance summaries.

Built for GIS analysts publishing maps under Section 508 compliance requirements.

---

## Compatibility

| Version | .NET | Min ArcGIS Pro | Notes |
|---|---|---|---|
| v1.x | .NET 6 | 3.0 | Maintained on `v1` branch |
| v2.x | .NET 8 | 3.2 | Current — adds fix engine + optional BYOK AI |
| v2.1 (planned) | .NET 10 | 3.7 | May 2026 — single csproj retarget, no API changes |

---

## Screenshots

### Ribbon & Dockpane

The add-in adds an **Accessibility** tab to the ArcGIS Pro ribbon. Clicking the button opens the dockpane.

<img width="888" height="151" alt="image" src="https://github.com/user-attachments/assets/c633109e-33af-4268-aae7-3a9ee690924b" />

### Dashboard

After running an audit, the Dashboard tab shows the overall compliance score and per-principle breakdowns with pass/fail/warning counts.

<img width="500" height="779" alt="image" src="https://github.com/user-attachments/assets/b3d33d1c-bc6b-421d-b850-9304b9932549" />

### Findings List

Each principle tab (Perceivable, Operable, Understandable, Robust) lists findings with severity icons, color swatches, contrast ratios, and inline remediation previews.

<img width="496" height="431" alt="image" src="https://github.com/user-attachments/assets/5ef4700b-f27a-4a85-93f6-ce788f4dc4d3" />

### Finding Detail Window

Double-click any finding to open a modeless detail window showing the full issue description, color preview with colorblind simulation across all three deficiency types, remediation guidance, and navigation/copy actions.

<img width="975" height="759" alt="image" src="https://github.com/user-attachments/assets/21b25edb-f7c2-49ca-83fa-ca0edbd26d5c" />

### Colorblind Simulation Window

The Color Sim window shows the full map palette under Protanopia (red-blind), Deuteranopia (green-blind), and Tritanopia (blue-blind) simulation. Each swatch is labeled with its source layer and category. Pairwise distinguishability issues and contrast losses are called out below.

<img width="975" height="759" alt="image" src="https://github.com/user-attachments/assets/7b80faea-4243-4c07-b3d4-ae97eb16d4fa" />

### Settings Window

Configure audit behavior: toggle pass findings visibility, enable/disable colorblind safety evaluation, and adjust the contrast warning margin.

<img width="975" height="759" alt="image" src="https://github.com/user-attachments/assets/9609c4ba-ea19-4640-9d62-d03273a84548" />

### Dark Theme

All views and ProWindows automatically adapt to ArcGIS Pro's dark theme. Severity colors, progress bars, and secondary text are all theme-safe.

<img width="418" height="790" alt="image" src="https://github.com/user-attachments/assets/d0a72ca9-874c-4adf-94c3-e7edb670cf56" />

---

## Features

### Audit
- **ArcGIS Pro maps** — active map CIM inspection
- **ArcGIS Pro layouts** — print/PDF export accessibility
- **Portal/AGOL web maps** — browse and scan from your active portal connection
- **Experience Builder apps** — widget and configuration analysis

### Auto-Fix (v2 — no API key required)
- One-click deterministic fixes for contrast failures, font size violations, missing alt-text stubs, and colorblind palette violations
- **Fix All Auto** — batch apply all available deterministic fixes in a single pass with summary result count
- Fix button per finding in the findings grid — applies directly to the CIM element

### AI-Assisted Fixes (v2 — optional BYOK)
- LLM-drafted alt-text suggestions for layout elements — user reviews before applying
- Alternative accessible color palette suggestions preserving visual intent
- Plain-language explanation of findings and how to resolve them in context
- Section 508 compliance summary generation for documentation
- Supports **Anthropic (Claude)** and **OpenAI (GPT-4o)**
- API key stored in Windows Credential Manager — never bundled, never proxied
- Full audit and all deterministic fixes work with no key configured

### What It Checks

| WCAG Principle | Checks | Example |
|---|---|---|
| **Perceivable** | Text/label contrast, non-text contrast, alt text, color-only encoding, images of text, reading order | Label color #333 on #555 = 1.99:1 (FAIL, needs 4.5:1) |
| **Operable** | Page titles, headings and labels in pop-ups | Missing or generic map title |
| **Understandable** | Language of page attribute | Web map missing `culture` property |
| **Robust** | Pop-up HTML validity, ARIA labels on interactive widgets | Unlabeled buttons in pop-up HTML |

### Additional Capabilities
- **CIM-level color contrast analysis** against WCAG AA thresholds (4.5:1 normal text, 3:1 large text, 3:1 non-text)
- **Symbology-aware colorblind simulation** using Brettel/Mollon matrices — each swatch labeled with source layer, category, and color role (Fill/Stroke/Label), with pairwise distinguishability analysis and simulated contrast loss detection
- **Portal web map browser** — browse web maps from your signed-in portal, or enter an item ID directly
- **Imagery background detection** — flags heterogeneous backgrounds for manual review instead of false passes
- **Halo-aware contrast** — checks label halo color as the effective background when present
- **Layout element descriptions** — reads alt text from CIM `CustomProperties`
- **Layout reading order analysis** — compares element z-order to spatial position
- **Pop-up HTML validation** — checks for unclosed tags, missing alt text, unlabeled inputs
- **Navigate to Element** — select the source layer or layout element from a finding detail
- **Copy finding detail** to clipboard for documentation
- **Score dashboard** — overall and per-principle compliance scores with pass/total breakdowns
- **Configurable settings** — toggle pass findings, warning margin, colorblind safety; persisted to `%LocalAppData%\AccessibilityAuditor\`

---

## WCAG 2.1 AA Coverage

13 rule implementations covering 12 unique WCAG criteria across all 4 principles:

| Rule ID | Criterion | Principle | Auto Level |
|---|---|---|---|
| `WCAG_1_1_1_ALT_TEXT` | 1.1.1 Non-text Content | Perceivable | Auto |
| `WCAG_1_1_1_PORTAL_DESC` | 1.1.1 Portal Description | Perceivable | Auto |
| `WCAG_1_3_1_STRUCTURE` | 1.3.1 Info and Relationships | Perceivable | Semi |
| `WCAG_1_3_2_SEQUENCE` | 1.3.2 Meaningful Sequence | Perceivable | Semi |
| `WCAG_1_4_1_USE_OF_COLOR` | 1.4.1 Use of Color | Perceivable | Semi |
| `WCAG_1_4_3_CONTRAST` | 1.4.3 Contrast (Minimum) | Perceivable | Auto |
| `WCAG_1_4_5_IMAGES_TEXT` | 1.4.5 Images of Text | Perceivable | Semi |
| `WCAG_1_4_11_NON_TEXT` | 1.4.11 Non-text Contrast | Perceivable | Auto |
| `WCAG_2_4_2_TITLE` | 2.4.2 Page Titled | Operable | Auto |
| `WCAG_2_4_6_HEADINGS` | 2.4.6 Headings and Labels | Operable | Semi |
| `WCAG_3_1_1_LANGUAGE` | 3.1.1 Language of Page | Understandable | Auto |
| `WCAG_4_1_1_POPUP` | 4.1.1 Parsing | Robust | Auto |
| `WCAG_4_1_2_NAME_ROLE` | 4.1.2 Name, Role, Value | Robust | Semi |

---

## Architecture

```
AccessibilityAuditor/
├── Core/
│   ├── Models/          # Finding, ColorInfo, ScoreCard, AuditSettings, LabeledColor, etc.
│   ├── Rules/           # IComplianceRule contract, RuleRegistry
│   └── Constants/       # WcagCriteria definitions, ContrastThresholds
├── Rules/               # 13 rule implementations
├── Services/
│   ├── CimInspector/    # CimWalker, SymbologyAnalyzer, LabelAnalyzer, BackgroundColorEstimator
│   ├── ColorAnalysis/   # ContrastCalculator, ColorBlindSimulator, PaletteEvaluator
│   ├── PortalInspector/ # PortalItemChecker, WebMapChecker, PopupChecker, ExBChecker
│   ├── RuleEngine/      # RuleExecutor, RemediationEngine
│   ├── Fixes/           # IFixStrategy, FixResult, DeterministicFixStrategy, LLMFixStrategy, FixEngine
│   └── LLM/             # ILLMProvider, AnthropicProvider, OpenAIProvider, CredentialProvider
├── Orchestration/       # AuditOrchestrator, ScanPipeline, AuditContext
├── ViewModels/          # MVVM ViewModels (DockPane, Dashboard, Principle, FindingDetail, Settings, ColorSim, About)
├── Views/               # WPF XAML dockpane content + value converters
├── Windows/             # ProWindow dialogs (FindingDetail, ColorSimulation, Settings, About)
├── docs/screenshots/    # UI screenshots for documentation
└── tests/               # xUnit tests (16 files, 145+ cases)
```

### Design Principles
- **MVVM strictly enforced** — Views are XAML-only; all logic in ViewModels
- **QueuedTask-safe** — All CIM access runs on the Main CIM Thread
- **Continue-on-error** — Individual rule failures produce Error findings, never crash the scan
- **Extensible rule engine** — Add new WCAG rules by implementing `IComplianceRule` and registering
- **ProWindow pattern** — Detail views and settings use `ProWindow` for automatic Pro dark/light theming
- **Settings flow** — `AuditSettings` propagate from UI → pipeline → context → rules for configurable thresholds
- **Screen reader accessible** — All controls have `AutomationProperties.Name`, `HelpText`, `LiveSetting`, and keyboard bindings
- **Fix-layer separation** — Fix engine is additive; audit engine and all check classes are unchanged in v2

### Key Technical Decisions
- **CIM inspection** over rendered bitmap sampling — fast, deterministic, read-only
- **Symbology-aware simulation** over pixel-level filter — each palette color retains its layer name, category, and role (Fill/Stroke/Label), enabling pairwise analysis
- **System.Text.Json** for Portal REST parsing
- **CommunityToolkit.Mvvm** for MVVM base classes
- **No external color libraries** — contrast math is self-contained
- **IFixStrategy interface** — deterministic and LLM fix strategies are independently testable and extensible without modifying FixEngine
- **BYOK via Windows Credential Manager** — API keys never bundled, never proxied, never logged

---

## Requirements

- **ArcGIS Pro 3.2+** (v2) or 3.0+ (v1 branch)
- **.NET 8** (`net8.0-windows`)
- **ArcGIS Pro SDK 3.4** assemblies (referenced from `C:\Program Files\ArcGIS\Pro\bin\`)
- Optional: Anthropic or OpenAI API key for AI-assisted fixes

## Installation

1. Build the solution in Visual Studio 2022
2. The add-in (`.esriAddinX`) is generated in the output directory
3. Double-click the `.esriAddinX` file to install into ArcGIS Pro
4. In ArcGIS Pro, find the **Accessibility** tab on the ribbon
5. Click **Accessibility Auditor** to open the dockpane

## Usage

1. Select a target from the dropdown: **Active Map**, **Active Layout**, or **Portal Web Map**
2. For portal targets, click the refresh button to browse web maps, or enter an item ID directly
3. Click **Run Audit**
4. Review findings in the tabbed interface (Dashboard, Perceivable, Operable, Understandable, Robust)
5. Click **Fix** on any finding to apply a deterministic fix, or open an AI-assisted suggestion
6. Click **Fix All Auto** to apply all available deterministic fixes in one pass
7. Double-click any finding to open the detail window with color previews, colorblind simulation, and remediation guidance
8. Use **Navigate to Element** to jump to the source layer or layout element
9. Use **Color Sim** to view the full map palette under colorblind simulation
10. Use **Settings** (⚙️) to configure audit behavior or add an API key for AI-assisted fixes
11. Use **About** (ℹ️) for version and license information

### Configuring AI-Assisted Fixes (optional)

1. Open **Settings** → **AI Fixes** tab
2. Select your provider: Anthropic or OpenAI
3. Enter your API key and click **Save Key**
4. Click **Test Connection** to verify
5. Fix buttons for AI-eligible findings become active automatically

Your key is stored in Windows Credential Manager and never transmitted except directly to your configured provider.

---

## Development

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test tests/AccessibilityAuditor.Tests/
```

> **Note:** Tests should be run via Visual Studio's Test Explorer due to ArcGIS Pro SDK build targets requiring desktop MSBuild.

### AI-Assisted Development

This project uses multi-agent AI-assisted development. See `ai-dev/` for specialized agent prompts, architecture decision records, and the WCAG criteria mapping spec.

| Agent | File | Expertise |
|---|---|---|
| Accessibility Expert | `agent-accessibility-expert.md` | WCAG interpretation, severity, remediation text |
| ArcGIS Pro SDK | `agent-arcgis-pro-sdk.md` | CIM traversal, SDK threading, color extraction |
| Color Science | `agent-color-science.md` | Contrast math, Brettel simulation, alpha compositing |
| WCAG Rule Engine | `agent-wcag-rule-engine.md` | Rule interface, registry, scoring |
| WPF / MVVM | `agent-wpf-mvvm.md` | DockPane + ProWindow XAML, Pro theming |
| Portal REST API | `agent-portal-rest-api.md` | Portal REST, web map JSON, pop-up HTML |
| C# / SDK (v2) | `csharp_expert.md` | Fix engine, BYOK layer, Settings tab |

---

## Development Phases

| Phase | Status | Scope |
|---|---|---|
| Phase 1 — Foundation | ✅ Complete | Solution scaffold, CIM walker, contrast calculator, 5 core rules, dockpane UI |
| Phase 2 — Color Engine | ✅ Complete | Colorblind simulation, palette evaluator, background estimation |
| Phase 3 — Portal Integration | ✅ Complete | Portal REST API, web map/pop-up/ExB scanning, 3 portal rules |
| Phase 4 — Rule Expansion | ✅ Complete | Reading order, images of text, headings/labels, name/role/value |
| Phase 5 — Polish | ✅ Complete | Settings wiring, symbology-aware color simulation, CIM description extraction, About window, navigate to element, dark theme, screen reader accessibility |
| Phase 6 — Fix Engine + BYOK | 🔄 In Progress | Deterministic auto-fix, optional LLM-assisted fixes, .NET 8 upgrade |

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## Security

See [SECURITY.md](SECURITY.md) for reporting vulnerabilities.

API keys are stored exclusively in Windows Credential Manager and are never written to disk, logs, or output files.

## License

Apache-2.0 — see [LICENSE](LICENSE) for details.

## Author

**Chris Lyons**
