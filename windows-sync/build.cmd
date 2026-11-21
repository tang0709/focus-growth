@echo off
setlocal
set "FG_SOURCE=%~dp0"
if exist "%FG_SOURCE%..\.git" if exist "%FG_SOURCE%..\preparation.js" (
  if not exist "%FG_SOURCE%web" mkdir "%FG_SOURCE%web"
  copy /y "%FG_SOURCE%..\index.html" "%FG_SOURCE%web\index.html" >nul
  copy /y "%FG_SOURCE%..\sync.js" "%FG_SOURCE%web\sync.js" >nul
  copy /y "%FG_SOURCE%..\preparation.js" "%FG_SOURCE%web\preparation.js" >nul
)
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /out:"%FG_SOURCE%FocusGrowthPrepare.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Net.Http.dll /reference:System.Web.Extensions.dll /reference:System.Security.dll "%FG_SOURCE%Core.cs" "%FG_SOURCE%Transport.cs" "%FG_SOURCE%Bridge.cs" "%FG_SOURCE%Program.cs" "%FG_SOURCE%Tests.cs" "%FG_SOURCE%Preparation.cs" "%FG_SOURCE%PreparationProgram.cs" "%FG_SOURCE%PreparationTests.cs"
exit /b %errorlevel%
