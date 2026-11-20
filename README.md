# Hull Breach

Juego de arena con vehículos armados (Unity). Este repositorio contiene el **launcher** y publica el juego en *Releases*.

## Jugar
1. Descarga `HullBreachLauncher.exe` desde la última Release (o de la carpeta `launcher/`).
2. Ábrelo: pulsa **INSTALAR/ACTUALIZAR** y luego **PLAY**. PLAY solo se habilita con la última versión instalada.

## Publicar una versión nueva
1. Compilar el juego como `HullBreach.exe` y comprimir la carpeta en `HullBreach-X.Y.zip` (el .exe en la raíz del zip).
2. `gh release create vX.Y HullBreach-X.Y.zip --title "Hull Breach X.Y" --notes-file notas.md`
3. Los launchers leen la última Release y muestran sus notas en "Novedades".

El código del launcher está en `launcher/Launcher.cs` (compila con el `csc.exe` de .NET Framework 4).
