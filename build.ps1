[CmdletBinding(SupportsShouldProcess)]
Param(
    # build semantic version major field
    [Parameter()][string]$VersionMajor = "1"
    # build semantic version minor field
    ,[Parameter()][string]$VersionMinor = "0"
    # build semantic version patch field
    ,[Parameter()][string]$VersionPatch = "0"
    # build configuration
    ,[Parameter()][string]$Configuration = "Release"
)

$version = "1.0"

Write-Information "build script v$version"
Write-Information "building v${VersionMajor}.${VersionMinor}.${VersionPatch}..."
dotnet build --configuration $Configuration -p:"VersionMajor=${VersionMajor},VersionMinor=${VersionMinor},VersionPatch=${VersionPatch}"