param(
    [string]$Commit = 'c25f5054d254d594fc44901e1951e850e9683407'
)

$ErrorActionPreference = 'Stop'
$outputDir = Join-Path $PSScriptRoot 'wwwroot/vendor/chess/tutorials'
$baseUrl = "https://raw.githubusercontent.com/mourad-ghafiri/KarpaChess/$Commit/"
$index = Invoke-RestMethod -Uri ($baseUrl + 'data/lessons/index.json') -TimeoutSec 30
$categories = @(
    foreach ($category in $index.categories) {
        $lessons = @(
            foreach ($name in $category.lessons) {
                Invoke-RestMethod -Uri ($baseUrl + 'data/lessons/en/' + $name) -TimeoutSec 30
            }
        )
        [ordered]@{ id = $category.id; icon = $category.icon; lessons = $lessons }
    }
)
$catalog = [ordered]@{
    source = "KarpaChess $Commit"
    sourceUrl = 'https://github.com/mourad-ghafiri/KarpaChess'
    license = 'MIT'
    categories = $categories
}
$license = (Invoke-WebRequest -Uri ($baseUrl + 'LICENSE') -UseBasicParsing -TimeoutSec 30).Content
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
[System.IO.File]::WriteAllText((Join-Path $outputDir 'lessons.json'), ($catalog | ConvertTo-Json -Depth 50), [System.Text.UTF8Encoding]::new($false))
[System.IO.File]::WriteAllText((Join-Path $outputDir 'LICENSE'), $license, [System.Text.UTF8Encoding]::new($false))
Write-Output "Imported $(@($categories | ForEach-Object { $_.lessons }).Count) lessons from $Commit"
