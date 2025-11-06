# Request/Response Middleware

## Overview

The K-Chief Marine Automation Platform implements a comprehensive middleware pipeline for handling cross-cutting concerns including request validation, rate limiting, logging, performance monitoring, and response caching. This document describes all middleware components and their configuration.

## Middleware Pipeline Order

The middleware pipeline is executed in the following order (critical for proper functionality):

```
1. CorrelationIdMiddleware          - Generate/retrieve correlation ID
2. RequestValidationMiddleware      - Validate incoming requests
3. RateLimitingMiddleware            - Enforce rate limits
4. ResponseCachingMiddleware         - Cache HTTP responses
5. ResponseTimeTrackingMiddleware    - Track detailed response times
6. RequestResponseLoggingMiddleware  - Log requests and responses
7. GlobalExceptionHandlingMiddleware - Handle exceptions globally
8. ResilienceMiddleware              - Apply resilience patterns
9. PerformanceMonitoringMiddleware   - Monitor performance metrics
10. Authentication/Authorization     - Authenticate and authorize requests
```

## Middleware Components

### 1. Correlation ID Middleware

**Purpose:** Ensures every request has a unique correlation ID for distributed tracing and request tracking.

**Features:**
- Generates correlation ID if not provided
- Checks for common correlation ID headers (`X-Correlation-ID`, `X-Request-ID`, `X-Trace-ID`)
- Adds correlation ID to response headers
- Pushes correlation ID to Serilog LogContext for structured logging

**Configuration:**
- No configuration required
- Automatically generates 12-character GUID if not provided

**Usage:**
```http
GET /api/vessels
X-Correlation-ID: abc123def456
```

**Response Headers:**
```http
X-Correlation-ID: abc123def456
```

### 2. Request Validation Middleware

**Purpose:** Validates incoming requests before they reach controllers to prevent invalid or malicious requests.

**Validation Checks:**
- Content-Length limits
- Content-Type validation for POST/PUT/PATCH
- Required headers
- Path length limits
- Query string length limits
- User agent blocking
- JSON format validation
- Path-based method restrictions

**Configuration:**
```json
{
  "Middleware": {
    "RequestValidation": {
      "MaxRequestSize": 10485760,
      "MaxPathLength": 2048,
      "MaxQueryStringLength": 2048,
      "AllowedContentTypes": [
        "application/json",
        "application/xml",
        "multipart/form-data"
      ],
      "RequiredHeaders": [],
      "BlockedUserAgents": []
    }
  }
}
```

**Error Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Request body size exceeds maximum allowed size",
  "instance": "/api/vessels",
  "correlationId": "abc123def456",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

### 3. Rate Limiting Middleware

**Purpose:** Prevents abuse and ensures fair resource usage by limiting the number of requests per time window.

**Features:**
- Fixed window and sliding window strategies
- Per-endpoint or global rate limiting
- IP-based, user-based, or API key-based identification
- Distributed caching support (Redis) for multi-instance deployments
- Configurable limits per endpoint
- Automatic exclusion of health checks and monitoring endpoints

**Configuration:**
```json
{
  "Middleware": {
    "RateLimiting": {
      "RequestsPerWindow": 100,
      "WindowSizeSeconds": 60,
      "Strategy": "FixedWindow",
      "PerEndpointLimiting": false,
      "ExcludedPaths": [
        "/health",
        "/health-ui",
        "/metrics"
      ]
    }
  }
}
```

**Rate Limit Headers:**
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1705312200
Retry-After: 45
```

**Rate Limit Exceeded Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Maximum 100 requests per 60 seconds.",
  "instance": "/api/vessels",
  "correlationId": "abc123def456",
  "retryAfter": 45,
  "timestamp": "2025-01-15T10:30:00Z"
}
```

**Strategies:**

1. **Fixed Window:** Requests are counted in fixed time windows (e.g., 0-60s, 60-120s)
2. **Sliding Window:** Requests are counted in a sliding time window

**Client Identification Priority:**
1. Authenticated user identity
2. API key (from `X-API-Key` header)
3. IP address

### 4. Response Caching Middleware

**Purpose:** Caches HTTP responses to improve performance and reduce server load.

**Features:**
- Automatic caching of GET requests
- Configurable cache duration per endpoint
- Cache-Control header support
- Size-based filtering
- User-specific caching (optional)

See [Caching Documentation](CACHING.md) for detailed information.

### 5. Response Time Tracking Middleware

**Purpose:** Tracks detailed response times and performance metrics for monitoring and optimization.

**Features:**
- Total request processing time
- Middleware pipeline time
- Status code categorization
- Per-endpoint timing
- Slow request detection
- Timing headers in responses

**Configuration:**
```json
{
  "Middleware": {
    "ResponseTimeTracking": {
      "SlowRequestThresholdMs": 1000,
      "IncludeTimingHeaders": true,
      "TrackDetailedTimings": true
    }
  }
}
```

**Response Headers:**
```http
X-Response-Time-Ms: 245
X-Middleware-Time-Ms: 12
X-Request-Start-Time: 1705312200000
```

