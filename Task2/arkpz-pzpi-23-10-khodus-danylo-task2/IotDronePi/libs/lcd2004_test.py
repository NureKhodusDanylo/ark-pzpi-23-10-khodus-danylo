"""
LCD2004 Display Test Script
Demonstrates all display screens and features
"""

import sys
sys.path.append('/libs')
sys.path.append('/core')

from lcd2004 import RobotDisplayManager, LCD2004
from robot import Robot
from time import sleep

def test_basic_lcd():
    """Test basic LCD functionality"""
    print("Testing basic LCD...")
    lcd = LCD2004()

    # Test 1: Basic text
    lcd.clear()
    lcd.write_line("LCD2004 Test", 0, center=True)
    lcd.write_line("Line 1", 1)
    lcd.write_line("Line 2", 2)
    lcd.write_line("Line 3", 3)
    sleep(2)

    # Test 2: Custom characters
    lcd.clear()
    lcd.write_string(chr(0) + " Battery", 0, 0)
    lcd.write_string(chr(1) + " Package", 0, 1)
    lcd.write_string(chr(2) + " Location", 0, 2)
    lcd.write_string(chr(3) + " Drone", 0, 3)
    sleep(2)

    # Test 3: More icons
    lcd.clear()
    lcd.write_string(chr(4) + " Charging", 0, 0)
    lcd.write_string(chr(5) + " WiFi", 0, 1)
    lcd.write_string(chr(6) + " OK", 0, 2)
    lcd.write_string(chr(7) + " Error", 0, 3)
    sleep(2)

    print("Basic LCD test complete!")


def test_display_manager():
    """Test RobotDisplayManager screens"""
    print("Testing RobotDisplayManager...")

    # Create display manager and mock robot
    display = RobotDisplayManager()
    robot = Robot(robot_id=12)
    robot.name = "TestBot"
    robot.battery_level = 85.0
    robot.current_latitude = 50.4431
    robot.current_longitude = 30.5234
    robot.current_node_id = 25
    robot.current_order_id = 456
    robot.set_status("Delivering")

    # Test 1: Boot screen
    print("1. Boot screen")
    display.display_boot("Initializing...")
    sleep(2)

    # Test 2: WiFi screens
    print("2. WiFi connecting")
    display.display_wifi_connecting("RobDelivery_Net")
    sleep(2)

    print("3. WiFi connected")
    display.display_wifi_connected("RobDelivery_Net", "192.168.1.100")
    sleep(2)

    # Test 3: Auth screen
    print("4. Authentication")
    display.display_auth("Authenticating...")
    sleep(1)
    display.display_auth("Authenticated OK")
    sleep(2)

    # Test 4: Initialization
    print("5. Initialization")
    display.display_initialization("Init Managers", "Battery, GPS...")
    sleep(2)
    display.display_initialization("Fetch Robot Info", "Connecting...")
    sleep(2)

    # Test 5: Main status screen
    print("6. Main status")
    display.display_main_status(robot, "IDLE")
    sleep(3)

    # Test 6: Order checking
    print("7. Checking orders")
    for i in range(4):
        display.display_order_check(robot)
        sleep(0.5)

    # Test 7: Order assigned
    print("8. Order assigned")
    display.display_fsm_state("ORDER_ASSIGNED", robot)
    sleep(2)

    # Test 8: Motors starting
    print("9. Motors starting")
    display.display_motors("starting", robot)
    sleep(2)

    # Test 9: Flight to pickup
    print("10. Flight to pickup")
    display.display_flight_info("FLIGHT_TO_PICKUP", robot, distance=1250)
    sleep(3)

    # Test 10: At pickup
    print("11. At pickup")
    display.display_at_location("pickup", robot)
    sleep(2)

    # Test 11: Opening compartment
    print("12. Opening compartment")
    display.display_compartment("opening", robot)
    sleep(2)

    # Test 12: Loading with progress
    print("13. Loading package")
    for i in range(6):
        display.display_loading(robot, i)
        sleep(1)

    # Test 13: Closing compartment
    print("14. Closing compartment")
    display.display_compartment("closing", robot)
    sleep(2)

    # Test 14: Flight to dropoff
    print("15. Flight to dropoff")
    display.display_flight_info("FLIGHT_TO_DROPOFF", robot, distance=2300)
    sleep(3)

    # Test 15: At dropoff
    print("16. At dropoff")
    display.display_at_location("dropoff", robot)
    sleep(2)

    # Test 16: Opening compartment
    print("17. Opening compartment")
    display.display_compartment("opening", robot)
    sleep(2)

    # Test 17: Waiting for pickup
    print("18. Waiting for recipient")
    for i in range(11):
        display.display_wait_for_pickup(robot, i)
        sleep(0.8)

    # Test 18: Package delivered
    print("19. Package delivered")
    display.display_delivered(robot)
    sleep(3)

    # Test 19: Closing compartment
    print("20. Closing compartment")
    display.display_compartment("closing", robot)
    sleep(2)

    # Test 20: Flight to charging
    print("21. Flight to charging")
    display.display_flight_info("FLIGHT_TO_CHARGING", robot)
    sleep(3)

    # Test 21: At charging station
    print("22. At charging station")
    display.display_at_location("charging", robot)
    sleep(2)

    # Test 22: Charging with increasing battery
    print("23. Charging")
    for battery in range(85, 101, 3):
        robot.battery_level = battery
        display.display_charging(robot)
        sleep(1)

    # Test 23: Fully charged and ready
    print("24. Fully charged")
    robot.battery_level = 100
    display.display_charging(robot)
    sleep(3)

    # Test 24: Back to idle
    print("25. Back to idle")
    robot.set_status("Idle")
    robot.current_order_id = None
    display.display_main_status(robot, "IDLE")
    sleep(3)

    # Test 25: Error screen
    print("26. Error screen")
    display.display_error("Connection lost", robot)
    sleep(3)

    # Final: Show complete message
    display.lcd.clear()
    display.lcd.write_line("All Tests", 0, center=True)
    display.lcd.write_line("Complete!", 1, center=True)
    display.lcd.write_string(chr(6) + "  " + chr(6), 2, center=True)
    sleep(3)

    print("Display manager test complete!")


