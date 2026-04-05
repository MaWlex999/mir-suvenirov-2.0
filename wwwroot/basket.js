document.addEventListener('DOMContentLoaded', function() {
    const basketContent = document.getElementById('basketContent');
    if (!basketContent) {
        if (window.updateBasketCounter) {
            window.updateBasketCounter();
        }
        return;
    }
    
    const basketContainer = basketContent;
    
    function loadBasket() {
        const basketData = localStorage.getItem('basket');
        return basketData ? JSON.parse(basketData) : [];
    }

    function saveBasket(basket) {
        localStorage.setItem('basket', JSON.stringify(basket));
        updateBasketCounter();
    }

    function updateBasketCounter() {
        const basket = loadBasket();
        const totalItems = basket.reduce((sum, item) => sum + item.quantity, 0);
        const counterElements = document.querySelectorAll('.basket-counter');
        counterElements.forEach(el => {
            if (totalItems > 0) {
                el.textContent = totalItems;
                el.style.display = 'inline-block';
            } else {
                el.style.display = 'none';
            }
        });
    }

    window.updateBasketCounter = updateBasketCounter;

    function renderBasket() {
        const basket = loadBasket();
        
        if (!basket || basket.length === 0) {
            basketContainer.innerHTML = `
                <h1>Корзина</h1>
                <div class="empty-basket">
                    <p class="empty-basket-icon">🛒</p>
                    <p class="empty-basket-text">Ваша корзина пуста</p>
                    <a href="catalog.html" class="btn-to-catalog">Перейти в каталог</a>
                </div>
            `;
            return;
        }

        let total = 0;
        const basketHTML = basket.map((item, index) => {
            const itemTotal = item.price * item.quantity;
            total += itemTotal;
            return `
                <div class="basket-item" data-index="${index}">
                    <div class="basket-item-image">
                        <img src="${item.image}" alt="${item.name}">
                    </div>
                    <div class="basket-item-info">
                        <h3><a href="product.html?id=${item.id}">${item.name}</a></h3>
                        <p class="basket-item-category">${window.getCategoryName ? window.getCategoryName(item.category) : item.category}</p>
                        <p class="basket-item-price">${item.price.toLocaleString()} руб. × ${item.quantity} = ${itemTotal.toLocaleString()} руб.</p>
                    </div>
                    <div class="basket-item-controls">
                        <div class="quantity-controls">
                            <button class="btn-quantity" data-action="decrease" data-index="${index}">−</button>
                            <span class="quantity-value">${item.quantity}</span>
                            <button class="btn-quantity" data-action="increase" data-index="${index}">+</button>
                        </div>
                        <button class="btn-remove" data-index="${index}">Удалить</button>
                    </div>
                </div>
            `;
        }).join('');

        basketContainer.innerHTML = `
            <h1>Корзина</h1>
            <div class="basket-items">
                ${basketHTML}
            </div>
            <div class="basket-summary">
                <div class="basket-total">
                    <p class="basket-total-label">Итого:</p>
                    <p class="basket-total-price">${total.toLocaleString()} руб.</p>
                </div>
                <div class="basket-actions">
                    <button class="btn-clear-basket">Очистить корзину</button>
                    <button class="btn-checkout">Оформить заказ</button>
                </div>
            </div>
        `;

        attachBasketEvents();
    }

    function attachBasketEvents() {
        document.querySelectorAll('.btn-quantity').forEach(btn => {
            btn.addEventListener('click', function() {
                const index = parseInt(this.dataset.index);
                const action = this.dataset.action;
                const basket = loadBasket();
                
                if (action === 'increase') {
                    basket[index].quantity++;
                } else if (action === 'decrease' && basket[index].quantity > 1) {
                    basket[index].quantity--;
                }
                
                saveBasket(basket);
                renderBasket();
            });
        });

        document.querySelectorAll('.btn-remove').forEach(btn => {
            btn.addEventListener('click', function() {
                const index = parseInt(this.dataset.index);
                const basket = loadBasket();
                basket.splice(index, 1);
                saveBasket(basket);
                renderBasket();
            });
        });

        const clearBtn = document.querySelector('.btn-clear-basket');
        if (clearBtn) {
            clearBtn.addEventListener('click', function() {
                if (confirm('Вы уверены, что хотите очистить корзину?')) {
                    saveBasket([]);
                    renderBasket();
                }
            });
        }

        const checkoutBtn = document.querySelector('.btn-checkout');
        if (checkoutBtn) {
            checkoutBtn.addEventListener('click', function() {
                const basket = loadBasket();
                if (basket.length > 0) {
                    alert('Спасибо за заказ! Ваш заказ на сумму ' + 
                          basket.reduce((sum, item) => sum + item.price * item.quantity, 0).toLocaleString() + 
                          ' руб. принят в обработку.');
                    saveBasket([]);
                    renderBasket();
                }
            });
        }
    }

    renderBasket();
    updateBasketCounter();
});

window.addToBasket = function(productId, btnElement) {
    if (window.AjaxAPI) {
        if (btnElement) {
            btnElement.disabled = true;
            btnElement.dataset.originalText = btnElement.textContent;
            btnElement.textContent = '...';
        }

        window.AjaxAPI.addToBasket(productId)
            .then(function(response) {
                if (btnElement) {
                    btnElement.disabled = false;
                    btnElement.textContent = btnElement.dataset.originalText || '🛒';
                }

                if (response.ok) {
                    document.querySelectorAll('.basket-counter').forEach(function(el) {
                        el.textContent = response.totalItems;
                        el.style.display = 'inline-block';
                    });

                    if (window.showToast) {
                        window.showToast('Товар добавлен в корзину', 'success');
                    }
                } else {
                    if (window.showToast) {
                        window.showToast(response.error || 'Ошибка добавления в корзину', 'error');
                    }
                }
            })
            .catch(function() {
                if (btnElement) {
                    btnElement.disabled = false;
                    btnElement.textContent = btnElement.dataset.originalText || '🛒';
                }
                if (window.showToast) {
                    window.showToast('Ошибка соединения с сервером', 'error');
                }
            });

        return;
    }

    if (!window.products) {
        console.error('Товары не загружены');
        return;
    }

    const product = window.products.find(p => p.id === productId);
    if (!product) return;

    const basketData = localStorage.getItem('basket');
    const basket = basketData ? JSON.parse(basketData) : [];
    const existingItem = basket.find(item => item.id === productId);

    if (existingItem) {
        existingItem.quantity++;
    } else {
        basket.push({
            id: product.id,
            name: product.name,
            price: product.price,
            category: product.category,
            image: product.image,
            quantity: 1
        });
    }

    localStorage.setItem('basket', JSON.stringify(basket));

    if (window.updateBasketCounter) {
        window.updateBasketCounter();
    }

    if (window.showToast) {
        window.showToast('Товар добавлен в корзину', 'success');
    }
};

