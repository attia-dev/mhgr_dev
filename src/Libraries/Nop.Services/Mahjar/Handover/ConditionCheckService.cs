// [Mahjar handover]
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Mahjar;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Mahjar.ItemStatus;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Shipping;

namespace Nop.Services.Mahjar.Handover;

/// <summary>
/// The handover desk service implementation. Photos are standard nopCommerce pictures; videos are streamed to
/// <see cref="CONDITION_MEDIA_PATH"/> exactly like the item-status videos (fresh file name on every write because
/// static files are cached for a year, extension floor, row repointed before the old file is unlinked).
///
/// NOTE on dependencies: <see cref="IOrderProcessingService"/> depends on the contract service, which depends on this
/// service (the send-contract guard), so it is resolved lazily where the order is delivered — never injected.
/// [Mahjar handover]
/// </summary>
public partial class ConditionCheckService : IConditionCheckService
{
    #region Constants

    /// <summary>Where uploaded condition-check videos live (writable, survives redeploys, range-served).</summary>
    public const string CONDITION_MEDIA_PATH = "/images/uploaded/condition-checks/";

    /// <summary>The item-status upload tree (the product condition report), which a receipt writes into.</summary>
    protected const string ITEM_STATUS_MEDIA_PATH = "/images/uploaded/item-status/";

    /// <summary>The per-order-item generic attribute the app reads the rental return state from.</summary>
    public const string RENTAL_RETURN_STATUS_ATTRIBUTE = "MahjarRentalReturnStatus";

    /// <summary>
    /// The hard floor of what may ever be written into the upload trees. The allowed-extensions setting can narrow
    /// it but never widen it into something a browser would run from the store's own origin.
    /// </summary>
    private static readonly string[] _videoExtensionFloor = [".mp4", ".webm", ".mov", ".m4v"];

    private static readonly char[] _separator = [','];

    /// <summary>
    /// One delivery sync at a time (the site runs as a single instance): two items of one order confirmed at the same
    /// moment must not each put the full remaining quantity into a shipment.
    /// </summary>
    private static readonly SemaphoreSlim _deliverySyncLock = new(1, 1);

    /// <summary>The caption column is nvarchar(400).</summary>
    private const int MAX_CAPTION_LENGTH = 400;

    #endregion

    #region Fields

    protected readonly CurrencySettings _currencySettings;
    protected readonly ICurrencyService _currencyService;
    protected readonly IEventPublisher _eventPublisher;
    protected readonly IGenericAttributeService _genericAttributeService;
    protected readonly IHandoverAssignmentService _assignmentService; //[Mahjar handover]
    protected readonly IItemStatusService _itemStatusService;
    protected readonly ILanguageService _languageService;
    protected readonly ILocalizationService _localizationService;
    protected readonly INopFileProvider _fileProvider;
    protected readonly IOrderService _orderService;
    protected readonly IPictureService _pictureService;
    protected readonly IPriceFormatter _priceFormatter;
    protected readonly IProductService _productService;
    protected readonly IRepository<ConditionCheck> _checkRepository;
    protected readonly IRepository<ConditionCheckPicture> _checkPictureRepository;
    protected readonly IRepository<ContractRequest> _contractRequestRepository;
    protected readonly IRepository<HandoverAssignment> _assignmentRepository; //[Mahjar handover]
    protected readonly IRepository<Order> _orderRepository;
    protected readonly IRepository<OrderItem> _orderItemRepository;
    protected readonly IRepository<Shipment> _shipmentRepository;
    protected readonly IRepository<ShipmentItem> _shipmentItemRepository;
    protected readonly IShipmentService _shipmentService;
    protected readonly IShortTermCacheManager _shortTermCacheManager;
    protected readonly IWorkContext _workContext;
    protected readonly ItemStatusSettings _itemStatusSettings;

    #endregion

    #region Ctor

    public ConditionCheckService(CurrencySettings currencySettings,
        ICurrencyService currencyService,
        IEventPublisher eventPublisher,
        IGenericAttributeService genericAttributeService,
        IHandoverAssignmentService assignmentService,
        IItemStatusService itemStatusService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        INopFileProvider fileProvider,
        IOrderService orderService,
        IPictureService pictureService,
        IPriceFormatter priceFormatter,
        IProductService productService,
        IRepository<ConditionCheck> checkRepository,
        IRepository<ConditionCheckPicture> checkPictureRepository,
        IRepository<ContractRequest> contractRequestRepository,
        IRepository<HandoverAssignment> assignmentRepository,
        IRepository<Order> orderRepository,
        IRepository<OrderItem> orderItemRepository,
        IRepository<Shipment> shipmentRepository,
        IRepository<ShipmentItem> shipmentItemRepository,
        IShipmentService shipmentService,
        IShortTermCacheManager shortTermCacheManager,
        IWorkContext workContext,
        ItemStatusSettings itemStatusSettings)
    {
        _currencySettings = currencySettings;
        _currencyService = currencyService;
        _eventPublisher = eventPublisher;
        _genericAttributeService = genericAttributeService;
        _assignmentService = assignmentService;
        _itemStatusService = itemStatusService;
        _languageService = languageService;
        _localizationService = localizationService;
        _fileProvider = fileProvider;
        _orderService = orderService;
        _pictureService = pictureService;
        _priceFormatter = priceFormatter;
        _productService = productService;
        _checkRepository = checkRepository;
        _checkPictureRepository = checkPictureRepository;
        _contractRequestRepository = contractRequestRepository;
        _assignmentRepository = assignmentRepository;
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _shipmentRepository = shipmentRepository;
        _shipmentItemRepository = shipmentItemRepository;
        _shipmentService = shipmentService;
        _shortTermCacheManager = shortTermCacheManager;
        _workContext = workContext;
        _itemStatusSettings = itemStatusSettings;
    }

    #endregion

    #region Utilities

    /// <summary>A locale resource in the current working language.</summary>
    protected virtual Task<string> ResourceAsync(string key)
    {
        return _localizationService.GetResourceAsync(key);
    }

    /// <summary>The frozen cart snapshot of a contract request (never null; a corrupt one reads as empty).</summary>
    protected static ContractCartSnapshot DeserializeSnapshot(ContractRequest request)
    {
        if (string.IsNullOrEmpty(request?.CartSnapshotJson))
            return new ContractCartSnapshot();

        try
        {
            return JsonSerializer.Deserialize<ContractCartSnapshot>(request.CartSnapshotJson) ?? new ContractCartSnapshot();
        }
        catch (JsonException)
        {
            return new ContractCartSnapshot();
        }
    }

    /// <summary>Throws the localized "locked" error unless the check is still a draft.</summary>
    protected virtual async Task EnsureEditableAsync(ConditionCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (!IsEditable(check))
            throw new NopException(await ResourceAsync("admin.mahjar.handover.locked"));
    }

    /// <summary>The SEO file name used for the photos of a product's checks.</summary>
    protected virtual async Task<string> GetSeoNameAsync(int productId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        return await _pictureService.GetPictureSeNameAsync(product?.Name ?? "condition-check");
    }

    /// <summary>
    /// Copies a picture into a NEW picture row. Checks never share picture rows with each other or with the product
    /// condition report: deleting a photo there must never delete the evidence here.
    /// </summary>
    protected virtual async Task<Picture> CopyPictureAsync(int pictureId, string seoFilename)
    {
        var source = await _pictureService.GetPictureByIdAsync(pictureId);
        if (source == null)
            return null;

        var binary = await _pictureService.LoadPictureBinaryAsync(source);
        if (binary == null || binary.Length == 0)
            return null;

        //already validated (and downscaled) when it was first uploaded
        return await _pictureService.InsertPictureAsync(binary, source.MimeType, seoFilename,
            source.AltAttribute, source.TitleAttribute, isNew: true, validateBinary: false);
    }

