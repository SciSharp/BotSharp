#!/bin/bash
# File Operations Script
# Performs common file operations and returns JSON output

show_help() {
    echo "Usage: file_operations.sh [--help] [--version] [--list DIR] [--count DIR]"
    echo ""
    echo "Performs file operations and returns JSON output"
    echo ""
    echo "Options:"
    echo "  --help          Show this help message"
    echo "  --version       Show version information"
    echo "  --list DIR      List files in directory"
    echo "  --count DIR     Count files in directory"
}

show_version() {
    echo "File Operations v1.0.0"
}

list_files() {
    local dir="$1"
    if [ ! -d "$dir" ]; then
        echo "{\"status\": \"error\", \"error\": \"Directory not found: $dir\"}"
        return 1
    fi
    
    echo "{"
    echo "  \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\","
    echo "  \"status\": \"success\","
    echo "  \"directory\": \"$dir\","
    echo "  \"files\": ["
    
    local first=true
    for file in "$dir"/*; do
        if [ -e "$file" ]; then
            if [ "$first" = true ]; then
                first=false
            else
                echo ","
            fi
            echo -n "    \"$(basename "$file")\""
        fi
    done
    
    echo ""
    echo "  ]"
    echo "}"
}

count_files() {
    local dir="$1"
    if [ ! -d "$dir" ]; then
        echo "{\"status\": \"error\", \"error\": \"Directory not found: $dir\"}"
        return 1
    fi
    
    local count=$(find "$dir" -maxdepth 1 -type f | wc -l)
    
    echo "{"
    echo "  \"timestamp\": \"$(date -u +%Y-%m-%dT%H:%M:%SZ)\","
    echo "  \"status\": \"success\","
    echo "  \"directory\": \"$dir\","
    echo "  \"file_count\": $count"
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
    --list)
        if [ -z "$2" ]; then
            echo "{\"status\": \"error\", \"error\": \"Directory argument required\"}"
            exit 1
        fi
        list_files "$2"
        exit $?
        ;;
    --count)
        if [ -z "$2" ]; then
            echo "{\"status\": \"error\", \"error\": \"Directory argument required\"}"
            exit 1
        fi
        count_files "$2"
        exit $?
        ;;
    *)
        show_help
        exit 1
        ;;
esac
