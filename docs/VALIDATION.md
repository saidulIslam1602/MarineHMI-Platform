# Data Validation with FluentValidation

## Overview

The K-Chief Marine Automation Platform uses FluentValidation for comprehensive, type-safe data validation. This provides a clean separation of validation logic from models and controllers, with support for custom validators, localization, and middleware integration.

## Architecture

### Validation Pipeline

```
Request
  ↓
ValidationMiddleware (JSON structure validation)
  ↓
RequestValidationMiddleware (HTTP-level validation)
  ↓
FluentValidationFilter (Model validation)
  ↓
Controller Action
```

## FluentValidation Integration

### Basic Setup

FluentValidation is integrated into the application through:

1. **Automatic Registration**: Validators are automatically discovered and registered
2. **Auto Validation**: Validation runs automatically before controller actions
3. **Client-Side Adapters**: Validation rules are available for client-side validation

### Configuration

```csharp
// In Program.cs
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
```

## Validators

### Request Model Validators

#### LoginRequestValidator

Validates user login requests:

```csharp
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(255)
            .Matches(@"^[a-zA-Z0-9_\-\.]+$");
            
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(255);
    }
}
```

**Validation Rules:**
- Username: Required, 3-255 characters, alphanumeric with underscores/hyphens/dots
- Password: Required, 6-255 characters

#### ChangePasswordRequestValidator

Validates password change requests:

```csharp
public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Z]")  // Uppercase
            .Matches(@"[a-z]")  // Lowercase
            .Matches(@"[0-9]")  // Digit
            .Matches(@"[^a-zA-Z0-9]")  // Special character
            .NotEqual(x => x.CurrentPassword);
    }
}
```

**Validation Rules:**
- Current Password: Required, minimum 6 characters
- New Password: Required, minimum 8 characters, must contain uppercase, lowercase, digit, and special character, must be different from current password

#### SetRpmRequestValidator

Validates engine RPM setting requests:

```csharp
public class SetRpmRequestValidator : AbstractValidator<SetRpmRequest>
{
    public SetRpmRequestValidator()
    {
        RuleFor(x => x.Rpm)
            .GreaterThanOrEqualTo(0)
            .LessThanOrEqualTo(10000);
    }
}
```

**Validation Rules:**
- RPM: Must be between 0 and 10000

### Custom Validators

#### Maritime Domain Validators

Custom validators for maritime-specific validation rules:

**VesselIdValidator**
- Validates vessel ID format: `vessel-XXX` where XXX is a 3-digit number
- Example: `vessel-001`, `vessel-123`

**EngineIdValidator**
- Validates engine ID format: `engine-XXX` where XXX is a 3-digit number
- Example: `engine-001`, `engine-456`

**MaritimeEmailValidator**
- Validates email addresses from allowed maritime domains
- Default domains: `kchief.com`, `maritime.com`
- Customizable per validator instance

**MaritimeCoordinateValidator**
- Validates latitude: -90 to 90 degrees
- Validates longitude: -180 to 180 degrees

**VesselNameValidator**
- Validates vessel names with maritime prefixes
- Prefixes: MS, MV, SS, M/V, S/S, MT, RV, USNS, USS
- Length: 3-100 characters

#### Usage Example

```csharp
public class VesselRequestValidator : AbstractValidator<VesselRequest>
{
    public VesselRequestValidator()
    {
        RuleFor(x => x.VesselId)
            .VesselId();  // Custom validator extension
            
        RuleFor(x => x.Name)
            .VesselName();  // Custom validator extension
            
        RuleFor(x => x.Email)
            .MaritimeEmail("kchief.com", "maritime.com");  // Custom validator with parameters
            
        RuleFor(x => x.Latitude)
            .MaritimeLatitude();  // Custom validator extension
            
        RuleFor(x => x.Longitude)
            .MaritimeLongitude();  // Custom validator extension
    }
}
```

## Validation Middleware

### ValidationMiddleware

Early validation middleware that validates JSON structure before models reach controllers:

**Features:**
- Validates JSON format
- Validates JSON structure (object/array)
- Returns standardized error responses
- Early rejection of malformed requests

**Error Response:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid JSON format: ...",
  "instance": "/api/vessels",
  "correlationId": "abc123def456",
  "timestamp": "2025-01-15T10:30:00Z"
}
```

### FluentValidationFilter

Action filter that integrates FluentValidation with ASP.NET Core:

**Features:**
- Automatic validation of action arguments
- Integration with ModelState
- Standardized error responses
- Correlation ID tracking

**Validation Flow:**
1. Action arguments are extracted
2. Validators are resolved from DI container
3. Validation is performed
4. Errors are added to ModelState
5. Standardized error response is returned if validation fails

## Localization

### Setup

Localization is configured for validation error messages:

```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
```

### Localized Validators

Validators can use localized error messages:

```csharp
public class LocalizedLoginRequestValidator : LocalizedValidatorBase<LoginRequest>
{
    public LocalizedLoginRequestValidator(IStringLocalizer localizer)
        : base(localizer)
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .WithMessage(GetLocalizedMessage("LOGIN_USERNAME_REQUIRED", "Username is required"));
    }
}
```

### Resource Files

Create resource files in `Resources/` directory:

**Resources/Validation.en-US.resx**
```xml
<data name="Validation.LOGIN_USERNAME_REQUIRED" xml:space="preserve">
  <value>Username is required</value>
