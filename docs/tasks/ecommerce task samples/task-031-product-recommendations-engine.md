# Task 031: Product Recommendations Engine

**Priority:** 31 / 36
**Tier:** B
**Complexity:** 8 Fibonacci points
**Phase:** Phase 10 - Advanced Features
**Dependencies:** Task 013

---

## Description

The Product Recommendations Engine provides intelligent product suggestions throughout the customer journey to increase average order value, improve product discovery, and create a personalized shopping experience. The system employs multiple recommendation strategies including related products (same category/tags), frequently bought together (based on historical order analysis), personalized recommendations (purchase history and browsing behavior), recently viewed products (session tracking), and trending products (popularity-based). These recommendations appear strategically at key decision points: product detail pages, shopping cart, homepage, checkout confirmation, and email campaigns.

From a technical perspective, the recommendation engine combines collaborative filtering (analyzing order patterns to find products frequently purchased together), content-based filtering (matching products by category, tags, attributes), and popularity metrics (view counts, purchase frequency, ratings). The system tracks product views via session cookies and customer accounts, building a behavioral profile used for personalization. For authenticated users, recommendations leverage purchase history and wishlist data. For guest users, recommendations use session-based recently viewed products and general trending items. The engine pre-computes frequently bought together relationships via nightly batch jobs to ensure fast API response times, avoiding complex on-the-fly calculations that could slow down page loads.

The implementation integrates with existing systems: product catalog (Task 013) for product data and availability, order management (Task 021) for purchase history analysis, wishlist (Task 030) for saved product signals, and analytics (Task 027) for tracking recommendation click-through rates and conversion metrics. The system stores product view tracking in a dedicated ProductView table with session/customer associations, and frequently bought together associations in a ProductAssociation table pre-computed from order data. API endpoints expose recommendations for each placement type, with configurable count parameters to request 3-8 products per section.

**Business Value:**

- **For Store Owner Sarah:** Recommendations increase average order value by 15-30% through effective cross-selling and upselling. Frequently bought together suggestions at product and cart pages convert 8-12% of recommendations into additional purchases. Personalized recommendations on homepage increase engagement time by 40% and repeat purchase rates by 25%. Product discovery improves—long-tail products (those not featured prominently) see 35% more visibility through related product suggestions. Analytics show which products pair well together, informing future bundle creation and marketing campaigns.

- **For Customer Alex:** Recommendations solve the "what else might I like?" discovery problem. Related products help customers explore alternatives and find the perfect match (e.g., comparing three vanilla candles before choosing). Frequently bought together suggestions provide helpful pairings customers might not have considered (candle + room spray + matches). Personalized recommendations feel curated to individual tastes, reducing search time by 50%. Recently viewed products provide quick access to products customers are considering, preventing lost intent when browsing multiple options.

- **For Developer David:** The recommendation engine uses clean architecture with separation between recommendation strategies (each strategy is a separate class implementing IRecommendationStrategy). Pre-computed associations via background jobs keep API response times under 200ms. Caching recommendations for popular products reduces database load by 60%. The system is extensible—new recommendation strategies can be added by implementing the strategy interface. Integration with existing systems uses well-defined repository interfaces, making the feature maintainable and testable.

**Key Features:**

- **Related Products:** Display 4-8 products from same category or with shared tags on product detail pages
- **Frequently Bought Together:** Show 3-4 products commonly purchased alongside current product, based on historical order analysis
- **Personalized Recommendations:** Homepage recommendations tailored to customer's purchase history and browsing behavior (8-12 products)
- **Recently Viewed:** Track last 6-10 products customer viewed in session, display on homepage or category pages
- **Trending Products:** Display most popular products by view count and purchase frequency for guest users
- **Cross-Sell in Cart:** Suggest complementary products in shopping cart to increase order value
- **Post-Purchase Recommendations:** Show related products on order confirmation page and follow-up emails
- **Product View Tracking:** Record every product page view with session ID, customer ID (if authenticated), timestamp
- **Background Job:** Nightly job analyzes order data to compute frequently bought together associations
- **Recommendation Widgets:** Reusable UI components for displaying recommendations across site

**Technical Approach:**

