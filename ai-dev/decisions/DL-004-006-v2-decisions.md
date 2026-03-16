# DL-004: BYOK is Optional — Audit and Deterministic Fixes Never Require a Key

**Date:** 2026-03-15
**Status:** Accepted
**Author:** Chris Lyons

## Context

v2 adds LLM-assisted fixes. The question is whether the AI features should be the primary path or strictly additive.

## Decision

BYOK is optional. The full audit and all deterministic fixes work with zero configuration. LLM features enhance the experience for users who choose to supply a key — they do not gate any existing capability.

## Alternatives Considered

- **Require key for advanced fixes** — Rejected. Government and enterprise users on restricted networks cannot use cloud APIs. Gating core functionality would exclude the primary audience.
- **Bundle a key** — Rejected. Creates rate limit exposure, security liability, and cost risk. Incompatible with the BYOK philosophy established in MetadataForge.

## Consequences

Enables: air-gapped deployment, zero-friction install, clear security story for IT reviewers.
Constrains: LLM features require user onboarding step (key acquisition and configuration).

---

# DL-005: IFixStrategy Interface Pattern

**Date:** 2026-03-15
**Status:** Accepted
**Author:** Chris Lyons

## Context

Fix strategies need to be independently testable, extensible without modifying FixEngine, and clearly separated between deterministic and LLM-assisted implementations.

## Decision

Each fix implementation implements `IFixStrategy` with `RequiresApiKey` and `ApplyFixAsync()`. `FixEngine` receives strategies via DI and resolves the appropriate one per finding type.

## Alternatives Considered

- **Switch statement in FixEngine** — Rejected. Adding a new fix type requires modifying FixEngine; violates open/closed principle.
- **Attributes on finding types** — Rejected. Adds reflection overhead and couples the model to the fix layer.

## Consequences

New fix strategies can be added by implementing the interface and registering in DI. Strategies are independently unit-testable by passing mock findings. `FixEngine` is stable — it never changes when a new fix type is added.

---

# DL-006: .NET 8 Upgrade — Single csproj Change, No API Migration

**Date:** 2026-03-15
**Status:** Accepted
**Author:** Chris Lyons

## Context

v1 targets .NET 6 / Pro SDK 3.0. v2 needs .NET 8 for modern C# features and to align with the current Pro SDK LTS. Pro 3.7 (May 2026) will require .NET 10.

## Decision

Upgrade `<TargetFramework>` from `net6.0-windows` to `net8.0-windows`. Bump SDK NuGet references to Pro SDK 3.4. No API migration required — Pro 3.x SDK is backwards compatible across .NET LTS versions.

Forward path to .NET 10 / Pro 3.7 is documented and requires the identical single-line change.

## Alternatives Considered

- **Stay on .NET 6** — Rejected. .NET 6 reaches end of support in November 2024 (already past). Cannot add `CredentialManagement` and modern `System.Text.Json` patterns cleanly.
- **Jump directly to .NET 10** — Rejected. Pro 3.7 SDK not available until May 2026. Visual Studio 2026 required for .NET 10 SDK development.

## Consequences

Minimum supported ArcGIS Pro version becomes 3.2 (the first Pro release that officially supports the .NET 8 runtime). v1 users on Pro 3.0–3.1 must remain on v1. This is documented clearly in the README compatibility table.
