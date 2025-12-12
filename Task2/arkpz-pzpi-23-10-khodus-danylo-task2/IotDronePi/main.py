import time
import network
import urequests
import ujson
import sys

# Add module paths jjgfdjgdfdgjfdjjfdgjdj
sys.path.append('/config')
sys.path.append('/core')
sys.path.append('/modules')
sys.path.append('/utils')

# Import configuration
from config import DEBUG

# Import core classes
from robot import Robot, RobotState as RobotStatus
from state_machine import DroneFSM, DroneState

# Import utility functions
from helpers import log_message

# Import managers
from wifi_manager import WiFiManager
from auth_manager import AuthManager
from gps_simulator import GPSSimulator
from battery_manager import BatteryManager
from telemetry import TelemetryManager
from order_manager import OrderManager
from hardware_controller import HardwareController


class RobotControllerFSM:
    """
    Main controller for robot operations with FSM
    Orchestrates all subsystems using finite state machine
    """

    def __init__(self):
        log_message("=" * 50)
        log_message("IoT Robot Delivery System (FSM) Starting...")
        log_message("=" * 50)

        # Initialize core robot
        self.robot = Robot()

        # Initialize FSM
        self.fsm = DroneFSM(self.robot)

        # Initialize managers
        self.wifi_manager = WiFiManager()
        self.auth_manager = AuthManager()
        self.gps_simulator = None
        self.battery_manager = None
        self.telemetry_manager = None
        self.order_manager = None
        self.hardware_controller = None

        # System state
        self.running = True
        self.initialized = False

        # Timers
        self.last_order_check_time = 0
        self.order_check_interval = 10  # Check for orders every 10 seconds

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

        # Step 3: Initialize managers (without GPS yet)
        self.battery_manager = BatteryManager(self.robot)
        self.telemetry_manager = TelemetryManager(self.robot, self.auth_manager)
        self.order_manager = OrderManager(self.robot, self.auth_manager)
        self.hardware_controller = HardwareController()

        # Step 4: Fetch robot information from server
        robot_info = self.telemetry_manager.fetch_robot_info()
        if robot_info:
            log_message("Robot initialized: {}".format(self.robot))
        else:
            log_message("Warning: Could not fetch robot info from server", "WARNING")

        # Step 5: Fetch START_NODE coordinates and set robot position
        from config import API_CONFIG
        start_node_id = API_CONFIG.get("START_NODE", 25)
        start_node = self.telemetry_manager.fetch_node_info(start_node_id)

        if start_node:
            start_lat = start_node.get('latitude')
            start_lon = start_node.get('longitude')
            if start_lat is not None and start_lon is not None:
                log_message("Setting robot start position from node {}: ({:.6f}, {:.6f})".format(
                    start_node_id, start_lat, start_lon
                ))
                self.robot.set_location(start_lat, start_lon, start_node_id)
            else:
                log_message("Warning: Start node has no coordinates, using config defaults", "WARNING")
        else:
            log_message("Warning: Could not fetch start node, using config defaults", "WARNING")

        # Step 6: Initialize GPS simulator (after robot position is set)
        self.gps_simulator = GPSSimulator(self.robot)

        # Step 7: Send initial telemetry
        self.telemetry_manager.send_status_update(force=True)

        self.initialized = True
        log_message("Robot initialization complete!")
        log_message("=" * 50)

        return True

    def main_loop(self):
        """
        Main control loop with FSM
        """
        log_message("Entering main control loop with FSM...")

        while self.running:
            try:
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
                        log_message("Battery critical! Emergency charging", "WARNING")
                        self.handle_emergency_battery()
                        continue

                # Update GPS position if moving
                if self.gps_simulator.is_moving:
                    still_moving = self.gps_simulator.update_position()

                    if not still_moving:
                        # Reached destination
                        self.handle_arrival_at_destination()

                # Send telemetry update
                if self.telemetry_manager.should_send_update():
                    self.telemetry_manager.send_status_update()

                # Process FSM state
                self.process_current_state()

                # Small delay to prevent excessive CPU usage
                time.sleep(0.5)

            except KeyboardInterrupt:
                log_message("Received shutdown signal", "WARNING")
                self.running = False
                break
            except Exception as e:
                log_message("Error in main loop: {}".format(str(e)), "ERROR")
                self.fsm.handle_error(str(e))
                time.sleep(1)

    def process_current_state(self):
        """
        Process current FSM state and execute appropriate actions
        """
        state = self.fsm.get_current_state()

        if state == DroneState.IDLE:
            self.state_idle()

        elif state == DroneState.CHECK_ORDERS:
            self.state_check_orders()

        elif state == DroneState.ORDER_ASSIGNED:
            self.state_order_assigned()

        elif state == DroneState.MOTORS_ON:
            self.state_motors_on()

        elif state == DroneState.FLIGHT_TO_PICKUP:
            self.state_flight_to_pickup()

        elif state == DroneState.AT_PICKUP:
            self.state_at_pickup()

        elif state == DroneState.OPEN_COMPARTMENT_PICKUP:
            self.state_open_compartment_pickup()

        elif state == DroneState.LOADING:
            self.state_loading()

        elif state == DroneState.CLOSE_COMPARTMENT_PICKUP:
            self.state_close_compartment_pickup()

        elif state == DroneState.FLIGHT_TO_DROPOFF:
            self.state_flight_to_dropoff()

        elif state == DroneState.AT_DROPOFF:
            self.state_at_dropoff()

        elif state == DroneState.OPEN_COMPARTMENT_DROPOFF:
            self.state_open_compartment_dropoff()

        elif state == DroneState.WAIT_FOR_PICKUP:
            self.state_wait_for_pickup()

        elif state == DroneState.PACKAGE_DELIVERED:
            self.state_package_delivered()

        elif state == DroneState.CLOSE_COMPARTMENT_DROPOFF:
            self.state_close_compartment_dropoff()

        elif state == DroneState.FLIGHT_TO_CHARGING:
            self.state_flight_to_charging()

        elif state == DroneState.AT_CHARGING_STATION:
            self.state_at_charging_station()

        elif state == DroneState.CHARGING:
            self.state_charging()

        elif state == DroneState.ERROR:
            self.state_error()

    # FSM State Handlers

    def state_idle(self):
        """IDLE state: Wait and check for orders periodically"""
        # Ensure robot status is Idle
        if self.robot.status != "Idle":
            self.robot.set_status("Idle")

        current_time = time.time()

        if current_time - self.last_order_check_time >= self.order_check_interval:
            self.last_order_check_time = current_time
            self.fsm.transition_to(DroneState.CHECK_ORDERS)

    def state_check_orders(self):
        """CHECK_ORDERS state: Fetch orders from server"""
        orders = self.order_manager.fetch_assigned_orders()

        if orders and len(orders) > 0:
            # Take first order
            order = orders[0]
            self.fsm.transition_to(DroneState.ORDER_ASSIGNED, {"order": order})
        else:
            # No orders, back to idle
            self.fsm.transition_to(DroneState.IDLE)

    def state_order_assigned(self):
        """ORDER_ASSIGNED state: Accept order and prepare"""
        order = self.fsm.get_state_data("order")

        if order:
            order_id = order.get("orderId")

            # Accept order on server
            if self.order_manager.accept_order(order_id):
                # Start order locally
                if self.order_manager.start_order(order):
                    self.fsm.transition_to(DroneState.MOTORS_ON)
                else:
                    log_message("Failed to start order locally", "ERROR")
                    self.fsm.transition_to(DroneState.ERROR)
            else:
                log_message("Failed to accept order on server", "ERROR")
                self.fsm.transition_to(DroneState.ERROR)
        else:
            self.fsm.transition_to(DroneState.IDLE)

    def state_motors_on(self):
        """MOTORS_ON state: Start motors and begin flight"""
        self.hardware_controller.start_motors()
        self.fsm.transition_to(DroneState.FLIGHT_TO_PICKUP)

    def state_flight_to_pickup(self):
        """FLIGHT_TO_PICKUP state: Flying to pickup location"""
        # Check if destination has been set (one-time setup)
        if not self.gps_simulator.is_moving:
            # Set destination
            pickup_coords = self.order_manager.get_pickup_coordinates()
            if pickup_coords:
                self.gps_simulator.set_destination(pickup_coords[0], pickup_coords[1])

                # Notify server
                self.order_manager.update_order_phase("FLIGHT_TO_PICKUP")
            else:
                log_message("No pickup coordinates available", "ERROR")
                self.fsm.handle_error("No pickup coordinates")

    def state_at_pickup(self):
        """AT_PICKUP state: Arrived at pickup location"""
        # Update current node to pickup node
        pickup_node_id = self.order_manager.get_pickup_node_id()
        if pickup_node_id:
            self.robot.current_node_id = pickup_node_id
            log_message("Arrived at pickup node {}".format(pickup_node_id))

        # Notify server
        self.order_manager.update_order_phase("AT_PICKUP")

        # Stop motors
        self.hardware_controller.stop_motors()

        # Transition to open compartment
        time.sleep(1)
        self.fsm.transition_to(DroneState.OPEN_COMPARTMENT_PICKUP)

    def state_open_compartment_pickup(self):
        """OPEN_COMPARTMENT_PICKUP state: Open compartment for loading"""
        self.hardware_controller.open_compartment()
        self.fsm.transition_to(DroneState.LOADING, {"entry_time": time.time()})

    def state_loading(self):
        """LOADING state: Wait for package to be loaded"""
        # Wait 5 seconds for loading (simulated)
        entry_time = self.fsm.get_state_data("entry_time", time.time())
        if time.time() - entry_time >= 5:
            log_message("Package loaded")
            self.fsm.transition_to(DroneState.CLOSE_COMPARTMENT_PICKUP)

    def state_close_compartment_pickup(self):
        """CLOSE_COMPARTMENT_PICKUP state: Close compartment after loading"""
        self.hardware_controller.close_compartment()
        self.fsm.transition_to(DroneState.FLIGHT_TO_DROPOFF)

    def state_flight_to_dropoff(self):
        """FLIGHT_TO_DROPOFF state: Flying to dropoff location"""
        # Check if destination has been set (one-time setup)
        if not self.gps_simulator.is_moving:
            # Start motors
            self.hardware_controller.start_motors()

            # Set destination
            dropoff_coords = self.order_manager.get_dropoff_coordinates()
            if dropoff_coords:
                self.gps_simulator.set_destination(dropoff_coords[0], dropoff_coords[1])

                # Notify server
                self.order_manager.update_order_phase("FLIGHT_TO_DROPOFF")
            else:
                log_message("No dropoff coordinates available", "ERROR")
                self.fsm.handle_error("No dropoff coordinates")

    def state_at_dropoff(self):
        """AT_DROPOFF state: Arrived at dropoff location"""
        # Update current node to dropoff node
        dropoff_node_id = self.order_manager.get_dropoff_node_id()
        if dropoff_node_id:
            self.robot.current_node_id = dropoff_node_id
            log_message("Arrived at dropoff node {}".format(dropoff_node_id))

        # Notify server
        self.order_manager.update_order_phase("AT_DROPOFF")

        # Stop motors
        self.hardware_controller.stop_motors()

        # Transition to open compartment
        time.sleep(1)
        self.fsm.transition_to(DroneState.OPEN_COMPARTMENT_DROPOFF)

    def state_open_compartment_dropoff(self):
        """OPEN_COMPARTMENT_DROPOFF state: Open compartment for unloading"""
        self.hardware_controller.open_compartment()
        self.fsm.transition_to(DroneState.WAIT_FOR_PICKUP, {"entry_time": time.time()})

    def state_wait_for_pickup(self):
        """WAIT_FOR_PICKUP state: Wait for recipient to take package"""
        # Check button press or timeout (10 seconds for simulation)
        entry_time = self.fsm.get_state_data("entry_time", time.time())
        if self.hardware_controller.is_button_pressed():
            log_message("Package picked up by recipient")
            self.fsm.transition_to(DroneState.PACKAGE_DELIVERED)
        elif time.time() - entry_time >= 10:
            log_message("Package pickup timeout (simulation)", "WARNING")
            self.fsm.transition_to(DroneState.PACKAGE_DELIVERED)

    def state_package_delivered(self):
        """PACKAGE_DELIVERED state: Package delivered successfully"""
        # Notify server
        self.order_manager.update_order_phase("PACKAGE_DELIVERED")

        # Complete order
        self.order_manager.complete_order()

        # Transition to close compartment
        time.sleep(1)
        self.fsm.transition_to(DroneState.CLOSE_COMPARTMENT_DROPOFF)

    def state_close_compartment_dropoff(self):
        """CLOSE_COMPARTMENT_DROPOFF state: Close compartment after delivery"""
        self.hardware_controller.close_compartment()

        # Check battery level
        if self.robot.battery_level < 50:
            # Need charging
            self.fsm.transition_to(DroneState.FLIGHT_TO_CHARGING)
        else:
            # Enough battery, return to idle
            self.fsm.transition_to(DroneState.IDLE)

    def state_flight_to_charging(self):
        """FLIGHT_TO_CHARGING state: Flying to charging station"""
        # Check if destination has been set (one-time setup)
        if not self.gps_simulator.is_moving:
            # Start motors
            self.hardware_controller.start_motors()

            # Notify server
            self.order_manager.update_order_phase("FLIGHT_TO_CHARGING")

            # TODO: Get nearest charging station coordinates from server
            # For now, just fly to a fixed location
            charging_lat = self.robot.current_latitude + 0.001
            charging_lon = self.robot.current_longitude
            self.gps_simulator.set_destination(charging_lat, charging_lon)

    def state_at_charging_station(self):
        """AT_CHARGING_STATION state: Arrived at charging station"""
        # Stop motors
        self.hardware_controller.stop_motors()

        # Start charging
        self.battery_manager.start_charging()
        self.robot.set_status("Charging")

        # Send telemetry
        self.telemetry_manager.send_status_update(force=True)

        self.fsm.transition_to(DroneState.CHARGING)

    def state_charging(self):
        """CHARGING state: Charging battery"""
        if self.robot.battery_level >= 95:
            log_message("Charging complete")
            self.battery_manager.stop_charging()
            self.robot.set_status("Idle")
            self.telemetry_manager.send_status_update(force=True)
            self.fsm.transition_to(DroneState.IDLE)

    def state_error(self):
        """ERROR state: Handle error condition"""
        error = self.fsm.get_state_data("error", "Unknown error")
        log_message("In ERROR state: {}".format(error), "ERROR")

        # Stop hardware
        self.hardware_controller.stop_motors()
        self.hardware_controller.close_compartment()

        # Cancel any active order
        if self.order_manager.has_active_order():
            self.order_manager.cancel_order("Error: {}".format(error))

        # Reset robot status to Idle
        self.robot.set_status("Idle")

        # Wait a bit
        time.sleep(5)

        # Try to recover to IDLE
        self.fsm.transition_to(DroneState.IDLE)

    # Helper methods

    def handle_arrival_at_destination(self):
        """
        Handle robot arrival at destination
        """
        state = self.fsm.get_current_state()

        if state == DroneState.FLIGHT_TO_PICKUP:
            self.fsm.transition_to(DroneState.AT_PICKUP)
        elif state == DroneState.FLIGHT_TO_DROPOFF:
            self.fsm.transition_to(DroneState.AT_DROPOFF)
        elif state == DroneState.FLIGHT_TO_CHARGING:
            self.fsm.transition_to(DroneState.AT_CHARGING_STATION)

    def handle_emergency_battery(self):
        """
        Handle emergency low battery situation
        """
        # Stop any active order
        if self.order_manager.has_active_order():
            self.order_manager.cancel_order("Emergency: Low battery")

        # Stop movement
        self.gps_simulator.stop_movement()
        self.hardware_controller.stop_motors()

        # Start charging
        self.battery_manager.start_charging()
        self.robot.set_status("Charging")

        # Send telemetry
        self.telemetry_manager.send_status_update(force=True)

        # Force FSM to charging state
        self.fsm.transition_to(DroneState.CHARGING)

    def shutdown(self):
        """
        Shutdown robot systems
        """
        log_message("Shutting down robot systems...")

        # Shutdown hardware
        if self.hardware_controller:
            self.hardware_controller.shutdown()

        # Send final telemetry
        if self.initialized:
            self.robot.set_status("Maintenance")
            self.telemetry_manager.send_status_update(force=True)

        # Disconnect WiFi
        self.wifi_manager.disconnect()

        log_message("Shutdown complete.")


def main():
    """
    Main entry point
    """
    controller = RobotControllerFSM()

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
