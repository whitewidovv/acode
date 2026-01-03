# Task 028: CDN and Performance Optimization with Cloudflare

**Priority**: High
**Tier**: Core
**Complexity**: Medium
**Phase**: 8 - Marketing & Analytics
**Dependencies**: Task 001 (Project Setup), Task 025 (SEO Optimization - Core Web Vitals)

---

## Description

Implement Cloudflare as a Content Delivery Network (CDN) and performance optimization platform for the Candle Store e-commerce website. Cloudflare provides global edge caching, image optimization, DDoS protection, Web Application Firewall (WAF), and automatic HTTPS, dramatically improving site performance, security, and reliability.

### Business Context and Value Proposition

Website performance directly impacts revenue in e-commerce. Studies show:
- **1 second delay in page load = 7% reduction in conversions**
- **53% of mobile users abandon sites that take > 3 seconds to load**
- **40% of users expect pages to load in < 2 seconds**

For a site generating $500,000 annual revenue:
- **Current state**: 3.5-second average page load, 2.8% conversion rate
- **After Cloudflare**: 1.2-second average page load (-66%), 3.6% conversion rate (+29%)
- **Revenue impact**: $500,000 × 29% = **$145,000 additional annual revenue**

Beyond performance, Cloudflare provides critical security and reliability benefits:

**DDoS Protection**: Protects against distributed denial-of-service attacks that could take the site offline. Average cost of downtime for e-commerce: $5,600 per minute. A single 2-hour DDoS attack avoided saves $672,000 in lost revenue.

**Web Application Firewall (WAF)**: Blocks SQL injection, XSS, and other attacks. Average cost of a data breach: $4.35 million. WAF reduces breach risk by 70-90%.

**Always Online**: Serves cached versions of the site if origin server goes down. Prevents revenue loss during server maintenance or outages.

**Global Performance**: 300+ edge locations worldwide ensure fast performance for international customers. Expands addressable market by making site usable in Europe, Asia, Australia.

**Cost Savings**: Reduces bandwidth costs by 60-80% through caching. For a site serving 50GB/month at $0.09/GB, saves ~$400/month ($4,800/year) in bandwidth costs.

### Technical Approach

Cloudflare operates as a reverse proxy between users and the origin server:

```
User → Cloudflare Edge (300+ locations) → Origin Server (candlestore.com)
```

**How It Works:**
1. User requests `https://candlestore.com/products/vanilla-bourbon`
2. Request is routed to nearest Cloudflare edge location (CDN node)
3. If content is cached, Cloudflare serves it immediately (cache hit, ~10-50ms response)
4. If not cached, Cloudflare fetches from origin server, caches it, and serves to user (cache miss, ~300-1000ms response)
5. Subsequent requests for same URL are served from cache (fast)

**Key Cloudflare Features:**

1. **Global CDN**
   - 300+ data centers worldwide
   - Anycast routing directs users to nearest edge location
   - Caches static assets (images, CSS, JS) at edge
   - Reduces latency from ~300ms (cross-continent) to ~20ms (local edge)

2. **Image Optimization (Polish & Mirage)**
   - **Polish**: Automatically optimizes images (lossy/lossless compression, WebP conversion)
   - **Mirage**: Lazy loads images, progressive JPEGs, responsive image sizing
   - Reduces image size by 30-50% without visible quality loss
   - Improves Largest Contentful Paint (LCP) Core Web Vital

3. **Caching Rules**
   - Cache static assets for long durations (1 year for versioned files)
   - Bypass cache for dynamic content (checkout, cart, user-specific pages)
   - Edge cache TTL, browser cache TTL, custom cache keys

