# Contributing to K-Chief Marine Automation Platform

Thank you for your interest in contributing to the K-Chief Marine Automation Platform. This document provides guidelines and instructions for contributing to the project.

## Code of Conduct

This project adheres to a code of conduct that all contributors are expected to follow. Please be respectful and professional in all interactions.

## Getting Started

1. Fork the repository
2. Clone your fork locally
3. Create a new branch for your feature or bug fix
4. Make your changes
5. Write or update tests as needed
6. Ensure all tests pass
7. Submit a pull request

## Development Setup

### Prerequisites

- .NET 8 SDK or later
- Git
- A code editor (Visual Studio, VS Code, or Rider)

### Building the Project

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test
```

## Coding Standards

### C# Style Guidelines

- Follow Microsoft's C# coding conventions
- Use meaningful variable and method names
- Keep methods focused and single-purpose
- Use async/await for I/O operations
- Implement proper error handling

### Code Organization

- Follow SOLID principles
- Use dependency injection
- Implement repository pattern for data access
- Separate concerns into appropriate layers

### Documentation

- Add XML documentation comments for public APIs
- Update README.md if adding new features
- Document complex algorithms or business logic

## Commit Guidelines

### Commit Message Format

Use clear, descriptive commit messages:

```
Short summary (50 characters or less)

More detailed explanation if needed. Wrap at 72 characters.
Explain what and why, not how.
```

### Examples

```
Add OPC UA client implementation

Implemented OPC UA client with support for reading and writing
node values. Includes connection management and error handling.

Fixes #123
```

```
Fix alarm system memory leak

Resolved issue where alarm subscriptions were not properly
disposed, causing memory leaks in long-running processes.

Closes #456
```

## Pull Request Process

1. Update the README.md with details of changes if applicable
2. Ensure all tests pass
3. Update documentation as needed
4. Request review from maintainers
5. Address any feedback or requested changes

### Pull Request Checklist

- [ ] Code follows the project's style guidelines
- [ ] Self-review has been performed
- [ ] Comments have been added for complex code
- [ ] Documentation has been updated
- [ ] Tests have been added or updated
- [ ] All tests pass locally
- [ ] No new warnings are introduced

## Testing Requirements

- Write unit tests for new features
- Maintain or improve code coverage
- Include integration tests for API endpoints
- Test error scenarios and edge cases

## Reporting Issues

When reporting issues, please include:

- Description of the issue
- Steps to reproduce
- Expected behavior
- Actual behavior
- Environment details (.NET version, OS, etc.)
- Relevant logs or error messages

## Feature Requests

Feature requests are welcome. Please provide:

- Clear description of the feature
- Use case or problem it solves
- Proposed implementation approach (if applicable)

## Questions

If you have questions, please open an issue with the `question` label.

Thank you for contributing to the K-Chief Marine Automation Platform!

