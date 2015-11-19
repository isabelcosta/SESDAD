@ECHO OFF
Start "" %cd%"\PuppetMaster\bin\Debug\PuppetMaster.exe.lnk" 0 %1
for /l %%x in (1, 1, %1) do (
   echo started %%x of %1
   Start ""  %cd%"\PuppetMaster\bin\Debug\PuppetMaster.exe.lnk" %%x
)
echo Done