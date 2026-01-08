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
    document.getElementById('book-search').value = '';
    document.getElementById('hidden-book-title').value = '';
    document.getElementById('hidden-book-author').value = '';
}

// 書籍検索（デモ用）
document.querySelector('.search-small-button')?.addEventListener('click', function () {
    const searchValue = document.getElementById('book-search').value.trim();
    if (searchValue) {
        // デモ用の書籍情報表示
        document.querySelector('.book-title-display').textContent = searchValue;
        document.querySelector('.book-author').textContent = '著者名';
        document.getElementById('selected-book').style.display = 'flex';
    }
});

// Enterキーで書籍検索
document.getElementById('book-search')?.addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        e.preventDefault();
        document.querySelector('.search-small-button').click();
    }
});

// フォーム送信前の準備
function prepareFormSubmit() {
    // エディタの内容をHTMLとしてhidden fieldに設定
    // Markdown変換はサーバー側で実施
    const htmlContent = quill.root.innerHTML;
    document.getElementById('review-content-html').value = htmlContent;

    // 書籍情報をhidden fieldに設定
    const bookTitle = document.querySelector('.book-title-display')?.textContent || '';
    const bookAuthor = document.querySelector('.book-author')?.textContent || '';
    document.getElementById('hidden-book-title').value = bookTitle;
    document.getElementById('hidden-book-author').value = bookAuthor;

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

// バリデーションエラー時の内容復元
window.addEventListener('DOMContentLoaded', function () {
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
});