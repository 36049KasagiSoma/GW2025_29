
// Quillエディタの初期化
const quill = new Quill('#editor-container', {
    theme: 'snow',
    placeholder: 'レビューを入力してください...',
    modules: {
        toolbar: [
            [{ 'header': [1, 2, 3, false] }],
            ['bold', 'italic', 'underline', 'strike'],
            [{ 'list': 'ordered' }, { 'list': 'bullet' }],
            ['blockquote', 'code-block'],
            ['link'],
            ['clean']
        ]
    }
});

// 評価星の処理
let selectedRating = 0;
const stars = document.querySelectorAll('.star');

stars.forEach(star => {
    star.addEventListener('click', function () {
        selectedRating = parseInt(this.getAttribute('data-rating'));
        document.getElementById('rating-value').value = selectedRating;
        updateStars();
    });

    star.addEventListener('mouseenter', function () {
        const rating = parseInt(this.getAttribute('data-rating'));
        highlightStars(rating);
    });
});

document.querySelector('.rating-container').addEventListener('mouseleave', function () {
    highlightStars(selectedRating);
});

function updateStars() {
    highlightStars(selectedRating);
}

function highlightStars(rating) {
    stars.forEach((star, index) => {
        if (index < rating) {
            star.classList.add('active');
            star.textContent = '★';
        } else {
            star.classList.remove('active');
            star.textContent = '☆';
        }
    });
}

// 書籍選択の削除
function removeBook() {
    document.getElementById('selected-book').style.display = 'none';
    document.getElementById('hidden-book-isbn').value = '';
    document.getElementById('hidden-book-title').value = '';
    document.getElementById('hidden-book-author').value = '';
    document.getElementById('hidden-book-publisher').value = '';
    document.getElementById('selected-book-image').src = '';
    document.getElementById('selected-book-image').style.display = 'none';
    document.getElementById('selected-book-noimage').style.display = 'block';
}

// フォーム送信前の準備
function prepareFormSubmit() {
    // エディタの内容をHTMLとしてhidden fieldに設定
    // Markdown変換はサーバー側で実施
    const htmlContent = quill.root.innerHTML;
    document.getElementById('review-content-html').value = htmlContent;

    // クライアント側の基本バリデーション
    if (quill.getText().trim().length === 0) {
        alert('レビュー本文を入力してください');
        return false;
    }

    return true;
}

// エディタの内容変更時の処理（オプション）
quill.on('text-change', function () {
    // 内容が変更されたらhidden fieldを更新
    document.getElementById('review-content-html').value = quill.root.innerHTML;
});

// ページ離脱時の確認（未保存の変更がある場合）
let initialContent = '';
let formSubmitting = false;

window.addEventListener('load', function () {
    initialContent = quill.root.innerHTML;
});

// フォーム送信時はページ離脱警告を出さない
document.getElementById('review-form')?.addEventListener('submit', function () {
    formSubmitting = true;
});

window.addEventListener('beforeunload', function (e) {
    if (formSubmitting) {
        return;
    }

    const currentContent = quill.root.innerHTML;
    const title = document.querySelector('input[name="ReviewInput.Title"]')?.value.trim();

    if (currentContent !== initialContent || title) {
        e.preventDefault();
        e.returnValue = '';
    }
});

async function fetchSelectedBookImage(isbn) {
    const bookCover = document.querySelector('#selected-book .book-cover-small');
    const noImage = document.getElementById('selected-book-noimage');
    const imageElement = document.getElementById('selected-book-image');

    if (!isbn || !bookCover) return;

    try {
        const response = await fetch(`/?handler=Image&isbn=${isbn}`);
        if (!response.ok) return;

        const blob = await response.blob();
        const imageUrl = URL.createObjectURL(blob);

        imageElement.src = imageUrl;
        imageElement.style.display = 'block';
        noImage.style.display = 'none';
    } catch (error) {
        console.error(`ISBN ${isbn} の書影取得に失敗しました:`, error);
    }
}

// バリデーションエラー時の内容復元
window.addEventListener('DOMContentLoaded', function () {
    // 既存レビューデータの読み込み
    if (window.existingReview && window.existingReview.bookIsbn) {
        // 書籍情報を設定
        document.getElementById('hidden-book-isbn').value = window.existingReview.bookIsbn;
        document.getElementById('hidden-book-title').value = window.existingReview.bookTitle;
        document.getElementById('hidden-book-author').value = window.existingReview.bookAuthor;

        document.querySelector('.book-title-display').textContent = window.existingReview.bookTitle;
        document.querySelector('.book-author').textContent = window.existingReview.bookAuthor;
        document.getElementById('selected-book').style.display = 'flex';
        fetchSelectedBookImage(window.existingReview.bookIsbn);

        // 評価を設定
        if (window.existingReview.rating > 0) {
            updateStars(window.existingReview.rating);
        }

        // エディタコンテンツを設定
        if (window.existingReview.contentHtml) {
            quill.root.innerHTML = window.existingReview.contentHtml;
            initialContent = window.existingReview.contentHtml; 
        }
    }

    const savedHtml = document.getElementById('review-content-html').value;
    const savedRating = document.getElementById('rating-value').value;
    const savedBookTitle = document.getElementById('hidden-book-title').value;
    const savedBookAuthor = document.getElementById('hidden-book-author').value;

    // エディタ内容の復元
    if (savedHtml && savedHtml.trim() !== '') {
        quill.root.innerHTML = savedHtml;
    }

    // 評価の復元
    if (savedRating) {
        selectedRating = parseInt(savedRating);
        updateStars();
    }

    // 書籍情報の復元
    if (savedBookTitle) {
        document.querySelector('.book-title-display').textContent = savedBookTitle;
        document.querySelector('.book-author').textContent = savedBookAuthor;
        document.getElementById('selected-book').style.display = 'flex';
    }

    // 書籍検索ダイアログを開く
    document.getElementById('book-search-button')?.addEventListener('click', function () {
        BookSearchDialog.open((book) => {
            // 選択された書籍情報を設定
            document.querySelector('.book-title-display').textContent = book.title;
            document.querySelector('.book-author').textContent = book.author || '著者不明';
            document.getElementById('hidden-book-isbn').value = book.isbn;
            document.getElementById('hidden-book-title').value = book.title;
            document.getElementById('hidden-book-author').value = book.author || '';
            document.getElementById('hidden-book-publisher').value = book.publisher || '';


            // 書籍画像の表示
            const bookImage = document.getElementById('selected-book-image');
            const noImage = document.getElementById('selected-book-noimage');
            if (book.imageUrl) {
                bookImage.src = book.imageUrl;
                bookImage.alt = book.title;
                bookImage.style.display = 'block';
                noImage.style.display = 'none';
            } else {
                bookImage.style.display = 'none';
                noImage.style.display = 'block';
            }

            document.getElementById('selected-book').style.display = 'flex';

            const imageElement = document.getElementById('selected-book-image');
            noImage.style.display = 'block';
            imageElement.style.display = 'none';

            fetchSelectedBookImage(book.isbn);
        });
    });
});

