param([string]$OutputDirectory = "")

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectRoot 'project-knowledge-base\outputs'
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function T([string]$base64) {
    [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($base64))
}

function Get-DisplayCategory([string]$category) {
    $map = @{
        Character = '6KeS6Imy'; Font = '5a2X5L2T'; Material = '5p2Q6LSo'; Other_Art = '5YW25LuW576O5pyv'
        PCG_Map = 'UENHIOWcsOWbvg=='; PCG_Terrain_Tile_Set = 'UENHIOWcsOW9ouWIh+eJh+mbhg=='
        Skill = '5oqA6IO9'; Tattoo = '57q56Lqr'; UI = '55WM6Z2i'; Weapon = '5q2m5Zmo'
        Prefab = 'UHJlZmFi'; Animation = '5Yqo55S7'; Audio = '6Z+z6aKR'; Effect = '54m55pWI'
    }
    if ($map.ContainsKey($category)) { return T $map[$category] }
    return $category
}

function Get-DisplayType([string]$extension) {
    $type = $extension.TrimStart('.').ToUpperInvariant()
    switch ($type) {
        'PNG' { return (T '5Zu+54mH77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'JPG' { return (T '5Zu+54mH77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'JPEG' { return (T '5Zu+54mH77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'TGA' { return (T '5Zu+54mH77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'PSD' { return (T '5Zu+54mH77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'PREFAB' { return T '6aKE5Yi25L2T77yIUFJFRkFC77yJ' }
        'MAT' { return T '5p2Q6LSo77yITUFU77yJ' }
        'ANIM' { return T '5Yqo55S777yIQU5JTe+8iQ==' }
        'CONTROLLER' { return T '5Yqo55S75o6n5Yi25Zmo77yIQ09OVFJPTExFUu+8iQ==' }
        'TTF' { return (T '5a2X5L2T77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'OTF' { return (T '5a2X5L2T77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'SPRITEATLAS' { return T '57K+54G15Zu+6ZuG77yIU1BSSVRFQVRMQVPvvIk=' }
        'FBX' { return T 'M0Qg5qih5Z6L77yIRkJY77yJ' }
        'WAV' { return (T '6Z+z6aKR77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'MP3' { return (T '6Z+z6aKR77yIe3R5cGV977yJ').Replace('{type}', $type) }
        'OGG' { return (T '6Z+z6aKR77yIe3R5cGV977yJ').Replace('{type}', $type) }
        default { return $type }
    }
}

$catalogPath = Join-Path $projectRoot 'GameData\AIData\GameplayCatalogs\totem_runtime_assets.json'
$runtimeCatalog = Get-Content -Raw -Encoding UTF8 $catalogPath | ConvertFrom-Json
$catalogByPath = @{}
foreach ($entry in $runtimeCatalog.entries) {
    foreach ($assetPath in @($entry.activeAssetPath, $entry.legacySourcePath)) {
        if (![string]::IsNullOrWhiteSpace($assetPath)) { $catalogByPath[$assetPath.Replace('\', '/')] = $entry }
    }
}

$pcgCatalogFiles = @('Assets\Resources\PCG\TerrainVisualCatalog.json', 'Assets\Resources\PCG\WorldObjectCatalog.json', 'Assets\Resources\PCG\TerrainTileSetCatalog.json', 'Assets\Resources\PCG\TerrainMaskOverlayCatalog.json', 'Assets\Resources\PCG\ZoneRuleCatalog.json')
$pcgText = ($pcgCatalogFiles | ForEach-Object { Get-Content -Raw -Encoding UTF8 (Join-Path $projectRoot $_) }) -join "`n"
$extensions = @('.png', '.jpg', '.jpeg', '.tga', '.psd', '.fbx', '.mat', '.spriteatlas', '.anim', '.controller', '.prefab', '.wav', '.mp3', '.ogg', '.ttf', '.otf')
$allAssets = Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Recurse -File | Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() -and $_.FullName -notlike '*\Plugins\*' -and $_.FullName -notlike '*\TextMesh Pro\*' }

function Get-Category([string]$relativePath) {
    if ($relativePath -match '/UI/') { return 'UI' }
    if ($relativePath -match '/PCG/') { return 'PCG_Map' }
    if ($relativePath -match '/Tattoo/') { return 'Tattoo' }
    if ($relativePath -match '/Character|/Characters/') { return 'Character' }
    if ($relativePath -match '/Anim/') { return 'Animation' }
    if ($relativePath -match '/Prefab/') { return 'Prefab' }
    if ($relativePath -match '/Materials?/') { return 'Material' }
    if ($relativePath -match '/Font/') { return 'Font' }
    if ($relativePath -match '/Audio/') { return 'Audio' }
    if ($relativePath -match '/Weapons/') { return 'Weapon' }
    if ($relativePath -match '/Skills/') { return 'Skill' }
    if ($relativePath -match '/Effects/') { return 'Effect' }
    return 'Other_Art'
}

function Get-ReviewState([string]$relativePath) {
    $entry = $catalogByPath[$relativePath]
    if ($null -ne $entry) {
        return [pscustomobject]@{ Index = ((T '6L+Q6KGM5pe257Si5byV77ya') + $entry.key); Role = (T '6L+Q6KGM5pe26YCa6L+H6LWE5rqQ6ZSuIHtrZXl9IOWKoOi9ve+8m+WOn+Wni+eUqOmAlOivtOaYjuingei/kOihjOaXtui1hOS6p+e0ouW8leOAgg==').Replace('{key}', $entry.key); Suggested = T '56Gu6K6k5L+d55WZ77yM57un57ut55Sx6L+Q6KGM5pe257Si5byV5Yqg6L29' }
    }
    if ($relativePath.StartsWith('Assets/Resources/')) {
        $resourcePath = [System.IO.Path]::ChangeExtension($relativePath.Substring('Assets/Resources/'.Length), $null).Replace('\', '/')
        if ($pcgText.Contains(('"asset": "' + $resourcePath + '"'))) {
            return [pscustomobject]@{ Index = T 'UENHIOe0ouW8lQ=='; Role = T '55SxIFBDRyDnm67lvZXphY3nva7mjInotYTmupDot6/lvoTliqDovb0='; Suggested = T '56Gu6K6k5L+d55WZ77yb5piv5ZCm57qz5YWl57uf5LiA6LWE5Lqn57Si5byV5b6F56Gu6K6k' }
        }
    }
    [pscustomobject]@{ Index = T '5pyq5Y+R546w546w5pyJ57Si5byV'; Role = ''; Suggested = T '56Gu6K6k55So6YCU77yb5Zyo55So5YiZ6KGl57Si5byV77yM5byD55So5YiZ5qCH6K6w5b6F5Yig6Zmk' }
}

$reviewItems = New-Object System.Collections.Generic.List[object]
$slicedAssets = $allAssets | Where-Object { $_.FullName -like '*\Sprite\PCG\Terrain\Sliced\*' }
$regularAssets = $allAssets | Where-Object { $_.FullName -notlike '*\Sprite\PCG\Terrain\Sliced\*' } | Sort-Object FullName
$serial = 1
foreach ($file in $regularAssets) {
    $relativePath = $file.FullName.Substring($projectRoot.Length + 1).Replace('\', '/')
    $state = Get-ReviewState $relativePath
    $reviewItems.Add([pscustomobject]@{ Id = ('ART-{0:d4}' -f $serial); Category = Get-DisplayCategory (Get-Category $relativePath); Path = $relativePath; Type = Get-DisplayType $file.Extension; ExistingIndex = $state.Index; CurrentRole = $state.Role; SuggestedAction = $state.Suggested; ReviewStatus = T '5b6F56Gu6K6k'; Notes = '' })
    $serial++
}

$tileGroups = $slicedAssets | Group-Object DirectoryName | Sort-Object Name
$tileNumber = 1
foreach ($group in $tileGroups) {
    $relativePath = $group.Name.Substring($projectRoot.Length + 1).Replace('\', '/')
    $reviewItems.Add([pscustomobject]@{ Id = ('PCG-TILE-{0:d2}' -f $tileNumber); Category = Get-DisplayCategory 'PCG_Terrain_Tile_Set'; Path = $relativePath + '/'; Type = (T 'UENHIOWcsOW9ouWIh+eJh++8iFBORyDDlyB7Y291bnR977yJ').Replace('{count}', $group.Count); ExistingIndex = ((T 'UENHIOe0ouW8lQ==') + ' (TerrainTileSetCatalog)'); CurrentRole = T '55SxIFBDRyDnm67lvZXphY3nva7mjInotYTmupDot6/lvoTliqDovb0='; SuggestedAction = T '56Gu6K6k5L+d55WZ77yb5piv5ZCm57qz5YWl57uf5LiA6LWE5Lqn57Si5byV5b6F56Gu6K6k'; ReviewStatus = T '5b6F56Gu6K6k'; Notes = '' })
    $tileNumber++
}

$headers = @((T '6LWE5rqQ57yW5Y+3'), (T '5YiG57G7'), (T '6Lev5b6E'), (T '57G75Z6L'), (T '546w5pyJ57Si5byV'), (T '5b2T5YmN55So6YCU6K+05piO'), (T '5bu66K6u5Yqo5L2c'), (T '56Gu6K6k57uT5p6c'), (T '5aSH5rOo'))
$columns = @(
    @{N=$headers[0];E={$_.Id}}, @{N=$headers[1];E={$_.Category}}, @{N=$headers[2];E={$_.Path}}, @{N=$headers[3];E={$_.Type}}, @{N=$headers[4];E={$_.ExistingIndex}}, @{N=$headers[5];E={$_.CurrentRole}}, @{N=$headers[6];E={$_.SuggestedAction}}, @{N=$headers[7];E={$_.ReviewStatus}}, @{N=$headers[8];E={$_.Notes}}
)
$csvPath = Join-Path $OutputDirectory 'art-asset-review.csv'
$reviewItems | Select-Object $columns | Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding UTF8

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# ' + (T '576O5pyv6LWE5rqQ5qC45a+55riF5Y2V'))
$lines.Add('')
$lines.Add((T '55Sf5oiQ5pe26Ze0') + ": $(Get-Date -Format 'yyyy-MM-dd HH:mm')")
$lines.Add('')
$lines.Add((T '6IyD5Zu077yaQXNzZXRzLyDkuIvpobnnm67oh6rmnInotYTmupDvvJvnrKzkuInmlrnmj5Lku7bkuI4gVGV4dE1lc2ggUHJvIOW3suaOkumZpOOAgnthbGx9IOS4quaWh+S7tu+8mntyZWd1bGFyfSDkuKrljZXpobnotYTmupDvvIx7dGlsZXN9IOS4qiBQQ0cg5Zyw5b2i5YiH54mH5bey5ZCI5bm25Li66LWE5rqQ6ZuG5ZCIIHtzZXRzfSDlpZfjgII=').Replace('{all}', $allAssets.Count).Replace('{regular}', $regularAssets.Count).Replace('{tiles}', $slicedAssets.Count).Replace('{sets}', $tileGroups.Count))
$lines.Add('')
$lines.Add((T '5aGr5YaZ6K+05piO77ya56Gu6K6k54q25oCB5Y+v5aGr77ya5L+d55WZ44CB5byD55So44CB6KGl57Si5byV5oiW5b6F56Gu6K6k44CC6K+35Zyo5aSH5rOo5Lit5aGr5YaZ55So6YCU5Y+K55uu5qCH57Si5byV6ZSu44CC'))
$lines.Add((T '6K+05piO77ya546w5pyJ57Si5byV5LuF6KGo56S65Y+R546w5LqG57Si5byV5pig5bCE77yb5pyq5Y+R546w546w5pyJ57Si5byV5LiN5Luj6KGo6LWE5rqQ5pyq6KKr5L2/55So44CC'))
foreach ($category in ($reviewItems | Group-Object Category | Sort-Object Name)) {
    $lines.Add('')
    $lines.Add("## $($category.Name) [$($category.Count)]")
    $lines.Add('')
    $lines.Add('| ' + ($headers -join ' | ') + ' |')
    $lines.Add('| --- | --- | --- | --- | --- | --- | --- | --- | --- |')
    foreach ($item in $category.Group) {
        $safe = @($item.Id, $item.Category, $item.Path, $item.Type, $item.ExistingIndex, $item.CurrentRole, $item.SuggestedAction, $item.ReviewStatus, $item.Notes) | ForEach-Object { ([string]$_).Replace('|', '\|').Replace("`r", ' ').Replace("`n", ' ') }
        $lines.Add('| ' + ($safe -join ' | ') + ' |')
    }
}

$markdownPath = Join-Path $OutputDirectory 'art-asset-review.md'
[System.IO.File]::WriteAllLines($markdownPath, $lines, [System.Text.UTF8Encoding]::new($true))
Write-Output "Generated: $markdownPath"
Write-Output "Generated: $csvPath"
Write-Output "Review items: $($reviewItems.Count)"
