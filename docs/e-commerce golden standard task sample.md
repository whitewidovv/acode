# Task 044: Subscription / Recurring Orders ("Candle Club")

**Priority:** 44 / 63
**Tier:** A (High Value)
**Complexity:** 13 Fibonacci points
**Phase:** Phase 13 - Next Priority Features
**Dependencies:** Task 002 (Domain Models), Task 015 (Checkout & Payment), Task 025 (Email Marketing)

---

## Description

The Subscription/Recurring Orders feature (branded as "Candle Club") implements a monthly recurring revenue model that transforms one-time candle buyers into loyal subscribers with predictable delivery schedules and automated billing. This feature enables customers to receive curated candles automatically each month at discounted subscription prices, while the business gains recurring revenue streams, improved cash flow forecasting, and significantly higher customer lifetime value (3.2× vs one-time purchasers). The subscription model offers tiered plans (Bronze, Silver, Gold) with varying quantities and pricing, customizable billing frequencies (monthly, bi-monthly, quarterly), scent preference management, and full self-service controls (pause, skip month, cancel anytime).

From a business perspective, subscriptions represent the highest-value feature in the Phase 13 roadmap: they generate **$88,000 annual recurring revenue (ARR)** at modest adoption (8% of customer base subscribes to average Silver tier at $50/month: 400 customers × 0.08 × $50 × 12 = $19,200 in first year, scaling to $88k by year 3 as subscriber base grows to 147 members). Subscription revenue is 5× more predictable than one-time sales (churn rate 8-12% monthly vs 95% customer drop-off after single purchase), reducing cash flow volatility and enabling confident inventory planning. Additionally, subscribers have 67% higher total lifetime value ($684 vs $213 for one-time buyers) due to extended retention periods (average 18 months vs 3 months for occasional purchasers) and higher monthly spend ($50/month subscription + $23/month additional purchases vs $71 total for one-time customers).

Technically, the feature integrates deeply with Stripe Subscriptions API for automated recurring billing, payment method management, invoice generation, and failed payment retry logic. The implementation includes Subscription domain entity (with PlanId, BillingFrequency, NextBillingDate, Status, PaymentMethodId), SubscriptionPlan entity (defining tier pricing and product quantities), background job for daily billing processing (Hangfire scheduler running at 2:00 AM UTC), webhook handlers for Stripe events (subscription.updated, invoice.payment_succeeded, invoice.payment_failed), and customer-facing self-service portal built with Blazor components. Admin features include subscription management dashboard, fulfillment queue generation (creating shipping orders for billed subscriptions), churn analysis reporting (cohort retention curves, MRR trends), and dunning email workflows (payment failed notifications with retry countdown).

The subscription lifecycle follows this flow: (1) Customer selects plan and billing frequency → (2) Enters payment method (stored as Stripe PaymentMethod) → (3) First invoice charged immediately → (4) Subscription created with NextBillingDate = today + frequency → (5) Daily billing job identifies subscriptions due for billing → (6) Stripe invoice created and charged → (7) On success: Order created, fulfillment queue updated, NextBillingDate advanced → (8) On failure: Payment retry scheduled (3 attempts over 7 days), dunning email sent → (9) After 3 failures: Subscription auto-canceled, customer notified.

Key constraints include minimum subscription commitment of 2 billing cycles before cancellation (prevents trial abuse), maximum 3 active subscriptions per customer (prevents accidental duplicates), Stripe payment retry schedule (day 3, day 5, day 7 after initial failure), scent preference limitations (customers select 3 favorite scents to influence product curation, but cannot guarantee specific products), and subscription pricing discount of 15-25% vs retail (Bronze: 15%, Silver: 20%, Gold: 25% discount to incentivize higher tiers).

---

## Use Cases

### Use Case 1: Sarah (Store Owner) Launches Candle Club Subscription Program

**Scenario:** Sarah wants to create predictable recurring revenue to smooth out seasonal sales fluctuations and improve inventory planning.

**Without This Feature:**
Sarah relies entirely on one-time purchases, which fluctuate wildly: November-December average $28,000/month (holiday season), January-March drop to $8,500/month (post-holiday slump). This volatility makes cash flow planning difficult - she has to maintain large cash reserves for slow months, can't confidently commit to inventory orders 3+ months ahead, and struggles to justify business loans due to inconsistent revenue. 92% of customers make only one purchase and never return, resulting in constant need for expensive customer acquisition (Facebook ads at $45 CAC). Total annual revenue: $186,000 with high variance.

**With This Feature:**
Sarah creates three subscription tiers in the admin panel: Bronze ($35/month, 1 candle), Silver ($50/month, 2 candles), Gold ($75/month, 3 candles + exclusive scent). She promotes the Candle Club via email campaign to 5,000 existing customers, offering 20% launch discount for first 3 months. Within 2 weeks, 147 customers subscribe (2.9% conversion): 52 Bronze, 71 Silver, 24 Gold. Immediate impact: **$7,350 monthly recurring revenue (MRR)**, providing baseline cash flow floor even in slow months. Over 12 months, subscription revenue totals $88,200 (MRR grows from $7,350 to $9,400 as subscribers accumulate), representing 32% of total revenue. Cash flow volatility decreases by 47% (standard deviation drops from $6,800 to $3,600), enabling confident 4-month inventory commitments. Customer lifetime value for subscribers: $684 (18-month average retention) vs $213 for one-time buyers.

**Outcome:**
- **Recurring Revenue:** $88,200 annual subscription revenue (32% of total revenue)
- **Cash Flow Stability:** 47% reduction in monthly revenue volatility
- **Higher LTV:** Subscribers worth 3.2× more than one-time customers ($684 vs $213)
- **Inventory Planning:** Predictable subscription volume enables 4-month advance orders
- **Reduced CAC:** Subscriber retention (88% monthly) far exceeds one-time customer retention (5%)

### Use Case 2: Alex (Customer) Subscribes to Silver Tier and Manages Subscription

**Scenario:** Alex buys candles monthly but often forgets to reorder, running out for 2-3 weeks between purchases. She wants automatic deliveries and a discount.

**Without This Feature:**
Alex manually places orders when she remembers (approximately every 35-40 days, not consistently monthly). She pays full retail price ($24.99 per candle), spends 15 minutes per order browsing and selecting products, and occasionally orders duplicate scents by mistake (forgets what she already has). Over 12 months, she purchases 26 candles at $24.99 each = $649.74 total spend. She runs out of candles 4-5 times per year, buying inferior grocery store alternatives for $12 each ($48-60 additional spend on non-store products). No loyalty rewards, no automatic deliveries, no scent curation based on preferences.

**With This Feature:**
Alex subscribes to Silver tier ($50/month for 2 candles, 20% discount vs $49.98 retail). During signup, she selects scent preferences: Lavender, Vanilla, Citrus (top 3 favorites). Every month on the 15th, her subscription auto-renews: Stripe charges her card, an order is automatically created with 2 candles matching her preferences (algorithm selects from new releases + seasonal favorites + previous month variety), and shipping label generated. She receives email notification: "Your Candle Club shipment is on the way! This month: Lavender Honey & Orange Blossom". In June and July (summer months when she uses fewer candles), she pauses her subscription via self-service portal. Resumes in August. In December, she needs extra candles for gifts, so she adds one-time purchase on top of subscription. Total annual spend: $500 subscription (10 months at $50, 2 months paused) + $75 gift purchases = $575 (vs $649.74 without subscription). Additional value: Never runs out of candles (consistent monthly delivery), never buys inferior alternatives, saves 11% annually, zero time spent browsing and reordering.

**Outcome:**
- **Cost Savings:** $74.74 saved annually (11% discount) despite slightly reduced candle consumption
- **Convenience:** Zero time spent reordering; automatic monthly delivery
- **Never Out of Stock:** Consistent supply eliminates 4-5 periods without candles
- **Personalization:** Scent preferences ensure she receives preferred fragrances
- **Flexibility:** Self-service pause for summer months when usage drops

### Use Case 3: Admin (Jordan) Processes Daily Subscription Fulfillment

**Scenario:** 147 active subscriptions require daily billing, order creation, and fulfillment queue management.

**Without This Feature:**
Jordan manually processes recurring orders using a spreadsheet: checks which customers are due for billing, manually creates invoice in Stripe, waits for payment confirmation, creates order in admin panel, adds to shipping queue. This process takes 3-4 hours daily (average 6 minutes per subscription × 40 subscriptions billed per day). Errors occur frequently: forgetting to bill a customer (revenue loss), billing a customer who canceled (refund + dissatisfaction), creating duplicate orders (inventory waste). No automated payment retry for failed charges - Jordan must manually contact customers with declined cards, resulting in 23% churn from failed payments (customers forget to update card and subscription lapses).

**With This Feature:**
At 2:00 AM UTC daily, automated billing job runs: queries all subscriptions where NextBillingDate = today, creates Stripe invoice for each, attempts charge. For successful charges (91% success rate): Order automatically created with subscription preferences, added to fulfillment queue, NextBillingDate advanced by 1 month, customer receives "Shipment on the way" email. For failed charges (9% failure rate): Payment retry scheduled (day 3, day 5, day 7), dunning email sent immediately ("Payment failed - update your card to avoid disruption"), subscription status set to PastDue. Jordan reviews fulfillment queue at 9:00 AM, sees 37 new subscription orders ready to ship (all prep work automated). At 10:00 AM, Jordan prints shipping labels in bulk, packs orders with subscription thank-you note. Jordan spends 45 minutes on subscription fulfillment (vs 3-4 hours manual processing), allowing time for high-value tasks (customer service, product curation). Failed payment recovery rate: 68% (automated retries + dunning emails recover 2/3 of failed charges), reducing churn from 23% to 3% for payment issues.

**Outcome:**
- **Time Savings:** 2.5-3 hours daily saved on subscription processing (85% reduction)
- **Error Elimination:** Zero manual billing errors (duplicate charges, missed billings, wrong amounts)
- **Churn Reduction:** Failed payment churn drops from 23% to 3% (automated retry + dunning)
- **Revenue Recovery:** 68% of failed payments recovered via automated retry workflow
- **Scalability:** System handles 500+ subscriptions with same 45-minute fulfillment time

---

## User Manual Documentation

### Overview

The Candle Club Subscription feature enables customers to receive curated candles automatically each month with discounted pricing and flexible management options. Subscriptions eliminate the need for manual reordering, provide consistent product delivery, and offer 15-25% savings vs retail prices. Store owners benefit from predictable recurring revenue, improved cash flow forecasting, and significantly higher customer lifetime value.

**Subscription Tiers:**

| Tier | Monthly Price | Candles Per Month | Discount vs Retail | Best For |
|------|---------------|-------------------|-------------------|----------|
| Bronze | $35 | 1 candle | 15% ($41.65 value) | Light users, testing subscription |
| Silver | $50 | 2 candles | 20% ($62.48 value) | Regular candle users |
| Gold | $75 | 3 candles + exclusive scent | 25% ($99.96 value) | Heavy users, enthusiasts |

**Billing Frequencies:**
- **Monthly:** Charged on same date each month (e.g., subscribe on 15th = billed 15th every month)
- **Bi-Monthly:** Charged every 2 months (e.g., Jan 15, Mar 15, May 15, etc.)
- **Quarterly:** Charged every 3 months (e.g., Jan 15, Apr 15, Jul 15, Oct 15)

**Key Features:**
- Automatic recurring billing via Stripe
- Scent preference management (select 3 favorite fragrance types)
- Self-service pause/resume (pause up to 3 months, resume anytime)
- Skip single month (useful for vacations or gift month timing)
- Cancel anytime (after 2 billing cycles minimum)
- Payment method management (update card without canceling)
- Subscription gifting (purchase subscription for someone else)
- Tier upgrade/downgrade (change plans anytime, prorated billing)

### Step-by-Step Instructions for Customers

#### Step 1: Subscribe to Candle Club

**Navigate to Subscription Page:**
```
https://candlestore.com/candle-club
```

**Select Tier and Billing Frequency:**
```
┌──────────────────────────────────────────────────────┐
│  Choose Your Candle Club Tier                        │
├──────────────────────────────────────────────────────┤
│                                                       │
│  ○ Bronze - $35/month                                │
│     1 candle delivered monthly                       │
│     Save 15% vs retail                               │
│                                                       │
│  ● Silver - $50/month   [MOST POPULAR]              │
│     2 candles delivered monthly                      │
│     Save 20% vs retail                               │
│                                                       │
│  ○ Gold - $75/month                                  │
│     3 candles + 1 exclusive scent monthly            │
│     Save 25% vs retail                               │
│                                                       │
│  Billing Frequency: [Monthly ▼]                     │
│     □ Monthly  □ Bi-Monthly  □ Quarterly             │
│                                                       │
│  [Continue to Preferences →]                         │
└──────────────────────────────────────────────────────┘
```

**Select Scent Preferences (Step 2):**
```
┌──────────────────────────────────────────────────────┐
│  Select Your Top 3 Scent Preferences                 │
├──────────────────────────────────────────────────────┤
│                                                       │
│  We'll curate your monthly candles based on          │
│  these preferences (new releases + favorites)        │
│                                                       │
│  ☑ Lavender                                          │
│  ☑ Vanilla                                           │
│  □ Citrus                                            │
│  ☑ Floral                                            │
│  □ Woodsy                                            │
│  □ Spice                                             │
│  □ Fresh                                             │
│  □ Fruity                                            │
│                                                       │
│  Note: 3 of 3 selected                               │
│                                                       │
│  [← Back]  [Continue to Payment →]                   │
└──────────────────────────────────────────────────────┘
```

**Enter Payment Information (Step 3):**
```
┌──────────────────────────────────────────────────────┐
│  Payment Information                                  │
├──────────────────────────────────────────────────────┤
│                                                       │
│  Card Number:  [4242 4242 4242 4242]                │
│  Expiry:       [12] / [25]                           │
│  CVC:          [123]                                 │
│                                                       │
│  Billing Address: [Use shipping address ✓]          │
│                                                       │
│  ☑ I agree to Terms of Service                      │
│  ☑ Authorize recurring charges                      │
│                                                       │
│  First charge today: $50.00                          │
│  Next charge: Feb 15, 2025 ($50.00)                 │
│                                                       │
│  [← Back]  [Subscribe Now →]                         │
└──────────────────────────────────────────────────────┘
```

**Confirmation:**
After clicking "Subscribe Now", you'll receive:
- Email confirmation with subscription details
- First shipment notification (ships within 2 business days)
- Link to manage subscription in account dashboard

#### Step 2: Manage Subscription

**Access Subscription Dashboard:**
Navigate to Account → My Subscriptions or `https://candlestore.com/account/subscriptions`

```
┌──────────────────────────────────────────────────────┐
│  My Subscriptions                                     │
├──────────────────────────────────────────────────────┤
│                                                       │
│  ┌────────────────────────────────────────────┐     │
│  │  Silver Tier - Active                       │     │
│  │  $50.00 / month                             │     │
│  │                                              │     │
│  │  Next Billing: Feb 15, 2025                 │     │
│  │  Next Amount: $50.00                        │     │
│  │                                              │     │
│  │  Scent Preferences:                         │     │
│  │  • Lavender  • Vanilla  • Floral            │     │
│  │                                              │     │
│  │  Payment Method: Visa ****4242               │     │
│  │  Status: Active ✓                           │     │
│  │                                              │     │
│  │  [Edit Preferences]  [Update Payment]       │     │
│  │  [Pause Subscription]  [Change Tier]        │     │
│  │  [Cancel Subscription]                      │     │
│  └────────────────────────────────────────────┘     │
│                                                       │
│  Upcoming Deliveries:                                 │
│  • Feb 15: 2 candles (Lavender Honey, Vanilla Bean) │
│  • Mar 15: 2 candles (To be curated)                │
│                                                       │
│  Past Deliveries:                                     │
│  • Jan 15: Orange Blossom, Floral Medley            │
│  • Dec 15: Winter Spice, Evergreen Forest           │
│                                                       │
│  [Skip Next Month]  [View Billing History]          │
└──────────────────────────────────────────────────────┘
```

**Pause Subscription:**
1. Click "Pause Subscription" button
2. Select resume date (or "Resume manually")
3. Confirm pause
4. **Result:** No charges or shipments during pause period; NextBillingDate updated to resume date

**Skip Single Month:**
1. Click "Skip Next Month" button
2. Confirm skip (e.g., "Skip February delivery?")
3. **Result:** February skipped, next billing moves to March 15

**Update Payment Method:**
1. Click "Update Payment" button
2. Enter new card details (processed via Stripe Elements)
3. Save changes
4. **Result:** Future charges use new payment method; no interruption to subscription

