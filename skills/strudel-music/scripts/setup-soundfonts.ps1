param([string]$BankId)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'This setup script is for Windows; install FluidSynth with the host package manager on other systems.' }

$resources = Get-Content (Join-Path $PSScriptRoot 'soundfont-resources.json') -Raw | ConvertFrom-Json
$catalogJson = & node (Join-Path $PSScriptRoot 'bank-catalog.mjs') --init
if ($LASTEXITCODE -ne 0) { throw 'Could not load the shared SoundFont catalog.' }
$catalog = $catalogJson | ConvertFrom-Json
$banks = @()
if ($BankId) {
    $banks = @($catalog.banks | Where-Object { $_.id -ceq $BankId })
    if ($banks.Count -ne 1) { throw "Unknown bank '$BankId' in $($catalog.catalogPath)" }
}
$cache = Join-Path $PSScriptRoot '.cache'
$fonts = $catalog.directory
$synth = Join-Path $cache 'fluidsynth'
$temporary = Join-Path $cache ('soundfont-setup-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temporary | Out-Null

function Get-VerifiedDownload([string]$Url, [string]$Destination, [string]$Sha256) {
    if ($env:STRUDEL_SOUNDFONT_DOWNLOADS -eq '0') { throw 'SoundFont/renderer downloads are disabled.' }
    if ($Url -notmatch '^https://') { throw 'Downloads require a recorded HTTPS source.' }
    & curl.exe --fail --location --silent --show-error --output $Destination $Url
    if ($LASTEXITCODE -ne 0) { throw "Download failed: $Url" }
    if ($Sha256 -and (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Sha256) {
        throw "Checksum mismatch: $Url"
    }
}

function Assert-SoundFont([string]$File, [string]$Sha256) {
    $stream = [IO.File]::OpenRead($File)
    try {
        $header = [byte[]]::new(12)
        if ($stream.Read($header, 0, 12) -ne 12 -or
            [Text.Encoding]::ASCII.GetString($header, 0, 4) -ne 'RIFF' -or
            [Text.Encoding]::ASCII.GetString($header, 8, 4) -ne 'sfbk') {
            throw "Invalid SF2 bank: $File"
        }
    } finally { $stream.Dispose() }
    if ($Sha256 -and (Get-FileHash -LiteralPath $File -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Sha256.ToLowerInvariant()) {
        throw "SoundFont checksum mismatch: $File"
    }
}

try {
    if (-not $env:FLUIDSYNTH_PATH -and -not (Test-Path -LiteralPath (Join-Path $cache 'fluidsynth-location.json'))) {
        if (Test-Path -LiteralPath $synth) { throw 'FluidSynth cache exists without its location record; refusing to overwrite it.' }
        $zip = Join-Path $temporary 'fluidsynth.zip'
        Get-VerifiedDownload $resources.renderer.windowsUrl $zip $resources.renderer.sha256
        Expand-Archive -LiteralPath $zip -DestinationPath $synth
        $executables = @(Get-ChildItem -LiteralPath $synth -Filter 'fluidsynth.exe' -Recurse -File)
        if ($executables.Count -ne 1) { throw 'Could not resolve the downloaded FluidSynth executable.' }
        & $executables[0].FullName --version
        if ($LASTEXITCODE -ne 0) { throw 'FluidSynth startup failed.' }
        @{ executable = [IO.Path]::GetRelativePath($PSScriptRoot, $executables[0].FullName) } |
            ConvertTo-Json | Set-Content (Join-Path $cache 'fluidsynth-location.json') -Encoding utf8
    }

    foreach ($bank in $banks) {
        $destination = Join-Path $fonts $bank.file
        if (Test-Path -LiteralPath $destination) {
            Assert-SoundFont $destination $bank.fileSha256
            Write-Output "Cached: $($bank.name)"
            continue
        }
        if ($bank.format -notin @('debian', 'sf2', 'archive')) { throw "Unsupported bank download format: $($bank.format)" }
        if (-not $bank.sha256 -and -not $bank.gitBlobSha1 -and -not ($bank.format -eq 'sf2' -and $bank.fileSha256)) {
            throw 'Bank downloads require a pinned checksum.'
        }
        $download = Join-Path $temporary ($bank.id + '.download')
        Get-VerifiedDownload $bank.url $download $bank.sha256
        if ($bank.format -eq 'debian') {
            $extract = Join-Path $temporary 'fluid-package'
            New-Item -ItemType Directory -Path $extract | Out-Null
            $tar = Join-Path $env:SystemRoot 'System32\tar.exe'
            & $tar -xf $download -C $extract 'data.tar.xz'
            if ($LASTEXITCODE -ne 0) { throw 'Could not unpack the verified Debian container.' }
            & $tar -xf (Join-Path $extract 'data.tar.xz') -C $extract
            if ($LASTEXITCODE -ne 0) { throw 'Could not unpack the verified SoundFont data.' }
            $font = Join-Path $extract ('usr\share\sounds\sf2\' + $bank.file)
            $license = Join-Path $extract 'usr\share\doc\fluid-soundfont-gm\copyright'
        } elseif ($bank.format -eq 'archive') {
            $extract = Join-Path $temporary $bank.id
            $arguments = @((Join-Path $PSScriptRoot 'soundfont-archive.mjs'), $download, $extract, $bank.archiveFile)
            if ($bank.licenseFile) { $arguments += $bank.licenseFile }
            $extractedJson = & node @arguments
            if ($LASTEXITCODE -ne 0) { throw "Could not extract the verified archive for $($bank.name)" }
            $extracted = $extractedJson | ConvertFrom-Json
            $font = $extracted.font
            $license = $extracted.license
            if (-not $license) {
                $license = Join-Path $temporary ($bank.id + '.LICENSE.txt')
                Get-VerifiedDownload $bank.licenseSource $license ''
            }
        } elseif ($bank.format -eq 'sf2') {
            if ($bank.gitBlobSha1) {
                $bytes = [IO.File]::ReadAllBytes($download)
                $prefix = [Text.Encoding]::ASCII.GetBytes("blob $($bytes.Length)" + [char]0)
                $sha = [Security.Cryptography.SHA1]::Create()
                try {
                    $null = $sha.TransformBlock($prefix, 0, $prefix.Length, $prefix, 0)
                    $null = $sha.TransformFinalBlock($bytes, 0, $bytes.Length)
                    $blobHash = [BitConverter]::ToString($sha.Hash).Replace('-', '').ToLowerInvariant()
                    if ($blobHash -ne $bank.gitBlobSha1) { throw 'Pinned bank checksum mismatch.' }
                } finally { $sha.Dispose() }
            }
            $font = $download
            $license = Join-Path $temporary ($bank.id + '.LICENSE.txt')
            Get-VerifiedDownload $bank.licenseSource $license ''
        }
        Assert-SoundFont $font $bank.fileSha256
        Move-Item -LiteralPath $font -Destination $destination
        $licenseDestination = Join-Path $fonts ($bank.id + '.LICENSE.txt')
        if (-not (Test-Path -LiteralPath $licenseDestination)) {
            Copy-Item -LiteralPath $license -Destination $licenseDestination
        }
        Write-Output "Ready: $($bank.name)"
    }
    Write-Output "Catalog: $($catalog.catalogPath)"
} finally {
    Remove-Item -LiteralPath $temporary -Recurse -Force
}
