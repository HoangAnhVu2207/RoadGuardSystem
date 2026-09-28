$ErrorActionPreference = 'Stop'
# This harness never creates or drops databases; the caller supplies an isolated test DB connection.
# Cleanup is performed by the caller against the exact named smoke database only.
Add-Type -AssemblyName System.Net.Http

$baseUrl = $env:AUTH_SMOKE_BASE_URL
$sqlConnectionString = $env:AUTH_SMOKE_SQL_CONNECTION_STRING
$operatorEmail = $env:AUTH_SMOKE_OPERATOR_EMAIL
$operatorPassword = $env:AUTH_SMOKE_OPERATOR_PASSWORD
$crewEmail = $env:AUTH_SMOKE_CREW_EMAIL
$crewPassword = $env:AUTH_SMOKE_CREW_PASSWORD
$logoutEmail = $env:AUTH_SMOKE_LOGOUT_EMAIL
$logoutPassword = $env:AUTH_SMOKE_LOGOUT_PASSWORD
$replacementPassword = $env:AUTH_SMOKE_REPLACEMENT_PASSWORD

function Invoke-JsonRequest {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body,
        [hashtable]$Headers = @{}
    )

    $request = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::$Method, "$baseUrl$Path")
    foreach ($header in $Headers.GetEnumerator()) { [void]$request.Headers.TryAddWithoutValidation($header.Key, $header.Value) }
    if ($null -ne $Body) { $request.Content = New-Object System.Net.Http.StringContent(($Body | ConvertTo-Json -Compress), [System.Text.Encoding]::UTF8, 'application/json') }
    $response = $script:httpClient.SendAsync($request).GetAwaiter().GetResult()
    $content = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    $responseHeaders = @{}
    foreach ($header in $response.Headers) { $responseHeaders[$header.Key] = ($header.Value -join ',') }
    foreach ($header in $response.Content.Headers) { $responseHeaders[$header.Key] = ($header.Value -join ',') }
    $parsed = $null
    if ($content) {
        try { $parsed = $content | ConvertFrom-Json } catch { }
    }
    [pscustomobject]@{ Status = [int]$response.StatusCode; Headers = $responseHeaders; Body = $parsed; Raw = $content }
}

function Get-Claim {
    param([string]$Token, [string]$Name)
    $payload = $Token.Split('.')[1].Replace('-', '+').Replace('_', '/')
    while (($payload.Length % 4) -ne 0) { $payload += '=' }
    $bytes = [Convert]::FromBase64String($payload)
    (($bytes | ForEach-Object { [char]$_ }) -join '') | ConvertFrom-Json | Select-Object -ExpandProperty $Name
}

function Invoke-Scalar {
    param([string]$Sql)
    $connection = New-Object System.Data.SqlClient.SqlConnection($sqlConnectionString)
    try {
        $connection.Open()
        $command = $connection.CreateCommand()
        $command.CommandText = $Sql
        return $command.ExecuteScalar()
    } finally { $connection.Dispose() }
}

function Assert-Status {
    param($Response, [int]$Expected, [string]$Name)
    if ($Response.Status -ne $Expected) { throw "$Name expected HTTP $Expected, got $($Response.Status)" }
    Write-Output "$Name status=$($Response.Status)"
}

$script:httpClient = New-Object System.Net.Http.HttpClient

$login = Invoke-JsonRequest 'POST' '/api/v1/auth/login' @{ email = $operatorEmail; password = $operatorPassword }
Assert-Status $login 200 'login'
$operatorAccess = $login.Body.accessToken
$operatorRefresh = $login.Body.refreshToken
$operatorUserId = Get-Claim $operatorAccess 'sub'
$operatorSessionId = Get-Claim $operatorAccess 'sid'
Write-Output "login tokenPair=$([bool]($operatorAccess -and $operatorRefresh)) role=$($login.Body.user.role)"

