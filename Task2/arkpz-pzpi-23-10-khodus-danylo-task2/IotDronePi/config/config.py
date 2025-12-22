# ===================================
# IoT Robot Configuration
# ===================================

# WiFi Configuration
WIFI_CONFIG = {
    "SSID": "Wokwi-GUEST",
    "PASSWORD": ""
}

# API Configuration
API_CONFIG = {
    "BASE_URL": "http://136.112.182.193:5102",
    "AUTH_ENDPOINT": "/api/Auth/robot/login",
    "ROBOT_STATUS_ENDPOINT": "/api/Robot/status",
    "ROBOT_ME_ENDPOINT": "/api/Robot/me",
    "REQUEST_TIMEOUT": 10,
    "START_NODE": 25
}

# Robot Credentials (must be configured for each robot)
ROBOT_CONFIG = {
    "SERIAL_NUMBER": "ESP32-DRONE-001",
    "ACCESS_KEY": "secret_robot_key_12345",
    "TYPE": "Drone",  # "Drone" or "GroundCourier"
    "BATTERY_CAPACITY_JOULES": 360000,  # Will be used for registration
    "ENERGY_CONSUMPTION_PER_METER": 36  # Will be used for registration
}

# Robot Characteristics
ROBOT_CHARACTERISTICS = {
    "TYPE": "Drone",  # "Drone" or "GroundCourier"
    "BATTERY_CAPACITY_JOULES": 360000,  # 100Wh
    "ENERGY_CONSUMPTION_PER_METER": 36,  # Joules per meter
    "MAX_SPEED_MS": 10.0,  # meters per second
    "MIN_BATTERY_LEVEL": 20.0  # minimum battery level for operations
}

# GPS Simulation Configuration
GPS_CONFIG = {
    "START_LATITUDE": 50.0,
    "START_LONGITUDE": 36.0,
    "MOVEMENT_STEP": 0.0001,  # degrees per update (approx 11 meters)
    "UPDATE_INTERVAL": 2  # seconds
}

# Telemetry Configuration
TELEMETRY_CONFIG = {
    "UPDATE_INTERVAL": 5,  # seconds
    "BATTERY_DRAIN_RATE": 0.1  # percent per second when moving
}

# Debug Configuration
DEBUG = True
