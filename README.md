# Hull Breach

Juego de arena con vehículos armados (Unity). Este repositorio contiene el **launcher** y publica el juego en *Releases*.

## Jugar
1. Descarga `HullBreachLauncher.exe` desde la última Release (o de la carpeta `launcher/`).
2. Ábrelo: pulsa **INSTALAR/ACTUALIZAR** y luego **PLAY**. PLAY solo se habilita con la última versión instalada.

## Instalación rápida (sin launcher) — Windows (carpeta `windows/`)
Descarga `windows/Instalar-HullBreach.bat` y `windows/Instalar-HullBreach.ps1` (en la misma carpeta) y haz doble clic en el `.bat`: descarga la última versión, la instala en `Game/` y abre el juego. Si ya la tienes, solo la abre. No usa la API de GitHub, así que no se bloquea por el límite de consultas.

Descarga directa del zip: `https://github.com/miguelpedraza-sketch/HullBreach/releases/latest` (archivo `HullBreach-X.Y.zip`).

## Linux x86_64 (carpeta `linux/`)
Descarga `linux/instalar-hullbreach.sh`, dale permisos y ejecútalo:
```
chmod +x instalar-hullbreach.sh
./instalar-hullbreach.sh          # instala/actualiza y juega
./instalar-hullbreach.sh --check  # solo comprueba si hay versión nueva
./instalar-hullbreach.sh --force  # reinstala
```
Instala en `Game-linux/`. Necesita `curl` o `wget` y `unzip` (o `python3`). La versión de Linux se publica en la release fija `linux-latest` (archivos `HullBreach-linux.zip` y `version.txt`), así que no afecta al instalador de Windows.

## Publicar una versión nueva
1. Compilar el juego como `HullBreach.exe` y comprimir la carpeta en `HullBreach-X.Y.zip` (el .exe en la raíz del zip).
2. `gh release create vX.Y HullBreach-X.Y.zip --title "Hull Breach X.Y" --notes-file notas.md`
3. Los launchers leen la última Release y muestran sus notas en "Novedades".

El código del launcher está en `launcher/Launcher.cs` (compila con el `csc.exe` de .NET Framework 4).

### Publicar la versión de Linux
1. Compilar para Linux x64 como `HullBreach.x86_64` y comprimir la carpeta en `HullBreach-linux.zip` (el ejecutable en la raíz).
2. Escribir la versión en `version.txt` y subir ambos: `gh release upload linux-latest HullBreach-linux.zip version.txt --clobber` (la primera vez, `gh release create linux-latest ... --latest=false`).
