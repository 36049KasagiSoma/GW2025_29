function toggleDrawer() {
    const drawer = document.querySelector('.drawer');
    const overlay = document.querySelector('.overlay');
    const hamburger = document.querySelector('.hamburger');
    drawer.classList.toggle('open');
    overlay.classList.toggle('active');
    hamburger.classList.toggle('open');
}