# RobDeliveryAPI - REST API Specification

## Base Information

- **Base URL**: `http://localhost:5102/api`
- **API Version**: 1.0
- **Protocol**: HTTPS (HTTP in development)
- **Authentication**: JWT Bearer Token
- **Content-Type**: application/json

---

## Authentication

### JWT Token Format
```
Authorization: Bearer <JWT_TOKEN>
```

### Token Claims
- `Id`: User ID (integer)
- `Role`: User role (User, Partner, Admin, Iot)
- `exp`: Expiration timestamp

---

## API Endpoints

### 1. User Controller (`/api/User`)

#### 1.1 Register User
- **Endpoint**: `POST /api/User/register`
- **Auth Required**: No
- **Description**: Register new user with password or Google OAuth

**Request Body (Password-based)**:
```json
{
  "userName": "string",
  "email": "string",
  "password": "string",
  "phoneNumber": "string (optional)"
}
```

**Request Body (Google OAuth)**:
```json
{
  "userName": "string",
  "email": "string",
  "googleIdToken": "string"
}
```

**Response**: `200 OK`
```json
{
  "status": "Success | UserAlreadyExists | InvalidData",
  "message": "string",
  "userId": "integer (optional)"
}
```

---

#### 1.2 Login User
- **Endpoint**: `POST /api/User/login`
- **Auth Required**: No
- **Description**: Authenticate user and receive JWT token

**Request Body (Password-based)**:
```json
{
  "email": "string",
  "password": "string"
}
```

**Request Body (Google OAuth)**:
```json
{
  "email": "string",
  "googleIdToken": "string"
}
```

**Response**: `200 OK`
```json
{
  "status": "Success | InvalidCredentials | UserNotFound",
  "token": "string (JWT token)",
  "userId": "integer",
  "userName": "string",
  "email": "string"
}
```

---

### 2. Order Controller (`/api/Order`)

#### 2.1 Create Order
- **Endpoint**: `POST /api/Order`
- **Auth Required**: Yes
- **Description**: Create new delivery order

**Request Body**:
```json
{
  "name": "string",
  "description": "string",
  "weight": "number (kg)",
  "price": "number (decimal)",
  "recipientId": "integer",
  "pickupNodeId": "integer",
  "dropoffNodeId": "integer"
}
```

**Response**: `201 Created`
```json
{
  "id": "integer",
  "name": "string",
  "description": "string",
  "weight": "number",
  "price": "number",
  "paid": "boolean",
  "status": "Pending | Processing | EnRoute | Delivered | Cancelled",
  "createdAt": "datetime",
  "completedAt": "datetime (nullable)",
  "senderId": "integer",
  "senderName": "string",
  "recipientId": "integer",
  "recipientName": "string",
  "robotId": "integer (nullable)",
  "robotName": "string (nullable)",
  "pickupNodeId": "integer",
  "pickupNodeName": "string",
  "dropoffNodeId": "integer",
  "dropoffNodeName": "string"
}
```

---

#### 2.2 Get Order by ID
- **Endpoint**: `GET /api/Order/{id}`
- **Auth Required**: Yes
- **Response**: `200 OK` (OrderResponseDTO)

---

#### 2.3 Get All Orders
- **Endpoint**: `GET /api/Order`
- **Auth Required**: Yes
- **Response**: `200 OK` (Array of OrderResponseDTO)

---

#### 2.4 Get My Orders
- **Endpoint**: `GET /api/Order/my-orders`
- **Auth Required**: Yes
- **Description**: Get orders for authenticated user (sent + received)
- **Response**: `200 OK` (Array of OrderResponseDTO)

---

#### 2.5 Get Orders by Status
- **Endpoint**: `GET /api/Order/status/{status}`
- **Auth Required**: Yes
- **Parameters**:
  - `status`: Pending | Processing | EnRoute | Delivered | Cancelled
- **Response**: `200 OK` (Array of OrderResponseDTO)

---

