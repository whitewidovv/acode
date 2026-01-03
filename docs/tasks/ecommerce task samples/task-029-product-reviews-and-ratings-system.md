# Task 029: Product Reviews and Ratings System

**Priority:** 29 / 63
**Tier:** B (Nice-to-Have)
**Complexity:** 13 Fibonacci points
**Phase:** Phase 10 - Advanced Features
**Dependencies:** Task 002 (Domain Models), Task 014 (Customer Accounts), Task 016 (Order Confirmation)

---

## Description

**Product Reviews and Ratings System** enables customers to share authentic feedback about purchased products through star ratings (1-5), written reviews, and photo uploads. This user-generated content builds trust with prospective buyers, improves product discovery, and provides valuable feedback to store owners about product quality and customer satisfaction. The system includes moderation workflows, verified purchase badges, helpful vote counting, and store owner response capabilities.

**Business Value:** Reviews are one of the highest-converting elements in e-commerce, with 93% of consumers reading reviews before purchase decisions (Spiegel Research). For candle stores where scent and quality cannot be evaluated online, authentic reviews reduce purchase hesitation. Products with reviews see 18% higher conversion rates and 12% higher average order values. Verified purchase badges increase trust by 35%. The system also provides actionable feedback: if 15+ customers mention "weak scent throw," the store owner can reformulate that product. For a candle store averaging 400 orders/month, implementing reviews can generate **$14,400 additional annual revenue** from improved conversion rates (3% increase on $40k monthly revenue) plus **$8,400 from reduced returns** (authentic feedback reduces expectation mismatches).

**Technical Approach:** The system introduces **Review** entity with star rating (1-5), title, content, optional photo URLs (stored in CDN), verified purchase flag (cross-referenced with order history), approval status, and helpful vote count. **ReviewVote** entity tracks which customers/sessions found reviews helpful (prevents duplicate voting). **ReviewResponse** entity enables store owners to respond to reviews publicly (addressing concerns, thanking customers). Reviews default to "pending approval" status, appearing in admin moderation queue. Once approved, reviews display on product detail pages with filtering (by star rating), sorting (most recent, highest rated, most helpful), and pagination. The system calculates aggregate rating (average of all approved reviews) and rating distribution (count per star level) displayed prominently on product pages. Automated emails request reviews 7 days post-delivery. Performance optimization includes database indexing on `(ProductId, IsApproved, CreatedAt)` for fast product page queries and caching of review summaries (average rating, total count) in Redis.

**Integration Points:** Integrates with Task 002 (Product entity) adding navigation property for reviews. Integrates with Task 014 (Customer Accounts) to attribute reviews to customers and verify purchases. Integrates with Task 016 (Order Confirmation) to trigger review request emails 7 days post-delivery. Integrates with Task 046 (Loyalty Program) to award points for leaving reviews (incentivizes participation). Integrates with CDN/image upload service for review photo storage. Admin panel (Task 006) gains moderation queue UI for approving/rejecting reviews and posting responses. Product detail page (Task 005) displays review summary, ratings distribution, and paginated review list.

**Constraints:** Review submission restricted to logged-in customers only (prevents spam). Verified purchase badge only applies if customer actually purchased the product (OrderItem.ProductId matches, order status = Delivered). Customers can only submit one review per product (prevents duplicate reviews from same person). Photo uploads limited to 3 images per review, max 5MB each (prevents abuse). Reviews longer than 2000 characters truncated. Inappropriate content filtered via profanity detection before moderation. Helpful voting rate-limited to prevent manipulation (1 vote per review per session/customer). Store owner responses limited to 1 per review. Review editing disabled after approval (preserves authenticity - customers can delete and resubmit). Performance: product page with 500+ reviews must load in <2 seconds using pagination.

---

## Use Cases

### Use Case 1: Alex (Customer) Leaves Verified Review After Purchase

**Without Reviews:** Alex purchases "Lavender Dreams" candle but cannot share feedback about the product. Other potential customers browsing the product page see no social proof, leading to 22% abandon rate (industry average without reviews). Alex wants to warn others that the scent is subtle (not strong throw), but has no platform to do so. Store owner doesn't receive feedback about scent throw issues affecting 30% of customers.

**With Reviews System:** Seven days after Alex's order is delivered, Alex receives automated email "How was your Lavender Dreams candle?" with "Write a Review" button. Alex clicks through to review form, rates 4 stars, titles review "Nice scent but subtle throw," writes "The lavender scent is pleasant and natural, but you need to be close to the candle to smell it. Great for a bedroom but not large living spaces." Uploads photo of candle burning. Submits review. Store owner sees review in **Admin → Reviews → Pending** moderation queue, approves it within 2 hours. Review appears on product page with "✓ Verified Purchase" badge. Over next month, 12 other customers leave similar "subtle scent" feedback. Store owner reformulates the product with 15% more fragrance oil. New reviews average 4.7 stars with improved scent throw comments. **Result:** Store improved product quality based on customer feedback, newer customers make informed purchase decisions (conversion rate increased 18%), authentic reviews reduced returns by 15% ($840 annual savings from fewer "not as expected" returns).

### Use Case 2: Sarah (Store Owner) Responds to Negative Review Publicly

**Without Review Response:** A customer leaves 2-star review saying "Candle arrived broken, very disappointed." Sarah has no way to publicly address this, making all potential customers see the negative review without context. Sarah lost sale due to shipping damage (not product quality issue) but it damages brand reputation for all future visitors.

**With Review Response:** Sarah sees 2-star review from Jordan in moderation queue: "Candle arrived broken in pieces. Glass jar shattered. Very disappointed." Sarah approves the review (authentic feedback) then clicks **Respond to Review**. Types response: "We're so sorry your candle arrived damaged! This is a shipping issue, not our product quality. We've sent you a replacement via expedited shipping and improved our packaging. We stand behind our products 100%." Response appears below Jordan's review with "Response from Store Owner" badge and timestamp. Future customers see the negative review BUT also see Sarah's professional, caring response and commitment to customer service. Conversion rate on this product drops only 2% (vs 8% with unaddressed negative review). Jordan updates review to 5 stars after receiving replacement: "UPDATE: Store owner made it right immediately. Replacement candle is beautiful and smells amazing. Great customer service!" **Result:** Turned negative review into positive brand impression, demonstrated excellent customer service publicly, prevented $3,200 annual revenue loss from unaddressed negativity.

### Use Case 3: Maria (Marketing Manager) Leverages Reviews for Social Proof Campaign

**Without Review Analytics:** Maria wants to create marketing campaign highlighting customer satisfaction but has no structured review data. She manually asks customers for testimonials via email (10% response rate), gets generic praise, and struggles to quantify satisfaction metrics.

**With Review System and Analytics:** Maria navigates to **Admin → Reports → Review Analytics**. Sees aggregate data: 427 total reviews, 4.6 average rating, 89% 4-5 star reviews. Filters reviews by 5-star ratings and products tagged "bestseller." Exports CSV of 50 best reviews for "Vanilla Bourbon" candle. Finds quote: "This is the only candle that actually smells like real vanilla beans, not fake vanilla extract. Burns for 60+ hours!" Maria uses this in Facebook ad campaign with headline "4.8 Stars from 500+ Customers." Includes user-submitted review photos in ad creative (after obtaining permission). Selects "Most Helpful" reviews highlighting specific benefits (long burn time, authentic scents, clean burn). Campaign performs 40% better than generic ads without social proof. Maria also identifies pain points from 3-star reviews: "Shipping took 2 weeks" appears 12 times. Maria works with operations to improve shipping times. **Result:** Leveraged authentic customer feedback for high-performing marketing campaign ($6,700 additional revenue from campaign), identified operational improvements from review patterns, built trust with prospective customers through transparent ratings display.

---
## User Manual Documentation

### Overview

The **Product Reviews and Ratings System** allows customers to share feedback about purchased products through star ratings, written reviews, and photo uploads. Store owners can moderate reviews, respond publicly, and gain insights into product quality and customer satisfaction.

