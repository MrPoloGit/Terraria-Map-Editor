param(
    [string] $ReleasePath = ".\release",
    [string] $VersionPrefix = "5.0.0",
    [string] $VersionSuffix = $null
)

$publishPath = "publish\avalonia"

$platforms = $(
    # linux builds
    "linux-arm64",
    "linux-musl-x64",
    "linux-x64",
    "linux-musl-arm64",
    # windows
    "win-x64",
    "win-arm64",
    # mac
    "osx-x64",
    "osx-arm64"
)

if (Test-Path -Path ".\$publishPath") { Remove-Item -Path ".\$publishPath" -Force -Recurse }

$platforms | ForEach-Object {
    $rid = $_
    $buildArgs = @(
        "publish"
        "-c"
        "Release"
        "-r"
        $rid
        "--self-contained"
        "true"
        "-p:PublishSingleFile=true"
        "-o"
        ".\$publishPath\$rid"
        "/p:VersionPrefix=""$VersionPrefix"""
        "--version-suffix"
        "$VersionSuffix"
        ".\src\TEdit5\TEdit5.csproj"
    )

    & dotnet $buildArgs

    if ($rid -like "osx-*") {
        # The CreateMacBundle MSBuild target produces TEdit.app one level above the publish dir.
        # Zip the .app bundle, then remove it so the next osx RID gets a clean slate.
        $bundlePath = ".\$publishPath\TEdit.app"
        Compress-Archive -Path $bundlePath -DestinationPath ".\$ReleasePath\TEditAvalonia-$VersionPrefix-$VersionSuffix-$rid.zip"
        Remove-Item -Path $bundlePath -Force -Recurse
    } else {
        Compress-Archive -Path ".\$publishPath\$rid\*" -DestinationPath ".\$ReleasePath\TEditAvalonia-$VersionPrefix-$VersionSuffix-$rid.zip"
    }
}
