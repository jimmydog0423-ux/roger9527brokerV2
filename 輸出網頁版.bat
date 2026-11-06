@echo off
setlocal
cd /d "%~dp0"
set "UNITY=%ProgramFiles%\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
if not exist "%UNITY%" (
  echo Unity 6000.6.4f1 was not found. Install it with the Web Build Support module.
  pause
  exit /b 1
)
echo Close the Unity Editor before exporting. Building the Web version...
start "" /wait "%UNITY%" -batchmode -quit -projectPath "%~dp0Game" -buildTarget WebGL -executeMethod SSD.EditorTools.SsdBuild.BuildWeb -logFile "%~dp0build-web.log"
if errorlevel 1 (
  echo Build failed. See build-web.log.
  pause
  exit /b 1
)
echo Build complete. Serve the WebBuild folder over HTTP.
pause
