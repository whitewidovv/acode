# Task 025: SEO Optimization

**Priority:** P1 (High)
**Tier:** Marketing & Growth
**Complexity:** 8 Fibonacci points (Medium-High)
**Phase:** Phase 8 - Marketing and Analytics

**Dependencies:**
- Task 011: Product API Endpoints (Product entity must exist)
- Task 012: Category Management (Category entity must exist)
- Task 014: Product Images (Image entity with alt text)

**Integration Points:**
- Task 027: Google Analytics Tracking (tracks organic search traffic)
- Task 028: CDN Cloudflare Setup (improves Core Web Vitals for SEO)

---

## 1. Description

SEO (Search Engine Optimization) optimization transforms the Candle Store from an invisible online storefront into a discoverable business that ranks on Google's first page for high-intent search queries like "lavender candles near me," "handmade soy candles," and "scented candles Portland Oregon." This implementation integrates technical SEO fundamentals (meta tags, structured data, sitemaps), on-page optimization (URL slugs, heading hierarchy, image alt text), and content optimization (keyword-rich product descriptions, category pages, blog content) to drive 300-500% organic traffic growth within 6-12 months. For Sarah, this means reducing customer acquisition cost from $25/customer (Facebook ads) to $3/customer (organic search) while establishing long-term brand authority that generates passive traffic without ongoing ad spend.

**Business Value:**

For the **store owner (Sarah)**, SEO optimization reduces dependence on paid advertising that currently costs $1,500-$2,000/month with declining ROI (cost per acquisition rising from $18 to $25 over 6 months due to ad fatigue and increased competition). Organic search traffic is "free" after initial setup - ranking #3 on Google for "handmade candles Oregon" drives 150-200 monthly clicks with 8-12% conversion rate (12-24 orders/month = $600-$1,200 revenue) with zero ongoing cost beyond content maintenance. Long-tail keyword optimization ("lavender vanilla candle for bedroom" instead of just "candles") captures high-intent buyers with 3-5x higher conversion rates (15-25% vs 5-8% for generic searches). Local SEO (Google Business Profile optimization, local citations, location pages) drives foot traffic to physical store/studio if Sarah hosts workshops or offers local pickup - "candles near me" searches convert 40-60% for local businesses. SEO builds compounding value - rankings improve over time as domain authority grows, creating durable traffic channel that persists even if Sarah pauses new content creation.

For **customers (Alex)**, discoverable product pages mean finding exactly the candle they need via natural search behavior ("best candles for relaxation" → discovers Lavender Dreams Candle product page ranking #4 on Google) rather than stumbling on random ads. Rich snippets (star ratings, price, availability displayed in search results) enable informed pre-click decisions - Alex can see 4.8★ rating and $24.99 price before clicking, reducing friction. FAQ schema markup surfaces answers directly in Google search results ("How long do soy candles burn?" → "Answer: 40-50 hours for 8oz candles") providing value even without clicking through to site. Blog content optimized for educational queries ("how to make candles last longer," "difference between soy and paraffin wax") builds trust and positions CandleStore as expert resource, not just transactional seller.

For **developers (David)**, modern SEO is implemented via structured metadata and semantic HTML rather than black-hat tactics (keyword stuffing, link farms). .NET 8's Blazor Server architecture poses SEO challenges (JavaScript-heavy, client-side rendering) solved by server-side pre-rendering for product/category pages (Blazor's `@rendermode` attribute) and static site generation for blog content. Clean URLs via slug-based routing (`/products/lavender-dreams-candle` not `/products?id=47`) leverage ASP.NET Core's routing middleware. Structured data (JSON-LD format) integrates cleanly via `<script type="application/ld+json">` blocks in page heads. Automated sitemap generation via EF Core queries ensures all products/categories indexed without manual updates. Performance optimization (image compression, lazy loading, CDN integration from Task 028) indirectly boosts SEO via Core Web Vitals (Largest Contentful Paint, First Input Delay metrics Google uses for ranking).

**Technical Approach:**

The implementation follows **Google's Core Web Vitals + E-A-T (Expertise, Authoritativeness, Trustworthiness) framework**. SEO flow integrates at multiple layers: (1) **Infrastructure Layer:** `SeoService` generates meta tags, JSON-LD structured data, canonical URLs for products/categories. XML sitemap auto-generated from database via EF Core queries (`_context.Products.Where(p => p.IsPublished).Select(p => new SitemapEntry { Url = p.Slug, LastMod = p.UpdatedAt })`). (2) **Application Layer:** `ProductService` enforces URL slug uniqueness (slugify product names: "Lavender Dreams" → "lavender-dreams", "Vanilla Bliss" → "vanilla-bliss") with collision detection ("lavender-dreams-2" if duplicate). `ImageService` generates alt text from product names/descriptions for accessibility and image SEO. (3) **UI Layer (Blazor):** Base page component `<SeoHead>` injects meta tags (`<title>`, `<meta name="description">`, Open Graph tags for social sharing). Product detail pages pre-render server-side for Google crawler (`@rendermode InteractiveServer` → `@rendermode Static` for SEO-critical pages). Blog component supports markdown content with automatic H1/H2/H3 hierarchy validation. (4) **Performance:** Implements lazy loading for images (`loading="lazy"` attribute), minifies CSS/JS via ASP.NET Core bundling, leverages CDN for static assets (Task 028 integration). (5) **Analytics Integration:** Tracks organic search traffic, keyword rankings, conversion rates via Google Search Console API (Task 027 integration) to measure SEO ROI.

**Technical SEO Checklist:**
- **Meta Tags:** Unique title (50-60 chars), description (150-160 chars) for every product/category page
- **Structured Data:** Product schema (name, price, availability, ratings), Organization schema, Breadcrumb schema
- **URL Structure:** Clean slugs (`/products/lavender-candle` not `/p/123`), logical hierarchy (`/categories/soy-candles/lavender`)
- **Sitemap:** XML sitemap at `/sitemap.xml`, auto-updated nightly, submitted to Google Search Console
- **Robots.txt:** Blocks admin panel (`Disallow: /admin`), allows product/category crawling (`Allow: /products`, `Allow: /categories`)
- **Canonical URLs:** Prevent duplicate content issues (product accessible via category → canonical points to `/products/slug`)
- **Mobile Optimization:** Responsive design (MudBlazor already responsive), mobile-first indexing compliance
- **Page Speed:** <2.5s Largest Contentful Paint, <100ms First Input Delay (Core Web Vitals thresholds)
- **HTTPS:** SSL/TLS encryption (SEO ranking factor since 2014)
- **Accessibility:** WCAG 2.1 AA compliance (semantic HTML, ARIA labels, keyboard navigation) - overlaps with SEO

**On-Page SEO Best Practices:**
- **Heading Hierarchy:** H1 (product name, 1 per page), H2 (section headers: "Description", "Ingredients", "Reviews"), H3 (subsections)
- **Keyword Placement:** Primary keyword in title, first 100 words of description, at least one H2, image alt text
- **Content Length:** Product descriptions 300-500 words (not 2 sentences), category pages 500-800 words with buying guides
- **Internal Linking:** Related products, category breadcrumbs, contextual blog links (blog post "Best Candles for Meditation" links to Lavender Dreams product)
- **Image Optimization:** Descriptive filenames (`lavender-dreams-candle-soy-wax.jpg` not `IMG_1234.jpg`), alt text with keywords ("Handmade lavender candle in glass jar with purple label"), WebP format (50-70% smaller than JPEG), lazy loading
- **User Intent Matching:** Informational queries → blog content, Transactional queries → product pages, Navigational queries → homepage/about page

**Content Strategy:**
- **Product Pages:** Storytelling approach ("Hand-poured in Eugene, Oregon using 100% soy wax and essential oils...") not just bullet points
- **Category Pages:** Buying guides ("How to Choose the Right Candle for Your Space: Size, Scent, Burn Time"), comparison tables
- **Blog Content:** Educational posts (400-800 words): "10 Tips to Make Your Candles Last Longer," "Soy vs Paraffin: The Ultimate Guide," "Candle Safety: Do's and Don'ts"
- **FAQ Pages:** Target question-based keywords ("how long do soy candles burn?", "are soy candles safe for pets?") with schema markup for rich snippets
- **Local SEO Pages:** "Handmade Candles in Portland, Oregon" landing page with embedded Google Maps, local business schema, customer testimonials with location mentions

**Integration Points:**

- **Task 011 (Product API Endpoints):** `Product` entity requires `Slug` (string), `MetaTitle` (string, 60 chars), `MetaDescription` (string, 160 chars), `IsPublished` (bool) properties. API endpoint `GET /api/products/{slug}` retrieves product by slug instead of numeric ID for SEO-friendly URLs.
- **Task 012 (Category Management):** `Category` entity requires `Slug`, `MetaTitle`, `MetaDescription` properties. Category pages at `/categories/{slug}` with server-side rendering for crawler accessibility.
- **Task 014 (Product Images):** Image upload flow auto-generates alt text from product name ("Lavender Dreams Candle - Handmade Soy Candle"). Images compressed to WebP format (Task 028 CDN integration). Lazy loading implemented via `loading="lazy"` attribute.
- **Task 027 (Google Analytics):** Tracks organic search traffic source (`utm_source=google`, `utm_medium=organic`), conversion rates by keyword, measures SEO ROI. Google Search Console integration tracks keyword rankings, click-through rates, impressions.
- **Task 028 (CDN Cloudflare Setup):** CDN serves static assets (images, CSS, JS) from edge locations, reducing page load time. Cloudflare automatically minifies HTML/CSS/JS. Improves Core Web Vitals metrics used for SEO ranking.

**Constraints and Considerations:**

- **Blazor SSR Challenges:** Blazor Server default render mode uses SignalR WebSockets for interactivity, requiring JavaScript execution. Google crawler executes JavaScript but performance penalty exists. Solution: Use `@rendermode Static` or `@rendermode InteractiveServer` with `@attribute [RenderModePrerender]` for SEO-critical pages to pre-render HTML on server before sending to client.
- **Duplicate Content Risk:** Same product accessible via multiple URLs (`/products/lavender-candle`, `/categories/soy-candles/lavender-candle`, `/search?q=lavender`) causes ranking dilution. Solution: Canonical tags pointing to primary URL (`<link rel="canonical" href="/products/lavender-candle">`).
- **Thin Content Penalty:** Pages with <300 words or duplicate manufacturer descriptions get penalized. Solution: Enforce minimum content length in admin panel validation ("Description must be at least 300 words"), write unique descriptions for all products.
- **Page Speed vs Rich Features:** High-resolution product images (Task 014) conflict with fast load times. Solution: Responsive images with `srcset` attribute (serve 400px image on mobile, 1200px on desktop), lazy loading below fold, WebP compression.
- **Keyword Cannibalization:** Multiple pages targeting same keyword ("lavender candles") compete for rankings. Solution: Keyword mapping spreadsheet, unique primary keywords per page (product page: "lavender dreams candle," category page: "lavender scented candles collection," blog post: "best lavender candles for relaxation").
- **Schema Markup Maintenance:** Product schema requires `price`, `availability`, `rating` to stay synchronized with database. Solution: Auto-generate JSON-LD from entity properties in `SeoService`, unit tests verify schema validity against Google's Structured Data Testing Tool.
- **Ranking Timeline:** SEO is slow - 3-6 months for competitive keywords, 1-3 months for long-tail. Sarah must continue paid ads during ramp-up period. Set realistic expectations: "SEO is a marathon, not a sprint."

**SEO KPIs (Key Performance Indicators):**
- **Organic Traffic:** Target 300-500 monthly sessions from organic search within 6 months (baseline: 50/month)
- **Keyword Rankings:** 10 products ranking in top 10 for primary keywords within 6 months, 50% of products in top 50
- **Click-Through Rate (CTR):** 3-5% CTR from Google search results (industry average: 2-3%)
- **Conversion Rate:** 8-12% conversion rate for organic traffic (higher intent than paid ads)
- **Core Web Vitals:** LCP <2.5s, FID <100ms, CLS <0.1 for 75th percentile of page loads
- **Domain Authority:** Increase from 5-10 (new site) to 20-30 within 12 months (Moz/Ahrefs metric)
- **Backlinks:** Acquire 20-50 quality backlinks from candle/lifestyle blogs, local business directories within 12 months

**Local SEO Strategy (If Applicable):**

If Sarah has physical store location or offers local pickup:
- **Google Business Profile:** Claim listing, optimize with photos, business hours, candle categories, "Women-Owned Business" attribute
- **NAP Consistency:** Name, Address, Phone number identical across Google, Yelp, Facebook, website footer
- **Local Citations:** List business on Yelp, YellowPages, Nextdoor, local Eugene/Oregon business directories
- **Location Pages:** Create `/locations/eugene-oregon` page with embedded map, directions, local testimonials
- **Local Keywords:** Target "candles Eugene Oregon," "handmade candles Portland," "candles near me"
- **Local Backlinks:** Sponsor local events, get featured in Eugene Weekly newspaper, partner with local gift shops

---

## 2. Use Cases

### Use Case 1: Alex Discovers Lavender Dreams Candle via Organic Search

**Scenario:** Alex is searching for a relaxation aid for her evening meditation routine. She types "best candles for relaxation lavender" into Google. She's never heard of CandleStore before but discovers it via organic search results.

**Without This Feature:**
Alex searches "best candles for relaxation lavender" on Google. Search results show:
1. Amazon - "Lavender Candles 3-Pack - $19.99"
2. Yankee Candle - "Lavender Spa Collection"
3. Target - "Threshold Lavender Candle - $12.99"
4. Generic listicle: "Top 10 Lavender Candles of 2025" (affiliate spam)
5-10. More big-box retailers and affiliate sites

CandleStore doesn't appear in results because:
- Product pages lack meta titles/descriptions (Google displays "Product | CandleStore" generic title)
- No structured data (no star ratings visible in search results)
- Product description is 2 sentences (thin content penalty)
- Image filenames are "IMG_1234.jpg" (no image search visibility)
- URL is `/products/47` (not `/products/lavender-dreams-candle` - URL structure penalty)

Alex clicks on Amazon's 3-pack, purchases there. Sarah loses $24.99 sale. Sarah's only discovery channel is Facebook ads ($25 cost per acquisition) which Alex might ignore due to ad fatigue.

**With This Feature:**
1. Alex searches "best candles for relaxation lavender" on Google
2. Search results now include CandleStore:
   ```
   4. Lavender Dreams Candle - Handmade Soy Candle | CandleStore
      candlestore.com › products › lavender-dreams-candle
      ★★★★★ 4.8 (47 reviews) · $24.99 · In stock
      Hand-poured lavender candle made with 100% soy wax and
      essential oils. Burns 40-50 hours. Perfect for meditation,
      yoga, and relaxation. Made in Eugene, Oregon.
   ```
3. Rich snippet displays:
   - Star rating: ★★★★★ 4.8 (from Product schema markup)
   - Price: $24.99 (from Product schema)
   - Availability: "In stock" (from Product schema)
   - Preview text: First 160 chars of meta description
4. Alex notices several trust signals:
   - High rating (4.8★) validates quality
   - "Handmade" + "Made in Eugene, Oregon" suggests artisan quality vs mass-market
   - "100% soy wax and essential oils" appeals to natural product preference
   - "40-50 hour burn time" provides specific value proposition
5. Alex clicks through to product page
6. Product page loads quickly (<2s LCP due to image optimization)
7. Page displays:
   - H1: "Lavender Dreams Candle - Handmade Soy Candle for Relaxation"
   - Detailed 400-word description including storytelling ("Sarah hand-pours each candle in her Eugene studio using locally-sourced soy wax...")
   - Section headers (H2): "Scent Profile," "Ingredients," "Burn Time," "Customer Reviews"
   - High-quality images with descriptive alt text ("Lavender Dreams candle in glass jar with purple label, lit with soft glow")
   - Internal links to related products ("Customers also loved: Vanilla Bliss Candle, Meditation Candle Collection")
8. Alex adds to cart, completes purchase
9. **Total cost to Sarah for this customer: $0** (organic traffic, no ad spend)

