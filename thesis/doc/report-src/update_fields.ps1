# Opens the built report in Word, fills the table of contents, the lists of tables and figures and the caption
# numbers, saves it, and optionally exports a PDF.
#   powershell -ExecutionPolicy Bypass -File thesis/doc/report-src/update_fields.ps1 [-Pdf <path>] [-Doc <docx>]
param([string]$Pdf = "", [string]$Doc = "")

if ($Doc -eq "") { $Doc = Join-Path $PSScriptRoot "..\BaoCao_DoAnChuyenNganh_TranHoangLong.docx" }
$docPath = (Resolve-Path $Doc).Path   # not $doc: PowerShell names ignore case and $Doc is a [string]
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $d = $word.Documents.Open($docPath)
    # twice: page numbers in the lists move once the lists themselves take their place
    for ($i = 0; $i -lt 2; $i++) {
        $d.Fields.Update() | Out-Null
        foreach ($t in $d.TablesOfContents) { $t.Update() | Out-Null }
        foreach ($t in $d.TablesOfFigures) { $t.Update() | Out-Null }
    }
    $d.Save()
    if ($Pdf -ne "") {
        $d.ExportAsFixedFormat($Pdf, 17)  # wdExportFormatPDF
        Write-Output "pdf $Pdf"
    }
    Write-Output ("pages " + $d.ComputeStatistics(2))
    $d.Close()
}
finally {
    $word.Quit()
}
