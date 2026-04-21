# API Reference

## Overview

This document provides sample API documentation for testing the Agent Skills reference file functionality.

## Table of Contents

1. [Authentication](#authentication)
2. [Endpoints](#endpoints)
3. [Data Models](#data-models)
4. [Error Handling](#error-handling)

## Authentication

All API requests require authentication using an API key:

```
Authorization: Bearer YOUR_API_KEY
```

## Endpoints

### GET /api/test

Retrieve test data.

**Request:**
```http
GET /api/test HTTP/1.1
Host: api.example.com
Authorization: Bearer YOUR_API_KEY
```

**Response:**
```json
{
  "status": "success",
  "data": {
    "id": 1,
    "name": "Test Item",
    "value": 42
  }
}
```

### POST /api/test

Create a new test item.

**Request:**
```http
POST /api/test HTTP/1.1
Host: api.example.com
Authorization: Bearer YOUR_API_KEY
Content-Type: application/json

{
  "name": "New Item",
  "value": 100
}
```

**Response:**
```json
{
  "status": "success",
  "data": {
    "id": 2,
    "name": "New Item",
    "value": 100
  }
}
```

## Data Models

### TestItem

| Field | Type   | Description           |
|-------|--------|-----------------------|
| id    | int    | Unique identifier     |
| name  | string | Item name             |
| value | int    | Numeric value         |

## Error Handling

### Error Response Format

```json
{
  "status": "error",
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message"
  }
}
```

### Common Error Codes

- `UNAUTHORIZED`: Invalid or missing API key
- `NOT_FOUND`: Resource not found
- `VALIDATION_ERROR`: Invalid request data
- `INTERNAL_ERROR`: Server error
