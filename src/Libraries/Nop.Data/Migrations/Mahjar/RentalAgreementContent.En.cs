// [Mahjar contracts]
namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// The English body of the rental agreement — a courtesy translation of <see cref="RentalAgreementContent.Arabic"/>
/// for customers whose contract language is English. It carries the same %Contract.*% / %Customer.*% / %Store.*%
/// tokens and states that the Arabic text prevails, because the Arabic wording is the store owner's legal text.
/// [Mahjar contracts]
/// </summary>
public static partial class RentalAgreementContent
{
    /// <summary>The English (courtesy) body of the rental agreement.</summary>
    public const string English = """
<div dir="ltr" style="font-family:Segoe UI,Tahoma,Arial,sans-serif;max-width:860px;margin:0 auto;color:#222;line-height:1.7;text-align:left">
<h1 style="text-align:center;margin-bottom:0">Renter Agreement on the MHGR Platform</h1>
<h2 style="text-align:center;margin-top:4px;font-weight:normal">Terms for renting items through the MHGR platform</h2>
<p style="text-align:center;color:#666">Version %Contract.Version% | Effective: %Contract.EffectiveDate% | Last updated: %Contract.UpdatedDate%</p>
<p style="text-align:center;color:#8C3B3B">This English text is provided for convenience. The Arabic version of this agreement is the binding one, and it prevails in the event of any difference.</p>

<h3>Agreement and transaction details</h3>
<table style="width:100%;border-collapse:collapse;font-size:14px">
<thead><tr>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold;width:38%">Item</th>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold">Value</th>
</tr></thead>
<tbody>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Platform operator</td><td style="border:1px solid #d9d2c5;padding:8px">%Store.LegalName% — commercial registration no. %Store.CommercialRegistration%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Agreement number</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.Number%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Rental order number</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.OrderNumber%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Agreement date</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.Date%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Renter</td><td style="border:1px solid #d9d2c5;padding:8px">%Customer.FullName%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">User identifier</td><td style="border:1px solid #d9d2c5;padding:8px">%Customer.Id% (%Customer.Email%)</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Identity verification method / reference</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.VerificationRef%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Rented item</td><td style="border:1px solid #d9d2c5;padding:8px">As set out in the table of items below</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Rental period</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.RentalPeriod%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Rental price</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.SubTotal%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Additional fees</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.ExtraFees%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Tax</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.Tax%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Guarantee required</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.GuaranteeType%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Guarantee amount, if any</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.GuaranteeAmount%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Late-return charge, if any</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.LateFee%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Handover time and place</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.DeliveryDetails%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Return time and place</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.ReturnDetails%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Support channel</td><td style="border:1px solid #d9d2c5;padding:8px">%Store.SupportChannel%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Trial item-value cap</td><td style="border:1px solid #d9d2c5;padding:8px">Up to %Contract.ItemValueCap%, unless MHGR approves a different cap for the user, the category or the transaction</td></tr>
</tbody>
</table>

<h3>The rented items</h3>
%Contract.ItemsTable%

<h3>Preamble and acceptance</h3>
<p>This agreement governs the relationship between MHGR and the renter for every rental made through the platform. The material details of each transaction are shown to the renter before it is confirmed, and the rental order summary, the item condition report, the handover record, the return record and any related guarantee or separate instrument form part of the transaction record under this agreement. Nothing in this agreement detracts from any mandatory consumer right granted by the laws in force in the Kingdom of Saudi Arabia.</p>
<p>The renter's electronic acceptance of this agreement, after being given the opportunity to read it, constitutes agreement to be bound by its terms and by the specific terms of every rental order confirmed through their account. A copy of the agreement, its version number and the date and time of acceptance are kept in the electronic record of the transaction.</p>

<h3>Article 1: Definitions</h3>
<ul>
<li>"MHGR": the electronic platform and its legal operator named in the agreement details, according to the role it actually performs in the transaction.</li>
<li>"The platform": the MHGR website, application or electronic systems through which the rental services are operated.</li>
<li>"The renter": the account holder who requests the rental of the item and actually receives it under this agreement and the order summary.</li>
<li>"The lessor": the individual owner or the commercial partner who offers the item for rent through MHGR.</li>
<li>"The item" or "the product": the product identified in the rental order summary and handed to the renter for a defined period.</li>
<li>"Rental order": the electronic order created by the renter, subject to availability, verification, payment, guarantees and the approved terms before handover.</li>
<li>"Rental order summary": the record of each transaction, stating at least the item details, the rental period, the price, the fees, the tax, the handover and return times and places, the guarantees and any special terms disclosed before commitment.</li>
<li>"Item condition report": the electronic record documenting the condition of the item and its accessories before handover and on return, including photographs, notes, prior scratches or defects and the necessary identifying data.</li>
<li>"Handover record": the record documenting the actual handover of the item and the passing of possession to the renter.</li>
<li>"Return record": the record documenting the actual return of the item to the approved party and the result of the initial inspection on return.</li>
<li>"Chain of possession": the record documenting the movement of the item between the approved parties or persons, from its receipt for preparation until its return and the closing of the transaction, as applicable.</li>
<li>"Guarantee": any financial guarantee, electronic promissory note or other lawful protection disclosed before commitment, according to the transaction.</li>
<li>"Return time": the date, time and place or method stated in the rental order summary for returning the item.</li>
<li>"Electronic record of the transaction": the data, approvals, reports, notifications, correspondence and electronic events connected with the rental order.</li>
</ul>

<h3>Article 2: Scope and supplementary documents</h3>
<p>This agreement applies to the rentals the renter makes through the platform. The rental order summary, the item condition report, the handover record, the return record and any guarantee or separate instrument connected with the transaction are supplementary documents to it, to the extent they relate to that transaction. No supplementary document is to be interpreted so as to reduce a mandatory consumer right granted by law.</p>

<h3>Article 3: Capacity and account validity</h3>
<p>The renter must have the legal capacity to contract, provide correct and up-to-date information, and use the account personally or through someone legally authorised to do so. MHGR may require appropriate verification before making rental available, and the renter must update any material information that affects the performance of the transaction.</p>

<h3>Article 4: Verification and rental eligibility</h3>
<p>MHGR may verify the user's identity through approved means or service providers. Successful verification does not by itself create a right to rent: MHGR may set rental eligibility or a rental cap according to risk factors, the operating record, the value and type of the item and the available protections, in a manner consistent with the law. During the trial phase, approval may be granted manually by MHGR.</p>

<h3>Article 5: Pre-contract disclosure</h3>
<p>Before the renter commits to the transaction, MHGR displays the available material information, including the description and main characteristics of the item, the rental period, the total price, fees, taxes and any additional amounts relating to handover, the payment, handover and return arrangements, the type of guarantee required and its terms, and any special condition that affects the renter's decision.</p>

<h3>Article 6: The rental order summary and transaction-specific terms</h3>
<p>A rental order summary is issued for every transaction, stating the commercial and operational values that vary for it, including the item and its reference value, the rental period and price, the fees, the tax, the handover and return times, the guarantee required and any late-return charge. The summary forms part of this agreement, and no fee may be added that was not disclosed, or that is not determinable from what was shown to the renter, before it falls due.</p>

<h3>Article 7: Accepting and performing a rental order</h3>
<ol>
<li>The renter submits the rental order through the platform and selects the item and an available period.</li>
<li>Availability, the renter's eligibility and the risk requirements of the transaction are reviewed.</li>
<li>The material information, the rental order summary and the transaction-specific terms are shown to the renter before confirmation.</li>
<li>The renter accepts this agreement and the transaction-specific terms electronically through the approved mechanism.</li>
<li>The renter completes payment of the amounts due under Article 9 (payment and invoice).</li>
<li>The renter provides the required guarantees, if any, under Article 10 (financial guarantee and promissory note).</li>
<li>The item is prepared and its condition report is produced under Article 11 (item condition report before receipt).</li>
<li>The renter reviews what is needed of the condition report and reports any apparent material difference before confirming receipt, where possible.</li>
<li>The documented handover takes place under Article 12 (handover and passing of possession), and the rental period starts according to the order's terms.</li>
<li>The renter returns the item at the time and in the manner specified; the return record is then created and the transaction is closed, or a review is opened where there is a delay, damage, loss, non-return or dispute.</li>
</ol>

<h3>Article 8: The order, availability and refusal before handover</h3>
<p>Creating a rental order does not entitle the renter to handover before availability is confirmed and the conditions are met. An order may be refused or cancelled before handover where the item is unavailable, verification fails, payment or the required guarantee is not completed, a material problem appears regarding ownership, authenticity or the soundness of the item, or there are serious and justified risks that prevent completing the transaction. The financial effect of cancellation is handled under Articles 25 (cancellation before handover), 26 (statutory rights of withdrawal) and 29 (refunds), as the case may be.</p>

<h3>Article 9: Payment and invoice</h3>
<p>Payments are made through the channels approved by MHGR, and the total amount due is displayed before completion. An invoice or transaction statement is issued in accordance with the applicable legal requirements and the role of each party. The renter may not pay the value of a transaction that arose through MHGR directly to the lessor in order to bypass the platform, its fees or the agreed protections.</p>

<h3>Article 10: Financial guarantee and promissory note</h3>
<p>A transaction may require a financial guarantee, an electronic promissory note or another lawful protection. The type, amount and terms of the guarantee are stated before commitment or in a separate document connected with the transaction; the amount of the guarantee or note, if any, is as stated in the order summary or its own document: %Contract.GuaranteeAmount%. The mere existence of a promissory note or guarantee, or successful identity verification, does not automatically establish every future claim or guarantee the economic ability to collect; facts and claims outside the scope of the note or guarantee remain subject to proof, procedure and the applicable laws.</p>

<h3>Article 11: The item condition report before receipt</h3>
<p>MHGR or the approved party produces a condition report for the item containing, according to its nature, photographs, notes, prior scratches or defects, the accessories and the necessary identifying data, and it may include a video and an internal reference or serial number. The condition report forms part of the transaction. What is needed of the report is shown to the renter before or at receipt according to the design of the service, and the renter may raise any apparent material difference before confirming receipt, where possible. The renter does not automatically bear any defect or prior damage documented in the report.</p>

<h3>Article 12: Handover and passing of possession</h3>
<p>Handover takes place at the approved location or by the approved method, and may be documented by a single-use verification code, a QR code, or another electronic or operational means approved by MHGR. Handover does not take place before the requirements of the transaction are complete, including identity, eligibility, acceptance, payment, the required guarantee and the condition report, as applicable. The renter's responsibility for possession starts from the documented actual receipt and ends on the actual, documented return to the approved party, without prejudice to any claim arising from an event that occurred during the period of possession.</p>

<h3>Article 13: Chain of possession</h3>
<p>MHGR may document the movement of the item in the chain of possession, including the time of receipt and handover, the approved party or person holding the item, the location where needed, and any material event connected with its movement. The record is kept in the transaction file to help establish responsibility at each stage, without being by itself conclusive in any dispute.</p>

<h3>Article 14: Duty of care, use, and prohibited use</h3>
<ul>
<li>Keeping the item with the care of an ordinary person and in a manner suited to its nature and value.</li>
<li>Using the item for its ordinary purpose and within the agreed benefit and the order's terms.</li>
<li>Not selling, pledging, sub-letting, assigning or handing the item to others except as MHGR or the lessor expressly permits.</li>
<li>Not modifying, repairing, opening, engraving, removing or obscuring a serial number or identifying mark without permission.</li>
<li>Not using the item in a way that exposes it to unusual risk or in conditions plainly unsuited to its nature.</li>
<li>Notifying MHGR without delay of any loss, theft, material damage or incident that may affect the safety or the return of the item.</li>
</ul>

<h3>Article 15: Rental period and extension</h3>
<p>The rental period begins and ends according to the rental order summary, taking into account the documented actual handover time. The renter keeping the item after the return time is not an automatic extension. No extension applies unless the platform approves it and the necessary fees, guarantees and requirements are met; the extension is recorded in the transaction record.</p>

<h3>Article 16: Return</h3>
<p>The renter must return the same item and all the accessories received, to the place, at the time and in the manner specified in the order summary. This obligation continues even if a financial disagreement or claim arises, unless MHGR or a competent body directs otherwise. The return is established by the return record under the annex on the item condition report and the handover and return records.</p>

<h3>Article 17: Late return</h3>
<p>If the item is not returned at the return time without an approved extension, the transaction is recorded as late and the notifications and escalation begin. The initial grace period, where adopted, is %Contract.LateGraceHours% hours from the return time. Any late charge must be fixed or determinable and disclosed in the order summary before it falls due; its amount or the way it is calculated is: %Contract.LateFee%. Paying any late charge does not remove the duty to return the item. If the item remains unreturned, Article 24 (non-return) applies.</p>

<h3>Article 18: Ordinary use and fair wear</h3>
<p>The renter does not automatically bear the effects of ordinary use proportionate to the nature of the item and the rental period. The distinction between fair wear and damage is based on the condition of the item before handover and after return, its nature and value, the technical evidence and appropriate custom, without charging the renter for damage for which responsibility is not established.</p>

<h3>Article 19: Damage and the renter's responsibility</h3>
<p>The renter is responsible for damage proven to have arisen from their transgression, negligence, misuse or breach of their contractual obligations in a way that entails responsibility under the law. The renter does not automatically bear documented prior damage, an internal defect not arising from their use, fair wear, or damage not proven to be attributable to them. A mere allegation by the lessor, or an undocumented difference, is not by itself sufficient to oblige the renter to pay a given amount.</p>

<h3>Article 20: Return inspection and the damage review file</h3>
<p>On return, the documentation is repeated to the appropriate extent and the condition report from before handover is compared with the later report. If a new difference or a missing accessory appears, a damage review is opened and the transaction is not closed as sound before the case is handled. The renter may review the basis of the claim and the available evidence and submit an objection or counter-evidence within %Contract.ObjectionDays% working days of being notified that the claim details are complete, without prejudice to the right of objection provided by law.</p>

<h3>Article 21: Assessing damage</h3>
<p>Damage is assessed on the basis of the available evidence, the condition reports, the inspection and a technical assessment where needed. The assessment may cover the reasonable cost of repair, the cost of the damaged part, the proven diminution in value where applicable, or the replacement value if repair is justifiably impossible. An expert or a specialised technical body may be engaged. Amounts may not be combined so as to produce compensation exceeding the proven loss without a legal basis, and the lessor's assessment alone does not bind the renter without a review of the relevant evidence.</p>

<h3>Article 22: Lost or damaged accessories</h3>
<p>The renter is not charged for an accessory unless it is documented in the receipt or the condition report. If an accessory is proven lost or damaged during the renter's possession for a reason that entails their responsibility, the financial effect is assessed on the basis of the reasonable replacement cost or the proven actual loss, according to the nature of the accessory.</p>

<h3>Article 23: Loss and theft</h3>
<p>The renter must notify MHGR as soon as they become aware of the loss or theft of the item, cooperate in providing the appropriate information and official reports where needed, and take no step that harms the possibility of recovering the item or preserving evidence. Reporting theft or loss does not by itself lead to automatic exoneration or liability; responsibility is determined according to the circumstances, the degree of care, the evidence and the applicable laws.</p>

<h3>Article 24: Non-return</h3>
<p>Where the item remains unreturned after its return time with no approved extension or justification being handled, MHGR may, proportionately to the case, suspend the account or rental eligibility, prevent new transactions, continue to require the renter to return the item, preserve the evidence file, activate the agreed protections in accordance with their terms, and take the appropriate legal steps. Non-return is not by itself an automatic criminal characterisation; that characterisation is for the competent authorities. Taking any of these steps is not a waiver of the lessor's right to recover the item or to claim what is legally due.</p>

<h3>Article 25: Cancellation before handover</h3>
<p>The renter may request cancellation of the order before handover, according to the stage of the transaction, their statutory rights and the specific terms shown to them. Where the service has not started and no benefit has been obtained, the financial effect is handled as required by law and the disclosed terms. No cancellation fee that was not disclosed in advance is charged. Where a lawful and applicable cancellation fee is adopted, it is stated in the order summary as an amount or a method of calculation: %Contract.CancellationFee%.</p>

<h3>Article 26: Statutory rights of withdrawal</h3>
<p>The consumer rights set out in the E-Commerce Law and other relevant laws apply where their conditions and exceptions apply to the transaction, and this contract is not to be interpreted so as to forfeit a mandatory right. The effect of rescission or withdrawal is determined by the nature of the transaction, the stage of its performance and whether the renter has begun to benefit from the service or the item, in accordance with the law and the disclosed terms consistent with it.</p>

<h3>Article 27: Delay in handover or performance</h3>
<p>If performance of the service or the handover is delayed in a way that gives the renter a statutory right of rescission or refund, those rights apply in accordance with the law. MHGR will inform the renter of any expected delay or difficulty that materially affects handover or performance where that is required, taking into account any other period the parties have lawfully agreed in the order summary.</p>

<h3>Article 28: Non-conformity or defect on receipt</h3>
<p>If the item differs materially from the description or the condition report, or has a defect that prevents the agreed benefit and of which the renter was unaware, the renter must, where possible, not confirm receipt and must open a review through the approved channel. The appropriate handling — cancellation, replacement, correction or otherwise — is determined by the case and the law, and the renter does not lose the right to report a material difference that was not apparent or documented at receipt where the law preserves that right.</p>

<h3>Article 29: Refunds</h3>
<p>Refunds are made through the approved channels, and the time they appear may differ according to the payment service provider or the bank. The amount of the refund and the basis of any lawful deduction are shown to the renter. Where MHGR adopts an operational period to process a refund request on its side, it is %Contract.RefundDays% working days from the date its requirements are met, not including the processing time at the bank or the payment provider unless that is within MHGR's control.</p>

<h3>Article 30: Complaints, disputes and objections</h3>
<p>MHGR provides an approved channel for submitting complaints, objections and documents, namely: %Store.SupportChannel%. The relevant evidence is preserved and the other party is given the opportunity to respond where needed, and a technical body or expert may be engaged. MHGR will handle the complaint or objection within a period not exceeding %Contract.ComplaintDays% working days from the date the necessary information and documents are complete, unless the nature of the case requires longer, with notice to the parties where appropriate. The internal handling mechanism does not prevent any party from resorting to the body competent under the law.</p>

<h3>Article 31: Rental record and risk management</h3>
<p>MHGR may use the outcomes of previous transactions to manage rental caps and eligibility — such as completed transactions, delays, proven damage, non-return and material disputes — on the appropriate legal basis and following the data-minimisation principle. This does not mean using financial, credit or sensitive data from external sources without a legal basis or appropriate legal arrangements.</p>

<h3>Article 32: Suspending rental eligibility or the account</h3>
<p>MHGR may suspend or restrict the renter's rental eligibility where there is a late item, a case of non-return, an unresolved material claim, a serious fraud indicator, materially incorrect information, an outstanding obligation connected with a previous transaction, or a material breach of this agreement. The step must be proportionate to the risk and the facts, and does not affect the user's statutory rights or their right to object through the approved channel.</p>

<h3>Article 33: Data and privacy</h3>
<p>MHGR processes the personal data necessary to operate the service, verification, payment, handover, return, risk management, claims and disputes and compliance with legal obligations, in accordance with MHGR's general privacy policy and the Personal Data Protection Law and its regulations. Acceptance of this agreement is not a general, unlimited consent to marketing or to any processing unrelated to the purpose or lacking an appropriate legal basis. The provisions of the data protection and privacy annex attached to this agreement apply.</p>

<h3>Article 34: Records, electronic evidence and acceptance</h3>
<p>The system keeps, as applicable, the account identity, the user identifier, a copy of the agreement and its version number, the date and time of acceptance, the order number, the payment and guarantee status, the condition report, the handover and return records, the notifications, claims, correspondence and material events. The renter acknowledges that electronic acceptance and records may have the evidential weight established by law where they meet the relevant legal requirements, without that meaning they are conclusive evidence that cannot be discussed or challenged under the law.</p>

<h3>Article 35: No circumvention outside the platform</h3>
<p>Neither the renter nor the lessor may move or extend a transaction that arose through MHGR outside the platform in order to bypass the fees, the protections or the documentation of the transaction. This does not prevent an independent dealing that does not arise from circumventing an existing transaction or from using information obtained by a party because of MHGR in order to take that same transaction off the platform.</p>

<h3>Article 36: Liability and rights that cannot be waived</h3>
<p>This agreement does not exempt MHGR or the lessor from any liability that may not lawfully be excluded, and does not make the renter absolutely liable for every incident regardless of its cause. Liability is allocated according to the role of each party, the proven facts, the provisions of this agreement, the transaction-specific terms and the applicable laws.</p>

<h3>Article 37: Force majeure and impossibility</h3>
<p>Cases of force majeure or impossibility are handled in accordance with the law and the circumstances. The renter must, so far as possible, safeguard the item, report without delay, and cooperate in returning or delivering it in the manner specified by MHGR or the competent body. The occurrence of force majeure is not to be interpreted so as to extend any party's liability beyond what the law provides.</p>

<h3>Article 38: Notices and updates</h3>
<p>Operational and contractual notices are sent through the application, the mobile number, the email address or any approved channel registered for the renter. Each transaction is governed by the version the renter accepted when confirming it, and a later material amendment does not apply retroactively to an existing transaction without a legal basis or the necessary consent. MHGR keeps a record of versions and of the version attached to each transaction.</p>

<h3>Article 39: Governing law and jurisdiction</h3>
<p>This agreement is governed by the laws of the Kingdom of Saudi Arabia. The parties will seek — without being required to forfeit any statutory right — to settle a dispute amicably through the approved channels within %Contract.SettlementDays% working days of the dispute details being complete. If it is not settled, jurisdiction lies with the judicial, quasi-judicial or other body competent under the law.</p>

<h3>Article 40: Order of precedence</h3>
<p>Where there is a conflict concerning a particular transaction, precedence is given — to the extent the law permits — to mandatory statutory provisions, then to the specific terms of the rental order summary accepted by the renter, then to this agreement, then to its annexes and the related operating procedures, in a manner that does not reduce mandatory consumer rights. Where this agreement refers to the privacy policy, that policy applies to the processing of personal data in accordance with the law and does not change the financial or operational obligations of the order except by clear text.</p>

<h3>Annex 1: Data protection and privacy in the rental relationship</h3>
<p>MHGR processes the renter's personal data to the extent necessary to create the account, verify identity, manage transactions, payment and settlement, handover and return, prevent fraud, manage risk, handle complaints and disputes and comply with legal obligations. MHGR's general privacy policy applies to the wider processing on the platform; this annex is a supplementary contractual statement and does not replace that policy.</p>
<ul>
<li>MHGR collects the minimum data necessary for the stated purpose, and does not treat operational consent as general consent to marketing.</li>
<li>Processing may be carried out by payment, verification, hosting, notification, delivery or inspection service providers, to the extent necessary and in accordance with the legal requirements and appropriate contractual arrangements.</li>
<li>Identity data, personal addresses and full serial numbers are not shown publicly except to the extent necessary and where lawful.</li>
<li>MHGR keeps data for as long as needed for the purpose, the legal obligations or claims, and then handles it in accordance with the approved retention and disposal policy.</li>
<li>The data subject has the rights established by law, including to be informed, to access, to obtain a copy, to correct, complete or update, to destruction in the established cases, and to withdraw consent where processing is based on it, subject to the legal controls and exceptions.</li>
<li>Where operating the service requires transferring personal data outside the Kingdom, that is done in accordance with the legal requirements and the necessary safeguards, and to the minimum extent necessary for the purpose.</li>
<li>Using an identity verification provider does not necessarily mean that MHGR stores biometric data or copies of identity documents; the actual scope of data is determined by the service, the legal basis and the data-minimisation principle.</li>
</ul>

<h3>Annex 2: Item condition report and the handover and return records</h3>
<p>This annex is created electronically for every rental and forms part of this agreement and the related rental order summary. The pre-handover report is not replaced after possession passes; any later correction is recorded as a separate amendment, with the original record preserved and auditable.</p>
<p><strong>First: item and transaction details</strong></p>
<ul>
<li>The order number, the internal item number, the brand, the model, the reference number and the internal serial number where needed.</li>
<li>The reference value of the transaction, the rental period, the inspection location, the person carrying out the inspection, and the date and time the report was created.</li>
<li>The accessories, such as the box, the card, the booklets, extra links, or any other accessory handed over.</li>
</ul>
<p><strong>Second: documentation before handover</strong></p>
<ul>
<li>Mandatory photographs from the angles set for the product category, and a short video where needed.</li>
<li>Documentation of any scratches, defects or traces of previous use, with a close-up photograph and a clear description.</li>
<li>Documentation of the operating condition or the functions that can be tested in practice without harming the item.</li>
<li>Inspection result: ready for handover / needs review / not fit for handover.</li>
</ul>
<p><strong>Third: the renter's acknowledgement of the condition</strong></p>
<p>The system shows the renter the condition summary, the material notes and the available photographs before receipt, and records their agreement to receive the item in the documented condition, without prejudice to their right to object to a material difference that was not apparent or documented at receipt.</p>
<p><strong>Fourth: the handover record</strong></p>
<ul>
<li>Handover does not take place before the requirements of identity, eligibility, acceptance, payment, the required guarantee and the condition report are complete, as applicable.</li>
<li>The handover is documented by a verification code, a QR code or an approved means, recording the identity or identifier of the recipient, the person carrying out the handover, the location, the date and the time.</li>
<li>The renter's possession and responsibility for the item start from the documented actual handover, within the limits of liability set out in this agreement and the law.</li>
</ul>
<p><strong>Fifth: the return report</strong></p>
<ul>
<li>The material inspection angles are repeated, and the item and its accessories are documented on return.</li>
<li>The initial result is: conforming / new difference / needs review.</li>
<li>If a new difference or a missing accessory appears, a review file is opened and the transaction is not closed as sound until the case is handled under Articles 19 (damage and the renter's responsibility), 20 (return inspection and the damage review file) and 21 (assessing damage).</li>
</ul>
<p><strong>Sixth: chain of possession</strong></p>
<p>Where needed, every material movement of possession of the item between the lessor, the handover point, the approved party or the renter is documented, including the date, the time, the party or person carrying it out, and the location or means of verification; this record is linked to the transaction file.</p>
<p><strong>Seventh: the electronic evidence file</strong></p>
<p>The transaction file gathers, as the case may be, this agreement and its version, the order summary, the condition report before and after handover, proof of payment, guarantee, handover and return, the chain of possession, the notifications and contact attempts, the relevant correspondence, and any technical assessment, objection or review outcome, so that the transaction can be reviewed and audited.</p>

<h3>The renter's acknowledgement and acceptance</h3>
<p>By accepting this agreement electronically, the renter acknowledges that they were given the opportunity to read and understand its terms, that the information they provided is correct to the best of their knowledge, and that they agree to be bound by this agreement and by the specific terms of the rental order concerned, in accordance with the laws in force in the Kingdom of Saudi Arabia. The renter further acknowledges that the electronic acceptance is documented and linked to the user identifier, the transaction number, the agreement version number, the date and time of acceptance and the means of approval.</p>
<table style="width:100%;border-collapse:collapse;font-size:14px">
<thead><tr>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold;width:38%">Acceptance record</th>
<th style="border:1px solid #d9d2c5;padding:8px;text-align:left;background-color:#f5f2ec;font-weight:bold">Value</th>
</tr></thead>
<tbody>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Renter</td><td style="border:1px solid #d9d2c5;padding:8px">%Customer.FullName%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">User identifier</td><td style="border:1px solid #d9d2c5;padding:8px">%Customer.Id%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Transaction number</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.Number%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Agreement version</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.Version%</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Date and time of acceptance</td><td style="border:1px solid #d9d2c5;padding:8px">Recorded electronically at the moment of signing, in the electronic record of the transaction</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Means of electronic approval</td><td style="border:1px solid #d9d2c5;padding:8px">Electronic signature through the MHGR platform, linked to the renter's account and IP address</td></tr>
<tr><td style="border:1px solid #d9d2c5;padding:8px">Verification reference / result</td><td style="border:1px solid #d9d2c5;padding:8px">%Contract.VerificationRef%</td></tr>
</tbody>
</table>

<h3>MHGR approval / electronic record</h3>
<p>MHGR approves this agreement electronically within the electronic record of transaction %Contract.Number%, which keeps the dates of sending and approval and the identifier of the approving employee. The platform operator is represented by: %Store.LegalName% — commercial registration no. %Store.CommercialRegistration%.</p>
<p style="margin-top:24px;color:#666">Signed electronically through the %Store.Name% platform.</p>
</div>
""";
}
