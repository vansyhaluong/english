# EnglishHub UI Style Guide

This is the **UI and visual design guide** for EnglishHub. It defines visual direction, semantic tokens, component behavior, interaction rules, and UX conventions for the product. It is not an architecture document.

The design language is inspired by the **dark-blue, immersive, translucent visual character of Brottin**, especially its use of luminous borders, light glass surfaces, and brighter hover states over a dark background. EnglishHub must use those ideas as a visual reference, not as a pixel-for-pixel copy.

---

## Overview

EnglishHub is a modern English-learning product for learners who need a calm, focused, and visually engaging place to study. The interface should feel **immersive, premium, clear, and lightweight**.

The background carries most of the dark visual identity. Because of that, foreground UI must **not become another layer of heavy dark rectangles**. Panels, controls, cards, menus, and hover states should generally use **light translucent surfaces, brighter borders, and soft blue-white highlights** so they remain clearly separated from the background.

When in doubt:

- Reach for **light translucent surfaces** before opaque dark surfaces.
- Reach for **semantic tokens** before hardcoded colors.
- Reach for **existing components** before creating one-off CSS.
- Reach for **hierarchy, spacing, and typography** before adding decoration.
- Use glass effects to create depth, not as decoration on every element.
- Prefer a quiet premium result over a visually busy one.

---

## Product and audience

- **Product:** EnglishHub
- **Category:** English-learning platform
- **Primary audience:** students, self-directed learners, and learners building daily study habits
- **Primary surfaces:** landing page, learning dashboard, lessons, vocabulary, grammar, listening, speaking, writing, review, profile, and settings
- **Primary visual reference:** Brottin (`https://brottin.quest/vi`)
- **Primary UX goal:** help users begin or continue learning with minimal friction

The product should feel approachable without becoming childish. It should feel polished without looking like a generic SaaS dashboard.

---

## Visual direction

### Core adjectives

EnglishHub should feel:

- **Immersive**
- **Focused**
- **Premium**
- **Calm**
- **Modern**
- **Lightweight**

### Visual hierarchy

The visual priority should generally be:

1. Main learning goal or primary content
2. Current action / primary CTA
3. Progress and context
4. Supporting navigation
5. Secondary metadata

Do not make every section visually equal. A screen should have one obvious primary focus.

### Background philosophy

The background owns the dark-blue atmosphere.

- Landing pages may use a full-viewport image, video, or atmospheric gradient.
- Product screens should use a deep navy base with subtle depth, not pure black everywhere.
- Background visuals must remain behind the content hierarchy.
- Use overlays when necessary to protect text contrast.
- Do not place large opaque black cards over an already dark background unless the content requires strong isolation.

### Surface philosophy

Foreground surfaces should feel slightly brighter than the environment.

Use:

- translucent white / blue-white fills
- thin light borders
- soft backdrop blur where useful
- subtle inner highlights
- restrained blue accents

Avoid:

- stacked dark gray cards
- large opaque navy blocks inside a navy page
- heavy shadows around every component
- excessive glassmorphism
- bright white panels that break the atmosphere

---

## Source of truth

The project must have **one canonical location** for design tokens.

If the codebase already has a global theme stylesheet or token file, extend it. Do not create a second competing token system.

| Concern | Rule |
| --- | --- |
| Semantic color tokens | Must live in the canonical global theme/token file |
| Typography | Must be defined globally and consumed through tokens/utilities |
| Spacing | Must use the shared spacing scale |
| Radius | Must use shared radius tokens |
| Component primitives | Must be reused before writing custom replacements |
| Motion | Must use shared duration/easing tokens |
| Dark visual treatment | Must follow this guide rather than component-local color guesses |

Never hardcode a color in component code when an existing semantic token already describes the role.

If a new token is needed, add the token once and use it everywhere.

---

## Design tokens

Token names below describe the required roles. Exact implementation syntax may differ by framework, but the semantic meaning must remain stable.

### Color foundation

