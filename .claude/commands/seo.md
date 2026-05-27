---
description: Optimize for search engines following Google Search guidelines. Use when asked to "improve SEO", "add structured data", "fix meta tags", "improve search ranking", or "make crawlable".
---

# SEO optimization

Search engine optimization based on Lighthouse SEO audits and Google Search guidelines.

## SEO fundamentals

| Factor | Influence | This Skill |
|--------|-----------|------------|
| Content quality & relevance | ~40% | Partial (structure) |
| Backlinks & authority | ~25% | ✗ |
| Technical SEO | ~15% | ✓ |
| Page experience (Core Web Vitals) | ~10% | See `/core-web-vitals` |
| On-page SEO | ~10% | ✓ |

---

## Technical SEO

### robots.txt
```text
User-agent: *
Allow: /
Disallow: /admin/
Disallow: /api/
Sitemap: https://example.com/sitemap.xml
```

### Canonical URLs
```html
<link rel="canonical" href="https://example.com/current-page">
```

### XML sitemap
```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>https://example.com/</loc>
    <lastmod>2024-01-15</lastmod>
    <changefreq>daily</changefreq>
    <priority>1.0</priority>
  </url>
</urlset>
```

### URL guidelines
- Use hyphens, not underscores
- Lowercase only, < 75 characters
- Include target keywords naturally
- HTTPS always

---

## On-page SEO

### Title tags (50–60 chars)
```html
<!-- ❌ Generic -->
<title>Home</title>

<!-- ✅ Descriptive with primary keyword first -->
<title>Blue Widgets for Sale | Premium Quality | Example Store</title>
```

### Meta descriptions (150–160 chars)
```html
<!-- ✅ Compelling with CTA -->
<meta name="description" content="Shop premium blue widgets with free shipping. 30-day returns. Rated 4.9/5 by 10,000+ customers. Order today and save 20%.">
```

### Heading hierarchy
```html
<!-- ✅ Single h1, logical hierarchy -->
<h1>Blue Widgets - Premium Quality</h1>
  <h2>Product Features</h2>
    <h3>Durability</h3>
  <h2>Customer Reviews</h2>
```

### Image SEO
```html
<!-- ✅ Descriptive filename + alt + dimensions -->
<img src="blue-widget-product-photo.webp"
     alt="Blue widget with chrome finish, side view"
     width="800" height="600"
     loading="lazy">
```

### Internal linking
```html
<!-- ❌ Non-descriptive -->
<a href="/products">Click here</a>

<!-- ✅ Descriptive anchor text with keywords -->
<a href="/products/blue-widgets">Browse our blue widget collection</a>
```

---

## Structured data (JSON-LD)

### Organization
```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "Organization",
  "name": "Example Company",
  "url": "https://example.com",
  "logo": "https://example.com/logo.png"
}
</script>
```

### Article
```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "Article",
  "headline": "How to Choose the Right Widget",
  "author": { "@type": "Person", "name": "Jane Smith" },
  "datePublished": "2024-01-15",
  "dateModified": "2024-01-20"
}
</script>
```

### Product
```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "Product",
  "name": "Blue Widget Pro",
  "offers": {
    "@type": "Offer",
    "price": "49.99",
    "priceCurrency": "USD",
    "availability": "https://schema.org/InStock"
  },
  "aggregateRating": {
    "@type": "AggregateRating",
    "ratingValue": "4.8",
    "reviewCount": "1250"
  }
}
</script>
```

### FAQ
```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "FAQPage",
  "mainEntity": [{
    "@type": "Question",
    "name": "What colors are available?",
    "acceptedAnswer": { "@type": "Answer", "text": "Blue, red, and green." }
  }]
}
</script>
```

### Breadcrumbs
```html
<script type="application/ld+json">
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    { "@type": "ListItem", "position": 1, "name": "Home", "item": "https://example.com" },
    { "@type": "ListItem", "position": 2, "name": "Products", "item": "https://example.com/products" }
  ]
}
</script>
```

**Validate at:** [Google Rich Results Test](https://search.google.com/test/rich-results)

---

## AI search visibility (emerging, 2026)

- **Don't block AI crawlers wholesale.** `OAI-SearchBot`, `PerplexityBot`, `ClaudeBot` each have separate `robots.txt` user-agents — decide per-bot.
- **Use schema.org structured data.** AI summarizers parse it more reliably than prose layouts.
- **Make first-paragraph answers self-contained.** Both featured snippets and AI summaries pull short, coherent passages.
- **`llms.txt`** — as of mid-2026 adoption is ~0.015% of sites and no major AI vendor has confirmed they read it. Treat as speculative.

---

## Audit checklist

### Critical
- [ ] HTTPS enabled
- [ ] robots.txt allows crawling
- [ ] No `noindex` on important pages
- [ ] Unique title tags (50–60 chars)
- [ ] Single `<h1>` per page

### High priority
- [ ] Meta descriptions present (150–160 chars)
- [ ] Sitemap submitted to Search Console
- [ ] Canonical URLs set
- [ ] Mobile-responsive (`viewport` meta)
- [ ] Core Web Vitals passing

### Medium priority
- [ ] Structured data (Article/Product/FAQ/Breadcrumbs)
- [ ] Internal linking with descriptive anchor text
- [ ] Image alt text and descriptive filenames
- [ ] URL structure: hyphens, lowercase, short

## Tools

| Tool | Use |
|------|-----|
| Google Search Console | Monitor indexing |
| Google PageSpeed Insights | Performance + CWV |
| Rich Results Test | Validate structured data |
| Lighthouse | Full SEO audit |
| Screaming Frog | Crawl analysis |

## References
- [Google Search Central](https://developers.google.com/search)
- [Schema.org](https://schema.org/)
