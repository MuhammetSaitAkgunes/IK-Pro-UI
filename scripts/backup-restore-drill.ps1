<#
.SYNOPSIS
    İK Pro veritabanı için yedek alma + geri yükleme tatbikatı.

.DESCRIPTION
    Yedeğin VAR OLMASI yeterli değildir; geri yüklenebildiği kanıtlanmalıdır.
    Bu script tam yedek alır, yedeği AYRI bir veritabanı adına geri yükler,
    tablo satır sayılarını kaynakla karşılaştırır ve tatbikat kopyasını düşürür.

    GÜVENLİK: Geri yükleme her zaman "<Database>_RestoreDrill" adına yapılır.
    Hedef adın kaynakla aynı olması reddedilir; mevcut veri asla üzerine yazılmaz.

.PARAMETER Database
    Yedeklenecek veritabanı (ör. IKProDb).

.PARAMETER BackupPath
    Yedek dosyasının yazılacağı klasör. SQL Server servis hesabının bu klasöre
    yazma izni olmalıdır.

.PARAMETER ServerInstance
    SQL Server örneği. Varsayılan: localhost

.PARAMETER KeepRestoredCopy
    Verilirse tatbikat kopyası silinmez (elle inceleme için).

.EXAMPLE
    pwsh scripts/backup-restore-drill.ps1 -Database IKProDb -BackupPath C:\yedek

.OUTPUTS
    Çıkış kodu 0 = tatbikat başarılı. 1 = başarısız (yedek, geri yükleme veya
    doğrulama adımlarından biri).
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Database,
    [Parameter(Mandatory = $true)][string]$BackupPath,
    [string]$ServerInstance = "localhost",
    [switch]$KeepRestoredCopy
)

$ErrorActionPreference = "Stop"
$drillDatabase = "${Database}_RestoreDrill"

# --- Koruma: tatbikat hedefi kaynakla aynı olamaz -------------------------
if ($drillDatabase -eq $Database) {
    Write-Error "Tatbikat hedefi kaynak veritabanıyla aynı olamaz ($Database). Geri yükleme iptal edildi."
    exit 1
}

function Invoke-Sql {
    param([string]$Query, [string]$OnDatabase = "master", [string]$Separator = "|")
    # Sütun ayracı açıkça verilir: dosya yolları boşluk içerebilir
    # ("C:\Program Files\..."), boşlukla ayrıştırma yolu ortadan böler.
    $output = & sqlcmd -S $ServerInstance -d $OnDatabase -E -b -h -1 -W -s $Separator -Q $Query 2>&1
    if ($LASTEXITCODE -ne 0) { throw "SQL hatası: $output" }
    return $output
}

function Get-TableRowCounts {
    param([string]$OnDatabase)
    # sys.partitions üzerinden satır sayısı: büyük tabloda COUNT(*) taramasından hızlı.
    $query = @"
SET NOCOUNT ON;
SELECT t.name + '=' + CAST(SUM(p.rows) AS varchar(20))
FROM sys.tables t
JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0,1)
GROUP BY t.name
ORDER BY t.name;
"@
    $rows = Invoke-Sql -Query $query -OnDatabase $OnDatabase
    $map = @{}
    foreach ($line in $rows) {
        $text = "$line".Trim()
        if ($text -match '^(.+)=(\d+)$') { $map[$Matches[1]] = [int]$Matches[2] }
    }
    return $map
}

