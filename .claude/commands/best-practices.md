---
description: Apply modern web development best practices for security, compatibility, and code quality. Use when asked to "apply best practices", "security audit", "modernize code", "code quality review", or "check for vulnerabilities".
---

# Best practices

Modern web development standards based on Lighthouse best practices audits. Covers security, browser compatibility, and code quality patterns.

## Security

### HTTPS everywhere

```html
<!-- ❌ Mixed content -->
<img src="http://example.com/image.jpg">

<!-- ✅ HTTPS only -->
<img src="https://example.com/image.jpg">
```

**HSTS Header:**
```
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
```

### Content Security Policy (CSP)

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' 'nonce-abc123' https://trusted.com;
  style-src 'self' 'nonce-abc123';
  img-src 'self' data: https:;
  connect-src 'self' https://api.example.com;
  frame-ancestors 'self';
  base-uri 'self';
  form-action 'self';
```

### Trusted Types (DOM-XSS defense)

Baseline across all major browsers since early 2026. Blocks raw strings reaching `innerHTML`, `eval`, or other DOM sinks.

```
Content-Security-Policy: require-trusted-types-for 'script'; trusted-types default;
```

```javascript
const escape = trustedTypes.createPolicy('default', {
  createHTML: (s) => DOMPurify.sanitize(s, { RETURN_TRUSTED_TYPE: true })
});

// ❌ Throws TypeError under enforcement
element.innerHTML = userInput;

// ✅ Goes through the policy
element.innerHTML = escape.createHTML(userInput);
```

### Subresource Integrity (SRI)

Pin every `<script>` and `<link>` from CDNs you don't control.

```html
<script src="https://cdn.example.com/lib@1.2.3/dist/lib.js"
        integrity="sha384-oqVuAfXRKap7fdgcCY5uykM6+R9GqQ8K/uxy9rx7HNQlGYl1kPzQho1wx4JwY8wC"
        crossorigin="anonymous"></script>
```

### Security headers

```
X-Frame-Options: DENY
X-Content-Type-Options: nosniff
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

**Do NOT send `X-XSS-Protection`** — deprecated and removed from Chrome/Edge. Use CSP + Trusted Types instead.

### Input sanitization

```javascript
// ❌ XSS vulnerable
element.innerHTML = userInput;

// ✅ Safe
element.textContent = userInput;

// ✅ If HTML needed
import DOMPurify from 'dompurify';
element.innerHTML = DOMPurify.sanitize(userInput);
```

### Secure cookies

```
Set-Cookie: session=abc123; Secure; HttpOnly; SameSite=Strict; Path=/
```

---

## Browser compatibility

### Required head elements

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">  <!-- Must be first -->
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Page title</title>
</head>
```

### Feature detection

```javascript
// ❌ Browser detection (brittle)
if (navigator.userAgent.includes('Chrome')) { }

// ✅ Feature detection
if ('IntersectionObserver' in window) { }
```

### Passive event listeners

```javascript
// ❌ Non-passive (may block scrolling)
element.addEventListener('touchstart', handler);
element.addEventListener('wheel', handler);

// ✅ Passive
element.addEventListener('touchstart', handler, { passive: true });
element.addEventListener('wheel', handler, { passive: true });
```

### Deprecated APIs to avoid

```javascript
// ❌ document.write (blocks parsing)
document.write('<script>');

// ✅ Dynamic loading
const script = document.createElement('script');
script.src = '...';
document.head.appendChild(script);

// ❌ Synchronous XHR
xhr.open('GET', url, false);

// ✅ Async fetch
const response = await fetch(url);
```

---

## Console & errors

### Global error handler

```javascript
window.addEventListener('error', (event) => {
  errorTracker.captureException(event.error);
});

window.addEventListener('unhandledrejection', (event) => {
  errorTracker.captureException(event.reason);
});
```

### Error boundaries (React)

```jsx
class ErrorBoundary extends React.Component {
  state = { hasError: false };
  static getDerivedStateFromError() { return { hasError: true }; }
  componentDidCatch(error, info) { errorTracker.captureException(error, { extra: info }); }
  render() {
    if (this.state.hasError) return <FallbackUI />;
    return this.props.children;
  }
}
```

---

## Source maps

```javascript
// ❌ Source maps exposed in production
devtool: 'source-map',

// ✅ Hidden source maps (upload to error tracker, not served publicly)
devtool: 'hidden-source-map',
```

**Strip `sourcesContent`** from production maps — it embeds unminified source inside the `.map` file.

---

## Code quality

### Semantic HTML

```html
<!-- ❌ Non-semantic -->
<div class="header"><div class="nav"><div class="nav-item">Home</div></div></div>

<!-- ✅ Semantic -->
<header><nav><a href="/">Home</a></nav></header>
<main><article><h1>Headline</h1></article></main>
```

### Image aspect ratios

```html
<!-- ✅ Preserve aspect ratio -->
<img src="photo.jpg" width="800" height="600">

<!-- ✅ CSS object-fit for flexibility -->
<img src="photo.jpg" style="width: 300px; height: 200px; object-fit: cover;">
```

---

## Audit checklist

### Security (critical)
- [ ] HTTPS enabled, no mixed content
- [ ] No vulnerable dependencies (`npm audit`)
- [ ] CSP headers configured
- [ ] `require-trusted-types-for 'script'` enforced
- [ ] Third-party scripts pinned with SRI hashes
- [ ] Security headers present (HSTS, X-Content-Type-Options, Referrer-Policy)
- [ ] Hidden source maps, `sourcesContent` stripped

### Compatibility
- [ ] Valid HTML5 doctype
- [ ] Charset declared first in head
- [ ] Viewport meta tag present
- [ ] No deprecated APIs
- [ ] Passive event listeners for scroll/touch

### Code quality
- [ ] No console errors
- [ ] Valid HTML (no duplicate IDs)
- [ ] Semantic HTML elements
- [ ] Proper error handling
- [ ] Memory cleanup in components

## References
- [MDN Web Security](https://developer.mozilla.org/en-US/docs/Web/Security)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
