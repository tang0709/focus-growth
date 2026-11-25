@echo off
setlocal
cd /d "%~dp0"
set "FG_CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%FG_CSC%" exit /b 2
if exist "..\device-session.js" (
  if not exist "web" mkdir "web"
  copy /y "..\index.html" "web\index.html" >nul
  copy /y "..\sync.js" "web\sync.js" >nul
  copy /y "..\preparation.js" "web\preparation.js" >nul
  copy /y "..\device-session.js" "web\device-session.js" >nul
)
if not exist "web\index.html" (
  if not exist "..\sync.js" exit /b 3
  if not exist "web" mkdir "web"
  copy /y "..\index.html" "web\index.html" >nul
  copy /y "..\sync.js" "web\sync.js" >nul
  copy /y "..\preparation.js" "web\preparation.js" >nul
)
"%FG_CSC%" /nologo /target:winexe /out:FocusGrowthSyncV04.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Net.Http.dll /r:System.Web.Extensions.dll /r:System.Security.dll Core.cs Transport.cs Bridge.cs Preparation.cs Tests.cs PreparationTests.cs Continuous.cs ContinuousBridge.cs ContinuousProgram.cs ContinuousTests.cs DeviceSessions.cs DesktopIntegration.cs DesktopActivity.cs
exit /b %errorlevel%