try {
    if (-not (Test-Path $BackupPath)) { New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null }
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $backupFile = Join-Path (Resolve-Path $BackupPath) "$Database-$stamp.bak"

    # Kaynak sayımı yedekten ÖNCE alınır. Yedek anlık görüntüdür; yedek sırasında
    # yazma olursa geri yüklenen kopya CANLI kaynakla birebir tutmaz. Önce/sonra
    # aralığı alınıp geri yüklenen değerin bu aralıkta olması beklenir — aksi hâlde
    # trafik alan bir üretim veritabanında tatbikat sürekli yanlış alarm verirdi.
    $before = Get-TableRowCounts -OnDatabase $Database

    Write-Host "[1/5] Yedek alınıyor: $backupFile"
    Invoke-Sql -Query "BACKUP DATABASE [$Database] TO DISK = N'$backupFile' WITH INIT, CHECKSUM, STATS = 25;" | Out-Null

    Write-Host "[2/5] Yedek bütünlüğü doğrulanıyor (RESTORE VERIFYONLY)"
    Invoke-Sql -Query "RESTORE VERIFYONLY FROM DISK = N'$backupFile' WITH CHECKSUM;" | Out-Null

    Write-Host "[3/5] Ayrı isme geri yükleniyor: $drillDatabase"
    $fileList = Invoke-Sql -Query "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK = N'$backupFile';"
    $moves = @()
    foreach ($line in $fileList) {
        # FILELISTONLY sütunları: LogicalName | PhysicalName | Type | ...
        $parts = "$line" -split '\|'
        if ($parts.Count -ge 2 -and $parts[1].Trim() -match '\.(mdf|ndf|ldf)$') {
            $logical = $parts[0].Trim()
            $ext = [System.IO.Path]::GetExtension($parts[1].Trim())
            $newPath = Join-Path (Resolve-Path $BackupPath) "$drillDatabase-$logical$ext"
            $moves += "MOVE N'$logical' TO N'$newPath'"
        }
    }
    if ($moves.Count -eq 0) { throw "Yedek dosya listesi okunamadı; geri yükleme yapılamıyor." }

    Invoke-Sql -Query @"
IF DB_ID('$drillDatabase') IS NOT NULL
BEGIN
    ALTER DATABASE [$drillDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [$drillDatabase];
END
RESTORE DATABASE [$drillDatabase] FROM DISK = N'$backupFile' WITH $($moves -join ', '), REPLACE, RECOVERY;
"@ | Out-Null

    Write-Host "[4/5] Satır sayıları karşılaştırılıyor"
    $after = Get-TableRowCounts -OnDatabase $Database
    $restored = Get-TableRowCounts -OnDatabase $drillDatabase

    $mismatches = @()
    $drifted = 0
    foreach ($table in $before.Keys) {
        if (-not $restored.ContainsKey($table)) { $mismatches += "${table}: geri yüklemede YOK"; continue }

        $low = [Math]::Min($before[$table], $after[$table])
        $high = [Math]::Max($before[$table], $after[$table])
        if ($low -ne $high) { $drifted++ }

        if ($restored[$table] -lt $low -or $restored[$table] -gt $high) {
            $mismatches += "${table}: geri yükleme $($restored[$table]), beklenen aralık $low-$high"
        }
    }

    Write-Host ("       karşılaştırılan tablo: {0}, toplam satır: {1}" -f `
        $before.Count, ($before.Values | Measure-Object -Sum).Sum)
    if ($drifted -gt 0) {
        Write-Host "       not: yedek sırasında $drifted tabloda yazma oldu; aralık kontrolü uygulandı"
    }

    if ($mismatches.Count -gt 0) {
        Write-Host "TATBİKAT BAŞARISIZ — satır sayıları tutmuyor:" -ForegroundColor Red
        $mismatches | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        exit 1
    }

    Write-Host "[5/5] Temizlik"
    if ($KeepRestoredCopy) {
        Write-Host "       tatbikat kopyası korundu: $drillDatabase"
    }
    else {
        Invoke-Sql -Query @"
ALTER DATABASE [$drillDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [$drillDatabase];
"@ | Out-Null
        Write-Host "       tatbikat kopyası düşürüldü"
    }

    Write-Host ""
    Write-Host "TATBİKAT BAŞARILI — yedek alındı, geri yüklendi ve doğrulandı." -ForegroundColor Green
    Write-Host "Yedek dosyası: $backupFile"
    exit 0
}
catch {
    Write-Host "TATBİKAT BAŞARISIZ: $_" -ForegroundColor Red
    exit 1
}