The implementation creates a RecommendationService in the Application layer with methods for each recommendation type: GetRelatedProductsAsync (queries products with matching CategoryId), GetFrequentlyBoughtTogetherAsync (queries pre-computed ProductAssociation table), GetPersonalizedRecommendationsAsync (queries customer's OrderItems for category preferences), GetRecentlyViewedAsync (queries ProductView table by session ID), and GetTrendingProductsAsync (queries products ordered by ViewCount). A ProductView entity tracks every product page view with fields: ProductId, SessionId, CustomerId (nullable), ViewedAt. A ProductAssociation entity stores pre-computed relationships: ProductId1, ProductId2, AssociationScore (count of orders containing both), AssociationType (FrequentlyBoughtTogether or RelatedProducts). Background job ProductAssociationJob runs nightly to recompute associations from Order/OrderItem data.

**Integration:**

- **Task 013 (Product Management API):** Recommendation service queries IProductRepository to fetch product details for recommendations. Only active products (IsActive = true) with stock (StockQuantity > 0) are included in recommendations. Product images, prices, and ratings are loaded for display in recommendation widgets.

- **Task 021 (Order Management API):** Frequently bought together logic queries OrderItems table to find products commonly ordered together. ProductAssociationJob analyzes completed orders to compute association scores. Personalized recommendations analyze customer's past OrderItems to identify preferred categories.

- **Task 030 (Wishlist):** Wishlist products are excluded from personalized recommendations to avoid suggesting products customer has already saved. Wishlist data provides additional signal for category preferences in personalization algorithm.

- **Task 027 (Google Analytics):** Recommendation clicks are tracked as GA4 events ("select_promotion" with item_id and creative_slot). Click-through rate (CTR) and conversion rate for each recommendation type are monitored in Analytics dashboard. A/B testing different recommendation algorithms uses GA4 custom dimensions.

**Constraints:**

- Frequently bought together requires minimum 10 completed orders containing target product to avoid recommendations based on insufficient data
- Recently viewed products limited to 10 items per session to prevent excessive storage
- Product view tracking creates significant database writes—requires indexing on (ProductId, ViewedAt) for performance
- Pre-computation job runs nightly (not real-time)—new product associations appear after delay
- Personalized recommendations require customer purchase history—guest users receive generic trending products
- Maximum 20 recommendations returned per API call to control response size and rendering performance
- Recommendations cached for 15 minutes per product to reduce database queries

---

## Use Cases

### Use Case 1: Alex (Customer) Discovers Frequently Bought Together Suggestion in Cart

**Scenario:** Alex adds "Ocean Breeze Candle (8 oz)" to her cart. She's ready to proceed to checkout but sees a "Frequently Bought Together" section suggesting three products commonly purchased with her candle: matching room spray, decorative candle matches, and a wick trimmer. Alex hadn't considered these accessories but realizes they would enhance her candle experience.

**Without This Feature:**
Alex adds the Ocean Breeze candle to cart and proceeds directly to checkout. She completes her purchase for $24.99. Two weeks later, after burning the candle, she realizes the wick needs trimming and searches Amazon for a wick trimmer, purchasing it elsewhere for $8.99 plus shipping. She also notices her friend has decorative matches that look elegant next to candles and wishes she had known to buy those with her original order. The store misses $25-30 in additional revenue from accessories Alex would have purchased if suggested at the right time. Alex has to make a second transaction elsewhere, resulting in poor customer experience and lost loyalty.

**With This Feature:**
Alex adds Ocean Breeze candle to cart. The cart page displays "Frequently Bought Together" section showing three products: "Ocean Breeze Room Spray (4 oz) - $14.99," "Gold Decorative Matches - $9.99," and "Stainless Steel Wick Trimmer - $8.99." The section shows social proof: "Customers who bought Ocean Breeze Candle also purchased these items." Alex thinks "These actually make sense—I do need a wick trimmer and the matching room spray would be nice." She clicks [Add to Cart] on all three accessories. Her order total increases from $24.99 to $58.96 (136% increase). She completes checkout feeling she got a complete candle experience kit. Two weeks later, she uses the wick trimmer to maintain her candle and enjoys the matching room spray in her bathroom. She leaves a 5-star review mentioning how much she loves the complete set and recommends this combination to friends.

**Outcome:**
- Store revenue increases from $24.99 to $58.96 (136% increase) from single customer
- Average order value (AOV) across site increases by 18% after implementing frequently bought together
- 12% of cart recommendations convert to additional purchases generating $15,000 monthly revenue
- Customer satisfaction increases—Alex has everything needed for optimal candle experience
- Repeat purchase likelihood increases 22% when customers buy recommended accessories (they invest more in the ecosystem)
- Analytics show room spray + candle combination has 65% co-purchase rate, validating recommendation accuracy

### Use Case 2: Sarah (Store Owner) Uses Recommendation Analytics to Create Product Bundles

**Scenario:** Sarah wants to create curated product bundles for the upcoming holiday season but isn't sure which products pair well together. She needs data-driven insights rather than guessing which combinations customers actually want.

**Without This Feature:**
Sarah creates bundles based on intuition: "Vanilla Bourbon Candle + Cinnamon Spice Candle + Winter Pine Candle" assuming customers want variety packs. She launches this holiday bundle for $69.99. Sales are disappointing—only 23 bundles sell in the first month. Customer feedback reveals buyers don't actually want three different scents; they want one candle with complementary accessories. Sarah wasted time creating an unappealing bundle and missed holiday revenue opportunities. She has no data on which products customers naturally buy together, so she's making blind decisions.

**With This Feature:**
Sarah logs into the admin dashboard and navigates to Reports > Product Recommendations > Association Analysis. She views a report showing top product pairs by co-purchase rate. The data reveals surprising insights: "Vanilla Bourbon Candle + Vanilla Bourbon Room Spray" has 58% co-purchase rate (most popular pairing), "Lavender Dreams Candle + Sleep Aid Pillow Spray" has 42% co-purchase, and "Ocean Breeze Candle + Beach House Reed Diffuser" has 39% co-purchase. Sarah also discovers that customers who buy premium candles ($30+) frequently add gold decorative matches (48% co-purchase rate). Armed with this data, Sarah creates three holiday bundles: "Vanilla Bourbon Experience" (candle + room spray + matches - $44.99, 15% savings), "Sleep Sanctuary" (Lavender Dreams candle + pillow spray + sleep mask - $49.99), and "Coastal Getaway" (Ocean Breeze candle + reed diffuser + beach-scented sachet - $54.99). She launches these bundles with confidence backed by data. In the first month, 187 bundles sell (8x more than previous intuition-based bundle), generating $9,359 in revenue. Customer reviews praise the thoughtful combinations: "Perfect pairing!" and "Exactly what I wanted in one package."

**Outcome:**
- Sarah creates data-driven bundles based on proven customer preferences, not guesses
- Bundle sales increase 8x from 23 to 187 units in first month
- Revenue from bundles: $9,359 vs $1,610 (481% increase)
- Product margin improves—bundles have 42% margin vs 35% for individual products (customers perceive value, less price comparison)
- Marketing campaigns target bundle buyers with complementary products (customers who bought Vanilla Bourbon Experience receive email about new Bourbon Vanilla candle variations)
- Inventory planning improves—Sarah stocks more room sprays knowing they co-sell with candles at 50%+ rates

### Use Case 3: Guest User Browsing Receives Trending Product Recommendations

**Scenario:** Jamie discovers Candle Store via Instagram ad. She's a first-time visitor with no account or purchase history. She browses the homepage looking for gift ideas but feels overwhelmed by the full product catalog (120 products). She needs guidance on what's popular or high-quality to narrow her options.

**Without This Feature:**
Jamie lands on homepage and sees a generic "Featured Products" section showing 6 random candles Sarah manually selected months ago. These products aren't necessarily popular or relevant—they're just what Sarah thought looked good at the time. Jamie clicks on one, reads the description, but has no context on whether it's a good choice. She browses through multiple category pages trying to gauge what's popular based on review counts. After 15 minutes of searching, she feels decision fatigue and thinks "There's too much to choose from—I'll come back later when I have more time." She closes the browser and never returns. Store loses a potential customer due to poor product discovery experience.

**With This Feature:**
Jamie lands on homepage and immediately sees "Trending Now: Customer Favorites" section displaying 8 products: Ocean Breeze Candle (★★★★★ 147 reviews), Vanilla Bourbon (★★★★★ 134 reviews), Lavender Dreams (★★★★☆ 89 reviews), etc. These are the most viewed and purchased products from the past 30 days, automatically ranked by popularity algorithm. Jamie thinks "These must be the best ones if they're trending" and clicks on Ocean Breeze. The product detail page shows "Related Products" section with 4 similar fresh/clean scent candles, helping her compare options. She adds Ocean Breeze to cart. The cart page shows "Frequently Bought Together" suggesting room spray and matches. She adds the spray. Below cart, a "Complete Your Experience" section shows a reed diffuser in Ocean Breeze scent (personalized based on her cart contents). She adds that too. Her order totals $58.97. She completes checkout as a guest, feeling confident in her purchases because trending data validated her choices. Her order confirmation email includes "You May Also Like" section with three other popular fresh scents. Two weeks later, she returns to the site (cookie tracking recognized her), sees "Recently Viewed" section reminding her of products she considered, and makes a second purchase of Lavender Dreams candle for herself.

**Outcome:**
- Jamie converts from overwhelmed browser to purchasing customer through guided product discovery
- Trending recommendations reduce decision fatigue, increasing conversion rate from 2.1% to 3.8% for first-time visitors
- Average session duration increases from 3:42 to 6:18 minutes when trending products are displayed (more engagement)
- Cart abandonment rate decreases from 68% to 54% for guests who interact with recommendations
- Second purchase rate for first-time customers increases from 12% to 22% when recently viewed products are shown
- Store revenue increases by $42,000 annually from guest user recommendations alone
- Jamie becomes repeat customer (lifetime value: $240) because initial experience was confidence-inspiring
## User Manual Documentation

### Overview

The Product Recommendations Engine automatically suggests relevant products to customers throughout their shopping journey, increasing product discovery, average order value, and customer satisfaction. The system provides five types of recommendations: Related Products (same category/similar attributes), Frequently Bought Together (historically co-purchased items), Personalized Recommendations (tailored to customer's purchase/browsing history), Recently Viewed Products (session-based tracking), and Trending Products (most popular items). Recommendations appear on product detail pages, shopping cart, homepage, and order confirmation pages.

**When to use this feature:**
- **Customers:** Recommendations appear automatically—no manual action required. Click any recommended product to view details or add to cart.
- **Store Owners:** Monitor recommendation performance in Analytics dashboard to optimize product associations and understand customer behavior patterns.
- **Developers:** Use recommendation API endpoints to display suggestions in custom UI components or email templates.

**Key capabilities:**
- Automatic product view tracking for recently viewed history
- Pre-computed frequently bought together associations updated nightly
- Category-based related product suggestions
- Purchase history analysis for personalized recommendations
- Trending products based on popularity metrics
- Configurable recommendation counts (3-20 items per section)
- Click-through and conversion tracking for recommendation effectiveness

---

### Customer Experience: Viewing Product Recommendations

#### Related Products on Product Detail Page

When viewing any product, scroll down to see "You May Also Like" or "Related Products" section:

```
┌─────────────────────────────────────────────────────────────┐
│  Ocean Breeze Candle (8 oz)                 ♥ Wishlist Cart │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [Product Image]    $24.99  ★★★★★ (147 reviews)            │
│                                                             │
│  Description: A refreshing ocean-inspired scent...          │
│                                                             │
│  [Add to Cart]  [Add to Wishlist]                          │
│                                                             │
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  You May Also Like                                          │
│                                                             │
│  ┌───────┐  ┌───────┐  ┌───────┐  ┌───────┐               │
│  │       │  │       │  │       │  │       │               │
│  │ Beach │  │Coastal│  │Summer │  │ Sea   │               │
│  │ House │  │ Mist  │  │Breeze │  │ Salt  │               │
│  │       │  │       │  │       │  │       │               │
│  │$26.99 │  │$24.99 │  │$22.99 │  │$29.99 │               │
│  │★★★★☆  │  │★★★★★  │  │★★★☆☆  │  │★★★★☆  │               │
│  └───────┘  └───────┘  └───────┘  └───────┘               │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

**How it works:** Related products are from the same category (e.g., all "Fresh & Clean" scents) or share tags (e.g., "ocean," "coastal," "beach"). The system shows 4-6 products excluding the current product.

---

#### Frequently Bought Together in Cart

When viewing your shopping cart, see products commonly purchased alongside your cart items:

```
┌─────────────────────────────────────────────────────────────┐
│  Shopping Cart (1 item)                                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  [Image] Ocean Breeze Candle (8 oz)                        │
│          $24.99 × 1                        [Remove]         │
│                                                             │
│  Subtotal: $24.99                                          │
│                                                             │
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  Frequently Bought Together                                 │
│  Customers who bought this also added:                      │
│                                                             │
│  ┌───────────────────────────────────────────────────────┐ │
│  │  ☑ Ocean Breeze Room Spray (4 oz)      $14.99 [Add]  │ │
│  │  ☐ Gold Decorative Matches              $9.99 [Add]  │ │
│  │  ☐ Stainless Steel Wick Trimmer         $8.99 [Add]  │ │
│  │                                                        │ │
│  │  [Add Selected to Cart ($23.98)]                      │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
│  [Continue Shopping]           [Proceed to Checkout]        │
└─────────────────────────────────────────────────────────────┘
```

**How it works:** System analyzes past orders to find products frequently ordered together. Only shows associations with 10+ co-purchases to ensure statistical significance. Click [Add] to add individual items or check multiple boxes and click [Add Selected to Cart].

---

#### Personalized Recommendations on Homepage

Authenticated users see recommendations based on purchase history:

```
┌─────────────────────────────────────────────────────────────┐
│  Candle Store                          Hi, Alex!  Cart (1)  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Recommended for You                                        │
│  Based on your previous purchases                           │
│                                                             │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐          │
│  │     │ │     │ │     │ │     │ │     │ │     │          │
│  │ New │ │Floral│ │Rose │ │Lily │ │Peony│ │Jasmi│          │
│  │Vanilla│Garden│Petals│Valley│Bloom│ ne  │          │
│  │     │ │     │ │     │ │     │ │     │ │     │          │
│  │$19.99│$24.99│$27.99│$26.99│$29.99│$31.99│          │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘          │
│                                                             │
│  Recently Viewed                                            │
│                                                             │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                           │
│  │Ocean│ │Beach│ │Coastal│Sea  │                           │
│  │Breeze│House│ │ Mist │Salt │                           │
│  │$24.99│$26.99│$24.99│$29.99│                           │
│  └─────┘ └─────┘ └─────┘ └─────┘                           │
└─────────────────────────────────────────────────────────────┘
```

**How it works:** For authenticated users, recommendations are drawn from categories they've previously purchased from. If Alex bought 3 floral candles, homepage shows more floral products. Guest users see "Trending Now" instead—most popular products by view/purchase count.

---

### Admin Experience: Monitoring Recommendation Performance

#### Accessing Recommendation Analytics

1. Log into Admin Panel
2. Navigate to Reports > Recommendations
3. View performance metrics for each recommendation type

**Analytics Dashboard:**

```
┌─────────────────────────────────────────────────────────────┐
│  Recommendation Performance Report                          │
│  Date Range: Last 30 Days                    [Export CSV]   │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Overview Metrics                                           │
│  ┌───────────────┬──────────┬──────────┬──────────┐       │
│  │ Type          │ Shown    │ Clicked  │ Conv Rate│       │
│  ├───────────────┼──────────┼──────────┼──────────┤       │
│  │ Related Prods │ 45,230   │ 7,892    │ 17.5%    │       │
│  │ Freq Bought   │ 12,450   │ 1,867    │ 15.0%    │       │
│  │ Personalized  │ 23,100   │ 4,158    │ 18.0%    │       │
│  │ Trending      │ 18,900   │ 2,835    │ 15.0%    │       │
│  └───────────────┴──────────┴──────────┴──────────┘       │
│                                                             │
│  Revenue Impact                                             │
│  Revenue from Recommendations: $18,450                      │
│  AOV Increase: +18.3%                                      │
│  Items per Order (with recs): 2.4 → 2.9                   │
│                                                             │
│  Top Performing Product Pairs (Frequently Bought Together) │
│  1. Ocean Breeze Candle + Ocean Breeze Room Spray (65%)   │
│  2. Lavender Dreams + Sleep Pillow Spray (58%)            │
│  3. Vanilla Bourbon + Vanilla Room Spray (54%)            │
│  4. Any Premium Candle + Gold Decorative Matches (48%)    │
│  5. Rose Garden + Rose Petal Diffuser Oil (42%)           │
└─────────────────────────────────────────────────────────────┘
```

**Key Metrics Explained:**
- **Shown:** Number of times recommendation was displayed to customers
- **Clicked:** Number of clicks on recommended products
- **Conv Rate:** Click-through rate (Clicked / Shown)
- **Revenue Impact:** Total revenue from orders containing recommended items
- **AOV Increase:** Average order value lift when recommendations are clicked

---

### Configuration and Settings

#### Admin Settings for Recommendations

Navigate to Admin Panel > Settings > Recommendations:

**Display Settings:**
- **Enable Related Products:** Toggle on/off (default: ON)
- **Related Products Count:** Number (default: 4, range: 3-8)
- **Enable Frequently Bought Together:** Toggle on/off (default: ON)
- **FBT Count:** Number (default: 3, range: 2-6)
- **Enable Personalized Recommendations:** Toggle on/off (default: ON)
- **Personalized Count:** Number (default: 8, range: 6-12)
- **Enable Recently Viewed:** Toggle on/off (default: ON)
- **Recently Viewed Count:** Number (default: 6, range: 4-10)

**Association Computation Settings:**
- **Minimum Co-Purchase Count:** Number (default: 10) - Minimum orders required to create FBT association
- **Association Refresh Frequency:** Dropdown (Daily, Weekly) - How often to recompute associations
- **Association Refresh Time:** Time (default: 02:00 AM) - When background job runs

**Tracking Settings:**
- **Enable Product View Tracking:** Toggle on/off (default: ON)
- **View Retention Period:** Days (default: 90) - How long to keep view history
- **Track Anonymous Users:** Toggle on/off (default: ON) - Track views from non-logged-in users

---

### Integration with Other Systems

#### Integration with Product Catalog (Task 013)

Recommendations query product data to ensure:
- Only active products (IsActive = true) appear in recommendations
- Out-of-stock products (StockQuantity = 0) are excluded
- Product images, prices, and ratings display correctly
- Discontinued products automatically removed from associations

---

#### Integration with Order Management (Task 021)

Frequently bought together analyzes OrderItems table:
- Only completed orders (Status = Delivered or Completed) are analyzed
- Orders from last 12 months weighted more heavily in algorithm
- Cancelled/refunded orders excluded from association computation
- Purchase history powers personalized recommendations

---

#### Integration with Google Analytics (Task 027)

Recommendation clicks tracked as GA4 events:
- Event name: "select_promotion"
- Parameters: item_id (ProductId), creative_slot ("related-products", "frequently-bought-together", etc.), promotion_name
- Conversion tracking: When customer adds recommended product to cart and completes purchase
- A/B testing: Compare recommendation algorithms using GA4 experiments

---

### Best Practices

1. **Monitor Click-Through Rates Weekly:** Review recommendation analytics to identify underperforming types. If related products CTR drops below 12%, consider adjusting count or algorithm. High-performing recommendation types should guide inventory and marketing decisions.

2. **Refresh Associations Regularly:** Run ProductAssociationJob nightly (default) to keep frequently bought together suggestions current. After major promotions or seasonal changes, manually trigger job to update associations faster.

3. **Exclude Recently Purchased Products:** Personalized recommendations should exclude products customer bought in last 30 days to avoid suggesting items they already own. This improves perceived relevance.

4. **Use Recently Viewed Strategically:** Display recently viewed on homepage and category pages to help customers pick up where they left off. Limit to 6 items to avoid overwhelming display.

5. **Test Recommendation Counts:** Experiment with showing 3 vs 4 vs 6 frequently bought together items. More isn't always better—3 focused suggestions often convert better than 6 diluted ones. Use A/B testing to find optimal count for your audience.

6. **Leverage Trending for New Customers:** Guest users and new accounts see trending products. Ensure your most popular, highest-rated products have good visibility and appeal to convert first-time visitors.

7. **Create Manual Overrides for Strategic Products:** While most recommendations are automatic, allow manual curation for key products. If launching new product, temporarily boost it in related product suggestions to accelerate initial sales.

---

### Troubleshooting

**Problem:** Frequently bought together showing no recommendations for popular product

**Solution:** Check if product has 10+ completed orders (minimum threshold). New products won't have associations until sufficient order data exists. Temporarily reduce minimum threshold in settings (Admin > Settings > Recommendations > Minimum Co-Purchase Count) from 10 to 5 for new products. Verify ProductAssociationJob is running nightly (check logs for "ProductAssociationJob completed" entries).

---

**Problem:** Personalized recommendations same for all customers (not personalized)

**Solution:** Verify customers have purchase history. Personalization requires at least 1 completed order. Guest users always receive generic trending products (expected behavior). Check that recommendation tracking is enabled (Settings > Recommendations > Enable Product View Tracking = ON). Review database: query ProductViews and OrderItems tables to confirm data is being captured.

---

**Problem:** Related products showing out-of-stock items

**Solution:** Related product query should filter IsActive = true AND StockQuantity > 0. Check RecommendationService.GetRelatedProductsAsync implementation includes stock check. Verify Product.StockQuantity is being updated correctly when inventory depletes. Cache invalidation may be delayed—recommendations are cached 15 minutes, so recently out-of-stock products may appear briefly until cache expires.

---

**Problem:** Recently viewed products not displaying for guest users

**Solution:** Recently viewed uses session cookies. Verify browser allows cookies (not in incognito/private mode with strict settings). Check that ProductViewTrackingMiddleware is registered in API Startup.cs. Inspect browser network tab: POST /api/recommendations/track-view should fire on each product page load. Check ProductViews table has SessionId populated for guest users.

---

**Problem:** Recommendations causing slow page load times

**Solution:** Recommendations are cached for 15 minutes to prevent repeated database queries. Verify caching is enabled (check IMemoryCache usage in RecommendationService). If still slow, reduce recommendation counts (Settings > Recommendations > Related Products Count from 6 to 4). Check ProductAssociationJob completed successfully—if job failed, on-the-fly FBT calculation is very slow. Review database indexes: ProductId, CategoryId, and OrderId columns should be indexed.

---

**Problem:** Click-through rate on recommendations very low (< 5%)

**Solution:** Low CTR indicates recommendations aren't relevant. Review association algorithm—may need tuning. Check if product images are displaying (broken images deter clicks). Verify recommendations are visually distinct from surrounding content (use borders, background color). Test different recommendation titles: "You May Also Like" vs "Complete Your Set" vs "Customers Also Bought" have different CTRs. Consider reducing count—12 recommendations may be overwhelming; try 4-6 focused suggestions.
## Acceptance Criteria / Definition of Done

### Core Functionality - Related Products

- [ ] Related products API endpoint GET /api/recommendations/related/{productId} returns products from same category
- [ ] Related products query excludes the current product (productId parameter)
- [ ] Related products query filters to only active products (IsActive = true)
- [ ] Related products query filters to only in-stock products (StockQuantity > 0)
- [ ] Related products are limited to configurable count (default 4, max 8)
- [ ] Related products are ordered by relevance score (matching tags > same subcategory > same parent category)
- [ ] Related products display on product detail page below product description
- [ ] Related products section has title "You May Also Like" or "Related Products"
- [ ] Each related product shows image, name, price, rating, and [Add to Cart] button
- [ ] Clicking related product navigates to that product's detail page
- [ ] Related products are cached for 15 minutes per product ID to reduce database load

### Core Functionality - Frequently Bought Together

- [ ] Frequently bought together API endpoint GET /api/recommendations/frequently-bought-together/{productId} returns associated products
- [ ] FBT query returns products from ProductAssociation table with AssociationType = FrequentlyBoughtTogether
- [ ] FBT associations require minimum 10 co-purchases (configurable threshold)
- [ ] FBT products are limited to configurable count (default 3, max 6)
- [ ] FBT products are ordered by association score descending (most frequent first)
- [ ] FBT products exclude current product and products already in cart
- [ ] FBT products filter to only active and in-stock items
- [ ] FBT displays on product detail page in "Frequently Bought Together" section
- [ ] FBT displays on shopping cart page suggesting additions to cart items
- [ ] Each FBT product shows checkbox, image, name, price, and [Add] button
- [ ] Multiple FBT products can be added to cart simultaneously with [Add Selected] button
- [ ] FBT section shows social proof text: "Customers who bought X also added these items"

### Core Functionality - Personalized Recommendations

- [ ] Personalized recommendations API endpoint GET /api/recommendations/personalized returns customer-specific products
- [ ] For authenticated users, query analyzes customer's OrderItems to identify preferred categories
- [ ] Personalized query weights recent purchases (last 90 days) more heavily than old purchases
- [ ] Personalized recommendations exclude products customer purchased in last 30 days
- [ ] Personalized recommendations exclude products currently in customer's wishlist
- [ ] For guest users, personalized endpoint returns trending products instead (most viewed/purchased)
- [ ] Personalized recommendations are limited to configurable count (default 8, max 12)
- [ ] Personalized recommendations display on homepage in "Recommended for You" section
- [ ] Section title adapts: "Recommended for You" (authenticated) vs "Trending Now" (guest)
- [ ] Personalized recommendations refresh every 24 hours per customer to reflect new behavior

### Core Functionality - Recently Viewed

- [ ] Product view tracking occurs on every product detail page load
- [ ] Product view creates/updates ProductView record with ProductId, SessionId, CustomerId, ViewedAt
- [ ] Session ID is generated and stored in cookie for anonymous users
- [ ] Product views are deduplicated—viewing same product twice updates ViewedAt timestamp
- [ ] Recently viewed API endpoint GET /api/recommendations/recently-viewed returns last N viewed products
- [ ] Recently viewed query uses SessionId for anonymous users, CustomerId for authenticated users
- [ ] Recently viewed products are ordered by ViewedAt descending (most recent first)
- [ ] Recently viewed products are limited to configurable count (default 6, max 10)
- [ ] Recently viewed excludes current product if viewing product detail page
- [ ] Recently viewed displays on homepage in "Recently Viewed" section
- [ ] Recently viewed section is hidden if user has 0 view history

### Core Functionality - Trending Products

- [ ] Trending products query ranks by composite score: (ViewCount * 0.3) + (PurchaseCount * 0.7)
- [ ] Trending calculation uses data from last 30 days (configurable time window)
- [ ] Trending products are recalculated nightly via background job
- [ ] Trending products are cached for 1 hour to reduce computation
- [ ] Trending products exclude out-of-stock and inactive products
- [ ] Trending products are limited to configurable count (default 8)
- [ ] Trending products display for guest users as "Trending Now" or "Customer Favorites"
- [ ] Trending section shows view count or purchase count as social proof (e.g., "147 customers love this")

### Background Job - ProductAssociationJob

- [ ] ProductAssociationJob runs on configurable schedule (default: nightly at 2:00 AM)
- [ ] Job queries OrderItems from completed orders (Status = Delivered or Completed)
- [ ] Job calculates co-purchase frequency for all product pairs in same orders
- [ ] Job creates ProductAssociation records with ProductId1, ProductId2, AssociationScore, AssociationType
- [ ] Association score is count of orders containing both products
- [ ] Job only creates associations with score >= configured minimum (default 10)
- [ ] Job deletes old associations below threshold (products that stopped co-selling)
- [ ] Job completion is logged: "ProductAssociationJob completed. Created X associations, deleted Y associations."
- [ ] Job failure sends alert notification to admin email
- [ ] Job runs in transaction to ensure data consistency

### API Endpoints

- [ ] GET /api/recommendations/related/{productId}?count=4 returns related products
- [ ] GET /api/recommendations/frequently-bought-together/{productId}?count=3 returns FBT products
- [ ] GET /api/recommendations/personalized?count=8 returns personalized or trending products
- [ ] GET /api/recommendations/recently-viewed?count=6 returns recently viewed products
- [ ] GET /api/recommendations/trending?count=8 returns trending products explicitly
- [ ] POST /api/recommendations/track-view with body {productId, sessionId} records product view
- [ ] All endpoints return ApiResponse<List<ProductListDto>> with standard structure
- [ ] All endpoints include CORS headers for cross-origin requests
- [ ] All endpoints support count parameter validation (min 1, max 20)
- [ ] All endpoints return empty array if no recommendations available (not 404)

### UI/UX - Recommendation Widgets

- [ ] Recommendation widgets use consistent styling across all placements
- [ ] Product cards in recommendations show image, name, price, rating
- [ ] Product card images are square (1:1 aspect ratio) with lazy loading
- [ ] Product cards show "Out of Stock" badge if StockQuantity = 0
- [ ] Product cards show sale price with original price struck through if on sale
- [ ] Hovering product card shows border highlight or shadow effect
- [ ] [Add to Cart] buttons on recommendation cards add product without page navigation
- [ ] Success notification appears after adding recommended product: "Added to cart"
- [ ] Recommendation sections have clear, descriptive titles
- [ ] Sections are visually separated from main content (border, background color, spacing)
- [ ] Mobile responsive: recommendations stack vertically or scroll horizontally on small screens

### UI/UX - Recently Viewed Widget

- [ ] Recently viewed displays as horizontal scrollable carousel on mobile
- [ ] Carousel has left/right arrow buttons for navigation
- [ ] Recently viewed section is sticky or positioned prominently on homepage
- [ ] Recently viewed shows timestamps for each product (e.g., "Viewed 2 hours ago")
- [ ] Clearing browser data removes recently viewed history (expected behavior documented)

### Performance - Recommendation Operations

- [ ] GET /api/recommendations/related/{productId} responds in < 200ms for cached products
- [ ] GET /api/recommendations/frequently-bought-together responds in < 150ms (pre-computed associations)
- [ ] GET /api/recommendations/personalized responds in < 300ms including purchase history analysis
- [ ] GET /api/recommendations/recently-viewed responds in < 100ms (simple session query)
- [ ] Product view tracking (POST /api/recommendations/track-view) completes in < 50ms
- [ ] ProductAssociationJob processes 10,000 orders in < 15 minutes
- [ ] Recommendation caching reduces database queries by 70%+ for popular products
- [ ] Homepage with 3 recommendation sections (personalized, trending, recently viewed) loads in < 2 seconds

### Data Persistence - Tracking Tables

- [ ] ProductView entity persisted to ProductViews table
- [ ] ProductView.Id is GUID primary key
- [ ] ProductView.ProductId foreign key references Products.Id with ON DELETE CASCADE
- [ ] ProductView.CustomerId foreign key references Customers.Id with ON DELETE SET NULL (nullable)
- [ ] ProductView.SessionId is NVARCHAR(50) for anonymous session tracking
- [ ] ProductView.ViewedAt is DATETIME2 with automatic timestamp
- [ ] Index exists on (ProductId, ViewedAt) for fast recently viewed queries
- [ ] Index exists on (SessionId, ViewedAt) for anonymous user queries
- [ ] Index exists on (CustomerId, ViewedAt) for authenticated user queries
- [ ] Unique constraint on (ProductId, SessionId) prevents duplicate guest views per session
- [ ] Unique constraint on (ProductId, CustomerId) prevents duplicate authenticated views

### Data Persistence - Association Tables

- [ ] ProductAssociation entity persisted to ProductAssociations table
- [ ] ProductAssociation.Id is GUID primary key
- [ ] ProductAssociation.ProductId1 foreign key references Products.Id with ON DELETE CASCADE
- [ ] ProductAssociation.ProductId2 foreign key references Products.Id with ON DELETE CASCADE
- [ ] ProductAssociation.AssociationScore is INT storing co-purchase count
- [ ] ProductAssociation.AssociationType is NVARCHAR(50) enum: "FrequentlyBoughtTogether", "Related"
- [ ] ProductAssociation.CreatedAt and UpdatedAt timestamps track when association was computed
- [ ] Index exists on (ProductId1, AssociationType, AssociationScore) for fast FBT queries
- [ ] Unique constraint on (ProductId1, ProductId2, AssociationType) prevents duplicates
- [ ] Check constraint ensures ProductId1 != ProductId2 (product cannot be associated with itself)

### Edge Cases - Recommendation Generation

- [ ] New products with < 10 co-purchases show related products only (no FBT until sufficient data)
- [ ] Products with no category show trending products instead of related products
- [ ] Customers with no purchase history receive trending products (not empty personalized)
- [ ] Single-product orders contribute to view data but not FBT associations (can't co-purchase alone)
- [ ] Discontinued products are automatically removed from all recommendations and associations
- [ ] If requested count exceeds available recommendations, return fewer (don't error)
- [ ] If all related products are out of stock, recommendation section is hidden (not shown empty)

### Edge Cases - Product View Tracking

- [ ] Viewing same product multiple times updates ViewedAt timestamp (doesn't create duplicate records)
- [ ] Product views from bots/crawlers are filtered out (user-agent check)
- [ ] Product views on 404 pages (invalid product ID) are not tracked
- [ ] Excessive views from same IP (>100/hour) are rate-limited to prevent abuse/DOS
- [ ] ProductViews older than retention period (default 90 days) are deleted by cleanup job

### Edge Cases - Frequently Bought Together

- [ ] FBT for products that are only self-purchased (never with other products) shows trending products fallback
- [ ] FBT associations with score < threshold are excluded even if no other recommendations exist
- [ ] Products frequently bought together with themselves (quantity > 1 in order) are handled by ignoring self-associations
- [ ] Orders with > 20 items are excluded from FBT calculation (bulk/wholesale orders skew data)

### Security - Recommendation System

- [ ] Product view tracking rate limited to 100 views per IP per hour
- [ ] SessionId is validated as alphanumeric only (prevents injection attacks)
- [ ] ProductId parameters are validated as GUID format before database queries
- [ ] Count parameters are validated as integers between 1-20 (prevents resource exhaustion)
- [ ] Recommendation endpoints do not expose customer PII (email, address, etc.)
- [ ] Customer purchase history in personalized recommendations is only accessible to that customer
- [ ] Admin recommendation analytics require Admin role (401 for unauthorized users)

### Documentation

- [ ] API documentation includes all recommendation endpoints with request/response examples
- [ ] README includes recommendation system architecture diagram
- [ ] Admin user guide explains how to interpret recommendation analytics
- [ ] Developer documentation explains how to add new recommendation strategies
## Testing Requirements

### Unit Tests

**Test 1: RecommendationService - GetRelatedProductsAsync - Returns Products From Same Category**

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using AutoFixture;
using CandleStore.Application.Services;
using CandleStore.Application.DTOs.Products;
using CandleStore.Application.Interfaces;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CandleStore.Tests.Unit.Application.Services
{
    public class RecommendationServiceTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<RecommendationService>> _mockLogger;
        private readonly RecommendationService _sut;

        public RecommendationServiceTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProductRepository = new Mock<IProductRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<RecommendationService>>();

            _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepository.Object);

            _sut = new RecommendationService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetRelatedProductsAsync_WithValidProduct_ReturnsProductsFromSameCategory()
        {
            // Arrange
            var categoryId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var sourceProduct = _fixture.Build<Product>()
                .With(p => p.Id, productId)
                .With(p => p.CategoryId, categoryId)
                .With(p => p.IsActive, true)
                .Create();

            var relatedProducts = _fixture.Build<Product>()
                .With(p => p.CategoryId, categoryId)
                .With(p => p.IsActive, true)
                .With(p => p.StockQuantity, 10)
                .CreateMany(4)
                .ToList();

            _mockProductRepository
                .Setup(r => r.GetByIdAsync(productId))
                .ReturnsAsync(sourceProduct);

            _mockProductRepository
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
                .ReturnsAsync(relatedProducts);

            var expectedDtos = _fixture.CreateMany<ProductListDto>(4).ToList();
            _mockMapper
                .Setup(m => m.Map<List<ProductListDto>>(relatedProducts))
                .Returns(expectedDtos);

            // Act
            var result = await _sut.GetRelatedProductsAsync(productId, count: 4);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(4);
            result.Should().BeEquivalentTo(expectedDtos);

            _mockProductRepository.Verify(r => r.GetByIdAsync(productId), Times.Once);
            _mockProductRepository.Verify(r => r.FindAsync(It.Is<Expression<Func<Product, bool>>>(
                expr => true // Expression verified by Moq
            )), Times.Once);
        }
    }
}
```

---

**Test 2: RecommendationService - GetFrequentlyBoughtTogetherAsync - Returns Associated Products**

```csharp
[Fact]
public async Task GetFrequentlyBoughtTogetherAsync_WithValidAssociations_ReturnsTopAssociatedProducts()
{
    // Arrange
    var productId = Guid.NewGuid();
    var associatedProductIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

    var associations = _fixture.Build<ProductAssociation>()
        .With(a => a.ProductId1, productId)
        .With(a => a.AssociationType, AssociationType.FrequentlyBoughtTogether)
        .With(a => a.AssociationScore, 50)
        .CreateMany(3)
        .Zip(associatedProductIds, (assoc, prodId) => {
            assoc.ProductId2 = prodId;
            return assoc;
        })
        .ToList();

    var mockAssociationRepository = new Mock<IProductAssociationRepository>();
    _mockUnitOfWork.Setup(u => u.ProductAssociations).Returns(mockAssociationRepository.Object);

    mockAssociationRepository
        .Setup(r => r.GetAssociationsAsync(productId, AssociationType.FrequentlyBoughtTogether, 3))
        .ReturnsAsync(associations);

    var associatedProducts = associatedProductIds.Select(id =>
        _fixture.Build<Product>()
            .With(p => p.Id, id)
            .With(p => p.IsActive, true)
            .Create()
    ).ToList();

    _mockProductRepository
        .Setup(r => r.GetByIdsAsync(associatedProductIds))
        .ReturnsAsync(associatedProducts);

    var expectedDtos = _fixture.CreateMany<ProductListDto>(3).ToList();
    _mockMapper
        .Setup(m => m.Map<List<ProductListDto>>(associatedProducts))
        .Returns(expectedDtos);

    // Act
    var result = await _sut.GetFrequentlyBoughtTogetherAsync(productId, count: 3);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(3);
    mockAssociationRepository.Verify(r => r.GetAssociationsAsync(
        productId,
        AssociationType.FrequentlyBoughtTogether,
        3
    ), Times.Once);
}
```

---

**Test 3: RecommendationService - GetPersonalizedRecommendationsAsync - Returns Category-Based Recommendations**

```csharp
[Fact]
public async Task GetPersonalizedRecommendationsAsync_WithPurchaseHistory_ReturnsPreferredCategoryProducts()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var preferredCategoryId = Guid.NewGuid();

    var mockOrderRepository = new Mock<IOrderRepository>();
    _mockUnitOfWork.Setup(u => u.Orders).Returns(mockOrderRepository.Object);

    mockOrderRepository
        .Setup(r => r.GetCustomerPreferredCategoriesAsync(customerId))
        .ReturnsAsync(new List<Guid> { preferredCategoryId });

    var recommendations = _fixture.Build<Product>()
        .With(p => p.CategoryId, preferredCategoryId)
        .With(p => p.IsActive, true)
        .With(p => p.IsFeatured, true)
        .CreateMany(8)
        .ToList();

    _mockProductRepository
        .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Product, bool>>>()))
        .ReturnsAsync(recommendations);

    var expectedDtos = _fixture.CreateMany<ProductListDto>(8).ToList();
    _mockMapper
        .Setup(m => m.Map<List<ProductListDto>>(recommendations))
        .Returns(expectedDtos);

    // Act
    var result = await _sut.GetPersonalizedRecommendationsAsync(customerId, count: 8);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(8);
}
```

---

**Test 4: RecommendationService - TrackProductViewAsync - Creates Or Updates View Record**

```csharp
[Fact]
public async Task TrackProductViewAsync_WithNewView_CreatesProductViewRecord()
{
    // Arrange
    var productId = Guid.NewGuid();
    var sessionId = "session-abc-123";
    Guid? customerId = Guid.NewGuid();

    var mockProductViewRepository = new Mock<IProductViewRepository>();
    _mockUnitOfWork.Setup(u => u.ProductViews).Returns(mockProductViewRepository.Object);

    mockProductViewRepository
        .Setup(r => r.GetByProductAndCustomerAsync(productId, customerId))
        .ReturnsAsync((ProductView)null); // No existing view

    mockProductViewRepository
        .Setup(r => r.AddAsync(It.IsAny<ProductView>()))
        .ReturnsAsync((ProductView pv) => pv);

    _mockUnitOfWork
        .Setup(u => u.SaveChangesAsync())
        .ReturnsAsync(1);

    // Act
    await _sut.TrackProductViewAsync(productId, sessionId, customerId);

    // Assert
    mockProductViewRepository.Verify(r => r.AddAsync(It.Is<ProductView>(pv =>
        pv.ProductId == productId &&
        pv.SessionId == sessionId &&
        pv.CustomerId == customerId &&
        pv.ViewedAt != default
    )), Times.Once);

    _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
}
```

---

**Test 5: RecommendationService - GetRecentlyViewedAsync - Returns Ordered By ViewedAt Descending**

```csharp
[Fact]
public async Task GetRecentlyViewedAsync_WithSessionId_ReturnsProductsOrderedByMostRecent()
{
    // Arrange
    var sessionId = "session-xyz-789";
    var now = DateTime.UtcNow;

    var productViews = new List<ProductView>
    {
        _fixture.Build<ProductView>()
            .With(pv => pv.SessionId, sessionId)
            .With(pv => pv.ViewedAt, now.AddHours(-1))
            .Create(),
        _fixture.Build<ProductView>()
            .With(pv => pv.SessionId, sessionId)
            .With(pv => pv.ViewedAt, now.AddMinutes(-30))
            .Create(),
        _fixture.Build<ProductView>()
            .With(pv => pv.SessionId, sessionId)
            .With(pv => pv.ViewedAt, now.AddMinutes(-10))
            .Create()
    };

    // Load products
    foreach (var pv in productViews)
    {
        pv.Product = _fixture.Build<Product>().With(p => p.Id, pv.ProductId).Create();
    }

    var mockProductViewRepository = new Mock<IProductViewRepository>();
    _mockUnitOfWork.Setup(u => u.ProductViews).Returns(mockProductViewRepository.Object);

    mockProductViewRepository
        .Setup(r => r.GetRecentlyViewedAsync(sessionId, null, 6))
        .ReturnsAsync(productViews);

    var expectedDtos = _fixture.CreateMany<ProductListDto>(3).ToList();
    _mockMapper
        .Setup(m => m.Map<List<ProductListDto>>(It.IsAny<List<Product>>()))
        .Returns(expectedDtos);

    // Act
    var result = await _sut.GetRecentlyViewedAsync(sessionId, count: 6);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(3);
}
```

---

### Integration Tests

**Test 1: RecommendationsController - GET /api/recommendations/related/{productId} - Returns Related Products**

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using CandleStore.Api;
using CandleStore.Application.DTOs;
using CandleStore.Application.DTOs.Products;

namespace CandleStore.Tests.Integration.Api.Controllers
{
    public class RecommendationsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public RecommendationsControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetRelatedProducts_WithValidProductId_ReturnsRelatedProducts()
        {
            // Arrange
            var productId = await SeedProductWithRelatedProducts();

            // Act
            var response = await _client.GetAsync($"/api/recommendations/related/{productId}?count=4");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductListDto>>>();
            apiResponse.Should().NotBeNull();
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Should().HaveCountGreaterThan(0);
            apiResponse.Data.Should().HaveCountLessThanOrEqualTo(4);
        }
    }
}
```

---

**Test 2: RecommendationsController - GET /api/recommendations/frequently-bought-together/{productId}**

```csharp
[Fact]
public async Task GetFrequentlyBoughtTogether_WithAssociations_ReturnsAssociatedProducts()
{
    // Arrange
    var productId = await SeedProductWithAssociations();

    // Act
    var response = await _client.GetAsync($"/api/recommendations/frequently-bought-together/{productId}?count=3");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<List<ProductListDto>>>();
    apiResponse.Should().NotBeNull();
    apiResponse.Success.Should().BeTrue();
    apiResponse.Data.Should().NotBeNull();
    apiResponse.Data.Should().HaveCount(3);
}
```

---

**Test 3: Product View Tracking - POST /api/recommendations/track-view**

```csharp
[Fact]
public async Task TrackProductView_WithValidData_CreatesViewRecord()
{
    // Arrange
    var productId = await SeedTestProduct();
    var sessionId = Guid.NewGuid().ToString();

    var trackRequest = new { productId, sessionId };

    // Act
    var response = await _client.PostAsJsonAsync("/api/recommendations/track-view", trackRequest);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify ProductView created in database
    using var scope = _factory.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CandleStoreDbContext>();
    var productView = await dbContext.ProductViews
        .FirstOrDefaultAsync(pv => pv.ProductId == productId && pv.SessionId == sessionId);
    productView.Should().NotBeNull();
    productView.ViewedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
}
```

---

### End-to-End (E2E) Tests

**Scenario 1: Customer Views Product And Sees Related Products**

1. Customer navigates to product detail page for "Ocean Breeze Candle"
2. **Expected:** Product detail page loads with product information
3. Customer scrolls down below product description
4. **Expected:** "You May Also Like" section displays with 4-6 product cards
5. **Expected:** Related products are from same category (Fresh & Clean scents)
6. **Expected:** Each product card shows image, name, price, rating, and [Add to Cart] button
7. Customer clicks on "Coastal Mist Candle" in related products
8. **Expected:** Navigate to Coastal Mist product detail page
9. **Expected:** Related products section shows different recommendations based on Coastal Mist

---

**Scenario 2: Customer Adds Product To Cart And Sees Frequently Bought Together**

1. Customer adds "Vanilla Bourbon Candle" to shopping cart
2. Customer clicks [View Cart] or navigates to /cart
3. **Expected:** Shopping cart displays with Vanilla Bourbon candle ($24.99)
4. **Expected:** "Frequently Bought Together" section appears below cart items
5. **Expected:** Section shows 3 products: Room Spray ($14.99), Decorative Matches ($9.99), Wick Trimmer ($8.99)
6. **Expected:** Each product has checkbox (unchecked by default) and [Add] button
7. Customer checks room spray and matches checkboxes
8. Customer clicks [Add Selected to Cart ($24.98)]
9. **Expected:** Both items added to cart
10. **Expected:** Cart total updates from $24.99 to $49.97
11. **Expected:** Frequently bought together section refreshes with new suggestions based on current cart

---

**Scenario 3: Authenticated Customer Sees Personalized Homepage Recommendations**

1. Customer logs in to account with purchase history (3 previous orders: 2 floral candles, 1 vanilla)
2. Customer navigates to homepage
3. **Expected:** "Recommended for You" section displays prominently
4. **Expected:** Section subtitle: "Based on your previous purchases"
5. **Expected:** 8 product recommendations displayed
6. **Expected:** Majority of recommendations are floral category (matching purchase history)
7. **Expected:** Recommendations exclude products customer purchased in last 30 days
8. Customer scrolls down
9. **Expected:** "Recently Viewed" section shows last 6 products viewed (from previous session)
10. Customer clicks product in recently viewed section
11. **Expected:** Navigate to product detail page

---

### Performance Tests

**Benchmark 1: Related Products API Performance**

```csharp
[Benchmark]
public async Task GetRelatedProducts_CachedProduct()
{
    var response = await _client.GetAsync($"/api/recommendations/related/{_popularProductId}?count=4");
    var content = await response.Content.ReadAsStringAsync();
}
```

**Target:** < 200ms for cached related products
**Pass Criteria:** 95th percentile < 300ms
**Rationale:** Related products appear on every product detail page—must load quickly to avoid delaying page render

---

**Benchmark 2: Frequently Bought Together Query Performance**

```csharp
[Benchmark]
public async Task GetFrequentlyBoughtTogether_PrecomputedAssociations()
{
    var response = await _client.GetAsync($"/api/recommendations/frequently-bought-together/{_productId}?count=3");
    var content = await response.Content.ReadAsStringAsync();
}
```

**Target:** < 150ms for FBT recommendations (pre-computed associations)
**Pass Criteria:** 95th percentile < 200ms
**Rationale:** FBT uses pre-computed ProductAssociation table—should be very fast since no complex on-the-fly calculation

---

**Benchmark 3: Product Association Job Performance**

```csharp
[Benchmark]
public async Task ProductAssociationJob_Process_10000_Orders()
{
    var job = _factory.Services.GetRequiredService<ProductAssociationJob>();
    await job.ExecuteAsync();
}
```

**Target:** Process 10,000 orders and compute associations in < 15 minutes
**Pass Criteria:** < 900 seconds (15 minutes)
**Rationale:** Job runs nightly when traffic is low—acceptable to take 10-15 minutes for comprehensive analysis

---

### Regression Tests

**Regression Test 1: Product Detail Page Not Broken By Recommendations**

- Verify product detail page displays correctly with and without related products feature enabled
- Ensure product information, images, [Add to Cart] button all function normally
- Verify related products section can be hidden via admin settings without breaking page

---

**Regression Test 2: Shopping Cart Functionality Unchanged**

- Verify cart calculations (subtotal, tax, shipping) remain correct when FBT products added
- Ensure remove from cart works for both original cart items and FBT-added items
- Verify checkout process works normally regardless of whether products came from FBT suggestions

---

## User Verification Steps

### Verification 1: View Related Products on Product Page

1. Navigate to any product detail page
2. Scroll down below product description and specifications
3. **Verify:** "You May Also Like" or "Related Products" section displays
4. **Verify:** Section shows 4-6 product cards
5. **Verify:** Products are from same category or have similar attributes
6. **Verify:** Each card shows product image, name, price, star rating
7. Click on one of the related products
8. **Verify:** Navigate to that product's detail page
9. **Verify:** New product page also has related products section with different recommendations

---

### Verification 2: Add Frequently Bought Together Products From Cart

1. Add a product to shopping cart
2. Navigate to /cart page
3. **Verify:** "Frequently Bought Together" section displays below cart items
4. **Verify:** Section shows 2-4 product suggestions with images, names, prices
5. **Verify:** Each product has checkbox and [Add] button
6. Check the checkboxes for 2 products
7. Click [Add Selected to Cart] button
8. **Verify:** Both products are added to cart
9. **Verify:** Cart total updates correctly
10. **Verify:** FBT section refreshes with new suggestions

---

### Verification 3: View Personalized Recommendations (Authenticated User)

1. Log in with account that has purchase history
2. Navigate to homepage
3. **Verify:** "Recommended for You" section displays
4. **Verify:** Subtitle says "Based on your previous purchases"
5. **Verify:** 6-12 product recommendations shown
6. **Verify:** Recommendations match categories from your past orders
7. **Verify:** Products you recently purchased are NOT in recommendations
8. Click one recommended product
9. **Verify:** Navigate to product detail page

---

### Verification 4: Track Recently Viewed Products

1. Browse 5 different product detail pages without logging in
2. Navigate to homepage
3. **Verify:** "Recently Viewed" section displays
4. **Verify:** Shows up to 6 of your recently viewed products
5. **Verify:** Products are ordered by most recent first
6. Clear browser cookies/data
7. Return to homepage
8. **Verify:** Recently viewed section is empty or hidden (expected—data cleared)

---

### Verification 5: Admin Views Recommendation Analytics

1. Log in to Admin Panel as admin user
2. Navigate to Reports > Recommendations
3. **Verify:** Analytics dashboard displays with performance metrics
4. **Verify:** Table shows each recommendation type with "Shown", "Clicked", "Conv Rate" columns
5. **Verify:** Revenue Impact section shows total revenue from recommendations
6. **Verify:** Top Performing Product Pairs section lists FBT combinations
7. Click [Export CSV] button
8. **Verify:** CSV file downloads with recommendation performance data

---

## Implementation Prompt for Claude

### Implementation Overview

You are implementing a Product Recommendations Engine for the Candle Store e-commerce platform. This system provides intelligent product suggestions using five strategies: Related Products (category/tag matching), Frequently Bought Together (order co-purchase analysis), Personalized Recommendations (purchase history + browsing behavior), Recently Viewed Products (session tracking), and Trending Products (popularity metrics). The system includes API endpoints, background jobs for pre-computing associations, product view tracking, and analytics.

### Prerequisites

**Required Completed Tasks:**
- Task 013 (Product Management API) - Product catalog and repository
- Task 021 (Order Management API) - Order data for FBT and personalization
- Task 027 (Google Analytics) - Track recommendation click-through rates

**NuGet Packages:**
- AutoMapper (already installed)
- Microsoft.Extensions.Caching.Memory (already installed for caching)
- No additional packages required

### Step-by-Step Implementation

#### Step 1: Create Domain Entities

**File:** `src/CandleStore.Domain/Entities/ProductView.cs`

```csharp
using System;

namespace CandleStore.Domain.Entities
{
    public class ProductView
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string SessionId { get; set; }
        public Guid? CustomerId { get; set; }
        public DateTime ViewedAt { get; set; }

        // Navigation
        public Product Product { get; set; }
        public Customer Customer { get; set; }
    }
}
```

**File:** `src/CandleStore.Domain/Entities/ProductAssociation.cs`

```csharp
namespace CandleStore.Domain.Entities
{
    public class ProductAssociation
    {
        public Guid Id { get; set; }
        public Guid ProductId1 { get; set; }
        public Guid ProductId2 { get; set; }
        public int AssociationScore { get; set; }
        public string AssociationType { get; set; } // "FrequentlyBoughtTogether", "Related"
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public Product Product1 { get; set; }
        public Product Product2 { get; set; }
    }
}
```

#### Step 2: Create Recommendation Service

**File:** `src/CandleStore.Application/Interfaces/IRecommendationService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandleStore.Application.DTOs.Products;

