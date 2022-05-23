set var=%0 
echo %var:~1,2% > E:\out.txt
echo %var%\..\ > E:\out2.txt
BPSmartdata.exe install start
pause