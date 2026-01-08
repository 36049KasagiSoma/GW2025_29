

// スクロール時のアップバー変化
window.addEventListener('scroll', () => {
    const appbar = document.querySelector('.appbar');
    const hamburger = document.querySelector('.hamburger');

    if (window.scrollY > 50) {
        appbar.classList.add('scrolled');
        hamburger.classList.add('scrolled');
    } else {
        appbar.classList.remove('scrolled');
        hamburger.classList.remove('scrolled');
    }
});

// 横スクロールのドラッグ機能
const scrollContainers = document.querySelectorAll('.horizontal-scroll');

scrollContainers.forEach(container => {
    let isDown = false;
    let startX;
    let scrollLeft;
    let hasMoved = false;

    container.addEventListener('mousedown', (e) => {
        isDown = true;
        hasMoved = false;
        startX = e.pageX - container.offsetLeft;
        scrollLeft = container.scrollLeft;
        container.classList.add('dragging');
    });

    container.addEventListener('mouseleave', () => {
        isDown = false;
        container.classList.remove('dragging');
    });

    container.addEventListener('mouseup', () => {
        isDown = false;
        container.classList.remove('dragging');
    });

    container.addEventListener('mousemove', (e) => {
        if (!isDown) return;
        e.preventDefault();
        hasMoved = true;
        const x = e.pageX - container.offsetLeft;
        const walk = (x - startX) * 2;
        container.scrollLeft = scrollLeft - walk;
    });
});