namespace CandleStore.Application.Interfaces
{
    public interface IRecommendationService
    {
        Task<List<ProductListDto>> GetRelatedProductsAsync(Guid productId, int count = 4);
        Task<List<ProductListDto>> GetFrequentlyBoughtTogetherAsync(Guid productId, int count = 3);
        Task<List<ProductListDto>> GetPersonalizedRecommendationsAsync(Guid? customerId, int count = 8);
        Task<List<ProductListDto>> GetRecentlyViewedAsync(string sessionId, Guid? customerId, int count = 6);
        Task<List<ProductListDto>> GetTrendingProductsAsync(int count = 8);
        Task TrackProductViewAsync(Guid productId, string sessionId, Guid? customerId);
    }
}
```

**File:** `src/CandleStore.Application/Services/RecommendationService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CandleStore.Application.DTOs.Products;
using CandleStore.Application.Interfaces;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;

namespace CandleStore.Application.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMemoryCache cache,
            ILogger<RecommendationService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<ProductListDto>> GetRelatedProductsAsync(Guid productId, int count = 4)
        {
            var cacheKey = $"related_{productId}_{count}";
            if (_cache.TryGetValue(cacheKey, out List<ProductListDto> cached))
                return cached;

            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
                return new List<ProductListDto>();

            var relatedProducts = await _unitOfWork.Products
                .GetQueryable()
                .Where(p => p.CategoryId == product.CategoryId &&
                           p.Id != productId &&
                           p.IsActive &&
                           p.StockQuantity > 0)
                .Take(count)
                .ToListAsync();

            var dtos = _mapper.Map<List<ProductListDto>>(relatedProducts);

            _cache.Set(cacheKey, dtos, TimeSpan.FromMinutes(15));
            return dtos;
        }

        public async Task<List<ProductListDto>> GetFrequentlyBoughtTogetherAsync(Guid productId, int count = 3)
        {
            var associations = await _unitOfWork.ProductAssociations
                .GetAssociationsAsync(productId, "FrequentlyBoughtTogether", count);

            var productIds = associations.Select(a => a.ProductId2).ToList();
            var products = await _unitOfWork.Products.GetByIdsAsync(productIds);

            return _mapper.Map<List<ProductListDto>>(products);
        }

        public async Task TrackProductViewAsync(Guid productId, string sessionId, Guid? customerId)
        {
            var existing = customerId.HasValue
                ? await _unitOfWork.ProductViews.GetByProductAndCustomerAsync(productId, customerId.Value)
                : await _unitOfWork.ProductViews.GetByProductAndSessionAsync(productId, sessionId);

            if (existing != null)
            {
                existing.ViewedAt = DateTime.UtcNow;
                await _unitOfWork.ProductViews.UpdateAsync(existing);
            }
            else
            {
                var view = new ProductView
                {
                    Id = Guid.NewGuid(),
                    ProductId = productId,
                    SessionId = sessionId,
                    CustomerId = customerId,
                    ViewedAt = DateTime.UtcNow
                };
                await _unitOfWork.ProductViews.AddAsync(view);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        // Additional methods: GetPersonalizedRecommendationsAsync, GetRecentlyViewedAsync, GetTrendingProductsAsync
    }
}
```

#### Step 3: Create API Controller

**File:** `src/CandleStore.Api/Controllers/RecommendationsController.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CandleStore.Application.DTOs;
using CandleStore.Application.DTOs.Products;
using CandleStore.Application.Interfaces;