**Key Features:**
- 5-star rating system with half-star display precision
- Written reviews with title and detailed content (up to 2000 characters)
- Photo uploads (up to 3 images per review)
- Verified Purchase badges for authenticated buyers
- Helpful vote system (customers vote if review was useful)
- Moderation queue for admin approval/rejection
- Store owner public responses
- Automated review request emails (7 days post-delivery)
- Review filtering (by star rating) and sorting (recent, highest, helpful)
- Aggregate rating and distribution display

---

### Section 1: Customer Review Submission

#### Submitting a Review

1. **Navigate to Product Detail Page** for a product you've purchased
2. Scroll to **Customer Reviews** section
3. Click **Write a Review** button
4. Complete the review form:

**Review Form Fields:**
- **Star Rating:** Click 1-5 stars (required)
- **Review Title:** Brief headline (e.g., "Amazing scent and long burn time") - max 100 characters
- **Review Content:** Detailed feedback (required, 50-2000 characters)
- **Upload Photos:** Optional - add up to 3 photos showing product, packaging, or candle burning
- **Terms Acceptance:** Check box confirming review follows community guidelines

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ WRITE A REVIEW: Lavender Dreams 8oz Candle                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│ Your Rating: ★ ★ ★ ★ ★ (5 stars)                                            │
│                                                                               │
│ Review Title:                                                                 │
│ ┌───────────────────────────────────────────────────────────────────────┐   │
│ │ Perfect for relaxation                                                 │   │
│ └───────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
│ Your Review:                                                                  │
│ ┌───────────────────────────────────────────────────────────────────────┐   │
│ │ This candle exceeded my expectations. The lavender scent is natural   │   │
│ │ and calming without being overpowering. Burns evenly for hours and    │   │
│ │ fills my bedroom with a spa-like aroma. Already ordering more!        │   │
│ │                                                                         │   │
│ │ Character count: 245 / 2000                                            │   │
│ └───────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
│ Add Photos (Optional):                                                       │
│ [Upload Photo 1]  [Upload Photo 2]  [Upload Photo 3]                         │
│                                                                               │
│ ☑ I agree to the review guidelines (no profanity, spam, or promotional       │
│   content)                                                                    │
│                                                                               │
│ ✓ Verified Purchase Badge: You purchased this product on March 15, 2024     │
│                                                                               │
│ [Submit Review]  [Cancel]                                                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

5. Click **Submit Review**
6. **Confirmation message** appears: "Thank you! Your review is pending approval and will appear shortly."
7. Review enters moderation queue (typically approved within 24 hours)

**Review Guidelines:**
- Be honest and authentic - share your genuine experience
- Focus on product quality (scent, burn time, packaging, value)
- Avoid profanity, personal attacks, or promotional content
- Don't include personal information (addresses, phone numbers)
- Photos should show the product, not unrelated content

**Verified Purchase Badge:**
- If you purchased this product from this store, your review displays "✓ Verified Purchase" badge
- Increases credibility and trustworthiness of your review
- Only applies to products you've actually ordered (cross-checked with order history)

---

### Section 2: Admin Review Moderation

#### Accessing the Moderation Queue

1. Log into Admin Panel
2. Navigate to **Admin → Reviews → Pending Reviews**
3. View list of unmoderated reviews sorted by submission date:

```
═══════════════════════════════════════════════════════════════════════════════
REVIEW MODERATION QUEUE
═══════════════════════════════════════════════════════════════════════════════

Showing 12 pending reviews

┌─────────────────────────────────────────────────────────────────────────────┐
│ ⭐⭐⭐⭐⭐ Lavender Dreams 8oz Candle                    Submitted: 2 hours ago│
│ ✓ Verified Purchase                                                          │
│                                                                               │
│ Title: "Perfect for relaxation"                                              │
│ Review: This candle exceeded my expectations. The lavender scent is natural  │
│ and calming without being overpowering. Burns evenly for hours...           │
│                                                                               │
│ Customer: Alex Chen (alex@example.com)                                       │
│ Photos: 2 uploaded                                                           │
│ [View Photos]                                                                │
│                                                                               │
│ Content Check: ✅ No profanity  ✅ No spam  ✅ Follows guidelines            │
│                                                                               │
│ [Approve]  [Reject]  [View Full Review]                                      │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│ ⭐⭐ Vanilla Bourbon 12oz Candle                        Submitted: 5 hours ago│
│ ✓ Verified Purchase                                                          │
│                                                                               │
│ Title: "Candle arrived broken"                                               │
│ Review: Very disappointed. The glass jar was shattered when it arrived...    │
│                                                                               │
│ Customer: Jordan Smith (jordan@example.com)                                  │
│ Photos: 1 uploaded (damaged product)                                         │
│ [View Photos]                                                                │
│                                                                               │
│ Content Check: ✅ No profanity  ✅ Legitimate complaint                      │
│                                                                               │
│ [Approve]  [Reject]  [View Full Review]                                      │
└─────────────────────────────────────────────────────────────────────────────┘

⚠️ 1 review flagged for potential profanity (manual review recommended)

[Approve All Clean Reviews]  [Export Queue]
```

#### Approving Reviews

1. Click **View Full Review** to read complete content
2. Review uploaded photos (if any) - ensure appropriate content
3. Verify review follows community guidelines:
   - No profanity or offensive language
   - No spam or promotional content
   - No personal attacks on staff/brand
   - Relevant to the product
4. Click **Approve** to publish review on product page
5. Review immediately appears on storefront with timestamp

**Approval Criteria:**
- ✅ **Approve:** Honest feedback (positive or negative) about product quality, scent, burn time, value
- ✅ **Approve:** Legitimate complaints about shipping damage, quality issues (these are valuable feedback)
- ❌ **Reject:** Profanity, hate speech, personal attacks
- ❌ **Reject:** Spam, promotional content, competitor mentions
- ❌ **Reject:** Off-topic content unrelated to the product
- ❌ **Reject:** Reviews from customers who didn't purchase (fraudulent)

#### Rejecting Reviews

1. Click **Reject** button
2. Select rejection reason from dropdown:
   - Profanity/Inappropriate Language
   - Spam/Promotional Content
   - Off-Topic/Not Product Related
   - Fraudulent (No Purchase Record)
   - Personal Attack
   - Other (specify)
3. Optionally add internal notes (not visible to customer)
4. Click **Confirm Rejection**
5. Customer receives email (optional): "Your review did not meet our community guidelines"

---

### Section 3: Responding to Reviews

#### Store Owner Response Workflow

1. Navigate to **Admin → Reviews → All Reviews**
2. Find review to respond to (often negative reviews requiring clarification)
3. Click **Respond** button
4. Type response in text area (up to 500 characters)

**Response Guidelines:**
- Address customer concern directly and professionally
- Apologize for negative experiences (shipping damage, quality issues)
- Explain resolution (replacement sent, refund issued, improved process)
- Thank customers for positive reviews
- Keep tone friendly, empathetic, solution-oriented

**Example Responses:**

**For Negative Review (Shipping Damage):**
```
"We're so sorry your candle arrived damaged! This is unacceptable. We've sent a replacement 
via expedited shipping and reinforced our packaging to prevent future issues. Your satisfaction 
is our priority. - Sarah, Store Owner"
```

**For Positive Review:**
```
"Thank you so much for the wonderful feedback, Alex! We're thrilled you love the Lavender Dreams
scent. Customers like you make our small business thrive! - Sarah"
```

**For Constructive Criticism:**
```
"Thank you for the honest feedback about scent throw. We've reformulated this candle with 15% 
more fragrance oil based on customer input like yours. We'd love to send you a sample of the 
new version to try! Please contact support@candlestore.com"
```

5. Click **Post Response**
6. Response appears below review with "Response from Store Owner" badge
7. Customer receives email notification of your response

**Response Best Practices:**
- Respond within 24-48 hours (shows attentiveness)
- Always maintain professional, friendly tone even with harsh reviews
- Use customer's name when possible (personalization)
- Offer solutions, not excuses
- Turn negative reviews into opportunities to demonstrate excellent customer service
- Public responses show future customers you care about satisfaction

---

### Section 4: Review Display on Product Pages

#### Customer-Facing Review Display

Product detail pages show reviews in dedicated section:

