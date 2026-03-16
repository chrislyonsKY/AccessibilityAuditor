# WCAG 2.1 AA Criteria Mapping for GIS Products

## Purpose
This document maps each WCAG 2.1 Level AA success criterion to its applicability
for GIS map products, defining what is author-controlled, platform-controlled,
and how the AccessibilityAuditor tool checks each criterion.

## Notation
- **Auto** = Fully automated check
- **Semi** = Automated scan + manual review flag
- **Manual** = Guidance only, human must evaluate
- **N/A** = Not applicable to map products
- **Platform** = Esri's responsibility (viewer/framework behavior)

---

## 1. Perceivable

| ID | Criterion | Level | Layout | Web Map | ExB | .mpkx | Check Type | Rule ID |
|---|---|---|---|---|---|---|---|---|
| 1.1.1 | Non-text Content | A | Auto | Auto | Semi | Auto | Alt text on elements, item descriptions | WCAG_1_1_1_ALT_TEXT |
| 1.2.1-5 | Time-based Media | A/AA | N/A | N/A | Platform | N/A | — | — |
| 1.3.1 | Info and Relationships | A | Semi | Semi | Semi | N/A | Reading order, pop-up structure | WCAG_1_3_1_STRUCTURE |
| 1.3.2 | Meaningful Sequence | A | Semi | Platform | Platform | N/A | Layout element order | WCAG_1_3_2_SEQUENCE |
| 1.3.3 | Sensory Characteristics | A | Manual | Manual | Manual | Manual | Color-only references in text | WCAG_1_3_3_SENSORY |
| 1.3.4 | Orientation | AA | N/A | Platform | Platform | N/A | — | — |
| 1.3.5 | Identify Input Purpose | AA | N/A | N/A | Semi | N/A | Form autocomplete attributes | WCAG_1_3_5_INPUT |
| 1.4.1 | Use of Color | A | Semi | Semi | Semi | Semi | Redundant encoding beyond color | WCAG_1_4_1_USE_OF_COLOR |
| 1.4.2 | Audio Control | A | N/A | N/A | Platform | N/A | — | — |
| 1.4.3 | Contrast (Minimum) | AA | Auto | Auto | Semi | Auto | 4.5:1 text, 3:1 large text | WCAG_1_4_3_CONTRAST |
| 1.4.4 | Resize Text | AA | N/A | Platform | Platform | N/A | — | — |
| 1.4.5 | Images of Text | AA | Semi | Semi | Semi | Semi | Detect rasterized text | WCAG_1_4_5_IMAGES_TEXT |
| 1.4.10 | Reflow | AA | N/A | Platform | Platform | N/A | — | — |
| 1.4.11 | Non-text Contrast | AA | Auto | Auto | Semi | Auto | 3:1 for graphics/UI | WCAG_1_4_11_NON_TEXT |
| 1.4.12 | Text Spacing | AA | N/A | Platform | Platform | N/A | — | — |
| 1.4.13 | Content on Hover | AA | N/A | Platform | Platform | N/A | — | — |

## 2. Operable

| ID | Criterion | Level | Layout | Web Map | ExB | .mpkx | Check Type | Rule ID |
|---|---|---|---|---|---|---|---|---|
| 2.1.1 | Keyboard | A | N/A | Platform | Platform | N/A | — | — |
| 2.1.2 | No Keyboard Trap | A | N/A | Platform | Platform | N/A | — | — |
| 2.1.4 | Character Key Shortcuts | A | N/A | Platform | Platform | N/A | — | — |
| 2.2.1-2 | Timing | A | N/A | Platform | Platform | N/A | — | — |
| 2.3.1 | Three Flashes | A | N/A | Platform | Platform | N/A | — | — |
| 2.4.1 | Bypass Blocks | A | N/A | Platform | Semi | N/A | Skip-nav in ExB | WCAG_2_4_1_BYPASS |
| 2.4.2 | Page Titled | A | Auto | Auto | Auto | Auto | Meaningful title exists | WCAG_2_4_2_TITLE |
| 2.4.3 | Focus Order | A | N/A | Platform | Platform | N/A | — | — |
| 2.4.5 | Multiple Ways | AA | N/A | N/A | Semi | N/A | Navigation alternatives | WCAG_2_4_5_MULTIPLE |
| 2.4.6 | Headings and Labels | AA | Semi | Semi | Semi | N/A | Descriptive headings | WCAG_2_4_6_HEADINGS |
| 2.5.1-4 | Input Modalities | A | N/A | Platform | Platform | N/A | — | — |

## 3. Understandable

| ID | Criterion | Level | Layout | Web Map | ExB | .mpkx | Check Type | Rule ID |
|---|---|---|---|---|---|---|---|---|
| 3.1.1 | Language of Page | A | Semi | Auto | Auto | Semi | Language attribute set | WCAG_3_1_1_LANGUAGE |
| 3.1.2 | Language of Parts | AA | Manual | Semi | Semi | Manual | Mixed language content | WCAG_3_1_2_LANG_PARTS |
| 3.2.1-2 | Predictable | A | N/A | Platform | Platform | N/A | — | — |
| 3.2.3 | Consistent Navigation | AA | N/A | N/A | Semi | N/A | Widget placement consistency | WCAG_3_2_3_NAVIGATION |
| 3.2.4 | Consistent Identification | AA | N/A | Semi | Semi | N/A | Same function = same label | WCAG_3_2_4_CONSISTENT |
| 3.3.1-2 | Input Assistance | A | N/A | Semi | Semi | N/A | Error identification, labels | WCAG_3_3_2_LABELS |
| 3.3.3-4 | Error Prevention | AA | N/A | Platform | Platform | N/A | — | — |

## 4. Robust

| ID | Criterion | Level | Layout | Web Map | ExB | .mpkx | Check Type | Rule ID |
|---|---|---|---|---|---|---|---|---|
| 4.1.1 | Parsing | A | N/A | Auto | Auto | N/A | Valid HTML in pop-ups | WCAG_4_1_1_PARSING |
| 4.1.2 | Name, Role, Value | A | N/A | Semi | Semi | N/A | ARIA on custom widgets | WCAG_4_1_2_NAME_ROLE |

---

## Summary Statistics

| Category | Total Criteria (AA) | Auto | Semi | Manual | Platform | N/A |
|---|---|---|---|---|---|---|
| Perceivable | 15 | 4 | 5 | 1 | 4 | 1 |
| Operable | 13 | 1 | 3 | 0 | 8 | 1 |
| Understandable | 11 | 2 | 4 | 1 | 3 | 1 |
| Robust | 2 | 1 | 1 | 0 | 0 | 0 |
| **Total** | **41** | **8** | **13** | **2** | **15** | **3** |

**Tool coverage: 23 of 41 criteria (56%)** — the remaining 44% are platform responsibility or not applicable to map content.
