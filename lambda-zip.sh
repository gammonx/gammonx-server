#!/bin/bash
cd src/GammonX/GammonX.Lambda/
dotnet publish GammonX.Lambda.csproj -c Debug --self-contained true -r linux-x64 -o ./bin/build-zip
cd bin/build-zip
# we create the bootstrap file
cat > "bootstrap" << 'EOF'
#!/bin/sh
./GammonX.Lambda
EOF
mkdir '..\..\zip'
powershell.exe -NoProfile -Command "Compress-Archive -Path * -DestinationPath '..\..\zip\lambda.zip' -Force"