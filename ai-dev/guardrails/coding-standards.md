# Coding Standards Guardrails — v2 Additions

> Extends existing coding-standards.md. Read the original first.
> These rules are additions and overrides for v2 features only.

## C# / .NET (v2 additions)

- `HttpClient` must be registered as a singleton in DI — never `new HttpClient()` inline
- All `ILLMProvider.CompleteAsync()` calls must accept and forward `CancellationToken`
- LLM call timeout must be configurable — default 30 seconds, set via `HttpClient.Timeout`
- `FixResult` is an immutable record — never mutate after creation
- `IFixStrategy.ApplyFixAsync()` must not throw for expected failure conditions — return `FixResult(Failed, reason)` instead
- Any Pro SDK / CIM access inside a fix strategy must be wrapped in `QueuedTask.Run()`
- `CredentialProvider` is the only class permitted to call the Windows Credential Manager API — no other class reads or writes credentials

## Credential Handling (CRITICAL)

- API keys MUST be stored exclusively in Windows Credential Manager
- API keys MUST NEVER appear in: log output, exception messages, `FixResult.Summary`, output HTML reports, or any temp file
- The `PasswordBox` in `SettingsView.xaml` must use WPF's built-in masking — never bind to a plain `string` property
- After saving, the UI shows only a status indicator ("Configured ✓") — never re-display the key value
- `CredentialProvider.Retrieve()` returns `null` if not configured — callers must handle null gracefully without logging the absence as an error

## Testing (v2 additions)

- `ILLMProvider` must be mockable via interface — no static HTTP calls in fix strategies
- `DeterministicFixStrategy` must have unit tests with mock `AuditFinding` inputs covering: fix applied, element not found, and CIM write failure
- `LLMFixStrategy` must have unit tests with a mock `ILLMProvider` covering: successful suggestion, timeout/cancellation, and provider error
- `CredentialProvider` tests must use a mock — never the real Windows Credential Manager in CI
