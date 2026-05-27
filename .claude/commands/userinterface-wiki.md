---
description: UI/UX best practices across animation, CSS, audio, typography, UX patterns, and icons. Use when reviewing animations, CSS pseudo-elements, audio feedback, typography, UX laws, prefetching, or icon implementations.
---

# UI Interface Wiki — v3.0.0

152 rules across 12 categories. Output format: `file:line - [rule-id] description`

## Category priority table

| # | Category | Priority | Rules |
|---|----------|----------|-------|
| 1 | Animation Principles | CRITICAL | 12 |
| 2 | Timing Functions | HIGH | 16 |
| 3 | Exit Animations | HIGH | 12 |
| 4 | CSS Pseudo Elements | MEDIUM | 15 |
| 5 | Audio Feedback | MEDIUM | 14 |
| 6 | Sound Synthesis | MEDIUM | 13 |
| 7 | Morphing Icons | LOW | 9 |
| 8 | Container Animation | MEDIUM | 7 |
| 9 | Laws of UX | HIGH | 23 |
| 10 | Predictive Prefetching | MEDIUM | 6 |
| 11 | Typography | MEDIUM | 16 |
| 12 | Visual Design | HIGH | 9 |

---

## 1. Animation Principles (CRITICAL)

```
timing-under-300ms     All animations must complete within 300ms
consistent-timing      Use the same duration for same animation type
no-entrance-ctx-menu   No entrance animations on context menus
exponential-ramps      Use exponential ramp for audio value changes
no-linear-easing       Never use linear easing for UI transitions
active-state-scale     Active/pressed states use scale, not color alone
subtle-squash-stretch  Squash/stretch range: 0.95–1.05 only
springs-for-overshoot  Use springs when overshoot is desirable
stagger-under-50ms     Stagger between items: max 50ms
single-focal-point     One focal animation per screen at a time
dim-background         Dim background when modal/overlay is open
z-index-stacking       Maintain consistent z-index stacking order
```

---

## 2. Timing Functions (HIGH)

```javascript
// Springs: for gestures, interruptible animations, velocity-based
const springConfig = { type: "spring", stiffness: 300, damping: 30, mass: 1 };
// Balanced spring: stiffness 200-400, damping 20-40, mass 0.5-1.5

// Easing for state changes: ease-out entrance, ease-in exit, ease-in-out view transitions
const easings = {
  entrance: 'cubic-bezier(0, 0, 0.2, 1)',    // ease-out
  exit:     'cubic-bezier(0.4, 0, 1, 1)',    // ease-in
  transition: 'cubic-bezier(0.4, 0, 0.2, 1)' // ease-in-out
};

// Linear only for: progress bars, loading indicators, data-driven motion
// Never linear for: UI state changes, user interactions

// Duration guidelines:
// press/hover:         120–180ms
// small state change:  180–260ms
// max duration:        300ms
// high-frequency ops:  no animation (scroll, resize, keyboard repeat)
// context menus:       no animation (instant)
```

---

## 3. Exit Animations (HIGH)

```jsx
// ✅ Always wrap with AnimatePresence
import { AnimatePresence, motion } from 'framer-motion';

<AnimatePresence>
  {isVisible && (
    <motion.div
      key="unique-key"         // required: enables exit detection
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      exit={{ opacity: 0 }}    // mirrors initial
    />
  )}
</AnimatePresence>

// ✅ mode="wait": halve timing for synchronization
<AnimatePresence mode="wait">

// ✅ mode="popLayout": for reordering lists
<AnimatePresence mode="popLayout">

// ✅ Disable enter on first render
<AnimatePresence initial={false}>

// ✅ useIsPresent for child-controlled unmounting
function Child() {
  const isPresent = useIsPresent();
  const safeToRemove = useSafeToRemove();
  useEffect(() => {
    if (!isPresent) {
      setTimeout(safeToRemove, 300);
    }
  }, [isPresent]);
}
```

---

## 4. CSS Pseudo Elements (MEDIUM)

```css
/* content property required */
.element::before { content: ''; }
.element::after  { content: ''; }

/* ✅ Prefer pseudo-elements over DOM nodes for decorative content */
.card::before {
  content: '';
  position: absolute;  /* parent needs position: relative */
  inset: 0;
  z-index: -1;
  background: linear-gradient(to bottom, transparent, rgba(0,0,0,.5));
}

/* ✅ Hit target expansion (no DOM change) */
.small-button::before {
  content: '';
  position: absolute;
  inset: -8px;
}

/* ✅ View Transitions — unique name per element, cleanup after */
.card { view-transition-name: card-1; } /* must be unique across page */

/* ✅ Style View Transition pseudo-elements */
::view-transition-old(card-1) { animation: slide-out 300ms ease-in; }
::view-transition-new(card-1) { animation: slide-in 300ms ease-out; }

/* ✅ Other useful pseudo-elements */
input::placeholder    { color: #999; }
::selection           { background: #b3d4ff; color: #000; }
li::marker            { color: var(--accent); }
p::first-line         { font-variant: small-caps; }
dialog::backdrop      { background: rgba(0,0,0,.5); backdrop-filter: blur(4px); }
```

