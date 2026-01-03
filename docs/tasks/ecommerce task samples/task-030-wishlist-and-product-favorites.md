# Task 030: Wishlist and Product Favorites

**Priority:** 30 / 36
**Tier:** B
**Complexity:** 5 Fibonacci points
**Phase:** Phase 10 - Advanced Features
**Dependencies:** Task 021 (Customer Authentication & JWT), Task 014 (Shopping Cart API)

---

## Description

The Wishlist and Product Favorites feature enables customers to save products for future purchase consideration, creating a persistent shopping list that increases return visits and conversion rates. This feature bridges the gap between product discovery and purchase decision, allowing customers to curate their own product collections without the commitment of adding items to their cart. By providing both authenticated wishlist storage (database-backed) and guest wishlist capabilities (localStorage-backed with automatic merge on login), the feature ensures seamless user experience across authentication states while maximizing engagement opportunities.

From a business perspective, wishlists drive significant value: they increase average order value by 20-35% (customers often purchase multiple wishlist items in a single session), reduce cart abandonment by providing an alternative to immediate purchase pressure, and create marketing opportunities through price drop notifications and back-in-stock alerts. The feature generates approximately **$18,000 in additional annual revenue** for a store with 5,000 active customers (assuming 60% wishlist adoption, 3.5 items per wishlist, 25% eventual conversion rate, and $35 average product price: 5,000 × 0.60 × 3.5 × 0.25 × $35 = $18,375). Additionally, wishlist abandonment emails achieve 15-20% conversion rates, significantly outperforming standard promotional emails.

Technically, the feature integrates with the existing Product Catalog (Task 003), Shopping Cart (Task 014), Customer Authentication (Task 021), and Email Marketing (Task 025) systems. It uses a WishlistItem entity with composite unique constraint on (CustomerId, ProductId) to prevent duplicate entries, implements real-time synchronization between guest and authenticated wishlists, and provides background job scheduling for price drop and inventory notifications. The wishlist count badge in the header updates via Blazor state management, ensuring immediate visual feedback when items are added or removed.

The implementation follows Clean Architecture patterns with WishlistItem domain entity, IWishlistService application interface, WishlistService business logic layer, WishlistRepository for database access, WishlistController API endpoints, and Blazor components for both customer-facing wishlist page and inline "Add to Wishlist" buttons. The system supports wishlist sharing via unique shareable URLs (public/private toggle), move-to-cart bulk operations, and wishlist analytics for admin users (most favorited products, wishlist-to-purchase conversion rates, average time-to-conversion).

Key constraints include maximum wishlist size of 100 items per customer (prevents abuse and storage bloat), 90-day auto-cleanup of inactive wishlist items for guest users, rate limiting on notification emails (max 1 price drop email per product per customer per week), and performance optimization through Redis caching of wishlist counts and denormalized product data (name, price, image URL) stored with WishlistItem to minimize JOIN queries.

---

## Use Cases

### Use Case 1: Alex (Customer) Curates Holiday Gift List

**Scenario:** Alex browses the candle store in early November, searching for holiday gifts for 8 different people with varying scent preferences.

**Without This Feature:**
Alex visits the store, finds a candle that's perfect for her sister, but isn't ready to commit to purchasing yet since she's still browsing for other recipients. She takes a screenshot of the product URL, then repeats this process for multiple candles. By the end of her browsing session, she has 12 screenshots and a dozen browser tabs open. Three days later when she's ready to purchase, she can't remember which candles were for which recipients, two of the products are now out of stock (but she doesn't know to check back later), and she's frustrated by having to re-find each product by manually typing product names into the search bar. She abandons the purchase, resulting in $0 revenue for the store.

**With This Feature:**
As Alex browses, she clicks the heart icon on each candle that catches her interest. The wishlist count in the header increments immediately (showing "8 items"), providing visual confirmation. She adds notes to some items ("Mom - lavender allergy, try vanilla instead"). Three days later, she receives an email notification that one of her wishlist items is back in stock. She returns to the site, clicks "My Wishlist" in the header, sees all 8 products with thumbnails and current prices, selects 6 items, and clicks "Move All to Cart" followed by quick checkout. She receives a second email two weeks later notifying her that one of her remaining wishlist items is now on sale at 20% off, prompting her to return and complete a second purchase.

**Outcome:**
- **Revenue Generated:** $245 from initial purchase + $28 from follow-up sale = $273 total (vs. $0 without feature)
- **Conversion Rate:** 75% of wishlist items purchased (6 of 8 initially, 1 of 2 remaining)
- **Customer Satisfaction:** Alex leaves a 5-star review mentioning "loved being able to save my favorites"
- **Repeat Visit:** 2 additional site visits triggered by automated notifications

### Use Case 2: Sarah (Store Owner) Leverages Wishlist Data for Marketing

**Scenario:** Sarah wants to identify which products generate the most interest but have lower conversion rates, indicating potential pricing or product description issues.

**Without This Feature:**
Sarah relies solely on Google Analytics page view data, which shows that "Midnight Jasmine" candle has 2,500 views per month but only 45 sales (1.8% conversion). She doesn't know whether visitors are interested but hesitating due to price, uncertain about the scent, or simply browsing without intent. She makes a blind decision to reduce the price by 15%, which increases sales to 68 but erodes profit margin unnecessarily. The true issue was unclear product photography, not pricing.

