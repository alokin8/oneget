# package script
erase *.zip
erase *.xml
erase *.pdb
$n = [int] (([System.DateTime]::Now - [System.DateTime]::Parse("03/19/2014") ).Ticks / 80000000)
$n = "OneGet[#$n].zip"
zip $n etc\* *.dll *.ps1xml *.txt *.psd1

echo $n