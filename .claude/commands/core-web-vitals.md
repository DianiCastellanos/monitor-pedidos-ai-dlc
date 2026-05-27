---
description: Optimize Core Web Vitals (LCP, INP, CLS) for better page experience and search ranking. Use when asked to "improve Core Web Vitals", "fix LCP", "reduce CLS", "optimize INP", "page experience optimization", or "fix layout shifts".
---

# Core Web Vitals optimization

Targeted optimization for the three Core Web Vitals metrics.

## The three metrics

| Metric | Measures | Good | Needs work | Poor |
|--------|----------|------|------------|------|
| **LCP** | Loading | ≤ 2.5s | 2.5s – 4s | > 4s |
| **INP** | Interactivity | ≤ 200ms | 200ms – 500ms | > 500ms |
| **CLS** | Visual Stability | ≤ 0.1 | 0.1 – 0.25 | > 0.25 |

Google measures at the **75th percentile** — 75% of visits must meet "Good".

---

## LCP: Largest Contentful Paint

### Common LCP issues

**1. Render-blocking resources**
```html
<!-- ❌ Blocks rendering -->
<link rel="stylesheet" href="/all-styles.css">

<!-- ✅ Critical CSS inlined, rest deferred -->
<style>/* Critical above-fold CSS */</style>
<link rel="preload" href="/styles.css" as="style"
      onload="this.onload=null;this.rel='stylesheet'">
```

**2. Slow resource load**
```html
<!-- ✅ Preloaded with high priority -->
<link rel="preload" href="/hero.webp" as="image" fetchpriority="high">
<img src="/hero.webp" alt="Hero" fetchpriority="high">
```

**3. Client-side rendering delays**
```javascript
// ❌ Content loads after JavaScript
useEffect(() => {
  fetch('/api/hero-text').then(r => r.json()).then(setHeroText);
}, []);

// ✅ Server-side or static rendering
export async function getServerSideProps() {
  const heroText = await fetchHeroText();
  return { props: { heroText } };
}
```

**4. Speculation Rules API (prerender likely-next navigations)**
```html
<script type="speculationrules">
{
  "prerender": [{
    "where": { "href_matches": "/*" },
    "eagerness": "moderate"
  }]
}
</script>
```

`moderate` eagerness starts after ~200ms hover. Start with `moderate` — captures most navigations without prerendering pages users never visit.

> ⚠️ Side effects (analytics, ads) fire when prerender starts, not when user navigates. Gate them on `document.prerendering`. Chromium-only — progressive enhancement.

### LCP checklist
- [ ] TTFB < 800ms (CDN, edge caching)
- [ ] LCP image preloaded with `fetchpriority="high"`
- [ ] LCP image optimized (WebP/AVIF, correct size)
- [ ] Critical CSS inlined (< 14KB)
- [ ] No render-blocking JavaScript in `<head>`
- [ ] LCP element in initial HTML (not JS-rendered)
- [ ] Speculation Rules added for likely navigations

### Identify LCP element
```javascript
new PerformanceObserver((list) => {
  const entries = list.getEntries();
  const last = entries[entries.length - 1];
  console.log('LCP element:', last.element, '| time:', last.startTime);
}).observe({ type: 'largest-contentful-paint', buffered: true });
```

---

## INP: Interaction to Next Paint

Total INP = **Input Delay** + **Processing Time** + **Presentation Delay**

| Phase | Target |
|-------|--------|
| Input Delay | < 50ms |
| Processing | < 100ms |
| Presentation | < 50ms |

### Common INP issues

**1. Long tasks — use `scheduler.yield()`**
```javascript
async function processLargeArray(items) {
  const CHUNK_SIZE = 100;
  for (let i = 0; i < items.length; i += CHUNK_SIZE) {
    items.slice(i, i + CHUNK_SIZE).forEach(expensiveOperation);

    if ('scheduler' in window && 'yield' in scheduler) {
      await scheduler.yield(); // boosted priority — preferred
    } else {
      await new Promise(r => setTimeout(r, 0)); // fallback
    }
  }
}
```