```css
:root {
  /* Background */
  --background: #07111f;
  --background-deep: #030a13;
  --background-elevated: #0b1728;

  /* Text */
  --foreground: #f4f7fb;
  --foreground-secondary: rgba(238, 244, 255, 0.78);
  --foreground-muted: rgba(231, 240, 255, 0.58);
  --foreground-disabled: rgba(231, 240, 255, 0.38);

  /* Light translucent surfaces */
  --surface: rgba(255, 255, 255, 0.07);
  --surface-hover: rgba(255, 255, 255, 0.12);
  --surface-active: rgba(255, 255, 255, 0.17);
  --surface-strong: rgba(255, 255, 255, 0.20);
  --surface-disabled: rgba(255, 255, 255, 0.04);

  /* Borders */
  --border: rgba(255, 255, 255, 0.16);
  --border-muted: rgba(255, 255, 255, 0.10);
  --border-hover: rgba(255, 255, 255, 0.28);
  --border-strong: rgba(255, 255, 255, 0.38);

  /* Brand / interaction */
  --primary: #7db7ff;
  --primary-hover: #9ac8ff;
  --primary-active: #66a8fb;
  --primary-foreground: #06101d;
  --ring: rgba(149, 197, 255, 0.92);

  /* State */
  --success: #72d6ad;
  --warning: #f4c46a;
  --destructive: #ff7d8a;
  --info: #83c7ff;
}
```

These values establish the initial system. If visual calibration against the live reference changes them, preserve the **semantic roles and relative contrast hierarchy**.

### Color roles

| Role | Use it for | Do not use it for |
| --- | --- | --- |
| `background` | Main app canvas | Cards or hover states |
| `background-deep` | Deep visual zones, media overlays | General component surfaces |
| `surface` | Cards, nav containers, lightweight panels | Full-page background |
| `surface-hover` | Hovered rows, ghost buttons, selectable cards | Persistent decoration |
| `surface-active` | Pressed/selected states | Default card fill |
| `border` | Default frames and dividers | Heavy emphasis |
| `border-hover` | Hovered/focused interactive frames | Static separators |
| `primary` | Primary CTA, selected learning action, meaningful progress accent | Decorative color everywhere |
| `ring` | Focus-visible and strong keyboard selection | Persistent borders |
| `foreground` | Primary text | Disabled/meta text |
| `foreground-secondary` | Secondary descriptions | Primary headings |
| `foreground-muted` | Metadata, helper copy, placeholders | Important body content |
| `destructive` | Errors and irreversible actions | Cancel/Back |

### Surface mixing

When a component needs a stronger or weaker interaction surface, derive it from existing tokens instead of inventing another hex value.

```css
background: color-mix(in srgb, var(--foreground) 10%, transparent);
```

Use the same principle for subtle selected, hover, or highlighted states.

---

## Typography

### Font family

Use **Mulish** as the primary UI family.

```css
--font-family-primary: "Mulish", "Mulish Fallback", system-ui, -apple-system, sans-serif;
```

Use a monospace family only for code, literal keyboard input, technical identifiers, or developer-facing content.

### Type scale

| Token | Size | Typical use |
| --- | ---: | --- |
| `text-xs` | 12px | Metadata, compact labels |
| `text-sm` | 14px | Secondary copy, dense controls |
| `text-base` | 16px | Default body text |
| `text-lg` | 18px | Prominent body / section intro |
| `text-xl` | 20px | Card / panel title |
| `text-2xl` | 24px | Section title |
| `text-3xl` | 32px | Page heading |
| `text-4xl` | 48px | Hero heading on compact screens |
| `text-5xl` | 64px | Large hero heading when space allows |

Default body line-height should be approximately **1.6**.

Headings should use tighter line-height and stronger weight than body copy, but should not become oversized simply to create visual impact.

### Typography rules

- Body text must remain readable against visual backgrounds.
- Important learning content must never use muted text colors.
- Avoid more than three visually competing type sizes in one local section.
- Buttons must use concise labels and consistent weight.
- Do not use uppercase for long labels or sentences.
- Long headings should wrap intentionally rather than shrink to unreadable sizes.

---

## Spacing

Use a shared spacing scale.

```text
space-1 = 4px
space-2 = 6px
space-3 = 8px
space-4 = 10px
space-5 = 12px
space-6 = 16px
space-7 = 20px
space-8 = 24px
space-9 = 32px
space-10 = 40px
space-11 = 48px
space-12 = 64px
```

### Spacing rules

- Use tighter spacing inside controls than between sections.
- Cards in the same group must share the same internal padding.
- Vertical rhythm must remain predictable.
- Do not introduce arbitrary values such as `17px`, `23px`, or `29px` unless there is a documented implementation constraint.
- Dense learning screens may use compact spacing, but grouping must remain obvious.

---

## Radius

Use moderate rounding. EnglishHub should feel soft but not bubbly.

```text
radius-xs   = 8px
radius-sm   = 12px
radius-md   = 16px
radius-lg   = 20px
radius-xl   = 24px
radius-full = 9999px
```

Rules:

- Inputs and standard buttons should normally use `radius-sm` or `radius-md`.
- Cards should normally use `radius-md` or `radius-lg`.
- Pills, compact status filters, and avatar-like controls may use `radius-full`.
- Do not make every rectangular element a pill.
- Nested containers should not all use large radii; reduce radius as hierarchy moves inward.

