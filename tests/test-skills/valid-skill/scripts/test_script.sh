#!/bin/bash
# Test Bash Script for Agent Skills Validation
# This script demonstrates a simple shell script that can be bundled with a skill.

show_help() {
    echo "Usage: test_script.sh [--help] [--version] [--test]"
    echo ""
    echo "Options:"
    echo "  --help     Show this help message"
    echo "  --version  Show version information"
    echo "  --test     Run a simple test"
}

show_version() {
    echo "Test Script v1.0.0"
}

run_test() {
    echo "{"
    echo "  \"status\": \"success\","
    echo "  \"message\": \"Bash test script executed successfully\","
    echo "  \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\","
    echo "  \"shell\": \"$SHELL\","
    echo "  \"test_data\": {"
    echo "    \"value1\": 42,"
    echo "    \"value2\": \"test\""
    echo "  }"
    echo "}"
}

# Main script logic
case "${1:-}" in
    --help)
        show_help
        exit 0
        ;;
    --version)
        show_version
        exit 0
        ;;
    --test)
        run_test
        exit 0
        ;;
    *)
        echo "Test bash script loaded. Use --help for usage information."
        exit 0
        ;;
esac
