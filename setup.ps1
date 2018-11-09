$url = "https://github.com/frida/frida/releases/download/12.2.23/frida-core-devkit-12.2.23-windows-x86_64.exe"
$fridaLibDir = "Frida.Exports\lib\x64\"
$output = "$PSScriptRoot\$fridaLibDir\extractor.exe"

echo "Downloading frida release"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
# Invoke-WebRequest -Uri $url -OutFile $output

echo "Extracting frida release"
pushd
cd $fridaLibDir
Invoke-Expression $output
popd