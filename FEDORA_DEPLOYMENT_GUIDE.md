# LibHub - Fedora Server Deployment Guide

This guide provides step-by-step instructions for deploying the LibHub project to your Fedora Dell server.

## Prerequisites

- Fedora server with sudo access
- Git installed
- Internet connection
- SSH access to the server

## Deployment Steps

### Step 1: Connect to Your Fedora Server

```bash
ssh your-username@your-server-ip
```

### Step 2: Remove Old Project (If Exists)

If you have an old version of LibHub running, stop and remove it:

```bash
cd /path/to/old/LibHub

sudo docker compose down -v

cd ..
rm -rf LibHub
```

**Note**: The `-v` flag removes volumes including the database. If you want to keep the old data, omit this flag.

### Step 3: Clone the New Project

Clone the latest version from your GitHub repository:

```bash
cd ~
git clone https://github.com/YOUR_USERNAME/LibHub.git
cd LibHub
```

Or if you're deploying from your main machine, you can specify the branch:

```bash
git clone -b main https://github.com/YOUR_USERNAME/LibHub.git
cd LibHub
```

### Step 4: Make the Deployment Script Executable

```bash
chmod +x deploy-fedora.sh
```

### Step 5: Run the Deployment Script

Execute the deployment script with sudo:

```bash
sudo ./deploy-fedora.sh
```

The script will:
1. Install Docker and Docker Compose (if not already installed)
2. Configure firewall rules (opens ports 8080, 5000, 8500)
3. Detect your server IP address
4. Stop any existing containers
5. Ask if you want to remove old volumes (choose 'y' for fresh start)
6. Build and start all containers
7. Wait for services to initialize
8. Display access URLs and helpful commands

### Step 6: Verify Deployment

After the script completes, verify all services are running:

```bash
docker compose ps
```

You should see all services in "Up" state with healthy status.

Check the logs if needed:

```bash
docker compose logs -f
```

Press `Ctrl+C` to exit log viewing.

### Step 7: Apply Seed Data (Optional)

After all services are running, apply the seed data to populate the database with sample books and users:

```bash
./scripts/apply-seed-data.sh
```

Or manually:

```bash
docker exec -i libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 < scripts/seed-data.sql
```

This will add:
- 7 sample books (each with 100 copies)
- Admin and test user accounts

### Step 8: Access the Application

The deployment script will display your server IP. Access the application from any device on your network:

- **Frontend**: `http://YOUR_SERVER_IP:8080`
- **API Gateway**: `http://YOUR_SERVER_IP:5000`
- **Consul UI**: `http://YOUR_SERVER_IP:8500`

Replace `YOUR_SERVER_IP` with the IP address shown by the deployment script.

## Post-Deployment Configuration

### Firewall Configuration

The script automatically configures firewalld. If you need to manually add ports:

```bash
sudo firewall-cmd --permanent --add-port=8080/tcp
sudo firewall-cmd --permanent --add-port=5000/tcp
sudo firewall-cmd --permanent --add-port=8500/tcp
sudo firewall-cmd --reload
```

### SELinux Considerations

If you encounter permission issues with Docker volumes, you may need to adjust SELinux:

```bash
sudo setsebool -P container_manage_cgroup on
```

Or temporarily disable SELinux (not recommended for production):

```bash
sudo setenforce 0
```

## Managing the Deployment

### View Logs

View logs for all services:

```bash
docker compose logs -f
```

View logs for a specific service:

```bash
docker compose logs -f [service-name]
```

Service names: `consul`, `mysql`, `userservice`, `catalogservice`, `loanservice`, `gateway`, `frontend`

### Restart Services

Restart all services:

```bash
docker compose restart
```

Restart a specific service:

```bash
docker compose restart [service-name]
```

### Stop Services

Stop all services without removing containers:

```bash
docker compose stop
```

Stop and remove all containers:

```bash
docker compose down
```

Stop and remove all containers and volumes (deletes data):

```bash
docker compose down -v
```

### Update Deployment

To update with new code changes:

```bash
cd ~/LibHub

git pull origin main

sudo docker compose down

sudo docker compose up -d --build
```

### View Container Status

```bash
docker compose ps
```

