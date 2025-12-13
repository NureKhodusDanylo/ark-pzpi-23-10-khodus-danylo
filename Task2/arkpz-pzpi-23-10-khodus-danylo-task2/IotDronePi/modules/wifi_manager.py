import network
import time
import sys
import gc

sys.path.append('/config')
sys.path.append('/utils')

from config import WIFI_CONFIG, DEBUG
from helpers import log_message

class WiFiManager:
    """
    Manages WiFi connection for ESP32
    """

    def __init__(self):
        # Run garbage collection before allocating WiFi resources
        gc.collect()
        
        self.wlan = None
        self.ssid = WIFI_CONFIG["SSID"]
        self.password = WIFI_CONFIG["PASSWORD"]
        self.connected = False
        
        self._init_wlan()

    def _init_wlan(self):
        """Internal method to initialize WLAN interface"""
        try:
            self.wlan = network.WLAN(network.STA_IF)
            self.wlan.active(True)
            return True
        except RuntimeError as e:
            log_message("WiFi init error: {}, retrying...".format(str(e)), "WARNING")
            time.sleep(1)
            try:
                self.wlan = network.WLAN(network.STA_IF)
                self.wlan.active(True)
                return True
            except Exception as e2:
                log_message("WiFi init failed details: {}".format(str(e2)), "ERROR")
                # self.wlan remains whatever it was (likely None or a broken object)
                if not isinstance(self.wlan, network.WLAN) and self.wlan is not None:
                     # Attempt to reset if it's junk
                     self.wlan = None
                return False
        except Exception as e:
             log_message("WiFi init generic error: {}".format(str(e)), "ERROR")
             self.wlan = None
             return False

    @property
    def wifi_ssid(self):
        """Property to access SSID for compatibility"""
        return self.ssid

    def connect(self, max_retries=10, retry_delay=1):
        """
        Connect to WiFi network
        """
        # Ensure wlan is initialized
        if self.wlan is None:
            log_message("WLAN interface not initialized, trying again...", "WARNING")
            if not self._init_wlan():
                return False

        if self.is_connected():
            log_message("Already connected to WiFi")
            return True

        log_message("Connecting to WiFi: {}".format(self.ssid))

        try:
            if not self.wlan.active():
                self.wlan.active(True)
            self.wlan.connect(self.ssid, self.password)
        except Exception as e:
            log_message("WiFi connect error: {}".format(str(e)), "ERROR")
            return False

        retry_count = 0
        while not self.wlan.isconnected() and retry_count < max_retries:
            if DEBUG:
                print(".", end="")
            time.sleep(retry_delay)
            retry_count += 1

        if self.wlan.isconnected():
            self.connected = True
            try:
                ip_address = self.wlan.ifconfig()[0]
                log_message("WiFi connected! IP: {}".format(ip_address))
            except:
                log_message("WiFi connected but failed to get IP")
            return True
        else:
            self.connected = False
            log_message("Failed to connect to WiFi after {} attempts".format(max_retries), "ERROR")
            return False

    def disconnect(self):
        """
        Disconnect from WiFi network
        """
        try:
            if self.wlan and self.wlan.isconnected():
                self.wlan.disconnect()
                self.wlan.active(False)
                self.connected = False
                log_message("Disconnected from WiFi")
        except:
            pass

    def is_connected(self):
        """
        Check if WiFi is connected
        """
        try:
            if self.wlan:
                return self.wlan.isconnected()
            return False
        except:
            return False

    def get_ip_address(self):
        """
        Get current IP address
        """
        if self.is_connected():
            return self.wlan.ifconfig()[0]
        return None

    def get_signal_strength(self):
        """
        Get WiFi signal strength (RSSI)
        """
        if self.is_connected():
            return self.wlan.status('rssi')
        return None

    def reconnect_if_needed(self):
        """
        Check connection and reconnect if disconnected
        """
        if not self.is_connected():
            log_message("WiFi disconnected. Attempting to reconnect...", "WARNING")
            return self.connect()
        return True