---

## 5. Audio Feedback (MEDIUM)

```javascript
// ✅ Always provide visual equivalent for any audio
// ✅ Respect prefers-reduced-motion — disable audio feedback
// ✅ Allow user to toggle audio on/off
// ✅ Volume: start at 0.3 (subtle default)
// ✅ Short sounds for confirmations only; errors/warnings only
// ✅ No decorative audio (sound for sound's sake)
// ✅ Match sound weight to action weight
// ✅ Match sound duration to interaction duration
// ✅ Preload audio to avoid delay on first play

const audio = new Audio('/sounds/notification.mp3');
audio.volume = 0.3;

// Reset currentTime before playing to allow rapid replays
async function playSound(audio) {
  audio.currentTime = 0;
  await audio.play().catch(() => {}); // handle autoplay policy silently
}
```

---

## 6. Sound Synthesis (MEDIUM)

```javascript
// ✅ Singleton AudioContext — create once, reuse
let ctx;
function getAudioContext() {
  if (!ctx) ctx = new AudioContext();
  if (ctx.state === 'suspended') ctx.resume(); // resume on user gesture
  return ctx;
}

function playClick() {
  const ctx = getAudioContext();
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();

  osc.connect(gain);
  gain.connect(ctx.destination);

  // ✅ Never use setValueAtTime(0) — causes click artifact; use 0.001
  gain.gain.setValueAtTime(0.001, ctx.currentTime);
  gain.gain.exponentialRampToValueAtTime(0.3, ctx.currentTime + 0.01);
  gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.05);

  osc.frequency.setValueAtTime(800, ctx.currentTime);
  // Filter: bandpass 3000–6000Hz, Q: 2–5
  const filter = ctx.createBiquadFilter();
  filter.type = 'bandpass';
  filter.frequency.value = 4000;
  filter.Q.value = 3;

  osc.start(ctx.currentTime);
  osc.stop(ctx.currentTime + 0.05);

  // ✅ Cleanup nodes after use
  osc.onended = () => { osc.disconnect(); gain.disconnect(); };
}
```

---

## 7. Morphing Icons (LOW)

```jsx
// ✅ Exactly 3 lines for hamburger icon (no more, no less)
// ✅ Consistent 14×14 viewBox across icon set
// ✅ round strokeLinecap on all paths
// ✅ aria-hidden="true" on all icon SVGs (label the button, not the icon)
// ✅ Spring physics for rotation (stiffness 300, damping 30)
// ✅ Respect prefers-reduced-motion

function HamburgerIcon({ isOpen }) {
  const spring = { type: "spring", stiffness: 300, damping: 30 };
  return (
    <svg viewBox="0 0 14 14" aria-hidden="true" width="14" height="14">
      <motion.line x1="1" y1="3" x2="13" y2="3"
        animate={isOpen ? { rotate: 45, y: 4 } : {}}
        style={{ originX: 7, originY: 3 }}
        transition={spring}
        strokeLinecap="round" />
      <motion.line x1="1" y1="7" x2="13" y2="7"
        animate={isOpen ? { opacity: 0 } : {}}
        strokeLinecap="round" />
      <motion.line x1="1" y1="11" x2="13" y2="11"
        animate={isOpen ? { rotate: -45, y: -4 } : {}}
        style={{ originX: 7, originY: 11 }}
        transition={spring}
        strokeLinecap="round" />
    </svg>
  );
}
```

---

## 8. Container Animation (MEDIUM)

```jsx
// ✅ Two-div pattern for height animation (outer clips, inner has content)
function AnimatedContainer({ children, isOpen }) {
  const contentRef = useRef(null);
  const [height, setHeight] = useState(0);

  useEffect(() => {
    if (!contentRef.current) return;
    const observer = new ResizeObserver(([entry]) => {
      setHeight(entry.contentRect.height);
    });
    observer.observe(contentRef.current);
    return () => observer.disconnect();
  }, []);

  return (
    <motion.div
      style={{ overflow: 'hidden' }}
      animate={{ height: isOpen ? height : 0 }}
      initial={false} // ✅ guard zero on initial render
    >
      <div ref={contentRef}>{children}</div>
    </motion.div>
  );
}
```

---

## 9. Laws of UX (HIGH)

