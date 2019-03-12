param([int]$buildNumber)

. .\build-helpers

$versionPrefix = "1.0.1"
$prerelease = $false

$authors = "Justin Pope"
$copyright = copyright 2013 $authors
$configuration = 'Release'
$versionSuffix = if ($prerelease) { "beta-{0:D4}" -f $buildNumber } else { "" }

function License {
    mit-license $copyright
}

function Assembly-Properties {
    generate "src\Directory.build.props" @"
<Project>
    <PropertyGroup>
        <Product>SQLDump</Product>
        <VersionPrefix>$versionPrefix</VersionPrefix>
        <VersionSuffix>$versionSuffix</VersionSuffix>
        <Authors>$authors</Authors>
        <Copyright>$copyright</Copyright>
        <PackageLicenseUrl>https://github.com/jpope/sqldump/blob/master/LICENSE.txt</PackageLicenseUrl>
        <PackageProjectUrl>https://github.com/jpope/sqldump</PackageProjectUrl>
        <PackageIconUrl></PackageIconUrl>
        <RepositoryUrl>https://github.com/jpope/sqldump</RepositoryUrl>
        <PackageOutputPath>..\..\packages</PackageOutputPath>
        <IncludeSymbols>true</IncludeSymbols>
        <LangVersion>latest</LangVersion>
    </PropertyGroup>
</Project>
"@
}

function Clean {
    exec { dotnet clean src -c $configuration /nologo }
}

function Restore {
    exec { dotnet restore src -s https://api.nuget.org/v3/index.json }
}

function Build {
    exec { dotnet build src -c $configuration --no-restore /nologo }
}

function Test {
}

function Package {
    exec { dotnet pack -c $configuration --no-restore --no-build /nologo } src\SQLDump
}

run-build {
    step { License }
    step { Assembly-Properties }
    step { Clean }
    step { Restore }
    step { Build }
    step { Test }
    step { Package }
}