namespace CandleStore.Api.Controllers
{
    [ApiController]
    [Route("api/recommendations")]
    public class RecommendationsController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationsController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("related/{productId}")]
        public async Task<ActionResult<ApiResponse<List<ProductListDto>>>> GetRelatedProducts(
            Guid productId,
            [FromQuery] int count = 4)
        {
            var products = await _recommendationService.GetRelatedProductsAsync(productId, count);

            return Ok(new ApiResponse<List<ProductListDto>>
            {
                Success = true,
                Data = products
            });
        }

        [HttpGet("frequently-bought-together/{productId}")]
        public async Task<ActionResult<ApiResponse<List<ProductListDto>>>> GetFrequentlyBoughtTogether(
            Guid productId,
            [FromQuery] int count = 3)
        {
            var products = await _recommendationService.GetFrequentlyBoughtTogetherAsync(productId, count);

            return Ok(new ApiResponse<List<ProductListDto>>
            {
                Success = true,
                Data = products
            });
        }

        [HttpGet("personalized")]
        public async Task<ActionResult<ApiResponse<List<ProductListDto>>>> GetPersonalizedRecommendations(
            [FromQuery] int count = 8)
        {
            var customerId = User.Identity.IsAuthenticated
                ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                : (Guid?)null;

            var products = await _recommendationService.GetPersonalizedRecommendationsAsync(customerId, count);

            return Ok(new ApiResponse<List<ProductListDto>>
            {
                Success = true,
                Data = products
            });
        }

        [HttpPost("track-view")]
        public async Task<ActionResult<ApiResponse<bool>>> TrackProductView([FromBody] TrackViewRequest request)
        {
            var customerId = User.Identity.IsAuthenticated
                ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                : (Guid?)null;

            await _recommendationService.TrackProductViewAsync(
                request.ProductId,
                request.SessionId,
                customerId);

            return Ok(new ApiResponse<bool> { Success = true, Data = true });
        }
    }

    public class TrackViewRequest
    {
        public Guid ProductId { get; set; }
        public string SessionId { get; set; }
    }
}
```

### Integration Points

**With Task 013 (Product Management):**
- Recommendations query IProductRepository for product data
- Only active, in-stock products included in recommendations

**With Task 021 (Order Management):**
- FBT analyzes OrderItems to find co-purchase patterns
- Personalization uses customer's OrderItems for category preferences

**With Task 027 (Google Analytics):**
- Track recommendation clicks as GA4 events: "select_promotion"
- Measure CTR and conversion rates by recommendation type

### Common Pitfalls to Avoid

❌ **Don't:** Compute frequently bought together on-the-fly (too slow)
✅ **Do:** Pre-compute associations nightly via background job, query ProductAssociation table

❌ **Don't:** Return recommendations without caching (database overload)
✅ **Do:** Cache recommendations for 15 minutes, especially for popular products

❌ **Don't:** Show recommendations for out-of-stock products
✅ **Do:** Filter all recommendations to StockQuantity > 0 and IsActive = true

---

**END OF TASK 031**
### Verification 6: Test Guest User Trending Products

1. Open browser in incognito mode (not logged in)
2. Navigate to homepage
3. **Verify:** "Trending Now" or "Customer Favorites" section displays
4. **Verify:** Section shows 6-8 popular products
5. **Verify:** Products have high review counts and ratings (these are the most popular)
6. Add one trending product to cart without logging in
7. **Verify:** Product adds successfully to guest cart
8. Navigate to cart page
9. **Verify:** FBT suggestions display based on guest cart item
10. **Verify:** FBT products are relevant to cart item

---

### Verification 7: Verify Product View Tracking Creates Records

1. Browse to product detail page for "Ocean Breeze Candle"
2. Open browser developer tools > Network tab
3. **Verify:** POST request to /api/recommendations/track-view fires on page load
4. **Verify:** Request payload contains productId and sessionId
5. **Verify:** Response returns 200 OK status
6. Browse to 3 more different product pages
7. Navigate to homepage
8. **Verify:** "Recently Viewed" section shows the 4 products you just viewed
9. **Verify:** Products are ordered with most recent first
10. Log in with account credentials
11. **Verify:** Recently viewed persists (uses customerId instead of sessionId after login)

---

### Verification 8: Verify Related Products Exclude Current Product

1. Navigate to product detail page for "Lavender Dreams Candle"
2. Scroll to "Related Products" section
3. **Verify:** Section displays 4-6 products
4. **Verify:** "Lavender Dreams Candle" does NOT appear in related products (excluded as current product)
5. **Verify:** All related products are from same category (Floral scents)
6. **Verify:** All related products show "In Stock" or quantity available
7. Click one of the related products
8. **Verify:** Navigate to that product's page
9. **Verify:** Related products section updates with different recommendations
10. **Verify:** Previously viewed product may now appear in this product's related section

---

### Verification 9: Test FBT With Multiple Cart Items

1. Add "Vanilla Bourbon Candle" to cart
2. Navigate to cart page
3. **Verify:** FBT section shows 3 products related to Vanilla Bourbon
4. Add "Ocean Breeze Candle" to cart (second product)
5. **Verify:** FBT section refreshes with new recommendations
6. **Verify:** FBT now shows products that pair with BOTH Vanilla Bourbon AND Ocean Breeze
7. Check 2 FBT products and click [Add Selected to Cart]
8. **Verify:** Both FBT products added successfully
9. **Verify:** FBT section refreshes again with suggestions for all 4 cart items
10. **Verify:** Products already in cart do NOT appear in FBT suggestions

---

### Verification 10: Admin Can Configure Recommendation Settings

1. Log in to Admin Panel as admin user
2. Navigate to Settings > Recommendations
3. **Verify:** "Enable Related Products" toggle displays (default: ON)
4. **Verify:** "Related Products Count" number input displays (default: 4)
5. Change Related Products Count to 6 and click [Save]
6. **Verify:** Success message: "Settings updated successfully"
7. Open storefront in new tab, navigate to any product page
8. **Verify:** Related products section now shows 6 products instead of 4
9. Return to Admin Settings, toggle "Enable Related Products" to OFF
10. **Verify:** Related products section disappears from storefront product pages
11. Toggle back to ON and save
12. **Verify:** Related products reappear on storefront

---

### Additional Troubleshooting Scenarios

**Problem:** Background job ProductAssociationJob failing with timeout error

**Solution:** Job is attempting to process too many orders in single batch. Check job logs for "Processing 50,000 orders..." message. If order count exceeds 20,000, optimize query with batching. Update ProductAssociationJob.ExecuteAsync to process orders in batches of 5,000:

```csharp
var totalOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
var batchSize = 5000;
var batches = (int)Math.Ceiling(totalOrders / (double)batchSize);