**Outcome (12 Months Post-Implementation):**
- **Organic Traffic:** 450 monthly organic search sessions (was 50/month)
- **"Lavender Candles" Rankings:** Position #7 on Google (Page 1)
- **Long-Tail Keywords:** Ranking in top 5 for 15 specific queries:
  - "lavender candle for meditation" (#3)
  - "handmade lavender candle Oregon" (#2)
  - "best soy lavender candle" (#5)
  - "lavender essential oil candle" (#4)
- **Conversion Rate:** 12% for organic traffic (vs 5% for paid ads) due to high intent
- **Revenue Impact:** 450 sessions × 12% conversion = 54 sales/month × $24.99 = $1,349/month organic revenue
- **Customer Acquisition Cost:** $0 (vs $25 for paid ads)
- **ROI:** Initial SEO implementation cost $800 (40 hours × $20/hour), recovered in 0.6 months, then pure profit

**Behavioral Insights:**
- Organic search users spend 3.2 minutes on site vs 1.8 minutes for paid ad traffic (higher engagement)
- 35% of organic visitors sign up for email newsletter vs 12% from ads (higher quality leads)
- 18% return purchase rate for organic customers vs 8% for ad-driven customers within 90 days (stronger brand affinity)

---

### Use Case 2: Sarah's Blog Post Ranks for "How to Make Candles Last Longer" and Drives 200 Monthly Visits

**Scenario:** Sarah creates educational blog content optimized for informational search queries. This builds topical authority and drives top-of-funnel traffic that converts over time.

**Without This Feature:**
Sarah doesn't have a blog. Her website contains only transactional pages (product listings, cart, checkout). When potential customers search "how to make candles last longer," they find:
1. WikiHow generic article
2. Yankee Candle blog post
3. Real Simple magazine article
4. Various candle company blogs

CandleStore is invisible for informational queries. Sarah misses opportunity to:
- Build brand awareness with people not ready to buy yet
- Establish expertise and trustworthiness
- Capture email addresses for nurture campaigns
- Internal link to product pages ("Trim your wick to 1/4 inch before each use. Our Wick Trimmer Tool makes this easy!")

Potential customer reads Yankee Candle's blog post, later buys from Yankee Candle because they're top-of-mind.

**With This Feature:**
1. Sarah writes 600-word blog post: "10 Expert Tips to Make Your Candles Last Longer (And Save Money)"
2. `SeoService` generates:
   - **Title:** "10 Ways to Make Candles Last Longer - Expert Tips | CandleStore" (58 chars)
   - **Meta Description:** "Learn how to extend your candle's burn time by 50% with these expert tips from professional candle maker Sarah. Plus, common mistakes to avoid." (153 chars)
   - **URL Slug:** `/blog/how-to-make-candles-last-longer`
   - **Heading Structure:**
     - H1: "10 Expert Tips to Make Your Candles Last Longer (And Save Money)"
     - H2: "1. Trim the Wick to 1/4 Inch Before Each Use"
     - H2: "2. Burn Candles for 3-4 Hours at a Time"
     - H2: "3. Avoid Drafts and Air Vents"
     - ... (10 tips total)
   - **FAQ Schema:**
     ```json
     {
       "@type": "FAQPage",
       "mainEntity": [{
         "question": "How long should you burn a candle?",
         "answer": "Burn candles for 3-4 hours at a time to ensure even wax pool. Never burn for more than 4 hours continuously."
       }]
     }
     ```
   - **Internal Links:** Each tip links to relevant products (Tip #1 links to Wick Trimmer Tool, Tip #5 links to Candle Care Kit)
3. Sarah publishes post, submits sitemap to Google Search Console
4. **Week 1-4:** Google indexes post, starts ranking on page 5-10 for target keywords
5. **Month 2-3:** Post climbs to page 2-3 as Google validates content quality (users click, don't bounce, spend 3+ minutes reading)
6. **Month 4:** Post reaches **position #5 on page 1** for "how to make candles last longer" (keyword difficulty: medium, search volume: 1,200/month)
7. **Month 6:** Post now ranking for 15 related keywords:
   - "how to make candles last longer" (#5) - 1,200 searches/month
   - "make candles burn longer" (#8) - 600 searches/month
   - "candle burning tips" (#7) - 400 searches/month
   - "how long should you burn a candle" (#3) - 800 searches/month via FAQ snippet
8. **Monthly Traffic to Blog Post:** 200 visitors/month (1,200 monthly searches × 5% CTR for position #5 + long-tail keywords)
9. **Conversion Funnel:**
   - 200 blog visitors
   - 35% click internal product links (70 people)
   - 15% of those purchase (10.5 sales)
   - Average order value: $45 (blog visitors buy multiple items: candle + wick trimmer + care kit)
   - **Monthly revenue from one blog post:** $472.50
10. **Email Capture:** Blog post includes newsletter signup CTA ("Get 10% off your first order + more candle care tips!"). 20% of visitors subscribe (40 email addresses/month). Nurture campaign converts 8% over 90 days (3.2 sales/month = $80/month additional revenue).

**Outcome (12 Months Post-Implementation):**
- **Blog Portfolio:** Sarah published 12 blog posts (1 per month), all ranking page 1-3
- **Total Monthly Blog Traffic:** 800 visitors/month across all posts
- **Blog-Driven Revenue:** $1,500/month from product clicks + email nurture
- **Brand Authority:** "Candle Expert" positioning enables partnerships with lifestyle bloggers, podcast interviews, local media features
- **Backlinks:** 8 external sites link to Sarah's blog posts as authoritative resources, boosting domain authority from 8 to 24 (Moz DA)
- **Time Investment:** 3 hours/post to write/optimize × 12 posts = 36 hours total (amortized to 3 hours/month)
- **Cost:** $0 beyond Sarah's time (or $600 if outsourced to freelance writer at $50/post)
- **ROI:** $1,500/month revenue ÷ $50/month amortized cost = 30x ROI

---

### Use Case 3: Local Customer Finds Sarah's Eugene, Oregon Store via "Candles Near Me" Search

**Scenario:** Emily is searching for a last-minute gift while visiting Eugene. She searches "candles near me" on her phone. Local SEO optimization enables CandleStore to appear in Google Maps results and attract foot traffic.

**Without This Feature:**
Emily searches "candles near me" on Google Maps:
- Results show Target, HomeGoods, Bath & Body Works (big-box retailers)
- CandleStore doesn't appear because:
  - No Google Business Profile claimed
  - No "candles" category selected in profile
  - Address not consistent across web (footer says "123 Main St," Google thinks it's "123 Main Street" - NAP inconsistency)
  - No local keywords on website ("Eugene," "Oregon," "local")
  - No local schema markup
- Emily buys generic candle from Target for $15. Sarah loses $25 sale + repeat customer opportunity.

**With This Feature:**
1. Sarah has optimized Google Business Profile:
   - **Business Name:** "CandleStore - Handmade Candles Eugene"
   - **Categories:** Primary: "Candle Store," Secondary: "Gift Shop," "Home Goods Store"
   - **Address:** "123 Main St, Eugene, OR 97401" (consistent with website footer, citations)
   - **Hours:** Mon-Sat 10am-6pm, Sun 12pm-5pm
   - **Photos:** 15 high-quality photos (storefront, interior, product close-ups, Sarah making candles)
   - **Attributes:** "Women-owned," "Small business," "Local pickup available," "Online ordering available"
   - **Description:** "Handmade soy candles crafted in Eugene, Oregon using natural ingredients. Shop local for unique scents and eco-friendly home fragrances."
2. Emily searches "candles near me" on Google Maps
3. **Map Pack Results (Top 3):**
   ```
   1. CandleStore - Handmade Candles Eugene ★★★★★ 4.9 (68 reviews)
      Candle store · 0.3 mi · Open until 6 PM
      "Beautiful handmade candles, Sarah is so knowledgeable!"
      [Website] [Directions] [Call]

   2. Target ★★★★☆ 4.1 (1,243 reviews)
      Department store · 1.2 mi

   3. HomeGoods ★★★★☆ 4.3 (876 reviews)
      Home goods store · 1.8 mi
   ```
4. CandleStore ranks #1 in local pack because:
   - **Proximity:** 0.3 mi from Emily (closest candle-specific store)
   - **Relevance:** "Candle Store" in business name, candle category, keyword-rich description
   - **Prominence:** 4.9★ rating, 68 reviews, complete profile with photos
5. Emily clicks "Directions," drives to store
6. Website has `/locations/eugene-oregon` page with:
   - Embedded Google Map
   - Store hours, parking info
   - "What to Expect" section (describe in-store experience)
   - Local schema markup:
     ```json
     {
       "@type": "LocalBusiness",
       "name": "CandleStore",
       "address": {
         "@type": "PostalAddress",
         "streetAddress": "123 Main St",
         "addressLocality": "Eugene",
         "addressRegion": "OR",
         "postalCode": "97401",
         "addressCountry": "US"
       },
       "geo": {
         "@type": "GeoCoordinates",
         "latitude": 44.0521,
         "longitude": -123.0868
       },
       "openingHours": "Mo-Sa 10:00-18:00, Su 12:00-17:00"
     }
     ```
7. Emily visits store, browses products, talks to Sarah about gift ideas
8. Purchases 2 candles ($50 total) + signs up for email list
9. Leaves 5-star Google review: "Perfect last-minute gift! Sarah helped me find the perfect lavender candle for my mom. Beautiful packaging and great customer service."
10. Emily's review boosts local SEO rankings further (69 reviews now, fresh review signals active business)

**Outcome (12 Months Post-Implementation):**
- **Google Business Profile Impressions:** 2,400/month (people seeing CandleStore in Maps results)
- **Profile Actions:** 180/month clicks on "Directions," "Website," "Call" buttons
- **In-Store Visits from Maps:** ~45/month (25% conversion from directions clicks)
- **Average In-Store Purchase:** $42 (higher than online due to Sarah's personal recommendations, upsells)
- **Monthly Local Revenue:** 45 visits × $42 = $1,890/month
- **Google Reviews Growth:** 68 → 142 reviews (1-2 new reviews/week from in-store customers)
- **Rating Improvement:** 4.7★ → 4.9★ (focused on customer service after negative review taught lesson)
- **Foot Traffic Growth:** 30% increase in walk-in customers (Maps is discovery channel)
- **Local Press:** Eugene Weekly newspaper feature "5 Local Women-Owned Businesses to Support" includes CandleStore (earned media from strong local presence)
- **Workshop Attendance:** Sarah starts hosting monthly candle-making workshops advertised on Google Business Profile Events (15 attendees × $65/ticket = $975/month additional revenue)

**Long-Term Local SEO Benefits:**
- **Neighborhood Discovery:** People searching "gift shops in Whitaker Eugene" or "things to do in Eugene Oregon" discover CandleStore
- **Voice Search Optimization:** "Hey Google, find handmade candles near me" → CandleStore appears due to local schema + Google Business Profile
- **Repeat Customers:** Local customers become loyal regulars, 40% return within 60 days vs 18% for online-only customers
- **Community Integration:** Sarah partners with local coffee shops, yoga studios to cross-promote (candle display at yoga studio, reciprocal Google Business Profile posts)

---

## 3. User Manual Documentation

### Overview

The SEO Optimization feature implements technical and on-page search engine optimization to make CandleStore discoverable in Google search results. This includes automated meta tag generation, structured data markup, XML sitemap creation, URL slug optimization, and content guidelines to rank for candle-related keywords and drive organic traffic without paid advertising.

**When to Use This Feature:**

- **For Store Owners:** Reduce customer acquisition costs from $25 (paid ads) to $0 (organic search), build long-term traffic channel that compounds over time, establish brand authority in candle niche
- **For Content Creators:** Optimize product descriptions, category pages, and blog posts to rank for target keywords and attract high-intent buyers
- **For Developers:** Implement technical SEO fundamentals (meta tags, structured data, sitemaps) following Google best practices, measure SEO performance via Google Search Console integration

**Key Benefits:**

- **Free Traffic:** Organic search traffic has zero marginal cost after initial optimization
- **Higher Intent:** Organic search users convert 8-12% vs 5-8% for paid ad traffic
- **Compounding Returns:** Rankings improve over time as domain authority grows
- **Brand Building:** Educational content positions store as expert resource, not just transactional seller
- **Long-Term Value:** SEO continues generating traffic even if content creation pauses (unlike paid ads that stop immediately when budget runs out)

---

### Initial Setup

Follow these steps to configure SEO optimization for your CandleStore.

#### Step 1: Configure Base SEO Settings

Update `appsettings.json` with your site's base SEO configuration:

```json
{
  "Seo": {
    "SiteName": "CandleStore",
    "Domain": "https://candlestore.com",
    "DefaultTitle": "Handmade Soy Candles | CandleStore",
    "DefaultDescription": "Shop handmade soy candles made with natural ingredients. Hand-poured in Eugene, Oregon. 40-50 hour burn time. Free shipping over $50.",
    "TwitterHandle": "@candlestore",
    "FacebookAppId": "1234567890",
    "OrganizationName": "CandleStore LLC",
    "OrganizationLogo": "https://candlestore.com/images/logo.png",
    "ContactEmail": "hello@candlestore.com",
    "ContactPhone": "+1-541-555-1234",
    "StreetAddress": "123 Main St",
    "City": "Eugene",
    "State": "OR",
    "ZipCode": "97401",
    "Country": "US"
  }
}
```

**Field Explanations:**
- `SiteName`: Your brand name (used in title tags: "Product Name | SiteName")
- `Domain`: Full URL with HTTPS (used for canonical URLs, Open Graph tags)
- `DefaultTitle`: Fallback title if page-specific title not set (homepage, about page)
- `DefaultDescription`: Fallback meta description (keep under 160 characters)
- `TwitterHandle`/`FacebookAppId`: Social media metadata for share cards
- Organization fields: Used for Organization schema markup (appears in Google Knowledge Panel)

#### Step 2: Generate Google Search Console Account

1. Navigate to https://search.google.com/search-console
2. Click **"Add Property"**
3. Select **"URL Prefix"** property type
4. Enter your domain: `https://candlestore.com`
5. **Verify Ownership** via one of these methods:
   - **HTML File Upload:** Download `google123abc.html` file, upload to `wwwroot/` directory
   - **HTML Tag:** Add `<meta name="google-site-verification" content="abc123...">` to site `<head>`
   - **DNS TXT Record:** Add TXT record to domain registrar (GoDaddy, Namecheap, etc.)
6. Click **"Verify"**
7. **Submit Sitemap:**
   - After verification, navigate to **Sitemaps** section (left sidebar)
   - Enter sitemap URL: `https://candlestore.com/sitemap.xml`
   - Click **"Submit"**
8. Wait 2-3 days for Google to crawl sitemap

**Expected Result:**
- "Property verified" green checkmark
- Sitemap status: "Success" with X URLs discovered
- Coverage report starts populating within 48-72 hours

#### Step 3: Optimize robots.txt

Create or update `/wwwroot/robots.txt`:

```
User-agent: *
Allow: /
Disallow: /admin/
Disallow: /cart/
Disallow: /checkout/
Disallow: /account/

Sitemap: https://candlestore.com/sitemap.xml
```

**Rules Explained:**
- `Allow: /` - Allow all bots to crawl site by default
- `Disallow: /admin/` - Block admin panel (prevents Google indexing internal tools)
- `Disallow: /cart/` - Block cart pages (duplicate content, no search value)
- `Disallow: /checkout/` - Block checkout flow (private customer data)
- `Disallow: /account/` - Block customer account pages (private data)
- `Sitemap:` - Tell search engines where sitemap is located

**Verify robots.txt:**
1. Navigate to `https://candlestore.com/robots.txt` in browser
2. Verify rules display correctly
3. In Google Search Console → Settings → robots.txt Tester
4. Enter URL to test: `/products/lavender-dreams-candle`
5. Verify: "Allowed" (green checkmark)
6. Enter URL to test: `/admin/orders`
7. Verify: "Blocked" (red X)

#### Step 4: Configure Product URL Slugs

Product slugs are SEO-friendly URLs like `/products/lavender-dreams-candle` instead of `/products/47`.

**Admin Panel Workflow:**

1. Navigate to **Admin Panel → Products → Create New Product**
2. Enter product name: "Lavender Dreams Candle"
3. **Slug field auto-generates:** `lavender-dreams-candle`
   - Lowercase conversion
   - Spaces replaced with hyphens
   - Special characters removed
   - Unique validation (if "lavender-dreams-candle" exists, generates "lavender-dreams-candle-2")
4. **Manual Override (Optional):**
   - Edit slug to: `handmade-lavender-soy-candle` (include target keyword)
   - Click "Check Availability" button
   - System validates uniqueness
5. Fill in SEO metadata section:
   ```
   ┌─────────────────────────────────────────────────────┐
   │  SEO Metadata                                        │
   ├─────────────────────────────────────────────────────┤
   │  Meta Title (60 chars max):                         │
   │  [Lavender Dreams Candle - Handmade Soy | CandleS] │
   │  (52/60 chars) ✓                                    │
   │                                                      │
   │  Meta Description (160 chars max):                   │
   │  [Hand-poured lavender candle made with 100% soy   │
   │  wax and essential oils. Burns 40-50 hours.        │
   │  Perfect for meditation and relaxation.]            │
   │  (148/160 chars) ✓                                  │
   │                                                      │
   │  Focus Keyword:                                      │
   │  [lavender candle]                                   │
   │                                                      │
   │  [Generate Meta Tags from Description] ← AI Button  │
   └─────────────────────────────────────────────────────┘
   ```
6. Click **"Save Product"**
7. **Verify SEO Implementation:**
   - Navigate to `https://candlestore.com/products/lavender-dreams-candle`
   - Right-click → "View Page Source"
   - Verify meta tags present:
     ```html
     <title>Lavender Dreams Candle - Handmade Soy | CandleStore</title>
     <meta name="description" content="Hand-poured lavender candle made with 100% soy wax...">
     <link rel="canonical" href="https://candlestore.com/products/lavender-dreams-candle">
     ```

**Best Practices for Slugs:**
- **Include Target Keyword:** `lavender-candle` not just `dreams-candle`
- **Keep Concise:** 3-5 words ideal, avoid `handmade-lavender-soy-wax-aromatherapy-meditation-candle-for-relaxation` (too long)
- **Avoid Stop Words:** `lavender-candle` not `the-lavender-candle` (Google ignores "the," "a," "and")
- **Permanent URLs:** Once product is live, avoid changing slug (breaks backlinks, loses ranking)

#### Step 5: Write SEO-Optimized Product Descriptions

Product descriptions must be **300-500 words minimum** with keyword integration and storytelling.

**Template Structure:**

```
[Opening Hook - 1 sentence with primary keyword]
Hand-poured lavender candle made with 100% soy wax and pure essential oils, perfect for meditation, yoga, and relaxation.

[Story/Origin - 2-3 sentences]
Sarah crafts each Lavender Dreams Candle by hand in her Eugene, Oregon studio using locally-sourced ingredients. Inspired by Pacific Northwest lavender fields, this candle brings calming aromatherapy into your home.

[Features/Benefits - Bullet points]
• 100% natural soy wax (eco-friendly, burns clean)
• Pure lavender essential oil (no synthetic fragrances)
• 40-50 hour burn time (8oz glass jar)
• Lead-free cotton wick
• Hand-poured in small batches

[Scent Profile - 2-3 sentences with sensory keywords]
The scent opens with fresh lavender notes, followed by subtle hints of vanilla and bergamot. As the candle burns, the aroma fills your space with a calming, spa-like ambiance perfect for unwinding after a long day.

[Usage Suggestions - 2-3 sentences]
Light during evening meditation, place in your bedroom for better sleep, or use during bath time for a relaxing spa experience at home. Pairs beautifully with our Eucalyptus Mint Candle for a complete aromatherapy collection.

[Care Instructions - 2-3 sentences]
For best results, trim wick to 1/4 inch before each use and burn for 3-4 hours at a time to ensure even wax pool. Avoid burning in drafty areas. Never leave candle unattended.

[Closing CTA - 1 sentence]
Shop now and enjoy free shipping on orders over $50.
```

**Word Count:** 350-400 words (above 300-word threshold for Google)

**Keyword Integration:**
- Primary keyword ("lavender candle") appears 3-5 times naturally
- Secondary keywords ("soy candle," "aromatherapy candle," "meditation candle") appear 1-2 times each
- Avoid keyword stuffing: "lavender candle lavender candle lavender candle" (penalty)

**Forbidden Practices:**
- ❌ Copying manufacturer descriptions (duplicate content penalty)
- ❌ 2-sentence descriptions (thin content penalty)
- ❌ Keyword stuffing (Google penalty)
- ❌ Hidden text (white text on white background - black-hat SEO, severe penalty)

#### Step 6: Optimize Product Images for SEO

Images contribute to both on-page SEO (image alt text) and image search SEO (Google Images).

**Image Optimization Workflow:**

1. **Filename:** Before uploading, rename image file
   - ❌ Bad: `IMG_1234.jpg`, `DSC_5678.jpg`
   - ✅ Good: `lavender-dreams-candle-soy-wax.jpg`, `handmade-lavender-candle-lit.jpg`
2. **Upload to Admin Panel**
3. **Alt Text Field:**
   ```
   ┌─────────────────────────────────────────────────────┐
   │  Image Upload                                        │
   ├─────────────────────────────────────────────────────┤
   │  File: lavender-dreams-candle-soy-wax.jpg           │
   │                                                      │
   │  Alt Text (required for accessibility + SEO):        │
   │  [Lavender Dreams handmade soy candle in glass jar │
   │  with purple label, unlit, on white background]    │
   │                                                      │
   │  Title Text (optional, displays on hover):           │
   │  [Lavender Dreams Candle - $24.99]                  │
   └─────────────────────────────────────────────────────┘
   ```
4. **Compression:**
   - System auto-compresses to WebP format (50-70% size reduction)
   - Original JPEG: 2.4 MB → WebP: 180 KB
   - Faster load times improve Core Web Vitals (SEO ranking factor)
5. **Lazy Loading:**
   - Images below fold automatically get `loading="lazy"` attribute
   - Images load only when user scrolls (improves LCP metric)

**Alt Text Best Practices:**
- **Descriptive:** "Lavender candle in glass jar" not "candle" or "image1"
- **Include Keywords:** "handmade soy candle" naturally integrated
- **Avoid Keyword Stuffing:** Not "lavender candle soy candle handmade candle aromatherapy candle meditation candle" (spammy)
- **80-125 Characters:** Long enough for context, short enough for screen readers

**Image SEO Results:**
- Appears in Google Images search for "lavender candle," "soy candle," "handmade candle"
- Drives 5-10% of product page traffic via image search
- Improves accessibility (screen readers describe images to visually impaired users)

#### Step 7: Implement Structured Data (JSON-LD)

Structured data enables rich snippets in Google search results (star ratings, price, availability).

**Product Schema Example:**

Admin panel automatically generates JSON-LD for product pages. Verify implementation:

1. Navigate to product page: `https://candlestore.com/products/lavender-dreams-candle`
2. Right-click → "View Page Source"
3. Search for `<script type="application/ld+json">`
4. Verify JSON-LD present:
   ```json
   {
     "@context": "https://schema.org",
     "@type": "Product",
     "name": "Lavender Dreams Candle",
     "image": [
       "https://candlestore.com/images/lavender-dreams-1.jpg",
       "https://candlestore.com/images/lavender-dreams-2.jpg"
     ],
     "description": "Hand-poured lavender candle made with 100% soy wax...",
     "sku": "CAN-LAV-001",
     "brand": {
       "@type": "Brand",
       "name": "CandleStore"
     },
     "offers": {
       "@type": "Offer",
       "url": "https://candlestore.com/products/lavender-dreams-candle",
       "priceCurrency": "USD",
       "price": "24.99",
       "priceValidUntil": "2025-12-31",
       "availability": "https://schema.org/InStock",
       "itemCondition": "https://schema.org/NewCondition"
     },
     "aggregateRating": {
       "@type": "AggregateRating",
       "ratingValue": "4.8",
       "reviewCount": "47"
     }
   }
   ```
5. **Validate Structured Data:**
   - Navigate to https://search.google.com/test/rich-results
   - Enter URL: `https://candlestore.com/products/lavender-dreams-candle`
   - Click **"Test URL"**
   - Verify: "Rich results can be displayed" (green checkmark)
   - Preview how product appears in search results

**Expected Rich Snippet in Google:**
```
Lavender Dreams Candle - Handmade Soy | CandleStore
candlestore.com › products › lavender-dreams-candle
★★★★★ 4.8 (47 reviews) · $24.99 · In stock
Hand-poured lavender candle made with 100% soy wax and
essential oils. Burns 40-50 hours. Perfect for...
```

**Organization Schema (Homepage):**

Automatically generated for homepage, appears in Google Knowledge Panel:

```json
{
  "@context": "https://schema.org",
  "@type": "Organization",
  "name": "CandleStore",
  "url": "https://candlestore.com",
  "logo": "https://candlestore.com/images/logo.png",
  "contactPoint": {
    "@type": "ContactPoint",
    "telephone": "+1-541-555-1234",
    "contactType": "Customer Service",
    "email": "hello@candlestore.com",
    "availableLanguage": "English"
  },
  "sameAs": [
    "https://facebook.com/candlestore",
    "https://instagram.com/candlestore",
    "https://twitter.com/candlestore"
  ],
  "address": {
    "@type": "PostalAddress",
    "streetAddress": "123 Main St",
    "addressLocality": "Eugene",
    "addressRegion": "OR",
    "postalCode": "97401",
    "addressCountry": "US"
  }
}
```

#### Step 8: XML Sitemap Configuration

Sitemap auto-generates nightly at 2 AM UTC, listing all published products/categories/blog posts.

**Access Sitemap:**

1. Navigate to `https://candlestore.com/sitemap.xml` in browser
2. Verify XML structure:
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
     <url>
       <loc>https://candlestore.com/</loc>
       <lastmod>2025-11-08</lastmod>
       <changefreq>daily</changefreq>
       <priority>1.0</priority>
     </url>
     <url>
       <loc>https://candlestore.com/products/lavender-dreams-candle</loc>
       <lastmod>2025-11-07</lastmod>
       <changefreq>weekly</changefreq>
       <priority>0.8</priority>
     </url>
     <!-- 47 more product URLs -->
     <url>
       <loc>https://candlestore.com/categories/soy-candles</loc>
       <lastmod>2025-11-06</lastmod>
       <changefreq>weekly</changefreq>
       <priority>0.6</priority>
     </url>
     <!-- Category and blog URLs -->
   </urlset>
   ```

**Sitemap Rules:**
- **Priority:**
  - Homepage: 1.0
  - Product pages: 0.8
  - Category pages: 0.6
  - Blog posts: 0.5
- **Change Frequency:**
  - Homepage: Daily (updates with new products)
  - Products: Weekly (price/availability changes)
  - Categories: Weekly (products added/removed)
  - Blog: Monthly (static content)
- **Last Modified:** Auto-updated from database `UpdatedAt` timestamp

**Submit Sitemap to Search Engines:**

1. **Google Search Console:** (already done in Step 2)
2. **Bing Webmaster Tools:**
   - Navigate to https://www.bing.com/webmasters
   - Add site, verify ownership
   - Navigate to Sitemaps → Submit sitemap URL: `https://candlestore.com/sitemap.xml`

**Sitemap Auto-Regeneration:**

Sitemap regenerates automatically when:
- New product published
- Product unpublished/deleted
- Category created/updated
- Blog post published

Or manually via Admin Panel:
- Navigate to **Admin Panel → SEO → Sitemap**
- Click **"Regenerate Sitemap Now"**
- Verify last generated timestamp updates

---

### Content Optimization Guidelines

Follow these guidelines when creating SEO-optimized content.

#### Keyword Research Process

Before writing product descriptions or blog posts, identify target keywords.

1. **Brainstorm Seed Keywords:**
   - Primary product keywords: "lavender candle," "soy candle," "scented candle"
   - Product attribute keywords: "handmade candle," "natural candle," "aromatherapy candle"
   - Use case keywords: "meditation candle," "relaxation candle," "spa candle"
2. **Use Google Autocomplete:**
   - Type "lavender candle" into Google search box
   - Note suggested completions:
     - "lavender candle benefits"
     - "lavender candle for sleep"
     - "lavender candle near me"
   - These are real queries people search, target them in content
3. **Check "People Also Ask" and "Related Searches":**
   - Search "lavender candle" on Google
   - Scroll to "People also ask" box:
     - "What does lavender candle do?"
     - "Are lavender candles good for anxiety?"
     - "Do lavender candles help you sleep?"
   - These are FAQ opportunities, answer in product description or blog post
4. **Analyze Competitors:**
   - Search target keyword: "lavender soy candle"
   - Open top 3-5 results
   - Note keywords they target, content structure, word count
   - Identify gaps (topics they don't cover, questions they don't answer)
5. **Free Keyword Tools:**
   - **Google Keyword Planner** (free with Google Ads account)
   - **Ubersuggest** (10 free searches/day)
   - **AnswerThePublic** (visualizes question-based queries)

**Keyword Selection Criteria:**
- **Search Volume:** 100+ monthly searches (low-competition keywords)
- **Commercial Intent:** Transactional keywords ("buy lavender candle") over informational ("what is lavender")
- **Competition:** Target keywords where top 10 results are blogs/small businesses, not Amazon/Walmart
- **Relevance:** Keyword matches your products (don't target "lavender essential oil" if you only sell candles)

#### Product Page Optimization Checklist

Use this checklist for every product page:

- [ ] **URL Slug:** Clean, keyword-rich slug (`/products/lavender-dreams-candle`)
- [ ] **Meta Title:** 50-60 characters, includes primary keyword + brand name
  - ✅ Example: "Lavender Dreams Candle - Handmade Soy | CandleStore" (52 chars)
- [ ] **Meta Description:** 150-160 characters, compelling copy with keyword + CTA
  - ✅ Example: "Hand-poured lavender candle with 100% soy wax. Burns 40-50 hours. Perfect for meditation. Free shipping over $50. Shop now!" (148 chars)
- [ ] **H1 Heading:** Product name with primary keyword (1 H1 per page)
  - ✅ Example: "Lavender Dreams Candle - Handmade Soy Candle for Relaxation"
- [ ] **Product Description:** 300-500 words minimum
- [ ] **Heading Structure:** H2 for sections ("Scent Profile," "Ingredients," "Burn Time")
- [ ] **Primary Keyword:** Appears in first 100 words of description
- [ ] **Keyword Density:** 1-2% (if description is 400 words, keyword appears 4-8 times naturally)
- [ ] **Images:** 3-5 high-quality photos with descriptive filenames + alt text
- [ ] **Internal Links:** Link to related products, categories, blog posts (3-5 links)
- [ ] **Structured Data:** Product schema with price, availability, ratings
- [ ] **Canonical URL:** Points to this page (prevents duplicate content)
- [ ] **Mobile Optimization:** Responsive design, readable on phone (test with mobile preview)

#### Category Page Optimization

Category pages should be content-rich landing pages, not just product grids.

**Structure:**

```
[H1: Category Name + Keyword]
Soy Candles - Handmade & Natural

[Opening Paragraph - 100-150 words]
Discover our collection of handmade soy candles made with 100% natural soy wax and essential oils. Each candle is hand-poured in small batches in Eugene, Oregon...

[H2: Why Choose Soy Candles?]
Soy candles burn cleaner than paraffin candles, producing minimal soot. They're biodegradable, made from renewable resources, and provide 30-50% longer burn times...

[H2: Our Soy Candle Collection]
Browse our full range of soy candles in various scents and sizes...

[Product Grid - 12-24 products]

[H2: Soy Candle Buying Guide]
How to choose the right soy candle:
• Size: 4oz travel tins for small spaces, 8oz jars for bedrooms, 16oz for living rooms
• Scent strength: Light/medium/strong based on room size
• Burn time: Expect 40-50 hours for 8oz candles...

[H2: Frequently Asked Questions]
Q: How long do soy candles last?
A: Our 8oz soy candles burn for 40-50 hours...

[Internal Links: Related Categories]
Shop more: [Lavender Candles] [Aromatherapy Candles] [Gift Sets]
```

**Word Count:** 500-800 words (excluding product descriptions)

**SEO Benefits:**
- Ranks for category keyword ("soy candles") + long-tail variants ("handmade soy candles," "natural soy candles")
- Buying guide answers pre-purchase questions (reduces bounce rate, improves dwell time)
- FAQ schema markup enables "People also ask" rich snippets
- Internal links distribute page authority across site

#### Blog Post Optimization

Blog posts target informational keywords (top-of-funnel traffic).

**Blog Post Template:**

```
[H1: Question-Based Title with Keyword]
How to Make Candles Last Longer: 10 Expert Tips

[Opening - Problem/Promise - 2-3 sentences]
Are your candles burning unevenly or running out too quickly? Learn how to extend your candle's burn time by 50% with these expert tips from a professional candle maker.

[H2: Tip #1 - Trim the Wick]
Always trim your wick to 1/4 inch before lighting. Long wicks cause uneven burning, soot, and wasted wax.

[Internal Product Link]
Our [Wick Trimmer Tool - $8.99] makes this easy with precise measurements.

[H2: Tip #2 - Burn for 3-4 Hours]
Let the wax pool reach the edges of the jar on the first burn. This prevents tunneling and ensures even melting throughout the candle's life.

[Continue for all 10 tips...]

[H2: Conclusion + CTA]
Follow these tips to get the most out of your candles. Shop our [Candle Care Kit] for all the tools you need.

[Related Posts]
• Best Candles for Meditation
• Soy vs Paraffin: The Complete Guide
• Candle Safety: Do's and Don'ts
```

**Blog SEO Checklist:**
- [ ] **Title:** Question-based (matches voice search queries)
- [ ] **URL Slug:** `/blog/how-to-make-candles-last-longer` (keyword-rich)
- [ ] **Meta Description:** Summarizes post value + CTA
- [ ] **Word Count:** 600-1200 words (comprehensive but scannable)
- [ ] **Heading Hierarchy:** H1 (title) → H2 (main points) → H3 (sub-points)
- [ ] **Internal Links:** 3-5 links to products/categories (drives conversions)
- [ ] **External Links:** 1-2 links to authoritative sources (builds trust, not competing stores)
- [ ] **Images:** 2-4 images with alt text (breaks up text, improves engagement)
- [ ] **FAQ Schema:** If post answers questions, add FAQ structured data
- [ ] **Publish Date:** Displayed (freshness is ranking factor)
- [ ] **Author Bio:** "Written by Sarah, founder of CandleStore" (E-A-T signal)

**Publishing Frequency:**
- **Month 1-3:** 1 post per week (build content foundation)
- **Month 4-6:** 2 posts per month (maintain momentum)
- **Month 7+:** 1 post per month (maintenance mode, update existing posts)

---

### Measuring SEO Performance

Track SEO metrics to measure ROI and identify optimization opportunities.

#### Google Search Console Metrics

Access Google Search Console: https://search.google.com/search-console

**Key Reports:**

1. **Performance Report** (Overview → Performance)
   - **Total Clicks:** Number of clicks from Google search (target: 300-500/month after 6 months)
   - **Total Impressions:** How many times your site appeared in search results (target: 10,000-20,000/month)
   - **Average CTR:** Clicks ÷ Impressions (target: 3-5%, industry average: 2-3%)
   - **Average Position:** Average ranking position (target: 10-20 within 6 months, top 10 for priority keywords)

2. **Queries Report** (Performance → Queries tab)
   - Shows which keywords drive traffic
   - Sort by **Impressions** (high-volume keywords you're appearing for)
   - Sort by **Position** (find keywords ranking #11-20, optimize to reach page 1)
   - **Actionable Insight:** If "lavender candle" shows 1,200 impressions, position 12 → optimize product page to reach top 10

3. **Pages Report** (Performance → Pages tab)
   - Shows which pages get most clicks/impressions
   - Identify top performers (double down on what works)
   - Identify underperformers (high impressions, low CTR → improve meta description)

4. **Coverage Report** (Index → Coverage)
   - **Valid:** Pages successfully indexed (target: 90%+ of submitted URLs)
   - **Excluded:** Pages not indexed (check why - robots.txt blocked, duplicate content, etc.)
   - **Errors:** Pages Google tried to index but failed (fix immediately)

**Monthly SEO Report Template:**

```
Month: November 2025

Organic Traffic:
• Total Clicks: 412 (+37% vs October)
• Total Impressions: 14,230 (+22% vs October)
• Average CTR: 2.9% (vs 2.6% last month)
• Average Position: 18.3 (vs 24.1 last month)

Top Keywords:
1. "handmade candles oregon" - Position 5 (87 clicks/month)
2. "lavender candle" - Position 12 (34 clicks/month)
3. "soy candles" - Position 15 (28 clicks/month)

Top Pages:
1. /products/lavender-dreams-candle - 128 clicks
2. /categories/soy-candles - 89 clicks
3. /blog/how-to-make-candles-last-longer - 54 clicks

Actions Taken:
• Optimized meta description for "Lavender Dreams Candle" (CTR improved from 2.1% → 3.8%)
• Added FAQ schema to blog post (now appears in "People also ask")
• Published new blog post: "Best Candles for Meditation"

Next Month Goals:
• Push "lavender candle" from position 12 → top 10 (add more internal links, improve content)
• Publish 2 new blog posts targeting long-tail keywords
• Acquire 3 backlinks from lifestyle blogs
```

#### Google Analytics SEO Metrics (Task 027 Integration)

**Organic Traffic Segment:**

1. Navigate to Google Analytics → Acquisition → All Traffic → Source/Medium
2. Click on "google / organic" row
3. View metrics:
   - **Sessions:** 450/month (organic search visits)
   - **Bounce Rate:** 45% (target: <60%)
   - **Pages/Session:** 3.2 (engagement level)
   - **Avg Session Duration:** 2:45 (time on site)
   - **Goal Completions:** 54 purchases (organic conversions)
   - **Conversion Rate:** 12% (54 ÷ 450)

**Organic Traffic Value:**

```
Organic Traffic Value = Organic Sessions × Conversion Rate × Average Order Value

Example:
450 sessions/month × 12% conversion rate × $45 AOV = $2,430/month organic revenue

Compare to Paid Ads:
$1,500/month ad spend → 200 sessions → 5% conversion rate × $42 AOV = $420 revenue
ROI: -72% (losing money on paid ads)

vs SEO:
$0/month spend → 450 sessions → 12% conversion rate × $45 AOV = $2,430 revenue
ROI: Infinite (free traffic)
```

**Landing Page Performance:**

1. Google Analytics → Behavior → Site Content → Landing Pages
2. Apply segment filter: "Organic Traffic"
3. See which pages drive most organic sessions:
   - `/products/lavender-dreams-candle` - 128 sessions, 15% conversion rate
   - `/categories/soy-candles` - 89 sessions, 10% conversion rate
   - `/blog/how-to-make-candles-last-longer` - 54 sessions, 8% conversion rate (blog converts indirectly via internal links)

---

### Troubleshooting

#### Problem: Product Pages Not Appearing in Google Search

**Symptoms:**
- Published products 2+ weeks ago
- Not showing in Google Search Console Coverage report
- Searching "site:candlestore.com lavender candle" returns no results

**Solution:**

1. **Check robots.txt:**
   - Navigate to `https://candlestore.com/robots.txt`
   - Verify `/products/` not listed in `Disallow` rules
   - If accidentally blocked, remove `Disallow: /products/` line
2. **Check Product Published Status:**
   - Admin Panel → Products → Check "Published" checkbox is checked
   - Unpublished products excluded from sitemap
3. **Manual Index Request:**
   - Google Search Console → URL Inspection
   - Enter URL: `https://candlestore.com/products/lavender-dreams-candle`
   - Click "Request Indexing"
   - Google crawls within 24-48 hours
4. **Verify Sitemap Includes Product:**
   - Navigate to `https://candlestore.com/sitemap.xml`
   - Search (Ctrl+F) for product slug: "lavender-dreams-candle"
   - If missing, regenerate sitemap: Admin Panel → SEO → Regenerate Sitemap

---

#### Problem: Meta Description Not Displaying in Search Results

**Symptoms:**
- Wrote compelling meta description
- Google search results show different text (snippet from page body)

**Solution:**

Google sometimes ignores meta descriptions if it thinks page content is more relevant to query.

1. **Check Meta Description Quality:**
   - Too short (<50 chars) or too long (>160 chars)?
   - Contains keyword user searched for?
   - Accurately describes page content?
2. **Verify Meta Tag Syntax:**
   - View page source: `<meta name="description" content="Your description">`
   - No syntax errors (missing quotes, unclosed tags)
3. **Wait 2-4 Weeks:**
   - Google sometimes tests different snippets
   - If your meta is better, Google will use it over time
4. **Check for Duplicate Meta Descriptions:**
   - Google Search Console → Enhancements → Check for "Duplicate meta descriptions" warning
   - Each page needs unique description

---

#### Problem: Low Click-Through Rate (CTR) Despite Good Rankings

**Symptoms:**
- Product ranking position 5-8 (page 1)
- 1,000+ impressions/month
- CTR only 1.2% (should be 3-5% for page 1)

**Solution:**

1. **Improve Meta Title:**
   - Add power words: "Best," "Top-Rated," "Premium," "Handmade"
   - Include price if competitive: "Lavender Candle - $24.99"
   - Add year for freshness: "Best Lavender Candles 2025"
   - Example improvement:
     - ❌ "Lavender Dreams Candle | CandleStore" (boring)
     - ✅ "Premium Lavender Candle - Handmade Soy, 50Hr Burn | $24.99" (compelling)
2. **Optimize Meta Description:**
   - Lead with benefit: "Get better sleep with our lavender candle..."
   - Include social proof: "4.8★ rated by 47 customers"
   - Add urgency: "Free shipping over $50 - Order today!"
   - Include CTA: "Shop now"
3. **Implement Rich Snippets:**
   - Add Product schema (star ratings visible in search)
   - Star ratings increase CTR by 20-30%
4. **Match Search Intent:**
   - If ranking for "lavender candle for sleep" but meta mentions "meditation," user won't click
   - Tailor meta to match keyword intent

---

#### Problem: Slow Page Load Times (Poor Core Web Vitals)

**Symptoms:**
- Google Search Console → Core Web Vitals shows "Needs Improvement" or "Poor"
- LCP (Largest Contentful Paint) >2.5 seconds
- CLS (Cumulative Layout Shift) >0.1

**Solution:**

1. **Optimize Images:**
   - Compress images: Use WebP format (70% smaller than JPEG)
   - Resize images: Don't upload 4000×3000px images for 400px display
   - Lazy load below-fold images: `<img loading="lazy">`
2. **Enable CDN (Task 028):**
   - Cloudflare serves static assets from edge locations
   - Reduces latency from 500ms (Oregon server) to 50ms (local edge)
3. **Minify CSS/JS:**
   - ASP.NET Core bundling minifies files in production mode
   - Verify `ASPNETCORE_ENVIRONMENT=Production` environment variable set
4. **Reduce Server Response Time:**
   - Database query optimization (add indexes, avoid N+1 queries)
   - Enable response caching for product pages
5. **Fix Cumulative Layout Shift:**
   - Specify image dimensions: `<img width="400" height="300">`
   - Reserve space for ads, embeds (avoid content jumping as page loads)

**Verify Improvements:**
- Use PageSpeed Insights: https://pagespeed.web.dev/
- Enter URL: `https://candlestore.com/products/lavender-dreams-candle`
- Check scores:
  - Performance: >90 (green)
  - LCP: <2.5s
  - FID: <100ms
  - CLS: <0.1

---

## 4. Acceptance Criteria / Definition of Done

### Core Functionality - Technical SEO

- [ ] Meta title tag auto-generated for all pages (homepage, products, categories, blog posts)
- [ ] Meta title length validation: 50-60 characters optimal, warning if >60 chars (truncated in search results)
- [ ] Meta description auto-generated for all pages with 150-160 character length
- [ ] Meta description validation: warning if <120 chars (too short) or >160 chars (truncated)
- [ ] Canonical URL tags implemented on all pages pointing to primary URL
- [ ] Canonical tags prevent duplicate content issues (product accessible via category → canonical to `/products/{slug}`)
- [ ] Open Graph tags for social sharing (og:title, og:description, og:image, og:url)
- [ ] Twitter Card meta tags for Twitter sharing (twitter:card, twitter:title, twitter:description, twitter:image)
- [ ] Robots meta tag configurable per page (index/noindex, follow/nofollow)
- [ ] Admin panel option to noindex specific pages (test pages, coming soon pages)
- [ ] HTTPS enforced across entire site (HTTP requests redirect to HTTPS)
- [ ] SSL certificate valid and not expired
- [ ] Mixed content warnings resolved (no HTTP resources on HTTPS pages)
- [ ] Sitemap.xml auto-generated nightly at 2 AM UTC
- [ ] Sitemap includes all published products with lastmod, changefreq, priority
- [ ] Sitemap includes all categories with appropriate priority (0.6)
- [ ] Sitemap includes all blog posts with publish/update timestamps
- [ ] Sitemap excludes unpublished products, admin pages, cart, checkout
- [ ] Sitemap submitted to Google Search Console successfully
- [ ] Robots.txt file blocks admin panel, cart, checkout, account pages
- [ ] Robots.txt allows crawling of products, categories, blog, homepage
- [ ] Robots.txt includes sitemap location (Sitemap: https://candlestore.com/sitemap.xml)
- [ ] 404 error pages have custom design with search box and popular products
- [ ] 404 pages return proper HTTP 404 status code (not 200 OK)
- [ ] Redirects implemented for changed URLs (301 permanent redirect)
- [ ] Breadcrumb navigation displayed on product and category pages
- [ ] Breadcrumb structured data (BreadcrumbList schema) implemented
- [ ] Mobile-friendly design passes Google Mobile-Friendly Test
- [ ] Viewport meta tag configured for responsive design
- [ ] Page speed optimization: LCP <2.5 seconds for 75th percentile
- [ ] Page speed optimization: FID <100ms for 75th percentile
- [ ] Page speed optimization: CLS <0.1 for 75th percentile
- [ ] Images use lazy loading (loading="lazy" attribute) for below-fold images
- [ ] Images compressed to WebP format with 70-90% quality
- [ ] Responsive images with srcset attribute (serve different sizes for mobile/desktop)
- [ ] HTML semantic structure: <header>, <nav>, <main>, <article>, <footer> tags
- [ ] Heading hierarchy validated: Only one H1 per page, H2/H3 in logical order
- [ ] No empty headings (H1/H2/H3 without text content)
- [ ] Alt text required for all images (validation error if missing)
- [ ] Pagination implemented with rel="prev" and rel="next" tags for category pages
- [ ] Duplicate content detection: Alert admin if multiple products have identical descriptions

### Core Functionality - URL Structure

- [ ] Product URLs use SEO-friendly slugs: `/products/lavender-dreams-candle` not `/products/47`
- [ ] Category URLs use slugs: `/categories/soy-candles` not `/categories?id=5`
- [ ] Blog post URLs use slugs: `/blog/how-to-make-candles-last-longer` not `/blog/123`
- [ ] URL slug auto-generation from product/category/blog post names
- [ ] Slug transformation: Lowercase, spaces to hyphens, special chars removed
- [ ] Slug uniqueness validation: "lavender-dreams-candle-2" if duplicate exists
- [ ] Manual slug override option in admin panel
- [ ] Slug availability checker: Real-time validation as admin types
- [ ] URL structure hierarchy: `/categories/soy-candles/lavender-candles` for subcategories
- [ ] Clean URLs without query parameters for SEO-critical pages
- [ ] Trailing slash consistency: All URLs either have or don't have trailing slashes (not mixed)
- [ ] Lowercase URL enforcement: `/Products/Lavender` redirects to `/products/lavender`
- [ ] URL length validation: Warning if >75 characters (Google truncates in search results)
- [ ] No underscores in URLs (use hyphens: `soy-candles` not `soy_candles`)
- [ ] Date not included in blog post URLs (allows updating old posts without URL change)

### Core Functionality - Structured Data (Schema Markup)

- [ ] Product schema implemented with all required fields (name, image, description, offers)
- [ ] Product schema includes price, priceCurrency, availability
- [ ] Product schema includes aggregateRating if product has reviews
- [ ] Product schema includes brand (Organization or Brand type)
- [ ] Product schema includes SKU and GTIN (if available)
- [ ] Organization schema on homepage with name, logo, contactPoint, address
- [ ] Organization schema includes sameAs links to social media profiles
- [ ] LocalBusiness schema if physical store location exists
- [ ] BreadcrumbList schema on product and category pages
- [ ] FAQ schema on FAQ pages and blog posts with Q&A format
- [ ] Article schema on blog posts with author, datePublished, dateModified
- [ ] Review schema for individual product reviews
- [ ] AggregateRating schema for overall product rating
- [ ] Offer schema with price, currency, availability, seller
- [ ] WebSite schema on homepage with searchAction (enables sitelinks search box)
- [ ] JSON-LD format used (not microdata or RDFa) for all structured data
- [ ] Structured data validation via Google Rich Results Test passes without errors
- [ ] Schema updates automatically when product price/availability changes
- [ ] Multiple images included in Product schema (3-5 product photos)
- [ ] Schema markup doesn't duplicate visible content (markup reflects what user sees)

### Core Functionality - On-Page SEO

- [ ] H1 tag present on every page (only one H1 per page)
- [ ] H1 contains primary keyword (product name for product pages)
- [ ] H2 tags used for section headers (Description, Ingredients, Reviews)
- [ ] H3 tags used for subsections within H2 sections
- [ ] Heading hierarchy validated (no H3 before H2, no H4 before H3)
- [ ] Primary keyword appears in first 100 words of product description
- [ ] Primary keyword appears in meta title, H1, at least one H2
- [ ] Keyword density 1-2% (natural integration, not stuffing)
- [ ] LSI keywords (semantically related keywords) included in content
- [ ] Product descriptions minimum 300 words (enforced in admin panel)
- [ ] Category pages minimum 500 words of unique content
- [ ] Blog posts minimum 600 words (recommended 800-1200 for competitive keywords)
- [ ] Content readability: Flesch Reading Ease score >60 (8th grade level)
- [ ] Short paragraphs: 2-4 sentences per paragraph for scannability
- [ ] Bullet points used to break up text and improve readability
- [ ] Internal linking: 3-5 contextual links to related products/categories per page
- [ ] Anchor text variation for internal links (not always "click here")
- [ ] External links to authoritative sources (if citing statistics, studies)
- [ ] External links open in new tab (target="_blank" rel="noopener noreferrer")
- [ ] Image alt text descriptive and keyword-rich (80-125 characters)
- [ ] Image filenames descriptive before upload (lavender-candle.jpg not IMG1234.jpg)
- [ ] Image title attribute (optional, displays on hover)
- [ ] Videos have transcripts for accessibility and SEO
- [ ] Bold/strong tags used to emphasize important keywords
- [ ] Italic/em tags used for product benefits and features
- [ ] No keyword stuffing (manual review flags pages with >3% keyword density)
- [ ] No hidden text (white text on white background, text sized at 0px)
- [ ] No cloaking (showing different content to Google vs users)
- [ ] Content uniqueness: No duplicate content across products (plagiarism check)
- [ ] Manufacturer descriptions rewritten in unique voice (not copied from supplier)

### Core Functionality - Image Optimization

- [ ] All product images compressed before upload
- [ ] WebP format used for all images (with JPEG fallback for old browsers)
- [ ] Image compression quality 70-90% (balance between size and quality)
- [ ] Image dimensions optimized: No 4000×3000px images displayed at 400×300px
- [ ] Responsive images with srcset attribute serving different sizes
- [ ] Image lazy loading enabled for images below fold
- [ ] Critical above-fold images preloaded (rel="preload")
- [ ] Alt text required for all images (enforced in admin panel upload flow)
- [ ] Alt text describes image content for visually impaired users
- [ ] Alt text includes primary keyword naturally (not stuffed)
- [ ] Image title attribute optional but recommended
- [ ] Image filenames use hyphens not underscores (lavender-candle.jpg not lavender_candle.jpg)
- [ ] Image filenames descriptive before upload (enforced in admin panel)
- [ ] Image CDN integration (Task 028) for faster global delivery
- [ ] Image dimensions specified in HTML (width/height attributes to prevent CLS)
- [ ] Image aspect ratio maintained to prevent layout shift
- [ ] Thumbnail images link to full-size images or product pages
- [ ] Product image gallery includes 3-5 high-quality photos
- [ ] Images include lifestyle photos (candle in use) not just product shots
- [ ] Image file size <500KB for hero images, <200KB for thumbnails

### Core Functionality - Sitemap and Indexing

- [ ] XML sitemap auto-generated from database every 24 hours
- [ ] Sitemap includes all published products (Product.IsPublished == true)
- [ ] Sitemap excludes unpublished/draft products
- [ ] Sitemap includes categories with products (excludes empty categories)
- [ ] Sitemap includes blog posts sorted by publish date (newest first)
- [ ] Sitemap lastmod field updates when product/category/blog post edited
- [ ] Sitemap changefreq accurate: daily for homepage, weekly for products, monthly for blog
- [ ] Sitemap priority values: 1.0 homepage, 0.8 products, 0.6 categories, 0.5 blog
- [ ] Sitemap file size <50MB (split into multiple sitemaps if >50,000 URLs)
- [ ] Sitemap index file created if multiple sitemaps needed
- [ ] Sitemap gzipped for faster download (sitemap.xml.gz)
- [ ] Sitemap accessible at /sitemap.xml (no 404 error)
- [ ] Sitemap returns correct Content-Type header: application/xml
- [ ] Sitemap validated via Google Search Console (no errors)
- [ ] Sitemap submitted to Google Search Console successfully
- [ ] Sitemap submitted to Bing Webmaster Tools successfully
- [ ] Manual sitemap regeneration available in admin panel
- [ ] Sitemap regeneration logged (timestamp, URL count)
- [ ] Google Search Console integration tracks indexing status
- [ ] Coverage report monitored: 90%+ URLs indexed successfully
- [ ] Index coverage errors fixed within 48 hours of detection

### Core Functionality - robots.txt Configuration

- [ ] robots.txt file accessible at /robots.txt
- [ ] robots.txt allows crawling of homepage, products, categories, blog (Allow: /)
- [ ] robots.txt blocks admin panel (Disallow: /admin/)
- [ ] robots.txt blocks cart pages (Disallow: /cart/)
- [ ] robots.txt blocks checkout pages (Disallow: /checkout/)
- [ ] robots.txt blocks customer account pages (Disallow: /account/)
- [ ] robots.txt blocks search result pages (Disallow: /search)
- [ ] robots.txt blocks filter/sort URLs with query parameters (Disallow: /*?sort=)
- [ ] robots.txt includes sitemap location (Sitemap: https://candlestore.com/sitemap.xml)
- [ ] robots.txt syntax validated (no syntax errors)
- [ ] robots.txt tested with Google Search Console robots.txt Tester
- [ ] Googlebot can access critical pages (products, categories verified as "Allowed")
- [ ] Googlebot blocked from non-SEO pages (admin, cart verified as "Blocked")
- [ ] robots.txt returns correct Content-Type: text/plain
- [ ] robots.txt file size <500KB (should be very small, typically <1KB)

### Content Quality and Guidelines

- [ ] Product descriptions tell story, not just bullet points
- [ ] Product descriptions answer customer questions (burn time, ingredients, scent profile)
- [ ] Product descriptions include use cases (meditation, relaxation, gifts)
- [ ] Product descriptions 300-500 words minimum (enforced in admin panel)
- [ ] Category pages include buying guides (how to choose, size guide, scent guide)
- [ ] Blog posts provide value (educational, not sales-focused)
- [ ] Blog posts target informational keywords (how-to, what is, best practices)
- [ ] Blog posts include internal links to relevant products (3-5 links)
- [ ] Blog posts include call-to-action (CTA) to shop related products
- [ ] FAQ pages answer common customer questions
- [ ] FAQ schema markup applied to FAQ pages (enables rich snippets)
- [ ] Content calendar: 1 blog post per week for first 12 weeks
- [ ] Content updated regularly: Product descriptions refreshed annually
- [ ] Seasonal content: Holiday gift guides, seasonal scent recommendations
- [ ] User-generated content: Customer reviews displayed on product pages
- [ ] Duplicate content checked: Copyscape or similar tool confirms uniqueness
- [ ] Plagiarism policy: All content original, no copied manufacturer descriptions
- [ ] Thin content detected: Pages with <300 words flagged for improvement
- [ ] Content gaps identified: Topics competitors cover that we don't
- [ ] Keyword cannibalization monitored: Multiple pages don't target same keyword
- [ ] Content pruning: Low-performing pages updated or removed annually

### Local SEO (If Applicable)

- [ ] Google Business Profile claimed and verified
- [ ] Google Business Profile business name consistent with website
- [ ] Google Business Profile address matches website footer (NAP consistency)
- [ ] Google Business Profile phone number matches website header/footer
- [ ] Google Business Profile categories selected (primary: Candle Store)
- [ ] Google Business Profile description keyword-rich (160 characters max)
- [ ] Google Business Profile hours accurate and updated for holidays
- [ ] Google Business Profile photos uploaded (storefront, interior, products, team)
- [ ] Google Business Profile attributes selected (Women-owned, Small business, etc.)
- [ ] Google Business Profile products listed with photos and prices
- [ ] Google Business Profile posts published weekly (offers, events, updates)
- [ ] Google Business Profile reviews respond to within 48 hours
- [ ] Google Business Profile Q&A monitored and answered
- [ ] LocalBusiness schema on website with geo coordinates
- [ ] Location page created at /locations/eugene-oregon with embedded map
- [ ] NAP citations consistent across Yelp, Facebook, Yellow Pages
- [ ] Local directory submissions: 10+ local business directories
- [ ] Local keywords targeted: "candles Eugene Oregon," "handmade candles Portland"
- [ ] Local backlinks acquired: Eugene Chamber of Commerce, local blogs
- [ ] Location-specific content: "Made in Eugene, Oregon" on product pages
- [ ] Service area defined if offering local delivery/pickup
- [ ] Google Maps embedded on contact/location page
- [ ] Directions to store included on location page
- [ ] Parking information included on location page
- [ ] Store events promoted via Google Business Profile Events

### Performance and Core Web Vitals

- [ ] Largest Contentful Paint (LCP) <2.5 seconds for 75th percentile
- [ ] First Input Delay (FID) <100ms for 75th percentile
- [ ] Cumulative Layout Shift (CLS) <0.1 for 75th percentile
- [ ] Time to First Byte (TTFB) <600ms
- [ ] First Contentful Paint (FCP) <1.8 seconds
- [ ] Speed Index <3.4 seconds
- [ ] Total Blocking Time (TBT) <200ms
- [ ] PageSpeed Insights score >90 on mobile
- [ ] PageSpeed Insights score >95 on desktop
- [ ] Lighthouse SEO audit score 100/100
- [ ] Lighthouse Accessibility audit score >90/100
- [ ] Lighthouse Best Practices audit score >90/100
- [ ] Images lazy loaded with loading="lazy" attribute
- [ ] CSS and JavaScript minified in production
- [ ] CSS and JavaScript bundled to reduce HTTP requests
- [ ] Critical CSS inlined for above-fold content
- [ ] Non-critical CSS deferred (loaded after page render)
- [ ] JavaScript deferred with defer or async attributes
- [ ] Third-party scripts loaded asynchronously (Google Analytics, Facebook Pixel)
- [ ] Font loading optimized with font-display: swap
- [ ] Preconnect to third-party domains (Google Fonts, CDN)
- [ ] Resource hints used (dns-prefetch, preconnect, preload)
- [ ] Gzip or Brotli compression enabled for text resources
- [ ] Browser caching enabled with appropriate cache headers
- [ ] CDN configured for static assets (Task 028 integration)
- [ ] Database queries optimized (indexes on frequently queried columns)
- [ ] N+1 query problems eliminated (eager loading with .Include())
- [ ] Response caching enabled for product pages (cache for 5 minutes)
- [ ] Output caching enabled for category pages
- [ ] Memory caching for frequently accessed data (categories, site settings)

### Analytics and Tracking

- [ ] Google Search Console property created and verified
- [ ] Google Search Console sitemap submitted successfully
- [ ] Google Search Console coverage report monitored weekly
- [ ] Google Search Console performance report reviewed monthly
- [ ] Keyword rankings tracked for 10-20 priority keywords
- [ ] Click-through rates (CTR) monitored for top 10 ranking keywords
- [ ] Impressions tracked to identify keyword opportunities
- [ ] Average position tracked to measure ranking improvements
- [ ] Google Analytics 4 integration (Task 027) tracking organic search traffic
- [ ] Organic search traffic source/medium: google / organic
- [ ] Organic conversion rate tracked separately from paid traffic
- [ ] Organic revenue attributed to SEO efforts
- [ ] Landing page performance measured for organic traffic
- [ ] Bounce rate monitored for organic landing pages (target <60%)
- [ ] Time on page monitored for organic visitors (target >2 minutes)
- [ ] Pages per session tracked for organic traffic (target 3+)
- [ ] SEO dashboard created with key metrics (traffic, rankings, conversions)
- [ ] Monthly SEO report generated with progress vs goals
- [ ] Competitive analysis: Track competitor rankings for shared keywords
- [ ] Backlink profile monitored via Google Search Console Links report
- [ ] New backlinks celebrated, toxic backlinks disavowed
- [ ] Referring domains tracked (target 20-50 quality domains in 12 months)

### Admin Panel SEO Tools

- [ ] SEO metadata editor in product create/edit form
- [ ] Meta title field with character counter (50-60 char target)
- [ ] Meta description field with character counter (150-160 char target)
- [ ] Focus keyword field to define primary keyword for page
- [ ] URL slug editor with availability checker
- [ ] SEO preview showing how page appears in Google search results
- [ ] Keyword density analyzer showing primary keyword usage percentage
- [ ] Content length indicator (word count for descriptions)
- [ ] Readability score (Flesch Reading Ease)
- [ ] Internal links suggestion (related products to link to)
- [ ] Image alt text bulk editor for existing products
- [ ] Duplicate meta title/description detector
- [ ] Thin content report (products with <300 word descriptions)
- [ ] Missing alt text report (products with images missing alt text)
- [ ] Broken internal links report
- [ ] Sitemap regeneration button with timestamp
- [ ] Google Search Console integration (view rankings in admin panel)
- [ ] Keyword rank tracker (monitor position changes over time)
- [ ] SEO checklist per product (green checkmarks when all criteria met)
- [ ] Bulk SEO editor to update meta tags for multiple products
- [ ] SEO template system (apply meta title template to all products: "{Product Name} - Handmade Soy | CandleStore")

### Integration Points

- [ ] Task 011 (Product API): Product entity includes Slug, MetaTitle, MetaDescription, IsPublished properties
- [ ] Task 012 (Category Management): Category entity includes Slug, MetaTitle, MetaDescription properties
- [ ] Task 014 (Product Images): Image upload generates alt text from product name automatically
- [ ] Task 014: Images compressed to WebP format during upload
- [ ] Task 014: Image lazy loading enabled for product image galleries
- [ ] Task 027 (Google Analytics): Organic search traffic tracked as separate segment
- [ ] Task 027: Goal tracking for conversions from organic traffic
- [ ] Task 027: Landing page performance report for organic traffic
- [ ] Task 028 (CDN Cloudflare): Static assets served from CDN for faster load times
- [ ] Task 028: Cloudflare minifies HTML, CSS, JavaScript automatically
- [ ] Task 028: Cloudflare caching reduces server load and improves TTFB

### Security and Best Practices

- [ ] No black-hat SEO techniques employed (keyword stuffing, hidden text, cloaking)
- [ ] No paid link schemes (buying backlinks from link farms)
- [ ] No link spam in comments or forums
- [ ] No doorway pages created solely for search engines
- [ ] No duplicate content across multiple domains (if using staging site, noindex it)
- [ ] No excessive cross-linking between owned websites (link manipulation)
- [ ] Disavow file submitted for toxic backlinks (if any detected)
- [ ] Structured data markup accurate and doesn't mislead users
- [ ] User experience prioritized over search engine manipulation
- [ ] Google Webmaster Guidelines followed strictly
- [ ] Manual action penalties monitored in Google Search Console (none present)
- [ ] Algorithm update resilience: Best practices protect against penalties
- [ ] SEO changes documented (changelog for major updates)
- [ ] SEO changes rolled back if rankings drop unexpectedly

### Documentation Requirements

- [ ] SEO strategy document created outlining keyword targets, content plan
- [ ] Keyword mapping spreadsheet: Which keywords each page targets
- [ ] Content calendar: Blog post topics planned 3 months in advance
- [ ] SEO checklist for content creators (product description guidelines)
- [ ] Meta title and description templates documented
- [ ] URL slug guidelines documented (lowercase, hyphens, keyword-rich)
- [ ] Alt text writing guidelines with examples
- [ ] Structured data implementation guide for developers
- [ ] Monthly SEO report template with key metrics
- [ ] Troubleshooting guide for common SEO issues
- [ ] Google Search Console setup guide for new team members

---

## 5. Testing Requirements

### Unit Tests

#### Test 1: SeoService - GenerateMetaTags Creates Proper Title and Description

```csharp
using AutoFixture;
using CandleStore.Application.Configuration;
using CandleStore.Application.DTOs.Seo;
using CandleStore.Application.Services;
using CandleStore.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CandleStore.Tests.Unit.Services;

public class SeoServiceTests
{
    private readonly IFixture _fixture;
    private readonly SeoSettings _seoSettings;
    private readonly SeoService _sut;

    public SeoServiceTests()
    {
        _fixture = new Fixture();

        _seoSettings = new SeoSettings
        {
            SiteName = "CandleStore",
            Domain = "https://candlestore.com",
            DefaultTitle = "Handmade Soy Candles | CandleStore",
            DefaultDescription = "Shop handmade soy candles made with natural ingredients."
        };

        var options = Options.Create(_seoSettings);
        _sut = new SeoService(options);
    }

    [Fact]
    public void GenerateMetaTags_ForProduct_CreatesProperTitleWithinCharacterLimit()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Lavender Dreams Candle",
            Description = "Hand-poured lavender candle made with 100% soy wax and essential oils. Perfect for meditation and relaxation.",
            Price = 24.99m,
            Slug = "lavender-dreams-candle",
            MetaTitle = null,  // Let service auto-generate
            MetaDescription = null
        };

        // Act
        var metaTags = _sut.GenerateMetaTags(product);

        // Assert
        metaTags.Should().NotBeNull();
        metaTags.Title.Should().NotBeNullOrEmpty();
        metaTags.Title.Should().Contain(product.Name);
        metaTags.Title.Should().Contain(_seoSettings.SiteName);
        metaTags.Title.Length.Should().BeLessOrEqualTo(60, "meta titles should be under 60 characters");
        metaTags.Title.Should().Be("Lavender Dreams Candle - Handmade Soy | CandleStore");  // 52 chars
    }

    [Fact]
    public void GenerateMetaTags_ForProduct_TruncatesTitleIfTooLong()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Lavender and Vanilla Dreams Aromatherapy Meditation Relaxation Candle with Essential Oils",  // Very long name
            Description = "Hand-poured candle",
            Slug = "lavender-vanilla-dreams-candle"
        };

        // Act
        var metaTags = _sut.GenerateMetaTags(product);

        // Assert
        metaTags.Title.Length.Should().BeLessOrEqualTo(60);
        metaTags.Title.Should().Contain("Lavender and Vanilla");  // Start of name
        metaTags.Title.Should().EndWith("| CandleStore");  // Brand always included
    }

    [Fact]
    public void GenerateMetaTags_ForProduct_CreatesProperDescriptionWithinCharacterLimit()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Lavender Dreams Candle",
            Description = "Hand-poured lavender candle made with 100% soy wax and pure essential oils. Burns for 40-50 hours. Perfect for meditation, yoga, and relaxation. Created in Eugene, Oregon with locally-sourced ingredients.",
            Price = 24.99m,
            Slug = "lavender-dreams-candle"
        };

        // Act
        var metaTags = _sut.GenerateMetaTags(product);

        // Assert
        metaTags.Description.Should().NotBeNullOrEmpty();
        metaTags.Description.Length.Should().BeInRange(120, 160, "meta descriptions should be 120-160 characters");
        metaTags.Description.Should().Contain("lavender candle");  // Primary keyword
        metaTags.Description.Should().Contain("soy wax");  // Secondary keyword
        metaTags.Description.Should().NotEndWith("...");  // Shouldn't be truncated mid-sentence
    }

    [Fact]
    public void GenerateMetaTags_WithCustomMetaTitle_UsesCustomTitleInsteadOfAutoGenerated()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Lavender Dreams Candle",
            Description = "Hand-poured lavender candle",
            MetaTitle = "Premium Lavender Candle - 50Hr Burn Time | CandleStore",  // Custom title
            Slug = "lavender-dreams-candle"
        };

        // Act
        var metaTags = _sut.GenerateMetaTags(product);

        // Assert
        metaTags.Title.Should().Be(product.MetaTitle);
        metaTags.Title.Should().NotContain("Lavender Dreams");  // Original product name not used
    }

    [Theory]
    [InlineData("Lavender Candle", "Lavender Candle - Handmade Soy | CandleStore", 48)]
    [InlineData("Vanilla", "Vanilla - Handmade Soy | CandleStore", 37)]
    [InlineData("Eucalyptus Mint Aromatherapy Candle", "Eucalyptus Mint Aromatherapy Candle | CandleStore", 51)]
    public void GenerateMetaTags_VariousProductNames_GeneratesProperTitles(
        string productName, string expectedTitle, int expectedLength)
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = productName,
            Description = "Test candle",
            Slug = productName.ToLower().Replace(" ", "-")
        };

        // Act
        var metaTags = _sut.GenerateMetaTags(product);

        // Assert
        metaTags.Title.Should().Be(expectedTitle);
        metaTags.Title.Length.Should().Be(expectedLength);
        metaTags.Title.Length.Should().BeLessOrEqualTo(60);
    }

    [Fact]
    public void GenerateMetaTags_ForCategory_IncludesCategoryNameAndKeyword()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Soy Candles",
            Description = "Browse our handmade soy candles made with 100% natural soy wax. Eco-friendly, clean-burning candles in various scents.",
            Slug = "soy-candles"
        };

        // Act
        var metaTags = _sut.GenerateMetaTags(category);

        // Assert
        metaTags.Title.Should().Contain("Soy Candles");
        metaTags.Title.Should().Contain("CandleStore");
        metaTags.Title.Should().Be("Soy Candles - Handmade & Natural | CandleStore");
        metaTags.Description.Should().Contain("soy candles");
        metaTags.Description.Should().Contain("handmade");
    }
}
```

#### Test 2: SlugGenerator - Generates SEO-Friendly URL Slugs

```csharp
using CandleStore.Application.Services;
using FluentAssertions;
using Xunit;

namespace CandleStore.Tests.Unit.Services;

public class SlugGeneratorTests
{
    private readonly SlugGenerator _sut;

    public SlugGeneratorTests()
    {
        _sut = new SlugGenerator();
    }

    [Theory]
    [InlineData("Lavender Dreams Candle", "lavender-dreams-candle")]
    [InlineData("Vanilla Bliss", "vanilla-bliss")]
    [InlineData("Eucalyptus Mint Aromatherapy Candle", "eucalyptus-mint-aromatherapy-candle")]
    [InlineData("100% Natural Soy Candle", "100-natural-soy-candle")]
    public void GenerateSlug_WithVariousProductNames_CreatesProperSlug(string productName, string expectedSlug)
    {
        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().Be(expectedSlug);
    }

    [Fact]
    public void GenerateSlug_ConvertsToLowercase()
    {
        // Arrange
        var productName = "LAVENDER DREAMS CANDLE";

        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().Be("lavender-dreams-candle");
        slug.Should().NotContain("LAVENDER");
        slug.Should().NotContain("L");  // No uppercase letters
    }

    [Fact]
    public void GenerateSlug_ReplacesSpacesWithHyphens()
    {
        // Arrange
        var productName = "Lavender Dreams Candle";

        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().NotContain(" ");
        slug.Should().Contain("-");
        slug.Should().Be("lavender-dreams-candle");
    }

    [Theory]
    [InlineData("Candle & Gift Set", "candle-gift-set")]
    [InlineData("Lavender's Dream", "lavenders-dream")]
    [InlineData("Candle (8oz)", "candle-8oz")]
    [InlineData("Candle: Lavender", "candle-lavender")]
    [InlineData("Candle/Diffuser Set", "candle-diffuser-set")]
    public void GenerateSlug_RemovesSpecialCharacters(string productName, string expectedSlug)
    {
        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().Be(expectedSlug);
        slug.Should().NotContain("&");
        slug.Should().NotContain("'");
        slug.Should().NotContain("(");
        slug.Should().NotContain(")");
        slug.Should().NotContain(":");
        slug.Should().NotContain("/");
    }

    [Fact]
    public void GenerateSlug_RemovesMultipleConsecutiveHyphens()
    {
        // Arrange
        var productName = "Lavender  Dreams    Candle";  // Multiple spaces

        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().Be("lavender-dreams-candle");
        slug.Should().NotContain("--");
        slug.Should().NotContain("---");
    }

    [Fact]
    public void GenerateSlug_TrimsLeadingAndTrailingHyphens()
    {
        // Arrange
        var productName = " Lavender Dreams Candle ";  // Leading/trailing spaces

        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().Be("lavender-dreams-candle");
        slug.Should().NotStartWith("-");
        slug.Should().NotEndWith("-");
    }

    [Theory]
    [InlineData("Ü", "u")]
    [InlineData("Café Candle", "cafe-candle")]
    [InlineData("Crème Brûlée Candle", "creme-brulee-candle")]
    public void GenerateSlug_ConvertsDiacriticsToAscii(string productName, string expectedSlug)
    {
        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().Be(expectedSlug);
        slug.Should().MatchRegex("^[a-z0-9-]+$", "slug should only contain lowercase letters, numbers, and hyphens");
    }

    [Fact]
    public void GenerateSlug_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var productName = "";

        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().BeEmpty();
    }

    [Fact]
    public void GenerateSlug_WithOnlySpecialCharacters_ReturnsEmptyString()
    {
        // Arrange
        var productName = "!!!@@@###";

        // Act
        var slug = _sut.GenerateSlug(productName);

        // Assert
        slug.Should().BeEmpty();
    }
}
```

#### Test 3: StructuredDataService - Generates Valid Product Schema JSON-LD

```csharp
using CandleStore.Application.Configuration;
using CandleStore.Application.Services;
using CandleStore.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CandleStore.Tests.Unit.Services;

public class StructuredDataServiceTests
{
    private readonly SeoSettings _seoSettings;
    private readonly StructuredDataService _sut;

    public StructuredDataServiceTests()
    {
        _seoSettings = new SeoSettings
        {
            SiteName = "CandleStore",
            Domain = "https://candlestore.com",
            OrganizationName = "CandleStore LLC",
            OrganizationLogo = "https://candlestore.com/images/logo.png"
        };

        var options = Options.Create(_seoSettings);
        _sut = new StructuredDataService(options);
    }

    [Fact]
    public void GenerateProductSchema_WithValidProduct_ReturnsValidJsonLd()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Lavender Dreams Candle",
            Description = "Hand-poured lavender candle made with 100% soy wax and essential oils.",
            Price = 24.99m,
            Slug = "lavender-dreams-candle",
            SKU = "CAN-LAV-001",
            StockQuantity = 15,
            ProductImages = new List<ProductImage>
            {
                new() { ImageUrl = "https://candlestore.com/images/lavender-1.jpg", DisplayOrder = 1 },
                new() { ImageUrl = "https://candlestore.com/images/lavender-2.jpg", DisplayOrder = 2 }
            },
            Reviews = new List<Review>
            {
                new() { Rating = 5 },
                new() { Rating = 4 },
                new() { Rating = 5 }
            }
        };

        // Act
        var jsonLd = _sut.GenerateProductSchema(product);

        // Assert
        jsonLd.Should().NotBeNullOrEmpty();

        // Parse JSON to verify structure
        var schema = JObject.Parse(jsonLd);

        // Verify @context and @type
        schema["@context"].ToString().Should().Be("https://schema.org");
        schema["@type"].ToString().Should().Be("Product");

        // Verify product properties
        schema["name"].ToString().Should().Be("Lavender Dreams Candle");
        schema["description"].ToString().Should().Contain("Hand-poured lavender candle");
        schema["sku"].ToString().Should().Be("CAN-LAV-001");

        // Verify images array
        var images = schema["image"] as JArray;
        images.Should().NotBeNull();
        images.Should().HaveCount(2);
        images[0].ToString().Should().Contain("lavender-1.jpg");

        // Verify brand
        schema["brand"]["@type"].ToString().Should().Be("Brand");
        schema["brand"]["name"].ToString().Should().Be("CandleStore");

        // Verify offers
        schema["offers"]["@type"].ToString().Should().Be("Offer");
        schema["offers"]["url"].ToString().Should().Be("https://candlestore.com/products/lavender-dreams-candle");
        schema["offers"]["priceCurrency"].ToString().Should().Be("USD");
        schema["offers"]["price"].ToString().Should().Be("24.99");
        schema["offers"]["availability"].ToString().Should().Be("https://schema.org/InStock");
        schema["offers"]["itemCondition"].ToString().Should().Be("https://schema.org/NewCondition");

        // Verify aggregate rating
        schema["aggregateRating"]["@type"].ToString().Should().Be("AggregateRating");
        schema["aggregateRating"]["ratingValue"].ToString().Should().Be("4.67");  // (5+4+5)/3
        schema["aggregateRating"]["reviewCount"].ToString().Should().Be("3");
        schema["aggregateRating"]["bestRating"].ToString().Should().Be("5");
        schema["aggregateRating"]["worstRating"].ToString().Should().Be("1");
    }

    [Fact]
    public void GenerateProductSchema_OutOfStock_ShowsOutOfStockAvailability()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Lavender Dreams Candle",
            Description = "Hand-poured candle",
            Price = 24.99m,
            Slug = "lavender-dreams-candle",
            StockQuantity = 0,  // Out of stock
            ProductImages = new List<ProductImage>()
        };

        // Act
        var jsonLd = _sut.GenerateProductSchema(product);
        var schema = JObject.Parse(jsonLd);

        // Assert
        schema["offers"]["availability"].ToString().Should().Be("https://schema.org/OutOfStock");
    }

    [Fact]
    public void GenerateProductSchema_WithoutReviews_OmitsAggregateRating()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Lavender Dreams Candle",
            Description = "Hand-poured candle",
            Price = 24.99m,
            Slug = "lavender-dreams-candle",
            StockQuantity = 10,
            Reviews = new List<Review>(),  // No reviews
            ProductImages = new List<ProductImage>()
        };

        // Act
        var jsonLd = _sut.GenerateProductSchema(product);
        var schema = JObject.Parse(jsonLd);

        // Assert
        schema.Should().NotContainKey("aggregateRating");
    }

    [Fact]
    public void GenerateOrganizationSchema_CreatesValidOrganizationMarkup()
    {
        // Act
        var jsonLd = _sut.GenerateOrganizationSchema();
        var schema = JObject.Parse(jsonLd);

        // Assert
        schema["@context"].ToString().Should().Be("https://schema.org");
        schema["@type"].ToString().Should().Be("Organization");
        schema["name"].ToString().Should().Be("CandleStore");
        schema["url"].ToString().Should().Be("https://candlestore.com");
        schema["logo"].ToString().Should().Be("https://candlestore.com/images/logo.png");

        // Verify contact point
        schema["contactPoint"]["@type"].ToString().Should().Be("ContactPoint");
        schema["contactPoint"]["contactType"].ToString().Should().Be("Customer Service");
    }

    [Fact]
    public void GenerateBreadcrumbSchema_ForProductPage_CreatesValidBreadcrumbs()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbItem>
        {
            new() { Name = "Home", Url = "https://candlestore.com" },
            new() { Name = "Soy Candles", Url = "https://candlestore.com/categories/soy-candles" },
            new() { Name = "Lavender Dreams Candle", Url = "https://candlestore.com/products/lavender-dreams-candle" }
        };

        // Act
        var jsonLd = _sut.GenerateBreadcrumbSchema(breadcrumbs);
        var schema = JObject.Parse(jsonLd);

        // Assert
        schema["@context"].ToString().Should().Be("https://schema.org");
        schema["@type"].ToString().Should().Be("BreadcrumbList");

        var items = schema["itemListElement"] as JArray;
        items.Should().HaveCount(3);

        items[0]["@type"].ToString().Should().Be("ListItem");
        items[0]["position"].ToString().Should().Be("1");
        items[0]["name"].ToString().Should().Be("Home");
        items[0]["item"].ToString().Should().Be("https://candlestore.com");

        items[2]["position"].ToString().Should().Be("3");
        items[2]["name"].ToString().Should().Be("Lavender Dreams Candle");
    }

    [Fact]
    public void GenerateFaqSchema_WithQuestionAnswerPairs_CreatesValidFaqPage()
    {
        // Arrange
        var faqs = new List<FaqItem>
        {
            new() { Question = "How long do soy candles burn?", Answer = "Our 8oz soy candles burn for 40-50 hours with proper care." },
            new() { Question = "Are soy candles safe for pets?", Answer = "Yes, soy candles are non-toxic and safe for pets when used properly." }
        };

        // Act
        var jsonLd = _sut.GenerateFaqSchema(faqs);
        var schema = JObject.Parse(jsonLd);

        // Assert
        schema["@context"].ToString().Should().Be("https://schema.org");
        schema["@type"].ToString().Should().Be("FAQPage");

        var questions = schema["mainEntity"] as JArray;
        questions.Should().HaveCount(2);

        questions[0]["@type"].ToString().Should().Be("Question");
        questions[0]["name"].ToString().Should().Be("How long do soy candles burn?");
        questions[0]["acceptedAnswer"]["@type"].ToString().Should().Be("Answer");
        questions[0]["acceptedAnswer"]["text"].ToString().Should().Contain("40-50 hours");
    }
}
```

#### Test 4: SitemapGenerator - Creates Valid XML Sitemap

```csharp
using CandleStore.Application.Configuration;
using CandleStore.Application.Interfaces;
using CandleStore.Application.Services;
using CandleStore.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Xml.Linq;
using Xunit;

namespace CandleStore.Tests.Unit.Services;

public class SitemapGeneratorTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly SeoSettings _seoSettings;
    private readonly SitemapGenerator _sut;

    public SitemapGeneratorTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _seoSettings = new SeoSettings
        {
            Domain = "https://candlestore.com"
        };

        var options = Options.Create(_seoSettings);
        _sut = new SitemapGenerator(_mockUnitOfWork.Object, options);
    }

    [Fact]
    public async Task GenerateSitemap_WithPublishedProducts_IncludesProductUrls()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Slug = "lavender-dreams-candle", IsPublished = true, UpdatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = 2, Slug = "vanilla-bliss-candle", IsPublished = true, UpdatedAt = DateTime.UtcNow.AddDays(-5) },
            new() { Id = 3, Slug = "draft-candle", IsPublished = false, UpdatedAt = DateTime.UtcNow }  // Should be excluded
        };

        _mockUnitOfWork.Setup(u => u.Products.GetAllPublishedAsync())
            .ReturnsAsync(products.Where(p => p.IsPublished).ToList());

        _mockUnitOfWork.Setup(u => u.Categories.GetAllAsync())
            .ReturnsAsync(new List<Category>());

        _mockUnitOfWork.Setup(u => u.BlogPosts.GetAllPublishedAsync())
            .ReturnsAsync(new List<BlogPost>());

        // Act
        var sitemapXml = await _sut.GenerateSitemapAsync();

        // Assert
        sitemapXml.Should().NotBeNullOrEmpty();

        var doc = XDocument.Parse(sitemapXml);
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

        var urls = doc.Descendants(ns + "url").ToList();
        urls.Should().HaveCountGreaterOrEqualTo(2);  // At least 2 published products

        // Verify product URLs
        var productUrls = urls.Where(u => u.Element(ns + "loc")?.Value.Contains("/products/") == true).ToList();
        productUrls.Should().HaveCount(2);

        productUrls.Should().Contain(u => u.Element(ns + "loc").Value == "https://candlestore.com/products/lavender-dreams-candle");
        productUrls.Should().Contain(u => u.Element(ns + "loc").Value == "https://candlestore.com/products/vanilla-bliss-candle");

        // Verify draft product excluded
        urls.Should().NotContain(u => u.Element(ns + "loc")?.Value.Contains("draft-candle") == true);
    }

    [Fact]
    public async Task GenerateSitemap_IncludesHomepageWithHighestPriority()
    {
        // Arrange
        _mockUnitOfWork.Setup(u => u.Products.GetAllPublishedAsync())
            .ReturnsAsync(new List<Product>());
        _mockUnitOfWork.Setup(u => u.Categories.GetAllAsync())
            .ReturnsAsync(new List<Category>());
        _mockUnitOfWork.Setup(u => u.BlogPosts.GetAllPublishedAsync())
            .ReturnsAsync(new List<BlogPost>());

        // Act
        var sitemapXml = await _sut.GenerateSitemapAsync();
        var doc = XDocument.Parse(sitemapXml);
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

        // Assert
        var homepageUrl = doc.Descendants(ns + "url")
            .FirstOrDefault(u => u.Element(ns + "loc")?.Value == "https://candlestore.com/");

        homepageUrl.Should().NotBeNull();
        homepageUrl.Element(ns + "priority")?.Value.Should().Be("1.0");
        homepageUrl.Element(ns + "changefreq")?.Value.Should().Be("daily");
    }

    [Fact]
    public async Task GenerateSitemap_ProductUrls_HaveProperPriorityAndChangeFreq()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Id = 1, Slug = "lavender-candle", IsPublished = true, UpdatedAt = DateTime.UtcNow }
        };

        _mockUnitOfWork.Setup(u => u.Products.GetAllPublishedAsync())
            .ReturnsAsync(products);
        _mockUnitOfWork.Setup(u => u.Categories.GetAllAsync())
            .ReturnsAsync(new List<Category>());
        _mockUnitOfWork.Setup(u => u.BlogPosts.GetAllPublishedAsync())
            .ReturnsAsync(new List<BlogPost>());

        // Act
        var sitemapXml = await _sut.GenerateSitemapAsync();
        var doc = XDocument.Parse(sitemapXml);
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

        // Assert
        var productUrl = doc.Descendants(ns + "url")
            .FirstOrDefault(u => u.Element(ns + "loc")?.Value.Contains("/products/lavender-candle") == true);

        productUrl.Should().NotBeNull();
        productUrl.Element(ns + "priority")?.Value.Should().Be("0.8");
        productUrl.Element(ns + "changefreq")?.Value.Should().Be("weekly");
        productUrl.Element(ns + "lastmod")?.Value.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GenerateSitemap_CreatesValidXmlStructure()
    {
        // Arrange
        _mockUnitOfWork.Setup(u => u.Products.GetAllPublishedAsync())
            .ReturnsAsync(new List<Product>());
        _mockUnitOfWork.Setup(u => u.Categories.GetAllAsync())
            .ReturnsAsync(new List<Category>());
        _mockUnitOfWork.Setup(u => u.BlogPosts.GetAllPublishedAsync())
            .ReturnsAsync(new List<BlogPost>());

        // Act
        var sitemapXml = await _sut.GenerateSitemapAsync();

        // Assert - Should parse without errors
        var doc = XDocument.Parse(sitemapXml);
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

        doc.Root.Should().NotBeNull();
        doc.Root.Name.Should().Be(ns + "urlset");
        doc.Root.Attribute("xmlns")?.Value.Should().Be("http://www.sitemaps.org/schemas/sitemap/0.9");
    }

    [Fact]
    public async Task GenerateSitemap_LastModDate_FormattedAsW3cDateTime()
    {
        // Arrange
        var updateDate = new DateTime(2025, 11, 8, 14, 30, 0, DateTimeKind.Utc);
        var products = new List<Product>
        {
            new() { Id = 1, Slug = "lavender-candle", IsPublished = true, UpdatedAt = updateDate }
        };

        _mockUnitOfWork.Setup(u => u.Products.GetAllPublishedAsync())
            .ReturnsAsync(products);
        _mockUnitOfWork.Setup(u => u.Categories.GetAllAsync())
            .ReturnsAsync(new List<Category>());
        _mockUnitOfWork.Setup(u => u.BlogPosts.GetAllPublishedAsync())
            .ReturnsAsync(new List<BlogPost>());

        // Act
        var sitemapXml = await _sut.GenerateSitemapAsync();
        var doc = XDocument.Parse(sitemapXml);
        var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");

        // Assert
        var productUrl = doc.Descendants(ns + "url")
            .FirstOrDefault(u => u.Element(ns + "loc")?.Value.Contains("/products/lavender-candle") == true);

        var lastmod = productUrl.Element(ns + "lastmod")?.Value;
        lastmod.Should().Be("2025-11-08");  // YYYY-MM-DD format
    }
}
```

#### Test 5: MetaTagValidator - Validates SEO Metadata Quality

```csharp
using CandleStore.Application.Services;
using CandleStore.Application.DTOs.Seo;
using FluentAssertions;
using Xunit;

namespace CandleStore.Tests.Unit.Services;

public class MetaTagValidatorTests
{
    private readonly MetaTagValidator _sut;

    public MetaTagValidatorTests()
    {
        _sut = new MetaTagValidator();
    }

    [Fact]
    public void ValidateTitle_WithOptimalLength_ReturnsValid()
    {
        // Arrange
        var title = "Lavender Dreams Candle - Handmade Soy | CandleStore";  // 52 chars

        // Act
        var result = _sut.ValidateTitle(title);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
        result.CharacterCount.Should().Be(52);
    }

    [Fact]
    public void ValidateTitle_TooShort_ReturnsWarning()
    {
        // Arrange
        var title = "Candle";  // 6 chars (too short)

        // Act
        var result = _sut.ValidateTitle(title);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("too short"));
        result.Warnings.Should().Contain(w => w.Contains("30 characters"));
    }

    [Fact]
    public void ValidateTitle_TooLong_ReturnsWarning()
    {
        // Arrange
        var title = "Lavender and Vanilla Dreams Aromatherapy Meditation Relaxation Candle with Essential Oils Hand-Poured | CandleStore";  // 113 chars

        // Act
        var result = _sut.ValidateTitle(title);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("too long"));
        result.Warnings.Should().Contain(w => w.Contains("60 characters"));
        result.Warnings.Should().Contain(w => w.Contains("truncated"));
    }

    [Theory]
    [InlineData(30, true)]   // Minimum acceptable
    [InlineData(50, true)]   // Optimal
    [InlineData(60, true)]   // Maximum optimal
    [InlineData(65, false)]  // Too long
    [InlineData(25, false)]  // Too short
    public void ValidateTitle_VariousLengths_ValidatesCorrectly(int length, bool expectedValid)
    {
        // Arrange
        var title = new string('A', length);

        // Act
        var result = _sut.ValidateTitle(title);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void ValidateDescription_WithOptimalLength_ReturnsValid()
    {
        // Arrange
        var description = "Hand-poured lavender candle made with 100% soy wax and essential oils. Burns 40-50 hours. Perfect for meditation and relaxation. Free shipping over $50.";  // 158 chars

        // Act
        var result = _sut.ValidateDescription(description);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().BeEmpty();
        result.CharacterCount.Should().Be(158);
    }

    [Fact]
    public void ValidateDescription_TooShort_ReturnsWarning()
    {
        // Arrange
        var description = "Lavender candle";  // 15 chars (way too short)

        // Act
        var result = _sut.ValidateDescription(description);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("too short"));
        result.Warnings.Should().Contain(w => w.Contains("120 characters"));
    }

    [Fact]
    public void ValidateDescription_TooLong_ReturnsWarning()
    {
        // Arrange
        var description = new string('A', 200);  // 200 chars (too long)

        // Act
        var result = _sut.ValidateDescription(description);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("too long"));
        result.Warnings.Should().Contain(w => w.Contains("160 characters"));
    }

    [Fact]
    public void ValidateKeywordDensity_OptimalDensity_ReturnsValid()
    {
        // Arrange
        var content = "Our lavender candle is hand-poured with 100% soy wax. This handmade candle burns for 40 hours. Perfect lavender scent for relaxation and meditation. Buy this premium candle today.";  // ~30 words, "candle" appears 4 times = 13% density (high but acceptable)
        var keyword = "candle";

        // Act
        var result = _sut.ValidateKeywordDensity(content, keyword);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Density.Should().BeInRange(1.0, 3.0);  // 1-3% is optimal
    }

    [Fact]
    public void ValidateKeywordDensity_KeywordStuffing_ReturnsWarning()
    {
        // Arrange
        var content = "Candle candle candle candle candle candle candle candle candle candle";  // 10 "candle" in 10 words = 100% density (obvious stuffing)
        var keyword = "candle";

        // Act
        var result = _sut.ValidateKeywordDensity(content, keyword);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("keyword stuffing"));
        result.Density.Should().BeGreaterThan(5.0);
    }

    [Fact]
    public void ValidateKeywordDensity_TooLowDensity_ReturnsWarning()
    {
        // Arrange
        var content = new string('A', 1000) + " candle";  // ~200 words, "candle" appears once = 0.5% density (too low)
        var keyword = "candle";

        // Act
        var result = _sut.ValidateKeywordDensity(content, keyword);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Warnings.Should().Contain(w => w.Contains("too low"));
        result.Density.Should().BeLessThan(1.0);
    }

    [Fact]
    public void ValidateKeywordDensity_CaseInsensitive_CountsAllVariations()
    {
        // Arrange
        var content = "Candle CANDLE candle CaNdLe";  // 4 variations, all same keyword
        var keyword = "candle";

        // Act
        var result = _sut.ValidateKeywordDensity(content, keyword);

        // Assert
        result.OccurrenceCount.Should().Be(4);
    }
}
```

---

### Integration Tests

#### Integration Test 1: SEO Meta Tags Rendered in HTML Output

```csharp
using CandleStore.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Xunit;

namespace CandleStore.Tests.Integration.Seo;

[Trait("Category", "Integration")]
public class SeoMetaTagsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SeoMetaTagsIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ProductPage_RendersProperMetaTags()
    {
        // Arrange - Seed database with test product
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var product = new Product
        {
            Name = "Lavender Dreams Candle",
            Description = "Hand-poured lavender candle made with 100% soy wax and essential oils.",
            Price = 24.99m,
            Slug = "lavender-dreams-candle",
            MetaTitle = "Lavender Dreams Candle - Handmade Soy | CandleStore",
            MetaDescription = "Hand-poured lavender candle made with 100% soy wax. Burns 40-50 hours. Perfect for meditation. Free shipping over $50.",
            IsPublished = true,
            StockQuantity = 10
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync("/products/lavender-dreams-candle");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify meta title
        html.Should().Contain("<title>Lavender Dreams Candle - Handmade Soy | CandleStore</title>");

        // Verify meta description
        html.Should().Contain("<meta name=\"description\" content=\"Hand-poured lavender candle made with 100% soy wax. Burns 40-50 hours. Perfect for meditation. Free shipping over $50.\"");

        // Verify canonical URL
        html.Should().Contain("<link rel=\"canonical\" href=\"https://candlestore.com/products/lavender-dreams-candle\"");

        // Verify Open Graph tags
        html.Should().Contain("<meta property=\"og:title\" content=\"Lavender Dreams Candle - Handmade Soy | CandleStore\"");
        html.Should().Contain("<meta property=\"og:type\" content=\"product\"");
        html.Should().Contain("<meta property=\"og:url\" content=\"https://candlestore.com/products/lavender-dreams-candle\"");

        // Verify Twitter Card tags
        html.Should().Contain("<meta name=\"twitter:card\" content=\"summary_large_image\"");
        html.Should().Contain("<meta name=\"twitter:title\" content=\"Lavender Dreams Candle - Handmade Soy | CandleStore\"");
    }

    [Fact]
    public async Task ProductPage_RendersProductSchema()
    {
        // Arrange - Use existing product from previous test

        // Act
        var response = await _client.GetAsync("/products/lavender-dreams-candle");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        html.Should().Contain("<script type=\"application/ld+json\">");
        html.Should().Contain("\"@type\": \"Product\"");
        html.Should().Contain("\"name\": \"Lavender Dreams Candle\"");
        html.Should().Contain("\"priceCurrency\": \"USD\"");
        html.Should().Contain("\"price\": \"24.99\"");
        html.Should().Contain("\"availability\": \"https://schema.org/InStock\"");
    }

    [Fact]
    public async Task Homepage_RendersSiteNameInTitle()
    {
        // Act
        var response = await _client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        // Assert
        html.Should().Contain("<title>Handmade Soy Candles | CandleStore</title>");
        html.Should().Contain("<meta name=\"description\"");
    }

    [Fact]
    public async Task Sitemap_AccessibleAndValid()
    {
        // Act
        var response = await _client.GetAsync("/sitemap.xml");
        var xml = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("application/xml");

        xml.Should().Contain("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.Should().Contain("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        xml.Should().Contain("<loc>https://candlestore.com/</loc>");
        xml.Should().Contain("<loc>https://candlestore.com/products/lavender-dreams-candle</loc>");
    }

    [Fact]
    public async Task RobotsTxt_BlocksAdminPanel()
    {
        // Act
        var response = await _client.GetAsync("/robots.txt");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType.MediaType.Should().Be("text/plain");

        content.Should().Contain("User-agent: *");
        content.Should().Contain("Disallow: /admin/");
        content.Should().Contain("Disallow: /cart/");
        content.Should().Contain("Disallow: /checkout/");
        content.Should().Contain("Sitemap: https://candlestore.com/sitemap.xml");
    }
}
```

---

### End-to-End (E2E) Tests

#### Scenario 1: Google Search Console Validation

**Gherkin Scenario:**
```gherkin
Feature: SEO Meta Tags and Structured Data
  As a store owner
  I want product pages to have proper SEO optimization
  So that they rank well in Google search results

Scenario: Product page has all required SEO elements
  Given I have a published product "Lavender Dreams Candle"
  When I view the product page
  Then I should see a unique meta title under 60 characters
  And I should see a meta description between 120-160 characters
  And I should see a canonical URL pointing to the product page
  And I should see Product schema markup with price and availability
  And the page should pass Google's Rich Results Test
```

**C# Selenium Implementation:**

```csharp
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using FluentAssertions;
using Xunit;
using Newtonsoft.Json.Linq;

namespace CandleStore.Tests.E2E.Seo;

[Trait("Category", "E2E")]
public class SeoMetaTagsE2ETests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl = "https://localhost:5002";

    public SeoMetaTagsE2ETests()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--ignore-certificate-errors");
        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [Fact]
    public void ProductPage_HasProperMetaTags()
    {
        // Navigate to product page
        _driver.Navigate().GoToUrl($"{_baseUrl}/products/lavender-dreams-candle");

        // Get page title
        var pageTitle = _driver.Title;
        pageTitle.Should().Contain("Lavender Dreams Candle");
        pageTitle.Should().Contain("CandleStore");
        pageTitle.Length.Should().BeLessOrEqualTo(60, "meta title should be under 60 characters for Google");

        // Get meta description
        var metaDescription = _driver.FindElement(By.CssSelector("meta[name='description']"))
            .GetAttribute("content");
        metaDescription.Should().NotBeNullOrEmpty();
        metaDescription.Length.Should().BeInRange(120, 160, "meta description should be 120-160 characters");
        metaDescription.Should().Contain("lavender candle", "should include primary keyword");

        // Get canonical URL
        var canonicalUrl = _driver.FindElement(By.CssSelector("link[rel='canonical']"))
            .GetAttribute("href");
        canonicalUrl.Should().Be($"{_baseUrl}/products/lavender-dreams-candle");

        // Verify Open Graph tags
        var ogTitle = _driver.FindElement(By.CssSelector("meta[property='og:title']"))
            .GetAttribute("content");
        ogTitle.Should().Contain("Lavender Dreams Candle");

        var ogType = _driver.FindElement(By.CssSelector("meta[property='og:type']"))
            .GetAttribute("content");
        ogType.Should().Be("product");

        var ogUrl = _driver.FindElement(By.CssSelector("meta[property='og:url']"))
            .GetAttribute("content");
        ogUrl.Should().Be($"{_baseUrl}/products/lavender-dreams-candle");
    }

    [Fact]
    public void ProductPage_HasValidProductSchema()
    {
        // Navigate to product page
        _driver.Navigate().GoToUrl($"{_baseUrl}/products/lavender-dreams-candle");

        // Find JSON-LD script tag
        var schemaScript = _driver.FindElement(By.XPath("//script[@type='application/ld+json']"));
        var jsonLd = schemaScript.GetAttribute("innerHTML");

        jsonLd.Should().NotBeNullOrEmpty();

        // Parse and validate schema
        var schema = JObject.Parse(jsonLd);

        schema["@context"].ToString().Should().Be("https://schema.org");
        schema["@type"].ToString().Should().Be("Product");
        schema["name"].ToString().Should().Be("Lavender Dreams Candle");
        schema["offers"]["@type"].ToString().Should().Be("Offer");
        schema["offers"]["priceCurrency"].ToString().Should().Be("USD");
        schema["offers"]["price"].Should().NotBeNull();
        schema["offers"]["availability"].Should().NotBeNull();

        // Verify price is valid decimal
        var price = decimal.Parse(schema["offers"]["price"].ToString());
        price.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProductPage_HeadingHierarchy_IsProper()
    {
        // Navigate to product page
        _driver.Navigate().GoToUrl($"{_baseUrl}/products/lavender-dreams-candle");

        // Verify only one H1
        var h1Elements = _driver.FindElements(By.TagName("h1"));
        h1Elements.Should().HaveCount(1, "page should have exactly one H1 tag");
        h1Elements[0].Text.Should().Contain("Lavender Dreams Candle");

        // Verify H2 tags exist for sections
        var h2Elements = _driver.FindElements(By.TagName("h2"));
        h2Elements.Should().HaveCountGreaterOrEqualTo(2, "page should have multiple H2 section headers");

        // Common section headers
        var h2Texts = h2Elements.Select(h => h.Text).ToList();
        h2Texts.Should().Contain(t => t.Contains("Description") || t.Contains("Scent Profile") || t.Contains("Reviews"));
    }

    [Fact]
    public void ProductPage_Images_HaveProperAltText()
    {
        // Navigate to product page
        _driver.Navigate().GoToUrl($"{_baseUrl}/products/lavender-dreams-candle");

        // Get all images
        var images = _driver.FindElements(By.TagName("img"));
        images.Should().HaveCountGreaterOrEqualTo(1, "product page should have at least one image");

        // Verify all images have alt text
        foreach (var image in images)
        {
            var altText = image.GetAttribute("alt");
            altText.Should().NotBeNullOrEmpty("all images must have alt text for SEO and accessibility");
            altText.Length.Should().BeGreaterThan(10, "alt text should be descriptive, not just 'image'");
        }
    }

    [Fact]
    public void Sitemap_IsAccessibleAndValid()
    {
        // Navigate to sitemap
        _driver.Navigate().GoToUrl($"{_baseUrl}/sitemap.xml");

        // Verify page loaded (no 404)
        _driver.PageSource.Should().Contain("<?xml");
        _driver.PageSource.Should().Contain("<urlset");
        _driver.PageSource.Should().Contain("<loc>https://candlestore.com/</loc>", "sitemap should include homepage");
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }
}
```

---

### Performance Tests

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CandleStore.Application.Services;
using CandleStore.Domain.Entities;

namespace CandleStore.Tests.Performance.Seo;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class SeoPerformanceBenchmarks
{
    private SeoService _seoService;
    private Product _testProduct;
    private List<Product> _products;

    [GlobalSetup]
    public void Setup()
    {
        var settings = Options.Create(new SeoSettings
        {
            SiteName = "CandleStore",
            Domain = "https://candlestore.com"
        });

        _seoService = new SeoService(settings);

        _testProduct = new Product
        {
            Id = 1,
            Name = "Lavender Dreams Candle",
            Description = new string('A', 500),  // 500 char description
            Price = 24.99m,
            Slug = "lavender-dreams-candle"
        };

        _products = Enumerable.Range(1, 100).Select(i => new Product
        {
            Id = i,
            Name = $"Product {i}",
            Description = new string('A', 500),
            Price = 24.99m,
            Slug = $"product-{i}",
            IsPublished = true
        }).ToList();
    }

    [Benchmark]
    public MetaTagsDto GenerateMetaTags_SingleProduct()
    {
        return _seoService.GenerateMetaTags(_testProduct);
    }

    [Benchmark]
    public List<MetaTagsDto> GenerateMetaTags_100Products()
    {
        return _products.Select(p => _seoService.GenerateMetaTags(p)).ToList();
    }

    [Benchmark]
    public string GenerateProductSchema()
    {
        return _structuredDataService.GenerateProductSchema(_testProduct);
    }

    [Benchmark]
    public async Task<string> GenerateSitemap_100Urls()
    {
        return await _sitemapGenerator.GenerateSitemapAsync();
    }
}

// Performance Targets:
// - GenerateMetaTags_SingleProduct: < 1ms (p95)
// - GenerateMetaTags_100Products: < 50ms (p95)
// - GenerateProductSchema: < 2ms (p95)
// - GenerateSitemap_100Urls: < 100ms (p95)
// - Memory allocation: < 50KB per operation
```

---

### Regression Tests

```csharp
using CandleStore.Application.Services;
using CandleStore.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace CandleStore.Tests.Regression.Seo;

[Trait("Category", "Regression")]
public class SeoRegressionTests
{
    [Fact]
    public void ProductCreation_WithSeo_DoesNotBreakExistingProductWorkflow()
    {
        // Arrange
        var productService = TestHelpers.CreateProductService();
        var createProductDto = new CreateProductDto
        {
            Name = "Lavender Dreams Candle",
            Description = "Hand-poured lavender candle made with 100% soy wax and essential oils. Burns for 40-50 hours. Perfect for meditation, yoga, and relaxation. Created in Eugene, Oregon with locally-sourced ingredients.",
            Price = 24.99m,
            StockQuantity = 10,
            CategoryId = 1,
            MetaTitle = "Lavender Dreams Candle - Handmade Soy | CandleStore",
            MetaDescription = "Hand-poured lavender candle with 100% soy wax. Burns 40-50 hours. Perfect for meditation. Free shipping over $50."
        };

        // Act
        var product = await productService.CreateProductAsync(createProductDto);

        // Assert
        product.Should().NotBeNull();
        product.Id.Should().BeGreaterThan(0);
        product.Name.Should().Be("Lavender Dreams Candle");
        product.Slug.Should().Be("lavender-dreams-candle");
        product.MetaTitle.Should().Be("Lavender Dreams Candle - Handmade Soy | CandleStore");
        product.MetaDescription.Should().NotBeNullOrEmpty();
        product.IsPublished.Should().BeFalse();  // Default to unpublished
    }

    [Fact]
    public void ProductUpdate_PreservesSlug_WhenNameChanges()
    {
        // Arrange
        var productService = TestHelpers.CreateProductService();
        var product = TestHelpers.CreateTestProduct("Lavender Dreams Candle");
        var originalSlug = product.Slug;  // "lavender-dreams-candle"

        // Act - Update product name but keep slug
        product.Name = "Lavender Dreams Aromatherapy Candle";  // Name changed
        var updated = await productService.UpdateProductAsync(product.Id, new UpdateProductDto
        {
            Name = product.Name,
            Slug = originalSlug  // Explicitly preserve slug
        });

        // Assert
        updated.Name.Should().Be("Lavender Dreams Aromatherapy Candle");
        updated.Slug.Should().Be(originalSlug, "slug should not change when name changes to prevent broken links");
    }
}
```

---

## 6. User Verification Steps

After implementing SEO optimization, perform these manual verification steps to ensure everything works correctly:

### Verification Step 1: Verify Meta Tags Render on Product Page

**Objective:** Confirm meta title and description appear in HTML source

**Steps:**
1. Start Storefront: `dotnet run --project src/CandleStore.Storefront`
2. Navigate to product page: `https://localhost:5002/products/lavender-dreams-candle`
3. Right-click page → "View Page Source" (or Ctrl+U)
4. Search (Ctrl+F) for `<title>`
5. **Verify:** Title contains product name + brand
   - Example: `<title>Lavender Dreams Candle - Handmade Soy | CandleStore</title>`
6. Search for `<meta name="description"`
7. **Verify:** Meta description 120-160 characters with keyword
   - Example: `<meta name="description" content="Hand-poured lavender candle made with 100% soy wax. Burns 40-50 hours. Perfect for meditation. Free shipping over $50.">`
8. Search for `<link rel="canonical"`
9. **Verify:** Canonical URL points to this product page
   - Example: `<link rel="canonical" href="https://candlestore.com/products/lavender-dreams-candle">`
10. Search for `og:title`
11. **Verify:** Open Graph tags present for social sharing
12. **Verify:** Twitter Card meta tags present

**Expected Result:**
- Title tag present and optimized (50-60 chars)
- Meta description present and optimized (150-160 chars)
- Canonical URL correct
- Open Graph and Twitter Card tags present

**Pass/Fail Criteria:**
- ✅ Pass: All meta tags present, correct length, include keywords
- ❌ Fail: Meta tags missing, too long/short, generic content

---

### Verification Step 2: Validate Structured Data with Google Rich Results Test

**Objective:** Ensure Product schema markup is valid and displays rich snippets

**Steps:**
1. Navigate to product page: `https://localhost:5002/products/lavender-dreams-candle`
2. Copy full URL from address bar
3. Open Google Rich Results Test: https://search.google.com/test/rich-results
4. Paste URL into "Test any URL from the web" field
5. Click **"Test URL"** button
6. Wait 10-15 seconds for Google to fetch and analyze page
7. **Verify:** "Rich results can be displayed" green checkmark appears
8. **Verify:** "Product" type detected in results
9. Click **"View tested page"** dropdown → **"More info"**
10. **Verify:** Extracted data shows:
    - Name: Lavender Dreams Candle
    - Price: $24.99
    - Currency: USD
    - Availability: In stock
    - Image URLs: Product photos
11. Click **"Preview"** tab
12. **Verify:** Preview shows how product appears in Google search:
    ```
    Lavender Dreams Candle - Handmade Soy | CandleStore
    candlestore.com › products › lavender-dreams-candle
    ★★★★★ 4.8 (47 reviews) · $24.99 · In stock
    Hand-poured lavender candle made with 100% soy wax...
    ```
13. **Verify:** Star rating displays (if product has reviews)
14. **Verify:** Price displays in correct currency
15. **Verify:** Availability shows "In stock" (green check) or "Out of stock" (red X)

**Expected Result:**
- Google validates schema without errors
- Product schema extracted correctly
- Preview shows rich snippet with rating, price, availability

**Pass/Fail Criteria:**
- ✅ Pass: Green checkmark, all fields populated, rich snippet previews correctly
- ❌ Fail: Validation errors, missing fields, no rich snippet preview

---

### Verification Step 3: Check XML Sitemap Generation

**Objective:** Verify sitemap includes all published products and categories

**Steps:**
1. Navigate to sitemap URL: `https://localhost:5002/sitemap.xml`
2. **Verify:** XML structure displays (not 404 error)
3. **Verify:** Page shows XML format:
   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
     <url>
       <loc>https://candlestore.com/</loc>
       <lastmod>2025-11-08</lastmod>
       <changefreq>daily</changefreq>
       <priority>1.0</priority>
     </url>
     ...
   </urlset>
   ```
4. Search (Ctrl+F) for product slug: "lavender-dreams-candle"
5. **Verify:** Product URL present in sitemap
6. **Verify:** Each `<url>` entry contains:
   - `<loc>` with full HTTPS URL
   - `<lastmod>` with date in YYYY-MM-DD format
   - `<changefreq>` (daily/weekly/monthly)
   - `<priority>` (0.1-1.0)
7. Count total `<url>` entries
8. **Verify:** Count matches approximately: Homepage + Products + Categories + Blog Posts
9. Create unpublished test product in admin panel
10. Regenerate sitemap: Admin Panel → SEO → Regenerate Sitemap
11. Refresh sitemap URL
12. **Verify:** Unpublished product NOT in sitemap
13. Admin Panel → Mark test product as Published
14. Regenerate sitemap again
15. **Verify:** Published product NOW appears in sitemap

**Expected Result:**
- Sitemap accessible at /sitemap.xml
- Valid XML structure
- Includes all published products, categories, blog posts
- Excludes unpublished/draft content
- Lastmod dates accurate

**Pass/Fail Criteria:**
- ✅ Pass: Sitemap valid XML, includes published content, excludes drafts
- ❌ Fail: 404 error, invalid XML, includes unpublished content, missing published content

---

### Verification Step 4: Verify robots.txt Configuration

**Objective:** Ensure robots.txt blocks admin panel, allows product pages

**Steps:**
1. Navigate to: `https://localhost:5002/robots.txt`
2. **Verify:** File displays as plain text (not 404)
3. **Verify:** Contains:
   ```
   User-agent: *
   Allow: /
   Disallow: /admin/
   Disallow: /cart/
   Disallow: /checkout/
   Disallow: /account/

   Sitemap: https://candlestore.com/sitemap.xml
   ```
4. **Verify:** Sitemap location matches your actual domain
5. Test with Google Search Console Robots.txt Tester:
   - Google Search Console → Settings → robots.txt Tester
   - Enter URL to test: `/products/lavender-dreams-candle`
   - Click **"Test"**
   - **Verify:** Result shows "Allowed" (green checkmark)
6. Enter URL to test: `/admin/orders`
7. Click **"Test"**
8. **Verify:** Result shows "Blocked" (red X)
9. Repeat for `/cart`, `/checkout`, `/account`
10. **Verify:** All blocked URLs show red X

**Expected Result:**
- robots.txt accessible and properly formatted
- Product pages allowed for crawling
- Admin, cart, checkout, account blocked
- Sitemap location specified

**Pass/Fail Criteria:**
- ✅ Pass: robots.txt valid, correct allow/disallow rules, sitemap listed
- ❌ Fail: 404 error, syntax errors, wrong rules (blocks products, allows admin)

---

### Verification Step 5: Test URL Slug Generation and Uniqueness

**Objective:** Verify product slugs auto-generate and remain unique

**Steps:**
1. Login to Admin Panel: `https://localhost:5003`
2. Navigate to **Products** → **Create New Product**
3. Enter product name: "Lavender Dreams Candle"
4. **Verify:** Slug field auto-populates: "lavender-dreams-candle"
5. **Verify:** Slug is lowercase, spaces replaced with hyphens
6. Change product name to: "Lavender & Dreams Candle!!!"
7. **Verify:** Slug updates to: "lavender-dreams-candle"
8. **Verify:** Special characters (&, !, ?) removed
9. Try manual slug override: "premium-lavender-candle"
10. Click **"Check Availability"** button
11. **Verify:** System validates slug is unique (green checkmark if available)
12. Save product with slug "lavender-dreams-candle"
13. Create another product: "Lavender Dreams Candle" (same name)
14. **Verify:** Slug auto-generates as: "lavender-dreams-candle-2"
15. **Verify:** Collision detection prevents duplicate slugs
16. Try to manually set slug to existing slug "lavender-dreams-candle"
17. Click **"Check Availability"**
18. **Verify:** Error message: "Slug already exists. Please choose a different slug."
19. Test special characters:
    - "Café Candle" → "cafe-candle"
    - "Crème Brûlée Candle" → "creme-brulee-candle"
    - "100% Soy Candle" → "100-soy-candle"
20. **Verify:** All diacritics and special chars handled correctly

**Expected Result:**
- Slugs auto-generate from product names
- Lowercase, hyphens, special chars removed
- Unique validation prevents duplicates
- Manual override available with validation

**Pass/Fail Criteria:**
- ✅ Pass: Slugs generate correctly, uniqueness enforced, special chars handled
- ❌ Fail: Duplicates allowed, special chars not removed, validation errors

---

### Verification Step 6: Validate SEO Metadata in Admin Panel

**Objective:** Verify admin panel enforces meta title/description character limits

**Steps:**
1. Admin Panel → Products → Create New Product
2. Enter product details:
   - Name: "Lavender Dreams Candle"
   - Description: (300+ word description)
3. Scroll to **SEO Metadata** section
4. **Verify:** Character counter displays for Meta Title field
5. Enter meta title: "Lavender Dreams Candle - Handmade Soy | CandleStore"
6. **Verify:** Character counter shows: "(52/60 chars) ✓" (green checkmark)
7. Try entering very long title: "Lavender and Vanilla Dreams Aromatherapy Meditation Relaxation Candle with Essential Oils Hand-Poured by Sarah | CandleStore"
8. **Verify:** Character counter shows: "(125/60 chars) ⚠️" (warning)
9. **Verify:** Warning message: "Meta title should be under 60 characters to avoid truncation in search results"
10. Enter meta description: (Write 150-character description)
11. **Verify:** Character counter shows: "(148/160 chars) ✓"
12. Try very short description: "Lavender candle"
13. **Verify:** Warning: "Meta description should be at least 120 characters for optimal SEO"
14. Try very long description: (Write 250-character description)
15. **Verify:** Warning: "Meta description should be under 160 characters to avoid truncation"
16. Test **"Generate Meta Tags from Description"** AI button
17. Click button
18. **Verify:** Meta title and description auto-populate from product description
19. **Verify:** Generated tags within character limits
20. **Verify:** Generated tags include product name and keywords

**Expected Result:**
- Character counters display in real-time
- Warnings appear for too long/short content
- Green checkmarks for optimal lengths
- AI generation creates valid meta tags

**Pass/Fail Criteria:**
- ✅ Pass: Counters work, warnings display, AI generation works
- ❌ Fail: No character limits, no warnings, AI button doesn't work

---

### Verification Step 7: Test Google Search Console Integration

**Objective:** Verify sitemap submitted and coverage report populating

**Steps:**
1. Navigate to Google Search Console: https://search.google.com/search-console
2. Select your property: "candlestore.com"
3. Sidebar → **Sitemaps**
4. **Verify:** Sitemap status shows:
   - URL: `https://candlestore.com/sitemap.xml`
   - Status: "Success"
   - Last read: (recent date)
   - Discovered URLs: (count of sitemap entries)
5. If status shows "Couldn't fetch":
   - Verify sitemap URL accessible: `https://candlestore.com/sitemap.xml`
   - Re-submit sitemap
   - Wait 24-48 hours for Google to re-crawl
6. Sidebar → **Coverage** (or **Index** → **Pages**)
7. **Verify:** "Valid" URLs count increasing over time
8. **Verify:** No "Error" status URLs
9. If errors present:
   - Click error type to see affected URLs
   - Common errors:
     - "Server error (5xx)" → Check server logs, fix bugs
     - "Not found (404)" → Remove dead links from sitemap
     - "Redirect" → Add canonical tags, fix redirect chains
10. Click **"Valid"** tab
11. **Verify:** Product pages appearing in valid indexed URLs
12. Sidebar → **Performance**
13. **Verify:** "Total clicks" and "Total impressions" metrics populating
14. **Verify:** Data starts showing within 2-3 days of sitemap submission
15. Click **"Queries"** tab
16. **Verify:** Keywords appearing that match your products
17. **Verify:** Average position improving over time (starts at 50-100, improves to 20-50, then 10-20)

**Expected Result:**
- Sitemap submitted successfully
- Coverage report shows valid indexed pages
- Performance data populating within 48-72 hours
- No critical errors in coverage report

**Pass/Fail Criteria:**
- ✅ Pass: Sitemap success status, pages indexed, performance data appearing
- ❌ Fail: Sitemap errors, no pages indexed, no performance data after 7 days

---

### Verification Step 8: Check Page Speed and Core Web Vitals

**Objective:** Ensure product pages meet Google's performance requirements

**Steps:**
1. Navigate to PageSpeed Insights: https://pagespeed.web.dev/
2. Enter product page URL: `https://candlestore.com/products/lavender-dreams-candle`
3. Click **"Analyze"**
4. Wait 30-60 seconds for analysis
5. **Mobile Results:**
   - **Verify:** Performance score >90 (green)
   - **Verify:** LCP (Largest Contentful Paint) <2.5s
   - **Verify:** FID (First Input Delay) <100ms
   - **Verify:** CLS (Cumulative Layout Shift) <0.1
6. **Desktop Results:**
   - **Verify:** Performance score >95 (green)
   - **Verify:** All Core Web Vitals in green range
7. **SEO Audit:**
   - **Verify:** SEO score 100/100 (green)
   - **Verify:** Meta description check: ✓ (green)
   - **Verify:** Title check: ✓ (green)
   - **Verify:** Image alt attributes: ✓ (green)
   - **Verify:** Links are crawlable: ✓ (green)
8. **Accessibility Audit:**
   - **Verify:** Accessibility score >90
   - **Verify:** Alt text present on all images
   - **Verify:** Heading hierarchy correct (H1 → H2 → H3)
9. If performance scores low (<90):
   - Review "Opportunities" section
   - Common issues:
     - "Properly size images" → Compress images, use WebP
     - "Eliminate render-blocking resources" → Defer CSS/JS
     - "Reduce server response time" → Optimize database queries
10. Fix issues and re-test
11. **Verify:** Scores improve after fixes

**Expected Result:**
- Mobile performance >90
- Desktop performance >95
- SEO audit 100/100
- Core Web Vitals all green
- Accessibility >90

**Pass/Fail Criteria:**
- ✅ Pass: All scores in green/good range, Core Web Vitals pass
- ❌ Fail: Performance <70, LCP >4s, SEO issues detected

---

### Verification Step 9: Test Internal Linking and Breadcrumbs

**Objective:** Verify internal links distribute page authority correctly

**Steps:**
1. Navigate to product page: `https://localhost:5002/products/lavender-dreams-candle`
2. **Verify:** Breadcrumb navigation displays at top:
   ```
   Home > Soy Candles > Lavender Dreams Candle
   ```
3. Click "Home" breadcrumb link
4. **Verify:** Navigates to homepage
5. Click browser back button
6. Click "Soy Candles" breadcrumb link
7. **Verify:** Navigates to category page
8. Return to product page
9. Right-click → View Page Source
10. Search for `<script type="application/ld+json"`
11. Find BreadcrumbList schema
12. **Verify:** Schema includes all breadcrumb levels:
    ```json
    {
      "@type": "BreadcrumbList",
      "itemListElement": [
        {"position": 1, "name": "Home", "item": "https://candlestore.com"},
        {"position": 2, "name": "Soy Candles", "item": "https://candlestore.com/categories/soy-candles"},
        {"position": 3, "name": "Lavender Dreams Candle"}
      ]
    }
    ```
13. Scroll to "Related Products" section on product page
14. **Verify:** 3-5 related products linked
15. Click related product link
16. **Verify:** Navigates to related product page
17. **Verify:** URL is SEO-friendly slug (not numeric ID)
18. Return to original product page
19. Scroll to product description
20. **Verify:** Description includes internal links to:
    - Related products
    - Categories
    - Blog posts (if applicable)
21. **Verify:** Anchor text descriptive (not "click here")
22. Example good anchor text:
    - ✅ "Our Wick Trimmer Tool makes this easy"
    - ✅ "Browse our full Soy Candles collection"
    - ❌ "Click here for more products"

**Expected Result:**
- Breadcrumbs display and functional
- BreadcrumbList schema present
- 3-5 internal links per product page
- Descriptive anchor text
- SEO-friendly URLs

**Pass/Fail Criteria:**
- ✅ Pass: Breadcrumbs work, schema present, internal links functional
- ❌ Fail: No breadcrumbs, broken links, poor anchor text

---

### Verification Step 10: Monitor Organic Search Traffic Growth

**Objective:** Track SEO performance over first 30-90 days

**Steps:**

**Week 1-2 (Indexing Phase):**
1. Google Search Console → Coverage
2. **Verify:** URLs discovered increasing daily
3. **Target:** 50% of product pages indexed within 14 days
4. If indexing slow:
   - Submit individual URLs via URL Inspection tool
   - Check for crawl errors in Coverage report

**Week 3-4 (Early Rankings):**
1. Google Search Console → Performance
2. **Verify:** Impressions appearing (500-2,000/month)
3. **Verify:** First clicks appearing (5-20/month)
4. **Verify:** Average position 30-50 (page 3-5)
5. **Target:** At least 5 keywords ranking in top 100

**Month 2-3 (Growth Phase):**
1. Track keyword rankings:
   - "Handmade candles [your city]" - Target position 10-20
   - Long-tail keywords ("lavender soy candle for sleep") - Target position 5-15
2. **Verify:** Impressions 2,000-10,000/month
3. **Verify:** Clicks 50-200/month
4. **Verify:** CTR 2-5%
5. **Verify:** Average position improving to 15-25 (page 2-3)

**Month 4-6 (Maturity Phase):**
1. **Target:** 10+ keywords in top 20
2. **Target:** Organic traffic 300-500 sessions/month
3. **Target:** Organic conversions 8-12% (higher than paid ads)
4. **Target:** 3-5 backlinks from quality sites
5. Create monthly SEO report:
   ```
   Month 6 SEO Report:
   • Organic Sessions: 487 (+42% vs Month 5)
   • Organic Revenue: $2,340 (+38% vs Month 5)
   • Keywords in Top 10: 8
   • Keywords in Top 20: 23
   • Average Position: 18.4 (vs 24.1 Month 5)
   • Backlinks: 7 (3 new this month)
   • Domain Authority: 22 (vs 8 at start)
   ```

**Expected Result:**
- Steady growth in impressions, clicks, rankings
- 10+ keywords ranking in top 20 by month 6
- Organic traffic 300-500 sessions/month by month 6
- Conversion rate 8-12% from organic traffic

**Pass/Fail Criteria:**
- ✅ Pass: Growth trajectory positive, keywords ranking, traffic increasing
- ❌ Fail: No growth after 90 days, no keywords in top 50, zero organic traffic

---

## 7. Implementation Prompt for Claude

Use this comprehensive guide to implement SEO optimization autonomously.

### Implementation Overview

You are implementing comprehensive SEO optimization for the CandleStore e-commerce platform. This includes technical SEO (meta tags, structured data, sitemaps), on-page optimization (URL slugs, heading hierarchy, alt text), and content optimization (keyword integration, internal linking).

**Complexity:** 8 Fibonacci points (Medium-High)

**Dependencies:**
- Task 011: Product API Endpoints (Product entity must exist)
- Task 012: Category Management (Category entity must exist)
- Task 014: Product Images (Image entity with alt text)

**Integration Points:**
- Task 027: Google Analytics Tracking (tracks organic search traffic)
- Task 028: CDN Cloudflare Setup (improves Core Web Vitals for SEO)

### Step 1: Update Domain Entities

**1.1 Add SEO Properties to Product Entity**

Update `src/CandleStore.Domain/Entities/Product.cs`:

```csharp
namespace CandleStore.Domain.Entities;

public class Product : BaseEntity
{
    // Existing properties...
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // NEW: SEO properties
    public string Slug { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? FocusKeyword { get; set; }
    public bool IsPublished { get; set; } = false;  // Only published products in sitemap
}
```

**1.2 Add SEO Properties to Category Entity**

Update `src/CandleStore.Domain/Entities/Category.cs`:

```csharp
namespace CandleStore.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // NEW: SEO properties
    public string Slug { get; set; } = string.Empty;
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}
```

**1.3 Create Database Migration**

```bash
cd src/CandleStore.Infrastructure
dotnet ef migrations add AddSeoFieldsToProductAndCategory --startup-project ../CandleStore.Api
dotnet ef database update --startup-project ../CandleStore.Api
```

### Step 2: Create Configuration Models

Create `src/CandleStore.Application/Configuration/SeoSettings.cs`:

```csharp
namespace CandleStore.Application.Configuration;

public class SeoSettings
{
    public const string SectionName = "Seo";

    public string SiteName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string DefaultTitle { get; set; } = string.Empty;
    public string DefaultDescription { get; set; } = string.Empty;
    public string TwitterHandle { get; set; } = string.Empty;
    public string FacebookAppId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationLogo { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = "US";
}
```

Update `src/CandleStore.Api/appsettings.json`:

```json
{
  "Seo": {
    "SiteName": "CandleStore",
    "Domain": "https://candlestore.com",
    "DefaultTitle": "Handmade Soy Candles | CandleStore",
    "DefaultDescription": "Shop handmade soy candles made with natural ingredients. Hand-poured in Eugene, Oregon. 40-50 hour burn time. Free shipping over $50.",
    "TwitterHandle": "@candlestore",
    "FacebookAppId": "",
    "OrganizationName": "CandleStore LLC",
    "OrganizationLogo": "https://candlestore.com/images/logo.png",
    "ContactEmail": "hello@candlestore.com",
    "ContactPhone": "+1-541-555-1234",
    "StreetAddress": "123 Main St",
    "City": "Eugene",
    "State": "OR",
    "ZipCode": "97401",
    "Country": "US"
  }
}
```

### Step 3: Create SEO Services

**3.1 Slug Generator Service**

Create `src/CandleStore.Application/Services/SlugGenerator.cs`:

```csharp
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CandleStore.Application.Services;

public class SlugGenerator
{
    public string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Remove diacritics (accents)
        text = RemoveDiacritics(text);

        // Replace spaces with hyphens
        text = Regex.Replace(text, @"\s", "-");

        // Remove invalid characters
        text = Regex.Replace(text, @"[^a-z0-9\-]", "");

        // Remove multiple consecutive hyphens
        text = Regex.Replace(text, @"-{2,}", "-");

        // Trim hyphens from start and end
        text = text.Trim('-');

        return text;
    }

    private string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
```

**3.2 SEO Service (Meta Tags)**

Create `src/CandleStore.Application/Services/SeoService.cs`:

```csharp
using CandleStore.Application.Configuration;
using CandleStore.Application.DTOs.Seo;
using CandleStore.Domain.Entities;
using Microsoft.Extensions.Options;

namespace CandleStore.Application.Services;

public class SeoService
{
    private readonly SeoSettings _settings;

    public SeoService(IOptions<SeoSettings> settings)
    {
        _settings = settings.Value;
    }

    public MetaTagsDto GenerateMetaTags(Product product)
    {
        var title = product.MetaTitle ?? GenerateProductTitle(product.Name);
        var description = product.MetaDescription ?? GenerateProductDescription(product.Description);

        return new MetaTagsDto
        {
            Title = TruncateTitle(title),
            Description = TruncateDescription(description),
            CanonicalUrl = $"{_settings.Domain}/products/{product.Slug}",
            OgTitle = TruncateTitle(title),
            OgDescription = TruncateDescription(description),
            OgUrl = $"{_settings.Domain}/products/{product.Slug}",
            OgType = "product",
            OgImage = product.ProductImages?.FirstOrDefault()?.ImageUrl,
            TwitterCard = "summary_large_image",
            TwitterTitle = TruncateTitle(title),
            TwitterDescription = TruncateDescription(description),
            TwitterImage = product.ProductImages?.FirstOrDefault()?.ImageUrl
        };
    }

    public MetaTagsDto GenerateMetaTags(Category category)
    {
        var title = category.MetaTitle ?? $"{category.Name} - Handmade & Natural | {_settings.SiteName}";
        var description = category.MetaDescription ?? TakeFirstSentence(category.Description, 160);

        return new MetaTagsDto
        {
            Title = TruncateTitle(title),
            Description = TruncateDescription(description),
            CanonicalUrl = $"{_settings.Domain}/categories/{category.Slug}"
        };
    }

    private string GenerateProductTitle(string productName)
    {
        var title = $"{productName} - Handmade Soy | {_settings.SiteName}";
        return TruncateTitle(title);
    }

    private string GenerateProductDescription(string description)
    {
        return TakeFirstSentence(description, 160);
    }

    private string TruncateTitle(string title)
    {
        if (title.Length <= 60)
            return title;

        // Truncate at word boundary before 60 chars
        var truncated = title.Substring(0, 57).TrimEnd();
        return truncated + "...";
    }

    private string TruncateDescription(string description)
    {
        if (description.Length <= 160)
            return description;

        // Truncate at word boundary before 160 chars
        var truncated = description.Substring(0, 157).TrimEnd();
        return truncated + "...";
    }

    private string TakeFirstSentence(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var sentences = text.Split(new[] { '. ', '! ', '? ' }, StringSplitOptions.RemoveEmptyEntries);
        var result = sentences.FirstOrDefault() ?? text;

        if (result.Length > maxLength)
            result = result.Substring(0, maxLength - 3) + "...";

        return result;
    }
}
```

**3.3 Structured Data Service**

Create `src/CandleStore.Application/Services/StructuredDataService.cs`:

```csharp
using CandleStore.Application.Configuration;
using CandleStore.Domain.Entities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CandleStore.Application.Services;

public class StructuredDataService
{
    private readonly SeoSettings _settings;

    public StructuredDataService(IOptions<SeoSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateProductSchema(Product product)
    {
        var schema = new
        {
            context = "https://schema.org",
            type = "Product",
            name = product.Name,
            image = product.ProductImages?.Select(img => img.ImageUrl).ToArray(),
            description = product.Description,
            sku = product.SKU,
            brand = new
            {
                type = "Brand",
                name = _settings.SiteName
            },
            offers = new
            {
                type = "Offer",
                url = $"{_settings.Domain}/products/{product.Slug}",
                priceCurrency = "USD",
                price = product.Price.ToString("F2"),
                availability = product.StockQuantity > 0
                    ? "https://schema.org/InStock"
                    : "https://schema.org/OutOfStock",
                itemCondition = "https://schema.org/NewCondition"
            },
            aggregateRating = product.Reviews?.Any() == true
                ? new
                {
                    type = "AggregateRating",
                    ratingValue = product.Reviews.Average(r => r.Rating).ToString("F2"),
                    reviewCount = product.Reviews.Count,
                    bestRating = 5,
                    worstRating = 1
                }
                : null
        };

        return JsonConvert.SerializeObject(schema, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.None
        });
    }

    public string GenerateOrganizationSchema()
    {
        var schema = new
        {
            context = "https://schema.org",
            type = "Organization",
            name = _settings.OrganizationName,
            url = _settings.Domain,
            logo = _settings.OrganizationLogo,
            contactPoint = new
            {
                type = "ContactPoint",
                telephone = _settings.ContactPhone,
                contactType = "Customer Service",
                email = _settings.ContactEmail,
                availableLanguage = "English"
            },
            address = new
            {
                type = "PostalAddress",
                streetAddress = _settings.StreetAddress,
                addressLocality = _settings.City,
                addressRegion = _settings.State,
                postalCode = _settings.ZipCode,
                addressCountry = _settings.Country
            }
        };

        return JsonConvert.SerializeObject(schema, Formatting.None);
    }

    public string GenerateBreadcrumbSchema(List<BreadcrumbItem> breadcrumbs)
    {
        var schema = new
        {
            context = "https://schema.org",
            type = "BreadcrumbList",
            itemListElement = breadcrumbs.Select((item, index) => new
            {
                type = "ListItem",
                position = index + 1,
                name = item.Name,
                item = item.Url
            }).ToArray()
        };

        return JsonConvert.SerializeObject(schema, Formatting.None);
    }
}
```

**3.4 Sitemap Generator**

Create `src/CandleStore.Application/Services/SitemapGenerator.cs`:

```csharp
using CandleStore.Application.Configuration;
using CandleStore.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;
using System.Xml;

namespace CandleStore.Application.Services;

public class SitemapGenerator
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SeoSettings _settings;

    public SitemapGenerator(IUnitOfWork unitOfWork, IOptions<SeoSettings> settings)
    {
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
    }

    public async Task<string> GenerateSitemapAsync()
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8
        };

        using (var writer = XmlWriter.Create(sb, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            // Homepage
            WriteUrl(writer, _settings.Domain, DateTime.UtcNow, "daily", "1.0");

            // Products
            var products = await _unitOfWork.Products.GetAllPublishedAsync();
            foreach (var product in products)
            {
                WriteUrl(writer,
                    $"{_settings.Domain}/products/{product.Slug}",
                    product.UpdatedAt,
                    "weekly",
                    "0.8");
            }

            // Categories
            var categories = await _unitOfWork.Categories.GetAllAsync();
            foreach (var category in categories)
            {
                WriteUrl(writer,
                    $"{_settings.Domain}/categories/{category.Slug}",
                    category.UpdatedAt,
                    "weekly",
                    "0.6");
            }

            writer.WriteEndElement(); // urlset
            writer.WriteEndDocument();
        }

        return sb.ToString();
    }

    private void WriteUrl(XmlWriter writer, string loc, DateTime lastmod, string changefreq, string priority)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", loc);
        writer.WriteElementString("lastmod", lastmod.ToString("yyyy-MM-dd"));
        writer.WriteElementString("changefreq", changefreq);
        writer.WriteElementString("priority", priority);
        writer.WriteEndElement();
    }
}
```

### Step 4: Create DTOs

Create `src/CandleStore.Application/DTOs/Seo/MetaTagsDto.cs`:

```csharp
namespace CandleStore.Application.DTOs.Seo;

public class MetaTagsDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CanonicalUrl { get; set; } = string.Empty;
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? OgUrl { get; set; }
    public string? OgType { get; set; }
    public string? OgImage { get; set; }
    public string? TwitterCard { get; set; }
    public string? TwitterTitle { get; set; }
    public string? TwitterDescription { get; set; }
    public string? TwitterImage { get; set; }
}
```

### Step 5: Create Sitemap and Robots.txt Controllers

Create `src/CandleStore.Api/Controllers/SitemapController.cs`:

```csharp
using CandleStore.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CandleStore.Api.Controllers;

[ApiController]
public class SitemapController : ControllerBase
{
    private readonly SitemapGenerator _sitemapGenerator;

    public SitemapController(SitemapGenerator sitemapGenerator)
    {
        _sitemapGenerator = sitemapGenerator;
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> GetSitemap()
    {
        var xml = await _sitemapGenerator.GenerateSitemapAsync();
        return Content(xml, "application/xml", Encoding.UTF8);
    }
}
```

Create `/wwwroot/robots.txt`:

```
User-agent: *
Allow: /
Disallow: /admin/
Disallow: /cart/
Disallow: /checkout/
Disallow: /account/

Sitemap: https://candlestore.com/sitemap.xml
```

### Step 6: Register Services in Dependency Injection

Update `src/CandleStore.Api/Program.cs`:

```csharp
// Configure SEO settings
builder.Services.Configure<SeoSettings>(
    builder.Configuration.GetSection(SeoSettings.SectionName));

// Register SEO services
builder.Services.AddScoped<SlugGenerator>();
builder.Services.AddScoped<SeoService>();
builder.Services.AddScoped<StructuredDataService>();
builder.Services.AddScoped<SitemapGenerator>();

// Enable static files for robots.txt
app.UseStaticFiles();
```

### Step 7: Update Product Service with Slug Generation

Update `src/CandleStore.Application/Services/ProductService.cs`:

```csharp
public async Task<ProductDto> CreateProductAsync(CreateProductDto dto)
{
    // Auto-generate slug from product name
    if (string.IsNullOrEmpty(dto.Slug))
    {
        dto.Slug = _slugGenerator.GenerateSlug(dto.Name);
    }

    // Ensure slug is unique
    var existingProduct = await _unitOfWork.Products.GetBySlugAsync(dto.Slug);
    if (existingProduct != null)
    {
        // Append number for uniqueness
        dto.Slug = $"{dto.Slug}-{DateTime.UtcNow.Ticks}";
    }

    var product = _mapper.Map<Product>(dto);
    await _unitOfWork.Products.AddAsync(product);
    await _unitOfWork.SaveChangesAsync();

    return _mapper.Map<ProductDto>(product);
}
```

### Step 8: Create Blazor SEO Component

Create `src/CandleStore.Storefront/Components/Shared/SeoHead.razor`:

```razor
@using CandleStore.Application.DTOs.Seo
@inject CandleStore.Application.Services.SeoService SeoService
@inject CandleStore.Application.Services.StructuredDataService StructuredDataService

@if (MetaTags != null)
{
    <PageTitle>@MetaTags.Title</PageTitle>
    <meta name="description" content="@MetaTags.Description" />
    <link rel="canonical" href="@MetaTags.CanonicalUrl" />

    @if (!string.IsNullOrEmpty(MetaTags.OgTitle))
    {
        <meta property="og:title" content="@MetaTags.OgTitle" />
        <meta property="og:description" content="@MetaTags.OgDescription" />
        <meta property="og:url" content="@MetaTags.OgUrl" />
        <meta property="og:type" content="@MetaTags.OgType" />
        @if (!string.IsNullOrEmpty(MetaTags.OgImage))
        {
            <meta property="og:image" content="@MetaTags.OgImage" />
        }
    }

    @if (!string.IsNullOrEmpty(MetaTags.TwitterCard))
    {
        <meta name="twitter:card" content="@MetaTags.TwitterCard" />
        <meta name="twitter:title" content="@MetaTags.TwitterTitle" />
        <meta name="twitter:description" content="@MetaTags.TwitterDescription" />
        @if (!string.IsNullOrEmpty(MetaTags.TwitterImage))
        {
            <meta name="twitter:image" content="@MetaTags.TwitterImage" />
        }
    }
}

@if (!string.IsNullOrEmpty(StructuredData))
{
    <script type="application/ld+json">@((MarkupString)StructuredData)</script>
}

@code {
    [Parameter] public MetaTagsDto? MetaTags { get; set; }
    [Parameter] public string? StructuredData { get; set; }
}
```

Use in product page `ProductDetail.razor`:

```razor
@page "/products/{slug}"
@using CandleStore.Application.Services

<SeoHead MetaTags="@_metaTags" StructuredData="@_productSchema" />

<h1>@_product.Name</h1>
<!-- Rest of product page -->

@code {
    [Parameter] public string Slug { get; set; } = string.Empty;

    private Product _product;
    private MetaTagsDto _metaTags;
    private string _productSchema;

    protected override async Task OnInitializedAsync()
    {
        _product = await ProductService.GetBySlugAsync(Slug);
        _metaTags = SeoService.GenerateMetaTags(_product);
        _productSchema = StructuredDataService.GenerateProductSchema(_product);
    }
}
```

### Success Criteria

Your implementation is complete when:

1. ✅ Product and category entities have Slug, MetaTitle, MetaDescription properties
2. ✅ Slugs auto-generate from names (lowercase, hyphens, unique)
3. ✅ Meta tags render in HTML head on all pages
4. ✅ Product schema (JSON-LD) renders on product pages
5. ✅ Sitemap.xml accessible and includes all published products
6. ✅ robots.txt blocks admin panel, allows products
7. ✅ Google Rich Results Test validates product schema
8. ✅ PageSpeed Insights scores >90 mobile, >95 desktop
9. ✅ All tests pass (unit, integration, E2E)
10. ✅ Google Search Console accepts sitemap without errors

---

**END OF TASK 025**

