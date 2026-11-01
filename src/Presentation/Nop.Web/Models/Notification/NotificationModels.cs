// [Mahjar notifications]
using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Notification;

/// <summary>Header bell + unread badge for a logged-in buyer.</summary>
public partial record NotificationBellModel : BaseNopModel
{
    public int CurrentCustomerId { get; set; }
    public int UnreadCount { get; set; }
    public string Group { get; set; }
}

/// <summary>A single feed item (account page + AJAX flyout).</summary>
public partial record NotificationItemModel : BaseNopModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }
    public string Url { get; set; }
    public bool IsRead { get; set; }
    public string CreatedOnStr { get; set; }
    public int TypeId { get; set; }
    public string TypeLabel { get; set; }
}

/// <summary>Paged feed list.</summary>
public partial record NotificationListModel : BaseNopModel
{
    public NotificationListModel()
    {
        Items = new List<NotificationItemModel>();
    }

    public IList<NotificationItemModel> Items { get; set; }
    public int UnreadCount { get; set; }
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>Per-type preferences screen.</summary>
public partial record NotificationPreferenceModel : BaseNopModel
{
    public NotificationPreferenceModel()
    {
        Items = new List<NotificationPreferenceItemModel>();
    }

    public IList<NotificationPreferenceItemModel> Items { get; set; }
}

public partial record NotificationPreferenceItemModel : BaseNopModel
{
    public int TypeId { get; set; }
    public string TypeLabel { get; set; }
    public bool InAppEnabled { get; set; }
    public bool EmailEnabled { get; set; }
}
