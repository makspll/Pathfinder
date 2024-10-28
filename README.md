# Pathfinder
![Pathfinder](Pathfinder.jpg)

Finds and outputs all API routes found in a .NET assembly in textual or JSON format.

## Features
- Attribute based routing
- Conventional routing (templates + defaults have to be specified in a config file)
- .NET core support
- .NET framework support
- JSON and Text output
- Configurable backing lib for projects with custom routing mechanisms

# Usage
```
dotnet build test_assemblies/dotnet8
dotnet run --project src/app **/bin/**/dotnet8.dll -o Text
```
![image](https://github.com/user-attachments/assets/adc9b60c-c991-46b0-b474-8de967666467)

# Configuration

## Config file
The program is configured via `pathfinder.json` files found in your project. If the file is not found you can specify a path via the `-c` flag.

Currently the file needs to specify all your conventional routing configuration (anything that isn't attribute based).

### .NET framework
In .NET framework projects, you will need to specify whether each of your routes is an MVC or API route. This is done by adding a `Type` field to each route in the config file.

```json
{
    "ConventionalRoutes": [
        {
            "Template": "conventionalprefix/{controller}/{action}",
            "Type": "MVC"
        },
        {
            "Template": "conventionalprefix2/{controller}",
            "Defaults": {
                "action": "DefaultAction"
            },
            "Type": "MVC"
        },
        {
            "Template": "conventionalwithnoactionspecs",
            "Defaults": {
                "controller": "DefaultConventional",
                "action": "DefaultAction"
            },
            "Type": "MVC"
        },
        {
            "Template": "apiconventionalprefix/{controller}/{action}",
            "Type": "API"
        },
        {
            "Template": "apiconventionalprefix2/{controller}",
            "Defaults": {
                "action": "DefaultAction"
            },
            "Type": "API"
        },
        {
            "Template": "apiconventionalwithnoactionspecs",
            "Defaults": {
                "controller": "ApiDefaultConventionalApi",
                "action": "DefaultAction"
            },
            "Type": "API"
        }
    ]
}
```

### .NET core

.NET core does not make such a distinction, you shouldn't specify the type of controller:

```json
{
    "ConventionalRoutes": [
        {
            "Template": "conventionalprefix/{controller}/{action}"
        },
        {
            "Template": "conventionalprefix2/{controller}",
            "Defaults": {
                "action": "DefaultAction"
            }
        },
        {
            "Template": "conventionalwithnoactionspecs",
            "Defaults": {
                "controller": "DefaultConventional",
                "action": "DefaultAction"
            }
        }
    ]
}
```