# VOID Website Content Guide

This file is an internal writing guide for future website updates. It is not linked from the public site.

The goal is to keep VOID's website clear, technical, and human as the Engine and Language continue to grow. New milestone work should improve the story instead of turning existing paragraphs into longer feature inventories.

## Core writing rule

**Explain the idea before naming the APIs.**

A visitor should understand what a system does, why it exists, or what it changes for them before reading a list of implementation details.

Bad pattern:

> Threads, monitors, lock, timed waits, reset events, semaphores, cancellation, atomics, volatile access, GC coordination, and exceptions...

Better pattern:

> Threading is part of the managed runtime rather than a detached native layer. Blocking, cancellation, GC coordination, and atomic access share the same runtime rules.

Specific APIs can follow as evidence when the page is meant to document the feature surface.

## One card, one thought

A card should communicate one idea. If a paragraph has to explain three unrelated systems, split it into more cards.

Good card subjects:

- Familiar game code
- Value semantics
- Structured lifetime and failure
- Threading as a runtime feature
- Native compiler pipeline
- Compiler-owned tooling

Avoid using one card as storage for every completed feature.

## Landing pages vs reference pages

Landing and overview pages should explain what VOID is and why the architecture matters.

Reference and feature pages may be more specific because the visitor is actively looking for supported functionality.

Use this split:

- **Homepage:** identity, philosophy, product choice, clear reasons to explore.
- **Engine / Language overview:** what using the project feels like and what makes it distinct.
- **Core / Features:** supported capabilities, grouped by meaningful concepts.
- **Reference pages:** precise behavior, APIs, implementation model, and examples.
- **Roadmap:** what is complete, what comes next, and why that ordering makes architectural sense.

Lists are useful in reference material. They should not dominate landing-page copy.

## Do not append milestones to old sentences

When a milestone adds a feature, do not automatically add another comma item to an existing paragraph.

Instead:

1. Read the whole paragraph.
2. Ask what idea the paragraph is trying to communicate.
3. Rewrite the paragraph around the new state of the system.
4. Add the new API name only where it helps prove or clarify that idea.
5. Split the paragraph or card if it now has more than one subject.

The website should describe the current architecture, not the history of how features arrived.

## Explain architecture, not just capability

The strongest VOID copy explains relationships between systems.

Examples:

- The pluggable renderer matters because OpenGL is a backend, not the engine's public rendering identity.
- LSP tooling matters because the editor asks the same compiler semantic model used by real builds.
- Managed threading matters because workers participate in GC, exceptions, cleanup, and synchronization instead of living beside the runtime.
- Deterministic disposal matters because `using`, `finally`, iterator cleanup, and structured exits share one cleanup model.
- Generic operators and conversions matter because legality is proven before specialization instead of being rediscovered during code generation.

That is more useful than listing every class or syntax form involved.

## Headings and kickers

Keep headings short enough to be read as ideas, not second paragraphs.

The small kicker should identify the section or context. It should not repeat the heading in different words.

Prefer:

> `Current direction`  
> **The runtime is ready for a scheduler.**

Avoid:

> `Timed synchronization and cooperative cancellation`  
> **Timed synchronization and cooperative cancellation are complete.**

A heading does not need to contain every SEO keyword on the page.

## SEO rules

SEO should make each page easy to understand, not turn every description into a project-wide keyword list.

For each page:

1. Give the page one primary search intent.
2. Use a unique, descriptive `<title>`.
3. Keep the meta description concise and readable, usually around 140 to 170 characters when practical.
4. Put specific terminology on the page where it naturally belongs.
5. Do not repeat every language or engine feature in metadata.
6. Keep Open Graph and Twitter title/description aligned with the page metadata.
7. Update the sitemap `lastmod` when public page content meaningfully changes.

Examples of page intent:

- Homepage: VOID ecosystem, .NET 2D engine, native game-development language.
- Engine: extensible .NET 2D engine/framework and pluggable rendering.
- Language: C#-inspired game-development language with its own compiler/runtime.
- Compiler: native C output and shared compiler semantics.
- Threading: managed threading, synchronization, cancellation, and GC coordination.
- Tooling: compiler-backed diagnostics, LSP, completion, navigation, and symbols.
- Native: direct C interoperability and unsafe/native boundaries.
- Roadmap: current development direction and architectural ordering.

## Milestone update workflow

When a new milestone or block completes:

