document.addEventListener('DOMContentLoaded', function() {
    var serverFavorites = null;

    function loadFavorites() {
        if (serverFavorites) {
            return Array.from(serverFavorites);
        }
        var favoritesData = localStorage.getItem('favorites');
        return favoritesData ? JSON.parse(favoritesData) : [];
    }

    function refreshFavoritesFromServer() {
        if (!window.AjaxAPI) return Promise.resolve();
        return fetch('/api/favorites')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                if (data && data.ok && Array.isArray(data.items)) {
                    serverFavorites = new Set(data.items.map(function(item) { return item.productId; }));
                }
            })
            .catch(function() {});
    }

    function updateFavoriteButtons() {
        var favorites = loadFavorites();
        document.querySelectorAll('.favorite-btn, .btn-quick-view-favorite').forEach(function(btn) {
            var productId = parseInt(btn.dataset.id || btn.dataset.productId);
            if (!productId) return;

            var isFav = favorites.includes(productId);
            var icon = btn.querySelector('.favorite-icon, .favorite-btn-icon');
            if (icon) icon.textContent = isFav ? '♥' : '♡';
            btn.classList.toggle('active', isFav);
            btn.title = isFav ? 'Удалить из избранного' : 'Добавить в избранное';
        });
    }

    window.updateFavoriteButtonsGlobal = updateFavoriteButtons;

    function showNotification(message) {
        if (window.showToast) {
            window.showToast(message, 'success');
            return;
        }
        var notification = document.createElement('div');
        notification.className = 'favorite-notification';
        notification.textContent = message;
        document.body.appendChild(notification);
        setTimeout(function() { notification.classList.add('show'); }, 10);
        setTimeout(function() {
            notification.classList.remove('show');
            setTimeout(function() {
                if (notification.parentNode) notification.parentNode.removeChild(notification);
            }, 300);
        }, 2000);
    }

    window.toggleFavorite = function(productId) {
        if (window.AjaxAPI) {
            return window.AjaxAPI.toggleFavorite(productId)
                .then(function(response) {
                    if (response.ok) {
                        if (!serverFavorites) serverFavorites = new Set();
                        if (response.added) {
                            serverFavorites.add(productId);
                        } else {
                            serverFavorites.delete(productId);
                        }
                        updateFavoriteButtons();
                        showNotification(response.added ? 'Добавлено в избранное' : 'Удалено из избранного');
                    }
                    return response.added;
                });
        }

        var favorites = loadFavorites();
        var index = favorites.indexOf(productId);
        var added;

        if (index > -1) {
            favorites.splice(index, 1);
            added = false;
        } else {
            favorites.push(productId);
            added = true;
        }

        localStorage.setItem('favorites', JSON.stringify(favorites));
        updateFavoriteButtons();
        showNotification(added ? 'Добавлено в избранное' : 'Удалено из избранного');
        return added;
    };

    window.isFavorite = function(productId) {
        return loadFavorites().includes(productId);
    };

    document.addEventListener('click', function(e) {
        var favoriteBtn = e.target.closest('.favorite-btn');
        if (!favoriteBtn) return;

        e.preventDefault();
        var productId = parseInt(favoriteBtn.dataset.id || favoriteBtn.dataset.productId);
        if (!productId) return;

        favoriteBtn.disabled = true;
        var result = window.toggleFavorite(productId);

        if (result && typeof result.then === 'function') {
            result.then(function() {
                favoriteBtn.disabled = false;
            }).catch(function() {
                favoriteBtn.disabled = false;
            });
        } else {
            favoriteBtn.disabled = false;
        }
    });

    refreshFavoritesFromServer().then(function() {
        updateFavoriteButtons();
    });
});
