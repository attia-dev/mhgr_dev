/*
 * [Mahjar home-motion] MHGR "Living Home" — the storefront home page motion.
 * Vanilla, no library, loaded from Views/Home/Index.cshtml (footer bundle). Companion of
 * Themes/DefaultClean/Content/css/mahjar.home.css. Everything animates with transform/opacity; there
 * is ONE rAF-throttled, passive scroll handler; prefers-reduced-motion gets a still page (no autoplay,
 * no parallax, no Ken Burns) and a hero that cross-fades only when the visitor asks for another slide.
 *
 *  - hero carousel: 1.2s cross-fade, progress-bar pagination (the bar's CSS animation IS the timer),
 *    prev/next + RTL-aware arrow keys, pointer swipe, pause on hover/focus of the text or controls,
 *    when the tab is hidden and when the hero is off-screen; optional video on wide screens only
 *  - scroll: hero parallax + text fade, Feature-band parallax, the progress hairline
 *  - pointer parallax of the Spotlight stage (fine pointers only)
 *  - the visitor's Pause stops ALL of the hero's motion (rotation, stage loops, Ken Burns, cue, video);
 *    once scrolled away, the faded hero text and controls leave the tab order and take no clicks
 *  - ONE reveal system (sections, headings, staggered grids, band wipes) replacing the old two
 */