**Metrics Recorded:**
- `middleware_time_ms` - Time spent in middleware pipeline
- `response_time_{category}_ms` - Response time by status code category (2xx, 4xx, 5xx)
- `endpoint_time_{method}:{path}_ms` - Response time per endpoint

### 6. Request/Response Logging Middleware

**Purpose:** Comprehensive logging of all HTTP requests and responses with structured data.

**Features:**
- Request logging with headers, body, and metadata
- Response logging with status code, body, and timing
- Client IP detection (handles proxies and load balancers)
- Correlation ID tracking
- Log level based on status code
- Configurable body size limits

**Logged Information:**
- Request method, path, query string
- Client IP address
- User agent
- Request/response body (for appropriate content types)
- Response status code
- Elapsed time
- Correlation ID

**Log Levels:**
- `Error`: Status code >= 500
- `Warning`: Status code >= 400
- `Information`: Status code < 400

### 7. Global Exception Handling Middleware

**Purpose:** Catches and handles all unhandled exceptions globally.

**Features:**
- RFC 7807 Problem Details format
- Custom exception handling
- Correlation ID tracking
- Structured error logging
- Security-aware error messages

See [Error Handling Documentation](ERROR_HANDLING.md) for detailed information.

### 8. Resilience Middleware

**Purpose:** Applies resilience patterns (retry, circuit breaker, timeout) to requests.

**Features:**
- Automatic retry for transient failures
- Circuit breaker protection
- Timeout enforcement
- Bulkhead isolation
- Fallback mechanisms

See [Resilience Documentation](RESILIENCE.md) for detailed information.

### 9. Performance Monitoring Middleware

**Purpose:** Monitors overall request performance and records metrics.

**Features:**
- Request duration tracking
- Status code monitoring
- Slow request detection
- Performance metrics collection

**Integration:**
- Integrates with `PerformanceMonitoringService`
- Records metrics for Application Insights
- Provides `/metrics` endpoint

## Configuration

### Environment-Specific Configuration

**Development:**
```json
{
  "Middleware": {
    "RateLimiting": {
      "RequestsPerWindow": 1000,
      "WindowSizeSeconds": 60
    },
    "RequestValidation": {
      "MaxRequestSize": 52428800
    }
  }
}
```

**Production:**
```json
{
  "Middleware": {
    "RateLimiting": {
      "RequestsPerWindow": 100,
      "WindowSizeSeconds": 60
    },
    "RequestValidation": {
      "MaxRequestSize": 10485760
    }
  }
}
```

## Best Practices

### 1. Middleware Order
- Always place `CorrelationIdMiddleware` first
- Place validation and rate limiting early in pipeline
- Place exception handling before business logic
- Place logging after validation to avoid logging invalid requests

### 2. Rate Limiting
- Use distributed caching (Redis) for multi-instance deployments
- Configure appropriate limits based on endpoint criticality
- Exclude health checks and monitoring endpoints
- Monitor rate limit violations for abuse detection

### 3. Request Validation
- Set appropriate size limits based on use case
- Block known malicious user agents
- Validate content types strictly
- Use path-based rules for method restrictions

### 4. Response Time Tracking
- Set appropriate slow request thresholds
- Monitor timing metrics regularly
- Alert on performance degradation
- Use timing headers for client-side monitoring

### 5. Logging
- Use structured logging with correlation IDs
- Avoid logging sensitive data (passwords, tokens)
- Set appropriate body size limits
- Use appropriate log levels

## Monitoring and Observability

### Metrics

All middleware components integrate with the performance monitoring system:

- Request count by endpoint
- Response time percentiles
- Rate limit violations
- Validation failures
- Error rates by status code

### Health Checks

Middleware health is monitored through:
- `/health` - Overall system health
- `/metrics` - Performance metrics endpoint

### Logging

All middleware uses structured logging with:
- Correlation IDs for request tracking
- Contextual properties for filtering
- Appropriate log levels
- Integration with Serilog

## Troubleshooting

### Rate Limit Issues

**Problem:** Legitimate requests being rate limited

**Solutions:**
- Increase `RequestsPerWindow` for specific endpoints
- Use per-endpoint rate limiting
- Exclude specific paths
- Use distributed caching for accurate limits across instances

### Validation Failures

**Problem:** Valid requests being rejected

**Solutions:**
- Check `MaxRequestSize` configuration
- Verify `AllowedContentTypes` includes required types
- Review `RequiredHeaders` configuration
- Check path-based rules

### Performance Issues

**Problem:** Slow response times

**Solutions:**
- Review timing metrics in `/metrics`
- Check slow request logs
- Optimize middleware order
- Consider response caching
- Review database query performance

## Related Documentation

- [Architecture Documentation](ARCHITECTURE.md)
- [Error Handling](ERROR_HANDLING.md)
- [Logging](LOGGING.md)
- [Caching](CACHING.md)
- [Monitoring](MONITORING.md)
- [Resilience](RESILIENCE.md)

