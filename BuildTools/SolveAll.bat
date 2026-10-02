@echo off
rem Levels 폴더의 스테이지를 전부 솔버에 굴린다.
rem 실제 작업은 run_solver.ps1 이 한다. 콘솔에 진행을 흘리려면 그쪽이 편하다.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_solver.ps1" -All
exit /b %ERRORLEVEL%
