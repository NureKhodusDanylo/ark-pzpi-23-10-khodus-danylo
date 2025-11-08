# Real API Testing Script
$baseUrl = "http://localhost:5102"
$results = @()

Write-Host "=== ТЕСТУВАННЯ API RobDelivery ===" -ForegroundColor Green
Write-Host ""

# Test 1: Register first user (becomes admin)
Write-Host "Тест 1: Реєстрація першого користувача (автоматично стає адміністратором)" -ForegroundColor Yellow
$registerBody = @{
    userName = "AdminUser"
    email = "admin@example.com"
    password = "Admin123!"
    phoneNumber = "+380501234567"
    address = "Kyiv, Shevchenko St, 1"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
    Write-Host "Запит:" -ForegroundColor Cyan
    Write-Host $registerBody
    Write-Host "Відповідь:" -ForegroundColor Cyan
    Write-Host ($response | ConvertTo-Json)
    $results += @{
        Test = "Реєстрація першого користувача"
        Status = "✅ PASSED"
        Details = "Користувач успішно зареєстрований"
    }
} catch {
    Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
    $results += @{
        Test = "Реєстрація першого користувача"
        Status = "❌ FAILED"
        Details = $_.Exception.Message
    }
}
Write-Host ""

# Test 2: Login as admin
Write-Host "Тест 2: Вхід в систему як адміністратор" -ForegroundColor Yellow
$loginBody = @{
    email = "admin@example.com"
    password = "Admin123!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
    Write-Host "Запит:" -ForegroundColor Cyan
    Write-Host $loginBody
    Write-Host "Відповідь:" -ForegroundColor Cyan
    Write-Host ($loginResponse | ConvertTo-Json)
    $adminToken = $loginResponse.token
    $results += @{
        Test = "Вхід в систему"
        Status = "✅ PASSED"
        Details = "JWT токен отримано"
    }
} catch {
    Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
    $results += @{
        Test = "Вхід в систему"
        Status = "❌ FAILED"
        Details = $_.Exception.Message
    }
}
Write-Host ""

# Test 3: Generate admin key
Write-Host "Тест 3: Генерація адміністративного ключа" -ForegroundColor Yellow
$adminKeyBody = @{
    description = "Тестовий ключ для другого адміністратора"
    expiresAt = "2025-12-31T23:59:59Z"
} | ConvertTo-Json

try {
    $headers = @{
        "Authorization" = "Bearer $adminToken"
    }
    $adminKeyResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/keys/generate" -Method Post -Body $adminKeyBody -ContentType "application/json" -Headers $headers
    Write-Host "Запит:" -ForegroundColor Cyan
    Write-Host $adminKeyBody
    Write-Host "Відповідь:" -ForegroundColor Cyan
    Write-Host ($adminKeyResponse | ConvertTo-Json)
    $generatedKey = $adminKeyResponse.keyCode
    $results += @{
        Test = "Генерація адміністративного ключа"
        Status = "✅ PASSED"
        Details = "Ключ успішно згенеровано: $generatedKey"
    }
} catch {
    Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
    $results += @{
        Test = "Генерація адміністративного ключа"
        Status = "❌ FAILED"
        Details = $_.Exception.Message
    }
}
Write-Host ""

# Test 4: Register second user with admin key
if ($generatedKey) {
    Write-Host "Тест 4: Реєстрація другого користувача з адмін-ключем" -ForegroundColor Yellow
    $registerAdmin2Body = @{
        userName = "AdminUser2"
        email = "admin2@example.com"
        password = "Admin123!"
        phoneNumber = "+380501234568"
        address = "Kyiv, Khreshchatyk St, 10"
        adminKey = $generatedKey
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerAdmin2Body -ContentType "application/json"
        Write-Host "Запит:" -ForegroundColor Cyan
        Write-Host $registerAdmin2Body
        Write-Host "Відповідь:" -ForegroundColor Cyan
        Write-Host ($response | ConvertTo-Json)
        $results += @{
            Test = "Реєстрація з адмін-ключем"
            Status = "✅ PASSED"
            Details = "Другий адміністратор зареєстрований"
        }
    } catch {
        Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
        $results += @{
            Test = "Реєстрація з адмін-ключем"
            Status = "❌ FAILED"
            Details = $_.Exception.Message
        }
    }
    Write-Host ""
}