#### 2.6 Update Order Status
- **Endpoint**: `PATCH /api/Order/{id}/status`
- **Auth Required**: Yes
- **Request Body**:
```json
{
  "newStatus": "Processing | EnRoute | Delivered | Cancelled"
}
```
- **Response**: `200 OK`

**Status Transition Rules**:
- Pending → Processing | Cancelled
- Processing → EnRoute | Cancelled
- EnRoute → Delivered | Cancelled
- Delivered → (terminal state)
- Cancelled → (terminal state)

---

#### 2.7 Assign Robot to Order
- **Endpoint**: `POST /api/Order/{id}/assign-robot`
- **Auth Required**: Yes
- **Request Body**:
```json
{
  "robotId": "integer"
}
```

**Business Rules**:
- Robot must exist
- Robot status must be "Idle"
- Robot battery level must be >= 20%
- Order status must be Pending or Processing

**Response**: `200 OK`

---

#### 2.8 Cancel Order
- **Endpoint**: `POST /api/Order/{id}/cancel`
- **Auth Required**: Yes (must be order sender)
- **Description**: Cancel order (only sender can cancel)
- **Response**: `200 OK`

---

#### 2.9 Delete Order
- **Endpoint**: `DELETE /api/Order/{id}`
- **Auth Required**: Yes
- **Response**: `200 OK`

---

### 3. Robot Controller (`/api/Robot`)

#### 3.1 Create Robot
- **Endpoint**: `POST /api/Robot`
- **Auth Required**: Yes (Admin role)
- **Request Body**:
```json
{
  "name": "string",
  "model": "string",
  "type": "Ground | Aerial",
  "currentNodeId": "integer (optional)"
}
```

**Response**: `201 Created` (RobotResponseDTO)

---

#### 3.2 Get Robot by ID
- **Endpoint**: `GET /api/Robot/{id}`
- **Auth Required**: Yes
- **Response**: `200 OK`
```json
{
  "id": "integer",
  "name": "string",
  "model": "string",
  "type": "Ground | Aerial",
  "typeName": "string",
  "status": "Idle | Delivering | Charging | Maintenance",
  "statusName": "string",
  "batteryLevel": "number (0-100)",
  "currentNodeId": "integer (nullable)",
  "currentNodeName": "string (nullable)",
  "activeOrdersCount": "integer"
}
```

---

#### 3.3 Get All Robots
- **Endpoint**: `GET /api/Robot`
- **Auth Required**: Yes
- **Response**: `200 OK` (Array of RobotResponseDTO)

---

#### 3.4 Get Robots by Status
- **Endpoint**: `GET /api/Robot/status/{status}`
- **Auth Required**: Yes
- **Parameters**:
  - `status`: Idle | Delivering | Charging | Maintenance
- **Response**: `200 OK` (Array of RobotResponseDTO)

---

#### 3.5 Get Robots by Type
- **Endpoint**: `GET /api/Robot/type/{type}`
- **Auth Required**: Yes
- **Parameters**:
  - `type`: Ground | Aerial
- **Response**: `200 OK` (Array of RobotResponseDTO)

---

#### 3.6 Get Available Robots
- **Endpoint**: `GET /api/Robot/available`
- **Auth Required**: Yes
- **Description**: Get robots with status "Idle" and battery >= 20%
- **Response**: `200 OK` (Array of RobotResponseDTO)

---

#### 3.7 Update Robot
- **Endpoint**: `PUT /api/Robot/{id}`
- **Auth Required**: Yes (Admin role)
- **Request Body**:
```json
{
  "id": "integer",
  "name": "string",
  "model": "string",
  "type": "Ground | Aerial",
  "status": "Idle | Delivering | Charging | Maintenance",
  "batteryLevel": "number (0-100)",
  "currentNodeId": "integer (nullable)"
}
```
- **Response**: `200 OK`

---

#### 3.8 Delete Robot
- **Endpoint**: `DELETE /api/Robot/{id}`
- **Auth Required**: Yes (Admin role)
- **Response**: `200 OK`

---

### 4. Node Controller (`/api/Node`)