```
═══════════════════════════════════════════════════════════════════════════════
CUSTOMER REVIEWS
═══════════════════════════════════════════════════════════════════════════════

┌────────────────────────────────────────┐
│      OVERALL RATING                    │
│                                        │
│           4.6 ⭐⭐⭐⭐⭐               │
│     Based on 137 reviews              │
│                                        │
│  [Write a Review]                      │
└────────────────────────────────────────┘

RATING DISTRIBUTION:
┌────────────────────────────────────────────────────────────────────────────┐
│ 5 stars  ████████████████████████████████████░░░░░░  82 (60%)              │
│ 4 stars  ████████████████████░░░░░░░░░░░░░░░░░░░░░  38 (28%)              │
│ 3 stars  ████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  12 (9%)               │
│ 2 stars  ██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   3 (2%)               │
│ 1 star   █░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   2 (1%)               │
└────────────────────────────────────────────────────────────────────────────┘

FILTER BY: [All Stars ▼] | SORT BY: [Most Recent ▼]

─────────────────────────────────────────────────────────────────────────────

⭐⭐⭐⭐⭐  "Perfect for relaxation"                    ✓ Verified Purchase
Alex Chen - March 22, 2024

This candle exceeded my expectations. The lavender scent is natural and calming 
without being overpowering. Burns evenly for hours and fills my bedroom with a 
spa-like aroma. Already ordering more!

📷 📷  [View 2 photos]

Was this review helpful?  👍 Yes (23)  👎 No (1)

   ↳ Response from Store Owner (March 22, 2024):
     Thank you so much for the wonderful feedback, Alex! We're thrilled you love 
     the Lavender Dreams scent. - Sarah

─────────────────────────────────────────────────────────────────────────────

⭐⭐⭐⭐  "Nice scent but subtle throw"                  ✓ Verified Purchase
Jamie Rodriguez - March 20, 2024

The lavender scent is pleasant and natural, but you need to be close to the 
candle to smell it. Great for a bedroom but not large living spaces. Still a 
quality candle overall.

Was this review helpful?  👍 Yes (15)  👎 No (3)

─────────────────────────────────────────────────────────────────────────────

[Load More Reviews]  [1] [2] [3] ... [14]
```

**Review Sorting Options:**
- **Most Recent:** Newest reviews first (default)
- **Highest Rating:** 5-star reviews first
- **Lowest Rating:** 1-star reviews first  
- **Most Helpful:** Reviews with most "helpful" votes first

**Review Filtering:**
- **All Stars:** Show all reviews
- **5 Stars Only:** Show only 5-star reviews
- **4 Stars Only:** Show only 4-star reviews
- (etc. for 3, 2, 1 stars)

---

### Section 5: Automated Review Request Emails

#### Review Request Email Configuration

System automatically sends review request emails 7 days after order delivery.

**Email Content Example:**
```
Subject: How was your recent candle purchase?

Hi Alex,

We hope you're enjoying your Lavender Dreams candle from your recent order!

We'd love to hear your feedback. Your review helps other customers make confident
purchase decisions and helps us improve our products.

[Write a Review]  ← Button linking to review form

Your review will include a "Verified Purchase" badge to show you're an authentic
customer.

Thank you for supporting our small business!

Sarah @ Candle Store
```

**Email Timing:**
- Sent 7 days after "Delivered" order status (configurable in settings)
- Only sent if customer has not already reviewed the product
- Maximum 1 review request per product per customer
- Includes direct link to review form pre-filled with product and order details

**Admin Configuration:**
1. Navigate to **Admin → Settings → Review Settings**
2. Configure **Review Request Email**:
   - ☑ Enable automated review requests
   - **Send After:** 7 days post-delivery (adjustable 3-30 days)
   - **Email Subject:** Customizable
   - **Email Content:** Customizable template with variables {CustomerName}, {ProductName}, {OrderNumber}
3. Click **Save Settings**

---

### Section 6: Review Analytics and Reporting

#### Accessing Review Reports

1. Navigate to **Admin → Reports → Reviews**
2. View aggregate analytics:

**Key Metrics:**
- Total Reviews: 427
- Average Rating: 4.6 stars
- % 5-Star Reviews: 60%
- % 4-Star Reviews: 28%
- % 1-3 Star Reviews: 12%
- Reviews This Month: 38 (↑ 15% from last month)
- Average Response Rate: 85% (store owner responses to reviews)

**Product-Level Insights:**
- Best-Rated Products: (List of products with highest average ratings)
- Most-Reviewed Products: (List of products with most reviews)
- Needs Attention: Products with low ratings or negative trends

**Review Trends Over Time:**
- Graph showing review volume per month
- Graph showing average rating trend
- Identify seasonal patterns

**Export Options:**
- Export all reviews to CSV (for external analysis)
- Export 5-star reviews (for marketing testimonials)
- Export negative reviews (for quality improvement analysis)

---

### Section 7: Best Practices

1. **Respond to Negative Reviews Promptly:** Address concerns within 24 hours to demonstrate customer care and prevent churn

2. **Highlight Reviews in Marketing:** Feature 5-star reviews in email campaigns, social media, and product descriptions to build trust

3. **Incentivize Reviews:** Reward customers who leave reviews with loyalty points (10-50 points per review) to boost participation rate

4. **Monitor for Patterns:** If multiple customers mention same issue (e.g., "weak scent"), investigate and reformulate product

5. **Showcase Photo Reviews:** Highlight customer-submitted photos on homepage and social media (with permission) for authentic social proof

6. **Thank Reviewers:** Publicly thank customers for positive reviews - builds community and encourages more reviews

7. **Use Reviews for Product Development:** Analyze review content to identify new product opportunities (e.g., customers asking for larger size options)

---

### Section 8: Troubleshooting

**Problem:** Customer cannot submit review for product they purchased

**Solution:**
1. Verify customer is logged into same account used for purchase
2. Check order status is "Delivered" (reviews disabled for pending/cancelled orders)
3. Verify customer hasn't already submitted review for this product (one review per product per customer)
4. Check Review Settings are enabled globally (Admin → Settings → Reviews)

---

**Problem:** Review not appearing on product page after approval

**Solution:**
1. Clear browser cache and refresh product page
2. Verify review status is "Approved" in Admin → Reviews → All Reviews
3. Check review isn't flagged as spam by automated filters
4. Verify product page is correctly rendering reviews component (contact developer if missing)

---

**Problem:** Review request emails not being sent

**Solution:**
1. Navigate to Admin → Settings → Review Settings
2. Verify "Enable automated review requests" is checked
3. Verify email service is configured correctly (Admin → Settings → Email)
4. Check order status is "Delivered" (emails only sent after delivery confirmation)
5. Verify customer email address is valid in their account

---

**Problem:** Duplicate reviews appearing from same customer

**Solution:**
1. Check database for duplicate Review records with same CustomerId and ProductId
2. Delete duplicate entries (keep most recent)
3. Add database constraint to prevent duplicates: UNIQUE(CustomerId, ProductId)
4. Review form should check for existing reviews before submission

---

## Acceptance Criteria / Definition of Done

### Core Functionality
- [ ] Review entity with ReviewId (Guid PK), ProductId (FK), CustomerId (FK), Rating (1-5 int), Title (string, 100 chars), Content (string, 2000 chars), ImageUrls (List<string>), IsVerifiedPurchase (bool), IsApproved (bool), HelpfulCount (int), CreatedAt (DateTime)
- [ ] ReviewVote entity tracks helpful votes with ReviewId (FK), CustomerId or SessionId, IsHelpful (bool)
- [ ] ReviewResponse entity for store owner responses with ReviewId (FK), ResponseText (string, 500 chars), CreatedAt
- [ ] Customers can submit reviews only for products they purchased (verified purchase check)
- [ ] Review submission restricted to authenticated customers only
- [ ] One review per product per customer (database constraint enforced)
- [ ] Star rating required (1-5), title optional, content required (50-2000 chars)
- [ ] Photo upload supports up to 3 images per review, max 5MB each
- [ ] Review submission triggers email notification to admin
- [ ] Reviews default to IsApproved = false (pending moderation)

