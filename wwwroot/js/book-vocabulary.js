(() => {
    'use strict';
    const wordCharacter = /[\p{L}\p{M}\p{N}_]/u;
    const joiner = /['’\-]/u;
    const escapePattern = value => value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const before = (source, index) => index ? Array.from(source.slice(Math.max(0, index - 2), index)).pop() : '';
    const after = (source, index) => index < source.length ? String.fromCodePoint(source.codePointAt(index)) : '';
    const insideWordBefore = (source, index) => {
        const adjacent = before(source, index);
        return wordCharacter.test(adjacent) || joiner.test(adjacent) && wordCharacter.test(before(source, index - adjacent.length));
    };
    const insideWordAfter = (source, index) => {
        const adjacent = after(source, index);
        return wordCharacter.test(adjacent) || joiner.test(adjacent) && wordCharacter.test(after(source, index + adjacent.length));
    };

    window.BookVocabulary = class {
        constructor(raw, source) {
            this.entries = [];
            this.matches = [];
            try {
                const document = JSON.parse(raw);
                if (document?.version !== 1 || !Array.isArray(document.entries) || document.entries.length > 200) return;
                // Unknown/legacy data is ignored for highlighting, never treated as markup.
                const valid = entry => entry && typeof entry.word === 'string' && entry.word.trim() && entry.word.length <= 160
                    && typeof entry.meaningVi === 'string' && entry.meaningVi.trim() && entry.meaningVi.length <= 2000
                    && (entry.ipa == null || typeof entry.ipa === 'string' && entry.ipa.length <= 120)
                    && (entry.partOfSpeech == null || typeof entry.partOfSpeech === 'string' && entry.partOfSpeech.length <= 80);
                if (!document.entries.every(valid)) return;
                this.entries = document.entries;
            } catch { return; }
            const candidates = [];
            this.entries.forEach((entry, index) => {
                // Match flexible whitespace without rewriting the original text or offsets.
                const expression = new RegExp(`(?=(${entry.word.trim().split(/\s+/u).map(escapePattern).join('\\s+')}))`, 'giu');
                for (const match of source.matchAll(expression)) {
                    const start = match.index, end = start + match[1].length;
                    if (insideWordBefore(source, start) || insideWordAfter(source, end)) continue;
                    candidates.push({ start, end, index });
                }
            });
            // Leftmost match wins; at the same start, prefer the longest phrase.
            candidates.sort((a, b) => a.start - b.start || b.end - a.end || a.index - b.index);
            let end = 0;
            for (const match of candidates) {
                if (match.start < end) continue;
                this.matches.push(match);
                end = match.end;
            }
        }

        render(target, source, start, end, interactive = true) {
            const fragment = document.createDocumentFragment();
            let offset = start;
            // Binary search the first possible intersection with this page.
            let low = 0, high = this.matches.length;
            while (low < high) {
                const middle = Math.floor((low + high) / 2);
                if (this.matches[middle].end <= start) low = middle + 1;
                else high = middle;
            }
            for (let i = low; i < this.matches.length && this.matches[i].start < end; i++) {
                const match = this.matches[i];
                const from = Math.max(start, match.start), to = Math.min(end, match.end);
                fragment.append(document.createTextNode(source.slice(offset, from)));
                const highlight = document.createElement('span');
                highlight.className = 'vocabulary-highlight';
                highlight.textContent = source.slice(from, to);
                if (interactive) {
                    highlight.dataset.noteIndex = match.index;
                    highlight.setAttribute('role', 'button');
                    highlight.setAttribute('aria-haspopup', 'dialog');
                    highlight.setAttribute('aria-controls', 'book-vocabulary-popup');
                    highlight.tabIndex = 0;
                }
                fragment.append(highlight);
                offset = to;
            }
            fragment.append(document.createTextNode(source.slice(offset, end)));
            target.replaceChildren(fragment);
        }
    };
})();