| Law | Principle | Application |
|-----|-----------|-------------|
| Fitts's | Time ∝ distance/size | Large targets for frequent actions |
| Hick's | Time ∝ number of choices | Limit options to 5–7 |
| Miller's | Working memory: 7±2 items | Group into chunks of 3–5 |
| Doherty Threshold | < 400ms response | Show feedback within 100ms |
| Postel's | Liberal in input, strict in output | Accept many input formats |
| Jakob's | Users spend time on other sites | Match common patterns |
| Aesthetic-Usability | Beautiful = more usable (perceived) | Polish matters |
| Proximity | Close = related | Group related elements |
| Von Restorff | Distinct = memorable | Highlight key actions |
| Serial Position | First and last items recalled best | Important items at start/end |
| Peak-End | Judge by peak + end | Nail the key moment and exit |
| Goal-Gradient | Accelerate near goal | Show progress clearly |
| Cognitive Load | Reduce extraneous load | ≤4 items in working memory |

---

## 10. Predictive Prefetching (MEDIUM)

```javascript
// ✅ Trajectory-based prefetching (better than hover alone)
import { createPredictor } from 'predictive-fetcher';

const predictor = createPredictor({
  hitSlop: 100,        // px radius around target to start prefetch
  threshold: 0.7,      // confidence threshold
});

// ✅ Touch fallback: prefetch on touchstart (no hover on mobile)
link.addEventListener('touchstart', () => prefetch(link.href), { passive: true });

// ✅ Keyboard: prefetch on Tab focus
link.addEventListener('focus', () => prefetch(link.href));

// ✅ Use selectively — don't prefetch everything
// Only prefetch: primary CTAs, high-probability next pages
```

---

## 11. Typography (MEDIUM)

```css
/* Tabular numbers for dynamic data */
.counter, .price, .timer { font-variant-numeric: tabular-nums; }

/* Oldstyle figures for prose */
.body-text { font-variant-numeric: oldstyle-nums; }

/* Slashed zero for code/IDs */
.code { font-variant-numeric: slashed-zero; }

/* OpenType features */
.text {
  font-feature-settings: 'calt' 1, 'ss02' 1; /* contextual alternates, style set 2 */
  font-optical-sizing: auto;
  -webkit-font-smoothing: antialiased;
}

/* Text wrapping */
h1, h2, h3 { text-wrap: balance; }
p, li       { text-wrap: pretty; }

/* Underlines */
a { text-underline-offset: 0.2em; }

/* Variable fonts */
@font-face {
  font-family: 'Inter';
  src: url('/fonts/inter.woff2') format('woff2-variations');
  font-weight: 100 900; /* full range — no separate files needed */
  font-display: swap;
}

/* Letter spacing for uppercase */
.label-uppercase {
  text-transform: uppercase;
  letter-spacing: 0.08em; /* always add tracking with uppercase */
}

/* Justify + hyphenation for long-form text */
.article {
  text-align: justify;
  hyphens: auto;
}

/* Diagonal fractions */
.recipe-ingredient { font-variant-numeric: diagonal-fractions; }
```

---

## 12. Visual Design (HIGH)

```css
/* Concentric border radius: inner = outer - padding */
.card { border-radius: 16px; padding: 8px; }
.card .inner { border-radius: 8px; } /* 16 - 8 */

/* Layered shadows (adapt to any background) */
.elevated {
  box-shadow:
    0 0 0 1px rgba(0,0,0,.04),
    0 1px 2px rgba(0,0,0,.06),
    0 4px 8px rgba(0,0,0,.08),
    0 16px 32px rgba(0,0,0,.06);
}

/* Consistent shadow direction — always from top-left */
/* Shadow colors: tinted toward background, never pure black */

/* Elevation scale */
/* Level 0 (flat):   no shadow */
/* Level 1 (card):   0 1px 3px rgba(0,0,0,.1) */
/* Level 2 (popup):  0 4px 16px rgba(0,0,0,.12) */
/* Level 3 (modal):  0 16px 48px rgba(0,0,0,.16) */

/* Animate shadow via pseudo-element opacity (GPU-composited) */
.card { position: relative; }
.card::after {
  content: '';
  position: absolute;
  inset: 0;
  box-shadow: 0 16px 32px rgba(0,0,0,.16);
  opacity: 0;
  transition: opacity 200ms;
}
.card:hover::after { opacity: 1; }

/* Consistent spacing scale (4px base) */
/* 4, 8, 12, 16, 24, 32, 48, 64, 96, 128 */

/* Semi-transparent borders */
.button {
  border: 1px solid rgba(255,255,255,.15); /* works on any background */
}

/* 6-layer button shadow anatomy */
.button {
  box-shadow:
    inset 0 1px 0 rgba(255,255,255,.2),   /* top highlight */
    inset 0 -1px 0 rgba(0,0,0,.1),        /* bottom shadow */
    0 1px 2px rgba(0,0,0,.08),            /* drop shadow */
    0 0 0 1px rgba(0,0,0,.04),            /* border */
    0 4px 8px rgba(0,0,0,.06),            /* ambient */
    0 0 0 var(--focus-ring-width, 0) var(--focus-color); /* focus ring */
}
```
