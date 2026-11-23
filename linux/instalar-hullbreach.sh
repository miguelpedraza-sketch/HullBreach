#!/usr/bin/env bash
# Instalador de Hull Breach para Linux (x86_64).
# Instala o actualiza el juego a la última versión publicada para Linux y lo ejecuta.
# No usa la API de GitHub (sin límite de consultas): descarga ficheros de una Release fija.
#
# Uso:   ./instalar-hullbreach.sh            instala/actualiza y juega
#        ./instalar-hullbreach.sh --no-play  solo instala/actualiza
#        ./instalar-hullbreach.sh --check    solo dice si hay versión nueva
#        ./instalar-hullbreach.sh --force    reinstala aunque ya sea la última
set -u

REPO="miguelpedraza-sketch/HullBreach"
TAG="linux-latest"
BASE="https://github.com/$REPO/releases/download/$TAG"
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GAME="$DIR/Game-linux"
EXE="$GAME/HullBreach.x86_64"
VERFILE="$GAME/version.txt"

PLAY=1; CHECK=0; FORCE=0
for a in "$@"; do
  case "$a" in
    --no-play) PLAY=0 ;;
    --check)   CHECK=1; PLAY=0 ;;
    --force)   FORCE=1 ;;
    -h|--help) sed -n '2,10p' "$0"; exit 0 ;;
  esac
done

say()  { printf '\033[1;36m[Hull Breach]\033[0m %s\n' "$*"; }
fail() {
  printf '\033[1;31m[Hull Breach] %s\033[0m\n' "$*" >&2
  command -v zenity >/dev/null 2>&1 && [ -n "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ] && zenity --error --title="Hull Breach" --text="$*" 2>/dev/null
  exit 1
}

# Descarga con curl o wget (con barra de progreso en la terminal).
fetch() { # fetch URL DESTINO
  if command -v curl >/dev/null 2>&1; then curl -L --fail -# -o "$2" "$1"
  elif command -v wget >/dev/null 2>&1; then wget -O "$2" --show-progress -q "$1"
  else fail "Necesitas curl o wget para descargar el juego."; fi
}
fetch_text() {
  if command -v curl >/dev/null 2>&1; then curl -fsSL "$1" 2>/dev/null
  elif command -v wget >/dev/null 2>&1; then wget -qO- "$1" 2>/dev/null
  else return 1; fi
}

unzip_to() { # unzip_to ZIP CARPETA
  if command -v unzip >/dev/null 2>&1; then unzip -q -o "$1" -d "$2"
  elif command -v bsdtar >/dev/null 2>&1; then bsdtar -xf "$1" -C "$2"
  elif command -v python3 >/dev/null 2>&1; then python3 - "$1" "$2" <<'PY'
import sys, zipfile
zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])
PY
  else fail "Necesitas unzip (o python3) para instalar el juego."; fi
}

say "Buscando la última versión..."
LATEST="$(fetch_text "$BASE/version.txt" | tr -d '[:space:]')"
[ -z "$LATEST" ] && LATEST=""
INSTALLED=""
[ -f "$VERFILE" ] && [ -x "$EXE" ] && INSTALLED="$(tr -d '[:space:]' < "$VERFILE")"

if [ -z "$LATEST" ]; then
  if [ -n "$INSTALLED" ]; then say "Sin conexión con GitHub: se usa la versión instalada ($INSTALLED)."
  else fail "No se pudo consultar la última versión y no hay ninguna instalada. Revisa tu conexión."; fi
else
  say "Instalada: ${INSTALLED:-ninguna}  ->  Última: $LATEST"
  if [ "$CHECK" = 1 ]; then
    [ "$INSTALLED" = "$LATEST" ] && say "Estás al día." || say "Hay una versión nueva."
    exit 0
  fi
  if [ "$INSTALLED" != "$LATEST" ] || [ "$FORCE" = 1 ]; then
    ZIP="$(mktemp /tmp/hullbreach-XXXXXX.zip)"
    TMP="$DIR/Game-linux.tmp"
    say "Descargando HullBreach-linux.zip ..."
    fetch "$BASE/HullBreach-linux.zip" "$ZIP" || { rm -f "$ZIP"; fail "La descarga falló."; }
    say "Instalando..."
    rm -rf "$TMP"; mkdir -p "$TMP"
    unzip_to "$ZIP" "$TMP" || { rm -rf "$TMP" "$ZIP"; fail "No se pudo descomprimir."; }
    rm -f "$ZIP"
    # Si el zip trae una carpeta raíz, súbela un nivel.
    if [ ! -e "$TMP/HullBreach.x86_64" ]; then
      SUB="$(find "$TMP" -maxdepth 2 -name HullBreach.x86_64 -print -quit)"
      [ -n "$SUB" ] && { mv "$(dirname "$SUB")" "$TMP.inner" && rm -rf "$TMP" && mv "$TMP.inner" "$TMP"; }
    fi
    [ -e "$TMP/HullBreach.x86_64" ] || { rm -rf "$TMP"; fail "El paquete descargado no contiene el juego."; }
    chmod +x "$TMP/HullBreach.x86_64"
    printf '%s' "$LATEST" > "$TMP/version.txt"
    rm -rf "$GAME"; mv "$TMP" "$GAME"
    say "Listo: Hull Breach $LATEST instalado en $GAME"
  else
    say "Ya tienes la última versión."
  fi
fi

[ "$PLAY" = 1 ] || exit 0
[ -x "$EXE" ] || chmod +x "$EXE" 2>/dev/null
say "Iniciando el juego..."
cd "$GAME" && exec "$EXE"