**Change Tier (Upgrade/Downgrade):**
1. Click "Change Tier" button
2. Select new tier (Bronze, Silver, or Gold)
3. **Result:** Prorated credit/charge applied immediately; next billing at new tier price

**Cancel Subscription:**
1. Click "Cancel Subscription" button
2. Select cancellation reason (optional feedback survey)
3. Confirm cancellation
4. **Result:** Final shipment sent, no future charges, can resubscribe anytime

#### Step 3: Admin - Create Subscription Plans

**Admin Panel Navigation:**
Admin → Subscriptions → Plans → Create New Plan

```
┌──────────────────────────────────────────────────────┐
│  Create Subscription Plan                             │
├──────────────────────────────────────────────────────┤
│                                                       │
│  Plan Name: [Silver Tier]                            │
│  Description: [2 candles per month, curated...]      │
│                                                       │
│  Pricing:                                             │
│    Monthly Price: [$50.00]                           │
│    Retail Value:  [$62.48]  (auto-calculated)        │
│    Discount:      [20%]     (auto-calculated)        │
│                                                       │
│  Products:                                            │
│    Candles Per Delivery: [2]                         │
│    Include Exclusive Scent: [No ▼]                   │
│                                                       │
│  Stripe Integration:                                  │
│    Stripe Price ID: [price_abc123...]                │
│    [Sync with Stripe]                                │
│                                                       │
│  Availability:                                        │
│    ☑ Active (visible to customers)                   │
│    ☐ Featured (show as "Most Popular")               │
│                                                       │
│  [Cancel]  [Create Plan]                             │
└──────────────────────────────────────────────────────┘
```

#### Step 4: Admin - Process Subscription Fulfillment Queue

**Automated Daily Workflow:**
At 2:00 AM UTC, billing job runs automatically (no manual intervention). At 9:00 AM, admin checks fulfillment queue:

Admin → Subscriptions → Fulfillment Queue → Today

```
┌──────────────────────────────────────────────────────┐
│  Subscription Fulfillment Queue - Jan 15, 2025       │
├──────────────────────────────────────────────────────┤
│                                                       │
│  37 orders ready to ship                              │
│                                                       │
│  ☑ Select All    [Print All Labels]  [Export CSV]   │
│                                                       │
│  ┌────────────────────────────────────────────┐     │
│  │ ☑  Order #ORD-20250115-SUB001               │     │
│  │    Customer: Alex Johnson                   │     │
│  │    Tier: Silver (2 candles)                 │     │
│  │    Scents: Lavender Honey, Vanilla Bean     │     │
│  │    [Print Label]  [View Details]            │     │
│  └────────────────────────────────────────────┘     │
│                                                       │
│  ┌────────────────────────────────────────────┐     │
│  │ ☑  Order #ORD-20250115-SUB002               │     │
│  │    Customer: Maria Garcia                   │     │
│  │    Tier: Gold (3 candles + exclusive)       │     │
│  │    Scents: Citrus, Floral, Woodsy, Exclusive│     │
│  │    [Print Label]  [View Details]            │     │
│  └────────────────────────────────────────────┘     │
│                                                       │
│  [... 35 more orders ...]                            │
│                                                       │
│  [Mark All as Shipped]                               │
└──────────────────────────────────────────────────────┘
```

**Bulk Operations:**
1. Select all orders (or filter by tier)
2. Click "Print All Labels" → Generates PDF with 37 shipping labels
3. Pack orders with subscription thank-you cards
4. Affix labels and ship
5. Click "Mark All as Shipped" → Triggers tracking emails to customers

### Configuration / Settings

**Admin Subscription Settings:**

Admin → Settings → Subscriptions

```
Billing Job Schedule: [Daily at 2:00 AM UTC]
Payment Retry Schedule: [Day 3, Day 5, Day 7]
Maximum Retries: [3 attempts]
Auto-Cancel After Failed Retries: [Yes ▼]

Minimum Commitment: [2 billing cycles]
Maximum Active Subscriptions Per Customer: [3]
Allow Pause Duration: [Up to 3 months]

Scent Curation Algorithm: [Preferences + New Releases + Variety ▼]
Exclusive Scent Frequency (Gold Tier): [Every month]

Dunning Email Template: [Select Template ▼]
  • Payment Failed - Immediate
  • Payment Retry Reminder (Day 2)
  • Final Notice Before Cancellation (Day 6)

Subscription Discount Rates:
  Bronze: [15%]
  Silver: [20%]
  Gold:   [25%]

Stripe Webhook Endpoint: [https://candlestore.com/api/webhooks/stripe]
Webhook Secret: [whsec_...] (hidden)
```

### Integration with Other Systems

**With Task 015 (Checkout & Payment):**
- Uses Stripe Subscriptions API for recurring billing
- Shares PaymentMethod storage and card validation logic
- Subscription signup reuses checkout address collection flow

**With Task 025 (Email Marketing):**
- Subscription confirmation emails sent via EmailService
- Dunning emails (payment failed) trigger automated workflow
- Monthly "Shipment on the way" notifications

**With Task 016 (Order Management):**
- Each successful subscription billing creates Order entity
- Orders marked with IsSubscription flag for tracking
- Subscription orders added to standard fulfillment queue

**With Task 042 (Analytics & Reporting):**
- MRR (Monthly Recurring Revenue) dashboard
- Churn rate analysis (cohort retention curves)
- Subscription tier distribution pie chart
- LTV comparison: subscribers vs one-time buyers

### Best Practices

1. **Set Realistic Discount Levels:** Offer 15-25% subscription discount vs retail to incentivize commitment while maintaining margin. Discounts < 10% don't drive conversions; discounts > 30% erode profitability.

2. **Automate Everything:** Manual subscription billing is error-prone and doesn't scale beyond 20-30 subscribers. Use automated billing job, payment retry, and dunning emails to handle 500+ subscriptions with minimal admin time.

3. **Communicate Billing Dates Clearly:** Customer confusion about billing dates causes support tickets. Send email reminder 3 days before billing: "Your Candle Club renewal is coming up on Feb 15 ($50)".

4. **Make Cancellation Easy:** Don't hide the cancel button or require phone calls. Self-service cancellation (with optional feedback survey) builds trust and encourages resubscription later. 37% of canceled subscribers return within 6 months.

5. **Retry Failed Payments Aggressively:** 68% of failed payments succeed on retry (expired cards auto-renewed by banks, temporary holds cleared, customer updates card). Use 3 retry attempts over 7 days before canceling.

6. **Curate with Variety:** Don't send the same products every month. Mix customer preferences (60%) + new releases (25%) + surprise variety (15%) to keep subscriptions exciting and reduce boredom-driven churn.

7. **Offer Pause Instead of Cancel:** When customers attempt cancellation, offer 1-3 month pause as alternative. 52% choose pause over cancel, and 83% of pausers resume (vs 37% resubscribe after canceling).

### Troubleshooting

**Problem:** Customer's subscription billing failed with "Card declined" error.

**Solution:** Automated payment retry will attempt charge on Day 3, Day 5, and Day 7. Customer receives dunning email immediately with link to update payment method. If all 3 retries fail, subscription auto-cancels on Day 8. Customer can reactivate by updating card and clicking "Reactivate Subscription" button. Admin can manually retry payment via: Admin → Subscriptions → [Customer Name] → "Manual Retry Payment".

---

**Problem:** Customer complains they received duplicate scents 2 months in a row.

**Solution:** Scent curation algorithm prioritizes variety but occasionally repeats scents if limited inventory or narrow preferences. Admin can manually adjust next shipment: Admin → Subscriptions → [Customer] → Edit Next Shipment → Select specific products → Save. For systemic fix, increase "minimum months between repeat scents" setting from 2 to 3 months in Admin → Settings → Subscriptions → Curation Settings.

---

**Problem:** Subscription billing job didn't run today - no orders created.

**Solution:** Check Hangfire dashboard for job failures: Admin → System → Background Jobs → Scheduled. If job shows "Failed" status, view error log. Common causes:
1. **Stripe API downtime:** Job will auto-retry in 1 hour
2. **Database connection lost:** Restart API server: `systemctl restart candlestore-api`
3. **Job disabled by accident:** Re-enable: Hangfire → Recurring Jobs → "SubscriptionBillingJob" → Trigger Now

Manually trigger job: Admin → Subscriptions → Billing → "Run Manual Billing" (processes all overdue subscriptions).

---

**Problem:** MRR dashboard shows $0 even though 50 subscriptions are active.

**Solution:** MRR calculation caches overnight. Force refresh: Admin → Analytics → Subscriptions → "Refresh MRR Cache" button. If still $0, check database query: Run SQL: `SELECT SUM(MonthlyPrice) FROM Subscriptions WHERE Status = 'Active'`. If result > 0 but dashboard = 0, clear Redis cache: `redis-cli FLUSHDB` and refresh page.

---

**Problem:** Customer's subscription shows "Past Due" status but they claim payment method works.

**Solution:** Check Stripe Dashboard for declined payment reason: Stripe.com → Payments → Search customer email → View declined payment. Common reasons:
1. **Insufficient funds:** Customer needs to add funds to bank account
2. **Card expired:** Customer must update expiry date
3. **Bank fraud alert:** Customer must contact bank to approve charge
4. **International card declined:** Enable international payments in Stripe settings

Customer can update payment method immediately in self-service portal; subscription will auto-retry within 24 hours.

---

## Acceptance Criteria / Definition of Done

### Core Functionality
- [ ] Subscription entity exists with fields: Id, CustomerId, SubscriptionPlanId, StripeSub scriptionId, BillingFrequency (Monthly/BiMonthly/Quarterly), NextBillingDate, Status (Active/Paused/Canceled/PastDue), PaymentMethodId, ScentPreferences (JSON array), CreatedAt, CanceledAt
- [ ] SubscriptionPlan entity exists with fields: Id, Name, Description, MonthlyPrice, CandlesPerDelivery, IncludesExclusiveScent, DiscountPercent, StripePriceId, IsActive, DisplayOrder
- [ ] Customer can subscribe to plan by selecting tier and billing frequency
- [ ] Customer enters payment method via Stripe Elements (card stored as PaymentMethod in Stripe)
- [ ] First invoice charged immediately on subscription creation
- [ ] Subscription created in database with Status = Active, NextBillingDate = today + frequency interval
- [ ] Customer redirected to subscription confirmation page with details
- [ ] Confirmation email sent with subscription summary and manage link
- [ ] Daily billing job runs at 2:00 AM UTC processing all subscriptions where NextBillingDate <= today
- [ ] Billing job creates Stripe Invoice for each due subscription, attempts charge
- [ ] On successful charge: Order created, NextBillingDate advanced by frequency interval, "Shipment on way" email sent
- [ ] On failed charge: Subscription status set to PastDue, payment retry scheduled, dunning email sent
- [ ] Payment retry attempts occur on Day 3, Day 5, Day 7 after initial failure
- [ ] After 3 failed retries, subscription auto-canceled and customer notified
- [ ] Customer can pause subscription for 1-3 months via self-service portal
- [ ] Paused subscription shows Status = Paused, billing skipped until resume date
- [ ] Customer can resume paused subscription anytime or automatically on resume date
- [ ] Customer can skip single upcoming billing cycle (next month only)
- [ ] Customer can update payment method without interrupting subscription
- [ ] Customer can change scent preferences anytime (applies to next shipment)
- [ ] Customer can upgrade/downgrade tier with prorated billing adjustment
- [ ] Customer can cancel subscription (requires confirmation)
- [ ] Canceled subscription completes current billing cycle then stops (no refund for current period)
- [ ] Stripe webhook handles subscription.updated, invoice.payment_succeeded, invoice.payment_failed events
- [ ] Admin dashboard shows subscription list with filters (Status, Tier, Billing Date)
- [ ] Admin can view individual subscription details (history, upcoming shipments, payment method)
- [ ] Admin can manually retry failed payment
- [ ] Admin can manually cancel subscription with reason note
- [ ] Fulfillment queue shows daily subscription orders ready to ship
- [ ] Scent preference algorithm curates products based on customer selections (60% match preferences, 25% new releases, 15% variety)

### Business Rules Validation
- [ ] Minimum commitment of 2 billing cycles enforced (customer cannot cancel before 2 charges)
- [ ] Maximum 3 active subscriptions per customer (prevents accidental duplicates)
- [ ] Scent preferences limited to 3 selections (enforced during signup and updates)
- [ ] Subscription discount applied automatically (Bronze 15%, Silver 20%, Gold 25%)
- [ ] Billing frequency validated (only Monthly, BiMonthly, Quarterly allowed)
- [ ] NextBillingDate calculated correctly based on frequency (monthly = +1 month, bimonthly = +2 months, quarterly = +3 months)
- [ ] Paused subscription cannot be paused again (must resume first)
- [ ] Canceled subscription cannot be modified (customer must create new subscription)
- [ ] PastDue subscription blocks new orders until payment resolved

### Payment and Billing
- [ ] Stripe Subscription created with correct price, billing interval, payment method
- [ ] Stripe Customer created if not exists (or linked to existing Stripe Customer ID)
- [ ] Payment method saved in Stripe and linked to subscription
- [ ] Invoice created in Stripe for each billing attempt
- [ ] Invoice status synced: Paid → Order created, Failed → Retry scheduled
- [ ] Prorated amount calculated correctly for tier changes (charge difference immediately or credit to next invoice)
- [ ] Billing history visible to customer (all invoices with dates, amounts, statuses)
- [ ] Failed payment reason displayed to customer (card declined, insufficient funds, expired card)
- [ ] Payment retry countdown shown ("Next retry in 2 days")
- [ ] Dunning email sent immediately on failed payment with update payment link

### Subscription Management UI
- [ ] Customer subscription dashboard shows: Current tier, next billing date, next amount, scent preferences, payment method (last 4 digits), status
- [ ] "Pause Subscription" button opens modal with resume date selector (1-3 months or manual)
- [ ] "Skip Next Month" button confirms skip and updates next billing date display
- [ ] "Update Payment" button opens Stripe Elements card input form
- [ ] "Change Tier" button shows tier options with pricing comparison
- [ ] "Edit Preferences" button allows selecting 3 scent preferences with visual icons
- [ ] "Cancel Subscription" button shows cancellation survey (optional) and confirms termination
- [ ] Upcoming deliveries section shows next 2 shipments with product names (if curated) or "To be determined"
- [ ] Past deliveries section shows last 6 shipments with products delivered
- [ ] "View Billing History" link opens invoice list with download PDF option

### Email Notifications
- [ ] Subscription confirmation email sent immediately after signup with tier details, first shipment ETA
- [ ] Monthly "Shipment on the way" email sent when order created (includes product names, tracking link when available)
- [ ] Billing reminder email sent 3 days before next charge ("Your subscription renews in 3 days - $50 will be charged")
- [ ] Dunning email sent immediately on failed payment ("Payment failed - update your card")
- [ ] Payment retry reminder sent 1 day before each retry attempt ("Retry #2 scheduled tomorrow")
- [ ] Subscription cancellation confirmation email sent with final shipment details
- [ ] Pause confirmation email sent with resume date
- [ ] Resume notification email sent when paused subscription reactivates

### Admin Features
- [ ] Admin subscription list shows all subscriptions with columns: Customer, Tier, Status, Next Billing, MRR, Created Date
- [ ] Filters available: Status (Active/Paused/Canceled/PastDue), Tier (Bronze/Silver/Gold), Billing Frequency
- [ ] Search by customer name or email
- [ ] Sort by: Next Billing Date, MRR, Created Date, Customer Name
- [ ] Click subscription to view details: customer info, payment history, shipment history, scent preferences
- [ ] "Manual Retry Payment" button attempts immediate Stripe charge
- [ ] "Cancel Subscription" button with reason field (customer request, fraud, product unavailable)
- [ ] Fulfillment queue page shows today's subscription orders with product curation, shipping addresses
- [ ] "Print All Labels" button generates bulk PDF with shipping labels
- [ ] "Mark as Shipped" updates order status and triggers tracking email
- [ ] Subscription plan management: Create, Edit, Deactivate plans
- [ ] Subscription analytics dashboard: MRR chart, churn rate graph, tier distribution pie chart, cohort retention table

### Analytics and Reporting
- [ ] MRR (Monthly Recurring Revenue) calculated as SUM(MonthlyPrice) for all Active subscriptions
- [ ] MRR trend chart shows 12-month history with growth rate percentage
- [ ] Churn rate calculated monthly: (Cancellations + PastDue Auto-Cancels) / Active Subscriptions Start of Month
- [ ] Cohort retention table shows Month 1-12 retention for signup cohorts
- [ ] LTV comparison metric: Average revenue per subscriber vs one-time customer
- [ ] Subscription tier distribution pie chart (Bronze %, Silver %, Gold %)
- [ ] Failed payment recovery rate: Successful retries / Total failed payments
- [ ] Dunning effectiveness metric: Reactivations after dunning email / Dunning emails sent

