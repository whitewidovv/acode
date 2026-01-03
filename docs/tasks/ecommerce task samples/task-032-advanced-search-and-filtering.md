# Task 032: Advanced Search and Filtering

**Priority:** 32 / 36
**Tier:** B
**Complexity:** 8 Fibonacci points
**Phase:** Phase 10 - Advanced Features
**Dependencies:** Task 021

---

## Description

The Advanced Search and Filtering system transforms product discovery from basic keyword matching into an intelligent, faceted search experience that helps customers find exactly what they want quickly and efficiently. This feature implements autocomplete search suggestions, multi-select category and attribute filters, price range sliders, faceted navigation with result counts, search history tracking, synonym support, and "did you mean" spell-check suggestions. Customers can apply multiple filters simultaneously, see real-time result counts for each filter option, and sort results by price, relevance, rating, or newness. The search interface provides visual feedback through highlighted search terms in results, active filter pills that can be quickly removed, and intelligent "no results" messaging that suggests alternative searches or related products when queries return zero results.

From a technical perspective, the system leverages Entity Framework Core's full-text search capabilities (SQL Server CONTAINS/FREETEXT or PostgreSQL ts_vector) for basic implementations, with optional Elasticsearch integration for stores with 10,000+ products requiring sub-100ms response times. The search query builder constructs dynamic LINQ expressions based on selected filters, applying category filters via JOIN queries, price filters via range predicates, rating filters via aggregate calculations on review data, and stock availability filters via inventory checks. Autocomplete suggestions utilize trie-based prefix matching against product names and popular search terms, returning results in under 50ms to provide responsive typeahead experience. Search result highlighting uses regex replacement to wrap matching terms in `<mark>` tags for visual emphasis. Faceted navigation calculates result counts for each filter option by executing COUNT queries against the filtered result set, with aggressive caching (5-minute expiry) to prevent repeated expensive aggregate queries.

The implementation creates a SearchService in the Application layer with methods: SearchProductsAsync (executes filtered search with pagination), GetAutocompleteSuggestionsAsync (prefix matching for typeahead), GetSearchFacetsAsync (calculates filter counts), GetPopularSearchesAsync (tracks and returns most common queries), and GetDidYouMeanSuggestionsAsync (Levenshtein distance-based spell correction). A SearchQuery entity tracks customer searches with fields: SearchTerm, CustomerId (nullable), SessionId, SearchedAt, ResultCount, allowing analytics on what customers search for and conversion rates per search term. Filter state management uses query string parameters for SEO-friendly URLs (e.g., /products?category=candles&minPrice=20&maxPrice=40) enabling bookmarking and sharing of filtered views. The frontend Blazor component implements debounced autocomplete (300ms delay after typing stops), multi-select checkbox filters with "show more" expansion for long lists, and dual-handle range sliders for price selection.

**Business Value:**

- **For Store Owner Sarah:** Advanced search reduces product discovery time by 65%, directly increasing conversion rates from 3.2% to 4.7% as customers find relevant products faster. Faceted navigation reveals customer preferences—if 70% of searchers filter by "lavender" scent, Sarah knows to expand lavender product lines. Search analytics identify gaps in product catalog: if customers frequently search "soy wax candles" but result count is low, Sarah needs more soy products. "No results" tracking shows 847 searches for "citronella candles" monthly with zero results, representing $12,000 in lost revenue that prompts Sarah to add citronella products. Search-to-purchase conversion tracking (customers who search convert at 8.3% vs 2.9% for non-searchers) proves search investment ROI. Synonym support prevents lost sales—customers searching "smell" find products tagged with "scent" or "fragrance", capturing 15% more searches that would otherwise return zero results.

- **For Customer Alex:** Autocomplete saves typing effort and guides Alex toward existing products (typing "lav" suggests "Lavender Dreams", "Lavender Vanilla", "Lavender Mint"). Multi-select filters enable precise discovery: Alex filters by "Floral" category + "4-6 hour burn time" + "$20-$30 price" + "4+ star rating" to find exactly 6 products matching all criteria instead of browsing 120 products manually. Filter counts show availability before clicking (12 products in Floral category, 4 products with 4+ stars) preventing frustration of selecting filters that yield zero results. Active filter pills provide clear visual feedback on applied filters with one-click removal to backtrack. Search result highlighting shows *why* each product matched—searching "ocean beach candle" highlights "ocean" in Ocean Breeze product name and "beach house" in description. Sort options let Alex compare by price (budget-conscious), rating (quality-focused), or newness (seeking latest releases). "Did you mean 'lavender'?" suggestions rescue misspelled searches ("lavendr", "lavander") that would otherwise return zero results and cause abandonment.

- **For Developer David:** The search architecture uses repository pattern with ISearchRepository abstracting search implementation, enabling later migration from SQL full-text search to Elasticsearch without changing service layer code. Query builder pattern constructs dynamic LINQ expressions safely without SQL injection risk. Facet calculation uses projection queries (SELECT CategoryId, COUNT(*) GROUP BY CategoryId) rather than loading entire result set into memory, maintaining performance with 100,000+ products. Autocomplete response time target of <50ms requires database indexing strategy: composite index on (Name, IsActive) for prefix searches, full-text index on (Name, Description, LongDescription) for content searches. Caching strategy uses memory cache for popular searches (80% hit rate) and distributed Redis cache for cross-instance consistency in load-balanced deployments. The system is testable—mock ISearchRepository for unit tests, in-memory database for integration tests, Playwright for E2E autocomplete behavior testing.

**Key Features:**

- **Autocomplete Search Suggestions:** Typeahead dropdown showing 5-8 product name matches as user types (debounced 300ms)
- **Multi-Select Category Filters:** Checkbox list allowing selection of multiple categories simultaneously with OR logic
- **Price Range Slider:** Dual-handle slider for setting minimum and maximum price boundaries
- **Faceted Navigation:** Filter options with result counts (e.g., "Floral (23)", "Vanilla (18)") updating dynamically
- **Rating Filter:** Star rating filter (e.g., "4 stars & up") with review count display
- **Stock Availability Filter:** "In Stock Only" checkbox excluding out-of-stock products
- **Size/Variant Filters:** Filter by candle size (4 oz, 8 oz, 16 oz) or other variant attributes
- **Tag/Scent Filters:** Multi-select filters for product tags like "soy wax", "hand-poured", "organic"
- **Search Result Highlighting:** Matching search terms highlighted in yellow/bold in product names and descriptions
- **Sort Options:** Sort by relevance (default), price ascending/descending, rating, newest, best-selling
- **Active Filter Pills:** Visual chips showing applied filters with X icon for quick removal
- **Search History:** Dropdown showing customer's last 5 searches for quick re-searching
- **Popular Searches:** Display top 10 most common search terms as clickable suggestions on search page
- **"Did You Mean" Suggestions:** Spell-check offering corrected search term when query returns few/no results
- **Synonym Support:** Searching "smell" finds products with "scent", "fragrance", "aroma" in attributes
- **No Results Messaging:** When search returns zero results, suggest alternative searches or related products

**Technical Approach:**

The implementation creates SearchService in Application layer with dependencies on IProductRepository, ISearchHistoryRepository, IMemoryCache, and IMapper. SearchProductsAsync method builds dynamic LINQ query starting with base `_productRepository.GetQueryable().Where(p => p.IsActive)`, then chains additional Where clauses for each active filter: category filter uses `query.Categories.Contains(p.CategoryId)`, price range uses `p.Price >= minPrice && p.Price <= maxPrice`, rating filter joins Reviews table and calculates average `p.Reviews.Average(r => r.Rating) >= minRating`, stock filter uses `p.StockQuantity > 0`, tag filter uses `p.ProductTags.Any(t => query.Tags.Contains(t.TagId))`. Text search uses EF.Functions.FreeText for SQL Server or EF.Functions.ToTsVector for PostgreSQL full-text indexing. Sort logic uses switch expression mapping sort parameter to OrderBy/OrderByDescending clauses. Pagination applies Skip/Take after all filters and sorting. GetAutocompleteSuggestionsAsync queries products with `EF.Functions.Like(p.Name, $"{term}%")` prefix match, limited to 8 results, cached for 5 minutes per term prefix. GetSearchFacetsAsync executes parallel COUNT queries for each facet dimension: categories grouped by CategoryId, price ranges bucketed into predefined ranges (<$20, $20-$40, $40-$60, $60+), ratings grouped by star level. Result counts cached with 5-minute expiry keyed by search term hash to prevent recalculation on every keystroke. SearchHistory entity persists every search with CustomerId/SessionId for analytics and personalized history. SynonymService maps common synonyms (smell → scent, aroma, fragrance) expanding search query to include synonym terms in OR clause. DidYouMean service calculates Levenshtein distance between search term and known product names/tags, suggesting alternatives if distance <3 and original query returns <2 results.

Blazor SearchPage component contains MudTextField with OnInput event calling autocomplete API after 300ms debounce timer, displaying MudAutocomplete dropdown with results. Filter sidebar uses MudCheckBox components for category filters, MudRangeSlider for price, MudRating for star filter, all bound to SearchFilterModel. Applying filters constructs query string URL like `/search?q=lavender&category=floral&minPrice=20&maxPrice=40&minRating=4&sort=price_asc` enabling SEO indexing and bookmarking. Active filters displayed as MudChip components with CloseIcon event handler removing filter and re-executing search. Search results rendered in product grid with search term highlighting via Regex.Replace wrapping matches in `<mark class="highlight">` tags. No results view shows "No products match 'xyz'" message with MudButton suggestions for popular categories or "Clear Filters" action.

**Integration:**

- **Task 013 (Product Management API):** Search queries IProductRepository to fetch product data including Name, Description, Price, StockQuantity, CategoryId, Images. Only active products (IsActive = true) appear in search results. Product entity must have full-text index on searchable fields for performance.

- **Task 021 (Order Management API):** "Best Selling" sort option queries OrderItems to rank products by total quantity sold in last 30 days. Search analytics track search-to-purchase conversion by correlating SearchHistory records with subsequent Order records.

- **Task 029 (Reviews and Ratings):** Rating filter queries Review table to calculate average star rating per product. Search results display product rating and review count alongside other metadata.

- **Task 031 (Product Recommendations Engine):** "No results" page displays recommended products using trending products API as fallback. Search history data feeds personalized recommendations algorithm.

**Constraints:**

- Full-text search performance degrades with >50,000 products without Elasticsearch; requires proper database indexing strategy
- Autocomplete must respond in <50ms to feel instant; requires aggressive caching and prefix index optimization
- Facet count calculation expensive (multiple GROUP BY queries); requires caching and potential pre-computation for popular categories
- Search result highlighting regex replacement can cause XSS vulnerabilities if not properly HTML-encoded; must sanitize input and output
- Multi-select filters with >20 options create cluttered UI; requires "Show More" expansion or search-within-filter functionality
- Synonym mapping requires manual curation; automatic synonym detection (NLP-based) beyond scope of MVP
- "Did you mean" suggestions unreliable for product-specific terminology (e.g., "Ylang Ylang" might incorrectly suggest "Yang Yang"); requires domain-specific dictionary
- Search query length limited to 200 characters to prevent abuse and performance degradation
- Maximum 50 filters applicable simultaneously to prevent combinatorial explosion of query complexity

---

## Use Cases

### Use Case 1: Alex (Customer) Uses Faceted Search to Find Perfect Gift

**Scenario:** Alex is shopping for a birthday gift for her mother who loves floral scents but is sensitive to strong fragrances. Alex wants a medium-sized candle (not too small, not too large), high-quality (4+ star rating), and budget-friendly ($20-$30). Without advanced search, she must manually browse through 120 products, click into each product detail page, check size/price/rating, and determine if it's floral-scented.

**Without This Feature:**
Alex lands on "All Products" page showing 120 candles in random order. She sees Vanilla Bourbon Candle first (doesn't match—not floral). She clicks next page, sees Ocean Breeze (doesn't match—aquatic not floral). She tries category navigation clicking "Floral Scents" which narrows to 28 products, but they're all sizes and price ranges mixed together. She clicks into "Rose Garden 4oz" product detail page, sees it's $14.99 (too cheap, might be low quality), goes back, clicks "Lavender Dreams 16oz", sees it's $42.99 (over budget), goes back. After 12 minutes of clicking through product detail pages, she finds "Peony Bloom 8oz" at $26.99 with 4.5 stars—perfect! But she's frustrated and exhausted from the manual search process. She wonders if there are other good options she missed. Bounce rate for customers who can't find products quickly is 67%, and Alex represents the 33% who persisted, but her session time of 12 minutes is inefficient.

**With This Feature:**
Alex lands on homepage and clicks "Shop" navigation. The product catalog page displays a search bar and filter sidebar. She types "floral" in search—autocomplete suggests "Floral Scents (Category)" and several product names starting with "Floral". She clicks "Floral Scents (Category)" which loads 28 products. The filter sidebar shows:
- **Categories:** Floral (28 selected), Vanilla (34), Fresh & Clean (18)...
- **Price Range:** Slider showing $12-$65 range. Alex drags slider to $20-$40. Filter count updates to (12 products).
- **Size:** 4oz (5), 8oz (14), 16oz (9). Alex selects "8oz" checkbox. Count updates to (9 products).
- **Rating:** Alex clicks "4 stars & up" filter. Count updates to (6 products).

The main product grid now shows exactly 6 products matching all criteria: Floral category, $20-$40, 8oz, 4+ stars. Alex scans the 6 results visually in grid view without clicking into detail pages. She compares prices and ratings at a glance. She sees "Peony Bloom 8oz - $26.99 ★★★★★ (89 reviews)" and "Jasmine Night 8oz - $28.99 ★★★★☆ (63 reviews)". She clicks "Sort by: Rating" to see highest-rated first. Peony Bloom moves to top position. She adds it to cart. Total time: 2 minutes. Alex feels confident she found the best match because she saw *all* products meeting her criteria. Session time reduced 83% (12min → 2min), frustration eliminated, and Alex discovers 2 other products she adds to wishlist for future purchases.

**Outcome:**
- Customer time-to-purchase reduced from 12 minutes to 2 minutes (83% improvement)
- Conversion rate for filtered searches: 8.9% vs 3.2% for unfiltered browsing (178% increase)
- Filter usage correlates with 2.3x higher average order value ($42 vs $18)—customers who use filters are more intentional and buy additional items
- Bounce rate decreases from 67% to 41% when faceted navigation is available (39% improvement)
- 76% of customers who apply 3+ filters complete purchase vs 28% who browse without filtering
- Customer satisfaction score increases +24 points (post-purchase survey question: "How easy was it to find what you wanted?")
- Wishlist additions from search results: 1.8 products per session vs 0.4 for non-search sessions (customers discover complementary products via filters)

### Use Case 2: Sarah (Store Owner) Analyzes Search Data to Identify Product Gaps

**Scenario:** Sarah notices sales have plateaued despite increasing site traffic. She suspects customers are searching for products she doesn't carry, resulting in zero-result searches and lost revenue. She needs visibility into what customers are searching for and which searches are failing to return results.

**Without This Feature:**
Sarah has no data on customer search behavior. She relies on manual observation—occasionally she overhears customers at craft fairs asking for "citronella candles" or "eucalyptus scents", but she doesn't know if these are statistically significant demands or anecdotal requests. She makes gut-feel decisions about product line expansion: "I'll add some citrus candles because they seem trendy." She launches 3 new citrus products (Orange Zest, Lemon Verbena, Grapefruit Sunrise) and waits 3 months to evaluate sales. Results are disappointing—only 14 units sold across all three products. The products weren't actually in demand; Sarah guessed wrong. She wasted $2,400 on inventory (ingredients, production time, packaging) for products with weak market fit. Meanwhile, customers continue searching for "soy wax candles" (847 searches/month) which Sarah carries but hasn't tagged properly, resulting in zero-result searches and lost sales. Sarah has no visibility into this problem.

**With This Feature:**
Sarah logs into Admin Panel and navigates to Analytics > Search Analytics. She sees a dashboard showing:

**Top Searches (Last 30 Days):**
1. "lavender" - 2,341 searches, 94% result rate, 12.3% conversion
2. "vanilla" - 1,867 searches, 98% result rate, 10.8% conversion
3. "soy wax" - 847 searches, 8% result rate, 1.2% conversion ⚠️
4. "eucalyptus" - 634 searches, 12% result rate, 0.9% conversion ⚠️
5. "citronella" - 512 searches, 0% result rate, 0% conversion 🔴

**Zero-Result Searches:**
- "citronella candles" - 512 searches, $0 revenue (opportunity cost: ~$7,680 at $15 AOV)
- "beeswax candles" - 298 searches, $0 revenue (opportunity cost: ~$4,470)
- "unscented" - 187 searches, $0 revenue (opportunity cost: ~$2,805)

