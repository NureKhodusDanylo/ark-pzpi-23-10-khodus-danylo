import urequests
import ujson
import sys

sys.path.append('/config')
sys.path.append('/utils')

from config import API_CONFIG, ROBOT_CONFIG, DEBUG
from helpers import log_message

class AuthManager:
    """
    Manages robot authentication with the server
    """

    def __init__(self):
        self.base_url = API_CONFIG["BASE_URL"]
        self.auth_endpoint = API_CONFIG["AUTH_ENDPOINT"]
        self.register_endpoint = "/api/Auth/robot/register"
        self.serial_number = ROBOT_CONFIG["SERIAL_NUMBER"]
        self.access_key = ROBOT_CONFIG["ACCESS_KEY"]
        self.robot_type = ROBOT_CONFIG.get("TYPE", "Drone")
        self.token = None
        self.robot_id = None

    def login(self):
        """
        Authenticate robot with the server
        First tries to register, if robot exists - then login

        Returns:
            bool: True if authentication successful, False otherwise
        """
        # Try to register first
        if self._try_register():
            return True

        # If registration failed (robot already exists), try login
        log_message("Robot already registered. Attempting login...")
        return self._try_login()

    def _try_register(self):
        """
        Try to register robot on the server

        Returns:
            bool: True if registration successful, False otherwise
        """
        try:
            url = self.base_url + self.register_endpoint

            payload = {
                "name": "{}-{}".format(self.robot_type, self.serial_number[-3:]),
                "model": "ESP32-{}".format(self.robot_type),
                "type": self.robot_type,
                "serialNumber": self.serial_number,
                "accessKey": self.access_key,
                "batteryCapacityJoules": ROBOT_CONFIG.get("BATTERY_CAPACITY_JOULES", 360000),
                "energyConsumptionPerMeterJoules": ROBOT_CONFIG.get("ENERGY_CONSUMPTION_PER_METER", 36),
                "currentNodeId": API_CONFIG.get("START_NODE", 25)
            }

            headers = {
                'Content-Type': 'application/json'
            }

            if DEBUG:
                log_message("Attempting robot registration: {}".format(self.serial_number))

            response = urequests.post(
                url,
                data=ujson.dumps(payload),
                headers=headers
            )

            if response.status_code == 200:
                data = ujson.loads(response.text)
                self.token = data.get('token')
                self.robot_id = data.get('robotId')

                if self.token:
                    log_message("Robot registered successfully! Robot ID: {}".format(self.robot_id))
                    response.close()
                    return True
                else:
                    log_message("Registration failed: No token received", "ERROR")
                    response.close()
                    return False
            else:
                if DEBUG:
                    log_message("Registration failed: Status {}".format(response.status_code), "DEBUG")
                    log_message("Response: {}".format(response.text), "DEBUG")
                response.close()
                return False

        except Exception as e:
            if DEBUG:
                log_message("Registration error: {}".format(str(e)), "DEBUG")
            return False

    def _try_login(self):
        """
        Try to login robot on the server

        Returns:
            bool: True if login successful, False otherwise
        """
        try:
            url = self.base_url + self.auth_endpoint

            payload = {
                "serialNumber": self.serial_number,
                "accessKey": self.access_key
            }

            headers = {
                'Content-Type': 'application/json'
            }

            if DEBUG:
                log_message("Authenticating robot: {}".format(self.serial_number))

            response = urequests.post(
                url,
                data=ujson.dumps(payload),
                headers=headers
            )

            if response.status_code == 200:
                data = ujson.loads(response.text)
                self.token = data.get('token')
                self.robot_id = data.get('robotId')

                if self.token:
                    log_message("Login successful! Robot ID: {}".format(self.robot_id))
                    response.close()
                    return True
                else:
                    log_message("Login failed: No token received", "ERROR")
                    response.close()
                    return False
            else:
                log_message("Login failed: Status {}".format(response.status_code), "ERROR")
                if DEBUG:
                    log_message("Response: {}".format(response.text), "DEBUG")
                response.close()
                return False

        except Exception as e:
            log_message("Login error: {}".format(str(e)), "ERROR")
            return False

    def get_auth_header(self):
        """
        Get authorization header for API requests

        Returns:
            dict: Authorization header or empty dict if not authenticated
        """
        if self.token:
            return {'Authorization': 'Bearer {}'.format(self.token)}
        else:
            log_message("No authentication token available", "WARNING")
            return {}

    def is_authenticated(self):
        """
        Check if robot is authenticated

        Returns:
            bool: True if authenticated, False otherwise
        """
        return self.token is not None

    def get_robot_id(self):
        """
        Get authenticated robot ID

        Returns:
            int: Robot ID or None if not authenticated
        """
        return self.robot_id

    def get_token(self):
        """
        Get authentication token

        Returns:
            str: JWT token or None if not authenticated
        """
        return self.token

    def refresh_token(self):
        """
        Refresh authentication token by re-authenticating

        Returns:
            bool: True if refresh successful, False otherwise
        """
        log_message("Refreshing authentication token...")
        return self.login()

    def logout(self):
        """
        Clear authentication data
        """
        self.token = None
        self.robot_id = None
        log_message("Logged out")