for (int i = 0; i < batches; i++)
{
    var batchOrders = await _context.Orders
        .Where(o => o.Status == OrderStatus.Delivered)
        .Skip(i * batchSize)
        .Take(batchSize)
        .Include(o => o.OrderItems)
        .ToListAsync();

    await ProcessOrderBatch(batchOrders);
}
```

Alternatively, increase job timeout in Startup.cs from 15 minutes to 30 minutes for large catalogs. Monitor job execution time—if consistently >25 minutes, consider running bi-weekly instead of nightly or filtering to last 90 days of orders only.

---

**Problem:** Personalized recommendations showing same products to all customers despite different purchase histories

**Solution:** Verify personalization algorithm is using customer-specific data. Check RecommendationService.GetPersonalizedRecommendationsAsync implementation queries OrderItems filtered by customerId:

```csharp
var customerOrders = await _unitOfWork.Orders
    .GetQueryable()
    .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Delivered)
    .Include(o => o.OrderItems)
    .ToListAsync();

var purchasedCategoryIds = customerOrders
    .SelectMany(o => o.OrderItems)
    .Select(oi => oi.Product.CategoryId)
    .Distinct()
    .ToList();
```

If purchasedCategoryIds is empty for all customers, issue is likely in Order/OrderItem navigation property configuration. Verify OrderItem entity has `public Product Product { get; set; }` navigation property and EF Core is loading it via `.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)`. Check database—OrderItems table must have ProductId foreign key populated. If ProductId is null, OrderService.CreateOrderAsync is not properly setting ProductId when creating order items.

---

**Problem:** Related products showing products from different categories

**Solution:** Related products query should filter by matching CategoryId. Verify RecommendationService.GetRelatedProductsAsync:

```csharp
var relatedProducts = await _unitOfWork.Products
    .GetQueryable()
    .Where(p => p.CategoryId == sourceProduct.CategoryId && // Same category
               p.Id != productId && // Exclude current product
               p.IsActive &&
               p.StockQuantity > 0)
    .OrderBy(p => p.Name) // Or custom relevance score
    .Take(count)
    .ToListAsync();
