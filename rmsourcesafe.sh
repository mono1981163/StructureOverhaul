#!/bin/sh
# run in Cygwin as:
# . ./rmsourcesafe.sh
find.exe . -type f -name *.dsp -print0 | xargs -0 -r perl -p -i.vssbak -e 's/^.*Scc_ProjName.*$//g'
find.exe . -type f -name *.dsp -print0 | xargs -0 -r perl -p -i.vssbak -e 's/^.*Scc_LocalPath.*$//g'
find.exe . -type f -name *.dsw -print0 | xargs -0 -r sed -i '/begin.source.code.control/,/end.source.code.control/d'
find.exe . -type f -name *.sln -print0 | xargs -0 -n1 dos2unix
find.exe . -type f -name *.sln -print0 | xargs -0 -r sed -i '/GlobalSection(SourceCodeControl)/,/EndGlobalSection/d'
find.exe . -type f -name *.sln -print0 | xargs -0 -n1 unix2dos
find.exe . -type f -name *.*proj -print0 | xargs -0 -n1 dos2unix
find.exe . -type f -name *.*proj -print0 | xargs -0 -r perl -p -i.vssbak -e 's/^.*SccProjectName.*$//g'
find.exe . -type f -name *.*proj -print0 | xargs -0 -r perl -p -i.vssbak -e 's/^.*SccLocalPath.*$//g'
find.exe . -type f -name *.*proj -print0 | xargs -0 -r perl -p -i.vssbak -e 's/^.*SccAuxPath.*$//g'
find.exe . -type f -name *.*proj -print0 | xargs -0 -r perl -p -i.vssbak -e 's/^.*SccProvider.*$//g'
find.exe . -type f -name *.*proj -print0 | xargs -0 -n1 unix2dos
find.exe . -type f -name *.vssbak -print0 | xargs -0 -r rm -f 
find.exe . -type f -name *.*scc -print0 | xargs -0 -r rm -f 

