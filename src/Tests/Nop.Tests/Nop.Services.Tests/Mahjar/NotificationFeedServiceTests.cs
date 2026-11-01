// [Mahjar notifications]
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Nop.Core.Domain.Mahjar;
using Nop.Services.Mahjar.Notifications;
using NUnit.Framework;

namespace Nop.Tests.Nop.Services.Tests.Mahjar;

[TestFixture]
public class NotificationFeedServiceTests : ServiceTest
{
    private INotificationFeedService _feedService;
    private INotificationPreferenceService _preferenceService;

    [OneTimeSetUp]
    public void SetUp()
    {
        _feedService = GetService<INotificationFeedService>();
        _preferenceService = GetService<INotificationPreferenceService>();

        // The SQLite test provider doesn't materialize Create.TableFor<T> for brand-new entities (the
        // migration's version is committed but the table isn't built — a harness quirk; the real SQL Server
        // schema is created correctly). Create the two tables directly on the shared in-memory DB so the
        // service can be exercised. Column names match the entity property names (LinqToDB default mapping).
        using var conn = new SqliteConnection("Data Source=nopCommerceTest.sqlite;Mode=Memory;Cache=Shared");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Notification (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RecipientCustomerId INTEGER NOT NULL,
    RecipientVendorId INTEGER NOT NULL,
    RecipientTypeId INTEGER NOT NULL,
    NotificationTypeId INTEGER NOT NULL,
    Title TEXT,
    Body TEXT,
    EntityName TEXT,
    EntityId INTEGER NOT NULL,
    Url TEXT,
    IsRead INTEGER NOT NULL,
    StoreId INTEGER NOT NULL,
    CreatedOnUtc TEXT NOT NULL,
    ReadOnUtc TEXT NULL
);
CREATE TABLE IF NOT EXISTS NotificationPreference (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL,
    NotificationTypeId INTEGER NOT NULL,
    InAppEnabled INTEGER NOT NULL,
    EmailEnabled INTEGER NOT NULL
);";
        cmd.ExecuteNonQuery();
    }

    private static Notification BuyerNotification(int customerId, NotificationType type = NotificationType.OrderPlacedBuyer)
    {
        return new Notification
        {
            RecipientCustomerId = customerId,
            RecipientType = NotificationRecipientType.Buyer,
            NotificationType = type,
            Title = "T",
            Body = "B",
            StoreId = 0
        };
    }

    [Test]
    public async Task CanCreateAndCountUnread()
    {
        const int customerId = 987001;

        var created = await _feedService.CreateNotificationAsync(BuyerNotification(customerId));
        created.Should().NotBeNull();

        var unread = await _feedService.GetUnreadCountAsync(customerId, NotificationRecipientType.Buyer);
        unread.Should().Be(1);

        var list = await _feedService.GetNotificationsAsync(customerId, NotificationRecipientType.Buyer);
        list.Count.Should().Be(1);
        list.First().Title.Should().Be("T");
    }

    [Test]
    public async Task RespectsInAppOptOut()
    {
        const int customerId = 987002;

        // opt out of OrderPlacedBuyer in-app
        await _preferenceService.SetPreferenceAsync(customerId, NotificationType.OrderPlacedBuyer, inApp: false, email: true);

        var created = await _feedService.CreateNotificationAsync(BuyerNotification(customerId));
        created.Should().BeNull(); // skipped

        (await _feedService.GetUnreadCountAsync(customerId, NotificationRecipientType.Buyer)).Should().Be(0);

        // a different (still-enabled) type still writes
        var other = await _feedService.CreateNotificationAsync(BuyerNotification(customerId, NotificationType.OrderShipped));
        other.Should().NotBeNull();
        (await _feedService.GetUnreadCountAsync(customerId, NotificationRecipientType.Buyer)).Should().Be(1);
    }

    [Test]
    public async Task MarkAsReadIsOwnershipChecked()
    {
        const int ownerId = 987003;
        const int otherId = 987004;

        var n = await _feedService.CreateNotificationAsync(BuyerNotification(ownerId));

        // a different customer cannot mark it read
        await _feedService.MarkAsReadAsync(n.Id, otherId);
        (await _feedService.GetUnreadCountAsync(ownerId, NotificationRecipientType.Buyer)).Should().Be(1);

        // the owner can
        await _feedService.MarkAsReadAsync(n.Id, ownerId);
        (await _feedService.GetUnreadCountAsync(ownerId, NotificationRecipientType.Buyer)).Should().Be(0);
    }

    [Test]
    public async Task FiltersByRecipientType()
    {
        const int customerId = 987005;
        const int vendorId = 5550;

        await _feedService.CreateNotificationAsync(BuyerNotification(customerId));
        await _feedService.CreateNotificationAsync(new Notification
        {
            RecipientCustomerId = customerId,
            RecipientType = NotificationRecipientType.Vendor,
            RecipientVendorId = vendorId,
            NotificationType = NotificationType.OrderPlacedVendor,
            Title = "V",
            Body = "B",
            StoreId = 0
        });

        (await _feedService.GetUnreadCountAsync(customerId, NotificationRecipientType.Buyer)).Should().Be(1);
        (await _feedService.GetUnreadCountAsync(customerId, NotificationRecipientType.Vendor, vendorId)).Should().Be(1);

        // mark-all on the buyer feed must not touch the vendor feed
        await _feedService.MarkAllAsReadAsync(customerId, NotificationRecipientType.Buyer);
        (await _feedService.GetUnreadCountAsync(customerId, NotificationRecipientType.Buyer)).Should().Be(0);
        (await _feedService.GetUnreadCountAsync(customerId, NotificationRecipientType.Vendor, vendorId)).Should().Be(1);
    }
}