**Low-Result High-Search Terms:**
- "soy wax" - 847 searches but only 8% result rate because products aren't tagged with "soy wax" attribute

Sarah analyzes this data and makes strategic decisions:
1. **Add citronella and beeswax products:** These have clear demand (512 + 298 searches/month) with zero current offerings. Projected revenue: $12,150/month if she captures 50% of searchers at $15 AOV.
2. **Fix "soy wax" tagging:** All her candles are soy-based, but Product.Tags doesn't include "soy wax". She bulk-edits 45 products to add "soy wax" tag. Next month, "soy wax" result rate increases from 8% to 96%, and soy wax search conversion increases from 1.2% to 11.4% (850% improvement). Revenue from soy wax searches increases from $152/month to $1,448/month ($1,296 monthly gain, $15,552 annual gain) with zero new product development—just better tagging.
3. **Deprioritize citrus expansion:** Original plan was to add 5 new citrus products. Search data shows "citrus" only appears 89 times/month (low demand). She cancels citrus expansion, saving $4,200 in development costs.
4. **Create synonym mappings:** Search data shows customers search "smell" (234 times/month) but products use "scent" or "fragrance" terminology, resulting in 67% zero-result rate. She adds synonym mappings: smell → scent, fragrance, aroma. Zero-result rate drops from 67% to 12%.

**Outcome:**
- Sarah identifies $12,150/month revenue opportunity (citronella + beeswax products) via zero-result search analysis
- Fixing soy wax tagging generates $15,552 annual revenue increase with zero production cost—just metadata correction
- Avoids $4,200 wasted investment in citrus products with weak demand signal
- Search-driven product decisions increase new product success rate from 40% to 78% (measured by: % of new products with >100 sales in first 6 months)
- Search analytics become core input to quarterly product roadmap planning—data-driven instead of intuition-driven
- Total annual revenue impact from search insights: +$87,000 (new products + tagging fixes + avoided waste)

### Use Case 3: Guest User Receives "Did You Mean" Suggestion for Misspelled Search

**Scenario:** Jamie is a first-time visitor to Candle Store who heard about "Lavender Dreams" candle from a friend's Instagram post. She types "lavendr dreams" in the search bar (missing 'e' in lavender) and hits Enter. Without spell-check, this exact-match search returns zero results and Jamie assumes the product doesn't exist or the store is out of stock. She bounces from the site and purchases from a competitor.

**Without This Feature:**
Jamie types "lavendr dreams" (misspelling) in search bar and presses Enter. The search executes exact phrase match using `EF.Functions.Like(p.Name, "%lavendr dreams%")` which returns zero results. The page displays "No products found for 'lavendr dreams'" with no helpful suggestions. Jamie doesn't realize she misspelled the query. She thinks "Weird, my friend Sarah said this store has Lavender Dreams candle... maybe they're sold out or discontinued?" She navigates to homepage, tries browsing categories, but with 120 products across 8 categories, she doesn't know where to start. After 30 seconds of aimless clicking, she gives up and Googles "lavender dreams candle" which shows competitor results. She clicks a competitor link (paid ad at top of search results) and purchases Lavender Dreams candle from them for $28.99. Original store loses $24.99 sale plus potential lifetime customer value of $180 due to one-letter typo and lack of spell-check.

**With This Feature:**
Jamie types "lavendr dreams" in search bar and presses Enter. The SearchService executes search query which returns zero results. Before displaying "no results" page, the service calls GetDidYouMeanSuggestionsAsync("lavendr dreams"). This method:
1. Calculates Levenshtein distance between "lavendr" and all product names in database
2. Finds "Lavender Dreams" has distance of 1 (one missing character 'e')
3. Returns suggestion: "Did you mean 'Lavender Dreams'?"

The search results page displays:
```
No products found for "lavendr dreams"

Did you mean: Lavender Dreams?  [Search instead]
```

Jamie sees the suggestion and realizes her typo. She clicks [Search instead] which re-executes search with corrected term "Lavender Dreams". The corrected search returns 1 product: "Lavender Dreams Soy Candle 8oz - $24.99 ★★★★★ (127 reviews)". Jamie sees the product (exactly what her friend recommended), reads reviews, adds to cart, and completes purchase. The spell-check suggestion rescued a bounced session and converted it to a sale. Total session time: 1 minute (vs 30 seconds bounce in previous scenario).

Additionally, if Jamie had typed "lavender" correctly but searched for variant spelling like "lavander" (common misspelling), the system would still correct:
- Input: "lavander candle"
- Suggestion: "Did you mean 'lavender candle'?"
- Result: Customer finds products instead of bouncing

**Outcome:**
- 18% of searches contain misspellings or typos (industry average: 15-20%)
- "Did you mean" suggestions rescue 67% of misspelled searches that would otherwise return zero results
- Bounce rate for zero-result searches decreases from 89% to 34% when spell-check suggestions are present (62% improvement)
- Conversion rate for corrected searches: 6.8% (only slightly lower than correctly-spelled searches at 8.2%)
- Estimated monthly revenue saved from rescued typo searches: $4,680 (18% of 12,000 monthly searches = 2,160 typo searches × 67% rescue rate × 8.2% conversion × $18 AOV = $4,680)
- Annual revenue impact: $56,160 from spell-check feature alone
- Customer frustration eliminated—post-interaction survey shows 87% of customers who used "did you mean" feature rated experience as "helpful" or "very helpful"
- Improved brand perception: customers perceive site as "smart" and "helpful" when search understands intent despite typos
## User Manual Documentation

### Overview

The Advanced Search and Filtering system transforms product discovery from basic keyword matching into an intelligent, faceted search experience. This feature provides customers with powerful tools to find exactly what they're looking for: autocomplete search suggestions that appear as they type, multi-select category and attribute filters, price range sliders, real-time result counts for each filter option, and sort controls. Store owners benefit from search analytics showing what customers search for, which queries return zero results (indicating product gaps), and search-to-purchase conversion rates. Developers interact with search through well-defined API endpoints and repository interfaces, enabling integration with external search engines (Elasticsearch, Algolia) without changing application code.

**When to use this feature:**
- **Customers:** Use search bar on every page to find products by name, description, or attributes. Apply filters to narrow results by category, price, rating, size, or tags. Sort results to compare by price, popularity, or newness.
- **Store Owners:** Monitor search analytics (Admin Panel > Reports > Search Analytics) to understand customer intent, identify zero-result searches (product opportunities), and optimize product tagging based on popular search terms.
- **Developers:** Implement search via ISearchService interface. Extend search with custom facets (brand, burn time, scent family) or integrate external search providers without changing service layer code.

**Key capabilities:**
- Autocomplete typeahead responding in <50ms with 5-8 product name suggestions
- Multi-select checkbox filters for categories, tags, sizes with AND/OR logic
- Dual-handle range slider for price filtering ($0-$100 range)
- Faceted navigation with dynamic result counts ("Floral (23)", "Vanilla (18)")
- Star rating filter (1-5 stars, with "X stars & up" logic)
- Stock availability toggle ("In Stock Only" checkbox)
- Search result highlighting (matching terms wrapped in `<mark>` tags)
- Sort options: Relevance (default), Price (asc/desc), Rating, Newest, Best-Selling
- Active filter pills with one-click removal
- Search history dropdown (last 5 searches per session/customer)
- Popular searches display (top 10 most common queries site-wide)
- "Did you mean" spell-check suggestions using Levenshtein distance
- Synonym support ("smell" finds "scent", "fragrance", "aroma")
- No results messaging with alternative search suggestions or related products
- URL-based filter state (SEO-friendly, bookmarkable, shareable)

---

### Customer Experience: Using Search and Filters

#### Basic Text Search

**Accessing Search:**

Every page in Candle Store displays a search bar in the header navigation. The search bar is always visible and accessible via keyboard shortcut (Ctrl+K or Cmd+K on Mac).

```
┌─────────────────────────────────────────────────────────────┐
│  Candle Store                     [Search products...]  🔍  │
│  Home  Shop  About  Contact               Cart (0)  Login   │
└─────────────────────────────────────────────────────────────┘
```

**Performing a search:**

1. Click in search bar or press Ctrl+K
2. Type search query (e.g., "lavender candle")
3. **Autocomplete dropdown appears after 300ms:**

```
┌──────────────────────────────────────────┐
│  lavender candle                     🔍  │
├──────────────────────────────────────────┤
│  Lavender Dreams Candle (8 oz)           │
│  Lavender Vanilla Candle (12 oz)         │
│  French Lavender Pillar Candle           │
│  Lavender Mint Aromatherapy Candle       │
│  ────────────────────────────────────    │
│  Search for "lavender candle"            │
└──────────────────────────────────────────┘
```

4. Click a suggestion or press Enter to search
5. Navigate to search results page: `/search?q=lavender+candle`

**Search results page:**

```
┌─────────────────────────────────────────────────────────────┐
│  Search results for "lavender candle"          (24 results)  │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐                  ┌─────────────────────┐   │
│  │ Filters     │                  │ Sort: Relevance ▾   │   │
│  ├─────────────┤                  └─────────────────────┘   │
│  │ Categories  │                                            │
│  │ ☑ Floral (18)│                  Active Filters:          │
│  │ ☐ Vanilla(6)│                  [Floral ✕]              │
│  │ ☐ Fresh (8) │                                            │
│  │             │                  ┌───────┐  ┌───────┐     │
│  │ Price Range │                  │       │  │       │     │
│  │ $0━━━━━$100 │                  │Lavender│ │French │     │
│  │ ●─────────● │                  │Dreams │  │Lavender│    │
│  │             │                  │       │  │       │     │
│  │ Rating      │                  │$24.99 │  │$27.99 │     │
│  │ ☐ 4★ & up   │                  │★★★★★  │  │★★★★☆  │     │
│  │ ☐ 3★ & up   │                  └───────┘  └───────┘     │
│  │             │                                            │
│  │ Availability│                  ┌───────┐  ┌───────┐     │
│  │ ☑ In Stock  │                  │Lavender│ │Lavender│    │
│  │             │                  │Vanilla │  │ Mint  │     │
│  │ Size        │                  │       │  │       │     │
│  │ ☐ 4 oz (8)  │                  │$26.99 │  │$22.99 │     │
│  │ ☑ 8 oz (14) │                  │★★★★★  │  │★★★☆☆  │     │
│  │ ☐ 16oz (6)  │                  └───────┘  └───────┘     │
│  └─────────────┘                                            │
│                                   [Load More Results]       │
└─────────────────────────────────────────────────────────────┘
```

**How search results work:**
- Search term "lavender" highlighted in yellow in product names and descriptions
- Result count updates in real-time as filters are applied (24 → 18 when Floral selected)
- Products display in grid (desktop) or stack vertically (mobile)
- Each product card shows: image, name, price, rating, [Add to Cart] button
- Scroll to bottom loads more results (pagination: 24 per page)

---

#### Faceted Filtering

**Multi-Select Category Filter:**

```
┌─────────────────────────────┐
│ Categories                  │
├─────────────────────────────┤
│ ☑ Floral (18)               │  ← Selected (checkbox checked)
│ ☐ Vanilla (34)              │  ← Available (checkbox unchecked)
│ ☐ Fresh & Clean (28)        │
│ ☐ Seasonal (12)             │
│ ☐ Aromatherapy (16)         │
│ ☐ Fruity (9)                │
│ ☑ Woodsy (7)                │  ← Second selection
│ [Show More...]              │  ← Expand to see all categories
└─────────────────────────────┘
```

**How multi-select works:**
1. Click checkbox next to category name
2. Result count updates immediately (18 Floral + 7 Woodsy = 25 total results)
3. Product grid refreshes with filtered products
4. URL updates: `/search?q=lavender&category=floral&category=woodsy`
5. Filter logic: Shows products matching ANY selected category (OR logic)
6. Click [Show More...] to expand collapsed categories (shows all 12 categories)

---

**Price Range Slider:**

```
┌─────────────────────────────┐
│ Price Range                 │
├─────────────────────────────┤
│ $12                   $100  │  ← Current min/max prices
│                             │
│  ●──────────────────────●   │  ← Dual-handle slider
│ $0                      $150│  ← Full range (min/max available)
│                             │
│ Min: $12    Max: $100       │  ← Numeric inputs (manual entry)
│ [Apply]                     │  ← Apply button (updates on release)
└─────────────────────────────┘
```

**How price slider works:**
1. Drag left handle to set minimum price ($12)
2. Drag right handle to set maximum price ($100)
3. Filter applies automatically on handle release (no [Apply] button click needed)
4. Alternatively, type exact values in "Min:" and "Max:" input boxes and press Enter
5. URL updates: `/search?q=lavender&minPrice=12&maxPrice=100`
6. Result count updates: "24 results" → "18 results" (excludes 6 products outside price range)
7. Products outside range are removed from grid

---

**Rating Filter:**

```
┌─────────────────────────────┐
│ Customer Rating             │
├─────────────────────────────┤
│ ☐ ★★★★★ 5 stars (8)         │
│ ☑ ★★★★☆ 4 stars & up (32)   │  ← Selected
│ ☐ ★★★☆☆ 3 stars & up (48)   │
│ ☐ ★★☆☆☆ 2 stars & up (52)   │
│ ☐ ★☆☆☆☆ 1 star & up (54)    │
└─────────────────────────────┘
```

**How rating filter works:**
- Selecting "4 stars & up" shows products with average rating >= 4.0
- Count in parentheses shows number of products meeting that rating threshold
- Cumulative logic: "4 stars & up" includes both 4-star and 5-star products
- Only products with at least 1 review are included (products with 0 reviews excluded)
- Combining with other filters: Rating AND category AND price range all apply simultaneously

---

**Active Filter Pills:**

When filters are applied, active filter "pills" display above search results for easy removal:

```
┌──────────────────────────────────────────────────────────┐
│  Active Filters:                                         │
│  [Floral ✕]  [Woodsy ✕]  [$12-$100 ✕]  [4★ & up ✕]     │
│  Clear All                                               │
└──────────────────────────────────────────────────────────┘
```

**How filter pills work:**
- Each active filter displays as a rounded pill with label and X icon
- Click ✕ to remove that specific filter (results refresh immediately)
- Click "Clear All" to remove all filters and reset to full result set
- Filter pills are clickable/tappable on mobile devices

---

#### Sorting Search Results

**Sort dropdown:**

```
┌───────────────────────────┐
│ Sort: Relevance ▾         │  ← Click to expand
├───────────────────────────┤
│ • Relevance (default)     │  ← Currently selected (bullet point)
│   Price: Low to High      │
│   Price: High to Low      │
│   Rating: High to Low     │
│   Newest First            │
│   Best Selling            │
└───────────────────────────┘
```

**Sort options explained:**

1. **Relevance (default):** Products ordered by search term match quality
   - Exact name match ranks highest
   - Partial name match ranks second
   - Description/tag match ranks third
   - Algorithm: TF-IDF scoring (term frequency - inverse document frequency)

2. **Price: Low to High:** Ascending price order ($12.99 → $149.99)
   - Budget-conscious shoppers find cheapest options first
   - Useful for comparing similar products at different price points

3. **Price: High to Low:** Descending price order ($149.99 → $12.99)
   - Premium/luxury products appear first
   - Signals high quality or large sizes

4. **Rating: High to Low:** Products with highest average star rating first
   - Quality-focused shoppers prioritize well-reviewed products
   - Products with same rating ordered by review count (more reviews = higher rank)

5. **Newest First:** Most recently added products first (CreatedAt descending)
   - Customers seeking latest releases or seasonal products
   - New arrivals appear at top of results

6. **Best Selling:** Products ordered by total units sold (last 30 days)
   - Social proof: popular products must be good
   - Calculated nightly from OrderItems table

---

#### Search History and Popular Searches

**Search history dropdown:**

When clicking in the search bar, recent searches display:

```
┌──────────────────────────────────────────┐
│  [Search products...]                🔍  │
├──────────────────────────────────────────┤
│  Recent Searches                         │
│  ─────────────────────────────────────   │
│  lavender candle          (2 hours ago)  │
│  vanilla soy wax          (1 day ago)    │
│  eucalyptus aromatherapy  (3 days ago)   │
│  ocean breeze large       (1 week ago)   │
│                                          │
│  Popular Searches                        │
│  ─────────────────────────────────────   │
│  lavender  vanilla  rose  citrus         │
│  soy wax   beeswax  unscented            │
└──────────────────────────────────────────┘
```

**How search history works:**
- Stores last 5 searches per customer in ProductSearch table
- For guest users, stores in browser localStorage (SessionId-based)
- Clicking a recent search re-executes that query
- Clear search history via [Clear History] link or clearing browser data
- Recent searches auto-expire after 30 days

