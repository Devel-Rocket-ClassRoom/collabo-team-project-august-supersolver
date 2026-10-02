@echo off
rem targetStage.txt 에 적은 스테이지 하나만 솔버에 굴린다.
rem 실제 작업은 run_solver.ps1 이 한다. 콘솔에 진행을 흘리려면 그쪽이 편하다.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run_solver.ps1"
exit /b %ERRORLEVEL%