---

## Elevation, blur, and shadows

EnglishHub should use **borders + translucent surfaces** before heavy shadows.

Use three practical levels:

1. **Flat:** translucent surface + subtle border
2. **Raised:** slightly stronger surface + border + minimal shadow
3. **Floating:** popover/dialog/menu with stronger blur and a restrained deep shadow

Suggested floating treatment:

```css
box-shadow: 0 16px 48px rgba(0, 0, 0, 0.30);
backdrop-filter: blur(20px);
```

Do not add extra elevation levels without a specific interaction reason.

Backdrop blur should help separate content from a visual background. It should not be applied to every surface.

---

## Motion

```text
motion-instant = 150ms
motion-fast    = 220ms
motion-normal  = 300ms
motion-slow    = 500ms
```

Use smooth, restrained easing.

### Motion rules

- Hover feedback should be subtle and fast.
- Buttons may shift surface, border, opacity, or scale very slightly.
- Avoid bouncing controls.
- Avoid dramatic card lifts.
- Large background movement should be slow and atmospheric.
- Expanding/collapsing content should animate only when it improves continuity.
- `prefers-reduced-motion` must be respected.

---

## Components

Use the project's existing primitive/component library before creating new controls.

If two nearby components solve the same problem, they should share anatomy, spacing, icon sizing, states, and interaction behavior.

Every interactive component must define:

- default
- hover
- focus-visible
- active / pressed
- disabled
- loading where applicable
- error where applicable

### Buttons

#### Primary

Use for the single main action in a local flow.

Default:

- `background: var(--primary)`
- dark readable foreground
- subtle bright border/highlight if necessary

Hover:

- move toward `primary-hover`
- do not add a heavy shadow

Focus-visible:

- visible `ring`
- must remain visible even over complex background imagery

Disabled:

- lower contrast
- no hover response
- cursor and semantics must reflect disabled state

#### Secondary

Use a light translucent surface:

```text
surface + border
→ hover: surface-hover + border-hover
→ active: surface-active
```

Secondary controls should generally be **lighter than the dark page behind them**, not darker.

#### Ghost

Use for toolbar, nav, icon, or low-emphasis actions.

- Default background: transparent
- Hover: `surface-hover`
- Focus-visible: ring
- Active: `surface-active`

#### Destructive

Use only for actions that can remove data or have irreversible consequences.

Cancel, Back, Close, and Dismiss must not use destructive styling.

---

### Cards and panels

Cards should organize meaningful repeated content, learning modules, or clear groups. Do not place every section in a card.

Default card recipe:

```text
background: surface
border: border
radius: radius-lg
optional backdrop blur
```

Hoverable card:

```text
background: surface-hover
border: border-hover
```

Selected card:

```text
surface-active
+ visible primary/ring treatment
```

Rules:

- Avoid cards nested inside cards.
- Avoid opaque dark cards over the dark page.
- Use unframed sections when a boundary is not needed.
- A clickable card must have an obvious hover and focus-visible state.
- A card must not rely only on scale animation to communicate interactivity.

---

### Navigation

Navigation should remain visually quiet so learning content dominates.

- Prefer translucent/floating navigation over large opaque dark bars.
- Current item must be distinguishable through surface + text + optional accent, not color alone.
- Hover state should use the same light translucent language as Brottin-inspired controls.
- Icon-only navigation must have accessible names and tooltips where needed.
- Mobile navigation must preserve the same hierarchy rather than simply shrinking desktop navigation.

---

### Inputs and form controls

Default:

- slightly brighter translucent fill than the page
- visible but quiet border
- high-contrast text
- muted placeholder

Hover:

- `border-hover`

Focus-visible:

- clear `ring`
- border may strengthen

Error:

- error border/ring and explicit message
- do not communicate error by color alone

Disabled:

- reduce contrast without making the value unreadable if the value must still be reviewed

Labels must remain visible. Do not use placeholder text as the only label.

---

### Tabs and segmented controls

- Inactive items should stay low-emphasis.
- Hover should use `surface-hover`.
- Active items should use `surface-active` plus stronger text.
- The active state must remain understandable without relying only on blue.
- Horizontal overflow must scroll or collapse gracefully on narrow screens.

---

### Lists and selectable rows

For lesson lists, vocabulary lists, histories, settings, and search results:

- Idle: transparent or minimal surface
- Hover: `surface-hover`
- Keyboard-selected: `surface-hover` + visible inset/ring treatment
- Current/selected: `surface-active` + semantic selection cue
- Disabled: reduced contrast, no false hover affordance