1. Start from the current live `/docs` tree.
2. Identify which public pages genuinely changed.
3. Keep planned functionality on the Roadmap until it is implemented.
4. Once implemented, integrate it into the relevant page's existing story.
5. Update Examples only when the public syntax/API is confirmed and an example teaches something useful.
6. Prefer rewriting an old paragraph over extending a feature list.
7. Check whether a crowded card should become two cards.
8. Keep roadmap explanations focused on why the next layer is needed.
9. Recheck page title, meta description, Open Graph, Twitter metadata, and sitemap dates.
10. Validate internal links, assets, HTML, and example selectors before packaging `/docs`.

## Good existing patterns to preserve

### Roadmap transition sections

The strongest roadmap sections explain why the next phase follows from the completed one. Keep that pattern. Do not reduce roadmap transitions to a list of future APIs.

### Compiler / LSP explanation

The tooling pages are strongest when they explain that `voidc` owns the semantic model and the LSP is a protocol layer over it. Keep explaining what that architecture means for editor correctness instead of only listing supported LSP methods.

### Engine renderer explanation

The renderer pages work well when they explain the boundary: OpenGL is included, but higher-level engine systems are renderer-neutral. Preserve that architecture-first voice.

## Before / after examples

### Overloaded language card

Before:

> Classes, structs, inheritance, interfaces, delegates, events, iterators, patterns, threads, monitors, semaphores, cancellation, atomics...

After:

> VOID gives everyday game systems a familiar shape: classes, interfaces, generics, delegates, and events behave as normal language features instead of framework-specific patterns.

Then give threading, lifetime, values, compiler output, and tooling their own cards.

### Threading

Before:

> Thread, Monitor, lock, Thread.Sleep, timed Monitor.Wait, reset events, semaphores, CancellationToken, Interlocked, Volatile...

After:

> Threading is part of the managed runtime rather than a detached native layer. Blocking, cancellation, GC coordination, and atomic access share the same runtime rules.

The reference section can then name the actual APIs.

### Tooling

Before:

> Diagnostics, hover, signature help, completion, definitions, references, symbols...

After:

> The compiler answers the editor's questions too, so editor intelligence stays aligned with real builds instead of relying on a second language implementation.

## Voice

- Technical, clear, and confident.
- Explain before selling.
- Avoid hype that does not say anything concrete.
- Avoid generic filler such as "powerful", "next-generation", or "revolutionary" unless a specific fact immediately supports it.
- Avoid giant comma chains when normal sentences can explain the relationship.
- Avoid em dashes in site copy.
- Do not compare VOID to competitors unless the page is specifically about that comparison.
- Do not overstate planned work as current capability.
- Keep Engine and Language independent in the copy. They can complement each other without implying one requires the other.

## Visual language and page-continuation cues

Keep the icon system intentional. Reuse the existing circular VOID icon style and prefer a small reusable vocabulary of purpose-built symbols over placeholder letters or generic glyphs. If a card is about native output, interop, tooling, diagnostics, source mapping, or another recurring concept, use the matching branded icon rather than a bare `C`, `L`, `#`, question mark, or diamond. Numbered badges are fine when the number is the actual organizing device.

When a page or section can fill a viewport so neatly that it looks finished, preserve the layout and use the global continuation cue rather than adding filler copy or forcing the next section into view. The cue should remain subtle, fixed outside document flow, and disappear near the real bottom of the page.

Future icon additions should match the established system: circular purple treatment, simple readable line art, transparent background, and enough contrast to stay legible at the small feature-card size. Reuse an existing icon when the concept is genuinely the same instead of creating near-duplicates.

## Final check

Before shipping a content update, read each changed page once as a new visitor and ask:

- Do I understand the idea before I see the implementation details?
- Is any paragraph trying to be a feature database?
- Is any card doing more than one job?
- Does the heading add meaning, or repeat the kicker?
- Does the metadata describe this page rather than all of VOID?
- Did a new milestone get integrated into the story instead of appended to a list?

If the answer to those is good, the site is probably still speaking in the intended VOID voice.


## Icon implementation

### Canonical website icon assets

Feature-card icons are stored as individual transparent PNG files in `assets/images`. Do not use SVG placeholders or alternate `-gen1` / `-house` variants on the site. When an icon is approved, replace the canonical PNG itself and reference that file directly. The current homepage set is `icon-native-output.png`, `icon-simplicity.png`, `icon-native-interop.png`, and `icon-tooling.png`.

- when icon families need to match the established site artwork, prefer house-style raster PNG assets over mismatched generic SVG placeholders

### Replacing raster icon artwork

