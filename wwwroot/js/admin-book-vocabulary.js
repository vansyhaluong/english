(() => {
    'use strict';
    const editor = document.querySelector('[data-vocabulary-editor]');
    if (!editor) return;
    const vi = editor.dataset.culture === 'vi';
    const text = (vn, en) => vi ? vn : en;
    const composer = editor.querySelector('[data-note-composer]');
    const form = editor.querySelector('[data-vocabulary-form]');
    const rows = editor.querySelector('[data-vocabulary-rows]');
    const add = editor.querySelector('[data-add-note]');
    const cancel = editor.querySelector('[data-cancel-note]');
    const message = editor.querySelector('[data-vocabulary-message]');
    const title = editor.querySelector('[data-note-editor-title]');
    const fields = Object.fromEntries([...composer.querySelectorAll('[data-draft-field]')].map(field => [field.dataset.draftField, field]));
    let editing;
    const hasDraft = () => Object.values(fields).some(field => field.value.trim());
    const key = word => word.trim().replace(/\s+/gu, ' ').toUpperCase();
    function sync() {
        [...rows.children].forEach((row, index) => {
            row.querySelector('[data-note-number]').textContent = index + 1;
            row.classList.toggle('is-editing', row === editing);
            for (const field of row.querySelectorAll('[data-field]')) {
                field.name = `Input.Entries[${index}].${field.dataset.field}`;
                field.removeAttribute('id');
            }
        });
        editor.querySelector('[data-note-count]').textContent = rows.children.length;
        editor.querySelector('[data-vocabulary-empty]').hidden = rows.children.length > 0;
        add.disabled = !editing && rows.children.length >= 200;
        add.textContent = editing ? text('Cập nhật trong danh sách', 'Update in list') : text('+ Thêm vào danh sách', '+ Add to list');
        title.textContent = editing ? text('Sửa ghi chú', 'Edit note') : text('Thêm ghi chú', 'Add note');
        cancel.hidden = !editing && !hasDraft();
        cancel.textContent = editing ? text('Hủy sửa', 'Cancel edit') : text('Xóa nội dung', 'Clear fields');
    }
    function clear() {
        editing = undefined;
        composer.reset();
        for (const field of Object.values(fields)) field.setCustomValidity('');
        sync();
    }
    composer.addEventListener('input', event => {
        event.target.setCustomValidity('');
        cancel.hidden = !editing && !hasDraft();
    });
    composer.addEventListener('submit', event => {
        event.preventDefault();
        if (!editing && rows.children.length >= 200) {
            message.textContent = text('Tối đa 200 ghi chú trong một chương.', 'A chapter can have at most 200 notes.');
            return;
        }
        for (const field of [fields.Word, fields.MeaningVi]) {
            field.setCustomValidity(field.value.trim() ? '' : text('Vui lòng nhập nội dung.', 'Please enter a value.'));
        }
        const duplicate = [...rows.children].some(row => row !== editing && key(row.querySelector('[data-field="Word"]').value) === key(fields.Word.value));
        if (duplicate) fields.Word.setCustomValidity(text('Từ / cụm từ này đã có trong danh sách.', 'This word or phrase is already in the list.'));
        if (!composer.reportValidity()) return;
        const row = editing || editor.querySelector('[data-vocabulary-template]').content.firstElementChild.cloneNode(true);
        for (const [name, field] of Object.entries(fields)) {
            row.querySelector(`[data-field="${name}"]`).value = field.value;
            row.querySelector(`[data-copy-field="${name}"]`).textContent = field.value;
        }
        const wasEditing = Boolean(editing);
        if (!editing) rows.append(row);
        clear();
        message.textContent = wasEditing
            ? text('Đã cập nhật trong danh sách. Bấm Lưu thay đổi để lưu.', 'Updated in the list. Choose Save changes to save.')
            : text('Đã thêm vào danh sách. Bạn có thể nhập ghi chú tiếp theo; bấm Lưu thay đổi để lưu.', 'Added to the list. You can enter another note; choose Save changes to save.');
        fields.Word.focus();
    });
    cancel.addEventListener('click', () => {
        clear();
        message.textContent = '';
        fields.Word.focus();
    });
    rows.addEventListener('click', event => {
        const button = event.target.closest('[data-edit-note], [data-remove-note]');
        if (!button) return;
        const row = button.closest('[data-vocabulary-row]');
        if (button.hasAttribute('data-edit-note')) {
            if (hasDraft()) {
                message.textContent = text('Hãy thêm/cập nhật ghi chú đang nhập hoặc hủy trước khi sửa ghi chú khác.', 'Add/update or cancel the current draft before editing another note.');
                fields.Word.focus();
                return;
            }
            editing = row;
            for (const [name, field] of Object.entries(fields)) {
                field.value = row.querySelector(`[data-field="${name}"]`).value;
                field.setCustomValidity('');
            }
            sync();
            message.textContent = text('Đang sửa ghi chú đã chọn.', 'Editing the selected note.');
            fields.Word.focus();
        } else {
            const focus = row.nextElementSibling?.querySelector('[data-edit-note]') || row.previousElementSibling?.querySelector('[data-edit-note]') || fields.Word;
            row.remove();
            if (editing === row) clear();
            else sync();
            message.textContent = text('Đã xóa khỏi danh sách. Bấm Lưu thay đổi để lưu.', 'Removed from the list. Choose Save changes to save.');
            focus.focus();
        }
    });
    form.addEventListener('submit', event => {
        if (editing || hasDraft()) {
            event.preventDefault();
            message.textContent = text('Bấm Thêm/Cập nhật trong danh sách hoặc hủy phần đang nhập trước khi lưu.', 'Add/update the draft in the list or cancel it before saving.');
            fields.Word.focus();
        }
    });
    sync();
})();
