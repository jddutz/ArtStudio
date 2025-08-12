# Export the complete folder structure, including .cs files and folders, to a text file.
# Output is a flat list of relative paths (folders end with /), one per line.
# You can add more file types to the $fileTypes array as needed.

param(
    [string]$RootPath = (Get-Location),
    [string]$OutputFile = "folder-structure.txt"
)

# File types to include (add more extensions as needed)
$fileTypes = @("*.cs")

# Clear output file if it exists
if (Test-Path $OutputFile) {
    Remove-Item $OutputFile
}

# Helper function to check if a path contains 'bin' or 'obj' as a folder
function Is-IgnoredPath {
    param([string]$Path)
    return $Path -match "(\\|/)(bin|obj)(\\|/|$)"
}

# Get all directories (relative to root), ignoring 'bin' and 'obj'
$dirs = Get-ChildItem -Path $RootPath -Directory -Recurse | Where-Object { -not (Is-IgnoredPath $_.FullName) } | Sort-Object FullName
foreach ($dir in $dirs) {
    $relPath = Resolve-Path -Path $dir.FullName -Relative | ForEach-Object { $_.Replace(".\", "") }
    "$relPath/" | Out-File -FilePath $OutputFile -Append
}

# Get all files matching the patterns (relative to root), ignoring those in 'bin' or 'obj'
foreach ($type in $fileTypes) {
    $files = Get-ChildItem -Path $RootPath -Recurse -File -Filter $type | Where-Object { -not (Is-IgnoredPath $_.FullName) } | Sort-Object FullName
    foreach ($file in $files) {
        $relPath = Resolve-Path -Path $file.FullName -Relative | ForEach-Object { $_.Replace(".\", "") }
        "$relPath" | Out-File -FilePath $OutputFile -Append
    }
}

Write-Host "Exported folder structure to $OutputFile"