### Review Display
- [ ] Product detail page displays review summary (average rating, total count)
- [ ] Rating distribution histogram shows count per star level (5 stars: X, 4 stars: Y, etc.)
- [ ] Reviews displayed in paginated list (10 per page default)
- [ ] Review cards show star rating, title, content, customer name, date, verified badge, helpful count
- [ ] Verified Purchase badge displayed only for customers who purchased product
- [ ] Review photos displayed as thumbnail gallery with lightbox on click
- [ ] Store owner responses display below reviews with distinct styling
- [ ] Empty state message displayed when product has no reviews

### Filtering and Sorting
- [ ] Filter reviews by star rating (All, 5 stars, 4 stars, 3 stars, 2 stars, 1 star)
- [ ] Sort reviews by Most Recent (default)
- [ ] Sort reviews by Highest Rating
- [ ] Sort reviews by Lowest Rating
- [ ] Sort reviews by Most Helpful (helpfulCount descending)
- [ ] Filter and sort combinations work correctly (e.g., "5-star reviews, most recent first")

### Moderation
- [ ] Admin moderation queue lists all pending reviews with product name, customer, rating, date
- [ ] Admin can approve review (sets IsApproved = true, publishes on product page)
- [ ] Admin can reject review with reason (moves to rejected list, customer notified)
- [ ] Bulk approve action for multiple reviews simultaneously
- [ ] Profanity filter flags reviews with inappropriate language for manual review
- [ ] Admin can view approved reviews filtered by product, date range, rating
- [ ] Admin can delete reviews (soft delete with IsDeleted flag, preserves data)
- [ ] Review moderation audit trail logs all actions (approved by, rejected by, timestamps)

### Store Owner Responses
- [ ] Admin can respond to any approved review
- [ ] One response per review maximum (cannot post multiple responses)
- [ ] Response text limited to 500 characters
- [ ] Response displays below review with "Response from Store Owner" badge
- [ ] Response includes store owner name and timestamp
- [ ] Customer receives email notification when store owner responds to their review
- [ ] Admin can edit their own responses within 24 hours of posting
- [ ] Admin can delete their own responses

### Helpful Voting
- [ ] Customers can vote review as helpful or not helpful
- [ ] Authenticated customers vote tied to CustomerId (one vote per review)
- [ ] Anonymous visitors vote tied to SessionId (one vote per review per session)
- [ ] Helpful count increments/decrements based on votes
- [ ] Customer cannot vote on their own review
- [ ] Voting updates review helpfulCount in real-time
- [ ] Rate limiting prevents spam voting (max 50 votes per session per hour)

### Review Request Emails
- [ ] Automated job checks for delivered orders 7 days old without reviews
- [ ] Review request email sent to customer with product name, order number, review link
- [ ] Email includes direct link to review form pre-filled with product and customer data
- [ ] Only one review request sent per product per customer (no duplicate emails)
- [ ] Review request email includes unsubscribe option
- [ ] Admin can configure review request timing (3-30 days post-delivery)
- [ ] Admin can enable/disable automated review requests globally
- [ ] Email template supports variables: {CustomerName}, {ProductName}, {OrderNumber}

### Performance
- [ ] Product page with reviews loads in <2 seconds (500+ reviews paginated)
- [ ] Review summary calculation (average rating, distribution) cached for 5 minutes
- [ ] Review list API endpoint responds in <200ms (paginated query)
- [ ] Review submission completes in <300ms including photo upload
- [ ] Review search by product + approval status uses database index
- [ ] Helpful vote update completes in <100ms
- [ ] Moderation queue page loads <500ms with 100+ pending reviews

### Data Persistence
- [ ] Reviews preserved permanently (soft delete only, never hard delete)
- [ ] Review edit history tracked if editing enabled (optional)
- [ ] Review votes logged with timestamp for analytics
- [ ] Store owner responses cannot be deleted by customers
- [ ] Verified purchase flag immutable after review creation
- [ ] Review photos stored in CDN with permanent URLs

### Edge Cases
- [ ] Product deletion does not delete reviews (orphaned reviews remain for data integrity)
- [ ] Customer account deletion anonymizes reviews (replace name with "Anonymous Customer")
- [ ] Duplicate review submission prevented (check CustomerId + ProductId uniqueness)
- [ ] Review longer than 2000 characters truncated with "..." indicator
- [ ] Photo upload failure does not block review submission (photos optional)
- [ ] Review submission during product out-of-stock works correctly
- [ ] Concurrent helpful votes handled without race conditions (database-level increment)

### Security
- [ ] Review submission requires authentication (JWT or cookie-based)
- [ ] XSS protection on review content (HTML sanitization)
- [ ] Photo upload validated for file type (JPG, PNG only) and size (max 5MB)
- [ ] Profanity filter on review content before moderation
- [ ] Rate limiting on review submission (max 5 reviews per customer per day)
- [ ] Admin moderation actions restricted to Admin/SuperAdmin roles
- [ ] Helpful voting rate limited to prevent manipulation
- [ ] Store owner responses restricted to admin users only

### API Endpoints
- [ ] GET /api/products/{productId}/reviews - Returns paginated reviews with filters/sorting
- [ ] POST /api/products/{productId}/reviews - Create review (authenticated)
- [ ] POST /api/reviews/{reviewId}/vote - Vote review as helpful
- [ ] GET /api/products/{productId}/reviews/summary - Returns average rating and distribution
- [ ] GET /api/admin/reviews/pending - Returns all pending reviews
- [ ] PUT /api/admin/reviews/{reviewId}/approve - Approve review
- [ ] PUT /api/admin/reviews/{reviewId}/reject - Reject review
- [ ] POST /api/admin/reviews/{reviewId}/respond - Store owner response
- [ ] DELETE /api/admin/reviews/{reviewId} - Delete review (soft delete)

### Testing Coverage
- [ ] 60+ unit tests covering review business logic (rating calculation, verified purchase check, vote counting)
- [ ] 20+ integration tests for API endpoints
- [ ] 10+ E2E tests for complete workflows (submit review, moderate, respond)
- [ ] 5+ performance tests for review queries and aggregations
- [ ] 90%+ code coverage on review services and repositories

---

## Testing Requirements

### Unit Tests

#### Test 1: Calculate Average Rating with Multiple Reviews

```csharp
[Fact]
public async Task CalculateAverageRating_WithMultipleReviews_ReturnsCorrectAverage()
{
    // Arrange
    var productId = Guid.NewGuid();
    var reviews = new List<Review>
    {
        new() { ReviewId = Guid.NewGuid(), ProductId = productId, Rating = 5, IsApproved = true },
        new() { ReviewId = Guid.NewGuid(), ProductId = productId, Rating = 4, IsApproved = true },
        new() { ReviewId = Guid.NewGuid(), ProductId = productId, Rating = 5, IsApproved = true },
        new() { ReviewId = Guid.NewGuid(), ProductId = productId, Rating = 3, IsApproved = true },
        new() { ReviewId = Guid.NewGuid(), ProductId = productId, Rating = 5, IsApproved = true }
    };

    _mockReviewRepo.Setup(r => r.GetApprovedReviewsByProductAsync(productId))
        .ReturnsAsync(reviews);

    // Act
    var summary = await _sut.GetReviewSummaryAsync(productId);

    // Assert
    summary.AverageRating.Should().Be(4.4m); // (5+4+5+3+5) / 5 = 4.4
    summary.TotalReviews.Should().Be(5);
}
```

#### Test 2: Verified Purchase Check Returns True for Customer Who Purchased

```csharp
[Fact]
public async Task HasCustomerPurchasedProduct_WhenOrderExists_ReturnsTrue()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var productId = Guid.NewGuid();

    var order = new Order
    {
        OrderId = Guid.NewGuid(),
        CustomerId = customerId,
        OrderStatus = OrderStatus.Delivered,
        OrderItems = new List<OrderItem>
        {
            new() { ProductId = productId, Quantity = 1 }
        }
    };

    _mockOrderRepo.Setup(r => r.GetByCustomerIdAsync(customerId))
        .ReturnsAsync(new List<Order> { order });

    // Act
    var hasPurchased = await _sut.HasCustomerPurchasedProductAsync(customerId, productId);

    // Assert
    hasPurchased.Should().BeTrue();
}
```

