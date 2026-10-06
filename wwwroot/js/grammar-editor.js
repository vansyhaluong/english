(() => {
    const template = document.getElementById('grammar-editor-tools');
    if (!template) return;
    // Edit HTML source only. User-provided HTML is never inserted into the DOM.
    document.querySelectorAll('.grammar-editor').forEach(editor => {
        const textarea = editor.querySelector('textarea');
        const controls = template.content.cloneNode(true);
        controls.querySelectorAll('button').forEach(button => {
            button.addEventListener('click', () => {
                const start = textarea.selectionStart;
                const end = textarea.selectionEnd;
                const selected = textarea.value.slice(start, end);
                const tag = button.dataset.format;
                let replacement;
                if (tag === 'ul' || tag === 'ol') {
                    replacement = `<${tag}>\n${selected.split('\n').map(line => `<li>${line}</li>`).join('\n')}\n</${tag}>`;
                } else if (tag === 'table') {
                    replacement = `<table><thead><tr><th></th><th></th></tr></thead><tbody><tr><td>${selected}</td><td></td></tr></tbody></table>`;
                } else {
                    replacement = `<${tag}>${selected}</${tag}>`;
                }
                textarea.setRangeText(replacement, start, end, 'select');
                textarea.dispatchEvent(new Event('input', { bubbles: true }));
                textarea.focus();
            });
        });
        textarea.before(controls);
    });
})();
