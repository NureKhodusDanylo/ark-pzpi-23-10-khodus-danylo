# RobDeliveryAPI - Docker Deployment Guide

This guide explains how to run RobDeliveryAPI using Docker and Docker Compose.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose v3.8 or higher

### Install Docker

**Windows/Mac:**
- Download and install [Docker Desktop](https://www.docker.com/products/docker-desktop/)

**Linux (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install docker.io docker-compose
sudo systemctl start docker
sudo systemctl enable docker
```

## Quick Start

### 1. Build and Run with Docker Compose

```bash
# Build and start the container
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the container
docker-compose down
```

The API will be available at: `http://localhost:5102`

### 2. Build and Run with Docker (without compose)

```bash
# Build the Docker image
docker build -t robdeliveryapi:latest .

# Run the container
docker run -d \
  --name robdelivery-api \
  -p 5102:8080 \
  -v robdelivery-db:/app/Infrastructure/DB_Storage \
  -v robdelivery-uploads:/app/Uploads \
  -v robdelivery-backups:/app/Backups \
  robdeliveryapi:latest

# View logs
docker logs -f robdelivery-api

# Stop and remove container
docker stop robdelivery-api
docker rm robdelivery-api
```

## Configuration

### Environment Variables

Create a `.env` file in the project root (copy from `.env.example`):

```bash
cp .env.example .env
```

Edit `.env` to configure your settings:

```env
JWT_SECRET=YourSecretKeyHere
JWT_ISSUER=RobDeliveryAPI
JWT_AUDIENCE=RobDeliveryAPI
JWT_EXPIRATION_HOURS=24
```

**IMPORTANT:** Change `JWT_SECRET` to a strong random string in production!

### Ports

- **5102** - HTTP API endpoint
- **7102** - HTTPS endpoint (if configured)

Change ports in `docker-compose.yml` if needed:
```yaml
ports:
  - "8080:8080"  # Custom port mapping
```

## Data Persistence

Docker volumes are used to persist data:

- **robdelivery-db** - SQLite database (`Infrastructure/DB_Storage/RobDelivery.db`)
- **robdelivery-uploads** - User uploaded files (profile photos, order images)
- **robdelivery-backups** - Database and history backups

### View Volumes

```bash
# List volumes
docker volume ls

# Inspect volume
docker volume inspect robdelivery-db

# Remove volumes (WARNING: deletes all data!)
docker-compose down -v
```

### Backup Data

```bash
# Backup database volume
docker run --rm -v robdelivery-db:/data -v $(pwd):/backup alpine tar czf /backup/robdelivery-db-backup.tar.gz /data

# Restore database volume
docker run --rm -v robdelivery-db:/data -v $(pwd):/backup alpine sh -c "cd /data && tar xzf /backup/robdelivery-db-backup.tar.gz --strip 1"
```

## Health Checks

The container includes a health check that runs every 30 seconds:

```bash
# Check container health status
docker ps

# View health check logs
docker inspect --format='{{json .State.Health}}' robdelivery-api
```

## Development vs Production

### Development Mode

For development with hot reload, mount source code:

```yaml
# docker-compose.dev.yml
services:
  robdeliveryapi:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./RobDeliveryAPI:/app/RobDeliveryAPI
      - ./Application:/app/Application
      - ./Infrastructure:/app/Infrastructure
      - ./Entities:/app/Entities
```

Run with:
```bash
docker-compose -f docker-compose.dev.yml up
```

### Production Mode

Use the default `docker-compose.yml` for production deployment.

**Production checklist:**
- [ ] Change `JWT_SECRET` to a strong random value
- [ ] Configure proper logging
- [ ] Set up HTTPS with certificates
- [ ] Configure external database (optional)
- [ ] Set up reverse proxy (nginx/traefik)
- [ ] Configure monitoring and alerts

## Networking

### Access from Other Containers

Containers in the same network can access the API:

```yaml
services:
  my-app:
    image: my-app:latest
    networks:
      - robdelivery-network
    environment:
      - API_URL=http://robdeliveryapi:8080
```

### Reverse Proxy Setup (nginx)

```nginx
server {
    listen 80;
    server_name api.example.com;

    location / {
        proxy_pass http://localhost:5102;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker-compose logs robdeliveryapi

# Check if port is already in use
netstat -ano | findstr :5102  # Windows
lsof -i :5102                 # Linux/Mac
```

### Database Issues

```bash
# Remove database volume and recreate
docker-compose down
docker volume rm robdelivery-db
docker-compose up -d
```

### Permission Issues (Linux)

```bash
# Fix volume permissions
docker-compose exec robdeliveryapi chown -R app:app /app/Infrastructure/DB_Storage
```

### View Container Shell

```bash
# Access container shell
docker-compose exec robdeliveryapi /bin/bash

# Or for running container
docker exec -it robdelivery-api /bin/bash
```

## Useful Commands

```bash
# View running containers
docker-compose ps

# Restart container
docker-compose restart

# Rebuild and restart
docker-compose up -d --build

# View resource usage
docker stats robdelivery-api

# Remove all unused Docker resources
docker system prune -a

# Export logs to file
docker-compose logs > logs.txt
```

## Multi-Stage Build Optimization

The Dockerfile uses multi-stage builds to minimize image size:

1. **Build stage** - Compiles the application with .NET SDK
2. **Publish stage** - Creates optimized publish output
3. **Runtime stage** - Runs with lightweight ASP.NET Core runtime

Final image size: ~220MB (compared to ~2GB with SDK)

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build and Push Docker Image

on:
  push:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Build Docker image
        run: docker build -t robdeliveryapi:${{ github.sha }} .

      - name: Push to registry
        run: |
          echo ${{ secrets.DOCKER_PASSWORD }} | docker login -u ${{ secrets.DOCKER_USERNAME }} --password-stdin
          docker push robdeliveryapi:${{ github.sha }}
```

## Security Considerations

1. **Never commit `.env` file** - It contains secrets
2. **Use strong JWT secrets** - At least 32 characters
3. **Run container as non-root user** (can be added to Dockerfile)
4. **Keep base images updated** - Rebuild regularly
5. **Scan images for vulnerabilities:**
   ```bash
   docker scan robdeliveryapi:latest
   ```

## Support

For issues related to Docker deployment, check:
- Docker logs: `docker-compose logs`
- Application logs inside container
- GitHub Issues: https://github.com/NureKhodusDanylo/ark-pzpi-23-10-khodus-danylo

## License

See main project README for license information.
