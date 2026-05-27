---
description: Google's DESIGN.md standard — create, lint, and export design system tokens in YAML+Markdown format for AI coding agents. Use when asked to "create a design system", "document design tokens", "set up DESIGN.md", or "export design tokens".
---

# DESIGN.md Standard (Google Labs)

A format combining YAML front matter (machine-readable design tokens) with markdown prose (human-readable rationale) to describe visual design systems to AI coding agents.

## Format

```markdown
---
# YAML front matter: machine-readable design tokens
colors:
  primary: "#1a73e8"
  on-primary: "#ffffff"
  surface: "#ffffff"
  background: "#f8f9fa"

typography:
  font-family: "Google Sans, Roboto, system-ui, sans-serif"
  scale:
    xs: "12px"
    sm: "14px"
    base: "16px"
    lg: "20px"
    xl: "24px"
    2xl: "32px"
    3xl: "48px"

spacing:
  base: 4
  scale: [4, 8, 12, 16, 24, 32, 48, 64, 96, 128]

rounded:
  sm: "4px"
  md: "8px"
  lg: "16px"
  full: "9999px"

components:
  button:
    height: "36px"
    padding: "0 24px"
    border-radius: "18px"
---

# Design System — [Product Name]

## Overview

[One paragraph: what this product is, who it's for, the design intent.]

## Colors

[Rationale for color choices. When to use each color. Dark mode behavior.]

## Typography

[Font choice rationale. Scale usage guidelines. Responsive behavior.]

## Layout

[Grid system. Spacing philosophy. Breakpoints.]

## Elevation

[Shadow system. When to use each elevation level.]

## Shapes

[Border radius usage. Concentric radius rules.]

## Components

[Key component specifications. States. Variants.]

## Do's and Don'ts

| Do | Don't |
|----|-------|
| Use primary color for primary actions only | Use primary color for decoration |
| Maintain concentric border radius | Mix radius values randomly |
```

---

## Section ordering (required by linter)

1. Overview
2. Colors
3. Typography
4. Layout
5. Elevation
6. Shapes
7. Components
8. Do's and Don'ts

---

## Token schema reference

### Colors
```yaml
colors:
  primary: "#hex"           # Main brand color — primary actions
  on-primary: "#hex"        # Text/icons on primary color
  secondary: "#hex"         # Secondary actions
  surface: "#hex"           # Card/panel backgrounds
  background: "#hex"        # Page background
  error: "#hex"             # Error states
  on-error: "#hex"          # Text on error
  outline: "#hex"           # Borders, dividers
  # Dark mode variants
  dark:
    primary: "#hex"
    surface: "#hex"
    background: "#hex"
```

### Typography
```yaml
typography:
  font-family: "Name, fallback, system-ui"
  font-family-mono: "Name, monospace"
  scale:
    xs:   "12px / 1.4"
    sm:   "14px / 1.5"
    base: "16px / 1.6"
    lg:   "20px / 1.4"
    xl:   "24px / 1.3"
    2xl:  "32px / 1.25"
    3xl:  "48px / 1.2"
  weight:
    regular: 400
    medium:  500
    semibold: 600
    bold:    700
```

### Spacing
```yaml
spacing:
  base: 4       # 4px base unit
  scale: [4, 8, 12, 16, 24, 32, 48, 64, 96, 128]
```

### Components
```yaml
components:
  button:
    height: "36px"
    padding: "0 24px"
    border-radius: "18px"
    font-size: "14px"
    font-weight: 500
  input:
    height: "40px"
    padding: "0 12px"
    border-radius: "4px"
    border: "1px solid {colors.outline}"
  card:
    border-radius: "12px"
    padding: "16px"
    elevation: 1
```

---

## CLI commands

```bash
# Lint a DESIGN.md file
npx @google-labs/design-md lint DESIGN.md

# Show diff between two design versions
npx @google-labs/design-md diff DESIGN.md DESIGN.new.md

# Export tokens to Tailwind config
npx @google-labs/design-md export --format json-tailwind DESIGN.md

# Export to CSS custom properties
npx @google-labs/design-md export --format css-tailwind DESIGN.md

# Export to DTCG (Design Token Community Group) format
npx @google-labs/design-md export --format dtcg DESIGN.md
```

---

## Linting rules

| Rule | Description |
|------|-------------|
| `broken-ref` | Token references `{x.y}` that don't resolve |
| `missing-primary` | No `colors.primary` defined |
| `contrast-ratio` | Foreground/background pairs below 4.5:1 |
| `orphaned-tokens` | Tokens defined but never referenced |
| `token-summary` | Missing required token categories |
| `missing-sections` | Required sections not present |
| `missing-typography` | No typography tokens defined |
| `section-order` | Sections not in canonical order |

---

## Integration with impeccable

When using alongside `/impeccable`:
1. Create `DESIGN.md` first with this standard
2. Reference it in impeccable's context loading step
3. `impeccable` will use the DESIGN.md tokens to validate design decisions
4. Run `npx @google-labs/design-md lint DESIGN.md` before critique sessions

---

## Workflow: create a new design system

1. Run `/design-md` with product name and brief description
2. Answer questions about brand direction (register: brand vs product)
3. Generate DESIGN.md with tokens + rationale
4. Run `lint` to validate
5. Export to Tailwind/CSS for implementation
6. Commit DESIGN.md alongside code — it evolves with the product