#### Test 3: Prevent Duplicate Review from Same Customer

```csharp
[Fact]
public async Task CreateReview_WhenCustomerAlreadyReviewed_ThrowsException()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var productId = Guid.NewGuid();

    var existingReview = new Review
    {
        ReviewId = Guid.NewGuid(),
        CustomerId = customerId,
        ProductId = productId
    };

    _mockReviewRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
        .ReturnsAsync(existingReview);

    var createDto = new CreateReviewDto
    {
        Rating = 5,
        Title = "Great product",
        Content = "Already reviewed this before"
    };

    // Act & Assert
    await FluentActions.Invoking(() => _sut.CreateReviewAsync(productId, customerId, createDto, true))
        .Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*already reviewed*");
}
```

#### Test 4: Helpful Vote Increments Count Correctly

```csharp
[Fact]
public async Task VoteReviewHelpful_IncrementsHelpfulCount()
{
    // Arrange
    var reviewId = Guid.NewGuid();
    var sessionId = "session-12345";

    var review = new Review
    {
        ReviewId = reviewId,
        HelpfulCount = 10
    };

    _mockReviewRepo.Setup(r => r.GetByIdAsync(reviewId))
        .ReturnsAsync(review);

    _mockReviewVoteRepo.Setup(r => r.GetByReviewAndSessionAsync(reviewId, sessionId))
        .ReturnsAsync((ReviewVote)null); // No existing vote

    // Act
    await _sut.VoteReviewAsync(reviewId, sessionId, isHelpful: true);

    // Assert
    review.HelpfulCount.Should().Be(11);
    _mockReviewVoteRepo.Verify(r => r.AddAsync(It.Is<ReviewVote>(v =>
        v.ReviewId == reviewId &&
        v.SessionId == sessionId &&
        v.IsHelpful == true
    )), Times.Once);
    _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
}
```

#### Test 5: Rating Distribution Calculated Correctly

```csharp
[Fact]
public async Task GetReviewSummary_CalculatesRatingDistribution()
{
    // Arrange
    var productId = Guid.NewGuid();
    var reviews = new List<Review>
    {
        new() { Rating = 5, IsApproved = true },
        new() { Rating = 5, IsApproved = true },
        new() { Rating = 5, IsApproved = true },
        new() { Rating = 4, IsApproved = true },
        new() { Rating = 4, IsApproved = true },
        new() { Rating = 3, IsApproved = true },
        new() { Rating = 2, IsApproved = true }
    };

    _mockReviewRepo.Setup(r => r.GetApprovedReviewsByProductAsync(productId))
        .ReturnsAsync(reviews);

    // Act
    var summary = await _sut.GetReviewSummaryAsync(productId);

    // Assert
    summary.RatingDistribution[5].Should().Be(3);
    summary.RatingDistribution[4].Should().Be(2);
    summary.RatingDistribution[3].Should().Be(1);
    summary.RatingDistribution[2].Should().Be(1);
    summary.RatingDistribution[1].Should().Be(0);
}
```

#### Test 6: Review Request Email Only Sent Once Per Product

```csharp
[Fact]
public async Task SendReviewRequestEmails_DoesNotSendDuplicates()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var productId = Guid.NewGuid();
    var orderId = Guid.NewGuid();

    var order = new Order
    {
        OrderId = orderId,
        CustomerId = customerId,
        OrderStatus = OrderStatus.Delivered,
        DeliveredAt = DateTime.UtcNow.AddDays(-7),
        OrderItems = new List<OrderItem>
        {
            new() { ProductId = productId }
        }
    };

    // Customer already reviewed this product
    var existingReview = new Review
    {
        CustomerId = customerId,
        ProductId = productId
    };

    _mockOrderRepo.Setup(r => r.GetDeliveredOrdersAsync(It.IsAny<DateTime>()))
        .ReturnsAsync(new List<Order> { order });

    _mockReviewRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
        .ReturnsAsync(existingReview);

    // Act
    await _sut.SendReviewRequestEmailsAsync();

    // Assert
    // No email should be sent since customer already reviewed
    _mockEmailService.Verify(e => e.SendReviewRequestAsync(
        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()
    ), Times.Never);
}
```

---

### Integration Tests

#### Test 1: Complete Review Submission and Approval Flow

```csharp
[Fact]
public async Task ReviewWorkflow_SubmitAndApprove_Success()
{
    // Arrange - Create test product and customer
    var product = new Product
    {
        ProductId = Guid.NewGuid(),
        ProductName = "Test Candle",
        Price = 28m
    };

    var customer = new Customer
    {
        CustomerId = Guid.NewGuid(),
        Email = "test@example.com",
        FirstName = "Test"
    };

    // Create completed order for verified purchase
    var order = new Order
    {
        OrderId = Guid.NewGuid(),
        CustomerId = customer.CustomerId,
        OrderStatus = OrderStatus.Delivered,
        OrderItems = new List<OrderItem>
        {
            new() { ProductId = product.ProductId, Quantity = 1 }
        }
    };

    await _testDbContext.Products.AddAsync(product);
    await _testDbContext.Customers.AddAsync(customer);
    await _testDbContext.Orders.AddAsync(order);
    await _testDbContext.SaveChangesAsync();

    // Act - Submit review
    var createDto = new CreateReviewDto
    {
        Rating = 5,
        Title = "Amazing candle!",
        Content = "This candle exceeded my expectations. The scent is wonderful and it burns evenly."
    };

    var review = await _reviewService.CreateReviewAsync(
        product.ProductId, customer.CustomerId, createDto, hasPurchased: true);

    // Assert - Review created with pending approval
    review.Should().NotBeNull();
    review.IsApproved.Should().BeFalse();
    review.IsVerifiedPurchase.Should().BeTrue();

    // Act - Admin approves review
    await _reviewService.ApproveReviewAsync(review.ReviewId);

    // Assert - Review now approved and visible
    var approvedReview = await _testDbContext.Reviews
        .FirstOrDefaultAsync(r => r.ReviewId == review.ReviewId);

    approvedReview.IsApproved.Should().BeTrue();

    // Assert - Review appears in product reviews
    var productReviews = await _reviewService.GetProductReviewsAsync(
        product.ProductId, page: 1, pageSize: 10);

    productReviews.Items.Should().Contain(r => r.ReviewId == review.ReviewId);
}
```

#### Test 2: Store Owner Response to Review

```csharp
[Fact]
public async Task StoreOwnerResponse_PostsSuccessfully()
{
    // Arrange - Create approved review
    var review = new Review
    {
        ReviewId = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Rating = 4,
        Title = "Good but could be better",
        Content = "Nice candle but scent throw is weak",
        IsApproved = true
    };

    await _testDbContext.Reviews.AddAsync(review);
    await _testDbContext.SaveChangesAsync();

    // Act - Store owner responds
    var responseText = "Thank you for the feedback! We've reformulated this candle with stronger fragrance.";
    var response = await _reviewService.RespondToReviewAsync(review.ReviewId, responseText);

    // Assert - Response created
    response.Should().NotBeNull();
    response.ReviewId.Should().Be(review.ReviewId);
    response.ResponseText.Should().Be(responseText);

    // Assert - Response linked to review
    var reviewWithResponse = await _testDbContext.Reviews
        .Include(r => r.StoreResponse)
        .FirstAsync(r => r.ReviewId == review.ReviewId);

    reviewWithResponse.StoreResponse.Should().NotBeNull();
    reviewWithResponse.StoreResponse.ResponseText.Should().Be(responseText);
}
```

#### Test 3: Helpful Voting API Endpoint

```csharp
[Fact]
public async Task VoteReviewHelpful_ApiEndpoint_Success()
{
    // Arrange - Create test review
    var review = new Review
    {
        ReviewId = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        CustomerId = Guid.NewGuid(),
        Rating = 5,
        Title = "Great product",
        Content = "Highly recommend",
        IsApproved = true,
        HelpfulCount = 5
    };

    await _testDbContext.Reviews.AddAsync(review);
    await _testDbContext.SaveChangesAsync();

    // Act - Call API to vote helpful
    var response = await _client.PostAsJsonAsync(
        $"/api/products/{review.ProductId}/reviews/{review.ReviewId}/vote",
        new { isHelpful = true });

    // Assert - Vote successful
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var updatedReview = await _testDbContext.Reviews
        .FirstAsync(r => r.ReviewId == review.ReviewId);

    updatedReview.HelpfulCount.Should().Be(6); // Incremented from 5 to 6
}
```

