:: ----------------------------------------------------------------------
:: defining variables which will be later used for executing S7 CLI

@echo off
set S7CLI=\\cern.ch\dfs\Users\m\mzapolsk\Documents\training\S7CLI\s7cli.exe
set projectRoot=\\cern.ch\dfs\Users\m\mzapolsk\Documents\training
set newProjectName=KnightRider
set newProjectDirPath=%projectRoot%\%newProjectName%
set existingProjectDirPath=%newProjectDirPath%\KnightRi
set projectConfigPath=%projectRoot%\simatic300.CFG
set symbolsPath=%newProjectDirPath%\Output\S7InstanceGenerator\Symbol.sdf
set S7ProgramName="S7 Program(1)"
set libProjectName=ucpc_plc_siemens_6_3
set libProjectProgramName=BASELINE_S7
set destinationProjectProgramName="S7 Program(1)"

if ""%1""=="""" goto :all
goto %1

call :all
goto :eof

:all
call :createProject
call :importConfig
call :importSymbols
call :importLibSources
call :importLibBlocks
call :importSources
call :compile
goto :eof

:createProject
:: ----------------------------------------------------------------------
:: creating new project

@echo on
%S7CLI% createProject --projname %newProjectName% --projdir %newProjectDirPath%
@echo off
goto :eof

:importConfig
:: ----------------------------------------------------------------------
:: importing config to existing project

@echo on
%S7CLI% importConfig -p %existingProjectDirPath% -c %projectConfigPath%
@echo off
goto :eof

:importSymbols
:: ----------------------------------------------------------------------
:: importing symbols to existing project

:: %S7CLI% importSymbols -p %existingProjectDirPath% -s %symbolsPath%

:: ----------------------------------------------------------------------
:: importing symbols to existing project with the program name different that "S7 Program(1)"

@echo on
%S7CLI% importSymbols -p %existingProjectDirPath% -s %symbolsPath% --program %S7ProgramName%
@echo off
goto :eof

:importLibSources
:: ----------------------------------------------------------------------
:: importing sources from library

@echo on
%S7CLI% importLibSources --project %existingProjectDirPath% --program %destinationProjectProgramName% --library %libProjectName% --libprg %libProjectProgramName% 
@echo off
goto :eof

:importLibBlocks
:: ----------------------------------------------------------------------
:: importing blocks from library

@echo on
%S7CLI% importLibBlocks --project %existingProjectDirPath% --program %destinationProjectProgramName% --library %libProjectName% --libprg %libProjectProgramName% 
@echo off
goto :eof

:importSources
:: ----------------------------------------------------------------------
:: importing instance and logic sources

@echo on
%S7CLI% importSourcesDir --project %existingProjectDirPath% --program %destinationProjectProgramName% --srcdir %newProjectDirPath%\Output\S7InstanceGenerator 
%S7CLI% importSourcesDir --project %existingProjectDirPath% --program %destinationProjectProgramName% --srcdir %newProjectDirPath%\Output\S7LogicGenerator 
@echo off

goto :eof

:compile
:: ----------------------------------------------------------------------
:: compiling sources

@echo on
%S7CLI% compileSources --project %existingProjectDirPath% --program %destinationProjectProgramName% --sources 1_Compilation_Baseline,2_Compilation_instance,3_Compilation_LOGIC,4_Compilation_OB
@echo off

goto :eof
