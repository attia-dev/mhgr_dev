// [Mahjar notifications]
using System.Threading.Tasks;
using Nop.Web.Areas.Admin.Models.Notification;

namespace Nop.Web.Areas.Admin.Factories;

/// <summary>Builds the admin/vendor notification view models (vendor-scoped automatically). [Mahjar notifications]</summary>
public partial interface INotificationModelFactory
{
    Task<AdminNotificationBellModel> PrepareBellModelAsync();

    Task<NotificationListModel> PrepareListModelAsync(int pageIndex = 0);

    Task<NotificationPreferenceModel> PreparePreferenceModelAsync();

    Task SavePreferencesAsync(NotificationPreferenceModel model);
}
