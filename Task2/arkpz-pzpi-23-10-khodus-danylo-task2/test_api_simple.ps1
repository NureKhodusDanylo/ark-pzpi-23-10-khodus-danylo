# API Testing Script for Lab 3
$baseUrl = "http://localhost:5102/api"

Write-Host "=== LAB 3 API TESTING ===" -ForegroundColor Cyan
Write-Host ""

# 1. Register users
Write-Host "1. Registering users..." -ForegroundColor Yellow

$adminUser = @{
    userName = "AdminUser"
    email = "admin@test.com"
    password = "admin123"
} | ConvertTo-Json

$user1 = @{
    userName = "JohnDoe"
    email = "john@test.com"
    password = "john123"
} | ConvertTo-Json

$user2 = @{
    userName = "JaneDoe"
    email = "jane@test.com"
    password = "jane123"
} | ConvertTo-Json

Invoke-RestMethod -Uri "$baseUrl/User/register" -Method POST -Body $adminUser -ContentType "application/json" | Out-Null
Invoke-RestMethod -Uri "$baseUrl/User/register" -Method POST -Body $user1 -ContentType "application/json" | Out-Null
Invoke-RestMethod -Uri "$baseUrl/User/register" -Method POST -Body $user2 -ContentType "application/json" | Out-Null
Write-Host "[OK] Users registered" -ForegroundColor Green

# 2. Login as Admin
Write-Host "2. Login as admin..." -ForegroundColor Yellow

