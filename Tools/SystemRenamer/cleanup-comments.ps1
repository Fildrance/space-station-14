param(
    [string]$Root = "e:\Sources\Repos\ss14\space-station-14",
    [string]$Dump = "targets-dump.txt"
)

# Cleans up stale mentions of renamed Shared*System types in comments/strings.
# Reads the rename map produced by `--dump-targets` and does a word-boundary
# replacement over all tracked .cs files in the main repo and the engine submodule.

$pairs = Get-Content (Join-Path $Root $Dump) | Where-Object { $_ -match '\|shared$' } | ForEach-Object {
    $parts = $_.Split('|')
    [pscustomobject]@{ Old = $parts[2]; New = $parts[3] }
}

$map = @{}
foreach ($p in $pairs) { $map[$p.Old] = $p.New }

$pattern = '\b(' + (($map.Keys | ForEach-Object { [regex]::Escape($_) }) -join '|') + ')\b'
$regex = [regex]::new($pattern, [System.Text.RegularExpressions.RegexOptions]::Compiled)
$count = 0
$touched = [System.Collections.Generic.List[string]]::new()

foreach ($repo in @($Root, (Join-Path $Root 'RobustToolbox'))) {
    $files = git -C $repo ls-files '*.cs'
    foreach ($f in $files) {
        $path = Join-Path $repo $f.Replace('/', '\')
        if (-not (Test-Path $path)) { continue }
        $content = [IO.File]::ReadAllText($path)
        if (-not $content.Contains('Shared')) { continue }
        $updated = $regex.Replace($content, { param($m) $map[$m.Value] })
        if ($updated -ne $content) {
            $bom = $false
            $head = [IO.File]::ReadAllBytes($path)
            if ($head.Length -ge 3 -and $head[0] -eq 0xEF -and $head[1] -eq 0xBB -and $head[2] -eq 0xBF) { $bom = $true }
            [IO.File]::WriteAllText($path, $updated, (New-Object System.Text.UTF8Encoding($bom)))
            $count++
            $touched.Add($f)
        }
    }
}

"Files updated: $count"
$touched | Select-Object -First 40