def test_all_fsm_states():
    """Test all FSM state displays"""
    print("Testing all FSM states...")

    display = RobotDisplayManager()
    robot = Robot(robot_id=7)
    robot.name = "DroneBot"
    robot.battery_level = 75.0
    robot.current_latitude = 50.4431
    robot.current_longitude = 30.5234
    robot.current_node_id = 15
    robot.current_order_id = 789

    states = [
        "IDLE",
        "CHECK_ORDERS",
        "ORDER_ASSIGNED",
        "MOTORS_ON",
        "FLIGHT_TO_PICKUP",
        "AT_PICKUP",
        "OPEN_COMPARTMENT_PICKUP",
        "LOADING",
        "CLOSE_COMPARTMENT_PICKUP",
        "FLIGHT_TO_DROPOFF",
        "AT_DROPOFF",
        "OPEN_COMPARTMENT_DROPOFF",
        "WAIT_FOR_PICKUP",
        "PACKAGE_DELIVERED",
        "CLOSE_COMPARTMENT_DROPOFF",
        "FLIGHT_TO_CHARGING",
        "AT_CHARGING_STATION",
        "CHARGING",
        "ERROR"
    ]

    for state in states:
        print(f"State: {state}")
        display.lcd.clear()
        state_display = display._format_fsm_state(state)
        display.lcd.write_line(state_display, 0, center=True)
        display.lcd.write_line(f"Battery: {int(robot.battery_level)}%", 1)
        if robot.current_order_id:
            display.lcd.write_line(f"Order #{robot.current_order_id}", 2)
        sleep(1.5)

    print("FSM states test complete!")