    /// <summary>
    /// Maps a site-relative upload path to an absolute path, but only inside our two upload trees and never with a
    /// "." or ".." segment; null otherwise.
    /// </summary>
    protected virtual string ResolveUploadedFile(string virtualPath)
    {
        if (string.IsNullOrEmpty(virtualPath))
            return null;

        if (!virtualPath.StartsWith(CONDITION_MEDIA_PATH, StringComparison.InvariantCultureIgnoreCase) &&
            !virtualPath.StartsWith(ITEM_STATUS_MEDIA_PATH, StringComparison.InvariantCultureIgnoreCase))
            return null;

        var segments = virtualPath.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment == ".." || segment == "."))
            return null;

        return _fileProvider.GetAbsolutePath(segments);
    }

    /// <summary>
    /// Unlinks an uploaded video, best-effort: on Windows the file may be open under the static-file middleware
    /// streaming it to somebody, and leaking one file beats failing the caller.
    /// </summary>
    protected virtual void DeleteUploadedFile(string virtualPath)
    {
        var absolutePath = ResolveUploadedFile(virtualPath);
        if (absolutePath == null)
            return;

        try
        {
            if (_fileProvider.FileExists(absolutePath))
                _fileProvider.DeleteFile(absolutePath);
        }
        catch (IOException)
        {
            //locked — leak it rather than break the caller
        }
        catch (UnauthorizedAccessException)
        {
            //ditto for a permissions problem on the upload tree
        }
    }

    /// <summary>
    /// Copies a hosted video into /images/uploaded/{folder}/{id}/ under a fresh name. Returns the new site-relative
    /// path, or null when the source is missing, outside our trees or not a video we accept.
    /// </summary>
    protected virtual string CopyVideoFile(string sourceVirtualPath, string targetFolder, int targetId)
    {
        var sourcePath = ResolveUploadedFile(sourceVirtualPath);
        if (sourcePath == null || !_fileProvider.FileExists(sourcePath))
            return null;

        var extension = _fileProvider.GetFileExtension(sourcePath)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_videoExtensionFloor.Contains(extension))
            return null;

        var directoryPath = _fileProvider.GetAbsolutePath("images", "uploaded", targetFolder, targetId.ToString());
        _fileProvider.CreateDirectory(directoryPath);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        _fileProvider.FileCopy(sourcePath, _fileProvider.Combine(directoryPath, fileName));

        return $"/images/uploaded/{targetFolder}/{targetId}/{fileName}";
    }

    /// <summary>Formats an amount in the primary store currency for a language.</summary>
    protected virtual async Task<string> FormatAmountAsync(decimal amount, int languageId)
    {
        var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
        return await _priceFormatter.FormatPriceAsync(amount, true, currency, languageId, false);
    }

    /// <summary>The language customer-visible order notes are written in: the order's, else the first published.</summary>
    protected virtual async Task<int> GetOrderLanguageIdAsync(Order order)
    {
        var language = order.CustomerLanguageId > 0 ? await _languageService.GetLanguageByIdAsync(order.CustomerLanguageId) : null;
        if (language is { Published: true })
            return language.Id;

        return (await _languageService.GetAllLanguagesAsync()).FirstOrDefault()?.Id ?? 0;
    }

    /// <summary>A product's name in a language (empty when the product is gone).</summary>
    protected virtual async Task<string> GetProductNameAsync(int productId, int languageId)
    {
        var product = await _productService.GetProductByIdAsync(productId);
        return product == null ? string.Empty : await _localizationService.GetLocalizedAsync(product, p => p.Name, languageId);
    }

    /// <summary>Adds a customer-visible order note.</summary>
    protected virtual async Task AddOrderNoteAsync(Order order, string note)
    {
        await _orderService.InsertOrderNoteAsync(new OrderNote
        {
            OrderId = order.Id,
            Note = note,
            DisplayToCustomer = true,
            CreatedOnUtc = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Whether an order item was handed over: a confirmed delivery check, the whole stock order delivered, or the item
    /// sitting in a DELIVERED stock shipment (the stock order screens can deliver one shipment of a partially shipped
    /// order, which leaves the order itself only PartiallyShipped).
    /// </summary>
    protected virtual async Task<bool> IsDeliveredAsync(OrderItem orderItem, Order order)
    {
        if (order.ShippingStatus == ShippingStatus.Delivered)
            return true;

        var orderItemId = orderItem.Id;
        var shipments = _shipmentRepository.Table;
        if (await _shipmentItemRepository.Table
                .Where(item => item.OrderItemId == orderItemId &&
                               shipments.Any(shipment => shipment.Id == item.ShipmentId && shipment.DeliveryDateUtc != null))
                .CountAsync() > 0)
            return true;

        return await _checkRepository.Table
            .Where(check => check.OrderItemId == orderItemId &&
                            check.CheckTypeId == (int)ConditionCheckType.Delivery &&
                            check.StatusId == (int)ConditionCheckStatus.Confirmed)
            .CountAsync() > 0;
    }

    /// <summary>
    /// The check two concurrent requests raced to create: the unique checkpoint indexes let exactly one insert through,
    /// and the loser returns the winner's row.
    /// </summary>
    protected virtual async Task<ConditionCheck> FindConcurrentCheckAsync(ConditionCheck check)
    {
        var typeId = check.CheckTypeId;
        var productId = check.ProductId;
        var contractRequestId = check.ContractRequestId;
        var orderItemId = check.OrderItemId;

        var query = check.CheckType switch
        {
            ConditionCheckType.Intake => _checkRepository.Table.Where(c => c.CheckTypeId == typeId && c.ProductId == productId &&
                                                                          c.StatusId == (int)ConditionCheckStatus.Draft),
            ConditionCheckType.PreContract => _checkRepository.Table.Where(c => c.CheckTypeId == typeId &&
                                                                               c.ContractRequestId == contractRequestId &&
                                                                               c.ProductId == productId),
            _ => _checkRepository.Table.Where(c => c.CheckTypeId == typeId && c.OrderItemId == orderItemId)
        };

        return await query.OrderByDescending(c => c.Id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Moves the stock order along with every handover, through the stock shipment path, so everything nopCommerce
    /// hangs off a delivery keeps working (Shipment.DeliveryDateUtc, ShippingStatus, order completion, the "delivered"
    /// email): the not-yet-shipped quantity of every item the desk has handed over goes into one new shipment, then
    /// every undelivered shipment whose items were ALL handed over is shipped (or made ready for pickup) and delivered.
    /// The order is PartiallyShipped while items remain and Delivered (then Complete) with the last one.
    /// Returns false when there was nothing to do.
    /// </summary>
    protected virtual async Task<bool> SyncOrderDeliveredAsync(Order order)
    {
        await _deliverySyncLock.WaitAsync();
        try
        {
            //re-read inside the lock, past this request's short-term cache (it still holds the row as loaded before the
            //wait): a concurrent sync may have shipped / delivered it, an admin cancelled or refunded it. The fresh row is
            //cached again, so ShipAsync / DeliverAsync below work on — and write back — the current state
            _shortTermCacheManager.Remove(NopEntityCacheDefaults<Order>.ByIdCacheKey.Key, order.Id);
            order = await _orderService.GetOrderByIdAsync(order.Id);
            if (order == null || order.Deleted || order.OrderStatus == OrderStatus.Cancelled || order.PaymentStatus != PaymentStatus.Paid)
                return false;

            return await SyncOrderDeliveredCoreAsync(order);
        }
        finally
        {
            _deliverySyncLock.Release();
        }
    }

    /// <summary>The body of <see cref="SyncOrderDeliveredAsync"/>, run under its lock.</summary>
    protected virtual async Task<bool> SyncOrderDeliveredCoreAsync(Order order)
    {
        if (order.ShippingStatus is ShippingStatus.ShippingNotRequired or ShippingStatus.Delivered)
            return false;

        var orderId = order.Id;
        var handedOverItemIds = (await _checkRepository.Table
                .Where(check => check.OrderId == orderId &&
                                check.CheckTypeId == (int)ConditionCheckType.Delivery &&
                                check.StatusId == (int)ConditionCheckStatus.Confirmed)
                .Select(check => check.OrderItemId)
                .ToListAsync())
            .ToHashSet();
        if (!handedOverItemIds.Any())
            return false;

        //an item that ships nothing (not ship-enabled) has no shipment to carry it, so the ORDER must not turn Delivered
        //before the desk has handed that one over too. An item that went out whole in delivered stock shipments (the
        //order screens) was handed over as well — without it such an order would be held at Shipped forever
        var shipments = _shipmentRepository.Table;
        var deliveredQuantities = (await _shipmentItemRepository.Table
                .Where(item => shipments.Any(shipment => shipment.Id == item.ShipmentId && shipment.OrderId == orderId &&
                                                         shipment.DeliveryDateUtc != null))
                .Select(item => new { item.OrderItemId, item.Quantity })
                .ToListAsync())
            .GroupBy(item => item.OrderItemId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var everyItemHandedOver = (await _orderService.GetOrderItemsAsync(order.Id))
            .All(orderItem => handedOverItemIds.Contains(orderItem.Id) ||
                              (deliveredQuantities.TryGetValue(orderItem.Id, out var deliveredQuantity) &&
                               deliveredQuantity >= orderItem.Quantity));

        //resolved lazily: OrderProcessingService -> ContractService -> this service would be a DI cycle
        var orderProcessingService = EngineContext.Current.Resolve<IOrderProcessingService>();

        var shipmentItems = new List<ShipmentItem>();
        decimal? totalWeight = null;
        foreach (var orderItem in await _orderService.GetOrderItemsAsync(order.Id, isShipEnabled: true))
        {
            //only what the desk has actually handed over leaves with a shipment
            if (!handedOverItemIds.Contains(orderItem.Id))
                continue;

            var quantity = await _orderService.GetTotalNumberOfItemsCanBeAddedToShipmentAsync(orderItem);
            if (quantity <= 0)
                continue;

            var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
            if (orderItem.ItemWeight.HasValue)
                totalWeight = (totalWeight ?? decimal.Zero) + orderItem.ItemWeight.Value * quantity;

            //a multi-warehouse product reserved its stock in one of its warehouses when the order was placed; the
            //shipment has to name THAT warehouse or shipping never books the reservation (stock admin makes the
            //operator pick it). Everything else ships from product.WarehouseId, as the stock AddShipment does.
            var warehouseId = product?.WarehouseId ?? 0;
            if (product is { ManageInventoryMethod: ManageInventoryMethod.ManageStock, UseMultipleWarehouses: true })
            {
                var records = (await _productService.GetAllProductWarehouseInventoryRecordsAsync(product.Id))
                    .OrderByDescending(record => record.ReservedQuantity)
                    .ToList();
                warehouseId = (records.FirstOrDefault(record => record.ReservedQuantity >= quantity) ?? records.FirstOrDefault())?.WarehouseId
                              ?? warehouseId;
            }

            shipmentItems.Add(new ShipmentItem
            {
                OrderItemId = orderItem.Id,
                Quantity = quantity,
                WarehouseId = warehouseId
            });
        }

        var changed = false;
        if (shipmentItems.Any())
        {
            var shipment = new Shipment
            {
                OrderId = order.Id,
                TotalWeight = totalWeight,
                AdminComment = "Created by the handover desk when the item was handed over [Mahjar handover]",
                CreatedOnUtc = DateTime.UtcNow
            };
            await _shipmentService.InsertShipmentAsync(shipment);

            foreach (var shipmentItem in shipmentItems)
            {
                shipmentItem.ShipmentId = shipment.Id;
                await _shipmentService.InsertShipmentItemAsync(shipmentItem);
            }

            await _eventPublisher.PublishAsync(new ShipmentCreatedEvent(shipment));
            changed = true;
        }

        foreach (var shipment in await _shipmentService.GetShipmentsByOrderIdAsync(order.Id))
        {
            if (shipment.DeliveryDateUtc.HasValue)
                continue;

            //a shipment made in the stock order screens may still hold items the desk has not handed over
            var items = await _shipmentService.GetShipmentItemsByShipmentIdAsync(shipment.Id);
            if (!items.Any() || !items.All(item => handedOverItemIds.Contains(item.OrderItemId)))
                continue;

            //a pickup order goes ready-for-pickup -> delivered, a courier order shipped -> delivered
            if (order.PickupInStore)
            {
                if (!shipment.ReadyForPickupDateUtc.HasValue)
                {
                    await orderProcessingService.ReadyForPickupAsync(shipment, false);
                    changed = true;
                }
            }
            else if (!shipment.ShippedDateUtc.HasValue)
            {
                await orderProcessingService.ShipAsync(shipment, false);
                changed = true;
            }

            //the shipment that leaves nothing else to deliver turns the ORDER Delivered, so it waits while an item without
            //a shipment is still to be handed over at the desk (that item's confirmation delivers it)
            if (!everyItemHandedOver && await IsLastUndeliveredShipmentAsync(order, shipment))
                continue;

            await orderProcessingService.DeliverAsync(shipment, true);
            changed = true;
        }

        return changed;
    }

    /// <summary>Whether delivering this shipment would leave nothing else of the order to ship or deliver.</summary>
    protected virtual async Task<bool> IsLastUndeliveredShipmentAsync(Order order, Shipment shipment)
    {
        if (await _orderService.HasItemsToAddToShipmentAsync(order))
            return false;

        return (await _shipmentService.GetShipmentsByOrderIdAsync(order.Id))
            .All(other => other.Id == shipment.Id || other.DeliveryDateUtc.HasValue);
    }

    /// <summary>
    /// A receipt (intake or rental return) becomes the product's public condition report ("item status") at once:
    /// its photos and video REPLACE the report's — they document the item as it is now — the grade and summary are
    /// set when this check recorded them (a curated text is never blanked), and damage is APPENDED to the condition
    /// notes so a buyer sees the item's history.
    /// </summary>
    protected virtual async Task PublishToProductStatusAsync(ConditionCheck check)
    {
        var now = DateTime.UtcNow;

        var itemStatus = await _itemStatusService.GetItemStatusByProductIdAsync(check.ProductId);
        if (itemStatus == null)
        {
            itemStatus = new ProductItemStatus
            {
                ProductId = check.ProductId,
                ConditionGradeId = (int)ItemConditionGrade.NotSpecified,
                ShowOnProductPage = true,
                UpdatedOnUtc = now
            };
            await _itemStatusService.InsertItemStatusAsync(itemStatus);
        }

        //1) build the replacement FIRST — copies of the check's photos and video (the report's pictures can be deleted
        //   from the product page later on, so they are never shared) — so a storage failure half-way leaves the
        //   product's current report untouched instead of stripped
        var seoName = await GetSeoNameAsync(check.ProductId);
        var newPictures = new List<(Picture Picture, string Caption)>();
        string newVideoFile = null;
        try
        {
            foreach (var source in (await GetPicturesAsync(check.Id)).Take(Math.Max(_itemStatusSettings.MaxPictures, 1)))
            {
                var copy = await CopyPictureAsync(source.PictureId, seoName);
                if (copy != null)
                    newPictures.Add((copy, source.Caption));
            }

            if (!string.IsNullOrEmpty(check.VideoFile))
                newVideoFile = CopyVideoFile(check.VideoFile, "item-status", check.ProductId);
        }
        catch
        {
            foreach (var (picture, _) in newPictures)
                await _pictureService.DeletePictureAsync(picture);
            DeleteUploadedFile(newVideoFile);
            throw;
        }

        //2) swap: the report's photos (and their pictures) go, the copies come in
        foreach (var previousPicture in await _itemStatusService.GetItemStatusPicturesByProductIdAsync(check.ProductId))
        {
            var pictureId = previousPicture.PictureId;
            await _itemStatusService.DeleteItemStatusPictureAsync(previousPicture);

            var picture = await _pictureService.GetPictureByIdAsync(pictureId);
            if (picture != null)
                await _pictureService.DeletePictureAsync(picture);
        }

        var displayOrder = 0;
        foreach (var (picture, caption) in newPictures)
        {
            await _itemStatusService.InsertItemStatusPictureAsync(new ProductItemStatusPicture
            {
                ProductId = check.ProductId,
                PictureId = picture.Id,
                Caption = caption,
                DisplayOrder = displayOrder++
            });
        }

        //video: replaced too — or cleared, when this receipt recorded none (an older video shows an older state)
        var previousVideoFile = itemStatus.VideoFile;
        itemStatus.VideoFile = newVideoFile;
        itemStatus.VideoUrl = newVideoFile == null && ItemStatusVideoUrlHelper.IsAllowed(check.VideoUrl)
            ? check.VideoUrl.Trim()
            : null;

        if (check.ConditionGrade != ItemConditionGrade.NotSpecified)
            itemStatus.ConditionGradeId = check.ConditionGradeId;
        if (!string.IsNullOrWhiteSpace(check.ConditionNotes))
            itemStatus.Summary = check.ConditionNotes.Trim();

        itemStatus.InspectedOnUtc = check.ConfirmedOnUtc ?? now;
        itemStatus.ShowOnProductPage = true;
        itemStatus.UpdatedOnUtc = now;
        await _itemStatusService.UpdateItemStatusAsync(itemStatus);

        //the old file only once the row stopped pointing at it
        if (!string.Equals(previousVideoFile, itemStatus.VideoFile, StringComparison.InvariantCultureIgnoreCase))
            DeleteUploadedFile(previousVideoFile);

        if (check.HasDamage && !string.IsNullOrWhiteSpace(check.DamageDescription))
        {
            var notes = await _itemStatusService.GetItemStatusNotesByProductIdAsync(check.ProductId);
            await _itemStatusService.InsertItemStatusNoteAsync(new ProductItemStatusNote
            {
                ProductId = check.ProductId,
                NoteTypeId = (int)ItemStatusNoteType.Damage,
                SeverityId = check.DamageSeverityId > 0 ? check.DamageSeverityId : (int)ItemStatusNoteSeverity.Minor,
                Text = check.DamageDescription.Trim(),
                DisplayOrder = notes.Any() ? notes.Max(note => note.DisplayOrder) + 1 : 0,
                CreatedOnUtc = now
            });
        }
    }

    /// <summary>Publishes a receipt to the product condition report without ever undoing its confirmation.</summary>
    protected virtual async Task TryPublishToProductStatusAsync(ConditionCheck check, ConditionCheckConfirmResult result)
    {
        try
        {
            await PublishToProductStatusAsync(check);
            result.Messages.Add(await ResourceAsync("admin.mahjar.handover.published"));
        }
        catch (Exception exc)
        {
            result.Warnings.Add(string.Format(await ResourceAsync("admin.mahjar.handover.publishfailed"), exc.Message));
        }
    }

    /// <summary>Loads the orders and contracts referenced by a page of checks, for the queue rows.</summary>
    protected virtual async Task<(Dictionary<int, Order> Orders, Dictionary<int, ContractRequest> Contracts)> LoadReferencesAsync(
        IEnumerable<int> orderIds, IEnumerable<int> contractRequestIds)
    {
        var orderIdArray = orderIds.Where(id => id > 0).Distinct().ToArray();
        var contractIdArray = contractRequestIds.Where(id => id > 0).Distinct().ToArray();

        var orders = orderIdArray.Any()
            ? (await _orderRepository.Table.Where(order => orderIdArray.Contains(order.Id)).ToListAsync()).ToDictionary(order => order.Id)
            : new Dictionary<int, Order>();
        var contracts = contractIdArray.Any()
            ? (await _contractRequestRepository.Table.Where(request => contractIdArray.Contains(request.Id)).ToListAsync()).ToDictionary(request => request.Id)
            : new Dictionary<int, ContractRequest>();

        return (orders, contracts);
    }

    #endregion

    #region Methods

    #region Checks

    /// <summary>Gets a condition check by identifier (null if not found).</summary>
    public virtual async Task<ConditionCheck> GetCheckByIdAsync(int id)
    {
        return await _checkRepository.GetByIdAsync(id, cache => default);
    }

    /// <summary>Gets every check of a contract request, ordered by checkpoint.</summary>
    public virtual async Task<IList<ConditionCheck>> GetChecksByContractRequestIdAsync(int contractRequestId)
    {
        if (contractRequestId <= 0)
            return new List<ConditionCheck>();

        return await _checkRepository.Table
            .Where(check => check.ContractRequestId == contractRequestId)
            .OrderBy(check => check.CheckTypeId).ThenBy(check => check.Id)
            .ToListAsync();
    }

    /// <summary>Gets every check of an order, ordered by checkpoint.</summary>
    public virtual async Task<IList<ConditionCheck>> GetChecksByOrderIdAsync(int orderId)
    {
        if (orderId <= 0)
            return new List<ConditionCheck>();

        return await _checkRepository.Table
            .Where(check => check.OrderId == orderId)
            .OrderBy(check => check.CheckTypeId).ThenBy(check => check.Id)
            .ToListAsync();
    }

    /// <summary>The confirmed pre-contract checks of a contract plus the confirmed delivery / return checks of its order.</summary>
    public virtual async Task<IList<ConditionCheck>> GetCustomerVisibleChecksAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestId = request.Id;
        var orderId = request.OrderId;
        return await _checkRepository.Table
            .Where(check => check.StatusId == (int)ConditionCheckStatus.Confirmed &&
                            ((check.CheckTypeId == (int)ConditionCheckType.PreContract && check.ContractRequestId == requestId) ||
                             (orderId > 0 && check.OrderId == orderId &&
                              (check.CheckTypeId == (int)ConditionCheckType.Delivery || check.CheckTypeId == (int)ConditionCheckType.Return))))
            .OrderBy(check => check.CheckTypeId).ThenBy(check => check.Id)
            .ToListAsync();
    }

    /// <summary>Gets the check of a checkpoint, starting a draft when there is none yet.</summary>
    public virtual async Task<ConditionCheck> GetOrCreateCheckAsync(ConditionCheckType type, int productId, int contractRequestId,
        int orderItemId, Customer employee)
    {
        ArgumentNullException.ThrowIfNull(employee);

        var now = DateTime.UtcNow;
        var check = new ConditionCheck
        {
            CheckType = type,
            Status = ConditionCheckStatus.Draft,
            CreatedByCustomerId = employee.Id,
            CreatedOnUtc = now,
            UpdatedOnUtc = now
        };

        ConditionCheck existing;
        switch (type)
        {
            case ConditionCheckType.Intake:
            {
                var product = await _productService.GetProductByIdAsync(productId);
                if (product == null || product.Deleted)
                    throw new NopException(await ResourceAsync("admin.mahjar.handover.notfound"));

                //an intake in progress is continued; a confirmed one is history, so a new receipt starts fresh
                existing = await _checkRepository.Table
                    .Where(c => c.CheckTypeId == (int)ConditionCheckType.Intake && c.ProductId == productId &&
                                c.StatusId == (int)ConditionCheckStatus.Draft)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                check.ProductId = productId;
                break;
            }
            case ConditionCheckType.PreContract:
            {
                var request = await _contractRequestRepository.GetByIdAsync(contractRequestId, cache => default);
                if (request == null || DeserializeSnapshot(request).Lines.All(line => line.ProductId != productId))
                    throw new NopException(await ResourceAsync("admin.mahjar.handover.notfound"));

                existing = await _checkRepository.Table
                    .Where(c => c.CheckTypeId == (int)ConditionCheckType.PreContract &&
                                c.ContractRequestId == contractRequestId && c.ProductId == productId)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                if (existing == null && request.Status != ContractRequestStatus.Pending)
                    throw new NopException(await ResourceAsync("admin.mahjar.handover.precontract.notpending"));

                check.ProductId = productId;
                check.ContractRequestId = contractRequestId;
                break;
            }
            case ConditionCheckType.Delivery:
            case ConditionCheckType.Return:
            {
                var orderItem = await _orderService.GetOrderItemByIdAsync(orderItemId);
                var order = orderItem == null ? null : await _orderService.GetOrderByIdAsync(orderItem.OrderId);
                if (orderItem == null || order == null || order.Deleted)
                    throw new NopException(await ResourceAsync("admin.mahjar.handover.notfound"));

                if (type == ConditionCheckType.Return && !orderItem.RentalStartDateUtc.HasValue)
                    throw new NopException(await ResourceAsync("admin.mahjar.handover.return.notrental"));

                //[Mahjar handover] an assigned order belongs to its assignee: nobody else starts its handover or return
                if (!await _assignmentService.MayWorkOrderAsync(order.Id, employee))
                    throw new NopException(await ResourceAsync("admin.mahjar.handover.assign.notyours"));

                existing = await _checkRepository.Table
                    .Where(c => c.CheckTypeId == (int)type && c.OrderItemId == orderItemId)
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                check.ProductId = orderItem.ProductId;
                check.OrderId = order.Id;
                check.OrderItemId = orderItem.Id;

                //link the contract the order was placed against, when there is one
                var orderId = order.Id;
                check.ContractRequestId = (await _contractRequestRepository.Table
                    .Where(request => request.OrderId == orderId)
                    .OrderByDescending(request => request.Id)
                    .FirstOrDefaultAsync())?.Id ?? 0;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (existing != null)
            return existing;

        try
        {
            await _checkRepository.InsertAsync(check);
            return check;
        }
        catch (Exception)
        {
            //two requests opened the same queue row at once: the unique checkpoint index let one insert through
            var winner = await FindConcurrentCheckAsync(check);
            if (winner == null)
                throw;

            return winner;
        }
    }

    /// <summary>Saves the editable fields of a draft check.</summary>
    public virtual async Task UpdateCheckAsync(ConditionCheck check)
    {
        await EnsureEditableAsync(check);

        check.UpdatedOnUtc = DateTime.UtcNow;
        await _checkRepository.UpdateAsync(check);
    }

    /// <summary>Deletes a draft check with its photos and video.</summary>
    public virtual async Task DeleteDraftAsync(ConditionCheck check)
    {
        await EnsureEditableAsync(check);

        foreach (var checkPicture in await GetPicturesAsync(check.Id))
        {
            var pictureId = checkPicture.PictureId;
            await _checkPictureRepository.DeleteAsync(checkPicture);

            var picture = await _pictureService.GetPictureByIdAsync(pictureId);
            if (picture != null)
                await _pictureService.DeletePictureAsync(picture);
        }

        DeleteUploadedFile(check.VideoFile);
        await _checkRepository.DeleteAsync(check);

        try
        {
            var directoryPath = _fileProvider.GetAbsolutePath("images", "uploaded", "condition-checks", check.Id.ToString());
            if (_fileProvider.DirectoryExists(directoryPath))
                _fileProvider.DeleteDirectory(directoryPath);
        }
        catch (IOException)
        {
            //a file still held open by the static-file middleware must not break the deletion
        }
        catch (UnauthorizedAccessException)
        {
            //ditto for a permissions problem on the upload tree
        }
    }

    /// <summary>Whether the check can still be edited.</summary>
    public virtual bool IsEditable(ConditionCheck check)
    {
        return check?.Status == ConditionCheckStatus.Draft;
    }

    /// <summary>Whether the check has at least one photo or a video.</summary>
    public virtual async Task<bool> HasMediaAsync(ConditionCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (!string.IsNullOrEmpty(check.VideoFile) || ItemStatusVideoUrlHelper.IsAllowed(check.VideoUrl))
            return true;

        var checkId = check.Id;
        return await _checkPictureRepository.Table.Where(picture => picture.ConditionCheckId == checkId).CountAsync() > 0;
    }

    /// <summary>Confirms and locks an intake / delivery / return check and applies its checkpoint.</summary>
    public virtual async Task<ConditionCheckConfirmResult> ConfirmAsync(ConditionCheck check, Customer employee)
    {
        ArgumentNullException.ThrowIfNull(employee);
        await EnsureEditableAsync(check);

        if (check.CheckType == ConditionCheckType.PreContract)
            throw new NopException(await ResourceAsync("admin.mahjar.handover.precontract.lockedonsend"));

        if (!await HasMediaAsync(check))
            throw new NopException(await ResourceAsync("admin.mahjar.handover.mediarequired"));

        //one confirmed delivery / return per order item, ever
        if (check.CheckType is ConditionCheckType.Delivery or ConditionCheckType.Return)
        {
            var checkId = check.Id;
            var itemId = check.OrderItemId;
            var typeId = check.CheckTypeId;
            if (await _checkRepository.Table
                    .Where(c => c.Id != checkId && c.OrderItemId == itemId && c.CheckTypeId == typeId &&
                                c.StatusId == (int)ConditionCheckStatus.Confirmed)
                    .CountAsync() > 0)
                throw new NopException(await ResourceAsync("admin.mahjar.handover.alreadyrecorded"));
        }

        OrderItem orderItem = null;
        Order order = null;
        if (check.CheckType is ConditionCheckType.Delivery or ConditionCheckType.Return)
        {
            orderItem = await _orderService.GetOrderItemByIdAsync(check.OrderItemId);
            order = orderItem == null ? null : await _orderService.GetOrderByIdAsync(orderItem.OrderId);
            if (orderItem == null || order == null || order.Deleted || order.OrderStatus == OrderStatus.Cancelled)
                throw new NopException(await ResourceAsync("admin.mahjar.handover.notfound"));
        }

        //[Mahjar handover] the same rule at the moment it counts: only the assignee records the handover or the return
        if (check.CheckType is ConditionCheckType.Delivery or ConditionCheckType.Return &&
            !await _assignmentService.MayWorkOrderAsync(order.Id, employee))
        {
            throw new NopException(await ResourceAsync("admin.mahjar.handover.assign.notyours"));
        }

        if (check.CheckType == ConditionCheckType.Delivery && order.PaymentStatus != PaymentStatus.Paid)
            throw new NopException(await ResourceAsync("admin.mahjar.handover.delivery.notpaid"));

        var workingLanguage = await _workContext.GetWorkingLanguageAsync();
        if (check.CheckType == ConditionCheckType.Return)
        {
            if (!orderItem.RentalStartDateUtc.HasValue)
                throw new NopException(await ResourceAsync("admin.mahjar.handover.return.notrental"));

            if (!await IsDeliveredAsync(orderItem, order))
                throw new NopException(await ResourceAsync("admin.mahjar.handover.return.notdelivered"));

            var deposit = await GetRentalDepositAsync(orderItem);
            var deduction = Math.Round(Math.Max(check.DamageDeduction, decimal.Zero), 2);
            if (deduction > deposit)
                throw new NopException(string.Format(await ResourceAsync("admin.mahjar.handover.return.deductiontoohigh"),
                    await FormatAmountAsync(deposit, workingLanguage.Id)));

            check.DepositAmount = deposit;
            check.DamageDeduction = deduction;
        }
        else
        {
            check.DepositAmount = decimal.Zero;
            check.DamageDeduction = decimal.Zero;
        }

        //claim the Draft -> Confirmed transition ATOMICALLY (UPDATE ... WHERE StatusId = Draft): of two concurrent
        //confirms of one check only one gets the row, so the side effects below — order delivered, deposit, product
        //page, notifications — can never run twice
        var now = DateTime.UtcNow;
        var claimedCheckId = check.Id;
        var employeeId = employee.Id;
        var claimed = await LinqToDB.LinqExtensions.UpdateAsync(
            _checkRepository.Table.Where(c => c.Id == claimedCheckId && c.StatusId == (int)ConditionCheckStatus.Draft),
            c => new ConditionCheck
            {
                StatusId = (int)ConditionCheckStatus.Confirmed,
                ConfirmedByCustomerId = employeeId,
                ConfirmedOnUtc = now,
                UpdatedOnUtc = now
            });
        if (claimed == 0)
            throw new NopException(await ResourceAsync("admin.mahjar.handover.locked"));

        //then the full row (deposit / deduction) through the repository, whose entity event keeps the cache honest
        check.Status = ConditionCheckStatus.Confirmed;
        check.ConfirmedByCustomerId = employeeId;
        check.ConfirmedOnUtc = now;
        check.UpdatedOnUtc = now;
        await _checkRepository.UpdateAsync(check);

        var result = new ConditionCheckConfirmResult();
        switch (check.CheckType)
        {
            case ConditionCheckType.Intake:
                await TryPublishToProductStatusAsync(check, result);
                break;

            case ConditionCheckType.Delivery:
            {
                var languageId = await GetOrderLanguageIdAsync(order);
                await AddOrderNoteAsync(order, string.Format(
                    await _localizationService.GetResourceAsync("mahjar.handover.ordernote.delivered", languageId),
                    await GetProductNameAsync(check.ProductId, languageId)));

                //the stock order follows every handover: this item's shipment is delivered now, the order with its last item
                try
                {
                    if (await SyncOrderDeliveredAsync(order))
                    {
                        var updatedOrder = await _orderService.GetOrderByIdAsync(order.Id);
                        result.Messages.Add(await ResourceAsync(updatedOrder?.ShippingStatus == ShippingStatus.Delivered
                            ? "admin.mahjar.handover.delivery.orderupdated"
                            : "admin.mahjar.handover.delivery.partial"));
                    }
                }
                catch (Exception exc)
                {
                    result.Warnings.Add(string.Format(await ResourceAsync("admin.mahjar.handover.delivery.syncfailed"), exc.Message));
                }

                break;
            }

            case ConditionCheckType.Return:
            {
                //the state the app's order page already understands (ReturnRequested / Returned / Refunded)
                await _genericAttributeService.SaveAttributeAsync(orderItem, RENTAL_RETURN_STATUS_ATTRIBUTE, "Returned");

                var refund = check.DepositAmount - check.DamageDeduction;
                var languageId = await GetOrderLanguageIdAsync(order);
                await AddOrderNoteAsync(order, string.Format(
                    await _localizationService.GetResourceAsync("mahjar.handover.ordernote.returned", languageId),
                    await GetProductNameAsync(check.ProductId, languageId),
                    await FormatAmountAsync(check.DepositAmount, languageId),
                    await FormatAmountAsync(check.DamageDeduction, languageId),
                    await FormatAmountAsync(refund, languageId)));

                result.Messages.Add(string.Format(await ResourceAsync("admin.mahjar.handover.return.refundnote"),
                    await FormatAmountAsync(refund, workingLanguage.Id)));

                await TryPublishToProductStatusAsync(check, result);
                break;
            }
        }

        await _eventPublisher.PublishAsync(new ConditionCheckConfirmedEvent(check));

        return result;
    }

    /// <summary>Whether the desk handed an order item over (it has a confirmed delivery check).</summary>
    public virtual async Task<bool> IsHandedOverByDeskAsync(int orderItemId)
    {
        if (orderItemId <= 0)
            return false;

        return await _checkRepository.Table
            .Where(check => check.OrderItemId == orderItemId &&
                            check.CheckTypeId == (int)ConditionCheckType.Delivery &&
                            check.StatusId == (int)ConditionCheckStatus.Confirmed)
            .CountAsync() > 0;
    }

    /// <summary>
    /// Whether an order item was handed over to the customer: a confirmed desk delivery, the whole order delivered, or
    /// the item in a delivered stock shipment.
    /// </summary>
    public virtual Task<bool> IsOrderItemDeliveredAsync(OrderItem orderItem, Order order)
    {
        ArgumentNullException.ThrowIfNull(orderItem);
        ArgumentNullException.ThrowIfNull(order);

        return IsDeliveredAsync(orderItem, order);
    }

    /// <summary>Whether every item of a stock shipment was handed over by the desk.</summary>
    public virtual async Task<bool> IsShipmentHandedOverByDeskAsync(int shipmentId)
    {
        var itemIds = (await _shipmentItemRepository.Table
                .Where(item => item.ShipmentId == shipmentId)
                .Select(item => item.OrderItemId)
                .ToListAsync())
            .Distinct()
            .ToArray();
        if (itemIds.Length == 0)
            return false;

        var handedOverIds = (await _checkRepository.Table
                .Where(check => itemIds.Contains(check.OrderItemId) &&
                                check.CheckTypeId == (int)ConditionCheckType.Delivery &&
                                check.StatusId == (int)ConditionCheckStatus.Confirmed)
                .Select(check => check.OrderItemId)
                .ToListAsync())
            .Distinct()
            .Count();

        return handedOverIds == itemIds.Length;
    }

    #endregion

    #region Media

    /// <summary>Gets the photos of a check in display order.</summary>
    public virtual async Task<IList<ConditionCheckPicture>> GetPicturesAsync(int conditionCheckId)
    {
        return await _checkPictureRepository.Table
            .Where(picture => picture.ConditionCheckId == conditionCheckId)
            .OrderBy(picture => picture.DisplayOrder).ThenBy(picture => picture.Id)
            .ToListAsync();
    }

    /// <summary>Gets a check photo by identifier.</summary>
    public virtual async Task<ConditionCheckPicture> GetPictureByIdAsync(int id)
    {
        return await _checkPictureRepository.GetByIdAsync(id, cache => default);
    }

    /// <summary>Adds uploaded photos to a draft check; returns a localized error, or null.</summary>
    public virtual async Task<string> AddPhotosAsync(ConditionCheck check, IList<IFormFile> files)
    {
        await EnsureEditableAsync(check);

        if (files == null || !files.Any())
            return await ResourceAsync("admin.mahjar.handover.photo.addfailed");

        var existing = await GetPicturesAsync(check.Id);
        var count = existing.Count;
        var displayOrder = existing.Any() ? existing.Max(picture => picture.DisplayOrder) + 1 : 0;
        var seoName = await GetSeoNameAsync(check.ProductId);

        foreach (var file in files)
        {
            if (count >= _itemStatusSettings.MaxPictures)
                return string.Format(await ResourceAsync("admin.mahjar.handover.photo.limit"), _itemStatusSettings.MaxPictures);

            //null for an extension the picture service does not accept — never dereference it blindly
            var picture = await _pictureService.InsertPictureAsync(file);
            if (picture == null)
                return await ResourceAsync("admin.mahjar.handover.photo.addfailed");

            await _pictureService.SetSeoFilenameAsync(picture.Id, seoName);
            await _checkPictureRepository.InsertAsync(new ConditionCheckPicture
            {
                ConditionCheckId = check.Id,
                PictureId = picture.Id,
                DisplayOrder = displayOrder++
            });
            count++;
        }

        check.UpdatedOnUtc = DateTime.UtcNow;
        await _checkRepository.UpdateAsync(check);

        return null;
    }

    /// <summary>Updates the caption / display order of a photo of a draft check.</summary>
    public virtual async Task UpdatePictureAsync(ConditionCheckPicture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);
        await EnsureEditableAsync(await GetCheckByIdAsync(picture.ConditionCheckId));

        if (picture.Caption?.Length > MAX_CAPTION_LENGTH)
            throw new NopException(string.Format(await ResourceAsync("admin.mahjar.handover.caption.toolong"), MAX_CAPTION_LENGTH));

        await _checkPictureRepository.UpdateAsync(picture);
    }

    /// <summary>Removes a photo (and its picture) from a draft check.</summary>
    public virtual async Task DeletePictureAsync(ConditionCheckPicture picture)
    {
        ArgumentNullException.ThrowIfNull(picture);
        await EnsureEditableAsync(await GetCheckByIdAsync(picture.ConditionCheckId));

        var pictureId = picture.PictureId;
        await _checkPictureRepository.DeleteAsync(picture);

        var nopPicture = await _pictureService.GetPictureByIdAsync(pictureId);
        if (nopPicture != null)
            await _pictureService.DeletePictureAsync(nopPicture);
    }

    /// <summary>Stores the uploaded video of a draft check.</summary>
    public virtual async Task<(string VideoFile, string Error)> UploadVideoAsync(ConditionCheck check, IFormFile file)
    {
        await EnsureEditableAsync(check);

        if (file == null || file.Length == 0)
            return (null, await ResourceAsync("Admin.Catalog.Products.ItemStatus.Alert.VideoMissing"));

        if (file.Length > (long)_itemStatusSettings.MaxVideoSizeMb * 1024 * 1024)
            return (null, string.Format(await ResourceAsync("Admin.Catalog.Products.ItemStatus.Alert.VideoTooLarge"),
                _itemStatusSettings.MaxVideoSizeMb));

        var allowedExtensions = GetAllowedVideoExtensions();
        var fileExtension = _fileProvider.GetFileExtension(file.FileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            return (null, string.Format(await ResourceAsync("Admin.Catalog.Products.ItemStatus.Alert.VideoBadFormat"),
                string.Join(", ", allowedExtensions)));

        var directoryPath = _fileProvider.GetAbsolutePath("images", "uploaded", "condition-checks", check.Id.ToString());
        _fileProvider.CreateDirectory(directoryPath);

        //a fresh name every time: static files are served with a one year cache
        var fileName = $"{Guid.NewGuid():N}{fileExtension}";
        await using (var stream = new FileStream(_fileProvider.Combine(directoryPath, fileName), FileMode.Create))
        {
            await file.OpenReadStream().CopyToAsync(stream);
            await stream.FlushAsync();
        }

        //repoint the row FIRST, then unlink what it used to point at
        var previousVideoFile = check.VideoFile;
        check.VideoFile = $"{CONDITION_MEDIA_PATH}{check.Id}/{fileName}";
        check.UpdatedOnUtc = DateTime.UtcNow;
        await _checkRepository.UpdateAsync(check);

        DeleteUploadedFile(previousVideoFile);

        return (check.VideoFile, null);
    }

    /// <summary>Removes the uploaded video of a draft check.</summary>
    public virtual async Task RemoveVideoAsync(ConditionCheck check)
    {
        await EnsureEditableAsync(check);

        if (string.IsNullOrEmpty(check.VideoFile))
            return;

        var previousVideoFile = check.VideoFile;
        check.VideoFile = null;
        check.UpdatedOnUtc = DateTime.UtcNow;
        await _checkRepository.UpdateAsync(check);

        DeleteUploadedFile(previousVideoFile);
    }

    /// <summary>Copies the product condition report's photos, video, grade and summary into a draft check.</summary>
    public virtual async Task<int> ImportFromProductStatusAsync(ConditionCheck check)
    {
        await EnsureEditableAsync(check);

        var itemStatus = await _itemStatusService.GetItemStatusByProductIdAsync(check.ProductId);
        if (itemStatus == null)
            return 0;

        var copied = 0;
        var existing = await GetPicturesAsync(check.Id);
        var room = Math.Max(_itemStatusSettings.MaxPictures - existing.Count, 0);
        var displayOrder = existing.Any() ? existing.Max(picture => picture.DisplayOrder) + 1 : 0;
        var seoName = await GetSeoNameAsync(check.ProductId);

        foreach (var source in (await _itemStatusService.GetItemStatusPicturesByProductIdAsync(check.ProductId)).Take(room))
        {
            var copy = await CopyPictureAsync(source.PictureId, seoName);
            if (copy == null)
                continue;

            await _checkPictureRepository.InsertAsync(new ConditionCheckPicture
            {
                ConditionCheckId = check.Id,
                PictureId = copy.Id,
                Caption = source.Caption,
                DisplayOrder = displayOrder++
            });
            copied++;
        }

        if (string.IsNullOrEmpty(check.VideoFile) && string.IsNullOrEmpty(check.VideoUrl))
        {
            if (!string.IsNullOrEmpty(itemStatus.VideoFile))
            {
                var videoFile = CopyVideoFile(itemStatus.VideoFile, "condition-checks", check.Id);
                if (videoFile != null)
                {
                    check.VideoFile = videoFile;
                    copied++;
                }
            }
            else if (ItemStatusVideoUrlHelper.IsAllowed(itemStatus.VideoUrl))
            {
                check.VideoUrl = itemStatus.VideoUrl.Trim();
                copied++;
            }
        }

        if (check.ConditionGrade == ItemConditionGrade.NotSpecified)
            check.ConditionGradeId = itemStatus.ConditionGradeId;
        if (string.IsNullOrWhiteSpace(check.ConditionNotes))
            check.ConditionNotes = itemStatus.Summary;

        check.UpdatedOnUtc = DateTime.UtcNow;
        await _checkRepository.UpdateAsync(check);

        return copied;
    }

    /// <summary>The accepted video extensions, intersected with the hard floor.</summary>
    public virtual IList<string> GetAllowedVideoExtensions()
    {
        return (_itemStatusSettings.AllowedVideoExtensions ?? string.Empty)
            .Split(_separator, StringSplitOptions.RemoveEmptyEntries)
            .Select(extension => extension.Trim().ToLowerInvariant())
            .Where(extension => !string.IsNullOrEmpty(extension))
            .Select(extension => extension.StartsWith('.') ? extension : $".{extension}")
            .Where(extension => _videoExtensionFloor.Contains(extension))
            .ToList();
    }

    #endregion

    #region Contracts

    /// <summary>The products of a contract request that still lack before-signing condition media.</summary>
    public virtual async Task<IList<int>> GetProductIdsMissingPreContractMediaAsync(ContractRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var checks = (await GetChecksByContractRequestIdAsync(request.Id))
            .Where(check => check.CheckType == ConditionCheckType.PreContract)
            .ToList();

        var missing = new List<int>();
        foreach (var productId in DeserializeSnapshot(request).Lines.Select(line => line.ProductId).Distinct())
        {
            var check = checks.Where(c => c.ProductId == productId).OrderByDescending(c => c.Id).FirstOrDefault();
            if (check == null || !await HasMediaAsync(check))
                missing.Add(productId);
        }

        return missing;
    }

    /// <summary>Locks the pre-contract checks of a contract request (the contract was just sent).</summary>
    public virtual async Task LockPreContractChecksAsync(ContractRequest request, Customer employee)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        foreach (var check in (await GetChecksByContractRequestIdAsync(request.Id))
                     .Where(c => c.CheckType == ConditionCheckType.PreContract && c.Status == ConditionCheckStatus.Draft))
        {
            check.Status = ConditionCheckStatus.Confirmed;
            check.ConfirmedByCustomerId = employee?.Id ?? 0;
            check.ConfirmedOnUtc = now;
            check.UpdatedOnUtc = now;
            await _checkRepository.UpdateAsync(check);
        }
    }

    #endregion

    #region Rentals

    /// <summary>
    /// The refundable security deposit of a rental order item. The fork folds the deposit INTO the rental unit price
    /// ([Mahjar rental-deposit]), so it is not on the order item: the frozen contract line is the authority (the
    /// product may have been edited since), with the product's current per-unit deposit as the fallback.
    /// </summary>
    public virtual async Task<decimal> GetRentalDepositAsync(OrderItem orderItem)
    {
        ArgumentNullException.ThrowIfNull(orderItem);

        if (!orderItem.RentalStartDateUtc.HasValue)
            return decimal.Zero;

        var orderId = orderItem.OrderId;
        var request = await _contractRequestRepository.Table
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        if (request != null)
        {
            var rentalLines = DeserializeSnapshot(request).Lines
                .Where(line => line.IsRental && line.ProductId == orderItem.ProductId)
                .ToList();
            var line = rentalLines.FirstOrDefault(l => l.RentalStartUtc?.Date == orderItem.RentalStartDateUtc?.Date &&
                                                       l.RentalEndUtc?.Date == orderItem.RentalEndDateUtc?.Date)
                       ?? rentalLines.FirstOrDefault();
            if (line != null)
                return line.Deposit * orderItem.Quantity;
        }

        var product = await _productService.GetProductByIdAsync(orderItem.ProductId);
        return (product?.RentalSecurityDeposit ?? decimal.Zero) * orderItem.Quantity;
    }

    #endregion

    #region Queues

    /// <summary>
    /// [Mahjar handover] Narrows a queue that is built in memory (the before-signing one) by the same rules the SQL
    /// queues below apply.
    /// </summary>
    protected virtual IList<HandoverTask> ApplyFilter(IEnumerable<HandoverTask> tasks, HandoverTaskFilter filter, string keyword)
    {
        var today = DateTime.UtcNow.Date;

        return tasks.Where(task =>
        {
            //a keyword matches the row when ANY of its parts does
            if (keyword != null &&
                !(filter.KeywordProductIds.Contains(task.ProductId) ||
                  filter.KeywordCustomerIds.Contains(task.CustomerId) ||
                  (filter.KeywordContractRequestId > 0 && task.ContractRequestId == filter.KeywordContractRequestId) ||
                  (!string.IsNullOrEmpty(task.OrderNumber) && task.OrderNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase))))
            {
                return false;
            }

            //the "nobody yet" and "not started" sentinels are themselves positive numbers, so each pair of tests has
            //to be mutually exclusive — an else-if, never two independent ifs
            if (filter.AssignedCustomerId == HandoverTaskFilter.UNASSIGNED)
            {
                if (task.AssignedCustomerId != 0)
                    return false;
            }
            else if (filter.AssignedCustomerId > 0 && task.AssignedCustomerId != filter.AssignedCustomerId)
            {
                return false;
            }

            if (filter.StatusId == HandoverTaskFilter.NOT_STARTED)
            {
                if (task.Check != null)
                    return false;
            }
            else if (filter.StatusId > 0 && task.Check?.StatusId != filter.StatusId)
            {
                return false;
            }

            if (filter.FromUtc.HasValue && task.DateUtc < filter.FromUtc.Value)
                return false;

            if (filter.ToUtc.HasValue && task.DateUtc >= filter.ToUtc.Value)
                return false;

            if (filter.OverdueOnly && !(task.RentalEndUtc.HasValue && task.RentalEndUtc.Value.Date < today))
                return false;

            return true;
        }).ToList();
    }

    /// <summary>
    /// [Mahjar handover] Narrows the two queues whose rows ARE checks (intake in progress, completed). A row only
    /// exists once a check does, so "not started" can never match one.
    /// </summary>
    protected virtual IQueryable<ConditionCheck> ApplyCheckFilter(IQueryable<ConditionCheck> checks, HandoverTaskFilter filter, string keyword)
    {
        var orderTable = _orderRepository.Table;
        var contractTable = _contractRequestRepository.Table;
        var assignmentTable = _assignmentRepository.Table;

        if (keyword != null)
        {
            var productIds = filter.KeywordProductIds.ToArray();
            var customerIds = filter.KeywordCustomerIds.ToArray();
            var contractRequestId = filter.KeywordContractRequestId;
            checks = checks.Where(check =>
                productIds.Contains(check.ProductId) ||
                (contractRequestId > 0 && check.ContractRequestId == contractRequestId) ||
                orderTable.Any(order => order.Id == check.OrderId &&
                                        (customerIds.Contains(order.CustomerId) ||
                                         (order.CustomOrderNumber != null && order.CustomOrderNumber.Contains(keyword)))) ||
                contractTable.Any(request => request.Id == check.ContractRequestId && customerIds.Contains(request.CustomerId)));
        }

        if (filter.AssignedCustomerId == HandoverTaskFilter.UNASSIGNED)
        {
            checks = checks.Where(check => !assignmentTable.Any(assignment => assignment.OrderId == check.OrderId));
        }
        else if (filter.AssignedCustomerId > 0)
        {
            var assignedTo = filter.AssignedCustomerId;
            checks = checks.Where(check => assignmentTable.Any(assignment => assignment.OrderId == check.OrderId &&
                                                                            assignment.AssignedToCustomerId == assignedTo));
        }

        if (filter.StatusId == HandoverTaskFilter.NOT_STARTED)
        {
            //every row here is a check that exists, so nothing can be "not started"
            checks = checks.Where(check => check.Id == 0);
        }
        else if (filter.StatusId > 0)
        {
            var statusId = filter.StatusId;
            checks = checks.Where(check => check.StatusId == statusId);
        }

        if (filter.FromUtc.HasValue)
        {
            var fromUtc = filter.FromUtc.Value;
            checks = checks.Where(check => (check.ConfirmedOnUtc ?? check.UpdatedOnUtc) >= fromUtc);
        }

        if (filter.ToUtc.HasValue)
        {
            var toUtc = filter.ToUtc.Value;
            checks = checks.Where(check => (check.ConfirmedOnUtc ?? check.UpdatedOnUtc) < toUtc);
        }

        if (filter.OverdueOnly)
        {
            var today = DateTime.UtcNow.Date;
            var orderItemTable = _orderItemRepository.Table;
            checks = checks.Where(check => orderItemTable.Any(item => item.Id == check.OrderItemId &&
                                                                      item.RentalEndDateUtc != null &&
                                                                      item.RentalEndDateUtc < today));
        }

        return checks;
    }

    /// <summary>
    /// Gets one page of a handover-desk work queue, narrowed by what the desk searched for (null = the whole queue).
    /// </summary>
    public virtual async Task<IPagedList<HandoverTask>> SearchTasksAsync(HandoverStage stage, HandoverTaskFilter filter = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        pageIndex = Math.Max(pageIndex, 0);
        pageSize = Math.Max(pageSize, 1);
        //the default page size is int.MaxValue, so the offset is computed in long and clamped
        var skip = (int)Math.Min((long)pageIndex * pageSize, int.MaxValue);
        filter ??= new HandoverTaskFilter();
        var keyword = filter.HasKeyword ? filter.Keyword.Trim() : null;

        switch (stage)
        {
            case HandoverStage.AwaitingPreContract:
            {
                //few contracts are ever Pending at once, so the per-item rows are built and paged in memory
                var requests = await _contractRequestRepository.Table
                    .Where(request => request.StatusId == (int)ContractRequestStatus.Pending)
                    .OrderBy(request => request.CreatedOnUtc)
                    .ToListAsync();
                var requestIds = requests.Select(request => request.Id).ToArray();
                var checks = requestIds.Any()
                    ? await _checkRepository.Table
                        .Where(check => check.CheckTypeId == (int)ConditionCheckType.PreContract && requestIds.Contains(check.ContractRequestId))
                        .ToListAsync()
                    : new List<ConditionCheck>();

                var tasks = new List<HandoverTask>();
                foreach (var request in requests)
                {
                    foreach (var line in DeserializeSnapshot(request).Lines.GroupBy(l => l.ProductId).Select(group => group.First()))
                    {
                        tasks.Add(new HandoverTask
                        {
                            Stage = stage,
                            CheckType = ConditionCheckType.PreContract,
                            ProductId = line.ProductId,
                            ContractRequestId = request.Id,
                            CustomerId = request.CustomerId,
                            Quantity = line.Quantity,
                            RentalStartUtc = line.RentalStartUtc,
                            RentalEndUtc = line.RentalEndUtc,
                            DateUtc = request.CreatedOnUtc,
                            Check = checks.Where(c => c.ContractRequestId == request.Id && c.ProductId == line.ProductId)
                                .OrderByDescending(c => c.Id).FirstOrDefault()
                        });
                    }
                }

                //built in memory, so it is narrowed in memory too — before paging, or the pages would lie
                return new PagedList<HandoverTask>(ApplyFilter(tasks, filter, keyword), pageIndex, pageSize);
            }

            case HandoverStage.AwaitingDelivery:
            case HandoverStage.AwaitingReturn:
            {
                var type = stage == HandoverStage.AwaitingDelivery ? ConditionCheckType.Delivery : ConditionCheckType.Return;

                const int deliveryTypeId = (int)ConditionCheckType.Delivery;
                const int returnTypeId = (int)ConditionCheckType.Return;
                const int confirmedStatusId = (int)ConditionCheckStatus.Confirmed;
                const int cancelledOrderStatusId = (int)OrderStatus.Cancelled;
                const int paidPaymentStatusId = (int)PaymentStatus.Paid;
                const int deliveredShippingStatusId = (int)ShippingStatus.Delivered;

                //LinqToDB cannot build a select that projects whole entities out of a join and filters on them
                //afterwards, so the join projects SCALARS and "has a confirmed check" is a correlated EXISTS (Any)
                var checkTable = _checkRepository.Table;
                //an item in a DELIVERED stock shipment was handed over too (partially shipped orders)
                var shipmentItemTable = _shipmentItemRepository.Table;
                var shipmentTable = _shipmentRepository.Table;
                var rows = from orderItem in _orderItemRepository.Table
                           join order in _orderRepository.Table on orderItem.OrderId equals order.Id
                           where !order.Deleted && order.OrderStatusId != cancelledOrderStatusId
                           select new
                           {
                               OrderItemId = orderItem.Id,
                               orderItem.ProductId,
                               orderItem.Quantity,
                               orderItem.RentalStartDateUtc,
                               orderItem.RentalEndDateUtc,
                               OrderId = order.Id,
                               order.CustomOrderNumber,
                               order.CustomerId,
                               order.PaymentStatusId,
                               order.ShippingStatusId,
                               OrderCreatedOnUtc = order.CreatedOnUtc
                           };

                if (stage == HandoverStage.AwaitingDelivery)
                {
                    //paid, not delivered through the stock order screens, and not handed over by the desk yet
                    rows = rows.Where(row => row.PaymentStatusId == paidPaymentStatusId &&
                                             row.ShippingStatusId != deliveredShippingStatusId &&
                                             !shipmentItemTable.Any(item => item.OrderItemId == row.OrderItemId &&
                                                                            shipmentTable.Any(shipment => shipment.Id == item.ShipmentId &&
                                                                                                          shipment.DeliveryDateUtc != null)) &&
                                             !checkTable.Any(check => check.OrderItemId == row.OrderItemId &&
                                                                      check.CheckTypeId == deliveryTypeId &&
                                                                      check.StatusId == confirmedStatusId));
                    rows = rows.OrderBy(row => row.OrderCreatedOnUtc).ThenBy(row => row.OrderItemId);
                }
                else
                {
                    //rented, handed over (by the desk or the stock order screens), not back yet; soonest due first
                    rows = rows.Where(row => row.RentalStartDateUtc != null &&
                                             (row.ShippingStatusId == deliveredShippingStatusId ||
                                              shipmentItemTable.Any(item => item.OrderItemId == row.OrderItemId &&
                                                                            shipmentTable.Any(shipment => shipment.Id == item.ShipmentId &&
                                                                                                          shipment.DeliveryDateUtc != null)) ||
                                              checkTable.Any(check => check.OrderItemId == row.OrderItemId &&
                                                                      check.CheckTypeId == deliveryTypeId &&
                                                                      check.StatusId == confirmedStatusId)) &&
                                             !checkTable.Any(check => check.OrderItemId == row.OrderItemId &&
                                                                      check.CheckTypeId == returnTypeId &&
                                                                      check.StatusId == confirmedStatusId));
                    rows = rows.OrderBy(row => row.RentalEndDateUtc).ThenBy(row => row.OrderItemId);
                }

                //[Mahjar handover] what the desk searched for, applied in SQL so the paging stays honest
                if (keyword != null)
                {
                    var productIds = filter.KeywordProductIds.ToArray();
                    var customerIds = filter.KeywordCustomerIds.ToArray();
                    var contractRequestId = filter.KeywordContractRequestId;
                    var contractTable = _contractRequestRepository.Table;
                    rows = rows.Where(row => productIds.Contains(row.ProductId) ||
                                             customerIds.Contains(row.CustomerId) ||
                                             (row.CustomOrderNumber != null && row.CustomOrderNumber.Contains(keyword)) ||
                                             (contractRequestId > 0 && contractTable.Any(request => request.Id == contractRequestId &&
                                                                                                    request.OrderId == row.OrderId)));
                }

                if (filter.AssignedCustomerId != 0)
                {
                    var assignmentTable = _assignmentRepository.Table;
                    var assignedTo = filter.AssignedCustomerId;
                    rows = assignedTo == HandoverTaskFilter.UNASSIGNED
                        ? rows.Where(row => !assignmentTable.Any(assignment => assignment.OrderId == row.OrderId))
                        : rows.Where(row => assignmentTable.Any(assignment => assignment.OrderId == row.OrderId &&
                                                                              assignment.AssignedToCustomerId == assignedTo));
                }

                if (filter.StatusId != 0)
                {
                    var filteredTypeId = (int)type;
                    var statusId = filter.StatusId;
                    rows = statusId == HandoverTaskFilter.NOT_STARTED
                        ? rows.Where(row => !checkTable.Any(check => check.OrderItemId == row.OrderItemId &&
                                                                     check.CheckTypeId == filteredTypeId))
                        : rows.Where(row => checkTable.Any(check => check.OrderItemId == row.OrderItemId &&
                                                                    check.CheckTypeId == filteredTypeId &&
                                                                    check.StatusId == statusId));
                }

                if (filter.FromUtc.HasValue)
                {
                    var fromUtc = filter.FromUtc.Value;
                    rows = rows.Where(row => row.OrderCreatedOnUtc >= fromUtc);
                }

                if (filter.ToUtc.HasValue)
                {
                    var toUtc = filter.ToUtc.Value;
                    rows = rows.Where(row => row.OrderCreatedOnUtc < toUtc);
                }

                if (filter.OverdueOnly)
                {
                    var overdueBefore = DateTime.UtcNow.Date;
                    rows = rows.Where(row => row.RentalEndDateUtc != null && row.RentalEndDateUtc < overdueBefore);
                }

                var totalCount = await rows.CountAsync();
                var page = await rows.Skip(skip).Take(pageSize).ToListAsync();

                var itemIds = page.Select(row => row.OrderItemId).ToArray();
                var checks = itemIds.Any()
                    ? await _checkRepository.Table
                        .Where(check => check.CheckTypeId == (int)type && itemIds.Contains(check.OrderItemId))
                        .ToListAsync()
                    : new List<ConditionCheck>();
                var orderIds = page.Select(row => row.OrderId).Distinct().ToArray();
                var contracts = orderIds.Any()
                    ? await _contractRequestRepository.Table.Where(request => orderIds.Contains(request.OrderId)).ToListAsync()
                    : new List<ContractRequest>();

                //[Mahjar handover] who owns each order's handover, so the queue can show it and hide what is not theirs
                var assignees = await _assignmentService.GetAssignedCustomerIdsAsync(orderIds);

                var tasks = page.Select(row => new HandoverTask
                {
                    Stage = stage,
                    CheckType = type,
                    AssignedCustomerId = assignees.TryGetValue(row.OrderId, out var assignedTo) ? assignedTo : 0,
                    ProductId = row.ProductId,
                    ContractRequestId = contracts.Where(request => request.OrderId == row.OrderId)
                        .OrderByDescending(request => request.Id).FirstOrDefault()?.Id ?? 0,
                    OrderId = row.OrderId,
                    OrderNumber = row.CustomOrderNumber,
                    OrderItemId = row.OrderItemId,
                    Quantity = row.Quantity,
                    CustomerId = row.CustomerId,
                    RentalStartUtc = row.RentalStartDateUtc,
                    RentalEndUtc = row.RentalEndDateUtc,
                    DateUtc = row.OrderCreatedOnUtc,
                    Check = checks.Where(check => check.OrderItemId == row.OrderItemId)
                        .OrderByDescending(check => check.Id).FirstOrDefault()
                }).ToList();

                return new PagedList<HandoverTask>(tasks, pageIndex, pageSize, totalCount);
            }

            case HandoverStage.IntakeInProgress:
            case HandoverStage.Completed:
            {
                var checkQuery = stage == HandoverStage.IntakeInProgress
                    ? _checkRepository.Table
                        .Where(check => check.CheckTypeId == (int)ConditionCheckType.Intake && check.StatusId == (int)ConditionCheckStatus.Draft)
                        .OrderByDescending(check => check.UpdatedOnUtc)
                    : _checkRepository.Table
                        .Where(check => check.StatusId == (int)ConditionCheckStatus.Confirmed)
                        .OrderByDescending(check => check.ConfirmedOnUtc).ThenByDescending(check => check.Id);

                //[Mahjar handover] narrowed before paging, exactly like the queues above
                var filteredChecks = ApplyCheckFilter(checkQuery, filter, keyword);

                var totalCount = await filteredChecks.CountAsync();
                var page = await filteredChecks.Skip(skip).Take(pageSize).ToListAsync();

                var (orders, contracts) = await LoadReferencesAsync(
                    page.Select(check => check.OrderId), page.Select(check => check.ContractRequestId));
                //[Mahjar handover] who owns each order's handover, so the column says the truth in these queues too
                var assignees = await _assignmentService.GetAssignedCustomerIdsAsync(
                    page.Select(check => check.OrderId).Where(orderId => orderId > 0).Distinct().ToList());

                var tasks = page.Select(check =>
                {
                    orders.TryGetValue(check.OrderId, out var order);
                    contracts.TryGetValue(check.ContractRequestId, out var contract);
                    return new HandoverTask
                    {
                        Stage = stage,
                        CheckType = check.CheckType,
                        ProductId = check.ProductId,
                        ContractRequestId = check.ContractRequestId,
                        OrderId = check.OrderId,
                        OrderNumber = order?.CustomOrderNumber,
                        OrderItemId = check.OrderItemId,
                        CustomerId = order?.CustomerId ?? contract?.CustomerId ?? 0,
                        AssignedCustomerId = assignees.TryGetValue(check.OrderId, out var assignedTo) ? assignedTo : 0,
                        DateUtc = check.ConfirmedOnUtc ?? check.UpdatedOnUtc,
                        Check = check
                    };
                }).ToList();

                return new PagedList<HandoverTask>(tasks, pageIndex, pageSize, totalCount);
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(stage));
        }
    }

    /// <summary>The items of an order that the "awaiting delivery" queue lists (paid, active, not handed over yet).</summary>
    public virtual async Task<IList<OrderItem>> GetItemsAwaitingDeliveryAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        //the same rules as the AwaitingDelivery queue above
        if (order.Deleted || order.OrderStatus == OrderStatus.Cancelled ||
            order.PaymentStatus != PaymentStatus.Paid || order.ShippingStatus == ShippingStatus.Delivered)
            return new List<OrderItem>();

        var items = new List<OrderItem>();
        foreach (var orderItem in await _orderService.GetOrderItemsAsync(order.Id))
        {
            if (!await IsDeliveredAsync(orderItem, order))
                items.Add(orderItem);
        }

        return items;
    }

    /// <summary>
    /// [Mahjar handover] The rented items of an order that are out with the customer and not back yet — the same rules
    /// as the AwaitingReturn queue above. Used to tell an employee what a freshly assigned order actually leaves them.
    /// </summary>
    public virtual async Task<IList<OrderItem>> GetItemsAwaitingReturnAsync(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Deleted || order.OrderStatus == OrderStatus.Cancelled)
            return new List<OrderItem>();

        var items = new List<OrderItem>();
        foreach (var orderItem in await _orderService.GetOrderItemsAsync(order.Id))
        {
            //only a rental comes back, and only once it is actually out with the customer
            if (!orderItem.RentalStartDateUtc.HasValue || !await IsDeliveredAsync(orderItem, order))
                continue;

            var alreadyBack = await _checkRepository.Table
                .Where(check => check.OrderItemId == orderItem.Id &&
                                check.CheckTypeId == (int)ConditionCheckType.Return &&
                                check.StatusId == (int)ConditionCheckStatus.Confirmed)
                .CountAsync() > 0;
            if (!alreadyBack)
                items.Add(orderItem);
        }

        return items;
    }

    #endregion

    #endregion
}
