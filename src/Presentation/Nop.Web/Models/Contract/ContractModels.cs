// [Mahjar contracts]
using System;
using System.Collections.Generic;
using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Contract;

/// <summary>The "My contracts" account page: all contract requests of the current customer.</summary>
public partial record ContractListModel : BaseNopModel
{
    public ContractListModel()
    {
        Items = new List<ContractItemModel>();
    }

    public IList<ContractItemModel> Items { get; set; }
}

/// <summary>A single contract request card on the "My contracts" page.</summary>
public partial record ContractItemModel : BaseNopModel
{
    public ContractItemModel()
    {
        Lines = new List<ContractItemLineModel>();
    }

    public int Id { get; set; }
    public string Number { get; set; }
    public string CreatedOn { get; set; }
    public string Status { get; set; }
    public string StatusClass { get; set; }
    public bool CanSign { get; set; }
    public Guid Token { get; set; }
    public bool CanCancel { get; set; }
    public bool CanCheckout { get; set; }

    /// <summary>[Mahjar handover] whether the read-only contract page (document + condition checks) is available.</summary>
    public bool CanView { get; set; }

    public string RejectionReason { get; set; }
    public string GrandTotal { get; set; }
    public string SignedOn { get; set; }
    public IList<ContractItemLineModel> Lines { get; set; }
}

/// <summary>One snapshot line summarized on a contract request card.</summary>
public partial record ContractItemLineModel : BaseNopModel
{
    public string Name { get; set; }
    public bool IsRental { get; set; }
    public int Quantity { get; set; }
    public string Period { get; set; }
}

/// <summary>The contract signing page (frozen contract body + signature pad).</summary>
public partial record ContractSignModel : BaseNopModel
{
    public ContractSignModel()
    {
        ConditionChecks = new List<ContractConditionCheckModel>();
    }

    public int Id { get; set; }
    public string Number { get; set; }
    public Guid Token { get; set; }
    public string BodyHtml { get; set; }
    public string SignedName { get; set; }

    /// <summary>[Mahjar handover] the before-signing condition photos/video of each item — part of what is signed.</summary>
    public IList<ContractConditionCheckModel> ConditionChecks { get; set; }
}

/// <summary>
/// [Mahjar handover] The read-only contract page: the frozen document, the signature and the documented condition
/// of each item before signing, at delivery and at a rental return.
/// </summary>
public partial record ContractViewModel : BaseNopModel
{
    public ContractViewModel()
    {
        ConditionChecks = new List<ContractConditionCheckModel>();
    }

    public int Id { get; set; }
    public string Number { get; set; }
    public string Status { get; set; }
    public string BodyHtml { get; set; }
    public string SignedName { get; set; }
    public string SignedOn { get; set; }
    public string SignatureDataUrl { get; set; }
    public IList<ContractConditionCheckModel> ConditionChecks { get; set; }
}

/// <summary>[Mahjar handover] One documented condition check of an item, as the customer sees it.</summary>
public partial record ContractConditionCheckModel : BaseNopModel
{
    public ContractConditionCheckModel()
    {
        Photos = new List<ContractConditionPhotoModel>();
    }

    public int Id { get; set; }

    /// <summary>precontract / delivery / return — groups the checks by checkpoint.</summary>
    public string CheckTypeKey { get; set; }

    /// <summary>"Before signing" / "At delivery" / "At return".</summary>
    public string CheckType { get; set; }

    public string ProductName { get; set; }
    public string RecordedOn { get; set; }
    public string GradeText { get; set; }
    public string Notes { get; set; }
    public bool HasDamage { get; set; }
    public string DamageSeverityText { get; set; }
    public string DamageDescription { get; set; }
    public string DeductionText { get; set; }
    public string RefundText { get; set; }

    /// <summary>An uploaded file (site-relative path) or a validated external URL; null when there is no video.</summary>
    public string VideoUrl { get; set; }

    public bool VideoIsFile { get; set; }
    public IList<ContractConditionPhotoModel> Photos { get; set; }
}

/// <summary>[Mahjar handover] One condition photo.</summary>
public partial record ContractConditionPhotoModel : BaseNopModel
{
    public string ImageUrl { get; set; }
    public string FullSizeImageUrl { get; set; }
    public string Caption { get; set; }
}
