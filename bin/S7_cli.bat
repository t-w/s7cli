@echo off
cls
set error_return=0
::----------------------------------------------------------------------
:: path to the S7 command line interface

set s7_cli_path=H:\user\m\mdudek\scm\svn\te_crg_ce\automation\s7cli\s7cli\bin\Debug\s7cli.exe

::----------------------------------------------------------------------
:: UAB
set uab_project_path=D:\MDudek\Projects\CPC6_TEST_PLC\CPC6.generator\1.4
set uab_project_instance_dir=%uab_project_path%\Output\S7InstanceGenerator
set uab_project_logic_dir=%uab_project_path%\Output\S7LogicGenerator
set uab_project_symbols=%uab_project_instance_dir%\Symbol.sdf
set uab_project_compilation_file=1_Compilation_Baseline,2_Compilation_instance,3_Compilation_LOGIC,4_Compilation_OB

::----------------------------------------------------------------------
:: SIEMENS configuration

:: siemens project configuration
set siemens_project_name=CPC6_TEST_PLC
set siemens_new_project_path=D:\MDudek\Projects\CPC6_TEST_PLC\Siemens
set siemens_project_path=D:\MDudek\Projects\CPC6_TEST_PLC\Siemens\%siemens_project_name:~0,8%
set siemens_project_config=D:\MDudek\Projects\CPC6_TEST_PLC\Siemens\simatic300.CFG
set siemens_project_program_name="S7 Program(1)"

:: siemens baseline configuration
set siemens_baseline_project_name=ucpc_plc_siemens_6_3
set siemens_baseline_project_program_name=BASELINE_S7

::----------------------------------------------------------------------

if "%1"=="" goto :eof
if "%1"=="new" goto :new
if "%1"=="existing" goto :existing
goto %1
goto :eof

:new
call :createNewProject
call :importConfig
call :importSymbols
call :importLibSources
call :importLibBlocks
call :importInstanceSources
call :importLogicSources
call :compile
goto :eof

:existing
call :importSymbols
call :importLibSources
call :importLibBlocks
call :importInstanceSources
call :importLogicSources
call :compile
call :stopCPU
call :downloadSystemData
call :downloadAllBlocks
call :startCPU
goto :eof

:error
@echo off
echo.
echo Last command was unsuccessful! Exiting *.bat file!
goto :eof

::----------------------------------------------------------------------
:: below are only batch labels and execution of the s7 command line interface

: createNewProject
::----------------------------------------------------------------------
:: creating new project

@echo on
%s7_cli_path% createProject --projname %siemens_project_name% --projdir %siemens_new_project_path%
@echo off
goto :eof

:importConfig
:: ----------------------------------------------------------------------
:: importing config to existing project

@echo on
%s7_cli_path% importConfig --project %siemens_project_name% --config %siemens_project_config%
@echo off
goto :eof

:importSymbols
:: ----------------------------------------------------------------------
:: importing symbols to existing project with the program name different that "S7 Program(1)"

@echo on
%s7_cli_path% importSymbols --project %siemens_project_name% --symbols %uab_project_symbols% --program %siemens_project_program_name%
@echo off
goto :eof

:importLibSources
:: ----------------------------------------------------------------------
:: importing sources from library

@echo on
%s7_cli_path% importLibSources --project %siemens_project_name% --program %siemens_project_program_name% --library %siemens_baseline_project_name% --libprg %siemens_baseline_project_program_name% 
@echo off
goto :eof

:importLibBlocks
:: ----------------------------------------------------------------------
:: importing blocks from library

@echo on
%s7_cli_path% importLibBlocks --project %siemens_project_name% --program %siemens_project_program_name% --library %siemens_baseline_project_name% --libprg %siemens_baseline_project_program_name%
@echo off
goto :eof

:importInstanceSources
:: ----------------------------------------------------------------------
:: importing instance and logic sources

@echo on
%s7_cli_path% importSourcesDir --project %siemens_project_name% --program %siemens_project_program_name% --srcdir %uab_project_instance_dir% 
@echo off
goto :eof


:importLogicSources
:: ----------------------------------------------------------------------
:: importing instance and logic sources

@echo on
%s7_cli_path% importSourcesDir --project %siemens_project_name% --program %siemens_project_program_name% --srcdir %uab_project_logic_dir% 
@echo off
goto :eof

:compile
:: ----------------------------------------------------------------------
:: compiling sources

@echo on
%s7_cli_path% compileSources --project %siemens_project_name% --program %siemens_project_program_name% --sources %uab_project_compilation_file%
@echo off
goto :eof

:downloadSystemData
:: ----------------------------------------------------------------------
:: downloading all blocks to the CPU
@echo on
%s7_cli_path% downloadSystemData --project %siemens_project_name% --program %siemens_project_program_name% --force y
@echo off
goto :eof

:downloadAllBlocks
:: ----------------------------------------------------------------------
:: downloading all blocks to the CPU
@echo on
%s7_cli_path% downloadAllBlocks --project %siemens_project_name% --program %siemens_project_program_name% --force y
@echo off
goto :eof

:startCPU
:: ----------------------------------------------------------------------
:: starting CPU
@echo on
%s7_cli_path% startCPU --project %siemens_project_name% --program %siemens_project_program_name%
@echo off
goto :eof

:stopCPU
:: ----------------------------------------------------------------------
:: stopping CPU
@echo on
%s7_cli_path% stopCPU --project %siemens_project_name% --program %siemens_project_program_name%
@echo off
goto :eof