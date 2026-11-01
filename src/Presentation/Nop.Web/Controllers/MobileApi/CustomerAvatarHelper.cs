// [Mahjar app-batch3] customer profile photos, shared by the app API and the website's My account → Avatar page
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Media;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Mahjar.HomeSlides;
using Nop.Services.Media;
using SkiaSharp;
using ILogger = Nop.Services.Logging.ILogger;

namespace Nop.Web.Controllers;

/// <summary>
/// Customer profile photos ("avatars"), shared by the app API (api/customer/avatar) and the website's My account →
/// Avatar page. An upload is judged by its BYTES (a real JPEG, PNG or WebP; GIF too on the website), never by its name
/// or declared type alone. It is turned upright by its EXIF orientation, cropped to a square (centred; a little above
/// centre for a portrait, where the face usually is), scaled down to at most 640 px (never up) and re-encoded as a JPEG
/// at the store's image quality, which also drops the camera metadata (GPS position included). Each upload is stored as
/// a NEW picture under the stock AvatarPictureId attribute, so its url is new too and no browser or app keeps showing
/// the old face from its cache. The previous picture is deleted only when it was the customer's OWN avatar: once the
/// attribute has moved on, nothing may still use it (another customer's avatar, a product, a logo, a slide; a customer
/// import can point an avatar at any picture id). [Mahjar app-batch3]
/// </summary>
public static class CustomerAvatarHelper
{
    #region Constants

    /// <summary>The largest photo file accepted (the app itself sends a ~1024 px JPEG of a few hundred KB).</summary>
    public const int MaxUploadBytes = 5 * 1024 * 1024;

    /// <summary>The request cap of the upload endpoint: the file plus the multipart framing.</summary>
    public const int MaxRequestBytes = MaxUploadBytes + 64 * 1024;

    /// <summary>The size of the url the app gets (CustomerProfileDto.AvatarUrl): sharp in a phone avatar at 3x.</summary>
    public const int AppAvatarSize = 320;

    /// <summary>The side of the stored square, the source of every thumb.</summary>
    private const int STORED_SIZE = 640;

    /// <summary>Decompression-bomb guard: a larger source is never decoded.</summary>
    private const long MAX_SOURCE_PIXELS = 50_000_000;

    /// <summary>
    /// Decompression-bomb guard for the pixels actually decoded (~64 MB of RGBA): a JPEG / WebP decodes scaled down, but
    /// a PNG / GIF decodes at full size, and a flat 7000 px PNG is only a few KB on the wire.
    /// </summary>
    private const long MAX_DECODE_PIXELS = 4096L * 4096;

    /// <summary>A smaller source is not a usable photo.</summary>
    private const int MIN_SOURCE_SIDE = 32;

    /// <summary>Where a portrait's square starts, as a share of the height it loses (0.5 = centred).</summary>
    private const float PORTRAIT_TOP_SHARE = 0.4f;

    #endregion

    #region Fields

    /// <summary>
    /// One avatar change at a time per customer (striped by customer id): two overlapping changes (two devices, or the
    /// app and the website) would otherwise both read the same previous picture, and the new picture of the one whose
    /// save lost would be left behind, referenced by nothing. In-process, which is what this single-server store runs.
    /// </summary>
    private static readonly SemaphoreSlim[] _changeGates = Enumerable.Range(0, 32).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    #endregion

    /// <summary>Why an upload cannot be used.</summary>
    public enum AvatarImageError
    {
        None,
        NotAnImage,
        TooLarge
    }

    #region Utilities

    /// <summary>The image format the file's first bytes declare (magic numbers), or null for anything else.</summary>
    private static SKEncodedImageFormat? Sniff(byte[] data)
    {
        if (data == null || data.Length < 12)
            return null;

        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return SKEncodedImageFormat.Jpeg;

        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
            && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return SKEncodedImageFormat.Png;

        if (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F'
            && data[8] == 'W' && data[9] == 'E' && data[10] == 'B' && data[11] == 'P')
            return SKEncodedImageFormat.Webp;

        if (data[0] == 'G' && data[1] == 'I' && data[2] == 'F' && data[3] == '8'
            && (data[4] == '7' || data[4] == '9') && data[5] == 'a')
            return SKEncodedImageFormat.Gif;

        return null;
    }

