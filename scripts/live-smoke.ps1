<#
  End-to-end smoke test against a deployed From The Kitchen API.

  Signs in as the Admin (asks for the password; nothing is saved), creates one throwaway
  customer (smoke-<random>@example.test), places one order with it and walks the order
  through the workflow. That customer and order stay in the database afterwards.

  Usage:  .\scripts\live-smoke.ps1 -AdminEmail you@example.com
          .\scripts\live-smoke.ps1 -BaseUrl https://other-host -AdminEmail you@example.com
#>
param(
    [string]$BaseUrl = 'https://ftk-api-dang-a5hdhxc4a4c8asa5.italynorth-01.azurewebsites.net',
    [Parameter(Mandatory)][string]$AdminEmail
)

$ErrorActionPreference = 'Stop'
$script:pass = 0; $script:fail = 0

function Call([string]$Method, [string]$Path, $Body = $null, [string]$Token = $null) {
    $headers = @{}
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    $params = @{ Method = $Method; Uri = "$BaseUrl$Path"; Headers = $headers; UseBasicParsing = $true; TimeoutSec = 120 }
    if ($null -ne $Body) { $params.Body = ($Body | ConvertTo-Json -Depth 6); $params.ContentType = 'application/json' }
    try {
        $r = Invoke-WebRequest @params
        $code = [int]$r.StatusCode; $content = $r.Content
    } catch {
        if (-not $_.Exception.Response) { throw }
        $code = [int]$_.Exception.Response.StatusCode
        $content = (New-Object IO.StreamReader($_.Exception.Response.GetResponseStream())).ReadToEnd()
    }
    $json = $null
    if ($content) { try { $json = $content | ConvertFrom-Json } catch { } }
    [pscustomobject]@{ Code = $code; Json = $json }
}

function Check([string]$Name, $Expected, $Actual) {
    if ("$Expected" -eq "$Actual") { $script:pass++; Write-Host "PASS  $Name ($Actual)" -ForegroundColor Green }
    else { $script:fail++; Write-Host "FAIL  $Name - expected $Expected, got $Actual" -ForegroundColor Red }
}

function UserId([string]$Jwt) {
    $payload = $Jwt.Split('.')[1].Replace('-', '+').Replace('_', '/')
    $payload += '=' * ((4 - $payload.Length % 4) % 4)
    ([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload)) | ConvertFrom-Json).id
}

# ---- Admin
$secure = Read-Host "Password for $AdminEmail" -AsSecureString
$adminPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
$r = Call POST /api/Auth/login @{ email = $AdminEmail; password = $adminPassword }
Remove-Variable adminPassword
Check 'admin login' 200 $r.Code
if ($r.Code -ne 200) { Write-Host 'Stopping: admin login failed.' -ForegroundColor Red; exit 1 }
$admin = $r.Json.result.token
Check 'admin token has Admin role' 'Admin' $r.Json.result.role
Check 'admin-only endpoint works for admin' 200 (Call GET /api/AuthTest/1 -Token $admin).Code

# ---- Throwaway customer
$email = "smoke-$([guid]::NewGuid().ToString('N').Substring(0, 10))@example.test"
$password = "Smoke!$([guid]::NewGuid().ToString('N').Substring(0, 12))Aa1"
Check 'register customer' 200 (Call POST /api/Auth/register @{ email = $email; password = $password; name = 'Smoke Test'; role = 'Customer' }).Code
$r = Call POST /api/Auth/login @{ email = $email; password = $password }
Check 'customer login' 200 $r.Code
$customer = $r.Json.result.token
$customerId = UserId $customer
Check 'customer token has Customer role' 'Customer' $r.Json.result.role
Check 'customer cannot use admin-only endpoint' 403 (Call GET /api/AuthTest/1 -Token $customer).Code
Check 'customer cannot create an Admin' 403 (Call POST /api/Auth/register @{ email = "x$email"; password = $password; name = 'x'; role = 'Admin' } $customer).Code

# ---- Order: owner comes from the token, not the body
$order = @{
    pickUpName = 'Smoke Test'; phoneNumber = '555-0100'; pickUpEmail = $email
    applicationUserId = 'someone-else'; orderTotal = 16.48; totalItem = 2
    orderDetailsDTO = @(
        @{ menuItemId = 1; quantity = 1; itemName = 'Bruschetta'; price = 6.99 },
        @{ menuItemId = 2; quantity = 1; itemName = 'Crispy Calamari'; price = 9.49 }
    )
}
$r = Call POST /api/OrderHeader $order $customer
Check 'place order' 201 $r.Code
$orderId = $r.Json.result.orderHeaderId
Check 'order belongs to the caller, not the body' $customerId $r.Json.result.applicationUserId
Check 'new order is Confirmed' 'Confirmed' $r.Json.result.status

$r = Call GET "/api/OrderHeader/$orderId" -Token $customer
Check 'customer reads own order' 200 $r.Code
Check 'order has 2 lines' 2 @($r.Json.result.orderDetails).Count
$lineId = @($r.Json.result.orderDetails)[0].orderDetailId

$r = Call GET '/api/OrderHeader?userId=someone-else' -Token $customer
Check 'customer list only shows own orders' 0 @($r.Json.result | Where-Object { $_.applicationUserId -ne $customerId }).Count

# ---- Status workflow
Check 'customer cannot move order forward' 403 (Call PUT "/api/OrderHeader/$orderId" @{ orderHeaderId = $orderId; status = 'Ready For Pick Up' } $customer).Code
Check 'admin cannot skip a step' 400 (Call PUT "/api/OrderHeader/$orderId" @{ orderHeaderId = $orderId; status = 'Completed' } $admin).Code
Check 'admin: Confirmed -> Ready For Pick Up' 200 (Call PUT "/api/OrderHeader/$orderId" @{ orderHeaderId = $orderId; status = 'Ready For Pick Up' } $admin).Code
Check 'admin: Ready For Pick Up -> Completed' 200 (Call PUT "/api/OrderHeader/$orderId" @{ orderHeaderId = $orderId; status = 'Completed' } $admin).Code
Check 'completed order cannot be cancelled' 400 (Call PUT "/api/OrderHeader/$orderId" @{ orderHeaderId = $orderId; status = 'Cancelled' } $customer).Code

# ---- Ratings and admin views
Check 'customer rates own item' 200 (Call PUT "/api/OrderDetails/$lineId" @{ orderDetailId = $lineId; rating = 5 } $customer).Code
Check 'admin cannot rate a customer item' 403 (Call PUT "/api/OrderDetails/$lineId" @{ orderDetailId = $lineId; rating = 1 } $admin).Code
$r = Call GET "/api/OrderHeader/$orderId" -Token $admin
Check 'admin reads any order' 200 $r.Code
Check 'rating was saved' 5 @($r.Json.result.orderDetails | Where-Object orderDetailId -eq $lineId)[0].rating
Check 'final status is Completed' 'Completed' $r.Json.result.status
$r = Call GET "/api/OrderHeader?userId=$customerId" -Token $admin
Check 'admin filters orders by user' 1 @($r.Json.result).Count

Write-Host "---- $($script:pass) passed, $($script:fail) failed   (left behind: customer $email, order #$orderId)"
if ($script:fail -gt 0) { exit 1 }
