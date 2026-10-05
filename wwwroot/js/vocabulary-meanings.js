(() => {
    const form = document.querySelector('[data-meaning-form]');
    if (!form) return;
    const list = form.querySelector('[data-meaning-list]');
    const add = form.querySelector('[data-add-meaning]');
    function renumber() {
        const rows = Array.from(list.children);
        rows.forEach((row, index) => {
            row.querySelector('[data-meaning-id]').name = `Input.Meanings[${index}].Id`;
            const text = row.querySelector('[data-meaning-text]');
            text.name = `Input.Meanings[${index}].MeaningVi`;
            text.id = `meaning-${index}`;
            row.querySelector('[data-meaning-number]').textContent = index + 1;
            row.querySelector('[data-meaning-controls]').hidden = false;
            row.querySelector('[data-up]').disabled = index === 0;
            row.querySelector('[data-down]').disabled = index === rows.length - 1;
            row.querySelector('[data-remove]').disabled = rows.length === 1;
        });
    }
    add.hidden = false;
    add.addEventListener('click', () => {
        list.append(document.querySelector('#meaning-template').content.cloneNode(true));
        renumber();
        list.lastElementChild.querySelector('textarea').focus();
    });
    list.addEventListener('click', event => {
        const button = event.target.closest('button');
        if (!button) return;
        const row = button.closest('[data-meaning-row]');
        if (button.hasAttribute('data-up') && row.previousElementSibling) list.insertBefore(row, row.previousElementSibling);
        if (button.hasAttribute('data-down') && row.nextElementSibling) list.insertBefore(row.nextElementSibling, row);
        if (button.hasAttribute('data-remove') && list.children.length > 1) {
            const next = row.nextElementSibling || row.previousElementSibling;
            row.remove();
            next.querySelector('textarea').focus();
        }
        renumber();
    });
    form.addEventListener('submit', renumber);
    renumber();
})();
