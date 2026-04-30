# Learnings

- `src/LockPaper.Ui` is already a .NET MAUI app targeting Android and Windows, so LockPaper product work should align with that shared app shell before introducing a different client architecture.
- Use `dotnet format .\src\LockPaper.Ui\LockPaper.Ui.csproj --no-restore` followed by `dotnet build .\src\LockPaper.Ui\LockPaper.Ui.csproj --nologo --no-restore` for the MAUI client verification loop.
- `run-local.ps1 -Target unit-tests` runs every `*.UnitTests.csproj` under `.\tests`, so new unit-test projects should keep that naming convention to plug into the local workflow automatically.
