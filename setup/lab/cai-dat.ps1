# Cách C của setup/HUONG_DAN_PHONG_MAY.md bằng một lệnh: tải MySQL 8.4, Node.js 22, .NET SDK 10 và mã nguồn vào một
# thư mục, tạo cơ sở dữ liệu, build trang web và API. Không cần quyền admin, không cần Docker; cần Internet.
# Chạy một lần; chạy lại thì bỏ qua phần đã có (xoá thư mục mã nguồn trong thư mục cài để lấy bản mới).
#
#   powershell -ExecutionPolicy Bypass -File cai-dat.ps1                       # mã nguồn tải từ GitHub (nhánh main)
#   powershell -ExecutionPolicy Bypass -File cai-dat.ps1 -SourceZip D:\ma-nguon.zip   # mã nguồn chép sang máy
#
# Sau đó: powershell -ExecutionPolicy Bypass -File chay.ps1
param(
    [string]$Root = "",
    [string]$SourceZip = "",
    [string]$RootPassword = "RootThu2026",
    [string]$ApiPassword = "ApiThu2026",
    [int]$DbPort = 3306
)

# PowerShell 5.1 turns any stderr line of a program (npm, mysqladmin warnings) into an error under "Stop": programs
# are checked by their exit code instead, cmdlets that must not fail carry -ErrorAction Stop.
$ErrorActionPreference = "Continue"
$ProgressPreference = "SilentlyContinue"  # Invoke-WebRequest tải chậm hẳn khi vẽ thanh tiến độ
if (-not $Root) { $Root = if (Test-Path "D:\") { "D:\examprep" } else { "$env:USERPROFILE\examprep" } }
New-Item -ItemType Directory -Force $Root | Out-Null
$Downloads = Join-Path $Root "tai-ve"
New-Item -ItemType Directory -Force $Downloads | Out-Null

function Step($text) { Write-Host ""; Write-Host "== $text" -ForegroundColor Cyan }

function Fetch($url, $name) {
    $file = Join-Path $Downloads $name
    if (-not (Test-Path $file)) {
        Write-Host "Tải $url"
        Invoke-WebRequest $url -OutFile "$file.part" -UseBasicParsing -ErrorAction Stop
        Move-Item "$file.part" $file -ErrorAction Stop
    }
    return $file
}

# Giải nén một .zip có một thư mục gốc (mysql-8.4.6-winx64, node-v22…) thành $dest.
function Unzip($zip, $dest) {
    if (Test-Path $dest) { return }
    $tmp = "$dest.tmp"
    if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
    New-Item -ItemType Directory $tmp | Out-Null
    tar -xf $zip -C $tmp 2>$null
    if ($LASTEXITCODE -ne 0) {
        # tar.exe of Windows cannot read Vietnamese file names in a zip; Expand-Archive can, more slowly.
        Remove-Item $tmp -Recurse -Force
        Expand-Archive $zip $tmp -ErrorAction Stop
    }
    $inner = @(Get-ChildItem $tmp)
    if ($inner.Count -eq 1 -and $inner[0].PSIsContainer) { Move-Item $inner[0].FullName $dest -ErrorAction Stop; Remove-Item $tmp -Recurse -Force }
    else { Move-Item $tmp $dest -ErrorAction Stop }
}

function WaitPort($port, $seconds) {
    for ($i = 0; $i -lt $seconds; $i++) {
        $c = New-Object Net.Sockets.TcpClient
        try { $c.Connect("127.0.0.1", $port); return } catch { Start-Sleep 1 } finally { $c.Dispose() }
    }
    throw "Cổng $port không mở sau $seconds giây"
}

Write-Host "Thư mục cài: $Root"

Step "1/6 MySQL 8.4"
Unzip (Fetch "https://cdn.mysql.com/archives/mysql-8.4/mysql-8.4.6-winx64.zip" "mysql.zip") (Join-Path $Root "mysql")
$bin = Join-Path $Root "mysql\bin"
$data = Join-Path $Root "mysql-data"
if (-not (Test-Path $data)) {
    & "$bin\mysqld.exe" --initialize-insecure --console "--datadir=$data"
    if ($LASTEXITCODE -ne 0) { throw "MySQL không khởi tạo được. Nếu báo thiếu MSVCP140.dll hoặc VCRUNTIME140.dll: cần cài Microsoft Visual C++ Redistributable (nhờ phụ trách phòng máy)." }
    $mysqld = Start-Process "$bin\mysqld.exe" -ArgumentList "--console", "--datadir=$data", "--port=$DbPort", "--bind-address=127.0.0.1", "--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci" -PassThru -WindowStyle Minimized
    WaitPort $DbPort 60
    $sql = "ALTER USER 'root'@'localhost' IDENTIFIED BY '$RootPassword'; CREATE DATABASE examprep CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci; CREATE USER 'examprep'@'localhost' IDENTIFIED BY '$ApiPassword'; GRANT ALL ON examprep.* TO 'examprep'@'localhost';"
    & "$bin\mysql.exe" -uroot "--port=$DbPort" -e $sql
    if ($LASTEXITCODE -ne 0) { throw "Không tạo được cơ sở dữ liệu" }
    & "$bin\mysqladmin.exe" -uroot "-p$RootPassword" "--port=$DbPort" shutdown 2>$null
    $mysqld.WaitForExit(30000) | Out-Null
    Write-Host "Đã tạo cơ sở dữ liệu examprep"
}
else { Write-Host "Đã có $data, bỏ qua khởi tạo" }

Step "2/6 Node.js 22"
Unzip (Fetch "https://nodejs.org/dist/v22.20.0/node-v22.20.0-win-x64.zip" "node.zip") (Join-Path $Root "node")

Step "3/6 .NET SDK 10"
$dotnetDir = Join-Path $Root "dotnet"
if (-not (Test-Path (Join-Path $dotnetDir "dotnet.exe"))) {
    $installer = Fetch "https://dot.net/v1/dotnet-install.ps1" "dotnet-install.ps1"
    & powershell -ExecutionPolicy Bypass -File $installer -Channel 10.0 -InstallDir $dotnetDir
    if ($LASTEXITCODE -ne 0) { throw "Không cài được .NET SDK 10" }
}

Step "4/6 Mã nguồn"
$src = Join-Path $Root "cambridge-exam-prep"
if (-not (Test-Path $src)) {
    $zip = if ($SourceZip) { (Resolve-Path $SourceZip).Path } else { Fetch "https://github.com/trhlow/cambridge-exam-prep/archive/refs/heads/main.zip" "ma-nguon.zip" }
    Unzip $zip $src
}
else { Write-Host "Đã có $src (xoá thư mục này rồi chạy lại để lấy bản mới)" }

$env:PATH = "$dotnetDir;$(Join-Path $Root 'node');$env:PATH"
$env:DOTNET_ROOT = $dotnetDir
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

Step "5/6 Trang web (npm ci, npm run build)"
Push-Location (Join-Path $src "src\frontend")
try {
    npm ci --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw "npm ci lỗi" }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build lỗi" }
}
finally { Pop-Location }

Step "6/6 API (dotnet build)"
Push-Location (Join-Path $src "src\backend")
try {
    dotnet build src/ExamPrep.Api -v q
    if ($LASTEXITCODE -ne 0) { throw "dotnet build lỗi" }
}
finally { Pop-Location }

Write-Host ""
Write-Host "Cài xong. Chạy hệ thống: powershell -ExecutionPolicy Bypass -File `"$(Join-Path $src 'setup\lab\chay.ps1')`" -Root `"$Root`"" -ForegroundColor Green
