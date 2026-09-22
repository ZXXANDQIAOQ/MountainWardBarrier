@echo off
rem ===========================================================================
rem  MountainWardBarrier - one-click Android APK build
rem ===========================================================================
rem  Double-click this file to build the APK. No Unity GUI needed.
rem
rem  Output:  Build\Android\MountainWardBarrier.apk
rem  Log:     Build\Android\build.log      (send this file if the build fails)
rem
rem  Before the first run you must activate a Unity license once, by opening
rem  Unity Hub and signing in (Personal / free is enough). Unity refuses to
rem  build without a license.
rem
rem  NOTE: keep this file ASCII-only. Non-ASCII text in a .bat gets mangled by
rem  the OEM code page and produces garbled paths.
rem ===========================================================================

setlocal enableextensions

rem -------------------------------------------------------------- locate Unity
set "UNITY_EXE="
if exist "E:\App\Unity\2022.3.62f1\Editor\Unity.exe" (
  set "UNITY_EXE=E:\App\Unity\2022.3.62f1\Editor\Unity.exe"
)

if not defined UNITY_EXE (
  echo [i] Unity.exe not at the default path, searching E:\App\Unity ...
  for /f "delims=" %%P in ('dir /b /s "E:\App\Unity\Unity.exe" 2^>nul') do (
    if not defined UNITY_EXE set "UNITY_EXE=%%P"
  )
)

if not defined UNITY_EXE (
  echo.
  echo [ERROR] Unity.exe not found.
  echo         Open this .bat in a text editor and set UNITY_EXE by hand.
  echo.
  pause
  exit /b 1
)

echo [i] Unity : %UNITY_EXE%

rem ------------------------------------------------------------------ project
set "PROJ=%~dp0.."
for %%P in ("%PROJ%") do set "PROJ=%%~fP"

if not exist "%PROJ%\Assets" (
  echo [ERROR] Assets folder not found under "%PROJ%"
  echo         This .bat must stay inside the project's tools\ folder.
  pause
  exit /b 1
)

echo [i] Project: %PROJ%

set "OUTDIR=%PROJ%\Build\Android"
if not exist "%OUTDIR%" mkdir "%OUTDIR%" >nul 2>nul

set "LOG=%OUTDIR%\build.log"

rem --------------------------------------------------------------------- build
echo.
echo ---------------------------------------------------------------
echo  Building ... this takes a while (first IL2CPP build is slow)
echo  Log: %LOG%
echo ---------------------------------------------------------------
echo.

pushd "%PROJ%"
"%UNITY_EXE%" -batchmode -nographics -quit -accept-apiupdate ^
  -projectPath "%PROJ%" ^
  -executeMethod MountainWardBarrier.EditorTools.AndroidBuilder.BuildFromCommandLine ^
  -logFile "%LOG%"
set "CODE=%ERRORLEVEL%"
popd

rem ------------------------------------------------------------------- result
echo.
if "%CODE%"=="0" if exist "%OUTDIR%\MountainWardBarrier.apk" (
  echo [OK] APK is ready:
  echo      %OUTDIR%\MountainWardBarrier.apk
  echo.
  echo      Install to a connected phone with:
  echo      adb install -r "%OUTDIR%\MountainWardBarrier.apk"
  echo.
  pause
  exit /b 0
)

echo [FAIL] Unity exited with code %CODE%
echo.

findstr /c:"No valid Unity Editor license" "%LOG%" >nul 2>nul
if not errorlevel 1 (
  echo [HINT] Unity has no license yet.
  echo        Open Unity Hub, sign in, and activate a Personal license once.
  echo        This step cannot be automated.
  echo.
)

findstr /c:"error CS" "%LOG%" >nul 2>nul
if not errorlevel 1 (
  echo [HINT] Compile errors found. Grep the log for "error CS":
  echo        findstr /n "error CS" "%LOG%"
  echo.
)

echo Open the log for details:
echo   %LOG%
echo.
pause
exit /b %CODE%
