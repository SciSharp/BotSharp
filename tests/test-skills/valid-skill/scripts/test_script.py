#!/usr/bin/env python3
"""
Test Script for Agent Skills Validation

This script demonstrates a simple executable that can be bundled with a skill.
It performs basic operations to validate script execution functionality.
"""

import sys
import json
from datetime import datetime


def main():
    """Main function demonstrating script capabilities."""
    if len(sys.argv) > 1 and sys.argv[1] == "--help":
        print("Usage: test_script.py [--help] [--version] [--test]")
        print("\nOptions:")
        print("  --help     Show this help message")
        print("  --version  Show version information")
        print("  --test     Run a simple test")
        return 0
    
    if len(sys.argv) > 1 and sys.argv[1] == "--version":
        print("Test Script v1.0.0")
        return 0
    
    if len(sys.argv) > 1 and sys.argv[1] == "--test":
        result = {
            "status": "success",
            "message": "Test script executed successfully",
            "timestamp": datetime.now().isoformat(),
            "test_data": {
                "value1": 42,
                "value2": "test",
                "value3": [1, 2, 3]
            }
        }
        print(json.dumps(result, indent=2))
        return 0
    
    print("Test script loaded. Use --help for usage information.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
