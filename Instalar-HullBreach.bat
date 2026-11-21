@echo off
rem Instala/actualiza Hull Breach y lo ejecuta. Doble clic.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Instalar-HullBreach.ps1" %*
