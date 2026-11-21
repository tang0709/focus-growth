@echo off
setlocal
set "FG_ROOT=%~dp0"
if not exist "%FG_ROOT%FocusGrowthPrepare.exe" copy /y "%FG_ROOT%..\FocusGrowthPrepare.exe" "%FG_ROOT%FocusGrowthPrepare.exe" >nul
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /optimize+ /out:"%FG_ROOT%FocusGrowthFirstMigration.exe" /reference:"%FG_ROOT%FocusGrowthPrepare.exe" /reference:System.Net.Http.dll /reference:System.Security.dll "%FG_ROOT%FirstMigration.cs" "%FG_ROOT%MigrationTests.cs"
exit /b %errorlevel%