---

### End-to-End Tests

#### Scenario 1: Customer Leaves Review and Receives Store Response

1. Customer logs into account
2. Customer navigates to "My Orders" page
3. Customer finds delivered order containing "Lavender Dreams" candle
4. Customer clicks "Write a Review" button on order detail
5. **Expected:** Review form appears pre-filled with product name
6. Customer selects 5-star rating
7. Customer enters title "Perfect for relaxation"
8. Customer enters review content (200 characters about scent quality)
9. Customer uploads 2 photos of burning candle
10. Customer submits review
11. **Expected:** Success message "Thank you! Your review is pending approval"
12. **Expected:** Review appears in customer's account under "My Reviews" with "Pending" status
13. Admin logs into admin panel
14. Admin navigates to Reviews → Pending Reviews
15. **Expected:** Customer's review appears in moderation queue
16. Admin clicks "Approve" on review
17. **Expected:** Review status changes to "Approved"
18. Customer returns to product page
19. **Expected:** Review now visible on product page with "Verified Purchase" badge
20. Admin clicks "Respond" on review
21. Admin types response "Thank you for the wonderful feedback!"
22. **Expected:** Response appears below review on product page
23. **Expected:** Customer receives email notification of store owner response

---

#### Scenario 2: Browse and Filter Product Reviews

1. Anonymous visitor (not logged in) navigates to product detail page
2. **Expected:** Product shows aggregate rating (e.g., "4.6 stars") and review count
3. Visitor scrolls to Customer Reviews section
4. **Expected:** Rating distribution histogram displays (5 stars: 82, 4 stars: 38, etc.)
5. **Expected:** Reviews displayed in paginated list (10 per page)
6. Visitor clicks "Filter by 5 Stars"
7. **Expected:** Only 5-star reviews displayed
8. Visitor changes sort to "Most Helpful"
9. **Expected:** Reviews re-ordered by helpful vote count (descending)
10. Visitor clicks "👍 Yes" button on helpful review
11. **Expected:** Helpful count increments by 1
12. **Expected:** Button disabled (cannot vote again)
13. Visitor clicks on review photo thumbnail
14. **Expected:** Photo opens in lightbox/modal view
15. Visitor clicks "Load More Reviews" button
16. **Expected:** Next 10 reviews loaded and appended to list

---

#### Scenario 3: Automated Review Request Email Workflow

1. Customer completes purchase on March 1st
2. Order ships March 2nd
3. Order delivered March 5th (status updated to "Delivered")
4. **Expected:** No email sent immediately (7-day wait period)
5. March 12th (7 days post-delivery): Automated job runs at 6:00 AM
6. **Expected:** System identifies order eligible for review request
7. **Expected:** System checks customer hasn't already reviewed product
8. **Expected:** Review request email sent to customer
9. Customer receives email "How was your Lavender Dreams candle?"
10. Email includes "Write a Review" button/link
11. Customer clicks link in email
12. **Expected:** Redirects to review form pre-filled with product and order info
13. **Expected:** Review form shows "Verified Purchase" indicator
14. Customer completes and submits review
15. **Expected:** No additional review request emails sent (one per product limit)

---

### Performance Tests

#### Benchmark 1: Product Page Review Load Time

**Target:** Product page with 500+ reviews loads in <2 seconds

**Measurement:**
- Load product detail page with 500 approved reviews
- Measure time from request to fully rendered reviews section
- Test with cold cache and warm cache

**Pass Criteria:**
- Cold cache: <2.5 seconds
- Warm cache: <1.5 seconds
- 95th percentile: <2 seconds

**Optimization:**
- Paginate reviews (10 per page)
- Cache review summary (average rating, distribution) for 5 minutes
- Database index on (ProductId, IsApproved, CreatedAt)

---

#### Benchmark 2: Review Summary Calculation

**Target:** Calculate average rating and distribution for product in <100ms

**Measurement:**
- Query all approved reviews for product
- Calculate average rating
- Calculate rating distribution (count per star level)
- Measure execution time

**Pass Criteria:**
- Average: <80ms
- 95th percentile: <100ms
- Test with 100, 500, 1000 reviews

**Optimization:**
- Use database aggregation (AVG, COUNT GROUP BY)
- Cache results in Redis for 5 minutes
- Invalidate cache on new review approval

---

#### Benchmark 3: Review Submission with Photo Upload

**Target:** Submit review with 3 photos in <1 second

**Measurement:**
- Submit review form with title, content, 3 photos (2MB each)
- Upload photos to CDN
- Create Review record in database
- Send admin notification email
- Measure total time from submission to success response

**Pass Criteria:**
- Average: <800ms
- 95th percentile: <1 second
- Maximum: <1.5 seconds

**Optimization:**
- Upload photos to CDN asynchronously (background job)
- Return success response immediately after database insert
- Complete photo upload in background

---

### Regression Tests

