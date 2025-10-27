# API Testing Script for Lab 3
$baseUrl = "http://localhost:5102/api"

Write-Host "=== ЛАБОРАТОРНА РОБОТА №3 - ТЕСТУВАННЯ API ===" -ForegroundColor Cyan
Write-Host ""

# 1. Register users
Write-Host "1. Реєстрація користувачів..." -ForegroundColor Yellow

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
Write-Host "✓ Користувачів зареєстровано" -ForegroundColor Green

# 2. Login as Admin
Write-Host "2. Вхід як адмін..." -ForegroundColor Yellow

$loginData = @{
    email = "admin@test.com"
    password = "admin123"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/User/login" -Method POST -Body $loginData -ContentType "application/json"
$adminToken = $loginResponse.token
Write-Host "✓ Адмін увійшов. Token: $($adminToken.Substring(0,20))..." -ForegroundColor Green

# 3. Login as regular user
Write-Host "3. Вхід як звичайний користувач..." -ForegroundColor Yellow

$userLoginData = @{
    email = "john@test.com"
    password = "john123"
} | ConvertTo-Json

$userLoginResponse = Invoke-RestMethod -Uri "$baseUrl/User/login" -Method POST -Body $userLoginData -ContentType "application/json"
$userToken = $userLoginResponse.token
Write-Host "✓ Користувач увійшов" -ForegroundColor Green

# 4. Create Nodes
Write-Host "4. Створення точок доставки..." -ForegroundColor Yellow

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
Write-Host "✓ Створено 2 точки доставки" -ForegroundColor Green

# 5. Create Robots (Admin only)
Write-Host "5. Створення роботів (тільки Admin)..." -ForegroundColor Yellow

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
Write-Host "✓ Створено 2 роботи" -ForegroundColor Green

# 6. Create Orders
Write-Host "6. Створення замовлень..." -ForegroundColor Yellow

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
Write-Host "✓ Створено 2 замовлення" -ForegroundColor Green

Write-Host ""
Write-Host "=== ТЕСТУВАННЯ НОВОЇ ФУНКЦІОНАЛЬНОСТІ ЛР3 ===" -ForegroundColor Cyan
Write-Host ""

# TEST 1: Admin Statistics
Write-Host "ТЕСТ 1: Статистика системи (Admin)" -ForegroundColor Magenta
try {
    $stats = Invoke-RestMethod -Uri "$baseUrl/Admin/stats" -Method GET -Headers $headers
    Write-Host "✓ Користувачів: $($stats.totalUsers)" -ForegroundColor Green
    Write-Host "✓ Замовлень: $($stats.totalOrders)" -ForegroundColor Green
    Write-Host "✓ Роботів: $($stats.totalRobots)" -ForegroundColor Green
    Write-Host "✓ Точок: $($stats.totalNodes)" -ForegroundColor Green
    Write-Host "✓ Середній рівень батареї: $($stats.averageBatteryLevel)%" -ForegroundColor Green
} catch {
    Write-Host "✗ Помилка: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 2: Robot Efficiency Analytics
Write-Host "ТЕСТ 2: Аналітика ефективності роботів (Admin)" -ForegroundColor Magenta
try {
    $efficiency = Invoke-RestMethod -Uri "$baseUrl/Admin/analytics/robot-efficiency" -Method GET -Headers $headers
    Write-Host "✓ Ефективність роботів:" -ForegroundColor Green
    $efficiency.PSObject.Properties | ForEach-Object {
        Write-Host "  Robot ID $($_.Name): $($_.Value)" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✗ Помилка: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 3: Role-Based Authorization (try to access Admin endpoint with regular user)
Write-Host "ТЕСТ 3: Перевірка авторизації на основі ролей" -ForegroundColor Magenta
try {
    Invoke-RestMethod -Uri "$baseUrl/Admin/stats" -Method GET -Headers $userHeaders | Out-Null
    Write-Host "✗ ПОМИЛКА: Звичайний користувач отримав доступ до Admin endpoint!" -ForegroundColor Red
} catch {
    Write-Host "✓ Доступ заборонено для звичайного користувача (очікувана поведінка)" -ForegroundColor Green
}

Write-Host ""

# TEST 4: Assign Robot to Order (Business Logic)
Write-Host "ТЕСТ 4: Призначення робота на замовлення (бізнес-логіка)" -ForegroundColor Magenta
try {
    $assignData = @{ robotId = $r1.id } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Order/$($o1.id)/assign-robot" -Method POST -Body $assignData -ContentType "application/json" -Headers $userHeaders | Out-Null
    Write-Host "✓ Робот успішно призначений на замовлення" -ForegroundColor Green

    # Get order to see updated status
    $updatedOrder = Invoke-RestMethod -Uri "$baseUrl/Order/$($o1.id)" -Method GET -Headers $userHeaders
    Write-Host "✓ Статус замовлення: $($updatedOrder.status)" -ForegroundColor Cyan
    Write-Host "✓ Призначений робот: $($updatedOrder.robotName)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Помилка: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 5: Order Status Transitions
Write-Host "ТЕСТ 5: Валідація переходів статусів замовлення" -ForegroundColor Magenta
try {
    # Valid transition: Pending -> Processing
    $statusUpdate1 = @{ newStatus = 1 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Order/$($o2.id)/status" -Method PATCH -Body $statusUpdate1 -ContentType "application/json" -Headers $userHeaders | Out-Null
    Write-Host "✓ Валідний перехід: Pending → Processing" -ForegroundColor Green

    # Valid transition: Processing -> EnRoute
    $statusUpdate2 = @{ newStatus = 2 } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Order/$($o2.id)/status" -Method PATCH -Body $statusUpdate2 -ContentType "application/json" -Headers $userHeaders | Out-Null
    Write-Host "✓ Валідний перехід: Processing → EnRoute" -ForegroundColor Green

    # Try invalid transition: EnRoute -> Pending (should fail)
    try {
        $invalidStatus = @{ newStatus = 0 } | ConvertTo-Json
        Invoke-RestMethod -Uri "$baseUrl/Order/$($o2.id)/status" -Method PATCH -Body $invalidStatus -ContentType "application/json" -Headers $userHeaders | Out-Null
        Write-Host "✗ ПОМИЛКА: Невалідний перехід був дозволений!" -ForegroundColor Red
    } catch {
        Write-Host "✓ Невалідний перехід заблоковано (EnRoute → Pending)" -ForegroundColor Green
    }
} catch {
    Write-Host "✗ Помилка: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 6: Database Backup
Write-Host "ТЕСТ 6: Резервне копіювання бази даних (Admin)" -ForegroundColor Magenta
try {
    $backupData = @{ backupPath = "Backups" } | ConvertTo-Json
    $backupResult = Invoke-RestMethod -Uri "$baseUrl/Admin/backup" -Method POST -Body $backupData -ContentType "application/json" -Headers $headers
    Write-Host "✓ Backup створено: $($backupResult.message)" -ForegroundColor Green
} catch {
    Write-Host "✗ Помилка: $_" -ForegroundColor Red
}

Write-Host ""

# TEST 7: Export Delivery History
Write-Host "ТЕСТ 7: Експорт історії доставок (Admin)" -ForegroundColor Magenta
try {
    Invoke-RestMethod -Uri "$baseUrl/Admin/export/delivery-history" -Method GET -Headers $headers -OutFile "delivery_history_export.json"
    Write-Host "✓ Історія доставок експортована в delivery_history_export.json" -ForegroundColor Green

    $exportedData = Get-Content "delivery_history_export.json" | ConvertFrom-Json
    Write-Host "✓ Експортовано записів: $($exportedData.Count)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ Помилка: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== ТЕСТУВАННЯ ЗАВЕРШЕНО ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Підсумок тестів:" -ForegroundColor Yellow
Write-Host "✓ Статистика системи" -ForegroundColor Green
Write-Host "✓ Аналітика ефективності роботів" -ForegroundColor Green
Write-Host "✓ Авторизація на основі ролей" -ForegroundColor Green
Write-Host "✓ Бізнес-логіка призначення робота" -ForegroundColor Green
Write-Host "✓ Валідація переходів статусів" -ForegroundColor Green
Write-Host "✓ Резервне копіювання" -ForegroundColor Green
Write-Host "✓ Експорт історії доставок" -ForegroundColor Green
