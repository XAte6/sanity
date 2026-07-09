@echo off
setlocal

set ROOT=%~dp0
set BIN=%ROOT%bin
if not exist "%BIN%" mkdir "%BIN%"

where gradle >nul 2>&1
if errorlevel 1 (
    echo Gradle not found. Install Gradle or Android Studio, then run:
    echo   cd android
    echo   gradle :app:assembleRelease
    exit /b 1
)

pushd "%ROOT%"
gradle :app:assembleRelease
if errorlevel 1 (
    echo Android build failed.
    popd
    exit /b 1
)

copy /Y "app\build\outputs\apk\release\app-release-unsigned.apk" "%BIN%\Sanity.apk" >nul
if errorlevel 1 (
    copy /Y "app\build\outputs\apk\release\app-release.apk" "%BIN%\Sanity.apk" >nul
)

popd
echo Built %BIN%\Sanity.apk