def demo_delivery_cycle():
    """Demonstrate complete delivery cycle"""
    print("\n" + "="*50)
    print("FULL DELIVERY CYCLE DEMONSTRATION")
    print("="*50 + "\n")

    display = RobotDisplayManager()
    robot = Robot(robot_id=42)
    robot.name = "DeliveryBot-42"
    robot.battery_level = 95.0
    robot.current_latitude = 50.4431
    robot.current_longitude = 30.5234
    robot.set_status("Idle")

    # Phase 1: Idle and waiting
    print("Phase 1: Robot idle, checking for orders...")
    robot.current_node_id = 25
    display.display_main_status(robot, "IDLE")
    sleep(3)

    # Phase 2: Order received
    print("Phase 2: New order received!")
    robot.current_order_id = 12345
    display.display_fsm_state("ORDER_ASSIGNED", robot)
    sleep(2)

    # Phase 3: Starting delivery
    print("Phase 3: Starting motors and taking off...")
    robot.set_status("Delivering")
    display.display_motors("starting", robot)
    sleep(2)

    # Phase 4: Flying to pickup
    print("Phase 4: Flying to pickup location...")
    for i in range(3):
        robot.battery_level -= 2
        robot.current_latitude += 0.001
        display.display_flight_info("FLIGHT_TO_PICKUP", robot, distance=1500-(i*500))
        sleep(2)

    # Phase 5: Pickup
    print("Phase 5: Arriving at pickup...")
    robot.current_node_id = 30
    display.display_at_location("pickup", robot)
    sleep(2)

    print("Phase 5b: Opening compartment...")
    display.display_compartment("opening", robot)
    sleep(2)

    print("Phase 5c: Loading package...")
    for i in range(6):
        display.display_loading(robot, i)
        sleep(0.8)

    # Phase 6: Flying to dropoff
    print("Phase 6: Flying to delivery location...")
    for i in range(3):
        robot.battery_level -= 3
        robot.current_longitude += 0.001
        display.display_flight_info("FLIGHT_TO_DROPOFF", robot, distance=2000-(i*600))
        sleep(2)

    # Phase 7: Delivery
    print("Phase 7: Arriving at dropoff...")
    robot.current_node_id = 45
    display.display_at_location("dropoff", robot)
    sleep(2)

    print("Phase 7b: Opening compartment...")
    display.display_compartment("opening", robot)
    sleep(2)

    print("Phase 7c: Waiting for recipient...")
    for i in range(8):
        display.display_wait_for_pickup(robot, i)
        sleep(0.8)

    print("Phase 7d: Package delivered!")
    display.display_delivered(robot)
    sleep(3)

    # Phase 8: Return to charging
    print("Phase 8: Returning to charging station...")
    robot.set_status("Returning")
    robot.current_order_id = None
    for i in range(2):
        robot.battery_level -= 2
        display.display_flight_info("FLIGHT_TO_CHARGING", robot, distance=1000-(i*500))
        sleep(2)

    # Phase 9: Charging
    print("Phase 9: Charging battery...")
    robot.set_status("Charging")
    robot.current_node_id = 25
    display.display_at_location("charging", robot)
    sleep(2)

    for battery in range(int(robot.battery_level), 101, 5):
        robot.battery_level = battery
        display.display_charging(robot)
        sleep(1.5)

    # Phase 10: Ready for next delivery
    print("Phase 10: Fully charged and ready!")
    robot.battery_level = 100
    robot.set_status("Idle")
    display.display_main_status(robot, "IDLE")
    sleep(3)

    print("\n" + "="*50)
    print("DELIVERY CYCLE COMPLETE!")
    print("="*50 + "\n")


def main():
    """Main test function"""
    print("\n" + "="*60)
    print(" LCD2004 Display Library - Comprehensive Test Suite")
    print("="*60 + "\n")

    try:
        # Test 1: Basic LCD
        print("\n[Test 1] Basic LCD Functionality")
        print("-" * 40)
        test_basic_lcd()

        sleep(2)

        # Test 2: Display Manager
        print("\n[Test 2] Display Manager All Screens")
        print("-" * 40)
        test_display_manager()

        sleep(2)

        # Test 3: FSM States
        print("\n[Test 3] All FSM States")
        print("-" * 40)
        test_all_fsm_states()

        sleep(2)

        # Test 4: Full delivery cycle demo
        demo_delivery_cycle()

        # Success message
        lcd = LCD2004()
        lcd.clear()
        lcd.write_line("All Tests Passed!", 0, center=True)
        lcd.write_line(chr(6) + " " + chr(6) + " " + chr(6) + " " + chr(6), 1, center=True)
        lcd.write_line("LCD2004 Ready", 2, center=True)
        lcd.write_line("for Operation", 3, center=True)

        print("\n" + "="*60)
        print(" ALL TESTS COMPLETED SUCCESSFULLY!")
        print("="*60 + "\n")

    except Exception as e:
        print(f"\nERROR: {str(e)}")
        lcd = LCD2004()
        lcd.clear()
        lcd.write_line(chr(7) + " TEST FAILED " + chr(7), 0, center=True)
        lcd.write_line(str(e)[:20], 1)


if __name__ == "__main__":
    main()