**Popular searches:**
- Displays top 10 most frequently searched terms across all users (last 30 days)
- Calculated nightly by background job querying ProductSearch table
- Clicking a popular search executes that query
- Useful for discovery: customers see what others are searching for

---

### Developer Experience: Implementing Search Features

#### Search API Endpoints

**GET /api/search/products**

Execute product search with filters and sorting.

**Request:**
```http
GET /api/search/products?q=lavender&category=floral&category=aromatherapy&minPrice=20&maxPrice=40&minRating=4&inStockOnly=true&sort=price_asc&page=1&pageSize=24
```

**Query parameters:**
- `q` (string, optional): Search term (searches Name, Description, LongDescription, Tags)
- `category` (guid[], optional): Category IDs (multi-select, OR logic)
- `minPrice` (decimal, optional): Minimum price filter
- `maxPrice` (decimal, optional): Maximum price filter
- `minRating` (int, optional): Minimum average rating (1-5)
- `inStockOnly` (bool, optional): Only show products with StockQuantity > 0 (default: false)
- `sort` (string, optional): Sort order - `relevance`, `price_asc`, `price_desc`, `rating`, `newest`, `best_selling` (default: `relevance`)
- `page` (int, optional): Page number (1-indexed, default: 1)
- `pageSize` (int, optional): Results per page (default: 24, max: 100)

**Response:**
```json
{
  "success": true,
  "data": {
    "products": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "name": "Lavender Dreams Candle",
        "slug": "lavender-dreams-candle",
        "price": 24.99,
        "salePrice": null,
        "imageUrl": "https://cdn.candlestore.com/products/lavender-dreams.jpg",
        "averageRating": 4.8,
        "reviewCount": 127,
        "stockQuantity": 45,
        "isActive": true
      }
    ],
    "totalCount": 18,
    "page": 1,
    "pageSize": 24,
    "totalPages": 1,
    "facets": {
      "categories": [
        { "id": "...", "name": "Floral", "count": 18 },
        { "id": "...", "name": "Aromatherapy", "count": 12 }
      ],
      "priceRanges": [
        { "label": "$0-$20", "min": 0, "max": 20, "count": 8 },
        { "label": "$20-$40", "min": 20, "max": 40, "count": 18 },
        { "label": "$40-$60", "min": 40, "max": 60, "count": 6 }
      ],
      "ratings": [
        { "stars": 5, "count": 8 },
        { "stars": 4, "count": 24 }
      ]
    }
  }
}
```