# Test 5: Try to reuse admin key (should fail)
if ($generatedKey) {
    Write-Host "Тест 5: Спроба повторного використання ключа (має не спрацювати)" -ForegroundColor Yellow
    $registerFailBody = @{
        userName = "HackerUser"
        email = "hacker@example.com"
        password = "Hacker123!"
        phoneNumber = "+380501234569"
        address = "Unknown"
        adminKey = $generatedKey
    } | ConvertTo-Json

    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerFailBody -ContentType "application/json"
        Write-Host "Запит:" -ForegroundColor Cyan
        Write-Host $registerFailBody
        Write-Host "Відповідь:" -ForegroundColor Cyan
        Write-Host ($response | ConvertTo-Json)
        $results += @{
            Test = "Захист від повторного використання ключа"
            Status = "❌ FAILED"
            Details = "Ключ не повинен був прийматися повторно!"
        }
    } catch {
        Write-Host "Очікувана помилка: $($_.Exception.Message)" -ForegroundColor Green
        $results += @{
            Test = "Захист від повторного використання ключа"
            Status = "✅ PASSED"
            Details = "Система правильно відхилила повторне використання"
        }
    }
    Write-Host ""
}

# Test 6: Register regular user
Write-Host "Тест 6: Реєстрація звичайного користувача" -ForegroundColor Yellow
$registerUserBody = @{
    userName = "RegularUser"
    email = "user@example.com"
    password = "User123!"
    phoneNumber = "+380501234570"
    address = "Lviv, Svobody Ave, 5"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerUserBody -ContentType "application/json"
    Write-Host "Запит:" -ForegroundColor Cyan
    Write-Host $registerUserBody
    Write-Host "Відповідь:" -ForegroundColor Cyan
    Write-Host ($response | ConvertTo-Json)
    $results += @{
        Test = "Реєстрація звичайного користувача"
        Status = "✅ PASSED"
        Details = "Користувач зареєстрований без адмін-прав"
    }
} catch {
    Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
    $results += @{
        Test = "Реєстрація звичайного користувача"
        Status = "❌ FAILED"
        Details = $_.Exception.Message
    }
}
Write-Host ""

# Test 7: Login as regular user
Write-Host "Тест 7: Вхід як звичайний користувач" -ForegroundColor Yellow
$loginUserBody = @{
    email = "user@example.com"
    password = "User123!"
} | ConvertTo-Json

try {
    $userLoginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginUserBody -ContentType "application/json"
    Write-Host "Запит:" -ForegroundColor Cyan
    Write-Host $loginUserBody
    Write-Host "Відповідь:" -ForegroundColor Cyan
    Write-Host ($userLoginResponse | ConvertTo-Json)
    $userToken = $userLoginResponse.token
    $results += @{
        Test = "Вхід звичайного користувача"
        Status = "✅ PASSED"
        Details = "Токен отримано"
    }
} catch {
    Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
    $results += @{
        Test = "Вхід звичайного користувача"
        Status = "❌ FAILED"
        Details = $_.Exception.Message
    }
}
Write-Host ""

# Test 8: Create robot (admin only)
Write-Host "Тест 8: Створення робота (тільки для адміністратора)" -ForegroundColor Yellow
$robotBody = @{
    name = "TestRobot1"
    model = "DR-500"
    type = "Ground"
    maxLoad = 15.5
    maxRange = 50.0
    serialNumber = "SN-001"
    accessKey = "SECRET-KEY-001"
} | ConvertTo-Json

try {
    $headers = @{
        "Authorization" = "Bearer $adminToken"
    }
    $robotResponse = Invoke-RestMethod -Uri "$baseUrl/api/robot" -Method Post -Body $robotBody -ContentType "application/json" -Headers $headers
    Write-Host "Запит:" -ForegroundColor Cyan
    Write-Host $robotBody
    Write-Host "Відповідь:" -ForegroundColor Cyan
    Write-Host ($robotResponse | ConvertTo-Json)
    $robotId = $robotResponse.id
    $results += @{
        Test = "Створення робота"
        Status = "✅ PASSED"
        Details = "Робот створено з ID: $robotId"
    }
} catch {
    Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
    $results += @{
        Test = "Створення робота"
        Status = "❌ FAILED"
        Details = $_.Exception.Message
    }
}
Write-Host ""