```

If query is correct but results still show wrong categories, check Product.CategoryId in database. Some products may have CategoryId = NULL or incorrect CategoryId due to data migration issues. Run SQL query to find orphaned products:

```sql
SELECT Id, Name, CategoryId
FROM Products
WHERE CategoryId IS NULL
   OR CategoryId NOT IN (SELECT Id FROM Categories);
```

Fix orphaned products by assigning valid CategoryId. Consider adding database constraint `ALTER TABLE Products ADD CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)` to prevent future data integrity issues.

---

**Problem:** Recently viewed products showing duplicates of same product

**Solution:** ProductView table should have unique constraint preventing duplicate entries per session/customer. Check database schema:

```sql
-- For guest users (session-based)
CREATE UNIQUE INDEX IX_ProductViews_ProductId_SessionId
ON ProductViews (ProductId, SessionId)
WHERE CustomerId IS NULL;

-- For authenticated users (customer-based)
CREATE UNIQUE INDEX IX_ProductViews_ProductId_CustomerId
ON ProductViews (ProductId, CustomerId)
WHERE CustomerId IS NOT NULL;
```

If unique constraints exist but duplicates still appear, RecommendationService.TrackProductViewAsync may not be checking for existing records before inserting. Verify upsert logic:

```csharp
var existing = customerId.HasValue
    ? await _unitOfWork.ProductViews.GetByProductAndCustomerAsync(productId, customerId.Value)
    : await _unitOfWork.ProductViews.GetByProductAndSessionAsync(productId, sessionId);

