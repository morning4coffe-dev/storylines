$source = "src/Storylines/Resources/en/Resources.resw"
$locales = Get-ChildItem -Path src/Storylines/Resources -Directory | Where-Object { $_.Name -ne "en" }

function Get-ReswEntries($path) {
    if (-not (Test-Path $path)) { return $null }
    [xml]$xml = Get-Content $path
    $entries = @{}
    foreach ($data in $xml.root.data) {
        $name = $data.name
        $value = $data.value
        if ($entries.ContainsKey($name)) { Write-Host "Duplicate key $name in $path" }
        $entries[$name] = $value
    }
    return $entries
}

$enEntries = Get-ReswEntries $source
$enKeys = $enEntries.Count

foreach ($localeDir in $locales) {
    $locale = $localeDir.Name
    $targetPath = Join-Path $localeDir.FullName "Resources.resw"
    $targetEntries = Get-ReswEntries $targetPath
    if ($null -eq $targetEntries) { continue }

    $missing = @()
    foreach($k in $enEntries.Keys) { if (-not $targetEntries.ContainsKey($k)) { $missing += $k } }
    
    $extra = @()
    foreach($k in $targetEntries.Keys) { if (-not $enEntries.ContainsKey($k)) { $extra += $k } }

    $same = @()
    $empty = @()
    $mismatch = @()

    foreach($k in $targetEntries.Keys) {
        $v = $targetEntries[$k]
        if ([string]::IsNullOrWhiteSpace($v)) { $empty += $k }
        if ($enEntries.ContainsKey($k)) {
            $ev = $enEntries[$k]
            if ($v -eq $ev -and $v.Length -gt 3 -and $v -notmatch 'Storylines') { $same += $k }
            
            # Check brackets {0}
            $eb = [regex]::Matches($ev, '\{.*?\}').Value
            $tb = [regex]::Matches($v, '\{.*?\}').Value
            if ($eb.Count -ne $tb.Count) { $mismatch += "$k (Brackets)" }
            
            # Check ampersand
            if (($ev.Contains('&')) -and -not ($v.Contains('&'))) { $mismatch += "$k (Ampersand missing)" }
            
            # Whitespace
            if ($ev.StartsWith(' ') -ne $v.StartsWith(' ') -or $ev.EndsWith(' ') -ne $v.EndsWith(' ')) { $mismatch += "$k (Whitespace)" }
        }
    }

    Write-Host "--- Locale: $locale ---"
    Write-Host "Keys: $($targetEntries.Count) (EN: $enKeys)"
    if ($missing.Count -gt 0) { Write-Host "Missing ($($missing.Count)): $($missing -join ', ')" }
    if ($extra.Count -gt 0) { Write-Host "Extra ($($extra.Count)): $($extra -join ', ')" }
    if ($same.Count -gt 0) { Write-Host "Same as EN ($($same.Count)): $($same -join ', ')" }
    if ($empty.Count -gt 0) { Write-Host "Empty ($($empty.Count)): $($empty -join ', ')" }
    if ($mismatch.Count -gt 0) { Write-Host "Mismatches ($($mismatch.Count)): $($mismatch -join ', ')" }
    Write-Host ""
}
