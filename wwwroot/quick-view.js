document.addEventListener('DOMContentLoaded', function() {
    var modal = document.createElement('div');
    modal.className = 'quick-view-modal';
    modal.innerHTML =
        '<div class="quick-view-overlay"></div>' +
        '<div class="quick-view-content">' +
            '<button class="quick-view-close">&times;</button>' +
            '<div class="quick-view-body"></div>' +
        '</div>';
    document.body.appendChild(modal);

    var overlay = modal.querySelector('.quick-view-overlay');
    var closeBtn = modal.querySelector('.quick-view-close');
    var body = modal.querySelector('.quick-view-body');

    function renderProductInModal(product) {
        var isFav = window.isFavorite ? window.isFavorite(product.id) : false;
        var inStockBadge = product.inStock !== undefined
            ? '<span class="badge ' + (product.inStock ? 'badge-instock' : 'badge-onorder') + '">' +
              (product.inStock ? 'В наличии' : 'Под заказ') + '</span>'
            : '';
        var materialBadge = product.material
            ? '<span class="badge badge-material">' + product.material + '</span>'
            : '';

        body.innerHTML =
            '<div class="quick-view-image">' +
                '<img src="' + product.image + '" alt="' + product.name + '">' +
            '</div>' +
            '<div class="quick-view-info">' +
                '<h2>' + product.name + '</h2>' +
                '<p class="quick-view-category">' +
                    (window.getCategoryName ? window.getCategoryName(product.category) : product.category) +
                    ' ' + materialBadge +
                '</p>' +
                '<p class="quick-view-price">' + product.price.toLocaleString() + ' руб. ' + inStockBadge + '</p>' +
                '<p class="quick-view-description">' + product.description + '</p>' +
                '<div class="quick-view-actions">' +
                    '<a href="/Product/Details/' + product.id + '" class="btn-quick-view-details">Подробнее</a>' +
                    '<button class="btn-quick-view-basket" data-id="' + product.id + '">В корзину</button>' +
                    '<button class="btn-quick-view-favorite' + (isFav ? ' active' : '') + '" data-id="' + product.id + '">' +
                        '<span class="favorite-icon">' + (isFav ? '♥' : '♡') + '</span> В избранное' +
                    '</button>' +
                '</div>' +
            '</div>';

        var basketBtn = body.querySelector('.btn-quick-view-basket');
        if (basketBtn) {
            basketBtn.addEventListener('click', function() {
                if (window.addToBasket) {
                    window.addToBasket(product.id, this);
                }
            });
        }

        var favoriteBtn = body.querySelector('.btn-quick-view-favorite');
        if (favoriteBtn) {
            favoriteBtn.addEventListener('click', function() {
                var btn = this;
                if (window.toggleFavorite) {
                    var result = window.toggleFavorite(product.id);
                    if (result && typeof result.then === 'function') {
                        result.then(function() {
                            updateFavoriteButton(btn, product.id);
                        });
                    } else {
                        updateFavoriteButton(btn, product.id);
                    }
                }
            });
        }
    }

    function updateFavoriteButton(btn, productId) {
        if (!window.isFavorite) return;
        var isFav = window.isFavorite(productId);
        var icon = btn.querySelector('.favorite-icon');
        if (icon) icon.textContent = isFav ? '♥' : '♡';
        btn.classList.toggle('active', isFav);
    }

    function openQuickView(productId) {
        modal.classList.add('active');
        document.body.style.overflow = 'hidden';
        body.innerHTML =
            '<div class="quick-view-loader">' +
                '<div class="spinner"></div>' +
                '<p>Загрузка товара...</p>' +
            '</div>';

        if (window.AjaxAPI) {
            window.AjaxAPI.getProduct(productId)
                .then(function(response) {
                    if (!response.ok) {
                        body.innerHTML =
                            '<div class="quick-view-error">' +
                                '<p>' + (response.error || 'Ошибка загрузки') + '</p>' +
                            '</div>';
                        return;
                    }
                    renderProductInModal(response.product);
                })
                .catch(function() {
                    body.innerHTML =
                        '<div class="quick-view-error">' +
                            '<p>Не удалось загрузить информацию о товаре</p>' +
                        '</div>';
                });
        } else {
            var product = (window.products || []).find(function(p) { return p.id === productId; });
            if (product) {
                renderProductInModal(product);
            } else {
                body.innerHTML = '<div class="quick-view-error"><p>Товар не найден</p></div>';
            }
        }
    }

    function closeQuickView() {
        modal.classList.remove('active');
        document.body.style.overflow = '';
    }

    overlay.addEventListener('click', closeQuickView);
    closeBtn.addEventListener('click', closeQuickView);

    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && modal.classList.contains('active')) {
            closeQuickView();
        }
    });

    window.openQuickView = openQuickView;
});
