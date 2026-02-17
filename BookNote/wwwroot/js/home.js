// スクロール時のアップバー変化（PC のみ）
window.addEventListener('scroll', () => {
    if (window.innerWidth <= 768) return;

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

// リサイズ時にスマホ幅になったら scrolled クラスを除去
window.addEventListener('resize', () => {
    if (window.innerWidth <= 768) {
        document.querySelector('.appbar')?.classList.remove('scrolled');
        document.querySelector('.hamburger')?.classList.remove('scrolled');
    }
});

// 横スクロールのドラッグ機能
const scrollContainers = document.querySelectorAll('.horizontal-scroll');
scrollContainers.forEach(container => {
    let isDown = false;
    let startX;
    let scrollLeft;
    let hasMoved = false;
    const dragThreshold = 5; // ドラッグと判定する最小移動距離(px)

    container.addEventListener('mousedown', (e) => {
        isDown = true;
        hasMoved = false;
        startX = e.pageX - container.offsetLeft;
        scrollLeft = container.scrollLeft;
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
        const x = e.pageX - container.offsetLeft;
        const distance = Math.abs(x - startX);

        // 閾値を超えた場合のみドラッグと判定
        if (distance > dragThreshold) {
            e.preventDefault();
            hasMoved = true;
            container.classList.add('dragging');
            const walk = (x - startX) * 2;
            container.scrollLeft = scrollLeft - walk;
        }
    });

    // カードのクリックイベント
    const cards = container.querySelectorAll('.review-card-small');
    cards.forEach(card => {
        card.addEventListener('click', (e) => {
            if (!hasMoved) {
                const reviewId = card.getAttribute('data-review-id');
                window.location.href = `/ReviewDetails/${reviewId}`;
            }
        });
    });
});