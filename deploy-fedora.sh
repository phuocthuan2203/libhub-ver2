#!/bin/bash

set -e

echo "====================================="
echo "LibHub Fedora Server Deployment"
echo "====================================="
echo ""

if [ "$EUID" -ne 0 ]; then 
    echo "Please run with sudo: sudo ./deploy-fedora.sh"
    exit 1
fi

echo "[1/6] Installing Docker and Docker Compose..."
if ! command -v docker &> /dev/null; then
    dnf -y install dnf-plugins-core
    dnf config-manager --add-repo https://download.docker.com/linux/fedora/docker-ce.repo
    dnf install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
    systemctl start docker
    systemctl enable docker
    echo "✓ Docker installed successfully"
else
    echo "✓ Docker already installed"
fi

echo ""
echo "[2/6] Configuring firewall..."
if command -v firewall-cmd &> /dev/null; then
    firewall-cmd --permanent --add-port=8080/tcp
    firewall-cmd --permanent --add-port=5000/tcp
    firewall-cmd --permanent --add-port=8500/tcp
    firewall-cmd --reload
    echo "✓ Firewall configured (ports 8080, 5000, 8500 opened)"
else
    echo "⚠ firewalld not found, skipping firewall configuration"
fi

echo ""
echo "[3/6] Getting server IP address..."
SERVER_IP=$(hostname -I | awk '{print $1}')
echo "✓ Server IP: $SERVER_IP"

echo ""
echo "[4/6] Stopping existing containers..."
docker compose down 2>/dev/null || true

echo ""
echo "[5/6] Building and starting containers..."
docker compose up -d --build

echo ""
echo "[6/6] Waiting for services to be ready..."
sleep 30

echo ""
echo "====================================="
echo "✓ Deployment Complete!"
echo "====================================="
echo ""
echo "Access the application:"
echo "  - Frontend:    http://$SERVER_IP:8080"
echo "  - API Gateway: http://$SERVER_IP:5000"
echo "  - Consul UI:   http://$SERVER_IP:8500"
echo ""
echo "From other machines on your network, use:"
echo "  http://$SERVER_IP:8080"
echo ""
echo "To view logs:"
echo "  docker compose logs -f"
echo ""
echo "To stop services:"
echo "  docker compose down"
echo ""
