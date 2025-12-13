"""
Example: How to integrate DisplayManager into the main robot code

This file shows examples of integrating the display into:
1. HardwareController
2. Main FSM state handlers
3. Various subsystems
"""

# ============================================================
# EXAMPLE 1: Add Display to HardwareController
# ============================================================

# File: modules/hardware_controller.py

"""
Add these imports at the top:
"""
from display_manager import DisplayManager

"""
Modify the HardwareController class __init__:
"""

class HardwareController:
    def __init__(self):
        # ... existing initialization code ...

        # Initialize display
        try:
            self.display = DisplayManager()
            log_message("LCD display initialized")
        except Exception as e:
            log_message("Failed to initialize display: {}".format(str(e)), "WARNING")
            self.display = None

        # ... rest of initialization ...


# ============================================================
# EXAMPLE 2: Update Display in Main FSM States
# ============================================================

# File: main.py

"""
Add display updates in each state handler:
"""

class RobotControllerFSM:

    def state_idle(self):
        """IDLE state: Wait and check for orders periodically"""
        # Ensure robot status is Idle
        if self.robot.status != "Idle":
            self.robot.set_status("Idle")

        # UPDATE DISPLAY - Show idle status with animation
        if self.hardware_controller.display:
            self.hardware_controller.display.display_idle(self.robot)

        current_time = time.time()

        if current_time - self.last_order_check_time >= self.order_check_interval:
            self.last_order_check_time = current_time
            self.fsm.transition_to(DroneState.CHECK_ORDERS)


    def state_check_orders(self):
        """CHECK_ORDERS state: Fetch orders from server"""

        # UPDATE DISPLAY - Show checking orders animation
        if self.hardware_controller.display:
            self.hardware_controller.display.display_checking_orders(self.robot)

        orders = self.order_manager.fetch_assigned_orders()

        if orders and len(orders) > 0:
            order = orders[0]
            self.fsm.transition_to(DroneState.ORDER_ASSIGNED, {"order": order})
        else:
            if self.fsm.previous_state == DroneState.CHARGING:
                self.fsm.transition_to(DroneState.CHARGING)
            else:
                self.fsm.transition_to(DroneState.IDLE)


    def state_order_assigned(self):
        """ORDER_ASSIGNED state: Accept order and prepare"""
        order = self.fsm.get_state_data("order")

        if order:
            order_id = order.get("orderId")

            # UPDATE DISPLAY - Show order assigned
            if self.hardware_controller.display:
                self.hardware_controller.display.display_order_assigned(
                    self.robot, order_id
                )

            if self.order_manager.accept_order(order_id):
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


    def state_flight_to_pickup(self):
        """FLIGHT_TO_PICKUP state: Flying to pickup location"""

        # UPDATE DISPLAY - Show flight with distance
        if self.hardware_controller.display and self.gps_simulator.is_moving:
            distance = self.gps_simulator.get_distance_to_target()
            self.hardware_controller.display.display_flight_to_pickup(
                self.robot, distance
            )

        # Check if destination has been set
        if not self.gps_simulator.is_moving:
            pickup_coords = self.order_manager.get_pickup_coordinates()
            if pickup_coords:
                self.gps_simulator.set_destination(pickup_coords[0], pickup_coords[1])
                self.order_manager.update_order_phase("FLIGHT_TO_PICKUP")
            else:
                log_message("No pickup coordinates available", "ERROR")
                self.fsm.handle_error("No pickup coordinates")


    def state_at_pickup(self):
        """AT_PICKUP state: Arrived at pickup location"""
        pickup_node_id = self.order_manager.get_pickup_node_id()
        if pickup_node_id:
            self.robot.current_node_id = pickup_node_id
            log_message("Arrived at pickup node {}".format(pickup_node_id))

        # UPDATE DISPLAY - Show at pickup
        if self.hardware_controller.display:
            self.hardware_controller.display.display_at_pickup(self.robot)

        self.order_manager.update_order_phase("AT_PICKUP")
        self.hardware_controller.stop_motors()

        time.sleep(1)
        self.fsm.transition_to(DroneState.OPEN_COMPARTMENT_PICKUP)


    def state_loading(self):
        """LOADING state: Wait for package to be loaded"""
        entry_time = self.fsm.get_state_data("entry_time", time.time())
        elapsed = time.time() - entry_time

        # UPDATE DISPLAY - Show loading with timer
        if self.hardware_controller.display:
            self.hardware_controller.display.display_loading(self.robot, elapsed)

        if elapsed >= 5:
            log_message("Package loaded")
            self.fsm.transition_to(DroneState.CLOSE_COMPARTMENT_PICKUP)


    def state_flight_to_dropoff(self):
        """FLIGHT_TO_DROPOFF state: Flying to dropoff location"""

        # UPDATE DISPLAY - Show flight to dropoff
        if self.hardware_controller.display and self.gps_simulator.is_moving:
            distance = self.gps_simulator.get_distance_to_target()
            self.hardware_controller.display.display_flight_to_dropoff(
                self.robot, distance
            )

        if not self.gps_simulator.is_moving:
            self.hardware_controller.start_motors()
            dropoff_coords = self.order_manager.get_dropoff_coordinates()
            if dropoff_coords:
                self.gps_simulator.set_destination(dropoff_coords[0], dropoff_coords[1])
                self.order_manager.update_order_phase("FLIGHT_TO_DROPOFF")
            else:
                log_message("No dropoff coordinates available", "ERROR")
                self.fsm.handle_error("No dropoff coordinates")


    def state_at_dropoff(self):
        """AT_DROPOFF state: Arrived at dropoff location"""
        dropoff_node_id = self.order_manager.get_dropoff_node_id()
        if dropoff_node_id:
            self.robot.current_node_id = dropoff_node_id
            log_message("Arrived at dropoff node {}".format(dropoff_node_id))

        # UPDATE DISPLAY - Show at dropoff
        if self.hardware_controller.display:
            self.hardware_controller.display.display_at_dropoff(self.robot)

        self.order_manager.update_order_phase("AT_DROPOFF")
        self.hardware_controller.stop_motors()

        time.sleep(1)
        self.fsm.transition_to(DroneState.OPEN_COMPARTMENT_DROPOFF)


    def state_wait_for_pickup(self):
        """WAIT_FOR_PICKUP state: Wait for recipient to take package"""
        entry_time = self.fsm.get_state_data("entry_time", time.time())
        elapsed = time.time() - entry_time

        # UPDATE DISPLAY - Show unloading with timer
        if self.hardware_controller.display:
            self.hardware_controller.display.display_unloading(self.robot, elapsed)

        if self.hardware_controller.is_button_pressed():
            log_message("Package picked up by recipient")
            self.fsm.transition_to(DroneState.PACKAGE_DELIVERED)
        elif elapsed >= 10:
            log_message("Package pickup timeout (simulation)", "WARNING")
            self.fsm.transition_to(DroneState.PACKAGE_DELIVERED)


    def state_package_delivered(self):
        """PACKAGE_DELIVERED state: Package delivered successfully"""

        # UPDATE DISPLAY - Show success message
        if self.hardware_controller.display:
            self.hardware_controller.display.display_package_delivered(self.robot)

        self.order_manager.update_order_phase("PACKAGE_DELIVERED")
        self.order_manager.complete_order()

        time.sleep(2)  # Show success message for 2 seconds
        self.fsm.transition_to(DroneState.CLOSE_COMPARTMENT_DROPOFF)


    def state_flight_to_charging(self):
        """FLIGHT_TO_CHARGING state: Flying to charging station"""

        # UPDATE DISPLAY - Show flight to charging
        if self.hardware_controller.display and self.gps_simulator.is_moving:
            distance = self.gps_simulator.get_distance_to_target()
            self.hardware_controller.display.display_flight_to_charging(
                self.robot, distance
            )

        if not self.gps_simulator.is_moving:
            self.hardware_controller.start_motors()
            self.order_manager.update_order_phase("FLIGHT_TO_CHARGING")

            if self.home_charging_lat and self.home_charging_lon:
                self.gps_simulator.set_destination(
                    self.home_charging_lat,
                    self.home_charging_lon,
                    self.home_charging_node_id
                )
            else:
                log_message("No home charging station saved", "WARNING")


    def state_charging(self):
        """CHARGING state: Charging battery"""

        # UPDATE DISPLAY - Show charging animation
        if self.hardware_controller.display:
            self.hardware_controller.display.display_charging(self.robot)

        current_time = time.time()

        if current_time - self.last_order_check_time >= self.order_check_interval:
            self.last_order_check_time = current_time

            if self.robot.battery_level >= 95:
                self.fsm.transition_to(DroneState.CHECK_ORDERS)


    def state_error(self):
        """ERROR state: Handle error condition"""
        error = self.fsm.get_state_data("error", "Unknown error")
        log_message("In ERROR state: {}".format(error), "ERROR")

        # UPDATE DISPLAY - Show error
        if self.hardware_controller.display:
            self.hardware_controller.display.display_error(error)

        self.hardware_controller.stop_motors()
        self.hardware_controller.close_compartment()

        if self.order_manager.has_active_order():
            self.order_manager.cancel_order("Error: {}".format(error))

        self.robot.set_status("Idle")

        time.sleep(5)  # Show error for 5 seconds
        self.fsm.transition_to(DroneState.IDLE)


    def handle_emergency_battery(self):
        """Handle emergency low battery situation"""

        # UPDATE DISPLAY - Show low battery warning
        if self.hardware_controller.display:
            self.hardware_controller.display.display_low_battery_warning(self.robot)

        if self.order_manager.has_active_order():
            self.order_manager.cancel_order("Emergency: Low battery")

        self.gps_simulator.stop_movement()
        self.hardware_controller.stop_motors()

        self.battery_manager.start_charging()
        self.robot.set_status("Charging")

        self.telemetry_manager.send_status_update(force=True)
        self.fsm.transition_to(DroneState.CHARGING)