if (existing != null)
{
    existing.ViewedAt = DateTime.UtcNow; // Update timestamp
    await _unitOfWork.ProductViews.UpdateAsync(existing);
}
else
{
    var view = new ProductView { ... }; // Create new
    await _unitOfWork.ProductViews.AddAsync(view);
}
```

If logic is correct, race condition may occur when user rapidly refreshes product page. Add pessimistic locking or catch DbUpdateException for unique constraint violations and retry with update instead of insert.

---

**Problem:** FBT recommendations showing products with low association scores (<5 co-purchases)

**Solution:** Minimum co-purchase threshold configured too low. Navigate to Admin Panel > Settings > Recommendations > Minimum Co-Purchase Count. Default should be 10. If set to 1-5, associations based on insufficient data may appear, causing poor recommendations. Increase to 10-15 for statistically significant associations. After changing setting, manually trigger ProductAssociationJob to recompute associations:

```bash
# Via admin panel
Admin Panel > Tools > Background Jobs > ProductAssociationJob > [Run Now]

# Or via API (requires admin authentication)
POST /api/admin/jobs/product-association/execute
```

Job will delete associations with score < new threshold and only retain high-confidence pairings. Monitor FBT click-through rate after change—should increase from ~8% to ~12-15% with higher-quality recommendations.

---

**Problem:** Homepage loads slowly (>3 seconds) when displaying multiple recommendation sections

**Solution:** Multiple uncached recommendation queries executing simultaneously. Enable parallel execution and verify caching:

```csharp
// In homepage Blazor component OnInitializedAsync
await Task.WhenAll(
    LoadPersonalizedRecommendationsAsync(),
    LoadTrendingProductsAsync(),
    LoadRecentlyViewedAsync()
);
```

This executes all three recommendation queries in parallel instead of sequentially, reducing load time from 3 seconds to ~1 second. Verify each RecommendationService method caches results:

```csharp
public async Task<List<ProductListDto>> GetTrendingProductsAsync(int count = 8)
{
    var cacheKey = $"trending_{count}";
    if (_cache.TryGetValue(cacheKey, out List<ProductListDto> cached))
    {
        _logger.LogInformation("Returning cached trending products");
        return cached;
    }

    // Query database
    var trending = await QueryTrendingProducts(count);

    // Cache for 1 hour
    _cache.Set(cacheKey, trending, TimeSpan.FromHours(1));
    return trending;
}
```

Check application logs for "Returning cached..." messages. If not appearing, caching is failing. Common cause: IMemoryCache not registered in DI container. Verify Startup.cs:

```csharp
builder.Services.AddMemoryCache(); // Required in ConfigureServices
```

For multi-instance deployments (load balanced), replace IMemoryCache with distributed cache (Redis) to share cached recommendations across instances.

---

## Implementation Prompt for Claude (Extended)

### Implementation Overview

You are implementing a comprehensive Product Recommendations Engine for the Candle Store e-commerce platform. This system provides intelligent product suggestions using five distinct strategies: Related Products (category/tag matching), Frequently Bought Together (order co-purchase analysis via collaborative filtering), Personalized Recommendations (purchase history + browsing behavior), Recently Viewed Products (session tracking), and Trending Products (popularity metrics). The implementation spans multiple layers of Clean Architecture: Domain entities (ProductView, ProductAssociation), Application services (RecommendationService with caching and business logic), Infrastructure repositories (ProductViewRepository, ProductAssociationRepository), API endpoints (RecommendationsController), background jobs (ProductAssociationJob for nightly computation), and Blazor UI components (recommendation widgets).

### Prerequisites

**Required Completed Tasks:**
- Task 001 (Solution Structure) - Clean Architecture setup
- Task 002 (NuGet Packages) - AutoMapper, EF Core, FluentValidation
- Task 013 (Product Management API) - Product catalog, IProductRepository, ProductListDto
- Task 021 (Order Management API) - Order/OrderItem entities, order history queries
- Task 027 (Google Analytics) - Track recommendation click-through rates as GA4 events

**NuGet Packages Needed:**
- AutoMapper (already installed from Task 002)
- Microsoft.Extensions.Caching.Memory (already installed)
- Microsoft.EntityFrameworkCore (already installed from Task 002)
- Hangfire or Quartz.NET (for background job scheduling) - Install via:

```bash
dotnet add src/CandleStore.Api package Hangfire --version 1.8.0
dotnet add src/CandleStore.Api package Hangfire.AspNetCore --version 1.8.0
dotnet add src/CandleStore.Api package Hangfire.PostgreSql --version 1.20.0
# OR for SQL Server:
# dotnet add src/CandleStore.Api package Hangfire.SqlServer --version 1.8.0
```

**Database Migration Required:**
After implementing entities, create migration:

```bash
dotnet ef migrations add AddRecommendationTables --project src/CandleStore.Infrastructure --startup-project src/CandleStore.Api
dotnet ef database update --project src/CandleStore.Infrastructure --startup-project src/CandleStore.Api
```

---

### Step-by-Step Implementation

#### Step 1: Create Domain Entities

**File:** `src/CandleStore.Domain/Entities/ProductView.cs`

```csharp
using System;

namespace CandleStore.Domain.Entities
{
    /// <summary>
    /// Tracks every product page view for recently viewed functionality and analytics.
    /// Supports both anonymous users (SessionId) and authenticated users (CustomerId).
    /// </summary>
    public class ProductView
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Product that was viewed
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// Session identifier for anonymous users (from cookie or browser storage)
        /// Format: "session-{guid}" generated client-side
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// Customer identifier for authenticated users
        /// Null for guest/anonymous users
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Timestamp when product was viewed
        /// Updated on subsequent views of same product (upsert pattern)
        /// </summary>
        public DateTime ViewedAt { get; set; }

        // Navigation properties
        public Product Product { get; set; }
        public Customer Customer { get; set; }
    }
}
```

**File:** `src/CandleStore.Domain/Entities/ProductAssociation.cs`

```csharp
using System;

namespace CandleStore.Domain.Entities
{
    /// <summary>
    /// Stores pre-computed product associations for frequently bought together and related product recommendations.
    /// Updated nightly by ProductAssociationJob background job.
    /// </summary>
    public class ProductAssociation
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Source product (the product customer is currently viewing)
        /// </summary>
        public Guid ProductId1 { get; set; }

        /// <summary>
        /// Associated product (the product to recommend)
        /// </summary>
        public Guid ProductId2 { get; set; }

        /// <summary>
        /// Association strength score
        /// For FrequentlyBoughtTogether: count of orders containing both products
        /// Higher score = stronger association = higher recommendation priority
        /// </summary>
        public int AssociationScore { get; set; }

        /// <summary>
        /// Type of association
        /// Values: "FrequentlyBoughtTogether", "Related"
        /// </summary>
        public string AssociationType { get; set; }

        /// <summary>
        /// Timestamp when association was first computed
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Timestamp when association was last updated by background job
        /// Used to identify stale associations for cleanup
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public Product Product1 { get; set; }
        public Product Product2 { get; set; }
    }
}
```

**File:** `src/CandleStore.Domain/Enums/AssociationType.cs`

```csharp
namespace CandleStore.Domain.Enums
{
    public static class AssociationType
    {
        public const string FrequentlyBoughtTogether = "FrequentlyBoughtTogether";
        public const string Related = "Related";
    }
}
```

---

#### Step 2: Configure Entity Framework Mappings

**File:** `src/CandleStore.Infrastructure/Persistence/Configurations/ProductViewConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CandleStore.Domain.Entities;

