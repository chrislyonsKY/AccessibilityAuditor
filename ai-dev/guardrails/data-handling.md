# Data Handling Guardrails — v2 Additions

> Extends existing data-handling.md. Read the original first.

## Credential Security (CRITICAL — v2)

- API keys stored exclusively in Windows Credential Manager targets:
  - `AccessibilityAuditor/Anthropic`
  - `AccessibilityAuditor/OpenAI`
- Keys never written to disk outside Credential Manager
- Keys never logged at any level (Debug, Info, Warning, Error)
- Keys never included in exception `.Message` or `.ToString()` output
- Keys never appear in the HTML audit report
- `CredentialProvider` is the single point of credential access — no other class touches the Credential Manager API

## LLM Prompt Content

- LLM prompts may include: element type, element name, color values, font sizes, contrast ratios
- LLM prompts must NOT include: user PII, file paths containing usernames, server names, or UNC paths
- Sanitize any CIM element paths before including in prompts

## Output Files

- HTML audit reports must not include API key status, provider configuration, or any credential metadata
- Log files must not include API keys, key prefixes, or any string that could identify a key
