# Brottin — Học tiếng Đức online miễn phí từ A1 đến C2

## Mission
Create implementation-ready, token-driven UI guidance for Brottin — Học tiếng Đức online miễn phí từ A1 đến C2 that is optimized for consistency, accessibility, and fast delivery across documentation site.

## Brand
- Product/brand: Brottin — Học tiếng Đức online miễn phí từ A1 đến C2
- URL: https://brottin.quest/
- Audience: developers and technical teams
- Product surface: documentation site

## Style Foundations
- Visual style: structured, tokenized, content-first
- Main font style: `font.family.primary=Mulish`, `font.family.stack=Mulish, Mulish Fallback, system-ui, -apple-system, sans-serif`, `font.size.base=16px`, `font.weight.base=400`, `font.lineHeight.base=25.6px`
- Typography scale: `font.size.xs=12px`, `font.size.sm=14px`, `font.size.md=16px`, `font.size.lg=18px`, `font.size.xl=20px`, `font.size.2xl=24px`, `font.size.3xl=30px`, `font.size.4xl=60px`
- Color palette: `color.text.primary=lab(91.7699 1.74218 -3.26657)`, `color.text.secondary=lab(80.0793 3.16605 -5.85758)`, `color.text.tertiary=oklab(0.929998 0.00499013 -0.00863796 / 0.85)`, `color.text.inverse=lab(75.0532 23.3141 -46.9597)`, `color.surface.base=#000000`, `color.surface.muted=lab(7.03991 2.17281 -7.02048 / 0.66)`, `color.surface.raised=lab(7.03991 2.17281 -7.02048 / 0.44)`, `color.border.default=lab(100 0 0 / 0.16)`, `color.border.muted=oklab(0.999998 -0.00000980496 0.0000234246 / 0.112)`
- Spacing scale: `space.1=8px`, `space.2=10px`, `space.3=12px`, `space.4=14px`, `space.5=16px`, `space.6=20px`, `space.7=24px`, `space.8=28px`
- Radius/shadow/motion tokens: `radius.xs=12.8px`, `radius.sm=20px`, `radius.md=24px` | `shadow.1=rgba(0, 0, 0, 0) 0px 0px 0px 0px, rgba(0, 0, 0, 0) 0px 0px 0px 0px, rgba(0, 0, 0, 0) 0px 0px 0px 0px, rgba(0, 0, 0, 0) 0px 0px 0px 0px, lab(100 0 0 / 0.12) 0px 1px 0px 0px inset`, `shadow.2=rgba(0, 0, 0, 0) 0px 0px 0px 0px, rgba(0, 0, 0, 0) 0px 0px 0px 0px, rgba(0, 0, 0, 0) 0px 0px 0px 0px, rgba(0, 0, 0, 0) 0px 0px 0px 0px, lab(0 0 0 / 0.55) 0px 14px 44px -14px` | `motion.duration.instant=150ms`

## Accessibility
- Target: WCAG 2.2 AA
- Keyboard-first interactions required.
- Focus-visible rules required.
- Contrast constraints required.

## Writing Tone
Concise, confident, implementation-focused.

## Rules: Do
- Use semantic tokens, not raw hex values, in component guidance.
- Every component must define states for default, hover, focus-visible, active, disabled, loading, and error.
- Component behavior should specify responsive and edge-case handling.
- Interactive components must document keyboard, pointer, and touch behavior.
- Accessibility acceptance criteria must be testable in implementation.

## Rules: Don't
- Do not allow low-contrast text or hidden focus indicators.
- Do not introduce one-off spacing or typography exceptions.
- Do not use ambiguous labels or non-descriptive actions.
- Do not ship component guidance without explicit state rules.

## Guideline Authoring Workflow
1. Restate design intent in one sentence.
2. Define foundations and semantic tokens.
3. Define component anatomy, variants, interactions, and state behavior.
4. Add accessibility acceptance criteria with pass/fail checks.
5. Add anti-patterns, migration notes, and edge-case handling.
6. End with a QA checklist.

## Required Output Structure
- Context and goals.
- Design tokens and foundations.
- Component-level rules (anatomy, variants, states, responsive behavior).
- Accessibility requirements and testable acceptance criteria.
- Content and tone standards with examples.
- Anti-patterns and prohibited implementations.
- QA checklist.

## Component Rule Expectations
- Include keyboard, pointer, and touch behavior.
- Include spacing and typography token requirements.
- Include long-content, overflow, and empty-state handling.
- Include known page component density: cards (36), links (27), lists (5), buttons (2), navigation (2).

- Extraction diagnostics: Audience and product surface inference confidence is low; verify generated brand context.

## Quality Gates
- Every non-negotiable rule must use "must".
- Every recommendation should use "should".
- Every accessibility rule must be testable in implementation.
- Teams should prefer system consistency over local visual exceptions.