**Features That Must Not Break:**
- [ ] Product detail page displays correctly with and without reviews
- [ ] Product search and filtering performance unaffected by reviews
- [ ] Customer account page "My Orders" section unchanged
- [ ] Admin dashboard performance with review moderation queue
- [ ] Email service continues sending order confirmations (review requests don't interfere)
- [ ] Product CRUD operations (create, update, delete) work independently of reviews

---

## User Verification Steps

### Verification 1: Submit Review as Customer
1. Log into storefront as customer account
2. Navigate to product you've previously purchased
3. Click "Write a Review" button
4. Rate product 5 stars by clicking star icons
5. Enter title "Amazing candle!"
6. Enter review content (at least 50 characters)
7. Upload 1-2 product photos
8. Click "Submit Review"
9. **Verify:** Success message appears
10. **Verify:** Review shows "Pending Approval" in your account

---

### Verification 2: Moderate Review in Admin Panel
1. Log into Admin Panel with admin credentials
2. Navigate to **Admin → Reviews → Pending Reviews**
3. **Verify:** List displays pending reviews with product names, ratings, customer names
4. Click "View Full Review" on first pending review
5. **Verify:** Full review content, photos, and customer details displayed
6. Click "Approve" button
7. **Verify:** Success message "Review approved"
8. **Verify:** Review removed from pending queue
9. Navigate to storefront product page
10. **Verify:** Approved review now visible on product page

---

### Verification 3: Post Store Owner Response
1. In Admin Panel, navigate to **Admin → Reviews → All Reviews**
2. Find review with negative rating (1-3 stars)
3. Click "Respond" button
4. Type response: "We're sorry to hear this. Please contact support for a replacement."
5. Click "Post Response"
6. **Verify:** Response appears below review with "Response from Store Owner" badge
7. Navigate to storefront product page
8. **Verify:** Store response visible below review publicly
9. Check customer email inbox
10. **Verify:** Customer received notification of store response

---

### Verification 4: Vote Review as Helpful
1. As logged-in customer or anonymous visitor, visit product page
2. Scroll to Customer Reviews section
3. Find a helpful review
4. Click "👍 Yes" button under "Was this review helpful?"
5. **Verify:** Helpful count increments by 1
6. **Verify:** Vote button becomes disabled (cannot vote again)
7. Refresh page
8. **Verify:** Your vote persists (button still disabled, count remains)

---

### Verification 5: Filter and Sort Reviews
1. Navigate to product with 20+ reviews
2. **Verify:** Reviews default to "Most Recent" sort order
3. Click filter "5 Stars Only"
4. **Verify:** Only 5-star reviews displayed
5. Change sort to "Most Helpful"
6. **Verify:** Reviews re-ordered by helpful vote count
7. Click "All Stars" filter
8. **Verify:** All reviews displayed again
9. Change sort to "Lowest Rating"
10. **Verify:** 1-2 star reviews appear first

---

### Verification 6: Review Request Email
1. Place test order and mark as "Delivered"
2. Manually trigger review request job or wait 7 days
3. **Verify:** Review request email received in customer inbox
4. **Verify:** Email subject includes product name
5. Click "Write a Review" button in email
6. **Verify:** Redirects to review form with product pre-selected
7. **Verify:** Form shows "Verified Purchase" indicator
8. Submit review
9. **Verify:** No duplicate review request emails sent

---

### Verification 7: Review Summary Display
1. Navigate to product with multiple reviews
2. **Verify:** Average rating displayed (e.g., "4.6 stars")
3. **Verify:** Total review count displayed (e.g., "Based on 137 reviews")
4. **Verify:** Rating distribution histogram shows bars for each star level
5. **Verify:** Percentages add up to 100%
6. **Verify:** "Write a Review" button prominently displayed
7. Click a star bar (e.g., "3 stars")
8. **Verify:** Reviews filter to show only 3-star reviews

---

### Verification 8: Duplicate Review Prevention
1. Log in as customer who already reviewed a product
2. Navigate to that product's page
3. **Verify:** "Write a Review" button either hidden or shows "You've already reviewed this"
4. Attempt to access review form directly via URL
5. **Verify:** Error message "You've already reviewed this product"
6. **Verify:** Cannot submit duplicate review

---

### Verification 9: Review Photo Gallery
1. Navigate to product with reviews containing photos
2. **Verify:** Review cards display photo thumbnails (3 max)
3. Click on photo thumbnail
4. **Verify:** Photo opens in lightbox/modal overlay
5. **Verify:** Can navigate between photos using arrows
6. Click outside photo or "X" button
7. **Verify:** Lightbox closes, returns to reviews list

---

### Verification 10: Review Analytics Report
1. Log into Admin Panel
2. Navigate to **Admin → Reports → Reviews**
3. **Verify:** Dashboard displays total reviews, average rating, % distribution
4. **Verify:** "Reviews This Month" metric with trend indicator (↑/↓)
5. **Verify:** "Best-Rated Products" list sorted by average rating
6. **Verify:** "Most-Reviewed Products" list sorted by review count
7. Click "Export to CSV" button
8. **Verify:** CSV file downloads with all review data
9. Open CSV in Excel
10. **Verify:** Columns include Product, Rating, Title, Content, Date, Customer

---

## Implementation Prompt for Claude

### Implementation Overview

Implement a complete Product Reviews and Ratings System enabling customers to submit star ratings, written reviews, and photo uploads for products they've purchased. The system includes admin moderation workflows, verified purchase badges, helpful voting, store owner responses, automated review request emails, and comprehensive review analytics.

**Key Components:**
- Review entity with ratings (1-5 stars), title, content, photos, verified purchase flag
- ReviewVote entity for helpful voting
- ReviewResponse entity for store owner responses
- Review service layer with business logic (duplicate prevention, verified purchase check, average rating calculation)
- Reviews API endpoints (public and admin)
- Admin moderation queue UI (Blazor)
- Review display component for product pages (Blazor)
- Automated review request email job
- Review analytics and reporting

---

### Prerequisites

**Required:**
- .NET 8 SDK installed
- Completed Task 002 (Domain Models - Product, Order entities)
- Completed Task 014 (Customer Accounts)
- Completed Task 016 (Order Confirmation & Email)

**NuGet Packages:**
- No new packages required (uses existing AutoMapper, FluentValidation, EF Core)
- Optional: ImageSharp (for photo resizing/optimization)

---

### Step-by-Step Implementation

#### Step 1: Create Review Domain Entities

**File:** `src/CandleStore.Domain/Entities/Review.cs`

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CandleStore.Domain.Entities;

public class Review
{
    [Key]
    public Guid ReviewId { get; set; }

    public Guid ProductId { get; set; }

    public Guid CustomerId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public List<string> ImageUrls { get; set; } = new();

    public bool IsVerifiedPurchase { get; set; } = false;

    public bool IsApproved { get; set; } = false;

    public int HelpfulCount { get; set} = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; } = false;

    // Navigation Properties
    public virtual Product Product { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual List<ReviewVote> Votes { get; set; } = new();

    public virtual ReviewResponse? StoreResponse { get; set; }

    // Methods
    public void Approve()
    {
        IsApproved = true;
    }

    public void IncrementHelpfulCount()
    {
        HelpfulCount++;
    }

    public void DecrementHelpfulCount()
    {
        HelpfulCount = Math.Max(0, HelpfulCount - 1);
    }
}

public class ReviewVote
{
    [Key]
    public Guid VoteId { get; set; }

    public Guid ReviewId { get; set; }

    public Guid? CustomerId { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }

    public bool IsHelpful { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Review Review { get; set; } = null!;
}

public class ReviewResponse
{
    [Key]
    public Guid ResponseId { get; set; }

    public Guid ReviewId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ResponseText { get; set; } = string.Empty;

    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Review Review { get; set; } = null!;
}
```

**File:** `src/CandleStore.Domain/Entities/Product.cs` (add navigation property)

```csharp
// Add to existing Product entity:
public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

// Add computed property:
[NotMapped]
public decimal AverageRating => Reviews.Any(r => r.IsApproved)
    ? Reviews.Where(r => r.IsApproved).Average(r => r.Rating)
    : 0m;

[NotMapped]
public int TotalReviews => Reviews.Count(r => r.IsApproved);
```

---

#### Step 2: Create EF Core Configurations

**File:** `src/CandleStore.Infrastructure/Persistence/Configurations/ReviewConfiguration.cs`

```csharp
using CandleStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CandleStore.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.ReviewId);

        builder.HasIndex(r => new { r.ProductId, r.IsApproved, r.CreatedAt });

        builder.HasIndex(r => new { r.CustomerId, r.ProductId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(r => r.ImageUrls)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            );

        builder.HasQueryFilter(r => !r.IsDeleted);

        builder.HasOne(r => r.Product)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.StoreResponse)
            .WithOne(sr => sr.Review)
            .HasForeignKey<ReviewResponse>(sr => sr.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

#### Step 3: Create Repository Interfaces

**File:** `src/CandleStore.Application/Interfaces/Repositories/IReviewRepository.cs`

```csharp
using CandleStore.Domain.Entities;

namespace CandleStore.Application.Interfaces.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    Task<IEnumerable<Review>> GetApprovedReviewsByProductAsync(Guid productId);

    Task<PagedResult<Review>> GetProductReviewsPagedAsync(
        Guid productId,
        int page,
        int pageSize,
        int? starFilter = null,
        string sortBy = "recent");

    Task<Review?> GetByCustomerAndProductAsync(Guid customerId, Guid productId);

    Task<IEnumerable<Review>> GetPendingReviewsAsync();

    Task<Dictionary<int, int>> GetRatingDistributionAsync(Guid productId);
}
```

---

#### Step 4: Implement Review Service

**File:** `src/CandleStore.Application/Services/ReviewService.cs`

```csharp
using CandleStore.Application.DTOs;
using CandleStore.Application.Interfaces;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;
using AutoMapper;

namespace CandleStore.Application.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReviewRepository _reviewRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IMapper _mapper;

    public ReviewService(
        IUnitOfWork unitOfWork,
        IReviewRepository reviewRepo,
        IOrderRepository orderRepo,
        IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _reviewRepo = reviewRepo;
        _orderRepo = orderRepo;
        _mapper = mapper;
    }

    public async Task<ReviewDto> CreateReviewAsync(
        Guid productId,
        Guid customerId,
        CreateReviewDto createDto,
        bool hasPurchased)
    {
        // Check for duplicate review
        var existing = await _reviewRepo.GetByCustomerAndProductAsync(customerId, productId);
        if (existing != null)
            throw new InvalidOperationException("You have already reviewed this product");

        var review = new Review
        {
            ReviewId = Guid.NewGuid(),
            ProductId = productId,
            CustomerId = customerId,
            Rating = createDto.Rating,
            Title = createDto.Title ?? string.Empty,
            Content = createDto.Content,
            ImageUrls = createDto.ImageUrls ?? new List<string>(),
            IsVerifiedPurchase = hasPurchased,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        };

        await _reviewRepo.AddAsync(review);
        await _unitOfWork.CompleteAsync();

        return _mapper.Map<ReviewDto>(review);
    }

    public async Task<bool> HasCustomerPurchasedProductAsync(Guid customerId, Guid productId)
    {
        var orders = await _orderRepo.GetByCustomerIdAsync(customerId);

        return orders.Any(o =>
            o.OrderStatus == OrderStatus.Delivered &&
            o.OrderItems.Any(item => item.ProductId == productId));
    }

    public async Task<ReviewSummaryDto> GetReviewSummaryAsync(Guid productId)
    {
        var reviews = await _reviewRepo.GetApprovedReviewsByProductAsync(productId);

        var summary = new ReviewSummaryDto
        {
            TotalReviews = reviews.Count(),
            AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
            RatingDistribution = await _reviewRepo.GetRatingDistributionAsync(productId)
        };

        return summary;
    }

    public async Task VoteReviewAsync(Guid reviewId, string sessionId, bool isHelpful)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review == null)
            throw new NotFoundException($"Review {reviewId} not found");

        // Check if already voted
        var existingVote = await _unitOfWork.ReviewVotes
            .GetByReviewAndSessionAsync(reviewId, sessionId);

        if (existingVote != null)
            throw new InvalidOperationException("You have already voted on this review");

        var vote = new ReviewVote
        {
            VoteId = Guid.NewGuid(),
            ReviewId = reviewId,
            SessionId = sessionId,
            IsHelpful = isHelpful,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ReviewVotes.AddAsync(vote);

        if (isHelpful)
            review.IncrementHelpfulCount();

        await _unitOfWork.CompleteAsync();
    }

    public async Task ApproveReviewAsync(Guid reviewId)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review == null)
            throw new NotFoundException($"Review {reviewId} not found");

        review.Approve();
        await _unitOfWork.CompleteAsync();
    }

    public async Task<ReviewResponseDto> RespondToReviewAsync(
        Guid reviewId,
        string responseText,
        string createdBy)
    {
        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review == null)
            throw new NotFoundException($"Review {reviewId} not found");

        if (review.StoreResponse != null)
            throw new InvalidOperationException("Review already has a response");

        var response = new ReviewResponse
        {
            ResponseId = Guid.NewGuid(),
            ReviewId = reviewId,
            ResponseText = responseText,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        review.StoreResponse = response;
        await _unitOfWork.CompleteAsync();

        return _mapper.Map<ReviewResponseDto>(response);
    }
}
```

---

#### Step 5: Create API Controllers

**File:** `src/CandleStore.Api/Controllers/ReviewsController.cs`

```csharp
using CandleStore.Application.DTOs;
using CandleStore.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CandleStore.Api.Controllers;

[ApiController]
[Route("api/products/{productId}/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResultDto<ReviewDto>>>> GetReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? starFilter = null,
        [FromQuery] string sortBy = "recent")
    {
        var reviews = await _reviewService.GetProductReviewsAsync(
            productId, page, pageSize, starFilter, sortBy);

        return Ok(new ApiResponse<PagedResultDto<ReviewDto>>
        {
            Success = true,
            Data = reviews
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> CreateReview(
        Guid productId,
        [FromBody] CreateReviewDto createDto)
    {
        var customerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var hasPurchased = await _reviewService.HasCustomerPurchasedProductAsync(
            customerId, productId);

        var review = await _reviewService.CreateReviewAsync(
            productId, customerId, createDto, hasPurchased);

        return Ok(new ApiResponse<ReviewDto>
        {
            Success = true,
            Data = review,
            Message = "Review submitted for approval"
        });
    }

    [HttpPost("{reviewId}/vote")]
    public async Task<ActionResult<ApiResponse<bool>>> VoteHelpful(
        Guid productId,
        Guid reviewId,
        [FromBody] bool isHelpful)
    {
        var sessionId = HttpContext.Session.Id;
        await _reviewService.VoteReviewAsync(reviewId, sessionId, isHelpful);

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Data = true
        });
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<ReviewSummaryDto>>> GetReviewSummary(
        Guid productId)
    {
        var summary = await _reviewService.GetReviewSummaryAsync(productId);

        return Ok(new ApiResponse<ReviewSummaryDto>
        {
            Success = true,
            Data = summary
        });
    }
}
```

---

#### Step 6: Create Admin Reviews Controller

**File:** `src/CandleStore.Api/Controllers/AdminReviewsController.cs`

```csharp
[ApiController]
[Route("api/admin/reviews")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public AdminReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<List<ReviewDto>>>> GetPendingReviews()
    {
        var reviews = await _reviewService.GetPendingReviewsAsync();

        return Ok(new ApiResponse<List<ReviewDto>>
        {
            Success = true,
            Data = reviews
        });
    }

    [HttpPut("{reviewId}/approve")]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveReview(Guid reviewId)
    {
        await _reviewService.ApproveReviewAsync(reviewId);

        return Ok(new ApiResponse<bool>
        {
            Success = true,
            Data = true,
            Message = "Review approved"
        });
    }

    [HttpPost("{reviewId}/respond")]
    public async Task<ActionResult<ApiResponse<ReviewResponseDto>>> RespondToReview(
        Guid reviewId,
        [FromBody] string responseText)
    {
        var createdBy = User.Identity?.Name ?? "Admin";
        var response = await _reviewService.RespondToReviewAsync(
            reviewId, responseText, createdBy);

        return Ok(new ApiResponse<ReviewResponseDto>
        {
            Success = true,
            Data = response,
            Message = "Response posted"
        });
    }
}
```

---

#### Step 7: Create Blazor Review Display Component

**File:** `src/CandleStore.Storefront/Components/ProductReviews.razor`

(See original task content for complete Blazor component code from lines 460-621)

---

#### Step 8: Create Database Migration

```bash
dotnet ef migrations add AddReviewSystem --project src/CandleStore.Infrastructure --startup-project src/CandleStore.Api

dotnet ef database update --project src/CandleStore.Infrastructure --startup-project src/CandleStore.Api
```

---

#### Step 9: Implement Review Request Email Job

**File:** `src/CandleStore.Infrastructure/BackgroundJobs/SendReviewRequestEmailsJob.cs`

```csharp
using CandleStore.Application.Interfaces;

namespace CandleStore.Infrastructure.BackgroundJobs;

public class SendReviewRequestEmailsJob
{
    private readonly IReviewService _reviewService;
    private readonly IOrderRepository _orderRepo;
    private readonly IEmailService _emailService;

    public SendReviewRequestEmailsJob(
        IReviewService reviewService,
        IOrderRepository orderRepo,
        IEmailService emailService)
    {
        _reviewService = reviewService;
        _orderRepo = orderRepo;
        _emailService = emailService;
    }

    public async Task ExecuteAsync()
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var deliveredOrders = await _orderRepo.GetDeliveredOrdersAsync(sevenDaysAgo);

        foreach (var order in deliveredOrders)
        {
            foreach (var item in order.OrderItems)
            {
                // Check if customer already reviewed
                var existingReview = await _reviewService
                    .GetByCustomerAndProductAsync(order.CustomerId, item.ProductId);

                if (existingReview == null)
                {
                    await _emailService.SendReviewRequestAsync(
                        order.Customer.Email,
                        item.Product.ProductName,
                        item.ProductId,
                        order.OrderId);
                }
            }
        }
    }
}
```

Configure this job to run daily at 6:00 AM using Hangfire or Quartz.NET.

---

### Testing the Implementation

1. **Run migrations:**
   ```bash
   dotnet ef database update
   ```

2. **Start API and Admin panel:**
   ```bash
   dotnet run --project src/CandleStore.Api
   dotnet run --project src/CandleStore.Admin
   ```

3. **Test review submission:**
   - Log in as customer
   - Navigate to product page
   - Submit review
   - Verify pending status

4. **Test moderation:**
   - Log in to Admin panel
   - Check pending reviews queue
   - Approve review
   - Verify appears on product page

---

### Next Steps After This Task

After completing Task 029:
1. Proceed to Task 030 (Wishlist and Product Favorites)
2. Consider integrating reviews with loyalty program (Task 046) to reward reviewers
3. Add review photos to UGC gallery (Task 060) for social proof

---

**END OF TASK 029**
