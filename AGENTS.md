# AGENTS.md

## Project overview

### AWS Integration
- GammonX.DynamoDb (production assembly)
- GammonX.DynamoDb.Tests (non production assembly)
- GammonX.Lambda (production assembly)
- GammonX.Lambda.Tests (non production assembly)

### Mars Neural Net Bot Service
- GammonX.Mars.NN (production assembly)
- GammonX.Mars.NN.Tests (non production assembly)
- GammonX.Mars.Server (production assembly)
- GammonX.Mars.Server.Tests (non production assembly)
- GammonX.Mars.Training (non production assembly)
- GammonX.Mars.Training.Tests (non production assembly)

### Game Server
- GammonX.Server (production assembly)
- GammonX.Server.Tests (non production assembly)

### Shared Libraries
- GammonX.Engine (production assembly)
- GammonX.Engine.Tests (non production assembly)
- GammonX.Models (production assembly)
- GammonX.Models.Tests (non production assembly)

### Technical
- .net 10 sdk as prerequisite
- dependency versions in `src/GammonX/Directory.Build.props`

## Build and test command
- build solution
`dotnet build src/GammonX/GammonX.sln`
- build one affected project and its dependencies
`dotnet build src/GammonX/GammonX.Engine/GammonX.Engine.csproj`
- run one relevant test assembly
`dotnet test src/GammonX/{test-project-name}/{test-project-name}.csproj`
- run one relevant test class
`dotnet test src/GammonX/GammonX.Engine.Tests/GammonX.Engine.Tests.csproj --filter "FullyQualifiedName~MoveGenerationCancellationTests"`
- check the tracked diff for whitespace errors and conflict markers
`git diff --check`

## Code style guidelines
- see `src/GammonX/.editorconfig` for existing style guidance
- align with the existing code style
- use `{}` brackets for `if`, `foreach` and other statements
- use blank lines to differentiate logical blocks with the aim to increase readability
- document public facing interfaces, properties and classes. Only consider it for added or changed APIs
- comment non-obvious constraints and reasoning in complex code; avoid restating the implementation.
- apply the AAA (Arrange, Act, Assert) pattern for unit tests

## Testing instructions
- keep test execution scope relevant to the applied changes. Do not execute the whole solution test suite by default.
- look for existing coverage first. Extend relevant test classes; add focused test cases when needed.
- for shared-library changes, also run relevant tests in consuming projects when behavior or contracts change.

## Security considerations
- avoid reading credential files or printing secrets. If an authentication token is inadvertently encountered, notify the user without reproducing its value.