document.addEventListener('DOMContentLoaded', function () {
    try { initMobileMenu(); } catch (e) { console.error('Mobile menu init error:', e); }
    try { initSidebar(); } catch (e) { console.error('Sidebar init error:', e); }
    try { initDropdowns(); } catch (e) { console.error('Dropdowns init error:', e); }
    try { initQuantityInputs(); } catch (e) { console.error('Quantity inputs init error:', e); }
});

function initMobileMenu() {
    const btn = document.getElementById('mobileMenuBtn');
    const menu = document.getElementById('mobileMenu');
    if (btn && menu) {
        btn.addEventListener('click', function () {
            menu.classList.toggle('hidden');
            const icon = btn.querySelector('span');
            if (icon) {
                icon.textContent = menu.classList.contains('hidden') ? 'menu' : 'close';
            }
        });
    }
}

function initSidebar() {
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');

    if (toggleBtn && sidebar) {
        toggleBtn.addEventListener('click', function () {
            sidebar.classList.toggle('-translate-x-full');
            if (overlay) overlay.classList.toggle('hidden');
        });
    }

    if (overlay) {
        overlay.addEventListener('click', function () {
            sidebar.classList.add('-translate-x-full');
            overlay.classList.add('hidden');
        });
    }
}

function initDropdowns() {
    try {
        document.querySelectorAll('.dropdown-toggle').forEach(btn => {
            btn.addEventListener('click', function (e) {
                e.stopPropagation();
                const menu = this.nextElementSibling;
                const chevron = this.querySelector('.dropdown-chevron');
                if (!menu) return;

                const isOpening = menu.classList.contains('hidden');

                if (isOpening) {
                    menu.classList.remove('hidden', 'cat-dropdown-exit');
                    void menu.offsetWidth;
                    menu.classList.add('cat-dropdown-enter');
                    if (chevron) chevron.classList.add('chevron-rotate');
                } else {
                    if (chevron) {
                        menu.classList.remove('cat-dropdown-enter');
                        menu.classList.add('cat-dropdown-exit');
                        chevron.classList.remove('chevron-rotate');
                        setTimeout(() => {
                            menu.classList.add('hidden');
                            menu.classList.remove('cat-dropdown-exit');
                        }, 150);
                    } else {
                        menu.classList.add('hidden');
                    }
                }

                if (this.hasAttribute('aria-expanded')) {
                    this.setAttribute('aria-expanded', String(!isOpening));
                }
            });
        });
    } catch (e) { console.error('Dropdown toggle bind error:', e); }

    try {
        document.addEventListener('click', function () {
            document.querySelectorAll('.dropdown-menu:not(.hidden)').forEach(menu => {
                const toggle = menu.previousElementSibling;
                const chevron = toggle?.querySelector('.dropdown-chevron');
                menu.classList.add('hidden');
                menu.classList.remove('cat-dropdown-enter', 'cat-dropdown-exit');
                if (chevron) chevron.classList.remove('chevron-rotate');
                if (toggle?.hasAttribute('aria-expanded')) {
                    toggle.setAttribute('aria-expanded', 'false');
                }
            });
        });
    } catch (e) { console.error('Dropdown global close bind error:', e); }
}

function initQuantityInputs() {
    document.querySelectorAll('.qty-minus').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.parentElement.querySelector('.qty-input');
            if (input) {
                const val = parseInt(input.value) || 1;
                if (val > 1) {
                    input.value = val - 1;
                    input.dispatchEvent(new Event('change'));
                }
            }
        });
    });

    document.querySelectorAll('.qty-plus').forEach(btn => {
        btn.addEventListener('click', function () {
            const input = this.parentElement.querySelector('.qty-input');
            const max = parseInt(input?.getAttribute('max') || '999');
            if (input) {
                const val = parseInt(input.value) || 1;
                if (val < max) {
                    input.value = val + 1;
                    input.dispatchEvent(new Event('change'));
                }
            }
        });
    });
}

function previewImage(input, previewId) {
    const preview = document.getElementById(previewId);
    if (!preview || !input.files || !input.files[0]) return;

    const reader = new FileReader();
    reader.onload = function (e) {
        preview.src = e.target.result;
        preview.classList.remove('hidden');
    };
    reader.readAsDataURL(input.files[0]);
}

function previewMultipleImages(input, containerId) {
    const container = document.getElementById(containerId);
    if (!container || !input.files) return;

    container.innerHTML = '';
    Array.from(input.files).forEach(file => {
        const reader = new FileReader();
        reader.onload = function (e) {
            const div = document.createElement('div');
            div.className = 'relative group';
            div.innerHTML = `
                <img src="${e.target.result}" class="w-24 h-24 object-cover rounded-lg border" />
                <button type="button" onclick="this.parentElement.remove()" class="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-5 h-5 flex items-center justify-center text-xs hover:bg-red-600">×</button>
            `;
            container.appendChild(div);
        };
        reader.readAsDataURL(file);
    });
}

function confirmDelete(message) {
    return confirm(message || 'Are you sure you want to delete this item?');
}
