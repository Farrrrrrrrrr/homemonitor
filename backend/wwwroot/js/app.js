// IoT Motion Detection Dashboard JavaScript
class MotionDashboard {
    constructor() {
        this.ws = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 2000;
        this.alertCount = 0;
        this.logEntries = [];
        this.currentFilter = 'all';
        
        this.init();
    }

    init() {
        this.bindEvents();
        this.loadInitialData();
        this.connectWebSocket();
        this.startStatsRefresh();
        
        // Add initial log entry
        this.addLogEntry('info', 'Dashboard initialized. Ready to monitor motion events.');
    }

    bindEvents() {
        // Button event listeners
        document.getElementById('connectBtn').addEventListener('click', () => this.toggleConnection());
        document.getElementById('refreshBtn').addEventListener('click', () => this.refreshStats());
        document.getElementById('clearLogsBtn').addEventListener('click', () => this.clearLogs());
        document.getElementById('testMotionBtn').addEventListener('click', () => this.sendTestMotion());
        document.getElementById('logFilter').addEventListener('change', (e) => this.filterLogs(e.target.value));
    }

    async loadInitialData() {
        await this.refreshStats();
    }

    async refreshStats() {
        try {
            const response = await fetch('/api/dashboard/stats');
            if (response.ok) {
                const stats = await response.json();
                this.updateStats(stats);
                this.addLogEntry('success', 'Dashboard statistics refreshed successfully.');
            } else {
                throw new Error(`HTTP ${response.status}`);
            }
        } catch (error) {
            console.error('Failed to fetch stats:', error);
            this.addLogEntry('error', `Failed to fetch dashboard stats: ${error.message}`);
        }
    }

    updateStats(stats) {
        document.getElementById('totalEvents').textContent = stats.totalEvents || 0;
        document.getElementById('eventsLast24h').textContent = stats.eventsLast24h || 0;
        document.getElementById('eventsLastHour').textContent = stats.eventsLastHour || 0;
        document.getElementById('activeSensors').textContent = stats.activeSensors || 0;
        document.getElementById('connectedClients').textContent = stats.connectedClients || 0;
    }

    connectWebSocket() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const wsUrl = `${protocol}//${window.location.host}/ws`;
        