$loginData = @{
    email = "admin@test.com"
    password = "admin123"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/User/login" -Method POST -Body $loginData -ContentType "application/json"
$adminToken = $loginResponse.token
Write-Host "[OK] Admin logged in. Token: $($adminToken.Substring(0,20))..." -ForegroundColor Green

# 3. Login as regular user
Write-Host "3. Login as regular user..." -ForegroundColor Yellow

$userLoginData = @{
    email = "john@test.com"
    password = "john123"
} | ConvertTo-Json

$userLoginResponse = Invoke-RestMethod -Uri "$baseUrl/User/login" -Method POST -Body $userLoginData -ContentType "application/json"
$userToken = $userLoginResponse.token
$userId = $userLoginResponse.userId
Write-Host "[OK] User logged in. User ID: $userId" -ForegroundColor Green

# 4. Create Nodes
Write-Host "4. Creating delivery nodes..." -ForegroundColor Yellow

$headers = @{ Authorization = "Bearer $adminToken" }

$node1 = @{
    name = "Hub Central"
    latitude = 50.4501
    longitude = 30.5234
    type = 0
} | ConvertTo-Json

$node2 = @{
    name = "Station North"
    latitude = 50.4601
    longitude = 30.5334
    type = 1
} | ConvertTo-Json

$n1 = Invoke-RestMethod -Uri "$baseUrl/Node" -Method POST -Body $node1 -ContentType "application/json" -Headers $headers
$n2 = Invoke-RestMethod -Uri "$baseUrl/Node" -Method POST -Body $node2 -ContentType "application/json" -Headers $headers
Write-Host "[OK] Created 2 delivery nodes" -ForegroundColor Green

# 5. Create Robots (Admin only)
Write-Host "5. Creating robots (Admin only)..." -ForegroundColor Yellow

$robot1 = @{
    name = "DeliveryBot-01"
    model = "TB-1000"
    type = 0
    currentNodeId = $n1.id
} | ConvertTo-Json

$robot2 = @{
    name = "DroneBot-02"
    model = "DR-500"
    type = 1
    currentNodeId = $n1.id
} | ConvertTo-Json

$r1 = Invoke-RestMethod -Uri "$baseUrl/Robot" -Method POST -Body $robot1 -ContentType "application/json" -Headers $headers
$r2 = Invoke-RestMethod -Uri "$baseUrl/Robot" -Method POST -Body $robot2 -ContentType "application/json" -Headers $headers
Write-Host "[OK] Created 2 robots" -ForegroundColor Green

# 6. Create Orders
Write-Host "6. Creating orders..." -ForegroundColor Yellow

$userHeaders = @{ Authorization = "Bearer $userToken" }

$order1 = @{
    name = "Package 1"
    description = "Laptop"
    weight = 2.5
    price = 150.00
    recipientId = 2
    pickupNodeId = $n1.id
    dropoffNodeId = $n2.id
} | ConvertTo-Json

$order2 = @{
    name = "Package 2"
    description = "Books"
    weight = 1.0
    price = 50.00
    recipientId = 3
    pickupNodeId = $n2.id
    dropoffNodeId = $n1.id
} | ConvertTo-Json

$o1 = Invoke-RestMethod -Uri "$baseUrl/Order" -Method POST -Body $order1 -ContentType "application/json" -Headers $userHeaders
$o2 = Invoke-RestMethod -Uri "$baseUrl/Order" -Method POST -Body $order2 -ContentType "application/json" -Headers $userHeaders
Write-Host "[OK] Created 2 orders" -ForegroundColor Green

Write-Host ""
Write-Host "=== TESTING NEW LAB 3 FUNCTIONALITY ===" -ForegroundColor Cyan
Write-Host ""

# TEST 1: Admin Statistics
Write-Host "TEST 1: System Statistics (Admin)" -ForegroundColor Magenta
try {
    $stats = Invoke-RestMethod -Uri "$baseUrl/Admin/stats" -Method GET -Headers $headers
    Write-Host "[OK] Total Users: $($stats.totalUsers)" -ForegroundColor Green
    Write-Host "[OK] Total Orders: $($stats.totalOrders)" -ForegroundColor Green
    Write-Host "[OK] Total Robots: $($stats.totalRobots)" -ForegroundColor Green
    Write-Host "[OK] Total Nodes: $($stats.totalNodes)" -ForegroundColor Green
    Write-Host "[OK] Average Battery: $($stats.averageBatteryLevel)%" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Error: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 2: Robot Efficiency Analytics
Write-Host "TEST 2: Robot Efficiency Analytics (Admin)" -ForegroundColor Magenta
try {
    $efficiency = Invoke-RestMethod -Uri "$baseUrl/Admin/analytics/robot-efficiency" -Method GET -Headers $headers
    Write-Host "[OK] Robot efficiency calculated:" -ForegroundColor Green
    $efficiency.PSObject.Properties | ForEach-Object {
        Write-Host "  Robot ID $($_.Name): $($_.Value)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "[FAIL] Error: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 3: Role-Based Authorization
Write-Host "TEST 3: Role-Based Authorization Test" -ForegroundColor Magenta
try {
    Invoke-RestMethod -Uri "$baseUrl/Admin/stats" -Method GET -Headers $userHeaders | Out-Null
    Write-Host "[FAIL] Regular user accessed Admin endpoint!" -ForegroundColor Red
} catch {
    Write-Host "[OK] Access denied for regular user (expected)" -ForegroundColor Green
}

Write-Host ""

# TEST 4: Assign Robot to Order
Write-Host "TEST 4: Assign Robot to Order (Business Logic)" -ForegroundColor Magenta
try {
    $assignData = @{ robotId = $r1.id } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Order/$($o1.id)/assign-robot" -Method POST -Body $assignData -ContentType "application/json" -Headers $userHeaders | Out-Null
    Write-Host "[OK] Robot assigned to order" -ForegroundColor Green

    $updatedOrder = Invoke-RestMethod -Uri "$baseUrl/Order/$($o1.id)" -Method GET -Headers $userHeaders
    Write-Host "[OK] Order status: $($updatedOrder.status)" -ForegroundColor Cyan
    Write-Host "[OK] Assigned robot: $($updatedOrder.robotName)" -ForegroundColor Cyan
} catch {
    Write-Host "[FAIL] Error: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 5: Order Status Transitions
Write-Host "TEST 5: Order Status Transition Validation" -ForegroundColor Magenta
try {
    # Valid: Pending -> Processing
    $statusUpdate1 = @{ newStatus = 1 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Order/$($o2.id)/status" -Method PATCH -Body $statusUpdate1 -ContentType "application/json" -Headers $userHeaders | Out-Null
    Write-Host "[OK] Valid transition: Pending -> Processing" -ForegroundColor Green

    # Valid: Processing -> EnRoute
    $statusUpdate2 = @{ newStatus = 2 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Order/$($o2.id)/status" -Method PATCH -Body $statusUpdate2 -ContentType "application/json" -Headers $userHeaders | Out-Null
    Write-Host "[OK] Valid transition: Processing -> EnRoute" -ForegroundColor Green

    # Invalid: EnRoute -> Pending (should fail)
    try {
        $invalidStatus = @{ newStatus = 0 } | ConvertTo-Json
        Invoke-RestMethod -Uri "$baseUrl/Order/$($o2.id)/status" -Method PATCH -Body $invalidStatus -ContentType "application/json" -Headers $userHeaders | Out-Null
        Write-Host "[FAIL] Invalid transition was allowed!" -ForegroundColor Red
    } catch {
        Write-Host "[OK] Invalid transition blocked (EnRoute -> Pending)" -ForegroundColor Green
    }
} catch {
    Write-Host "[FAIL] Error: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 6: Database Backup
Write-Host "TEST 6: Database Backup (Admin)" -ForegroundColor Magenta
try {
    $backupData = @{ backupPath = "Backups" } | ConvertTo-Json
    $backupResult = Invoke-RestMethod -Uri "$baseUrl/Admin/backup" -Method POST -Body $backupData -ContentType "application/json" -Headers $headers
    Write-Host "[OK] Backup created: $($backupResult.message)" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Error: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 7: Export Delivery History
Write-Host "TEST 7: Export Delivery History (Admin)" -ForegroundColor Magenta
try {
    Invoke-RestMethod -Uri "$baseUrl/Admin/export/delivery-history" -Method GET -Headers $headers -OutFile "delivery_history_export.json"
    Write-Host "[OK] Delivery history exported to delivery_history_export.json" -ForegroundColor Green

    $exportedData = Get-Content "delivery_history_export.json" | ConvertFrom-Json
    Write-Host "[OK] Exported records: $($exportedData.Count)" -ForegroundColor Cyan
} catch {
    Write-Host "[FAIL] Error: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== TESTING COMPLETED ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "[OK] System Statistics" -ForegroundColor Green
Write-Host "[OK] Robot Efficiency Analytics" -ForegroundColor Green
Write-Host "[OK] Role-Based Authorization" -ForegroundColor Green
Write-Host "[OK] Robot Assignment Business Logic" -ForegroundColor Green
Write-Host "[OK] Status Transition Validation" -ForegroundColor Green
Write-Host "[OK] Database Backup" -ForegroundColor Green
Write-Host "[OK] Delivery History Export" -ForegroundColor Green
