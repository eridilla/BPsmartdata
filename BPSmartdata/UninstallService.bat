set var=%0
%var:~1,2%
cd %var%\..\
BPSmartdata.exe uninstall
pause