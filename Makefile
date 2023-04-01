.PHONY: run

run:
	dotnet build
	cp ./Library/* ./bin/Debug/net7.0/
	./bin/Debug/net7.0/soundsharp
