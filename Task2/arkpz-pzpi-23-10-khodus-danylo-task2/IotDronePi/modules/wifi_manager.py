import network
import time
import sys

sys.path.append('/config')
sys.path.append('/utils')

from config import WIFI_CONFIG, DEBUG
from helpers import log_message

class WiFiManager:
    """
    Manages WiFi connection for ESP32
    """

    def __init__(self):
        self.wlan = network.WLAN(network.STA_IF)
        self.ssid = WIFI_CONFIG["SSID"]
        self.password = WIFI_CONFIG["PASSWORD"]
        self.connected = False

    def connect(self, max_retries=10, retry_delay=1):
        """
        Connect to WiFi network

        Args:
            max_retries: Maximum number of connection attempts
            retry_delay: Delay between retries in seconds

        Returns:
            bool: True if connected successfully, False otherwise
        """
        if self.is_connected():
            log_message("Already connected to WiFi")
            return True

        log_message("Connecting to WiFi: {}".format(self.ssid))

        self.wlan.active(True)
        self.wlan.connect(self.ssid, self.password)

        retry_count = 0
        while not self.wlan.isconnected() and retry_count < max_retries:
            if DEBUG:
                print(".", end="")
            time.sleep(retry_delay)
            retry_count += 1

        if self.wlan.isconnected():
            self.connected = True
            ip_address = self.wlan.ifconfig()[0]
            log_message("WiFi connected! IP: {}".format(ip_address))
            return True
        else:
            self.connected = False
            log_message("Failed to connect to WiFi after {} attempts".format(max_retries), "ERROR")
            return False

    def disconnect(self):
        """
        Disconnect from WiFi network
        """
        if self.wlan.isconnected():
            self.wlan.disconnect()
            self.wlan.active(False)
            self.connected = False
            log_message("Disconnected from WiFi")

    def is_connected(self):
        """
        Check if WiFi is connected

        Returns:
            bool: True if connected, False otherwise
        """
        return self.wlan.isconnected()

    def get_ip_address(self):
        """
        Get current IP address

        Returns:
            str: IP address or None if not connected
        """
        if self.is_connected():
            return self.wlan.ifconfig()[0]
        return None

    def get_signal_strength(self):
        """
        Get WiFi signal strength (RSSI)

        Returns:
            int: RSSI value in dBm or None if not connected
        """
        if self.is_connected():
            return self.wlan.status('rssi')
        return None

    def reconnect_if_needed(self):
        """
        Check connection and reconnect if disconnected

        Returns:
            bool: True if connected, False otherwise
        """
        if not self.is_connected():
            log_message("WiFi disconnected. Attempting to reconnect...", "WARNING")
            return self.connect()
        return True
