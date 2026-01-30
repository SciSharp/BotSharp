---
name: data-analysis
description: Analyze and visualize data using Python pandas and matplotlib
---

# Data Analysis Skill

## Overview

This skill provides comprehensive data analysis capabilities using Python's data science stack (pandas, numpy, matplotlib, seaborn). Use it to analyze CSV files, perform statistical analysis, and create visualizations.

## Instructions

Use this skill when you need to:
- Load and explore CSV/Excel data files
- Perform statistical analysis (mean, median, correlation, etc.)
- Clean and transform data
- Create charts and visualizations
- Generate summary reports

## Prerequisites

- Python 3.8 or higher
- Required packages: pandas, numpy, matplotlib, seaborn

## Scripts

### analyze_csv.py

Performs comprehensive analysis on CSV files including:
- Basic statistics (count, mean, std, min, max)
- Missing value analysis
- Data type detection
- Correlation analysis

**Usage:**
```bash
python scripts/analyze_csv.py <csv_file> [--output report.txt]
```

### visualize_data.py

Creates various visualizations from data:
- Histograms for numerical columns
- Bar charts for categorical data
- Scatter plots for relationships
- Correlation heatmaps

**Usage:**
```bash
python scripts/visualize_data.py <csv_file> [--type histogram|scatter|heatmap]
```

### clean_data.py

Cleans and preprocesses data:
- Removes duplicates
- Handles missing values
- Normalizes column names
- Converts data types

**Usage:**
```bash
python scripts/clean_data.py <input_csv> --output <cleaned_csv>
```

## Examples

### Example 1: Analyze Sales Data

```bash
# Get statistical summary of sales data
python scripts/analyze_csv.py data/sales_2024.csv --output sales_analysis.txt
```

### Example 2: Visualize Customer Distribution

```bash
# Create histogram of customer ages
python scripts/visualize_data.py data/customers.csv --type histogram --column age
```

### Example 3: Clean Survey Data

```bash
# Clean and standardize survey responses
python scripts/clean_data.py data/survey_raw.csv --output data/survey_clean.csv
```

## Data Format Requirements

### CSV Files

- UTF-8 encoding recommended
- First row should contain column headers
- Consistent delimiter (comma, tab, or semicolon)
- Numeric values should not contain currency symbols

### Excel Files

- .xlsx or .xls format
- Data should be in the first sheet (or specify sheet name)
- Avoid merged cells in data range

## Analysis Capabilities

### Statistical Analysis

- Descriptive statistics (mean, median, mode, std dev)
- Correlation analysis
- Distribution analysis
- Outlier detection
- Trend analysis

### Data Cleaning

- Duplicate removal
- Missing value handling (drop, fill, interpolate)
- Data type conversion
- Column renaming and standardization
- Value normalization

### Visualization

- Line charts (time series)
- Bar charts (categorical comparisons)
- Histograms (distributions)
- Scatter plots (relationships)
- Heatmaps (correlations)
- Box plots (outliers)

## Configuration

The `assets/analysis_config.json` file contains default settings:
- Missing value strategy: drop, fill, or interpolate
- Outlier detection method: IQR or Z-score
- Visualization style: default, seaborn, or ggplot
- Output format: PNG, PDF, or SVG

## Best Practices

1. **Data Validation**: Always inspect data before analysis
2. **Backup Original**: Keep original data files unchanged
3. **Document Assumptions**: Note any data cleaning decisions
4. **Check Data Types**: Ensure columns have correct types
5. **Handle Missing Data**: Choose appropriate strategy for your use case

## Limitations

- Maximum file size: 500MB (configurable)
- Large datasets may require increased memory
- Complex visualizations may take longer to generate
- Some statistical methods require normally distributed data

## Error Handling

The scripts handle common errors:
- File not found or inaccessible
- Invalid CSV format or encoding
- Insufficient data for analysis
- Memory limitations for large files

## Output Formats

### Analysis Reports

- Plain text summary
- JSON structured data
- HTML formatted report
- Markdown documentation

### Visualizations

- PNG (default, good for web)
- PDF (high quality, print-ready)
- SVG (scalable, editable)

## Security Notes

- Validate data sources before processing
- Be cautious with data from untrusted sources
- Sanitize file paths to prevent directory traversal
- Limit file sizes to prevent resource exhaustion
- Don't expose sensitive data in visualizations
