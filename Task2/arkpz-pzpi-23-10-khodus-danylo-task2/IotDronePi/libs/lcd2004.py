"""
LCD2004 Display Library for Robot Delivery System
Provides display management for 20x4 character LCD display
"""

from machine import I2C, Pin
from time import sleep
import sys

sys.path.append('/core')
sys.path.append('/utils')


# MicroPython compatibility: string formatting helpers
def str_ljust(s, width, fillchar=' '):
    """Left-justify string (MicroPython compatible)"""
    s = str(s)
    if len(s) >= width:
        return s[:width]
    return s + (fillchar * (width - len(s)))


def str_rjust(s, width, fillchar=' '):
    """Right-justify string (MicroPython compatible)"""
    s = str(s)
    if len(s) >= width:
        return s[:width]
    return (fillchar * (width - len(s))) + s


def str_center(s, width, fillchar=' '):
    """Center string (MicroPython compatible)"""
    s = str(s)
    if len(s) >= width:
        return s[:width]
    padding = width - len(s)
    left_pad = padding // 2
    right_pad = padding - left_pad
    return (fillchar * left_pad) + s + (fillchar * right_pad)


class LCD2004:
    """
    I2C LCD2004 Display Controller
    4 lines x 20 characters display
    """

    # LCD Commands
    LCD_CLEARDISPLAY = 0x01
    LCD_RETURNHOME = 0x02
    LCD_ENTRYMODESET = 0x04
    LCD_DISPLAYCONTROL = 0x08
    LCD_CURSORSHIFT = 0x10
    LCD_FUNCTIONSET = 0x20
    LCD_SETCGRAMADDR = 0x40
    LCD_SETDDRAMADDR = 0x80

    # Flags for display entry mode
    LCD_ENTRYRIGHT = 0x00
    LCD_ENTRYLEFT = 0x02
    LCD_ENTRYSHIFTINCREMENT = 0x01
    LCD_ENTRYSHIFTDECREMENT = 0x00

    # Flags for display on/off control
    LCD_DISPLAYON = 0x04
    LCD_DISPLAYOFF = 0x00
    LCD_CURSORON = 0x02
    LCD_CURSOROFF = 0x00
    LCD_BLINKON = 0x01
    LCD_BLINKOFF = 0x00

    # Flags for function set
    LCD_8BITMODE = 0x10
    LCD_4BITMODE = 0x00
    LCD_2LINE = 0x08
    LCD_1LINE = 0x00
    LCD_5x10DOTS = 0x04
    LCD_5x8DOTS = 0x00

    # Flags for backlight control
    LCD_BACKLIGHT = 0x08
    LCD_NOBACKLIGHT = 0x00

    # Enable bit
    En = 0b00000100
    Rw = 0b00000010
    Rs = 0b00000001

    def __init__(self, i2c_addr=0x27, scl_pin=22, sda_pin=21):
        """
        Initialize LCD2004 display

        Args:
            i2c_addr: I2C address (default 0x27)
            scl_pin: SCL pin number (default 22)
            sda_pin: SDA pin number (default 21)
        """
        self.i2c_addr = i2c_addr
        self.i2c = I2C(0, scl=Pin(scl_pin), sda=Pin(sda_pin), freq=100000)

        self.backlight_val = self.LCD_BACKLIGHT

        # Initialize display
        sleep(0.05)
        self._write(0x03)
        sleep(0.005)
        self._write(0x03)
        sleep(0.0001)
        self._write(0x03)
        sleep(0.0001)
        self._write(0x02)

        # Configure display
        self._write_command(self.LCD_FUNCTIONSET | self.LCD_2LINE | self.LCD_5x8DOTS | self.LCD_4BITMODE)
        self._write_command(self.LCD_DISPLAYCONTROL | self.LCD_DISPLAYON | self.LCD_CURSOROFF | self.LCD_BLINKOFF)
        self._write_command(self.LCD_CLEARDISPLAY)
        sleep(0.002)
        self._write_command(self.LCD_ENTRYMODESET | self.LCD_ENTRYLEFT | self.LCD_ENTRYSHIFTDECREMENT)

    def _write(self, data):
        """Write data to I2C"""
        self.i2c.writeto(self.i2c_addr, bytearray([data]))

    def _strobe(self, data):
        """Strobe enable bit"""
        self._write(data | self.En | self.backlight_val)
        sleep(0.0005)
        self._write(((data & ~self.En) | self.backlight_val))
        sleep(0.0001)

    def _write_four_bits(self, data):
        """Write 4 bits"""
        self._write(data | self.backlight_val)
        self._strobe(data)

    def _write_command(self, cmd):
        """Write command to LCD"""
        self._write_four_bits(cmd & 0xF0)
        self._write_four_bits((cmd << 4) & 0xF0)

    def _write_data(self, data):
        """Write data to LCD"""
        self._write_four_bits(self.Rs | (data & 0xF0))
        self._write_four_bits(self.Rs | ((data << 4) & 0xF0))

    def clear(self):
        """Clear display"""
        self._write_command(self.LCD_CLEARDISPLAY)
        sleep(0.002)

    def backlight_on(self):
        """Turn on backlight"""
        self.backlight_val = self.LCD_BACKLIGHT
        self._write(self.backlight_val)

    def backlight_off(self):
        """Turn off backlight"""
        self.backlight_val = self.LCD_NOBACKLIGHT
        self._write(self.backlight_val)

    def set_cursor(self, col, row):
        """
        Set cursor position

        Args:
            col: Column (0-19)
            row: Row (0-3)
        """
        row_offsets = [0x00, 0x40, 0x14, 0x54]
        self._write_command(self.LCD_SETDDRAMADDR | (col + row_offsets[row]))

    def write_string(self, text, col=0, row=0):
        """
        Write string to display

        Args:
            text: String to write (max 20 chars)
            col: Start column (0-19)
            row: Row (0-3)
        """
        self.set_cursor(col, row)
        for char in text[:20]:
            self._write_data(ord(char))

    def write_line(self, text, row, center=False):
        """
        Write string to entire line

        Args:
            text: String to write
            row: Row number (0-3)
            center: Center text on line
        """
        # Pad/truncate text to 20 chars
        if center:
            text = text[:20].center(20)
        else:
            text = text[:20].ljust(20)
        self.write_string(text, 0, row)

    def create_char(self, location, charmap):
        """
        Create custom character

        Args:
            location: Character location (0-7)
            charmap: 8-byte array defining character
        """
        location &= 0x7
        self._write_command(self.LCD_SETCGRAMADDR | (location << 3))
        for i in range(8):
            self._write_data(charmap[i])


