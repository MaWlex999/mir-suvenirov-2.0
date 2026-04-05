window.AjaxAPI = (function () {

    function search(query) {
        var q = (query || '').trim();
        return fetch('/api/search?q=' + encodeURIComponent(q))
            .then(function (r) { return r.json(); });
    }

    function getProduct(id) {
        return fetch('/api/products/' + id)
            .then(function (r) { return r.json(); });
    }

    function getCatalog(filters) {
        var params = new URLSearchParams();
        var data = filters || {};

        Object.keys(data).forEach(function (key) {
            var value = data[key];
            if (value === undefined || value === null) return;
            if (typeof value === 'string' && value.trim() === '') return;
            params.set(key, value);
        });

        return fetch('/api/catalog?' + params.toString())
            .then(function (r) { return r.json(); });
    }

    function addToBasket(productId) {
        return fetch('/api/basket', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ productId: productId })
        }).then(function (r) { return r.json(); });
    }

    function deleteProduct(productId) {
        return fetch('/api/products/' + productId, {
            method: 'DELETE'
        }).then(function (r) { return r.json(); });
    }

    function toggleFavorite(productId) {
        return fetch('/api/favorites', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ productId: productId })
        }).then(function (r) { return r.json(); });
    }

    return {
        search: search,
        getProduct: getProduct,
        getCatalog: getCatalog,
        addToBasket: addToBasket,
        deleteProduct: deleteProduct,
        toggleFavorite: toggleFavorite
    };

})();

if (!window.showToast) {
    window.showToast = function (message, type) {
        var toast = document.createElement('div');
        var normalizedType = type || 'success';
        var icon = normalizedType === 'error' ? '✕' : (normalizedType === 'warning' ? '⚠' : '✓');
        toast.className = 'ajax-toast ajax-toast--' + normalizedType;
        toast.innerHTML =
            '<span class="ajax-toast-content">' +
            '<span class="ajax-toast-icon" aria-hidden="true">' + icon + '</span>' +
            '<span>' + message + '</span>' +
            '</span>';
        document.body.appendChild(toast);

        setTimeout(function () { toast.classList.add('show'); }, 10);

        setTimeout(function () {
            toast.classList.remove('show');
            setTimeout(function () {
                if (toast.parentNode) toast.parentNode.removeChild(toast);
            }, 350);
        }, 2000);
    };
}