#### 4.1 Create Node
- **Endpoint**: `POST /api/Node`
- **Auth Required**: Yes
- **Request Body**:
```json
{
  "name": "string",
  "description": "string",
  "latitude": "number (degrees)",
  "longitude": "number (degrees)",
  "type": "Hub | Station | Warehouse"
}
```

**Response**: `201 Created` (NodeResponseDTO)

---

#### 4.2 Get Node by ID
- **Endpoint**: `GET /api/Node/{id}`
- **Auth Required**: Yes
- **Response**: `200 OK`
```json
{
  "id": "integer",
  "name": "string",
  "latitude": "number",
  "longitude": "number",
  "type": "Hub | Station | Warehouse",
  "typeName": "string"
}
```

---

#### 4.3 Get All Nodes
- **Endpoint**: `GET /api/Node`
- **Auth Required**: Yes
- **Response**: `200 OK` (Array of NodeResponseDTO)

---

#### 4.4 Get Nodes by Type
- **Endpoint**: `GET /api/Node/type/{type}`
- **Auth Required**: Yes
- **Parameters**:
  - `type`: Hub | Station | Warehouse
- **Response**: `200 OK` (Array of NodeResponseDTO)

---

#### 4.5 Update Node
- **Endpoint**: `PUT /api/Node/{id}`
- **Auth Required**: Yes
- **Request Body**: (Same as Create Node + id field)
- **Response**: `200 OK`

---

#### 4.6 Delete Node
- **Endpoint**: `DELETE /api/Node/{id}`
- **Auth Required**: Yes
- **Response**: `200 OK`

---

### 5. Partner Controller (`/api/Partner`)

#### 5.1 Create Partner
- **Endpoint**: `POST /api/Partner`
- **Auth Required**: Yes
- **Request Body**:
```json
{
  "name": "string",
  "contactInfo": "string",
  "address": "string"
}
```

**Response**: `201 Created` (PartnerResponseDTO)

---

#### 5.2 Get Partner by ID
- **Endpoint**: `GET /api/Partner/{id}`
- **Auth Required**: Yes
- **Response**: `200 OK`
```json
{
  "id": "integer",
  "name": "string",
  "contactInfo": "string",
  "address": "string"
}
```

---

#### 5.3 Get All Partners
- **Endpoint**: `GET /api/Partner`
- **Auth Required**: Yes
- **Response**: `200 OK` (Array of PartnerResponseDTO)

---

#### 5.4 Update Partner
- **Endpoint**: `PUT /api/Partner/{id}`
- **Auth Required**: Yes
- **Request Body**: (Same as Create Partner + id field)
- **Response**: `200 OK`

---

#### 5.5 Delete Partner
- **Endpoint**: `DELETE /api/Partner/{id}`
- **Auth Required**: Yes
- **Response**: `200 OK`

---

### 6. Admin Controller (`/api/Admin`)
**All endpoints require Admin role**

#### 6.1 Get System Statistics
- **Endpoint**: `GET /api/Admin/stats`
- **Auth Required**: Yes (Admin)
- **Description**: Get comprehensive system statistics
- **Response**: `200 OK`
```json
{
  "totalUsers": "integer",
  "totalOrders": "integer",
  "totalRobots": "integer",
  "totalNodes": "integer",
  "totalPartners": "integer",
  "activeOrders": "integer",
  "completedOrders": "integer",
  "cancelledOrders": "integer",
  "availableRobots": "integer",
  "busyRobots": "integer",
  "chargingRobots": "integer",
  "averageBatteryLevel": "number",
  "totalRevenue": "number"
}
```

---

#### 6.2 Export Delivery History
- **Endpoint**: `GET /api/Admin/export/delivery-history`
- **Auth Required**: Yes (Admin)
- **Description**: Export complete delivery history as JSON file
- **Response**: `200 OK` (File download)
- **Content-Type**: application/json
- **File Format**: JSON array with all orders

---

