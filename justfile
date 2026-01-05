# Show all available commands
default:
    just --list

# Format all C# files
format:
    dotnet csharpier .

# Run the demo
demo:
    dotnet run --project demo

# Build the library
build:
    dotnet build RopStarterPack.csproj

# Pack for NuGet
pack:
    dotnet pack RopStarterPack.csproj -c Release

# Run tests in watch mode
test:
    watchexec -e cs -w . -- dotnet test tests/

# Run tests once
test-once:
    dotnet test tests/
