# Docker Setup for K-Chief Marine Automation Platform

This directory contains Docker configuration files for containerizing the K-Chief Marine Automation Platform.

## Prerequisites

- Docker Desktop or Docker Engine installed
- Docker Compose installed

## Building the Docker Image

To build the Docker image:

```bash
docker build -f docker/Dockerfile -t kchief-api:latest .
```

## Running with Docker Compose

To run the application using Docker Compose:

```bash
cd docker
docker-compose up -d
```

The API will be available at:
- HTTP: http://localhost:8080
- HTTPS: https://localhost:8081

## Stopping the Container

To stop the container:

```bash
cd docker
docker-compose down
```

## Viewing Logs

To view container logs:

```bash
cd docker
docker-compose logs -f kchief-api
```

## Building and Running in One Command

```bash
cd docker
docker-compose up --build
```

## Environment Variables

You can customize the configuration by setting environment variables in the `docker-compose.yml` file or by creating a `.env` file.

## Notes

- The OPC UA certificate stores are mounted as a volume to persist certificates between container restarts
- The application runs in Development mode by default in the Docker container
- For production, set `ASPNETCORE_ENVIRONMENT=Production` in the docker-compose.yml file

