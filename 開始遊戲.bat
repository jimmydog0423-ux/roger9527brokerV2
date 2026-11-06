@echo off
cd /d "%~dp0"
if not exist "Build\水蛇洞經紀人.exe" (
  echo 找不到遊戲執行檔 Build\水蛇洞經紀人.exe
  echo 請先執行「輸出遊戲.bat」把遊戲輸出一次。
  pause
  exit /b 1
)
start "" "Build\水蛇洞經紀人.exe"