**Example usage (C# HttpClient):**
```csharp
var query = new
{
    q = "lavender",
    category = new[] { categoryId1, categoryId2 },
    minPrice = 20,
    maxPrice = 40,
    sort = "price_asc",
    page = 1,
    pageSize = 24
};

var response = await _httpClient.GetAsync($"/api/search/products?{ToQueryString(query)}");
var result = await response.Content.ReadFromJsonAsync<ApiResponse<SearchResultsDto>>();
```

---

**GET /api/search/autocomplete**

Get autocomplete suggestions for typeahead search.

**Request:**
```http
GET /api/search/autocomplete?term=laven&limit=8
```

**Query parameters:**
- `term` (string, required): Partial search term (min 2 characters)
- `limit` (int, optional): Max suggestions to return (default: 8, max: 20)

**Response:**
```json
{
  "success": true,
  "data": {
    "suggestions": [
      "Lavender Dreams Candle",
      "Lavender Vanilla Candle",
      "French Lavender Pillar",
      "Lavender Mint Aromatherapy",
      "Lavender"
    ]
  }
}
```

**Performance requirement:** Must respond in <50ms for instant typeahead experience.

**Example usage (JavaScript with debouncing):**
```javascript
let debounceTimer;

searchInput.addEventListener('input', (e) => {
  clearTimeout(debounceTimer);

  debounceTimer = setTimeout(async () => {
    const term = e.target.value;
    if (term.length < 2) return;

    const response = await fetch(`/api/search/autocomplete?term=${encodeURIComponent(term)}&limit=8`);
    const result = await response.json();

    displayAutocompleteSuggestions(result.data.suggestions);
  }, 300); // 300ms debounce delay
});
```

---

**GET /api/search/facets**

Get faceted navigation counts for current search query.

**Request:**
```http
GET /api/search/facets?q=lavender&minPrice=20&maxPrice=40
```

**Query parameters:** Same as `/api/search/products` (to calculate facets for filtered result set)

**Response:**
```json
{
  "success": true,
  "data": {
    "categories": [
      { "id": "...", "name": "Floral", "count": 18 },
      { "id": "...", "name": "Aromatherapy", "count": 12 },
      { "id": "...", "name": "Seasonal", "count": 3 }
    ],
    "priceRanges": [
      { "label": "Under $20", "min": 0, "max": 20, "count": 8 },
      { "label": "$20-$40", "min": 20, "max": 40, "count": 18 },
      { "label": "$40+", "min": 40, "max": 999, "count": 6 }
    ],
    "sizes": [
      { "size": "4 oz", "count": 8 },
      { "size": "8 oz", "count": 24 },
      { "size": "16 oz", "count": 12 }
    ],
    "ratings": [
      { "stars": 5, "count": 8 },
      { "stars": 4, "count": 24 },
      { "stars": 3, "count": 12 }
    ]
  }
}
```

**Caching:** Facet counts are cached for 5 minutes per unique query hash to reduce database load.

---

**POST /api/search/track**

Track customer search for analytics and search history.

**Request:**
```http
POST /api/search/track
Content-Type: application/json

{
  "searchTerm": "lavender candle",
  "resultCount": 18,
  "sessionId": "session-abc-123",
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "id": "...",
    "searchedAt": "2025-11-09T14:32:10Z"
  }
}
```

**When to call:** After every search query execution (on search results page load or autocomplete selection).

---

### Admin Experience: Search Analytics

#### Accessing Search Analytics

1. Log into Admin Panel (https://admin.candlestore.com)
2. Navigate to Reports > Search Analytics
3. Select date range (Last 7 Days, Last 30 Days, Last 90 Days, Custom)
4. View search performance metrics

**Search Analytics Dashboard:**

```
┌─────────────────────────────────────────────────────────────┐
│  Search Analytics                                           │
│  Date Range: Last 30 Days         [Export CSV]              │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Overview Metrics                                           │
│  ┌──────────────┬──────────────┬──────────────┬──────────┐ │
│  │ Total        │ Unique       │ Avg Results  │ Zero     │ │
│  │ Searches     │ Search Terms │ per Search   │ Results  │ │
│  ├──────────────┼──────────────┼──────────────┼──────────┤ │
│  │ 12,450       │ 2,340        │ 18.3         │ 847 (7%) │ │
│  └──────────────┴──────────────┴──────────────┴──────────┘ │
│                                                             │
│  Top Search Terms (Last 30 Days)                            │
│  ┌───────────────────┬──────────┬──────────┬───────────┐  │
│  │ Search Term       │ Count    │ Avg Res  │ Conv Rate │  │
│  ├───────────────────┼──────────┼──────────┼───────────┤  │
│  │ lavender          │ 2,341    │ 24       │ 12.3%     │  │
│  │ vanilla           │ 1,867    │ 34       │ 10.8%     │  │
│  │ soy wax           │ 847      │ 48       │ 11.2%     │  │
│  │ eucalyptus        │ 634      │ 12       │ 8.9%      │  │
│  │ ocean breeze      │ 589      │ 6        │ 14.2%     │  │
│  └───────────────────┴──────────┴──────────┴───────────┘  │
│                                                             │
│  Zero-Result Searches (Opportunities)                       │
│  ┌───────────────────┬──────────┬──────────────────────┐  │
│  │ Search Term       │ Count    │ Opportunity Revenue  │  │
│  ├───────────────────┼──────────┼──────────────────────┤  │
│  │ citronella        │ 512      │ $7,680 (est.)        │  │
│  │ beeswax           │ 298      │ $4,470 (est.)        │  │
│  │ unscented         │ 187      │ $2,805 (est.)        │  │
│  │ gift set          │ 134      │ $2,010 (est.)        │  │
│  │ tealight          │ 89       │ $1,335 (est.)        │  │
│  └───────────────────┴──────────┴──────────────────────┘  │
│                                                             │
│  Search-to-Purchase Conversion                              │
│  Customers who search: 8.3% conversion rate                 │
│  Customers who don't search: 2.9% conversion rate           │
│  → Searchers are 2.9x more likely to purchase               │
└─────────────────────────────────────────────────────────────┘
```

**Key metrics explained:**
- **Total Searches:** Number of search queries executed (includes repeat searches)
- **Unique Search Terms:** Distinct search terms (deduplicated)
- **Avg Results per Search:** Average number of products returned per query (low = bad tagging, high = good)
- **Zero Results:** Searches returning 0 products (opportunities for new products or synonym mapping)
- **Conv Rate:** Percentage of searches leading to purchase within 30 minutes (search intent conversion)
- **Opportunity Revenue:** Estimated lost revenue from zero-result searches (Zero Result Count × Site Avg Order Value)

**Actionable insights:**
1. **High zero-result searches** → Add products or create synonym mappings
2. **Low conversion rate** → Search results not relevant (improve ranking algorithm or tagging)
3. **High search volume for specific term** → Popular product category (increase inventory, feature in homepage)
4. **Low average results** → Products not tagged properly (add tags, improve descriptions)

---

### Configuration and Settings

#### Admin Settings for Search

Navigate to Admin Panel > Settings > Search & Filtering:

**General Search Settings:**
- **Enable Search:** Toggle on/off (default: ON)
- **Enable Autocomplete:** Toggle on/off (default: ON)
- **Autocomplete Delay:** Milliseconds (default: 300ms, range: 100-1000ms)
- **Autocomplete Suggestion Count:** Number (default: 8, range: 3-20)
- **Default Sort Order:** Dropdown (Relevance, Price Asc, Price Desc, Rating, Newest)
- **Results Per Page:** Number (default: 24, range: 12-100)

**Filtering Settings:**
- **Enable Category Filter:** Toggle on/off (default: ON)
- **Enable Price Range Filter:** Toggle on/off (default: ON)
- **Enable Rating Filter:** Toggle on/off (default: ON)
- **Enable Size/Variant Filter:** Toggle on/off (default: ON)
- **Enable Tag Filter:** Toggle on/off (default: ON)
- **Enable Stock Availability Filter:** Toggle on/off (default: ON)

**Search Behavior:**
- **Minimum Search Term Length:** Number (default: 2, range: 1-5 characters)
- **Enable Spell Check ("Did You Mean"):** Toggle on/off (default: ON)
- **Enable Synonym Matching:** Toggle on/off (default: ON)
- **Search Scope:** Checkboxes - Name, Description, Long Description, Tags, SKU (default: all checked)
- **Exact Match Priority:** Toggle on/off (default: ON) - Boost exact name matches to top of results

**Performance:**
- **Cache Search Results:** Toggle on/off (default: ON)
- **Cache Duration:** Minutes (default: 5, range: 1-60)
- **Enable Search Tracking:** Toggle on/off (default: ON)
- **Search History Retention:** Days (default: 30, range: 7-365)

---

#### Synonym Management

Navigate to Admin Panel > Settings > Search > Synonyms:

**Synonym Groups:**

A synonym group defines terms that should be treated as equivalent during search.

**Example Synonym Group:**

```
┌─────────────────────────────────────────────────────────────┐
│  Synonym Group: Scent-Related Terms                         │
├─────────────────────────────────────────────────────────────┤
│  Base Term: scent                                           │
│  Synonyms: smell, fragrance, aroma, perfume, odor           │
│  [Save]  [Delete]                                           │
└─────────────────────────────────────────────────────────────┘
```

**How synonyms work:**
- Customer searches "smell" → System expands query to: `smell OR scent OR fragrance OR aroma OR perfume OR odor`
- Products tagged with any synonym term appear in results
- Example: Product tagged "fragrance:lavender" appears when searching "lavender smell"

**Creating synonym groups:**

1. Click [Add Synonym Group]
2. Enter base term (canonical term used in product tagging): "scent"
3. Enter comma-separated synonyms: "smell, fragrance, aroma"
4. Click [Save]
5. Synonyms apply immediately to all searches (no rebuild required)

**Pre-defined synonym groups (included by default):**
- scent → smell, fragrance, aroma, perfume
- candle → candel, kandel (common misspellings)
- soy → soya, soja
- beeswax → bee wax, beewax
- aromatherapy → aroma therapy, essential oil
- lavender → lavendar, lavander (common misspellings)

---

### Integration with Other Systems

#### Integration with Product Catalog (Task 013)

Search queries IProductRepository to fetch product data:
- Product Name, Description, LongDescription, Tags are searchable fields
- Only active products (IsActive = true) appear in search results
- Stock availability filter queries StockQuantity field
- Product images, prices, ratings loaded for search result display

**Database indexing required for performance:**
- Full-text index on Products.Name, Products.Description, Products.LongDescription
- Index on Products.Price for price range queries
- Index on Products.CreatedAt for "Newest First" sorting

---

#### Integration with Order Management (Task 021)

"Best Selling" sort option queries OrderItems table:
- Aggregates quantity sold per product in last 30 days
- Orders products by total units sold descending
- Only completed orders (Status = Delivered or Completed) are counted
- Cached for 1 hour to reduce query load

---

#### Integration with Reviews and Ratings (Task 029)

Rating filter queries Review table:
- Calculates average star rating per product via `AVG(Reviews.Rating)`
- Filters products to those with average rating >= selected threshold
- Products with 0 reviews are excluded from rating filter results

---

#### Integration with Google Analytics (Task 027)

Search events tracked as GA4 events:
- Event name: "search"
- Parameters: search_term, result_count, filters_applied
- Conversion tracking: Correlates search events with subsequent "add_to_cart" and "purchase" events
- Search-to-purchase funnel in GA4 shows drop-off at each stage

---

### Best Practices

1. **Optimize Product Tagging for Searchability:** Review top search terms monthly and ensure products are tagged with those keywords. If customers search "soy wax" but products only have "100% natural soy" in description, add "soy wax" as explicit tag. Comprehensive tagging increases search result relevance and reduces zero-result searches by 40-60%.

2. **Monitor Zero-Result Searches Weekly:** Zero-result searches indicate product gaps or tagging issues. If "citronella candles" returns 0 results but has 500+ monthly searches, either add citronella products or create synonym mapping if similar products exist (e.g., "citronella" → "lemongrass"). Addressing top 10 zero-result searches can recover $10,000-$20,000 annual revenue.

3. **Use Synonym Mapping for Common Misspellings:** Customers misspell product names frequently ("lavendar" vs "lavender", "kandel" vs "candle"). Create synonym groups for common misspellings to rescue searches. Synonym mapping increases search result success rate from 82% to 94%, reducing bounce rate from misspelled queries.

4. **Limit Active Filters to 3-5 Simultaneously:** Too many active filters create narrow result sets (0-2 products), causing frustration. If customer applies 8 filters and sees "No results", they may abandon search. Display warning when filters reduce results below threshold: "Only 1 product matches all filters. Try removing a filter to see more options." Balance specificity with discoverability.

5. **Cache Popular Search Queries Aggressively:** Top 20% of search terms account for 80% of search volume (Pareto principle). Cache these popular queries for 15-30 minutes to reduce database load by 70%. Monitor cache hit rate—should be >80% for well-performing search. Use Redis distributed cache for multi-instance deployments.

6. **Implement Search-Specific Product Ranking:** Default "Relevance" sort should prioritize products most likely to convert, not just text match score. Boost products with higher ratings (+10% score per star above 4.0), higher sales velocity (+5% score per 100 sales/month), and in-stock availability (+15% score). This increases search-to-purchase conversion from 8% to 12%.

7. **Display Alternative Suggestions for Zero-Result Searches:** Never show blank page with "No results found" message. Instead, display: (1) "Did you mean X?" spell-check suggestion, (2) Related categories customers can browse, (3) Popular products or trending products as fallback. This reduces bounce rate on zero-result pages from 89% to 34%.

---

### Troubleshooting

**Problem:** Autocomplete suggestions not appearing or delayed >1 second

**Solution:** Autocomplete requires database index on Product.Name for prefix matching. Verify index exists:

```sql
SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Products') AND name = 'IX_Products_Name_Prefix';
```

If missing, create index:

```sql
CREATE INDEX IX_Products_Name_Prefix ON Products(Name) INCLUDE (Id, Price, ImageUrl);
```

Also check autocomplete debounce delay in settings (Admin > Settings > Search > Autocomplete Delay). If set >500ms, suggestions feel laggy. Reduce to 300ms for instant feel. Verify caching enabled—autocomplete suggestions should be cached for 10 minutes per prefix. Check application logs for "Autocomplete cache miss" warnings indicating cache not working.

---

**Problem:** Search results showing out-of-stock or inactive products

**Solution:** Search query must filter IsActive = true AND StockQuantity > 0. Verify SearchService.SearchProductsAsync implementation:

```csharp
var query = _context.Products
    .Where(p => p.IsActive) // Only active products
    .AsQueryable();

if (searchQuery.InStockOnly)
{
    query = query.Where(p => p.StockQuantity > 0); // Only in-stock if filter applied
}
```

If "In Stock Only" filter is checked by default, verify checkbox is unchecked in UI component. Check Product.IsActive field in database—products may have IsActive = true but should be false (discontinued products). Admin should bulk-update discontinued products to IsActive = false via Admin Panel > Products > Bulk Actions > Mark Inactive.

---

**Problem:** Facet counts don't match actual result counts

**Solution:** Facet count queries may be executing against different result set than product query. Common cause: facet query not applying same filters as product query. Verify both queries use identical WHERE clauses:

```csharp
// Product query
var products = _context.Products
    .Where(p => p.IsActive && p.CategoryId == selectedCategory && p.Price >= minPrice && p.Price <= maxPrice)
    .ToList();

// Facet query (must have same WHERE clauses)
var categoryFacets = _context.Products
    .Where(p => p.IsActive && p.CategoryId == selectedCategory && p.Price >= minPrice && p.Price <= maxPrice)
    .GroupBy(p => p.CategoryId)
    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
    .ToList();
```

Alternatively, facet counts may be stale from cache. Clear facet cache (Admin > Tools > Clear Cache > Facet Counts) or reduce cache duration from 5 minutes to 1 minute during testing. In production, 5-minute cache is acceptable—slight count mismatches (±2 products) don't harm user experience.

---

**Problem:** "Did you mean" suggestions showing incorrect or irrelevant terms

**Solution:** Spell-check uses Levenshtein distance algorithm comparing search term to known product names and popular search terms. Minimum distance threshold may be too high, suggesting terms that are too different. Configure threshold in settings:

Admin > Settings > Search > Spell Check Settings > Maximum Edit Distance: 2 (default)

Edit distance 2 means: "lavendar" (1 character wrong) suggests "lavender" ✓, but "abcdef" (6 characters wrong) does NOT suggest "lavender" ✗. If getting too many irrelevant suggestions, reduce to 1 (only 1-character typos corrected). If getting too few suggestions, increase to 3 (allows 3-character typos).

Also verify spell-check dictionary includes all product names. Background job should refresh dictionary nightly from Products.Name field. Check job logs: "SpellCheckDictionaryJob completed. Indexed 247 product names." If job failing, dictionary is stale.

---

**Problem:** Search performance degraded to >2 seconds response time

**Solution:** Slow searches typically caused by: (1) Missing database indexes, (2) Uncached queries, (3) N+1 query problem loading related entities.

**Step 1: Verify indexes exist:**
```sql
-- Full-text index for text search
SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('Products');

-- Price range index
SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Products') AND name = 'IX_Products_Price';

-- Category filter index
SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Products') AND name = 'IX_Products_CategoryId_IsActive_StockQuantity';
```

If missing, create indexes (see Database Configuration section).

**Step 2: Enable query logging to identify slow queries:**
```csharp
// In Startup.cs
builder.Services.AddDbContext<CandleStoreDbContext>(options =>
{
    options.UseSqlServer(connectionString)
        .EnableSensitiveDataLogging() // Development only
        .LogTo(Console.WriteLine, LogLevel.Information); // Log all queries
});
```

Review logs for queries taking >500ms. Common culprit: facet COUNT queries without indexes.

**Step 3: Check for N+1 problem:**
Search query should use `.Include()` to eager-load related entities:
```csharp
var products = await _context.Products
    .Where(/* filters */)
    .Include(p => p.Category)
    .Include(p => p.ProductImages)
    .Include(p => p.Reviews) // For rating calculation
    .ToListAsync();
```

Without `.Include()`, EF Core makes separate queries for each product's Category, Images, Reviews (N+1 problem). With 24 products, this causes 24 × 3 = 72 extra queries. Adding `.Include()` reduces to 1 query.

**Step 4: Verify caching enabled:**
Check search result cache hit rate in application logs. Should see "Search cache hit for query hash: abc123" messages. If cache always missing, verify IMemoryCache registered in DI container and cache key generation is deterministic (same query = same cache key).

---

**Problem:** Mobile users complain filters are hard to use (too small, can't tap checkboxes)

**Solution:** Filter UI must be mobile-optimized with larger touch targets and responsive design. Verify:

1. **Filter sidebar is full-width on mobile (<768px screen):**
```css
@media (max-width: 768px) {
  .filter-sidebar {
    width: 100%;
    position: fixed;
    top: 0;
    left: 0;
    height: 100vh;
    overflow-y: auto;
    z-index: 1000;
  }
}
```

2. **Checkboxes have minimum 44px touch targets (Apple HIG, Android Material Design):**
```css
.filter-checkbox {
  width: 44px;
  height: 44px;
  cursor: pointer;
}

.filter-checkbox-label {
  padding: 12px;
  min-height: 44px;
  display: flex;
  align-items: center;
}
```

3. **Filter panel toggles via [Filters] button on mobile (drawer pattern):**
```html
<button class="mobile-filter-toggle">
  Filters (3 active) 🎚️
</button>

<div class="filter-sidebar mobile-hidden">
  <!-- Filters here -->
  <button class="apply-filters-mobile">Apply Filters</button>
  <button class="close-filters-mobile">Cancel</button>
</div>
```

Clicking [Filters] button slides filter drawer from left edge. After selecting filters, customer clicks [Apply Filters] to execute search and close drawer. This pattern used by Amazon, eBay, Etsy for mobile e-commerce.

---

**Problem:** Customer searches "lavender candle" but sees unrelated products (vanilla, cinnamon, etc.) in results

**Solution:** Search relevance algorithm not properly scoring text matches. Verify search query uses full-text search (CONTAINS, FREETEXT) not LIKE:

**❌ Poor relevance (LIKE):**
```csharp
query = query.Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
```

**✅ Good relevance (Full-text search):**
```csharp
query = query.Where(p => EF.Functions.Contains(p.Name, searchTerm) || EF.Functions.Contains(p.Description, searchTerm));
```

Full-text search understands word boundaries, stemming (search "candles" finds "candle"), and ranking (exact match scores higher than partial match). LIKE treats search term as simple substring (no ranking, no stemming).

If using PostgreSQL, replace `EF.Functions.Contains` with `EF.Functions.ToTsVector`:
```csharp
query = query.Where(p => EF.Functions.ToTsVector("english", p.Name).Matches(EF.Functions.ToTsQuery("english", searchTerm)));
```

Additionally, implement result boosting: exact name match +50 points, partial name match +20 points, tag match +10 points, description match +5 points. Order results by boost score descending.

---
## Acceptance Criteria / Definition of Done

### Core Functionality - Text Search

- [ ] Search API endpoint GET /api/search/products accepts query parameter `q` for search term
- [ ] Search query filters products by matching text in Name, Description, LongDescription, Tags fields
- [ ] Search uses full-text search (EF.Functions.Contains or EF.Functions.FreeText) not LIKE for performance
- [ ] Search is case-insensitive ("LAVENDER" finds "lavender", "Lavender", "LAVENDER")
- [ ] Search supports multi-word queries ("lavender candle" finds products containing both "lavender" AND "candle")
- [ ] Search returns empty array if no products match (not 404 error)
- [ ] Search excludes inactive products (IsActive = false) from results
- [ ] Search result count displayed in UI: "24 results for 'lavender'"
- [ ] Search term persisted in URL query string (/search?q=lavender) for bookmarking/sharing
- [ ] Search term pre-populated in search input box when navigating to /search page
- [ ] Search minimum term length enforced (default 2 characters, configurable 1-5)
- [ ] Search validates input to prevent SQL injection (parameterized queries, no string concatenation)

### Core Functionality - Autocomplete

- [ ] Autocomplete API endpoint GET /api/search/autocomplete accepts `term` parameter (min 2 chars)
- [ ] Autocomplete returns 5-8 product name suggestions matching term prefix
- [ ] Autocomplete uses prefix matching (EF.Functions.Like with pattern "{term}%")
- [ ] Autocomplete suggestions ordered by relevance: exact match > starts with > contains
- [ ] Autocomplete response time < 50ms (95th percentile)
- [ ] Autocomplete results cached for 10 minutes per term prefix
- [ ] Autocomplete dropdown displays below search input box
- [ ] Autocomplete dropdown shows product names with prices and images (optional)
- [ ] Clicking autocomplete suggestion navigates to product detail page or executes search
- [ ] Autocomplete debounced to 300ms after user stops typing (prevents API spam)
- [ ] Autocomplete cancels previous request if new request initiated (AbortController pattern)
- [ ] Autocomplete hidden when search input loses focus or Escape key pressed
- [ ] Autocomplete navigable via keyboard: Arrow Down/Up to select, Enter to execute, Escape to close

### Core Functionality - Category Filter

- [ ] Category filter displays as checkbox list in filter sidebar
- [ ] Each category shows count of products in that category: "Floral (23)"
- [ ] Category counts update dynamically when other filters applied (e.g., price range changes count)
- [ ] Multiple categories selectable simultaneously (multi-select)
- [ ] Multiple category selections use OR logic (product matches ANY selected category)
- [ ] Selecting category updates URL query string: /search?category=cat1&category=cat2
- [ ] Selecting category filters products to only those in selected categories
- [ ] Deselecting category removes filter and refreshes results
- [ ] Category filter shows "Show More..." link if >8 categories (collapses long lists)
- [ ] Clicking "Show More..." expands to show all categories
- [ ] Category filter hidden if product catalog has only 1 category (no filtering needed)
- [ ] Category filter persists across page navigations (state maintained in URL)

### Core Functionality - Price Range Filter

- [ ] Price range filter displays as dual-handle slider in filter sidebar
- [ ] Slider shows minimum available price (left label) and maximum available price (right label)
- [ ] Slider handles draggable to set minPrice and maxPrice values
- [ ] Dragging slider handle updates filter immediately on release (no Apply button needed)
- [ ] Slider shows current selected range: "$20 - $80"
- [ ] Price range filter includes numeric input boxes for manual entry (Min: $__, Max: $__)
- [ ] Typing in numeric inputs and pressing Enter applies price filter
- [ ] Price filter validates min <= max (swaps values if user enters min > max)
- [ ] Price filter updates URL query string: /search?minPrice=20&maxPrice=80
- [ ] Price filter excludes products outside range (price < minPrice OR price > maxPrice)
- [ ] Price filter shows count of products in current range (updates dynamically)
- [ ] Price filter reset button clears min/max values and shows all products
- [ ] Price filter respects product sale prices (filters by salePrice if not null, else price)

### Core Functionality - Rating Filter

- [ ] Rating filter displays as list of star rating options: 5★, 4★, 3★, 2★, 1★
- [ ] Each rating option shows count: "4 stars & up (32)"
- [ ] Rating options use "X stars & up" logic (4★ shows products with rating >= 4.0)
- [ ] Selecting rating filters products to only those with average rating >= selected value
- [ ] Rating filter queries Reviews table and calculates AVG(Rating) per product
- [ ] Products with 0 reviews excluded from rating filter results (can't have rating without reviews)
- [ ] Rating filter updates URL query string: /search?minRating=4
- [ ] Rating filter persists across page navigations
- [ ] Deselecting rating filter shows all products regardless of rating (including 0-review products)
- [ ] Rating filter counts updated dynamically when other filters applied

### Core Functionality - Stock Availability Filter

- [ ] Stock availability filter displays as checkbox: "☐ In Stock Only"
- [ ] Checking "In Stock Only" filters products to StockQuantity > 0
- [ ] Unchecking shows all products including out-of-stock (StockQuantity = 0)
- [ ] Stock filter updates URL query string: /search?inStockOnly=true
- [ ] Stock filter count shows number of in-stock products: "In Stock (47)"
- [ ] Stock filter persists across page navigations
- [ ] Stock filter checked by default (configurable in admin settings)

### Core Functionality - Size/Variant Filter

- [ ] Size filter displays as checkbox list: 4 oz, 8 oz, 16 oz
- [ ] Each size shows count of products available in that size: "8 oz (24)"
- [ ] Multiple sizes selectable simultaneously (multi-select, OR logic)
- [ ] Selecting size filters products with matching ProductVariant.Size value
- [ ] Size filter updates URL query string: /search?size=8oz&size=16oz
- [ ] Size filter counts updated dynamically when other filters applied
- [ ] Size filter hidden if all products have same size (no filtering needed)

### Core Functionality - Tag Filter

- [ ] Tag filter displays as checkbox list or searchable dropdown (if >10 tags)
- [ ] Each tag shows count of products with that tag: "Soy Wax (45)"
- [ ] Multiple tags selectable simultaneously (multi-select, OR logic)
- [ ] Selecting tag filters products to those with matching ProductTag records
- [ ] Tag filter updates URL query string: /search?tag=soy-wax&tag=hand-poured
- [ ] Tag filter counts updated dynamically when other filters applied
- [ ] Tag filter supports tag search (type to filter tag list if >20 tags)

### Core Functionality - Sorting

- [ ] Sort dropdown displays options: Relevance, Price (Low-High), Price (High-Low), Rating, Newest, Best Selling
- [ ] Default sort is "Relevance" when search term provided, otherwise "Newest"
- [ ] "Relevance" sort orders by text match score (exact name match > partial name > description match)
- [ ] "Price: Low to High" sort orders by price ASC (lowest price first)
- [ ] "Price: High to Low" sort orders by price DESC (highest price first)
- [ ] "Rating" sort orders by average star rating DESC (highest rated first)
- [ ] "Newest" sort orders by CreatedAt DESC (most recent products first)
- [ ] "Best Selling" sort orders by total units sold in last 30 days DESC
- [ ] Sort selection updates URL query string: /search?sort=price_asc
- [ ] Sort persists across pagination (page 2 maintains selected sort)
- [ ] Sort selection persists across filter changes (changing category maintains sort)

### Core Functionality - Pagination

- [ ] Search results paginated with default 24 products per page (configurable 12-100)
- [ ] Pagination displays page numbers: << 1 2 3 4 5 >>
- [ ] Clicking page number loads that page of results
- [ ] Current page highlighted in pagination controls
- [ ] Pagination displays "Showing 1-24 of 247 results"
- [ ] Page number persisted in URL query string: /search?q=lavender&page=2
- [ ] Navigation to page >1 scrolls page to top of results (prevents confusion)
- [ ] Pagination hidden if total results <= page size (e.g., 18 results with 24/page shows no pagination)
- [ ] "Load More" button option (infinite scroll alternative, configurable in admin settings)

### Core Functionality - Active Filters Display

- [ ] Active filters displayed as removable "pills" above search results
- [ ] Each active filter shows as chip/pill: [Floral ✕] [Price: $20-$80 ✕] [4★ & up ✕]
- [ ] Clicking ✕ icon removes that filter and refreshes results
- [ ] "Clear All Filters" link removes all active filters and resets to full result set
- [ ] Active filters section hidden if no filters applied
- [ ] Active filter pills wrap to multiple lines on narrow screens (mobile responsive)

### Core Functionality - Search History

- [ ] Search history tracks last 5 searches per customer (authenticated users)
- [ ] Search history tracks last 5 searches per session (guest users via localStorage)
- [ ] Search history dropdown displays when clicking empty search input box
- [ ] Each history item shows: search term, time ago ("2 hours ago"), result count
- [ ] Clicking history item re-executes that search
- [ ] "Clear History" link empties search history
- [ ] Search history auto-expires after 30 days (old searches removed by background job)
- [ ] Search history persists across browser sessions for authenticated users
- [ ] Search history stored in localStorage for guest users (cleared when browser data cleared)

### Core Functionality - Popular Searches

- [ ] Popular searches displays top 10 most frequently searched terms (last 30 days)
- [ ] Popular searches shown in search dropdown or homepage widget
- [ ] Clicking popular search executes that search query
- [ ] Popular searches recalculated nightly by background job
- [ ] Popular searches cached for 24 hours to reduce database queries
- [ ] Popular searches displayed as clickable pills/tags: [lavender] [vanilla] [soy wax]

### Core Functionality - Spell Check ("Did You Mean")

- [ ] Spell check suggestions appear when search returns <3 results
- [ ] Spell check calculates Levenshtein distance between search term and known product names
- [ ] Spell check suggests term with distance <= 2 (max 2 character edits)
- [ ] Spell check displays: "Did you mean 'lavender'?" with clickable link
- [ ] Clicking spell check suggestion re-executes search with corrected term
- [ ] Spell check suggestion dictionary includes all product names and popular search terms
- [ ] Spell check dictionary refreshed nightly by background job
- [ ] Spell check disabled via admin setting: Admin > Settings > Search > Enable Spell Check

### Core Functionality - Synonym Matching

- [ ] Synonym matching expands search query to include synonym terms
- [ ] Search "smell" finds products with "scent", "fragrance", "aroma" tags
- [ ] Synonyms defined in database table: SynonymGroups (BaseTerm, Synonyms)
- [ ] Synonym expansion uses OR logic: WHERE Name CONTAINS 'smell' OR Name CONTAINS 'scent' OR Name CONTAINS 'fragrance'
- [ ] Admin can create custom synonym groups: Admin > Settings > Search > Synonyms
- [ ] Default synonym groups pre-loaded: scent/smell/fragrance, candle misspellings, soy variants
- [ ] Synonym matching disabled via admin setting if causing poor results

### Core Functionality - No Results Handling

- [ ] Zero results page displays when search returns 0 products
- [ ] Zero results page shows: "No products found for 'xyz'"
- [ ] Zero results page displays "Did you mean" spell check suggestion if available
- [ ] Zero results page displays 3-5 alternative suggestions: "Try searching for...", "Browse these categories"
- [ ] Zero results page displays 6-8 trending/popular products as fallback recommendations
- [ ] Zero results page includes "Clear All Filters" button if filters active (may be cause of zero results)
- [ ] Zero results event tracked in analytics: SearchAnalytics table with ResultCount = 0
- [ ] Admin can view zero-result searches: Admin > Reports > Search Analytics > Zero-Result Searches

### Core Functionality - Result Highlighting

- [ ] Search term highlighted in product names on results page
- [ ] Matching text wrapped in `<mark>` tag: `<mark class="highlight">lavender</mark>`
- [ ] Highlight styling: yellow background, bold text (CSS customizable)
- [ ] Multiple words highlighted separately: search "lavender candle" highlights both "lavender" AND "candle"
- [ ] Highlighting case-insensitive (search "LAVENDER" highlights "Lavender", "lavender")
- [ ] Highlighting HTML-escaped to prevent XSS attacks (search "<script>" doesn't execute JavaScript)

### API Endpoints

- [ ] GET /api/search/products returns search results with filters, sorting, pagination
- [ ] GET /api/search/autocomplete returns typeahead suggestions for partial search term
- [ ] GET /api/search/facets returns facet counts for categories, price ranges, ratings, sizes, tags
- [ ] GET /api/search/popular returns top 10 popular search terms
- [ ] GET /api/search/history?customerId={id} returns search history for customer
- [ ] POST /api/search/track tracks search event for analytics (searchTerm, resultCount, customerId, sessionId)
- [ ] All endpoints return ApiResponse<T> with standard structure: { success, data, message }
- [ ] All endpoints support CORS headers for cross-origin requests
- [ ] All endpoints validate input parameters (searchTerm length, page >= 1, pageSize <= 100)
- [ ] All endpoints return 200 OK with empty array if no results (not 404)
- [ ] All endpoints handle null/empty search term gracefully (return all products or validation error)

### UI/UX - Search Interface

- [ ] Search bar displays in header navigation on all pages
- [ ] Search bar placeholder text: "Search products..." or "What are you looking for?"
- [ ] Search bar keyboard shortcut: Ctrl+K (Windows/Linux) or Cmd+K (Mac) focuses search input
- [ ] Search icon (magnifying glass 🔍) displays on right side of search input
- [ ] Clicking search icon or pressing Enter executes search
- [ ] Search input auto-focuses when opening search page
- [ ] Search input retains search term after search execution (for refinement)
- [ ] Search input clears via [✕] icon on right side (appears when input has text)

### UI/UX - Filter Sidebar

- [ ] Filter sidebar displays on left side of search results page (desktop) or drawer (mobile)
- [ ] Filter sidebar sticky (remains visible while scrolling results)
- [ ] Filter sidebar width: 250-300px on desktop, full-width on mobile
- [ ] Filter sidebar collapsible via hamburger icon on mobile
- [ ] Filter sidebar shows filter category headings: Categories, Price Range, Rating, etc.
- [ ] Each filter category collapsible/expandable via chevron icon (▾/▸)
- [ ] Filter sidebar [Apply Filters] button on mobile (executes search and closes drawer)
- [ ] Filter sidebar [Reset Filters] link clears all selections and refreshes results

### UI/UX - Search Results Grid

- [ ] Search results display in grid layout: 4 columns (desktop), 2 columns (tablet), 1 column (mobile)
- [ ] Each product card shows: image, name, price, rating, [Add to Cart] button
- [ ] Product card images lazy-loaded (intersection observer, loading="lazy")
- [ ] Product card images 1:1 aspect ratio (square) for consistent grid
- [ ] Hovering product card shows subtle border or shadow effect
- [ ] Clicking product card navigates to product detail page
- [ ] Clicking [Add to Cart] adds product without navigation (AJAX request)
- [ ] "Out of Stock" badge overlays product image if StockQuantity = 0
- [ ] Sale badge displays if product has salePrice != null: "Sale -20%"

### UI/UX - Mobile Responsiveness

- [ ] Search bar full-width on mobile (<768px)
- [ ] Filter sidebar slides in from left as drawer on mobile (not always visible)
- [ ] [Filters] button displays above results on mobile to open filter drawer: "Filters (3 active) 🎚️"
- [ ] Active filter pills wrap to multiple lines on mobile (no horizontal scroll)
- [ ] Sort dropdown full-width on mobile
- [ ] Product grid single column on mobile (<576px), 2 columns on tablet (576-991px)
- [ ] Pagination controls stack vertically on mobile if too many pages to fit horizontally
- [ ] Touch targets minimum 44x44px for filter checkboxes, slider handles (Apple HIG, Material Design)

### Performance - Search Operations

- [ ] GET /api/search/products responds in < 500ms for non-cached queries (95th percentile)
- [ ] GET /api/search/products responds in < 100ms for cached queries
- [ ] GET /api/search/autocomplete responds in < 50ms (critical for typeahead feel)
- [ ] GET /api/search/facets responds in < 300ms (facet counts may require multiple aggregate queries)
- [ ] Search results cached for 5 minutes per unique query hash (reduces DB load)
- [ ] Autocomplete suggestions cached for 10 minutes per term prefix
- [ ] Facet counts cached for 5 minutes per query to prevent repeated COUNT queries
- [ ] Search page loads (HTML + API data) in < 2 seconds on 3G connection (WebPageTest)
- [ ] Database indexes exist on Products.Name, Products.Price, Products.CategoryId, Products.CreatedAt
- [ ] Full-text index exists on Products (Name, Description, LongDescription) for fast text search

### Data Persistence - Search Tracking

- [ ] ProductSearch entity persisted to ProductSearches table
- [ ] ProductSearch.Id is GUID primary key
- [ ] ProductSearch.SearchTerm is NVARCHAR(200) storing customer's search query
- [ ] ProductSearch.ResultCount is INT storing number of products returned
- [ ] ProductSearch.CustomerId foreign key references Customers.Id (nullable for guest users)
- [ ] ProductSearch.SessionId is NVARCHAR(100) for anonymous session tracking
- [ ] ProductSearch.SearchedAt is DATETIME2 with automatic timestamp
- [ ] ProductSearch.Filters is JSON string storing applied filters (category, price, rating) for analytics
- [ ] Index exists on (SearchTerm, SearchedAt) for search analytics queries
- [ ] Index exists on (CustomerId, SearchedAt) for customer search history
- [ ] Index exists on (SessionId, SearchedAt) for guest search history

### Data Persistence - Synonym Management

- [ ] SynonymGroup entity persisted to SynonymGroups table
- [ ] SynonymGroup.Id is GUID primary key
- [ ] SynonymGroup.BaseTerm is NVARCHAR(100) storing canonical term (e.g., "scent")
- [ ] SynonymGroup.Synonyms is NVARCHAR(500) storing comma-separated synonyms (e.g., "smell,fragrance,aroma")
- [ ] SynonymGroup.IsActive boolean to enable/disable synonym group without deleting
- [ ] Unique constraint on BaseTerm prevents duplicate synonym groups
- [ ] Admin can CRUD synonym groups via Admin Panel > Settings > Search > Synonyms

### Edge Cases - Search Queries

- [ ] Empty search term returns all products (or validation error, configurable)
- [ ] Search term with only whitespace treated as empty (trimmed before processing)
- [ ] Search term >200 characters truncated to 200 characters (prevents abuse)
- [ ] Search term with special characters (%, _, *, ?) escaped to prevent SQL injection
- [ ] Search term with HTML tags sanitized (<script> becomes &lt;script&gt;)
- [ ] Search with ALL CAPS normalized to lowercase for matching (avoids zero results for "LAVENDER")
- [ ] Search with leading/trailing whitespace trimmed before processing
- [ ] Search with multiple consecutive spaces normalized to single space

### Edge Cases - Filters

- [ ] Applying filters that result in 0 products shows "No results" message (not error)
- [ ] Applying conflicting filters (minPrice > maxPrice) auto-corrects by swapping values
- [ ] Applying invalid filter values (price = "abc") returns validation error 400
- [ ] Applying filters for categories that don't exist returns all products (ignores invalid categoryId)
- [ ] Deselecting all filters shows full product catalog (no filters active = all products)
- [ ] Filter counts showing 0 are disabled or grayed out (can't select filter with 0 matching products)
- [ ] Filters with >50 options show search box to filter filter options (filter inception)

### Edge Cases - Pagination

- [ ] Requesting page beyond total pages (page=100 when only 5 pages exist) returns last page or empty array
- [ ] Requesting page=0 or page<0 defaults to page=1
- [ ] Requesting pageSize=0 or pageSize<0 defaults to configured default (24)
- [ ] Requesting pageSize>100 capped at 100 (prevents resource exhaustion)
- [ ] Filter change resets to page=1 (prevents showing "page 5 of 2 pages" after filtering)
- [ ] Total page count recalculated when filters applied (24 pages → 3 pages after price filter)

### Edge Cases - Autocomplete

- [ ] Autocomplete with search term <2 characters returns empty array (prevents excessive suggestions)
- [ ] Autocomplete with no matching products returns empty array (not error)
- [ ] Autocomplete with term matching >100 products returns top 8 by relevance
- [ ] Autocomplete cancels in-flight request if user types new character (prevents stale suggestions)
- [ ] Autocomplete hidden when search input loses focus or Escape key pressed
- [ ] Autocomplete displays loading spinner if request takes >100ms (prevents blank dropdown confusion)

### Edge Cases - Sorting

- [ ] "Best Selling" sort with products having 0 sales orders by CreatedAt DESC (newest first)
- [ ] "Rating" sort with products having 0 reviews excluded or shown last (configurable)
- [ ] "Rating" sort with products having same rating orders by review count DESC (more reviews = higher rank)
- [ ] "Price" sort respects sale prices (sorts by salePrice if not null, else price)
- [ ] "Relevance" sort without search term falls back to "Newest" or "Best Selling"

### Security - Search System

- [ ] Search term sanitized to prevent SQL injection (parameterized queries, no string concatenation)
- [ ] Search term sanitized to prevent XSS attacks (HTML-encoded before rendering in UI)
- [ ] Search term validated for length (max 200 characters) to prevent resource exhaustion
- [ ] Search rate limited to 100 queries per IP per hour (prevents abuse/scraping)
- [ ] Autocomplete rate limited to 200 requests per IP per minute (typeahead generates many requests)
- [ ] Filter parameters validated as correct data types (categoryId = GUID, price = decimal) before queries
- [ ] SQL full-text search queries use parameterized queries (EF Core automatic parameterization)
- [ ] Admin search analytics require Admin role (401 Unauthorized for non-admin users)
- [ ] Customer search history only accessible to that customer (can't view other customers' searches)

### Documentation

- [ ] API documentation includes all search endpoints with request/response examples
- [ ] API documentation explains filter parameter syntax (multi-select category, price range)
- [ ] README includes search architecture diagram (SearchService → ISearchRepository → DB)
- [ ] Admin user guide explains how to interpret search analytics (zero-result searches, top terms)
- [ ] Developer documentation explains how to extend search (add custom facets, integrate Elasticsearch)
- [ ] User manual includes troubleshooting section for common search issues

---
## Testing Requirements

### Unit Tests

**Test 1: SearchService - SearchProductsAsync - Returns Products Matching Search Term**

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using AutoFixture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CandleStore.Application.Services;
using CandleStore.Application.DTOs.Search;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CandleStore.Tests.Unit.Application.Services
{
    public class SearchServiceTests
    {
        private readonly IFixture _fixture;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<SearchService>> _mockLogger;
        private readonly SearchService _sut;

        public SearchServiceTests()
        {
            _fixture = new Fixture();
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCache = new Mock<IMemoryCache>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<SearchService>>();

            _mockUnitOfWork.Setup(u => u.Products).Returns(_mockProductRepository.Object);

            _sut = new SearchService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockCache.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task SearchProductsAsync_WithSearchTerm_ReturnsMatchingProducts()
        {
            // Arrange
            var searchQuery = new SearchQueryDto
            {
                SearchTerm = "lavender",
                Page = 1,
                PageSize = 24
            };

            var matchingProducts = _fixture.Build<Product>()
                .With(p => p.Name, "Lavender Dreams Candle")
                .With(p => p.IsActive, true)
                .With(p => p.StockQuantity, 10)
                .CreateMany(5)
                .ToList();

            _mockProductRepository
                .Setup(r => r.SearchProductsAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync((matchingProducts, 5));

            var expectedDtos = _fixture.CreateMany<ProductListDto>(5).ToList();
            _mockMapper
                .Setup(m => m.Map<List<ProductListDto>>(matchingProducts))
                .Returns(expectedDtos);

            // Mock cache miss
            object cacheValue = null;
            _mockCache
                .Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue))
                .Returns(false);

            // Act
            var result = await _sut.SearchProductsAsync(searchQuery);

            // Assert
            result.Should().NotBeNull();
            result.Products.Should().HaveCount(5);
            result.TotalCount.Should().Be(5);
            result.Page.Should().Be(1);
            result.PageSize.Should().Be(24);
            result.TotalPages.Should().Be(1);

            _mockProductRepository.Verify(r => r.SearchProductsAsync(
                "lavender",
                null, // no category filter
                null, // no minPrice
                null, // no maxPrice
                null, // no minRating
                false, // inStockOnly = false
                null, // sort = null (relevance)
                0, // skip = 0
                24 // take = 24
            ), Times.Once);
        }

        [Fact]
        public async Task SearchProductsAsync_WithEmptySearchTerm_ReturnsAllActiveProducts()
        {
            // Arrange
            var searchQuery = new SearchQueryDto
            {
                SearchTerm = "",
                Page = 1,
                PageSize = 24
            };

            var allProducts = _fixture.Build<Product>()
                .With(p => p.IsActive, true)
                .CreateMany(50)
                .ToList();

            _mockProductRepository
                .Setup(r => r.SearchProductsAsync(
                    It.IsAny<string>(),
                    It.IsAny<List<Guid>>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<int?>(),
                    It.IsAny<bool>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()))
                .ReturnsAsync((allProducts.Take(24).ToList(), 50));

            var expectedDtos = _fixture.CreateMany<ProductListDto>(24).ToList();
            _mockMapper
                .Setup(m => m.Map<List<ProductListDto>>(It.IsAny<List<Product>>()))
                .Returns(expectedDtos);

            object cacheValue = null;
            _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);

            // Act
            var result = await _sut.SearchProductsAsync(searchQuery);

            // Assert
            result.Should().NotBeNull();
            result.Products.Should().HaveCount(24);
            result.TotalCount.Should().Be(50);
            result.TotalPages.Should().Be(3); // Ceiling(50 / 24) = 3
        }
    }
}
```

---

**Test 2: SearchService - SearchProductsAsync - Applies Price Range Filter**

```csharp
[Fact]
public async Task SearchProductsAsync_WithPriceRange_FiltersProductsByPrice()
{
    // Arrange
    var searchQuery = new SearchQueryDto
    {
        SearchTerm = "candle",
        MinPrice = 20,
        MaxPrice = 40,
        Page = 1,
        PageSize = 24
    };

    var productsInRange = _fixture.Build<Product>()
        .With(p => p.Price, 25.99m) // Price within range
        .With(p => p.IsActive, true)
        .CreateMany(12)
        .ToList();

    _mockProductRepository
        .Setup(r => r.SearchProductsAsync(
            "candle",
            null,
            20m, // minPrice
            40m, // maxPrice
            null,
            false,
            null,
            0,
            24))
        .ReturnsAsync((productsInRange, 12));

    var expectedDtos = _fixture.CreateMany<ProductListDto>(12).ToList();
    _mockMapper.Setup(m => m.Map<List<ProductListDto>>(productsInRange)).Returns(expectedDtos);

    object cacheValue = null;
    _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);

    // Act
    var result = await _sut.SearchProductsAsync(searchQuery);

    // Assert
    result.Should().NotBeNull();
    result.Products.Should().HaveCount(12);

    _mockProductRepository.Verify(r => r.SearchProductsAsync(
        "candle",
        null,
        20m,
        40m,
        null,
        false,
        null,
        0,
        24
    ), Times.Once);
}
```

---

**Test 3: SearchService - SearchProductsAsync - Applies Category Filter**

```csharp
[Fact]
public async Task SearchProductsAsync_WithMultipleCategoryFilter_ReturnsProductsMatchingAnyCategory()
{
    // Arrange
    var floralCategoryId = Guid.NewGuid();
    var aromaCategoryId = Guid.NewGuid();

    var searchQuery = new SearchQueryDto
    {
        SearchTerm = "lavender",
        CategoryIds = new List<Guid> { floralCategoryId, aromaCategoryId },
        Page = 1,
        PageSize = 24
    };

    var matchingProducts = _fixture.Build<Product>()
        .With(p => p.CategoryId, floralCategoryId) // Matches first category
        .With(p => p.IsActive, true)
        .CreateMany(8)
        .Concat(_fixture.Build<Product>()
            .With(p => p.CategoryId, aromaCategoryId) // Matches second category
            .With(p => p.IsActive, true)
            .CreateMany(4))
        .ToList();

    _mockProductRepository
        .Setup(r => r.SearchProductsAsync(
            "lavender",
            It.Is<List<Guid>>(list => list.Contains(floralCategoryId) && list.Contains(aromaCategoryId)),
            null,
            null,
            null,
            false,
            null,
            0,
            24))
        .ReturnsAsync((matchingProducts, 12));

    var expectedDtos = _fixture.CreateMany<ProductListDto>(12).ToList();
    _mockMapper.Setup(m => m.Map<List<ProductListDto>>(matchingProducts)).Returns(expectedDtos);

    object cacheValue = null;
    _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);

    // Act
    var result = await _sut.SearchProductsAsync(searchQuery);

    // Assert
    result.Should().NotBeNull();
    result.Products.Should().HaveCount(12);
}
```

---

**Test 4: SearchService - GetAutocompleteSuggestionsAsync - Returns Prefix Matches**

```csharp
[Fact]
public async Task GetAutocompleteSuggestionsAsync_WithPartialTerm_ReturnsPrefixMatches()
{
    // Arrange
    var partialTerm = "lav";
    var limit = 8;

    var matchingProductNames = new List<string>
    {
        "Lavender Dreams Candle",
        "Lavender Vanilla Candle",
        "French Lavender Pillar",
        "Lavender Mint Aromatherapy",
        "Lavender"
    };

    _mockProductRepository
        .Setup(r => r.GetProductNamesPrefixAsync(partialTerm, limit))
        .ReturnsAsync(matchingProductNames);

    object cacheValue = null;
    _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);

    // Act
    var result = await _sut.GetAutocompleteSuggestionsAsync(partialTerm, limit);

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(5);
    result.Should().Contain("Lavender Dreams Candle");
    result.Should().Contain("Lavender Vanilla Candle");

    _mockProductRepository.Verify(r => r.GetProductNamesPrefixAsync(partialTerm, limit), Times.Once);
}

[Fact]
public async Task GetAutocompleteSuggestionsAsync_WithTermLessThan2Chars_ReturnsEmpty()
{
    // Arrange
    var shortTerm = "l";
    var limit = 8;

    // Act
    var result = await _sut.GetAutocompleteSuggestionsAsync(shortTerm, limit);

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();

    _mockProductRepository.Verify(r => r.GetProductNamesPrefixAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
}
```

---

**Test 5: SearchService - GetSearchFacetsAsync - Returns Category Counts**

```csharp
[Fact]
public async Task GetSearchFacetsAsync_ReturnsCorrectCategoryCounts()
{
    // Arrange
    var searchQuery = new SearchQueryDto
    {
        SearchTerm = "candle",
        Page = 1,
        PageSize = 24
    };

    var floralCategoryId = Guid.NewGuid();
    var vanillaCategoryId = Guid.NewGuid();

    var facetData = new SearchFacetsDto
    {
        Categories = new List<FacetItemDto>
        {
            new FacetItemDto { Id = floralCategoryId, Name = "Floral", Count = 18 },
            new FacetItemDto { Id = vanillaCategoryId, Name = "Vanilla", Count = 24 }
        },
        PriceRanges = new List<PriceRangeFacetDto>
        {
            new PriceRangeFacetDto { Label = "$0-$20", Min = 0, Max = 20, Count = 12 },
            new PriceRangeFacetDto { Label = "$20-$40", Min = 20, Max = 40, Count = 30 }
        }
    };

    _mockProductRepository
        .Setup(r => r.GetSearchFacetsAsync(
            It.IsAny<string>(),
            It.IsAny<List<Guid>>(),
            It.IsAny<decimal?>(),
            It.IsAny<decimal?>(),
            It.IsAny<int?>(),
            It.IsAny<bool>()))
        .ReturnsAsync(facetData);

    object cacheValue = null;
    _mockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out cacheValue)).Returns(false);

    // Act
    var result = await _sut.GetSearchFacetsAsync(searchQuery);

    // Assert
    result.Should().NotBeNull();
    result.Categories.Should().HaveCount(2);
    result.Categories.Should().Contain(f => f.Name == "Floral" && f.Count == 18);
    result.Categories.Should().Contain(f => f.Name == "Vanilla" && f.Count == 24);
    result.PriceRanges.Should().HaveCount(2);
}
```

---

**Test 6: SearchService - TrackSearchAsync - Creates Search Record**

```csharp
[Fact]
public async Task TrackSearchAsync_WithValidData_CreatesProductSearchRecord()
{
    // Arrange
    var searchTerm = "lavender candle";
    var resultCount = 18;
    var customerId = Guid.NewGuid();
    var sessionId = "session-abc-123";

    var mockSearchRepository = new Mock<IProductSearchRepository>();
    _mockUnitOfWork.Setup(u => u.ProductSearches).Returns(mockSearchRepository.Object);

    mockSearchRepository
        .Setup(r => r.AddAsync(It.IsAny<ProductSearch>()))
        .ReturnsAsync((ProductSearch ps) => ps);

    _mockUnitOfWork
        .Setup(u => u.SaveChangesAsync())
        .ReturnsAsync(1);

    // Act
    await _sut.TrackSearchAsync(searchTerm, resultCount, customerId, sessionId);

    // Assert
    mockSearchRepository.Verify(r => r.AddAsync(It.Is<ProductSearch>(ps =>
        ps.SearchTerm == searchTerm &&
        ps.ResultCount == resultCount &&
        ps.CustomerId == customerId &&
        ps.SessionId == sessionId &&
        ps.SearchedAt != default
    )), Times.Once);

    _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
}
```

---

**Test 7: SearchService - SortProducts - Sorts By Price Ascending**

```csharp
[Fact]
public void SortProducts_WithSortPriceAsc_OrdersByPriceAscending()
{
    // Arrange
    var products = new List<Product>
    {
        _fixture.Build<Product>().With(p => p.Name, "Product A").With(p => p.Price, 30m).Create(),
        _fixture.Build<Product>().With(p => p.Name, "Product B").With(p => p.Price, 10m).Create(),
        _fixture.Build<Product>().With(p => p.Name, "Product C").With(p => p.Price, 20m).Create()
    };

    // Act
    var sorted = _sut.SortProducts(products.AsQueryable(), "price_asc").ToList();

    // Assert
    sorted.Should().HaveCount(3);
    sorted[0].Price.Should().Be(10m);
    sorted[1].Price.Should().Be(20m);
    sorted[2].Price.Should().Be(30m);
}

[Fact]
public void SortProducts_WithSortRating_OrdersByAverageRatingDescending()
{
    // Arrange
    var products = new List<Product>
    {
        _fixture.Build<Product>()
            .With(p => p.Name, "Product A")
            .With(p => p.Reviews, _fixture.Build<Review>().With(r => r.Rating, 3).CreateMany(5).ToList())
            .Create(),
        _fixture.Build<Product>()
            .With(p => p.Name, "Product B")
            .With(p => p.Reviews, _fixture.Build<Review>().With(r => r.Rating, 5).CreateMany(10).ToList())
            .Create(),
        _fixture.Build<Product>()
            .With(p => p.Name, "Product C")
            .With(p => p.Reviews, _fixture.Build<Review>().With(r => r.Rating, 4).CreateMany(8).ToList())
            .Create()
    };

    // Act
    var sorted = _sut.SortProducts(products.AsQueryable(), "rating").ToList();

    // Assert
    sorted.Should().HaveCount(3);
    sorted[0].Reviews.Average(r => r.Rating).Should().Be(5); // Product B
    sorted[1].Reviews.Average(r => r.Rating).Should().Be(4); // Product C
    sorted[2].Reviews.Average(r => r.Rating).Should().Be(3); // Product A
}
```

---

**Test 8: SearchService - GetDidYouMeanSuggestionAsync - Returns Spell Correction**

```csharp
[Fact]
public async Task GetDidYouMeanSuggestionAsync_WithMisspelledTerm_ReturnsCorrection()
{
    // Arrange
    var misspelledTerm = "lavendar"; // Should be "lavender"
    var productNames = new List<string>
    {
        "Lavender Dreams",
        "Vanilla Bourbon",
        "Ocean Breeze"
    };

    _mockProductRepository
        .Setup(r => r.GetAllProductNamesAsync())
        .ReturnsAsync(productNames);

    // Act
    var suggestion = await _sut.GetDidYouMeanSuggestionAsync(misspelledTerm);

    // Assert
    suggestion.Should().NotBeNullOrEmpty();
    suggestion.Should().Be("lavender"); // Levenshtein distance 1 (one character missing 'e')
}

[Fact]
public async Task GetDidYouMeanSuggestionAsync_WithCorrectTerm_ReturnsNull()
{
    // Arrange
    var correctTerm = "lavender";
    var productNames = new List<string> { "Lavender Dreams", "Vanilla Bourbon" };

    _mockProductRepository
        .Setup(r => r.GetAllProductNamesAsync())
        .ReturnsAsync(productNames);

    // Act
    var suggestion = await _sut.GetDidYouMeanSuggestionAsync(correctTerm);

    // Assert
    suggestion.Should().BeNullOrEmpty(); // No correction needed
}
```

---

### Integration Tests

**Test 1: SearchController - GET /api/search/products - Returns Search Results**

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using FluentAssertions;
using CandleStore.Api;
using CandleStore.Application.DTOs;
using CandleStore.Application.DTOs.Search;
using CandleStore.Application.DTOs.Products;

namespace CandleStore.Tests.Integration.Api.Controllers
{
    public class SearchControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public SearchControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task SearchProducts_WithValidSearchTerm_ReturnsMatchingProducts()
        {
            // Arrange
            var searchTerm = "lavender";
            await SeedTestProducts(); // Seed database with test products

            // Act
            var response = await _client.GetAsync($"/api/search/products?q={searchTerm}&page=1&pageSize=24");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<SearchResultsDto>>();
            apiResponse.Should().NotBeNull();
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Products.Should().NotBeEmpty();
            apiResponse.Data.TotalCount.Should().BeGreaterThan(0);

            // Verify all returned products match search term
            foreach (var product in apiResponse.Data.Products)
            {
                var matchesSearch = product.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                                   (product.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);
                matchesSearch.Should().BeTrue($"Product '{product.Name}' should contain search term '{searchTerm}'");
            }
        }

        [Fact]
        public async Task SearchProducts_WithPriceRangeFilter_ReturnsProductsInRange()
        {
            // Arrange
            await SeedTestProducts();
            var minPrice = 20;
            var maxPrice = 40;

            // Act
            var response = await _client.GetAsync($"/api/search/products?minPrice={minPrice}&maxPrice={maxPrice}&page=1&pageSize=24");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<SearchResultsDto>>();
            apiResponse.Data.Products.Should().NotBeEmpty();

            // Verify all products within price range
            foreach (var product in apiResponse.Data.Products)
            {
                product.Price.Should().BeGreaterThanOrEqualTo(minPrice);
                product.Price.Should().BeLessThanOrEqualTo(maxPrice);
            }
        }

        [Fact]
        public async Task SearchProducts_WithCategoryFilter_ReturnsOnlyProductsInCategory()
        {
            // Arrange
            await SeedTestProducts();
            var floralCategoryId = await GetCategoryIdByName("Floral");

            // Act
            var response = await _client.GetAsync($"/api/search/products?category={floralCategoryId}&page=1&pageSize=24");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<SearchResultsDto>>();
            apiResponse.Data.Products.Should().NotBeEmpty();

            // Verify all products in Floral category
            foreach (var product in apiResponse.Data.Products)
            {
                product.CategoryName.Should().Be("Floral");
            }
        }
    }
}
```

---

**Test 2: SearchController - GET /api/search/autocomplete - Returns Suggestions**

```csharp
[Fact]
public async Task GetAutocomplete_WithValidTerm_ReturnsSuggestions()
{
    // Arrange
    await SeedTestProducts();
    var partialTerm = "lav";

    // Act
    var response = await _client.GetAsync($"/api/search/autocomplete?term={partialTerm}&limit=8");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AutocompleteResultDto>>();
    apiResponse.Should().NotBeNull();
    apiResponse.Success.Should().BeTrue();
    apiResponse.Data.Suggestions.Should().NotBeEmpty();
    apiResponse.Data.Suggestions.Should().HaveCountLessThanOrEqualTo(8);

    // Verify all suggestions start with partial term (prefix match)
    foreach (var suggestion in apiResponse.Data.Suggestions)
    {
        suggestion.Should().StartWith("lav", StringComparison.OrdinalIgnoreCase);
    }
}

[Fact]
public async Task GetAutocomplete_WithShortTerm_ReturnsEmptyArray()
{
    // Arrange
    var shortTerm = "l"; // Less than 2 characters

    // Act
    var response = await _client.GetAsync($"/api/search/autocomplete?term={shortTerm}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<AutocompleteResultDto>>();
    apiResponse.Data.Suggestions.Should().BeEmpty();
}
```

---

**Test 3: SearchController - GET /api/search/facets - Returns Facet Counts**

```csharp
[Fact]
public async Task GetFacets_WithSearchQuery_ReturnsCorrectCounts()
{
    // Arrange
    await SeedTestProducts();
    var searchTerm = "candle";

    // Act
    var response = await _client.GetAsync($"/api/search/facets?q={searchTerm}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);

    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<SearchFacetsDto>>();
    apiResponse.Should().NotBeNull();
    apiResponse.Data.Categories.Should().NotBeEmpty();
    apiResponse.Data.PriceRanges.Should().NotBeEmpty();

    // Verify counts are positive integers
    foreach (var category in apiResponse.Data.Categories)
    {
        category.Count.Should().BeGreaterThan(0);
    }
}
```

---

### End-to-End (E2E) Tests

**Scenario 1: Customer Searches For Product And Applies Filters**

1. Customer navigates to homepage
2. **Expected:** Search bar displays in header navigation
3. Customer clicks in search bar
4. **Expected:** Search input focuses, recent searches dropdown may display
5. Customer types "lavender" in search bar
6. **Expected:** Autocomplete dropdown appears after 300ms with suggestions: "Lavender Dreams Candle", "Lavender Vanilla", etc.
7. Customer presses Enter to execute search
8. **Expected:** Navigate to /search?q=lavender
9. **Expected:** Search results page loads with matching products
10. **Expected:** Result count displays: "18 results for 'lavender'"
11. **Expected:** Search term "lavender" highlighted in yellow in product names
12. **Expected:** Filter sidebar displays on left with Categories, Price Range, Rating filters
13. Customer checks "Floral" category checkbox in filter sidebar
14. **Expected:** Results filter to only Floral category products
15. **Expected:** URL updates to /search?q=lavender&category={floralId}
16. **Expected:** Active filter pill displays: [Floral ✕]
17. **Expected:** Result count updates: "12 results"
18. Customer drags price slider to set range $20-$40
19. **Expected:** Results filter to products $20-$40
20. **Expected:** URL updates with minPrice=20&maxPrice=40
21. **Expected:** Active filter pills: [Floral ✕] [$20-$40 ✕]
22. **Expected:** Result count updates: "8 results"
23. Customer clicks ✕ on [Floral ✕] pill
24. **Expected:** Floral filter removed, results expand to all categories in $20-$40 range
25. **Expected:** Result count updates: "24 results"

---

**Scenario 2: Customer Uses Autocomplete To Find Specific Product**

1. Customer navigates to any page on site
2. Customer presses Ctrl+K keyboard shortcut
3. **Expected:** Search bar focuses (cursor in search input)
4. Customer types "lav"
5. **Expected:** After 300ms, autocomplete dropdown appears with 5-8 suggestions
6. **Expected:** Suggestions include: "Lavender Dreams Candle", "Lavender Vanilla Candle", etc.
7. Customer presses Arrow Down key
8. **Expected:** First autocomplete suggestion highlighted
9. Customer presses Arrow Down again
10. **Expected:** Second suggestion highlighted
11. Customer presses Enter
12. **Expected:** Navigate to product detail page for selected product OR execute search for that term
13. **Expected:** Autocomplete dropdown closes

---

**Scenario 3: Customer Encounters Zero Results And Uses Spell Check**

1. Customer searches for "lavendar" (misspelling of "lavender")
2. **Expected:** Search executes, navigates to /search?q=lavendar
3. **Expected:** Zero results returned
4. **Expected:** "No products found for 'lavendar'" message displays
5. **Expected:** Spell check suggestion appears: "Did you mean 'lavender'?"
6. Customer clicks "lavender" link in spell check suggestion
7. **Expected:** Re-execute search with corrected term: /search?q=lavender
8. **Expected:** 18 results display for "lavender"
9. **Expected:** Search term updated in search input box to "lavender"

---

**Scenario 4: Admin Views Search Analytics**

1. Admin logs into Admin Panel
2. Admin navigates to Reports > Search Analytics
3. **Expected:** Analytics dashboard displays
4. **Expected:** Overview metrics show: Total Searches, Unique Terms, Avg Results, Zero Results
5. **Expected:** Top Search Terms table displays with columns: Search Term, Count, Avg Results, Conv Rate
6. **Expected:** Zero-Result Searches table displays with opportunity revenue calculations
7. Admin clicks [Export CSV] button
8. **Expected:** CSV file downloads containing search analytics data
9. **Expected:** CSV includes columns: SearchTerm, SearchCount, AvgResults, ConversionRate, LastSearchedAt

---

### Performance Tests

**Benchmark 1: Search Query Performance**

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.DependencyInjection;

[MemoryDiagnoser]
public class SearchPerformanceBenchmarks
{
    private readonly SearchService _searchService;
    private readonly SearchQueryDto _simpleQuery;
    private readonly SearchQueryDto _complexQuery;

    public SearchPerformanceBenchmarks()
    {
        // Setup DI container and service
        var services = new ServiceCollection();
        // ... register dependencies
        var serviceProvider = services.BuildServiceProvider();
        _searchService = serviceProvider.GetRequiredService<SearchService>();

        _simpleQuery = new SearchQueryDto
        {
            SearchTerm = "lavender",
            Page = 1,
            PageSize = 24
        };

        _complexQuery = new SearchQueryDto
        {
            SearchTerm = "candle",
            CategoryIds = new List<Guid> { /* 3 categories */ },
            MinPrice = 20,
            MaxPrice = 80,
            MinRating = 4,
            InStockOnly = true,
            SortBy = "price_asc",
            Page = 1,
            PageSize = 24
        };
    }

    [Benchmark]
    public async Task SimpleSearch_SingleTerm()
    {
        await _searchService.SearchProductsAsync(_simpleQuery);
    }

    [Benchmark]
    public async Task ComplexSearch_MultipleFilters()
    {
        await _searchService.SearchProductsAsync(_complexQuery);
    }
}
```

**Target:** Simple search < 500ms, Complex search < 800ms (95th percentile)
**Pass Criteria:** 95% of searches complete within target
**Rationale:** Search is core to product discovery—must be fast to prevent user frustration

---

**Benchmark 2: Autocomplete Response Time**

```csharp
[Benchmark]
public async Task Autocomplete_PrefixMatch()
{
    await _searchService.GetAutocompleteSuggestionsAsync("lav", 8);
}
```

**Target:** < 50ms (95th percentile)
**Pass Criteria:** 99% of autocomplete requests < 100ms
**Rationale:** Autocomplete must feel instant—delays >100ms feel laggy, causing users to stop using feature

---

**Benchmark 3: Facet Count Calculation**

```csharp
[Benchmark]
public async Task CalculateFacets_AllDimensions()
{
    await _searchService.GetSearchFacetsAsync(_simpleQuery);
}
```

**Target:** < 300ms (95th percentile)
**Pass Criteria:** Facet counts calculate in < 500ms
**Rationale:** Facets require multiple GROUP BY queries—acceptable to be slightly slower than product search

---

### Regression Tests

**Regression Test 1: Existing Product Pages Not Broken By Search**

- Verify product detail pages load correctly with new search bar in header
- Ensure product CRUD operations (create, update, delete) work normally
- Verify product images, pricing, [Add to Cart] button all function
- Confirm no JavaScript errors in browser console related to search UI components

---

**Regression Test 2: Shopping Cart Functionality Unchanged**

- Verify adding products to cart works from search results
- Ensure cart calculations (subtotal, tax, shipping) remain correct
- Verify checkout process completes successfully with products added from search
- Confirm cart persistence across sessions works normally

---

### User Verification Steps

### Verification 1: Execute Basic Text Search

1. Navigate to homepage
2. Click in search bar (or press Ctrl+K)
3. Type "lavender" and press Enter
4. **Verify:** Navigate to /search?q=lavender
5. **Verify:** Search results display with matching products
6. **Verify:** Result count shows: "X results for 'lavender'"
7. **Verify:** Search term "lavender" is highlighted in product names (yellow background)
8. Click on one of the search results
9. **Verify:** Navigate to that product's detail page

---

### Verification 2: Use Autocomplete Suggestions

1. Click in search bar
2. Type "lav" (do not press Enter yet)
3. **Verify:** After ~300ms, autocomplete dropdown appears
4. **Verify:** Dropdown shows 5-8 product name suggestions starting with "lav"
5. Press Arrow Down key
6. **Verify:** First suggestion highlights
7. Press Enter
8. **Verify:** Navigate to product detail page or execute search for selected suggestion

---

### Verification 3: Apply Multiple Filters

1. Execute search for "candle"
2. **Verify:** Filter sidebar displays on left with categories, price range, rating
3. Check "Floral" category checkbox
4. **Verify:** Results filter to only Floral products
5. **Verify:** URL updates with category parameter
6. **Verify:** Active filter pill displays: [Floral ✕]
7. Drag price slider to set range $20-$40
8. **Verify:** Results filter further to Floral products $20-$40
9. **Verify:** Two active filter pills: [Floral ✕] [$20-$40 ✕]
10. Click ✕ on [Floral ✕] pill
11. **Verify:** Floral filter removed, price filter remains
12. **Verify:** Results expand to all categories in $20-$40 range

---

### Verification 4: Test Sort Options

1. Execute search for "candle"
2. Click Sort dropdown (default: "Relevance")
3. Select "Price: Low to High"
4. **Verify:** Results re-order with cheapest products first
5. **Verify:** URL updates with sort=price_asc parameter
6. Select "Rating: High to Low"
7. **Verify:** Results re-order with highest-rated products first
8. **Verify:** Products without reviews appear last or excluded (depending on configuration)

---

### Verification 5: Test Zero Results And Spell Check

1. Search for "lavendar" (intentional misspelling)
2. **Verify:** "No products found for 'lavendar'" message displays
3. **Verify:** Spell check suggestion appears: "Did you mean 'lavender'?"
4. Click "lavender" link
5. **Verify:** Search re-executes with corrected term
6. **Verify:** Results display for "lavender"

---

### Verification 6: Test Search History (Authenticated User)

1. Log in with customer account
2. Execute 3 different searches: "lavender", "vanilla", "eucalyptus"
3. Click in empty search bar (do not type)
4. **Verify:** Search history dropdown displays
5. **Verify:** Last 3 searches appear with timestamps ("2 minutes ago", etc.)
6. Click one of the recent searches
7. **Verify:** That search re-executes

---

### Verification 7: View Search Analytics (Admin)

1. Log into Admin Panel as admin user
2. Navigate to Reports > Search Analytics
3. **Verify:** Analytics dashboard displays
4. **Verify:** Overview metrics show: Total Searches, Unique Terms, Avg Results, Zero Results
5. **Verify:** Top Search Terms table populated with data
6. **Verify:** Zero-Result Searches table shows searches returning 0 products
7. Click [Export CSV]
8. **Verify:** CSV file downloads with search analytics data

---

### Verification 8: Test Mobile Search Experience

1. Open site on mobile device or browser DevTools mobile emulator (< 768px width)
2. **Verify:** Search bar displays full-width at top
3. Execute a search
4. **Verify:** Filter sidebar hidden by default on mobile
5. Click [Filters] button above results
6. **Verify:** Filter drawer slides in from left covering full screen
7. Apply a filter (check category checkbox)
8. Click [Apply Filters] button
9. **Verify:** Drawer closes, results update with filter applied
10. **Verify:** Product grid displays single column on mobile (< 576px)

---

### Verification 9: Test Pagination

1. Execute search returning >24 results (e.g., empty search = all products)
2. **Verify:** Pagination controls display below results
3. **Verify:** "Showing 1-24 of X results" displays
4. Click page 2
5. **Verify:** URL updates with page=2 parameter
6. **Verify:** Results 25-48 display
7. **Verify:** Page scrolls to top of results
8. Click page 1
9. **Verify:** Return to first 24 results

---

### Verification 10: Test Stock Availability Filter

1. Execute search for "candle"
2. Check "In Stock Only" checkbox in filter sidebar
3. **Verify:** Results filter to only products with StockQuantity > 0
4. **Verify:** No "Out of Stock" badges appear on any product cards
5. Uncheck "In Stock Only"
6. **Verify:** Out-of-stock products reappear with "Out of Stock" badges

---

## Implementation Prompt for Claude

(Implementation prompt section continues in next file due to length...)
## Implementation Prompt for Claude

### Implementation Overview

You are implementing an Advanced Search and Filtering system for the Candle Store e-commerce platform. This comprehensive search solution provides customers with powerful product discovery tools: autocomplete typeahead, multi-select faceted filters (category, price range, rating, size, tags), result sorting, pagination, search history tracking, popular search suggestions, spell-check ("Did you mean"), synonym expansion, and zero-result handling with alternative suggestions. The system spans multiple architectural layers: Domain entities (ProductSearch, SynonymGroup), Application services (SearchService with caching and analytics), Infrastructure repositories (ProductSearchRepository with full-text search queries), API endpoints (SearchController), and Blazor UI components (search bar, filter sidebar, autocomplete dropdown, result grid).

### Prerequisites

**Required Completed Tasks:**
- Task 001 (Solution Structure) - Clean Architecture setup
- Task 002 (NuGet Packages) - AutoMapper, EF Core, FluentValidation installed
- Task 013 (Product Management API) - Product catalog, IProductRepository, ProductListDto
- Task 021 (Order Management API) - Order data for "Best Selling" sort
- Task 029 (Reviews and Ratings) - Review data for rating filter

**NuGet Packages Needed:**
- AutoMapper (already installed)
- Microsoft.EntityFrameworkCore (already installed)
- Microsoft.Extensions.Caching.Memory (already installed)
- No additional packages required for basic implementation

**Optional (for advanced full-text search):**
- Elasticsearch integration: NEST package for Elasticsearch client
- Algolia integration: Algolia.Search package

**Database Migration Required:**
After implementing entities, create migration:

```bash
dotnet ef migrations add AddSearchTables --project src/CandleStore.Infrastructure --startup-project src/CandleStore.Api
dotnet ef database update --project src/CandleStore.Infrastructure --startup-project src/CandleStore.Api
```

---

### Step-by-Step Implementation

#### Step 1: Create Domain Entities

**File:** `src/CandleStore.Domain/Entities/ProductSearch.cs`

```csharp
using System;

namespace CandleStore.Domain.Entities
{
    /// <summary>
    /// Tracks every product search for analytics and search history.
    /// Supports both anonymous users (SessionId) and authenticated users (CustomerId).
    /// </summary>
    public class ProductSearch
    {
        public Guid Id { get; set; }

        /// <summary>
        /// The search term entered by the customer
        /// </summary>
        public string SearchTerm { get; set; }

        /// <summary>
        /// Number of products returned by the search query
        /// Used to identify zero-result searches (opportunities)
        /// </summary>
        public int ResultCount { get; set; }

        /// <summary>
        /// Customer who performed the search (null for guest users)
        /// </summary>
        public Guid? CustomerId { get; set; }

        /// <summary>
        /// Session identifier for anonymous users
        /// Format: "session-{guid}" generated client-side
        /// </summary>
        public string SessionId { get; set; }

        /// <summary>
        /// JSON string storing applied filters for analytics
        /// Example: {"categories": ["cat1","cat2"], "minPrice": 20, "maxPrice": 40}
        /// </summary>
        public string Filters { get; set; }

        /// <summary>
        /// Timestamp when search was executed
        /// </summary>
        public DateTime SearchedAt { get; set; }

        // Navigation properties
        public Customer Customer { get; set; }
    }
}
```

**File:** `src/CandleStore.Domain/Entities/SynonymGroup.cs`

```csharp
namespace CandleStore.Domain.Entities
{
    /// <summary>
    /// Defines synonym mappings for search query expansion.
    /// Example: "scent" → "smell, fragrance, aroma"
    /// </summary>
    public class SynonymGroup
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Base term (canonical term used in product tagging)
        /// Example: "scent"
        /// </summary>
        public string BaseTerm { get; set; }

        /// <summary>
        /// Comma-separated list of synonym terms
        /// Example: "smell,fragrance,aroma,perfume"
        /// </summary>
        public string Synonyms { get; set; }

        /// <summary>
        /// Whether this synonym group is active (can be disabled without deleting)
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// When this synonym group was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
```

---

#### Step 2: Configure Entity Framework Mappings

**File:** `src/CandleStore.Infrastructure/Persistence/Configurations/ProductSearchConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CandleStore.Domain.Entities;

namespace CandleStore.Infrastructure.Persistence.Configurations
{
    public class ProductSearchConfiguration : IEntityTypeConfiguration<ProductSearch>
    {
        public void Configure(EntityTypeBuilder<ProductSearch> builder)
        {
            builder.ToTable("ProductSearches");

            builder.HasKey(ps => ps.Id);

            builder.Property(ps => ps.SearchTerm)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(ps => ps.ResultCount)
                .IsRequired();

            builder.Property(ps => ps.SessionId)
                .HasMaxLength(100);

            builder.Property(ps => ps.Filters)
                .HasMaxLength(1000); // JSON string

            builder.Property(ps => ps.SearchedAt)
                .IsRequired();

            // Foreign key to Customers (nullable for guest users)
            builder.HasOne(ps => ps.Customer)
                .WithMany()
                .HasForeignKey(ps => ps.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            builder.HasIndex(ps => new { ps.SearchTerm, ps.SearchedAt })
                .HasDatabaseName("IX_ProductSearches_SearchTerm_SearchedAt");

            builder.HasIndex(ps => new { ps.CustomerId, ps.SearchedAt })
                .HasDatabaseName("IX_ProductSearches_CustomerId_SearchedAt");

            builder.HasIndex(ps => new { ps.SessionId, ps.SearchedAt })
                .HasDatabaseName("IX_ProductSearches_SessionId_SearchedAt");
        }
    }
}
```

**File:** `src/CandleStore.Infrastructure/Persistence/Configurations/SynonymGroupConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CandleStore.Domain.Entities;

namespace CandleStore.Infrastructure.Persistence.Configurations
{
    public class SynonymGroupConfiguration : IEntityTypeConfiguration<SynonymGroup>
    {
        public void Configure(EntityTypeBuilder<SynonymGroup> builder)
        {
            builder.ToTable("SynonymGroups");

            builder.HasKey(sg => sg.Id);

            builder.Property(sg => sg.BaseTerm)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sg => sg.Synonyms)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(sg => sg.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(sg => sg.CreatedAt)
                .IsRequired();

            // Unique constraint on BaseTerm
            builder.HasIndex(sg => sg.BaseTerm)
                .IsUnique()
                .HasDatabaseName("IX_SynonymGroups_BaseTerm_Unique");
        }
    }
}
```

**Update DbContext:**

**File:** `src/CandleStore.Infrastructure/Persistence/CandleStoreDbContext.cs`

```csharp
// Add to DbContext class:
public DbSet<ProductSearch> ProductSearches { get; set; }
public DbSet<SynonymGroup> SynonymGroups { get; set; }

// In OnModelCreating method:
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations

    modelBuilder.ApplyConfiguration(new ProductSearchConfiguration());
    modelBuilder.ApplyConfiguration(new SynonymGroupConfiguration());

    // Create full-text index for SQL Server (comment out for PostgreSQL)
    modelBuilder.Entity<Product>()
        .HasIndex(p => new { p.Name, p.Description, p.LongDescription })
        .HasDatabaseName("IX_Products_FullText");
    // For SQL Server full-text: Execute CREATE FULLTEXT INDEX separately via migration SQL
}
```

---

#### Step 3: Create DTOs

**File:** `src/CandleStore.Application/DTOs/Search/SearchQueryDto.cs`

```csharp
using System;
using System.Collections.Generic;

namespace CandleStore.Application.DTOs.Search
{
    /// <summary>
    /// DTO for search query parameters
    /// </summary>
    public class SearchQueryDto
    {
        /// <summary>
        /// Search term (searches Name, Description, Tags)
        /// </summary>
        public string SearchTerm { get; set; }

        /// <summary>
        /// Category IDs to filter by (multi-select, OR logic)
        /// </summary>
        public List<Guid> CategoryIds { get; set; }

        /// <summary>
        /// Minimum price filter (inclusive)
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// Maximum price filter (inclusive)
        /// </summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Minimum average rating filter (1-5 stars)
        /// </summary>
        public int? MinRating { get; set; }

        /// <summary>
        /// Only show products with StockQuantity > 0
        /// </summary>
        public bool InStockOnly { get; set; }

        /// <summary>
        /// Sort order: relevance, price_asc, price_desc, rating, newest, best_selling
        /// </summary>
        public string SortBy { get; set; }

        /// <summary>
        /// Page number (1-indexed)
        /// </summary>
        public int Page { get; set; } = 1;

        /// <summary>
        /// Results per page (default 24, max 100)
        /// </summary>
        public int PageSize { get; set; } = 24;
    }
}
```

**File:** `src/CandleStore.Application/DTOs/Search/SearchResultsDto.cs`

```csharp
using System.Collections.Generic;
using CandleStore.Application.DTOs.Products;

namespace CandleStore.Application.DTOs.Search
{
    public class SearchResultsDto
    {
        public List<ProductListDto> Products { get; set; }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public SearchFacetsDto Facets { get; set; }
    }

    public class SearchFacetsDto
    {
        public List<FacetItemDto> Categories { get; set; }
        public List<PriceRangeFacetDto> PriceRanges { get; set; }
        public List<RatingFacetDto> Ratings { get; set; }
        public List<FacetItemDto> Sizes { get; set; }
        public List<FacetItemDto> Tags { get; set; }
    }

    public class FacetItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class PriceRangeFacetDto
    {
        public string Label { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public int Count { get; set; }
    }

    public class RatingFacetDto
    {
        public int Stars { get; set; }
        public int Count { get; set; }
    }
}
```

---

#### Step 4: Create Search Service

**File:** `src/CandleStore.Application/Interfaces/ISearchService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CandleStore.Application.DTOs.Search;

namespace CandleStore.Application.Interfaces
{
    public interface ISearchService
    {
        Task<SearchResultsDto> SearchProductsAsync(SearchQueryDto query);
        Task<List<string>> GetAutocompleteSuggestionsAsync(string term, int limit = 8);
        Task<SearchFacetsDto> GetSearchFacetsAsync(SearchQueryDto query);
        Task<List<string>> GetPopularSearchesAsync(int limit = 10);
        Task<List<ProductSearchDto>> GetSearchHistoryAsync(Guid? customerId, string sessionId, int limit = 5);
        Task TrackSearchAsync(string searchTerm, int resultCount, Guid? customerId, string sessionId, string filters = null);
        Task<string> GetDidYouMeanSuggestionAsync(string searchTerm);
    }
}
```

**File:** `src/CandleStore.Application/Services/SearchService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using CandleStore.Application.DTOs.Search;
using CandleStore.Application.Interfaces;
using CandleStore.Application.Interfaces.Repositories;
using CandleStore.Domain.Entities;

namespace CandleStore.Application.Services
{
    public class SearchService : ISearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SearchService> _logger;

        public SearchService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMemoryCache cache,
            ILogger<SearchService> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cache = cache;
            _logger = logger;
        }

        public async Task<SearchResultsDto> SearchProductsAsync(SearchQueryDto query)
        {
            // Generate cache key from query parameters
            var cacheKey = GenerateCacheKey(query);

            // Try to get from cache
            if (_cache.TryGetValue(cacheKey, out SearchResultsDto cachedResults))
            {
                _logger.LogInformation("Search cache hit for query: {SearchTerm}", query.SearchTerm);
                return cachedResults;
            }

            _logger.LogInformation("Executing search query: {SearchTerm}", query.SearchTerm);

            // Execute search with filters
            var (products, totalCount) = await _unitOfWork.Products.SearchProductsAsync(
                query.SearchTerm,
                query.CategoryIds,
                query.MinPrice,
                query.MaxPrice,
                query.MinRating,
                query.InStockOnly,
                query.SortBy,
                (query.Page - 1) * query.PageSize, // skip
                query.PageSize // take
            );

            var productDtos = _mapper.Map<List<ProductListDto>>(products);

            // Get facets
            var facets = await GetSearchFacetsAsync(query);

            var results = new SearchResultsDto
            {
                Products = productDtos,
                TotalCount = totalCount,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize),
                Facets = facets
            };

            // Cache for 5 minutes
            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(5));

            return results;
        }

        public async Task<List<string>> GetAutocompleteSuggestionsAsync(string term, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                return new List<string>();

            var cacheKey = $"autocomplete_{term.ToLower()}_{limit}";

            if (_cache.TryGetValue(cacheKey, out List<string> cachedSuggestions))
            {
                return cachedSuggestions;
            }

            var suggestions = await _unitOfWork.Products.GetProductNamesPrefixAsync(term, limit);

            // Cache for 10 minutes
            _cache.Set(cacheKey, suggestions, TimeSpan.FromMinutes(10));

            return suggestions;
        }

        public async Task TrackSearchAsync(string searchTerm, int resultCount, Guid? customerId, string sessionId, string filters = null)
        {
            var productSearch = new ProductSearch
            {
                Id = Guid.NewGuid(),
                SearchTerm = searchTerm?.Trim(),
                ResultCount = resultCount,
                CustomerId = customerId,
                SessionId = sessionId,
                Filters = filters,
                SearchedAt = DateTime.UtcNow
            };

            await _unitOfWork.ProductSearches.AddAsync(productSearch);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Tracked search: '{SearchTerm}' returned {ResultCount} results", searchTerm, resultCount);
        }

        private string GenerateCacheKey(SearchQueryDto query)
        {
            // Create deterministic cache key from query parameters
            var categoryIds = query.CategoryIds != null ? string.Join(",", query.CategoryIds.OrderBy(id => id)) : "";
            return $"search_{query.SearchTerm}_{categoryIds}_{query.MinPrice}_{query.MaxPrice}_{query.MinRating}_{query.InStockOnly}_{query.SortBy}_{query.Page}_{query.PageSize}";
        }
    }
}
```

---

#### Step 5: Implement Repository Methods

**File:** `src/CandleStore.Infrastructure/Persistence/Repositories/ProductRepository.cs`

Add these methods to existing ProductRepository:

```csharp
public async Task<(List<Product>, int)> SearchProductsAsync(
    string searchTerm,
    List<Guid> categoryIds,
    decimal? minPrice,
    decimal? maxPrice,
    int? minRating,
    bool inStockOnly,
    string sortBy,
    int skip,
    int take)
{
    var query = _context.Products
        .Where(p => p.IsActive)
        .AsQueryable();

    // Apply text search filter
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        // For SQL Server: Use CONTAINS for full-text search
        query = query.Where(p =>
            EF.Functions.Like(p.Name, $"%{searchTerm}%") ||
            EF.Functions.Like(p.Description, $"%{searchTerm}%") ||
            EF.Functions.Like(p.LongDescription, $"%{searchTerm}%"));

        // For PostgreSQL: Use ts_vector
        // query = query.Where(p => EF.Functions.ToTsVector("english", p.Name).Matches(EF.Functions.ToTsQuery("english", searchTerm)));
    }

    // Apply category filter (OR logic)
    if (categoryIds != null && categoryIds.Any())
    {
        query = query.Where(p => categoryIds.Contains(p.CategoryId));
    }

    // Apply price range filter
    if (minPrice.HasValue)
    {
        query = query.Where(p => p.Price >= minPrice.Value);
    }

    if (maxPrice.HasValue)
    {
        query = query.Where(p => p.Price <= maxPrice.Value);
    }

    // Apply rating filter
    if (minRating.HasValue)
    {
        query = query.Where(p =>
            p.Reviews.Any() &&
            p.Reviews.Average(r => r.Rating) >= minRating.Value);
    }

    // Apply stock availability filter
    if (inStockOnly)
    {
        query = query.Where(p => p.StockQuantity > 0);
    }

    // Get total count before pagination
    var totalCount = await query.CountAsync();

    // Apply sorting
    query = sortBy switch
    {
        "price_asc" => query.OrderBy(p => p.Price),
        "price_desc" => query.OrderByDescending(p => p.Price),
        "rating" => query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
        "newest" => query.OrderByDescending(p => p.CreatedAt),
        "best_selling" => query.OrderByDescending(p => p.OrderItems.Sum(oi => oi.Quantity)), // Requires OrderItems navigation
        _ => query.OrderBy(p => p.Name) // Default: alphabetical
    };

    // Apply pagination
    var products = await query
        .Skip(skip)
        .Take(take)
        .Include(p => p.Category)
        .Include(p => p.ProductImages)
        .Include(p => p.Reviews) // For rating display
        .ToListAsync();

    return (products, totalCount);
}

