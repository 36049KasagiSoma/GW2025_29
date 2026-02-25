const BookSearchDialog = {
    callback: null,
    allBooks: [],
    currentPage: 1,
    itemsPerPage: 10,
    _progressTimer: null,
    open(callback) {
        this.callback = callback;
        this.allBooks = [];
        this.currentPage = 1;
        document.getElementById('bookSearchDialog').style.display = 'block';
        document.getElementById('bookSearchInput').focus();
    },
    close() {
        document.getElementById('bookSearchDialog').style.display = 'none';
        document.getElementById('bookSearchResults').innerHTML = '';
        document.getElementById('bookSearchInput').value = '';
        document.getElementById('bookSearchPagination').style.display = 'none';
        document.getElementById('bookSearchLoading').style.display = 'none';
        this._stopProgress();
        this._setProgress(0);
        this.allBooks = [];
        this.currentPage = 1;
    },
    _setProgress(value) {
        const pct = Math.round(value * 100);
        const fill = document.getElementById('bookSearchProgressFill');
        const label = document.getElementById('bookSearchProgressLabel');
        if (fill) fill.style.width = `${pct}%`;
        if (label) label.textContent = `${pct}%`;
    },
    _startProgressPolling() {
        this._setProgress(0);
        const poll = async () => {
            try {
                const res = await fetch('/api/books/search-progress');
                if (!res.ok) return;
                const data = await res.json();
                this._setProgress(data.progress ?? 0);
            } catch { }
        };
        poll();
        this._progressTimer = setInterval(poll, 300);
    },
    _stopProgress() {
        if (this._progressTimer) {
            clearInterval(this._progressTimer);
            this._progressTimer = null;
        }
    },
    async search() {
        const query = document.getElementById('bookSearchInput').value.trim();
        if (!query) return;
        await fetch('/api/books/search-progress?reset=true');
        const loading = document.getElementById('bookSearchLoading');
        const results = document.getElementById('bookSearchResults');
        const pagination = document.getElementById('bookSearchPagination');
        loading.style.display = 'block';
        results.innerHTML = '';
        pagination.style.display = 'none';
        this._startProgressPolling();
        try {
            const url = `/api/books/search?query=${encodeURIComponent(query)}`;
            const response = await fetch(url);
            const books = await response.json();
            this._stopProgress();
            this._setProgress(1);
            loading.style.display = 'none';
            this.allBooks = books.map(result => result.book).filter(book => book);
            if (this.allBooks.length === 0) {
                results.innerHTML = '<div class="search-empty">検索結果が見つかりませんでした</div>';
                return;
            }
            this.currentPage = 1;
            this.renderPage();
            this.renderPagination();
        } catch (error) {
            this._stopProgress();
            loading.style.display = 'none';
            results.innerHTML = '<div class="search-empty">エラーが発生しました: ' + error.message + '</div>';
        }
    },
    renderPage() {
        const results = document.getElementById('bookSearchResults');
        const startIndex = (this.currentPage - 1) * this.itemsPerPage;
        const endIndex = startIndex + this.itemsPerPage;
        const pageBooks = this.allBooks.slice(startIndex, endIndex);
        results.innerHTML = pageBooks.map(book => `
            <div class="search-result-item" data-isbn="${book.isbn || book.Isbn}" onclick='BookSearchDialog.select(${JSON.stringify(book)})'>
                <div class="search-result-cover">NoImage</div>
                <div class="search-result-info">
                    <div class="search-result-title">${book.title || book.Title}</div>
                    <div class="search-result-author">${book.author || book.Author || '著者不明'}</div>
                    <div class="search-result-isbn">ISBN: ${book.isbn || book.Isbn || '不明'}</div>
                </div>
            </div>
        `).join('');
        this.fetchBookImages();
    },
    renderPagination() {
        const pagination = document.getElementById('bookSearchPagination');
        const totalPages = Math.ceil(this.allBooks.length / this.itemsPerPage);
        if (totalPages <= 1) {
            pagination.style.display = 'none';
            return;
        }
        let paginationHtml = '';
        if (this.currentPage > 1) {
            paginationHtml += `<button class="pagination-btn" onclick="BookSearchDialog.goToPage(${this.currentPage - 1})">‹ 前へ</button>`;
        }
        const startPage = Math.max(1, this.currentPage - 2);
        const endPage = Math.min(totalPages, this.currentPage + 2);
        if (startPage > 1) {
            paginationHtml += `<button class="pagination-btn" onclick="BookSearchDialog.goToPage(1)">1</button>`;
            if (startPage > 2) paginationHtml += `<span class="pagination-ellipsis">...</span>`;
        }
        for (let i = startPage; i <= endPage; i++) {
            const activeClass = i === this.currentPage ? 'active' : '';
            paginationHtml += `<button class="pagination-btn ${activeClass}" onclick="BookSearchDialog.goToPage(${i})">${i}</button>`;
        }
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) paginationHtml += `<span class="pagination-ellipsis">...</span>`;
            paginationHtml += `<button class="pagination-btn" onclick="BookSearchDialog.goToPage(${totalPages})">${totalPages}</button>`;
        }
        if (this.currentPage < totalPages) {
            paginationHtml += `<button class="pagination-btn" onclick="BookSearchDialog.goToPage(${this.currentPage + 1})">次へ ›</button>`;
        }
        pagination.innerHTML = paginationHtml;
        pagination.style.display = 'flex';
    },
    goToPage(page) {
        this.currentPage = page;
        this.renderPage();
        this.renderPagination();
        document.getElementById('bookSearchResults').scrollIntoView({ behavior: 'smooth', block: 'start' });
    },
    select(book) {
        if (this.callback) this.callback(book);
        this.close();
    },
    async fetchBookImages() {
        const resultItems = document.querySelectorAll('.search-result-item');
        const isbnMap = new Map();
        resultItems.forEach(item => {
            const isbn = item.dataset.isbn;
            const bookCover = item.querySelector('.search-result-cover');
            if (!bookCover || bookCover.querySelector('img')) return;
            if (!isbnMap.has(isbn)) isbnMap.set(isbn, []);
            isbnMap.get(isbn).push(bookCover);
        });
        const fetchPromises = Array.from(isbnMap.entries()).map(async ([isbn, bookCovers]) => {
            try {
                const response = await fetch(`/?handler=Image&isbn=${isbn}`);
                if (!response.ok) return;
                const blob = await response.blob();
                const imageUrl = URL.createObjectURL(blob);
                bookCovers.forEach(bookCover => {
                    bookCover.innerHTML = `<img src="${imageUrl}" alt="書影" draggable="false">`;
                });
            } catch (error) {
                console.error(`ISBN ${isbn} の画像取得に失敗しました:`, error);
            }
        });
        await Promise.all(fetchPromises);
    }
};
document.addEventListener('DOMContentLoaded', () => {
    const input = document.getElementById('bookSearchInput');
    if (input) {
        input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') BookSearchDialog.search();
        });
    }
});