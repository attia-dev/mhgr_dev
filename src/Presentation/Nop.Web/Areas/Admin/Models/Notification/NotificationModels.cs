// [Mahjar notifications]
using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace Nop.Web.Areas.Admin.Models.Notification;

/// <summary>Admin/vendor header bell + unread badge.</summary>
public partial record AdminNotificationBellModel : BaseNopModel
{
    public int UnreadCount { get; set; }
    public string Group { get; set; }
    public bool IsVendor { get; set; }
}

/// <summary>A single admin/vendor feed row.</summary>
public partial record NotificationModel : BaseNopEntityModel
{
    public string Title { get; set; }
    public string Body { get; set; }
    public string Url { get; set; }
    public bool IsRead { get; set; }
    public string CreatedOnStr { get; set; }
    public string TypeLabel { get; set; }
}

/// <summary>Paged admin/vendor feed.</summary>
public partial record NotificationListModel : BaseNopModel
{
    public NotificationListModel()
    {
        Items = new List<NotificationModel>();
    }

    public IList<NotificationModel> Items { get; set; }
    public int UnreadCount { get; set; }
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>Admin/vendor per-type preferences.</summary>
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
