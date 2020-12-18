$OutputDir = $args[0]
if (!$OutputDir) { 
    Write-Warning "Please specific output directory."
    exit -1 
}

# Write-Output $OutputDir
$SentryDllPath = Join-Path -Path $OutputDir -ChildPath "OsuPlayer.Sentry.dll"
# Write-Output $SentryDllPath

if (![System.IO.File]::Exists($SentryDllPath)) {
    Write-Warning """$($SentryDllPath)"" does not exists. Skip!"
    exit 
}

try {
    if (Get-Command "Confuser.CLI") {
        $line = 
        "<project outputDir=""."" baseDir=""."" xmlns=""http://confuser.codeplex.com"">
    <rule pattern=""true"" preset=""maximum"" inherit=""false"" />
    <module path=""OsuPlayer.Sentry.dll"" />
</project>"

        Write-Output $line
        $tempProjPath = Join-Path -Path $OutputDir -ChildPath "confuser.crproj"
        Out-File -FilePath $tempProjPath -InputObject $line 
        Confuser.CLI $tempProjPath -n
        Remove-Item $tempProjPath
        exit $LastExitCode
    }
}
catch {
    Write-Warning """Confuser.CLI"" is not in the environment path. Skip!
Latest release of ConfuserEX: https://github.com/mkaring/ConfuserEx/releases"
    exit 
}