# K-Chief Marine Automation Platform

A comprehensive marine automation platform built with .NET 8, designed to provide scalable, high-quality solutions for vessel control systems, industrial protocol integration, and real-time data management.

## Overview

The K-Chief Marine Automation Platform is a modular, distributed system that simulates and manages vessel control operations. It provides a foundation for building integrated marine automation solutions with support for industrial communication protocols, alarm management, and real-time data processing.

## Architecture

The platform follows a modular architecture with clear separation of concerns:

- **KChief.Platform.Core**: Core domain models, interfaces, and shared components
- **KChief.Platform.API**: RESTful API layer providing HTTP endpoints for system integration
- **KChief.DataAccess**: Data persistence layer with repository pattern implementation
- **KChief.VesselControl**: Vessel control logic and engine monitoring
- **KChief.AlarmSystem**: Alarm and event management system
- **KChief.Protocols.OPC**: OPC UA protocol integration for industrial communication
- **KChief.Protocols.Modbus**: Modbus TCP/RTU protocol integration

## Technology Stack

- **.NET 8**: Latest LTS version of .NET
- **ASP.NET Core**: Web API framework
- **Entity Framework Core**: ORM for data access
- **xUnit**: Unit testing framework
- **Docker**: Containerization support

## Prerequisites

- .NET 8 SDK or later
- Docker Desktop (optional, for containerized deployment)
- Git

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/saidulIslam1602/K-Chief-Marine-Automation-Platform.git
cd K-Chief-Marine-Automation-Platform
```

### Build the Solution

```bash
dotnet restore
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Run the API

```bash
cd src/KChief.Platform.API
dotnet run
```

The API will be available at `https://localhost:5001` or `http://localhost:5000` (depending on your configuration).

## Project Structure

```
K-Chief-Marine-Automation-Platform/
├── src/
│   ├── KChief.Platform.Core/          # Core domain and shared components
│   ├── KChief.Platform.API/            # REST API layer
│   ├── KChief.DataAccess/             # Data access layer
│   ├── KChief.VesselControl/          # Vessel control logic
│   ├── KChief.AlarmSystem/            # Alarm management
│   ├── KChief.Protocols.OPC/          # OPC UA integration
│   └── KChief.Protocols.Modbus/       # Modbus integration
├── tests/
│   ├── KChief.Platform.Tests/         # Unit tests
│   └── KChief.Integration.Tests/      # Integration tests
├── docker/                             # Docker configuration files
├── docs/                               # Additional documentation
└── .github/
    └── workflows/                      # CI/CD pipeline definitions
```

## Features

### Current Implementation

- Modular platform architecture
- RESTful API endpoints
- Unit and integration testing framework
- Docker containerization support
- CI/CD pipeline configuration

### Planned Features

- OPC UA client/server implementation
- Modbus TCP/RTU protocol support
- Real-time data streaming with SignalR
- Comprehensive alarm and event management
- Historical data logging and retrieval
- Message bus integration for distributed communication
- Legacy code modernization examples

## Development Guidelines

### Code Standards

- Follow C# coding conventions and best practices
- Implement SOLID principles
- Write comprehensive unit tests
- Use async/await for I/O operations
- Implement proper error handling and logging

### Testing

- Maintain high test coverage (target: 80%+)
- Write unit tests for all business logic
- Include integration tests for API endpoints
- Use mocking frameworks for dependencies

### Git Workflow

- Create feature branches from `main`
- Use descriptive commit messages
- Submit pull requests for code review
- Ensure all tests pass before merging

## Contributing

Contributions are welcome. Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contact

For questions or inquiries, please open an issue in the repository.

## Acknowledgments

This project is designed to demonstrate modern software development practices in the marine automation domain, with a focus on platform architecture, system integration, and industrial protocol communication.

