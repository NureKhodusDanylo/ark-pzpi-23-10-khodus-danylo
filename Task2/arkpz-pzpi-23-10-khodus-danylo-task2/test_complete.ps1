# Complete test with fresh data
$baseUrl = "http://localhost:5102/api"

Write-Host "=== COMPLETE API TEST ===" -ForegroundColor Cyan

# Login as user
$userLoginData = @{
    email = "john@test.com"
    password = "john123"
} | ConvertTo-Json

$userLoginResponse = Invoke-RestMethod -Uri "$baseUrl/User/login" -Method POST -Body $userLoginData -ContentType "application/json"
$userToken = $userLoginResponse.token
$userHeaders = @{ Authorization = "Bearer $userToken" }

# Login as admin
$adminLoginData = @{
    email = "admin@test.com"
    password = "admin123"
} | ConvertTo-Json

$adminLoginResponse = Invoke-RestMethod -Uri "$baseUrl/User/login" -Method POST -Body $adminLoginData -ContentType "application/json"
$adminToken = $adminLoginResponse.token
$adminHeaders = @{ Authorization = "Bearer $adminToken" }

Write-Host "Logged in successfully" -ForegroundColor Green

# Get existing data
$nodes = Invoke-RestMethod -Uri "$baseUrl/Node" -Method GET -Headers $adminHeaders
$robots = Invoke-RestMethod -Uri "$baseUrl/Robot" -Method GET -Headers $adminHeaders

Write-Host "Nodes: $($nodes.Count), Robots: $($robots.Count)" -ForegroundColor Cyan

# Create a fresh order with Pending status
Write-Host "`nCreating new order..." -ForegroundColor Yellow
$newOrder = @{
    name = "Test Package"
    description = "Testing assignment"
    weight = 1.5
    price = 75.00
    recipientId = 3
    pickupNodeId = $nodes[0].id
    dropoffNodeId = $nodes[1].id
} | ConvertTo-Json

$createdOrder = Invoke-RestMethod -Uri "$baseUrl/Order" -Method POST -Body $newOrder -ContentType "application/json" -Headers $userHeaders
Write-Host "Created Order ID: $($createdOrder.id), Status: $($createdOrder.status)" -ForegroundColor Green

# TEST 1: Assign Robot to Order
Write-Host "`n=== TEST 1: Assign Robot ===" -ForegroundColor Magenta
try {
    $assignData = @{ robotId = $robots[0].id } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/Order/$($createdOrder.id)/assign-robot" -Method POST -Body $assignData -ContentType "application/json" -Headers $userHeaders
    Write-Host "[OK] Robot assigned successfully" -ForegroundColor Green

    # Verify assignment
    $updatedOrder = Invoke-RestMethod -Uri "$baseUrl/Order/$($createdOrder.id)" -Method GET -Headers $userHeaders
    Write-Host "[OK] Order status: $($updatedOrder.status), Robot: $($updatedOrder.robotName)" -ForegroundColor Green
} catch {
    Write-Host "[FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

# TEST 2: Database Backup
Write-Host "`n=== TEST 2: Database Backup ===" -ForegroundColor Magenta
try {
    $backupData = @{ backupPath = "Backups" } | ConvertTo-Json
    $backupResult = Invoke-RestMethod -Uri "$baseUrl/Admin/backup" -Method POST -Body $backupData -ContentType "application/json" -Headers $adminHeaders
    Write-Host "[OK] $($backupResult.message)" -ForegroundColor Green

    # Check if backup files exist
    if (Test-Path "Backups") {
        $backupFiles = Get-ChildItem "Backups" | Select-Object -First 2
        Write-Host "[OK] Found $($backupFiles.Count) backup files" -ForegroundColor Green
    }
} catch {
    Write-Host "[FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails.Message) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Red
    }
}

Write-Host "`n=== TEST COMPLETE ===" -ForegroundColor Cyan