#### 6.3 Create Database Backup
- **Endpoint**: `POST /api/Admin/backup`
- **Auth Required**: Yes (Admin)
- **Description**: Create backup of database and delivery history
- **Request Body**:
```json
{
  "backupPath": "string (optional, defaults to 'Backups')"
}
```

**Response**: `200 OK`
```json
{
  "message": "Backup created successfully",
  "path": "string"
}
```

**Backup Contents**:
1. SQLite database file copy
2. JSON export of delivery history

---

#### 6.4 Get Robot Efficiency Analytics
- **Endpoint**: `GET /api/Admin/analytics/robot-efficiency`
- **Auth Required**: Yes (Admin)
- **Description**: Get efficiency metrics for all robots
- **Response**: `200 OK`
```json
{
  "1": 85.5,
  "2": 92.3,
  "3": 78.1
}
```
- Key: Robot ID
- Value: Efficiency score (0-100)

**Efficiency Calculation**:
```
Efficiency = (Completed Orders) / (101 - Battery Level) * 100
```

---

## Error Responses

### Standard Error Format
```json
{
  "error": "string (error message)",
  "details": "string (optional, detailed error info)"
}
```

### HTTP Status Codes
- `200 OK` - Successful request
- `201 Created` - Resource created successfully
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Missing or invalid JWT token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

---

## Business Logic

### Route Navigation
**Note**: Route planning and navigation is handled by the robot's on-board systems (IoT devices). The server only provides:
- Node coordinates (latitude, longitude)
- Pickup and dropoff locations
- Current robot location

The robot autonomously calculates optimal routes considering:
- Charging stations
- Pickup points
- Delivery destinations
- Battery level
- Real-time obstacles

### Order Assignment Rules
1. Robot must be in "Idle" status
2. Robot battery level must be >= 20%
3. Order status must be "Pending" or "Processing"

### Status Validation
- Orders follow strict state machine transitions
- Invalid transitions are rejected with 400 error
- Completed/Cancelled orders cannot change status

---

## Testing

### Swagger UI
Available at `/swagger` in development mode

### HTTP Test File
Use `RobDeliveryAPI.http` for testing all endpoints

### Sample Test Flow
1. Register admin user with role "Admin"
2. Login to get JWT token
3. Create nodes (pickup and dropoff locations)
4. Create robots
5. Create order
6. Assign robot to order
7. Update order status through delivery lifecycle
8. View statistics on admin dashboard

---

## Database Schema

### Entities
- **User** (Id, UserName, Email, PasswordHash, GoogleId, PhoneNumber, Role)
- **Order** (Id, Name, Description, Weight, Price, Paid, Status, SenderId, RecipientId, RobotId, PickupNodeId, DropoffNodeId, CreatedAt, CompletedAt)
- **Robot** (Id, Name, Model, Type, Status, BatteryLevel, CurrentNodeId)
- **Node** (Id, Name, Latitude, Longitude, Type)
- **Partner** (Id, Name, ContactInfo, Address)

### Relationships
- Order.Sender → User (many-to-one)
- Order.Recipient → User (many-to-one)
- Order.AssignedRobot → Robot (many-to-one, optional)
- Order.PickupNode → Node (many-to-one)
- Order.DropoffNode → Node (many-to-one)
- Robot.CurrentNode → Node (many-to-one, optional)
- Robot.ActiveOrders → Order collection

---

## Security

### JWT Configuration
- Algorithm: HS256
- Token expiration: Configured in appsettings.json
- ClockSkew: Zero (strict expiration)

### Role-Based Access
- **User**: Create orders, view own orders
- **Partner**: Extended access to partner-related operations
- **Admin**: Full system access, analytics, backups
- **Iot**: Robot control and telemetry

### Password Security
- Passwords hashed using SHA-256
- Minimum password requirements enforced
- Google OAuth supported as alternative

---

## Notes
- All datetime values are in UTC
- Geographic coordinates use WGS84 datum (for robot navigation)
- Currency amounts are decimal with 2 decimal places
- Route planning is handled by robot's on-board AI
- Battery levels are percentages (0-100)