When replacing an existing icon with substantially different generated artwork, use a new asset filename (for example `icon-tooling-gen2.png`) and update the HTML reference. Do not overwrite an old filename and assume local browsers will invalidate it; the static site may keep the previous image cached during review.

## VOID icon generation

The established icon family is a strict visual system. Use `assets/images/feature-icons.png` as the primary reference and `assets/images/feature-icons-extra.png` as the extended reference set. Do not improvise a new badge style.

### Visual rules

- Transparent PNG canvas.
- Transparent center/interior except for the ring and glyph.
- No filled dark disc behind the icon.
- Thin segmented circular frame matching the existing icons.
- Crisp violet/lavender line art with a pale-lavender core.
- Glow stays tight to the strokes. Almost no outer halo.
- Never add purple fog, a large bloom, a soft cloudy background, or a thick luminous coin edge.
- The outer glow should disappear into transparency within roughly 2–5% of the icon diameter.
- Keep the ring proportions, segment positions, line thickness, brightness, and overall scale consistent with the reference spritesheets.
- The center glyph should be simple geometric line art that remains readable at small card-icon sizes.
- Reuse an existing icon when its meaning already fits. Create a new one only when the page introduces a genuinely different concept.

### Base generation prompt

Use the existing VOID icon spritesheet as a strict image reference, not loose inspiration. Create a single square transparent PNG icon from the same visual family. Keep the circular frame nearly identical to the reference and change only the center glyph. Use a transparent center, thin segmented ring, crisp violet/lavender strokes, pale-lavender stroke cores, and an extremely tight purple glow. Do not add a solid inner disc, fog, haze, a large bloom, or a wide outer halo. Outside the ring should return to full transparency almost immediately. The result must look like another icon from the same original VOID set, not a separate neon badge style.

Then append only the requested center-glyph description.

### Review check before publishing

Compare the new icon directly beside `icon-engine.png` and `icon-language.png` at the actual website card size. If the outer glow is visibly wider, the center is filled, or the ring looks like a different badge family, reject it before wiring it into the site.



### Approved growth/control icon concepts

For the homepage card **"Keep control as you grow"**, keep these approved PNG concepts stored in `assets/images`:

- `icon-control.png` — active icon in use (modular swap / replaceable piece)
- `icon-growth-merge-flow.png` — alternate concept (merge flow / guided growth)
- `icon-growth-network-control.png` — alternate concept (structured node network / controlled architecture)

Use the active `icon-control.png` on the homepage unless a later visual review replaces it.


### Language overview icon mapping

Current approved Language overview icons:

- `icon-nullability-new.png` — Nullability & initialization
- `icon-source-checking-new.png` — Source-only checking
- `icon-semantic-queries-new.png` — Semantic queries
- `icon-precise-diagnostics-new.png` — Precise diagnostics
- `icon-debug-source-mapping-new.png` — Debug source mapping
- `icon-compiler-lsp-new.png` — Compiler-backed LSP
- `icon-native-interop.png` — Native C integration

Avoid reusing the same icon for distinct compiler/tooling concepts when the cards are adjacent in the same grid.

Reference sheet: `assets/images/language-overview-icons.png`. Keep these approved assets and reuse them for future Language overview/tooling cards when the meaning fits.

## Why VOID page

`why-void/index.html` is a top-level explanation of why VOID Engine and VOID Language exist. Keep it in the main navigation between Language and Articles.

The page is intentionally FAQ-shaped and uses native `<details>` / `<summary>` elements so the questions are scannable without becoming a wall of text. Keep answers collapsed by default.

When updating this page:

- Explain the design reason before listing implementation details.
- Do not frame C#, .NET, MonoGame, SFML, NativeAOT, Blazor, or other tools as bad. Acknowledge what they solve, then explain why VOID chooses different tradeoffs.
- Do not claim that native code is automatically faster than .NET. Explain which runtime layers or behaviors VOID removes or owns, and leave performance claims to measurements.
- Say that VOID does not depend on the CLR or an external managed VM. Do not say VOID has no runtime at all; GC, exceptions, threading, synchronization, reflection metadata, and related features require VOID-owned runtime support.
- C does not provide reflection. Describe VOID reflection as compiler-generated native metadata designed around AOT from the start.
- Web support is a whole-stack target. Do not claim that C# cannot run in browsers. Explain that browser deployment requires the application and its platform/native dependencies to support the browser environment.
- Keep Microsoft documentation links beside claims about .NET behavior that may change over time.
- Avoid em dashes in page copy.