Long text must truncate or wrap according to content importance. Never let secondary metadata destroy the main label width.

---

### Tooltips

Use tooltips to name icon-only or ambiguous compact controls.

Do not use tooltips for:

- critical errors
- required instructions
- interactive content
- information users must keep visible while completing a task

Tooltips must be reachable by keyboard focus as well as pointer hover.

---

### Icons

Use one icon family consistently across the product.

Recommended size hierarchy:

- 14px: metadata / very dense UI
- 16px: standard controls
- 18–20px: prominent actions
- 28px+: featured learning/empty-state illustration only

Icons should inherit surrounding semantic text color whenever possible.

Do not mix multiple icon styles unless there is a strong product reason.

---

## Learning-specific components

### Learning module cards

Used for Vocabulary, Grammar, Listening, Speaking, Writing, Reading, Review, etc.

Each card should show:

1. Clear module name
2. One useful status/progress signal at most
3. Optional concise supporting description
4. One obvious interaction target

Do not overload module cards with badges, percentages, streaks, buttons, and metadata simultaneously.

### Progress

Progress indicators must communicate actual stored progress.

- Use primary blue for meaningful progress.
- Use muted tracks.
- Always expose a textual or accessible value.
- Never imply completion or streak state without real data.

### Vocabulary / flashcards

Flashcards may use a stronger light translucent surface than normal cards because they are the primary content object.

- Front/back states must be obvious.
- Flip animation must respect reduced motion.
- The card must remain keyboard operable.
- Long definitions and examples must scroll or reflow rather than overflow.

### Audio and speaking controls

- Play/record states must be visually and semantically distinct.
- Recording must show a persistent active state.
- Microphone permission errors must appear inline with a recovery action.
- Do not rely on animation alone to communicate recording.

---

## Landing page rules

The landing page should be visually stronger than app/product screens.

### Hero

The hero should occupy most of the first viewport when appropriate.

Prefer:

- immersive full-bleed background visual/video
- minimal copy
- one primary CTA
- one secondary action at most
- translucent floating controls
- strong foreground/background contrast

Avoid:

- generic centered SaaS hero + three cards underneath
- excessive marketing badges
- giant text simply for impact
- several competing CTAs
- opaque dark panels hiding the background visual

### Section composition

- Alternate between framed and unframed sections.
- Use cards only for repeated items or content that benefits from containment.
- Let visual assets carry atmosphere.
- Preserve negative space.
- Use asymmetric layouts where they improve visual rhythm.

---

## Dashboard and learning workspace rules

Product screens should be calmer and more functional than the landing page.

- The current learning task must dominate.
- Navigation and secondary metrics must recede.
- Do not turn every metric into a separate card.
- Use grouping and alignment before adding containers.
- Keep high-frequency actions visible.
- Move low-frequency actions into menus or secondary surfaces.

---

## Interaction rules

### Pointer

- Every clickable surface must provide hover feedback.
- Hover feedback should normally brighten the surface/border rather than darken it.
- Cursor behavior must match interaction semantics.

### Keyboard

- All interactive controls must be keyboard reachable.
- Focus-visible must never be removed.
- Enter/Space behavior must match native expectations.
- Esc should close dismissible overlays without side effects.
- Focus must return to the invoking control after closing modal/popover interactions where appropriate.

### Touch

- Primary touch targets should be at least 44×44 CSS px where practical.
- Hover-only information must have an alternative on touch.
- Closely grouped controls must maintain enough separation to prevent accidental activation.

---

## Loading and perceived duration

Match feedback to perceived duration:

| Duration | Feedback |
| --- | --- |
| 0–100 ms | No visible loading state |
| 100 ms–1 s | Disable action where needed |
| 1–3 s | Spinner or concise label change |
| 3 s+ / multi-step | Named progress stage or persistent status |

Disable repeat submission immediately when duplicate actions would be harmful.

Avoid layout shift when labels change to loading states.

---

## Accessibility

Target **WCAG 2.2 AA**.

### Required acceptance criteria

- Body text must meet minimum contrast requirements against its actual rendered background.
- Interactive controls must have a visible focus indicator.
- Focus indication must remain visible over background images/video.
- All icon-only buttons must have accessible names.
- Form errors must be programmatically associated with the relevant field.
- Error/success states must not rely on color alone.
- Interactive targets must remain keyboard operable.
- Dialog focus must be managed correctly.
- Reduced motion must be honored.
- Text zoom/reflow must not cause horizontal page scrolling at normal responsive breakpoints except for intentionally scrollable content.
- Video backgrounds must not reduce readability; provide overlays and reduced-motion/static alternatives where needed.

