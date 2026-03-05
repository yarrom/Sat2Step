@echo off
REM Run SimpleSatParser
dotnet run --project SimpleSatParser -- test_files/Dice.sat test_output/Dice.stp
dotnet run --project SimpleSatParser -- test_files/CopperBar.sat test_output/CopperBar.stp 

pause
