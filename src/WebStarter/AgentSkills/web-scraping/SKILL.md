---
name: web-scraping
description: Extract data from websites using Python BeautifulSoup and Requests
---

# Web Scraping Skill

## Overview

This skill provides tools for extracting data from websites using Python's web scraping libraries (BeautifulSoup, Requests, lxml). Use it to collect data from web pages, parse HTML, and extract structured information.

## Instructions

Use this skill when you need to:
- Extract text content from web pages
- Parse HTML tables and lists
- Collect data from multiple pages
- Download images and files
- Monitor website changes

## Prerequisites

- Python 3.8 or higher
- Required packages: requests, beautifulsoup4, lxml
- Optional: selenium (for JavaScript-heavy sites)

## Scripts

### scrape_page.py

Extracts content from a single web page.

**Usage:**
```bash
python scripts/scrape_page.py <url> [--selector "div.content"]
```

**Output:** Extracted text or HTML content

### scrape_table.py

Extracts tables from web pages and converts to CSV.

**Usage:**
```bash
python scripts/scrape_table.py <url> [--output data.csv]
```

**Output:** CSV file with table data

### download_images.py

Downloads all images from a web page.

**Usage:**
```bash
python scripts/download_images.py <url> [--output-dir images/]
```

**Output:** Downloaded images in specified directory

## Examples

### Example 1: Extract Article Text

```bash
# Extract main content from a news article
python scripts/scrape_page.py https://example.com/article --selector "article.content"
```

### Example 2: Scrape Product Table

```bash
# Extract product pricing table
python scripts/scrape_table.py https://example.com/products --output products.csv
```

### Example 3: Download Product Images

```bash
# Download all product images
python scripts/download_images.py https://example.com/gallery --output-dir product_images/
```

## Supported Features

### HTML Parsing

- CSS selectors for element targeting
- XPath expressions for complex queries
- Tag-based extraction
- Attribute extraction
- Text content extraction

### Data Extraction

- Tables (convert to CSV/JSON)
- Lists (ordered and unordered)
- Links and URLs
- Images and media
- Metadata (title, description, keywords)

### Advanced Features

- Follow pagination links
- Handle AJAX/JavaScript content (with Selenium)
- Respect robots.txt
- Rate limiting and delays
- User-agent rotation

## Configuration

The `assets/scraping_config.json` file contains default settings:
- Request timeout: 30 seconds
- User-agent string
- Rate limiting: 1 request per second
- Retry attempts: 3
- Respect robots.txt: true

## Best Practices

1. **Respect robots.txt**: Always check and follow robots.txt rules
2. **Rate Limiting**: Add delays between requests to avoid overloading servers
3. **User-Agent**: Use a descriptive user-agent string
4. **Error Handling**: Handle network errors and invalid responses
5. **Legal Compliance**: Ensure scraping is allowed by website terms of service

## Limitations

- Cannot scrape JavaScript-rendered content (use Selenium for that)
- May be blocked by anti-scraping measures (CAPTCHA, rate limiting)
- Website structure changes may break selectors
- Some sites require authentication or API access

## Legal and Ethical Considerations

### Legal

- Check website Terms of Service before scraping
- Respect copyright and intellectual property
- Don't scrape personal or sensitive data without permission
- Follow data protection regulations (GDPR, CCPA)

### Ethical

- Don't overload servers with excessive requests
- Respect robots.txt and meta robots tags
- Identify your bot with a proper user-agent
- Cache responses to minimize requests
- Consider using official APIs when available

## Error Handling

The scripts handle common errors:
- Network timeouts and connection errors
- HTTP error codes (404, 403, 500, etc.)
- Invalid HTML structure
- Missing elements or selectors
- Encoding issues

## Security Notes

- Validate and sanitize all URLs before scraping
- Be cautious with URLs from untrusted sources
- Don't execute JavaScript from scraped content
- Sanitize extracted data before storage
- Use HTTPS when possible
- Don't store sensitive data in plain text

## Troubleshooting

### Issue: Connection Timeout

**Solution:**
- Increase timeout value in configuration
- Check internet connection
- Verify URL is accessible

### Issue: 403 Forbidden Error

**Solution:**
- Add or change user-agent header
- Check if IP is blocked
- Respect rate limits

### Issue: Empty Results

**Solution:**
- Verify CSS selector is correct
- Check if content is JavaScript-rendered
- Inspect page source to confirm element exists

### Issue: Encoding Problems

**Solution:**
- Specify correct encoding in configuration
- Use UTF-8 as default
- Handle special characters properly

## Output Formats

### Text Output

- Plain text (stripped HTML)
- Markdown (converted from HTML)
- Raw HTML

### Structured Data

- CSV (for tables and lists)
- JSON (for complex structures)
- XML (for hierarchical data)

## Advanced Usage

### Using Custom Headers

```python
headers = {
    'User-Agent': 'MyBot/1.0',
    'Accept-Language': 'en-US,en;q=0.9'
}
```

### Handling Pagination

```python
# Follow "Next" links automatically
while next_page:
    scrape_page(next_page)
    next_page = find_next_link()
```

### Using Selenium for JavaScript

```python
from selenium import webdriver

driver = webdriver.Chrome()
driver.get(url)
# Wait for JavaScript to load
content = driver.page_source
```

## Resources

See `assets/scraping_guide.md` for:
- Detailed CSS selector examples
- XPath tutorial
- Anti-scraping countermeasures
- Legal resources and guidelines
