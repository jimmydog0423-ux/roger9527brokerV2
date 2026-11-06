@echo off
cd /d "%~dp0"
set "UNITY=C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
if not exist "%UNITY%" (
  echo 找不到 Unity 6000.6.4f1：
  echo %UNITY%
  echo 如果你裝在別的位置，請用記事本打開這個檔案，修改第三行的路徑。
  pause
  exit /b 1
)
echo 正在輸出遊戲，大約需要一到三分鐘。
echo 輸出前請先關閉 Unity 編輯器，否則會失敗。
"%UNITY%" -batchmode -quit -projectPath "%~dp0Game" -buildTarget Win64 -executeMethod SSD.EditorTools.SsdBuild.BuildWindows -logFile "%~dp0build.log"
if errorlevel 1 (
  echo.
  echo 輸出失敗，詳細原因請看 build.log。
  pause
  exit /b 1
)
echo.
echo 完成！之後直接點「開始遊戲.bat」，或打開 Build\水蛇洞經紀人.exe 就能玩。
pause
