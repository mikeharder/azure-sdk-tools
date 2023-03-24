# TypeSpec Validator

## Installing
1. Install .NET 6.0
1. Install typespec-validator
```
dotnet tool update azure.sdk.tools.typespecvalidator --global --add-source https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json --version "1.0.0-dev*"
```

## Usage
### Validate all TypeSpec Projects under a path
```
typespec-validator <path>
```

Example:
```
typespec-validator specificaton\contosowidgetmanager
```

### Validate only TypeSpec Projects changed relative to a git commit (or branch)
```
typespec-validator --git-diff <commit> <path>
```

Example:
```
typespec-validator --git-diff main specificaton\contosowidgetmanager
```
