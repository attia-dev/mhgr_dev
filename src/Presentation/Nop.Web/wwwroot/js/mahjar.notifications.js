// [Mahjar notifications] storefront bell: SignalR live updates + flyout feed + mark read/all
(function () {
    'use strict';

    var $bell = $('#notification-bell');
    if (!$bell.length) {
        return;
    }

    var $badge = $bell.find('.notification-unread');
    var $flyout = $('#notification-flyout');
    var $list = $flyout.find('.notification-flyout-list');

    var feedUrl = $bell.data('feed-url');
    var markReadUrl = $bell.data('mark-read-url');
    var markAllUrl = $bell.data('mark-all-url');
    var group = $bell.data('group');

    function setBadge(count) {
        count = parseInt(count, 10) || 0;
        $badge.text(count).toggleClass('empty', count === 0);
    }

    function bumpBadge() {
        setBadge((parseInt($badge.text(), 10) || 0) + 1);
    }

    function renderItems(items) {
        $list.empty();
        if (!items || !items.length) {
            $list.append('<li class="empty">' + ($flyout.data('empty-text') || '') + '</li>');
            return;
        }
        items.forEach(function (n) {
            prependItem(n, true);
        });
    }

    function prependItem(n, append) {
        var cls = 'notification-flyout-item' + (n.isRead || n.IsRead ? ' read' : ' unread');
        var title = n.title || n.Title || '';
        var body = n.body || n.Body || '';
        var when = n.createdOnStr || n.CreatedOnStr || '';
        var $li = $('<li class="' + cls + '"></li>')
            .attr('data-id', n.id || n.Id || 0)
            .attr('data-url', n.url || n.Url || '')
            .append($('<div class="t"></div>').text(title))
            .append($('<div class="b"></div>').text(body))
            .append($('<div class="w"></div>').text(when));
        if (append) {
            $list.append($li);
        } else {
            $list.prepend($li);
        }
    }

    // click a flyout item → mark read + open its details
    $list.on('click', '.notification-flyout-item', function () {
        var id = parseInt($(this).attr('data-id'), 10) || 0;
        var url = $(this).attr('data-url');
        if (id && markReadUrl) {
            $.ajax({ url: markReadUrl, type: 'POST', data: addAntiForgeryToken({ id: id }) });
        }
        if (url) {
            window.location.href = '/' + url.replace(/^\//, '');
        }
    });

    function loadFeed() {
        if (!feedUrl) {
            return;
        }
        $.ajax({
            url: feedUrl,
            type: 'GET',
            data: { page: 0 },
            success: function (res) {
                if (res && res.success && res.model) {
                    renderItems(res.model.Items || res.model.items);
                    setBadge(res.model.UnreadCount != null ? res.model.UnreadCount : res.model.unreadCount);
                }
            }
        });
    }

    function markAll() {
        if (!markAllUrl) {
            return;
        }
        var data = addAntiForgeryToken({});
        $.ajax({
            url: markAllUrl,
            type: 'POST',
            data: data,
            success: function (res) {
                if (res && res.success) {
                    setBadge(0);
                    $list.find('.notification-flyout-item').removeClass('unread').addClass('read');
                    $('.notification-item').removeClass('unread').addClass('read');
                }
            }
        });
    }

    // toggle flyout
    $('#notification-bell-toggle').on('click', function (e) {
        // let the link navigate on small screens; on desktop show flyout
        if ($flyout.length) {
            e.preventDefault();
            var opening = !$flyout.hasClass('open');
            $flyout.toggleClass('open', opening);
            if (opening) {
                loadFeed();
            }
        }
    });

    $(document).on('click', function (e) {
        if ($flyout.length && !$.contains($bell[0], e.target)) {
            $flyout.removeClass('open');
        }
    });

    $flyout.on('click', '.mark-all-read', function (e) {
        e.preventDefault();
        markAll();
    });

    // page-level "mark all read" button (Index page)
    $(document).on('click', '.mark-all-read-page', function (e) {
        e.preventDefault();
        markAllUrl = $(this).data('mark-all-url') || markAllUrl;
        markAll();
    });

    // SignalR live channel
    if (typeof signalR !== 'undefined') {
        var conn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications?group=' + encodeURIComponent(group || ''))
            .withAutomaticReconnect()
            .build();

        conn.on('ReceiveNotification', function (n) {
            bumpBadge();
            if ($flyout.hasClass('open')) {
                prependItem(n, false);
            }
            if (typeof displayBarNotification === 'function') {
                displayBarNotification(n.title || n.Title || '', 'success', 4000);
            }
        });

        conn.on('UnreadCount', function (count) {
            setBadge(count);
        });

        conn.start().catch(function (err) {
            if (window.console) {
                console.warn('notification hub:', err);
            }
        });
    }
})();