---

## Responsive behavior

### Mobile

- Preserve hierarchy, not desktop geometry.
- Stack content when side-by-side layouts become difficult to scan.
- Keep primary actions reachable.
- Reduce decorative background complexity before reducing text legibility.
- Cards should usually become full-width or near-full-width.
- Large hero headings must scale down fluidly.

### Tablet

- Prefer two-column layouts only when both columns remain readable.
- Avoid squeezing dense desktop sidebars beside primary learning content.

### Desktop

- Use width to create breathing room, not to add unnecessary panels.
- Constrain long reading content to comfortable line lengths.
- Use larger backgrounds and spatial composition without stretching text blocks excessively.

---

## Content and tone

EnglishHub UI copy should be:

- concise
- friendly
- confident
- specific
- learner-focused

Prefer:

- `Tiếp tục học`
- `Bắt đầu bài luyện`
- `Nghe lại`
- `Kiểm tra đáp án`

Avoid vague labels such as:

- `Đi`
- `Tiếp`
- `Xử lý`
- `Thực hiện`

UI copy must not claim an action succeeded until the system has real result state confirming it.

---

## Screen UX review rubric

When reviewing a page, screenshot, or prototype, use this order:

1. **Top fixes:** identify the three highest-impact changes.
2. **Hierarchy:** verify that the primary learning goal/action is obvious.
3. **Surface balance:** check that translucent foreground surfaces remain distinguishable from the dark background without becoming heavy dark blocks.
4. **Friction:** identify unnecessary clicks, controls, labels, or containers.
5. **Alignment and rhythm:** check grid, spacing, baselines, and visual grouping.
6. **Interaction:** verify hover, focus-visible, active, loading, disabled, and error states.
7. **Keyboard and touch:** verify practical interaction paths.
8. **Responsive behavior:** check wrapping, overflow, and content priority.
9. **Content:** verify concise labels and truthful state language.
10. **Accessibility:** verify contrast, focus, semantics, and reduced motion.

---

## Anti-patterns

Do not introduce the following without a documented reason:

- generic purple/blue AI gradients
- repeated three-card SaaS feature layouts
- dark cards on dark cards on dark backgrounds
- glassmorphism on every element
- excessive rounded rectangles
- huge headings with weak information hierarchy
- multiple bright accent colors competing on one screen
- hidden focus indicators
- low-contrast gray text
- arbitrary spacing values
- one-off component styles when a shared primitive exists
- nested cards without a structural reason
- hover states that become darker and disappear into the background
- decorative borders brighter than actual interactive focus states
- important information hidden only in tooltips
- animation used only to make the UI feel "alive"

---

## Migration rules

When updating existing EnglishHub UI:

1. Preserve functionality first.
2. Replace raw colors with semantic tokens.
3. Normalize spacing and radius.
4. Replace heavy opaque dark surfaces with the documented translucent hierarchy where appropriate.
5. Normalize hover/focus/active states.
6. Reuse existing primitives.
7. Improve hierarchy before adding decoration.
8. Update one coherent surface at a time rather than partially restyling unrelated components.

Do not mix the old and new visual systems indefinitely within the same flow.

---

## When this guide is silent

If this guide does not answer a UI question:

1. Inspect the closest existing sibling component.
2. Reuse the nearest existing primitive.
3. Prefer an existing semantic token.
4. Prefer the established dark-blue + light-translucent surface language.
5. Check whether the decision improves hierarchy, readability, and learning flow.
6. If two reasonable options remain and the choice changes product direction, ask before inventing a new pattern.

---

## QA checklist

Before considering a UI change complete, verify:

- [ ] The screen follows the dark-blue visual direction.
- [ ] Foreground surfaces are distinguishable from the dark background without becoming overly dark.
- [ ] Hover states brighten or strengthen surfaces/borders consistently.
- [ ] Semantic tokens are used instead of one-off colors.
- [ ] Typography follows the shared family and scale.
- [ ] Spacing follows the shared scale.
- [ ] Radius follows the shared scale.
- [ ] Existing primitives are reused where possible.
- [ ] The primary action is visually obvious.
- [ ] All interactive states are defined.
- [ ] Focus-visible is clearly visible.
- [ ] Keyboard operation works.
- [ ] Touch targets are usable.
- [ ] Long content, overflow, empty, loading, and error states are handled.
- [ ] Responsive behavior has been checked.
- [ ] Contrast meets WCAG 2.2 AA.
- [ ] Reduced-motion behavior works.
- [ ] The UI does not look like a generic SaaS/AI template.
- [ ] The result remains visually consistent with the rest of EnglishHub.

