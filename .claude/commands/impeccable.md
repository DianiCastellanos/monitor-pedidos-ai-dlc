---
description: Impeccable frontend design skill — evaluate and improve UI quality using design laws, anti-pattern detection, and systematic review. Use when asked to "review the UI", "audit design", "polish the interface", "detect AI slop", or "improve visual quality".
allowed-tools:
  - Bash(npx impeccable *)
---

# Impeccable — Frontend Design

1 skill · 23 commands · curated anti-patterns for impeccable frontend design.

> For full functionality including anti-pattern CLI scanning, run `npx impeccable skills install` to install the complete skill with all reference files.

---

## Setup

```bash
# Detect UI anti-patterns in files/dirs
npx impeccable detect src/

# Detect via URL (requires Puppeteer)
npx impeccable detect https://example.com

# JSON output for CI
npx impeccable detect --json src/

# Fast regex-only mode
npx impeccable detect --fast src/
```

---

## Shared Design Laws

### Color

- Use OKLCH for all color definitions — perceptual uniformity, predictable lightness steps
- Every color decision requires a **strategy**: Restrained (near-monochrome), Committed (one accent), Full palette (3–5 hues), or Drenched (color as structure)
- No pure black (`#000`) or pure white (`#fff`) — use `oklch(8% 0.02 240)` / `oklch(97% 0.01 240)`
- Color contrast: ≥ 4.5:1 for body text, ≥ 3:1 for large text and UI components

### Theme

- Every design has one **scene sentence**: "This feels like ___." If you can't finish the sentence, the design has no identity.
- Choose a single dominant material: paper, glass, metal, fabric, light, void

### Typography

- Maximum 2 typefaces per project (usually 1 for product UI)
- Establish a modular scale: 12, 14, 16, 20, 24, 32, 48, 64
- Body text: 16–18px, line-height 1.5–1.65, measure 60–75 characters
- Use `text-wrap: balance` on headings, `text-wrap: pretty` on body
- Apply `font-variant-numeric: tabular-nums` to all numbers
- Prefer `font-optical-sizing: auto`
- `-webkit-font-smoothing: antialiased` on macOS roots

### Layout

- 4px spacing base: 4, 8, 12, 16, 24, 32, 48, 64, 96, 128
- Concentric border radius: `inner = outer − padding`
- Squint test: hierarchy must be readable at 20% blur
- Touch targets: minimum 44×44px

### Motion

- All animations ≤ 300ms
- Ease-out for entrances, ease-in for exits, ease-in-out for transitions
- Springs for gestures and interruptible animations
- Stagger: maximum 50ms between items
- Always `@media (prefers-reduced-motion: reduce)`

---

## Absolute Bans (never do these)

| Anti-pattern | Why |
|-------------|-----|
| Side-stripe accent borders (left/right colored border on card) | Looks like a CSS default, not a design decision |
| Gradient text on headings | Unreadable on varied backgrounds, screams "AI generated" |
| Glassmorphism as primary design language | Overused, legibility problems |
| Hero-metric layout (giant number, tiny label) without design intent | Template energy |
| Identical card grid with no visual hierarchy variation | Monotonous, no focal point |
| Modal as the first thought for any user action | Usually a better pattern exists |
| `transition: all` | Animates layout properties, causes reflows |
| `will-change: all` | Wastes GPU memory |
| Pure cyan/purple gradient + dark background | Classic "AI slop" palette |

---

## AI Slop Test

Before shipping, ask: does this look like it came from an AI design prompt? Check for:
- [ ] Gradient text on main headings
- [ ] Glowing accents on dark backgrounds
- [ ] Side-stripe border on cards
- [ ] All sections center-aligned
- [ ] Identical card grid with no variation
- [ ] Purple/violet gradient as brand color (without strong intentional reason)
- [ ] Too many drop shadows at inconsistent angles
- [ ] Hero with giant metric number + tiny descriptive label

If 2+ apply → redesign before proceeding.

---

## 23 Commands

| Category | Command | What it does |
|----------|---------|-------------|
| **Build** | `/craft` | Build a feature end-to-end with design quality |
| **Build** | `/shape` | Create a design brief from requirements |
| **Evaluate** | `/critique` | Full design critique (A11y, perf, anti-patterns, heuristics) |
| **Evaluate** | `/audit` | 5-dimension technical audit with scoring |
| **Evaluate** | `/heuristics-scoring` | Score against Nielsen's 10 heuristics |
| **Evaluate** | `/personas` | Evaluate against 5 user archetypes |
| **Evaluate** | `/cognitive-load` | Assess and reduce cognitive load |
| **Refine** | `/polish` | Systematic polish across 11 dimensions |
| **Refine** | `/bolder` | Amplify design personality |
| **Refine** | `/quieter` | Reduce visual noise |
| **Refine** | `/distill` | Simplify to the essence |
| **Enhance** | `/colorize` | Apply or refine color strategy |
| **Enhance** | `/typeset` | Improve typography systematically |
| **Enhance** | `/layout` | Fix spacing, rhythm, and grid |
| **Enhance** | `/animate` | Plan and implement animations |
| **Enhance** | `/delight` | Add micro-interactions and personality |
| **Enhance** | `/onboard` | Design better onboarding flows |
| **Fix** | `/clarify` | Improve UX copy and error messages |
| **Fix** | `/harden` | Handle edge cases and i18n |
| **Fix** | `/optimize` | Core Web Vitals and rendering performance |
| **Fix** | `/adapt` | Responsive design across breakpoints |
| **Iterate** | `/overdrive` | Push design to extraordinary quality |
| **Iterate** | `/extract` | Extract and document design system tokens |

---

## Routing Rules

1. **No files provided** → run `/shape` to create a design brief first
2. **Files provided, no command** → run `/audit` then recommend next steps
3. **Explicit command** → execute that command with the provided context

---

## Brand vs Product Register

**Brand register** (marketing sites, landing pages):
- Expressive typography, committed/drenched color
- Personality-forward, editorial layout
- Imagery-led, generous whitespace

**Product register** (apps, dashboards, tools):
- System fonts or one neutral typeface
- Restrained color (semantic vocabulary)
- Predictable grids, familiar component patterns
- All 8 interaction states implemented

Detect register from context before applying any design laws.
