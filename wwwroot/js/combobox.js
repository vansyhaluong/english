/* Reuse: load combobox.css/js once, then mark a single select with data-eh-combobox.
 * Optional: data-searchable="true", data-placeholder, data-search-placeholder,
 * data-empty-text, data-theme="light|dark". Keep native options, name/asp-for,
 * labels and validation markup. Theme also follows ancestor data-theme/.light/.dark.
 * After programmatic value changes, dispatch change (or eh-combobox:refresh).
 * Option/attribute mutations and form reset are observed automatically.
 */
(() => {
    'use strict';
    let sequence = 0;
    const english = document.documentElement.lang.startsWith('en');

    function enhance(select) {
        if (select.multiple || select.size > 1 || select.closest('.eh-combobox')) return;
        const id = `eh-combobox-${++sequence}`;
        const searchable = select.dataset.searchable === 'true';
        const wrapper = document.createElement('div');
        wrapper.className = 'eh-combobox';
        if (select.dataset.theme) wrapper.dataset.theme = select.dataset.theme;
        const control = document.createElement('button');
        control.type = 'button';
        control.id = id;
        control.className = 'eh-combobox__control';
        const valueLabel = document.createElement('span');
        valueLabel.className = 'eh-combobox__value';
        const chevron = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        chevron.setAttribute('viewBox', '0 0 24 24');
        chevron.setAttribute('aria-hidden', 'true');
        chevron.setAttribute('focusable', 'false');
        chevron.classList.add('eh-combobox__chevron');
        const path = document.createElementNS('http://www.w3.org/2000/svg', 'path');
        path.setAttribute('d', 'm6 9 6 6 6-6');
        chevron.append(path);
        control.append(valueLabel, chevron);
        control.setAttribute('role', 'combobox');
        control.setAttribute('aria-haspopup', searchable ? 'dialog' : 'listbox');
        control.setAttribute('aria-expanded', 'false');
        control.setAttribute('aria-autocomplete', 'none');
        const popup = document.createElement('div');
        popup.id = `${id}-popup`;
        popup.className = 'eh-combobox__popup';
        if (searchable) popup.setAttribute('role', 'dialog');
        const search = document.createElement('input');
        search.type = 'text';
        search.className = 'eh-combobox__search';
        search.autocomplete = 'off';
        search.spellcheck = false;
        search.placeholder = select.dataset.searchPlaceholder || (english ? 'Search…' : 'Tìm kiếm…');
        search.setAttribute('aria-label', search.placeholder);
        search.setAttribute('role', 'combobox');
        search.setAttribute('aria-autocomplete', 'list');
        search.setAttribute('aria-expanded', 'false');
        const list = document.createElement('div');
        list.id = `${id}-list`;
        list.className = 'eh-combobox__list';
        list.setAttribute('role', 'listbox');
        // The top layer avoids clipping by ancestor overflow and stacking contexts.
        if (typeof popup.showPopover === 'function') popup.setAttribute('popover', 'manual');
        popup.hidden = true;
        popup.append(...(searchable ? [search, list] : [list]));
        control.setAttribute('aria-controls', searchable ? popup.id : list.id);
        search.setAttribute('aria-controls', list.id);
        const labelIds = Array.from(select.labels || [], (label, index) => {
            if (!label.id) label.id = `${id}-label-${index}`;
            return label.id;
        }).join(' ');
        const labelledBy = select.getAttribute('aria-labelledby') || labelIds;
        if (labelledBy) {
            control.setAttribute('aria-labelledby', labelledBy);
            list.setAttribute('aria-labelledby', labelledBy);
            popup.setAttribute('aria-labelledby', labelledBy);
        } else {
            control.setAttribute('aria-label', select.getAttribute('aria-label') || select.name || 'Combobox');
            list.setAttribute('aria-label', control.getAttribute('aria-label'));
            popup.setAttribute('aria-label', control.getAttribute('aria-label'));
        }

        let opened = false, entries = [], active = -1, invalid = false, motion;
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
        function animatePopup(show) {
            motion?.cancel();
            if (reducedMotion.matches || typeof popup.animate !== 'function') return null;
            const shift = popup.dataset.side === 'top' ? '4px' : '-4px';
            const collapsed = { opacity: 0, transform: `translateY(${shift}) scale(.98)` };
            const expanded = { opacity: 1, transform: 'translateY(0) scale(1)' };
            return popup.animate(show ? [collapsed, expanded] : [expanded, collapsed],
                { duration: show ? 140 : 100, easing: 'cubic-bezier(.2,.8,.2,1)', fill: 'both' });
        }
        function positionList() {
            if (!opened) return;
            const rect = control.getBoundingClientRect();
            const viewport = window.visualViewport;
            const top = viewport?.offsetTop || 0;
            const left = viewport?.offsetLeft || 0;
            const bottom = top + (viewport?.height || window.innerHeight);
            const right = left + (viewport?.width || window.innerWidth);
            const gap = 6, margin = 8;
            if (rect.bottom < top || rect.top > bottom) { close(); return; }
            const below = Math.max(0, bottom - rect.bottom - gap - margin);
            const above = Math.max(0, rect.top - top - gap - margin);
            const width = Math.min(rect.width, right - left - margin * 2);
            popup.style.width = `${width}px`;
            const style = getComputedStyle(popup);
            const frame = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom)
                + parseFloat(style.borderTopWidth) + parseFloat(style.borderBottomWidth);
            const desired = list.scrollHeight + frame + (searchable ? search.offsetHeight + parseFloat(style.gap) : 0);
            const flip = below < Math.min(360, desired) && above > below;
            const height = Math.min(360, flip ? above : below);
            popup.dataset.side = flip ? 'top' : 'bottom';
            popup.style.maxHeight = `${height}px`;
            popup.style.left = `${Math.max(left + margin, Math.min(rect.left, right - width - margin))}px`;
            popup.style.top = `${flip ? rect.top - gap - Math.min(desired, height) : rect.bottom + gap}px`;
        }
        const selectedText = () => select.selectedIndex < 0
            ? select.dataset.placeholder || '' : select.options[select.selectedIndex].text;
        function activate(index, scroll = false) {
            active = index;
            entries.forEach((entry, i) => entry.node.classList.toggle('is-active', i === active));
            if (active >= 0) {
                (searchable ? search : control).setAttribute('aria-activedescendant', entries[active].node.id);
                if (scroll) entries[active].node.scrollIntoView({ block: 'nearest' });
            } else (searchable ? search : control).removeAttribute('aria-activedescendant');
        }
        function render(query = '') {
            list.replaceChildren();
            entries = [];
            Array.from(select.options).forEach((option, index) => {
                if (option.hidden || !option.text.toLocaleLowerCase().includes(query.toLocaleLowerCase())) return;
                const item = document.createElement('div');
                item.id = `${id}-option-${index}`;
                item.className = 'eh-combobox__option';
                item.setAttribute('role', 'option');
                item.setAttribute('aria-selected', String(option.selected));
                const disabled = option.disabled || (option.parentElement.tagName === 'OPTGROUP' && option.parentElement.disabled);
                item.setAttribute('aria-disabled', String(disabled));
                item.textContent = option.text;
                const entryIndex = entries.length;
                item.addEventListener('pointermove', () => { if (!disabled) activate(entryIndex); });
                item.addEventListener('pointerdown', event => event.preventDefault());
                item.addEventListener('click', () => { if (!disabled) choose(entryIndex); });
                entries.push({ node: item, option, disabled });
                list.append(item);
            });
            if (!entries.length) {
                const empty = document.createElement('div');
                empty.className = 'eh-combobox__empty';
                empty.setAttribute('role', 'status');
                empty.textContent = select.dataset.emptyText || (english ? 'No matching options' : 'Không có lựa chọn phù hợp');
                list.append(empty);
            }
            activate(entries.findIndex(entry => entry.option.selected && !entry.disabled));
            positionList();
        }
        function sync() {
            invalid = invalid && !select.validity.valid;
            control.disabled = select.matches(':disabled');
            control.setAttribute('aria-required', String(select.required));
            control.setAttribute('aria-invalid', String(invalid || select.classList.contains('input-validation-error') || select.getAttribute('aria-invalid') === 'true'));
            for (const attribute of ['aria-describedby', 'aria-errormessage']) {
                if (select.hasAttribute(attribute)) control.setAttribute(attribute, select.getAttribute(attribute));
                else control.removeAttribute(attribute);
            }
            if (control.disabled && opened) close();
            valueLabel.textContent = selectedText();
            if (opened) render(searchable ? search.value : '');
        }
        function open() {
            if (opened) return;
            sync();
            if (control.disabled) return;
            motion?.cancel();
            opened = true;
            popup.inert = false;
            control.setAttribute('aria-expanded', 'true');
            search.value = '';
            search.setAttribute('aria-expanded', 'true');
            popup.hidden = false;
            if (popup.hasAttribute('popover')) popup.showPopover();
            render();
            motion = animatePopup(true);
            if (searchable) search.focus({ preventScroll: true });
        }
        function close() {
            if (!opened) return;
            opened = false;
            popup.inert = true;
            const hide = () => {
                if (opened) return;
                if (popup.hasAttribute('popover') && popup.matches(':popover-open')) popup.hidePopover();
                popup.hidden = true;
            };
            motion = animatePopup(false);
            if (motion) motion.onfinish = hide; else hide();
            search.setAttribute('aria-expanded', 'false');
            search.removeAttribute('aria-activedescendant');
            control.setAttribute('aria-expanded', 'false');
            control.removeAttribute('aria-activedescendant');
            valueLabel.textContent = selectedText();
        }
        function choose(index) {
            const entry = entries[index];
            if (!entry || entry.disabled || select.matches(':disabled')) return;
            const previous = select.selectedIndex;
            select.selectedIndex = entry.option.index;
            invalid = !select.validity.valid;
            close();
            if (previous !== select.selectedIndex) {
                select.dispatchEvent(new Event('input', { bubbles: true }));
                select.dispatchEvent(new Event('change', { bubbles: true }));
            }
            sync();
            control.focus();
        }
        control.addEventListener('click', () => { if (opened) close(); else open(); });
        control.addEventListener('focus', sync);
        search.addEventListener('input', () => render(search.value));
        function onKeydown(event) {
            if (event.isComposing) return;
            if (event.key === 'Tab') {
                if (opened) { close(); control.focus(); }
                return;
            }
            if (event.key === 'Escape') {
                if (opened) { event.preventDefault(); event.stopPropagation(); close(); control.focus(); }
                return;
            }
            if (event.key === 'Enter' || (event.target === control && event.key === ' ')) {
                event.preventDefault();
                if (opened) choose(active); else open();
                return;
            }
            if ((event.key === 'Home' || event.key === 'End') && (event.target === control || event.ctrlKey)) {
                event.preventDefault();
                open();
                const index = event.key === 'Home'
                    ? entries.findIndex(entry => !entry.disabled)
                    : entries.findLastIndex(entry => !entry.disabled);
                activate(index, true);
                return;
            }
            if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return;
            event.preventDefault();
            const wasOpen = opened;
            open();
            if (!wasOpen && active >= 0) { activate(active, true); return; }
            const step = event.key === 'ArrowDown' ? 1 : -1;
            let next = active < 0 ? (step > 0 ? 0 : entries.length - 1) : active + step;
            while (next >= 0 && next < entries.length && entries[next].disabled) next += step;
            if (next >= 0 && next < entries.length) activate(next, true);
        }
        control.addEventListener('keydown', onKeydown);
        search.addEventListener('keydown', onKeydown);
        wrapper.addEventListener('focusout', event => { if (!wrapper.contains(event.relatedTarget)) close(); });
        document.addEventListener('pointerdown', event => { if (!wrapper.contains(event.target)) close(); });
        window.addEventListener('resize', positionList);
        document.addEventListener('scroll', event => { if (event.target !== list) positionList(); }, true);
        window.visualViewport?.addEventListener('resize', positionList);
        window.visualViewport?.addEventListener('scroll', positionList);
        select.addEventListener('focus', () => control.focus());
        select.addEventListener('change', () => { invalid = !select.validity.valid; sync(); });
        select.addEventListener('eh-combobox:refresh', sync);
        select.addEventListener('invalid', event => {
            event.preventDefault();
            invalid = true;
            sync();
            control.focus();
        });
        if (select.form) {
            select.form.addEventListener('reset', () => setTimeout(() => { invalid = false; close(); sync(); }, 0));
            // Existing forms may reset select.value in a button's click handler.
            select.form.addEventListener('click', () => queueMicrotask(sync));
            select.form.addEventListener('submit', sync);
        }
        new MutationObserver(sync).observe(select, { subtree: true, childList: true, characterData: true, attributes: true });
        // Hide only after the replacement is fully initialized; keep select in the form.
        select.before(wrapper);
        wrapper.append(select, control, popup);
        select.classList.add('eh-combobox__native');
        select.tabIndex = -1;
        select.setAttribute('aria-hidden', 'true');
        sync();
    }
    function initialize() { document.querySelectorAll('select[data-eh-combobox]').forEach(enhance); }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize);
    else initialize();
})();
