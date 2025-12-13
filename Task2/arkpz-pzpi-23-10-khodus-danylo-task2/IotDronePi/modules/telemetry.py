import urequests
import ujson
import time
import sys
from machine import Pin
import tm1637
from time import sleep

sys.path.append('/config')
sys.path.append('/utils')

from config import API_CONFIG, TELEMETRY_CONFIG, DEBUG
from helpers import log_message

class TelemetryManager:
    """
    Manages telemetry data transmission to server
    """

    def __init__(self, robot, auth_manager):
        self.robot = robot
        self.auth_manager = auth_manager
        self.base_url = API_CONFIG["BASE_URL"]
        self.status_endpoint = API_CONFIG["ROBOT_STATUS_ENDPOINT"]
        self.me_endpoint = API_CONFIG["ROBOT_ME_ENDPOINT"]
        self.update_interval = TELEMETRY_CONFIG["UPDATE_INTERVAL"]
        self.last_update_time = 0
        try:
            self.tm = tm1637.TM1637(clk=Pin(18), dio=Pin(19))
            self.tm.brightness(7)
        except Exception as e:
            log_message("Display init failed: " + str(e), "ERROR")
            self.tm = None

    def send_status_update(self, force=False):
        """
        Send robot status update to server

        Args:
            force: Force sending update even if interval hasn't passed

        Returns:
            bool: True if update sent successfully, False otherwise
        """
        current_time = time.time()

        # Check if it's time to send update
        if not force and (current_time - self.last_update_time < self.update_interval):
            return True

        self.last_update_time = current_time

        if not self.auth_manager.is_authenticated():
            log_message("Cannot send telemetry: Not authenticated", "WARNING")
            return False

        try:
            url = self.base_url + self.status_endpoint

            # Prepare telemetry payload
            payload = {
                "status": self.robot.status,
                "batteryLevel": round(self.robot.battery_level, 2),
                "currentNodeId": self.robot.current_node_id,
                "currentLatitude": self.robot.current_latitude,
                "currentLongitude": self.robot.current_longitude,
                "targetNodeId": self.robot.target_node_id
            }

            headers = {
                'Content-Type': 'application/json'
            }
            headers.update(self.auth_manager.get_auth_header())

            if DEBUG:
                log_message(
                    "Sending telemetry: Status={}, Battery={:.1f}%, "
                    "Pos=({:.6f}, {:.6f})".format(
                        self.robot.status,
                        self.robot.battery_level,
                        self.robot.current_latitude or 0.0,
                        self.robot.current_longitude or 0.0
                    ),
                    "DEBUG"
                )

            self.tm.number(int(self.robot.battery_level))

            response = urequests.post(
                url,
                data=ujson.dumps(payload),
                headers=headers
            )

            if response.status_code == 200:
                if DEBUG:
                    log_message("Telemetry sent successfully", "DEBUG")
                response.close()
                return True
            else:
                log_message(
                    "Telemetry failed: Status {}".format(response.status_code),
                    "ERROR"
                )
                if DEBUG:
                    log_message("Response: {}".format(response.text), "DEBUG")
                response.close()
                return False

        except Exception as e:
            log_message("Telemetry error: {}".format(str(e)), "ERROR")
            return False

    def fetch_robot_info(self):
        """
        Fetch robot information from server

        Returns:
            dict: Robot information or None if failed
        """
        if not self.auth_manager.is_authenticated():
            log_message("Cannot fetch robot info: Not authenticated", "WARNING")
            return None

        try:
            url = self.base_url + self.me_endpoint

            headers = self.auth_manager.get_auth_header()

            response = urequests.get(url, headers=headers)

            if response.status_code == 200:
                data = ujson.loads(response.text)
                log_message("Robot info fetched successfully")

                # Update robot with server data
                self.robot.set_robot_info(data)

                response.close()
                return data
            else:
                log_message(
                    "Failed to fetch robot info: Status {}".format(response.status_code),
                    "ERROR"
                )
                response.close()
                return None

        except Exception as e:
            log_message("Error fetching robot info: {}".format(str(e)), "ERROR")
            return None

    def should_send_update(self):
        """
        Check if it's time to send telemetry update

        Returns:
            bool: True if should send update, False otherwise
        """
        current_time = time.time()
        return (current_time - self.last_update_time) >= self.update_interval

    def get_last_update_time(self):
        """
        Get timestamp of last telemetry update

        Returns:
            float: Timestamp of last update
        """
        return self.last_update_time

    def fetch_node_info(self, node_id):
        """
        Fetch node information from server

        Args:
            node_id: Node ID to fetch

        Returns:
            dict: Node information with coordinates or None if failed
        """
        if not self.auth_manager.is_authenticated():
            log_message("Cannot fetch node info: Not authenticated", "WARNING")
            return None

        try:
            url = "{}/api/Node/{}".format(self.base_url, node_id)

            headers = self.auth_manager.get_auth_header()

            response = urequests.get(url, headers=headers)

            if response.status_code == 200:
                data = ujson.loads(response.text)
                log_message("Node {} info fetched: ({:.6f}, {:.6f})".format(
                    node_id,
                    data.get('latitude', 0.0),
                    data.get('longitude', 0.0)
                ))

                response.close()
                return data
            else:
                log_message(
                    "Failed to fetch node info: Status {}".format(response.status_code),
                    "ERROR"
                )
                response.close()
                return None

        except Exception as e:
            log_message("Error fetching node info: {}".format(str(e)), "ERROR")
            return None
