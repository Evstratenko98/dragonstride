param(
  [Parameter(Mandatory=$true)]
  [string]$ModuleName
)

$paths = @(
  "$ModuleName\Domain",
  "$ModuleName\Application",
  "$ModuleName\Presentation",
  "$ModuleName\Infrastructure",
  "$ModuleName\Config",
  "$ModuleName\Events",
  "$ModuleName\Art\Prefabs",
  "$ModuleName\Art\Materials"
)

foreach ($p in $paths) {
  New-Item -ItemType Directory -Path $p -Force | Out-Null
}

Write-Host "✅ Модуль '$ModuleName' создан в текущей директории"