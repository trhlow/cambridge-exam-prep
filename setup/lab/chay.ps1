# Bật hệ thống đã cài bằng cai-dat.ps1: ba cửa sổ (MySQL, API, trang web), đợi từng phần sẵn sàng, rồi in địa chỉ cho
# máy con. Tắt: đóng ba cửa sổ đó (cửa sổ MySQL cuối cùng), hoặc chạy lại với -Stop.
#
#   powershell -ExecutionPolicy Bypass -File chay.ps1 [-Root D:\examprep] [-Port 8080]
#   powershell -ExecutionPolicy Bypass -File chay.ps1 -Stop
param(
    [string]$Root = "",
    [int]$Port = 8080,
    [int]$DbPort = 3306,
    [string]$RootPassword = "RootThu2026",
    [string]$ApiPassword = "ApiThu2026",
    [switch]$Stop
)

# PowerShell 5.1 turns any stderr line of a program (npm, mysqladmin warnings) into an error under "Stop": programs
# are checked by their exit code instead, cmdlets that must not fail carry -ErrorAction Stop.
$ErrorActionPreference = "Continue"
if (-not $Root) { $Root = if (Test-Path "D:\examprep") { "D:\examprep" } else { "$env:USERPROFILE\examprep" } }
$bin = Join-Path $Root "mysql\bin"
$src = Join-Path $Root "cambridge-exam-prep"
if (-not (Test-Path $src)) { throw "Chưa cài ở ${Root}: chạy cai-dat.ps1 trước" }

if ($Stop) {
    Get-Process node, dotnet, ExamPrep.Api -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "$Root*" } | Stop-Process -Force
    & "$bin\mysqladmin.exe" -uroot "-p$RootPassword" "--port=$DbPort" shutdown 2>$null
    Write-Host "Đã tắt"
    return
}

function Wait($what, $test, $seconds) {
    Write-Host -NoNewline "Đợi $what"
    for ($i = 0; $i -lt $seconds; $i++) {
        if (& $test) { Write-Host " xong"; return }
        Write-Host -NoNewline "."
        Start-Sleep 2
    }
    throw "$what chưa sẵn sàng sau $($seconds * 2) giây: xem cửa sổ tương ứng"
}

function PortOpen($port) {
    $c = New-Object Net.Sockets.TcpClient
    try { $c.Connect("127.0.0.1", $port); return $true } catch { return $false } finally { $c.Dispose() }
}

function Ok($url) {
    try { return (Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200 } catch { return $false }
}

$path = "`$env:PATH = '$Root\dotnet;$Root\node;' + `$env:PATH; `$env:DOTNET_ROOT = '$Root\dotnet'"

if (-not (PortOpen $DbPort)) {
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "`$host.UI.RawUI.WindowTitle = '1 MySQL'; & '$bin\mysqld.exe' --console '--datadir=$Root\mysql-data' --port=$DbPort --bind-address=127.0.0.1 --character-set-server=utf8mb4 --collation-server=utf8mb4_0900_ai_ci --max-connections=300"
}
Wait "MySQL (cổng $DbPort)" { PortOpen $DbPort } 30

if (-not (Ok "http://localhost:5180/health")) {
    $api = "$path; `$host.UI.RawUI.WindowTitle = '2 API'; `$env:ConnectionStrings__ExamPrep = 'server=localhost;port=$DbPort;database=examprep;user=examprep;password=$ApiPassword'; `$env:Database__MigrateOnStart = 'true'; cd '$src\src\backend'; dotnet run --project src/ExamPrep.Api --launch-profile http"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", $api
}
Wait "API (lần đầu mất 1–2 phút để tạo bảng)" { Ok "http://localhost:5180/health" } 120

if (-not (Ok "http://localhost:$Port/api/site")) {
    $web = "$path; `$host.UI.RawUI.WindowTitle = '3 Web'; cd '$src\src\frontend'; npx vite preview --host 0.0.0.0 --port $Port --strictPort"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", $web
}
Wait "trang web (cổng $Port)" { Ok "http://localhost:$Port/api/site" } 30

$ips = Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notmatch '^(127\.|169\.254\.)' -and $_.InterfaceAlias -notmatch 'vEthernet|Loopback|WSL' } | Select-Object -ExpandProperty IPAddress
Write-Host ""
Write-Host "Hệ thống đã chạy. Địa chỉ cho máy con:" -ForegroundColor Green
foreach ($ip in $ips) {
    Write-Host "  Thí sinh:              http://${ip}:$Port"
    Write-Host "  Giám thị, quản trị:    http://${ip}:$Port/staff"
}

if (-not (Get-NetFirewallRule -DisplayName "Victory exam ($Port)" -ErrorAction SilentlyContinue)) {
    Write-Host ""
    Write-Host "Chưa có luật tường lửa cho cổng ${Port}: máy con sẽ không vào được. Người có quyền admin chạy một lần:" -ForegroundColor Yellow
    Write-Host "  New-NetFirewallRule -DisplayName `"Victory exam ($Port)`" -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow -Profile Any"
}
