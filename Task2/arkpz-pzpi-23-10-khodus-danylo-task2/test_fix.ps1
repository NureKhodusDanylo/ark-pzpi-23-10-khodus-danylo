# Quick diagnostic test for failing endpoints
$baseUrl = "http://localhost:5102/api"

Write-Host "=== DIAGNOSTIC TEST ===" -ForegroundColor Cyan

# Login as admin
$loginData = @{
    email = "admin@test.com"
    password = "admin123"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Uri "$baseUrl/User/login" -Method POST -Body $loginData -ContentType "application/json"
$adminToken = $loginResponse.token
$headers = @{ Authorization = "Bearer $adminToken" }

Write-Host "Logged in as admin" -ForegroundColor Green

# Get all orders to find actual IDs
Write-Host "`nFetching orders..." -ForegroundColor Yellow
$orders = Invoke-RestMethod -Uri "$baseUrl/Order" -Method GET -Headers $headers

if ($orders.Count -gt 0) {
    $firstOrder = $orders[0]
    Write-Host "Found Order ID: $($firstOrder.id)" -ForegroundColor Cyan
    Write-Host "Order Status: $($firstOrder.status)" -ForegroundColor Cyan
    Write-Host "Order Name: $($firstOrder.name)" -ForegroundColor Cyan

    # Get all robots
    Write-Host "`nFetching robots..." -ForegroundColor Yellow
    $robots = Invoke-RestMethod -Uri "$baseUrl/Robot" -Method GET -Headers $headers

    if ($robots.Count -gt 0) {
        $firstRobot = $robots[0]
        Write-Host "Found Robot ID: $($firstRobot.id)" -ForegroundColor Cyan
        Write-Host "Robot Status: $($firstRobot.status)" -ForegroundColor Cyan

        # TEST 1: Assign Robot to Order
        Write-Host "`n=== TEST 1: Assign Robot ===" -ForegroundColor Magenta
        try {
            $assignData = @{ robotId = $firstRobot.id } | ConvertTo-Json
            $assignUrl = "$baseUrl/Order/$($firstOrder.id)/assign-robot"
            Write-Host "Testing URL: $assignUrl" -ForegroundColor Gray

            Invoke-RestMethod -Uri $assignUrl -Method POST -Body $assignData -ContentType "application/json" -Headers $headers
            Write-Host "[OK] Robot assigned successfully" -ForegroundColor Green
        } catch {
            Write-Host "[FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
            if ($_.ErrorDetails.Message) {
                Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
            }
        }
    }
} else {
    Write-Host "No orders found in database!" -ForegroundColor Red
}

# TEST 2: Database Backup
Write-Host "`n=== TEST 2: Database Backup ===" -ForegroundColor Magenta
try {
    $backupData = @{ backupPath = "Backups" } | ConvertTo-Json
    $backupResult = Invoke-RestMethod -Uri "$baseUrl/Admin/backup" -Method POST -Body $backupData -ContentType "application/json" -Headers $headers
    Write-Host "[OK] Backup result: $($backupResult.message)" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}
