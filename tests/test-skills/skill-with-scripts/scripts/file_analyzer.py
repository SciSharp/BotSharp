#!/usr/bin/env python3
"""
File Analyzer Script

Analyzes files and generates reports.
Demonstrates file operations in bundled scripts.
"""

import sys
import json
import argparse
import os
from datetime import datetime


def analyze_file(file_path):
    """Analyze a file and return statistics."""
    result = {
        "timestamp": datetime.now().isoformat(),
        "file_path": file_path,
        "status": "success",
        "analysis": None
    }
    
    try:
        if not os.path.exists(file_path):
            result["status"] = "error"
            result["error"] = "File not found"
            return result
        
        stat = os.stat(file_path)
        
        with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
        
        result["analysis"] = {
            "size_bytes": stat.st_size,
            "line_count": len(content.splitlines()),
            "char_count": len(content),
            "word_count": len(content.split()),
            "is_empty": len(content) == 0,
            "modified_time": datetime.fromtimestamp(stat.st_mtime).isoformat()
        }
        
    except PermissionError:
        result["status"] = "error"
        result["error"] = "Permission denied"
    except Exception as e:
        result["status"] = "error"
        result["error"] = str(e)
    
    return result


def main():
    """Main function."""
    parser = argparse.ArgumentParser(
        description="Analyze files and generate reports"
    )
    parser.add_argument(
        "--version",
        action="version",
        version="File Analyzer v1.0.0"
    )
    parser.add_argument(
        "--file",
        help="Path to file to analyze"
    )
    
    args = parser.parse_args()
    
    if not args.file:
        parser.print_help()
        return 1
    
    result = analyze_file(args.file)
    print(json.dumps(result, indent=2))
    
    return 0 if result["status"] == "success" else 1


if __name__ == "__main__":
    sys.exit(main())