class RobotDisplayManager:
    """
    High-level display manager for robot information
    Manages LCD2004 display for RobDelivery robot
    """

    def __init__(self, lcd=None):
        """
        Initialize display manager

        Args:
            lcd: LCD2004 instance (creates new if None)
        """
        if lcd is None:
            self.lcd = LCD2004()
        else:
            self.lcd = lcd

        self.current_screen = None
        self.animation_frame = 0

        # Create custom characters
        self._create_custom_chars()

    def _create_custom_chars(self):
        """Create custom characters for display"""
        # Battery icon (char 0)
        battery_full = [
            0b01110,
            0b11111,
            0b11111,
            0b11111,
            0b11111,
            0b11111,
            0b11111,
            0b11111
        ]

        # Delivery box icon (char 1)
        delivery_box = [
            0b11111,
            0b10001,
            0b11111,
            0b10001,
            0b10001,
            0b10001,
            0b10001,
            0b11111
        ]

        # Location pin icon (char 2)
        location_pin = [
            0b00100,
            0b01110,
            0b11111,
            0b11111,
            0b01110,
            0b00100,
            0b00000,
            0b00000
        ]

        # Airplane/Drone icon (char 3)
        drone_icon = [
            0b00100,
            0b01110,
            0b11111,
            0b00100,
            0b01010,
            0b10001,
            0b00000,
            0b00000
        ]

        # Charging icon (char 4)
        charging_icon = [
            0b00100,
            0b00110,
            0b01111,
            0b00110,
            0b00100,
            0b01100,
            0b11100,
            0b01100
        ]

        # WiFi icon (char 5)
        wifi_icon = [
            0b00000,
            0b01110,
            0b10001,
            0b00100,
            0b01010,
            0b00000,
            0b00100,
            0b00000
        ]

        # OK checkmark (char 6)
        ok_icon = [
            0b00000,
            0b00001,
            0b00011,
            0b10110,
            0b11100,
            0b01000,
            0b00000,
            0b00000
        ]

        # Error icon (char 7)
        error_icon = [
            0b00000,
            0b10001,
            0b01010,
            0b00100,
            0b01010,
            0b10001,
            0b00000,
            0b00000
        ]

        self.lcd.create_char(0, battery_full)
        self.lcd.create_char(1, delivery_box)
        self.lcd.create_char(2, location_pin)
        self.lcd.create_char(3, drone_icon)
        self.lcd.create_char(4, charging_icon)
        self.lcd.create_char(5, wifi_icon)
        self.lcd.create_char(6, ok_icon)
        self.lcd.create_char(7, error_icon)

    def display_boot(self, message="Booting..."):
        """Display boot screen"""
        self.lcd.clear()
        self.lcd.write_line("RobDelivery System", 0, center=True)
        self.lcd.write_line("IoT Robot v1.0", 1, center=True)
        self.lcd.write_line("", 2)
        self.lcd.write_line(message, 3, center=True)
        self.current_screen = "boot"

    def display_wifi_connecting(self, ssid=""):
        """Display WiFi connection screen"""
        self.lcd.clear()
        self.lcd.write_string(chr(5) + " WiFi Connecting", 0, 0)
        self.lcd.write_line(ssid[:20], 1)
        self.lcd.write_line("Please wait...", 2, center=True)
        self.current_screen = "wifi_connecting"

    def display_wifi_connected(self, ssid="", ip=""):
        """Display WiFi connected screen"""
        self.lcd.clear()
        self.lcd.write_string(chr(5) + chr(6) + " WiFi Connected", 0, 0)
        self.lcd.write_line(ssid[:20], 1)
        self.lcd.write_line("IP: " + ip[:16], 2)
        self.current_screen = "wifi_connected"

    def display_auth(self, status="Authenticating..."):
        """Display authentication screen"""
        self.lcd.clear()
        self.lcd.write_line("Server Auth", 0, center=True)
        self.lcd.write_line(status[:20], 1, center=True)
        self.current_screen = "auth"

    def display_initialization(self, step="", progress=""):
        """Display initialization progress"""
        self.lcd.clear()
        self.lcd.write_line("Initializing...", 0, center=True)
        self.lcd.write_line(step[:20], 1)
        if progress:
            self.lcd.write_line(progress[:20], 2, center=True)
        self.current_screen = "init"

    def display_main_status(self, robot, fsm_state=""):
        """
        Display main robot status

        Args:
            robot: Robot instance
            fsm_state: Current FSM state name
        """
        self.lcd.clear()

        # Line 1: Robot ID and Status
        line1 = "R#{} {}".format(
            robot.robot_id or "?",
            robot.status[:12]
        )
        self.lcd.write_line(line1, 0)

        # Line 2: Battery and location
        battery_icon = chr(0) if robot.battery_level > 20 else chr(4)
        line2 = "{}{}% {:.4f}".format(
            battery_icon,
            int(robot.battery_level),
            robot.current_latitude or 0.0
        )
        self.lcd.write_line(line2, 1)

        # Line 3: FSM State
        state_display = self._format_fsm_state(fsm_state)
        if robot.status == "Charging":
            state_display = chr(4) + " CHARGING"
        elif robot.status == "Idle":
            state_display = chr(6) + " IDLE"
        self.lcd.write_line(state_display[:20], 2)

        # Line 4: Order info or coordinates
        if robot.current_order_id:
            line4 = chr(1) + " Order #{}".format(robot.current_order_id)
        else:
            line4 = chr(2) + " {:.4f}".format(robot.current_longitude or 0.0)
        self.lcd.write_line(line4, 3)

        self.current_screen = "main_status"

    def display_fsm_state(self, fsm_state, robot):
        """
        Display detailed FSM state information

        Args:
            fsm_state: Current FSM state
            robot: Robot instance
        """
        self.lcd.clear()

        # Line 1: State name
        state_name = self._format_fsm_state(fsm_state)
        self.lcd.write_line(state_name, 0, center=True)

        # Line 2: Battery
        self.lcd.write_string(chr(0) + " Battery: {}%".format(int(robot.battery_level)), 0, 1)

        # Line 3: Position
        if robot.current_node_id:
            self.lcd.write_string(chr(2) + " Node: #{}".format(robot.current_node_id), 0, 2)
        else:
            self.lcd.write_string(chr(2) + " GPS Navigation", 0, 2)

        # Line 4: Order
        if robot.current_order_id:
            self.lcd.write_string(chr(1) + " Order #{}".format(robot.current_order_id), 0, 3)
        else:
            self.lcd.write_line("No active order", 3, center=True)

        self.current_screen = "fsm_state"

    def display_flight_info(self, fsm_state, robot, distance=None):
        """
        Display flight information

        Args:
            fsm_state: Current FSM state
            robot: Robot instance
            distance: Distance to destination (optional)
        """
        self.lcd.clear()

        # Line 1: Flight state
        if "PICKUP" in fsm_state:
            line1 = chr(3) + " To Pickup Point"
        elif "DROPOFF" in fsm_state:
            line1 = chr(3) + " To Dropoff Point"
        elif "CHARGING" in fsm_state:
            line1 = chr(3) + " To Charging"
        else:
            line1 = chr(3) + " In Flight"
        self.lcd.write_line(line1, 0)

        # Line 2: Current position
        line2 = "Lat: {:.5f}".format(robot.current_latitude or 0.0)
        self.lcd.write_line(line2, 1)

        line3 = "Lon: {:.5f}".format(robot.current_longitude or 0.0)
        self.lcd.write_line(line3, 2)

        # Line 4: Battery or distance
        if distance:
            line4 = chr(0) + "{}% Dist:{}m".format(int(robot.battery_level), int(distance))
        else:
            line4 = chr(0) + " Battery: {}%".format(int(robot.battery_level))
        self.lcd.write_line(line4, 3)

        self.current_screen = "flight_info"

    def display_at_location(self, location_type, robot):
        """
        Display arrival at location

        Args:
            location_type: "pickup", "dropoff", or "charging"
            robot: Robot instance
        """
        self.lcd.clear()

        # Line 1: Location type
        if location_type == "pickup":
            line1 = chr(2) + chr(6) + " AT PICKUP POINT"
        elif location_type == "dropoff":
            line1 = chr(2) + chr(6) + " AT DROPOFF POINT"
        elif location_type == "charging":
            line1 = chr(2) + chr(6) + " AT CHARGING"
        else:
            line1 = chr(2) + " ARRIVED"
        self.lcd.write_line(line1, 0)

        # Line 2: Node info
        if robot.current_node_id:
            line2 = "Node: #{}".format(robot.current_node_id)
        else:
            line2 = "Location confirmed"
        self.lcd.write_line(line2, 1, center=True)

        # Line 3: Battery
        line3 = chr(0) + " Battery: {}%".format(int(robot.battery_level))
        self.lcd.write_line(line3, 2)

        # Line 4: Order
        if robot.current_order_id:
            line4 = chr(1) + " Order #{}".format(robot.current_order_id)
            self.lcd.write_line(line4, 3)

        self.current_screen = "at_location"

    def display_compartment(self, action, robot):
        """
        Display compartment action

        Args:
            action: "opening" or "closing"
            robot: Robot instance
        """
        self.lcd.clear()

        # Line 1: Action
        if action == "opening":
            line1 = ">>> OPENING <<<"
        elif action == "closing":
            line1 = "<<< CLOSING >>>"
        else:
            line1 = "COMPARTMENT"
        self.lcd.write_line(line1, 0, center=True)

        # Line 2: Compartment status
        self.lcd.write_line("Compartment Door", 1, center=True)

        # Line 3: Order
        if robot.current_order_id:
            line3 = chr(1) + " Order #{}".format(robot.current_order_id)
            self.lcd.write_line(line3, 2)

        # Line 4: Battery
        line4 = chr(0) + " {}%".format(int(robot.battery_level))
        self.lcd.write_line(line4, 3)

        self.current_screen = "compartment"

    def display_loading(self, robot, elapsed_seconds=0):
        """
        Display loading status

        Args:
            robot: Robot instance
            elapsed_seconds: Seconds elapsed
        """
        self.lcd.clear()

        # Line 1: Loading message
        self.lcd.write_line(chr(1) + " LOADING PACKAGE", 0)

        # Line 2: Progress bar
        progress = min(elapsed_seconds / 5.0, 1.0)
        bar_length = int(progress * 20)
        bar = "=" * bar_length
        line2 = bar.ljust(20)
        self.lcd.write_line(line2, 1)

        # Line 3: Time
        line3 = "Time: {}s / 5s".format(elapsed_seconds)
        self.lcd.write_line(line3, 2, center=True)

        # Line 4: Order
        if robot.current_order_id:
            line4 = "Order #{}".format(robot.current_order_id)
            self.lcd.write_line(line4, 3, center=True)

        self.current_screen = "loading"

    def display_wait_for_pickup(self, robot, elapsed_seconds=0):
        """
        Display waiting for recipient

        Args:
            robot: Robot instance
            elapsed_seconds: Seconds elapsed
        """
        self.lcd.clear()

        # Line 1: Message
        self.lcd.write_line("WAITING FOR", 0, center=True)
        self.lcd.write_line("RECIPIENT", 1, center=True)

        # Line 2: Timer
        remaining = max(0, 10 - elapsed_seconds)
        line3 = "Timeout: {}s".format(remaining)
        self.lcd.write_line(line3, 2, center=True)

        # Line 4: Instruction
        self.lcd.write_line("Press button ->", 3)

        self.current_screen = "wait_pickup"

    def display_delivered(self, robot):
        """
        Display delivery complete

        Args:
            robot: Robot instance
        """
        self.lcd.clear()

        # Line 1-2: Success message
        self.lcd.write_line(chr(6) + chr(6) + " PACKAGE " + chr(6) + chr(6), 0, center=True)
        self.lcd.write_line("DELIVERED!", 1, center=True)

        # Line 3: Order completed
        if robot.current_order_id:
            line3 = chr(1) + " Order #{}".format(robot.current_order_id)
            self.lcd.write_line(line3, 2)

        # Line 4: Status
        self.lcd.write_line("Returning home...", 3, center=True)

        self.current_screen = "delivered"

    def display_charging(self, robot):
        """
        Display charging status

        Args:
            robot: Robot instance
        """
        self.lcd.clear()

        # Line 1: Charging
        self.lcd.write_line(chr(4) + " CHARGING", 0, center=True)

        # Line 2: Battery percentage with bar
        battery_pct = int(robot.battery_level)
        bar_length = int((battery_pct / 100.0) * 20)
        line2 = "=" * bar_length
        self.lcd.write_line(line2.ljust(20), 1)

        # Line 3: Percentage
        line3 = "{}%".format(battery_pct)
        self.lcd.write_line(line3, 2, center=True)

        # Line 4: Status
        if battery_pct >= 95:
            line4 = chr(6) + " Ready for orders"
        elif battery_pct == 100:
            line4 = chr(6) + " Fully charged"
        else:
            line4 = "Charging..."
        self.lcd.write_line(line4, 3, center=True)

        self.current_screen = "charging"

    def display_error(self, error_message, robot=None):
        """
        Display error message

        Args:
            error_message: Error description
            robot: Robot instance (optional)
        """
        self.lcd.clear()

        # Line 1: Error header
        self.lcd.write_line(chr(7) + " ERROR " + chr(7), 0, center=True)

        # Line 2-3: Error message
        if len(error_message) <= 20:
            self.lcd.write_line(error_message[:20], 1, center=True)
        else:
            self.lcd.write_line(error_message[:20], 1)
            self.lcd.write_line(error_message[20:40], 2)

        # Line 4: Recovery message
        if robot:
            line4 = "Battery: {}%".format(int(robot.battery_level))
            self.lcd.write_line(line4, 3, center=True)
        else:
            self.lcd.write_line("Recovering...", 3, center=True)

        self.current_screen = "error"

    def display_order_check(self, robot):
        """
        Display checking for orders

        Args:
            robot: Robot instance
        """
        self.lcd.clear()

        # Line 1: Status
        self.lcd.write_line("Checking Orders", 0, center=True)

        # Line 2: Animation
        animations = ["|", "/", "-", "\\"]
        anim = animations[self.animation_frame % 4]
        self.lcd.write_line(anim, 1, center=True)
        self.animation_frame += 1

        # Line 3: Robot status
        line3 = "Status: {}".format(robot.status[:12])
        self.lcd.write_line(line3, 2)

        # Line 4: Battery
        line4 = chr(0) + " {}%".format(int(robot.battery_level))
        self.lcd.write_line(line4, 3)

        self.current_screen = "order_check"

    def display_motors(self, state, robot):
        """
        Display motor status

        Args:
            state: "starting" or "stopping"
            robot: Robot instance
        """
        self.lcd.clear()

        # Line 1: Motor state
        if state == "starting":
            line1 = chr(3) + " MOTORS STARTING"
        elif state == "stopping":
            line1 = chr(3) + " MOTORS STOPPING"
        else:
            line1 = chr(3) + " MOTORS"
        self.lcd.write_line(line1, 0)

        # Line 2: Status
        self.lcd.write_line("Propellers active", 1, center=True)

        # Line 3: Battery
        line3 = chr(0) + " Battery: {}%".format(int(robot.battery_level))
        self.lcd.write_line(line3, 2)

        # Line 4: Order
        if robot.current_order_id:
            line4 = chr(1) + " Order #{}".format(robot.current_order_id)
            self.lcd.write_line(line4, 3)

        self.current_screen = "motors"

    def _format_fsm_state(self, fsm_state):
        """
        Format FSM state name for display

        Args:
            fsm_state: FSM state string

        Returns:
            str: Formatted state name (max 20 chars)
        """
        # Map states to display names
        state_names = {
            "IDLE": "IDLE",
            "CHECK_ORDERS": "Checking Orders",
            "ORDER_ASSIGNED": chr(1) + " Order Assigned",
            "MOTORS_ON": chr(3) + " Motors ON",
            "FLIGHT_TO_PICKUP": chr(3) + " To Pickup",
            "AT_PICKUP": chr(2) + " At Pickup",
            "OPEN_COMPARTMENT_PICKUP": "Opening Bay",
            "LOADING": chr(1) + " Loading",
            "CLOSE_COMPARTMENT_PICKUP": "Closing Bay",
            "FLIGHT_TO_DROPOFF": chr(3) + " To Delivery",
            "AT_DROPOFF": chr(2) + " At Dropoff",
            "OPEN_COMPARTMENT_DROPOFF": "Opening Bay",
            "WAIT_FOR_PICKUP": "Waiting Customer",
            "PACKAGE_DELIVERED": chr(6) + " Delivered!",
            "CLOSE_COMPARTMENT_DROPOFF": "Closing Bay",
            "FLIGHT_TO_CHARGING": chr(3) + " To Charge",
            "AT_CHARGING_STATION": chr(2) + " At Charger",
            "CHARGING": chr(4) + " Charging",
            "ERROR": chr(7) + " ERROR"
        }

        return state_names.get(fsm_state, fsm_state[:20])

    def clear(self):
        """Clear display"""
        self.lcd.clear()

    def backlight_on(self):
        """Turn on backlight"""
        self.lcd.backlight_on()

    def backlight_off(self):
        """Turn off backlight"""
        self.lcd.backlight_off()
