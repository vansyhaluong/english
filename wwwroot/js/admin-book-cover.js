(() => {
    'use strict';
    document.querySelectorAll('[data-cover-preview]').forEach(preview => {
        const image = preview.querySelector('[data-cover-image]');
        const fallback = preview.querySelector('[data-cover-fallback]');
        image.addEventListener('error', () => { image.hidden = true; fallback.hidden = false; });
        image.addEventListener('load', () => { image.hidden = false; fallback.hidden = true; });
        if (image.getAttribute('src') && image.complete && !image.naturalWidth) {
            image.hidden = true;
            fallback.hidden = false;
        }
    });

    const form = document.querySelector('[data-book-cover-form]');
    if (!form) return;
    const input = form.querySelector('[data-cover-input]');
    const image = form.querySelector('[data-cover-image]');
    const fallback = form.querySelector('[data-cover-fallback]');
    const error = form.querySelector('[data-cover-error]');
    const original = image.getAttribute('src');
    let objectUrl;
    function restore() {
        if (objectUrl) URL.revokeObjectURL(objectUrl);
        objectUrl = null;
        if (original) image.src = original;
        else image.removeAttribute('src');
        image.hidden = !original;
        fallback.hidden = !!original;
    }
    input.addEventListener('change', () => {
        restore();
        error.textContent = '';
        input.setCustomValidity('');
        const file = input.files[0];
        if (!file) return;
        // This is only early feedback. The service decodes the actual image bytes.
        const problem = file.size <= 0 || file.size > 2 * 1024 * 1024 ? input.dataset.sizeError
            : !/\.(jpe?g|png|webp)$/i.test(file.name) ? input.dataset.typeError : '';
        if (problem) {
            error.textContent = problem;
            input.setCustomValidity(problem);
            return;
        }
        objectUrl = URL.createObjectURL(file);
        image.src = objectUrl;
        image.hidden = false;
        fallback.hidden = true;
    });
    form.addEventListener('reset', () => {
        restore();
        error.textContent = '';
        input.setCustomValidity('');
    });
    window.addEventListener('pagehide', () => { if (objectUrl) URL.revokeObjectURL(objectUrl); });
})();
