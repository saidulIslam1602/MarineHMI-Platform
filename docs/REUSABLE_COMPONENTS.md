# Reusable Components and Frameworks

## Overview

The K-Chief Marine Automation Platform includes a comprehensive set of reusable components, frameworks, and utilities that promote code reuse, consistency, and maintainability across the entire application.

## Architecture

### Component Organization

```
KChief.Platform.Core/
├── Validation/          # Shared validation framework
├── Extensions/          # Extension methods library
├── Services/            # Base classes for services
├── Middleware/          # Base middleware classes
└── Utilities/           # Utility classes
```

## Shared Validation Framework

### BaseValidator<T>

Base class for FluentValidation validators with common validation rules.

**Location:** `KChief.Platform.Core/Validation/BaseValidator.cs`

**Features:**
- Common validation methods
- Consistent error messages
- Error code generation
- Reusable validation patterns

**Usage Example:**

```csharp
public class VesselRequestValidator : BaseValidator<VesselRequest>
{
    public VesselRequestValidator()
    {
        RuleFor(x => x.Name)
            .ValidateRequiredString("Name")
            .ValidateStringLength("Name", 3, 100);
            
        RuleFor(x => x.Email)
            .ValidateEmail("Email");
            
        RuleFor(x => x.Length)
            .ValidateRange("Length", 1.0, 1000.0);
    }
}
```

**Available Methods:**
- `ValidateRequiredString` - Validates non-empty strings
- `ValidateStringLength` - Validates string length
- `ValidateEmail` - Validates email format
- `ValidateRange` - Validates numeric ranges
- `ValidateGreaterThan` - Validates minimum values
- `ValidateLessThan` - Validates maximum values
- `ValidateNotInPast` - Validates dates not in past
- `ValidateNotInFuture` - Validates dates not in future
- `ValidateGuid` - Validates GUID format
- `ValidateUrl` - Validates URL format

## Extension Methods Library

### DateTimeExtensions

Extension methods for DateTime operations.

**Location:** `KChief.Platform.Core/Extensions/DateTimeExtensions.cs`

**Features:**
- Unix timestamp conversion
- Date range operations (start/end of day, week, month)
- Relative time strings
- Duration formatting

**Usage Examples:**

```csharp
// Unix timestamp
var timestamp = DateTime.UtcNow.ToUnixTimeSeconds();
var date = DateTimeExtensions.FromUnixTimeSeconds(timestamp);

// Date ranges
var startOfDay = dateTime.StartOfDay();
var endOfMonth = dateTime.EndOfMonth();

// Relative time
var relative = dateTime.ToRelativeTimeString(); // "2 hours ago"

// Duration
var duration = TimeSpan.FromMinutes(90).ToDurationString(); // "1h 30m"
```

### StringExtensions

Extension methods for string operations.

**Location:** `KChief.Platform.Core/Extensions/StringExtensions.cs`

**Features:**
- Case conversion (camelCase, PascalCase, kebab-case, snake_case)
- Truncation
- Masking sensitive data
- HTML stripping
- Email extraction
- Pattern matching

**Usage Examples:**

```csharp
// Case conversion
var camel = "HelloWorld".ToCamelCase(); // "helloWorld"
var kebab = "HelloWorld".ToKebabCase(); // "hello-world"
var snake = "HelloWorld".ToSnakeCase(); // "hello_world"

// Truncation
var truncated = "Long text here".Truncate(10); // "Long te..."

// Masking
var masked = "password123".Mask(4); // "pass*******"

// Pattern matching
var isValid = "vessel-001".MatchesPattern(@"^vessel-\d{3}$");
```

### CollectionExtensions

Extension methods for collections.

**Location:** `KChief.Platform.Core/Extensions/CollectionExtensions.cs`

**Features:**
- Null/empty checks
- ForEach operations (sync and async)
- Batching
- Safe dictionary conversion
- Distinct by key
- Shuffling
- Null filtering

**Usage Examples:**

```csharp
// Null checks
if (collection.IsNullOrEmpty()) { /* ... */ }

// ForEach
collection.ForEach(item => Console.WriteLine(item));
await collection.ForEachAsync(async item => await ProcessAsync(item));

// Batching
var batches = collection.Batch(100);

// Distinct by property
var distinct = collection.DistinctBy(x => x.Id);

// WhereNotNull
var nonNull = collection.WhereNotNull();
```

## Base Classes

### BaseService

Base class for services with common functionality.

**Location:** `KChief.Platform.Core/Services/BaseService.cs`

**Features:**
- Automatic logging
- Error handling
- Retry logic
- Execution time measurement

**Usage Example:**

```csharp
public class MyService : BaseService
{
    public MyService(ILogger<MyService> logger) : base(logger) { }

    public async Task<string> GetDataAsync(string id)
    {
        return await ExecuteWithLoggingAsync(
            "GetData",
            async () => await FetchDataAsync(id),
            defaultValue: string.Empty);
    }

    public async Task ProcessAsync()
    {
        await ExecuteWithRetryAsync(
            async () => await DoWorkAsync(),
            maxRetries: 3,
            delay: TimeSpan.FromSeconds(1));
    }
}
```

