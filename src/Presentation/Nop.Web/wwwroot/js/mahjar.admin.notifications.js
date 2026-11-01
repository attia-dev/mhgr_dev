// [Mahjar notifications] admin/vendor navbar bell: SignalR live updates + flyout feed + mark read/all
$(function () {
    'use strict';

    var $bell = $('#admin-notification-bell');
    if (!$bell.length) {
        return;
    }

    var $badge = $bell.find('.notification-unread');
    var $list = $bell.find('.notification-flyout-list');

    var feedUrl = $bell.data('feed-url');
    var markAllUrl = $bell.data('mark-all-url');
    var markReadUrl = $bell.data('mark-read-url');
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
            return;
        }
        items.forEach(function (n) {
            var cls = 'notification-flyout-item' + ((n.isRead || n.IsRead) ? ' read' : ' unread');
            var $item = $('<div class="' + cls + '"></div>')
                .attr('data-id', n.id || n.Id || 0)
                .attr('data-url', n.url || n.Url || '')
                .append($('<div class="t"></div>').text(n.title || n.Title || ''))
                .append($('<div class="b"></div>').text(n.body || n.Body || ''))
                .append($('<div class="w"></div>').text(n.createdOnStr || n.CreatedOnStr || ''));
            $list.append($item);
        });
    }

    // click a flyout item → mark read + open its details
    $list.on('click', '.notification-flyout-item', function (e) {
        e.preventDefault();
        var id = parseInt($(this).attr('data-id'), 10) || 0;
        var url = $(this).attr('data-url');
        if (id && markReadUrl) {
            $.ajax({ url: markReadUrl, type: 'POST', data: addAntiForgeryToken({ id: id }) });
        }
        if (url) {
            window.location.href = url;
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

    function markAll(url) {
        url = url || markAllUrl;
        if (!url) {
            return;
        }
        var data = addAntiForgeryToken({});
        $.ajax({
            url: url,
            type: 'POST',
            data: data,
            success: function (res) {
                if (res && res.success) {
                    setBadge(0);
                    $list.find('.notification-flyout-item').removeClass('unread').addClass('read');
                }
            }
        });
    }

    // AdminLTE opens the dropdown on toggle click; load the feed then
    $('#admin-notification-toggle').on('click', function () {
        loadFeed();
    });

    $bell.on('click', '.mark-all-read', function (e) {
        e.preventDefault();
        markAll();
    });

    $(document).on('click', '.mark-all-read-page', function (e) {
        e.preventDefault();
        markAll($(this).data('mark-all-url'));
    });

    // SignalR live channel
    if (typeof signalR !== 'undefined') {
        var conn = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/notifications?group=' + encodeURIComponent(group || ''))
            .withAutomaticReconnect()
            .build();

        conn.on('ReceiveNotification', function () {
            bumpBadge();
        });

        conn.on('UnreadCount', function (count) {
            setBadge(count);
        });

        conn.start().catch(function (err) {
            if (window.console) {
                console.warn('admin notification hub:', err);
            }
        });
    }
});
