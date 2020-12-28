dotnet publish -r linux-arm --self-contained false /p:ShowLinkerSizeComparison=true
pushd .\bin\Debug\net5.0\linux-arm\publish
scp -v -r .\* pi@ashley:~/Projects/net-reader
popd
scp -v -r epub\* pi@ashley:~/Projects/net-reader
