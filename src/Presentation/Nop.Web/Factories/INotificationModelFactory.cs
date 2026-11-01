// [Mahjar notifications]
using System.Threading.Tasks;
using Nop.Web.Models.Notification;

namespace Nop.Web.Factories;

/// <summary>Builds the storefront (buyer) notification view models. [Mahjar notifications]</summary>
public partial interface INotificationModelFactory
{
    Task<NotificationBellModel> PrepareBellModelAsync();

    Task<NotificationListModel> PrepareListModelAsync(int pageIndex = 0);

    Task<NotificationPreferenceModel> PreparePreferenceModelAsync();

    Task SavePreferencesAsync(NotificationPreferenceModel model);
}
