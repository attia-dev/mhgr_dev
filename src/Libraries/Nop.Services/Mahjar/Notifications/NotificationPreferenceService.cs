// [Mahjar notifications]
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Mahjar;
using Nop.Data;

namespace Nop.Services.Mahjar.Notifications;

/// <summary>
/// Per-customer notification preference service. [Mahjar notifications]
/// </summary>
public partial class NotificationPreferenceService : INotificationPreferenceService
{
    #region Fields

    protected readonly IRepository<NotificationPreference> _preferenceRepository;

    #endregion

    #region Ctor

    public NotificationPreferenceService(IRepository<NotificationPreference> preferenceRepository)
    {
        _preferenceRepository = preferenceRepository;
    }

    #endregion

    #region Methods

    public virtual async Task<bool> IsInAppEnabledAsync(int customerId, NotificationType type)
    {
        if (customerId <= 0)
            return true;

        var typeId = (int)type;
        var row = await _preferenceRepository.Table
            .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.NotificationTypeId == typeId);

        //no row => enabled by default (opt-out model)
        return row?.InAppEnabled ?? true;
    }

    public virtual async Task<IList<NotificationPreference>> GetPreferencesAsync(int customerId)
    {
        return await _preferenceRepository.Table
            .Where(p => p.CustomerId == customerId)
            .ToListAsync();
    }

    public virtual async Task SetPreferenceAsync(int customerId, NotificationType type, bool inApp, bool email)
    {
        var typeId = (int)type;
        var row = await _preferenceRepository.Table
            .FirstOrDefaultAsync(p => p.CustomerId == customerId && p.NotificationTypeId == typeId);

        if (row == null)
        {
            await _preferenceRepository.InsertAsync(new NotificationPreference
            {
                CustomerId = customerId,
                NotificationTypeId = typeId,
                InAppEnabled = inApp,
                EmailEnabled = email
            });
        }
        else
        {
            row.InAppEnabled = inApp;
            row.EmailEnabled = email;
            await _preferenceRepository.UpdateAsync(row);
        }
    }

    #endregion
}