**2. Heavy event handlers — prioritize visual feedback first**
```javascript
button.addEventListener('click', async () => {
  // 1. Immediate visual feedback
  button.classList.add('loading');

  // 2. Yield so browser paints before blocking
  if ('scheduler' in window && 'yield' in scheduler) {
    await scheduler.yield();
  }

  // 3. Heavy work
  const result = calculateComplexThing();
  updateUI(result);

  // 4. Non-urgent work last
  if ('requestIdleCallback' in window) {
    requestIdleCallback(() => trackEvent('click'));
  }
});
```

**3. React: memoize expensive components**
```javascript
const MemoizedExpensive = React.memo(ExpensiveComponent);

function App() {
  const [count, setCount] = useState(0);
  return (
    <div>
      <Counter count={count} />
      <MemoizedExpensive /> {/* Won't re-render on count change */}
    </div>
  );
}
```

### INP checklist
- [ ] No tasks > 50ms on main thread
- [ ] Event handlers complete quickly (< 100ms)
- [ ] Visual feedback provided immediately
- [ ] Heavy work deferred with `scheduler.yield()` or `requestIdleCallback`
- [ ] Third-party scripts don't block interactions

### INP debugging
```javascript
new PerformanceObserver((list) => {
  for (const entry of list.getEntries()) {
    if (entry.duration > 200) {
      console.warn('Slow interaction:', {
        type: entry.name,
        duration: entry.duration,
        target: entry.target
      });
    }
  }
}).observe({ type: 'event', buffered: true, durationThreshold: 40 });
```

---

## CLS: Cumulative Layout Shift

**CLS Formula:** `impact fraction × distance fraction`

### Common CLS causes

**1. Images without dimensions**
```html
<!-- ❌ Causes layout shift when loaded -->
<img src="photo.jpg" alt="Photo">

<!-- ✅ Space reserved -->
<img src="photo.jpg" alt="Photo" width="800" height="600">

<!-- ✅ Or use aspect-ratio -->
<img src="photo.jpg" alt="Photo" style="aspect-ratio: 4/3; width: 100%;">
```

**2. Web fonts causing FOUT**
```css
/* ✅ Optional font (no shift if slow) */
@font-face {
  font-family: 'Custom';
  src: url('custom.woff2') format('woff2');
  font-display: optional;
}

/* ✅ Or match fallback metrics */
@font-face {
  font-family: 'Custom';
  src: url('custom.woff2') format('woff2');
  font-display: swap;
  size-adjust: 105%;
  ascent-override: 95%;
}
```

**3. Animations triggering layout**
```css
/* ❌ Animates layout properties */
.animate { transition: height 0.3s, width 0.3s; }

/* ✅ Use transform instead */
.animate { transition: transform 0.3s; }
```

### CLS checklist
- [ ] All images have `width`/`height` or `aspect-ratio`
- [ ] Ads/embeds have reserved space (`min-height`)
- [ ] Fonts use `font-display: optional` or matched metrics
- [ ] Dynamic content inserted below viewport
- [ ] Animations use `transform`/`opacity` only

### CLS debugging
```javascript
new PerformanceObserver((list) => {
  for (const entry of list.getEntries()) {
    if (!entry.hadRecentInput) {
      console.log('Layout shift:', entry.value);
      entry.sources?.forEach(s => console.log('  Element:', s.node));
    }
  }
}).observe({ type: 'layout-shift', buffered: true });
```

---

## Measurement

```javascript
import {onLCP, onINP, onCLS} from 'web-vitals';

function sendToAnalytics({name, value, rating}) {
  gtag('event', name, {
    event_category: 'Web Vitals',
    value: Math.round(name === 'CLS' ? value * 1000 : value),
    event_label: rating
  });
}

onLCP(sendToAnalytics);
onINP(sendToAnalytics);
onCLS(sendToAnalytics);
```

## Framework quick fixes

### Next.js
```jsx
import Image from 'next/image';
<Image src="/hero.jpg" priority fill alt="Hero" />

const HeavyComponent = dynamic(() => import('./Heavy'), { ssr: false });
```

### React
```jsx
<link rel="preload" href="/hero.jpg" as="image" fetchpriority="high" />

const [isPending, startTransition] = useTransition();
startTransition(() => setExpensiveState(newValue));
```

## References
- [web.dev LCP](https://web.dev/articles/lcp)
- [web.dev INP](https://web.dev/articles/inp)
- [web.dev CLS](https://web.dev/articles/cls)
