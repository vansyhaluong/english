// Expand an in-page disclosure before navigating to its content.
document.querySelectorAll('a[href^="#"]').forEach(link => {
    link.addEventListener('click', () => {
        const target = document.getElementById(link.getAttribute('href').slice(1));
        if (target instanceof HTMLDetailsElement) target.open = true;
    });
});
function revealLinkedDisclosure() {
    const target = document.getElementById(location.hash.slice(1));
    if (target instanceof HTMLDetailsElement) target.open = true;
}
window.addEventListener('hashchange', revealLinkedDisclosure);
revealLinkedDisclosure();

const sceneMotionButton = document.querySelector('[data-scene-motion]');
if (sceneMotionButton) {
    let manuallyPaused = false;
    function updateSceneMotion() {
        document.body.classList.toggle('scene-motion-paused', manuallyPaused || document.hidden);
        sceneMotionButton.textContent = manuallyPaused ? 'Chuyển động nền' : 'Dừng nền';
        sceneMotionButton.setAttribute('aria-pressed', String(manuallyPaused));
        sceneMotionButton.setAttribute('aria-label', manuallyPaused ? 'Bật chuyển động ảnh nền' : 'Tạm dừng chuyển động ảnh nền');
    }
    sceneMotionButton.addEventListener('click', () => {
        manuallyPaused = !manuallyPaused;
        updateSceneMotion();
    });
    document.addEventListener('visibilitychange', updateSceneMotion);
    sceneMotionButton.hidden = false;
    updateSceneMotion();
}

// A small, local phrase book. No network requests or learning-progress writes.
const phraseBook = document.querySelector('[data-phrase-book]');
if (phraseBook) {
    const phrases = [
        ['A little progress every day adds up.', 'Mỗi ngày tiến bộ một chút, bạn sẽ đi được thật xa.'],
        ['Take your time. You are learning.', 'Cứ từ từ nhé. Bạn đang học mà.'],
        ['Every new word opens a new door.', 'Mỗi từ mới mở ra một cánh cửa mới.'],
        ['It is okay to make mistakes.', 'Mắc lỗi cũng không sao cả.'],
        ['Small steps can take you a long way.', 'Những bước nhỏ có thể đưa bạn đi thật xa.'],
        ['Try again. You know more than yesterday.', 'Thử lại nhé. Bạn đã biết nhiều hơn hôm qua rồi.']
    ];
    const sheet = phraseBook.querySelector('.note-sheet');
    const copy = sheet.querySelector('.note-copy');
    const pauseButton = phraseBook.querySelector('[data-phrase-pause]');
    const nextButton = phraseBook.querySelector('[data-phrase-next]');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    let index = 0;
    let paused = reducedMotion.matches;
    let hovered = false;
    let turning = false;
    let timer;
    let animation;

    function scheduleTurn() {
        window.clearTimeout(timer);
        if (!paused && !hovered && !document.hidden && !phraseBook.contains(document.activeElement)) {
            timer = window.setTimeout(() => turnPage(false), 8000);
        }
    }

    function updatePauseButton() {
        pauseButton.textContent = paused ? 'Tự đổi câu' : 'Tạm dừng';
        pauseButton.setAttribute('aria-label', paused ? 'Bật tự đổi câu' : 'Tạm dừng tự đổi câu');
        pauseButton.setAttribute('aria-pressed', String(paused));
    }

    async function turnPage(manual) {
        if (turning) return;
        window.clearTimeout(timer);
        turning = true;
        let outgoing;
        // Turn the old sheet over the new one; controls stay outside the animation.
        if (!reducedMotion.matches && typeof sheet.animate === 'function') {
            outgoing = sheet.cloneNode(true);
            outgoing.classList.add('note-turning-sheet');
            outgoing.setAttribute('aria-hidden', 'true');
            outgoing.style.height = `${sheet.offsetHeight}px`;
            phraseBook.append(outgoing);
        }
        index = (index + 1) % phrases.length;
        copy.setAttribute('aria-live', manual ? 'polite' : 'off');
        copy.querySelector('[lang="en"]').textContent = phrases[index][0];
        copy.querySelector('.note-translation').textContent = phrases[index][1];
        sheet.querySelector('.note-page-number').textContent = `${String(index + 1).padStart(2, '0')} / 06`;
        try {
            if (outgoing) {
                animation = outgoing.animate([
                    { transform: 'rotateY(0deg)', opacity: 1, filter: 'brightness(1)' },
                    { transform: 'rotateY(-65deg)', opacity: 0.9, filter: 'brightness(0.92)', offset: 0.65 },
                    { transform: 'rotateY(-105deg)', opacity: 0, filter: 'brightness(0.85)' }
                ], { duration: 850, easing: 'cubic-bezier(.4,0,.2,1)', fill: 'forwards' });
                await animation.finished;
            }
        } catch {
            // A motion-preference change can cancel the decorative animation.
        } finally {
            outgoing?.remove();
            animation = null;
            turning = false;
            scheduleTurn();
        }
    }

    nextButton.addEventListener('click', () => turnPage(true));
    pauseButton.addEventListener('click', () => {
        paused = !paused;
        updatePauseButton();
        scheduleTurn();
    });
    phraseBook.addEventListener('mouseenter', () => { hovered = true; scheduleTurn(); });
    phraseBook.addEventListener('mouseleave', () => { hovered = false; scheduleTurn(); });
    phraseBook.addEventListener('focusin', () => window.clearTimeout(timer));
    phraseBook.addEventListener('focusout', () => window.setTimeout(scheduleTurn, 0));
    document.addEventListener('visibilitychange', scheduleTurn);
    reducedMotion.addEventListener('change', () => {
        if (reducedMotion.matches) { paused = true; animation?.cancel(); }
        updatePauseButton();
        scheduleTurn();
    });
    phraseBook.querySelector('.note-controls').hidden = false;
    updatePauseButton();
    scheduleTurn();
}