        try {
            this.ws = new WebSocket(wsUrl);
            
            this.ws.onopen = () => {
                this.isConnected = true;
                this.reconnectAttempts = 0;
                this.updateConnectionStatus(true);
                this.addLogEntry('success', 'WebSocket connected successfully.');
            };

            this.ws.onmessage = (event) => {
                try {
                    const data = JSON.parse(event.data);
                    this.handleWebSocketMessage(data);
                } catch (error) {
                    console.error('Error parsing WebSocket message:', error);
                    this.addLogEntry('error', `Error parsing WebSocket message: ${error.message}`);
                }
            };

            this.ws.onclose = () => {
                this.isConnected = false;
                this.updateConnectionStatus(false);
                this.addLogEntry('warning', 'WebSocket connection closed.');
                this.attemptReconnect();
            };

            this.ws.onerror = (error) => {
                console.error('WebSocket error:', error);
                this.addLogEntry('error', 'WebSocket connection error occurred.');
            };
        } catch (error) {
            console.error('Failed to create WebSocket:', error);
            this.addLogEntry('error', `Failed to create WebSocket connection: ${error.message}`);
        }
    }

    handleWebSocketMessage(data) {
        if (data.type === 'motion_alert') {
            this.handleMotionAlert(data.data);
        } else {
            this.addLogEntry('info', `Received WebSocket message: ${JSON.stringify(data)}`);
        }
    }

    handleMotionAlert(alertData) {
        this.alertCount++;
        this.updateAlertCount();
        this.addMotionAlert(alertData);
        this.addLogEntry('motion', `Motion detected! Sensor: ${alertData.sensorId}, Location: ${alertData.location}`);
        
        // Show browser notification if permitted
        this.showNotification(alertData);
    }

    addMotionAlert(alertData) {
        const alertsContainer = document.getElementById('motionAlerts');
        const noAlertsDiv = alertsContainer.querySelector('.no-alerts');
        
        if (noAlertsDiv) {
            noAlertsDiv.remove();
        }

        const alertDiv = document.createElement('div');
        alertDiv.className = 'motion-alert recent';
        alertDiv.innerHTML = `
            <div class="alert-header">
                <span class="alert-sensor">🚨 ${alertData.sensorId}</span>
                <span class="alert-time">${new Date(alertData.detectedAt).toLocaleString()}</span>
            </div>
            <div class="alert-location">📍 ${alertData.location}</div>
        `;

        alertsContainer.insertBefore(alertDiv, alertsContainer.firstChild);

        // Remove 'recent' class after animation
        setTimeout(() => {
            alertDiv.classList.remove('recent');
        }, 2000);

        // Limit to 10 most recent alerts
        const alerts = alertsContainer.querySelectorAll('.motion-alert');
        if (alerts.length > 10) {
            alerts[alerts.length - 1].remove();
        }
    }

    updateAlertCount() {
        document.getElementById('alertCount').textContent = this.alertCount;
    }

    updateConnectionStatus(connected) {
        const statusIndicator = document.getElementById('statusIndicator');
        const statusText = document.getElementById('statusText');
        const connectBtn = document.getElementById('connectBtn');

        if (connected) {
            statusIndicator.className = 'status-indicator online';
            statusText.textContent = 'Connected';
            connectBtn.textContent = 'Disconnect';
            connectBtn.classList.remove('btn-primary');
            connectBtn.classList.add('btn-warning');
        } else {
            statusIndicator.className = 'status-indicator offline';
            statusText.textContent = 'Disconnected';
            connectBtn.textContent = 'Connect to WebSocket';
            connectBtn.classList.remove('btn-warning');
            connectBtn.classList.add('btn-primary');
        }
    }

    attemptReconnect() {
        if (this.reconnectAttempts < this.maxReconnectAttempts) {
            this.reconnectAttempts++;
            const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
            
            this.addLogEntry('info', `Attempting to reconnect in ${delay/1000} seconds... (${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
            
            setTimeout(() => {
                this.connectWebSocket();
            }, delay);
        } else {
            this.addLogEntry('error', 'Maximum reconnection attempts reached. Please refresh the page or click Connect.');
        }
    }

    toggleConnection() {
        if (this.isConnected) {
            this.disconnect();
        } else {
            this.connectWebSocket();
        }
    }

    disconnect() {
        if (this.ws) {
            this.ws.close();
            this.ws = null;
        }
        this.isConnected = false;
        this.updateConnectionStatus(false);
        this.addLogEntry('info', 'Manually disconnected from WebSocket.');
    }

    async sendTestMotion() {
        try {
            const testData = {
                sensorId: `TEST_WEB_${Date.now()}`,
                eventType: 'motion_detected',
                location: 'Web Dashboard Test'
            };

            const response = await fetch('/api/motion-events', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(testData)
            });

            if (response.ok) {
                const result = await response.json();
                this.addLogEntry('success', `Test motion event sent successfully. Event ID: ${result.id}`);
            } else {
                throw new Error(`HTTP ${response.status}`);
            }
        } catch (error) {
            console.error('Failed to send test motion:', error);
            this.addLogEntry('error', `Failed to send test motion event: ${error.message}`);
        }
    }

    addLogEntry(level, message) {
        const timestamp = new Date().toLocaleString();
        const logEntry = {
            timestamp,
            level,
            message,
            id: Date.now()
        };

        this.logEntries.unshift(logEntry);
        
        // Limit to 100 log entries
        if (this.logEntries.length > 100) {
            this.logEntries = this.logEntries.slice(0, 100);
        }

        this.renderLogs();
    }

    renderLogs() {
        const logContainer = document.getElementById('activityLog');
        const filteredLogs = this.logEntries.filter(entry => {
            if (this.currentFilter === 'all') return true;
            return entry.level === this.currentFilter;
        });

        logContainer.innerHTML = filteredLogs.map(entry => `
            <div class="log-entry ${entry.level}">
                <span class="log-time">${entry.timestamp}</span>
                <span class="log-message">${entry.message}</span>
            </div>
        `).join('');
    }

    filterLogs(filter) {
        this.currentFilter = filter;
        this.renderLogs();
    }

    clearLogs() {
        this.logEntries = [];
        this.renderLogs();
        this.addLogEntry('info', 'Activity log cleared.');
    }

    startStatsRefresh() {
        // Refresh stats every 30 seconds
        setInterval(() => {
            if (this.isConnected) {
                this.refreshStats();
            }
        }, 30000);
    }

    showNotification(alertData) {
        if ('Notification' in window) {
            if (Notification.permission === 'granted') {
                new Notification('🚨 Motion Detected!', {
                    body: `Sensor ${alertData.sensorId} detected motion at ${alertData.location}`,
                    icon: 'data:image/svg+xml,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><text y=".9em" font-size="90">🏠</text></svg>'
                });
            } else if (Notification.permission !== 'denied') {
                Notification.requestPermission().then(permission => {
                    if (permission === 'granted') {
                        this.showNotification(alertData);
                    }
                });
            }
        }
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.dashboard = new MotionDashboard();
    
    // Request notification permission
    if ('Notification' in window && Notification.permission === 'default') {
        Notification.requestPermission();
    }
});

// Utility functions for external access
window.sendTestMotion = () => window.dashboard.sendTestMotion();
window.refreshStats = () => window.dashboard.refreshStats();
window.clearLogs = () => window.dashboard.clearLogs();