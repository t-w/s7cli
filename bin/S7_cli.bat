:: ----------------------------------------------------------------------
:: defining variables which will be later used for executing S7 CLI

@echo off
set newProjectName=new_proj
set newProjectDirPath=D:\MDudek\Projects\S7_CLI\tests
set existingProjectDirPath=D:\MDudek\Projects\S7_CLI\tests\new_proj
set projectConfigPath=D:\MDudek\Projects\S7_CLI\tests\CFP_SHC4_ARC45.CFG
set symbolsPath=D:\MDudek\Projects\S7_CLI\tests\Symbol.sdf
set S7ProgramName=S7_prog_name
set libProjectName=ucpc_plc_siemens_6_1
set libProjectProgramName=BASELINE_S7
set destinationProjectProgramName="S7 Program(1)"

@echo on
:: ----------------------------------------------------------------------
:: creating new project

s7_cli.exe createProject -n %newProjectName% -d %newProjectDirPath%

:: ----------------------------------------------------------------------
:: importing config to existing project

s7_cli.exe importProjectConfig -d %existingProjectDirPath% -c %projectConfigPath%

:: ----------------------------------------------------------------------
:: importing symbols to existing project

s7_cli.exe importSymbols -d %existingProjectDirPath% -s %symbolsPath%

:: ----------------------------------------------------------------------
:: importing symbols to existing project with the program name different that "S7 Program(1)"

::s7_cli.exe importSymbols -d %existingProjectDirPath% -s %symbolsPath% -p %S7ProgramName%

:: ----------------------------------------------------------------------
:: importing sources from library

s7_cli.exe importLibSources -d %existingProjectDirPath% --lib-proj-name %libProjectName% --lib-proj-prog-name %libProjectProgramName% --dest-proj-prog-name %destinationProjectProgramName%

:: ----------------------------------------------------------------------
:: importing blocks from library

s7_cli.exe importLibBlocks -d %existingProjectDirPath% --lib-proj-name %libProjectName% --lib-proj-prog-name %libProjectProgramName% --dest-proj-prog-name %destinationProjectProgramName%