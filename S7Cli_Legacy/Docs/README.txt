
                                      
                  _|_|_|_|_|            _|  _|
          _|_|_|          _|    _|_|_|  _|    
        _|_|            _|    _|        _|  _|
            _|_|      _|      _|        _|  _|
        _|_|_|      _|          _|_|_|  _|  _|

        Command-line interface for Siemens SIMATIC Step7(tm)
        (C) 2013-2019 CERN, TE-CRG-CE

        Authors: Michal Dudek, Tomasz Wolak
===============================================================================

S7cli is a simple command line interface to Siemens SIMATIC Step7(r)
(programming environment for Siemens Programmable Logic Controllers, PLCs).

It was created to fill the gap in tools for automation scripting that would
allow to manage (in particular - build) SIMATIC projects in an automated way
(without human interaction, ie. without using graphical interface).

See 'Usage' below as a summary of available functionality.


Installation
============
It is a stand-alone executable. Just copy it where it is convenient to start it from.
See also the INSTALL file.


Usage
=====

    s7cli <command> [command args] [-h]

Option -h displays help for specified command.


Available commands:

  createProject
      - Create new, empty project in specified location

  createLib
      - Create a new, empty library in specified location

  listProjects
      - List available Simatic projects

  listPrograms
      - List available programs in Simatic project/library

  listContainers
      - List available containers in Simatic project/library

  listStations
      - List available stations in Simatic project/library

  importConfig
      - Import station configuration from a file

  exportConfig
      - Export station configuration to a file

  importSymbols
      - Import program symbols from a file

  exportSymbols
      - Export program symbols to a file

  listSources
      - List of source code modules in a program

  listBlocks
      - List of blocks in specified program

  importLibSources
      - Import all sources from a library to a project

  importLibBlocks
      - Import all blocks from a library to a project

  importSources
      - Import specified source code files

  importSourcesDir
      - Import all source code files from specified directory
        (valid sources: .SCL, .AWL, .INP, .GR7)

  compileSources
      - Compile specified source code module(s)

  exportSources
      - Export specified source code module(s)

  exportAllSources
      - Export all source code module(s) from a program

  exportProgramStructure
      - Exports the block calling structure into a DIF-File
        (experimental, not tested!!!)

  compileStation
      - Compiles station hardware and connections (experimental, don't use it!!!)

  compileAllStations
      - Compiles all stations' hardware and connections

  downloadSystemData
      - Downloads "System data" to the PLC

  downloadBlocks
      - Downloads blocks (omits "System data") to the PLC

  download
      - Downloads all blocks (including system data) for a given program to the PLC

  startCPU
      - Starts (new start) PLC

  stopCPU
      - Stops PLC