**Available Methods:**
- `ExecuteWithLoggingAsync` - Execute with automatic logging
- `ExecuteWithRetryAsync` - Execute with retry logic
- `MeasureExecutionTimeAsync` - Measure and log execution time

### BaseRepository

Base class for repositories with common functionality.

**Location:** `KChief.Platform.Core/Services/BaseRepository.cs`

**Features:**
- ID validation
- Entity validation
- Operation logging

**Usage Example:**

```csharp
public class MyRepository : BaseRepository
{
    public MyRepository(ILogger<MyRepository> logger) : base(logger) { }

    public Task<Entity> GetByIdAsync(string id)
    {
        ValidateId(id, "Entity");
        LogOperation("GetById", id);
        // ... implementation
    }
}
```

### BaseMiddleware

Base class for middleware with common functionality.

**Location:** `KChief.Platform.Core/Middleware/BaseMiddleware.cs`

**Features:**
- Correlation ID handling
- Client IP detection
- Log context creation
- Exception handling
- Health check skipping

**Usage Example:**

```csharp
public class MyMiddleware : BaseMiddleware
{
    public MyMiddleware(RequestDelegate next, ILogger<MyMiddleware> logger) 
        : base(next, logger) { }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldProcess(context))
        {
            await Next(context);
            return;
        }

        using (CreateLogContext(context, "MyOperation"))
        {
            try
            {
                var correlationId = GetCorrelationId(context);
                var clientIp = GetClientIpAddress(context);
                
                // Process request
                await Next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
    }
}
```

## Utility Classes

### Guard

Guard clauses for parameter validation.

**Location:** `KChief.Platform.Core/Utilities/Guard.cs`

**Usage Examples:**

```csharp
// Null checks
Guard.AgainstNull(value, nameof(value));
Guard.AgainstNullOrEmpty(stringValue, nameof(stringValue));
Guard.AgainstNullOrWhiteSpace(stringValue, nameof(stringValue));

// Range checks
Guard.AgainstOutOfRange(value, 0, 100, nameof(value));
Guard.AgainstLessThan(value, 0, nameof(value));
Guard.AgainstGreaterThan(value, 100, nameof(value));

// Collection checks
Guard.AgainstNullOrEmpty(collection, nameof(collection));

// Condition checks
Guard.Against(condition, "Invalid condition", nameof(parameter));
Guard.Require(condition, "Required condition not met");
```

### JsonHelper

Helper for JSON serialization/deserialization.

**Location:** `KChief.Platform.Core/Utilities/JsonHelper.cs`

**Usage Examples:**

```csharp
// Serialize
var json = JsonHelper.Serialize(obj);
var jsonIndented = JsonHelper.Serialize(obj, indented: true);

// Deserialize
var obj = JsonHelper.Deserialize<MyType>(json);
var objOrThrow = JsonHelper.DeserializeOrThrow<MyType>(json);

// Try deserialize
if (JsonHelper.TryDeserialize<MyType>(json, out var result))
{
    // Use result
}

// Clone
var clone = JsonHelper.Clone(original);
```

### IdGenerator

Utility for generating unique identifiers.

**Location:** `KChief.Platform.Core/Utilities/IdGenerator.cs`

**Usage Examples:**

```csharp
// Basic IDs
var guid = IdGenerator.GenerateGuid();
var shortGuid = IdGenerator.GenerateShortGuid();
var sequential = IdGenerator.GenerateSequentialId("PREFIX");

// Domain-specific IDs
var vesselId = IdGenerator.GenerateVesselId(); // "vessel-abc123def456"
var engineId = IdGenerator.GenerateEngineId(); // "engine-abc123def456"
var sensorId = IdGenerator.GenerateSensorId(); // "sensor-abc123def456"
var alarmId = IdGenerator.GenerateAlarmId(); // "alarm-abc123def456"
var correlationId = IdGenerator.GenerateCorrelationId(); // "abc123def456"
```

### HttpClientHelper

Helper for HTTP client operations.

**Location:** `KChief.Platform.Core/Utilities/HttpClientHelper.cs`

**Usage Examples:**

```csharp
// Create clients
var client = HttpClientHelper.CreateClient("https://api.example.com");
var authClient = HttpClientHelper.CreateAuthenticatedClient(token);
var apiKeyClient = HttpClientHelper.CreateApiKeyClient(apiKey);

// GET request
var data = await HttpClientHelper.GetAsync<MyType>(client, "/endpoint");

// POST request
var response = await HttpClientHelper.PostAsync<Request, Response>(
    client, "/endpoint", request);

// PUT request
var updated = await HttpClientHelper.PutAsync<Request, Response>(
    client, "/endpoint", request);

// DELETE request
await HttpClientHelper.DeleteAsync(client, "/endpoint");
```