$refresh = Invoke-JsonRequest 'POST' '/api/v1/auth/refresh' @{ refreshToken = $operatorRefresh }
Assert-Status $refresh 200 'refresh'
$rotatedRefresh = $refresh.Body.refreshToken
Write-Output "refresh rotated=$([bool]($rotatedRefresh -and $rotatedRefresh -ne $operatorRefresh))"

$replay = Invoke-JsonRequest 'POST' '/api/v1/auth/refresh' @{ refreshToken = $operatorRefresh }
Assert-Status $replay 401 'refresh-replay'
$operatorRevoked = Invoke-Scalar "SELECT COUNT(1) FROM [Sessions] WHERE [Id] = '$operatorSessionId' AND [RevokedAt] IS NOT NULL"
Write-Output "refresh-replay durableSessionRevoked=$([int]$operatorRevoked -eq 1)"

$logoutLogin = Invoke-JsonRequest 'POST' '/api/v1/auth/login' @{ email = $logoutEmail; password = $logoutPassword }
Assert-Status $logoutLogin 200 'logout-login'
$logoutAccess = $logoutLogin.Body.accessToken
$logoutSessionId = Get-Claim $logoutAccess 'sid'
$logoutKey = 'auth-smoke-logout-001'
$logout = Invoke-JsonRequest 'POST' '/api/v1/auth/logout' $null @{ Authorization = "Bearer $logoutAccess"; 'Idempotency-Key' = $logoutKey }
Assert-Status $logout 204 'logout'
$logoutRevoked = Invoke-Scalar "SELECT COUNT(1) FROM [Sessions] WHERE [Id] = '$logoutSessionId' AND [RevokedAt] IS NOT NULL"
Write-Output "logout durableSessionRevoked=$([int]$logoutRevoked -eq 1)"

$recoveryKnown = Invoke-JsonRequest 'POST' '/api/v1/auth/password-recovery-requests' @{ email = $operatorEmail }
Assert-Status $recoveryKnown 202 'recovery-known'
Write-Output "recovery-known location=$([bool]$recoveryKnown.Headers.Location) bodyEmpty=$([string]::IsNullOrEmpty($recoveryKnown.Raw))"
$recoveryUnknown = Invoke-JsonRequest 'POST' '/api/v1/auth/password-recovery-requests' @{ email = 'missing.auth.smoke@example.test' }
Assert-Status $recoveryUnknown 202 'recovery-unknown'
Write-Output "recovery-unknown location=$([bool]$recoveryUnknown.Headers.Location)"
$recoveryRows = Invoke-Scalar "SELECT COUNT(1) FROM [PasswordRecoveryRequests]"
Write-Output "recovery durableRows=$recoveryRows"

$crewLogin = Invoke-JsonRequest 'POST' '/api/v1/auth/login' @{ email = $crewEmail; password = $crewPassword }
Assert-Status $crewLogin 200 'change-password-login'
$crewAccess = $crewLogin.Body.accessToken
$crewUserId = Get-Claim $crewAccess 'sub'
$crewSessionId = Get-Claim $crewAccess 'sid'
$changeKey = 'auth-smoke-change-password-001'
$change = Invoke-JsonRequest 'POST' '/api/v1/auth/change-password' @{ currentPassword = $crewPassword; newPassword = $replacementPassword } @{ Authorization = "Bearer $crewAccess"; 'Idempotency-Key' = $changeKey }
Assert-Status $change 204 'change-password'
$crewRevoked = Invoke-Scalar "SELECT COUNT(1) FROM [Sessions] WHERE [UserId] = '$crewUserId' AND [RevokedAt] IS NOT NULL"
Write-Output "change-password durableRevokedSessions=$crewRevoked"
$oldPasswordLogin = Invoke-JsonRequest 'POST' '/api/v1/auth/login' @{ email = $crewEmail; password = $crewPassword }
Assert-Status $oldPasswordLogin 401 'change-password-old-password'
$newPasswordLogin = Invoke-JsonRequest 'POST' '/api/v1/auth/login' @{ email = $crewEmail; password = $replacementPassword }
Assert-Status $newPasswordLogin 200 'change-password-new-password'
Write-Output 'AUTH_SMOKE_PASS'
