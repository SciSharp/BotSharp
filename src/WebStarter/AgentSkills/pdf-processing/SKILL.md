---
name: pdf-processing
description: Extract text and data from PDF documents using Python
---

# PDF Processing Skill

## Overview

This skill provides tools for extracting text, tables, and metadata from PDF documents. It uses Python libraries like PyPDF2 and pdfplumber to handle various PDF formats.

## Instructions

Use this skill when you need to:
- Extract text content from PDF files
- Parse tables from PDF documents
- Extract metadata (author, title, creation date)
- Split or merge PDF files
- Convert PDF pages to images

## Prerequisites

- Python 3.8 or higher
- Required packages: PyPDF2, pdfplumber, Pillow

## Scripts

### extract_text.py

Extracts all text content from a PDF file.

**Usage:**
```bash
python scripts/extract_text.py <pdf_file_path>
```

**Output:** Plain text content of the PDF

### extract_tables.py

Extracts tables from PDF documents and converts them to CSV format.

**Usage:**
```bash
python scripts/extract_tables.py <pdf_file_path> [--output tables.csv]
```

**Output:** CSV file containing extracted tables

### get_metadata.py

Retrieves PDF metadata including author, title, subject, and creation date.

**Usage:**
```bash
python scripts/get_metadata.py <pdf_file_path>
```

**Output:** JSON object with metadata fields

## Examples

### Example 1: Extract Text from Invoice

```python
# Extract text from an invoice PDF
python scripts/extract_text.py invoices/invoice_2024_001.pdf
```

### Example 2: Extract Tables from Report

```python
# Extract tables from a financial report
python scripts/extract_tables.py reports/q4_financial.pdf --output q4_tables.csv
```

### Example 3: Get Document Metadata

```python
# Get metadata from a contract
python scripts/get_metadata.py contracts/service_agreement.pdf
```

## References

See `references/pdf_processing_guide.md` for detailed documentation on:
- Handling encrypted PDFs
- Working with scanned documents (OCR)
- Performance optimization for large PDFs
- Common troubleshooting steps

## Configuration

The `assets/config.json` file contains default settings:
- Maximum file size: 50MB
- OCR language: English
- Output encoding: UTF-8
- Table detection sensitivity: Medium

## Limitations

- Scanned PDFs require OCR (not included by default)
- Complex layouts may affect table extraction accuracy
- Password-protected PDFs require manual password input
- Very large PDFs (>100MB) may require increased memory

## Error Handling

The scripts handle common errors:
- File not found
- Corrupted PDF files
- Insufficient permissions
- Memory limitations

## Security Notes

- Always validate PDF file sources
- Be cautious with PDFs from untrusted sources
- Limit file sizes to prevent resource exhaustion
- Sanitize extracted text before processing
