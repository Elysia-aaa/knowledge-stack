(function () {
    'use strict';
    var root = document.documentElement, key = 'knowledge-stack-theme';
    var system = window.matchMedia('(prefers-color-scheme: dark)'), saved = null;
    try { saved = localStorage.getItem(key); } catch (_) { /* Storage may be unavailable. */ }
    var manual = saved === 'light' || saved === 'dark';
    function apply(theme) {
        root.setAttribute('data-theme', theme);
        var meta = document.querySelector('meta[name="theme-color"]');
        if (meta) meta.setAttribute('content', theme === 'dark' ? '#111214' : '#f6f6f4');
        var button = document.getElementById('themeToggle');
        if (!button) return;
        var action = theme === 'dark' ? '切换到白色主题' : '切换到黑色主题';
        button.setAttribute('aria-label', action); button.setAttribute('title', action);
        button.setAttribute('aria-pressed', theme === 'dark' ? 'true' : 'false');
        button.querySelector('.theme-toggle-label').textContent = theme === 'dark' ? '白色' : '黑色';
    }
    apply(manual ? saved : system.matches ? 'dark' : 'light');
    document.addEventListener('DOMContentLoaded', function () {
        apply(root.getAttribute('data-theme'));
        var button = document.getElementById('themeToggle');
        if (button) button.addEventListener('click', function () {
            var theme = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
            manual = true; apply(theme);
            try { localStorage.setItem(key, theme); } catch (_) { /* Theme still works in this page. */ }
        });
        document.querySelectorAll('.main-nav a').forEach(function (item) {
            var url = new URL(item.href, location.href);
            if (url.pathname.toLowerCase() === location.pathname.toLowerCase() &&
                (url.searchParams.get('view') || 'profile') === (new URL(location.href).searchParams.get('view') || 'profile'))
                item.setAttribute('aria-current', 'page');
        });
    });
    system.addEventListener('change', function () { if (!manual) apply(system.matches ? 'dark' : 'light'); });
    window.addEventListener('storage', function (event) {
        if (event.key !== key) return;
        manual = event.newValue === 'light' || event.newValue === 'dark';
        apply(manual ? event.newValue : system.matches ? 'dark' : 'light');
    });
})();
