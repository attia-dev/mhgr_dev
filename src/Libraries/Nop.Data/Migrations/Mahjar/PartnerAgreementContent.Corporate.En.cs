// [Mahjar partner-agreement]
namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// The English courtesy body of the corporate partner master agreement ("اتفاقية الشريك التجاري — المؤسسات
/// والشركات"), version 1.0, shown to partners whose legal form is an establishment or a company. The Arabic text is
/// the legally prevailing version; this translation is provided for convenience only. Every "[●]" and every checkbox
/// of the original document is a %Partner.*% / %Store.*% / %Agreement.*% token resolved at render time from
/// PartnerAgreementSettings (with the partner's own acceptance record taking precedence, then a dash).
/// Kept in its own file because it is content, not logic. [Mahjar partner-agreement]
/// </summary>
public static partial class PartnerAgreementContent
{
    /// <summary>The English (courtesy) body of the corporate partner master agreement.</summary>
    public const string CorporateEnglish = """
<div dir="ltr" style="font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:860px;margin:0 auto;color:#222;line-height:1.9;text-align:left">
<p style="text-align:center;color:#666;letter-spacing:2px;margin-bottom:4px">MHGR | محجر</p>
<h1 style="text-align:center;margin-bottom:0">Master Commercial Partner Agreement with the MHGR Platform</h1>
<h2 style="text-align:center;margin-top:4px;font-weight:normal">Corporate Partners — Establishments and Companies | Final Operational Version</h2>
<p style="text-align:center;color:#666;margin:4px 0">English courtesy translation. The Arabic text is the legally prevailing version.</p>
<p style="text-align:center;color:#666">Version %Agreement.Version% | Effective date: %Agreement.EffectiveDate%</p>

<h3>Agreement Data</h3>
<table dir="ltr" style="width:100%;border-collapse:collapse;font-size:14px">
<thead><tr>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold;width:38%">Item</th>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold">Value</th>
</tr></thead>
<tbody>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Legal name of MHGR</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Store.LegalName%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Commercial registration of MHGR</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Store.CommercialRegistration%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">National address and contact details</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Store.NationalAddress% — %Store.SupportChannel%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Name of the commercial partner</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.Name%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Legal form (establishment / company / store or other entity)</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.LegalForm%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Commercial registration of the Partner</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.CommercialRegistration%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">VAT number — if applicable</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.VatNumber%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Name and capacity of the authorised signatory</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.SignatoryName% — %Partner.SignatoryCapacity%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Verification / authorisation method</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.VerificationMethod%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Approved settlement account</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.SettlementAccount%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Activated services</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.ActivatedServices%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Effective date</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Agreement.EffectiveDate%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Version number</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Agreement.Version%</td></tr>
</tbody>
</table>

<h3>Preamble</h3>
<p>Whereas MHGR is a Saudi electronic platform for luxury products that operates as an electronic marketplace connecting customers with partners and provides, according to the service, technical and operational tools for managing supply, demand, payment, documentation, delivery, collection, settlement and complaints; and whereas the commercial partner wishes to use the Platform to offer its products for sale and/or for rental; the two parties have agreed that this Agreement shall be the master agreement governing the relationship between them, and that it shall apply to each service only upon the activation of that service's own annex.</p>

<h3>Article (1): Definitions</h3>
<p>For the purposes of this Agreement: "MHGR" means the legal entity that operates the Platform; "the Platform" means MHGR's website, application or digital interfaces; "the Partner" means the contracting establishment, company, store or entity; "the Product" means the listed item; "the Customer" means the end user; "a Sale Transaction" means the purchase of a product through the Platform; "a Rental Transaction" means enabling a renter to hold the product temporarily in return for consideration; "the Service Annex" means the annex specific to sale or to rental; "the Commercial Schedule" means the schedule of commissions, fees and settlements; "the Product File" means the digital record of the product; "the Condition Report" means the report documenting the condition of the product before delivery and upon return in the case of rental; "the Delivery Point" means the location or entity approved within MHGR's procedures; "Settlement" means the transfer of the net amounts due to the Partner once the conditions for them are met; "the Transaction Documents" means all records, reports and approvals relating to the transaction.</p>

<h3>Article (2): Subject Matter and Scope of the Agreement</h3>
<p>This Agreement governs the general relationship between MHGR and the Partner with respect to the listing of products, account and data management, ownership, authenticity, commissions, settlements, privacy, confidentiality, liability and electronic records. The provisions specific to sale or to rental apply only upon the activation of the annex for that service.</p>
<p>The Commercial Schedule and any activated Service Annex form an integral part of this Agreement.</p>

<h3>Article (3): Activated Services</h3>
<p>The activated services are determined in the Commercial Schedule or in the electronic activation record. They may be rental only, sale only, or both services. The activation of one service does not give rise to an automatic right to have the other service activated.</p>

<h3>Article (4): Nature of the Relationship and the Role of MHGR</h3>
<p>MHGR is an electronic platform and a technical and operational intermediary; ownership of the products does not pass to it merely by reason of listing or of temporary possession for inspection or delivery. The role and the responsibilities of MHGR are determined according to the service, the actual transaction and the work it undertakes.</p>
<p>This Agreement does not create a legal partnership, an employment relationship or a general agency between the parties. The Partner is not a representative of MHGR, and MHGR is not a representative of the Partner, except within the limits of an express and specific authorisation, if any.</p>

<h3>Article (5): Partner Data and Authorisation</h3>
<p>The Partner represents that its legal data and the data of its authorised representative are correct, and that the commercial registration and the licences necessary for its activity are valid, and it undertakes to update them immediately upon any material change.</p>
<p>No employee or representative who is not authorised may enter into obligations in the name of the Partner through the Platform beyond the limits of their recorded powers.</p>

<h3>Article (6): Compliance with Laws and Licences</h3>
<p>Each party shall comply with the laws and regulations applicable to its role. The Partner shall maintain the licences, registrations and the tax and commercial obligations necessary to carry on its activity, and the acceptance of its account on the Platform does not constitute an exemption from any independent statutory obligation.</p>

<h3>Article (7): Ownership of the Products and the Right of Disposal</h3>
<p>The Partner represents that it owns the listed products or holds the legal right to sell or to rent them according to the service, and that it has no knowledge of any pledge, attachment, claim or third-party right that prevents their disposal.</p>
<p>MHGR may reasonably request whatever is necessary to establish ownership, the source of the product or the right of disposal, and may suspend the product or the transaction where there is a serious indication of a dispute over ownership.</p>

<h3>Article (8): Authenticity and Anti-Counterfeiting</h3>
<p>The Partner undertakes not to offer a counterfeit product as genuine, and warrants the correctness of any description, certificate or claim relating to authenticity. MHGR may request documents or a specialist inspection before or after listing where there is an operational need or a risk indicator.</p>
<p>Acceptance of a listing does not constitute an absolute certification by MHGR of the authenticity of the product unless MHGR provides an independent and express verification service in that capacity.</p>

<h3>Article (9): The Product File and Information</h3>
<p>The Partner shall provide an accurate and truthful description of the product, including the category, the brand, the model, the condition, the known material defects, the accessories, the reference or serial number where needed, appropriate images, and any information necessary for an informed decision.</p>
<p>MHGR may withhold the full serial number and any unnecessary data from public display while retaining them internally for verification and risk-management purposes.</p>

<h3>Article (10): Inventory and Availability</h3>
<p>The Partner shall keep the availability status up to date and shall not confirm an order for a product that is not available. MHGR may suspend the product where cancellations resulting from inaccurate inventory recur.</p>

<h3>Article (11): Prices and Transparency</h3>
<p>The sale or rental price and the mechanism for amending it are determined in accordance with the Service Annex and the Commercial Schedule. The price or the fees relating to a confirmed transaction must not be changed retroactively except with the necessary consent or for a statutory reason.</p>
<p>The material information relating to the price, the fees, the taxes and the delivery or performance is displayed to the user before the transaction is completed, as applicable.</p>

<h3>Article (12): Commissions and Fees</h3>
<p>MHGR is entitled to the commission and the fees set out in the Commercial Schedule in force at the time of the transaction. No fees that have not been disclosed to the Partner are due under the agreed relationship.</p>
<p>Any change to the commission or the fees does not apply to a transaction confirmed before the change takes effect, unless the parties expressly agree otherwise or the change is imposed by law.</p>

<h3>Article (13): Payments and Collection</h3>
<p>Payments are made through the payment methods approved by MHGR or by the relevant payment services provider. This Agreement does not presume that MHGR holds customer funds or provides an escrow account service unless a separate and express arrangement so provides.</p>
<p>The Partner shall not ask the customer to transfer the value of a transaction that arose through MHGR to an account outside the approved route with the intention of bypassing the Platform or its protection procedures.</p>

<h3>Article (14): Financial Settlements</h3>
<p>The Partner's dues are settled in accordance with the Commercial Schedule and after the entitlement conditions specific to the service are met. The portion directly connected to a dispute, a return or a payment chargeback may be withheld until its effect has been determined, without withholding amounts unrelated to the matter without justification.</p>
<p>Set-off or recovery from subsequent settlements may be effected where an amount is established as being due to MHGR or to a customer as a result of a return, a valid refund or an error in a previous settlement, provided that there is a clear contractual or statutory basis and a record of the amount and its reason.</p>

<h3>Article (15): Invoices and Taxes</h3>
<p>Each party bears the tax and invoicing obligations that fall upon it according to its capacity and role. Where the Partner is the legal supplier or seller, it remains responsible for issuing the invoice or the required tax document unless a separate statutory arrangement permits MHGR to issue it on its behalf.</p>
<p>This Agreement does not set any tax rate that is not prescribed by law; taxes apply in accordance with the regulations in force at the time of the transaction.</p>

<h3>Article (16): General Obligations of MHGR</h3>
<p>MHGR shall operate the Platform and manage the transactions within the scope of its agreed role, keep the essential operational records, display the information that must be disclosed, apply reasonable procedures for complaints and disputes, and engage service providers where needed, having regard to the relevant statutory obligations.</p>

<h3>Article (17): General Obligations of the Partner</h3>
<p>The Partner shall fulfil confirmed orders, prepare the correct product in the agreed condition and with the agreed accessories, cooperate in inspection, delivery, returns and disputes, and respond to MHGR's requests within the operational periods specified in the Schedule or in the system.</p>

<h3>Article (18): Delivery and Collection Points</h3>
<p>The Partner's store or another location may be approved as a delivery and collection point if MHGR agrees to that. In that case the Partner and its employees shall comply with MHGR's procedures, shall not deliver or receive the product outside the approved system status, and shall document the employee, the time, the location and the product in accordance with the prescribed mechanism.</p>

<h3>Article (19): Protection of Customer Data</h3>
<p>The Partner may not use customer data except to the extent necessary to perform the transaction, to comply with a statutory obligation, or on another legitimate basis. The data of an MHGR customer may not be used for independent marketing or shared with a party for whom it is not necessary without an appropriate basis.</p>
<p>Both parties shall apply the appropriate technical and organisational controls to the data under their control, and their roles in the processing of the data are determined according to the actual nature of the processing, the privacy policy and the relevant arrangements.</p>

<h3>Article (20): Confidentiality</h3>
<p>Each party shall preserve the confidentiality of the non-public information it obtains by reason of the relationship, including commercial, financial and technical information and transaction data, and shall not disclose it except to employees, advisers or service providers who need it, or where there is a statutory obligation.</p>

<h3>Article (21): Intellectual Property and Content</h3>
<p>Each party remains the owner of its marks and rights. The Partner grants MHGR a non-exclusive licence, limited to the term of the relationship, to use its trade name, its mark, the images of the products and their descriptions to the extent necessary to display and market the products and to operate the service.</p>
<p>The Partner represents that the content it provides does not infringe the rights of others, and shall remove or correct any content where the right to use it is shown not to be valid.</p>

<h3>Article (22): Reviews and Experience-Related Content</h3>
<p>The Platform may enable customers to review their experience in accordance with its rules. It is prohibited to create fake reviews, to manipulate them, or to pressure customers in return for a misleading review.</p>

<h3>Article (23): Prevention of Off-Platform Circumvention</h3>
<p>If the Partner comes to know a customer or an order by reason of MHGR, it may not move that same transaction or its agreed direct extension outside the Platform with the intention of avoiding the commission or the protection procedures. This clause shall not be construed as a general and permanent prohibition on every independent relationship that does not arise from the circumvention of an MHGR transaction.</p>

<h3>Article (24): Anti-Fraud and Unlawful Use</h3>
<p>MHGR may suspend an order, a product or an account where there are serious indications of fraud, forgery, a counterfeit product, disputed ownership or misuse, provided that the measure is proportionate to the risk and limited to what is necessary in order to verify and to protect the parties.</p>

<h3>Article (25): Complaints and Operational Disputes</h3>
<p>The parties shall cooperate in handling complaints and in providing the relevant information and evidence. An internal operational decision of MHGR does not constitute a final judicial ruling and does not prevent any party from exercising its right before the competent authorities.</p>

<h3>Article (26): Electronic Records and Evidence</h3>
<p>MHGR may keep electronic records of the Agreement and its annexes, of the activation of services, of orders, approvals, payment, delivery, collection, reports and correspondence. These records are used in accordance with the laws relating to evidence and electronic transactions, without being regarded as conclusive evidence that is not open to discussion.</p>

<h3>Article (27): Engagement of Service Providers</h3>
<p>MHGR may engage providers of payment, technology, identity, transport, inspection, storage or communication services in order to perform the service, without this in itself changing the allocation of statutory responsibilities between the parties. The Partner shall cooperate with the relevant approved procedures.</p>

<h3>Article (28): Liability and Allocation of Risk</h3>
<p>Each party bears responsibility for its own breach and for the obligations falling within the scope of its control in accordance with the law and with this Agreement. This Agreement contains no exemption from a liability from which exemption is not legally permitted.</p>
<p>MHGR does not guarantee to the Partner the achievement of any minimum level of sales or rentals, and does not guarantee the solvency of any customer or the collection of the full value of a lost or unreturned product, unless it has provided an independent and express guarantee on its own terms.</p>

<h3>Article (29): Indemnity for Claims Arising from the Partner's Breach</h3>
<p>The Partner bears such direct damages or claims as are legally established to have arisen from its material breach of the obligations of ownership, authenticity or the right of disposal, from the infringement of the rights of others, or from providing misleading information, without extending the indemnity to lump-sum or unproven amounts.</p>

<h3>Article (30): Force Majeure and Circumstances Beyond Control</h3>
<p>A party is not regarded as in breach to the extent that performance is impossible for it by reason of a circumstance beyond its control as recognised by law, and it shall notify the other party and take reasonable steps to mitigate the effect and to resume performance whenever possible.</p>

<h3>Article (31): Term of the Agreement</h3>
<p>This Agreement begins on the date of its acceptance or signature and continues for a term of %Partner.AgreementTerm%, and is renewed in accordance with %Partner.RenewalBasis%, unless it is terminated in accordance with its provisions.</p>

<h3>Article (32): Suspension and Termination</h3>
<p>Either party may terminate this Agreement by notice of %Partner.TerminationNoticeDays% days/business days, having regard to ongoing transactions. MHGR may suspend immediately where there is a material breach, a risk of fraud, a counterfeit or stolen product, or a statutory reason requiring it, while allowing the matter to be remedied where that is appropriate.</p>

<h3>Article (33): Effect of Termination</h3>
<p>The expiry of the Agreement does not affect confirmed orders, settlements, returns, disputes or obligations that arose before termination, and the provisions on confidentiality, data, intellectual property, records and prior liabilities remain in force to the extent necessary.</p>

<h3>Article (34): Amendment and Version Management</h3>
<p>The Agreement or its annexes may be updated for operational or statutory reasons, and the version number and the effective date are displayed. Material amendments do not apply retroactively to a confirmed transaction without a statutory basis or appropriate acceptance.</p>

<h3>Article (35): Notices</h3>
<p>Notices are sent to the registered contact details, through the Platform, or to the approved email address: %Partner.Email%. The Partner shall keep its data up to date, and the cases that require a special formal notice are determined by law or by agreement.</p>

<h3>Article (36): Order of Precedence of the Documents</h3>
<p>In the event of conflict, precedence is given to the mandatory laws, then to the terms specific to the transaction, then to the activated Service Annex, then to this Master Agreement, then to the Commercial Schedule, then to the operational procedures to the extent that they do not conflict with the foregoing.</p>

<h3>Article (37): Assignment and Transfer</h3>
<p>The Partner may not transfer this Agreement, its account or its material obligations to another party without the consent of MHGR, unless the law requires otherwise. MHGR may reorganise the performance of the service through an affiliate or a service provider without transferring its statutory obligations in a manner that prejudices the rights of the other party.</p>

<h3>Article (38): Governing Law and Dispute Resolution</h3>
<p>This Agreement is governed by the laws of the Kingdom of Saudi Arabia. The parties shall endeavour to settle the dispute amicably within %Partner.AmicableSettlementDays% business days from the completion of the notice of the dispute, and if resolution is not possible the dispute falls within the jurisdiction of the competent judicial authority in the Kingdom, unless the parties agree in a separate valid document upon another lawful means.</p>

<h3>Article (39): Electronic Acceptance</h3>
<p>This Agreement and the acceptance of its annexes may be concluded electronically in accordance with the relevant laws. The system retains the identifier of the party, the name of the document, the version number, the date and time of acceptance, the activated services, and the technical reference associated with the acceptance.</p>

<h2>Annex (A): Activation of Services and the Commercial Schedule</h2>
<p>This Annex sets out the activated services and the variable commercial data. Any field that is not completed is not approved until it has been agreed and recorded.</p>
<table dir="ltr" style="width:100%;border-collapse:collapse;font-size:14px">
<thead><tr>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold;width:38%">Item</th>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold">Approved value</th>
</tr></thead>
<tbody>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Activated services</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.ActivatedServices%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Rental commission</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.RentalCommission%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Sale commission</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.SaleCommission%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Listing fee — if any</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.ListingFee%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Inspection fee — if any</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.InspectionFee%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Delivery / collection fee — if any</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.DeliveryFee%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Storage fee — if any</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.StorageFee%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Order preparation time</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.PrepTime% (hours / working days)</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Settlement cycle / period</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.SettlementCycle%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Minimum settlement amount — if any</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.MinimumSettlement%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Time limit for responding to an operational claim</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.ClaimResponseDays% business days</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Termination notice period</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.TerminationNoticeDays% days</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Approved settlement account</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.SettlementAccount%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Other commercial notes</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.CommercialNotes%</td></tr>
</tbody>
</table>

<h2>Annex (B): Special Terms for the Rental Service for the Commercial Partner</h2>
<p>This Annex applies only if the rental service is activated for the Partner. It supplements the Master Agreement and does not replace it.</p>

<h3>Article (1): Scope of the Rental Service</h3>
<p>The Partner makes its products available for rental through the Platform in accordance with the periods, the prices and the availability set out in each order, and MHGR manages the transaction according to its operational and technical role.</p>

<h3>Article (2): Limits on the Acceptance of Products</h3>
<p>The acceptance of a product is subject to the policies on risk, category, condition and value. The current pilot cap for the value of an item eligible for rental is SAR %Partner.RentalItemValueCap%, unless MHGR approves otherwise in writing or electronically.</p>

<h3>Article (3): Eligibility of the Renter</h3>
<p>MHGR manages the eligibility of the renter and the applicable limits in accordance with the risk procedures available to it, and the acceptance of a renter does not constitute a guarantee of their performance or their solvency, nor that the product will not be exposed to any incident.</p>

<h3>Article (4): The Rental Order and Its Confirmation</h3>
<p>The order does not become ready for delivery until the applicable requirements of availability, approval, payment, contractual acceptance, guarantees and the condition report have been completed. Payment alone is not sufficient to oblige the Partner to deliver if material requirements remain incomplete.</p>

<h3>Article (5): Preparation of the Product</h3>
<p>The Partner shall prepare the same listed product with the accessories and in the condition described, and shall not substitute it except after the transaction has been updated and the necessary approvals have been obtained.</p>

<h3>Article (6): Condition Report of the Item</h3>
<p>A condition report must be created before delivery, including, according to the type of product, the images, the video, the reference or serial number, the accessories, the previous defects or scratches, and the condition of the material parts. The report forms an integral part of the transaction.</p>
<p>The original report may not be amended after delivery in a way that erases the previous version, and any subsequent correction must be recorded together with its time effect.</p>

<h3>Article (7): Delivery and the Chain of Possession</h3>
<p>Delivery takes place through the approved point or mechanism, and a verification code, a QR code or another electronic means may be used. The system documents the product, the order, the time, the place, the employee or entity, and the verification method.</p>
<p>If the Partner's store is an approved delivery point, the product shall not be delivered before the authorised operational status appears in the system.</p>

<h3>Article (8): The Rental Period and Availability</h3>
<p>The rental period, the return date and the price are determined in the order summary. The renter's continued possession after the due date is not an automatic extension, and any extension must be approved through the Platform.</p>

<h3>Article (9): Return and Inspection</h3>
<p>Upon return, a new condition report is created and the product, the serial number, the accessories and the condition are compared with the previous data. If a new observation appears, the order is not closed as sound before the case has been handled.</p>

<h3>Article (10): Normal Wear and Damage</h3>
<p>The effect of normal use that is proportionate to the nature of the product and the rental period is not automatically regarded as damage giving rise to compensation. Any claim of new damage is reviewed on the basis of the evidence, the cause, and the relationship of the damage to the renter's possession.</p>

<h3>Article (11): Assessment of Damage</h3>
<p>The Partner's unilateral assessment is not binding on the renter. Reliance may be placed on the before/after report, the images and video, the reasonable cost of repair, the cost of the part, the established diminution in value, or the replacement value where repair is not possible, with an expert where needed.</p>

<h3>Article (12): Missing Accessories</h3>
<p>A claim for a missing accessory is not accepted unless it is documented within the delivery documents, and the loss is assessed on the basis of the replacement cost or the established economic effect.</p>

<h3>Article (13): Loss and Theft</h3>
<p>In the event of loss or theft, a review case is opened and the evidence and official reports are collected where needed. Describing the incident as theft does not lead to the automatic exemption or inculpation of any party, and liability is determined according to the circumstances, the evidence and the law.</p>

<h3>Article (14): Delay and Failure to Return</h3>
<p>Where the return date is exceeded without an approved extension, the transaction is recorded as late and the procedures of communication, escalation and the announced fees, if any, are applied. If the failure to return continues, the customer may be suspended, the agreed protection measures activated, and the appropriate legal action taken.</p>
<p>MHGR does not guarantee to the Partner the recovery or the collection of the full value of the product except under an independent and express guarantee.</p>

<h3>Article (15): Guarantees and the Promissory Note</h3>
<p>MHGR may require a financial guarantee, an electronic promissory note or another protection measure according to the transaction and as disclosed. None of these alone constitutes an absolute guarantee of collection or automatic proof of every future claimed amount.</p>

<h3>Article (16): Cancellation</h3>
<p>Cancellations are handled according to the stage of the order, the reason for the cancellation and the customer's statutory rights. The Partner shall not cancel a confirmed order without a legitimate reason, and the effects of cancellation and the fees are determined in the Schedule or in the terms of the transaction.</p>

<h3>Article (17): Settlement of the Rental Proceeds</h3>
<p>The Partner is entitled to the net rental proceeds after the settlement conditions set out in the Commercial Schedule have been met, and the disputed portion may be withheld until the effect of the damage, the return or the chargeback has been determined.</p>

<h2>Annex (C): Special Terms for the Sale Service and the Commission for the Commercial Partner</h2>
<p>This Annex applies only if the sale service is activated for the Partner. It supplements the Master Agreement and the Commercial Schedule.</p>

<h3>Article (1): Scope of Sale</h3>
<p>The Partner offers its products for sale through MHGR in accordance with the price, the condition, the description and the availability announced for each product, and the purchase, delivery, return and settlement are managed in accordance with this Annex.</p>

<h3>Article (2): Description and Condition of the Product</h3>
<p>It must be stated whether the product is new, used or refurbished where that applies, and the known material defects and the traces of use that affect the buyer's decision must be disclosed.</p>

<h3>Article (3): Price and the Order</h3>
<p>The price of the product is determined in its file or in the order summary, and the total of the fees, taxes and costs that must be disclosed is displayed to the buyer before the order is completed. The price of a confirmed transaction may not be changed retroactively without a legitimate reason and the necessary consent.</p>

<h3>Article (4): Confirmation of Availability and Preparation</h3>
<p>The Partner shall confirm availability and prepare the same product with the accessories described within %Partner.PrepTime% from the notification of the order, or in accordance with the Commercial Schedule.</p>

<h3>Article (5): Inspection and Verification</h3>
<p>MHGR may subject the product to a condition, conformity or authenticity inspection according to the service or the risk, and the success of a limited operational inspection is not an absolute guarantee extending beyond the scope of the announced inspection.</p>

<h3>Article (6): Delivery</h3>
<p>Delivery takes place in accordance with the approved mechanism and is evidenced in a manner appropriate to the transaction. The Partner shall cooperate in issuing or providing the documents required for the customer where responsibility for them falls upon it.</p>

<h3>Article (7): Non-Conformity and Authenticity</h3>
<p>If the product differs materially from the description, or a serious issue appears regarding ownership or authenticity, the transaction may be suspended, the settlement stopped, or the return and investigation commenced according to the case, while preserving the statutory rights of the parties.</p>

<h3>Article (8): Cancellation and Return</h3>
<p>The buyer's rights prescribed by law and the cancellation and return terms announced for the transaction apply. This Annex may not be construed as a waiver of a mandatory consumer right.</p>
<p>The party bearing the cost of the return or of the services performed is determined according to the reason for the return, what was disclosed, and the relevant laws.</p>

<h3>Article (9): Inspection of the Returned Item</h3>
<p>Where a returned product needs to be inspected, the inspection takes place within %Partner.ReturnInspectionDays% business days from its receipt and its condition and accessories are documented, without charging the buyer with prior or unestablished damage.</p>

<h3>Article (10): Refunds</h3>
<p>The amount due to the buyer is processed through the payment method or the approved mechanism. If the amount has already been settled to the Partner, the effect of the return may be processed in the subsequent settlements according to the reason for the return and the Commercial Schedule.</p>

<h3>Article (11): Payment Chargebacks</h3>
<p>The Partner shall cooperate in responding to payment chargebacks or bank refunds by providing the evidence of the transaction within the period of %Partner.ClaimResponseDays% business days or within the period imposed by the payment provider, and shall bear the financial effect that is established to be connected to its breach or to the validity of the reversal of the transaction under the contract and the law.</p>

<h3>Article (12): Commission and Settlement</h3>
<p>The sale commission is due in accordance with the Commercial Schedule, and the dues are settled after the settlement conditions have been met. The listing of the product or the creation of the order alone is not a conclusive ground for entitlement to settlement before the specified requirements have been completed.</p>

<h2>Acknowledgement and Electronic Acceptance</h2>
<p>By accepting this Agreement electronically, the party acknowledges that it was given the opportunity to review and to understand the provisions of the Agreement and of the activated annexes, and that its acceptance applies to the version specified below and to the services that have been activated for it, without prejudice to any special statutory requirements that may apply to a particular document or transaction.</p>
<table dir="ltr" style="width:100%;border-collapse:collapse;font-size:14px">
<thead><tr>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold;width:38%">Item</th>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold">Value</th>
</tr></thead>
<tbody>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Name / identifier of the party</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.Name% (%Partner.UserId%)</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Agreement number / identifier</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Agreement.Id%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Agreement version</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Agreement.Version%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Activated services</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Partner.ActivatedServices%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Accepted annexes</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Agreement.AcceptedAnnexes%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Date and time of acceptance</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Agreement.AcceptedOn%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Electronic acceptance method</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Agreement.AcceptanceMethod%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">Technical transaction record / reference</td><td style="border:1px solid #d9d2c5;padding:8px;text-align:left">%Agreement.TechnicalRef%</td></tr>
</tbody>
</table>
</div>
""";
}