(function () {
    'use strict';

    var doc = document, win = window, root = doc.documentElement;
    if (!root.classList.contains('html-home-page')) return;

    var rtl = (root.getAttribute('dir') || '').toLowerCase() === 'rtl';
    var reduce = !!(win.matchMedia && win.matchMedia('(prefers-reduced-motion: reduce)').matches);
    var hasIO = 'IntersectionObserver' in win;
    var finePointer = !!(win.matchMedia && win.matchMedia('(hover: hover) and (pointer: fine)').matches);
    var motion = !reduce && hasIO;
    var each = function (list, fn) { Array.prototype.forEach.call(list, fn); };
    var raf = win.requestAnimationFrame || function (fn) { return setTimeout(fn, 16); };

    if (motion) root.classList.add('mhx-motion', 'mhx-reveal');

    /* the per-frame custom properties are registered NON-inheriting and written on the few elements that use them,
       so a pointer move or a scroll frame restyles those elements only — never the whole hero subtree (an inherited
       variable on the hero root invalidated every hidden slide's rings and stage layers on each write) */
    function registerVar(name, initial) {
        try {
            if (win.CSS && CSS.registerProperty) CSS.registerProperty({ name: name, syntax: '<number>', inherits: false, initialValue: initial });
        } catch (err) { /* already registered / unsupported: the plain (inherited) variable still works */ }
    }
    registerVar('--mhx-px', '0');
    registerVar('--mhx-py', '0');
    registerVar('--mhx-sy', '0');
    registerVar('--mhx-fade', '1');
    var lastY = win.pageYOffset || root.scrollTop || 0; /* the scroll offset, kept by the scroll handler */

    /* ---------- full-bleed width: 100vw minus the classic scrollbar ---------- */
    function setScrollbarWidth() {
        var sbw = win.innerWidth - root.clientWidth;
        root.style.setProperty('--mhx-sbw', (sbw > 0 && sbw < 40 ? sbw : 0) + 'px');
    }
    setScrollbarWidth();

    /* =====================================================================================
       HERO CAROUSEL
       ===================================================================================== */
    var hero = doc.getElementById('mhx-hero');
    var heroApi = null;
    var heroTop = 0; /* the hero's page offset: its scroll parallax starts once its top edge has left the viewport */
    var heroBox = { left: 0, w: 0, h: 0 }; /* measured in size() (load, resize) — never per pointer move */
    var heroScrollEls = []; /* the elements carrying --mhx-sy / --mhx-fade */
    var heroFaded = false;

    function initHero() {
        if (!hero) return null;
        var slides = hero.querySelectorAll('.mhx-slide');
        var n = slides.length;
        if (!n) return null;

        var bars = hero.querySelectorAll('.mhx-bar');
        var curEl = hero.querySelector('.mhx-hero__cur');
        var toggle = hero.querySelector('.mhx-hero__toggle');
        var live = hero.querySelector('.mhx-sr');
        var interval = parseInt(hero.getAttribute('data-interval'), 10) || 6500;
        var cur = 0;
        var leaveTimers = {};
        var pauses = {};
        var auto = !reduce && n > 1;
        var videoOk = !reduce && !!(win.matchMedia && win.matchMedia('(min-width: 641px)').matches)
            && !(navigator.connection && navigator.connection.saveData);

        hero.style.setProperty('--mhx-interval', interval + 'ms');
        /* the inline hero script normally arms the first slide's intro itself (after the display fonts load) */
        var introArmed = hero.classList.contains('mhx-ready');
        hero.classList.add('mhx-ready');
        if (auto) hero.classList.add('is-auto');

        function pad(i) { return (i < 10 ? '0' : '') + i; }

        function setPaused(reason, on) {
            if (on) pauses[reason] = true; else delete pauses[reason];
            var paused = false;
            for (var k in pauses) if (pauses.hasOwnProperty(k)) { paused = true; break; }
            hero.classList.toggle('is-paused', paused);
            /* the visitor's Pause stops ALL of the hero's motion, not only the rotation (WCAG 2.2.2): the stage's
               float / sweep / rings / glow, the Ken Burns and the scroll cue (CSS .is-user-paused) — and the video */
            hero.classList.toggle('is-user-paused', !!pauses.user);
            var video = slides[cur].querySelector('video.is-loaded');
            if (video) { if (videoHeld()) video.pause(); else playVideo(slides[cur]); }
        }

        /* a video stays paused while the visitor paused the hero, the tab is hidden or the hero is off-screen */
        function videoHeld() { return !!(pauses.user || pauses.hidden || pauses.offscreen); }

        function playVideo(slide) {
            if (!videoOk) return;
            var v = slide.querySelector('video[data-src]');
            if (!v) return;
            if (!v.classList.contains('is-loaded')) {
                v.classList.add('is-loaded');
                v.src = v.getAttribute('data-src');
                v.addEventListener('playing', function () { v.classList.add('is-playing'); });
            }
            var p = v.play();
            if (p && p.catch) p.catch(function () { /* autoplay refused: the poster stays */ });
        }

        function stopVideo(slide) {
            var v = slide.querySelector('video.is-loaded');
            if (v) v.pause();
        }

        function go(next, manual) {
            next = ((next % n) + n) % n;
            if (next === cur) return;
            var prev = slides[cur];
            var el = slides[next];
            var prevIndex = cur;
            /* focus inside the outgoing slide (its CTA, after an arrow key) follows to the incoming one: making the
               outgoing slide inert would otherwise drop it to <body> (WCAG 2.4.3) */
            var hadFocus = prev.contains(doc.activeElement);

            /* the outgoing slide stays opaque underneath while the incoming one fades in on top; its text and its
               watch leave first (CSS .is-leaving) and the incoming ones wait for them (the .is-cycled delays), so two
               slides are never seen superimposed */
            hero.classList.add('is-cycled');
            clearTimeout(leaveTimers[prevIndex]);
            prev.classList.remove('is-active');
            prev.classList.add('is-leaving');
            leaveTimers[prevIndex] = setTimeout(function () {
                prev.classList.remove('is-leaving', 'is-in');
                stopVideo(prev);
            }, 1250);

            clearTimeout(leaveTimers[next]);
            el.classList.remove('is-leaving', 'is-in');
            void el.offsetWidth; /* restart the intro from its hidden state */
            el.classList.add('is-active');
            el.removeAttribute('inert');
            if (hadFocus) {
                var focusTo = el.querySelector('.mhx-cta') || el.querySelector('.mhx-slide__hit:not([tabindex="-1"])') || bars[next] || hero;
                try { focusTo.focus({ preventScroll: true }); } catch (err) { focusTo.focus(); }
            }
            prev.setAttribute('inert', '');
            raf(function () { raf(function () { el.classList.add('is-in'); }); });

            if (bars.length) {
                bars[prevIndex].classList.remove('is-active');
                bars[prevIndex].removeAttribute('aria-current');
                bars[next].classList.add('is-active');
                bars[next].setAttribute('aria-current', 'true');
            }
            if (curEl) curEl.textContent = pad(next + 1);
            cur = next;
            if (!videoHeld()) playVideo(el);
            if (manual && live) {
                var title = el.querySelector('.mhx-title');
                live.textContent = (el.getAttribute('aria-label') || '') + (title ? ' — ' + title.textContent.replace(/\s+/g, ' ').trim() : '');
            }
        }

        /* the active bar's fill animation is the timer: when it ends, advance */
        each(bars, function (bar, i) {
            bar.addEventListener('click', function () { go(i, true); });
            var fill = bar.querySelector('.mhx-bar__fill');
            if (fill) fill.addEventListener('animationend', function (e) {
                if (e.animationName === 'mhxFill' && auto && i === cur) go(cur + 1, false);
            });
        });

        var prevBtn = hero.querySelector('.mhx-arrow--prev');
        var nextBtn = hero.querySelector('.mhx-arrow--next');
        if (prevBtn) prevBtn.addEventListener('click', function () { go(cur - 1, true); });
        if (nextBtn) nextBtn.addEventListener('click', function () { go(cur + 1, true); });

        if (toggle) toggle.addEventListener('click', function () {
            var on = toggle.getAttribute('aria-pressed') !== 'true';
            toggle.setAttribute('aria-pressed', on ? 'true' : 'false');
            toggle.setAttribute('aria-label', toggle.getAttribute(on ? 'data-label-play' : 'data-label-pause'));
            setPaused('user', on);
        });

        /* keyboard: the arrow that points "forward" in the reading direction goes to the next slide */
        hero.addEventListener('keydown', function (e) {
            if (n < 2 || e.altKey || e.ctrlKey || e.metaKey) return;
            var key = e.key;
            if (key !== 'ArrowLeft' && key !== 'ArrowRight') return;
            var forward = rtl ? key === 'ArrowLeft' : key === 'ArrowRight';
            go(cur + (forward ? 1 : -1), true);
            e.preventDefault();
        });

        /* pause on hover of the text / controls (not the whole picture, so a resting cursor does not freeze it) and on focus */
        if (finePointer) {
            each(hero.querySelectorAll('.mhx-slide__inner, .mhx-hero__controls'), function (zone) {
                zone.addEventListener('mouseenter', function () { setPaused('hover', true); });
                zone.addEventListener('mouseleave', function () { setPaused('hover', false); });
            });
        }
        /* pause on KEYBOARD focus only: a click or tap on an arrow, a bar or the toggle also focuses that button
           in Chrome, and it must not freeze the carousel for good while the toggle still says "playing" */
        hero.addEventListener('focusin', function (e) {
            var kb = false;
            try { kb = e.target.matches(':focus-visible'); } catch (err) { kb = false; }
            setPaused('focus', kb);
            /* keyboard focus inside un-hides a faded hero (CSS .is-kb-focus); kept while focus moves on to another hero
               control, so the next control is still visible — and focusable — when the browser moves focus to it */
            hero.classList.toggle('is-kb-focus', kb);
        });
        hero.addEventListener('focusout', function (e) {
            if (!e.relatedTarget || !hero.contains(e.relatedTarget)) {
                setPaused('focus', false);
                hero.classList.remove('is-kb-focus');
            }
        });
        doc.addEventListener('visibilitychange', function () { setPaused('hidden', doc.hidden); });
        if (hasIO) {
            new IntersectionObserver(function (entries) {
                setPaused('offscreen', !entries[0].isIntersecting);
            }, { threshold: 0.2 }).observe(hero);
        }

        /* pointer swipe (horizontal only; vertical scrolling stays native through touch-action: pan-y) */
        var sx = null, sy = 0, swiped = false;
        hero.addEventListener('pointerdown', function (e) {
            /* a touch swipe is followed by no click: reset here, or the next tap on a control would be swallowed */
            swiped = false;
            if (e.pointerType === 'mouse' && e.button !== 0) return;
            if (e.target.closest && e.target.closest('button')) return;
            sx = e.clientX; sy = e.clientY;
        }, { passive: true });
        hero.addEventListener('pointerup', function (e) {
            if (sx === null || n < 2) return;
            var dx = e.clientX - sx, dy = e.clientY - sy;
            sx = null;
            if (Math.abs(dx) < 50 || Math.abs(dx) < Math.abs(dy) * 1.3) return;
            swiped = true;
            var forward = rtl ? dx > 0 : dx < 0;
            go(cur + (forward ? 1 : -1), true);
        }, { passive: true });
        hero.addEventListener('pointercancel', function () { sx = null; }, { passive: true });
        /* a swipe is not a click on the slide link */
        hero.addEventListener('click', function (e) {
            if (swiped) { swiped = false; e.preventDefault(); e.stopPropagation(); }
        }, true);

        /* pointer parallax of the Spotlight stage (desktop, fine pointer). No layout read per move: the hero box
           comes from size() and the scroll offset from the scroll handler; the offset is written on the ACTIVE
           slide's two stage wrappers only (registered non-inheriting, see registerVar) */
        if (!reduce && finePointer) {
            var pending = false, px = 0, py = 0, pointerSlide = -1;
            var stageLayers = function (i) {
                var s = slides[i];
                if (!s.mhxLayers) s.mhxLayers = s.querySelectorAll('.mhx-stage__ringwrap, .mhx-stage__platewrap');
                return s.mhxLayers;
            };
            var writePointer = function (x, y) {
                if (pointerSlide >= 0 && pointerSlide !== cur) {
                    each(stageLayers(pointerSlide), function (l) { l.style.removeProperty('--mhx-px'); l.style.removeProperty('--mhx-py'); });
                }
                pointerSlide = cur;
                each(stageLayers(cur), function (l) { l.style.setProperty('--mhx-px', x); l.style.setProperty('--mhx-py', y); });
            };
            hero.addEventListener('mousemove', function (e) {
                if (!heroBox.w || !heroBox.h) return;
                px = Math.max(-1, Math.min(1, ((e.clientX - heroBox.left) / heroBox.w - 0.5) * 2));
                py = Math.max(-1, Math.min(1, ((e.clientY - (heroTop - lastY)) / heroBox.h - 0.5) * 2));
                if (pending) return;
                pending = true;
                raf(function () {
                    pending = false;
                    writePointer(px.toFixed(3), py.toFixed(3));
                });
            }, { passive: true });
            hero.addEventListener('mouseleave', function () { writePointer('0', '0'); });
        }

        /* the scroll cue glides to the section right under the hero */
        var cue = hero.querySelector('.mhx-cue');
        if (cue) cue.addEventListener('click', function (e) {
            var target = doc.getElementById('mhx-after-hero');
            if (!target) return;
            e.preventDefault();
            var top = target.getBoundingClientRect().top + (win.pageYOffset || root.scrollTop);
            win.scrollTo({ top: top, behavior: reduce ? 'auto' : 'smooth' });
            target.setAttribute('tabindex', '-1');
            try { target.focus({ preventScroll: true }); } catch (err) { /* old browsers */ }
        });

        /* the hero fills the first screen under the masthead (capped in CSS) */
        function size() {
            var r = hero.getBoundingClientRect();
            lastY = win.pageYOffset || root.scrollTop || 0;
            heroTop = Math.max(0, Math.round(r.top + lastY));
            heroBox.left = r.left;
            heroBox.w = r.width;
            heroBox.h = r.height;
            hero.style.setProperty('--mhx-top', heroTop + 'px');
            /* [Mahjar home-motion] hero + the trust marquee under it fill the first screen together (CSS --mhx-hh) */
            var foot = doc.getElementById('mhx-after-hero');
            if (foot && foot.parentNode === hero.parentNode) hero.style.setProperty('--mhx-foot', Math.round(foot.getBoundingClientRect().height) + 'px');
        }
        size();
        heroScrollEls = hero.querySelectorAll('.mhx-slide__media, .mhx-slide__content, .mhx-hero__chrome');

        /* intro of the first slide (only when the inline script did not arm it) */
        if (!introArmed) raf(function () { raf(function () { slides[0].classList.add('is-in'); }); });
        playVideo(slides[0]);

        return { el: hero, size: size };
    }

    /* =====================================================================================
       ONE REVEAL SYSTEM
       ===================================================================================== */
    function neutraliseLegacyReveal() {
        /* the customheadtags script tags cards with .mh-reveal on DOMContentLoaded — undo it on this page */
        each(doc.querySelectorAll('.mh-reveal'), function (el) {
            el.classList.remove('mh-reveal', 'is-visible');
            el.style.transitionDelay = '';
        });
    }

    function initReveal() {
        /* staggered grids: the delay restarts on every visual row */
        each(doc.querySelectorAll('.mhx-stagger'), function (grid) {
            var k = 0, rowTop = null;
            each(grid.children, function (el) {
                var t = el.offsetTop;
                if (rowTop === null || Math.abs(t - rowTop) > 4) { rowTop = t; k = 0; }
                el.classList.add('mhx-rv');
                el.style.setProperty('--mhx-d', (Math.min(k, 6) * 90) + 'ms');
                k++;
            });
        });
        /* news / best sellers, if enabled, join the same rhythm */
        each(doc.querySelectorAll('.html-home-page .news-list-homepage, .html-home-page .bestsellers, .html-home-page .home-page-polls'), function (el) {
            el.classList.add('mhx-rv');
        });

        var targets = doc.querySelectorAll('.mhx-rv');
        if (!motion) {
            each(targets, function (el) { el.classList.add('is-in'); });
            each(doc.querySelectorAll('.mhx-band'), function (el) { el.classList.add('is-in'); });
            return;
        }
        /* once revealed, an element gets its own transition list back: the reveal rule replaces it, which made the
           Own/Rent tiles' flex-grow and the category tiles' shadow snap on hover */
        function settle(el) {
            var d = parseInt(el.style.getPropertyValue('--mhx-d'), 10) || 0;
            setTimeout(function () {
                el.classList.remove('mhx-rv');
                el.style.removeProperty('--mhx-d');
            }, 1300 + d);
        }
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) { e.target.classList.add('is-in'); io.unobserve(e.target); settle(e.target); }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -6% 0px' });
        each(targets, function (el) { io.observe(el); });

        var bandIo = new IntersectionObserver(function (entries) {
            entries.forEach(function (e) {
                if (e.isIntersecting) { e.target.classList.add('is-in'); bandIo.unobserve(e.target); }
            });
        }, { threshold: 0.3 });
        each(doc.querySelectorAll('.mhx-band'), function (el) { bandIo.observe(el); });
    }

    /* =====================================================================================
       TRUST MARQUEE: a real pause / play control (hover alone serves neither keyboard nor touch — WCAG 2.2.2)
       ===================================================================================== */
    function initMarquee() {
        each(doc.querySelectorAll('.mhx-marquee'), function (m) {
            var btn = m.querySelector('.mhx-marquee__toggle');
            if (!btn) return;
            if (!motion) { btn.hidden = true; return; }
            btn.addEventListener('click', function () {
                var on = btn.getAttribute('aria-pressed') !== 'true';
                btn.setAttribute('aria-pressed', on ? 'true' : 'false');
                btn.setAttribute('aria-label', btn.getAttribute(on ? 'data-label-play' : 'data-label-pause'));
                m.classList.toggle('is-paused', on);
            });
        });
    }

    /* =====================================================================================
       THE scroll handler (rAF-throttled, passive): progress hairline, hero parallax, band parallax
       ===================================================================================== */
    var bar = doc.querySelector('.mhx-progress__bar');
    var bands = doc.querySelectorAll('.mhx-band');
    var ticking = false;
    var heroDone = false;

    function update() {
        ticking = false;
        /* read everything first … */
        var y = win.pageYOffset || root.scrollTop;
        lastY = y;
        var vh = win.innerHeight;
        var max = root.scrollHeight - vh;
        var h = hero && !reduce ? (hero.offsetHeight || 1) : 0;
        var rects = [];
        if (!reduce) for (var i = 0; i < bands.length; i++) rects.push(bands[i].getBoundingClientRect());

        /* … then write (no layout thrashing) */
        if (bar) bar.style.transform = 'scaleX(' + (max > 0 ? Math.min(1, Math.max(0, y / max)) : 0).toFixed(4) + ')';
        if (reduce) return;

        if (hero) {
            /* relative to the hero's own top: the media drifts down only once the hero's top edge is above the
               viewport, so the strip that drift uncovers at its top is never on screen (no dark seam) */
            var sy = Math.min(Math.max(y - heroTop, 0), h);
            if (sy < h || !heroDone) {
                var fade = Math.max(0, 1 - sy / (h * 0.62));
                var syText = sy.toFixed(1), fadeText = fade.toFixed(3);
                for (var j = 0; j < heroScrollEls.length; j++) {
                    heroScrollEls[j].style.setProperty('--mhx-sy', syText);
                    heroScrollEls[j].style.setProperty('--mhx-fade', fadeText);
                }
                /* faded out = hidden for real (CSS .is-faded): invisible controls take no clicks and leave the tab
                   order; keyboard focus inside the hero brings them back (see mahjar.home.css) */
                var faded = fade < 0.05;
                if (faded !== heroFaded) { heroFaded = faded; hero.classList.toggle('is-faded', faded); }
                heroDone = sy >= h;
            }
        }

        for (var k = 0; k < rects.length; k++) {
            var r = rects[k];
            if (r.bottom < -40 || r.top > vh + 40) continue;
            var p = (r.top + r.height / 2 - vh / 2) / (vh / 2 + r.height / 2); /* -1 … 1 */
            bands[k].style.setProperty('--mhx-bp', (-p * r.height * 0.11).toFixed(1) + 'px');
        }
    }

    function onScroll() {
        if (!ticking) { ticking = true; raf(update); }
    }

    /* =====================================================================================
       boot
       ===================================================================================== */
    heroApi = initHero();
    initMarquee();

    function afterLegacy() {
        neutraliseLegacyReveal();
        initReveal();
    }
    /* the customheadtags reveal runs on DOMContentLoaded — our listener is registered later, so it runs after it */
    if (doc.readyState === 'loading') doc.addEventListener('DOMContentLoaded', afterLegacy);
    else afterLegacy();

    win.addEventListener('scroll', onScroll, { passive: true });
    win.addEventListener('resize', function () {
        setScrollbarWidth();
        if (heroApi) heroApi.size();
        onScroll();
    }, { passive: true });
    win.addEventListener('load', function () {
        setScrollbarWidth();
        if (heroApi) heroApi.size();
        onScroll();
    });
    update();
})();
