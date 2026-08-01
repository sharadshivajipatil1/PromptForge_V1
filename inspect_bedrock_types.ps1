$dll = 'C:\Users\sharadp\.nuget\packages\awssdk.bedrock\3.7.200\lib\netstandard2.0\AWSSDK.Bedrock.dll'
$xml = 'C:\Users\sharadp\.nuget\packages\awssdk.bedrock\3.7.200\lib\netstandard2.0\AWSSDK.Bedrock.xml'
Write-Output "DLL: $dll"
Write-Output "XML: $xml"
$asm = [System.Reflection.Assembly]::LoadFile($dll)
try {
    $asm.GetTypes() | Where-Object { $_.Name -like '*Bedrock*' -or $_.Name -like '*Agent*' -or $_.Name -like '*Invoke*' -or $_.Name -like '*Text*' } | ForEach-Object { Write-Output "TYPE: $($_.FullName)" }
} catch [System.Reflection.ReflectionTypeLoadException] {
    Write-Output "ReflectionTypeLoadException"
    $_.LoaderExceptions | ForEach-Object { Write-Output "LOADER: $($_.Message)" }
}
Write-Output "--- XML SEARCH ---"
$lines = Get-Content $xml
$matches = $lines | Select-String -Pattern 'InvokeAgentRequest|TextInputConfig|Invoke.*Request|Invoke.*Async'
foreach ($match in $matches) {
    Write-Output "XML: $($match.Line.Trim())"
}
