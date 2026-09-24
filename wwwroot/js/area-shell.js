const menuToggle = document.querySelector('[data-area-menu-toggle]');
const menuClose = document.querySelector('[data-area-menu-close]');

if (menuToggle && menuClose) {
    const setMenuOpen = open => {
        document.body.classList.toggle('area-menu-open', open);
        menuToggle.setAttribute('aria-expanded', String(open));
    };

    menuToggle.addEventListener('click', () => {
        setMenuOpen(menuToggle.getAttribute('aria-expanded') !== 'true');
    });
    menuClose.addEventListener('click', () => setMenuOpen(false));
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            setMenuOpen(false);
            menuToggle.focus();
        }
    });
}
