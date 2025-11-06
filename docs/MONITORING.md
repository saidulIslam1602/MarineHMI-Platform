# Monitoring and Observability Guide

## Overview

The K-Chief Marine Automation Platform implements comprehensive monitoring and observability features to ensure production-ready operations. This includes health checks, performance monitoring, metrics collection, and integration with monitoring platforms.

## Health Checks

### Available Health Check Endpoints

#### 1. General Health Check
```
GET /health
```
Returns the overall health status of all components.

**Response Example:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "self": {
      "status": "Healthy",
      "description": "API is running"
    },
    "database": {
      "status": "Healthy",
      "description": "Database connection is healthy"
    },
    "opcua": {
      "status": "Healthy", 
      "description": "OPC UA client is available but not connected"
    }
  }
}
```

#### 2. Readiness Probe
```
GET /health/ready
```
Kubernetes readiness probe endpoint. Checks if the application is ready to serve traffic.

#### 3. Liveness Probe  
```
GET /health/live
```
Kubernetes liveness probe endpoint. Checks if the application is running and should be restarted if unhealthy.

### Health Check Components

#### Built-in Health Checks
- **Self Check**: Verifies the API is running
- **Database Check**: Tests Entity Framework DbContext connectivity
- **SQLite Check**: Validates database file accessibility
- **Memory Check**: Monitors allocated and private memory usage

#### Custom Health Checks
- **OPC UA Client**: Checks OPC UA client connectivity and availability
- **Modbus Client**: Validates Modbus TCP client status
- **Message Bus**: Tests RabbitMQ connection (graceful degradation to simulation mode)
- **Vessel Control Service**: Verifies core vessel management functionality
- **Alarm System**: Checks alarm service operations and active alarm counts

### Health Check Configuration

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddSqlite(connectionString, "sqlite")
    .AddCheck<OpcUaHealthCheck>("opcua")
    .AddCheck<ModbusHealthCheck>("modbus")
    .AddCheck<MessageBusHealthCheck>("messagebus")
    .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 1024, "memory")
    .AddPrivateMemoryHealthCheck(maximumMemoryBytes: 2_000_000_000, "private_memory");
```

## Health Checks UI

### Dashboard Access
```
GET /health-ui
```
Interactive dashboard showing:
- Real-time health status of all components
- Historical health data and trends
- Response times and failure rates
- Detailed error information

### Features
- **Real-time Updates**: 30-second refresh interval
- **Historical Data**: 50 entries per endpoint
- **Visual Indicators**: Color-coded status (Green/Yellow/Red)
- **Detailed Views**: Expandable component details
- **Export Capabilities**: Health data export options

## Performance Monitoring

### Metrics Collection

The platform collects comprehensive performance metrics:

#### HTTP Request Metrics
- **Request Count**: Total HTTP requests by method, endpoint, status code
- **Request Duration**: Response time histograms
- **Error Rate**: Failed requests by endpoint and error type
- **Throughput**: Requests per second

#### Application Metrics
- **Vessel Operations**: Operation duration and success rates
- **Database Operations**: Query performance and connection health
- **Memory Usage**: Heap allocation and garbage collection
- **CPU Usage**: Processor utilization
- **Active Connections**: Connection pool status

#### Custom Business Metrics
- **Vessel Count**: Number of active vessels
- **Engine Status**: Running/stopped engine counts
- **Active Alarms**: Alarm counts by severity
- **Protocol Connections**: OPC UA and Modbus connection status

### Performance Monitoring Service

```csharp
public class PerformanceMonitoringService
{
    // Records HTTP request metrics
    public void RecordHttpRequest(string method, string endpoint, int statusCode, double duration);
    
    // Records vessel operation metrics
    public void RecordVesselOperation(string operation, string vesselId, bool success, double duration);
    
    // Records database operation metrics  
    public void RecordDatabaseOperation(string operation, string table, bool success, double duration);
    
    // Gets current performance statistics
    public object GetPerformanceStats();
}
```

