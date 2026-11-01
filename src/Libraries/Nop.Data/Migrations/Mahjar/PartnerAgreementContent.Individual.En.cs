// [Mahjar partner-agreement]
namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// The English body of the MHGR individual partner master agreement ("اتفاقية الشريك الفرد الرئيسية مع منصة محجر",
/// version 1.0) — a courtesy translation of the Arabic original, which remains the legally prevailing text. Every
/// "[●]" blank and every checkbox field of the original is a %Partner.*% / %Store.*% / %Agreement.*% token resolved
/// at render time from PartnerAgreementSettings (and the accepting vendor's own record). Kept in its own file
/// because it is content, not logic. [Mahjar partner-agreement]
/// </summary>
public static partial class PartnerAgreementContent
{
    /// <summary>The English (courtesy) body of the individual partner master agreement.</summary>
    public const string IndividualEnglish = """
<div dir="ltr" style="font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:860px;margin:0 auto;color:#222;line-height:1.7;text-align:left">
<h1 style="text-align:center;margin-bottom:0">Master Individual Partner Agreement with the MHGR Platform</h1>
<h2 style="text-align:center;margin-top:4px;font-weight:normal">MHGR — final operational version</h2>
<p style="text-align:center;color:#8C3B3B">English courtesy translation. The Arabic text is the legally prevailing version.</p>
<p style="text-align:center;color:#666">Version %Agreement.Version% | Effective date: %Agreement.EffectiveDate%</p>

<h3>Agreement details</h3>
<table style="width:100%;border-collapse:collapse;font-size:14px">
<thead><tr>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold;width:38%">Item</th>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold">Value</th>
</tr></thead>
<tbody>
<tr><td style="border:1px solid #d9d2c5;padding:8px">MHGR legal name</td><td style="border:1px solid #d9d2c5;padding:8px">%Store.LegalName%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">MHGR commercial registration</td><td style="border:1px solid #d9d2c5;padding:8px">%Store.CommercialRegistration%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">National address and contact details</td><td style="border:1px solid #d9d2c5;padding:8px">%Store.NationalAddress%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Name of the Individual Partner</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.Name%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">User identifier</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.UserId%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Identity verification method</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.VerificationMethod%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Approved contact details</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.Email%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Approved settlement account</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.SettlementAccount%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Activated services</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.ActivatedServices%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Effective date</td><td style="border:1px solid #d9d2c5;padding:8px">%Agreement.EffectiveDate%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Version number</td><td style="border:1px solid #d9d2c5;padding:8px">%Agreement.Version%</td></tr>
</tbody>
</table>

<h3>Preamble</h3>
<p>Whereas MHGR is a Saudi electronic platform for luxury products that enables individuals to list the products they own for sale and/or for rental in accordance with the platform's controls; and whereas the Individual Partner wishes to make use of the platform to dispose of products that he lawfully owns; the two parties have therefore agreed that this Agreement shall be the master agreement governing the relationship, and that the provisions of each service shall apply only upon the activation of its own annex.</p>

<h3>Article 1: Definitions</h3>
<p>For the purposes of this Agreement: "MHGR" means the entity operating the platform; the "Individual Partner" means the natural person who owns the account and the product; the "Product" means the listed item; the "Customer" means the buyer or the renter, according to the transaction; the "Service Annex" means the sale annex or the rental annex; the "Product File" means the digital record of the item; the "Condition Report" means the documentation of the product's condition for rental purposes; the "Settlement" means the transfer of the net amounts due to the Partner; the "Transaction Documents" means all of the records, approvals and reports relating to the order.</p>

<h3>Article 2: Subject matter and scope of the Agreement</h3>
<p>This Agreement governs the general relationship between MHGR and the Individual Partner with respect to the account, verification, the ownership and authenticity of products, their listing, fees and settlements, privacy, liability, and electronic records. The sale annex or the rental annex applies only upon the activation of the relevant service.</p>

<h3>Article 3: Activated services</h3>
<p>The service is specified in the activation record: %Partner.ActivatedServices%. Either of the two services may be activated or suspended without terminating the master agreement, unless the circumstances require otherwise.</p>

<h3>Article 4: The nature of MHGR's role</h3>
<p>MHGR is a platform and a technical and operational intermediary, and does not become the owner of the product merely by reason of the listing or of temporary possession for the purposes of inspection or delivery. It may undertake, itself or through service providers, verification, payment, inspection, delivery, collection, documentation, disputes and settlements.</p>
<p>MHGR does not guarantee the successful sale or rental of the product, nor does it guarantee the customer's solvency or the collection of the full value of a lost or unreturned product, except under a separate and express guarantee.</p>

<h3>Article 5: Capacity and verification</h3>
<p>The Individual Partner acknowledges that he has legal capacity and that his data is correct. MHGR may verify his identity through the approved means. Passing verification does not mean the automatic acceptance of every product or of every transaction.</p>

<h3>Article 6: The nature of the individual's activity and regulatory obligations</h3>
<p>The Individual Partner uses the platform to sell or rent products that he owns or that he has the right to dispose of. If his activity, by reason of its continuity, volume or organisation, becomes an activity that requires registration, licensing or an additional obligation, he undertakes to satisfy that requirement and to notify MHGR where needed.</p>
<p>Acceptance of an individual's account on the platform does not constitute a determination by MHGR that every future form of his activity is exempt from any regulatory requirement.</p>

<h3>Article 7: Ownership of the product</h3>
<p>The Individual Partner acknowledges that he is the lawful owner of the product or is authorised to dispose of it, and that he is not aware of any pledge, attachment, report of loss or theft, or third-party right that prevents the sale or the rental.</p>
<p>MHGR may request an invoice, a certificate, images, serial-number data or any suitable evidence of ownership or of provenance.</p>

<h3>Article 8: Authenticity</h3>
<p>The Partner undertakes not to present a counterfeit product as genuine, and undertakes that the information he provides about authenticity is correct. MHGR may request an inspection or documents, and the acceptance of a listing does not represent an absolute guarantee of authenticity unless an independent and express verification service is provided.</p>

<h3>Article 9: The product file and the serial number</h3>
<p>A file is created for the product which may include the brand, the model, the condition, the value, images, video, accessories, the reference or serial number, and ownership documents. The full serial number need not be displayed to the public.</p>

<h3>Article 10: Accuracy of description and disclosure</h3>
<p>The Partner undertakes to describe the condition of the product truthfully and to disclose known defects, repairs or material deficiencies that may affect the customer's decision. MHGR may suspend the product where there is a material discrepancy or misleading information.</p>

<h3>Article 11: Prices and availability</h3>
<p>The Partner sets the price or adopts the proposed price according to the service. He undertakes to keep the availability of the product up to date and not to accept an order if it has become unavailable. Prices and the basic fees appear in the transaction summary.</p>

<h3>Article 12: Commissions and fees</h3>
<p>MHGR is entitled to the commission and the fees set out in the commercial schedule in force at the time of the transaction. No fee that has not been agreed or that has not been disclosed within the relationship shall be applied.</p>

<h3>Article 13: Payment and settlements</h3>
<p>Payments are made through the approved means, and the Partner's dues are settled to a financial account documented in his name or approved in accordance with MHGR's procedures within %Partner.SettlementCycle% or in accordance with the commercial schedule, after the conditions of entitlement are met.</p>
<p>The portion that is the subject of a dispute, a refund or a payment objection may be withheld until its effect is determined, without withholding unrelated amounts without justification.</p>

<h3>Article 14: Taxes and financial obligations</h3>
<p>The Individual Partner bears the tax or financial obligations that apply to him by law according to his status and his activity, and MHGR does not determine his individual tax position except within the limits that the law imposes on the platform or under a separate service.</p>

<h3>Article 15: Delivery and collection through MHGR</h3>
<p>In transactions that require an organised handover, delivery or collection takes place through a location, a partner or a mechanism approved by MHGR. Direct delivery between the Partner and the customer is not the default route for transactions that MHGR requires to be documented through an approved point.</p>

<h3>Article 16: Privacy and minimising data sharing</h3>
<p>MHGR works to reduce the direct sharing of data between the Partner and the customer to what is necessary to carry out the transaction. There is no requirement to show the residential address, the full identity data, or contact means that are not necessary, to the other party.</p>
<p>Each party processes the data it receives only for the legitimate purpose, and MHGR's processing is subject to the privacy policy and to the Personal Data Protection Law.</p>

<h3>Article 17: Protection of customer data</h3>
<p>The Partner may not use customer data obtained by reason of MHGR for marketing or for communication unrelated to the transaction without an appropriate justification, nor share it with anyone other than those who are necessary to carry out the transaction.</p>

<h3>Article 18: Content and intellectual property rights</h3>
<p>The Partner grants MHGR a limited, non-exclusive licence to use the images and the description of the product to the extent necessary for display, marketing and the operation of the service. He acknowledges that the materials he provides do not infringe the rights of third parties.</p>

<h3>Article 19: Prohibition of circumvention</h3>
<p>The Partner may not use data or a communication that arose by reason of MHGR in order to divert the same transaction outside the platform with the intention of avoiding the commission or the protection procedures. This does not preclude an independent relationship that is not an extension of the circumvention of an MHGR transaction.</p>

<h3>Article 20: Electronic records</h3>
<p>MHGR may keep an electronic record of the acceptance of the Agreement, the activation of services, orders, payment, delivery, collection, reports and correspondence. These records are used in accordance with the relevant laws, without being regarded as conclusive evidence that is not open to discussion.</p>

<h3>Article 21: Complaints and disputes</h3>
<p>The Partner shall cooperate in handling customer complaints and in providing the necessary information. MHGR may take a temporary operational decision to protect the transaction or the funds in dispute, and this does not prevent the parties from resorting to the competent authorities.</p>

<h3>Article 22: Fraud and unlawful products</h3>
<p>MHGR may suspend an account or a product where there are serious indications of counterfeiting, fraud, forgery, a stolen product, disputed ownership or misuse, and to the extent proportionate to the risk and to the verification required.</p>

<h3>Article 23: Liability</h3>
<p>Each party bears liability for its own breach within the scope of its control in accordance with the law. The Agreement does not contain any exemption from a liability from which exemption is not legally permitted.</p>
<p>MHGR does not guarantee the value of the product or its collection in the event of loss or of non-return except under a separate and express guarantee, and identity verification alone does not constitute a guarantee against risk.</p>

<h3>Article 24: Claims arising from incorrect information or ownership</h3>
<p>The Partner bears such direct financial effect as is legally established to arise from presenting a product that he does not own, from a materially incorrect statement, or from a misleading claim of authenticity, having regard to proof of the damage and of causation and to the non-imposition of unjustified arbitrary amounts.</p>

<h3>Article 25: Force majeure</h3>
<p>Inability to perform because of a circumstance beyond control is dealt with in accordance with the law, and the affected party undertakes to give notice and to take what is reasonably possible to mitigate the damage.</p>

<h3>Article 26: Term of the Agreement and termination</h3>
<p>The Agreement begins on the date of acceptance and continues until it is terminated. Either party may terminate it upon %Partner.TerminationNoticeDays% days' notice, while ongoing transactions and prior rights and claims remain in force until they are completed.</p>

<h3>Article 27: Suspension</h3>
<p>MHGR may suspend the account or a specific service in the event of a material breach, a pending high-risk claim, a suspicion of fraud, a non-return, or insufficient ownership documents, with a review of the reason and an opportunity to remedy it where that is appropriate.</p>

<h3>Article 28: Effect of termination</h3>
<p>Termination does not extinguish dues, liabilities or transactions that began before it, and the provisions on data, records, claims and confidentiality continue to apply to the extent necessary.</p>

<h3>Article 29: Amendment and versions</h3>
<p>The Agreement or the annexes may be updated, with the version and the effective date shown, and a material amendment is not applied retroactively to an ongoing transaction without a legal basis or an appropriate acceptance.</p>

<h3>Article 30: Order of precedence of documents</h3>
<p>Precedence is given to mandatory laws, then to the terms specific to the transaction, then to the service annex, then to the master agreement, then to the commercial schedule, then to the operational procedures.</p>

<h3>Article 31: Notices</h3>
<p>Notices are sent through the platform, the mobile number, the registered email or any approved channel. The Partner undertakes to keep his contact details up to date.</p>

<h3>Article 32: Governing law and dispute resolution</h3>
<p>This Agreement is subject to the laws of the Kingdom of Saudi Arabia. The two parties shall attempt an amicable resolution within %Partner.AmicableSettlementDays% working days from the completion of the dispute notice, after which the dispute falls within the jurisdiction of the competent judicial authority in the Kingdom, unless there is a valid lawful agreement to the contrary.</p>

<h3>Article 33: Electronic acceptance</h3>
<p>The Agreement and the annexes may be accepted electronically, and the system retains the user identifier, the version number, the date and time of acceptance, the activated services and the technical reference.</p>

<h2>Annex A: Service activation and the commercial schedule</h2>
<p>The following schedule is a variable part of the Agreement and is completed upon activation. No unapproved figure is to be assumed.</p>
<table style="width:100%;border-collapse:collapse;font-size:14px">
<thead><tr>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold;width:38%">Item</th>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold">Approved value</th>
</tr></thead>
<tbody>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Activated services</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.ActivatedServices%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Rental commission</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.RentalCommission%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Sale commission</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.SaleCommission%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Inspection fee, if any</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.InspectionFee%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Delivery / collection fee, if any</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.DeliveryFee%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Time to prepare / hand over the product</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.PrepTime% (hours / working days)</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Settlement period / cycle</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.SettlementCycle%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Approved settlement account</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.SettlementAccount%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Time allowed to respond to a claim</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.ClaimResponseDays% working days</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Termination notice period</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.TerminationNoticeDays% days</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Other notes</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.CommercialNotes%</td></tr>
</tbody>
</table>

<h2>Annex B: Special terms for renting out the Individual Partner's products</h2>
<p>This annex applies upon the activation of the rental service for the Individual Partner.</p>

<h3>Article 1: Eligible products and the value cap</h3>
<p>The item is subject to a review of its category, condition, ownership and risk. The current pilot cap for the value of an item is SAR %Partner.RentalItemValueCap%, unless MHGR approves an exception.</p>

<h3>Article 2: The order and the Partner's approval</h3>
<p>When an order is received, the Partner may accept or reject it according to availability before the transaction is confirmed. After confirmation he undertakes to prepare the item and not to deal outside the platform in respect of the same order.</p>

<h3>Article 3: Renter eligibility</h3>
<p>MHGR manages renter eligibility in accordance with the procedures available to it. Acceptance of the renter does not constitute a guarantee that no delay, damage, loss or non-return will occur.</p>

<h3>Article 4: Guarantees</h3>
<p>The transaction may require a financial guarantee, a promissory note or another protection means, which appears in the terms of the transaction. The existence of a single means is not an absolute guarantee of collection or of the recovery of the product.</p>

<h3>Article 5: Handing the item over to an MHGR point</h3>
<p>The Partner undertakes to hand over the same registered item, and in individual transactions it is preferable that the handover take place through an approved point or entity rather than by undocumented direct delivery between the parties.</p>

<h3>Article 6: The item condition report</h3>
<p>The condition is documented before possession passes to the renter, by means of images, video, the serial number, the accessories, pre-existing scratches and other matters according to the type of product. The report forms part of the transaction.</p>

<h3>Article 7: Chain of possession</h3>
<p>The system documents the passing of possession between the Partner, the handover point and the renter, and upon return, according to the operational model. Neither the Partner nor the renter automatically bears a period of possession that he did not actually hold according to the record and the evidence.</p>

<h3>Article 8: Return</h3>
<p>The return takes place by the approved method, with re-inspection and comparison before the transaction is closed.</p>

<h3>Article 9: Wear and damage</h3>
<p>Ordinary use proportionate to the nature of the product and to the rental period is not automatically treated as compensable damage. Where a new discrepancy appears, a damage review is opened, and the assessment is based on the evidence and not on the Partner's estimate alone.</p>

<h3>Article 10: Assessment of the claim</h3>
<p>The claim may be calculated on the basis of a reasonable repair, of the damaged part, of the established diminution in value, or of replacement where repair is not possible, with the assistance of a technical body where needed.</p>

<h3>Article 11: Loss, theft and non-return</h3>
<p>These cases are managed through an evidence file and escalation, and MHGR does not guarantee the Partner the full value of the product unless a separate guarantee exists. The Partner undertakes to provide proof of ownership and of value and the information necessary for the claim.</p>

<h3>Article 12: Delay</h3>
<p>The announced late-return procedures and fees, if any, apply to the renter, and a delay is not treated as automatic consent to an extension of the rental.</p>

<h3>Article 13: Settlement</h3>
<p>The net rental proceeds are settled to the Partner in accordance with the commercial schedule and with the conditions for closing the transaction. The portion connected to a pending claim may be withheld until it is dealt with.</p>

<h2>Annex C: Special terms for the sale of the Individual Partner's products and the commission</h2>
<p>This annex applies upon the activation of the sale service for the Individual Partner.</p>

<h3>Article 1: Ownership and sale</h3>
<p>The Partner confirms that he owns the item and has the right to sell it, and that he does not offer a stolen, lost or counterfeit product, or one that is subject to a right preventing its disposal.</p>

<h3>Article 2: Condition of the product</h3>
<p>A used product must be described accurately, with disclosure of the known defects, repairs and material traces, and the images must be recent and must represent the item offered as far as possible.</p>

<h3>Article 3: Price and commission</h3>
<p>The sale price is set out in the product file or in the transaction summary, and MHGR's commission and the fees specified in the commercial schedule are deducted when entitlement to them arises.</p>

<h3>Article 4: The order and availability</h3>
<p>The product is not regarded as finally sold merely because of a customer's interest or the creation of an order, before payment and any necessary inspection or requirements are completed. The Partner undertakes not to dispose of the product outside the platform after the order is confirmed, except upon a lawful cancellation.</p>

<h3>Article 5: Inspection and authenticity</h3>
<p>An inspection of ownership, authenticity or condition may be requested before the handover. If a material problem appears, the transaction may be suspended or cancelled according to its outcome.</p>

<h3>Article 6: Handover</h3>
<p>The product is handed over by the approved method, and the accessories and the internal identity of the item are documented where needed.</p>

<h3>Article 7: Non-conformity and return</h3>
<p>The buyer's statutory rights and the announced terms of the transaction apply. If the product is materially non-conforming, or a problem appears with its authenticity or ownership, the Partner shall cooperate in the return and in refunding the amounts due according to the case.</p>

<h3>Article 8: Inspection of the returned product</h3>
<p>The returned product may be inspected and its condition documented within %Partner.ReturnInspectionDays% working days, and the buyer does not bear a trace that is not established to have arisen after the handover or for a reason that entails his liability.</p>

<h3>Article 9: Settlement, and a refund after settlement</h3>
<p>The Partner's dues are settled in accordance with the commercial schedule. If the transaction is validly refunded after settlement, the amount due may be dealt with in a subsequent settlement or by a documented request for payment, according to the reason for the refund.</p>

<h3>Article 10: Regulatory obligations where selling is repeated</h3>
<p>If the Partner's activity becomes a commercial or professional practice that requires registration or additional obligations, he undertakes to take the necessary steps and shall not continue to present himself as making occasional individual sales if that no longer reflects reality.</p>

<h3>Acknowledgement and electronic acceptance</h3>
<p>By accepting this Agreement electronically, the party acknowledges that he has been given the opportunity to review and understand the provisions of the Agreement and of the activated annexes, and that his acceptance applies to the version specified below and to the services that have been activated for him, without prejudice to any special regulatory requirements that may apply to a particular document or transaction.</p>
<table style="width:100%;border-collapse:collapse;font-size:14px">
<thead><tr>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold;width:38%">Item</th>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold">Value</th>
</tr></thead>
<tbody>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Name / identifier of the party</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.Name% (%Partner.UserId%)</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Agreement number / identifier</td><td style="border:1px solid #d9d2c5;padding:8px">%Agreement.Id%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Agreement version</td><td style="border:1px solid #d9d2c5;padding:8px">%Agreement.Version%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Activated services</td><td style="border:1px solid #d9d2c5;padding:8px">%Partner.ActivatedServices%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Accepted annexes</td><td style="border:1px solid #d9d2c5;padding:8px">%Agreement.AcceptedAnnexes%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Date and time of acceptance</td><td style="border:1px solid #d9d2c5;padding:8px">%Agreement.AcceptedOn%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Electronic acceptance method</td><td style="border:1px solid #d9d2c5;padding:8px">%Agreement.AcceptanceMethod%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Technical transaction record / reference</td><td style="border:1px solid #d9d2c5;padding:8px">%Agreement.TechnicalRef%</td></tr>
</tbody>
</table>
</div>
""";
}