</data>
```

**Resources/Validation.es-ES.resx**
```xml
<data name="Validation.LOGIN_USERNAME_REQUIRED" xml:space="preserve">
  <value>El nombre de usuario es obligatorio</value>
</data>
```

## Error Codes

All validators use error codes for consistent error identification:

### Authentication Errors
- `LOGIN_USERNAME_REQUIRED` - Username is required
- `LOGIN_USERNAME_MIN_LENGTH` - Username too short
- `LOGIN_USERNAME_MAX_LENGTH` - Username too long
- `LOGIN_USERNAME_INVALID_FORMAT` - Invalid username format
- `LOGIN_PASSWORD_REQUIRED` - Password is required
- `LOGIN_PASSWORD_MIN_LENGTH` - Password too short
- `LOGIN_PASSWORD_MAX_LENGTH` - Password too long

### Password Change Errors
- `CHANGE_PASSWORD_CURRENT_REQUIRED` - Current password required
- `CHANGE_PASSWORD_CURRENT_MIN_LENGTH` - Current password too short
- `CHANGE_PASSWORD_NEW_REQUIRED` - New password required
- `CHANGE_PASSWORD_NEW_MIN_LENGTH` - New password too short
- `CHANGE_PASSWORD_NEW_MAX_LENGTH` - New password too long
- `CHANGE_PASSWORD_NEW_UPPERCASE` - New password missing uppercase
- `CHANGE_PASSWORD_NEW_LOWERCASE` - New password missing lowercase
- `CHANGE_PASSWORD_NEW_DIGIT` - New password missing digit
- `CHANGE_PASSWORD_NEW_SPECIAL` - New password missing special character
- `CHANGE_PASSWORD_NEW_DIFFERENT` - New password same as current

### Engine Control Errors
- `SET_RPM_MIN_VALUE` - RPM below minimum
- `SET_RPM_MAX_VALUE` - RPM above maximum
- `SET_RPM_EXCEEDS_MAX` - RPM exceeds engine maximum
- `SET_RPM_ENGINE_NOT_FOUND` - Engine not found
- `SET_RPM_ENGINE_NOT_RUNNING` - Engine not running

## Validation Response Format

### Success Response

Valid requests proceed normally to controller actions.

### Validation Error Response

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/auth/login",
  "correlationId": "abc123def456",
  "timestamp": "2025-01-15T10:30:00Z",
  "errors": {
    "Username": [
      "Username is required",
      "Username must be at least 3 characters long"
    ],
    "Password": [
      "Password is required"
    ]
  }
}
```

## Best Practices

### 1. Validator Organization
- One validator per request model
- Group related validators in the same namespace
- Use descriptive validator names

### 2. Validation Rules
- Keep rules focused and specific
- Use error codes for consistent error identification
- Provide clear, actionable error messages

### 3. Custom Validators
- Create reusable custom validators for domain-specific rules
- Use extension methods for fluent API
- Document validator behavior

### 4. Performance
- Validators are registered as singletons (stateless)
- Validation runs early in the pipeline
- Failed validations short-circuit request processing

### 5. Testing
- Test validators independently
- Test validation error responses
- Test custom validators with edge cases

## Usage Examples

### Creating a New Validator

```csharp
public class CreateVesselRequestValidator : AbstractValidator<CreateVesselRequest>
{
    public CreateVesselRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Vessel name is required")
            .WithErrorCode("VESSEL_NAME_REQUIRED")
            .VesselName()
            .WithErrorCode("VESSEL_NAME_INVALID_FORMAT");
            
        RuleFor(x => x.VesselId)
            .NotEmpty()
            .VesselId();
            
        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("Vessel length must be greater than 0")
            .WithErrorCode("VESSEL_LENGTH_INVALID");
    }
}
```

### Using Custom Validators

```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress()
    .MaritimeEmail("kchief.com", "maritime.com");
```

### Conditional Validation

```csharp
RuleFor(x => x.NewPassword)
    .NotEmpty()
    .When(x => !string.IsNullOrEmpty(x.CurrentPassword))
    .WithMessage("New password is required when changing password");
```

### Cross-Property Validation

```csharp
RuleFor(x => x.ConfirmPassword)
    .Equal(x => x.NewPassword)
    .WithMessage("Passwords do not match")
    .WithErrorCode("PASSWORDS_MISMATCH");
```

## Integration with Existing Code

### ModelValidationFilter

The existing `ModelValidationFilter` has been replaced with `FluentValidationFilter` which:
- Integrates FluentValidation with ASP.NET Core
- Maintains the same error response format
- Adds FluentValidation-specific features

### Backward Compatibility

- DataAnnotations on models still work
- FluentValidation runs in addition to DataAnnotations
- Both validation systems can coexist

## Troubleshooting

### Validator Not Running

**Problem:** Validator not being executed

**Solutions:**
- Ensure validator is registered: `AddValidatorsFromAssemblyContaining<Program>()`
- Check validator naming convention: `{ModelName}Validator`
- Verify validator is in the same assembly

### Validation Errors Not Returned

**Problem:** Validation errors not appearing in response

**Solutions:**
- Ensure `FluentValidationFilter` is registered
- Check ModelState is being populated
- Verify error response format

### Custom Validator Not Working

**Problem:** Custom validator not executing

**Solutions:**
- Verify validator extends `PropertyValidator<T, TProperty>`
- Check `IsValid` method implementation
- Ensure validator is registered correctly

## Related Documentation

- [Middleware Documentation](MIDDLEWARE.md)
- [Error Handling](ERROR_HANDLING.md)
- [API Documentation](API.md)

