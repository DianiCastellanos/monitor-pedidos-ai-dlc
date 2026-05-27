---
description: Generate, edit, and manage images using built-in image generation or the OpenAI image API. Use when asked to "generate an image", "create an illustration", "make a mockup", "edit an image", or "create visual assets".
---

# Image Generation

Two execution modes:
- **Built-in `image_gen` tool** — default, no API key needed
- **CLI fallback (`scripts/image_gen.py`)** — requires `OPENAI_API_KEY`, used for batch/transparent workflows

---

## Decision tree

1. **Intent**: generate new image OR edit existing?
2. **Strategy**: single image OR repeated calls OR CLI batch?
3. **Transparency needed?** → use chroma-key workflow
4. **Batch (3+ images)?** → consider CLI fallback

---

## Rules

- Never silently switch to `gpt-image-1.5` without telling the user
- `"batch"` alone doesn't mean CLI fallback — ask if unclear
- Never leave project assets at `$CODEX_HOME/*` — save to project directory
- Version siblings instead of overwriting: `hero-v1.png`, `hero-v2.png`

---

## Save-path policy

```
tmp/imagegen/     ← intermediates (chroma-key, work-in-progress)
output/imagegen/  ← finals (approved, production-ready)
```

---

## Transparent image workflow

1. Generate with green chroma-key background (`#00ff00`)
2. Run `scripts/remove_chroma_key.py` to extract alpha
3. Validate alpha channel is clean
4. Ask before escalating to CLI `gpt-image-1.5`

---

## Shared prompt schema

Use this structure for every image generation request:

```
Use case:        [photorealistic-natural | product-mockup | ui-mockup | ads-marketing | logo-brand | background-extraction | illustration | icon]
Asset type:      [photo | illustration | icon | mockup | background | texture | pattern]
Primary request: [what to create in one sentence]
Scene:           [environment, setting, context]
Subject:         [main focus of the image]
Style:           [realistic | flat | isometric | hand-drawn | minimal | etc.]
Composition:     [centered | rule-of-thirds | full-bleed | portrait | landscape]
Lighting:        [natural | studio | dramatic | soft | golden hour | etc.]
Color:           [palette or dominant colors]
Text:            [text to include, or "none"]
Constraints:     [technical requirements: size, format, transparency]
Avoid:           [what NOT to include]
```

---

## Prompt augmentation rules

**Allowed augmentations** (add without asking):
- Lighting details ("soft natural light from left")
- Composition guidance ("rule of thirds")
- Style consistency ("flat design, 2px stroke weight")
- Technical specs ("16:9 ratio, clean background")

**NOT allowed** (always ask first):
- Changing the subject or concept
- Adding people/faces not requested
- Changing brand colors or logos
- Adding text not requested

---

## gpt-image-2 size constraints

| Constraint | Value |
|-----------|-------|
| Max edge | 3840px |
| Pixel multiple | 16px |
| Max aspect ratio | 3:1 |
| Min pixels | 655,360 (≈ 1024×640) |
| Max pixels | 8,294,400 (≈ 2880×2880) |

**Popular sizes:** 1024×1024, 1024×1792, 1792×1024, 1536×1024, 1024×1536, 2048×2048

**Quality levels:** `low`, `medium`, `high`, `ultra`

---

## CLI fallback setup

```bash
uv pip install openai pillow
export OPENAI_API_KEY="sk-..."
python scripts/image_gen.py --prompt "..." --output output/imagegen/result.png
```

---

## Use-case examples

| Use case | Key considerations |
|----------|-------------------|
| `photorealistic-natural` | Lighting, depth of field, natural textures |
| `product-mockup` | Clean background, accurate proportions, brand colors |
| `ui-mockup` | Platform conventions, realistic content, proper spacing |
| `ads-marketing` | Bold composition, clear hierarchy, CTA space |
| `logo-brand` | Vector-style, scalable, few colors, memorable |
| `background-extraction` | Chroma-key workflow, clean edges |
| `illustration` | Consistent style, cohesive color palette |
| `icon` | 14×14 or 24×24 or 48×48 grid, `aria-hidden` |

---

## Workflow steps

1. Confirm intent (generate / edit / batch)
2. Fill prompt schema — ask user for missing critical fields
3. Apply allowed augmentations silently
4. Generate with built-in tool
5. Preview and get feedback
6. Iterate (version siblings, don't overwrite)
7. Save final to `output/imagegen/`
8. If transparency needed → chroma-key workflow
9. If batch → propose CLI fallback, confirm before switching