### ValidationHelper

Helper for validation operations.

**Location:** `KChief.Platform.Core/Utilities/ValidationHelper.cs`

**Usage Examples:**

```csharp
// Email validation
if (ValidationHelper.IsValidEmail(email)) { /* ... */ }

// URL validation
if (ValidationHelper.IsValidUrl(url)) { /* ... */ }

// GUID validation
if (ValidationHelper.IsValidGuid(guid)) { /* ... */ }

// Domain-specific validation
if (ValidationHelper.IsValidVesselId(vesselId)) { /* ... */ }
if (ValidationHelper.IsValidEngineId(engineId)) { /* ... */ }

// Object validation
var result = ValidationHelper.ValidateObject(obj);
if (!result.IsValid)
{
    // Handle errors
}
```

### ReflectionHelper

Helper for reflection operations.

**Location:** `KChief.Platform.Core/Utilities/ReflectionHelper.cs`

**Usage Examples:**

```csharp
// Get types implementing interface
var types = ReflectionHelper.GetTypesImplementing<IService>(assembly);

// Get types inheriting from base class
var types = ReflectionHelper.GetTypesInheritingFrom<BaseService>(assembly);

// Property operations
var value = ReflectionHelper.GetPropertyValue(obj, "PropertyName");
ReflectionHelper.SetPropertyValue(obj, "PropertyName", value);

// Attribute operations
var properties = ReflectionHelper.GetPropertiesWithAttribute<RequiredAttribute>(type);
var hasAttribute = ReflectionHelper.HasAttribute<SerializableAttribute>(type);

// Create instance
var instance = ReflectionHelper.CreateInstance<MyType>(arg1, arg2);
```

## Best Practices

### 1. Use Base Classes

Always inherit from base classes when creating services, repositories, or middleware:

```csharp
public class MyService : BaseService
{
    public MyService(ILogger<MyService> logger) : base(logger) { }
}
```

### 2. Use Extension Methods

Leverage extension methods for common operations:

```csharp
// Instead of
if (string.IsNullOrWhiteSpace(value)) { }

// Use
if (value.IsNullOrWhiteSpace()) { }
```

### 3. Use Guard Clauses

Validate parameters at method entry:

```csharp
public void Process(string value)
{
    Guard.AgainstNullOrWhiteSpace(value, nameof(value));
    // ... rest of method
}
```

### 4. Use Utility Classes

Use utility classes for common operations:

```csharp
// Instead of manual JSON serialization
var json = JsonSerializer.Serialize(obj, options);

// Use helper
var json = JsonHelper.Serialize(obj);
```

### 5. Consistent Error Handling

Use base service methods for consistent error handling:

```csharp
return await ExecuteWithLoggingAsync(
    "OperationName",
    async () => await DoWorkAsync(),
    defaultValue);
```

## Integration Examples

### Using BaseValidator

```csharp
using KChief.Platform.Core.Validation;

public class CreateVesselValidator : BaseValidator<CreateVesselRequest>
{
    public CreateVesselValidator()
    {
        RuleFor(x => x.Name)
            .ValidateRequiredString("Name")
            .ValidateStringLength("Name", 3, 100);
    }
}
```

### Using Extension Methods

```csharp
using KChief.Platform.Core.Extensions;

var relativeTime = alarm.TriggeredAt.ToRelativeTimeString();
var maskedPassword = password.Mask(4);
var batches = alarms.Batch(100);
```

### Using Guard Clauses

```csharp
using KChief.Platform.Core.Utilities;

public void Process(string id, int value)
{
    Guard.AgainstNullOrWhiteSpace(id, nameof(id));
    Guard.AgainstOutOfRange(value, 0, 100, nameof(value));
    // ... process
}
```

### Using BaseService

```csharp
using KChief.Platform.Core.Services;

public class VesselService : BaseService
{
    public VesselService(ILogger<VesselService> logger) : base(logger) { }

    public async Task<Vessel> GetVesselAsync(string id)
    {
        return await ExecuteWithLoggingAsync(
            "GetVessel",
            async () => await FetchVesselAsync(id),
            defaultValue: null!);
    }
}
```

## Component Reusability

### Cross-Project Usage

All components in `KChief.Platform.Core` are designed to be reusable across projects:

- **Validation Framework**: Use in any project requiring validation
- **Extension Methods**: Available throughout the solution
- **Base Classes**: Inherit in any service/repository/middleware
- **Utilities**: Use in any project for common operations

### Benefits

1. **Consistency**: Common patterns across the application
2. **Maintainability**: Changes in one place affect all usages
3. **Productivity**: Less code to write, more focus on business logic
4. **Quality**: Tested, proven patterns
5. **Documentation**: Well-documented, discoverable components

## Related Documentation

- [Validation Documentation](VALIDATION.md)
- [Middleware Documentation](MIDDLEWARE.md)
- [Architecture Documentation](ARCHITECTURE.md)
- [Developer Guide](DEVELOPER_GUIDE.md)