**With This Feature:**
Sarah logs into the admin panel and navigates to Analytics → Wishlist Insights. She sees that "Midnight Jasmine" has been added to 380 wishlists (15.2% of viewers), indicating strong interest. She drills down and sees the average "time from wishlist add to purchase" is 45 days (vs. 18 days site average), and 62% of wishlist adds never convert. She sends a targeted "price drop" email to the 380 customers with this item wishlisted (even though price hasn't changed - the email just says "Still interested? Check it out!"), resulting in 76 immediate purchases (20% email conversion rate). She nets $2,660 in revenue from a 5-minute email campaign. She also identifies that the long conversion time suggests customer hesitation, so she adds more detailed scent notes and customer review photos to the product page.

**Outcome:**
- **Revenue Impact:** $2,660 from targeted email (cost: $0 beyond SendGrid usage)
- **ROI:** Infinite (no marginal cost for email campaign)
- **Data-Driven Decision:** Avoided unnecessary 15% price cut (saving $12 per unit × 68 sales = $816 in preserved margin)
- **Product Improvement:** Identified weak product page content, leading to ongoing conversion rate improvements

### Use Case 3: Jordan (Customer Service) Assists Customer with Lost Wishlist

**Scenario:** Customer Maria calls customer service upset because she "lost all her saved candles" after accidentally clearing her browser cookies.

**Without This Feature:**
Jordan has no way to help Maria recover her saved items. Maria describes the products from memory ("one was purple and smelled like flowers..."), but Jordan can't identify the specific products. Maria is frustrated and hangs up without placing an order. The customer service interaction takes 15 minutes and ends negatively, potentially losing Maria as a customer permanently.

**With This Feature:**
Jordan asks Maria for her email address, searches the admin panel for her customer account, and sees that Maria's wishlist contains 5 items (data persists server-side for authenticated users). Jordan emails Maria a direct link to her wishlist page. Maria clicks the link, logs in, and sees all 5 products intact. Alternatively, if Maria was a guest user, Jordan explains that creating an account will enable persistent wishlist storage, converting Maria from guest to registered customer. Maria creates an account, and Jordan explains that any future wishlist items will now be saved permanently.

**Outcome:**
- **Problem Resolution Time:** 3 minutes (vs. 15 minutes of futile searching)
- **Customer Satisfaction:** Maria completes her $89 purchase and thanks Jordan for the quick help
- **Account Creation:** Maria converts from guest to registered user, increasing lifetime value potential
- **Customer Retention:** Positive service experience increases likelihood of repeat purchase

---

## User Manual Documentation

### Overview

The Wishlist and Product Favorites feature allows customers to save products for future consideration without adding them to their shopping cart. Wishlists are persistent (stored server-side for authenticated users), shareable (via unique URLs), and integrated with notification systems for price drops and inventory alerts.

**When to Use Wishlists:**
- **Gift Planning:** Curate products for multiple recipients before committing to purchase
- **Budget Management:** Save expensive items to purchase later when budget allows
- **Product Comparison:** Collect similar products to compare before making final decision
- **Seasonal Shopping:** Save holiday/seasonal items months in advance
- **Registry Alternative:** Share wishlist link with friends/family for gift suggestions

**Key Features:**
- Add/remove products with single click
- Guest wishlist (localStorage) automatically merges with account wishlist on login
- Wishlist count badge in site header
- Move items from wishlist to cart (individually or bulk)
- Email notifications for price drops and back-in-stock alerts
- Shareable wishlist URLs (public/private)
- Product notes/comments on wishlist items
- Wishlist analytics for store owners

### Step-by-Step Instructions for Customers

#### Step 1: Adding Products to Wishlist

**From Product Card (Catalog Page):**
```
┌────────────────────────────────┐
│  [Product Image]               │
│                                │
│  Lavender Dreams               │
│  $24.99                        │
│                                │
│  [Add to Cart]  [♡ Wishlist]  │  ← Click heart icon
└────────────────────────────────┘
```

Click the heart icon (♡) on any product card. The icon fills (♥) and changes color to indicate the item is wishlisted. The wishlist count badge in the header increments immediately.

**From Product Detail Page:**
```
┌──────────────────────────────────────────┐
│  Lavender Dreams                         │
│  ★★★★★ (127 reviews)                     │
│                                          │
│  [Large Product Image]                   │
│                                          │
│  Price: $24.99                           │
│  Size: [8oz ▼]                           │
│  Quantity: [1] [-] [+]                   │
│                                          │
│  [Add to Cart]  [♥ Add to Wishlist]     │  ← Click to wishlist
└──────────────────────────────────────────┘
```

Click the "Add to Wishlist" button. A toast notification appears: "Added to your wishlist! View wishlist →" (clickable link).

**Result:** Product is saved to your wishlist. If you're not logged in, the item is stored in browser localStorage and will merge with your account wishlist when you log in.

#### Step 2: Viewing Your Wishlist

Navigate to **Account → My Wishlist** or click the wishlist icon in the header.

```
┌────────────────────────────────────────────────────────┐
│  My Wishlist (8 items)                    [Share ▼]    │
├────────────────────────────────────────────────────────┤
│                                                         │
│  ┌──────────────────────────────────────────────┐     │
│  │ ☑ [Image] Lavender Dreams          $24.99    │     │
│  │              Added: Nov 3, 2024              │     │
│  │              [Move to Cart] [Remove] [♥]     │     │
│  │              Note: Gift for Mom              │     │
│  └──────────────────────────────────────────────┘     │
│                                                         │
│  ┌──────────────────────────────────────────────┐     │
│  │ ☑ [Image] Midnight Jasmine         $29.99    │     │
│  │              Added: Nov 1, 2024              │     │
│  │              🔔 Price drop! Was $34.99       │     │
│  │              [Move to Cart] [Remove] [♥]     │     │
│  └──────────────────────────────────────────────┘     │
│                                                         │
│  [... 6 more items ...]                                │
│                                                         │
│  ☑ Select All    [Move Selected to Cart (2)]          │
└────────────────────────────────────────────────────────┘
```

**Wishlist Page Features:**
- Checkbox selection for bulk operations
- Individual product actions (Move to Cart, Remove, Add Note)
- Price change indicators (price drops shown in green)
- Out-of-stock indicators (with "Notify Me" option)
- Add personal notes to items
- Sort by: Date Added, Price, Product Name

#### Step 3: Moving Items to Cart

**Individual Item:**
Click the "Move to Cart" button on any wishlist item. The item is removed from the wishlist and added to the shopping cart with quantity = 1.

**Bulk Move:**
1. Check the boxes next to items you want to purchase
2. Click "Move Selected to Cart (X)" at the bottom of the page
3. All selected items are added to cart simultaneously
4. Toast notification: "Moved 3 items to cart. Proceed to checkout →"

**Result:** Items are in your shopping cart and removed from wishlist. The wishlist count decrements accordingly.

#### Step 4: Sharing Your Wishlist

**Create Shareable Link:**
1. On the wishlist page, click the "Share" dropdown
2. Select "Generate Shareable Link"
3. Choose visibility: **Public** (anyone with link) or **Private** (requires password)
4. Click "Generate Link"
5. Copy the generated URL: `https://candlestore.com/wishlist/share/a1b2c3d4e5f6`

```
┌─────────────────────────────────────────┐
│  Share Your Wishlist                    │
├─────────────────────────────────────────┤
│                                         │
│  Visibility: ○ Public  ● Private        │
│                                         │
│  Password (required for private):       │
│  [******************]                   │
│                                         │
│  Shareable Link:                        │
│  https://candlestore.com/wishlist/...   │
│                                         │
│  [Copy Link]  [Send via Email]          │
│                                         │
│  ⓘ Recipients can view your wishlist    │
│    and optionally purchase items for    │
│    you as gifts                         │
└─────────────────────────────────────────┘
```

**Use Cases for Sharing:**
- Send to family/friends before birthdays or holidays
- Share with wedding/event registries
- Collaborate with partner on home decor decisions

#### Step 5: Enabling Notifications

**Price Drop Alerts:**
1. On the wishlist page, each item has a "🔔 Notify" toggle
2. Enable for items you want to monitor
3. You'll receive an email when the price drops by 10% or more

**Back-in-Stock Alerts:**
- Automatically enabled for out-of-stock wishlist items
- You'll receive an email when the item is restocked
- Email includes direct "Add to Cart" link for quick purchase

**Notification Settings:**
Navigate to **Account → Settings → Notifications** to configure frequency:
- Immediate (as soon as change detected)
- Daily digest (1 email per day with all updates)
- Weekly digest (1 email per week)

### Configuration / Settings

**Wishlist Preferences (Customer Settings):**
- **Default Visibility:** Public or Private for shared wishlists
- **Notification Frequency:** Immediate, Daily, Weekly, Off
- **Auto-Remove After Purchase:** Enabled (default) or Disabled
- **Max Wishlist Size:** 100 items (hard limit to prevent abuse)

**Admin Settings (Store Configuration):**
- **Enable Guest Wishlists:** Yes (default) or No
- **Wishlist Item Limit:** 100 (configurable)
- **Guest Wishlist Expiration:** 90 days of inactivity
- **Price Drop Threshold:** 10% (minimum discount to trigger notification)
- **Notification Rate Limit:** 1 email per product per customer per 7 days

### Integration with Other Systems

**With Task 014 (Shopping Cart API):**
- "Move to Cart" calls `CartService.AddItemAsync(productId, quantity: 1)`
- Cart count badge updates immediately via Blazor state management
- If product has variants, user is prompted to select variant before moving to cart

**With Task 021 (Customer Authentication):**
- Authenticated users: Wishlist stored in database (`WishlistItems` table)
- Guest users: Wishlist stored in browser localStorage (`guest_wishlist` key)
- On login: `WishlistService.MergeGuestWishlistAsync()` combines localStorage items with database wishlist
- Duplicate detection: Prevents adding same product twice via unique constraint

**With Task 025 (Email Marketing System):**
- Price drop emails triggered by background job (`PriceDropNotificationJob.cs`)
- Back-in-stock emails triggered by inventory update events
- Email templates: `wishlist-price-drop.html`, `wishlist-back-in-stock.html`
- Unsubscribe link included in all notification emails

**With Task 029 (Product Reviews):**
- Wishlist items display review stars and count
- Customers who wishlisted a product are prompted to review after purchase

**With Task 003 (Product Catalog):**
- Wishlist stores ProductId foreign key
- Denormalized product data (name, price, imageUrl) cached in WishlistItem for performance
- Background job syncs denormalized data nightly in case product details change

### Best Practices

1. **Use Notes for Gift Planning:** Add recipient names or occasion notes to wishlist items to stay organized when shopping for multiple people. Example: "Gift for Dad - Father's Day 2024."

2. **Enable Notifications Selectively:** Only enable price drop notifications for expensive items you're monitoring. Enabling for all items results in notification fatigue and lower engagement.

3. **Share Private Wishlists for Gift Events:** Create a private wishlist for birthdays/holidays and share with close friends/family. Use a memorable password (e.g., "MySweetBirthday2024").

4. **Merge Guest Wishlist Before Purchasing:** If you've been browsing as a guest, create an account before checking out to preserve your wishlist for future visits.

5. **Review Wishlist Seasonally:** Set a quarterly reminder to review and prune your wishlist. Remove items you're no longer interested in to keep the list manageable and relevant.

6. **Combine with Cart for Comparison:** Add multiple similar products to your wishlist, then move them to your cart to see the total cost comparison before committing to one option.

7. **Use Wishlist for Budget Planning:** Add all desired items to wishlist, then prioritize and move items to cart based on available budget. This prevents impulse overspending.

### Troubleshooting

**Problem:** Items added to wishlist as guest disappeared after logging in.

**Solution:** This occurs if you added items as a guest, then created a new account instead of logging into an existing account. The guest wishlist merge only occurs when logging into an account that existed before adding the items. To prevent this, create your account first, then browse and add items to wishlist. If you've already lost items, browse the catalog and re-add them (logged in this time).

---

**Problem:** Not receiving price drop notification emails even though notification is enabled.

**Solution:** Check the following:
1. Verify email notification settings: Account → Settings → Notifications → "Wishlist Alerts" must be enabled
2. Check spam/junk folder for emails from noreply@candlestore.com
3. Ensure the price dropped by at least 10% (minimum threshold)
4. Note that notifications are rate-limited to 1 email per product per week to prevent spam
5. If the product was added to wishlist after the price drop occurred, you won't receive a notification (system only alerts on new changes)

---

**Problem:** "Move to Cart" button is grayed out for a wishlist item.

**Solution:** This occurs when:
- **Product is out of stock:** Click "Notify Me" to receive back-in-stock alert
- **Product has variants (size/scent) not selected:** Click the product to visit the detail page, select variant, then add to cart manually
- **Product has been discontinued:** Admin has soft-deleted the product; remove from wishlist as it's no longer available

---

**Problem:** Wishlist count shows "100 items" and won't let me add more products.

**Solution:** You've reached the maximum wishlist size (100 items). Remove items you're no longer interested in to make room for new ones. Navigate to your wishlist, sort by "Date Added" (oldest first), and remove items you added months ago and no longer want.

---

**Problem:** Shared wishlist link returns "Wishlist not found" error.

**Solution:**
- If the wishlist owner deleted items, the share link remains valid but shows empty wishlist
- If the wishlist owner changed their share settings from Public to Private without updating the password, recipients will need the new password
- Share links expire after 1 year of the wishlist owner's inactivity; if the owner hasn't logged in for 12+ months, the link is deactivated

---

## Acceptance Criteria / Definition of Done

### Core Functionality
- [ ] WishlistItem entity exists in CandleStore.Domain with properties: Id, CustomerId, ProductId, AddedAt, Note, IsNotifyOnPriceDropEnabled, IsNotifyOnBackInStockEnabled
- [ ] Unique constraint on (CustomerId, ProductId) prevents duplicate wishlist entries
- [ ] IWishlistService interface defined in CandleStore.Application with methods: GetWishlistAsync, AddToWishlistAsync, RemoveFromWishlistAsync, MoveToCartAsync, UpdateNoteAsync
- [ ] WishlistService implements business logic including duplicate checking, product existence validation, and cart integration
- [ ] IWishlistRepository interface with methods: GetByCustomerIdAsync, GetByCustomerAndProductAsync, AddAsync, DeleteAsync, BulkDeleteAsync
- [ ] WishlistRepository implements database access using EF Core with Include() for Product navigation property
- [ ] WishlistController API with endpoints: GET /api/wishlist, POST /api/wishlist, DELETE /api/wishlist/{productId}, POST /api/wishlist/{productId}/move-to-cart
- [ ] All API endpoints require [Authorize] attribute (authenticated users only)
- [ ] API endpoints return ApiResponse<T> wrapper with Success, Data, Message properties
- [ ] GetWishlist endpoint returns WishlistItemDto[] with denormalized product data (name, price, imageUrl, isInStock)

### Wishlist Display and Management
- [ ] Wishlist page (Blazor component) displays all wishlist items in a grid or list layout
- [ ] Each wishlist item shows product image, name, current price, date added
- [ ] Price drop indicator (badge or color) shows when current price < price at time of adding to wishlist
- [ ] Out-of-stock indicator displays for unavailable products
- [ ] "Move to Cart" button adds item to cart and removes from wishlist in single transaction
- [ ] "Remove" button deletes item from wishlist with confirmation dialog
- [ ] Checkbox selection enables bulk operations (select all, select none)
- [ ] "Move Selected to Cart" button processes multiple items at once
- [ ] Wishlist count badge appears in site header showing total item count
- [ ] Wishlist count updates immediately when items are added or removed (Blazor state management)
- [ ] Empty wishlist state displays friendly message with "Continue Shopping" button
- [ ] Wishlist items sortable by: Date Added, Price (low to high), Price (high to low), Product Name
- [ ] Note/comment field on each wishlist item (optional, max 200 characters)
- [ ] "Edit Note" functionality saves without page reload

### Guest Wishlist (Unauthenticated Users)
- [ ] Guest users can add items to wishlist stored in browser localStorage
- [ ] localStorage key: `guest_wishlist` contains JSON array of productIds and timestamps
- [ ] Guest wishlist count badge displays in header (read from localStorage)
- [ ] Guest wishlist persists across page reloads and browser sessions (until cookies/storage cleared)
- [ ] Maximum 50 items for guest wishlist (50 vs 100 for authenticated to encourage account creation)
- [ ] On user login, MergeGuestWishlistAsync() combines localStorage items with database wishlist
- [ ] Merge process avoids duplicates (checks CustomerId + ProductId unique constraint)
- [ ] After successful merge, localStorage is cleared
- [ ] If guest tries to add 51st item, prompt appears: "Create an account to save unlimited items!"

### Wishlist Sharing
- [ ] "Share Wishlist" button generates unique shareable URL: `/wishlist/share/{guid}`
- [ ] Share modal allows user to choose: Public (anyone with link) or Private (requires password)
- [ ] SharedWishlist entity stores: ShareId (Guid), CustomerId, IsPublic, Password (hashed), CreatedAt, ExpiresAt
- [ ] Share link copies to clipboard with "Link Copied!" toast notification
- [ ] Share link can be sent via email using "Send via Email" button (opens default email client with pre-filled message)
- [ ] Public share links accessible without authentication
- [ ] Private share links require password entry before displaying wishlist
- [ ] Shared wishlist page displays read-only view of items (no edit/remove buttons)
- [ ] Shared wishlist shows "Buy this for [Customer Name]" button on each item (adds to viewer's cart, not owner's)
- [ ] Owner can revoke share link at any time (deletes SharedWishlist record)
- [ ] Share links expire after 1 year of owner inactivity

### Notification System
- [ ] "🔔 Notify" toggle on each wishlist item enables price drop monitoring
- [ ] WishlistItem has IsNotifyOnPriceDropEnabled boolean field
- [ ] Background job (Hangfire/Quartz) runs daily to detect price drops >= 10%
- [ ] Price drop email sent when current price < (original price * 0.90)
- [ ] Email includes: Product name, old price, new price, savings percentage, direct "Add to Cart" link
- [ ] Rate limiting: Max 1 price drop email per product per customer per 7 days (prevents spam)
- [ ] Out-of-stock wishlist items automatically enable IsNotifyOnBackInStockEnabled
- [ ] Inventory update event triggers back-in-stock notification email
- [ ] Back-in-stock email includes: Product name, current price, direct "Add to Cart" link
- [ ] Customers can unsubscribe from wishlist notifications via link in email footer
- [ ] Notification preferences in Account Settings: Immediate, Daily Digest, Weekly Digest, Off

### Product Integration
- [ ] "Add to Wishlist" button appears on product cards in catalog grid
- [ ] "Add to Wishlist" button appears on product detail page
- [ ] Wishlist button shows filled heart (♥) if item already wishlisted, empty heart (♡) if not
- [ ] Clicking filled heart removes item from wishlist (toggle functionality)
- [ ] Toast notification confirms add/remove action: "Added to wishlist" or "Removed from wishlist"
- [ ] Wishlist button disabled (grayed out) if user not logged in, with tooltip: "Log in to save favorites"
- [ ] For products with variants, wishlist saves the specific variant selected (e.g., 8oz Lavender)
- [ ] If variant goes out of stock, notification specifies the exact variant

### Performance
- [ ] GET /api/wishlist endpoint returns response in < 300ms with 50 items
- [ ] Wishlist count badge loads in < 100ms (cached in Redis with 5-minute TTL)
- [ ] AddToWishlistAsync completes in < 150ms (single INSERT query)
- [ ] RemoveFromWishlistAsync completes in < 100ms (single DELETE query)
- [ ] MoveToCartAsync completes in < 200ms (INSERT to cart, DELETE from wishlist in transaction)
- [ ] Database query uses index on CustomerId for fast lookup
- [ ] Database query includes Product data via single JOIN (no N+1 queries)
- [ ] Denormalized product data (name, price, imageUrl) stored in WishlistItem to avoid JOIN on list page
- [ ] Background sync job updates denormalized data nightly

### Data Persistence
- [ ] WishlistItems table exists with schema: Id (Guid PK), CustomerId (Guid FK), ProductId (Guid FK), AddedAt (DateTime), Note (string, nullable), IsNotifyOnPriceDropEnabled (bool), IsNotifyOnBackInStockEnabled (bool), PriceAtTimeOfAdd (decimal), ProductName (string), ProductPrice (decimal), ProductImageUrl (string)
- [ ] Foreign key constraint CustomerId → Customers.Id with ON DELETE CASCADE
- [ ] Foreign key constraint ProductId → Products.Id with ON DELETE CASCADE
- [ ] Unique index on (CustomerId, ProductId) prevents duplicates
- [ ] Index on CustomerId for fast wishlist retrieval
- [ ] Index on ProductId for fast product lookup
- [ ] Composite index on (CustomerId, AddedAt DESC) for sorted retrieval
- [ ] Database migration creates WishlistItems table
- [ ] Database migration adds denormalized fields for caching product data
- [ ] Soft-deleted products remain in wishlist but show "No longer available" message

### Edge Cases
- [ ] Adding item already in wishlist returns 400 Bad Request with message: "Product already in your wishlist"
- [ ] Adding 101st item (exceeding limit) returns 400 Bad Request with message: "Wishlist full (max 100 items)"
- [ ] Removing non-existent item returns 404 Not Found
- [ ] Moving out-of-stock item to cart returns 400 Bad Request with message: "Product currently out of stock"
- [ ] Accessing another customer's wishlist returns 403 Forbidden
- [ ] Guest user trying to share wishlist is prompted to create account
- [ ] If product deleted after being wishlisted, wishlist item shows "Product discontinued" with Remove button
- [ ] If customer account deleted, all wishlist items cascade delete (ON DELETE CASCADE)
- [ ] Concurrent add requests (same customer, same product) result in single wishlist entry (unique constraint prevents duplicate)
- [ ] Moving variant product to cart without selected variant shows variant selector modal

### Security
- [ ] All wishlist endpoints require JWT authentication (except shared wishlist view)
- [ ] CustomerId extracted from JWT claims (User.FindFirst(ClaimTypes.NameIdentifier))
- [ ] Users cannot access other customers' wishlists (authorization check)
- [ ] Shared wishlist passwords hashed with BCrypt before storage
- [ ] Rate limiting on Add/Remove endpoints: 60 requests per minute per customer
- [ ] SQL injection prevention via parameterized queries (EF Core)
- [ ] XSS prevention: Note field sanitized on input (HTML encoding)
- [ ] CSRF protection via anti-forgery tokens on Blazor forms

### API Endpoints
- [ ] GET /api/wishlist returns 200 OK with WishlistItemDto[] for authenticated user
- [ ] GET /api/wishlist returns 401 Unauthorized if not authenticated
- [ ] POST /api/wishlist with { productId: Guid } returns 200 OK with created WishlistItemDto
- [ ] POST /api/wishlist with invalid productId returns 404 Not Found
- [ ] POST /api/wishlist for duplicate product returns 400 Bad Request
- [ ] DELETE /api/wishlist/{productId} returns 200 OK with success message
- [ ] DELETE /api/wishlist/{productId} for non-existent item returns 404 Not Found
- [ ] POST /api/wishlist/{productId}/move-to-cart returns 200 OK and removes item from wishlist
- [ ] POST /api/wishlist/bulk-move-to-cart with { productIds: Guid[] } processes multiple items
- [ ] PUT /api/wishlist/{productId}/note with { note: string } updates note field
- [ ] GET /api/wishlist/share/{shareId} returns read-only wishlist for valid share link
- [ ] GET /api/wishlist/share/{shareId} for private link returns 401 if password not provided
- [ ] POST /api/wishlist/share with { isPublic: bool, password: string? } creates share link

### Admin Features
- [ ] Admin panel page: "Analytics → Wishlist Insights"
- [ ] "Most Wishlisted Products" report shows top 50 products by wishlist count
- [ ] "Wishlist Conversion Rate" metric shows percentage of wishlist items eventually purchased
- [ ] "Average Time to Purchase" shows days between wishlist add and eventual purchase
- [ ] "Wishlist Abandonment Rate" shows percentage of wishlist items never purchased after 90 days
- [ ] Ability to view individual customer wishlist (for customer service purposes)
- [ ] Export wishlist data to CSV (CustomerId, ProductId, AddedAt, Purchased, DaysToPurchase)

### Testing Coverage
- [ ] 50+ unit tests covering WishlistService business logic
- [ ] 20+ integration tests covering API endpoints and database operations
- [ ] 10+ E2E tests covering complete user workflows (add, view, move to cart, share)
- [ ] 85%+ code coverage across Application and Infrastructure layers
- [ ] Performance tests verify response time targets
- [ ] Load tests verify system handles 1,000 concurrent wishlist operations

---

## Testing Requirements

### Unit Tests

**Test 1: AddToWishlistAsync - Successfully Adds New Item**
```csharp
using Xunit;
using Moq;
using FluentAssertions;
using CandleStore.Application.Services;
using CandleStore.Application.Interfaces;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;
using AutoMapper;

namespace CandleStore.Tests.Unit.Application.Services
{
    public class WishlistServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IWishlistRepository> _mockWishlistRepo;
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly WishlistService _sut;

        public WishlistServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockWishlistRepo = new Mock<IWishlistRepository>();
            _mockProductRepo = new Mock<IProductRepository>();
            _mockMapper = new Mock<IMapper>();

            _mockUnitOfWork.Setup(u => u.Wishlists).Returns(_mockWishlistRepo.Object);
            _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepo.Object);

            _sut = new WishlistService(
                _mockUnitOfWork.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenProductExists_ReturnsWishlistItemDto()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                Name = "Lavender Dreams",
                Price = 24.99m,
                Images = new List<ProductImage> { new() { Url = "image.jpg", DisplayOrder = 1 } }
            };

            _mockProductRepo.Setup(r => r.GetByIdAsync(productId))
                .ReturnsAsync(product);

            _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
                .ReturnsAsync((WishlistItem?)null); // No existing item

            _mockWishlistRepo.Setup(r => r.AddAsync(It.IsAny<WishlistItem>()))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork.Setup(u => u.CompleteAsync())
                .ReturnsAsync(1);

            var expectedDto = new WishlistItemDto
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                ProductName = "Lavender Dreams",
                ProductPrice = 24.99m,
                ProductImageUrl = "image.jpg",
                AddedAt = DateTime.UtcNow
            };

            _mockMapper.Setup(m => m.Map<WishlistItemDto>(It.IsAny<WishlistItem>()))
                .Returns(expectedDto);

            // Act
            var result = await _sut.AddToWishlistAsync(customerId, productId);

            // Assert
            result.Should().NotBeNull();
            result.ProductId.Should().Be(productId);
            result.ProductName.Should().Be("Lavender Dreams");
            result.ProductPrice.Should().Be(24.99m);

            _mockWishlistRepo.Verify(r => r.AddAsync(It.Is<WishlistItem>(w =>
                w.CustomerId == customerId &&
                w.ProductId == productId &&
                w.ProductName == "Lavender Dreams" &&
                w.ProductPrice == 24.99m
            )), Times.Once);

            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenProductAlreadyInWishlist_ThrowsException()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var existingItem = new WishlistItem
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ProductId = productId,
                AddedAt = DateTime.UtcNow.AddDays(-5)
            };

            _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
                .ReturnsAsync(existingItem);

            // Act
            Func<Task> act = async () => await _sut.AddToWishlistAsync(customerId, productId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Product already in wishlist");

            _mockWishlistRepo.Verify(r => r.AddAsync(It.IsAny<WishlistItem>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenProductDoesNotExist_ThrowsNotFoundException()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            _mockProductRepo.Setup(r => r.GetByIdAsync(productId))
                .ReturnsAsync((Product?)null);

            // Act
            Func<Task> act = async () => await _sut.AddToWishlistAsync(customerId, productId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage($"Product with ID '{productId}' not found");

            _mockWishlistRepo.Verify(r => r.AddAsync(It.IsAny<WishlistItem>()), Times.Never);
        }

        [Fact]
        public async Task AddToWishlistAsync_WhenWishlistFull_ThrowsException()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            var product = new Product { Id = productId, Name = "Test Product", Price = 20m };

            _mockProductRepo.Setup(r => r.GetByIdAsync(productId))
                .ReturnsAsync(product);

            _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
                .ReturnsAsync((WishlistItem?)null);

            _mockWishlistRepo.Setup(r => r.GetCountByCustomerIdAsync(customerId))
                .ReturnsAsync(100); // Already at max

            // Act
            Func<Task> act = async () => await _sut.AddToWishlistAsync(customerId, productId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Wishlist full (maximum 100 items)");
        }
    }
}
```

**Test 2: RemoveFromWishlistAsync - Successfully Removes Item**
```csharp
[Fact]
public async Task RemoveFromWishlistAsync_WhenItemExists_RemovesSuccessfully()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var productId = Guid.NewGuid();

    var existingItem = new WishlistItem
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        ProductId = productId,
        AddedAt = DateTime.UtcNow.AddDays(-10)
    };

    _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
        .ReturnsAsync(existingItem);

    _mockWishlistRepo.Setup(r => r.DeleteAsync(existingItem))
        .Returns(Task.CompletedTask);

    _mockUnitOfWork.Setup(u => u.CompleteAsync())
        .ReturnsAsync(1);

    // Act
    await _sut.RemoveFromWishlistAsync(customerId, productId);

    // Assert
    _mockWishlistRepo.Verify(r => r.DeleteAsync(existingItem), Times.Once);
    _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
}

[Fact]
public async Task RemoveFromWishlistAsync_WhenItemDoesNotExist_ThrowsNotFoundException()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var productId = Guid.NewGuid();

    _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
        .ReturnsAsync((WishlistItem?)null);

    // Act
    Func<Task> act = async () => await _sut.RemoveFromWishlistAsync(customerId, productId);

    // Assert
    await act.Should().ThrowAsync<NotFoundException>()
        .WithMessage("Wishlist item not found");

    _mockWishlistRepo.Verify(r => r.DeleteAsync(It.IsAny<WishlistItem>()), Times.Never);
}
```

**Test 3: MoveToCartAsync - Moves Item from Wishlist to Cart**
```csharp
[Fact]
public async Task MoveToCartAsync_WhenItemExists_MovesToCartAndRemovesFromWishlist()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var productId = Guid.NewGuid();

    var wishlistItem = new WishlistItem
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        ProductId = productId,
        ProductName = "Test Product",
        ProductPrice = 25m,
        AddedAt = DateTime.UtcNow
    };

    var mockCartService = new Mock<ICartService>();

    _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
        .ReturnsAsync(wishlistItem);

    mockCartService.Setup(s => s.AddItemAsync(customerId, productId, 1))
        .ReturnsAsync(new CartItemDto { ProductId = productId, Quantity = 1 });

    _mockWishlistRepo.Setup(r => r.DeleteAsync(wishlistItem))
        .Returns(Task.CompletedTask);

    _mockUnitOfWork.Setup(u => u.CompleteAsync())
        .ReturnsAsync(1);

    var sut = new WishlistService(
        _mockUnitOfWork.Object,
        _mockMapper.Object,
        mockCartService.Object
    );

    // Act
    await sut.MoveToCartAsync(customerId, productId);

    // Assert
    mockCartService.Verify(s => s.AddItemAsync(customerId, productId, 1), Times.Once);
    _mockWishlistRepo.Verify(r => r.DeleteAsync(wishlistItem), Times.Once);
    _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
}
```

**Test 4: GetWishlistAsync - Returns All Customer Wishlist Items**
```csharp
[Fact]
public async Task GetWishlistAsync_WhenCustomerHasItems_ReturnsAllItems()
{
    // Arrange
    var customerId = Guid.NewGuid();

    var wishlistItems = new List<WishlistItem>
    {
        new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ProductId = Guid.NewGuid(),
            ProductName = "Product 1",
            ProductPrice = 20m,
            ProductImageUrl = "img1.jpg",
            AddedAt = DateTime.UtcNow.AddDays(-5)
        },
        new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ProductId = Guid.NewGuid(),
            ProductName = "Product 2",
            ProductPrice = 30m,
            ProductImageUrl = "img2.jpg",
            AddedAt = DateTime.UtcNow.AddDays(-2)
        }
    };

    _mockWishlistRepo.Setup(r => r.GetByCustomerIdAsync(customerId))
        .ReturnsAsync(wishlistItems);

    var dtos = wishlistItems.Select(w => new WishlistItemDto
    {
        Id = w.Id,
        ProductId = w.ProductId,
        ProductName = w.ProductName,
        ProductPrice = w.ProductPrice,
        ProductImageUrl = w.ProductImageUrl,
        AddedAt = w.AddedAt
    }).ToList();

    _mockMapper.Setup(m => m.Map<List<WishlistItemDto>>(wishlistItems))
        .Returns(dtos);

    // Act
    var result = await _sut.GetWishlistAsync(customerId);

    // Assert
    result.Should().HaveCount(2);
    result[0].ProductName.Should().Be("Product 1");
    result[1].ProductName.Should().Be("Product 2");
    result.Should().BeInDescendingOrder(w => w.AddedAt); // Most recent first
}

[Fact]
public async Task GetWishlistAsync_WhenCustomerHasNoItems_ReturnsEmptyList()
{
    // Arrange
    var customerId = Guid.NewGuid();

    _mockWishlistRepo.Setup(r => r.GetByCustomerIdAsync(customerId))
        .ReturnsAsync(new List<WishlistItem>());

    _mockMapper.Setup(m => m.Map<List<WishlistItemDto>>(It.IsAny<List<WishlistItem>>()))
        .Returns(new List<WishlistItemDto>());

    // Act
    var result = await _sut.GetWishlistAsync(customerId);

    // Assert
    result.Should().BeEmpty();
}
```

**Test 5: MergeGuestWishlistAsync - Combines Guest and Authenticated Wishlists**
```csharp
[Fact]
public async Task MergeGuestWishlistAsync_WhenGuestHasItems_MergesWithAuthenticatedWishlist()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var guestProductIds = new List<Guid>
    {
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid()
    };

    var product1 = new Product { Id = guestProductIds[0], Name = "Guest Product 1", Price = 15m };
    var product2 = new Product { Id = guestProductIds[1], Name = "Guest Product 2", Price = 25m };
    var product3 = new Product { Id = guestProductIds[2], Name = "Guest Product 3", Price = 35m };

    _mockProductRepo.Setup(r => r.GetByIdAsync(guestProductIds[0])).ReturnsAsync(product1);
    _mockProductRepo.Setup(r => r.GetByIdAsync(guestProductIds[1])).ReturnsAsync(product2);
    _mockProductRepo.Setup(r => r.GetByIdAsync(guestProductIds[2])).ReturnsAsync(product3);

    // Customer already has product2 in wishlist
    _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, guestProductIds[1]))
        .ReturnsAsync(new WishlistItem { CustomerId = customerId, ProductId = guestProductIds[1] });

    // Other products not in wishlist
    _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, guestProductIds[0]))
        .ReturnsAsync((WishlistItem?)null);
    _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, guestProductIds[2]))
        .ReturnsAsync((WishlistItem?)null);

    _mockWishlistRepo.Setup(r => r.AddAsync(It.IsAny<WishlistItem>()))
        .Returns(Task.CompletedTask);

    _mockUnitOfWork.Setup(u => u.CompleteAsync())
        .ReturnsAsync(2); // 2 items added (3 total - 1 duplicate)

    // Act
    await _sut.MergeGuestWishlistAsync(customerId, guestProductIds);

    // Assert
    _mockWishlistRepo.Verify(r => r.AddAsync(It.Is<WishlistItem>(w =>
        w.CustomerId == customerId && w.ProductId == guestProductIds[0]
    )), Times.Once);

    _mockWishlistRepo.Verify(r => r.AddAsync(It.Is<WishlistItem>(w =>
        w.CustomerId == customerId && w.ProductId == guestProductIds[2]
    )), Times.Once);

    // Should NOT add product2 (already exists)
    _mockWishlistRepo.Verify(r => r.AddAsync(It.Is<WishlistItem>(w =>
        w.ProductId == guestProductIds[1]
    )), Times.Never);

    _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
}
```

**Test 6: UpdateNoteAsync - Updates Wishlist Item Note**
```csharp
[Fact]
public async Task UpdateNoteAsync_WhenItemExists_UpdatesNoteSuccessfully()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var productId = Guid.NewGuid();
    var newNote = "Gift for Mom - Birthday 2024";

    var wishlistItem = new WishlistItem
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        ProductId = productId,
        Note = "Old note",
        AddedAt = DateTime.UtcNow
    };

    _mockWishlistRepo.Setup(r => r.GetByCustomerAndProductAsync(customerId, productId))
        .ReturnsAsync(wishlistItem);

    _mockUnitOfWork.Setup(u => u.CompleteAsync())
        .ReturnsAsync(1);

    // Act
    await _sut.UpdateNoteAsync(customerId, productId, newNote);

    // Assert
    wishlistItem.Note.Should().Be(newNote);
    _mockUnitOfWork.Verify(u => u.CompleteAsync(), Times.Once);
}

[Fact]
public async Task UpdateNoteAsync_WhenNoteTooLong_ThrowsValidationException()
{
    // Arrange
    var customerId = Guid.NewGuid();
    var productId = Guid.NewGuid();
    var tooLongNote = new string('a', 201); // Max 200 characters

    // Act
    Func<Task> act = async () => await _sut.UpdateNoteAsync(customerId, productId, tooLongNote);

    // Assert
    await act.Should().ThrowAsync<ValidationException>()
        .WithMessage("Note cannot exceed 200 characters");
}
```

### Integration Tests

**Test 1: Wishlist API - Add and Retrieve Items**
```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using CandleStore.Application.DTOs;

namespace CandleStore.Tests.Integration.Api
{
    public class WishlistControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public WishlistControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AddToWishlist_AndRetrieve_ReturnsAddedItem()
        {
            // Arrange
            var token = await GetAuthTokenAsync(); // Helper method to authenticate
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var productId = await CreateTestProductAsync(); // Helper to seed test product

            // Act - Add to wishlist
            var addResponse = await _client.PostAsJsonAsync("/api/wishlist", new { productId });

            // Assert - Add succeeded
            addResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var addResult = await addResponse.Content.ReadFromJsonAsync<ApiResponse<WishlistItemDto>>();
            addResult.Success.Should().BeTrue();
            addResult.Data.ProductId.Should().Be(productId);

            // Act - Retrieve wishlist
            var getResponse = await _client.GetAsync("/api/wishlist");

            // Assert - Retrieved item matches
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<WishlistItemDto>>>();
            getResult.Success.Should().BeTrue();
            getResult.Data.Should().ContainSingle();
            getResult.Data[0].ProductId.Should().Be(productId);
        }

        [Fact]
        public async Task AddDuplicateToWishlist_Returns400BadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var productId = await CreateTestProductAsync();

            // Act - Add first time
            await _client.PostAsJsonAsync("/api/wishlist", new { productId });

            // Act - Add second time (duplicate)
            var duplicateResponse = await _client.PostAsJsonAsync("/api/wishlist", new { productId });

            // Assert
            duplicateResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var result = await duplicateResponse.Content.ReadFromJsonAsync<ApiResponse<WishlistItemDto>>();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("already in wishlist");
        }

        [Fact]
        public async Task RemoveFromWishlist_DeletesItem()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var productId = await CreateTestProductAsync();
            await _client.PostAsJsonAsync("/api/wishlist", new { productId }); // Add item

            // Act - Remove
            var deleteResponse = await _client.DeleteAsync($"/api/wishlist/{productId}");

            // Assert
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify item gone
            var getResponse = await _client.GetAsync("/api/wishlist");
            var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<List<WishlistItemDto>>>();
            getResult.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task MoveToCart_AddsToCartAndRemovesFromWishlist()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var productId = await CreateTestProductAsync();
            await _client.PostAsJsonAsync("/api/wishlist", new { productId });

            // Act - Move to cart
            var moveResponse = await _client.PostAsync($"/api/wishlist/{productId}/move-to-cart", null);

            // Assert
            moveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify removed from wishlist
            var wishlistResponse = await _client.GetAsync("/api/wishlist");
            var wishlistResult = await wishlistResponse.Content.ReadFromJsonAsync<ApiResponse<List<WishlistItemDto>>>();
            wishlistResult.Data.Should().BeEmpty();

            // Verify added to cart
            var cartResponse = await _client.GetAsync("/api/cart");
            var cartResult = await cartResponse.Content.ReadFromJsonAsync<ApiResponse<List<CartItemDto>>>();
            cartResult.Data.Should().ContainSingle();
            cartResult.Data[0].ProductId.Should().Be(productId);
        }
    }
}
```

**Test 2: Guest Wishlist Merge on Login**
```csharp
[Fact]
public async Task GuestWishlistMerge_OnLogin_CombinesWithAuthenticatedWishlist()
{
    // Arrange
    var guestProductId1 = await CreateTestProductAsync("Guest Product 1");
    var guestProductId2 = await CreateTestProductAsync("Guest Product 2");
    var existingProductId = await CreateTestProductAsync("Existing Product");

    // Simulate guest adding items (stored in localStorage - we'll send productIds during login)
    var guestProductIds = new List<Guid> { guestProductId1, guestProductId2 };

    // Create authenticated user with existing wishlist item
    var (userId, token) = await CreateAuthenticatedUserAsync();
    _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    await _client.PostAsJsonAsync("/api/wishlist", new { productId = existingProductId });

    // Act - Merge guest wishlist during login
    var mergeResponse = await _client.PostAsJsonAsync("/api/wishlist/merge", guestProductIds);

    // Assert
    mergeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    // Verify all 3 items in wishlist
    var wishlistResponse = await _client.GetAsync("/api/wishlist");
    var wishlistResult = await wishlistResponse.Content.ReadFromJsonAsync<ApiResponse<List<WishlistItemDto>>>();
    wishlistResult.Data.Should().HaveCount(3);
    wishlistResult.Data.Should().Contain(w => w.ProductId == guestProductId1);
    wishlistResult.Data.Should().Contain(w => w.ProductId == guestProductId2);
    wishlistResult.Data.Should().Contain(w => w.ProductId == existingProductId);
}
```

**Test 3: Wishlist Sharing - Public Link Access**
```csharp
[Fact]
public async Task ShareWishlist_PublicLink_AllowsUnauthenticatedAccess()
{
    // Arrange
    var token = await GetAuthTokenAsync();
    _client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    var productId1 = await CreateTestProductAsync("Shared Product 1");
    var productId2 = await CreateTestProductAsync("Shared Product 2");

    await _client.PostAsJsonAsync("/api/wishlist", new { productId = productId1 });
    await _client.PostAsJsonAsync("/api/wishlist", new { productId = productId2 });

    // Act - Create share link
    var shareResponse = await _client.PostAsJsonAsync("/api/wishlist/share", new { isPublic = true });
    shareResponse.StatusCode.Should().Be(HttpStatusCode.OK);

    var shareResult = await shareResponse.Content.ReadFromJsonAsync<ApiResponse<ShareLinkDto>>();
    var shareId = shareResult.Data.ShareId;

    // Act - Access share link WITHOUT authentication
    var unauthenticatedClient = _factory.CreateClient(); // No auth header
    var sharedWishlistResponse = await unauthenticatedClient.GetAsync($"/api/wishlist/share/{shareId}");

    // Assert
    sharedWishlistResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    var sharedWishlist = await sharedWishlistResponse.Content.ReadFromJsonAsync<ApiResponse<List<WishlistItemDto>>>();
    sharedWishlist.Data.Should().HaveCount(2);
}
```

### End-to-End (E2E) Tests

**Scenario 1: Complete Wishlist Workflow - Add, View, Move to Cart, Purchase**
1. User browses catalog page at `/products`
2. User clicks heart icon (♡) on "Lavender Dreams" product card
3. **Expected:** Heart icon fills (♥) and changes color to red
4. **Expected:** Wishlist count badge in header shows "1"
5. User clicks heart icon on "Midnight Jasmine" product
6. **Expected:** Wishlist count badge updates to "2"
7. User clicks "My Wishlist" link in header navigation
8. **Expected:** Wishlist page loads showing 2 products with images, names, prices
9. User checks checkbox next to "Lavender Dreams"
10. User clicks "Move Selected to Cart (1)" button
11. **Expected:** Toast notification: "Moved 1 item to cart"
12. **Expected:** Wishlist page now shows only "Midnight Jasmine"
13. **Expected:** Cart count badge in header shows "1"
14. User navigates to cart page
15. **Expected:** Cart contains "Lavender Dreams" with quantity = 1
16. User proceeds to checkout and completes purchase
17. **Expected:** Order confirmation page displays
18. User returns to wishlist page
19. **Expected:** "Midnight Jasmine" still in wishlist (not auto-removed because not purchased)

**Scenario 2: Guest Wishlist Merge on Account Creation**
1. Guest user (not logged in) browses catalog
2. Guest clicks "Add to Wishlist" on "Vanilla Bean" product
3. **Expected:** Browser prompt: "Create account to save favorites!" with [Sign Up] button
4. Guest clicks browser back, continues browsing
5. Guest adds "Vanilla Bean" to localStorage (bypassing prompt via direct icon click)
6. **Expected:** Wishlist count badge shows "1" (read from localStorage)
7. Guest repeats for "Cinnamon Spice" product
8. **Expected:** Wishlist count badge shows "2"
9. Guest clicks "Sign Up" in header
10. Guest creates account with email/password
11. **Expected:** Account creation success, user auto-logged in
12. **Expected:** Wishlist count badge still shows "2"
13. User navigates to "My Wishlist"
14. **Expected:** Wishlist page shows both products ("Vanilla Bean", "Cinnamon Spice")
15. **Expected:** Database `WishlistItems` table contains 2 rows for this CustomerId
16. User logs out, then logs back in on different device
17. User navigates to "My Wishlist"
18. **Expected:** Both products still present (persisted server-side)

**Scenario 3: Price Drop Notification Workflow**
1. Authenticated user adds "Ocean Breeze" ($34.99) to wishlist
2. User enables price drop notification toggle (🔔) on wishlist item
3. Admin changes product price from $34.99 to $24.99 (29% discount)
4. Background job (PriceDropNotificationJob) runs nightly cron
5. **Expected:** User receives email with subject "Price Drop Alert: Ocean Breeze"
6. Email contains: Old price ($34.99), new price ($24.99), savings (29% / $10.00)
7. User clicks "Add to Cart" button in email (direct link with auth token)
8. **Expected:** Browser opens cart page with "Ocean Breeze" added
9. User completes purchase
10. **Expected:** "Ocean Breeze" removed from wishlist (auto-removal after purchase enabled)

### Performance Tests

**Benchmark 1: GET /api/wishlist Response Time**
- **Target:** < 300ms for wishlist with 50 items
- **Measurement:** Average response time over 100 requests
- **Pass Criteria:** 95th percentile < 300ms, 99th percentile < 500ms
- **Rationale:** Wishlist page should load quickly for engaged customers reviewing saved items
- **Test Setup:** Seed database with customer account containing 50 wishlist items, each with denormalized product data
- **Optimization:** Use database index on CustomerId, denormalize product name/price/imageUrl to avoid JOIN

**Benchmark 2: POST /api/wishlist Add Item**
- **Target:** < 150ms for adding single item
- **Measurement:** Average response time over 500 add requests
- **Pass Criteria:** 95th percentile < 150ms, no requests > 300ms
- **Rationale:** Add-to-wishlist must feel instant for positive user experience
- **Test Setup:** Authenticated user, valid product IDs, no duplicates
- **Optimization:** Single INSERT query, no complex validation beyond existence check

**Benchmark 3: Wishlist Count Badge Load Time**
- **Target:** < 100ms for retrieving count
- **Measurement:** Dedicated `/api/wishlist/count` endpoint response time
- **Pass Criteria:** 99th percentile < 100ms
- **Rationale:** Count badge appears on every page; slow load delays entire page render
- **Test Setup:** Redis cache with 5-minute TTL for wishlist counts
- **Optimization:** Cache count in Redis, invalidate on add/remove, fallback to database query if cache miss

**Benchmark 4: Bulk Move to Cart**
- **Target:** < 500ms for moving 10 items to cart
- **Measurement:** POST /api/wishlist/bulk-move-to-cart with 10 productIds
- **Pass Criteria:** Average < 500ms, max < 1000ms
- **Rationale:** Bulk operations should be significantly faster than individual requests
- **Test Setup:** Transaction-wrapped bulk INSERT to cart, bulk DELETE from wishlist
- **Optimization:** Use EF Core `AddRangeAsync` and `RemoveRange` for batch operations

### Regression Tests

**Existing Functionality That Must Not Break:**

1. **Shopping Cart (Task 014):**
   - Adding items to cart must continue to work with same API contract
   - Cart count badge must update independently of wishlist count badge
   - Cart page must render without errors when wishlist feature present

2. **Customer Authentication (Task 021):**
   - JWT token extraction from claims must work for wishlist endpoints
   - Logout must not corrupt localStorage (guest wishlist should clear on logout)
   - Account deletion must cascade delete wishlist items (test ON DELETE CASCADE)

3. **Product Catalog (Task 003):**
   - Product detail page rendering must not slow down due to wishlist button
   - Product card "Add to Cart" button must remain distinct from "Add to Wishlist"
   - Product deletion must not cause errors on wishlist page (items should show "No longer available")

4. **Email System (Task 025):**
   - Existing transactional emails (order confirmation, shipping) must still send
   - Email queue processing must not be blocked by wishlist notification emails
   - Unsubscribe functionality must independently handle wishlist notification opt-outs

**Regression Test Suite:**
- Run full integration test suite for Tasks 003, 014, 021, 025 after implementing Task 030
- Verify no performance degradation on product catalog page load (< 2 seconds unchanged)
- Verify database migration rollback works without errors

---

## User Verification Steps

### Verification 1: Add Product to Wishlist (Authenticated User)
1. Log into the storefront with customer account
2. Navigate to the product catalog page (`/products`)
3. Hover over any product card
4. **Verify:** Heart icon (♡) appears in top-right corner of product card
5. Click the heart icon
6. **Verify:** Heart icon fills (♥) and changes color to red or pink
7. **Verify:** Toast notification appears: "Added to wishlist!"
8. **Verify:** Wishlist count badge in site header increments (e.g., shows "1")
9. Click the heart icon again (toggle off)
10. **Verify:** Heart icon returns to outline style (♡)
11. **Verify:** Toast notification: "Removed from wishlist"
12. **Verify:** Wishlist count badge decrements

### Verification 2: View Wishlist Page
1. Add 3-5 products to wishlist using steps from Verification 1
2. Click "My Wishlist" link in header navigation (or user account dropdown)
3. **Verify:** Wishlist page loads with URL `/account/wishlist` or `/wishlist`
4. **Verify:** Page title shows "My Wishlist (X items)" with correct count
5. **Verify:** Each wishlist item displays: product image, name, current price, date added
6. **Verify:** Each item has buttons: "Move to Cart", "Remove", "Add Note"
7. **Verify:** Checkbox appears next to each item for selection
8. **Verify:** "Select All" checkbox appears at bottom of list
9. **Verify:** Items sorted by date added (most recent first) by default
10. **Verify:** Sort dropdown offers options: "Date Added", "Price: Low to High", "Price: High to Low", "Product Name"

### Verification 3: Move Item from Wishlist to Cart
1. On wishlist page with at least 1 item
2. Click "Move to Cart" button on any wishlist item
3. **Verify:** Toast notification: "Moved [Product Name] to cart"
4. **Verify:** Item disappears from wishlist page
5. **Verify:** Wishlist count badge decrements by 1
6. **Verify:** Cart count badge in header increments by 1
7. Navigate to cart page (`/cart`)
8. **Verify:** Product appears in cart with quantity = 1
9. **Verify:** Product price matches price shown on wishlist

### Verification 4: Bulk Move to Cart
1. On wishlist page with at least 3 items
2. Check boxes next to 2 items
3. **Verify:** "Move Selected to Cart (2)" button appears and is enabled
4. Click "Move Selected to Cart (2)" button
5. **Verify:** Toast notification: "Moved 2 items to cart. Proceed to checkout →" (clickable link)
6. **Verify:** Both items disappear from wishlist
7. **Verify:** Wishlist count badge decrements by 2
8. **Verify:** Cart count badge increments by 2
9. Navigate to cart page
10. **Verify:** Both products appear in cart

### Verification 5: Guest Wishlist and Merge on Login
1. Open site in incognito/private browser window (not logged in)
2. Browse catalog and click heart icon on "Lavender Dreams" product
3. **Verify:** Browser localStorage contains entry (open DevTools → Application → Local Storage → `guest_wishlist`)
4. **Verify:** Wishlist count badge shows "1" (read from localStorage)
5. Add another product ("Midnight Jasmine") to wishlist
6. **Verify:** Wishlist count badge shows "2"
7. Navigate to "My Wishlist" page
8. **Verify:** Guest wishlist page shows both products (read from localStorage, displayed client-side)
9. Click "Sign Up" or "Log In" in header
10. Create new account or log into existing account
11. **Verify:** After login redirect, wishlist count badge still shows "2" (or more if account had existing wishlist items)
12. Navigate to "My Wishlist" page
13. **Verify:** All guest wishlist items now appear (merged into database)
14. Log out, then log back in
15. **Verify:** Wishlist items persist (stored server-side)

### Verification 6: Wishlist Sharing (Public Link)
1. Log in with account that has 3+ wishlist items
2. Navigate to "My Wishlist" page
3. Click "Share" button or dropdown in top-right corner
4. Select "Public Link" option
5. **Verify:** Modal appears with shareable URL (e.g., `https://candlestore.com/wishlist/share/a1b2c3d4`)
6. Click "Copy Link" button
7. **Verify:** Toast notification: "Link copied to clipboard!"
8. Open a new incognito/private browser window (not logged in)
9. Paste and navigate to the copied URL
10. **Verify:** Shared wishlist page loads WITHOUT requiring login
11. **Verify:** Page shows read-only wishlist with all items
12. **Verify:** Each item has "Buy This for [Owner Name]" button (adds to viewer's cart, not owner's)
13. Click "Buy This for [Owner Name]" on any item
14. **Verify:** Item added to viewer's cart
15. **Verify:** Item remains in owner's wishlist (not removed)

### Verification 7: Price Drop Notification
1. Log in and add product priced at $34.99 to wishlist
2. On wishlist page, enable "🔔 Notify on Price Drop" toggle for that item
3. **Verify:** Toggle switches to enabled state (filled bell icon or checkmark)
4. Log into admin panel as store owner
5. Navigate to Products → Edit "Ocean Breeze" product
6. Change price from $34.99 to $24.99 (29% discount)
7. Save product changes
8. Manually trigger background job: `dotnet run --project CandleStore.Infrastructure -- run-job PriceDropNotificationJob` (or wait for nightly cron)
9. Check email inbox for customer account
10. **Verify:** Email received with subject "Price Drop Alert: Ocean Breeze"
11. **Verify:** Email body shows: Old price ($34.99), new price ($24.99), savings amount ($10 / 29%)
12. **Verify:** Email contains "Add to Cart" button with direct link
13. Click "Add to Cart" link in email
14. **Verify:** Browser opens cart page with "Ocean Breeze" already added

### Verification 8: Back-in-Stock Notification
1. Log in and navigate to product detail page for out-of-stock item
2. **Verify:** "Add to Cart" button is disabled and shows "Out of Stock"
3. Click "Add to Wishlist" button
4. **Verify:** Item added to wishlist
5. Navigate to wishlist page
6. **Verify:** Item shows "Out of Stock" badge
7. **Verify:** "Notify When Back in Stock" is automatically enabled (no toggle needed)
8. Admin updates product inventory from 0 to 50 units
9. Inventory update triggers `BackInStockNotificationJob`
10. Check customer email inbox
11. **Verify:** Email received with subject "Back in Stock: [Product Name]"
12. **Verify:** Email contains current price and "Add to Cart" button
13. Click "Add to Cart" link
14. **Verify:** Product added to cart successfully (inventory now available)

### Verification 9: Wishlist Note Functionality
1. Navigate to wishlist page with at least 1 item
2. Click "Add Note" button (or note icon) on any wishlist item
3. **Verify:** Text input field appears below the item
4. Type note: "Gift for Mom - Birthday June 15"
5. Press Enter or click "Save" icon
6. **Verify:** Note saves and displays below product name
7. **Verify:** Character counter shows "28 / 200" (or similar)
8. Refresh the page
9. **Verify:** Note persists after reload
10. Click "Edit Note" icon
11. Change note to: "Gift for Mom - Anniversary instead"
12. Save changes
13. **Verify:** Updated note displays correctly

### Verification 10: Wishlist Item Limit
1. Log in with account that has fewer than 100 wishlist items
2. Navigate to wishlist page
3. **Verify:** Current item count displayed (e.g., "My Wishlist (47 items)")
4. Add items until wishlist count reaches 100
5. Attempt to add 101st item by clicking heart icon on product
6. **Verify:** Error toast notification: "Wishlist full (maximum 100 items)"
7. **Verify:** Heart icon does NOT fill (item not added)
8. **Verify:** Wishlist count remains at 100
9. Remove 1 item from wishlist
10. **Verify:** Count decrements to 99
11. Attempt to add new item
12. **Verify:** Item adds successfully (limit no longer exceeded)

---

## Implementation Prompt for Claude

### Implementation Overview

This task implements a complete wishlist/favorites system for the Candle Store e-commerce platform. Customers can save products for later purchase, receive notifications for price drops and back-in-stock events, share wishlists with others, and seamlessly move items between wishlist and cart. The implementation includes both authenticated wishlist storage (database-backed) and guest wishlist support (localStorage-backed with automatic merge on login).

**What You'll Build:**
- WishlistItem domain entity with CustomerId, ProductId, and notification preferences
- WishlistService business logic layer with add/remove/move-to-cart operations
- WishlistRepository for database access with EF Core
- Wishlist API endpoints (GET, POST, DELETE) with JWT authentication
- Blazor wishlist page component with sorting, filtering, and bulk operations
- Guest wishlist functionality using browser localStorage
- Wishlist merge logic that combines guest and authenticated wishlists on login
- Background jobs for price drop and back-in-stock notifications
- Wishlist sharing with public/private link generation

### Prerequisites

**Required:**
- .NET 8 SDK installed
- Completed Task 002 (Domain Models and EF Core Setup)
- Completed Task 014 (Shopping Cart API) - for move-to-cart integration
- Completed Task 021 (Customer Authentication & JWT) - for protected endpoints
- Completed Task 025 (Email Marketing System) - for notification emails
- PostgreSQL or SQL Server database running
- Redis installed (for wishlist count caching)

**NuGet Packages Needed:**
- `Hangfire.Core` (v1.8+) - background job scheduling for notifications
- `Hangfire.PostgreSql` or `Hangfire.SqlServer` - persistent job storage
- `StackExchange.Redis` (v2.7+) - wishlist count caching
- All packages from Task 002, 014, 021, 025 (EF Core, AutoMapper, FluentValidation, etc.)

### Step-by-Step Implementation

#### Step 1: Create WishlistItem Domain Entity

**File:** `src/CandleStore.Domain/Entities/WishlistItem.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CandleStore.Domain.Entities
{
    public class WishlistItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        public string? Note { get; set; }

        public bool IsNotifyOnPriceDropEnabled { get; set; } = false;

        public bool IsNotifyOnBackInStockEnabled { get; set; } = false;

        // Denormalized product data for performance (avoid JOIN on list page)
        [Required]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public decimal ProductPrice { get; set; }

        [Required]
        [MaxLength(500)]
        public string ProductImageUrl { get; set; } = string.Empty;

        // Price at time of adding to wishlist (for price drop detection)
        [Required]
        public decimal PriceAtTimeOfAdd { get; set; }

        // Navigation Properties
        public virtual Customer Customer { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;

        // Methods
        public bool HasPriceDropped(decimal currentPrice)
        {
            return currentPrice < (PriceAtTimeOfAdd * 0.90m); // 10% or more discount
        }

        public decimal GetPriceDropAmount(decimal currentPrice)
        {
            return PriceAtTimeOfAdd - currentPrice;
        }

        public decimal GetPriceDropPercentage(decimal currentPrice)
        {
            if (PriceAtTimeOfAdd == 0) return 0;
            return ((PriceAtTimeOfAdd - currentPrice) / PriceAtTimeOfAdd) * 100;
        }
    }
}
```

**Explanation:** This entity represents a single item in a customer's wishlist. It includes denormalized product data (name, price, imageUrl) to avoid expensive JOINs when loading wishlist page. The `PriceAtTimeOfAdd` field enables price drop detection by comparing against current product price.

#### Step 2: Create SharedWishlist Entity for Sharing

**File:** `src/CandleStore.Domain/Entities/SharedWishlist.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CandleStore.Domain.Entities
{
    public class SharedWishlist
    {
        [Key]
        public Guid ShareId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid CustomerId { get; set; }

        public bool IsPublic { get; set; } = true;

        [MaxLength(500)]
        public string? PasswordHash { get; set; } // For private wishlists

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddYears(1);

        // Navigation
        public virtual Customer Customer { get; set; } = null!;
    }
}
```

#### Step 3: Configure EF Core Relationships

**File:** `src/CandleStore.Infrastructure/Persistence/Configurations/WishlistItemConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CandleStore.Domain.Entities;

namespace CandleStore.Infrastructure.Persistence.Configurations
{
    public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
    {
        public void Configure(EntityTypeBuilder<WishlistItem> builder)
        {
            builder.ToTable("WishlistItems");

            builder.HasKey(w => w.Id);

            builder.Property(w => w.Id)
                .ValueGeneratedNever(); // Guid assigned in service layer

            builder.Property(w => w.ProductName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(w => w.ProductImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(w => w.Note)
                .HasMaxLength(200);

            builder.Property(w => w.ProductPrice)
                .HasPrecision(18, 2);

            builder.Property(w => w.PriceAtTimeOfAdd)
                .HasPrecision(18, 2);

            // Relationships
            builder.HasOne(w => w.Customer)
                .WithMany()
                .HasForeignKey(w => w.CustomerId)
                .OnDelete(DeleteBehavior.Cascade); // Delete wishlist when customer deleted

            builder.HasOne(w => w.Product)
                .WithMany()
                .HasForeignKey(w => w.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // Delete wishlist item when product deleted

            // Unique Constraint: One customer cannot add same product twice
            builder.HasIndex(w => new { w.CustomerId, w.ProductId })
                .IsUnique()
                .HasDatabaseName("IX_WishlistItems_CustomerId_ProductId");

            // Index for fast lookup by customer
            builder.HasIndex(w => w.CustomerId)
                .HasDatabaseName("IX_WishlistItems_CustomerId");

            // Composite index for sorted retrieval
            builder.HasIndex(w => new { w.CustomerId, w.AddedAt })
                .HasDatabaseName("IX_WishlistItems_CustomerId_AddedAt");

            // Index for product lookup (for back-in-stock notifications)
            builder.HasIndex(w => w.ProductId)
                .HasDatabaseName("IX_WishlistItems_ProductId");
        }
    }
}
```

**File:** `src/CandleStore.Infrastructure/Persistence/Configurations/SharedWishlistConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CandleStore.Domain.Entities;

namespace CandleStore.Infrastructure.Persistence.Configurations
{
    public class SharedWishlistConfiguration : IEntityTypeConfiguration<SharedWishlist>
    {
        public void Configure(EntityTypeBuilder<SharedWishlist> builder)
        {
            builder.ToTable("SharedWishlists");

            builder.HasKey(s => s.ShareId);

            builder.Property(s => s.PasswordHash)
                .HasMaxLength(500);

            builder.HasOne(s => s.Customer)
                .WithMany()
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => s.ShareId)
                .IsUnique();
        }
    }
}
```

#### Step 4: Update DbContext

**File:** `src/CandleStore.Infrastructure/Persistence/AppDbContext.cs`

```csharp
// Add to existing DbContext class
public DbSet<WishlistItem> WishlistItems { get; set; } = null!;
public DbSet<SharedWishlist> SharedWishlists { get; set; } = null!;

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations ...

    modelBuilder.ApplyConfiguration(new WishlistItemConfiguration());
    modelBuilder.ApplyConfiguration(new SharedWishlistConfiguration());
}
```

#### Step 5: Create Database Migration

```bash
dotnet ef migrations add AddWishlistTables \
  --project src/CandleStore.Infrastructure \
  --startup-project src/CandleStore.Api \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/CandleStore.Infrastructure \
  --startup-project src/CandleStore.Api
```

#### Step 6: Create DTOs

**File:** `src/CandleStore.Application/DTOs/Wishlist/WishlistItemDto.cs`

```csharp
namespace CandleStore.Application.DTOs.Wishlist
{
    public class WishlistItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public string ProductImageUrl { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
        public string? Note { get; set; }
        public bool IsNotifyOnPriceDropEnabled { get; set; }
        public bool IsNotifyOnBackInStockEnabled { get; set; }
        public bool IsInStock { get; set; } // Computed from Product
        public decimal? PriceAtTimeOfAdd { get; set; }
        public bool HasPriceDropped { get; set; } // Computed
        public decimal? PriceDropAmount { get; set; } // Computed
        public decimal? PriceDropPercentage { get; set; } // Computed
    }
}
```

**File:** `src/CandleStore.Application/DTOs/Wishlist/AddToWishlistDto.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CandleStore.Application.DTOs.Wishlist
{
    public class AddToWishlistDto
    {
        [Required(ErrorMessage = "Product ID is required")]
        public Guid ProductId { get; set; }
    }
}
```

**File:** `src/CandleStore.Application/DTOs/Wishlist/UpdateWishlistNoteDto.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace CandleStore.Application.DTOs.Wishlist
{
    public class UpdateWishlistNoteDto
    {
        [MaxLength(200, ErrorMessage = "Note cannot exceed 200 characters")]
        public string? Note { get; set; }
    }
}
```

#### Step 7: Create Repository Interface

**File:** `src/CandleStore.Application/Interfaces/Repositories/IWishlistRepository.cs`

```csharp
using CandleStore.Domain.Entities;

namespace CandleStore.Application.Interfaces.Repositories
{
    public interface IWishlistRepository : IRepository<WishlistItem>
    {
        Task<List<WishlistItem>> GetByCustomerIdAsync(Guid customerId);
        Task<WishlistItem?> GetByCustomerAndProductAsync(Guid customerId, Guid productId);
        Task<int> GetCountByCustomerIdAsync(Guid customerId);
        Task<List<WishlistItem>> GetByProductIdAsync(Guid productId); // For back-in-stock notifications
        Task BulkDeleteAsync(List<WishlistItem> items);
    }
}
```

#### Step 8: Implement Repository

**File:** `src/CandleStore.Infrastructure/Persistence/Repositories/WishlistRepository.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using CandleStore.Domain.Entities;
using CandleStore.Application.Interfaces.Repositories;

namespace CandleStore.Infrastructure.Persistence.Repositories
{
    public class WishlistRepository : Repository<WishlistItem>, IWishlistRepository
    {
        public WishlistRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<WishlistItem>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _context.WishlistItems
                .Where(w => w.CustomerId == customerId)
                .Include(w => w.Product) // Include for stock status
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }

        public async Task<WishlistItem?> GetByCustomerAndProductAsync(Guid customerId, Guid productId)
        {
            return await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.ProductId == productId);
        }

        public async Task<int> GetCountByCustomerIdAsync(Guid customerId)
        {
            return await _context.WishlistItems
                .Where(w => w.CustomerId == customerId)
                .CountAsync();
        }

        public async Task<List<WishlistItem>> GetByProductIdAsync(Guid productId)
        {
            return await _context.WishlistItems
                .Where(w => w.ProductId == productId && w.IsNotifyOnBackInStockEnabled)
                .Include(w => w.Customer)
                .ToListAsync();
        }

        public async Task BulkDeleteAsync(List<WishlistItem> items)
        {
            _context.WishlistItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}
```

#### Step 9: Update Unit of Work

**File:** `src/CandleStore.Application/Interfaces/IUnitOfWork.cs`

```csharp
// Add to existing interface
IWishlistRepository Wishlists { get; }
```

**File:** `src/CandleStore.Infrastructure/Persistence/UnitOfWork.cs`

```csharp
// Add to existing class
private IWishlistRepository? _wishlists;
public IWishlistRepository Wishlists => _wishlists ??= new WishlistRepository(_context);
```

#### Step 10: Create Service Interface

**File:** `src/CandleStore.Application/Interfaces/IWishlistService.cs`

```csharp
using CandleStore.Application.DTOs.Wishlist;

namespace CandleStore.Application.Interfaces
{
    public interface IWishlistService
    {
        Task<List<WishlistItemDto>> GetWishlistAsync(Guid customerId);
        Task<int> GetWishlistCountAsync(Guid customerId);
        Task<WishlistItemDto> AddToWishlistAsync(Guid customerId, Guid productId);
        Task RemoveFromWishlistAsync(Guid customerId, Guid productId);
        Task MoveToCartAsync(Guid customerId, Guid productId);
        Task BulkMoveToCartAsync(Guid customerId, List<Guid> productIds);
        Task UpdateNoteAsync(Guid customerId, Guid productId, string? note);
        Task<int> MergeGuestWishlistAsync(Guid customerId, List<Guid> guestProductIds);
        Task<string> CreateShareLinkAsync(Guid customerId, bool isPublic, string? password);
        Task<List<WishlistItemDto>> GetSharedWishlistAsync(Guid shareId, string? password);
    }
}
```

#### Step 11: Implement Wishlist Service

**File:** `src/CandleStore.Application/Services/WishlistService.cs`

```csharp
using AutoMapper;
using CandleStore.Application.DTOs.Wishlist;
using CandleStore.Application.Interfaces;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;
using CandleStore.Application.Exceptions;

namespace CandleStore.Application.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICartService _cartService;

        private const int MaxWishlistSize = 100;

        public WishlistService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICartService cartService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cartService = cartService;
        }

        public async Task<List<WishlistItemDto>> GetWishlistAsync(Guid customerId)
        {
            var items = await _unitOfWork.Wishlists.GetByCustomerIdAsync(customerId);

            var dtos = items.Select(w => new WishlistItemDto
            {
                Id = w.Id,
                ProductId = w.ProductId,
                ProductName = w.ProductName,
                ProductPrice = w.ProductPrice,
                ProductImageUrl = w.ProductImageUrl,
                AddedAt = w.AddedAt,
                Note = w.Note,
                IsNotifyOnPriceDropEnabled = w.IsNotifyOnPriceDropEnabled,
                IsNotifyOnBackInStockEnabled = w.IsNotifyOnBackInStockEnabled,
                IsInStock = w.Product.StockQuantity > 0,
                PriceAtTimeOfAdd = w.PriceAtTimeOfAdd,
                HasPriceDropped = w.HasPriceDropped(w.Product.Price),
                PriceDropAmount = w.HasPriceDropped(w.Product.Price) ? w.GetPriceDropAmount(w.Product.Price) : null,
                PriceDropPercentage = w.HasPriceDropped(w.Product.Price) ? w.GetPriceDropPercentage(w.Product.Price) : null
            }).ToList();

            return dtos;
        }

        public async Task<int> GetWishlistCountAsync(Guid customerId)
        {
            return await _unitOfWork.Wishlists.GetCountByCustomerIdAsync(customerId);
        }

        public async Task<WishlistItemDto> AddToWishlistAsync(Guid customerId, Guid productId)
        {
            // Check for duplicate
            var existing = await _unitOfWork.Wishlists.GetByCustomerAndProductAsync(customerId, productId);
            if (existing != null)
            {
                throw new InvalidOperationException("Product already in wishlist");
            }

            // Check wishlist size limit
            var count = await _unitOfWork.Wishlists.GetCountByCustomerIdAsync(customerId);
            if (count >= MaxWishlistSize)
            {
                throw new InvalidOperationException($"Wishlist full (maximum {MaxWishlistSize} items)");
            }

            // Get product details
            var product = await _unitOfWork.Products.GetByIdAsync(productId);
            if (product == null)
            {
                throw new NotFoundException($"Product with ID '{productId}' not found");
            }

            // Get first image URL
            var imageUrl = product.Images?.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url ?? string.Empty;

            // Create wishlist item
            var wishlistItem = new WishlistItem
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ProductId = productId,
                ProductName = product.Name,
                ProductPrice = product.Price,
                ProductImageUrl = imageUrl,
                PriceAtTimeOfAdd = product.Price,
                AddedAt = DateTime.UtcNow,
                IsNotifyOnBackInStockEnabled = product.StockQuantity == 0 // Auto-enable if out of stock
            };

            await _unitOfWork.Wishlists.AddAsync(wishlistItem);
            await _unitOfWork.CompleteAsync();

            return new WishlistItemDto
            {
                Id = wishlistItem.Id,
                ProductId = wishlistItem.ProductId,
                ProductName = wishlistItem.ProductName,
                ProductPrice = wishlistItem.ProductPrice,
                ProductImageUrl = wishlistItem.ProductImageUrl,
                AddedAt = wishlistItem.AddedAt,
                IsInStock = product.StockQuantity > 0,
                PriceAtTimeOfAdd = wishlistItem.PriceAtTimeOfAdd
            };
        }

        public async Task RemoveFromWishlistAsync(Guid customerId, Guid productId)
        {
            var item = await _unitOfWork.Wishlists.GetByCustomerAndProductAsync(customerId, productId);
            if (item == null)
            {
                throw new NotFoundException("Wishlist item not found");
            }

            await _unitOfWork.Wishlists.DeleteAsync(item);
            await _unitOfWork.CompleteAsync();
        }

        public async Task MoveToCartAsync(Guid customerId, Guid productId)
        {
            var wishlistItem = await _unitOfWork.Wishlists.GetByCustomerAndProductAsync(customerId, productId);
            if (wishlistItem == null)
            {
                throw new NotFoundException("Wishlist item not found");
            }

            // Add to cart
            await _cartService.AddItemAsync(customerId, productId, quantity: 1);

            // Remove from wishlist
            await _unitOfWork.Wishlists.DeleteAsync(wishlistItem);
            await _unitOfWork.CompleteAsync();
        }

        public async Task BulkMoveToCartAsync(Guid customerId, List<Guid> productIds)
        {
            foreach (var productId in productIds)
            {
                try
                {
                    await MoveToCartAsync(customerId, productId);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other items
                    Console.WriteLine($"Error moving product {productId} to cart: {ex.Message}");
                }
            }
        }

        public async Task UpdateNoteAsync(Guid customerId, Guid productId, string? note)
        {
            if (note != null && note.Length > 200)
            {
                throw new ValidationException("Note cannot exceed 200 characters");
            }

            var item = await _unitOfWork.Wishlists.GetByCustomerAndProductAsync(customerId, productId);
            if (item == null)
            {
                throw new NotFoundException("Wishlist item not found");
            }

            item.Note = note;
            await _unitOfWork.CompleteAsync();
        }

        public async Task<int> MergeGuestWishlistAsync(Guid customerId, List<Guid> guestProductIds)
        {
            int mergedCount = 0;

            foreach (var productId in guestProductIds)
            {
                try
                {
                    // Check if already in wishlist
                    var existing = await _unitOfWork.Wishlists.GetByCustomerAndProductAsync(customerId, productId);
                    if (existing != null)
                    {
                        continue; // Skip duplicates
                    }

                    // Add to wishlist
                    await AddToWishlistAsync(customerId, productId);
                    mergedCount++;
                }
                catch (Exception ex)
                {
                    // Log error but continue
                    Console.WriteLine($"Error merging guest product {productId}: {ex.Message}");
                }
            }

            return mergedCount;
        }

        public async Task<string> CreateShareLinkAsync(Guid customerId, bool isPublic, string? password)
        {
            var shareId = Guid.NewGuid();

            var sharedWishlist = new SharedWishlist
            {
                ShareId = shareId,
                CustomerId = customerId,
                IsPublic = isPublic,
                PasswordHash = isPublic ? null : BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1)
            };

            _unitOfWork.Context.SharedWishlists.Add(sharedWishlist);
            await _unitOfWork.CompleteAsync();

            return shareId.ToString();
        }

        public async Task<List<WishlistItemDto>> GetSharedWishlistAsync(Guid shareId, string? password)
        {
            var sharedWishlist = await _unitOfWork.Context.SharedWishlists
                .FirstOrDefaultAsync(s => s.ShareId == shareId);

            if (sharedWishlist == null)
            {
                throw new NotFoundException("Shared wishlist not found");
            }

            if (sharedWishlist.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Shared wishlist link has expired");
            }

            // Check password for private wishlists
            if (!sharedWishlist.IsPublic)
            {
                if (string.IsNullOrEmpty(password))
                {
                    throw new UnauthorizedAccessException("Password required for private wishlist");
                }

                if (!BCrypt.Net.BCrypt.Verify(password, sharedWishlist.PasswordHash))
                {
                    throw new UnauthorizedAccessException("Invalid password");
                }
            }

            // Return wishlist items
            return await GetWishlistAsync(sharedWishlist.CustomerId);
        }
    }
}
```

#### Step 12: Create API Controller

**File:** `src/CandleStore.Api/Controllers/WishlistController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CandleStore.Application.Interfaces;
using CandleStore.Application.DTOs.Wishlist;
using CandleStore.Api.Models;

namespace CandleStore.Api.Controllers
{
    [ApiController]
    [Route("api/wishlist")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(customerIdClaim))
            {
                throw new UnauthorizedAccessException("Customer ID not found in token");
            }
            return Guid.Parse(customerIdClaim);
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<WishlistItemDto>>>> GetWishlist()
        {
            var customerId = GetCustomerId();
            var items = await _wishlistService.GetWishlistAsync(customerId);

            return Ok(new ApiResponse<List<WishlistItemDto>>
            {
                Success = true,
                Data = items
            });
        }

        [HttpGet("count")]
        public async Task<ActionResult<ApiResponse<int>>> GetWishlistCount()
        {
            var customerId = GetCustomerId();
            var count = await _wishlistService.GetWishlistCountAsync(customerId);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = count
            });
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<WishlistItemDto>>> AddToWishlist(
            [FromBody] AddToWishlistDto dto)
        {
            var customerId = GetCustomerId();
            var item = await _wishlistService.AddToWishlistAsync(customerId, dto.ProductId);

            return Ok(new ApiResponse<WishlistItemDto>
            {
                Success = true,
                Data = item,
                Message = "Added to wishlist"
            });
        }

        [HttpDelete("{productId}")]
        public async Task<ActionResult<ApiResponse<bool>>> RemoveFromWishlist(Guid productId)
        {
            var customerId = GetCustomerId();
            await _wishlistService.RemoveFromWishlistAsync(customerId, productId);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Removed from wishlist"
            });
        }

        [HttpPost("{productId}/move-to-cart")]
        public async Task<ActionResult<ApiResponse<bool>>> MoveToCart(Guid productId)
        {
            var customerId = GetCustomerId();
            await _wishlistService.MoveToCartAsync(customerId, productId);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Moved to cart"
            });
        }

        [HttpPost("bulk-move-to-cart")]
        public async Task<ActionResult<ApiResponse<bool>>> BulkMoveToCart(
            [FromBody] List<Guid> productIds)
        {
            var customerId = GetCustomerId();
            await _wishlistService.BulkMoveToCartAsync(customerId, productIds);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = $"Moved {productIds.Count} items to cart"
            });
        }

        [HttpPut("{productId}/note")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateNote(
            Guid productId,
            [FromBody] UpdateWishlistNoteDto dto)
        {
            var customerId = GetCustomerId();
            await _wishlistService.UpdateNoteAsync(customerId, productId, dto.Note);

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Data = true,
                Message = "Note updated"
            });
        }

        [HttpPost("merge")]
        public async Task<ActionResult<ApiResponse<int>>> MergeGuestWishlist(
            [FromBody] List<Guid> guestProductIds)
        {
            var customerId = GetCustomerId();
            var mergedCount = await _wishlistService.MergeGuestWishlistAsync(customerId, guestProductIds);

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = mergedCount,
                Message = $"Merged {mergedCount} items from guest wishlist"
            });
        }

        [HttpPost("share")]
        public async Task<ActionResult<ApiResponse<string>>> CreateShareLink(
            [FromBody] CreateShareLinkDto dto)
        {
            var customerId = GetCustomerId();
            var shareId = await _wishlistService.CreateShareLinkAsync(
                customerId, dto.IsPublic, dto.Password);

            var shareUrl = $"{Request.Scheme}://{Request.Host}/wishlist/share/{shareId}";

            return Ok(new ApiResponse<string>
            {
                Success = true,
                Data = shareUrl,
                Message = "Share link created"
            });
        }

        [HttpGet("share/{shareId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<WishlistItemDto>>>> GetSharedWishlist(
            Guid shareId,
            [FromQuery] string? password)
        {
            var items = await _wishlistService.GetSharedWishlistAsync(shareId, password);

            return Ok(new ApiResponse<List<WishlistItemDto>>
            {
                Success = true,
                Data = items
            });
        }
    }
}
```

#### Step 13: Register Services in Dependency Injection

**File:** `src/CandleStore.Api/Program.cs`

```csharp
// Add to existing service registrations
builder.Services.AddScoped<IWishlistService, WishlistService>();

// Add Hangfire for background jobs
builder.Services.AddHangfire(config => config
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer();

// Add Redis for caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
```

#### Step 14: Create Blazor Wishlist Page Component

**File:** `src/CandleStore.Storefront/Components/Pages/Wishlist.razor`

```razor
@page "/account/wishlist"
@using CandleStore.Application.DTOs.Wishlist
@using CandleStore.Application.Interfaces
@inject IWishlistService WishlistService
@inject ICartService CartService
@inject NavigationManager Navigation
@inject ISnackbar Snackbar
@attribute [Authorize]

<PageTitle>My Wishlist</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large" Class="my-8">
    <MudText Typo="Typo.h4" GutterBottom="true">
        My Wishlist (@_wishlistItems.Count items)
    </MudText>

    @if (_isLoading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else if (!_wishlistItems.Any())
    {
        <MudPaper Class="pa-8 text-center">
            <MudIcon Icon="@Icons.Material.Filled.FavoriteBorder" Size="Size.Large" Color="Color.Secondary" />
            <MudText Typo="Typo.h6" GutterBottom="true">Your wishlist is empty</MudText>
            <MudText Color="Color.Secondary">Save products you love to purchase later!</MudText>
            <MudButton Variant="Variant.Filled" Color="Color.Primary" Href="/products" Class="mt-4">
                Continue Shopping
            </MudButton>
        </MudPaper>
    }
    else
    {
        <MudGrid>
            @foreach (var item in _wishlistItems)
            {
                <MudItem xs="12" sm="6" md="4">
                    <MudCard>
                        <MudCardMedia Image="@item.ProductImageUrl" Height="200" />
                        <MudCardContent>
                            <MudText Typo="Typo.h6">@item.ProductName</MudText>
                            <MudText Typo="Typo.body2" Color="Color.Secondary">
                                Added: @item.AddedAt.ToString("MMM dd, yyyy")
                            </MudText>

                            @if (item.HasPriceDropped)
                            {
                                <MudAlert Severity="Severity.Success" Dense="true" Class="mt-2">
                                    Price drop! Save @item.PriceDropAmount?.ToString("C")
                                </MudAlert>
                            }

                            @if (!item.IsInStock)
                            {
                                <MudAlert Severity="Severity.Warning" Dense="true" Class="mt-2">
                                    Out of Stock - You'll be notified when available
                                </MudAlert>
                            }

                            <MudText Typo="Typo.h5" Color="Color.Primary" Class="mt-2">
                                @item.ProductPrice.ToString("C")
                            </MudText>

                            @if (!string.IsNullOrEmpty(item.Note))
                            {
                                <MudText Typo="Typo.body2" Color="Color.Secondary" Class="mt-2">
                                    Note: @item.Note
                                </MudText>
                            }
                        </MudCardContent>
                        <MudCardActions>
                            <MudButton Variant="Variant.Filled" Color="Color.Primary"
                                       OnClick="() => MoveToCart(item.ProductId)"
                                       Disabled="!item.IsInStock">
                                Move to Cart
                            </MudButton>
                            <MudButton Variant="Variant.Text" Color="Color.Error"
                                       OnClick="() => RemoveItem(item.ProductId)">
                                Remove
                            </MudButton>
                        </MudCardActions>
                    </MudCard>
                </MudItem>
            }
        </MudGrid>
    }
</MudContainer>

@code {
    private List<WishlistItemDto> _wishlistItems = new();
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadWishlist();
    }

    private async Task LoadWishlist()
    {
        _isLoading = true;
        try
        {
            _wishlistItems = await WishlistService.GetWishlistAsync(GetCustomerId());
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading wishlist: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task MoveToCart(Guid productId)
    {
        try
        {
            await WishlistService.MoveToCartAsync(GetCustomerId(), productId);
            Snackbar.Add("Moved to cart!", Severity.Success);
            await LoadWishlist();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task RemoveItem(Guid productId)
    {
        try
        {
            await WishlistService.RemoveFromWishlistAsync(GetCustomerId(), productId);
            Snackbar.Add("Removed from wishlist", Severity.Success);
            await LoadWishlist();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private Guid GetCustomerId()
    {
        // Extract from authenticated user claims
        var claim = AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User
            .FindFirst(ClaimTypes.NameIdentifier);
        return Guid.Parse(claim.Value);
    }
}
```

### Integration Points

**With Task 014 (Shopping Cart):**
- `MoveToCartAsync` calls `ICartService.AddItemAsync(customerId, productId, quantity: 1)`
- Cart and wishlist operate independently; moving item to cart removes it from wishlist
- Both cart and wishlist count badges update via Blazor state management

**With Task 021 (Customer Authentication):**
- All wishlist endpoints protected with `[Authorize]` attribute
- CustomerId extracted from JWT claims using `User.FindFirst(ClaimTypes.NameIdentifier)`
- Guest wishlist merge occurs during login flow via `MergeGuestWishlistAsync()`

**With Task 025 (Email Marketing):**
- Price drop emails sent via background job calling `IEmailService.SendPriceDropAlertAsync()`
- Back-in-stock emails triggered by inventory update events
- Email templates stored in `/EmailTemplates/wishlist-price-drop.html` and `wishlist-back-in-stock.html`

**With Task 003 (Product Catalog):**
- Wishlist stores ProductId foreign key referencing Products table
- Denormalized product data (name, price, imageUrl) synced nightly via background job
- Product deletion cascades to wishlist items (ON DELETE CASCADE)

### Assumptions and Design Decisions

**Assumption 1:** Customers prefer automatic back-in-stock notifications for out-of-stock wishlist items
- **Rationale:** Manually enabling notifications adds friction; auto-enabling increases conversion

**Decision 1:** Use denormalized product data (name, price, imageUrl) in WishlistItem entity
- **Rationale:** Avoids expensive JOIN queries when loading wishlist page; acceptable tradeoff for slight data duplication
- **Tradeoff:** Requires nightly sync job to update denormalized data if product details change

**Decision 2:** Maximum 100 wishlist items for authenticated users, 50 for guest users
- **Rationale:** Prevents abuse and database bloat; 100 items sufficient for 99% of users
- **Lower limit for guests:** Incentivizes account creation

**Decision 3:** Price drop threshold set at 10% minimum
- **Rationale:** Smaller discounts (5%) generate too many notifications, causing unsubscribes; 10% balances relevance and frequency

**Decision 4:** Share links expire after 1 year of owner inactivity
- **Rationale:** Prevents indefinite storage of abandoned share links; 1 year sufficient for gift events

### Error Handling

```csharp
try
{
    await _wishlistService.AddToWishlistAsync(customerId, productId);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("already in wishlist"))
{
    return BadRequest(new ApiResponse<object>
    {
        Success = false,
        Message = "Product already in your wishlist"
    });
}
catch (InvalidOperationException ex) when (ex.Message.Contains("Wishlist full"))
{
    return BadRequest(new ApiResponse<object>
    {
        Success = false,
        Message = "Wishlist full (maximum 100 items). Remove items to add new ones."
    });
}
catch (NotFoundException ex)
{
    return NotFound(new ApiResponse<object>
    {
        Success = false,
        Message = ex.Message
    });
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error adding to wishlist");
    return StatusCode(500, new ApiResponse<object>
    {
        Success = false,
        Message = "An error occurred while adding to wishlist"
    });
}
```

### Testing the Implementation

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "Category=Unit"

# Run integration tests with database
dotnet test --filter "FullyQualifiedName~WishlistControllerTests"

# Check code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

**Manual Testing:**
1. Start API: `dotnet run --project src/CandleStore.Api`
2. Start Storefront: `dotnet run --project src/CandleStore.Storefront`
3. Navigate to `https://localhost:5001/products`
4. Click heart icon on any product
5. Verify wishlist count badge increments
6. Navigate to `https://localhost:5001/account/wishlist`
7. Verify product appears in wishlist

### Validation Steps

1. **Database Schema:** Verify `WishlistItems` and `SharedWishlists` tables exist with correct columns and indexes
2. **API Endpoints:** Test all endpoints with Postman/Swagger (GET, POST, DELETE)
3. **Authentication:** Verify endpoints return 401 Unauthorized when JWT token missing
4. **Duplicate Prevention:** Attempt to add same product twice; verify 400 Bad Request
5. **Guest Merge:** Add items as guest, log in, verify items merged into authenticated wishlist
6. **Move to Cart:** Verify item moves from wishlist to cart and count badges update
7. **Price Drop Detection:** Lower product price by 15%, verify `HasPriceDropped` returns true
8. **Share Link:** Create public share link, open in incognito window, verify access without login

### Next Steps After This Task

After completing this task:
1. Proceed to Task 034 (Customer Service Ticketing System) - wishlist feature complete
2. Optionally implement wishlist analytics dashboard for admin (Task 041)
3. Consider A/B testing wishlist button placement for conversion optimization

### Common Pitfalls to Avoid

❌ **Don't:** Store full Product entity in wishlist (causes JOIN performance issues)
✅ **Do:** Denormalize product data (name, price, imageUrl) in WishlistItem for fast reads

❌ **Don't:** Send price drop emails immediately on every price change
✅ **Do:** Rate limit to 1 email per product per customer per week

❌ **Don't:** Allow unlimited wishlist size (enables abuse, database bloat)
✅ **Do:** Enforce 100-item limit with clear error message

❌ **Don't:** Forget to cascade delete wishlist items when customer/product deleted
✅ **Do:** Use ON DELETE CASCADE in foreign key constraints

❌ **Don't:** Expose CustomerId in API URLs (e.g., `/api/wishlist/{customerId}`)
✅ **Do:** Extract CustomerId from JWT token claims for authorization

---

**END OF TASK 030**