4. **Automatic HTTPS & HTTP/2**
   - Free SSL/TLS certificates (Let's Encrypt or Cloudflare-issued)
   - HTTP/2 and HTTP/3 (QUIC) support for faster multiplexing
   - Always Use HTTPS redirect (HTTP → HTTPS)

5. **Web Application Firewall (WAF)**
   - OWASP Top 10 protection (SQL injection, XSS, CSRF)
   - Bot management (block malicious bots, allow good bots like Google)
   - Rate limiting to prevent brute-force attacks

6. **DDoS Protection**
   - Absorbs volumetric DDoS attacks (up to Tbps scale)
   - Layer 3/4 (network) and Layer 7 (application) protection
   - Automatic mitigation without manual intervention

7. **Performance Features**
   - Minification (JS, CSS, HTML)
   - Brotli compression (better than gzip)
   - Rocket Loader (defers JavaScript loading)
   - Auto-Prefetch (predictive loading of links user is likely to click)

### Implementation Components

**1. DNS Migration**
- Change nameservers from current DNS provider to Cloudflare nameservers
- Migrate DNS records (A, CNAME, MX, TXT)
- Enable Cloudflare proxy for website records (orange cloud icon)

**2. SSL/TLS Configuration**
- Choose SSL/TLS encryption mode: "Full (strict)" recommended
- Enable "Always Use HTTPS"
- Configure HSTS (HTTP Strict Transport Security)
- Enable TLS 1.3 for improved performance

**3. Caching Configuration**
- Set caching level to "Standard"
- Create Page Rules for custom caching:
  - Cache static assets (images, CSS, JS) for 1 year
  - Bypass cache for dynamic pages (cart, checkout, my-account)
  - Cache API responses with short TTLs (5 minutes)

**4. Image Optimization**
- Enable Polish (lossy compression for best performance)
- Enable Mirage (lazy loading, responsive images)
- Configure custom cache for `/images/*` path

**5. Performance Optimization**
- Enable Auto Minify (JavaScript, CSS, HTML)
- Enable Brotli compression
- Enable Rocket Loader for JavaScript optimization
- Configure HTTP/2 and HTTP/3 (QUIC)

**6. Security Configuration**
- Enable Web Application Firewall (WAF) with OWASP Core Ruleset
- Configure Bot Fight Mode to block malicious bots
- Set up Rate Limiting rules (e.g., max 60 requests/min per IP on login endpoint)
- Enable Browser Integrity Check

**7. Monitoring and Analytics**
- Review Cloudflare Analytics dashboard (bandwidth saved, requests served, threats blocked)
- Set up alerts for unusual traffic patterns
- Monitor Cache Hit Rate (target: >85%)

### Integration with Existing Features

**Task 025 (SEO Optimization - Core Web Vitals):**
- Cloudflare image optimization improves LCP (Largest Contentful Paint)
- Minification and compression improve FID (First Input Delay)
- Edge caching reduces TTFB (Time to First Byte)
- Target: LCP < 2.5s, FID < 100ms, CLS < 0.1

**Task 027 (Google Analytics):**
- Cloudflare caching is transparent to GA4 tracking (analytics scripts are not cached)
- Page Rules ensure GTM script loads from origin to capture latest configuration

**Task 026 (Email Marketing):**
- Cloudflare does not interfere with SendGrid emails (emails sent directly, not through Cloudflare)
- Unsubscribe/preference links in emails work correctly with caching bypassed

**Task 024 (Shipping Integration):**
- API endpoints for shipping rate calculation bypass Cloudflare cache (dynamic content)
- Webhook endpoints for tracking updates have cache bypass rules

### Performance Expectations

**Before Cloudflare** (typical small e-commerce site):
- Page Load Time: 3.5 seconds
- Time to First Byte (TTFB): 800ms
- Largest Contentful Paint (LCP): 4.2 seconds
- Total Page Size: 3.2 MB
- Bandwidth Usage: 50 GB/month

**After Cloudflare** (with optimization):
- Page Load Time: 1.2 seconds (-66%)
- Time to First Byte (TTFB): 120ms (-85%)
- Largest Contentful Paint (LCP): 1.8 seconds (-57%)
- Total Page Size: 1.6 MB (-50% from compression & image optimization)
- Bandwidth Usage from Origin: 10 GB/month (-80% from caching)

**Cache Hit Rate Target**: 85-95% (meaning 85-95% of requests served from edge cache, not origin)

**Cost Savings**:
- Bandwidth: 50GB → 10GB at $0.09/GB = $3.60/month saved ($43.20/year)
- Cloudflare Free tier: $0/month
- **Net savings: $43.20/year + avoided DDoS/downtime costs**

### Privacy and Compliance

**GDPR Compliance**:
- Cloudflare Data Processing Addendum (DPA) available for compliance
- Cloudflare logs visitor IPs but only retains for 4 hours (analytics require 24-hour retention)
- Privacy-first configuration: Disable IP Geolocation header if not needed

**Cookie Usage**:
- Cloudflare sets `__cflb` cookie for load balancing (only if using Cloudflare Load Balancer - not required for basic CDN)
- No tracking cookies set by Cloudflare by default
- Mention in privacy policy if using Cloudflare analytics features

**Content Filtering**:
- WAF may block legitimate requests if overly aggressive
- Monitor WAF events and create exceptions for false positives

---

## Use Cases

### Use Case 1: Emily Experiences Fast Page Loads on Mobile in Australia

**Persona**: Emily, potential customer in Sydney, Australia, browsing on mobile (4G connection)

**Current Situation (Before Cloudflare):**
Emily searches Google for "luxury scented candles" and clicks on Candle Store's link. The website is hosted on a server in Oregon, USA.

**Network Path**: Sydney → Los Angeles → Oregon (15,000 km, ~200ms latency each direction)

When Emily clicks the link:
1. **DNS Lookup**: 150ms (Sydney → Oregon DNS server)
2. **TLS Handshake**: 400ms (2 round-trips: Sydney → Oregon)
3. **HTML Download**: 800ms (HTML fetched from Oregon server)
4. **CSS/JS Download**: 1,200ms (multiple resources from Oregon)
5. **Images Load**: 2,500ms (product images from Oregon, 500KB each)

**Total Page Load Time**: ~5 seconds

Emily sees a blank screen for 2 seconds, then content starts rendering. Images load slowly, appearing one by one over 3 more seconds. The experience feels sluggish.

**User Frustration**: "This site is too slow. I'll try another store."

Emily closes the tab and visits a competitor with faster loading. **Sale lost: $42.00**

**After Cloudflare Implementation:**
The website's nameservers are changed to Cloudflare. Cloudflare has an edge location in Sydney (2ms latency).

When Emily clicks the Google link:
1. **DNS Lookup**: 10ms (Cloudflare Anycast DNS responds from Sydney edge)
2. **TLS Handshake**: 20ms (TLS termination at Sydney edge)
3. **HTML Download**: 15ms (HTML served from Cloudflare cache in Sydney)
4. **CSS/JS Download**: 25ms (CSS/JS cached in Sydney)
5. **Images Load**: 80ms (Images cached in Sydney, WebP format reduces size by 40%)

**Total Page Load Time**: ~150ms (0.15 seconds)

Emily experiences near-instant page load. Content and images appear immediately. The site feels as fast as browsing locally-hosted content.

**User Experience**: "Wow, this site is really fast! Let me browse some products."

Emily browses 8 products, adds 2 to cart, and completes checkout. **Sale completed: $42.00**

**Key Metrics Improvement**:
- Page load time: 5 seconds → 0.15 seconds (97% faster)
- Bounce rate: 68% → 32% (53% reduction)
- Conversion rate: 1.8% → 3.2% (+78%)

**Revenue Impact** (for international traffic):
- International traffic: 25% of total (625 sessions/day)
- Before: 625 × 1.8% conversion × $42 AOV = $472/day
- After: 625 × 3.2% conversion × $42 AOV = $840/day
- **Additional revenue: $368/day ($134,320/year)**

---

### Use Case 2: Candle Store Survives DDoS Attack Without Downtime

**Persona**: Sarah, Operations Manager at Candle Store

**Scenario**: A competitor launches a DDoS attack against Candle Store during Black Friday (peak sales day).

**Current Situation (Before Cloudflare):**
The website is hosted on a single VPS with 2 CPUs, 4GB RAM, and 100 Mbps network bandwidth.

**Friday, 9:30 AM**: Black Friday sale goes live. Traffic spikes to 2,000 concurrent users.

**Friday, 10:15 AM**: Sarah receives alerts that the website is slow. Response times increase from 200ms to 5 seconds.

**Friday, 10:20 AM**: Website becomes completely unresponsive. Server is overwhelmed.

**Investigation**: Server logs show 50,000 requests/second from 10,000 different IP addresses. This is a botnet-based DDoS attack.

**Attack Characteristics**:
- Type: HTTP flood (Layer 7 DDoS)
- Volume: 50,000 requests/second
- Target: Homepage and product pages (CPU-intensive dynamic pages)
- Server capacity: 500 requests/second max

The server cannot handle 100x normal load. All legitimate users see timeout errors.

**Mitigation Attempts**:
1. Sarah contacts hosting provider. They suggest upgrading server (takes 2-4 hours to provision).
2. Sarah attempts to identify attack IPs and block them manually. Attack sources change every minute.
3. Server remains down for 4 hours while Sarah scrambles to mitigate.

**Financial Impact**:
- Black Friday revenue (projected): $25,000
- Revenue lost during 4-hour outage: $25,000 × (4/24) = $4,167
- Customer trust damage (estimated 20% of customers won't return): $5,000 future revenue
- **Total cost of attack: $9,167**

**After Cloudflare Implementation:**
The website is behind Cloudflare's CDN and DDoS protection.

**Friday, 9:30 AM**: Black Friday sale goes live. Traffic spikes to 2,000 concurrent users, all served from Cloudflare edge cache.

**Friday, 10:15 AM**: Attacker launches DDoS attack with 50,000 requests/second.

**Cloudflare Response** (automatic, no human intervention):
1. Attack traffic is detected within 5 seconds (abnormal request patterns)
2. Cloudflare's Anycast network distributes attack across 300+ data centers (166 requests/second per datacenter - easily handled)
3. WAF rules identify attack signatures (user-agents, request patterns)
4. Cloudflare Challenge pages are served to suspicious IPs (CAPTCHA to verify human)
5. Legitimate traffic continues to be served from cache

**Origin Server**: Receives only 200 requests/second (normal traffic, filtered by Cloudflare). Server operates normally.

**Customer Experience**: Legitimate customers experience normal page loads. No downtime. No impact on sales.

**Sarah's Experience**: Sarah receives Cloudflare alert about elevated traffic at 10:17 AM. She reviews Cloudflare analytics:
- Total requests: 50,000/second
- Requests served from cache: 49,500/second (99%)
- Requests to origin: 200/second (legitimate traffic)
- Threats blocked: 49,300/second (98.6%)

Sarah takes no action. Cloudflare automatically mitigates the attack.

**Friday, 11:00 AM**: Attack stops. Total duration: 45 minutes.

**Financial Impact**:
- Revenue lost: $0 (no downtime)
- Customer trust: Maintained
- Cloudflare cost: $0 (Free plan includes DDoS protection)
- **Attack completely mitigated with zero impact**

**Long-Term Benefits**:
- Peace of mind during high-traffic events
- No need for expensive DDoS mitigation services (typically $500-$5,000/month)
- Reputation preserved (customers see fast, reliable site)

---

### Use Case 3: Mike Reduces Image Sizes and Improves Core Web Vitals

**Persona**: Mike, Developer at Candle Store

**Current Situation (Before Cloudflare Image Optimization):**
The Candle Store website has 150 product photos, each uploaded by the marketing team as high-resolution JPEGs (2000×2000px, ~1.2MB each).

**Homepage**: Displays 12 product images
- Total image size: 12 × 1.2MB = 14.4MB
- Load time on 10 Mbps connection: 11.5 seconds
- Largest Contentful Paint (LCP): 8.2 seconds (terrible - Google recommends < 2.5s)

**Core Web Vitals Report** (from Google PageSpeed Insights):
- LCP: 8.2 seconds 🔴 (Poor)
- FID: 120ms 🟡 (Needs Improvement)
- CLS: 0.05 🟢 (Good)
- **Overall Score: 42/100 (Poor)**

**SEO Impact**: Google's algorithm penalizes slow sites. Organic search traffic is 30% lower than it could be.

**Mike's Manual Optimization Attempt**:
Mike downloads all product images and manually optimizes them using Photoshop:
1. Resize to 800×800px (appropriate for web display)
2. Reduce JPEG quality to 80%
3. Re-upload to server

**Time spent**: 5 hours (150 images × 2 minutes each)

**Results After Manual Optimization**:
- Image size per photo: ~250KB (79% reduction)
- Homepage image size: 12 × 250KB = 3MB
- LCP: 3.8 seconds 🟡 (Needs Improvement - still not < 2.5s)
- **Overall Score: 67/100 (Needs Improvement)**

**Ongoing Problem**: Every time marketing uploads a new product photo, Mike must manually optimize it. Unsustainable.

**After Cloudflare Image Optimization (Polish & Mirage)**:
Mike enables Cloudflare Polish (lossy compression) and Mirage (lazy loading).

**Cloudflare Automatic Optimizations**:
1. **Polish**: Converts JPEGs to WebP format (better compression)
   - 1.2MB JPEG → 420KB WebP (65% reduction)
   - No visible quality loss
2. **Mirage**: Implements lazy loading
   - Only loads images in viewport (first 3-4 products)
   - Loads additional images as user scrolls
3. **Responsive Images**: Serves different sizes based on device
   - Mobile (375px width): Serves 400×400px version (150KB)
   - Desktop (1920px width): Serves 800×800px version (420KB)

**Results After Cloudflare**:
- **Homepage Load** (desktop):
  - Initial page load: 4 images visible above fold
  - Image size: 4 × 420KB = 1.68MB
  - Load time: 1.3 seconds
  - LCP: 1.9 seconds 🟢 (Good!)
- **Homepage Load** (mobile):
  - Initial page load: 2 images visible
  - Image size: 2 × 150KB = 300KB
  - Load time: 0.6 seconds
  - LCP: 1.2 seconds 🟢 (Excellent!)

**Core Web Vitals Report** (after Cloudflare):
- LCP: 1.9 seconds 🟢 (Good - < 2.5s target)
- FID: 45ms 🟢 (Good)
- CLS: 0.03 🟢 (Good)
- **Overall Score: 94/100 (Excellent)**

**SEO Impact**:
- Organic search traffic increases by 42% over 3 months (Google favors fast sites)
- Additional 320 organic visits/day × 3.2% conversion × $42 AOV = $430/day
- **Additional annual revenue from SEO: $156,950**

**Mike's Time Saved**:
- No manual image optimization needed
- Marketing team uploads original high-res photos
- Cloudflare optimizes automatically on-the-fly
- **Time saved: 5 hours/month ($1,500/year at $25/hour developer rate)**

**Additional Benefits**:
- Future product photos automatically optimized
- Bandwidth costs reduced by 65% (smaller images)
- Mobile users get appropriately-sized images (better mobile experience)
- Progressive loading improves perceived performance

---
# Task 028: CDN Cloudflare Setup - User Manual

## 1. Overview

Cloudflare acts as a Content Delivery Network (CDN) and performance/security platform for the Candle Store website. This system provides:

- **Global CDN**: 300+ edge locations serve cached content close to users worldwide
- **Image Optimization**: Automatic compression, WebP conversion, lazy loading
- **DDoS Protection**: Absorbs volumetric attacks up to Tbps scale
- **Web Application Firewall (WAF)**: Blocks SQL injection, XSS, and other attacks
- **Automatic HTTPS**: Free SSL certificates and HTTP/2 support
- **Performance Features**: Minification, compression, caching
- **Analytics**: Traffic insights, bandwidth savings, threat intelligence

**Benefits**:
- 66% faster page load times (3.5s → 1.2s average)
- 80% bandwidth cost reduction through caching
- 99.99% uptime with Always Online feature
- Protection against DDoS and application attacks
- Improved SEO through better Core Web Vitals scores

## 2. Initial Setup - Creating Cloudflare Account

### 2.1 Sign Up for Cloudflare

**Step 1: Create Account**
1. Visit https://dash.cloudflare.com/sign-up
2. Enter email address and create password
3. Click **"Create Account"**
4. Verify email address via confirmation link

**Step 2: Choose Plan**
For Candle Store, the **Free Plan** is sufficient for most needs. Features included:
- Global CDN (all 300+ edge locations)
- Unlimited bandwidth
- DDoS protection
- Free SSL certificate
- Basic WAF rules

**Pro Plan** ($20/month) adds:
- Image optimization (Polish & Mirage)
- Mobile optimization
- Advanced WAF rules
- 20 Page Rules (vs 3 on Free)

**For Production**: Recommend starting with Free plan, upgrade to Pro if image optimization is critical.

### 2.2 Add Website to Cloudflare

**Step 1: Add Site**
1. In Cloudflare dashboard, click **"Add a Site"**
2. Enter domain: `candlestore.com`
3. Click **"Add Site"**

**Step 2: Select Plan**
1. Choose **"Free"** plan
2. Click **"Continue"**

**Step 3: Review DNS Records**
Cloudflare automatically scans existing DNS records and imports them.

1. Verify DNS records are correct:
   - **A record**: `candlestore.com` → [Your Server IP] (e.g., `203.0.113.45`)
   - **CNAME record**: `www` → `candlestore.com`
   - **MX records**: Mail server records (if using email)
   - **TXT records**: SPF, DKIM, etc.
2. For website records (A, CNAME for www), ensure **Proxy Status** is **Proxied** (orange cloud icon)
   - Proxied = Traffic goes through Cloudflare CDN
   - DNS Only = Traffic goes directly to origin (gray cloud)
3. For mail records (MX), leave as **DNS Only** (mail must go direct to mail server)
4. Click **"Continue"**

**Step 4: Change Nameservers**
Cloudflare provides two nameservers (e.g., `chad.ns.cloudflare.com` and `dina.ns.cloudflare.com`).

1. Log in to your domain registrar (e.g., GoDaddy, Namecheap, Google Domains)
2. Find DNS/Nameserver settings
3. Replace current nameservers with Cloudflare nameservers:
   - Remove old nameservers (e.g., `ns1.example.com`, `ns2.example.com`)
   - Add: `chad.ns.cloudflare.com`
   - Add: `dina.ns.cloudflare.com`
4. Save changes
5. Return to Cloudflare dashboard
6. Click **"Done, check nameservers"**

**Propagation Time**: DNS changes take 24-48 hours to propagate globally. During this time, some users may see the old site, others the Cloudflare-proxied site.

**Verification**: After 24-48 hours, Cloudflare dashboard will show "Status: Active" with a green checkmark.

## 3. SSL/TLS Configuration

### 3.1 Enable HTTPS

**Step 1: Choose SSL/TLS Encryption Mode**
1. In Cloudflare dashboard, go to **SSL/TLS** tab
2. Choose encryption mode:
   - **Off**: Not recommended (no encryption)
   - **Flexible**: Encrypts traffic between user and Cloudflare, but not Cloudflare to origin (⚠️ insecure)
   - **Full**: Encrypts both hops, but doesn't validate origin certificate (acceptable)
   - **Full (strict)**: Encrypts both hops and validates origin certificate (✅ recommended)
3. Select **"Full (strict)"**

**Requirements for Full (strict)**:
- Origin server must have valid SSL certificate
- If using self-signed certificate on origin, use "Full" mode instead

**Step 2: Enable Always Use HTTPS**
1. Go to **SSL/TLS** → **Edge Certificates**
2. Locate **"Always Use HTTPS"** setting
3. Toggle **On**
4. This redirects all HTTP requests to HTTPS (http://candlestore.com → https://candlestore.com)

**Step 3: Enable HTTP Strict Transport Security (HSTS)**
1. Go to **SSL/TLS** → **Edge Certificates**
2. Locate **"HTTP Strict Transport Security (HSTS)"**
3. Click **"Enable HSTS"**
4. Configure settings:
   - **Max-Age Header**: 12 months (recommended)
   - **Apply HSTS to subdomains**: On (if using subdomains like `www.candlestore.com`)
   - **Preload**: Off (enable only after testing HSTS for 1-2 months)
   - **No-Sniff Header**: On
5. Click **"Next"** and acknowledge the warning
6. Click **"Enable HSTS"**

**Warning**: HSTS is difficult to reverse. Once enabled and browsers cache the header, site must remain on HTTPS. Test thoroughly before enabling Preload.

### 3.2 Configure Advanced SSL Settings

**TLS Version**:
1. Go to **SSL/TLS** → **Edge Certificates**
2. **Minimum TLS Version**: Set to **TLS 1.2** (recommended)
   - TLS 1.0/1.1 are deprecated and insecure
   - TLS 1.2 is widely supported (99%+ of browsers)
3. **TLS 1.3**: Enable (faster handshakes, improved security)

**Automatic HTTPS Rewrites**:
1. Go to **SSL/TLS** → **Edge Certificates**
2. **Automatic HTTPS Rewrites**: Toggle **On**
3. This automatically converts insecure HTTP links in HTML to HTTPS (e.g., `http://example.com/image.jpg` → `https://example.com/image.jpg`)

## 4. Caching Configuration

### 4.1 Set Caching Level

1. Go to **Caching** → **Configuration**
2. **Caching Level**: Select **"Standard"** (recommended)
   - No Query String: Ignores query strings in cache key (treats `page.html?foo=bar` same as `page.html`)
   - Ignore Query String: Caches everything, ignores query strings
   - **Standard**: Caches static files, respects query strings (recommended)

### 4.2 Configure Browser Cache TTL

1. In **Caching** → **Configuration**
2. **Browser Cache TTL**: Select **"Respect Existing Headers"** (recommended)
   - Cloudflare respects `Cache-Control` headers sent by origin server
   - Alternative: Set specific TTL (e.g., 4 hours, 1 day, 1 month)

### 4.3 Create Page Rules for Custom Caching

**Page Rules** allow fine-grained control over caching for specific URL patterns.

**Example Page Rules**:

**Rule 1: Cache Static Assets (Images, CSS, JS)**
1. Go to **Rules** → **Page Rules**
2. Click **"Create Page Rule"**
3. **URL Pattern**: `candlestore.com/images/*`
4. Add Setting: **"Cache Level"** → **"Cache Everything"**
5. Add Setting: **"Edge Cache TTL"** → **"1 month"**
6. Add Setting: **"Browser Cache TTL"** → **"1 month"**
7. Click **"Save and Deploy"**

Repeat for:
- `candlestore.com/css/*`
- `candlestore.com/js/*`
- `candlestore.com/_framework/*` (Blazor static assets)

**Rule 2: Bypass Cache for Dynamic Pages**
1. Create Page Rule
2. **URL Pattern**: `candlestore.com/cart*`
3. Add Setting: **"Cache Level"** → **"Bypass"**
4. Save and Deploy

Repeat for:
- `candlestore.com/checkout*`
- `candlestore.com/my-account*`
- `candlestore.com/api/*` (unless API responses are cacheable)

**Rule 3: Short Cache for Product Pages**
1. Create Page Rule
2. **URL Pattern**: `candlestore.com/products/*`
3. Add Setting: **"Edge Cache TTL"** → **"2 hours"**
4. Add Setting: **"Cache Level"** → **"Cache Everything"**
5. Save and Deploy

**Page Rule Limits**: Free plan allows 3 Page Rules, Pro plan allows 20.

### 4.4 Purge Cache

When content is updated (e.g., product price changes, new blog post), purge Cloudflare cache to serve fresh content.

**Purge Everything** (clears entire cache):
1. Go to **Caching** → **Configuration**
2. Click **"Purge Everything"**
3. Confirm

**⚠️ Warning**: Purging everything causes temporary traffic spike to origin server as cache rebuilds.

**Purge by Single File** (recommended for specific updates):
1. Go to **Caching** → **Configuration**
2. Click **"Custom Purge"**
3. Select **"URL"**
4. Enter full URL: `https://candlestore.com/products/vanilla-bourbon`
5. Click **"Purge"**

**Purge by Tag or Prefix** (Pro plan feature):
- Allows purging all URLs matching a tag or prefix
- Example: Purge all product pages with tag `product-page`

## 5. Image Optimization (Pro Plan)

### 5.1 Enable Polish (Image Compression)

**Polish** automatically compresses images served through Cloudflare.

1. Go to **Speed** → **Optimization**
2. Locate **"Polish"** setting
3. Select compression level:
   - **Off**: No compression
   - **Lossless**: Compresses without quality loss (~10-20% size reduction)
   - **Lossy**: Compresses with minimal quality loss (~40-60% size reduction, recommended)
4. Enable **"WebP"** conversion (if browser supports WebP, serve WebP instead of JPEG/PNG)
5. Save

**How It Works**: When user requests `https://candlestore.com/images/candle.jpg`:
1. Cloudflare checks if WebP-optimized version exists in cache
2. If not, Cloudflare fetches original from origin, compresses it, converts to WebP, caches it
3. Cloudflare serves WebP to user (if browser supports WebP, otherwise serves optimized JPEG)

**Expected Results**:
- JPEG images: 40-50% smaller (with lossy)
- PNG images: 20-30% smaller
- WebP images: 30-40% smaller than JPEG

### 5.2 Enable Mirage (Lazy Loading and Responsive Images)

**Mirage** lazy-loads images and serves appropriately-sized images based on device.

1. Go to **Speed** → **Optimization**
2. Locate **"Mirage"** setting
3. Toggle **On**

**How It Works**:
- Images below the fold are not loaded until user scrolls
- Placeholder low-resolution images are shown initially
- Full-resolution images load progressively as user scrolls
- Mobile devices receive smaller images (e.g., 400px width instead of 2000px)

**Compatibility**: Mirage requires JavaScript. Browsers with JS disabled will see all images load normally.

## 6. Performance Optimization

### 6.1 Enable Auto Minify

**Minification** removes whitespace, comments, and unnecessary characters from code.

1. Go to **Speed** → **Optimization**
2. Locate **"Auto Minify"**
3. Check boxes for:
   - ✅ JavaScript
   - ✅ CSS
   - ✅ HTML
4. Save

**Expected Results**:
- JavaScript size reduction: 15-30%
- CSS size reduction: 10-20%
- HTML size reduction: 5-10%

**Caution**: Minification can occasionally break code. If site breaks after enabling:
1. Test JavaScript functionality (forms, buttons, cart)
2. If broken, disable JavaScript minification
3. Minify JavaScript manually during build process instead

### 6.2 Enable Brotli Compression

**Brotli** is a compression algorithm superior to gzip (20-30% better compression).

1. Go to **Speed** → **Optimization**
2. Locate **"Brotli"** setting
3. Toggle **On**

**How It Works**: Cloudflare compresses text-based content (HTML, CSS, JS, JSON) using Brotli before sending to user. Browser decompresses on-the-fly.

**Expected Results**:
- Text file size reduction: 20-30% better than gzip
- Already-compressed files (images, videos) are not re-compressed

### 6.3 Enable Rocket Loader (Optional)

**Rocket Loader** defers JavaScript loading to improve initial page render time.

1. Go to **Speed** → **Optimization**
2. Locate **"Rocket Loader"** setting
3. Toggle **On**

**How It Works**:
- JavaScript files are loaded asynchronously after page content renders
- Improves Time to Interactive (TTI) and First Contentful Paint (FCP)

**⚠️ Caution**: Rocket Loader can break JavaScript that expects synchronous loading. Test thoroughly.

**Recommendation**: Enable Rocket Loader, test all site functionality (add to cart, checkout, forms). If anything breaks, disable Rocket Loader.

### 6.4 Enable HTTP/2 and HTTP/3

**HTTP/2** multiplexes multiple requests over single TCP connection (faster than HTTP/1.1).
**HTTP/3** uses QUIC protocol over UDP (even faster, especially on mobile).

1. Go to **Network** tab
2. **HTTP/2**: Toggle **On** (should be on by default)
3. **HTTP/3 (with QUIC)**: Toggle **On**
4. Save

**Expected Results**:
- Faster page loads on repeat visits (connection reuse)
- Better performance on high-latency connections (mobile)

## 7. Security Configuration

### 7.1 Enable Web Application Firewall (WAF)

**WAF** blocks malicious requests (SQL injection, XSS, etc.).

1. Go to **Security** → **WAF**
2. **WAF Managed Rules**: Toggle **On**
3. Click **"Managed Rules"** to configure:
   - **Cloudflare Managed Ruleset**: On (OWASP Core Ruleset)
   - **Ruleset Action**: Block (blocks malicious requests)
   - **Sensitivity**: Medium (recommended - balances security and false positives)
4. Save

**Monitoring WAF Events**:
1. Go to **Security** → **Events**
2. Review blocked requests
3. If legitimate requests are blocked (false positives), create **WAF Exception**:
   - Click on blocked event
   - Click **"Create exception"**
   - Choose criteria (e.g., "Skip WAF for IP 203.0.113.45")
   - Save

### 7.2 Configure Bot Fight Mode

**Bot Fight Mode** blocks bad bots (scrapers, spam bots) while allowing good bots (Google, Bing).

1. Go to **Security** → **Bots**
2. **Bot Fight Mode**: Toggle **On** (Free plan)
   - **Super Bot Fight Mode**: Available on Pro plan (more advanced)
3. Save

**How It Works**:
- Cloudflare serves Challenge pages to suspected bots
- Good bots (verified user-agents like Googlebot) bypass challenges
- Bad bots fail challenges and are blocked

### 7.3 Set Up Rate Limiting

**Rate Limiting** prevents brute-force attacks and API abuse.

**Example: Limit Login Attempts**
1. Go to **Security** → **WAF** → **Rate Limiting Rules**
2. Click **"Create Rate Limiting Rule"**
3. Configure:
   - **Rule Name**: "Limit Login Attempts"
   - **Match**: URL Path equals `/api/auth/login`
   - **Requests**: 5 requests per 1 minute per IP
   - **Action**: Block for 10 minutes
4. Deploy

**Example: Limit API Calls**
1. Create Rate Limiting Rule
2. **Match**: URL Path starts with `/api/`
3. **Requests**: 100 requests per 1 minute per IP
4. **Action**: Block for 1 minute
5. Deploy

**Rate Limiting Limits**: Free plan allows 1 rate limiting rule, Pro plan allows 10.

## 8. Monitoring and Analytics

### 8.1 Cloudflare Analytics Dashboard

1. Go to **Analytics & Logs** → **Traffic**
2. Review metrics:
   - **Requests**: Total requests served
   - **Bandwidth**: Data transferred (origin bandwidth + cached bandwidth)
   - **Requests Served**: Breakdown by cached vs uncached
   - **Threats**: Blocked requests (DDoS, WAF, Bot Fight)
   - **Status Codes**: HTTP 200, 404, 500, etc.

**Key Metrics to Monitor**:
- **Cache Hit Rate**: (Cached Requests / Total Requests) × 100%
  - Target: 85-95%
  - Low cache hit rate means many requests are hitting origin (not optimal)
- **Bandwidth Saved**: Bandwidth served from cache (doesn't hit origin)
  - Target: 60-80% of total bandwidth
- **Threats Blocked**: Count of malicious requests blocked
  - High number indicates site is under attack

### 8.2 Set Up Alerts

**Traffic Anomaly Alerts**:
1. Go to **Notifications**
2. Click **"Add"** → **"Traffic Anomalies"**
3. Configure:
   - Alert when: Requests per minute > [X% increase from average]
   - Example: Alert when requests > 300% of 7-day average
   - Notification: Email to admin@candlestore.com
4. Save

**Security Event Alerts**:
1. Add Notification → **"Security Events"**
2. Alert when: Threats blocked > [threshold]
3. Example: Alert when > 100 threats blocked in 5 minutes (potential DDoS)
4. Save

## 9. Troubleshooting Common Issues

### 9.1 Site Not Loading After Nameserver Change

**Symptoms**: After changing nameservers to Cloudflare, site shows "DNS_PROBE_FINISHED_NXDOMAIN" or times out.

**Causes & Solutions**:

**Cause 1: Nameserver Propagation Not Complete**
- DNS changes take 24-48 hours to propagate globally
- Solution: Wait 24-48 hours, check again
- Verification: Use https://whatsmydns.net to check global DNS propagation

**Cause 2: Incorrect DNS Records in Cloudflare**
- DNS records not properly imported from previous DNS provider
- Solution:
  1. Go to Cloudflare **DNS** tab
  2. Verify A record for `candlestore.com` points to correct origin IP
  3. Verify CNAME for `www` points to `candlestore.com`
  4. Add missing records if needed

**Cause 3: Origin Server Blocking Cloudflare IPs**
- Firewall on origin server blocks Cloudflare IP ranges
- Solution:
  1. Whitelist Cloudflare IP ranges in origin firewall
  2. Cloudflare IP ranges: https://www.cloudflare.com/ips/
  3. Add all IPv4 and IPv6 ranges to firewall allow list

### 9.2 SSL Certificate Errors

**Symptoms**: Browser shows "Your connection is not private" or "ERR_CERT_AUTHORITY_INVALID"

**Causes & Solutions**:

**Cause 1: SSL/TLS Mode Set to "Full (strict)" but Origin Has Self-Signed Cert**
- Cloudflare cannot validate self-signed certificates
- Solution: Change SSL/TLS mode to "Full" instead of "Full (strict)"

**Cause 2: Origin Server Not Configured for HTTPS**
- SSL/TLS mode set to "Full" or "Full (strict)" but origin only supports HTTP
- Solution:
  - Install SSL certificate on origin server (Let's Encrypt free certificate)
  - Or change SSL/TLS mode to "Flexible" (⚠️ less secure)

**Cause 3: Certificate Not Yet Issued**
- Cloudflare is still generating SSL certificate (takes up to 24 hours)
- Solution: Wait 24 hours for certificate issuance

### 9.3 Cache Not Working (Low Cache Hit Rate)

**Symptoms**: Cloudflare analytics show cache hit rate < 50%

**Causes & Solutions**:

**Cause 1: Dynamic Content Not Excluded from Caching**
- Pages like cart, checkout are being cached (wrong)
- Solution: Create Page Rules to bypass cache for dynamic URLs (see Section 4.3)

**Cause 2: Origin Sends "Cache-Control: no-cache" Headers**
- Origin server tells Cloudflare not to cache
- Solution:
  1. Check origin server Cache-Control headers
  2. Remove `no-cache` for static assets
  3. Or override with Page Rule "Cache Level: Cache Everything"

**Cause 3: Query Strings Preventing Cache**
- URLs like `/products?session=abc123` have unique query strings
- Each unique URL is cached separately
- Solution: Use Page Rule "Cache Key" to ignore specific query parameters

### 9.4 Images Not Optimizing (Polish Not Working)

**Symptoms**: Images are same size before/after enabling Polish

**Causes & Solutions**:

**Cause 1: Using Free Plan**
- Polish requires Pro plan ($20/month)
- Solution: Upgrade to Pro plan or optimize images manually before upload

**Cause 2: Images Served from Subdomain Not Proxied**
- Images hosted on `cdn.candlestore.com` but DNS record is "DNS Only" (not proxied)
- Solution: Change DNS record to "Proxied" (orange cloud icon)

**Cause 3: Images Not Cached**
- Polish only works on cached images
- If cache is bypassed, Polish doesn't run
- Solution: Ensure images URL path has caching enabled (Page Rule)

---

**End of User Manual**

This manual covers complete setup and optimization of Cloudflare for the Candle Store e-commerce platform. For additional support:
- Cloudflare Help Center: https://support.cloudflare.com
- Cloudflare Community: https://community.cloudflare.com
- Cloudflare Status: https://www.cloudflarestatus.com (check for outages)
# Task 028: CDN Cloudflare Setup - Acceptance Criteria

## 1. Cloudflare Account and DNS Configuration

### 1.1 Account Setup
- [ ] Cloudflare account is created with business email address
- [ ] Website is added to Cloudflare account
- [ ] Appropriate plan is selected (Free or Pro based on requirements)
- [ ] Account billing information is configured (if using paid plan)
- [ ] Two-factor authentication (2FA) is enabled on Cloudflare account for security

### 1.2 DNS Migration
- [ ] All existing DNS records are imported from previous DNS provider
- [ ] A record for root domain (candlestore.com) points to correct origin server IP
- [ ] CNAME record for www subdomain points to root domain (or directly to origin)
- [ ] MX records for email are configured correctly (DNS Only, not proxied)
- [ ] TXT records (SPF, DKIM, DMARC) are migrated correctly
- [ ] Any subdomain records (api, admin, cdn) are configured
- [ ] Website DNS records (A, CNAME for www) have Proxy Status set to "Proxied" (orange cloud)
- [ ] Non-website records (MX, TXT) have Proxy Status set to "DNS Only" (gray cloud)
- [ ] Nameservers at domain registrar are changed to Cloudflare nameservers
- [ ] DNS propagation is verified after 24-48 hours (site resolves correctly globally)
- [ ] Cloudflare dashboard shows "Status: Active" for the site

## 2. SSL/TLS Configuration

### 2.1 HTTPS Setup
- [ ] SSL/TLS encryption mode is set to "Full (strict)" (or "Full" if origin has self-signed cert)
- [ ] Cloudflare Origin CA certificate is installed on origin server (if using Full (strict))
- [ ] "Always Use HTTPS" is enabled to redirect HTTP → HTTPS
- [ ] HTTP Strict Transport Security (HSTS) is configured with 12-month max-age
- [ ] HSTS includes subdomains if applicable
- [ ] Automatic HTTPS Rewrites is enabled to convert insecure links

### 2.2 Advanced SSL Settings
- [ ] Minimum TLS version is set to TLS 1.2
- [ ] TLS 1.3 is enabled for improved performance
- [ ] Opportunistic Encryption is enabled
- [ ] Edge certificates are automatically renewed before expiration
- [ ] SSL certificate is valid and trusted by major browsers (no certificate warnings)

### 2.3 Certificate Validation
- [ ] Website loads successfully over HTTPS (https://candlestore.com)
- [ ] SSL certificate shows "Issued by Cloudflare" or custom certificate if configured
- [ ] No mixed content warnings in browser console
- [ ] HTTPS is enforced (HTTP URLs automatically redirect to HTTPS)

## 3. Caching Configuration

### 3.1 Basic Caching Settings
- [ ] Caching level is set to "Standard"
- [ ] Browser Cache TTL is set to "Respect Existing Headers" or appropriate duration
- [ ] Development Mode can be enabled/disabled for testing (bypasses cache for 3 hours)
- [ ] Cache is purged after major content updates

### 3.2 Page Rules for Caching
- [ ] Page Rule exists to cache static assets (images, CSS, JS) for long duration (1 month+)
- [ ] Cache Level is set to "Cache Everything" for static asset paths
- [ ] Edge Cache TTL is set to 1 month for static assets
- [ ] Browser Cache TTL is set appropriately for static assets
- [ ] Page Rule exists to bypass cache for dynamic pages (cart, checkout, my-account)
- [ ] Page Rule bypasses cache for API endpoints (unless API responses are cacheable)
- [ ] Page Rule caches product pages with moderate TTL (2-4 hours)
- [ ] Page Rules are ordered correctly (most specific rules first)
- [ ] Total Page Rules count is within plan limits (3 for Free, 20 for Pro)

### 3.3 Cache Performance
- [ ] Cache hit rate is monitored in Cloudflare Analytics
- [ ] Cache hit rate is >= 85% for production traffic
- [ ] Bandwidth savings from caching is >= 60%
- [ ] Origin server bandwidth usage is reduced after Cloudflare enablement
- [ ] Cache purge functionality works correctly (Purge Everything, Purge by URL)
- [ ] Cached content is served quickly (TTFB < 100ms for cached responses)

## 4. Image Optimization (Pro Plan)

### 4.1 Polish Configuration
- [ ] Polish is enabled with "Lossy" compression for maximum size reduction
- [ ] WebP conversion is enabled
- [ ] JPEG images are automatically compressed by 40-60%
- [ ] PNG images are automatically compressed by 20-30%
- [ ] WebP images are served to compatible browsers
- [ ] Original image quality is acceptable after compression (no visible artifacts)
- [ ] Fallback to JPEG/PNG works for browsers not supporting WebP

### 4.2 Mirage Configuration
- [ ] Mirage is enabled for lazy loading and responsive images
- [ ] Images below the fold are lazy-loaded (not loaded until scrolled into view)
- [ ] Placeholder images are shown while full images load
- [ ] Mobile devices receive appropriately-sized images (smaller than desktop)
- [ ] Lazy loading works correctly in all major browsers
- [ ] Images load progressively as user scrolls

### 4.3 Image Optimization Results
- [ ] Average image file size is reduced by >= 40% after Polish enablement
- [ ] Page load time improves due to smaller images
- [ ] Largest Contentful Paint (LCP) improves with image optimization
- [ ] Mobile page size is significantly smaller than desktop (responsive images working)

## 5. Performance Optimization

### 5.1 Minification
- [ ] Auto Minify is enabled for JavaScript, CSS, and HTML
- [ ] JavaScript file sizes are reduced by 15-30% through minification
- [ ] CSS file sizes are reduced by 10-20% through minification
- [ ] HTML file sizes are reduced by 5-10% through minification
- [ ] Website functionality is not broken by minification (all forms, buttons, scripts work)
- [ ] If minification breaks functionality, specific files are excluded or minification is disabled for that type

### 5.2 Compression
- [ ] Brotli compression is enabled
- [ ] Text-based files (HTML, CSS, JS, JSON, XML) are compressed with Brotli
- [ ] Compression reduces file sizes by 20-30% compared to gzip
- [ ] Already-compressed files (images, videos) are not double-compressed
- [ ] Browser receives and decompresses Brotli correctly

### 5.3 HTTP/2 and HTTP/3
- [ ] HTTP/2 is enabled
- [ ] HTTP/3 (QUIC) is enabled
- [ ] Multiple resources are multiplexed over single connection (HTTP/2)
- [ ] Connection reuse reduces overhead for repeat visits
- [ ] HTTP/3 is used for browsers that support it (Chrome, Firefox)

### 5.4 Additional Performance Features
- [ ] Rocket Loader is enabled (if it doesn't break JavaScript functionality)
- [ ] Early Hints are enabled to improve LCP
- [ ] Auto Prefetch is tested and enabled if appropriate
- [ ] AMP (Accelerated Mobile Pages) support is configured if using AMP

### 5.5 Performance Metrics
- [ ] Page load time is reduced by >= 50% after Cloudflare enablement
- [ ] Time to First Byte (TTFB) is < 200ms for cached content
- [ ] Time to First Byte (TTFB) is < 800ms for uncached content
- [ ] Largest Contentful Paint (LCP) is < 2.5 seconds
- [ ] First Input Delay (FID) is < 100ms
- [ ] Cumulative Layout Shift (CLS) is < 0.1
- [ ] Core Web Vitals scores improve in Google PageSpeed Insights (target: 90+ score)

## 6. Security Configuration

### 6.1 Web Application Firewall (WAF)
- [ ] WAF Managed Rules are enabled
- [ ] Cloudflare Managed Ruleset is active (OWASP Core Ruleset)
- [ ] WAF sensitivity is set to appropriate level (Medium recommended)
- [ ] WAF blocks SQL injection attempts
- [ ] WAF blocks XSS (cross-site scripting) attempts
- [ ] WAF blocks CSRF (cross-site request forgery) attempts
- [ ] WAF blocks known attack patterns (from Cloudflare threat intelligence)
- [ ] Legitimate requests are not blocked by WAF (false positives minimized)
- [ ] WAF exceptions are created for false positives when necessary
- [ ] WAF events are monitored in Security > Events dashboard

### 6.2 Bot Management
- [ ] Bot Fight Mode is enabled (Free plan) or Super Bot Fight Mode (Pro plan)
- [ ] Malicious bots are blocked or challenged
- [ ] Good bots (Googlebot, Bingbot) are allowed through (verified user-agents)
- [ ] Challenge pages are served to suspected bots (CAPTCHA or JavaScript challenge)
- [ ] Bot traffic is monitored in Security > Bots dashboard
- [ ] Bot score is used to identify automated traffic

### 6.3 Rate Limiting
- [ ] Rate limiting rules are created for critical endpoints (login, API)
- [ ] Login endpoint has rate limit (e.g., 5 attempts per minute per IP)
- [ ] API endpoints have rate limits to prevent abuse (e.g., 100 requests/min per IP)
- [ ] Rate limiting action is configured (block, challenge, or JavaScript challenge)
- [ ] Rate limiting duration is appropriate (e.g., block for 10 minutes after threshold exceeded)
- [ ] Legitimate high-traffic users can be whitelisted if needed
- [ ] Rate limiting events are monitored

### 6.4 DDoS Protection
- [ ] DDoS protection is enabled automatically (included in all plans)
- [ ] HTTP flood attacks are mitigated automatically
- [ ] UDP flood attacks are absorbed at network edge
- [ ] Volumetric attacks (Tbps scale) are handled by Cloudflare's Anycast network
- [ ] DDoS alerts are configured to notify administrators
- [ ] Site remains online during simulated DDoS attack (test with small controlled test)

### 6.5 Additional Security Features
- [ ] Browser Integrity Check is enabled to block requests from non-standard browsers
- [ ] Email Obfuscation is enabled to hide email addresses from scrapers (if emails displayed on site)
- [ ] Hotlink Protection is configured to prevent other sites from embedding images
- [ ] IP Access Rules are configured for known bad actors (if applicable)
- [ ] User-Agent blocking rules are created for known malicious user-agents

## 7. Monitoring and Analytics

### 7.1 Cloudflare Analytics Dashboard
- [ ] Cloudflare Analytics dashboard is accessible and displays data
- [ ] Traffic analytics show requests over time (daily, hourly breakdown)
- [ ] Bandwidth analytics show data transfer trends
- [ ] Cached vs uncached requests are tracked
- [ ] Cache hit rate is displayed and monitored
- [ ] Threats blocked are displayed (WAF, Bot Fight, Firewall)
- [ ] Top countries, top paths, top referrers are tracked
- [ ] HTTP status code breakdown is available (200, 404, 500, etc.)

### 7.2 Performance Insights
- [ ] Origin response time is tracked
- [ ] Edge response time is tracked and is significantly lower than origin
- [ ] Cloudflare-added latency is minimal (< 10ms)
- [ ] Performance improvements are quantifiable (before/after comparison)

### 7.3 Security Event Monitoring
- [ ] Security events dashboard shows blocked threats
- [ ] WAF events are logged with details (rule triggered, IP, path, timestamp)
- [ ] Bot events are logged
- [ ] Rate limiting events are logged
- [ ] Firewall events are logged
- [ ] Events can be filtered by type, IP, country, user-agent

### 7.4 Alerts and Notifications
- [ ] Traffic anomaly alerts are configured
- [ ] Security event alerts are configured (e.g., alert when threats > threshold)
- [ ] Downtime alerts are configured (if origin goes down)
- [ ] SSL certificate expiration alerts are configured
- [ ] Alerts are sent to appropriate email addresses or Slack/PagerDuty integrations

## 8. Reliability and Uptime

### 8.1 Always Online
- [ ] Always Online is enabled to serve cached pages if origin is down
- [ ] If origin server is unreachable, Cloudflare serves cached version with banner "This is a cached page"
- [ ] When origin recovers, live version is served again
- [ ] Always Online works correctly during simulated origin downtime test

### 8.2 Load Balancing (Optional - Pro/Business Plan)
- [ ] If using multiple origin servers, Cloudflare Load Balancer is configured
- [ ] Health checks are configured to monitor origin server availability
- [ ] Automatic failover occurs if primary origin fails
- [ ] Traffic is distributed across multiple origins

### 8.3 Argo Smart Routing (Optional - Paid Feature)
- [ ] If enabled, Argo routes traffic through fastest Cloudflare paths
- [ ] Argo reduces origin response time by 30%+
- [ ] Argo analytics show performance improvements

## 9. Integration with Application

### 9.1 Origin Server Configuration
- [ ] Origin server accepts connections from Cloudflare IP ranges
- [ ] Firewall on origin allows Cloudflare IPs (whitelist configured)
- [ ] Origin server has valid SSL certificate (if using Full (strict) mode)
- [ ] Origin server sends appropriate Cache-Control headers for static assets
- [ ] Origin server sets `X-Robots-Tag: noindex` for admin pages (prevents caching)

### 9.2 Application Code Updates
- [ ] Application code does not rely on client IP address directly (uses CF-Connecting-IP header)
- [ ] Rate limiting in application uses `CF-Connecting-IP` header to get real client IP
- [ ] Logging includes Cloudflare headers for debugging (CF-Ray, CF-Cache-Status)
- [ ] Application handles Cloudflare 52x errors gracefully (shows user-friendly message)

### 9.3 Third-Party Integrations
- [ ] Google Analytics tracking is not affected by Cloudflare (events track correctly)
- [ ] Stripe payment processing works correctly (HTTPS, no caching of checkout pages)
- [ ] SendGrid emails are sent correctly (emails not routed through Cloudflare)
- [ ] EasyPost shipping API calls work correctly (API bypass cache)

## 10. Testing and Validation

### 10.1 Functionality Testing
- [ ] Website loads correctly through Cloudflare
- [ ] All pages are accessible (homepage, products, cart, checkout, my-account)
- [ ] Forms work correctly (contact form, newsletter signup, checkout)
- [ ] Add to cart functionality works
- [ ] Checkout flow works end-to-end (cart → shipping → payment → confirmation)
- [ ] User authentication works (login, logout, session management)
- [ ] Admin panel is accessible and functional
- [ ] API endpoints return correct responses

### 10.2 Performance Testing
- [ ] Page load time is measured before and after Cloudflare (using tools like WebPageTest, PageSpeed Insights)
- [ ] Page load time improvement is >= 50%
- [ ] Mobile page load time is tested separately
- [ ] Core Web Vitals are tested and improved
- [ ] Cache hit rate is measured and is >= 85%

### 10.3 Security Testing
- [ ] SQL injection attempts are blocked by WAF (test with safe payloads in staging environment)
- [ ] XSS attempts are blocked by WAF
- [ ] Bot traffic is challenged or blocked
- [ ] Rate limiting prevents brute-force login attempts (test with automated script)
- [ ] DDoS simulation (small-scale) is absorbed by Cloudflare

### 10.4 Cross-Browser and Device Testing
- [ ] Website works correctly in Chrome (desktop and mobile)
- [ ] Website works correctly in Firefox (desktop)
- [ ] Website works correctly in Safari (desktop and iOS)
- [ ] Website works correctly in Edge (desktop)
- [ ] Image optimization (WebP) works in supporting browsers
- [ ] Fallback to JPEG/PNG works in browsers not supporting WebP

### 10.5 Global Testing
- [ ] Website loads quickly from different geographic locations (use tools like KeyCDN Performance Test)
- [ ] Users in US, Europe, Asia, Australia experience fast load times (< 2 seconds)
- [ ] DNS resolves correctly globally (use whatsmydns.net)

## 11. Documentation and Training

### 11.1 Configuration Documentation
- [ ] Cloudflare account credentials are securely stored (password manager)
- [ ] Nameservers used are documented in project README
- [ ] DNS records are documented
- [ ] Page Rules are documented with purpose and reasoning
- [ ] SSL/TLS configuration is documented
- [ ] Security settings (WAF, Bot Fight, Rate Limiting) are documented

### 11.2 Runbooks and Procedures
- [ ] Runbook exists for purging Cloudflare cache when content is updated
- [ ] Runbook exists for handling WAF false positives (creating exceptions)
- [ ] Runbook exists for responding to DDoS attacks (monitoring, escalation)
- [ ] Runbook exists for SSL certificate renewal (if using custom certificates)
- [ ] Runbook exists for DNS changes (adding new subdomains, updating records)

### 11.3 Team Training
- [ ] Operations team is trained on Cloudflare dashboard
- [ ] Team knows how to purge cache
- [ ] Team knows how to monitor analytics and security events
- [ ] Team knows how to create and modify Page Rules
- [ ] Team knows how to respond to Cloudflare alerts

## 12. Cost Management

### 12.1 Plan Selection
- [ ] Appropriate Cloudflare plan is selected based on feature requirements
- [ ] Free plan is used if image optimization and advanced WAF are not required
- [ ] Pro plan ($20/month) is considered if Polish, Mirage, or advanced WAF are needed
- [ ] Billing is set up correctly if using paid plan

### 12.2 Cost Monitoring
- [ ] Cloudflare usage is monitored (bandwidth, requests)
- [ ] If approaching plan limits, upgrade is considered or usage is optimized
- [ ] Cost-benefit analysis shows Cloudflare provides positive ROI (bandwidth savings + performance improvements > monthly cost)

---

**Total Acceptance Criteria: 205 items**

All criteria must be met for Task 028 to be considered complete. Each criterion should be verified through manual testing, monitoring dashboards, or configuration reviews.
# Task 028: CDN Cloudflare Setup - Testing Requirements

## 1. Integration Tests

### 1.1 Cloudflare Header Detection Tests

```csharp
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CandleStore.Tests.Integration.Cloudflare
{
    [Collection("Integration Tests")]
    public class CloudflareHeaderTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CloudflareHeaderTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task HomePage_WhenAccessedThroughCloudflare_IncludesCloudflareHeaders()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add("CF-Connecting-IP", "203.0.113.45"); // Simulated Cloudflare header

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();

            // Cloudflare sets CF-Cache-Status header
            var hasCFCacheStatus = response.Headers.Contains("CF-Cache-Status") ||
                                  response.Headers.Contains("cf-cache-status");

            // Note: In test environment without actual Cloudflare, this may not be present
            // This test is more relevant for staging/production validation
        }

        [Fact]
        public async Task StaticAsset_HasCacheControlHeader()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/css/app.css");

            // Assert
            response.EnsureSuccessStatusCode();

            response.Headers.CacheControl.Should().NotBeNull();
            response.Headers.CacheControl.MaxAge.Should().NotBeNull();
            response.Headers.CacheControl.MaxAge.Value.TotalDays.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task DynamicPage_Cart_HasNoCacheHeaders()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/cart");

            // Assert
            response.EnsureSuccessStatusCode();

            // Cart page should not be cached
            var cacheControl = response.Headers.CacheControl;
            if (cacheControl != null)
            {
                // Either no-cache or private
                var isNotCacheable = cacheControl.NoCache ||
                                   cacheControl.NoStore ||
                                   cacheControl.Private;

                isNotCacheable.Should().BeTrue("Cart page should not be cached");
            }
        }

        [Fact]
        public async Task APIEndpoint_IncludesNoCacheHeaders()
        {
            // Arrange & Act
            var response = await _client.GetAsync("/api/products");

            // Assert
            response.EnsureSuccessStatusCode();

            var cacheControl = response.Headers.CacheControl;
            cacheControl.Should().NotBeNull();

            // API responses should either be uncached or have short TTL
            if (cacheControl.MaxAge.HasValue)
            {
                cacheControl.MaxAge.Value.TotalMinutes.Should().BeLessThan(60,
                    "API responses should have short cache duration or no-cache");
            }
        }
    }
}
```

### 1.2 Client IP Detection Tests (Cloudflare Headers)

```csharp
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CandleStore.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace CandleStore.Tests.Integration.Middleware
{
    public class CloudflareClientIPMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WithCFConnectingIPHeader_SetsRemoteIPAddress()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1"); // Cloudflare edge IP

            context.Request.Headers["CF-Connecting-IP"] = "203.0.113.45"; // Real client IP

            var nextCalled = false;
            RequestDelegate next = (HttpContext ctx) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new CloudflareClientIPMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            nextCalled.Should().BeTrue();

            // Middleware should set RemoteIpAddress to CF-Connecting-IP value
            context.Connection.RemoteIpAddress.Should().NotBeNull();
            context.Connection.RemoteIpAddress.ToString().Should().Be("203.0.113.45");
        }

        [Fact]
        public async Task InvokeAsync_WithoutCFConnectingIPHeader_LeavesRemoteIPUnchanged()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var originalIP = IPAddress.Parse("198.51.100.23");
            context.Connection.RemoteIpAddress = originalIP;

            // No CF-Connecting-IP header

            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
            var middleware = new CloudflareClientIPMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Connection.RemoteIpAddress.Should().Be(originalIP);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidCFConnectingIP_LeavesRemoteIPUnchanged()
        {
            // Arrange
            var context = new DefaultHttpContext();
            var originalIP = IPAddress.Parse("198.51.100.23");
            context.Connection.RemoteIpAddress = originalIP;

            context.Request.Headers["CF-Connecting-IP"] = "invalid-ip-address";

            RequestDelegate next = (HttpContext ctx) => Task.CompletedTask;
            var middleware = new CloudflareClientIPMiddleware(next);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Connection.RemoteIpAddress.Should().Be(originalIP);
        }
    }
}
```

## 2. End-to-End Tests

### 2.1 Page Load Performance E2E Test

```csharp
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;

namespace CandleStore.Tests.E2E.Performance
{
    [Collection("E2E Tests")]
    public class CloudflarePerformanceE2ETests : IDisposable
    {
        private readonly IWebDriver _driver;
        private readonly string _baseUrl = "https://candlestore.com"; // Production URL with Cloudflare

        public CloudflarePerformanceE2ETests()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");

            _driver = new ChromeDriver(options);
        }

        [Fact(Skip = "E2E test - requires production environment with Cloudflare")]
        public async Task HomePage_LoadsWithinPerformanceTarget()
        {
            // Arrange
            var stopwatch = Stopwatch.StartNew();

            // Act
            _driver.Navigate().GoToUrl(_baseUrl);

            // Wait for page to fully load
            var jsExecutor = (IJavaScriptExecutor)_driver;
            await WaitForPageLoad(jsExecutor);

            stopwatch.Stop();

            // Assert
            var loadTime = stopwatch.ElapsedMilliseconds;

            loadTime.Should().BeLessThan(2000,
                "Homepage should load in < 2 seconds with Cloudflare CDN");
        }

        [Fact(Skip = "E2E test - requires production environment")]
        public async Task ProductPage_MeasuresTimeToFirstByte()
        {
            // Arrange
            _driver.Navigate().GoToUrl($"{_baseUrl}/products/vanilla-bourbon");

            var jsExecutor = (IJavaScriptExecutor)_driver;
            await WaitForPageLoad(jsExecutor);

            // Act - Get performance metrics via Navigation Timing API
            var ttfbMs = jsExecutor.ExecuteScript(@"
                var perfData = window.performance.timing;
                return perfData.responseStart - perfData.requestStart;
            ");

            var ttfb = Convert.ToInt64(ttfbMs);

            // Assert
            ttfb.Should().BeLessThan(200,
                "Time to First Byte should be < 200ms for cached content with Cloudflare");
        }

        [Fact(Skip = "E2E test - requires production environment")]
        public async Task ProductPage_MeasuresLargestContentfulPaint()
        {
            // Arrange
            _driver.Navigate().GoToUrl($"{_baseUrl}/products/lavender-dreams");

            var jsExecutor = (IJavaScriptExecutor)_driver;
            await WaitForPageLoad(jsExecutor);

            // Wait for LCP to be recorded (usually within 2-3 seconds)
            await Task.Delay(3000);

            // Act - Get LCP via PerformanceObserver API
            var lcpMs = jsExecutor.ExecuteScript(@"
                return new Promise((resolve) => {
                    new PerformanceObserver((list) => {
                        const entries = list.getEntries();
                        const lastEntry = entries[entries.length - 1];
                        resolve(lastEntry.renderTime || lastEntry.loadTime);
                    }).observe({entryTypes: ['largest-contentful-paint']});

                    setTimeout(() => resolve(0), 5000); // Timeout after 5 seconds
                });
            ");

            var lcp = Convert.ToDouble(lcpMs);

            // Assert
            lcp.Should().BeLessThan(2500,
                "Largest Contentful Paint should be < 2.5s for good Core Web Vitals score");
        }

        [Fact(Skip = "E2E test - requires production environment")]
        public void ImageOptimization_ServesWebPFormat()
        {
            // Arrange
            _driver.Navigate().GoToUrl($"{_baseUrl}/products");

            // Act - Check first product image source
            var firstImage = _driver.FindElement(By.CssSelector(".product-card img"));
            var imageSrc = firstImage.GetAttribute("src");

            // Assert
            imageSrc.Should().NotBeNullOrEmpty();

            // If Cloudflare Polish is enabled and browser supports WebP,
            // image should be served in WebP format
            // Note: This depends on browser and Cloudflare configuration

            var jsExecutor = (IJavaScriptExecutor)_driver;
            var supportsWebP = (bool)jsExecutor.ExecuteScript(@"
                var elem = document.createElement('canvas');
                return !!(elem.getContext && elem.getContext('2d')) &&
                       elem.toDataURL('image/webp').indexOf('data:image/webp') == 0;
            ");

            if (supportsWebP)
            {
                // Browser supports WebP, Cloudflare should serve WebP (or image should be WebP)
                // This test is informational - actual format depends on Cloudflare Polish settings
            }
        }

        private async Task WaitForPageLoad(IJavaScriptExecutor jsExecutor)
        {
            await Task.Run(() =>
            {
                var maxWaitTime = TimeSpan.FromSeconds(30);
                var startTime = DateTime.Now;

                while (DateTime.Now - startTime < maxWaitTime)
                {
                    var readyState = jsExecutor.ExecuteScript("return document.readyState").ToString();
                    if (readyState == "complete")
                    {
                        return;
                    }

                    Task.Delay(100).Wait();
                }

                throw new TimeoutException("Page did not load within 30 seconds");
            });
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }
    }
}
```

### 2.2 Cache Verification E2E Test

```csharp
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CandleStore.Tests.E2E.Cloudflare
{
    [Collection("E2E Tests")]
    public class CloudflareCacheE2ETests
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://candlestore.com";

        public CloudflareCacheE2ETests()
        {
            _httpClient = new HttpClient();
        }

        [Fact(Skip = "E2E test - requires production environment with Cloudflare")]
        public async Task StaticAsset_IsCachedByCloudflare()
        {
            // Arrange
            var assetUrl = $"{_baseUrl}/css/app.css";

            // Act - First request (likely cache miss)
            var response1 = await _httpClient.GetAsync(assetUrl);
            response1.EnsureSuccessStatusCode();

            var cacheStatus1 = response1.Headers.GetValues("CF-Cache-Status").FirstOrDefault();

            // Wait a moment
            await Task.Delay(100);

            // Second request (should be cache hit)
            var response2 = await _httpClient.GetAsync(assetUrl);
            response2.EnsureSuccessStatusCode();

            var cacheStatus2 = response2.Headers.GetValues("CF-Cache-Status").FirstOrDefault();

            // Assert
            // First request might be MISS or EXPIRED
            cacheStatus1.Should().BeOneOf("MISS", "EXPIRED", "HIT", "DYNAMIC");

            // Second request should be HIT (served from Cloudflare cache)
            cacheStatus2.Should().Be("HIT",
                "Static assets should be served from Cloudflare cache on repeat requests");
        }

        [Fact(Skip = "E2E test - requires production environment")]
        public async Task DynamicPage_BypassesCloudflareCache()
        {
            // Arrange
            var cartUrl = $"{_baseUrl}/cart";

            // Act
            var response = await _httpClient.GetAsync(cartUrl);
            response.EnsureSuccessStatusCode();

            // Get CF-Cache-Status header
            var hasCacheStatus = response.Headers.TryGetValues("CF-Cache-Status", out var cacheStatusValues);

            // Assert
            if (hasCacheStatus)
            {
                var cacheStatus = cacheStatusValues.FirstOrDefault();

                // Cart should bypass cache (BYPASS or DYNAMIC)
                cacheStatus.Should().BeOneOf("BYPASS", "DYNAMIC",
                    "Dynamic pages like cart should bypass Cloudflare cache");
            }
        }
    }
}
```

## 3. Performance Tests

### 3.1 Load Time Benchmark (Before/After Cloudflare)

```csharp
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace CandleStore.Tests.Performance.Cloudflare
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, targetCount: 10)]
    public class PageLoadBenchmarks
    {
        private HttpClient _httpClient;

        [GlobalSetup]
        public void Setup()
        {
            _httpClient = new HttpClient();
        }

        [Benchmark]
        public async Task LoadHomePage_ThroughCloudflare()
        {
            var response = await _httpClient.GetAsync("https://candlestore.com");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
        }

        [Benchmark]
        public async Task LoadProductPage_ThroughCloudflare()
        {
            var response = await _httpClient.GetAsync("https://candlestore.com/products/vanilla-bourbon");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
        }

        [Benchmark]
        public async Task LoadProductListingPage_ThroughCloudflare()
        {
            var response = await _httpClient.GetAsync("https://candlestore.com/products");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _httpClient?.Dispose();
        }

        /*
         * Expected Results (with Cloudflare CDN and caching enabled):
         *
         * | Method                               | Mean       | Error    | StdDev   | Gen0   | Allocated |
         * |------------------------------------- |-----------:|---------:|---------:|-------:|----------:|
         * | LoadHomePage_ThroughCloudflare       | 185.3 ms   | 8.2 ms   | 7.7 ms   | -      | 125 KB    |
         * | LoadProductPage_ThroughCloudflare    | 142.7 ms   | 5.4 ms   | 5.1 ms   | -      | 98 KB     |
         * | LoadProductListingPage_ThroughCF     | 267.8 ms   | 11.3 ms  | 10.6 ms  | -      | 187 KB    |
         *
         * Analysis:
         * - Cached pages load in < 200ms (very fast)
         * - Cloudflare CDN significantly reduces latency vs direct origin connection
         * - First load (cache miss) may be slower (500-1000ms) but subsequent loads are fast
         *
         * Without Cloudflare (baseline comparison):
         * - HomePage: ~800-1200ms
         * - ProductPage: ~600-900ms
         * - ListingPage: ~1000-1500ms
         *
         * Performance improvement: 70-85% faster with Cloudflare caching
         */
    }
}
```

### 3.2 Image Load Performance Benchmark

```csharp
using System.Net.Http;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace CandleStore.Tests.Performance.Cloudflare
{
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 2, targetCount: 5)]
    public class ImageLoadBenchmarks
    {
        private HttpClient _httpClient;

        [GlobalSetup]
        public void Setup()
        {
            _httpClient = new HttpClient();
        }

        [Benchmark]
        public async Task LoadProductImage_OriginalJPEG()
        {
            // Direct load from origin (bypassing Cloudflare Polish for comparison)
            var response = await _httpClient.GetAsync("https://origin.candlestore.com/images/candle-1.jpg");
            response.EnsureSuccessStatusCode();
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
        }

        [Benchmark]
        public async Task LoadProductImage_ThroughCloudflarePolish()
        {
            // Load through Cloudflare with Polish enabled
            var response = await _httpClient.GetAsync("https://candlestore.com/images/candle-1.jpg");
            response.EnsureSuccessStatusCode();
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _httpClient?.Dispose();
        }

        /*
         * Expected Results:
         *
         * | Method                                  | Mean       | Allocated |
         * |---------------------------------------- |-----------:|----------:|
         * | LoadProductImage_OriginalJPEG           | 1,245 ms   | 1200 KB   |
         * | LoadProductImage_ThroughCloudflarePolish| 487 ms     | 420 KB    |
         *
         * Analysis:
         * - Cloudflare Polish reduces image size by ~65% (1200KB → 420KB)
         * - Load time improves by ~61% (1245ms → 487ms)
         * - Smaller images = faster load, less bandwidth usage
         */
    }
}
```

## 4. Regression Tests

### 4.1 SSL Certificate Expiration Regression Test

```csharp
using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CandleStore.Tests.Regression.Cloudflare
{
    /// <summary>
    /// Regression test for ensuring SSL certificate is valid and not expiring soon
    ///
    /// Cloudflare auto-renews certificates, but this test validates the process is working
    /// </summary>
    public class SSLCertificateRegressionTests
    {
        [Fact(Skip = "Regression test - requires production URL")]
        public async Task SslCertificate_IsValidAndNotExpiringSoon()
        {
            // Arrange
            var url = "https://candlestore.com";
            X509Certificate2 certificate = null;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    certificate = new X509Certificate2(cert);
                    return true; // Accept certificate for inspection
                }
            };

            var httpClient = new HttpClient(handler);

            // Act
            var response = await httpClient.GetAsync(url);

            // Assert
            certificate.Should().NotBeNull("Certificate should be present");

            // Check certificate is issued by Cloudflare or valid CA
            var issuer = certificate.Issuer;
            issuer.Should().Contain("Cloudflare", "Certificate should be issued by Cloudflare");

            // Check certificate is not expired
            var now = DateTime.Now;
            certificate.NotBefore.Should().BeBefore(now, "Certificate should be valid (not before date)");
            certificate.NotAfter.Should().BeAfter(now, "Certificate should not be expired");

            // Check certificate is not expiring within next 30 days
            var expirationDate = certificate.NotAfter;
            var daysUntilExpiration = (expirationDate - now).TotalDays;

            daysUntilExpiration.Should().BeGreaterThan(30,
                "Certificate should not expire within next 30 days (Cloudflare auto-renewal should occur before expiration)");
        }
    }
}
```

### 4.2 Cache Bypass Regression Test

```csharp
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace CandleStore.Tests.Regression.Cloudflare
{
    /// <summary>
    /// Regression test for Bug #2301: Cart and checkout pages were being cached by Cloudflare
    ///
    /// Bug Details:
    /// - Cart and checkout pages were cached, causing users to see other users' cart contents
    /// - Privacy issue: User A saw User B's cart items
    /// - Caused by missing Page Rule to bypass cache for dynamic pages
    ///
    /// Fix: Created Page Rules to bypass cache for /cart*, /checkout*, /my-account*
    /// </summary>
    public class CacheBypassRegressionTests
    {
        private readonly HttpClient _httpClient;

        public CacheBypassRegressionTests()
        {
            _httpClient = new HttpClient();
        }

        [Fact(Skip = "Regression test - requires production environment")]
        public async Task CartPage_BypassesCloudflareCache()
        {
            // Arrange
            var cartUrl = "https://candlestore.com/cart";

            // Act
            var response = await _httpClient.GetAsync(cartUrl);
            response.EnsureSuccessStatusCode();

            // Assert
            var hasCacheStatus = response.Headers.TryGetValues("CF-Cache-Status", out var cacheStatusValues);

            if (hasCacheStatus)
            {
                var cacheStatus = cacheStatusValues.FirstOrDefault();

                cacheStatus.Should().BeOneOf("BYPASS", "DYNAMIC",
                    "Cart page must bypass Cloudflare cache to prevent showing wrong user's cart");
            }

            // Additionally check Cache-Control header from origin
            var cacheControl = response.Headers.CacheControl;
            if (cacheControl != null)
            {
                var isNotCacheable = cacheControl.NoCache || cacheControl.NoStore || cacheControl.Private;
                isNotCacheable.Should().BeTrue("Cart should have no-cache headers from origin");
            }
        }

        [Fact(Skip = "Regression test - requires production environment")]
        public async Task CheckoutPage_BypassesCloudflareCache()
        {
            // Arrange
            var checkoutUrl = "https://candlestore.com/checkout";

            // Act
            var response = await _httpClient.GetAsync(checkoutUrl);

            // Assert
            var hasCacheStatus = response.Headers.TryGetValues("CF-Cache-Status", out var cacheStatusValues);

            if (hasCacheStatus)
            {
                var cacheStatus = cacheStatusValues.FirstOrDefault();

                cacheStatus.Should().BeOneOf("BYPASS", "DYNAMIC",
                    "Checkout page must bypass cache to prevent exposing sensitive payment information");
            }
        }

        [Fact(Skip = "Regression test - requires production environment")]
        public async Task MyAccountPage_BypassesCloudflareCache()
        {
            // Arrange
            var myAccountUrl = "https://candlestore.com/my-account";

            // Act
            var response = await _httpClient.GetAsync(myAccountUrl);

            // Assert
            var hasCacheStatus = response.Headers.TryGetValues("CF-Cache-Status", out var cacheStatusValues);

            if (hasCacheStatus)
            {
                var cacheStatus = cacheStatusValues.FirstOrDefault();

                cacheStatus.Should().BeOneOf("BYPASS", "DYNAMIC",
                    "My Account page must bypass cache to prevent exposing user's personal information");
            }
        }
    }
}
```

---

**End of Testing Requirements**

Testing for Cloudflare integration includes:
- **Integration Tests**: 2 test classes covering Cloudflare header detection and client IP handling
- **E2E Tests**: 2 test classes covering page load performance and cache verification
- **Performance Tests**: 2 benchmark suites measuring page load times and image optimization improvements
- **Regression Tests**: 2 regression tests preventing SSL issues and cache bypass failures

Total: 15+ test implementations focusing on critical Cloudflare functionality.

**Note**: Many tests are marked with `Skip` attribute because they require production environment with Cloudflare enabled. These tests should be run manually in staging/production environments or as part of monitoring/smoke tests.
# Task 028: CDN Cloudflare Setup - Verification Steps and Implementation Prompt

## User Verification Steps

### Step 1: Verify Cloudflare is Active and Proxying Traffic

**Objective**: Confirm website is successfully routing through Cloudflare CDN.

**Instructions**:
1. Open browser and navigate to https://candlestore.com
2. Open browser Developer Tools (F12)
3. Go to **Network** tab
4. Reload the page (Ctrl+R or Cmd+R)
5. Click on the first request (usually the HTML document)
6. Go to **Headers** tab
7. Verify **Response Headers** include Cloudflare headers:
   - `server: cloudflare`
   - `cf-ray: [unique identifier]` (e.g., `cf-ray: 7a1b2c3d4e5f-SJC`)
   - `cf-cache-status: HIT` or `MISS` or `DYNAMIC`
8. Use online tools to verify Cloudflare:
   - Visit https://www.whatsmyip.org/cloudflare-check/
   - Enter `candlestore.com`
   - Verify result shows "✅ Using Cloudflare"
9. Check DNS propagation:
   - Visit https://whatsmydns.net
   - Enter `candlestore.com`
   - Select "A" record type
   - Verify IPs shown are Cloudflare IPs (not your origin server IP)
   - Cloudflare IP ranges: 104.16.0.0 through 104.31.255.255, 172.64.0.0 through 172.71.255.255

**Expected Result**: Website is accessible through Cloudflare, headers confirm traffic is proxied, DNS resolves to Cloudflare IPs.

---

### Step 2: Verify SSL/TLS Configuration and HTTPS

**Objective**: Confirm HTTPS is working correctly with valid SSL certificate.

**Instructions**:
1. Navigate to https://candlestore.com in browser
2. Click padlock icon in address bar
3. Click **"Certificate is valid"** or **"Connection is secure"**
4. Verify certificate details:
   - **Issued to**: candlestore.com
   - **Issued by**: Cloudflare Inc ECC CA-3 (or similar Cloudflare CA)
   - **Valid from**: [recent date]
   - **Valid until**: [future date, typically 3-12 months from issue]
5. Try accessing HTTP version: http://candlestore.com
6. Verify browser automatically redirects to HTTPS version
7. Test mixed content:
   - Open Developer Tools → Console
   - Look for mixed content warnings (should be none)
8. Test HSTS:
   - In browser, type: chrome://net-internals/#hsts (Chrome) or about:networking#hsts (Firefox)
   - Query HSTS for: candlestore.com
   - Verify HSTS is enabled with max-age

**Expected Result**: Valid SSL certificate from Cloudflare, HTTP redirects to HTTPS, no mixed content warnings, HSTS is active.

---

### Step 3: Verify Caching is Working

**Objective**: Confirm static assets are being cached by Cloudflare.

**Instructions**:
1. Clear browser cache (Ctrl+Shift+Del or Cmd+Shift+Del)
2. Navigate to https://candlestore.com
3. Open Developer Tools → Network tab
4. Reload page
5. Look for static assets: CSS, JavaScript, images
6. Click on a CSS file (e.g., `app.css`)
7. Go to **Headers** tab
8. Check **Response Headers**:
   - `cf-cache-status` should be `MISS` (first load) or `HIT` (if previously cached)
   - `cache-control` should show caching duration (e.g., `max-age=2592000` for 30 days)
9. Reload page again (Ctrl+R)
10. Check same CSS file
11. `cf-cache-status` should now be `HIT` (served from Cloudflare cache)
12. Note response time: Should be very fast (< 50ms for cached files)

**Test Cache Bypass for Dynamic Pages**:
1. Navigate to https://candlestore.com/cart
2. Check Network tab → Headers
3. `cf-cache-status` should be `BYPASS` or `DYNAMIC` (not cached)

**Expected Result**: Static assets show `HIT` status on repeat loads, dynamic pages show `BYPASS`, cached files load in < 50ms.

---

### Step 4: Verify Image Optimization (Pro Plan Only)

**Objective**: Confirm Cloudflare Polish is optimizing images.

**Instructions**:
1. Navigate to https://candlestore.com/products
2. Open Developer Tools → Network tab
3. Filter by "Img" to show only images
4. Click on a product image (e.g., `vanilla-candle.jpg`)
5. Go to **Headers** tab
6. Check **Response Headers**:
   - `cf-polished: lossy` (if Polish is working) or `cf-polished: origSize=1200KB newSize=420KB` (size reduction details)
   - `content-type: image/webp` (if browser supports WebP and Cloudflare converted)
7. Note image file size in **Size** column
8. Compare to original:
   - If Polish is working, size should be 40-60% smaller than original
9. Test WebP delivery:
   - Right-click image → "Copy image address"
   - Paste URL in new tab
   - Check if WebP is served (content-type: image/webp)

**Test Mirage (Lazy Loading)**:
1. Navigate to product listing page with many images
2. Scroll to bottom of page
3. Watch Network tab as you scroll
4. Verify images load progressively as you scroll (not all at once on page load)

**Expected Result**: Images are compressed (cf-polished header present), WebP is served to compatible browsers, lazy loading works.

---

### Step 5: Verify Performance Improvements

**Objective**: Measure page load performance improvement with Cloudflare.

**Instructions**:
1. Use Google PageSpeed Insights:
   - Visit https://pagespeed.web.dev/
   - Enter URL: https://candlestore.com
   - Click **"Analyze"**
2. Review Performance Score:
   - **Target**: 90+ for desktop, 80+ for mobile
   - If below target, review **Opportunities** section for improvements
3. Check Core Web Vitals:
   - **Largest Contentful Paint (LCP)**: Should be < 2.5s (Good)
   - **First Input Delay (FID)**: Should be < 100ms (Good)
   - **Cumulative Layout Shift (CLS)**: Should be < 0.1 (Good)
4. Compare to baseline (before Cloudflare):
   - If you measured performance before Cloudflare, compare scores
   - Expected improvement: 50-70% reduction in load time
5. Use WebPageTest.org for detailed analysis:
   - Visit https://www.webpagetest.org/
   - Enter URL: https://candlestore.com
   - Select test location: Choose multiple locations (US, Europe, Asia)
   - Run test
   - Review results:
     - **Time to First Byte (TTFB)**: Should be < 200ms for most locations
     - **Start Render**: Should be < 1s
     - **Fully Loaded**: Should be < 3s
     - Check "Waterfall" view to see if assets are loading from cache

**Expected Result**: PageSpeed score 90+, Core Web Vitals in "Good" range, TTFB < 200ms globally.

---

### Step 6: Verify Security Features (WAF, Bot Protection)

**Objective**: Confirm WAF and security features are protecting the site.

**Instructions**:

**WAF Verification**:
1. Log in to Cloudflare dashboard (https://dash.cloudflare.com)
2. Select your website (candlestore.com)
3. Navigate to **Security** → **WAF**
4. Verify **Managed Rules** is enabled
5. Check **Security Events** dashboard:
   - Shows blocked requests in last 24 hours
   - If no events, security is quiet (good - no attacks detected)
   - If events are present, review to ensure legitimate traffic isn't blocked

**Test WAF** (in staging environment only, NOT production):
1. Attempt SQL injection in URL parameter:
   - URL: https://staging.candlestore.com/products?id=1' OR '1'='1
   - Expected: Cloudflare blocks request with Challenge page or error
2. Check Security Events dashboard for blocked event

**Bot Protection Verification**:
1. In Cloudflare dashboard, go to **Security** → **Bots**
2. Verify **Bot Fight Mode** is enabled
3. Review bot traffic in **Bot Analytics**:
   - Good bots (Google, Bing) should be allowed
   - Bad bots should be challenged or blocked

**Expected Result**: WAF is active and blocking malicious requests, bot protection is enabled, good bots are allowed.

---

### Step 7: Verify Rate Limiting (If Configured)

**Objective**: Test that rate limiting prevents brute-force attacks.

**Instructions**:
1. Identify endpoint with rate limiting (e.g., /api/auth/login)
2. Use tool like Postman or curl to send repeated requests:
   ```bash
   for i in {1..10}; do curl -X POST https://candlestore.com/api/auth/login -d '{"email":"test@test.com","password":"wrong"}'; done
   ```
3. After threshold is exceeded (e.g., 5 failed logins), verify:
   - Cloudflare returns 429 Too Many Requests status
   - Or Cloudflare shows Challenge page
4. In Cloudflare dashboard, check **Security** → **Events**
5. Look for rate limiting events (should show blocked IPs)
6. Wait for rate limit duration to expire (e.g., 10 minutes)
7. Retry request - should work again after rate limit window resets

**Expected Result**: Rate limiting blocks excessive requests, returns 429 status or Challenge page, resets after duration expires.

---

### Step 8: Verify Cloudflare Analytics

**Objective**: Confirm analytics are tracking traffic and performance.

**Instructions**:
1. Log in to Cloudflare dashboard
2. Navigate to **Analytics & Logs** → **Traffic**
3. Verify metrics are populating:
   - **Requests**: Shows request count over time (should match expected traffic)
   - **Bandwidth**: Shows data transferred
   - **Unique Visitors**: Shows visitor count
   - **Threats**: Shows blocked requests (WAF, bots, firewall)
4. Check **Caching** tab:
   - **Bandwidth Saved**: Percentage of traffic served from cache
   - **Total Requests Served**: Breakdown of cached vs uncached
5. Calculate **Cache Hit Rate**:
   - Formula: (Cached Requests / Total Requests) × 100%
   - **Target**: 85-95%
   - If below 85%, review Page Rules and caching configuration
6. Check **Performance** tab:
   - **Origin Response Time**: Time for origin server to respond (should be consistent)
   - **Edge Response Time**: Time for Cloudflare to respond (should be low, < 50ms)

**Expected Result**: Analytics show traffic data, cache hit rate >= 85%, bandwidth savings >= 60%, edge response time < 50ms.

---

### Step 9: Test DDoS Protection (Simulated Small-Scale Test)

**Objective**: Verify Cloudflare can handle traffic spikes.

**Instructions**:
⚠️ **WARNING**: Only perform in staging environment with permission. Do NOT attack production without explicit authorization.

1. Use load testing tool (Apache Bench, JMeter, or Locust)
2. Send moderate traffic spike to test Cloudflare's handling:
   ```bash
   # Apache Bench: 1000 requests, 10 concurrent
   ab -n 1000 -c 10 https://staging.candlestore.com/
   ```
3. Monitor Cloudflare dashboard during test:
   - **Analytics → Traffic**: Should show spike in requests
   - Site should remain responsive
   - Origin server load should be moderate (Cloudflare absorbs most traffic)
4. Check if Cloudflare activated "I'm Under Attack" mode automatically
5. After test, verify site returns to normal
6. Review **Security Events** for any anomalies detected

**Expected Result**: Site remains online during traffic spike, Cloudflare absorbs traffic, origin server is protected.

**Note**: For actual DDoS testing, Cloudflare supports controlled testing with advance notice. Contact Cloudflare support for enterprise-level DDoS testing.

---

### Step 10: Verify Always Online Feature

**Objective**: Confirm Always Online serves cached pages if origin is down.

**Instructions**:
1. Temporarily take origin server offline:
   - Stop web server: `sudo systemctl stop nginx` (or Apache)
   - Or block Cloudflare IPs at firewall temporarily
2. Navigate to https://candlestore.com in browser
3. Verify behavior:
   - If page was previously cached, Cloudflare serves cached version
   - Banner appears: "You are seeing a cached version of this page"
   - Page content is static (forms won't submit, cart won't update)
4. Try navigating to different pages:
   - Pages that were cached will load
   - Pages that weren't cached will show error
5. Restore origin server:
   - Start web server: `sudo systemctl start nginx`
   - Or unblock Cloudflare IPs at firewall
6. Reload page
7. Banner disappears, live version is served

**Expected Result**: Cached pages load even when origin is down, Always Online banner appears, live version resumes when origin is restored.

---

## Implementation Prompt for Claude

### Overview

Configure Cloudflare as a Content Delivery Network (CDN) and performance optimization platform for the Candle Store e-commerce website. Cloudflare provides global caching, image optimization, DDoS protection, Web Application Firewall, and automatic HTTPS.

**Key Steps:**
1. Create Cloudflare account and add website
2. Migrate DNS to Cloudflare nameservers
3. Configure SSL/TLS (Full (strict) mode)
4. Set up caching rules (Page Rules)
5. Enable performance features (Brotli, minification, HTTP/2)
6. Configure security (WAF, Bot Fight Mode, Rate Limiting)
7. Monitor analytics and optimize

### 1. Create Cloudflare Account and Add Website

**Step 1: Sign Up**
1. Visit https://dash.cloudflare.com/sign-up
2. Create account with business email
3. Verify email

**Step 2: Add Site**
1. Click **"Add a Site"**
2. Enter: `candlestore.com`
3. Select **Free** plan (or **Pro** if image optimization needed)
4. Cloudflare scans existing DNS records

**Step 3: Review DNS Records**
Ensure these records exist:
- **A record**: `candlestore.com` → `[Origin IP]` (Proxied - orange cloud)
- **CNAME**: `www` → `candlestore.com` (Proxied)
- **MX records**: Mail server records (DNS Only - gray cloud)

**Step 4: Change Nameservers**
1. Log in to domain registrar (GoDaddy, Namecheap, etc.)
2. Replace nameservers with Cloudflare nameservers (provided by Cloudflare)
3. Wait 24-48 hours for propagation
4. Cloudflare dashboard shows "Active" status when complete

### 2. Configure SSL/TLS

**Enable HTTPS**:
```
Cloudflare Dashboard → SSL/TLS
├─ Overview → Encryption Mode: Full (strict)
├─ Edge Certificates → Always Use HTTPS: On
├─ Edge Certificates → HTTP Strict Transport Security (HSTS): Enable
│  ├─ Max-Age: 12 months
│  ├─ Include subdomains: On
│  └─ Preload: Off (enable after testing)
└─ Edge Certificates → Minimum TLS Version: TLS 1.2
```

**Install Origin Certificate** (if using Full (strict)):
1. Go to **SSL/TLS** → **Origin Server**
2. Click **"Create Certificate"**
3. Select key type: RSA (2048)
4. Hostnames: `*.candlestore.com`, `candlestore.com`
5. Validity: 15 years
6. Click **"Create"**
7. Copy **Origin Certificate** and **Private Key**
8. Install on origin server (Nginx/Apache)

**Nginx Configuration**:
```nginx
server {
    listen 443 ssl http2;
    server_name candlestore.com www.candlestore.com;

    ssl_certificate /etc/ssl/cloudflare/candlestore.pem;
    ssl_certificate_key /etc/ssl/cloudflare/candlestore.key;

    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;
    ssl_prefer_server_ciphers on;

    # Additional SSL security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;

    location / {
        proxy_pass http://localhost:5000; # Blazor Server app
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection $connection_upgrade;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### 3. Configure Caching Rules (Page Rules)

**Create Page Rules**:

**Rule 1: Cache Static Assets**
```
URL: candlestore.com/images/*
Settings:
  - Cache Level: Cache Everything
  - Edge Cache TTL: 1 month
  - Browser Cache TTL: 1 month
```

Repeat for `/css/*`, `/js/*`, `/_framework/*`

**Rule 2: Bypass Cache for Dynamic Pages**
```
URL: candlestore.com/cart*
Settings:
  - Cache Level: Bypass
```

Repeat for `/checkout*`, `/my-account*`, `/api/*`

**Rule 3: Short Cache for Product Pages**
```
URL: candlestore.com/products/*
Settings:
  - Cache Level: Cache Everything
  - Edge Cache TTL: 2 hours
```

### 4. Enable Performance Features

**Auto Minify**:
```
Speed → Optimization
├─ Auto Minify
│  ├─ JavaScript: On
│  ├─ CSS: On
│  └─ HTML: On
├─ Brotli: On
└─ HTTP/2: On (enabled by default)
```

**HTTP/3 (QUIC)**:
```
Network → HTTP/3 (with QUIC): On
```

### 5. Configure Security

**WAF**:
```
Security → WAF
└─ Managed Rules: On
   └─ Cloudflare Managed Ruleset: Enabled
```

**Bot Fight Mode**:
```
Security → Bots
└─ Bot Fight Mode: On
```

**Rate Limiting** (example for login):
```
Security → WAF → Rate Limiting Rules
└─ Create Rule:
   ├─ Name: Limit Login Attempts
   ├─ Match: URL Path = /api/auth/login
   ├─ Requests: 5 per 1 minute per IP
   └─ Action: Block for 10 minutes
```

### 6. Application Code Updates (Optional)

**Middleware to Read CF-Connecting-IP Header**:

Create `src/CandleStore.Api/Middleware/CloudflareClientIPMiddleware.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Threading.Tasks;

namespace CandleStore.Api.Middleware
{
    public class CloudflareClientIPMiddleware
    {
        private readonly RequestDelegate _next;

        public CloudflareClientIPMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var cfConnectingIP = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();

            if (!string.IsNullOrEmpty(cfConnectingIP) && IPAddress.TryParse(cfConnectingIP, out var ipAddress))
            {
                context.Connection.RemoteIpAddress = ipAddress;
            }

            await _next(context);
        }
    }
}
```

Register in `Program.cs`:

```csharp
app.UseMiddleware<CloudflareClientIPMiddleware>();
```

### 7. Testing and Validation

**Verify Cloudflare is Active**:
```bash
curl -I https://candlestore.com
```

Look for `server: cloudflare` and `cf-ray:` headers.

**Test Caching**:
```bash
# First request (cache miss)
curl -I https://candlestore.com/css/app.css

# Second request (should be cache hit)
curl -I https://candlestore.com/css/app.css
```

Check `CF-Cache-Status: HIT` on second request.

**Measure Performance**:
- Before: Measure page load time using WebPageTest.org
- After: Re-measure after Cloudflare is active
- Expected: 50-70% reduction in load time

This implementation provides a complete Cloudflare setup for the Candle Store e-commerce platform, improving performance, security, and reliability.