### Metrics Endpoint

```
GET /metrics
```
Returns current performance statistics in JSON format:

```json
{
  "memoryUsageMB": 145.23,
  "cpuUsagePercent": 12.5,
  "threadCount": 24,
  "handleCount": 156,
  "workingSetMB": 178.45,
  "privateMemoryMB": 134.67,
  "gcGen0Collections": 45,
  "gcGen1Collections": 12,
  "gcGen2Collections": 3,
  "uptime": "01:23:45.123"
}
```

## Request Monitoring

### Performance Monitoring Middleware

Automatically tracks all HTTP requests:

- **Request Correlation**: Unique correlation ID per request
- **Response Time Tracking**: Millisecond precision timing
- **Slow Request Detection**: Configurable threshold alerting
- **Error Logging**: Detailed error context and stack traces
- **Request/Response Headers**: Correlation ID injection

### Correlation ID Tracking

Each request receives a unique correlation ID:
```
X-Correlation-ID: a1b2c3d4
```

This ID is:
- Added to all log entries
- Included in response headers
- Used for distributed tracing
- Available in error reports

### Slow Request Monitoring

Configurable thresholds for slow request detection:
```json
{
  "Monitoring": {
    "SlowRequestThresholdSeconds": 1.0
  }
}
```

Slow requests are logged with:
- Full request details
- Performance breakdown
- Correlation ID for tracking
- Recommended optimizations

## Application Insights Integration

### Configuration

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=your-key-here"
  }
}
```

### Telemetry Collection

Application Insights automatically collects:

#### Request Telemetry
- HTTP request details
- Response times and status codes
- User sessions and page views
- Custom events and metrics

#### Dependency Telemetry
- Database calls and performance
- External API calls
- Message queue operations
- Cache operations

#### Exception Telemetry
- Unhandled exceptions
- Custom exception tracking
- Stack traces and context
- User impact analysis

#### Performance Counters
- CPU and memory usage
- Request rates and throughput
- Dependency call rates
- Custom business metrics

### Custom Telemetry

```csharp
// Track custom events
telemetryClient.TrackEvent("VesselStarted", new Dictionary<string, string>
{
    ["VesselId"] = vesselId,
    ["EngineCount"] = engineCount.ToString()
});

// Track custom metrics
telemetryClient.TrackMetric("ActiveVessels", vesselCount);

// Track dependencies
telemetryClient.TrackDependency("OPC UA", "ReadNode", startTime, duration, success);
```

## Kubernetes Integration

### Health Check Probes

#### Liveness Probe
```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 30
  timeoutSeconds: 5
  failureThreshold: 3
```

#### Readiness Probe
```yaml
readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  timeoutSeconds: 5
  failureThreshold: 3
```

#### Startup Probe
```yaml
startupProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 5
  failureThreshold: 30
```

### Horizontal Pod Autoscaling

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: kchief-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: kchief-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
```

## Prometheus Integration

### ServiceMonitor Configuration

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: kchief-api-metrics
spec:
  selector:
    matchLabels:
      app: kchief-api
  endpoints:
  - port: http
    path: /metrics
    interval: 30s
```

### Metrics Export

The `/metrics` endpoint provides Prometheus-compatible metrics:

```
# HELP kchief_http_requests_total Total number of HTTP requests
# TYPE kchief_http_requests_total counter
kchief_http_requests_total{method="GET",endpoint="/api/vessels",status_code="200"} 1234

