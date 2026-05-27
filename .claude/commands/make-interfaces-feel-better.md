---
description: Apply UI polish details that make interfaces feel better — border radius, animations, typography, touch targets, shadows. Use when asked to "polish the UI", "improve feel", "make it feel more native", or "review interactions".
---

# Details that make interfaces feel better

Great interfaces rarely come from a collection of small details that compound into a great experience. Apply these principles when building or reviewing UI code.

## Quick Reference

| Category | When to Use |
| --- | --- |
| Typography | Text wrapping, font smoothing, tabular numbers |
| Surfaces | Border radius, optical alignment, shadows, image outlines, hit areas |
| Animations | Interruptible animations, enter/exit transitions, icon animations, scale on press |
| Performance | Transition specificity, `will-change` usage |

## Core Principles

### 1. Concentric Border Radius
"Outer radius = inner radius + padding." Mismatched radii on nested elements is the most common thing that makes interfaces feel off.

```css
/* ❌ Mismatched */
.card { border-radius: 16px; padding: 16px; }
.card .inner { border-radius: 4px; }

/* ✅ Concentric: inner = outer - padding */
.card { border-radius: 16px; padding: 8px; }
.card .inner { border-radius: 8px; } /* 16 - 8 = 8 */
```

### 2. Optical Over Geometric Alignment
When geometric centering looks off, align optically. Buttons with icons, play triangles, and asymmetric icons all need manual adjustment.

```css
/* ✅ Nudge icon slightly right for optical centering */
.play-icon { transform: translateX(1px); }
```

### 3. Shadows Over Borders
Layer multiple transparent `box-shadow` values for natural depth. Shadows adapt to any background; solid borders don't.

```css
/* ❌ Flat border */
.card { border: 1px solid #e2e8f0; }

/* ✅ Layered shadows */
.card {
  box-shadow:
    0 0 0 1px rgba(0,0,0,.04),
    0 2px 4px rgba(0,0,0,.06),
    0 8px 16px rgba(0,0,0,.06);
}
```

### 4. Interruptible Animations
Use CSS transitions for interactive state changes — they can be interrupted mid-animation. Reserve keyframes for staged sequences that run once.

```css
/* ✅ CSS transition — interruptible */
.btn { transition: background 150ms ease-out; }

/* Use @keyframes only for one-shot sequences */
@keyframes toast-in { from { opacity: 0; transform: translateY(8px); } }
```

### 5. Split and Stagger Enter Animations
"Don't animate a single container. Break content into semantic chunks and stagger each with ~100ms delay."

```css
.item:nth-child(1) { animation-delay: 0ms; }
.item:nth-child(2) { animation-delay: 100ms; }
.item:nth-child(3) { animation-delay: 200ms; }
```

### 6. Subtle Exit Animations
Use a small fixed `translateY` instead of full height. Exits should be softer than enters.

```css
/* ✅ Subtle exit — translates out a small amount */
@keyframes exit { to { opacity: 0; transform: translateY(4px); } }
```

### 7. Contextual Icon Animations
Animate icons with `opacity`, `scale`, and `blur`. Use: scale from `0.25` to `1`, opacity from `0` to `1`, blur from `4px` to `0px`. For motion libraries, use `transition: { type: "spring", duration: 0.3, bounce: 0 }`. Without libraries, keep both icons in DOM and cross-fade with CSS transitions using `cubic-bezier(0.2, 0, 0, 1)`.

```css
.icon-enter {
  animation: icon-in 200ms cubic-bezier(0.2, 0, 0, 1) forwards;
}
@keyframes icon-in {
  from { opacity: 0; scale: 0.25; filter: blur(4px); }
  to   { opacity: 1; scale: 1;    filter: blur(0); }
}
```

### 8. Font Smoothing
"Apply `-webkit-font-smoothing: antialiased` to the root layout on macOS for crisper text."

```css
body {
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}
```

### 9. Tabular Numbers
"Use `font-variant-numeric: tabular-nums` for any dynamically updating numbers to prevent layout shift."

```css
.counter, .price, .timer {
  font-variant-numeric: tabular-nums;
}
```

### 10. Text Wrapping
Use `text-wrap: balance` on headings. Use `text-wrap: pretty` for body text to avoid orphans.

```css
h1, h2, h3, h4 { text-wrap: balance; }
p, li           { text-wrap: pretty; }
```

### 11. Image Outlines
"Add a subtle `1px` outline with low opacity to images for consistent depth." Use pure black in light mode (`rgba(0, 0, 0, 0.1)`) and pure white in dark mode (`rgba(255, 255, 255, 0.1)`).

```css
img {
  outline: 1px solid rgba(0, 0, 0, 0.1);
  outline-offset: -1px; /* inset */
}

@media (prefers-color-scheme: dark) {
  img { outline-color: rgba(255, 255, 255, 0.1); }
}
```

### 12. Scale on Press
"A subtle `scale(0.96)` on click gives buttons tactile feedback. Always use `0.96`. Never use a value smaller than `0.95`" — anything below feels exaggerated.

```css
button:active { transform: scale(0.96); }
```

### 13. Skip Animation on Page Load
Use `initial={false}` on `AnimatePresence` to prevent enter animations on first render.

```jsx
<AnimatePresence initial={false}>
  {isOpen && <Modal key="modal" />}
</AnimatePresence>
```

### 14. Never Use `transition: all`
Always specify exact properties: `transition-property: scale, opacity`. Tailwind's `transition-transform` covers `transform, translate, scale, rotate`.

```css
/* ❌ Animates everything, including layout properties */
.btn { transition: all 200ms; }

/* ✅ Only composited properties */
.btn { transition: opacity 200ms, transform 200ms; }
```

### 15. Use `will-change` Sparingly
"Only for `transform`, `opacity`, `filter` — properties the GPU can composite. Never use `will-change: all`."

```css
/* ✅ Only on elements that actually animate */
.animated-card {
  will-change: transform;
}

/* Remove after animation completes */
.animated-card.done {
  will-change: auto;
}
```

### 16. Minimum Hit Area
Interactive elements need at least 40×40px hit area. Extend with a pseudo-element if the visible element is smaller.

```css
/* ✅ Extend hit area without changing visual size */
.small-icon-btn {
  position: relative;
}
.small-icon-btn::before {
  content: '';
  position: absolute;
  inset: -8px; /* extends hit area by 8px on all sides */
}
```
