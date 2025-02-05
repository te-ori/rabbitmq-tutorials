function New-RabbitMQProject {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,
        [Parameter(Mandatory = $true)]
        [string]$OutputDirectory
    )

    $ProjectDirectory = Join-Path -Path $OutputDirectory -ChildPath $ProjectName

    $pathParts = $ProjectDirectory -split '[\\/]'| Where-Object { $_ -ne '.' }   | ForEach-Object { Format-Name -i $_ }
    Write-Host "Formatted path parts: $pathParts" -ForegroundColor Cyan
    $joinedNamespace = $pathParts -join '.'
    Write-Host "Joined path as namespace: $joinedNamespace" -ForegroundColor Magenta

    if ( !(Test-Path $ProjectDirectory)) {
        New-Item -Path $ProjectDirectory -ItemType Directory
    }

    # Create new console project
    dotnet new console -n $ProjectName  --use-program-main -o "$ProjectDirectory"
    Write-Host "Created project '$ProjectName' in '$ProjectDirectory'"

    $ProjectFilePath = Join-Path -Path $ProjectDirectory -ChildPath "$ProjectName.csproj"

    # Add RabbitMQ.Client package
    dotnet add "$ProjectFilePath" package RabbitMQ.Client
    Write-Host "Added RabbitMQ.Client package to '$ProjectName'"

    # Add reference to Common project
    dotnet add "$ProjectFilePath" reference .\Common\Common.csproj
    Write-Host "Added reference to Common project"

    # rename program.cs to $ProjectName.cs
    $ProgramFileName = "$ProjectName.Program.cs"
    Rename-Item "$ProjectDirectory\Program.cs" "$ProgramFileName"
    Write-Host "Renamed Program.cs to '$ProgramFileName'"

    # Add using statements to $ProjectName.Program.cs
    # $currentContent = Get-Content "$ProjectDirectory\$ProgramFileName" -Raw
    # Write-Host "Current content read from '$ProjectDirectory\$ProgramFileName'"

    $newContent = @"
using static System.Console;
using RabbitMQ.Client;
using Common;
using System.Threading.Tasks;

namespace $joinedNamespace;

class Program
{
    static async Task Main(string[] args)
    {
        WriteLine("Hello, $ProjectName!");

        using var manager = new RabbitMqManager();
        await manager.Initialize();

        
    }
}

"@
    Set-Content -Path "$ProjectDirectory\$ProgramFileName" -Value $newContent 
    Write-Host "Added using statements to '$ProjectDirectory\$ProgramFileName'"

    Write-Host $ProjectFilePath
    dotnet sln add "$ProjectFilePath"

    Write-Host "Created project $ProjectName with RabbitMQ.Client package and Common project reference"
}

function Format-Name {
    param(
        [Parameter(Mandatory = $true)]
        [string]$i
    )
    Write-Host "Formatting name: $i" -ForegroundColor Cyan
    $name = ($i | Select-String -Pattern "^([\W\d]+)?(.+)" | % { $_.Matches[0].Groups[2].Value -replace "[ %]", "_" })
    Write-Host "Formatted name: $name" -ForegroundColor Cyan
    return $name
}