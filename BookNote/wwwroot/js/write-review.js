const quill = new Quill('#editor-container', {
    theme: 'snow',
    placeholder: 'レビューを入力してください...',
    modules: {
        toolbar: [
            [{ 'header': [1, 2, 3, false] }],
            ['bold', 'italic', 'underline', 'strike'],
            [{ 'list': 'ordered' }, { 'list': 'bullet' }],
            ['blockquote'],
            ['clean']
        ]
    }
});
let selectedRating = 0;
const stars = document.querySelectorAll('.star');
stars.forEach(star => {
    star.addEventListener('click', function () {
        if (window.isPublished) return;
        selectedRating = parseInt(this.getAttribute('data-rating'));
        document.getElementById('rating-value').value = selectedRating;
        updateStars();
    });
    star.addEventListener('mouseenter', function () {
        if (window.isPublished) return;
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
function disableFormInputs() {
    const form = document.getElementById('review-form');
    const inputs = form.querySelectorAll('input, textarea, button');
    inputs.forEach(input => {
        input.disabled = true;
    });
}
let isSubmitting = false;
function prepareFormSubmit(isDraft) {
    if (isSubmitting) {
        return false;
    }
    const htmlContent = quill.root.innerHTML;
    document.getElementById('review-content-html').value = htmlContent;
    if (!isDraft) {
        if (quill.getText().trim().length === 0) {
            alert('レビュー本文を入力してください');
            return false;
        }
    }
    isSubmitting = true;
    setTimeout(() => {
        disableFormInputs();
    }, 0);
    return true;
}
quill.on('text-change', function () {
    document.getElementById('review-content-html').value = quill.root.innerHTML;
});
let initialContent = '';
window.addEventListener('load', function () {
    initialContent = quill.root.innerHTML;
});
document.getElementById('review-form')?.addEventListener('submit', function () {
    isSubmitting = true;
});
window.addEventListener('beforeunload', function (e) {
    if (isSubmitting) {
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
window.addEventListener('DOMContentLoaded', function () {
    if (window.existingReview && window.existingReview.bookIsbn) {
        document.getElementById('hidden-book-isbn').value = window.existingReview.bookIsbn;
        document.getElementById('hidden-book-title').value = window.existingReview.bookTitle;
        document.getElementById('hidden-book-author').value = window.existingReview.bookAuthor;
        document.querySelector('.book-title-display').textContent = window.existingReview.bookTitle;
        document.querySelector('.book-author').textContent = window.existingReview.bookAuthor;
        document.getElementById('selected-book').style.display = 'flex';
        fetchSelectedBookImage(window.existingReview.bookIsbn);
        if (window.existingReview.rating > 0) {
            selectedRating = window.existingReview.rating;
            updateStars();
        }
        if (window.existingReview.contentHtml) {
            quill.root.innerHTML = window.existingReview.contentHtml;
            initialContent = window.existingReview.contentHtml;
        }
    }
    const savedHtml = document.getElementById('review-content-html').value;
    const savedRating = document.getElementById('rating-value').value;
    const savedBookTitle = document.getElementById('hidden-book-title').value;
    const savedBookAuthor = document.getElementById('hidden-book-author').value;
    if (savedHtml && savedHtml.trim() !== '') {
        quill.root.innerHTML = savedHtml;
    }
    if (savedRating) {
        selectedRating = parseInt(savedRating);
        updateStars();
    }
    if (savedBookTitle) {
        document.querySelector('.book-title-display').textContent = savedBookTitle;
        document.querySelector('.book-author').textContent = savedBookAuthor;
        document.getElementById('selected-book').style.display = 'flex';
    }
    if (!window.isPublished) {
        document.getElementById('book-search-button')?.addEventListener('click', function () {
            BookSearchDialog.open((book) => {
                document.querySelector('.book-title-display').textContent = book.title;
                document.querySelector('.book-author').textContent = book.author || '著者不明';
                document.getElementById('hidden-book-isbn').value = book.isbn;
                document.getElementById('hidden-book-title').value = book.title;
                document.getElementById('hidden-book-author').value = book.author || '';
                document.getElementById('hidden-book-publisher').value = book.publisher || '';
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
    }
    if (window.isPublished) {
        document.querySelectorAll('.star').forEach(star => {
            star.style.pointerEvents = 'none';
            star.style.cursor = 'not-allowed';
        });
        const titleInput = document.querySelector('input[name="ReviewInput.Title"]');
        if (titleInput) {
            titleInput.disabled = true;
            titleInput.readOnly = true;
        }
        document.getElementById('hidden-book-isbn').disabled = true;
        document.getElementById('hidden-book-title').disabled = true;
        document.getElementById('hidden-book-author').disabled = true;
        document.getElementById('hidden-book-publisher').disabled = true;
    }
});