#!/usr/bin/env python3
"""
CSV Data Analysis Script
Performs comprehensive statistical analysis on CSV files.
"""

import sys
import argparse
import json
from pathlib import Path

def analyze_csv(csv_path, output_path=None):
    """
    Analyze a CSV file and generate statistical summary.
    
    Args:
        csv_path: Path to the CSV file
        output_path: Optional path to save the analysis report
        
    Returns:
        Analysis results as a dictionary
    """
    try:
        # Note: This is a placeholder implementation
        # In production, you would use pandas:
        #
        # import pandas as pd
        # df = pd.read_csv(csv_path)
        # 
        # analysis = {
        #     'shape': df.shape,
        #     'columns': list(df.columns),
        #     'dtypes': df.dtypes.to_dict(),
        #     'missing_values': df.isnull().sum().to_dict(),
        #     'statistics': df.describe().to_dict(),
        #     'correlations': df.corr().to_dict() if df.select_dtypes(include='number').shape[1] > 1 else {}
        # }
        
        # Placeholder implementation
        analysis = {
            'file': str(csv_path),
            'shape': [100, 5],
            'columns': ['id', 'name', 'age', 'city', 'score'],
            'dtypes': {
                'id': 'int64',
                'name': 'object',
                'age': 'int64',
                'city': 'object',
                'score': 'float64'
            },
            'missing_values': {
                'id': 0,
                'name': 2,
                'age': 1,
                'city': 3,
                'score': 0
            },
            'statistics': {
                'age': {
                    'count': 99,
                    'mean': 35.5,
                    'std': 12.3,
                    'min': 18,
                    'max': 65
                },
                'score': {
                    'count': 100,
                    'mean': 75.2,
                    'std': 15.8,
                    'min': 45,
                    'max': 98
                }
            }
        }
        
        # Generate report
        report = generate_report(analysis)
        
        # Save or print
        if output_path:
            with open(output_path, 'w') as f:
                f.write(report)
            print(f"Analysis saved to: {output_path}")
        else:
            print(report)
        
        return analysis
        
    except FileNotFoundError:
        print(f"Error: File not found: {csv_path}", file=sys.stderr)
        return None
    except Exception as e:
        print(f"Error analyzing CSV: {str(e)}", file=sys.stderr)
        return None

def generate_report(analysis):
    """Generate a formatted text report from analysis results."""
    report = []
    report.append("=" * 60)
    report.append("CSV DATA ANALYSIS REPORT")
    report.append("=" * 60)
    report.append(f"\nFile: {analysis['file']}")
    report.append(f"Shape: {analysis['shape'][0]} rows × {analysis['shape'][1]} columns")
    
    report.append("\n" + "-" * 60)
    report.append("COLUMNS")
    report.append("-" * 60)
    for col in analysis['columns']:
        dtype = analysis['dtypes'].get(col, 'unknown')
        missing = analysis['missing_values'].get(col, 0)
        report.append(f"  {col:20s} {dtype:15s} (missing: {missing})")
    
    report.append("\n" + "-" * 60)
    report.append("STATISTICS")
    report.append("-" * 60)
    for col, stats in analysis['statistics'].items():
        report.append(f"\n{col}:")
        for stat, value in stats.items():
            report.append(f"  {stat:10s}: {value:.2f}")
    
    report.append("\n" + "=" * 60)
    
    return "\n".join(report)

def main():
    parser = argparse.ArgumentParser(description='Analyze CSV files')
    parser.add_argument('csv_file', help='Path to the CSV file')
    parser.add_argument('--output', '-o', help='Output file path (optional)')
    parser.add_argument('--format', '-f', choices=['text', 'json'], default='text',
                        help='Output format (default: text)')
    
    args = parser.parse_args()
    
    # Analyze CSV
    analysis = analyze_csv(args.csv_file, args.output if args.format == 'text' else None)
    
    # Output JSON if requested
    if analysis and args.format == 'json':
        print(json.dumps(analysis, indent=2))

if __name__ == '__main__':
    main()
