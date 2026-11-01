/*
 * [Mahjar home-motion] slim sticky masthead (site-wide, from 1001px).
 * The header + menu bar are wrapped in #mhx-masthead (Views/Shared/_Root.cshtml). This script makes the
 * wrapper position:sticky with a NEGATIVE top equal to the height of everything above the menu bar, so the
 * utility row and the logo row scroll away naturally and only the slim menu bar stays (with a mini logo).
 * Once compact it turns to glass (.is-compact), hides while scrolling down (.is-hidden) and comes back on
 * any scroll up, on hover and when something inside it gets focus. The compact bar keeps the two things a
 * shopper reaches for at its end: a search button (back to the header search) and the cart with its live count
 * (mirrored from the header's own .cart-qty, which AjaxCart updates). Styles: the [Mahjar home-motion] tail of
 * styles.css / styles.rtl.css. Below 1001px, or without this script, the header is the plain static one.
 * One rAF-throttled passive scroll listener; transform/opacity only.
 */
(function () {
    'use strict';

    var doc = document, win = window, root = doc.documentElement;
    var mast = doc.getElementById('mhx-masthead');
    if (!mast || !win.matchMedia) return;

    var menu = mast.querySelector('.header-menu');
    if (!menu) return;

    var desktop = win.matchMedia('(min-width: 1001px)');
    var on = false, ticking = false, locked = false;
    var lastY = win.pageYOffset || root.scrollTop;
    var naturalTop = 0, collapseAt = 0, fullHeight = 0;
    var raf = win.requestAnimationFrame || function (fn) { return setTimeout(fn, 16); };

    function scrollY() { return win.pageYOffset || root.scrollTop; }

    var reduce = !!win.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var SVG = 'http://www.w3.org/2000/svg';

    function icon(paths) {
        var svg = doc.createElementNS(SVG, 'svg');
        svg.setAttribute('viewBox', '0 0 24 24');
        svg.setAttribute('aria-hidden', 'true');
        svg.setAttribute('focusable', 'false');
        paths.forEach(function (d) {
            var p = doc.createElementNS(SVG, 'path');
            p.setAttribute('d', d);
            svg.appendChild(p);
        });
        return svg;
    }

    /* the compact bar's end: search + cart (built from the header's own controls, so no extra markup or strings) */
    function buildEnd() {
        var end = doc.createElement('div');
        end.className = 'mhx-masthead__end';

        var input = doc.getElementById('small-searchterms');
        var form = doc.getElementById('small-search-box-form');
        if (input && form) {
            var sb = doc.createElement('button');
            sb.type = 'button';
            sb.className = 'mhx-masthead__icon mhx-masthead__search';
            var sbText = form.querySelector('.search-box-button');
            sb.setAttribute('aria-label', ((sbText && sbText.textContent) || input.getAttribute('placeholder') || '').trim());
            sb.appendChild(icon(['M10.5 4a6.5 6.5 0 1 1 0 13a6.5 6.5 0 1 1 0-13z', 'M15.3 15.3l5 5']));
            sb.addEventListener('click', function () {
                win.scrollTo({ top: 0, behavior: reduce ? 'auto' : 'smooth' });
                setTimeout(function () {
                    try { input.focus({ preventScroll: true }); } catch (err) { input.focus(); }
                }, reduce ? 0 : 520);
            });
            end.appendChild(sb);
        }

        var cart = doc.querySelector('.header-links .ico-cart');
        if (cart) {
            var a = doc.createElement('a');
            a.href = cart.getAttribute('href');
            a.className = 'mhx-masthead__icon mhx-masthead__cart';
            a.appendChild(icon(['M5.5 8.5h13l-1 11.5h-11z', 'M9 8.5V7a3 3 0 0 1 6 0v1.5']));
            var qty = doc.createElement('span');
            qty.className = 'mhx-masthead__qty';
            qty.setAttribute('aria-hidden', 'true');
            a.appendChild(qty);
            var label = cart.querySelector('.cart-label');
            var sync = function () {
                var src = cart.querySelector('.cart-qty');
                var text = src ? src.textContent.replace(/[()\s]/g, '') : '';
                qty.textContent = text;
                qty.hidden = !text || /^[0٠]$/.test(text);
                a.setAttribute('aria-label', ((label ? label.textContent : '') + ' ' + (src ? src.textContent : '')).trim());
            };
            sync();
            if ('MutationObserver' in win) new MutationObserver(sync).observe(cart, { childList: true, characterData: true, subtree: true });
            end.appendChild(a);
        }

        if (end.childNodes.length) mast.appendChild(end);
    }
    buildEnd();

    function measure() {
        if (!desktop.matches) { disable(); return; }
        /* the masthead's place in the flow: right under whatever precedes it (the admin bar, or the page top) */
        var prev = mast.previousElementSibling;
        naturalTop = prev ? prev.getBoundingClientRect().bottom + scrollY() : mast.parentNode.getBoundingClientRect().top + scrollY();
        collapseAt = menu.offsetTop;                 /* the utility + logo rows above the menu bar */
        fullHeight = mast.offsetHeight;
        mast.style.setProperty('--mhx-mast-top', (-collapseAt) + 'px');
        mast.style.setProperty('--mhx-menu-h', menu.offsetHeight + 'px');
        if (!on) {
            on = true;
            mast.classList.add('mhx-masthead--sticky');
        }
        update();
    }

    function disable() {
        if (!on) return;
        on = false;
        mast.classList.remove('mhx-masthead--sticky', 'is-compact', 'is-hidden');
        mast.style.removeProperty('--mhx-mast-top');
    }

    function update() {
        ticking = false;
        if (!on) return;
        var y = scrollY();
        var compact = y > naturalTop + collapseAt - 1;
        mast.classList.toggle('is-compact', compact);

        var dy = y - lastY;
        if (!compact || locked || y < naturalTop + fullHeight + 60) {
            mast.classList.remove('is-hidden');
        } else if (dy > 6) {
            mast.classList.add('is-hidden');
        } else if (dy < -6) {
            mast.classList.remove('is-hidden');
        }
        if (Math.abs(dy) > 6) lastY = y;
    }

    function onScroll() {
        if (!ticking) { ticking = true; raf(update); }
    }

    /* never hide under the visitor's hand or keyboard (flyout cart, notifications, menus) */
    mast.addEventListener('mouseenter', function () { locked = true; mast.classList.remove('is-hidden'); });
    mast.addEventListener('mouseleave', function () { locked = false; });
    mast.addEventListener('focusin', function () { locked = true; mast.classList.remove('is-hidden'); });
    mast.addEventListener('focusout', function (e) { if (!e.relatedTarget || !mast.contains(e.relatedTarget)) locked = false; });

    win.addEventListener('scroll', onScroll, { passive: true });
    win.addEventListener('resize', function () { raf(measure); }, { passive: true });
    win.addEventListener('load', measure);
    if (desktop.addEventListener) desktop.addEventListener('change', measure);
    else if (desktop.addListener) desktop.addListener(measure);

    measure();
})();
