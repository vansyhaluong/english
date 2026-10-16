(() => {
    'use strict';
    const reader = document.querySelector('[data-book-reader]');
    if (!reader) return;
    const find = name => reader.querySelector(`[data-reader-${name}]`);
    const vi = reader.dataset.culture.startsWith('vi');
    const text = (vn, en) => vi ? vn : en;
    const page = find('page');
    const pageNext = find('page-next');
    const paper = reader.querySelector('.reader-paper');
    const spread = window.matchMedia('(min-width: 1000px)');
    const visiblePages = () => spread.matches ? 2 : 1;
    const chapters = find('chapters');
    const mark = find('mark');
    const previous = find('prev');
    const next = find('next');
    const status = find('status');
    const retry = find('retry');
    const measure = document.createElement('div');
    measure.className = 'reader-page reader-measure';
    measure.setAttribute('aria-hidden', 'true');
    measure.lang = 'en';
    paper.append(measure);
    let details, chapter, requestedId, request;
    let pages = [], current = 0, anchor = 0, generation = 0, resizeTimer;
    let loading = false, paginating = false, saving = false;
    let tokens;
    let active = true;
    let vocabulary = new window.BookVocabulary(null, '');
    const popup = reader.querySelector('[data-vocabulary-popup]');
    const vocabularyField = name => popup.querySelector(`[data-vocabulary-${name}]`);
    const speak = vocabularyField('speak');
    const speechStatus = vocabularyField('speech-status');
    const speechAvailable = 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window;
    let selectedNote, noteTrigger, utterance;
    let noteRectIndex = 0;
    function positionVocabulary() {
        if (!popup.open || !noteTrigger?.isConnected) return;
        const rectangles = [...noteTrigger.getClientRects()];
        const word = rectangles[Math.min(noteRectIndex, rectangles.length - 1)] || noteTrigger.getBoundingClientRect();
        const leaf = noteTrigger.closest('.reader-leaf').getBoundingClientRect();
        const viewport = window.visualViewport;
        const viewportLeft = viewport?.offsetLeft || 0;
        const viewportTop = viewport?.offsetTop || 0;
        const leftEdge = Math.max(viewportLeft + 12, leaf.left + 12);
        const rightEdge = Math.min(viewportLeft + (viewport?.width || window.innerWidth) - 12, leaf.right - 12);
        const topEdge = Math.max(viewportTop + 12, leaf.top + 12);
        const bottomEdge = Math.min(viewportTop + (viewport?.height || window.innerHeight) - 12, leaf.bottom - 12);
        popup.style.width = `${Math.max(1, Math.min(360, rightEdge - leftEdge))}px`;
        popup.style.maxHeight = `${Math.max(1, bottomEdge - topEdge)}px`;
        const bounds = popup.getBoundingClientRect();
        const below = word.bottom + 8;
        // At the bottom of the leaf, overlap the word rather than overflow the book.
        const top = below + bounds.height <= bottomEdge ? below : word.top;
        popup.style.left = `${Math.max(leftEdge, Math.min(word.left, rightEdge - bounds.width))}px`;
        popup.style.top = `${Math.max(topEdge, Math.min(top, bottomEdge - bounds.height))}px`;
    }
    function stopSpeech() {
        if (utterance) {
            utterance.onend = utterance.onerror = null;
            window.speechSynthesis.cancel();
            utterance = undefined;
        }
    }
    function closeVocabulary() {
        stopSpeech();
        if (popup.open) popup.close();
    }
    function openVocabulary(trigger, event) {
        const entry = vocabulary.entries[Number(trigger.dataset.noteIndex)];
        if (!entry) return;
        stopSpeech();
        noteTrigger = trigger;
        const rectangles = [...trigger.getClientRects()];
        noteRectIndex = event?.detail ? Math.max(0, rectangles.findIndex(rect => event.clientX >= rect.left && event.clientX <= rect.right && event.clientY >= rect.top && event.clientY <= rect.bottom)) : 0;
        selectedNote = entry;
        vocabularyField('word').textContent = entry.word;
        vocabularyField('meaning').textContent = entry.meaningVi;
        vocabularyField('ipa').textContent = entry.ipa || '';
        vocabularyField('pos').textContent = entry.partOfSpeech || '';
        speak.disabled = !speechAvailable;
        speechStatus.textContent = speechAvailable ? '' : text('Trình duyệt này chưa hỗ trợ phát âm.', 'Speech is not available in this browser.');
        popup.showModal();
        positionVocabulary();
    }
    paper.addEventListener('click', event => {
        const trigger = event.target.closest('[data-note-index]');
        if (trigger) openVocabulary(trigger, event);
    });
    paper.addEventListener('keydown', event => {
        const trigger = event.target.closest('[data-note-index]');
        if (!trigger || !['Enter', ' '].includes(event.key)) return;
        event.preventDefault();
        openVocabulary(trigger);
    });
    vocabularyField('close').addEventListener('click', closeVocabulary);
    popup.addEventListener('close', () => {
        stopSpeech();
        if (noteTrigger?.isConnected) noteTrigger.focus({ preventScroll: true });
        noteTrigger = undefined;
    });
    new ResizeObserver(positionVocabulary).observe(popup);
    reader.addEventListener('scroll', event => {
        if (!popup.contains(event.target)) positionVocabulary();
    }, { capture: true, passive: true });
    window.visualViewport?.addEventListener('resize', positionVocabulary);
    window.visualViewport?.addEventListener('scroll', positionVocabulary);
    speak.addEventListener('click', () => {
        if (!speechAvailable || !selectedNote) return;
        stopSpeech();
        try {
            const message = new SpeechSynthesisUtterance(selectedNote.word);
            utterance = message;
            message.lang = 'en-US';
            const voices = window.speechSynthesis.getVoices().filter(voice => /^en(?:[-_]|$)/i.test(voice.lang));
            const voice = voices.find(voice => voice.localService) || voices.find(voice => voice.default) || voices[0];
            if (voice) { message.voice = voice; message.lang = voice.lang; }
            message.rate = .9;
            speak.disabled = true;
            speechStatus.textContent = text('Đang phát âm…', 'Speaking…');
            message.onend = () => { utterance = undefined; speak.disabled = false; speechStatus.textContent = ''; };
            message.onerror = () => {
                utterance = undefined;
                speak.disabled = false;
                speechStatus.textContent = text('Không phát âm được trên thiết bị này. Bạn có thể thử lại.', 'Speech could not play on this device. You can retry.');
            };
            window.speechSynthesis.speak(message);
        } catch {
            utterance = undefined;
            speak.disabled = false;
            speechStatus.textContent = text('Phát âm hiện không khả dụng.', 'Speech is currently unavailable.');
        }
    });
    // Keep opens in order, even if an earlier response arrives late.
    let writes = Promise.resolve();
    const name = item => item.title?.trim() || `Chapter ${item.displayOrder}`;
    const url = path => {
        const result = new URL(reader.dataset.api + path, location.origin);
        result.searchParams.set('culture', reader.dataset.culture);
        return result;
    };
    async function json(path, signal) {
        const response = await fetch(url(path), { headers: { Accept: 'application/json' }, signal });
        if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) throw new Error('Books unavailable');
        return response.json();
    }
    function post(path, body) {
        const operation = writes.then(async () => {
            tokens ??= await json('antiforgery');
            const response = await fetch(url(path), {
                method: 'POST',
                headers: { [tokens.headerName]: tokens.requestToken, 'Content-Type': 'application/json' },
                ...(body === undefined ? {} : { body: JSON.stringify(body) })
            });
            if (!response.ok) {
                if (response.status === 400) tokens = undefined;
                throw new Error('Unable to save');
            }
        });
        writes = operation.catch(() => {});
        return operation;
    }
    function updateProgress() {
        details.book.readChapterCount = details.chapters.filter(item => item.isRead).length;
        find('progress').textContent = `${details.book.readChapterCount} / ${details.chapters.length} ${text('chương đã đọc', 'chapters read')}`;
        for (const option of chapters.options) {
            const item = details.chapters.find(item => String(item.id) === option.value);
            option.textContent = `${name(item)}${item.isRead ? text(' · Đã đọc', ' · Read') : ''}`;
        }
        mark.textContent = chapter?.isRead ? text('Bỏ đánh dấu đã đọc', 'Mark as unread') : text('Đánh dấu đã đọc', 'Mark as read');
        mark.setAttribute('aria-pressed', String(Boolean(chapter?.isRead)));
    }
    function controls() {
        const blocked = loading || paginating || saving || !chapter || !pages.length;
        previous.disabled = blocked || (current === 0 && !chapter.previousChapterId);
        next.disabled = blocked || (current + visiblePages() >= pages.length && !chapter.nextChapterId);
        previous.textContent = current === 0 && chapter?.previousChapterId ? text('← Chương trước', '← Previous chapter') : text('← Trang trước', '← Previous page');
        next.textContent = current + visiblePages() >= pages.length && chapter?.nextChapterId ? text('Chương sau →', 'Next chapter →') : text('Trang sau →', 'Next page →');
        mark.disabled = loading || saving || !chapter;
        chapters.disabled = loading || saving;
        paper.setAttribute('aria-busy', String(loading || paginating));
    }
    function showPage(direction = 0) {
        if (!chapter || !pages.length) return;
        closeVocabulary();
        const range = pages[current];
        // Plain text only: never interpret chapter content as HTML.
        vocabulary.render(page, chapter.contentEn, range.start, range.end);
        const second = spread.matches ? pages[current + 1] : undefined;
        if (second) vocabulary.render(pageNext, chapter.contentEn, second.start, second.end);
        else pageNext.textContent = '';
        page.scrollTop = 0;
        pageNext.scrollTop = 0;
        anchor = range.start;
        find('count').textContent = `${text('Trang', 'Page')} ${current + 1}${second ? `–${current + 2}` : ''} / ${pages.length}`;
        find('folio-left').textContent = current + 1;
        find('folio-right').textContent = second ? current + 2 : '';
        find('end-note').textContent = current + visiblePages() >= pages.length ? text('Hết chương', 'End of chapter') : '';
        controls();
        for (const leaf of reader.querySelectorAll('.reader-leaf')) leaf.getAnimations().forEach(animation => animation.cancel());
        if (direction && !window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            const leaf = (spread.matches && direction > 0 ? pageNext : page).closest('.reader-leaf');
            leaf.animate([
                { opacity: .6, transform: `perspective(1400px) rotateY(${direction * 4}deg)` },
                { opacity: 1, transform: 'perspective(900px) rotateY(0) translateX(0)' }
            ], { duration: 220, easing: 'ease-out' });
        }
    }
    async function paginate() {
        if (!active || !chapter) return;
        const version = ++generation;
        const source = chapter.contentEn;
        const position = anchor;
        paginating = true;
        controls();
        measure.style.width = `${page.clientWidth}px`;
        measure.style.height = `${page.clientHeight}px`;
        const result = [];
        // Offsets preserve every character and never split a grapheme (e.g. an emoji).
        const boundaries = [0];
        if (typeof Intl.Segmenter === 'function') {
            for (const part of new Intl.Segmenter('en', { granularity: 'grapheme' }).segment(source)) {
                boundaries.push(part.index + part.segment.length);
                if (boundaries.length % 16000 === 0) {
                    await new Promise(resolve => requestAnimationFrame(resolve));
                    if (generation !== version || !active) return;
                }
            }
        } else {
            for (const point of source) boundaries.push(boundaries[boundaries.length - 1] + point.length);
        }
        const fits = (start, end) => {
            vocabulary.render(measure, source, start, end, false);
            return measure.scrollHeight <= measure.clientHeight && measure.scrollWidth <= measure.clientWidth;
        };
        let startIndex = 0;
        while (startIndex < boundaries.length - 1) {
            const start = boundaries[startIndex];
            let low = startIndex, step = 256;
            let high = Math.min(startIndex + step, boundaries.length - 1);
            // Exponential search avoids measuring the entire remaining long chapter per page.
            while (fits(start, boundaries[high])) {
                low = high;
                if (high === boundaries.length - 1) break;
                step *= 2;
                high = Math.min(startIndex + step, boundaries.length - 1);
            }
            while (low + 1 < high) {
                const middle = Math.floor((low + high) / 2);
                if (fits(start, boundaries[middle])) low = middle;
                else high = middle;
            }
            let endIndex = Math.max(startIndex + 1, low);
            if (endIndex < boundaries.length - 1) {
                // Prefer the last word boundary; only an oversized word is split.
                const candidate = source.slice(start, boundaries[endIndex]);
                const whitespace = [...candidate.matchAll(/\s+/gu)].pop();
                if (whitespace) {
                    const wordEnd = start + whitespace.index + whitespace[0].length;
                    while (endIndex > startIndex + 1 && boundaries[endIndex] > wordEnd) endIndex--;
                }
            }
            result.push({ start, end: boundaries[endIndex] });
            startIndex = endIndex;
            if (result.length % 8 === 0) {
                await new Promise(resolve => requestAnimationFrame(resolve));
                if (generation !== version || !active) return;
            }
        }
        if (generation !== version || !active) return;
        pages = result.length ? result : [{ start: 0, end: 0 }];
        current = pages.findIndex(range => position >= range.start && position < range.end);
        if (current < 0) current = pages.length - 1;
        current = Math.floor(current / visiblePages()) * visiblePages();
        paginating = false;
        measure.textContent = '';
        showPage();
        // Retain the original reading offset across consecutive layout changes.
        anchor = position;
    }
    async function openChapter(id, atEnd = false) {
        closeVocabulary();
        vocabulary = new window.BookVocabulary(null, '');
        request?.abort();
        request = new AbortController();
        const { signal } = request;
        ++generation;
        requestedId = id;
        loading = true;
        paginating = false;
        chapter = undefined;
        pages = [];
        current = anchor = 0;
        retry.hidden = true;
        page.textContent = '';
        pageNext.textContent = '';
        find('chapter-title').textContent = '';
        find('count').textContent = '';
        find('folio-left').textContent = find('folio-right').textContent = find('end-note').textContent = '';
        status.textContent = text('Đang mở chương…', 'Opening chapter…');
        chapters.value = String(id);
        controls();
        const book = details;
        try {
            const loaded = await json(`${book.book.id}/chapters/${id}`, signal);
            if (signal.aborted) return;
            chapter = loaded;
            vocabulary = new window.BookVocabulary(loaded.vocabularyNotes, loaded.contentEn);
            const summary = book.chapters.find(item => item.id === loaded.id);
            if (summary) summary.isRead = loaded.isRead;
            find('chapter-title').textContent = name(loaded);
            const address = new URL(location.href);
            address.searchParams.set('chapterId', loaded.id);
            history.replaceState(null, '', address);
            anchor = atEnd ? Math.max(0, loaded.contentEn.length - 1) : 0;
            loading = false;
            status.textContent = loaded.contentEn.trim() ? '' : text('Chương này chưa có nội dung.', 'This chapter has no content yet.');
            updateProgress();
            await paginate();
            if (signal.aborted) return;
            // Opening a chapter records its position; page turns never mark it read.
            try {
                await post(`${book.book.id}/chapters/${id}/opened`);
                book.book.continueChapterId = id;
            } catch {
                if (!signal.aborted) status.textContent = text('Không lưu được chương đọc tiếp. Bạn vẫn có thể đọc; mở lại chương để thử lưu lại.', 'Unable to save your place. You can still read; reopen the chapter to retry saving.');
            }
        } catch {
            if (!signal.aborted) {
                status.textContent = text('Không mở được chương. Chương có thể không còn khả dụng. Hãy thử lại hoặc chọn chương khác.', 'Unable to open this chapter. It may no longer be available. Retry or choose another chapter.');
                retry.hidden = false;
            }
        } finally {
            if (!signal.aborted) { loading = false; controls(); }
        }
    }
    async function loadBook() {
        closeVocabulary();
        request?.abort();
        ++generation;
        details = chapter = undefined;
        pages = [];
        current = anchor = 0;
        paginating = false;
        page.textContent = pageNext.textContent = '';
        request = new AbortController();
        const { signal } = request;
        loading = true;
        retry.hidden = true;
        status.textContent = text('Đang mở sách…', 'Opening book…');
        controls();
        try {
            details = await json(reader.dataset.bookId, signal);
            if (signal.aborted) return;
            find('title').textContent = details.book.title;
            find('running-title').textContent = details.book.title;
            find('author').textContent = details.book.author;
            find('topic').textContent = details.book.topic;
            document.title = `${details.book.title} · EnglishHub`;
            chapters.replaceChildren(...details.chapters.map(item => new Option(name(item), String(item.id))));
            updateProgress();
            const specified = Number(new URL(location.href).searchParams.get('chapterId') || reader.dataset.chapterId);
            const id = details.chapters.some(item => item.id === specified) ? specified
                : details.book.continueChapterId || details.chapters[0]?.id;
            if (id) { await openChapter(id); return; }
            status.textContent = text('Sách chưa có chương khả dụng. Hãy quay lại tủ sách.', 'No chapters are available yet. Return to the library.');
        } catch {
            if (!signal.aborted) {
                status.textContent = text('Không mở được sách. Hãy thử lại hoặc quay về tủ sách.', 'Unable to open this book. Retry or return to the library.');
                retry.hidden = false;
            }
        } finally {
            if (!signal.aborted) { loading = false; controls(); }
        }
    }
    window.addEventListener('pagehide', () => { closeVocabulary(); active = false; request?.abort(); ++generation; clearTimeout(resizeTimer); });
    window.addEventListener('pageshow', event => { if (event.persisted) { active = true; loadBook(); } });
    chapters.addEventListener('change', () => openChapter(Number(chapters.value)));
    retry.addEventListener('click', () => details && requestedId ? openChapter(requestedId) : loadBook());
    function turn(direction) {
        if ((direction < 0 ? previous : next).disabled) return;
        const target = current + direction * visiblePages();
        if (target >= 0 && target < pages.length) { current = target; showPage(direction); }
        else openChapter(direction < 0 ? chapter.previousChapterId : chapter.nextChapterId, direction < 0);
    }
    previous.addEventListener('click', () => turn(-1));
    next.addEventListener('click', () => turn(1));
    reader.addEventListener('keydown', event => {
        if (popup.open) return;
        if (!['ArrowLeft', 'ArrowRight'].includes(event.key) || event.target.closest('select, input, textarea, button') || event.altKey || event.ctrlKey || event.metaKey) return;
        event.preventDefault();
        turn(event.key === 'ArrowLeft' ? -1 : 1);
    });
    mark.addEventListener('click', async () => {
        if (mark.disabled) return;
        const active = chapter, book = details;
        const isRead = !active.isRead;
        saving = true;
        controls();
        try {
            await post(`${book.book.id}/chapters/${active.id}/read`, { isRead });
            active.isRead = isRead;
            const summary = book.chapters.find(item => item.id === active.id);
            if (summary) summary.isRead = isRead;
            if (details === book) {
                updateProgress();
                if (active === chapter) status.textContent = text('Đã lưu trạng thái đọc.', 'Reading status saved.');
            }
        } catch {
            if (details === book && active === chapter) status.textContent = text('Không lưu được trạng thái đọc. Vui lòng thử lại.', 'Unable to save reading status. Please retry.');
        } finally { saving = false; controls(); }
    });
    const schedule = () => {
        if (!active || !chapter) return;
        closeVocabulary();
        ++generation;
        paginating = true;
        controls();
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(paginate, 120);
    };
    find('font').addEventListener('change', event => {
        reader.style.setProperty('--reader-font', `${event.target.value}px`);
        schedule();
    });
    new ResizeObserver(schedule).observe(page);
    spread.addEventListener('change', schedule);
    document.fonts?.ready.then(schedule);
    loadBook();
})();
