import urequests
import ujson
import sys

sys.path.append('/config')
sys.path.append('/utils')

from config import API_CONFIG, DEBUG
from helpers import log_message

class OrderManager:
    """
    Manages robot orders and delivery missions
    """

    def __init__(self, robot, auth_manager):
        self.robot = robot
        self.auth_manager = auth_manager
        self.base_url = API_CONFIG["BASE_URL"]
        self.current_order = None
        self.pickup_coordinates = None
        self.dropoff_coordinates = None
        self.route_waypoints = None

    def fetch_assigned_orders(self):
        """
        Fetch orders assigned to this robot from server

        Returns:
            list: List of assigned orders or empty list
        """
        if not self.auth_manager.is_authenticated():
            log_message("Cannot fetch orders: Not authenticated", "WARNING")
            return []

        try:
            # Build request URL
            url = "{}/api/Robot/my-orders".format(self.base_url)

            headers = {
                "Authorization": "Bearer {}".format(self.auth_manager.get_token()),
                "Content-Type": "application/json"
            }

            if DEBUG:
                log_message("Fetching orders from: {}".format(url), "DEBUG")

            # Make POST request with empty body
            response = urequests.post(url, headers=headers, data="{}")

            if response.status_code == 200:
                orders = ujson.loads(response.text)
                log_message("Fetched {} order(s) from server".format(len(orders)))
                return orders
            else:
                log_message(
                    "Failed to fetch orders: {} - {}".format(response.status_code, response.text),
                    "ERROR"
                )
                return []

        except Exception as e:
            log_message("Error fetching orders: {}".format(str(e)), "ERROR")
            return []
        finally:
            if 'response' in locals():
                response.close()

    def accept_order(self, order_id):
        """
        Accept an assigned order from server

        Args:
            order_id: Order ID to accept

        Returns:
            bool: True if accepted successfully
        """
        if not self.auth_manager.is_authenticated():
            log_message("Cannot accept order: Not authenticated", "WARNING")
            return False

        try:
            # Build request URL
            url = "{}/api/Robot/order/{}/accept".format(self.base_url, order_id)

            # Build headers with JWT token
            headers = {
                "Authorization": "Bearer {}".format(self.auth_manager.get_token()),
                "Content-Type": "application/json"
            }

            log_message("Accepting order {}...".format(order_id))

            if DEBUG:
                log_message("POST URL: {}".format(url), "DEBUG")
                log_message("Token (first 20 chars): {}...".format(self.auth_manager.get_token()[:20]), "DEBUG")

            # Make POST request
            response = urequests.post(url, headers=headers)

            if DEBUG:
                log_message("Response status: {}".format(response.status_code), "DEBUG")
                log_message("Response body: {}".format(response.text), "DEBUG")

            if response.status_code == 200:
                result = ujson.loads(response.text)
                log_message("Order {} accepted: {}".format(order_id, result.get("message", "")))
                return True
            else:
                log_message(
                    "Failed to accept order: {} - {}".format(response.status_code, response.text),
                    "ERROR"
                )
                return False

        except Exception as e:
            log_message("Error accepting order: {}".format(str(e)), "ERROR")
            return False
        finally:
            if 'response' in locals():
                response.close()

    def start_order(self, order_data):
        """
        Start a delivery order from server data

        Args:
            order_data: Order data from server (OrderAssignmentDTO)

        Returns:
            bool: True if order started, False otherwise
        """
        # Allow starting order if robot is Idle or Delivering (server may have already set status)
        if self.robot.status != "Idle" and self.robot.status != "Delivering":
            log_message("Cannot start order: Robot is busy with status {}".format(self.robot.status), "WARNING")
            return False

        if self.robot.is_battery_low():
            log_message("Cannot start order: Battery too low", "WARNING")
            return False

        # Extract order information
        order_id = order_data.get("orderId")
        pickup_lat = order_data.get("pickupLatitude")
        pickup_lon = order_data.get("pickupLongitude")
        dropoff_lat = order_data.get("dropoffLatitude")
        dropoff_lon = order_data.get("dropoffLongitude")
        route = order_data.get("route", [])

        self.current_order = {
            "id": order_id,
            "name": order_data.get("orderName", ""),
            "weight": order_data.get("weight", 0),
            "pickup": {"lat": pickup_lat, "lon": pickup_lon, "nodeId": order_data.get("pickupNodeId")},
            "dropoff": {"lat": dropoff_lat, "lon": dropoff_lon, "nodeId": order_data.get("dropoffNodeId")},
            "totalDistance": order_data.get("totalDistanceMeters", 0),
            "batteryUsage": order_data.get("estimatedBatteryUsagePercent", 0)
        }

        self.pickup_coordinates = (pickup_lat, pickup_lon)
        self.dropoff_coordinates = (dropoff_lat, dropoff_lon)
        self.route_waypoints = route

        self.robot.set_status("Delivering")

        log_message("Order {} started: {} ({} kg)".format(
            order_id, self.current_order["name"], self.current_order["weight"]
        ))
        log_message("Route: {} waypoints, {:.0f}m total, {:.1f}% battery".format(
            len(route), self.current_order["totalDistance"], self.current_order["batteryUsage"]
        ))

        return True

    def update_order_phase(self, phase_name, latitude=None, longitude=None):
        """
        Update order delivery phase on server

        Args:
            phase_name: Phase name (FLIGHT_TO_PICKUP, AT_PICKUP, etc.)
            latitude: Current latitude (optional)
            longitude: Current longitude (optional)

        Returns:
            bool: True if updated successfully
        """
        if not self.current_order:
            log_message("Cannot update phase: No active order", "WARNING")
            return False

        if not self.auth_manager.is_authenticated():
            log_message("Cannot update phase: Not authenticated", "WARNING")
            return False

        try:
            order_id = self.current_order["id"]

            # Build request URL
            url = "{}/api/Robot/order/{}/phase".format(self.base_url, order_id)

            # Build headers with JWT token
            headers = {
                "Authorization": "Bearer {}".format(self.auth_manager.get_token()),
                "Content-Type": "application/json"
            }

            # Build request body
            body = {
                "phase": phase_name,
                "latitude": latitude if latitude else self.robot.current_latitude,
                "longitude": longitude if longitude else self.robot.current_longitude,
                "timestamp": "{}Z".format(self._get_utc_timestamp())
            }

            log_message("Updating order phase to: {}".format(phase_name))

            # Make POST request
            response = urequests.post(url, headers=headers, data=ujson.dumps(body))

            if response.status_code == 200:
                log_message("Order phase updated successfully")
                return True
            else:
                log_message(
                    "Failed to update phase: {} - {}".format(response.status_code, response.text),
                    "ERROR"
                )
                return False

        except Exception as e:
            log_message("Error updating order phase: {}".format(str(e)), "ERROR")
            return False
        finally:
            if 'response' in locals():
                response.close()

    def _get_utc_timestamp(self):
        """
        Get current UTC timestamp in ISO format

        Returns:
            str: ISO formatted timestamp
        """
        import time
        # Get current time tuple
        t = time.localtime()
        # Format as ISO 8601 (simple version without timezone offset calculation)
        return "{:04d}-{:02d}-{:02d}T{:02d}:{:02d}:{:02d}".format(
            t[0], t[1], t[2], t[3], t[4], t[5]
        )

    def get_pickup_coordinates(self):
        """
        Get pickup coordinates

        Returns:
            tuple: (latitude, longitude) or None
        """
        return self.pickup_coordinates

    def get_dropoff_coordinates(self):
        """
        Get dropoff coordinates

        Returns:
            tuple: (latitude, longitude) or None
        """
        return self.dropoff_coordinates

    def get_route_waypoints(self):
        """
        Get route waypoints from server

        Returns:
            list: List of waypoint dicts or None
        """
        return self.route_waypoints

    def complete_order(self):
        """
        Complete current order
        """
        if self.current_order:
            order_id = self.current_order["id"]
            log_message("Order {} completed".format(order_id))

            self.current_order = None
            self.pickup_coordinates = None
            self.dropoff_coordinates = None

            self.robot.complete_delivery()

    def cancel_order(self, reason="Unknown"):
        """
        Cancel current order

        Args:
            reason: Cancellation reason
        """
        if self.current_order:
            order_id = self.current_order["id"]
            log_message("Order {} cancelled: {}".format(order_id, reason), "WARNING")

            self.current_order = None
            self.pickup_coordinates = None
            self.dropoff_coordinates = None

            self.robot.set_status("Idle")

    def has_active_order(self):
        """
        Check if robot has an active order

        Returns:
            bool: True if has active order, False otherwise
        """
        return self.current_order is not None

    def get_current_order_id(self):
        """
        Get current order ID

        Returns:
            int: Order ID or None if no active order
        """
        if self.current_order:
            return self.current_order["id"]
        return None

    def get_order_phase(self):
        """
        Get current order phase

        Returns:
            str: Order phase or None if no active order
        """
        if self.current_order:
            return self.current_order["phase"]
        return None
