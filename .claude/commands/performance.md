---
description: Optimize web performance — loading speed, bundle size, images, fonts, caching, runtime. Use when asked to "improve performance", "speed up the site", "reduce bundle size", "optimize loading", or "fix slow page".
---

# Performance Optimization

Comprehensive performance optimization guide. Targets: LCP < 2.5s, FCP < 1.8s, total page weight < 1.5MB.

## Performance budget

| Resource | Budget |
|----------|--------|
| Total page weight | < 1.5MB |
| JavaScript | < 300KB (gzipped) |
| CSS | < 100KB |
| Images | < 1MB total |
| Fonts | < 100KB |
| Third-party scripts | < 200KB |

---

## Server & Network

### TTFB < 800ms
- Use a CDN (static assets to the edge)
- Enable Brotli compression
- Use HTTP/2 or HTTP/3
- Consider SSG/ISR for cacheable content

### HTTP 103 Early Hints
Delivers `Link: preload` headers before the full response — can yield 20–30% LCP improvement on image-heavy pages.

```
103 Early Hints
Link: </styles.css>; rel=preload; as=style
Link: </hero.webp>; rel=preload; as=image
```

### Preconnect to required origins
```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://api.example.com" crossorigin>
```

### Speculation Rules API (instant navigations)
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

---

## JavaScript

### Defer non-essential scripts
```html
<!-- ❌ Blocking -->
<script src="heavy-library.js"></script>

<!-- ✅ Deferred -->
<script defer src="heavy-library.js"></script>

<!-- ✅ Non-blocking async (for scripts without DOM dependency) -->
<script async src="analytics.js"></script>

<!-- ✅ ES modules defer by default -->
<script type="module" src="app.js"></script>
```

### Code splitting
```javascript
// Route-level splitting (React Router / Next.js)
const ProductPage = React.lazy(() => import('./ProductPage'));

// Component-level splitting
const HeavyChart = dynamic(() => import('./HeavyChart'), { ssr: false });

// Feature-level splitting (load on demand)
button.addEventListener('click', async () => {
  const { initEditor } = await import('./editor');
  initEditor();
}, { once: true });
```

### Tree shaking
```javascript
// ❌ Imports entire library (e.g. lodash: ~70KB)
import _ from 'lodash';
_.debounce(fn, 300);

// ✅ Named import — bundler tree-shakes the rest
import { debounce } from 'lodash-es';

// ✅ Even better: use the native equivalent when available
const debounced = (fn, delay) => {
  let timer;
  return (...args) => { clearTimeout(timer); timer = setTimeout(() => fn(...args), delay); };
};
```

### Avoid barrel file imports (critical for bundle size)
```javascript
// ❌ Barrel import can load hundreds of modules
import { Button } from '@company/ui';   // may load 200+ components

// ✅ Direct import — only loads what you need
import { Button } from '@company/ui/button';
```

---

## Images

### Format selection
| Format | Use case |
|--------|----------|
| AVIF | Photos, complex images (92%+ browser support) |
| WebP | Fallback for AVIF |
| PNG | Images with transparency (when AVIF/WebP not suitable) |
| SVG | Icons, logos, illustrations |

### Responsive images
```html
<!-- ✅ Modern format with fallback + responsive sizes -->
<picture>
  <source type="image/avif" srcset="hero-400.avif 400w, hero-800.avif 800w" sizes="(max-width: 600px) 400px, 800px">
  <source type="image/webp" srcset="hero-400.webp 400w, hero-800.webp 800w" sizes="(max-width: 600px) 400px, 800px">
  <img src="hero-800.jpg" alt="Hero image" width="800" height="450"
       fetchpriority="high" loading="eager">
</picture>

<!-- ✅ Below-fold images: lazy load -->
<img src="product.webp" alt="Product" width="400" height="400" loading="lazy">
```

### LCP image: always high priority
```html
<link rel="preload" href="/hero.webp" as="image" fetchpriority="high">
<img src="/hero.webp" alt="Hero" fetchpriority="high">
```

---

## Fonts

### Optimize loading
```css
/* ✅ Preload critical font weights only */
/* In <head>: <link rel="preload" href="/fonts/inter-400.woff2" as="font" crossorigin> */

@font-face {
  font-family: 'Inter';
  src: url('/fonts/inter-400.woff2') format('woff2');
  font-display: swap;        /* shows fallback immediately */
  unicode-range: U+0000-00FF; /* subset to Latin */
}
```

### Variable fonts (when using 3+ weights)
```css
/* ✅ One file instead of multiple weight files */
@font-face {
  font-family: 'Inter';
  src: url('/fonts/inter-variable.woff2') format('woff2-variations');
  font-weight: 100 900;
  font-display: swap;
}
```

### System font stack (fastest — no font download)
```css
body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', system-ui,
               Roboto, Helvetica, Arial, sans-serif;
}
```

---

## Caching

```
# Static assets (long TTL + content hash in filename)
Cache-Control: public, max-age=31536000, immutable

# HTML (short TTL)
Cache-Control: public, max-age=0, must-revalidate

# API responses
Cache-Control: private, max-age=60
```

---

## Runtime performance

### Avoid layout thrashing
```javascript
// ❌ Forces multiple reflows
elements.forEach(el => {
  const height = el.offsetHeight;  // read
  el.style.height = height + 10 + 'px';  // write — triggers reflow
});

// ✅ Batch reads then writes
const heights = elements.map(el => el.offsetHeight);  // all reads
elements.forEach((el, i) => {
  el.style.height = heights[i] + 10 + 'px';  // all writes
});
```

### Debounce expensive operations
```javascript
// ✅ Limit how often resize/scroll handlers run
const debounce = (fn, delay) => {
  let timer;
  return (...args) => { clearTimeout(timer); timer = setTimeout(() => fn(...args), delay); };
};
window.addEventListener('resize', debounce(handleResize, 150));
```

### Virtualize long lists
```jsx
import { VirtualList } from 'react-virtualized';

// Only renders visible rows — critical for 1000+ item lists
<VirtualList
  height={600}
  rowCount={items.length}
  rowHeight={50}
  rowRenderer={({ index, key, style }) => (
    <div key={key} style={style}>{items[index].name}</div>
  )}
/>
```

### View Transitions API (smooth page transitions)
```javascript
document.startViewTransition(() => {
  updateDOM(); // any DOM change is animated
});
```

---

## Third-party scripts

### Facade pattern (load on interaction)
```javascript
// ❌ Chat widget loads immediately, blocks TTI
<script src="https://chat-widget.com/embed.js"></script>

// ✅ Facade: show placeholder, load real widget on click
const facade = document.getElementById('chat-facade');
facade.addEventListener('click', async () => {
  const { initChat } = await import('https://chat-widget.com/embed.js');
  initChat();
  facade.remove();
}, { once: true });
```

---

## Measurement

| Metric | Target | Tool |
|--------|--------|------|
| LCP | < 2.5s | Lighthouse, CrUX |
| FCP | < 1.8s | Lighthouse |
| Speed Index | < 3.4s | WebPageTest |
| TBT | < 200ms | Lighthouse |
| TTI | < 3.8s | Lighthouse |

```bash
# Lighthouse CLI
npx lighthouse https://example.com --output html --view

# Bundle analysis
npx webpack-bundle-analyzer stats.json
```

## References
- [web.dev/performance](https://web.dev/performance)
- [Core Web Vitals](/core-web-vitals)