    /// <summary>A copy of the bitmap turned upright for its EXIF orientation (0th row/column as the camera wrote them).</summary>
    private static SKBitmap Orient(SKBitmap source, SKEncodedOrigin origin)
    {
        var w = source.Width;
        var h = source.Height;
        var swapAxes = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var result = new SKBitmap(new SKImageInfo(swapAxes ? h : w, swapAxes ? w : h, source.ColorType, source.AlphaType));
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.Transparent);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight:      //mirrored: (w - x, y)
                canvas.Translate(w, 0);
                canvas.Scale(-1, 1);
                break;
            case SKEncodedOrigin.BottomRight:   //upside down: (w - x, h - y)
                canvas.Translate(w, h);
                canvas.RotateDegrees(180);
                break;
            case SKEncodedOrigin.BottomLeft:    //mirrored upside down: (x, h - y)
                canvas.Translate(0, h);
                canvas.Scale(1, -1);
                break;
            case SKEncodedOrigin.LeftTop:       //transposed: (y, x)
                canvas.Scale(-1, 1);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightTop:      //a quarter turn clockwise: (h - y, x) — the usual portrait phone photo
                canvas.Translate(h, 0);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.RightBottom:   //transversed: (h - y, w - x)
                canvas.Translate(h, w);
                canvas.Scale(1, -1);
                canvas.RotateDegrees(90);
                break;
            case SKEncodedOrigin.LeftBottom:    //a quarter turn anticlockwise: (y, w - x)
                canvas.Translate(0, w);
                canvas.RotateDegrees(-90);
                break;
        }
        canvas.DrawBitmap(source, 0, 0);
        return result;
    }

    /// <summary>The customer's gate in <see cref="_changeGates"/>.</summary>
    private static SemaphoreSlim GateOf(Customer customer)
    {
        return _changeGates[(customer.Id & int.MaxValue) % _changeGates.Length];
    }

    /// <summary>
    /// The avatar picture id as it is stored NOW. The request's own copy of the customer's attributes (the per-request
    /// cache) was read when its customer / token was checked, before it waited at the gate, so another request may have
    /// changed the avatar since: that copy is dropped first, so this read and the save after it see the stored rows.
    /// </summary>
    private static async Task<int> GetStoredAvatarIdAsync(Customer customer, IGenericAttributeService genericAttributeService)
    {
        try
        {
            EngineContext.Current.Resolve<IShortTermCacheManager>()
                .Remove(NopCommonDefaults.GenericAttributeCacheKey.Key, customer.Id, nameof(Customer));
        }
        catch
        {
            //no request scope to resolve from (never so for the API and the website): the copy then stands
        }

        return await genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Whether a declared (multipart part / browser) content type is one of the accepted photo types. The bytes are
    /// checked separately by <see cref="Normalize"/>: BOTH must pass.
    /// </summary>
    public static bool IsAllowedContentType(string contentType, bool allowGif)
    {
        var type = (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();

        return type is "image/jpeg" or "image/jpg" or "image/pjpeg" or "image/png" or "image/webp"
            || (allowGif && type == "image/gif");
    }

    /// <summary>
    /// Turn an uploaded photo into the stored avatar: a square JPEG, upright, at most 640 px (and never more than
    /// MediaSettings.MaximumImageSize), encoded at MediaSettings.DefaultImageQuality. Returns why it cannot be used, or
    /// <see cref="AvatarImageError.None"/> with the JPEG.
    /// </summary>
    public static AvatarImageError Normalize(byte[] data, bool allowGif, MediaSettings mediaSettings, out byte[] jpeg)
    {
        jpeg = null;

        var sniffed = Sniff(data);
        if (sniffed == null || (sniffed == SKEncodedImageFormat.Gif && !allowGif))
            return AvatarImageError.NotAnImage;

        try
        {
            using var encodedData = SKData.CreateCopy(data);
            using var codec = SKCodec.Create(encodedData);

            //the decoder must agree with the magic number (no polyglot files)
            if (codec == null || codec.EncodedFormat != sniffed.Value)
                return AvatarImageError.NotAnImage;

            var info = codec.Info;
            if (info.Width < MIN_SOURCE_SIDE || info.Height < MIN_SOURCE_SIDE)
                return AvatarImageError.NotAnImage;
            if ((long)info.Width * info.Height > MAX_SOURCE_PIXELS)
                return AvatarImageError.TooLarge;

            var maxSide = mediaSettings?.MaximumImageSize > 0 ? Math.Min(STORED_SIZE, mediaSettings.MaximumImageSize) : STORED_SIZE;
            var shortSide = Math.Min(info.Width, info.Height);
            var target = Math.Min(maxSide, shortSide);

            //decode at the smallest size the codec offers natively that still covers the square (JPEG in 1/8 steps,
            //WebP at any size; PNG/GIF at full size), so a 12 MP phone photo is never fully decoded
            var decodeSize = info.Size;
            var scaled = codec.GetScaledDimensions((float)target / shortSide);
            if (scaled.Width > 0 && scaled.Height > 0 && Math.Min(scaled.Width, scaled.Height) >= target)
                decodeSize = scaled;
            if ((long)decodeSize.Width * decodeSize.Height > MAX_DECODE_PIXELS)
                return AvatarImageError.TooLarge;

            var decodeInfo = new SKImageInfo(decodeSize.Width, decodeSize.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);
            using var decoded = new SKBitmap(decodeInfo);
            if (codec.GetPixels(decodeInfo, decoded.GetPixels()) != SKCodecResult.Success)
                return AvatarImageError.NotAnImage;

            var origin = codec.EncodedOrigin;
            using var oriented = origin is SKEncodedOrigin.TopLeft or SKEncodedOrigin.Default ? null : Orient(decoded, origin);
            var upright = oriented ?? decoded;

            //the square: centred, or a little above centre for a portrait
            var side = Math.Min(upright.Width, upright.Height);
            var left = (upright.Width - side) / 2f;
            var top = upright.Height > upright.Width
                ? (upright.Height - side) * PORTRAIT_TOP_SHARE
                : (upright.Height - side) / 2f;
            var size = Math.Min(target, side);

            using var output = new SKBitmap(new SKImageInfo(size, size, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
            using (var canvas = new SKCanvas(output))
            using (var paint = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true })
            {
                //a transparent PNG / WebP / GIF lands on white (a JPEG has no alpha)
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(upright, SKRect.Create(left, top, side, side), SKRect.Create(0, 0, size, size), paint);
            }

            var quality = mediaSettings?.DefaultImageQuality > 0 ? Math.Clamp(mediaSettings.DefaultImageQuality, 1, 100) : 85;
            using var image = SKImage.FromBitmap(output);
            using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            jpeg = encoded?.ToArray();

            return jpeg is { Length: > 0 } ? AvatarImageError.None : AvatarImageError.NotAnImage;
        }
        catch
        {
            jpeg = null;
            return AvatarImageError.NotAnImage;
        }
    }

    /// <summary>
    /// The customer's profile photo url at <paramref name="targetSize"/> px: ABSOLUTE, off the request host (the store's
    /// MediaSettings.UseAbsoluteImagePath); null when there is none.
    /// </summary>
    public static async Task<string> GetUrlAsync(Customer customer, int targetSize,
        IGenericAttributeService genericAttributeService, IPictureService pictureService)
    {
        var pictureId = await genericAttributeService.GetAttributeAsync<int>(customer, NopCustomerDefaults.AvatarPictureIdAttribute);
        if (pictureId <= 0)
            return null;

        var url = await pictureService.GetPictureUrlAsync(pictureId, targetSize, showDefaultPicture: false);

        return string.IsNullOrEmpty(url) ? null : url;
    }

    /// <summary>
    /// Store a normalised JPEG (see <see cref="Normalize"/>) as the customer's avatar: a NEW picture, then the previous
    /// one is deleted when it was the customer's own (a failure there is only logged: the new photo is already in place).
    /// </summary>
    public static async Task<Picture> ReplaceAsync(Customer customer, byte[] jpeg, IGenericAttributeService genericAttributeService,
        IPictureService pictureService, IHomeSlideService homeSlideService, ILogger logger)
    {
        var gate = GateOf(customer);
        await gate.WaitAsync();
        try
        {
            var previousId = await GetStoredAvatarIdAsync(customer, genericAttributeService);

            var picture = await pictureService.InsertPictureAsync(jpeg, MimeTypes.ImageJpeg, null);
            await genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AvatarPictureIdAttribute, picture.Id);

            if (previousId > 0 && previousId != picture.Id)
                await DeleteOwnPictureAsync(previousId, homeSlideService, logger);

            return picture;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>Remove the customer's avatar: the attribute is cleared and the picture deleted when it was their own.</summary>
    public static async Task RemoveAsync(Customer customer,
        IGenericAttributeService genericAttributeService, IHomeSlideService homeSlideService, ILogger logger)
    {
        var gate = GateOf(customer);
        await gate.WaitAsync();
        try
        {
            var previousId = await GetStoredAvatarIdAsync(customer, genericAttributeService);
            await genericAttributeService.SaveAttributeAsync(customer, NopCustomerDefaults.AvatarPictureIdAttribute, 0);

            if (previousId > 0)
                await DeleteOwnPictureAsync(previousId, homeSlideService, logger);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Delete a picture the customer's avatar attribute no longer points to, but only when nothing else references it
    /// either, by the store-wide "picture in use" check (IHomeSlideService.IsPictureInUseAsync: product galleries,
    /// attribute and combination images, category / manufacturer / vendor images, condition photos, every customer's
    /// avatar, the store and PDF logos, Swiper and home slides): a picture shared with anything is never taken from it.
    /// </summary>
    private static async Task DeleteOwnPictureAsync(int pictureId, IHomeSlideService homeSlideService, ILogger logger)
    {
        try
        {
            await homeSlideService.DeletePictureIfUnusedAsync(pictureId);
        }
        catch (Exception exc)
        {
            await logger.WarningAsync($"Mahjar: the previous avatar picture #{pictureId} could not be deleted", exc);
        }
    }

    #endregion
}
