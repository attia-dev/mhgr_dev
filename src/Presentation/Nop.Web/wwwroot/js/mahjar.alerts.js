/* [Mahjar alerts] Branded replacement for the native browser alert() so storefront validation
   messages (one-page checkout, contract signing, ...) show a Mahjar-styled modal instead of the
   browser "localhost:5000 says" dialog.
   - window.mahjarAlert(message, title?) shows the modal; successive calls are queued
   - window.alert is overridden at parse time; the native dialog stays at window.__nativeAlert
   - No jQuery dependency; nothing is injected into the DOM until the first call
   Styles: [Mahjar alerts] block in Themes/DefaultClean/Content/css/styles.css + styles.rtl.css */
(function () {
    'use strict';

    if (window.__nativeAlert)
        return; /* already installed */

    var nativeAlert = typeof window.alert === 'function' ? window.alert.bind(window) : function () { };
    window.__nativeAlert = nativeAlert;

    var queue = [];
    var open = false;
    var overlay = null;
    var titleEl = null;
    var messageEl = null;
    var okButton = null;

    function okLabel() {
        var lang = (document.documentElement.lang || '').toLowerCase();
        return lang.indexOf('ar') === 0 ? 'موافق' : 'OK';
    }

    function ensureBuilt() {
        if (overlay)
            return;
        overlay = document.createElement('div');
        overlay.className = 'mahjar-alert-overlay';
        var card = document.createElement('div');
        card.className = 'mahjar-alert-card';
        card.setAttribute('role', 'alertdialog');
        card.setAttribute('aria-modal', 'true');
        titleEl = document.createElement('div');
        titleEl.className = 'mahjar-alert-title';
        messageEl = document.createElement('div');
        messageEl.className = 'mahjar-alert-message';
        var buttons = document.createElement('div');
        buttons.className = 'mahjar-alert-buttons';
        okButton = document.createElement('button');
        okButton.type = 'button';
        okButton.className = 'button-1 mahjar-alert-ok'; /* .button-1 -> themed dark pill */
        buttons.appendChild(okButton);
        card.appendChild(titleEl);
        card.appendChild(messageEl);
        card.appendChild(buttons);
        overlay.appendChild(card);

        okButton.addEventListener('click', dismiss);
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay)
                dismiss();
        });
        document.addEventListener('keydown', function (e) {
            if (open && (e.key === 'Escape' || e.key === 'Esc' || e.keyCode === 27)) {
                e.preventDefault();
                dismiss();
            }
        });
    }

    function render(item) {
        ensureBuilt();
        /* follow the document direction so the modal is RTL on the Arabic storefront */
        overlay.setAttribute('dir', document.documentElement.getAttribute('dir') || document.dir || 'ltr');
        if (item.title) {
            titleEl.textContent = item.title; /* textContent only - never innerHTML */
            titleEl.removeAttribute('hidden');
        } else {
            titleEl.textContent = '';
            titleEl.setAttribute('hidden', 'hidden');
        }
        messageEl.textContent = item.message;
        okButton.textContent = okLabel();
        if (!overlay.parentNode)
            document.body.appendChild(overlay);
        open = true;
        try { okButton.focus(); } catch (e) { }
    }

    function dismiss() {
        if (!open)
            return;
        if (queue.length) {
            render(queue.shift()); /* show the next queued alert */
            return;
        }
        open = false;
        if (overlay && overlay.parentNode)
            overlay.parentNode.removeChild(overlay);
    }

    function mahjarAlert(message, title) {
        var text = (message === undefined || message === null) ? '' : String(message);
        if (!document.body) {
            /* called before <body> exists - fall back to the native dialog */
            nativeAlert(text);
            return;
        }
        var item = { message: text, title: title ? String(title) : null };
        if (open) {
            queue.push(item);
            return;
        }
        render(item);
    }

    window.mahjarAlert = mahjarAlert;
    window.alert = mahjarAlert;
})();