### Performance
- [ ] Billing job processes 1000 subscriptions in < 10 minutes
- [ ] Single subscription billing (Stripe invoice creation + charge) completes in < 3 seconds
- [ ] Subscription dashboard loads in < 500ms with 50 subscriptions
- [ ] Admin fulfillment queue loads in < 1 second with 200 pending orders
- [ ] MRR dashboard loads in < 2 seconds (uses Redis cache with 1-hour TTL)
- [ ] Scent curation algorithm selects products in < 200ms per subscription

### Data Persistence
- [ ] Subscriptions table with indexes on: CustomerId, Status, NextBillingDate, SubscriptionPlanId
- [ ] SubscriptionPlans table with index on IsActive
- [ ] SubscriptionBillingHistory table tracks all billing attempts (date, amount, status, invoice ID)
- [ ] Foreign key: Subscription.CustomerId → Customers.Id (ON DELETE CASCADE)
- [ ] Foreign key: Subscription.SubscriptionPlanId → SubscriptionPlans.Id (ON DELETE RESTRICT - prevent deleting active plans)
- [ ] Subscription soft delete (IsDeleted flag) for canceled subscriptions (retain history)
- [ ] Billing history never deleted (permanent audit trail)

### Edge Cases
- [ ] Customer attempts to subscribe to same tier twice - error message: "You already have an active subscription to this tier"
- [ ] Customer attempts to cancel before 2 billing cycles - error message: "Minimum commitment is 2 months (X days remaining)"
- [ ] Customer updates payment method during billing attempt - new method used for in-progress charge
- [ ] Billing job encounters Stripe API error - job retries automatically in 1 hour, logs error for admin review
- [ ] Customer downgrades tier mid-cycle - prorated credit applied to next invoice
- [ ] Customer upgrades tier mid-cycle - prorated charge processed immediately
- [ ] Subscription billing lands on invalid date (e.g., Feb 31) - bills on last day of month (Feb 28/29)
- [ ] Customer pauses subscription with billing due today - pause takes effect after today's charge
- [ ] Product in scent preference goes out of stock - algorithm selects alternative from same scent family

### Security
- [ ] Subscription endpoints require authentication (JWT token)
- [ ] Customer can only view/modify their own subscriptions (authorization check)
- [ ] Stripe webhook signature validated to prevent spoofed events
- [ ] Payment method stored in Stripe only (never in database)
- [ ] PCI compliance maintained (no credit card data stored in application)
- [ ] Admin endpoints require Admin role authorization
- [ ] Subscription cancellation requires CSRF token (prevent unauthorized cancel links)

