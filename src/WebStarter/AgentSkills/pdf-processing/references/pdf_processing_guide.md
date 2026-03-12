# PDF Processing Guide

## Overview

This guide provides detailed information on processing PDF documents using the pdf-processing skill.

## Installation

### Required Python Packages

```bash
pip install PyPDF2 pdfplumber Pillow
```

### Optional Packages for OCR

```bash
pip install pytesseract pdf2image
```

## Advanced Usage

### Handling Encrypted PDFs

To process password-protected PDFs:

```python
import PyPDF2

with open('encrypted.pdf', 'rb') as file:
    reader = PyPDF2.PdfReader(file)
    if reader.is_encrypted:
        reader.decrypt('password')
    # Process the PDF
```

### Working with Scanned Documents (OCR)

For scanned PDFs without text layer:

```python
from pdf2image import convert_from_path
import pytesseract

# Convert PDF to images
images = convert_from_path('scanned.pdf')

# Extract text using OCR
text = ''
for image in images:
    text += pytesseract.image_to_string(image)
```

### Performance Optimization

For large PDFs:

1. **Process pages in batches**: Don't load entire PDF into memory
2. **Use streaming**: Process pages one at a time
3. **Limit page range**: Only process necessary pages
4. **Cache results**: Store extracted data for reuse

```python
# Process specific page range
for page_num in range(start_page, end_page):
    page = reader.pages[page_num]
    text = page.extract_text()
    # Process text
```

## Common Issues and Solutions

### Issue: Garbled Text Extraction

**Cause:** PDF uses custom fonts or encoding

**Solution:**
- Try different extraction libraries (PyPDF2, pdfplumber, pdfminer)
- Use OCR as fallback
- Check PDF font embedding

### Issue: Tables Not Detected

**Cause:** Complex table layouts or merged cells

**Solution:**
- Adjust table detection settings in pdfplumber
- Use manual coordinate-based extraction
- Pre-process PDF to simplify layout

### Issue: Memory Errors with Large PDFs

**Cause:** Loading entire PDF into memory

**Solution:**
- Process pages incrementally
- Increase system memory
- Split PDF into smaller files

### Issue: Slow Processing

**Cause:** Complex PDF structure or large file size

**Solution:**
- Use multiprocessing for parallel page processing
- Optimize extraction parameters
- Cache intermediate results

## Best Practices

1. **Validate Input**: Always check file exists and is valid PDF
2. **Handle Errors**: Implement proper error handling and logging
3. **Resource Management**: Close file handles properly
4. **Security**: Validate PDF sources and sanitize extracted content
5. **Testing**: Test with various PDF types and formats

## API Reference

### extract_text.py

```
Usage: python extract_text.py <pdf_file> [--output <file>]

Arguments:
  pdf_file          Path to the PDF file
  --output, -o      Output file path (optional)

Returns:
  Extracted text content
```

### extract_tables.py

```
Usage: python extract_tables.py <pdf_file> [--output <file>]

Arguments:
  pdf_file          Path to the PDF file
  --output, -o      Output CSV file path (default: tables.csv)

Returns:
  CSV file with extracted tables
```

### get_metadata.py

```
Usage: python get_metadata.py <pdf_file> [--format <format>]

Arguments:
  pdf_file          Path to the PDF file
  --format, -f      Output format: json or text (default: json)

Returns:
  JSON or text with PDF metadata
```

## Examples

### Example 1: Batch Processing

```python
import os
from pathlib import Path

pdf_dir = Path('pdfs')
for pdf_file in pdf_dir.glob('*.pdf'):
    text = extract_text_from_pdf(pdf_file)
    output_file = pdf_file.with_suffix('.txt')
    output_file.write_text(text)
```

### Example 2: Extract Specific Pages

```python
import PyPDF2

with open('document.pdf', 'rb') as file:
    reader = PyPDF2.PdfReader(file)
    # Extract pages 5-10
    for page_num in range(4, 10):
        page = reader.pages[page_num]
        text = page.extract_text()
        print(f"Page {page_num + 1}: {text}")
```

### Example 3: Merge Multiple PDFs

```python
import PyPDF2

merger = PyPDF2.PdfMerger()

for pdf in ['doc1.pdf', 'doc2.pdf', 'doc3.pdf']:
    merger.append(pdf)

merger.write('merged.pdf')
merger.close()
```

## Resources

- [PyPDF2 Documentation](https://pypdf2.readthedocs.io/)
- [pdfplumber Documentation](https://github.com/jsvine/pdfplumber)
- [PDF Specification](https://www.adobe.com/devnet/pdf/pdf_reference.html)
