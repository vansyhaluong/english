(() => {
    const workspace = document.querySelector('.grammar-workspace');
    const toolbar = workspace?.querySelector('[data-grammar-toolbar]');
    if (!toolbar) return;
    const tabs = [...workspace.querySelectorAll('[role="tab"]')];
    const selectTab = tab => {
        tabs.forEach(item => {
            const selected = item === tab;
            item.setAttribute('aria-selected', String(selected));
            item.tabIndex = selected ? 0 : -1;
            document.getElementById(item.getAttribute('aria-controls')).hidden = !selected;
        });
    };
    toolbar.hidden = false;
    tabs.forEach((tab, index) => {
        tab.addEventListener('click', () => selectTab(tab));
        tab.addEventListener('keydown', event => {
            let next;
            if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') next = tabs[(index + 1) % tabs.length];
            if (event.key === 'Home') next = tabs[0];
            if (event.key === 'End') next = tabs[tabs.length - 1];
            if (next) { event.preventDefault(); selectTab(next); next.focus(); }
        });
    });
    workspace.querySelectorAll('.grammar-outline a, .grammar-current, .grammar-lesson-footer a').forEach(link => {
        link.addEventListener('click', () => selectTab(tabs[0]));
    });
    const typeToggle = workspace.querySelector('.grammar-type-toggle');
    typeToggle.addEventListener('click', () => {
        const plain = workspace.classList.toggle('grammar-plain');
        typeToggle.setAttribute('aria-pressed', String(plain));
        typeToggle.setAttribute('aria-label', plain ? 'Dùng kiểu chữ viết tay' : 'Dùng kiểu chữ thông thường');
    });
})();
