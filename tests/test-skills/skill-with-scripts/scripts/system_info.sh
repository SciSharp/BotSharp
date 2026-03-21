#!/bin/bash
# System Information Script
# Collects basic system information and returns JSON output

show_help() {
    echo "Usage: system_info.sh [--help] [--version]"
    echo ""
    echo "Collects system information and returns JSON output"
    echo ""
    echo "Options:"
    echo "  --help     Show this help message"
    echo "  --version  Show version information"
}

show_version() {
    echo "System Info v1.0.0"
}

collect_info() {
    echo "{"
    echo "  \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\","
    echo "  \"status\": \"success\","
    echo "  \"system\": {"
    echo "    \"os\": \"$(uname -s)\","
    echo "    \"kernel\": \"$(uname -r)\","
    echo "    \"architecture\": \"$(uname -m)\","
    echo "    \"hostname\": \"$(hostname)\","
    echo "    \"user\": \"$USER\","
    echo "    \"shell\": \"$SHELL\","
    echo "    \"pwd\": \"$(pwd)\""
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
    *)
        collect_info
        exit 0
        ;;
esac
