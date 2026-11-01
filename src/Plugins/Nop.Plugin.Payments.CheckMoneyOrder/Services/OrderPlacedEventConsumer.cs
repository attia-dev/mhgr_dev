// [Mahjar payments]
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Logging;
using Nop.Services.Orders;

namespace Nop.Plugin.Payments.CheckMoneyOrder.Services;

/// <summary>
/// [Mahjar payments] While the store has no live payment gateway, this method stands in for one. When
/// <see cref="CheckMoneyOrderPaymentSettings.MarkOrdersAsPaidAutomatically"/> is on, an order placed with it is
/// marked as paid straight away — the customer finishing checkout is treated as a completed payment — so the order
/// moves on by itself: the buyer is told the payment was received and the handover desk gets its task, without an
/// admin pressing "Mark as paid".
///
/// It listens to the order being PLACED rather than hooking the payment method's post-processing, because only the
/// web checkout calls that hook: the mobile app places orders through its own API, and an admin can create one by
/// hand. Every path raises this event. Auto-discovered by ITypeFinder. [Mahjar payments]
/// </summary>
public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
{
    #region Fields

    /// <summary>The system name this plugin is registered under; only its own orders are touched.</summary>
    private const string PAYMENT_METHOD_SYSTEM_NAME = "Payments.CheckMoneyOrder";

    protected readonly ILogger _logger;
    protected readonly IOrderProcessingService _orderProcessingService;
    protected readonly IOrderService _orderService;
    protected readonly ISettingService _settingService;

    #endregion

    #region Ctor

    public OrderPlacedEventConsumer(ILogger logger,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        ISettingService settingService)
    {
        _logger = logger;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _settingService = settingService;
    }

    #endregion

    #region Methods

    public async Task HandleEventAsync(OrderPlacedEvent eventMessage)
    {
        var order = eventMessage?.Order;
        if (order == null)
            return;

        if (!string.Equals(order.PaymentMethodSystemName, PAYMENT_METHOD_SYSTEM_NAME, StringComparison.InvariantCultureIgnoreCase))
            return;

        //the setting can be overridden per store
        var settings = await _settingService.LoadSettingAsync<CheckMoneyOrderPaymentSettings>(order.StoreId);
        if (!settings.MarkOrdersAsPaidAutomatically)
            return;

        //an order that is already paid, or cancelled, must not be touched
        if (!_orderProcessingService.CanMarkOrderAsPaid(order))
            return;

        try
        {
            //Deliberately NOT MarkOrderAsPaidAsync. This consumer runs INSIDE PlaceOrderAsync, which right after
            //raising OrderPlacedEvent does: CheckOrderStatusAsync(order), then ProcessOrderPaidAsync(order) when the
            //order is paid — on this very instance. So simply marking it paid here is enough: the order's paid event
            //is raised by that tail, exactly once. Calling MarkOrderAsPaidAsync would raise a SECOND one, and the
            //buyer would be told twice that the payment arrived while the handover desk got the task twice (seen in
            //testing: order #5053, two of every notification 32ms apart). Both places that raise OrderPlacedEvent —
            //checkout and the recurring-payment run — end with that same tail.
            order.PaymentStatusId = (int)PaymentStatus.Paid;
            order.PaidDateUtc = DateTime.UtcNow;
            await _orderService.UpdateOrderAsync(order);

            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = "Order has been marked as paid",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
        }
        catch (Exception exception)
        {
            //the order itself is placed and must stand; the admin can still mark it as paid by hand
            await _logger.ErrorAsync($"Check / Money Order: order #{order.Id} could not be marked as paid automatically.", exception);
        }
    }

    #endregion
}
