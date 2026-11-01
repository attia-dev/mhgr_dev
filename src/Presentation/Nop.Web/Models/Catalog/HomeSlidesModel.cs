// [Mahjar home-motion]
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Catalog;

/// <summary>
/// The storefront home "Living Home" slides of one placement: the Hero carousel under the menu bar, or the
/// full-bleed parallax Feature bands between the categories and the featured products. Everything the view
/// needs is resolved server-side (localized texts, picture urls per breakpoint, a safe target url). [Mahjar home-motion]
/// </summary>
public partial record HomeSlidesModel : BaseNopModel
{
    /// <summary>"hero" or "feature" (the view name follows it)</summary>
    public string Placement { get; set; }

    public IList<HomeSlideItemModel> Slides { get; set; } = new List<HomeSlideItemModel>();

    /// <summary>One slide / band. [Mahjar home-motion]</summary>
    public partial record HomeSlideItemModel : BaseNopEntityModel
    {
        /// <summary>Main (wide) picture url — about 1920px for FullBleed, about 1000px for a Spotlight render</summary>
        public string ImageUrl { get; set; }

        /// <summary>The ≤640px picture url (the slide's mobile picture when it has one, else a lighter copy of the main one)</summary>
        public string MobileImageUrl { get; set; }

        /// <summary>Optional MP4 (FullBleed hero only; http(s) or site-relative, else null)</summary>
        public string VideoUrl { get; set; }

        public string Kicker { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        public string ButtonText { get; set; }

        /// <summary>Resolved link (site-relative or absolute http(s)); null = the slide links nowhere</summary>
        public string Url { get; set; }

        /// <summary>True when <see cref="Url"/> is an absolute url on another host (opened with rel=noopener)</summary>
        public bool IsExternal { get; set; }

        /// <summary>"fullbleed" | "spotlight"</summary>
        public string Layout { get; set; }

        /// <summary>"start" | "center" | "end" (logical — start = right in RTL)</summary>
        public string ContentPosition { get; set; }

        /// <summary>0..90 — the legibility gradient darkness</summary>
        public int OverlayOpacity { get; set; }
    }
}
