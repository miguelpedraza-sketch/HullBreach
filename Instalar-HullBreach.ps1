# Instala o actualiza Hull Breach a la última versión publicada y lo ejecuta.
# No usa la API de GitHub (no tiene límite de consultas): lee la redirección de /releases/latest.
param([switch]$SinJugar)

$ErrorActionPreference = 'Stop'
$repo    = 'miguelpedraza-sketch/HullBreach'
$destino = Join-Path $PSScriptRoot 'Game'          # misma carpeta que usa el launcher
$tmp     = Join-Path $PSScriptRoot 'Game.tmp'
$zip     = Join-Path $env:TEMP 'HullBreach_update.zip'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

try {
    Write-Host 'Buscando la última versión...'
    $req = [Net.HttpWebRequest]::Create("https://github.com/$repo/releases/latest")
    $req.Method = 'HEAD'; $req.AllowAutoRedirect = $false; $req.UserAgent = 'HullBreachInstaller'
    $resp = $req.GetResponse(); $loc = $resp.Headers['Location']; $resp.Close()
    if (-not $loc -or $loc -notmatch '/tag/(.+)$') { throw 'No se pudo leer la última versión.' }
    $tag = [Uri]::UnescapeDataString($Matches[1]).Trim()
    $ver = $tag.TrimStart('v', 'V')
    $url = "https://github.com/$repo/releases/download/$tag/HullBreach-$ver.zip"

    $verFile = Join-Path $destino 'version.txt'
    $exe     = Join-Path $destino 'HullBreach.exe'
    $instalada = if ((Test-Path $verFile) -and (Test-Path $exe)) { (Get-Content $verFile -Raw).Trim() } else { $null }

    if ($instalada -eq $tag) {
        Write-Host "Ya tienes la última versión ($ver)."
    } else {
        Write-Host ("Instalada: {0}  ->  Última: {1}" -f $(if ($instalada) { $instalada } else { 'ninguna' }), $tag)
        Write-Host "Descargando $url"
        if (Get-Command curl.exe -ErrorAction SilentlyContinue) {
            & curl.exe -L --fail -# -o $zip $url
            if ($LASTEXITCODE -ne 0) { throw "La descarga falló (código $LASTEXITCODE)." }
        } else {
            (New-Object Net.WebClient).DownloadFile($url, $zip)
        }

        Write-Host 'Instalando...'
        if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force }
        New-Item -ItemType Directory $tmp | Out-Null
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $tmpFull = (Resolve-Path $tmp).Path
        $za = [IO.Compression.ZipFile]::OpenRead($zip)
        try {
            foreach ($en in $za.Entries) {
                if ($en.FullName.EndsWith('/') -or $en.FullName.EndsWith('\')) { continue }   # carpetas
                $dest = [IO.Path]::GetFullPath((Join-Path $tmp $en.FullName))
                if (-not $dest.StartsWith($tmpFull, [StringComparison]::OrdinalIgnoreCase)) { continue }
                [void](New-Item -ItemType Directory -Force (Split-Path $dest))
                [IO.Compression.ZipFileExtensions]::ExtractToFile($en, $dest, $true)
            }
        } finally { $za.Dispose() }
        Set-Content -Path (Join-Path $tmp 'version.txt') -Value $tag -NoNewline
        if (Test-Path $destino) { Remove-Item $destino -Recurse -Force }
        Move-Item $tmp $destino
        Remove-Item $zip -Force -ErrorAction SilentlyContinue
        Write-Host "Listo: Hull Breach $ver instalado en $destino"
    }

    if (-not $SinJugar) { Write-Host 'Iniciando el juego...'; Start-Process $exe -WorkingDirectory $destino }
}
catch {
    Write-Host ''
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if (Test-Path $tmp) { Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue }
    Read-Host 'Pulsa Enter para cerrar'
    exit 1
}