# ============================================================
# EXAMPLE 3: WiFi and Authentication Integration
# ============================================================

# In initialize() method

def initialize(self):
    """Initialize all subsystems"""
    log_message("Initializing robot subsystems...")

    # Show boot screen
    if self.hardware_controller and self.hardware_controller.display:
        self.hardware_controller.display.display_boot()
        time.sleep(2)
        self.hardware_controller.display.display_system_check()

    # Step 1: Connect to WiFi
    from config import WIFI_CONFIG

    # Show connecting
    if self.hardware_controller and self.hardware_controller.display:
        self.hardware_controller.display.display_wifi_connecting(
            WIFI_CONFIG["SSID"]
        )

    if not self.wifi_manager.connect():
        log_message("Failed to connect to WiFi. Cannot proceed.", "ERROR")

        # Show error
        if self.hardware_controller and self.hardware_controller.display:
            self.hardware_controller.display.display_wifi_error()
            time.sleep(3)

        return False

    # Show success
    if self.hardware_controller and self.hardware_controller.display:
        ip = self.wifi_manager.get_ip_address()
        self.hardware_controller.display.display_wifi_connected(
            WIFI_CONFIG["SSID"], ip
        )
        time.sleep(2)

    # Step 2: Authenticate with server
    # Show authenticating
    if self.hardware_controller and self.hardware_controller.display:
        self.hardware_controller.display.display_authenticating()

    if not self.auth_manager.login():
        log_message("Failed to authenticate with server. Cannot proceed.", "ERROR")

        # Show error
        if self.hardware_controller and self.hardware_controller.display:
            self.hardware_controller.display.display_auth_error()
            time.sleep(3)

        return False

    # Show success
    robot_id = self.auth_manager.get_robot_id()
    self.robot.robot_id = robot_id

    if self.hardware_controller and self.hardware_controller.display:
        self.hardware_controller.display.display_auth_success(robot_id)
        time.sleep(2)

    # ... rest of initialization ...

    return True


