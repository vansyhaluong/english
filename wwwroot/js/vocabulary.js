(() => {
    const dialog = document.getElementById('study-dialog');
    if (!dialog || typeof dialog.showModal !== 'function') return;
    const find = selector => dialog.querySelector(selector);
    let words = [];
    let index = 0;
    let opener;
    const canSpeak = 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window;
    function stopAudio() { if (canSpeak) window.speechSynthesis.cancel(); }
    function render() {
        stopAudio();
        const word = words[index];
        find('[data-study-word]').textContent = word.word;
        find('[data-study-ipa]').textContent = word.ipa;
        find('[data-study-meaning]').textContent = word.meaning;
        find('[data-study-pos]').textContent = word.pos;
        find('[data-study-example]').textContent = word.example;
        find('.study-counter').textContent = `Thẻ ${index + 1} / ${words.length}`;
        find('[data-answer]').hidden = true;
        find('[data-reveal]').hidden = false;
        find('[data-previous]').disabled = index === 0;
        find('[data-next]').textContent = index === words.length - 1 ? 'Hoàn thành ✓' : 'Thẻ tiếp →';
        find('[data-speak]').hidden = !canSpeak;
        find('.study-status').textContent = '';
    }
    document.querySelectorAll('[data-study]').forEach(button => {
        button.hidden = false;
        button.addEventListener('click', () => {
            words = Array.from(document.getElementById(`words-${button.dataset.study}`).querySelectorAll('[data-word]'), row => ({
                word: row.querySelector('dt').textContent,
                ipa: row.querySelector('[data-ipa]').textContent,
                pos: row.querySelector('[data-pos]').textContent,
                meaning: row.querySelector('[data-meaning]').textContent,
                example: row.querySelector('[data-example]').textContent
            }));
            opener = button;
            index = 0;
            find('#study-title').textContent = button.dataset.title;
            render();
            dialog.showModal();
            document.body.classList.add('dialog-open');
        });
    });
    find('[data-study-close]').addEventListener('click', () => dialog.close());
    dialog.addEventListener('close', () => {
        stopAudio();
        document.body.classList.remove('dialog-open');
        opener?.focus();
    });
    find('[data-reveal]').addEventListener('click', () => {
        find('[data-answer]').hidden = false;
        find('[data-reveal]').hidden = true;
        find('[data-next]').focus();
    });
    find('[data-previous]').addEventListener('click', () => { if (index > 0) { index--; render(); } });
    find('[data-next]').addEventListener('click', () => {
        if (index < words.length - 1) { index++; render(); }
        else { stopAudio(); find('.study-status').textContent = 'Bạn đã đi hết bộ thẻ! Có thể xem lại hoặc đóng để chọn bộ khác.'; }
    });
    find('[data-speak]').addEventListener('click', () => {
        if (!canSpeak) return;
        stopAudio();
        const utterance = new SpeechSynthesisUtterance(words[index].word);
        utterance.lang = 'en-GB';
        utterance.rate = .85;
        utterance.onerror = event => {
            if (event.error !== 'canceled' && event.error !== 'interrupted') find('.study-status').textContent = 'Trình duyệt chưa phát được giọng tiếng Anh. Bạn có thể xem phiên âm để ôn tập.';
        };
        window.speechSynthesis.speak(utterance);
    });
})();