### Check Resource Usage

```bash
docker stats
```

## Troubleshooting

### Services Not Starting

1. Check Docker service status:
   ```bash
   sudo systemctl status docker
   ```

2. Restart Docker if needed:
   ```bash
   sudo systemctl restart docker
   ```

3. Check logs for errors:
   ```bash
   docker compose logs
   ```

### Port Already in Use

If ports are already in use, find and stop the conflicting process:

```bash
sudo netstat -tulpn | grep :8080
sudo netstat -tulpn | grep :5000
sudo netstat -tulpn | grep :8500
```

Kill the process using the port:

```bash
sudo kill -9 [PID]
```

### Cannot Access from Other Machines

1. Verify firewall rules:
   ```bash
   sudo firewall-cmd --list-all
   ```

2. Check if services are bound to 0.0.0.0:
   ```bash
   docker compose ps
   netstat -tulpn | grep docker-proxy
   ```

3. Ensure your router allows traffic on these ports

### MySQL Connection Issues

If services can't connect to MySQL:

1. Check MySQL container is healthy:
   ```bash
   docker compose ps mysql
   ```

2. View MySQL logs:
   ```bash
   docker compose logs mysql
   ```

3. Wait longer - MySQL takes time to initialize on first run

4. Restart MySQL container:
   ```bash
   docker compose restart mysql
   ```

### Out of Disk Space

Check disk usage:

```bash
df -h
```

Clean up Docker resources:

```bash
sudo docker system prune -a
sudo docker volume prune
```

### Memory Issues

Check available memory:

```bash
free -h
```

If low on memory, consider:
- Stopping other services
- Adding swap space
- Upgrading server RAM

## Database Management

### Access MySQL Container

```bash
docker exec -it libhub-mysql mysql -u libhub_user -p
```

Password: `LibHub@Dev2025`

### Backup Database

```bash
docker exec libhub-mysql mysqldump -u root -pLibHub@2025 --all-databases > backup.sql
```

### Restore Database

```bash
docker exec -i libhub-mysql mysql -u root -pLibHub@2025 < backup.sql
```

## Security Recommendations

1. **Change Default Passwords**: Update MySQL passwords in `docker-compose.yml` and environment variables

2. **Use HTTPS**: Set up a reverse proxy (nginx/traefik) with SSL certificates

3. **Firewall**: Only open necessary ports and restrict access by IP if possible

4. **Regular Updates**: Keep Docker and system packages updated
   ```bash
   sudo dnf update
   ```

5. **Monitor Logs**: Regularly check logs for suspicious activity

6. **Backup**: Set up automated backups of the database

## Performance Optimization

### Increase Docker Resources

Edit Docker daemon configuration:

```bash
sudo nano /etc/docker/daemon.json
```

Add:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
```

Restart Docker:

```bash
sudo systemctl restart docker
```

### Monitor Performance

Install and use monitoring tools:

```bash
sudo dnf install htop
htop
```

## Automated Deployment Script

For future deployments, you can create an automated script:

```bash
nano ~/redeploy-libhub.sh
```

Add:

```bash
#!/bin/bash
cd ~/LibHub
git pull origin main
sudo docker compose down
sudo docker compose up -d --build
echo "Deployment complete!"
```

Make it executable:

```bash
chmod +x ~/redeploy-libhub.sh
```

Run it:

```bash
./redeploy-libhub.sh
```

## Default Credentials

After deployment, you can log in with:

- **Admin Account**
  - Username: `admin`
  - Email: `admin@libhub.com`
  
- **Test User Account**
  - Username: `testuser`
  - Email: `test@libhub.com`

**Note**: Passwords are hashed in the seed data. You may need to implement a password reset feature or check the actual password in your seed data file.

## Support

If you encounter issues:

1. Check the logs: `docker compose logs -f`
2. Verify all containers are running: `docker compose ps`
3. Review this troubleshooting guide
4. Check Docker and Fedora documentation

## Summary

You have successfully deployed LibHub to your Fedora server! The application is now accessible from any device on your network. Remember to:

- Keep your system updated
- Monitor logs regularly
- Backup your database
- Secure your deployment with proper passwords and firewall rules

Enjoy your LibHub deployment!
