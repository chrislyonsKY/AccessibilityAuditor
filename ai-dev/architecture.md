E# AccessibilityAuditor v2 — Architecture Addendum

> Extends the existing architecture.md. Read the original first.
> Only v2 additions are documented here.

---

## New Data Flow

```
Existing audit flow (unchanged):
  AuditEngine.Run(layout) → IEnumerable<AuditFinding>

New fix flow:
  ViewModel receives AuditFinding list
        ↓
  FixEngine.ResolveStrategy(finding) → IFixStrategy?
        ↓
  ViewModel binds strategy availability to "Fix" button state
        ↓
  User clicks "Fix"
        ↓
  DeterministicFixStrategy:
    QueuedTask.Run(() => apply CIM change) → FixResult(Applied)
    ViewModel refreshes finding row as resolved

  LLMFixStrategy:
    CredentialProvider.Retrieve(provider) → key
    ILLMProvider.CompleteAsync(context, ct) → suggestion string
    FixResult(Suggested, content) → shown in review panel
    User confirms → QueuedTask.Run(() => apply CIM change)
    User dismisses → finding stays in list unchanged
```

---

## Fix Engine

```csharp
// FixEngine resolves which strategy handles which finding type.
// Finding types without a registered strategy return null — no fix button shown.
public class FixEngine
{
    private readonly IEnumerable<IFixStrategy> _strategies;
    private readonly CredentialProvider _credentials;

    public IFixStrategy? ResolveStrategy(AuditFinding finding) { ... }
    public async Task<IReadOnlyList<FixResult>> ApplyAllDeterministicAsync(
        IEnumerable<AuditFinding> findings, CancellationToken ct) { ... }
}
```

---

## LLM Provider Abstraction

```csharp
public interface ILLMProvider
{
    string ProviderName { get; }
    Task<string> CompleteAsync(string prompt, CancellationToken ct);
}
```

`AnthropicProvider` posts to `https://api.anthropic.com/v1/messages`.  
`OpenAIProvider` posts to `https://api.openai.com/v1/chat/completions`.  
Both read their key from `CredentialProvider` at call time — never cached in memory between calls.

---

## Credential Storage

Target names in Windows Credential Manager:
- `AccessibilityAuditor/Anthropic`
- `AccessibilityAuditor/OpenAI`

`CredentialProvider` exposes:
```csharp
void Store(LLMProviderType provider, string key);
string? Retrieve(LLMProviderType provider);   // null if not configured
void Delete(LLMProviderType provider);
bool IsConfigured(LLMProviderType provider);
```

`IsConfigured()` is the only method the ViewModel calls to determine button availability. The key itself never leaves `LLMFixStrategy`.

---

## Settings Tab

Added as a second tab to the existing dockpane. The existing audit tab is tab index 0 — Settings is tab index 1. No structural changes to the existing tab.

Settings tab contains:
- Provider `RadioButton` group: Anthropic / OpenAI
- API key `PasswordBox` (masked) + "Save Key" button
- "Test Connection" button → sends minimal completion request, shows pass/fail status
- Key status indicator: "Configured ✓" / "Not configured" — no key value ever displayed
- "Remove Key" button → calls `CredentialProvider.Delete()`

---

## Dockpane Changes to Existing Audit Tab

Two additions only — do not restructure existing layout:

1. **"Fix" button column** in findings `DataGrid` — per-row, visibility bound to finding's strategy availability
2. **"Fix All Auto" button** in toolbar area — runs `FixEngine.ApplyAllDeterministicAsync()` across all findings

---

## Deterministic Color Fix Algorithm

For contrast failures, the nearest WCAG AA-passing color is calculated as follows:

1. Parse failing foreground color from CIM element
2. Get background color (from CIM or default white)
3. Walk the HSL hue of the foreground color, adjusting lightness in steps
4. At each step, compute WCAG contrast ratio: `(L1 + 0.05) / (L2 + 0.05)`
5. Return the first color achieving ratio ≥ 4.5:1 (AA normal) or ≥ 3:1 (AA large text)
6. Present as a one-click swap — user sees before/after swatch before committing

This is pure math — no external dependency, no LLM.

---

## .NET 8 Upgrade

Single change to `.csproj`:
```xml
<!-- Before -->
<TargetFramework>net6.0-windows</TargetFramework>

<!-- After -->
<TargetFramework>net8.0-windows</TargetFramework>
```

SDK NuGet references bump from Pro SDK 3.0 to 3.4. No API changes required — 3.x SDK is backwards compatible.

Forward path to .NET 10 / Pro 3.7 (May 2026): same single-line change to `net10.0-windows` + SDK NuGet bump to 3.7.

---

## New Dependencies

| Package | Purpose |
|---|---|
| `CredentialManagement` | Windows Credential Manager access |
| `System.Text.Json` | JSON serialization for LLM request/response (.NET 8 built-in) |

No other new NuGet dependencies. `HttpClient` is .NET built-in.