# HELP kchief_http_request_duration_seconds HTTP request duration
# TYPE kchief_http_request_duration_seconds histogram
kchief_http_request_duration_seconds_bucket{method="GET",endpoint="/api/vessels",le="0.1"} 890
```

## Alerting and Notifications

### Health Check Alerts

Configure alerts based on health check status:

#### Critical Alerts
- API completely down (liveness probe failing)
- Database connectivity lost
- Memory usage exceeding limits
- High error rates (>5% of requests)

#### Warning Alerts  
- Slow response times (>1 second average)
- Degraded service status
- High active alarm counts
- Protocol connection issues

### Performance Alerts

#### Response Time Alerts
```
avg(kchief_http_request_duration_seconds) > 1.0
```

#### Error Rate Alerts
```
rate(kchief_http_errors_total[5m]) / rate(kchief_http_requests_total[5m]) > 0.05
```

#### Memory Usage Alerts
```
kchief_memory_usage_bytes / 1024 / 1024 > 512
```

## Logging Configuration

### Structured Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "KChief.Platform.API.Middleware.PerformanceMonitoringMiddleware": "Information"
    }
  }
}
```

### Log Correlation

All logs include:
- **Correlation ID**: Request tracking
- **Timestamp**: UTC timestamps
- **Level**: Log severity
- **Category**: Logger category
- **Message**: Structured message
- **Properties**: Additional context

### Log Aggregation

Recommended log aggregation setup:
- **ELK Stack**: Elasticsearch, Logstash, Kibana
- **Azure Monitor**: Application Insights integration
- **Grafana Loki**: Prometheus ecosystem integration

## Monitoring Best Practices

### Health Check Design
1. **Fast Execution**: Keep health checks under 5 seconds
2. **Meaningful Status**: Return specific error information
3. **Dependency Isolation**: Don't fail for non-critical dependencies
4. **Graceful Degradation**: Continue operating with reduced functionality

### Performance Monitoring
1. **Baseline Establishment**: Measure normal performance patterns
2. **Threshold Setting**: Set realistic alerting thresholds
3. **Trend Analysis**: Monitor performance trends over time
4. **Capacity Planning**: Use metrics for scaling decisions

### Alerting Strategy
1. **Alert Fatigue Prevention**: Avoid noisy alerts
2. **Severity Classification**: Critical vs. warning alerts
3. **Escalation Procedures**: Define response procedures
4. **Documentation**: Maintain runbook documentation

## Troubleshooting

### Common Issues

#### Health Check Failures
```bash
# Check health status
curl http://localhost:8080/health

# Check specific component
curl http://localhost:8080/health | jq '.entries.database'

# View health UI
open http://localhost:8080/health-ui
```

#### Performance Issues
```bash
# Check current metrics
curl http://localhost:8080/metrics

# Monitor request patterns
tail -f logs/application.log | grep "Request completed"

# Check memory usage
curl http://localhost:8080/metrics | jq '.memoryUsageMB'
```

#### Database Connectivity
```bash
# Test database health
curl http://localhost:8080/health | jq '.entries.database'

# Check Entity Framework logs
grep "Microsoft.EntityFrameworkCore" logs/application.log
```

### Diagnostic Commands

```bash
# Health check status
kubectl get pods -l app=kchief-api
kubectl describe pod <pod-name>

# View pod logs
kubectl logs -f <pod-name>

# Port forward for local testing
kubectl port-forward svc/kchief-api-service 8080:80

# Check HPA status
kubectl get hpa kchief-api-hpa
```

## Monitoring Checklist

### Development Environment
- [ ] Health checks return expected status
- [ ] Performance metrics are collected
- [ ] Slow requests are logged
- [ ] Error handling works correctly
- [ ] Health UI is accessible

### Staging Environment
- [ ] All health checks pass
- [ ] Performance baselines established
- [ ] Alert thresholds configured
- [ ] Log aggregation working
- [ ] Kubernetes probes configured

### Production Environment
- [ ] Monitoring dashboards created
- [ ] Alert notifications configured
- [ ] Escalation procedures documented
- [ ] Performance SLAs defined
- [ ] Disaster recovery tested

This comprehensive monitoring setup ensures production-ready observability and helps maintain high availability and performance of the K-Chief Marine Automation Platform.
