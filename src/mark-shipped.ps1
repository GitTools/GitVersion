#! /usr/bin/env pwsh

[CmdletBinding(PositionalBinding = $false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function MarkShipped([string]$dir) {
    $nullableHeader = "#nullable enable";
    $shippedFilePath = Join-Path $dir "PublicAPI.Shipped.txt"
    $shipped = @()
    $shipped += Get-Content $shippedFilePath

    $unshippedFilePath = Join-Path $dir "PublicAPI.Unshipped.txt"
    $unshipped = Get-Content $unshippedFilePath
    $added = @()
    $removed = @()
    $removedPrefix = "*REMOVED*";

    foreach ($item in $unshipped) {
        if ($item.Length -gt 0 -and $item -ne $nullableHeader) {
            if ( $item.StartsWith($removedPrefix)) {
                $item = $item.Substring($removedPrefix.Length)
                $removed += $item
            }
            else {
                $shipped += $item
                $added += $item
            }
        }
    }

    $changeCount = $added.Count + $removed.Count
    Write-Host ("{0,-6} Processed {1}" -f "[$changeCount]", $unshippedFilePath)

    $shipped | Sort-Object -Unique | Where-Object { -not $removed.Contains($_) } | Out-File $shippedFilePath -Encoding Ascii
    $nullableHeader | Out-File $unshippedFilePath -Encoding Ascii
}

try {
    foreach ($file in Get-ChildItem -re -in "PublicApi.Shipped.txt") {
        $dir = Split-Path -parent $file
        MarkShipped $dir
    }
    if ($null -ne (git status --porcelain)) {
        Write-Host "Changes detected, committing and pushing changes"
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}
