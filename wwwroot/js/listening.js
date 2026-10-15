(() => {
    'use strict';
    const root = document.querySelector('.listening');
    if (!root) return;
    const labels = JSON.parse(document.getElementById('listening-labels').textContent);
    const el = id => document.getElementById('listening-' + id);
    const format = (key, ...values) => labels[key].replace(/\{(\d+)\}/g, (_, n) => values[n]);
    const params = new URLSearchParams(location.search);
    const form = el('filters');
    const audio = el('audio');
    let page = 1, totalPages = 1, requestNumber = 0, lesson, cues = [], cueButtons = [], activeIndex = -1;
    let player, usingYouTube = false, mediaReady = false, mediaFailed = false;
    let syncInterval, readyTimeout, seekTimeout, cancelApiLoad;
    const lessonId = params.get('lesson');
    const url = path => path + '?culture=' + encodeURIComponent(root.dataset.culture);
    const pageUrl = id => {
        const query = new URLSearchParams(location.search);
        if (id == null) query.delete('lesson'); else query.set('lesson', id);
        query.set('culture', root.dataset.culture);
        return location.pathname + '?' + query;
    };
    function errorMessage(response, saving) {
        if (response.redirected || response.status === 401 || response.status === 403) return labels.Session;
        if (response.status === 404) return labels.Unavailable;
        return saving ? labels.SaveError : labels.LoadError;
    }
    async function getJson(path) {
        const response = await fetch(path, { credentials: 'same-origin', headers: { Accept: 'application/json' } });
        if (!response.ok || response.redirected || !response.headers.get('content-type')?.includes('application/json'))
            throw new Error(errorMessage(response, false));
        return response.json();
    }
    function node(tag, text, className) {
        const item = document.createElement(tag);
        if (text != null) item.textContent = text;
        if (className) item.className = className;
        return item;
    }
    function choices(select, items, selected) {
        select.replaceChildren(select.options[0]);
        items.forEach(item => select.add(new Option(item.name, item.id)));
        select.value = selected;
    }
    function lessonCover(item) {
        const cover = node('div', null, 'listening-card-cover');
        const placeholder = node('span', '♪', 'listening-card-placeholder');
        placeholder.setAttribute('aria-hidden', 'true');
        cover.append(placeholder);
        if (typeof item.youTubeVideoId === 'string' && /^[A-Za-z0-9_-]{11}$/.test(item.youTubeVideoId)) {
            const thumbnail = node('img');
            thumbnail.alt = ''; thumbnail.loading = 'lazy'; thumbnail.decoding = 'async';
            thumbnail.width = 480; thumbnail.height = 360;
            thumbnail.addEventListener('error', () => { thumbnail.hidden = true; }, { once: true });
            thumbnail.src = 'https://img.youtube.com/vi/' + encodeURIComponent(item.youTubeVideoId) + '/hqdefault.jpg';
            cover.append(thumbnail);
        }
        cover.append(node('span', item.level, 'listening-level-badge'));
        return cover;
    }
    async function loadList() {
        const request = ++requestNumber;
        root.setAttribute('aria-busy', 'true');
        el('message').textContent = labels.Loading;
        el('retry').hidden = true;
        el('grid').replaceChildren();
        el('empty').hidden = true;
        el('pagination').hidden = true;
        el('count').textContent = '';
        const query = new URLSearchParams(new FormData(form));
        for (const [key, value] of [...query]) if (!value) query.delete(key);
        query.set('Page', page);
        query.set('culture', root.dataset.culture);
        history.replaceState(null, '', location.pathname + '?' + query);
        try {
            const data = await getJson('/listening/lessons?' + query);
            if (request !== requestNumber) return;
            page = data.query.page; totalPages = data.totalPages;
            query.set('Page', page);
            history.replaceState(null, '', location.pathname + '?' + query);
            choices(form.elements.LevelId, data.choices.levels, query.get('LevelId') || '');
            choices(form.elements.TopicId, data.choices.topics, query.get('TopicId') || '');
            for (const item of data.items) {
                const link = node('a', null, 'listening-card');
                link.href = pageUrl(item.id);
                const body = node('div', null, 'listening-card-body');
                const footer = node('div', null, 'listening-card-footer');
                const status = node('span', item.isLearned ? labels.Learned : labels.NotLearned, 'listening-badge');
                status.classList.toggle('is-learned', item.isLearned);
                footer.append(node('span', format('Cues', item.cueCount)), status);
                body.append(node('h2', item.title), node('p', item.topic, 'listening-card-topic'), footer);
                link.append(lessonCover(item), body);
                el('grid').append(link);
            }
            el('empty').hidden = data.items.length !== 0;
            el('count').textContent = format('Count', data.totalCount);
            el('page').textContent = format('Page', page, totalPages);
            el('previous').disabled = page <= 1;
            el('next').disabled = page >= totalPages;
            el('pagination').hidden = data.totalCount === 0;
            el('message').textContent = '';
        } catch (error) {
            if (request !== requestNumber) return;
            el('message').textContent = error instanceof TypeError ? labels.LoadError : error.message;
            el('retry').hidden = false;
        } finally { if (request === requestNumber) root.setAttribute('aria-busy', 'false'); }
    }
    function progress() {
        el('progress').textContent = lesson.isLearned ? labels.Learned : labels.NotLearned;
        el('mark').textContent = lesson.isLearned ? labels.Unmark : labels.Mark;
        el('mark').setAttribute('aria-pressed', String(lesson.isLearned));
        el('progress').classList.toggle('is-learned', lesson.isLearned);
    }
    function scrollCue() {
        if (activeIndex < 0 || el('transcript').hidden) return;
        const container = el('transcript'), button = cueButtons[activeIndex];
        const bounds = button.getBoundingClientRect(), viewport = container.getBoundingClientRect();
        if (bounds.top < viewport.top || bounds.bottom > viewport.bottom)
            container.scrollTop += bounds.top - viewport.top - container.clientHeight / 2 + bounds.height / 2;
    }
    function stopSynchronization() {
        clearInterval(syncInterval); syncInterval = undefined;
    }
    function enableSeek(enabled) {
        cueButtons.forEach(button => { button.disabled = !enabled; });
        for (const id of ['play', 'seek', 'cue-previous', 'cue-replay', 'cue-next']) el(id).disabled = !enabled;
    }
    const mediaTime = () => usingYouTube ? player.getCurrentTime() : audio.currentTime;
    const mediaDuration = () => usingYouTube ? player.getDuration() : audio.duration;
    const clock = seconds => {
        seconds = Number.isFinite(seconds) ? Math.max(0, Math.floor(seconds)) : 0;
        return Math.floor(seconds / 60) + ':' + String(seconds % 60).padStart(2, '0');
    };
    function cueIndexAt(seconds) {
        return cues.findLastIndex(cue => cue.startMs <= seconds * 1000);
    }
    function updateControls(seconds) {
        const duration = mediaDuration();
        const playing = usingYouTube ? player.getPlayerState() === window.YT.PlayerState.PLAYING : !audio.paused;
        el('play').dataset.playing = String(playing);
        el('play').setAttribute('aria-label', playing ? labels.Pause : labels.Play);
        el('play').title = playing ? labels.Pause : labels.Play;
        el('playback-time').textContent = clock(seconds) + ' / ' + clock(duration);
        el('seek').disabled = !Number.isFinite(duration) || duration <= 0;
        if (!el('seek').matches(':active')) el('seek').value = duration > 0 ? Math.round(seconds / duration * 1000) : 0;
        el('seek').setAttribute('aria-valuetext', clock(seconds) + ' / ' + clock(duration));
        const index = cueIndexAt(seconds);
        el('cue-previous').disabled = index <= 0;
        el('cue-replay').disabled = cues.length === 0;
        el('cue-next').disabled = cues.length === 0 || index >= cues.length - 1;
    }
    function seekMedia(seconds, play = false) {
        if (!mediaReady || mediaFailed || !Number.isFinite(seconds)) return;
        try {
            if (usingYouTube) {
                player.seekTo(seconds, true);
                if (play) player.playVideo();
                clearTimeout(seekTimeout);
                seekTimeout = setTimeout(synchronize, 150);
            } else {
                audio.currentTime = Math.min(seconds, Number.isFinite(audio.duration) ? audio.duration : Infinity);
                if (play) audio.play().catch(() => { /* Playback remains available through native controls. */ });
                synchronize();
            }
        } catch { if (usingYouTube) videoFailure(labels.VideoError); }
    }
    function navigateCue(direction) {
        if (!mediaReady || mediaFailed || !cues.length) return;
        try {
            const current = cueIndexAt(mediaTime());
            const index = direction === 0 ? Math.max(0, current) : current + direction;
            if (index >= 0 && index < cues.length) seekMedia(cues[index].startMs / 1000, true);
        } catch { if (usingYouTube) videoFailure(labels.VideoError); }
    }
    function disposeMedia() {
        stopSynchronization();
        clearTimeout(readyTimeout); clearTimeout(seekTimeout);
        mediaReady = false;
        enableSeek(false);
        if (player) {
            try { player.destroy(); } catch { /* A failed iframe may already be gone. */ }
            player = undefined;
        }
        el('video').replaceChildren();
        audio.pause(); audio.removeAttribute('src'); audio.load();
    }
    function videoFailure(message) {
        mediaFailed = true; mediaReady = false;
        stopSynchronization(); clearTimeout(readyTimeout); clearTimeout(seekTimeout);
        enableSeek(false);
        highlightAt(-1);
        el('video-status').textContent = message;
        el('video-status').hidden = false;
    }
    function loadYouTubeApi() {
        if (window.YT?.Player) return Promise.resolve();
        if (!window.englishListeningYouTubeApi) {
            window.englishListeningYouTubeApi = new Promise((resolve, reject) => {
                const script = document.createElement('script');
                const timeout = setTimeout(() => reject(new Error(labels.VideoApiError)), 15000);
                cancelApiLoad = () => { clearTimeout(timeout); reject(new Error(labels.VideoApiError)); };
                const previous = window.onYouTubeIframeAPIReady;
                window.onYouTubeIframeAPIReady = () => {
                    clearTimeout(timeout);
                    try { if (typeof previous === 'function') previous(); }
                    finally { resolve(); }
                };
                script.src = 'https://www.youtube.com/iframe_api';
                script.onerror = () => { clearTimeout(timeout); reject(new Error(labels.VideoApiError)); };
                document.head.append(script);
            });
        }
        return window.englishListeningYouTubeApi;
    }
    async function initializeVideo(videoId, request) {
        el('video-status').textContent = labels.VideoPending;
        el('video-status').hidden = false;
        try {
            await loadYouTubeApi();
            if (request !== requestNumber) return;
            const mount = node('div'); el('video').replaceChildren(mount);
            readyTimeout = setTimeout(() => videoFailure(labels.VideoReadyError), 15000);
            player = new window.YT.Player(mount, {
                width: '100%', height: '100%', videoId,
                playerVars: { origin: location.origin, playsinline: 1, autoplay: 0, controls: 1, fs: 1 },
                events: {
                    onReady: event => {
                        if (request !== requestNumber) {
                            try { event.target.destroy(); } catch { /* Already disposed. */ }
                            return;
                        }
                        if (mediaFailed) return;
                        clearTimeout(readyTimeout);
                        try {
                            event.target.getIframe().title = labels.Video;
                            mediaReady = true; enableSeek(true);
                            el('video-status').hidden = true;
                            synchronize();
                        } catch { videoFailure(labels.VideoError); }
                    },
                    onStateChange: event => {
                        if (request !== requestNumber || !mediaReady || mediaFailed) return;
                        stopSynchronization(); synchronize();
                        if (!mediaFailed && event.data === window.YT.PlayerState.PLAYING)
                            syncInterval = setInterval(synchronize, 150);
                    },
                    onError: event => {
                        if (request !== requestNumber) return;
                        const message = event.data === 100 ? labels.VideoUnavailable
                            : event.data === 101 || event.data === 150 ? labels.VideoEmbedError
                            : event.data === 153 ? labels.VideoIdentityError : labels.VideoError;
                        videoFailure(message);
                    }
                }
            });
        } catch (error) {
            if (request === requestNumber) videoFailure(error instanceof Error && error.message === labels.VideoApiError ? labels.VideoApiError : labels.VideoError);
        }
    }
    function synchronize() {
        if (!mediaReady || mediaFailed) return;
        try {
            const seconds = mediaTime();
            highlightAt(seconds * 1000);
            updateControls(seconds);
        } catch { if (usingYouTube) videoFailure(labels.VideoError); }
    }
    function highlightAt(time) {
        if (!Number.isFinite(time)) return;
        let low = 0, high = cues.length - 1, index = -1;
        while (low <= high) {
            const middle = (low + high) >> 1;
            if (cues[middle].startMs <= time) { index = middle; low = middle + 1; } else high = middle - 1;
        }
        if (index >= 0 && time >= cues[index].endMs) index = -1;
        if (index === activeIndex) return;
        if (activeIndex >= 0) cueButtons[activeIndex].removeAttribute('aria-current');
        activeIndex = index;
        if (index >= 0) { cueButtons[index].setAttribute('aria-current', 'true'); scrollCue(); }
    }
    async function loadLesson() {
        const request = ++requestNumber;
        disposeMedia(); mediaFailed = false;
        el('lesson').hidden = true;
        el('catalog').hidden = true; el('detail').hidden = false;
        el('heading').hidden = false;
        root.setAttribute('aria-labelledby', 'listening-title');
        el('back').href = pageUrl(null);
        el('message').textContent = labels.Loading; el('retry').hidden = true;
        root.setAttribute('aria-busy', 'true');
        try {
            if (!/^[1-9]\d*$/.test(lessonId)) throw new Error(labels.Unavailable);
            const data = await getJson(url('/listening/' + lessonId));
            if (request !== requestNumber) return;
            lesson = data.lesson; cues = data.cues; activeIndex = -1;
            el('lesson-title').textContent = lesson.title;
            el('heading').hidden = true;
            root.setAttribute('aria-labelledby', 'listening-lesson-title');
            el('meta').replaceChildren(node('span', lesson.level, 'listening-level-badge'), node('span', lesson.topic, 'listening-topic'));
            el('description').textContent = lesson.description || '';
            el('description').hidden = !lesson.description;
            el('audio-error').hidden = true;
            usingYouTube = lesson.youTubeVideoId != null;
            audio.hidden = usingYouTube; el('video').hidden = !usingYouTube;
            el('player-controls').hidden = !usingYouTube;
            el('play').dataset.playing = 'false';
            el('playback-time').textContent = '0:00 / 0:00';
            el('seek').value = 0;
            el('video-status').hidden = true;
            progress();
            el('cues').replaceChildren();
            cueButtons = cues.map(cue => {
                const button = node('button'); button.type = 'button';
                const seconds = Math.floor(cue.startMs / 1000);
                button.append(node('span', cue.sequence, 'listening-cue-number'), node('span', cue.text, 'listening-cue-text'),
                    node('span', Math.floor(seconds / 60) + ':' + String(seconds % 60).padStart(2, '0'), 'listening-time'));
                button.disabled = true;
                button.addEventListener('click', () => seekMedia(cue.startMs / 1000));
                const li = node('li'); li.append(button); el('cues').append(li);
                return button;
            });
            el('no-cues').hidden = cues.length !== 0;
            el('cue-controls').hidden = cues.length === 0;
            el('cue-count').textContent = format('Cues', cues.length);
            el('toggle').hidden = el('transcript').hidden = el('cue-hint').hidden = cues.length === 0;
            el('toggle').setAttribute('aria-expanded', 'true'); el('toggle').textContent = labels.HideTranscript;
            el('lesson').hidden = false; el('message').textContent = '';
            el('lesson-title').focus();
            if (usingYouTube) {
                // The server derives this ID from the persisted URL. Never embed the submitted URL.
                if (/^[A-Za-z0-9_-]{11}$/.test(lesson.youTubeVideoId))
                    initializeVideo(lesson.youTubeVideoId, request);
                else videoFailure(labels.VideoError);
            } else {
                try {
                    // Retain legacy HTTP(S)/internal audio, without selecting arbitrary media schemes.
                    const internalAudio = /^\/listening\/audio\/(?:voa-[1-5]|[a-f0-9]{32})$/.test(lesson.audioUrl);
                    const source = internalAudio ? new URL(lesson.audioUrl, location.origin) : new URL(lesson.audioUrl);
                    if (!['http:', 'https:'].includes(source.protocol)) throw new Error();
                    audio.src = source.href;
                } catch { mediaFailed = true; el('audio-error').hidden = false; }
            }
        } catch (error) {
            if (request !== requestNumber) return;
            el('message').textContent = error instanceof TypeError ? labels.LoadError : error.message;
            el('retry').hidden = false;
        } finally { if (request === requestNumber) root.setAttribute('aria-busy', 'false'); }
    }
    el('mark').addEventListener('click', async () => {
        el('mark').disabled = true; el('save-status').textContent = labels.Saving;
        try {
            const tokens = await getJson(url('/listening/antiforgery'));
            const body = new URLSearchParams({ IsLearned: String(!lesson.isLearned) });
            body.set(tokens.formFieldName, tokens.requestToken);
            const response = await fetch(url('/listening/' + lesson.id + '/learned'), { method: 'POST', credentials: 'same-origin', body });
            if (response.status !== 204 || response.redirected) throw new Error(errorMessage(response, true));
            lesson.isLearned = !lesson.isLearned; progress(); el('save-status').textContent = labels.Saved;
        } catch (error) { el('save-status').textContent = error instanceof TypeError ? labels.SaveError : error.message; }
        finally { el('mark').disabled = false; }
    });
    el('toggle').addEventListener('click', () => {
        const hidden = !el('transcript').hidden;
        el('transcript').hidden = hidden;
        el('toggle').setAttribute('aria-expanded', String(!hidden));
        el('toggle').textContent = hidden ? labels.ShowTranscript : labels.HideTranscript;
        if (!hidden) scrollCue();
    });
    el('cue-previous').addEventListener('click', () => navigateCue(-1));
    el('cue-replay').addEventListener('click', () => navigateCue(0));
    el('cue-next').addEventListener('click', () => navigateCue(1));
    el('play').addEventListener('click', () => {
        if (!usingYouTube || !mediaReady || mediaFailed) return;
        try {
            if (player.getPlayerState() === window.YT.PlayerState.PLAYING) player.pauseVideo();
            else player.playVideo();
        } catch { videoFailure(labels.VideoError); }
    });
    el('seek').addEventListener('change', () => {
        if (!mediaReady || mediaFailed) return;
        try {
            const duration = mediaDuration();
            if (Number.isFinite(duration) && duration > 0) seekMedia(Number(el('seek').value) / 1000 * duration);
        } catch { if (usingYouTube) videoFailure(labels.VideoError); }
    });
    ['timeupdate', 'seeking', 'seeked', 'ended', 'play', 'pause'].forEach(event => audio.addEventListener(event, synchronize));
    audio.addEventListener('loadedmetadata', () => {
        if (usingYouTube || mediaFailed || !audio.hasAttribute('src')) return;
        mediaReady = true; enableSeek(true); synchronize();
    });
    audio.addEventListener('error', () => {
        if (usingYouTube || !audio.hasAttribute('src')) return;
        mediaFailed = true; mediaReady = false;
        el('audio-error').hidden = false; enableSeek(false); highlightAt(-1);
    });
    form.addEventListener('submit', event => { event.preventDefault(); page = 1; loadList(); });
    el('reset').addEventListener('click', () => { form.elements.Search.value = ''; form.elements.LevelId.value = ''; form.elements.TopicId.value = ''; page = 1; loadList(); });
    el('previous').addEventListener('click', () => { if (page > 1) { page--; loadList(); } });
    el('next').addEventListener('click', () => { if (page < totalPages) { page++; loadList(); } });
    el('retry').addEventListener('click', () => lessonId !== null ? loadLesson() : loadList());
    // A browser back/forward cache restore must refresh the learned labels.
    window.addEventListener('pageshow', event => { if (event.persisted) location.reload(); });
    window.addEventListener('pagehide', () => { ++requestNumber; disposeMedia(); cancelApiLoad?.(); });
    if (lessonId !== null) loadLesson();
    else {
        form.elements.Search.value = params.get('Search') || '';
        for (const key of ['LevelId', 'TopicId']) if (params.get(key)) form.elements[key].add(new Option(params.get(key), params.get(key), true, true));
        page = Math.max(1, Number.parseInt(params.get('Page'), 10) || 1);
        loadList();
    }
})();
