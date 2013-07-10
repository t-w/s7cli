
PROJECTNAME=Sector56
PROJECT=`pwd | xargs cygpath -w `\\Sector56
UAB_PROJECT=D:\\controls\\apps\\sector56\\uab
BASELINE=ucpc_plc_siemens_6_4
#BASELINE=$((cygpath -w '/cygdrive/d/controls/cpc6/plc/baseline/ucpc-plc-siemens-6.4'))
S7CLI=s7cli


ARC_APP="S7 Program ARC56"
LSS_APP="S7 Program LSS56"

CLIDEBUG=1



#TARGETS=

all:    create_empty \
	unzip_base \
	import_baseline \
	import_symbols \
	import_generated_sources \
	compile



create_empty:
	rm -rf Sector56/
	${S7CLI} createProject --projname ${PROJECTNAME} --projdir D:\\controls\\apps\\sector56\\plc

unzip_base:
	#rm -rf Sector56/*
	unzip -o t:\\Controls\\Services\\Jenkins\\input\\plc_base\\plc_base_Sector56.zip

import_baseline:
	${S7CLI} importLibBlocks -p ${PROJECT} --program ${ARC_APP} -l ${BASELINE} --libprg "BASELINE_S7" --debug ${CLIDEBUG}
	${S7CLI} importLibBlocks -p ${PROJECT} --program ${LSS_APP} -l ${BASELINE} --libprg "BASELINE_S7" --debug ${CLIDEBUG}
	${S7CLI} importLibSources -p ${PROJECT} --program ${ARC_APP} -l ${BASELINE} --libprg "BASELINE_S7" --debug ${CLIDEBUG}
	${S7CLI} importLibSources -p ${PROJECT} --program ${LSS_APP} -l ${BASELINE} --libprg "BASELINE_S7" --debug ${CLIDEBUG}

import_symbols:
	${S7CLI} importSymbols -p ${PROJECT} \
		--symbols ${UAB_PROJECT}\\arc56\\Output\\S7InstanceGenerator\\Symbol.sdf \
		--program ${ARC_APP}
#	${S7CLI} importSymbols -p ${PROJECT} \
#		--symbols ${UAB_PROJECT}\\lss56\\Output\\S7InstanceGenerator\\Symbol.sdf \
#		--program ${LSS_APP}

import_instances:
	${S7CLI} importSourcesDir -p ${PROJECT} \
		--program ${ARC_APP} \
		--srcdir ${UAB_PROJECT}\\arc56\\Output\\S7InstanceGenerator \
		--force y
#	${S7CLI} importSourcesDir -p ${PROJECT} \
#		--program ${LSS_APP} \
#		--srcdir ${UAB_PROJECT}\\lss56\\Output\\S7InstanceGenerator \
#		--force y

import_logic:
	${S7CLI} importSourcesDir -p ${PROJECT} \
		--program ${ARC_APP} \
		--srcdir ${UAB_PROJECT}\\arc56\\Output\\S7LogicGenerator \
		--force y
#	${S7CLI} importSourcesDir -p ${PROJECT} \
#		--program ${LSS_APP} \
#		--srcdir ${UAB_PROJECT}\\lss56\\Output\\S7LogicGenerator \
#		--force y

import_generated_sources: import_instances import_logic


compile_baseline:
	( ${S7CLI} compileSources -p ${PROJECT} --program ${ARC_APP} \
		--sources 1_Compilation_Baseline || exit 0 )
	( ${S7CLI} compileSources -p ${PROJECT} --program ${LSS_APP} \
		--sources 1_Compilation_Baseline || exit 0 )

compile_instances:
	( ${S7CLI} compileSources -p ${PROJECT} --program ${ARC_APP} \
		--sources 2_Compilation_instance || exit 0 )
	( ${S7CLI} compileSources -p ${PROJECT} --program ${LSS_APP} \
		--sources 2_Compilation_instance || exit 0 )

compile_logic:
	( ${S7CLI} compileSources -p ${PROJECT} --program ${ARC_APP} \
		--sources 3_Compilation_LOGIC || exit 0 )
	( ${S7CLI} compileSources -p ${PROJECT} --program ${LSS_APP} \
		--sources 3_Compilation_LOGIC || exit 0 )


compile_main:
	( ${S7CLI} compileSources -p ${PROJECT} --program ${ARC_APP} \
		--sources 4_Compilation_OB || exit 0 )
	( ${S7CLI} compileSources -p ${PROJECT} --program ${LSS_APP} \
		--sources 4_Compilation_OB || exit 0 )


compile: compile_baseline compile_instances compile_logic compile_main




clean:
	rm -rf Sector56/

.phony: all clean
