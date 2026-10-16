(() => {
    'use strict';
    const root = document.querySelector('[data-student-books]');
    if (!root) return;
    const find = selector => root.querySelector(selector);
    const vi = root.dataset.culture.startsWith('vi');
    const text = (vn, en) => vi ? vn : en;
    const gallery = find('[data-gallery]');
    const message = find('[data-message]');
    const retry = find('[data-retry]');
    const previous = find('[data-previous]');
    const next = find('[data-next]');
    const detailPanel = find('[data-detail-panel]');
    const closeButton = find('[data-close]');
    const menuButton = find('[data-menu-toggle]');
    const menuLayer = find('[data-menu-layer]');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const palette = ['#b7976c', '#945a3e', '#566044'];
    let books = [];
    let cards = [];
    let selected = 0;
    let detailRequest;
    let focusTimer;
    let closingTimer;
    let selectedCard = null;
    let frame = 0;
    let pointerX = 0;
    let pointerY = 0;
    let pointerClientX = -10000;
    let pointerClientY = -10000;

    async function getJson(url, signal) {
        const response = await fetch(url, { headers: { Accept: 'application/json' }, signal });
        if (!response.ok || !response.headers.get('content-type')?.includes('application/json')) {
            throw new Error('Books unavailable');
        }
        return response.json();
    }

    function select(index) {
        if (document.body.dataset.mode === 'detail' || !books.length) return;
        selected = (index + books.length) % books.length;
        const left = books.length > 2 ? (selected - 1 + books.length) % books.length : -1;
        const right = books.length > 1 ? (selected + 1) % books.length : -1;
        cards.forEach((card, i) => {
            const slot = i === selected ? 'centre' : i === left ? 'left' : i === right ? 'right' : 'hidden';
            card.dataset.slot = slot;
            card.inert = slot === 'hidden';
            card.tabIndex = slot === 'hidden' ? -1 : 0;
            card.setAttribute('aria-hidden', String(slot === 'hidden'));
            card.setAttribute('aria-label', `${i === selected ? text('Mở chi tiết', 'Open details for') : text('Chọn', 'Select')} ${books[i].title}`);
            if (i === selected) card.setAttribute('aria-current', 'true');
            else card.removeAttribute('aria-current');
        });
        // Keep keyboard focus on a visible book when the focused side book leaves the stack.
        if (gallery.contains(document.activeElement) && document.activeElement.inert) cards[selected].focus();
        find('[data-position]').textContent = `${selected + 1} / ${books.length}`;
    }

    function createCard(book, index) {
        const card = find('[data-book-template]').content.firstElementChild.cloneNode(true);
        card.style.setProperty('--cover-color', palette[index % palette.length]);
        card.style.setProperty('--cover-ink', index % palette.length === 0 ? '#31291e' : '#f4e6cc');
        card.querySelector('.cover-title').textContent = book.title;
        card.querySelector('.cover-kicker').textContent = book.topic;
        card.querySelector('.cover-subtitle').textContent = book.author;
        const image = card.querySelector('img');
        if (book.coverUrl) {
            try {
                // Uploaded covers use /books/covers/...; legacy covers may use absolute HTTP/HTTPS URLs.
                const url = new URL(book.coverUrl, location.origin);
                if (['https:', 'http:'].includes(url.protocol) && !url.username && !url.password) {
                    image.addEventListener('load', () => { image.hidden = false; card.classList.add('has-cover'); });
                    image.addEventListener('error', () => { image.hidden = true; card.classList.remove('has-cover'); });
                    image.src = url.href;
                }
            } catch { /* Retain the sample's coloured cover when the saved URL is invalid. */ }
        }
        card.addEventListener('click', () => index === selected ? openDetails(book) : select(index));
        card.addEventListener('pointerenter', () => card.dataset.hovered = 'true');
        card.addEventListener('pointerleave', () => card.dataset.hovered = 'false');
        return card;
    }

    async function load() {
        retry.hidden = true;
        message.hidden = false;
        message.textContent = text('Đang tải sách…', 'Loading books…');
        gallery.setAttribute('aria-busy', 'true');
        try {
            const url = new URL(root.dataset.api, location.origin);
            url.searchParams.set('culture', root.dataset.culture);
            const first = await getJson(url);
            const items = [...first.items];
            // The existing endpoint is paginated; include every page without changing its contract.
            for (let page = 2; page <= first.totalPages; page++) {
                url.searchParams.set('page', String(page));
                const result = await getJson(url);
                items.push(...result.items);
            }
            books = [...new Map(items.map(book => [book.id, book])).values()];
            gallery.replaceChildren();
            cards = books.map(createCard);
            gallery.append(...cards);
            message.hidden = books.length > 0;
            message.textContent = text('Chưa có sách để đọc. Hãy quay lại sau.', 'No books are available yet. Check back later.');
            find('[data-dock]').hidden = books.length === 0;
            previous.disabled = next.disabled = books.length < 2;
            if (books.length) {
                // Establish the initial positions before enabling transitions between books.
                select(0);
                requestAnimationFrame(() => requestAnimationFrame(() => root.classList.add('is-ready')));
            }
        } catch {
            message.textContent = text('Không tải được tủ sách. Vui lòng thử lại.', 'Unable to load the library. Please try again.');
            retry.hidden = false;
        } finally {
            gallery.setAttribute('aria-busy', 'false');
        }
    }

    async function openDetails(book) {
        if (document.body.dataset.mode === 'detail') return;
        clearTimeout(closingTimer);
        selectedCard = cards[selected];
        selectedCard.style.setProperty('--detail-yaw', '-5deg');
        selectedCard.style.setProperty('--detail-pitch', '0deg');
        cards.forEach(card => {
            card.classList.toggle('selected', card === selectedCard);
            card.tabIndex = -1;
            card.inert = true;
            card.dataset.hovered = 'false';
        });
        document.body.dataset.mode = 'detail';
        detailPanel.inert = false;
        detailPanel.setAttribute('aria-hidden', 'false');
        closeButton.tabIndex = 0;
        find('[data-detail-scroll]').scrollTop = 0;
        focusTimer = setTimeout(() => {
            closeButton.focus({ preventScroll: true });
            if (window.innerWidth > 900) root.scrollTop = 0;
        }, reducedMotion.matches ? 0 : 700);
        detailRequest?.abort();
        detailRequest = new AbortController();
        const { signal } = detailRequest;
        find('[data-detail-title]').textContent = book.title;
        find('[data-detail-author]').textContent = book.author;
        find('[data-detail-topic]').textContent = book.topic;
        find('[data-detail-description]').textContent = '';
        find('[data-detail-chapters]').replaceChildren();
        const startReading = find('[data-start-reading]');
        startReading.hidden = true;
        const status = find('[data-detail-status]');
        status.textContent = text('Đang tải chi tiết…', 'Loading details…');
        try {
            const url = new URL(`${root.dataset.details}${book.id}`, location.origin);
            url.searchParams.set('culture', root.dataset.culture);
            const details = await getJson(url, signal);
            if (signal.aborted) return;
            find('[data-detail-title]').textContent = details.book.title;
            find('[data-detail-author]').textContent = details.book.author;
            find('[data-detail-topic]').textContent = details.book.topic;
            find('[data-detail-description]').textContent = details.book.description || text('Chưa có mô tả.', 'No description available.');
            status.textContent = details.chapters.length
                ? `${details.book.readChapterCount} / ${details.chapters.length} ${text('chương đã đọc', 'chapters read')}`
                : text('Chưa có chương để đọc.', 'No chapters available yet.');
            const readingUrl = chapterId => {
                const url = new URL(`${root.dataset.readBase}${book.id}/read`, location.origin);
                url.searchParams.set('culture', root.dataset.culture);
                if (chapterId) url.searchParams.set('chapterId', chapterId);
                return url.href;
            };
            startReading.hidden = !details.chapters.length;
            startReading.href = readingUrl(details.book.continueChapterId);
            for (const chapter of details.chapters) {
                const item = document.createElement('li');
                const copy = document.createElement('span');
                copy.className = 'doc-step-copy';
                const title = document.createElement('a');
                title.href = readingUrl(chapter.id);
                title.className = 'reader-chapter-link';
                const updateTitle = () => { title.textContent = `${chapter.title?.trim() || `Chapter ${chapter.displayOrder}`}${chapter.isRead ? text(' · Đã đọc', ' · Read') : ''}`; };
                updateTitle();
                copy.append(title);
                item.append(copy);
                find('[data-detail-chapters]').append(item);
            }
        } catch {
            if (!signal.aborted) status.textContent = text('Không tải được chi tiết. Đóng và mở lại để thử lại.', 'Unable to load details. Close and reopen to retry.');
        }
    }

    function closeDetail() {
        if (document.body.dataset.mode !== 'detail') return;
        clearTimeout(focusTimer);
        detailRequest?.abort();
        document.body.dataset.mode = 'gallery';
        detailPanel.inert = true;
        detailPanel.setAttribute('aria-hidden', 'true');
        closeButton.tabIndex = -1;
        const lastCard = selectedCard;
        lastCard?.style.setProperty('--detail-yaw', '-5deg');
        lastCard?.style.setProperty('--detail-pitch', '0deg');
        closingTimer = setTimeout(() => {
            cards.forEach(card => card.classList.remove('selected'));
            selectedCard = null;
            select(selected);
            lastCard?.focus({ preventScroll: true });
        }, reducedMotion.matches ? 0 : 700);
    }

    // Parallax and opened-book orbit are retained from the supplied sample.
    function updateParallax() {
        frame = 0;
        if (reducedMotion.matches) return;
        if (document.body.dataset.mode === 'detail' && selectedCard) {
            const bounds = selectedCard.getBoundingClientRect();
            const centreX = bounds.left + bounds.width / 2;
            const centreY = bounds.top + bounds.height / 2;
            const inViewport = pointerClientX >= 0 && pointerClientX <= window.innerWidth
                && pointerClientY >= 0 && pointerClientY <= window.innerHeight;
            const reachX = pointerClientX < centreX ? Math.max(centreX, 1) : Math.max(window.innerWidth - centreX, 1);
            const reachY = pointerClientY < centreY ? Math.max(centreY, 1) : Math.max(window.innerHeight - centreY, 1);
            const x = inViewport ? Math.max(-1, Math.min(1, (pointerClientX - centreX) / reachX)) : -5 / 16;
            const y = inViewport ? Math.max(-1, Math.min(1, (pointerClientY - centreY) / reachY)) : 0;
            selectedCard.style.setProperty('--detail-yaw', `${x * 16}deg`);
            selectedCard.style.setProperty('--detail-pitch', `${y * -10}deg`);
            return;
        }
        document.documentElement.style.setProperty('--mx', `${pointerX * 11}px`);
        document.documentElement.style.setProperty('--my', `${pointerY * 8}px`);
        cards.forEach(card => {
            if (card.dataset.slot === 'hidden') return;
            const depth = card.dataset.slot === 'centre' ? 1 : .58;
            card.style.setProperty('--local-x', `${pointerX * 15 * depth}px`);
            card.style.setProperty('--local-y', `${pointerY * 9 * depth}px`);
        });
    }
    window.addEventListener('pointermove', event => {
        pointerX = event.clientX / window.innerWidth - .5;
        pointerY = event.clientY / window.innerHeight - .5;
        pointerClientX = event.clientX;
        pointerClientY = event.clientY;
        if (!frame) frame = requestAnimationFrame(updateParallax);
    }, { passive: true });
    window.addEventListener('pointerleave', () => {
        pointerX = pointerY = 0;
        pointerClientX = pointerClientY = -10000;
        if (!frame) frame = requestAnimationFrame(updateParallax);
    });
    previous.addEventListener('click', () => select(selected - 1));
    next.addEventListener('click', () => select(selected + 1));
    function toggleMenu(open = document.body.dataset.menu !== 'open') {
        document.body.dataset.menu = open ? 'open' : 'closed';
        menuLayer.inert = !open;
        menuLayer.setAttribute('aria-hidden', String(!open));
        menuButton.setAttribute('aria-expanded', String(open));
        menuButton.setAttribute('aria-label', open ? text('Đóng menu', 'Close menu') : text('Mở menu', 'Open menu'));
        if (open) find('.menu-link').focus({ preventScroll: true });
        else menuButton.focus({ preventScroll: true });
    }
    menuButton.addEventListener('click', () => toggleMenu());
    find('[data-menu-close]').addEventListener('click', event => {
        event.preventDefault();
        toggleMenu(false);
        closeDetail();
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            if (document.body.dataset.menu === 'open') toggleMenu(false);
            else closeDetail();
            return;
        }
        if (document.body.dataset.menu === 'open') return;
        if (document.body.dataset.mode === 'detail' || books.length < 2 || !['ArrowLeft', 'ArrowRight'].includes(event.key)) return;
        event.preventDefault();
        select(selected + (event.key === 'ArrowLeft' ? -1 : 1));
    });
    closeButton.addEventListener('click', closeDetail);
    retry.addEventListener('click', load);
    load();
})();
