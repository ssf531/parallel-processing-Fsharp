if exist functionCode.exe del functionCode.exe
fsc  -r:Hopac.dll -r:Hopac.Core.dll -r:Hopac.Platform.dll functionCode.fs
pause
