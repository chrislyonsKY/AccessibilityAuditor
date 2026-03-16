# ADR-001: CIM Inspection vs. Map Rendering for Color Analysis

## Status
Accepted

## Context
To evaluate WCAG color contrast compliance, we need to determine the colors visible to the user in the map product. There are two approaches:

1. **CIM Inspection** — Read the Cartographic Information Model object graph to extract symbol colors, label colors, and background colors programmatically
2. **Map Rendering** — Render the map to a bitmap and perform pixel-level color sampling

## Decision
We use **CIM Inspection** as the primary approach, with rendered pixel sampling as a future enhancement for edge cases.

## Rationale

**CIM Inspection advantages:**
- Fast — no rendering required
- Precise — exact color values from the symbology definition
- Deterministic — same CIM always produces same results
- Non-destructive — read-only access
- Works without a map view — can inspect map packages and definitions

**CIM Inspection limitations:**
- Cannot determine actual rendered color when transparency composites against complex backgrounds
- Cannot determine label placement (which background pixels are behind which labels)
- Missed effects: visual hierarchy from layer ordering, overlapping symbols

**Map Rendering advantages:**
- Shows exactly what the user sees
- Handles all compositing, transparency, and overlap

**Map Rendering limitations:**
- Slow — must render the map
- View-dependent — results change with extent, scale, and screen resolution
- Complex — pixel sampling algorithms for "background of this label" are non-trivial

## Consequences
- Phase 1 uses CIM-only inspection
- We flag heterogeneous backgrounds (imagery, raster) as `ManualReview` rather than attempting pixel sampling
- We correctly handle transparency/opacity in CIM by computing composited colors algebraically where background is known (solid fill layers)
- Future phases may add optional rendered-bitmap sampling for higher fidelity

## Related Skills
- `Skills/01_Foundational_Cognition/010_Tradeoff_Analysis.md`
- `Skills/02_Architecture_and_Engineering/020_Performance_Optimization.md`