# Test 9: Create order
Write-Host "Тест 9: Створення замовлення" -ForegroundColor Yellow
$orderBody = @{
    recipientId = 1
    name = "Тестова посилка"
    description = "Книга для друга"
    weight = 2.5
    productPrice = 500.00
    isProductPaid = $true
} | ConvertTo-Json

try {
    $headers = @{
        "Authorization" = "Bearer $userToken"
    }
    $orderResponse = Invoke-RestMethod -Uri "$baseUrl/api/order" -Method Post -Body $orderBody -ContentType "application/json" -Headers $headers
    Write-Host "Запит:" -ForegroundColor Cyan
    Write-Host $orderBody
    Write-Host "Відповідь:" -ForegroundColor Cyan
    Write-Host ($orderResponse | ConvertTo-Json)
    $orderId = $orderResponse.id
    $results += @{
        Test = "Створення замовлення"
        Status = "✅ PASSED"
        Details = "Замовлення створено з ID: $orderId, Ціна доставки: $($orderResponse.deliveryPrice)"
    }
} catch {
    Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
    $results += @{
        Test = "Створення замовлення"
        Status = "❌ FAILED"
        Details = $_.Exception.Message
    }
}
Write-Host ""

# Test 10: Assign robot to order
if ($orderId -and $robotId) {
    Write-Host "Тест 10: Призначення робота для замовлення" -ForegroundColor Yellow
    try {
        $headers = @{
            "Authorization" = "Bearer $adminToken"
        }
        $assignResponse = Invoke-RestMethod -Uri "$baseUrl/api/order/$orderId/assign/$robotId" -Method Post -Headers $headers
        Write-Host "Запит: POST /api/order/$orderId/assign/$robotId" -ForegroundColor Cyan
        Write-Host "Відповідь:" -ForegroundColor Cyan
        Write-Host ($assignResponse | ConvertTo-Json)
        $results += @{
            Test = "Призначення робота"
            Status = "✅ PASSED"
            Details = "Робот призначений для доставки"
        }
    } catch {
        Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
        $results += @{
            Test = "Призначення робота"
            Status = "❌ FAILED"
            Details = $_.Exception.Message
        }
    }
    Write-Host ""
}

# Test 11: Get admin statistics
Write-Host "Тест 11: Отримання статистики системи" -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $adminToken"
    }
    $statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/stats" -Method Get -Headers $headers
    Write-Host "Запит: GET /api/admin/stats" -ForegroundColor Cyan
    Write-Host "Відповідь:" -ForegroundColor Cyan
    Write-Host ($statsResponse | ConvertTo-Json -Depth 3)
    $results += @{
        Test = "Статистика системи"
        Status = "✅ PASSED"
        Details = "Користувачів: $($statsResponse.totalUsers), Замовлень: $($statsResponse.totalOrders), Роботів: $($statsResponse.totalRobots)"
    }
} catch {
    Write-Host "Помилка: $($_.Exception.Message)" -ForegroundColor Red
    $results += @{
        Test = "Статистика системи"
        Status = "❌ FAILED"
        Details = $_.Exception.Message
    }
}
Write-Host ""

# Summary
Write-Host ""
Write-Host "=== ПІДСУМКИ ТЕСТУВАННЯ ===" -ForegroundColor Green
Write-Host ""
$passed = ($results | Where-Object { $_.Status -like "*PASSED*" }).Count
$failed = ($results | Where-Object { $_.Status -like "*FAILED*" }).Count
Write-Host "Всього тестів: $($results.Count)" -ForegroundColor Cyan
Write-Host "Успішних: $passed" -ForegroundColor Green
Write-Host "Невдалих: $failed" -ForegroundColor Red
Write-Host ""

foreach ($result in $results) {
    Write-Host "$($result.Status) $($result.Test)" -ForegroundColor $(if ($result.Status -like "*PASSED*") { "Green" } else { "Red" })
    Write-Host "   Деталі: $($result.Details)" -ForegroundColor Gray
    Write-Host ""
}
