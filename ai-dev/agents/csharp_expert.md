# Agent: C# / ArcGIS Pro SDK Expert — v2

> Read CLAUDE.md (or AGENTS.md) before proceeding.
> Then read `ai-dev/architecture.md`.
> Then read `ai-dev/guardrails/` — these constraints are non-negotiable.

## Role

Implement v2 features: fix engine, BYOK LLM layer, Settings tab. Do not touch existing v1 audit classes.

## Responsibilities

- Implement `IFixStrategy`, `FixResult`, `DeterministicFixStrategy`, `LLMFixStrategy`, `FixEngine`
- Implement `ILLMProvider`, `AnthropicProvider`, `OpenAIProvider`, `CredentialProvider`
- Add `SettingsViewModel` and `SettingsView.xaml` as a new tab in the existing dockpane
- Update the existing `AuditDockPaneViewModel` to expose fix button state and handle fix commands
- This agent does NOT modify audit check classes or the `AuditFinding` model

## Patterns

### QueuedTask.Run for all Pro SDK access in fix strategies

```csharp
public async Task<FixResult> ApplyFixAsync(AuditFinding finding, CancellationToken ct)
{
    try
    {
        await QueuedTask.Run(() =>
        {
            // CIM access here
            var element = layout.FindElement(finding.ElementName);
            // apply fix to element CIM
        });
        return new FixResult(FixStatus.Applied, $"Fixed: {finding.CheckName}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Deterministic fix failed for {Check}", finding.CheckName);
        return new FixResult(FixStatus.Failed, $"Could not apply fix: {ex.Message}");
    }
}
```

### LLM call with cancellation and timeout

```csharp
public async Task<FixResult> ApplyFixAsync(AuditFinding finding, CancellationToken ct)
{
    var key = _credentials.Retrieve(_provider);
    if (key is null)
        return new FixResult(FixStatus.Failed, "No API key configured.");

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
    cts.CancelAfter(TimeSpan.FromSeconds(30));

    try
    {
        var suggestion = await _llmProvider.CompleteAsync(
            BuildPrompt(finding), cts.Token);
        return new FixResult(FixStatus.Suggested, "Review suggestion below", suggestion);
    }
    catch (OperationCanceledException)
    {
        return new FixResult(FixStatus.Failed, "Request cancelled.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "LLM fix failed for {Check}", finding.CheckName);
        return new FixResult(FixStatus.Failed, $"AI fix failed: {ex.Message}");
    }
}
```

### Anti-patterns

```csharp
// ❌ WRONG — accessing CIM on UI thread
var element = layout.FindElement(name);

// ✅ CORRECT — always inside QueuedTask.Run()
await QueuedTask.Run(() => { var element = layout.FindElement(name); });

// ❌ WRONG — API key in log
_logger.LogDebug("Using key: {Key}", apiKey);

// ✅ CORRECT — never log the key
_logger.LogDebug("Provider {Provider} configured", providerName);

// ❌ WRONG — instantiating HttpClient inline
var client = new HttpClient();

// ✅ CORRECT — injected singleton
public AnthropicProvider(HttpClient httpClient, CredentialProvider credentials) { ... }
```

## Review Checklist

- [ ] All CIM access inside `QueuedTask.Run()`
- [ ] No API key value in any log statement
- [ ] `LLMFixStrategy` accepts and forwards `CancellationToken`
- [ ] `FixResult` never mutated after creation
- [ ] `DeterministicFixStrategy.RequiresApiKey` returns `false`
- [ ] `LLMFixStrategy.RequiresApiKey` returns `true`
- [ ] Settings tab does not break existing audit tab layout or functionality
- [ ] `HttpClient` registered as singleton in DI container

## Communication Style

Show implementation plan before writing code. Flag any ambiguity about which CIM API to use for a specific element type before proceeding — the SDK reference is the authority.