# ============================================================
# EXAMPLE 4: Shutdown Integration
# ============================================================

def shutdown(self):
    """Shutdown robot systems"""
    log_message("Shutting down robot systems...")

    # Show shutdown on display
    if self.hardware_controller and self.hardware_controller.display:
        self.hardware_controller.display.shutdown()

    # Shutdown hardware
    if self.hardware_controller:
        self.hardware_controller.shutdown()

    # ... rest of shutdown ...


# ============================================================
# SUMMARY OF CHANGES
# ============================================================

"""
To integrate the display into your robot:

1. Add to hardware_controller.py:
   - Import DisplayManager
   - Initialize in __init__: self.display = DisplayManager()

2. Add to main.py state handlers:
   - state_idle(): display.display_idle(robot)
   - state_check_orders(): display.display_checking_orders(robot)
   - state_order_assigned(): display.display_order_assigned(robot, order_id)
   - state_flight_to_pickup(): display.display_flight_to_pickup(robot, distance)
   - state_at_pickup(): display.display_at_pickup(robot)
   - state_loading(): display.display_loading(robot, elapsed_time)
   - state_flight_to_dropoff(): display.display_flight_to_dropoff(robot, distance)
   - state_at_dropoff(): display.display_at_dropoff(robot)
   - state_wait_for_pickup(): display.display_unloading(robot, elapsed_time)
   - state_package_delivered(): display.display_package_delivered(robot)
   - state_flight_to_charging(): display.display_flight_to_charging(robot, distance)
   - state_charging(): display.display_charging(robot)
   - state_error(): display.display_error(error_message)
   - handle_emergency_battery(): display.display_low_battery_warning(robot)

3. Add to initialize():
   - Boot screen
   - WiFi connecting/connected/error
   - Authentication screens

4. Add to shutdown():
   - display.shutdown()

That's it! Your robot will now have full visual feedback on the LCD display.
"""
