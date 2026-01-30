#!/usr/bin/env python3
"""
Data Processor Script

Processes input data and returns structured JSON output.
Demonstrates script bundling with Agent Skills.
"""

import sys
import json
import argparse
from datetime import datetime


def process_data(data_type, input_value):
    """Process data based on type."""
    result = {
        "timestamp": datetime.now().isoformat(),
        "data_type": data_type,
        "input": input_value,
        "processed": None,
        "status": "success"
    }
    
    if data_type == "number":
        try:
            num = float(input_value)
            result["processed"] = {
                "value": num,
                "squared": num ** 2,
                "doubled": num * 2,
                "is_positive": num > 0
            }
        except ValueError:
            result["status"] = "error"
            result["error"] = "Invalid number format"
    
    elif data_type == "text":
        result["processed"] = {
            "length": len(input_value),
            "uppercase": input_value.upper(),
            "lowercase": input_value.lower(),
            "word_count": len(input_value.split())
        }
    
    elif data_type == "list":
        try:
            items = json.loads(input_value)
            result["processed"] = {
                "count": len(items),
                "first": items[0] if items else None,
                "last": items[-1] if items else None,
                "sorted": sorted(items)
            }
        except (json.JSONDecodeError, TypeError):
            result["status"] = "error"
            result["error"] = "Invalid list format"
    
    else:
        result["status"] = "error"
        result["error"] = f"Unknown data type: {data_type}"
    
    return result


def main():
    """Main function."""
    parser = argparse.ArgumentParser(
        description="Process data and return structured output"
    )
    parser.add_argument(
        "--version",
        action="version",
        version="Data Processor v1.0.0"
    )
    parser.add_argument(
        "--type",
        choices=["number", "text", "list"],
        help="Type of data to process"
    )
    parser.add_argument(
        "--input",
        help="Input value to process"
    )
    
    args = parser.parse_args()
    
    if not args.type or not args.input:
        parser.print_help()
        return 1
    
    result = process_data(args.type, args.input)
    print(json.dumps(result, indent=2))
    
    return 0 if result["status"] == "success" else 1


if __name__ == "__main__":
    sys.exit(main())