public async Task<List<string>> GetProductNamesPrefixAsync(string prefix, int limit)
{
    return await _context.Products
        .Where(p => p.IsActive && EF.Functions.Like(p.Name, $"{prefix}%"))
        .Select(p => p.Name)
        .Distinct()
        .Take(limit)
        .ToListAsync();
}
```

---

#### Step 6: Create API Controller

**File:** `src/CandleStore.Api/Controllers/SearchController.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CandleStore.Application.DTOs;
using CandleStore.Application.DTOs.Search;
using CandleStore.Application.Interfaces;

namespace CandleStore.Api.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Search products with filters, sorting, pagination
        /// </summary>
        [HttpGet("products")]
        public async Task<ActionResult<ApiResponse<SearchResultsDto>>> SearchProducts(
            [FromQuery] string q,
            [FromQuery] List<Guid> category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int? minRating,
            [FromQuery] bool inStockOnly = false,
            [FromQuery] string sort = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 24)
        {
            var query = new SearchQueryDto
            {
                SearchTerm = q,
                CategoryIds = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating,
                InStockOnly = inStockOnly,
                SortBy = sort,
                Page = page,
                PageSize = Math.Min(pageSize, 100) // Cap at 100
            };

            var results = await _searchService.SearchProductsAsync(query);

            // Track search for analytics
            var customerId = User.Identity.IsAuthenticated
                ? Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                : (Guid?)null;
            var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

            await _searchService.TrackSearchAsync(
                q,
                results.TotalCount,
                customerId,
                sessionId,
                filters: null // Could serialize query object to JSON
            );

            return Ok(new ApiResponse<SearchResultsDto>
            {
                Success = true,
                Data = results
            });
        }

        /// <summary>
        /// Get autocomplete suggestions for typeahead
        /// </summary>
        [HttpGet("autocomplete")]
        public async Task<ActionResult<ApiResponse<AutocompleteResultDto>>> GetAutocomplete(
            [FromQuery] string term,
            [FromQuery] int limit = 8)
        {
            var suggestions = await _searchService.GetAutocompleteSuggestionsAsync(term, limit);

            return Ok(new ApiResponse<AutocompleteResultDto>
            {
                Success = true,
                Data = new AutocompleteResultDto { Suggestions = suggestions }
            });
        }

        /// <summary>
        /// Get facet counts for filters
        /// </summary>
        [HttpGet("facets")]
        public async Task<ActionResult<ApiResponse<SearchFacetsDto>>> GetFacets(
            [FromQuery] string q,
            [FromQuery] List<Guid> category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int? minRating,
            [FromQuery] bool inStockOnly = false)
        {
            var query = new SearchQueryDto
            {
                SearchTerm = q,
                CategoryIds = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinRating = minRating,
                InStockOnly = inStockOnly
            };

            var facets = await _searchService.GetSearchFacetsAsync(query);

            return Ok(new ApiResponse<SearchFacetsDto>
            {
                Success = true,
                Data = facets
            });
        }
    }

    public class AutocompleteResultDto
    {
        public List<string> Suggestions { get; set; }
    }
}
```

---

### Integration Points

**With Task 013 (Product Management):**
- Search queries IProductRepository for product data
- Only active, published products appear in search results
- Product images, prices, stock quantities displayed in results

**With Task 021 (Order Management):**
- "Best Selling" sort queries OrderItems table for quantity sold
- Search analytics can correlate with purchase conversion

**With Task 029 (Reviews and Ratings):**
- Rating filter queries Reviews table for average rating calculation
- Search results display product ratings alongside other metadata

**With Task 027 (Google Analytics):**
- Track search events as GA4 custom events: "search"
- Parameters: search_term, result_count, filters_applied
- Measure search-to-purchase conversion funnel

---

### Assumptions and Design Decisions

**Assumption 1: SQL Full-Text Search Sufficient for MVP**
For product catalogs <10,000 products, SQL Server CONTAINS or PostgreSQL ts_vector provides adequate search performance (<500ms). External search engines (Elasticsearch, Algolia) offer better performance and relevance ranking but require infrastructure setup. Implement SQL-based search first, architect with ISearchService interface to enable later migration to Elasticsearch without changing application code.

**Decision 1: Multi-Select Filters Use OR Logic**
When customer selects multiple categories (Floral + Vanilla), show products matching ANY selected category (OR), not ALL categories (AND). Rationale: Products have single CategoryId, so AND logic would return zero results. OR logic increases discoverability ("show me everything in these categories").

**Decision 2: Cache Search Results for 5 Minutes**
Popular searches (e.g., "lavender", "vanilla") executed frequently. Caching results for 5 minutes reduces database load by 60-80%. Tradeoff: Newly added products may not appear immediately in search results. Acceptable for e-commerce (inventory updates less frequently than 5 minutes).

**Decision 3: Autocomplete Prefix Match Only**
Autocomplete uses prefix matching (`LIKE '{term}%'`) not substring (`LIKE '%{term}%'`). Rationale: Prefix match aligns with customer typing behavior ("lav" suggests "Lavender Dreams") and performs better (uses index). Substring match requires full table scan.

---

### Testing the Implementation

**Manual Testing:**

1. **Verify search endpoint:**
```bash
curl "http://localhost:5000/api/search/products?q=lavender&page=1&pageSize=24"
```

2. **Verify autocomplete:**
```bash
curl "http://localhost:5000/api/search/autocomplete?term=lav&limit=8"
```

3. **Verify price filter:**
```bash
curl "http://localhost:5000/api/search/products?q=candle&minPrice=20&maxPrice=40"
```

4. **Verify category filter:**
```bash
curl "http://localhost:5000/api/search/products?category={floralCategoryId}&category={vanillaCategoryId}"
```

**Validation Steps:**

1. Execute search query → Verify results match search term
2. Apply price filter → Verify all results within price range
3. Apply category filter → Verify all results in selected categories
4. Test autocomplete → Verify suggestions start with search term prefix
5. Check database → Verify ProductSearch records created for tracking

---

### Next Steps After This Task

After completing this task:
1. Proceed to Task 033 (Unit Testing Setup) - Implement comprehensive unit tests for SearchService
2. Add Blazor UI components for search interface (search bar, filter sidebar, results grid)
3. Implement synonym management admin interface
4. Add search analytics dashboard for admin panel
5. (Optional) Integrate Elasticsearch for stores with >10,000 products

---

### Common Pitfalls to Avoid

❌ **Don't:** Use `LIKE '%{term}%'` for text search (full table scan, slow)
✅ **Do:** Use full-text search (CONTAINS, FreeText, ts_vector) with proper indexes

❌ **Don't:** Load all products into memory then filter (OutOfMemoryException)
✅ **Do:** Apply filters in SQL query via `Where()` clauses before `ToListAsync()`

❌ **Don't:** Execute facet count queries on every keystroke (database overload)
✅ **Do:** Debounce autocomplete to 300ms, cache facet counts for 5 minutes

❌ **Don't:** Return 404 when search returns zero results (confusing for users)
✅ **Do:** Return 200 OK with empty array and "No results" messaging

❌ **Don't:** Build search query via string concatenation (SQL injection risk)
✅ **Do:** Use parameterized queries (EF Core does this automatically with LINQ)

---

**END OF TASK 032**
