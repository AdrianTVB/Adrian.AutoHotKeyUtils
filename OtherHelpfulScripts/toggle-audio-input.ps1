$device1 = "Headphones (Realtek(R) Audio)"
$device2 = "Speakers (3- Logitech USB Headset)"

$Audio = Get-AudioDevice -playback
Write-Output "Audio device was " $Audio.Name
Write-Output "Audio device now set to " 

if ($Audio.Name.StartsWith($device1)) {
   (Get-AudioDevice -list | Where-Object Name -like ("$device2*") | Set-AudioDevice).Name
}  Else {
   (Get-AudioDevice -list | Where-Object Name -like ("$device1*") | Set-AudioDevice).Name
}