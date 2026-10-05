(() => {
    const root = document.querySelector('.listening');
    if (!root) return;
    const search = root.querySelector('#listening-search');
    const level = root.querySelector('#listening-level');
    const sort = root.querySelector('#listening-sort');
    const grid = root.querySelector('.listening-grid');
    const cards = Array.from(grid.children);
    const normalize = value => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
    function update() {
        const query = normalize(search.value);
        let count = 0;
        const ordered = [...cards].sort((a, b) => sort.value === 'title'
            ? a.dataset.title.localeCompare(b.dataset.title, 'en')
            : sort.value === 'level' ? a.dataset.level.localeCompare(b.dataset.level)
            : Number(a.dataset.order) - Number(b.dataset.order));
        for (const card of ordered) {
            card.hidden = (level.value !== '' && card.dataset.level !== level.value)
                || !normalize(`${card.dataset.title} ${card.dataset.level}`).includes(query);
            if (!card.hidden) count++;
            grid.append(card);
        }
        root.querySelector('.listening-empty').hidden = count !== 0;
        root.querySelector('.listening-count').textContent = `${count} bài nghe mẫu`;
    }
    search.addEventListener('input', update);
    level.addEventListener('change', update);
    sort.addEventListener('change', update);
    root.querySelector('[data-listening-reset]').addEventListener('click', () => {
        search.value = '';
        level.value = '';
        update();
        search.focus();
    });
    root.querySelector('[data-listening-filters]').hidden = false;
})();
