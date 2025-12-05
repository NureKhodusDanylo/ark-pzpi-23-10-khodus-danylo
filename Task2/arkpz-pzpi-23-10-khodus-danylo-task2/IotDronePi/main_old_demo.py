"""
IoT Robot Delivery System - Main Entry Point
ESP32 MicroPython Application for RobDeliveryAPI

This application manages a robotic delivery drone/courier,
handling WiFi connection, authentication, GPS navigation,
battery management, and telemetry communication with the server.
"""

import time
import network
import urequests
import ujson
import sys

# Add module paths
sys.path.append('/config')
sys.path.append('/core')
sys.path.append('/modules')
sys.path.append('/utils')

# Import configuration
from config import DEBUG

# Import core classes
from robot import Robot, RobotState

# Import utility functions
from helpers import log_message

# Import managers
from wifi_manager import WiFiManager
from auth_manager import AuthManager
from gps_simulator import GPSSimulator
from battery_manager import BatteryManager
from telemetry import TelemetryManager
from order_manager import OrderManager


class RobotController:
    """
    Main controller for robot operations
    Orchestrates all subsystems
    """

    def __init__(self):
        log_message("=" * 50)
        log_message("IoT Robot Delivery System Starting...")
        log_message("=" * 50)

        # Initialize core robot
        self.robot = Robot()

        # Initialize managers
        self.wifi_manager = WiFiManager()
        self.auth_manager = AuthManager()
        self.gps_simulator = None  # Will be initialized after robot is created
        self.battery_manager = None
        self.telemetry_manager = None
        self.order_manager = None

        # System state
        self.running = True
        self.initialized = False

    def initialize(self):
        """
        Initialize all subsystems
        """
        log_message("Initializing robot subsystems...")

        # Step 1: Connect to WiFi
        if not self.wifi_manager.connect():
            log_message("Failed to connect to WiFi. Cannot proceed.", "ERROR")
            return False

        # Step 2: Authenticate with server
        if not self.auth_manager.login():
            log_message("Failed to authenticate with server. Cannot proceed.", "ERROR")
            return False

        # Set robot ID from authentication
        self.robot.robot_id = self.auth_manager.get_robot_id()

        # Step 3: Initialize remaining managers
        self.gps_simulator = GPSSimulator(self.robot)
        self.battery_manager = BatteryManager(self.robot)
        self.telemetry_manager = TelemetryManager(self.robot, self.auth_manager)
        self.order_manager = OrderManager(self.robot, self.auth_manager)

        # Step 4: Fetch robot information from server
        robot_info = self.telemetry_manager.fetch_robot_info()
        if robot_info:
            log_message("Robot initialized: {}".format(self.robot))
        else:
            log_message("Warning: Could not fetch robot info from server", "WARNING")

        # Step 5: Send initial telemetry
        self.telemetry_manager.send_status_update(force=True)

        self.initialized = True
        log_message("Robot initialization complete!")
        log_message("=" * 50)

        return True

    def main_loop(self):
        """
        Main control loop
        """
        log_message("Entering main control loop...")

        loop_counter = 0

        while self.running:
            try:
                loop_counter += 1

                # Check WiFi connection
                if not self.wifi_manager.reconnect_if_needed():
                    log_message("WiFi connection lost. Retrying...", "WARNING")
                    time.sleep(5)
                    continue

                # Update battery
                self.battery_manager.update_battery()

                # Check for critical battery
                if self.battery_manager.check_battery_critical():
                    if not self.battery_manager.is_charging:
                        log_message("Battery critical! Starting emergency charging", "WARNING")
                        self.handle_low_battery()

                # Update GPS position if moving
                if self.gps_simulator.is_moving:
                    still_moving = self.gps_simulator.update_position()

                    if not still_moving:
                        # Reached destination
                        self.handle_arrival_at_destination()

                # Send telemetry update
                if self.telemetry_manager.should_send_update():
                    self.telemetry_manager.send_status_update()

                # Demo mode: Simulate a delivery mission every 60 seconds
                if loop_counter % 60 == 30 and self.robot.status == RobotState.IDLE:
                    self.start_demo_delivery()

                # Small delay to prevent excessive CPU usage
                time.sleep(0.5)

            except KeyboardInterrupt:
                log_message("Received shutdown signal", "WARNING")
                self.running = False
                break
            except Exception as e:
                log_message("Error in main loop: {}".format(str(e)), "ERROR")
                time.sleep(1)

    def handle_arrival_at_destination(self):
        """
        Handle robot arrival at destination
        """
        if self.order_manager.has_active_order():
            phase = self.order_manager.get_order_phase()

            if phase == "going_to_pickup":
                log_message("Arrived at pickup location")
                self.order_manager.update_order_phase("delivering")

                # Navigate to dropoff
                dropoff = self.order_manager.dropoff_coordinates
                if dropoff:
                    self.gps_simulator.set_destination(dropoff[0], dropoff[1])

            elif phase == "delivering":
                log_message("Arrived at dropoff location. Delivery complete!")
                self.order_manager.complete_order()

                # Send telemetry update
                self.telemetry_manager.send_status_update(force=True)

                # Return to idle
                self.robot.set_status(RobotState.IDLE)

    def handle_low_battery(self):
        """
        Handle low battery situation
        """
        # Cancel any active orders
        if self.order_manager.has_active_order():
            self.order_manager.cancel_order("Low battery")

        # Stop movement
        self.gps_simulator.stop_movement()

        # Start charging
        self.battery_manager.start_charging()
        self.robot.set_status(RobotState.CHARGING)

        # Send telemetry update
        self.telemetry_manager.send_status_update(force=True)

    def start_demo_delivery(self):
        """
        Start a demonstration delivery mission
        (Simulated for testing purposes)
        """
        log_message("Starting demo delivery mission...")

        # Define demo coordinates (pickup and dropoff)
        pickup_lat = self.robot.current_latitude + 0.001  # ~111 meters north
        pickup_lon = self.robot.current_longitude

        dropoff_lat = pickup_lat + 0.001  # Another ~111 meters north
        dropoff_lon = pickup_lon + 0.001  # ~111 meters east

        # Start order
        if self.order_manager.start_order(9999, pickup_lat, pickup_lon, dropoff_lat, dropoff_lon):
            # Navigate to pickup
            self.gps_simulator.set_destination(pickup_lat, pickup_lon)

            # Send telemetry update
            self.telemetry_manager.send_status_update(force=True)

    def shutdown(self):
        """
        Shutdown robot systems
        """
        log_message("Shutting down robot systems...")

        # Send final telemetry
        if self.initialized:
            self.robot.set_status(RobotState.MAINTENANCE)
            self.telemetry_manager.send_status_update(force=True)

        # Disconnect WiFi
        self.wifi_manager.disconnect()

        log_message("Shutdown complete.")


def main():
    """
    Main entry point
    """
    controller = RobotController()

    try:
        # Initialize robot
        if controller.initialize():
            # Run main loop
            controller.main_loop()
        else:
            log_message("Initialization failed. Exiting.", "ERROR")

    except Exception as e:
        log_message("Fatal error: {}".format(str(e)), "ERROR")

    finally:
        # Cleanup
        controller.shutdown()


# Start the application
if __name__ == "__main__":
    main()