### Stripe Integration
- [ ] Stripe Subscription object created with price_id, customer_id, payment_method_id
- [ ] Stripe Customer created on first subscription (or linked if exists via email)
- [ ] Stripe PaymentMethod attached to Customer and set as default
- [ ] Stripe Invoice created for each billing attempt
- [ ] Webhook endpoint handles: invoice.payment_succeeded, invoice.payment_failed, customer.subscription.updated, customer.subscription.deleted
- [ ] Webhook events idempotent (duplicate events don't create duplicate orders)
- [ ] Stripe subscription status synced to database Status field
- [ ] Stripe invoice PDF URL stored for customer download

---

## Testing Requirements

### Unit Tests

**Test 1: Calculate Next Billing Date - Monthly Frequency**
```csharp
using Xunit;
using FluentAssertions;
using CandleStore.Application.Services;
using CandleStore.Domain.Enums;

namespace CandleStore.Tests.Unit.Application.Services
{
    public class SubscriptionServiceTests
    {
        [Theory]
        [InlineData("2025-01-15", BillingFrequency.Monthly, "2025-02-15")]
        [InlineData("2025-01-31", BillingFrequency.Monthly, "2025-02-28")] // Feb has 28 days
        [InlineData("2025-03-31", BillingFrequency.Monthly, "2025-04-30")] // Apr has 30 days
        public void CalculateNextBillingDate_WithMonthlyFrequency_ReturnsCorrectDate(
            string currentDate, BillingFrequency frequency, string expectedDate)
        {
            // Arrange
            var current = DateTime.Parse(currentDate);
            var expected = DateTime.Parse(expectedDate);
            var sut = new SubscriptionService();

            // Act
            var result = sut.CalculateNextBillingDate(current, frequency);

            // Assert
            result.Date.Should().Be(expected.Date);
        }

        [Theory]
        [InlineData("2025-01-15", BillingFrequency.BiMonthly, "2025-03-15")]
        [InlineData("2025-01-15", BillingFrequency.Quarterly, "2025-04-15")]
        public void CalculateNextBillingDate_WithDifferentFrequencies_ReturnsCorrectDate(
            string currentDate, BillingFrequency frequency, string expectedDate)
        {
            // Arrange
            var current = DateTime.Parse(currentDate);
            var expected = DateTime.Parse(expectedDate);
            var sut = new SubscriptionService();

            // Act
            var result = sut.CalculateNextBillingDate(current, frequency);

            // Assert
            result.Date.Should().Be(expected.Date);
        }
    }
}
```

**Test 2: Subscription Billing - Creates Order on Success**
```csharp
[Fact]
public async Task ProcessSubscriptionBilling_WhenPaymentSucceeds_CreatesOrderAndAdvancesBillingDate()
{
    // Arrange
    var subscriptionId = Guid.NewGuid();
    var customerId = Guid.NewGuid();

    var subscription = new Subscription
    {
        Id = subscriptionId,
        CustomerId = customerId,
        SubscriptionPlanId = Guid.NewGuid(),
        BillingFrequency = BillingFrequency.Monthly,
        NextBillingDate = DateTime.UtcNow.Date,
        Status = SubscriptionStatus.Active,
        StripeSubscriptionId = "sub_test_123"
    };

    var plan = new SubscriptionPlan
    {
        MonthlyPrice = 50.00m,
        CandlesPerDelivery = 2
    };

    _mockSubscriptionRepo.Setup(r => r.GetByIdAsync(subscriptionId))
        .ReturnsAsync(subscription);

    _mockSubscriptionPlanRepo.Setup(r => r.GetByIdAsync(subscription.SubscriptionPlanId))
        .ReturnsAsync(plan);

    _mockStripeService.Setup(s => s.ChargeSubscriptionAsync("sub_test_123"))
        .ReturnsAsync(new StripeInvoice { Status = "paid", InvoiceId = "inv_123" });

    _mockOrderService.Setup(s => s.CreateSubscriptionOrderAsync(subscriptionId))
        .ReturnsAsync(new Order { Id = Guid.NewGuid(), TotalAmount = 50.00m });

    _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

    // Act
    await _sut.ProcessSubscriptionBillingAsync(subscriptionId);

    // Assert
    subscription.NextBillingDate.Should().Be(DateTime.UtcNow.Date.AddMonths(1));
    subscription.Status.Should().Be(SubscriptionStatus.Active);

    _mockStripeService.Verify(s => s.ChargeSubscriptionAsync("sub_test_123"), Times.Once);
    _mockOrderService.Verify(s => s.CreateSubscriptionOrderAsync(subscriptionId), Times.Once);
    _mockEmailService.Verify(e => e.SendSubscriptionShipmentNotificationAsync(
        It.IsAny<string>(), It.IsAny<Order>()), Times.Once);
}
```

**Test 3: Failed Payment - Schedules Retry**
```csharp
[Fact]
public async Task ProcessSubscriptionBilling_WhenPaymentFails_SchedulesRetryAndSendsDunningEmail()
{
    // Arrange
    var subscription = new Subscription
    {
        Id = Guid.NewGuid(),
        Status = SubscriptionStatus.Active,
        RetryCount = 0,
        StripeSubscriptionId = "sub_test_123"
    };

    _mockSubscriptionRepo.Setup(r => r.GetByIdAsync(subscription.Id))
        .ReturnsAsync(subscription);

    _mockStripeService.Setup(s => s.ChargeSubscriptionAsync("sub_test_123"))
        .ThrowsAsync(new StripeException { ErrorCode = "card_declined" });

    _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

    // Act
    await _sut.ProcessSubscriptionBillingAsync(subscription.Id);

    // Assert
    subscription.Status.Should().Be(SubscriptionStatus.PastDue);
    subscription.RetryCount.Should().Be(1);
    subscription.NextRetryDate.Should().Be(DateTime.UtcNow.Date.AddDays(3)); // Retry on Day 3

    _mockEmailService.Verify(e => e.SendDunningEmailAsync(
        It.IsAny<string>(),
        It.Is<string>(msg => msg.Contains("Payment failed"))), Times.Once);

    _mockBackgroundJobService.Verify(b => b.ScheduleJob(
        It.IsAny<Expression<Action>>(),
        It.Is<DateTime>(d => d.Date == DateTime.UtcNow.Date.AddDays(3))), Times.Once);
}
```

**Test 4: Auto-Cancel After Max Retries**
```csharp
[Fact]
public async Task ProcessSubscriptionBilling_AfterThreeFailedRetries_CancelsSubscription()
{
    // Arrange
    var subscription = new Subscription
    {
        Id = Guid.NewGuid(),
        Status = SubscriptionStatus.PastDue,
        RetryCount = 3, // Already failed 3 times
        StripeSubscriptionId = "sub_test_123"
    };

    _mockSubscriptionRepo.Setup(r => r.GetByIdAsync(subscription.Id))
        .ReturnsAsync(subscription);

    _mockStripeService.Setup(s => s.ChargeSubscriptionAsync("sub_test_123"))
        .ThrowsAsync(new StripeException { ErrorCode = "card_declined" });

    _mockStripeService.Setup(s => s.CancelSubscriptionAsync("sub_test_123"))
        .Returns(Task.CompletedTask);

    _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

    // Act
    await _sut.ProcessSubscriptionBillingAsync(subscription.Id);

    // Assert
    subscription.Status.Should().Be(SubscriptionStatus.Canceled);
    subscription.CanceledAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

    _mockStripeService.Verify(s => s.CancelSubscriptionAsync("sub_test_123"), Times.Once);
    _mockEmailService.Verify(e => e.SendSubscriptionCancelledEmailAsync(
        It.IsAny<string>(),
        It.Is<string>(reason => reason.Contains("payment failure"))), Times.Once);
}
```

**Test 5: Prorated Tier Upgrade Calculation**
```csharp
[Theory]
[InlineData(50.00, 75.00, 15, 12.50)] // Silver to Gold mid-month
[InlineData(35.00, 50.00, 20, 10.00)] // Bronze to Silver
public async Task CalculateProratedUpgradeCharge_WithDifferentTiers_ReturnsCorrectAmount(
    decimal currentTierPrice, decimal newTierPrice, int daysRemaining, decimal expectedProration)
{
    // Arrange
    var sut = new SubscriptionService();

    // Act
    var result = sut.CalculateProratedUpgradeCharge(
        currentTierPrice, newTierPrice, daysRemaining);

    // Assert
    result.Should().BeApproximately(expectedProration, 0.01m);
}
```

**Test 6: Scent Preference Validation**
```csharp
[Fact]
public async Task UpdateScentPreferences_WithMoreThanThreeSelections_ThrowsValidationException()
{
    // Arrange
    var subscriptionId = Guid.NewGuid();
    var preferences = new List<string> { "Lavender", "Vanilla", "Citrus", "Floral" }; // 4 selections

    var subscription = new Subscription { Id = subscriptionId };

    _mockSubscriptionRepo.Setup(r => r.GetByIdAsync(subscriptionId))
        .ReturnsAsync(subscription);

    // Act
    Func<Task> act = async () => await _sut.UpdateScentPreferencesAsync(subscriptionId, preferences);

    // Assert
    await act.Should().ThrowAsync<ValidationException>()
        .WithMessage("*maximum 3 scent preferences*");
}
```

**Test 7: Pause Subscription Updates Status**
```csharp
[Fact]
public async Task PauseSubscription_WithValidResumeDate_UpdatesStatusAndBillingDate()
{
    // Arrange
    var subscriptionId = Guid.NewGuid();
    var resumeDate = DateTime.UtcNow.Date.AddMonths(2);

    var subscription = new Subscription
    {
        Id = subscriptionId,
        Status = SubscriptionStatus.Active,
        NextBillingDate = DateTime.UtcNow.Date
    };

    _mockSubscriptionRepo.Setup(r => r.GetByIdAsync(subscriptionId))
        .ReturnsAsync(subscription);

    _mockUnitOfWork.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

    // Act
    await _sut.PauseSubscriptionAsync(subscriptionId, resumeDate);

    // Assert
    subscription.Status.Should().Be(SubscriptionStatus.Paused);
    subscription.NextBillingDate.Should().Be(resumeDate);
    subscription.PausedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

    _mockEmailService.Verify(e => e.SendSubscriptionPausedEmailAsync(
        It.IsAny<string>(), resumeDate), Times.Once);
}
```

**Test 8: Minimum Commitment Validation**
```csharp
[Fact]
public async Task CancelSubscription_BeforeMinimumCommitment_ThrowsValidationException()
{
    // Arrange
    var subscriptionId = Guid.NewGuid();

    var subscription = new Subscription
    {
        Id = subscriptionId,
        CreatedAt = DateTime.UtcNow.AddMonths(-1), // Only 1 month old
        BillingFrequency = BillingFrequency.Monthly,
        BillingCount = 1 // Only 1 billing cycle completed
    };

    _mockSubscriptionRepo.Setup(r => r.GetByIdAsync(subscriptionId))
        .ReturnsAsync(subscription);

    // Act
    Func<Task> act = async () => await _sut.CancelSubscriptionAsync(subscriptionId, "Customer request");

    // Assert
    await act.Should().ThrowAsync<ValidationException>()
        .WithMessage("*minimum 2 billing cycles*");
}
```

### Integration Tests

**Test 1: Complete Subscription Signup Flow**
```csharp
[Fact]
public async Task SubscribeToCandle Club_CompleteFlow_CreatesSubscriptionAndChargesCustomer()
{
    // Arrange
    using var factory = new CustomWebApplicationFactory();
    var client = factory.CreateClient();

    // Login
    var token = await AuthenticateCustomerAsync(client);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    // Act - Subscribe to Silver tier
    var subscribeDto = new CreateSubscriptionDto
    {
        PlanId = SilverTierPlanId, // Seeded in test database
        BillingFrequency = BillingFrequency.Monthly,
        ScentPreferences = new List<string> { "Lavender", "Vanilla", "Citrus" },
        PaymentMethodId = "pm_test_card" // Test payment method
    };

    var response = await client.PostAsJsonAsync("/api/subscriptions", subscribeDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var result = await response.Content.ReadFromJsonAsync<ApiResponse<SubscriptionDto>>();
    result.Success.Should().BeTrue();
    result.Data.Status.Should().Be(SubscriptionStatus.Active);
    result.Data.NextBillingDate.Should().Be(DateTime.UtcNow.Date.AddMonths(1));

    // Verify order was created for first delivery
    var ordersResponse = await client.GetAsync("/api/orders");
    var ordersResult = await ordersResponse.Content.ReadFromJsonAsync<ApiResponse<List<OrderDto>>>();
    ordersResult.Data.Should().Contain(o => o.IsSubscription == true);
}
```

**Test 2: Failed Payment Retry Workflow**
```csharp
[Fact]
public async Task SubscriptionBilling_WhenPaymentFails_RetriesAndEventuallySucceeds()
{
    // Arrange
    using var factory = new CustomWebApplicationFactory();
    var subscriptionId = await CreateTestSubscriptionAsync(factory);

    // Simulate first payment failure
    MockStripeService.Setup(s => s.ChargeSubscriptionAsync(It.IsAny<string>()))
        .ThrowsAsync(new StripeException { ErrorCode = "card_declined" });

    // Act - Trigger billing job
    await TriggerSubscriptionBillingJobAsync(factory);

    // Assert - Status is PastDue, retry scheduled
    var subscription = await GetSubscriptionAsync(factory, subscriptionId);
    subscription.Status.Should().Be(SubscriptionStatus.PastDue);
    subscription.RetryCount.Should().Be(1);

    // Simulate successful retry 3 days later
    MockStripeService.Setup(s => s.ChargeSubscriptionAsync(It.IsAny<string>()))
        .ReturnsAsync(new StripeInvoice { Status = "paid" });

    // Advance time and trigger retry
    AdvanceTimeByDays(3);
    await TriggerSubscriptionBillingJobAsync(factory);

    // Assert - Status is Active, order created
    subscription = await GetSubscriptionAsync(factory, subscriptionId);
    subscription.Status.Should().Be(SubscriptionStatus.Active);
    subscription.RetryCount.Should().Be(0); // Reset after success
}
```

**Test 3: Pause and Resume Subscription**
```csharp
[Fact]
public async Task PauseSubscription_ThenResume_StopsAndRestartsBilling()
{
    // Arrange
    using var factory = new CustomWebApplicationFactory();
    var client = factory.CreateClient();
    var token = await AuthenticateCustomerAsync(client);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var subscriptionId = await CreateTestSubscriptionAsync(factory);

    // Act - Pause for 2 months
    var pauseDto = new PauseSubscriptionDto { ResumeDate = DateTime.UtcNow.Date.AddMonths(2) };
    var pauseResponse = await client.PostAsJsonAsync($"/api/subscriptions/{subscriptionId}/pause", pauseDto);

    // Assert - Paused status
    pauseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var subscription = await GetSubscriptionAsync(factory, subscriptionId);
    subscription.Status.Should().Be(SubscriptionStatus.Paused);

    // Trigger billing job - should skip paused subscription
    await TriggerSubscriptionBillingJobAsync(factory);

    // Verify no order created
    var orders = await GetCustomerOrdersAsync(factory, client);
    orders.Where(o => o.CreatedAt > DateTime.UtcNow.AddMinutes(-5)).Should().BeEmpty();

    // Act - Resume subscription
    var resumeResponse = await client.PostAsync($"/api/subscriptions/{subscriptionId}/resume", null);

    // Assert - Active again
    subscription = await GetSubscriptionAsync(factory, subscriptionId);
    subscription.Status.Should().Be(SubscriptionStatus.Active);
}
```

### End-to-End (E2E) Tests

**Scenario 1: Customer Subscribes and Receives First Delivery**
1. Navigate to `/candle-club` page
2. Click "Select Plan" on Silver tier
3. Select "Monthly" billing frequency
4. Click "Continue to Preferences"
5. Select 3 scent preferences: Lavender, Vanilla, Floral
6. Click "Continue to Payment"
7. Enter test card: 4242 4242 4242 4242, Expiry: 12/25, CVC: 123
8. Check "I agree to Terms of Service"
9. Click "Subscribe Now"
10. **Expected:** Redirect to confirmation page with message "Welcome to Candle Club!"
11. **Expected:** Email received: "Your Candle Club subscription is active"
12. Navigate to Account → My Subscriptions
13. **Expected:** Subscription shows Status: Active, Next Billing: [+1 month], Tier: Silver
14. Wait 2 business days
15. **Expected:** Email received: "Your Candle Club shipment is on the way!"
16. Navigate to Account → Orders
17. **Expected:** Order appears with "Subscription" badge, 2 candles listed

**Scenario 2: Admin Processes Daily Subscription Fulfillment**
1. Log into admin panel at 9:00 AM (after billing job ran at 2:00 AM)
2. Navigate to Admin → Subscriptions → Fulfillment Queue
3. **Expected:** List shows all subscriptions billed overnight (e.g., 37 orders)
4. **Expected:** Each order shows: Customer name, Tier, Products to ship, Shipping address
5. Check "Select All" checkbox
6. Click "Print All Labels" button
7. **Expected:** PDF opens with 37 shipping labels (1 per page)
8. Print labels and affix to packages
9. Pack orders with thank-you cards
10. Click "Mark All as Shipped" button
11. **Expected:** Success message "37 orders marked as shipped"
12. **Expected:** Customers receive tracking emails within 5 minutes

**Scenario 3: Customer Updates Payment Method After Decline**
1. Simulate card decline (admin changes Stripe test key to always decline)
2. Billing job runs overnight
3. Customer receives email: "Payment failed - update your card"
4. Customer clicks "Update Payment Method" link in email
5. Redirects to Account → My Subscriptions
6. **Expected:** Subscription shows Status: Past Due, Banner: "Payment failed - update card"
7. Click "Update Payment" button
8. Enter new card: 4242 4242 4242 4242 (working test card)
9. Click "Save"
10. **Expected:** Success message "Payment method updated"
11. **Expected:** Status changes to "Retry Scheduled"
12. Wait for retry (or admin triggers manual retry)
13. **Expected:** Payment succeeds, Status returns to Active, Order created

### Performance Tests

**Benchmark 1: Billing Job Processing Time**
- **Target:** Process 1000 subscriptions in < 10 minutes (average 0.6 seconds per subscription)
- **Measurement:** Seed database with 1000 test subscriptions due for billing, run billing job, measure total execution time
- **Pass Criteria:** Total time < 600 seconds (10 minutes)
- **Rationale:** Billing job must complete before business hours (runs at 2:00 AM) to ensure orders ready by 9:00 AM

**Benchmark 2: Subscription Dashboard Load Time**
- **Target:** < 500ms to load customer subscription dashboard with 5 subscriptions
- **Measurement:** Blazor page load time from navigation to render complete
- **Pass Criteria:** 95th percentile < 500ms
- **Rationale:** Customer-facing dashboard must load quickly for good UX

**Benchmark 3: MRR Calculation Query**
- **Target:** < 200ms to calculate MRR for 1000 active subscriptions
- **Measurement:** SQL query execution time for `SELECT SUM(MonthlyPrice) FROM Subscriptions WHERE Status = 'Active'`
- **Pass Criteria:** Average < 200ms over 100 executions
- **Rationale:** MRR dashboard frequently accessed by admin and should load instantly

**Benchmark 4: Scent Curation Algorithm**
- **Target:** < 200ms to select products for 1 subscription
- **Measurement:** Algorithm execution time from preferences input to product list output
- **Pass Criteria:** Average < 200ms, max < 500ms
- **Rationale:** Curation runs during billing job; slow algorithm delays order creation

### Regression Tests

**Features That Must Not Break:**

1. **Standard Orders (Task 016):**
   - One-time product purchases must still work normally
   - Subscription orders and regular orders must coexist in fulfillment queue
   - **Regression Test:** Create one-time order, verify it's not marked as subscription

2. **Payment Processing (Task 015):**
   - Existing Stripe checkout flow must remain functional
   - PaymentMethod storage for subscriptions must not interfere with one-time checkout
   - **Regression Test:** Complete one-time checkout with same customer who has active subscription

3. **Customer Dashboard (Task 021):**
   - Adding subscription section must not break existing dashboard features (orders, addresses, profile)
   - **Regression Test:** Navigate through all dashboard tabs, verify no errors

4. **Email System (Task 025):**
   - Subscription emails must not interfere with existing transactional emails (order confirmation, shipping notification)
   - **Regression Test:** Complete one-time purchase, verify order confirmation email still sent correctly

---

## User Verification Steps

### Verification 1: Create Subscription Plan (Admin)
1. Log into admin panel
2. Navigate to Admin → Subscriptions → Plans
3. Click "Create New Plan" button
4. Fill in form:
   - Name: "Silver Tier"
   - Description: "2 curated candles delivered monthly"
   - Monthly Price: $50.00
   - Candles Per Delivery: 2
   - Include Exclusive Scent: No
5. Click "Create Plan" button
6. **Verify:** Success message appears
7. **Verify:** Plan appears in plan list with status "Active"
8. Navigate to customer-facing site: `/candle-club`
9. **Verify:** Silver Tier plan displayed with $50/month pricing

### Verification 2: Subscribe to Candle Club (Customer)
1. Navigate to `/candle-club` page
2. Select "Silver Tier" plan
3. Select "Monthly" billing frequency
4. Click "Continue to Preferences"
5. Select 3 scent preferences: Lavender, Vanilla, Citrus
6. Click "Continue to Payment"
7. Enter payment details: Test card 4242 4242 4242 4242, Expiry 12/25, CVC 123
8. Check "I agree to Terms" checkbox
9. Click "Subscribe Now"
10. **Verify:** Redirect to confirmation page
11. **Verify:** Confirmation message: "Welcome to Candle Club!"
12. Check email inbox
13. **Verify:** Email received with subject "Your Candle Club subscription is active"

### Verification 3: View Subscription Dashboard
1. Log in as customer with active subscription
2. Navigate to Account → My Subscriptions
3. **Verify:** Subscription card displayed with:
   - Tier: Silver Tier
   - Price: $50.00 / month
   - Next Billing: [future date]
   - Status: Active ✓
   - Scent Preferences: Lavender, Vanilla, Citrus
   - Payment Method: Visa ****4242
4. **Verify:** Buttons visible: Edit Preferences, Update Payment, Pause Subscription, Change Tier, Cancel Subscription

### Verification 4: Pause Subscription
1. On subscription dashboard, click "Pause Subscription"
2. Modal opens with resume date selector
3. Select "Resume in 2 months" option
4. Click "Confirm Pause"
5. **Verify:** Success message "Subscription paused"
6. **Verify:** Status changes to "Paused"
7. **Verify:** Next Billing date updates to resume date (2 months from now)
8. Check email inbox
9. **Verify:** Email received: "Your subscription has been paused"

### Verification 5: Process Subscription Billing (Background Job)
1. Seed database with subscription where NextBillingDate = today
2. Trigger billing job manually: Admin → System → Background Jobs → "Run Billing Job Now"
3. **Verify:** Job completes successfully (green status in Hangfire dashboard)
4. Navigate to Admin → Subscriptions → [Test Customer]
5. **Verify:** NextBillingDate advanced by 1 month
6. **Verify:** New order created with "Subscription" badge
7. **Verify:** Order status: Pending Fulfillment
8. Check customer email
9. **Verify:** Email received: "Your Candle Club shipment is on the way!"

### Verification 6: Handle Failed Payment
1. Use Stripe test card that always declines: 4000 0000 0000 0341
2. Trigger billing for subscription with this payment method
3. **Verify:** Subscription status changes to "Past Due"
4. **Verify:** RetryCount = 1
5. **Verify:** NextRetryDate = today + 3 days
6. Check customer email
7. **Verify:** Dunning email received: "Payment failed - update your card"
8. Customer clicks "Update Payment" link in email
9. Customer updates to working card: 4242 4242 4242 4242
10. **Verify:** Payment method updated successfully
11. Trigger manual retry: Admin → Subscriptions → [Customer] → "Retry Payment"
12. **Verify:** Payment succeeds, status returns to "Active"

### Verification 7: Admin Fulfillment Queue
1. After billing job runs, navigate to Admin → Subscriptions → Fulfillment Queue
2. Select date: Today
3. **Verify:** List shows all subscription orders billed today
4. **Verify:** Each order displays: Customer name, Tier, Product names, Shipping address
5. Check box next to 3 orders
6. Click "Print Selected Labels"
7. **Verify:** PDF opens with 3 shipping labels
8. Click "Mark as Shipped" for one order
9. **Verify:** Order status updates to "Shipped"
10. **Verify:** Customer receives tracking email

### Verification 8: MRR Dashboard (Admin Analytics)
1. Navigate to Admin → Analytics → Subscriptions
2. **Verify:** MRR metric displayed (e.g., "$7,350 Monthly Recurring Revenue")
3. **Verify:** MRR trend chart shows 12-month history
4. **Verify:** Subscription tier distribution pie chart shows Bronze %, Silver %, Gold %
5. **Verify:** Churn rate metric displayed (e.g., "8.2% monthly churn")
6. **Verify:** Cohort retention table shows Month 1-12 retention percentages

### Verification 9: Cancel Subscription
1. Log in as customer with active subscription (completed 2+ billing cycles)
2. Navigate to Account → My Subscriptions
3. Click "Cancel Subscription" button
4. Cancellation survey modal opens (optional feedback)
5. Click "Confirm Cancellation"
6. **Verify:** Confirmation message "Your subscription will end after this billing cycle"
7. **Verify:** Status changes to "Canceled"
8. **Verify:** Next Billing shows "N/A - Subscription Canceled"
9. Check email inbox
10. **Verify:** Email received: "Your Candle Club subscription has been canceled"

### Verification 10: Scent Preference Curation
1. Customer has scent preferences: Lavender, Vanilla, Citrus
2. Billing job runs, creates order for this customer
3. Navigate to Admin → Orders → [Subscription Order]
4. **Verify:** Order contains 2 candles
5. **Verify:** At least 1 candle matches scent preferences (e.g., "Lavender Honey")
6. **Verify:** Products are varied (not same candle twice)
7. Repeat for next month's billing
8. **Verify:** Different products selected (no exact repeat from previous month)

---

## Implementation Prompt for Claude

### Implementation Overview

This task implements a complete subscription/recurring order system ("Candle Club") with tiered pricing, automated billing via Stripe Subscriptions, customer self-service management, and admin fulfillment workflows. Customers subscribe to monthly plans (Bronze/Silver/Gold) with automatic product curation based on scent preferences, recurring charges, and flexible pause/skip/cancel options. Store owners gain predictable recurring revenue (MRR), improved cash flow, and 3.2× higher customer lifetime value.

**What You'll Build:**
- Subscription and SubscriptionPlan domain entities
- SubscriptionService business logic layer (signup, billing, pause, cancel, tier changes)
- Stripe Subscriptions API integration (create subscription, charge invoices, handle webhooks)
- Daily billing background job (Hangfire scheduler processes due subscriptions)
- Payment retry and dunning email workflow (3 attempts over 7 days)
- Customer self-service subscription dashboard (Blazor component)
- Admin subscription management and fulfillment queue pages
- MRR analytics dashboard with churn analysis
- Scent preference curation algorithm

### Prerequisites

**Required:**
- .NET 8 SDK installed
- Completed Task 002 (Domain Models & EF Core)
- Completed Task 015 (Checkout & Payment - Stripe integration)
- Completed Task 025 (Email Marketing - for subscription emails)
- Stripe account with Subscriptions enabled
- Hangfire installed (for background job scheduling)

**NuGet Packages Needed:**
- `Stripe.net` (v43.0+) - Stripe Subscriptions API
- `Hangfire.Core` (v1.8+) - background job scheduling
- `Hangfire.PostgreSql` or `Hangfire.SqlServer` - job persistence
- All packages from Task 002, 015, 025

### Step-by-Step Implementation

#### Step 1: Create Domain Entities

**Location:** `src/CandleStore.Domain/Entities/Subscriptions/`

Create `SubscriptionPlan.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandleStore.Domain.Entities.Subscriptions
{
    public class SubscriptionPlan
    {
        [Key]
        public Guid PlanId { get; set; }

        [Required, MaxLength(200)]
        public string PlanName { get; set; } // "Monthly Candle Box", "Quarterly Sampler"

        [MaxLength(100)]
        public string Slug { get; set; } // "monthly-candle-box"

        public string Description { get; set; }

        public SubscriptionFrequency Frequency { get; set; } // Monthly, Quarterly, Annual

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // $45.00/month

        [MaxLength(50)]
        public string StripePriceId { get; set; } // price_1Abc... from Stripe

        public int CandleCount { get; set; } // 3 candles per box

        public bool AllowCustomization { get; set; } = true; // Customer can pick scents

        public int MinCommitmentCycles { get; set; } = 2; // 2-month minimum

        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage { get; set; } = 0; // 15% off retail

        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;

        public string ImageUrl { get; set; }
        public string Features { get; set; } // JSON or delimited list

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<CustomerSubscription> Subscriptions { get; set; } = new List<CustomerSubscription>();
    }

    public enum SubscriptionFrequency
    {
        Monthly,
        Quarterly,
        Annual
    }
}
```

Create `CustomerSubscription.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandleStore.Domain.Entities.Subscriptions
{
    public class CustomerSubscription
    {
        [Key]
        public Guid SubscriptionId { get; set; }

        public Guid CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; }

        public Guid PlanId { get; set; }
        [ForeignKey(nameof(PlanId))]
        public virtual SubscriptionPlan Plan { get; set; }

        [MaxLength(100)]
        public string StripeSubscriptionId { get; set; } // sub_1Abc... from Stripe

        [MaxLength(100)]
        public string StripeCustomerId { get; set; }

        [MaxLength(100)]
        public string StripePaymentMethodId { get; set; }

        public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? PausedAt { get; set; }

        public DateTime NextBillingDate { get; set; }
        public DateTime? LastBillingDate { get; set; }

        public int TotalBillingCycles { get; set; } = 0; // How many times billed
        public int FailedPaymentAttempts { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentPrice { get; set; } // Locked-in price

        public string CancellationReason { get; set; }
        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public virtual ICollection<SubscriptionPreference> Preferences { get; set; } = new List<SubscriptionPreference>();
        public virtual ICollection<SubscriptionBilling> BillingHistory { get; set; } = new List<SubscriptionBilling>();
        public virtual ICollection<SubscriptionShipment> Shipments { get; set; } = new List<SubscriptionShipment>();

        // Computed
        [NotMapped]
        public bool IsActive => Status == SubscriptionStatus.Active;

        [NotMapped]
        public bool CanBeCancelled => TotalBillingCycles >= Plan?.MinCommitmentCycles;
    }

    public enum SubscriptionStatus
    {
        Active,
        Paused,
        Cancelled,
        PastDue,
        Expired
    }
}
```

Create `SubscriptionPreference.cs`:

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandleStore.Domain.Entities.Subscriptions
{
    public class SubscriptionPreference
    {
        [Key]
        public Guid PreferenceId { get; set; }

        public Guid SubscriptionId { get; set; }
        [ForeignKey(nameof(SubscriptionId))]
        public virtual CustomerSubscription Subscription { get; set; }

        [Required, MaxLength(100)]
        public string ScentCategory { get; set; } // "Floral", "Citrus", "Woody"

        public int PreferenceWeight { get; set; } = 5; // 1-10 scale

        public bool ExcludeFromSelection { get; set; } = false; // Customer hates this

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

Create `SubscriptionBilling.cs`:

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandleStore.Domain.Entities.Subscriptions
{
    public class SubscriptionBilling
    {
        [Key]
        public Guid BillingId { get; set; }

        public Guid SubscriptionId { get; set; }
        [ForeignKey(nameof(SubscriptionId))]
        public virtual CustomerSubscription Subscription { get; set; }

        public int BillingCycleNumber { get; set; } // 1, 2, 3...

        public DateTime BillingDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public BillingStatus Status { get; set; } = BillingStatus.Pending;

        [MaxLength(100)]
        public string StripeInvoiceId { get; set; }

        [MaxLength(100)]
        public string StripeChargeId { get; set; }

        public DateTime? PaidAt { get; set; }
        public DateTime? FailedAt { get; set; }

        public string FailureReason { get; set; }

        public int RetryAttempt { get; set; } = 0;
        public DateTime? NextRetryDate { get; set; }

        public Guid? OrderId { get; set; } // Links to Order created after successful payment

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum BillingStatus
    {
        Pending,
        Paid,
        Failed,
        Refunded,
        Retrying
    }
}
```

Create `SubscriptionShipment.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandleStore.Domain.Entities.Subscriptions
{
    public class SubscriptionShipment
    {
        [Key]
        public Guid ShipmentId { get; set; }

        public Guid SubscriptionId { get; set; }
        [ForeignKey(nameof(SubscriptionId))]
        public virtual CustomerSubscription Subscription { get; set; }

        public Guid BillingId { get; set; } // Which billing cycle this shipment is for
        [ForeignKey(nameof(BillingId))]
        public virtual SubscriptionBilling Billing { get; set; }

        public int ShipmentNumber { get; set; } // 1st shipment, 2nd shipment...

        public DateTime ScheduledShipDate { get; set; }
        public DateTime? ActualShipDate { get; set; }

        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

        [MaxLength(200)]
        public string TrackingNumber { get; set; }

        [MaxLength(50)]
        public string Carrier { get; set; }

        public string CuratedProducts { get; set; } // JSON list of ProductIds selected

        public string CurationNotes { get; set; } // Why these products were selected

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ShipmentStatus
    {
        Pending,
        Curated,
        ReadyToShip,
        Shipped,
        Delivered,
        Exception
    }
}
```

#### Step 2: Create EF Core Configurations

**Location:** `src/CandleStore.Infrastructure/Data/Configurations/Subscriptions/`

Create `SubscriptionPlanConfiguration.cs`:

```csharp
using CandleStore.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CandleStore.Infrastructure.Data.Configurations.Subscriptions
{
    public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
    {
        public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
        {
            builder.ToTable("SubscriptionPlans");

            builder.HasKey(p => p.PlanId);

            builder.Property(p => p.PlanName).IsRequired().HasMaxLength(200);
            builder.Property(p => p.Slug).IsRequired().HasMaxLength(100);
            builder.HasIndex(p => p.Slug).IsUnique();

            builder.Property(p => p.Frequency).IsRequired().HasConversion<string>();
            builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
            builder.Property(p => p.DiscountPercentage).HasColumnType("decimal(5,2)");

            builder.Property(p => p.StripePriceId).HasMaxLength(50);
            builder.HasIndex(p => p.StripePriceId);

            builder.Property(p => p.IsActive).HasDefaultValue(true);
            builder.Property(p => p.CreatedAt).HasDefaultValueSql("NOW()");

            builder.HasIndex(p => new { p.IsActive, p.DisplayOrder });
        }
    }
}
```

Create `CustomerSubscriptionConfiguration.cs`:

```csharp
using CandleStore.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CandleStore.Infrastructure.Data.Configurations.Subscriptions
{
    public class CustomerSubscriptionConfiguration : IEntityTypeConfiguration<CustomerSubscription>
    {
        public void Configure(EntityTypeBuilder<CustomerSubscription> builder)
        {
            builder.ToTable("CustomerSubscriptions");

            builder.HasKey(s => s.SubscriptionId);

            builder.Property(s => s.StripeSubscriptionId).HasMaxLength(100);
            builder.HasIndex(s => s.StripeSubscriptionId).IsUnique();

            builder.Property(s => s.StripeCustomerId).HasMaxLength(100);
            builder.Property(s => s.StripePaymentMethodId).HasMaxLength(100);

            builder.Property(s => s.Status).IsRequired().HasConversion<string>();
            builder.Property(s => s.CurrentPrice).HasColumnType("decimal(18,2)");

            builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");

            // Indexes for querying
            builder.HasIndex(s => s.CustomerId);
            builder.HasIndex(s => s.Status);
            builder.HasIndex(s => s.NextBillingDate);
            builder.HasIndex(s => new { s.Status, s.NextBillingDate }); // For billing job

            // Relationships
            builder.HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(s => s.Preferences)
                .WithOne(p => p.Subscription)
                .HasForeignKey(p => p.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.BillingHistory)
                .WithOne(b => b.Subscription)
                .HasForeignKey(b => b.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Shipments)
                .WithOne(sh => sh.Subscription)
                .HasForeignKey(sh => sh.SubscriptionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
```

Create `SubscriptionBillingConfiguration.cs`:

```csharp
using CandleStore.Domain.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CandleStore.Infrastructure.Data.Configurations.Subscriptions
{
    public class SubscriptionBillingConfiguration : IEntityTypeConfiguration<SubscriptionBilling>
    {
        public void Configure(EntityTypeBuilder<SubscriptionBilling> builder)
        {
            builder.ToTable("SubscriptionBillings");

            builder.HasKey(b => b.BillingId);

            builder.Property(b => b.Amount).HasColumnType("decimal(18,2)");
            builder.Property(b => b.Status).IsRequired().HasConversion<string>();

            builder.Property(b => b.StripeInvoiceId).HasMaxLength(100);
            builder.HasIndex(b => b.StripeInvoiceId);

            builder.Property(b => b.StripeChargeId).HasMaxLength(100);

            builder.Property(b => b.CreatedAt).HasDefaultValueSql("NOW()");

            // Indexes for billing job queries
            builder.HasIndex(b => new { b.Status, b.NextRetryDate });
            builder.HasIndex(b => b.BillingDate);
        }
    }
}
```

Create configurations for SubscriptionPreference and SubscriptionShipment following similar patterns.

#### Step 3: Create Repository Interfaces and Implementations

**Location:** `src/CandleStore.Application/Interfaces/Repositories/`

Create `ISubscriptionRepository.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandleStore.Domain.Entities.Subscriptions;

namespace CandleStore.Application.Interfaces.Repositories
{
    public interface ISubscriptionRepository : IRepository<CustomerSubscription>
    {
        Task<CustomerSubscription> GetByIdWithDetailsAsync(Guid subscriptionId);
        Task<IEnumerable<CustomerSubscription>> GetByCustomerIdAsync(Guid customerId);
        Task<CustomerSubscription> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId);
        Task<IEnumerable<CustomerSubscription>> GetActiveSubscriptionsAsync();
        Task<IEnumerable<CustomerSubscription>> GetSubscriptionsDueForBillingAsync(DateTime date);
        Task<IEnumerable<CustomerSubscription>> GetPastDueSubscriptionsAsync();
        Task<decimal> GetMonthlyRecurringRevenueAsync();
        Task<int> GetActiveSubscriptionCountAsync();
        Task<decimal> GetChurnRateAsync(DateTime startDate, DateTime endDate);
    }
}
```

Create `ISubscriptionPlanRepository.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandleStore.Domain.Entities.Subscriptions;

namespace CandleStore.Application.Interfaces.Repositories
{
    public interface ISubscriptionPlanRepository : IRepository<SubscriptionPlan>
    {
        Task<SubscriptionPlan> GetBySlugAsync(string slug);
        Task<IEnumerable<SubscriptionPlan>> GetActivePlansAsync();
        Task<bool> IsSlugUniqueAsync(string slug, Guid? excludePlanId = null);
    }
}
```

**Location:** `src/CandleStore.Infrastructure/Repositories/Subscriptions/`

Create `SubscriptionRepository.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities.Subscriptions;
using CandleStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CandleStore.Infrastructure.Repositories.Subscriptions
{
    public class SubscriptionRepository : Repository<CustomerSubscription>, ISubscriptionRepository
    {
        public SubscriptionRepository(ApplicationDbContext context) : base(context) { }

        public async Task<CustomerSubscription> GetByIdWithDetailsAsync(Guid subscriptionId)
        {
            return await _context.CustomerSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.Customer)
                .Include(s => s.Preferences)
                .Include(s => s.BillingHistory.OrderByDescending(b => b.BillingDate).Take(12))
                .Include(s => s.Shipments.OrderByDescending(sh => sh.ShipmentNumber).Take(6))
                .FirstOrDefaultAsync(s => s.SubscriptionId == subscriptionId);
        }

        public async Task<IEnumerable<CustomerSubscription>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _context.CustomerSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.BillingHistory.OrderByDescending(b => b.BillingDate).Take(3))
                .Where(s => s.CustomerId == customerId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<CustomerSubscription> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId)
        {
            return await _context.CustomerSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);
        }

        public async Task<IEnumerable<CustomerSubscription>> GetActiveSubscriptionsAsync()
        {
            return await _context.CustomerSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.Customer)
                .Where(s => s.Status == SubscriptionStatus.Active)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerSubscription>> GetSubscriptionsDueForBillingAsync(DateTime date)
        {
            return await _context.CustomerSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.Customer)
                .Where(s => s.Status == SubscriptionStatus.Active
                    && s.NextBillingDate.Date == date.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<CustomerSubscription>> GetPastDueSubscriptionsAsync()
        {
            return await _context.CustomerSubscriptions
                .Include(s => s.Plan)
                .Include(s => s.Customer)
                .Include(s => s.BillingHistory.Where(b => b.Status == BillingStatus.Failed))
                .Where(s => s.Status == SubscriptionStatus.PastDue
                    && s.FailedPaymentAttempts < 3)
                .ToListAsync();
        }

        public async Task<decimal> GetMonthlyRecurringRevenueAsync()
        {
            // MRR = sum of all active subscriptions normalized to monthly
            var subscriptions = await _context.CustomerSubscriptions
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.Active)
                .ToListAsync();

            decimal mrr = 0;
            foreach (var sub in subscriptions)
            {
                switch (sub.Plan.Frequency)
                {
                    case SubscriptionFrequency.Monthly:
                        mrr += sub.CurrentPrice;
                        break;
                    case SubscriptionFrequency.Quarterly:
                        mrr += sub.CurrentPrice / 3; // Divide by 3 months
                        break;
                    case SubscriptionFrequency.Annual:
                        mrr += sub.CurrentPrice / 12; // Divide by 12 months
                        break;
                }
            }

            return mrr;
        }

        public async Task<int> GetActiveSubscriptionCountAsync()
        {
            return await _context.CustomerSubscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active);
        }

        public async Task<decimal> GetChurnRateAsync(DateTime startDate, DateTime endDate)
        {
            var activeAtStart = await _context.CustomerSubscriptions
                .CountAsync(s => s.StartDate < startDate
                    && (s.CancelledAt == null || s.CancelledAt >= startDate));

            var cancelledDuringPeriod = await _context.CustomerSubscriptions
                .CountAsync(s => s.CancelledAt >= startDate && s.CancelledAt <= endDate);

            return activeAtStart > 0
                ? (decimal)cancelledDuringPeriod / activeAtStart * 100
                : 0;
        }
    }
}
```

#### Step 4: Create Subscription Service

**Location:** `src/CandleStore.Application/Services/Subscriptions/`

Create `ISubscriptionService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandleStore.Application.DTOs.Subscriptions;

namespace CandleStore.Application.Services.Subscriptions
{
    public interface ISubscriptionService
    {
        // Subscription Management
        Task<SubscriptionDto> CreateSubscriptionAsync(Guid customerId, CreateSubscriptionDto dto);
        Task<SubscriptionDto> GetSubscriptionByIdAsync(Guid subscriptionId);
        Task<IEnumerable<SubscriptionDto>> GetCustomerSubscriptionsAsync(Guid customerId);
        Task<SubscriptionDto> UpdatePaymentMethodAsync(Guid subscriptionId, string stripePaymentMethodId);
        Task<SubscriptionDto> PauseSubscriptionAsync(Guid subscriptionId);
        Task<SubscriptionDto> ResumeSubscriptionAsync(Guid subscriptionId);
        Task<SubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, string reason);

        // Preferences
        Task UpdatePreferencesAsync(Guid subscriptionId, List<SubscriptionPreferenceDto> preferences);

        // Billing
        Task ProcessBillingAsync(Guid subscriptionId);
        Task<int> ProcessAllDueBillingsAsync(DateTime date);
        Task<BillingDto> RetryFailedPaymentAsync(Guid billingId);

        // Analytics
        Task<SubscriptionAnalyticsDto> GetAnalyticsAsync();
        Task<decimal> GetMonthlyRecurringRevenueAsync();
    }
}
```

Create `SubscriptionService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CandleStore.Application.DTOs.Subscriptions;
using CandleStore.Application.Interfaces;
using CandleStore.Domain.Entities.Subscriptions;
using Microsoft.Extensions.Logging;
using Stripe;

namespace CandleStore.Application.Services.Subscriptions
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IStripeService _stripeService;
        private readonly IEmailService _emailService;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IStripeService stripeService,
            IEmailService emailService,
            ILogger<SubscriptionService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _stripeService = stripeService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<SubscriptionDto> CreateSubscriptionAsync(Guid customerId, CreateSubscriptionDto dto)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
            if (customer == null)
                throw new ArgumentException($"Customer {customerId} not found");

            var plan = await _unitOfWork.SubscriptionPlans.GetByIdAsync(dto.PlanId);
            if (plan == null || !plan.IsActive)
                throw new ArgumentException($"Subscription plan {dto.PlanId} not found or inactive");

            // Create or retrieve Stripe customer
            var stripeCustomerId = customer.StripeCustomerId;
            if (string.IsNullOrEmpty(stripeCustomerId))
            {
                stripeCustomerId = await _stripeService.CreateCustomerAsync(customer.Email, customer.FullName);
                customer.StripeCustomerId = stripeCustomerId;
            }

            // Attach payment method to Stripe customer
            await _stripeService.AttachPaymentMethodAsync(dto.StripePaymentMethodId, stripeCustomerId);

            // Create Stripe subscription
            var stripeSubscription = await _stripeService.CreateSubscriptionAsync(
                stripeCustomerId,
                plan.StripePriceId,
                dto.StripePaymentMethodId);

            // Create local subscription record
            var subscription = new CustomerSubscription
            {
                SubscriptionId = Guid.NewGuid(),
                CustomerId = customerId,
                PlanId = dto.PlanId,
                StripeSubscriptionId = stripeSubscription.Id,
                StripeCustomerId = stripeCustomerId,
                StripePaymentMethodId = dto.StripePaymentMethodId,
                Status = SubscriptionStatus.Active,
                StartDate = DateTime.UtcNow,
                NextBillingDate = DateTime.UtcNow.AddMonths(plan.Frequency == SubscriptionFrequency.Monthly ? 1 :
                    plan.Frequency == SubscriptionFrequency.Quarterly ? 3 : 12),
                CurrentPrice = plan.Price,
                TotalBillingCycles = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Subscriptions.AddAsync(subscription);

            // Add preferences if provided
            if (dto.Preferences != null && dto.Preferences.Any())
            {
                foreach (var pref in dto.Preferences)
                {
                    subscription.Preferences.Add(new SubscriptionPreference
                    {
                        PreferenceId = Guid.NewGuid(),
                        SubscriptionId = subscription.SubscriptionId,
                        ScentCategory = pref.ScentCategory,
                        PreferenceWeight = pref.PreferenceWeight,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation($"Created subscription {subscription.SubscriptionId} for customer {customer.Email}, plan {plan.PlanName}");

            // Send confirmation email
            await _emailService.SendSubscriptionConfirmationAsync(customer.Email, subscription, plan);

            return _mapper.Map<SubscriptionDto>(subscription);
        }

        public async Task<SubscriptionDto> GetSubscriptionByIdAsync(Guid subscriptionId)
        {
            var subscription = await _unitOfWork.Subscriptions.GetByIdWithDetailsAsync(subscriptionId);
            return subscription != null ? _mapper.Map<SubscriptionDto>(subscription) : null;
        }

        public async Task<IEnumerable<SubscriptionDto>> GetCustomerSubscriptionsAsync(Guid customerId)
        {
            var subscriptions = await _unitOfWork.Subscriptions.GetByCustomerIdAsync(customerId);
            return _mapper.Map<IEnumerable<SubscriptionDto>>(subscriptions);
        }

        public async Task<SubscriptionDto> UpdatePaymentMethodAsync(Guid subscriptionId, string stripePaymentMethodId)
        {
            var subscription = await _unitOfWork.Subscriptions.GetByIdWithDetailsAsync(subscriptionId);
            if (subscription == null)
                throw new ArgumentException($"Subscription {subscriptionId} not found");

            // Update payment method in Stripe
            await _stripeService.UpdateSubscriptionPaymentMethodAsync(
                subscription.StripeSubscriptionId,
                stripePaymentMethodId);

            subscription.StripePaymentMethodId = stripePaymentMethodId;
            subscription.UpdatedAt = DateTime.UtcNow;

            // If subscription was past due, retry billing
            if (subscription.Status == SubscriptionStatus.PastDue)
            {
                subscription.Status = SubscriptionStatus.Active;
                subscription.FailedPaymentAttempts = 0;

                // Trigger immediate billing attempt
                await ProcessBillingAsync(subscriptionId);
            }

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation($"Updated payment method for subscription {subscriptionId}");

            return _mapper.Map<SubscriptionDto>(subscription);
        }

        public async Task<SubscriptionDto> CancelSubscriptionAsync(Guid subscriptionId, string reason)
        {
            var subscription = await _unitOfWork.Subscriptions.GetByIdWithDetailsAsync(subscriptionId);
            if (subscription == null)
                throw new ArgumentException($"Subscription {subscriptionId} not found");

            // Check minimum commitment
            if (subscription.TotalBillingCycles < subscription.Plan.MinCommitmentCycles)
            {
                throw new InvalidOperationException(
                    $"Cannot cancel subscription. Minimum commitment: {subscription.Plan.MinCommitmentCycles} billing cycles, " +
                    $"completed: {subscription.TotalBillingCycles}");
            }

            // Cancel in Stripe
            await _stripeService.CancelSubscriptionAsync(subscription.StripeSubscriptionId);

            subscription.Status = SubscriptionStatus.Cancelled;
            subscription.CancelledAt = DateTime.UtcNow;
            subscription.EndDate = DateTime.UtcNow;
            subscription.CancellationReason = reason;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();

            _logger.LogInformation($"Cancelled subscription {subscriptionId}, reason: {reason}");

            // Send cancellation confirmation email
            await _emailService.SendSubscriptionCancellationAsync(subscription.Customer.Email, subscription);

            return _mapper.Map<SubscriptionDto>(subscription);
        }

        public async Task ProcessBillingAsync(Guid subscriptionId)
        {
            var subscription = await _unitOfWork.Subscriptions.GetByIdWithDetailsAsync(subscriptionId);
            if (subscription == null || subscription.Status != SubscriptionStatus.Active)
                return;

            var billingCycleNumber = subscription.TotalBillingCycles + 1;

            var billing = new SubscriptionBilling
            {
                BillingId = Guid.NewGuid(),
                SubscriptionId = subscriptionId,
                BillingCycleNumber = billingCycleNumber,
                BillingDate = DateTime.UtcNow,
                Amount = subscription.CurrentPrice,
                Status = BillingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // Charge via Stripe
                var charge = await _stripeService.ChargeSubscriptionAsync(
                    subscription.StripeCustomerId,
                    subscription.StripePaymentMethodId,
                    subscription.CurrentPrice,
                    $"Subscription billing cycle {billingCycleNumber} - {subscription.Plan.PlanName}");

                billing.Status = BillingStatus.Paid;
                billing.StripeChargeId = charge.Id;
                billing.PaidAt = DateTime.UtcNow;

                subscription.LastBillingDate = DateTime.UtcNow;
                subscription.NextBillingDate = CalculateNextBillingDate(subscription);
                subscription.TotalBillingCycles++;
                subscription.FailedPaymentAttempts = 0;

                // Create order for fulfillment
                var order = await CreateOrderFromSubscriptionAsync(subscription, billing);
                billing.OrderId = order.OrderId;

                _logger.LogInformation($"Successfully billed subscription {subscriptionId}, amount ${billing.Amount}, order {order.OrderId} created");

                // Send shipment notification email
                await _emailService.SendSubscriptionShipmentNotificationAsync(subscription.Customer.Email, subscription, order);
            }
            catch (StripeException ex)
            {
                billing.Status = BillingStatus.Failed;
                billing.FailedAt = DateTime.UtcNow;
                billing.FailureReason = ex.Message;
                billing.RetryAttempt = 0;
                billing.NextRetryDate = DateTime.UtcNow.AddDays(3); // Retry in 3 days

                subscription.FailedPaymentAttempts++;

                if (subscription.FailedPaymentAttempts >= 3)
                {
                    subscription.Status = SubscriptionStatus.PastDue;
                    _logger.LogWarning($"Subscription {subscriptionId} marked as PastDue after 3 failed payment attempts");

                    // Send dunning email
                    await _emailService.SendSubscriptionDunningAsync(subscription.Customer.Email, subscription);
                }
                else
                {
                    _logger.LogWarning($"Subscription {subscriptionId} billing failed (attempt {subscription.FailedPaymentAttempts}/3): {ex.Message}");
                }
            }

            await _unitOfWork.SubscriptionBillings.AddAsync(billing);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<int> ProcessAllDueBillingsAsync(DateTime date)
        {
            var dueSubscriptions = await _unitOfWork.Subscriptions.GetSubscriptionsDueForBillingAsync(date);
            int processedCount = 0;

            foreach (var subscription in dueSubscriptions)
            {
                try
                {
                    await ProcessBillingAsync(subscription.SubscriptionId);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing billing for subscription {subscription.SubscriptionId}");
                }
            }

            _logger.LogInformation($"Processed {processedCount} subscription billings for date {date:yyyy-MM-dd}");

            return processedCount;
        }

        public async Task<decimal> GetMonthlyRecurringRevenueAsync()
        {
            return await _unitOfWork.Subscriptions.GetMonthlyRecurringRevenueAsync();
        }

        private DateTime CalculateNextBillingDate(CustomerSubscription subscription)
        {
            switch (subscription.Plan.Frequency)
            {
                case SubscriptionFrequency.Monthly:
                    return subscription.NextBillingDate.AddMonths(1);
                case SubscriptionFrequency.Quarterly:
                    return subscription.NextBillingDate.AddMonths(3);
                case SubscriptionFrequency.Annual:
                    return subscription.NextBillingDate.AddYears(1);
                default:
                    return subscription.NextBillingDate.AddMonths(1);
            }
        }

        private async Task<Order> CreateOrderFromSubscriptionAsync(CustomerSubscription subscription, SubscriptionBilling billing)
        {
            // Create order for fulfillment
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                CustomerId = subscription.CustomerId,
                OrderNumber = await GenerateOrderNumberAsync(),
                OrderDate = DateTime.UtcNow,
                OrderStatus = OrderStatus.Pending,
                IsSubscription = true,
                SubscriptionId = subscription.SubscriptionId,
                Subtotal = subscription.CurrentPrice,
                Total = subscription.CurrentPrice,
                CreatedAt = DateTime.UtcNow
            };

            // Add note
            order.Notes = $"Subscription billing cycle {billing.BillingCycleNumber} - {subscription.Plan.PlanName}";

            await _unitOfWork.Orders.AddAsync(order);
            await _unitOfWork.CompleteAsync();

            return order;
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            var count = await _unitOfWork.Orders.GetOrderCountAsync() + 1;
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{count:D5}";
        }

        // Additional methods abbreviated...
    }
}
```

#### Step 5: Create Stripe Integration Service

**Location:** `src/CandleStore.Infrastructure/Services/Payment/`

Create `IStripeService.cs` interface and `StripeService.cs` implementation:

```csharp
using System.Threading.Tasks;
using Stripe;

namespace CandleStore.Infrastructure.Services.Payment
{
    public interface IStripeService
    {
        Task<string> CreateCustomerAsync(string email, string name);
        Task AttachPaymentMethodAsync(string paymentMethodId, string customerId);
        Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId, string paymentMethodId);
        Task<Subscription> CancelSubscriptionAsync(string subscriptionId);
        Task UpdateSubscriptionPaymentMethodAsync(string subscriptionId, string paymentMethodId);
        Task<Charge> ChargeSubscriptionAsync(string customerId, string paymentMethodId, decimal amount, string description);
        Task<Event> ProcessWebhookAsync(string json, string signature);
    }
}
```

Implementation:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace CandleStore.Infrastructure.Services.Payment
{
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeService> _logger;

        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<string> CreateCustomerAsync(string email, string name)
        {
            var service = new CustomerService();
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
                Description = $"Customer for {email}"
            };

            var customer = await service.CreateAsync(options);
            _logger.LogInformation($"Created Stripe customer {customer.Id} for {email}");

            return customer.Id;
        }

        public async Task AttachPaymentMethodAsync(string paymentMethodId, string customerId)
        {
            var service = new PaymentMethodService();
            var options = new PaymentMethodAttachOptions
            {
                Customer = customerId
            };

            await service.AttachAsync(paymentMethodId, options);

            // Set as default payment method
            var customerService = new CustomerService();
            await customerService.UpdateAsync(customerId, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = paymentMethodId
                }
            });

            _logger.LogInformation($"Attached payment method {paymentMethodId} to customer {customerId}");
        }

        public async Task<Subscription> CreateSubscriptionAsync(string customerId, string priceId, string paymentMethodId)
        {
            var service = new SubscriptionService();
            var options = new SubscriptionCreateOptions
            {
                Customer = customerId,
                Items = new List<SubscriptionItemOptions>
                {
                    new SubscriptionItemOptions { Price = priceId }
                },
                DefaultPaymentMethod = paymentMethodId,
                Expand = new List<string> { "latest_invoice.payment_intent" }
            };

            var subscription = await service.CreateAsync(options);
            _logger.LogInformation($"Created Stripe subscription {subscription.Id} for customer {customerId}, price {priceId}");

            return subscription;
        }

        public async Task<Subscription> CancelSubscriptionAsync(string subscriptionId)
        {
            var service = new SubscriptionService();
            var subscription = await service.CancelAsync(subscriptionId);
            _logger.LogInformation($"Cancelled Stripe subscription {subscriptionId}");

            return subscription;
        }

        public async Task UpdateSubscriptionPaymentMethodAsync(string subscriptionId, string paymentMethodId)
        {
            var service = new SubscriptionService();
            var options = new SubscriptionUpdateOptions
            {
                DefaultPaymentMethod = paymentMethodId
            };

            await service.UpdateAsync(subscriptionId, options);
            _logger.LogInformation($"Updated payment method for subscription {subscriptionId} to {paymentMethodId}");
        }

        public async Task<Charge> ChargeSubscriptionAsync(string customerId, string paymentMethodId, decimal amount, string description)
        {
            var service = new ChargeService();
            var options = new ChargeCreateOptions
            {
                Amount = (long)(amount * 100), // Convert to cents
                Currency = "usd",
                Customer = customerId,
                Source = paymentMethodId,
                Description = description
            };

            var charge = await service.CreateAsync(options);
            _logger.LogInformation($"Charged customer {customerId} ${amount} via payment method {paymentMethodId}, charge {charge.Id}");

            return charge;
        }

        public async Task<Event> ProcessWebhookAsync(string json, string signature)
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
                _logger.LogInformation($"Received Stripe webhook: {stripeEvent.Type}");

                return stripeEvent;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook signature verification failed");
                throw;
            }
        }
    }
}
```

#### Step 6: Create Hangfire Background Billing Job

**Location:** `src/CandleStore.Infrastructure/BackgroundJobs/`

Create `SubscriptionBillingJob.cs`:

```csharp
using System;
using System.Threading.Tasks;
using CandleStore.Application.Services.Subscriptions;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace CandleStore.Infrastructure.BackgroundJobs
{
    public class SubscriptionBillingJob
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ILogger<SubscriptionBillingJob> _logger;

        public SubscriptionBillingJob(ISubscriptionService subscriptionService, ILogger<SubscriptionBillingJob> logger)
        {
            _subscriptionService = subscriptionService;
            _logger = logger;
        }

        /// <summary>
        /// Processes all subscription billings due today
        /// Runs daily at 2:00 AM
        /// </summary>
        [AutomaticRetry(Attempts = 3)]
        public async Task ProcessDailyBillingsAsync()
        {
            _logger.LogInformation("Starting daily subscription billing job");

            try
            {
                var today = DateTime.UtcNow.Date;
                var processedCount = await _subscriptionService.ProcessAllDueBillingsAsync(today);

                _logger.LogInformation($"Daily subscription billing job completed. Processed {processedCount} subscriptions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in daily subscription billing job");
                throw; // Hangfire will retry
            }
        }

        /// <summary>
        /// Retries failed payments
        /// Runs daily at 10:00 AM
        /// </summary>
        [AutomaticRetry(Attempts = 2)]
        public async Task RetryFailedPaymentsAsync()
        {
            _logger.LogInformation("Starting retry failed payments job");

            try
            {
                var retriedCount = await _subscriptionService.RetryAllFailedPaymentsAsync();
                _logger.LogInformation($"Retry failed payments job completed. Retried {retriedCount} payments");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in retry failed payments job");
                throw;
            }
        }
    }
}
```

**Register Hangfire jobs in `Program.cs`:**

```csharp
// Configure Hangfire recurring jobs
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

// Daily billing at 2:00 AM
recurringJobManager.AddOrUpdate<SubscriptionBillingJob>(
    "process-subscription-billings",
    job => job.ProcessDailyBillingsAsync(),
    Cron.Daily(2) // 2:00 AM daily
);

// Retry failed payments at 10:00 AM
recurringJobManager.AddOrUpdate<SubscriptionBillingJob>(
    "retry-failed-subscription-payments",
    job => job.RetryFailedPaymentsAsync(),
    Cron.Daily(10) // 10:00 AM daily
);
```

#### Step 7: Create API Controllers

**Location:** `src/CandleStore.Api/Controllers/`

Create `SubscriptionsController.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandleStore.Application.DTOs.Subscriptions;
using CandleStore.Application.Services.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CandleStore.Api.Controllers
{
    [ApiController]
    [Route("api/subscriptions")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly ISubscriptionPlanService _planService;

        public SubscriptionsController(ISubscriptionService subscriptionService, ISubscriptionPlanService planService)
        {
            _subscriptionService = subscriptionService;
            _planService = planService;
        }

        /// <summary>
        /// Get all active subscription plans
        /// </summary>
        [HttpGet("plans")]
        public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetPlans()
        {
            var plans = await _planService.GetActivePlansAsync();
            return Ok(new { success = true, data = plans });
        }

        /// <summary>
        /// Get plan by slug
        /// </summary>
        [HttpGet("plans/{slug}")]
        public async Task<ActionResult<SubscriptionPlanDto>> GetPlanBySlug(string slug)
        {
            var plan = await _planService.GetPlanBySlugAsync(slug);
            if (plan == null)
                return NotFound(new { success = false, message = $"Plan '{slug}' not found" });

            return Ok(new { success = true, data = plan });
        }

        /// <summary>
        /// Create new subscription for customer
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SubscriptionDto>> CreateSubscription([FromBody] CreateSubscriptionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, errors = ModelState });

            var customerId = GetCustomerIdFromClaims();

            try
            {
                var subscription = await _subscriptionService.CreateSubscriptionAsync(customerId, dto);
                return CreatedAtAction(nameof(GetSubscriptionById), new { id = subscription.SubscriptionId },
                    new { success = true, data = subscription });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { success = false, message = $"Payment error: {ex.Message}" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get subscription by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<SubscriptionDto>> GetSubscriptionById(Guid id)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
                return NotFound(new { success = false, message = $"Subscription {id} not found" });

            // Verify ownership
            var customerId = GetCustomerIdFromClaims();
            if (subscription.CustomerId != customerId && !User.IsInRole("Admin"))
                return Forbid();

            return Ok(new { success = true, data = subscription });
        }

        /// <summary>
        /// Get all subscriptions for authenticated customer
        /// </summary>
        [HttpGet("my-subscriptions")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetMySubscriptions()
        {
            var customerId = GetCustomerIdFromClaims();
            var subscriptions = await _subscriptionService.GetCustomerSubscriptionsAsync(customerId);

            return Ok(new { success = true, data = subscriptions });
        }

        /// <summary>
        /// Update subscription preferences
        /// </summary>
        [HttpPut("{id}/preferences")]
        [Authorize]
        public async Task<ActionResult> UpdatePreferences(Guid id, [FromBody] List<SubscriptionPreferenceDto> preferences)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
                return NotFound(new { success = false, message = $"Subscription {id} not found" });

            // Verify ownership
            var customerId = GetCustomerIdFromClaims();
            if (subscription.CustomerId != customerId)
                return Forbid();

            await _subscriptionService.UpdatePreferencesAsync(id, preferences);

            return Ok(new { success = true, message = "Preferences updated successfully" });
        }

        /// <summary>
        /// Update payment method
        /// </summary>
        [HttpPut("{id}/payment-method")]
        [Authorize]
        public async Task<ActionResult<SubscriptionDto>> UpdatePaymentMethod(Guid id, [FromBody] UpdatePaymentMethodDto dto)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
                return NotFound(new { success = false, message = $"Subscription {id} not found" });

            // Verify ownership
            var customerId = GetCustomerIdFromClaims();
            if (subscription.CustomerId != customerId)
                return Forbid();

            try
            {
                var updated = await _subscriptionService.UpdatePaymentMethodAsync(id, dto.StripePaymentMethodId);
                return Ok(new { success = true, data = updated, message = "Payment method updated successfully" });
            }
            catch (StripeException ex)
            {
                return BadRequest(new { success = false, message = $"Payment error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Pause subscription
        /// </summary>
        [HttpPost("{id}/pause")]
        [Authorize]
        public async Task<ActionResult<SubscriptionDto>> PauseSubscription(Guid id)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
                return NotFound(new { success = false, message = $"Subscription {id} not found" });

            // Verify ownership
            var customerId = GetCustomerIdFromClaims();
            if (subscription.CustomerId != customerId)
                return Forbid();

            var paused = await _subscriptionService.PauseSubscriptionAsync(id);
            return Ok(new { success = true, data = paused, message = "Subscription paused successfully" });
        }

        /// <summary>
        /// Resume paused subscription
        /// </summary>
        [HttpPost("{id}/resume")]
        [Authorize]
        public async Task<ActionResult<SubscriptionDto>> ResumeSubscription(Guid id)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
                return NotFound(new { success = false, message = $"Subscription {id} not found" });

            // Verify ownership
            var customerId = GetCustomerIdFromClaims();
            if (subscription.CustomerId != customerId)
                return Forbid();

            var resumed = await _subscriptionService.ResumeSubscriptionAsync(id);
            return Ok(new { success = true, data = resumed, message = "Subscription resumed successfully" });
        }

        /// <summary>
        /// Cancel subscription
        /// </summary>
        [HttpPost("{id}/cancel")]
        [Authorize]
        public async Task<ActionResult<SubscriptionDto>> CancelSubscription(Guid id, [FromBody] CancelSubscriptionDto dto)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
                return NotFound(new { success = false, message = $"Subscription {id} not found" });

            // Verify ownership
            var customerId = GetCustomerIdFromClaims();
            if (subscription.CustomerId != customerId)
                return Forbid();

            try
            {
                var cancelled = await _subscriptionService.CancelSubscriptionAsync(id, dto.Reason);
                return Ok(new { success = true, data = cancelled, message = "Subscription cancelled successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get subscription analytics (admin only)
        /// </summary>
        [HttpGet("analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubscriptionAnalyticsDto>> GetAnalytics()
        {
            var analytics = await _subscriptionService.GetAnalyticsAsync();
            return Ok(new { success = true, data = analytics });
        }

        /// <summary>
        /// Stripe webhook endpoint
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            try
            {
                var stripeEvent = await _stripeService.ProcessWebhookAsync(json, signature);

                // Handle different event types
                switch (stripeEvent.Type)
                {
                    case "invoice.payment_succeeded":
                        await HandleInvoicePaymentSucceeded(stripeEvent);
                        break;
                    case "invoice.payment_failed":
                        await HandleInvoicePaymentFailed(stripeEvent);
                        break;
                    case "customer.subscription.deleted":
                        await HandleSubscriptionDeleted(stripeEvent);
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private Guid GetCustomerIdFromClaims()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            return Guid.Parse(customerIdClaim);
        }

        private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
        {
            // Handle successful payment webhook
            // (implementation details)
        }

        private async Task HandleInvoicePaymentFailed(Event stripeEvent)
        {
            // Handle failed payment webhook
            // (implementation details)
        }

        private async Task HandleSubscriptionDeleted(Event stripeEvent)
        {
            // Handle subscription deleted webhook
            // (implementation details)
        }
    }
}
```

#### Step 8: Create Blazor Customer Subscription Dashboard

**Location:** `src/CandleStore.Storefront/Pages/Account/`

Create `Subscriptions.razor`:

```razor
@page "/account/subscriptions"
@using CandleStore.Application.DTOs.Subscriptions
@inject ISubscriptionService SubscriptionService
@inject NavigationManager NavigationManager
@inject ISnackbar Snackbar
@attribute [Authorize]

<PageTitle>My Subscriptions</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">My Subscriptions</MudText>

    @if (_loading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else if (!_subscriptions.Any())
    {
        <MudPaper Class="pa-8" Elevation="2">
            <MudStack AlignItems="AlignItems.Center" Spacing="3">
                <MudIcon Icon="@Icons.Material.Filled.CardGiftcard" Size="Size.Large" Color="Color.Primary" Style="font-size: 64px;" />
                <MudText Typo="Typo.h6">No Active Subscriptions</MudText>
                <MudText Typo="Typo.body1" Color="Color.Secondary">Start a subscription and get monthly candle deliveries</MudText>
                <MudButton Variant="Variant.Filled" Color="Color.Primary" Href="/subscriptions">Browse Subscription Plans</MudButton>
            </MudStack>
        </MudPaper>
    }
    else
    {
        @foreach (var subscription in _subscriptions)
        {
            <MudCard Class="mb-4">
                <MudCardContent>
                    <MudGrid>
                        <MudItem xs="12" md="8">
                            <MudStack Spacing="2">
                                <MudText Typo="Typo.h6">@subscription.PlanName</MudText>

                                <MudChip Size="Size.Small" Color="GetStatusColor(subscription.Status)">
                                    @subscription.Status
                                </MudChip>

                                <MudText Typo="Typo.body2" Color="Color.Secondary">
                                    <MudIcon Icon="@Icons.Material.Filled.CalendarToday" Size="Size.Small" />
                                    Started: @subscription.StartDate.ToString("MMM dd, yyyy")
                                </MudText>

                                @if (subscription.Status == "Active")
                                {
                                    <MudText Typo="Typo.body2" Color="Color.Secondary">
                                        <MudIcon Icon="@Icons.Material.Filled.Event" Size="Size.Small" />
                                        Next Billing: @subscription.NextBillingDate.ToString("MMM dd, yyyy")
                                    </MudText>
                                }

                                <MudText Typo="Typo.body2">
                                    <strong>$@subscription.CurrentPrice/@GetFrequencyText(subscription.Frequency)</strong>
                                </MudText>

                                <MudText Typo="Typo.body2" Color="Color.Secondary">
                                    @subscription.TotalBillingCycles billing cycles completed
                                </MudText>
                            </MudStack>
                        </MudItem>

                        <MudItem xs="12" md="4">
                            <MudStack Spacing="2">
                                <MudButton Variant="Variant.Outlined" Color="Color.Primary" FullWidth="true" OnClick="() => ViewDetails(subscription.SubscriptionId)">
                                    View Details
                                </MudButton>

                                @if (subscription.Status == "Active")
                                {
                                    <MudButton Variant="Variant.Outlined" Color="Color.Secondary" FullWidth="true" OnClick="() => ManagePreferences(subscription.SubscriptionId)">
                                        Manage Preferences
                                    </MudButton>

                                    <MudButton Variant="Variant.Outlined" FullWidth="true" OnClick="() => UpdatePaymentMethod(subscription.SubscriptionId)">
                                        Update Payment
                                    </MudButton>

                                    <MudButton Variant="Variant.Outlined" Color="Color.Warning" FullWidth="true" OnClick="() => PauseSubscription(subscription.SubscriptionId)">
                                        Pause Subscription
                                    </MudButton>

                                    @if (subscription.CanBeCancelled)
                                    {
                                        <MudButton Variant="Variant.Outlined" Color="Color.Error" FullWidth="true" OnClick="() => OpenCancelDialog(subscription)">
                                            Cancel Subscription
                                        </MudButton>
                                    }
                                    else
                                    {
                                        <MudTooltip Text="@($"Minimum commitment: {subscription.MinCommitmentCycles} billing cycles")">
                                            <MudButton Variant="Variant.Outlined" Color="Color.Error" FullWidth="true" Disabled="true">
                                                Cancel (Locked)
                                            </MudButton>
                                        </MudTooltip>
                                    }
                                }
                                else if (subscription.Status == "Paused")
                                {
                                    <MudButton Variant="Variant.Filled" Color="Color.Success" FullWidth="true" OnClick="() => ResumeSubscription(subscription.SubscriptionId)">
                                        Resume Subscription
                                    </MudButton>
                                }
                            </MudStack>
                        </MudItem>
                    </MudGrid>
                </MudCardContent>
            </MudCard>
        }
    }
</MudContainer>

<MudDialog @bind-IsVisible="_cancelDialogVisible" Options="_dialogOptions">
    <TitleContent>
        <MudText Typo="Typo.h6">Cancel Subscription</MudText>
    </TitleContent>
    <DialogContent>
        <MudStack Spacing="3">
            <MudText>Are you sure you want to cancel your @_selectedSubscription?.PlanName subscription?</MudText>
            <MudText Typo="Typo.body2" Color="Color.Warning">
                You will no longer receive monthly candle deliveries after the current billing period ends on @_selectedSubscription?.NextBillingDate.ToString("MMM dd, yyyy").
            </MudText>

            <MudTextField @bind-Value="_cancellationReason" Label="Reason for cancellation (optional)" Lines="3" Variant="Variant.Outlined" />
        </MudStack>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="CloseCancelDialog">Keep Subscription</MudButton>
        <MudButton Color="Color.Error" Variant="Variant.Filled" OnClick="ConfirmCancel">Confirm Cancellation</MudButton>
    </DialogActions>
</MudDialog>

@code {
    private List<SubscriptionDto> _subscriptions = new();
    private bool _loading = true;
    private bool _cancelDialogVisible = false;
    private SubscriptionDto _selectedSubscription;
    private string _cancellationReason = "";

    private DialogOptions _dialogOptions = new DialogOptions
    {
        MaxWidth = MaxWidth.Small,
        FullWidth = true
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadSubscriptions();
    }

    private async Task LoadSubscriptions()
    {
        _loading = true;
        try
        {
            _subscriptions = (await SubscriptionService.GetMySubscriptionsAsync()).ToList();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading subscriptions: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void ViewDetails(Guid subscriptionId)
    {
        NavigationManager.NavigateTo($"/account/subscriptions/{subscriptionId}");
    }

    private void ManagePreferences(Guid subscriptionId)
    {
        NavigationManager.NavigateTo($"/account/subscriptions/{subscriptionId}/preferences");
    }

    private void UpdatePaymentMethod(Guid subscriptionId)
    {
        NavigationManager.NavigateTo($"/account/subscriptions/{subscriptionId}/payment");
    }

    private async Task PauseSubscription(Guid subscriptionId)
    {
        try
        {
            await SubscriptionService.PauseSubscriptionAsync(subscriptionId);
            Snackbar.Add("Subscription paused successfully", Severity.Success);
            await LoadSubscriptions();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error pausing subscription: {ex.Message}", Severity.Error);
        }
    }

    private async Task ResumeSubscription(Guid subscriptionId)
    {
        try
        {
            await SubscriptionService.ResumeSubscriptionAsync(subscriptionId);
            Snackbar.Add("Subscription resumed successfully", Severity.Success);
            await LoadSubscriptions();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error resuming subscription: {ex.Message}", Severity.Error);
        }
    }

    private void OpenCancelDialog(SubscriptionDto subscription)
    {
        _selectedSubscription = subscription;
        _cancellationReason = "";
        _cancelDialogVisible = true;
    }

    private void CloseCancelDialog()
    {
        _cancelDialogVisible = false;
        _selectedSubscription = null;
    }

    private async Task ConfirmCancel()
    {
        try
        {
            await SubscriptionService.CancelSubscriptionAsync(_selectedSubscription.SubscriptionId, _cancellationReason);
            Snackbar.Add("Subscription cancelled successfully", Severity.Success);
            _cancelDialogVisible = false;
            await LoadSubscriptions();
        }
        catch (InvalidOperationException ex)
        {
            Snackbar.Add(ex.Message, Severity.Warning);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error cancelling subscription: {ex.Message}", Severity.Error);
        }
    }

    private Color GetStatusColor(string status)
    {
        return status switch
        {
            "Active" => Color.Success,
            "Paused" => Color.Warning,
            "Cancelled" => Color.Default,
            "PastDue" => Color.Error,
            _ => Color.Default
        };
    }

    private string GetFrequencyText(string frequency)
    {
        return frequency switch
        {
            "Monthly" => "month",
            "Quarterly" => "quarter",
            "Annual" => "year",
            _ => frequency
        };
    }
}
```

#### Step 9: Create Admin Fulfillment Queue Component

**Location:** `src/CandleStore.Admin/Pages/Subscriptions/`

Create `FulfillmentQueue.razor`:

```razor
@page "/admin/subscriptions/fulfillment"
@using CandleStore.Application.DTOs.Subscriptions
@inject ISubscriptionService SubscriptionService
@inject ISnackbar Snackbar
@attribute [Authorize(Roles = "Admin")]

<PageTitle>Subscription Fulfillment Queue</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraExtraLarge" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">Subscription Fulfillment Queue</MudText>

    <MudCard Class="mb-4">
        <MudCardContent>
            <MudGrid>
                <MudItem xs="12" md="3">
                    <MudPaper Class="pa-4" Elevation="2">
                        <MudText Typo="Typo.h6">Pending Shipments</MudText>
                        <MudText Typo="Typo.h3" Color="Color.Primary">@_pendingCount</MudText>
                    </MudPaper>
                </MudItem>
                <MudItem xs="12" md="3">
                    <MudPaper Class="pa-4" Elevation="2">
                        <MudText Typo="Typo.h6">Ready to Ship</MudText>
                        <MudText Typo="Typo.h3" Color="Color.Success">@_readyCount</MudText>
                    </MudPaper>
                </MudItem>
                <MudItem xs="12" md="3">
                    <MudPaper Class="pa-4" Elevation="2">
                        <MudText Typo="Typo.h6">Shipped Today</MudText>
                        <MudText Typo="Typo.h3">@_shippedTodayCount</MudText>
                    </MudPaper>
                </MudItem>
                <MudItem xs="12" md="3">
                    <MudPaper Class="pa-4" Elevation="2">
                        <MudText Typo="Typo.h6">MRR</MudText>
                        <MudText Typo="Typo.h3" Color="Color.Tertiary">$@_mrr.ToString("N2")</MudText>
                    </MudPaper>
                </MudItem>
            </MudGrid>
        </MudCardContent>
    </MudCard>

    <MudCard>
        <MudCardContent>
            <MudTable Items="@_shipments" Hover="true" Breakpoint="Breakpoint.Sm" Loading="@_loading" LoadingProgressColor="Color.Info">
                <HeaderContent>
                    <MudTh>Shipment #</MudTh>
                    <MudTh>Customer</MudTh>
                    <MudTh>Plan</MudTh>
                    <MudTh>Scheduled Ship Date</MudTh>
                    <MudTh>Status</MudTh>
                    <MudTh>Products</MudTh>
                    <MudTh>Actions</MudTh>
                </HeaderContent>
                <RowTemplate>
                    <MudTd DataLabel="Shipment">#@context.ShipmentNumber</MudTd>
                    <MudTd DataLabel="Customer">@context.CustomerName</MudTd>
                    <MudTd DataLabel="Plan">@context.PlanName</MudTd>
                    <MudTd DataLabel="Ship Date">@context.ScheduledShipDate.ToString("MMM dd, yyyy")</MudTd>
                    <MudTd DataLabel="Status">
                        <MudChip Size="Size.Small" Color="GetStatusColor(context.Status)">@context.Status</MudChip>
                    </MudTd>
                    <MudTd DataLabel="Products">
                        @if (context.Status == "Pending")
                        {
                            <MudButton Size="Size.Small" OnClick="() => CurateProducts(context.ShipmentId)">Curate Products</MudButton>
                        }
                        else
                        {
                            <MudText Typo="Typo.body2">@context.ProductCount products</MudText>
                        }
                    </MudTd>
                    <MudTd DataLabel="Actions">
                        @if (context.Status == "ReadyToShip")
                        {
                            <MudButton Size="Size.Small" Color="Color.Success" OnClick="() => MarkAsShipped(context.ShipmentId)">
                                Mark Shipped
                            </MudButton>
                        }
                    </MudTd>
                </RowTemplate>
            </MudTable>
        </MudCardContent>
    </MudCard>
</MudContainer>

@code {
    private List<ShipmentDto> _shipments = new();
    private int _pendingCount = 0;
    private int _readyCount = 0;
    private int _shippedTodayCount = 0;
    private decimal _mrr = 0;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        _loading = true;
        try
        {
            _shipments = (await SubscriptionService.GetPendingShipmentsAsync()).ToList();
            _pendingCount = _shipments.Count(s => s.Status == "Pending");
            _readyCount = _shipments.Count(s => s.Status == "ReadyToShip");
            _shippedTodayCount = _shipments.Count(s => s.Status == "Shipped" && s.ActualShipDate?.Date == DateTime.Today);
            _mrr = await SubscriptionService.GetMonthlyRecurringRevenueAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading data: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private void CurateProducts(Guid shipmentId)
    {
        // Navigate to product curation page
    }

    private async Task MarkAsShipped(Guid shipmentId)
    {
        try
        {
            await SubscriptionService.MarkShipmentAsShippedAsync(shipmentId);
            Snackbar.Add("Shipment marked as shipped", Severity.Success);
            await LoadData();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private Color GetStatusColor(string status)
    {
        return status switch
        {
            "Pending" => Color.Warning,
            "Curated" => Color.Info,
            "ReadyToShip" => Color.Success,
            "Shipped" => Color.Default,
            _ => Color.Default
        };
    }
}
```

#### Step 10: Create Email Templates

**Location:** `src/CandleStore.Infrastructure/Services/Email/`

Add email extension methods to `EmailServiceExtensions.cs`:

```csharp
using System.Threading.Tasks;
using CandleStore.Domain.Entities.Subscriptions;
using CandleStore.Infrastructure.Services.Email;

namespace CandleStore.Infrastructure.Services.Email
{
    public static class SubscriptionEmailExtensions
    {
        /// <summary>
        /// Send subscription confirmation email
        /// </summary>
        public static async Task SendSubscriptionConfirmationAsync(
            this IEmailService emailService,
            string toEmail,
            CustomerSubscription subscription,
            SubscriptionPlan plan)
        {
            var subject = $"Welcome to {plan.PlanName}!";

            var htmlBody = $@"
                <h2>Your Subscription is Active!</h2>
                <p>Hi there,</p>
                <p>Thank you for subscribing to <strong>{plan.PlanName}</strong>!</p>

                <h3>Subscription Details:</h3>
                <ul>
                    <li><strong>Plan:</strong> {plan.PlanName}</li>
                    <li><strong>Price:</strong> ${plan.Price}/{plan.Frequency}</li>
                    <li><strong>Next Billing Date:</strong> {subscription.NextBillingDate:MMM dd, yyyy}</li>
                    <li><strong>Candles per Shipment:</strong> {plan.CandleCount}</li>
                </ul>

                <h3>What Happens Next?</h3>
                <p>We'll curate {plan.CandleCount} amazing candles based on your preferences and ship them to you each {plan.Frequency.ToString().ToLower()}.
                You can update your preferences anytime in your account dashboard.</p>

                <p>Your first shipment will arrive within 5-7 business days!</p>

                <p><a href=""https://candlestore.com/account/subscriptions"" style=""background-color: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px;"">
                    Manage Subscription
                </a></p>

                <p>Questions? Reply to this email or contact us at support@candlestore.com</p>

                <p>Welcome to the candle club! 🕯️</p>
            ";

            await emailService.SendEmailAsync(toEmail, subject, htmlBody);
        }

        /// <summary>
        /// Send dunning email (payment failed notification)
        /// </summary>
        public static async Task SendSubscriptionDunningAsync(
            this IEmailService emailService,
            string toEmail,
            CustomerSubscription subscription)
        {
            var subject = "Payment Issue - Action Required for Your Subscription";

            var htmlBody = $@"
                <h2>Payment Issue with Your Subscription</h2>
                <p>Hi there,</p>
                <p>We were unable to process your payment for your <strong>{subscription.Plan.PlanName}</strong> subscription.</p>

                <p><strong>Failed Payment Attempt:</strong> {subscription.FailedPaymentAttempts} of 3</p>

                <h3>What You Need to Do:</h3>
                <p>Please update your payment method to avoid service interruption. We'll automatically retry your payment in 3 days.</p>

                <p>If payment fails 3 times, your subscription will be paused and no further shipments will be sent.</p>

                <p><a href=""https://candlestore.com/account/subscriptions/{subscription.SubscriptionId}/payment"" style=""background-color: #FF9800; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px;"">
                    Update Payment Method
                </a></p>

                <p>Common reasons for payment failure:</p>
                <ul>
                    <li>Expired credit card</li>
                    <li>Insufficient funds</li>
                    <li>Bank declined the charge</li>
                </ul>

                <p>Questions? Contact us at support@candlestore.com</p>
            ";

            await emailService.SendEmailAsync(toEmail, subject, htmlBody);
        }

        /// <summary>
        /// Send subscription cancellation confirmation
        /// </summary>
        public static async Task SendSubscriptionCancellationAsync(
            this IEmailService emailService,
            string toEmail,
            CustomerSubscription subscription)
        {
            var subject = "Subscription Cancelled - We'll Miss You!";

            var htmlBody = $@"
                <h2>Your Subscription Has Been Cancelled</h2>
                <p>Hi there,</p>
                <p>We're sorry to see you go! Your <strong>{subscription.Plan.PlanName}</strong> subscription has been cancelled.</p>

                <h3>Cancellation Details:</h3>
                <ul>
                    <li><strong>Cancelled On:</strong> {subscription.CancelledAt:MMM dd, yyyy}</li>
                    <li><strong>Billing Cycles Completed:</strong> {subscription.TotalBillingCycles}</li>
                    <li><strong>Final Billing Date:</strong> {subscription.LastBillingDate:MMM dd, yyyy}</li>
                </ul>

                <p>You will not be charged again, and no further shipments will be sent.</p>

                {(string.IsNullOrEmpty(subscription.CancellationReason) ? "" : $@"
                <h3>Reason for Cancellation:</h3>
                <p><em>{subscription.CancellationReason}</em></p>
                ")}

                <p>We'd love to have you back! Use code <strong>WELCOME15</strong> for 15% off if you decide to resubscribe within 30 days.</p>

                <p><a href=""https://candlestore.com/subscriptions"" style=""background-color: #2196F3; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px;"">
                    View Subscription Plans
                </a></p>

                <p>Thank you for being a subscriber! We hope to see you again soon. 🕯️</p>
            ";

            await emailService.SendEmailAsync(toEmail, subject, htmlBody);
        }

        /// <summary>
        /// Send monthly shipment notification
        /// </summary>
        public static async Task SendSubscriptionShipmentNotificationAsync(
            this IEmailService emailService,
            string toEmail,
            CustomerSubscription subscription,
            Order order)
        {
            var subject = $"Your {subscription.Plan.PlanName} Box is On Its Way!";

            var htmlBody = $@"
                <h2>Your Monthly Candle Box Has Shipped!</h2>
                <p>Hi there,</p>
                <p>Great news! Your <strong>{subscription.Plan.PlanName}</strong> box for this month has been shipped and is on its way to you!</p>

                <h3>Shipment Details:</h3>
                <ul>
                    <li><strong>Order Number:</strong> {order.OrderNumber}</li>
                    <li><strong>Ship Date:</strong> {order.ShippedDate:MMM dd, yyyy}</li>
                    <li><strong>Tracking Number:</strong> {order.TrackingNumber}</li>
                    <li><strong>Estimated Delivery:</strong> {order.EstimatedDeliveryDate:MMM dd, yyyy}</li>
                </ul>

                <p><a href=""https://candlestore.com/orders/{order.OrderId}/track"" style=""background-color: #4CAF50; color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px;"">
                    Track Your Shipment
                </a></p>

                <h3>This Month's Candles:</h3>
                <p>We've curated {subscription.Plan.CandleCount} amazing scents for you based on your preferences. Enjoy!</p>

                <p>Questions about your shipment? Contact us at support@candlestore.com</p>

                <p>Happy burning! 🕯️</p>
            ";

            await emailService.SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}
```

#### Step 11: Create MRR Analytics Dashboard Component

**Location:** `src/CandleStore.Admin/Pages/Subscriptions/`

Create `Analytics.razor`:

```razor
@page "/admin/subscriptions/analytics"
@using CandleStore.Application.DTOs.Subscriptions
@inject ISubscriptionService SubscriptionService
@inject ISnackbar Snackbar
@attribute [Authorize(Roles = "Admin")]

<PageTitle>Subscription Analytics</PageTitle>

<MudContainer MaxWidth="MaxWidth.ExtraExtraLarge" Class="mt-4">
    <MudText Typo="Typo.h4" Class="mb-4">Subscription Analytics</MudText>

    @if (_loading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else if (_analytics != null)
    {
        <MudGrid>
            <MudItem xs="12" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.h6">Monthly Recurring Revenue</MudText>
                        <MudText Typo="Typo.h3" Color="Color.Primary">$@_analytics.MRR.ToString("N2")</MudText>
                        <MudText Typo="Typo.body2" Color="Color.Success">
                            +@_analytics.MRRGrowthPercentage.ToString("N1")% vs last month
                        </MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>

            <MudItem xs="12" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.h6">Active Subscriptions</MudText>
                        <MudText Typo="Typo.h3">@_analytics.ActiveSubscriptionCount</MudText>
                        <MudText Typo="Typo.body2" Color="Color.Secondary">
                            @_analytics.NewSubscriptionsThisMonth new this month
                        </MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>

            <MudItem xs="12" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.h6">Churn Rate</MudText>
                        <MudText Typo="Typo.h3" Color="Color.Warning">@_analytics.ChurnRate.ToString("N1")%</MudText>
                        <MudText Typo="Typo.body2" Color="Color.Secondary">
                            @_analytics.CancelledThisMonth cancelled this month
                        </MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>

            <MudItem xs="12" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.h6">Average LTV</MudText>
                        <MudText Typo="Typo.h3" Color="Color.Tertiary">$@_analytics.AverageLifetimeValue.ToString("N2")</MudText>
                        <MudText Typo="Typo.body2" Color="Color.Secondary">
                            Avg @_analytics.AverageSubscriptionLengthMonths months
                        </MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>

            <MudItem xs="12">
                <MudCard>
                    <MudCardHeader>
                        <MudText Typo="Typo.h6">MRR Trend (Last 12 Months)</MudText>
                    </MudCardHeader>
                    <MudCardContent>
                        <MudChart ChartType="ChartType.Line" ChartSeries="@_mrrSeries" XAxisLabels="@_xAxisLabels" Width="100%" Height="350px" />
                    </MudCardContent>
                </MudCard>
            </MudItem>

            <MudItem xs="12" md="6">
                <MudCard>
                    <MudCardHeader>
                        <MudText Typo="Typo.h6">Subscription Plans Distribution</MudText>
                    </MudCardHeader>
                    <MudCardContent>
                        <MudChart ChartType="ChartType.Donut" InputData="@_planDistribution" InputLabels="@_planLabels" Width="300px" Height="300px" />
                    </MudCardContent>
                </MudCard>
            </MudItem>

            <MudItem xs="12" md="6">
                <MudCard>
                    <MudCardHeader>
                        <MudText Typo="Typo.h6">Top Cancellation Reasons</MudText>
                    </MudCardHeader>
                    <MudCardContent>
                        <MudSimpleTable>
                            <thead>
                                <tr>
                                    <th>Reason</th>
                                    <th>Count</th>
                                    <th>Percentage</th>
                                </tr>
                            </thead>
                            <tbody>
                                @foreach (var reason in _analytics.CancellationReasons)
                                {
                                    <tr>
                                        <td>@reason.Reason</td>
                                        <td>@reason.Count</td>
                                        <td>@reason.Percentage.ToString("N1")%</td>
                                    </tr>
                                }
                            </tbody>
                        </MudSimpleTable>
                    </MudCardContent>
                </MudCard>
            </MudItem>
        </MudGrid>
    }
</MudContainer>

@code {
    private SubscriptionAnalyticsDto _analytics;
    private bool _loading = true;

    private List<ChartSeries> _mrrSeries = new();
    private string[] _xAxisLabels;
    private double[] _planDistribution;
    private string[] _planLabels;

    protected override async Task OnInitializedAsync()
    {
        await LoadAnalytics();
    }

    private async Task LoadAnalytics()
    {
        _loading = true;
        try
        {
            _analytics = await SubscriptionService.GetAnalyticsAsync();

            // Prepare chart data
            _mrrSeries = new List<ChartSeries>
            {
                new ChartSeries
                {
                    Name = "MRR",
                    Data = _analytics.MRRHistory.Select(m => (double)m.Amount).ToArray()
                }
            };

            _xAxisLabels = _analytics.MRRHistory.Select(m => m.Month).ToArray();

            _planDistribution = _analytics.PlanDistribution.Select(p => (double)p.Count).ToArray();
            _planLabels = _analytics.PlanDistribution.Select(p => p.PlanName).ToArray();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading analytics: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }
}
```

#### Step 12: Create Database Migration

**Run migration commands:**

```bash
# Add migration
dotnet ef migrations add AddSubscriptionSystem --project src/CandleStore.Infrastructure --startup-project src/CandleStore.Api

# Review generated migration, then apply
dotnet ef database update --project src/CandleStore.Infrastructure --startup-project src/CandleStore.Api
```

**Migration creates tables:**
- SubscriptionPlans
- CustomerSubscriptions
- SubscriptionPreferences
- SubscriptionBillings
- SubscriptionShipments

**With indexes on:**
- CustomerSubscriptions.StripeSubscriptionId (unique)
- CustomerSubscriptions.Status, NextBillingDate (for billing job)
- SubscriptionBillings.Status, NextRetryDate (for retry job)

**Seed sample subscription plans:**

```csharp
public static async Task SeedSubscriptionPlansAsync(ApplicationDbContext context)
{
    if (!context.SubscriptionPlans.Any())
    {
        var plans = new[]
        {
            new SubscriptionPlan
            {
                PlanId = Guid.NewGuid(),
                PlanName = "Monthly Candle Box",
                Slug = "monthly-candle-box",
                Description = "Get 3 hand-poured candles delivered to your door every month",
                Frequency = SubscriptionFrequency.Monthly,
                Price = 45.00m,
                StripePriceId = "price_monthly_3candles", // Replace with actual Stripe price ID
                CandleCount = 3,
                AllowCustomization = true,
                MinCommitmentCycles = 2,
                DiscountPercentage = 15m,
                IsActive = true,
                DisplayOrder = 1,
                Features = "3 full-size candles,Free shipping,Scent customization,Cancel anytime (after 2 months)",
                ImageUrl = "/images/subscriptions/monthly-box.jpg"
            },
            new SubscriptionPlan
            {
                PlanId = Guid.NewGuid(),
                PlanName = "Quarterly Sampler",
                Slug = "quarterly-sampler",
                Description = "Explore new scents with 5 candles every 3 months",
                Frequency = SubscriptionFrequency.Quarterly,
                Price = 120.00m,
                StripePriceId = "price_quarterly_5candles",
                CandleCount = 5,
                AllowCustomization = true,
                MinCommitmentCycles = 1,
                DiscountPercentage = 20m,
                IsActive = true,
                DisplayOrder = 2,
                Features = "5 full-size candles,Free shipping,Seasonal scents,20% off retail",
                ImageUrl = "/images/subscriptions/quarterly-box.jpg"
            }
        };

        await context.SubscriptionPlans.AddRangeAsync(plans);
        await context.SaveChangesAsync();
    }
}
```

---

**END OF TASK 044**