namespace CandleStore.Infrastructure.Persistence.Configurations
{
    public class ProductViewConfiguration : IEntityTypeConfiguration<ProductView>
    {
        public void Configure(EntityTypeBuilder<ProductView> builder)
        {
            builder.ToTable("ProductViews");

            builder.HasKey(pv => pv.Id);

            builder.Property(pv => pv.SessionId)
                .IsRequired()
                .HasMaxLength(100); // "session-{guid}" format

            builder.Property(pv => pv.ViewedAt)
                .IsRequired();

            // Foreign key to Products with cascade delete
            builder.HasOne(pv => pv.Product)
                .WithMany()
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to Customers with SET NULL (preserve view data if customer deleted)
            builder.HasOne(pv => pv.Customer)
                .WithMany()
                .HasForeignKey(pv => pv.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            builder.HasIndex(pv => new { pv.ProductId, pv.ViewedAt })
                .HasDatabaseName("IX_ProductViews_ProductId_ViewedAt");

            builder.HasIndex(pv => new { pv.SessionId, pv.ViewedAt })
                .HasDatabaseName("IX_ProductViews_SessionId_ViewedAt");

            builder.HasIndex(pv => new { pv.CustomerId, pv.ViewedAt })
                .HasDatabaseName("IX_ProductViews_CustomerId_ViewedAt");

            // Unique constraint: one view record per product per session (guest users)
            builder.HasIndex(pv => new { pv.ProductId, pv.SessionId })
                .IsUnique()
                .HasDatabaseName("IX_ProductViews_ProductId_SessionId_Unique")
                .HasFilter("CustomerId IS NULL"); // Only for guest users

            // Unique constraint: one view record per product per customer (authenticated users)
            builder.HasIndex(pv => new { pv.ProductId, pv.CustomerId })
                .IsUnique()
                .HasDatabaseName("IX_ProductViews_ProductId_CustomerId_Unique")
                .HasFilter("CustomerId IS NOT NULL"); // Only for authenticated users
        }
    }
}
```

**File:** `src/CandleStore.Infrastructure/Persistence/Configurations/ProductAssociationConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CandleStore.Domain.Entities;

namespace CandleStore.Infrastructure.Persistence.Configurations
{
    public class ProductAssociationConfiguration : IEntityTypeConfiguration<ProductAssociation>
    {
        public void Configure(EntityTypeBuilder<ProductAssociation> builder)
        {
            builder.ToTable("ProductAssociations");

            builder.HasKey(pa => pa.Id);

            builder.Property(pa => pa.AssociationScore)
                .IsRequired();

            builder.Property(pa => pa.AssociationType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(pa => pa.CreatedAt)
                .IsRequired();

            builder.Property(pa => pa.UpdatedAt)
                .IsRequired();

            // Foreign keys with cascade delete
            builder.HasOne(pa => pa.Product1)
                .WithMany()
                .HasForeignKey(pa => pa.ProductId1)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pa => pa.Product2)
                .WithMany()
                .HasForeignKey(pa => pa.ProductId2)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade cycles

            // Index for fast FBT queries: "Get all products associated with ProductId1 of type FBT, ordered by score"
            builder.HasIndex(pa => new { pa.ProductId1, pa.AssociationType, pa.AssociationScore })
                .HasDatabaseName("IX_ProductAssociations_ProductId1_Type_Score");

            // Unique constraint: one association record per product pair per type
            builder.HasIndex(pa => new { pa.ProductId1, pa.ProductId2, pa.AssociationType })
                .IsUnique()
                .HasDatabaseName("IX_ProductAssociations_ProductId1_ProductId2_Type_Unique");

            // Check constraint: product cannot be associated with itself
            builder.HasCheckConstraint("CK_ProductAssociations_NoSelfAssociation",
                "ProductId1 <> ProductId2");
        }
    }
}
```

**Update DbContext:**

**File:** `src/CandleStore.Infrastructure/Persistence/CandleStoreDbContext.cs`

```csharp
// Add to DbContext class:
public DbSet<ProductView> ProductViews { get; set; }
public DbSet<ProductAssociation> ProductAssociations { get; set; }

// In OnModelCreating method:
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations

    modelBuilder.ApplyConfiguration(new ProductViewConfiguration());
    modelBuilder.ApplyConfiguration(new ProductAssociationConfiguration());
}
```

---

#### Step 3: Create Repository Interfaces

**File:** `src/CandleStore.Application/Interfaces/Repositories/IProductViewRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandleStore.Domain.Entities;

namespace CandleStore.Application.Interfaces.Repositories
{
    public interface IProductViewRepository : IRepository<ProductView>
    {
        /// <summary>
        /// Get product view for specific product and customer (authenticated user)
        /// </summary>
        Task<ProductView> GetByProductAndCustomerAsync(Guid productId, Guid customerId);

        /// <summary>
        /// Get product view for specific product and session (guest user)
        /// </summary>
        Task<ProductView> GetByProductAndSessionAsync(Guid productId, string sessionId);

        /// <summary>
        /// Get recently viewed products for session or customer, ordered by ViewedAt descending
        /// </summary>
        /// <param name="sessionId">Session identifier (for guest users)</param>
        /// <param name="customerId">Customer identifier (for authenticated users)</param>
        /// <param name="count">Number of products to return (default 10)</param>
        Task<List<ProductView>> GetRecentlyViewedAsync(string sessionId, Guid? customerId, int count = 10);

        /// <summary>
        /// Delete product views older than retention period (for data cleanup)
        /// </summary>
        /// <param name="retentionDays">Number of days to retain view history (default 90)</param>
        Task DeleteOldViewsAsync(int retentionDays = 90);
    }
}
```

**File:** `src/CandleStore.Application/Interfaces/Repositories/IProductAssociationRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandleStore.Domain.Entities;

namespace CandleStore.Application.Interfaces.Repositories
{
    public interface IProductAssociationRepository : IRepository<ProductAssociation>
    {
        /// <summary>
        /// Get product associations for a given product, filtered by association type and ordered by score descending
        /// </summary>
        /// <param name="productId">Source product ID</param>
        /// <param name="associationType">Type of association (FrequentlyBoughtTogether or Related)</param>
        /// <param name="count">Number of associations to return (default 10)</param>
        Task<List<ProductAssociation>> GetAssociationsAsync(Guid productId, string associationType, int count = 10);

        /// <summary>
        /// Delete associations with score below minimum threshold (for data cleanup)
        /// </summary>
        /// <param name="minimumScore">Minimum association score to retain (default 10)</param>
        Task DeleteLowScoreAssociationsAsync(int minimumScore = 10);

        /// <summary>
        /// Bulk upsert product associations (for background job)
        /// Creates new associations or updates existing ones
        /// </summary>
        Task BulkUpsertAssociationsAsync(List<ProductAssociation> associations);
    }
}
```

**Update IUnitOfWork:**

**File:** `src/CandleStore.Application/Interfaces/Repositories/IUnitOfWork.cs`

```csharp
// Add to IUnitOfWork interface:
public interface IUnitOfWork : IDisposable
{
    // ... existing repositories
    IProductViewRepository ProductViews { get; }
    IProductAssociationRepository ProductAssociations { get; }

    Task<int> SaveChangesAsync();
}
```

---

#### Step 4: Implement Repositories

**File:** `src/CandleStore.Infrastructure/Persistence/Repositories/ProductViewRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;

namespace CandleStore.Infrastructure.Persistence.Repositories
{
    public class ProductViewRepository : Repository<ProductView>, IProductViewRepository
    {
        public ProductViewRepository(CandleStoreDbContext context) : base(context)
        {
        }

        public async Task<ProductView> GetByProductAndCustomerAsync(Guid productId, Guid customerId)
        {
            return await _context.ProductViews
                .FirstOrDefaultAsync(pv => pv.ProductId == productId && pv.CustomerId == customerId);
        }

        public async Task<ProductView> GetByProductAndSessionAsync(Guid productId, string sessionId)
        {
            return await _context.ProductViews
                .FirstOrDefaultAsync(pv => pv.ProductId == productId && pv.SessionId == sessionId && pv.CustomerId == null);
        }

        public async Task<List<ProductView>> GetRecentlyViewedAsync(string sessionId, Guid? customerId, int count = 10)
        {
            var query = _context.ProductViews.AsQueryable();

            if (customerId.HasValue)
            {
                // Authenticated user: query by CustomerId
                query = query.Where(pv => pv.CustomerId == customerId.Value);
            }
            else
            {
                // Guest user: query by SessionId
                query = query.Where(pv => pv.SessionId == sessionId && pv.CustomerId == null);
            }

            return await query
                .Include(pv => pv.Product) // Load product details
                    .ThenInclude(p => p.ProductImages)
                .OrderByDescending(pv => pv.ViewedAt) // Most recent first
                .Take(count)
                .ToListAsync();
        }

        public async Task DeleteOldViewsAsync(int retentionDays = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var oldViews = await _context.ProductViews
                .Where(pv => pv.ViewedAt < cutoffDate)
                .ToListAsync();

            _context.ProductViews.RemoveRange(oldViews);
            await _context.SaveChangesAsync();
        }
    }
}
```

**File:** `src/CandleStore.Infrastructure/Persistence/Repositories/ProductAssociationRepository.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;

namespace CandleStore.Infrastructure.Persistence.Repositories
{
    public class ProductAssociationRepository : Repository<ProductAssociation>, IProductAssociationRepository
    {
        public ProductAssociationRepository(CandleStoreDbContext context) : base(context)
        {
        }

        public async Task<List<ProductAssociation>> GetAssociationsAsync(Guid productId, string associationType, int count = 10)
        {
            return await _context.ProductAssociations
                .Where(pa => pa.ProductId1 == productId && pa.AssociationType == associationType)
                .OrderByDescending(pa => pa.AssociationScore) // Highest score first
                .Take(count)
                .Include(pa => pa.Product2) // Load associated product details
                .ToListAsync();
        }

        public async Task DeleteLowScoreAssociationsAsync(int minimumScore = 10)
        {
            var lowScoreAssociations = await _context.ProductAssociations
                .Where(pa => pa.AssociationScore < minimumScore)
                .ToListAsync();

            _context.ProductAssociations.RemoveRange(lowScoreAssociations);
            await _context.SaveChangesAsync();
        }

        public async Task BulkUpsertAssociationsAsync(List<ProductAssociation> associations)
        {
            foreach (var association in associations)
            {
                var existing = await _context.ProductAssociations
                    .FirstOrDefaultAsync(pa =>
                        pa.ProductId1 == association.ProductId1 &&
                        pa.ProductId2 == association.ProductId2 &&
                        pa.AssociationType == association.AssociationType);

                if (existing != null)
                {
                    // Update existing association
                    existing.AssociationScore = association.AssociationScore;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new association
                    association.Id = Guid.NewGuid();
                    association.CreatedAt = DateTime.UtcNow;
                    association.UpdatedAt = DateTime.UtcNow;
                    await _context.ProductAssociations.AddAsync(association);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
```

**Update UnitOfWork:**

**File:** `src/CandleStore.Infrastructure/Persistence/Repositories/UnitOfWork.cs`

```csharp
// Add to UnitOfWork class:
private IProductViewRepository _productViews;
private IProductAssociationRepository _productAssociations;

public IProductViewRepository ProductViews
{
    get { return _productViews ??= new ProductViewRepository(_context); }
}

public IProductAssociationRepository ProductAssociations
{
    get { return _productAssociations ??= new ProductAssociationRepository(_context); }
}
```

---

**END OF TASK 031 EXPANSION**
