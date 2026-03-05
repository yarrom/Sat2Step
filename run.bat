@echo off
REM Run SimpleSatParser
rem dotnet run --project SimpleSatParser -- test_files/Dice.sat test_output/Dice.stp
rem dotnet run --project SimpleSatParser -- test_files/CopperBar.sat test_output/CopperBar.stp 

REM Run SatStepConverter
dotnet run --project SatStepConverter -- test_files/Dice.sat test_output/Dice.step
dotnet run --project SatStepConverter -- test_files/CopperBar.sat test_output/CopperBar.step

pause
