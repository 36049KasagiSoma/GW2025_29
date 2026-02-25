document.addEventListener('DOMContentLoaded', () => {
    initCarousels();
    initStarRatings();
    setupRecommendCardClicks();
    loadRecommendBookImages();
    loadRecommendUserIcons();
});
function initCarousels() {
    document.querySelectorAll('.carousel-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            const targetId = btn.dataset.target;
            const track = document.getElementById(targetId);
            if (!track) return;
            const cardWidth = track.querySelector('.carousel-card')?.offsetWidth ?? 220;
            const scrollAmount = (cardWidth + 20) * 2;
            if (btn.classList.contains('carousel-btn-next')) {
                track.scrollBy({ left: scrollAmount, behavior: 'smooth' });
            } else {
                track.scrollBy({ left: -scrollAmount, behavior: 'smooth' });
            }
        });
    });
}
function initStarRatings() {
    document.querySelectorAll('.masonry-stars[data-rating]').forEach(el => {
        const rating = parseInt(el.dataset.rating) || 0;
        el.textContent = '★'.repeat(rating) + '☆'.repeat(5 - rating);
    });
}
function setupRecommendCardClicks() {
    document.querySelectorAll('[data-review-id]').forEach(card => {
        const reviewId = card.dataset.reviewId;
        if (!reviewId) return;
        card.style.cursor = 'pointer';
        card.addEventListener('click', () => {
            window.location.href = `/ReviewDetails/${reviewId}`;
        });
    });
}
async function loadRecommendBookImages() {
    const coverSelectors = [
        '.carousel-cover',
        '.featured-cover',
        '.small-cover',
        '.masonry-cover',
        '.ranking-cover'
    ];
    const cards = document.querySelectorAll('[data-isbn]');
    const isbnMap = new Map();
    cards.forEach(card => {
        const isbn = card.dataset.isbn;
        if (!isbn) return;
        const cover = coverSelectors.reduce((found, sel) => found || card.querySelector(sel), null);
        if (!cover || cover.querySelector('img:not(.loading-gif)')) return;
        if (!isbnMap.has(isbn)) isbnMap.set(isbn, []);
        isbnMap.get(isbn).push(cover);
    });
    isbnMap.forEach(covers => covers.forEach(cover => {
        cover.style.display = 'flex';
        cover.style.alignItems = 'center';
        cover.style.justifyContent = 'center';
        cover.innerHTML = `<img src="/image/loadImage.gif" alt="読み込み中" class="loading-gif" draggable="false" style="width:32px;height:32px;object-fit:contain;">`;
    }));
    await Promise.all(Array.from(isbnMap.entries()).map(async ([isbn, covers]) => {
        try {
            const res = await fetch(`/?handler=Image&isbn=${isbn}`);
            if (!res.ok) { covers.forEach(c => { c.innerHTML = ''; }); return; }
            const url = URL.createObjectURL(await res.blob());
            covers.forEach(c => {
                c.innerHTML = `<img src="${url}" alt="書影" draggable="false" style="width:100%;height:100%;object-fit:cover;">`;
            });
        } catch (e) {
            covers.forEach(c => { c.innerHTML = ''; });
        }
    }));
}
async function loadRecommendUserIcons() {
    const iconSelectors = [
        '.carousel-icon',
        '.featured-icon',
        '.small-icon',
        '.masonry-icon',
        '.ranking-icon'
    ];
    const allIcons = iconSelectors.flatMap(sel => Array.from(document.querySelectorAll(sel)));
    const publicIdMap = new Map();
    allIcons.forEach(icon => {
        const publicId = icon.dataset.publicId;
        if (!publicId || icon.querySelector('img:not(.loading-gif)')) return;
        if (!publicIdMap.has(publicId)) publicIdMap.set(publicId, []);
        publicIdMap.get(publicId).push(icon);
    });
    publicIdMap.forEach(icons => icons.forEach(icon => {
        icon.style.display = 'flex';
        icon.style.alignItems = 'center';
        icon.style.justifyContent = 'center';
        icon.innerHTML = `<img src="/image/loadImage.gif" alt="読み込み中" class="loading-gif" draggable="false" style="width:16px;height:16px;object-fit:contain;">`;
    }));
    await Promise.all(Array.from(publicIdMap.entries()).map(async ([publicId, icons]) => {
        try {
            const res = await fetch(`/?handler=UserIcon&publicId=${publicId}`);
            if (!res.ok) { icons.forEach(i => { i.innerHTML = ''; }); return; }
            const url = URL.createObjectURL(await res.blob());
            icons.forEach(i => {
                i.innerHTML = `<img src="${url}" alt="ユーザーアイコン" draggable="false" style="width:100%;height:100%;object-fit:cover;">`;
            });
        } catch (e) {
            icons.forEach(i => { i.innerHTML = ''; });
        }
    }));
}