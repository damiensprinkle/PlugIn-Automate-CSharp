<#
.SYNOPSIS
    Checks whether a target project is compatible with the PlugIn-Automate-CSharp framework.

.DESCRIPTION
    Inspects the target project directory and reports on:
      - API project selection (interactive -- asks whether you need Tests.Api, Tests.Api.Live, or both)
      - Prerequisites  (.NET 8 SDK, NSwag CLI, Node.js, Playwright browsers)
      - Swagger/NSwag  (Swashbuckle package, UseSwagger(), swagger.json, nswag.json, generated client)
      - Packages       (target framework, version alignment, JWT key strength)
      - Test config    (testsettings.json / apitestsettings.json pools, credential completeness)
      - Structure      (.editorconfig, .gitignore, GitHub Actions workflow)
      - Visual tests   (baseline images present)
      - Live API probe (optional -- hit the Swagger endpoint if -ApiUrl is supplied)

.PARAMETER TargetPath
    Root directory of the project to check. Defaults to the current directory.

.PARAMETER ApiUrl
    Base URL of a running API instance (e.g. http://localhost:5000).
    When supplied, the script probes /swagger/v1/swagger.json and the auth endpoint.

.PARAMETER Detailed
    Show extra diagnostic lines for each check.

.EXAMPLE
    .\Check-Compatibility.ps1
    .\Check-Compatibility.ps1 -TargetPath C:\repos\MyApp
    .\Check-Compatibility.ps1 -TargetPath C:\repos\MyApp -ApiUrl http://localhost:5000 -Detailed
#>

param(
    [string] $TargetPath = ".",
    [string] $ApiUrl     = "",
    [switch] $Detailed
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

$script:PassCount = 0
$script:WarnCount = 0
$script:FailCount = 0

function Write-Pass([string]$msg) {
    Write-Host "  [PASS] $msg" -ForegroundColor Green
    $script:PassCount++
}

function Write-Warn([string]$msg) {
    Write-Host "  [WARN] $msg" -ForegroundColor Yellow
    $script:WarnCount++
}

function Write-Fail([string]$msg) {
    Write-Host "  [FAIL] $msg" -ForegroundColor Red
    $script:FailCount++
}

function Write-Info([string]$msg) {
    if ($Detailed) { Write-Host "         $msg" -ForegroundColor DarkGray }
}

function Write-Section([string]$title) {
    Write-Host ""
    Write-Host $title -ForegroundColor Cyan
    Write-Host ("-" * $title.Length) -ForegroundColor DarkGray
}

function Get-CsprojFiles([string]$root) {
    return Get-ChildItem -Path $root -Recurse -Filter "*.csproj" |
        Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }
}

function Get-PkgVersion([string]$xml, [string]$pkg) {
    $escaped = [regex]::Escape($pkg)
    if ($xml -match "Include=`"$escaped`"\s+Version=`"([^`"]+)`"") {
        return $Matches[1]
    }
    return $null
}

function Test-MinVer([string]$actual, [string]$min) {
    try { return ([version]$actual) -ge ([version]$min) } catch { return $false }
}

# ---------------------------------------------------------------------------
# Resolve path
# ---------------------------------------------------------------------------

$TargetPath = Resolve-Path $TargetPath | Select-Object -ExpandProperty Path

Write-Host ""
Write-Host "PlugIn-Automate-CSharp Compatibility Check" -ForegroundColor White
Write-Host "===========================================" -ForegroundColor White
Write-Host "Target : $TargetPath"
if ($ApiUrl) { Write-Host "API    : $ApiUrl" }

# ===========================================================================
# API Project Selection (interactive -- skipped on CI)
# ===========================================================================

$testsApiExists     = Test-Path (Join-Path $TargetPath "Tests.Api")
$testsApiLiveExists = Test-Path (Join-Path $TargetPath "Tests.Api.Live")
$isCI = ($env:CI -eq "true") -or ([bool]$env:TF_BUILD) -or ($env:GITHUB_ACTIONS -eq "true")

if ($testsApiExists -and $testsApiLiveExists -and -not $isCI) {
    Write-Host ""
    Write-Host "API Testing Mode" -ForegroundColor Cyan
    Write-Host "----------------" -ForegroundColor DarkGray
    Write-Host "  Both API test projects are present:"
    Write-Host ""
    Write-Host "    [1] Tests.Api       In-process WebApplicationFactory -- no running server needed."
    Write-Host "                        Best for unit-style integration tests with a swapped database."
    Write-Host ""
    Write-Host "    [2] Tests.Api.Live  Targets a live server with parallel credential pools"
    Write-Host "                        and [UseAccount], mirroring the E2E pattern."
    Write-Host "                        Best for smoke-testing dev, staging, or CI environments."
    Write-Host ""
    Write-Host "    [3] Both            Keep both projects."
    Write-Host ""

    try   { $choice = Read-Host "  Which will you use? (1/2/3)" }
    catch { $choice = "skip" }

    switch ($choice.Trim()) {
        "1" {
            Write-Host ""
            Write-Host "  You chose in-process testing (Tests.Api)." -ForegroundColor Green
            Write-Host "  Tests.Api.Live is not needed -- remove it with:" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "    dotnet sln remove Tests.Api.Live\Tests.Api.Live.csproj" -ForegroundColor DarkGray
            Write-Host "    Remove-Item -Recurse -Force Tests.Api.Live" -ForegroundColor DarkGray
            Write-Host ""
        }
        "2" {
            Write-Host ""
            Write-Host "  You chose live API testing (Tests.Api.Live)." -ForegroundColor Green
            Write-Host "  Tests.Api is not needed -- remove it with:" -ForegroundColor Yellow
            Write-Host ""
            Write-Host "    dotnet sln remove Tests.Api\Tests.Api.csproj" -ForegroundColor DarkGray
            Write-Host "    Remove-Item -Recurse -Force Tests.Api" -ForegroundColor DarkGray
            Write-Host ""
        }
        "3" {
            Write-Host ""
            Write-Host "  Keeping both projects." -ForegroundColor Green
            Write-Host ""
        }
        default {
            Write-Host ""
            Write-Host "  Skipping project recommendation." -ForegroundColor DarkGray
            Write-Host ""
        }
    }
} elseif (-not $testsApiExists -and -not $testsApiLiveExists -and -not $isCI) {
    Write-Host ""
    Write-Host "  Neither Tests.Api nor Tests.Api.Live were found at the target path." -ForegroundColor Yellow
    Write-Host "  Add at least one to enable API testing." -ForegroundColor Yellow
}

# ===========================================================================
# 1. Prerequisites
# ===========================================================================

Write-Section "1. Prerequisites"

# .NET SDK
try {
    $dotnetVer = (dotnet --version 2>$null).Trim()
    if (Test-MinVer $dotnetVer "8.0.0") {
        Write-Pass ".NET SDK $dotnetVer (>= 8.0 required)"
    } else {
        Write-Fail ".NET SDK $dotnetVer found -- 8.0 or higher required"
    }
    Write-Info "Path: $(Get-Command dotnet | Select-Object -ExpandProperty Source)"
} catch {
    Write-Fail ".NET SDK not found -- install from https://dot.net"
}

# PowerShell version
$psVer = $PSVersionTable.PSVersion.ToString()
if ([version]$PSVersionTable.PSVersion -ge [version]"5.1") {
    Write-Pass "PowerShell $psVer"
} else {
    Write-Warn "PowerShell $psVer -- 5.1 or higher recommended"
}

# NSwag CLI
try {
    $nswagOut = dotnet nswag version 2>&1 | Select-Object -First 1
    if ($nswagOut -match "(\d+\.\d+\.\d+)") {
        Write-Pass "NSwag CLI $($Matches[1])"
    } else {
        Write-Pass "NSwag CLI detected"
    }
} catch {
    Write-Warn "NSwag CLI not installed -- required for client code generation"
    Write-Info "Install: dotnet tool install -g NSwag.ConsoleCore"
}

# Node.js
try {
    $nodeVer = (node --version 2>$null).Trim().TrimStart("v")
    if (Test-MinVer $nodeVer "18.0.0") {
        Write-Pass "Node.js v$nodeVer (>= 18 recommended)"
    } else {
        Write-Warn "Node.js v$nodeVer -- 18 or higher recommended for Vite/frontend builds"
    }
} catch {
    Write-Warn "Node.js not found -- required only if the project has a frontend"
}

# Playwright browsers
$playwrightCachePaths = @(
    "$env:LOCALAPPDATA\ms-playwright",
    (Join-Path $HOME ".cache\ms-playwright"),
    (Join-Path $HOME "Library\Caches\ms-playwright")
)
$pwCache = $playwrightCachePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($pwCache) {
    $chromiumDir = Get-ChildItem -Path $pwCache -Filter "chromium-*" -Directory -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($chromiumDir) {
        Write-Pass "Playwright Chromium found ($($chromiumDir.Name))"
    } else {
        Write-Warn "Playwright cache found but Chromium is missing"
        Write-Info "Run: playwright install chromium"
    }
} else {
    Write-Warn "Playwright browsers not found -- run 'playwright install chromium' after build"
}

# ===========================================================================
# 2. Project Structure
# ===========================================================================

Write-Section "2. Project Structure"

# Solution file
$slnFiles = @(Get-ChildItem -Path $TargetPath -Filter "*.sln" -ErrorAction SilentlyContinue)
if ($slnFiles.Count -gt 0) {
    Write-Pass "Solution file: $($slnFiles[0].Name)"
    $slnContent = Get-Content $slnFiles[0].FullName -Raw
    $projMatches = [regex]::Matches($slnContent, 'Project\([^)]+\)\s*=\s*"([^"]+)"')
    foreach ($m in $projMatches) {
        Write-Info "  Project: $($m.Groups[1].Value)"
    }
} else {
    Write-Warn "No .sln file found at the target root"
}

# .editorconfig
$editorConfigPath = Join-Path $TargetPath ".editorconfig"
if (Test-Path $editorConfigPath) {
    $ec = Get-Content $editorConfigPath -Raw
    Write-Pass ".editorconfig present"
    if ($ec -match "csharp_style_expression_bodied_methods\s*=\s*never") {
        Write-Pass ".editorconfig enforces block-bodied methods"
    } else {
        Write-Warn ".editorconfig does not set csharp_style_expression_bodied_methods = never:warning"
    }
} else {
    Write-Warn ".editorconfig not found"
}

# .gitignore
$gitignorePath = Join-Path $TargetPath ".gitignore"
if (Test-Path $gitignorePath) {
    $gi = Get-Content $gitignorePath -Raw
    Write-Pass ".gitignore present"
    $expected = @("bin/", "obj/", "testsettings.local.json", "apitestsettings.local.json", "VisualResults/", "traces/")
    foreach ($e in $expected) {
        if ($gi -notmatch [regex]::Escape($e)) {
            Write-Warn ".gitignore is missing entry: $e"
        }
    }
} else {
    Write-Warn ".gitignore not found"
}

# GitHub Actions workflow
$ciFile = Get-ChildItem -Path $TargetPath -Recurse -Filter "*.yml" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match "\.github.workflows" } |
    Select-Object -First 1

if ($ciFile) {
    $ci = Get-Content $ciFile.FullName -Raw
    Write-Pass "CI workflow: $($ciFile.Name)"

    if ($ci -match "dotnet-version:\s*[`"']?8\.") {
        Write-Pass "CI uses .NET 8"
    } else {
        Write-Fail "CI dotnet-version does not appear to be 8.x"
    }

    if ($ci -match "net8\.0") {
        Write-Pass "CI output paths reference net8.0"
    } else {
        Write-Warn "CI output paths may still reference an older runtime folder (e.g. net7.0)"
    }

    if ($ci -match "TokenKey") {
        Write-Pass "CI sets TokenKey environment variable"
    } else {
        Write-Warn "TokenKey not found in CI workflow -- JWT auth will fail on CI"
    }
} else {
    Write-Warn "No GitHub Actions workflow found under .github/workflows/"
}

# ===========================================================================
# 3. NuGet Package Analysis
# ===========================================================================

Write-Section "3. NuGet Package Analysis"

$csprojFiles = Get-CsprojFiles -root $TargetPath

if ($csprojFiles.Count -eq 0) {
    Write-Warn "No .csproj files found"
} else {
    Write-Info "Found $($csprojFiles.Count) project(s)"
}

$swashbuckleFound   = $false
$playwrightPkgFound = $false
$xunitFound         = $false
$magickFound        = $false
$nswagMsBuildFound  = $false

foreach ($csproj in $csprojFiles) {
    $xml  = Get-Content $csproj.FullName -Raw
    $name = $csproj.Name

    # Target framework
    if ($xml -match "<TargetFramework>(net[^<]+)</TargetFramework>") {
        $tf = $Matches[1]
        if ($tf -match "^net[89]") {
            Write-Pass "$name -- TargetFramework: $tf"
        } else {
            Write-Fail "$name -- TargetFramework: $tf (net8.0 or higher required)"
        }
    }

    # Track packages across all projects
    if ($xml -match "Swashbuckle\.AspNetCore")                    { $swashbuckleFound   = $true }
    if ($xml -match "Microsoft\.Playwright")                       { $playwrightPkgFound = $true }
    if ($xml -match '"xunit"')                                     { $xunitFound         = $true }
    if ($xml -match "Magick\.NET")                                 { $magickFound        = $true }
    if ($xml -match "NSwag\.MSBuild")                              { $nswagMsBuildFound  = $true }

    # Warn about .NET 7 package versions on a .NET 8 project
    $fxPkgs = @(
        "Microsoft.AspNetCore.Authentication.JwtBearer",
        "Microsoft.AspNetCore.Identity.EntityFrameworkCore",
        "Microsoft.AspNetCore.Mvc.Testing",
        "Microsoft.EntityFrameworkCore.Sqlite",
        "Microsoft.EntityFrameworkCore.InMemory",
        "Microsoft.EntityFrameworkCore.Design"
    )
    foreach ($pkg in $fxPkgs) {
        $ver = Get-PkgVersion $xml $pkg
        if ($ver -and $ver.StartsWith("7.")) {
            Write-Warn "$name : $pkg $ver -- upgrade to 8.x to match TargetFramework"
        }
    }

    # JWT token key strength check
    $devSettings = Join-Path $csproj.DirectoryName "appsettings.Development.json"
    if (Test-Path $devSettings) {
        $appJson = Get-Content $devSettings -Raw
        # Use -replace to extract token key value safely
        $tokenKeyMatch = [regex]::Match($appJson, '"TokenKey"\s*:\s*"([^"]+)"')
        if ($tokenKeyMatch.Success) {
            $key     = $tokenKeyMatch.Groups[1].Value
            $keyLen  = [System.Text.Encoding]::UTF8.GetByteCount($key)
            if ($keyLen -ge 64) {
                Write-Pass "TokenKey in appsettings.Development.json: $keyLen bytes (>= 64 required for HS512)"
            } else {
                Write-Fail "TokenKey in appsettings.Development.json: $keyLen bytes -- HS512 requires >= 64 bytes (use 72+ chars)"
            }
        }
    }
}

if ($xunitFound)         { Write-Pass "xunit referenced" }         else { Write-Warn "xunit not found -- required for test projects" }
if ($playwrightPkgFound) { Write-Pass "Microsoft.Playwright referenced" } else { Write-Warn "Microsoft.Playwright not found -- required for E2E tests" }
if ($magickFound)        { Write-Pass "Magick.NET referenced (visual tests)" } else { Write-Warn "Magick.NET not found -- required for visual regression tests" }

# ===========================================================================
# 4. Swagger / NSwag Setup
# ===========================================================================

Write-Section "4. Swagger / NSwag Setup"

if ($swashbuckleFound) {
    Write-Pass "Swashbuckle.AspNetCore package found"
} else {
    Write-Warn "Swashbuckle.AspNetCore not found -- add to your API project"
    Write-Info "dotnet add package Swashbuckle.AspNetCore"
}

# app.UseSwagger() in Program.cs / Startup.cs
$programFiles = Get-ChildItem -Path $TargetPath -Recurse -Include "Program.cs","Startup.cs" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" }

$useSwaggerFound = $false
foreach ($f in $programFiles) {
    if ((Get-Content $f.FullName -Raw) -match "UseSwagger\s*\(") {
        $useSwaggerFound = $true
        Write-Pass "app.UseSwagger() found in $($f.Name)"
        break
    }
}
if (-not $useSwaggerFound) {
    Write-Warn "app.UseSwagger() not detected in Program.cs / Startup.cs"
    Write-Info "Add: app.UseSwagger(); app.UseSwaggerUI(); to your API startup"
}

if ($nswagMsBuildFound) {
    Write-Pass "NSwag.MSBuild found (client auto-generated on build)"
} else {
    Write-Warn "NSwag.MSBuild not referenced -- add to Tests.Client for automatic code generation"
    Write-Info "dotnet add package NSwag.MSBuild"
}

# nswag.json
$nswagJsonFile = Get-ChildItem -Path $TargetPath -Recurse -Filter "nswag.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" } |
    Select-Object -First 1

if ($nswagJsonFile) {
    $relPath = $nswagJsonFile.FullName.Replace($TargetPath, ".")
    Write-Pass "nswag.json found: $relPath"

    try {
        $nswag = Get-Content $nswagJsonFile.FullName -Raw | ConvertFrom-Json

        if ($nswag.runtime -eq "Net80") {
            Write-Pass "nswag.json runtime: Net80"
        } elseif ($nswag.runtime) {
            Write-Warn "nswag.json runtime: '$($nswag.runtime)' -- update to Net80 for .NET 8"
        }

        $outputFile = $nswag.codeGenerators.openApiToCSharpClient.output
        if ($outputFile) {
            Write-Pass "nswag.json output: $outputFile"
            $outputFull = Join-Path $nswagJsonFile.DirectoryName $outputFile
            if (Test-Path $outputFull) {
                Write-Pass "Generated client exists: $outputFile"
            } else {
                Write-Warn "Generated client not found: $outputFile"
                Write-Info "Build the solution to trigger NSwag code generation"
            }
        }
    } catch {
        Write-Warn "nswag.json could not be parsed: $($_.Exception.Message)"
    }
} else {
    Write-Warn "nswag.json not found -- NSwag code generation not configured"
    Write-Info "Create nswag.json at your Tests.Client project root"
}

# swagger.json
$swaggerFiles = @(Get-ChildItem -Path $TargetPath -Recurse -Filter "swagger.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" })

if ($swaggerFiles.Count -gt 0) {
    foreach ($sf in $swaggerFiles) {
        $age    = (Get-Date) - $sf.LastWriteTime
        $ageStr = if ($age.TotalDays -ge 1) { "$([int]$age.TotalDays)d ago" } else { "$([int]$age.TotalHours)h ago" }
        $rel    = $sf.FullName.Replace($TargetPath, ".")
        Write-Pass "swagger.json: $rel (last modified $ageStr)"
        try {
            $spec      = Get-Content $sf.FullName -Raw | ConvertFrom-Json
            $pathCount = @($spec.paths | Get-Member -MemberType NoteProperty -ErrorAction SilentlyContinue).Count
            Write-Info "Title: $($spec.info.title)  Version: $($spec.info.version)  Paths: $pathCount"
        } catch {
            Write-Warn "swagger.json at $($sf.Name) could not be parsed"
        }
    }
} else {
    Write-Warn "swagger.json not found -- required for NSwag code generation"
    Write-Info "Export: dotnet swagger tofile --output swagger.json <project.dll> v1"
    Write-Info "Or:     curl http://localhost:5000/swagger/v1/swagger.json -o Tests.Client/swagger.json"
}

# ===========================================================================
# 5. Live API Probe (optional)
# ===========================================================================

if ($ApiUrl) {
    Write-Section "5. Live API Probe"

    $swaggerUrl = $ApiUrl.TrimEnd("/") + "/swagger/v1/swagger.json"
    Write-Info "Probing: $swaggerUrl"
    try {
        $resp = Invoke-WebRequest -Uri $swaggerUrl -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        if ($resp.StatusCode -eq 200) {
            Write-Pass "Swagger endpoint reachable (HTTP 200)"
            try {
                $spec      = $resp.Content | ConvertFrom-Json
                $pathCount = @($spec.paths | Get-Member -MemberType NoteProperty -ErrorAction SilentlyContinue).Count
                Write-Pass "Swagger spec is valid JSON -- $pathCount path(s) defined"
                Write-Info "Title: $($spec.info.title)  Version: $($spec.info.version)"
            } catch {
                Write-Warn "Swagger endpoint returned 200 but content is not valid JSON"
            }
        } else {
            Write-Warn "Swagger endpoint returned HTTP $($resp.StatusCode)"
        }
    } catch {
        Write-Warn "Could not reach $swaggerUrl"
        Write-Info "Ensure the API is running: dotnet run --project <API.csproj>"
        Write-Info "Error: $($_.Exception.Message)"
    }

    # Auth endpoint probe
    $loginUrl = $ApiUrl.TrimEnd("/") + "/api/account/login"
    Write-Info "Probing auth endpoint: $loginUrl"
    try {
        $body    = '{"email":"probe@check.local","password":"invalid"}'
        $headers = @{ "Content-Type" = "application/json" }
        $authResp = Invoke-WebRequest -Uri $loginUrl -Method POST -Body $body `
                        -Headers $headers -UseBasicParsing -TimeoutSec 10 -ErrorAction Stop
        Write-Pass "Auth endpoint reachable (HTTP $($authResp.StatusCode))"
    } catch [System.Net.WebException] {
        $sc = [int]$_.Exception.Response.StatusCode
        if ($sc -in @(400, 401, 404)) {
            Write-Pass "Auth endpoint reachable (HTTP $sc -- expected for invalid credentials)"
        } else {
            Write-Warn "Auth endpoint returned unexpected HTTP $sc"
        }
    } catch {
        Write-Warn "Auth endpoint not reachable at $loginUrl"
        Write-Info "Error: $($_.Exception.Message)"
    }
}

# ===========================================================================
# 6. Test Configuration
# ===========================================================================

$secNum = if ($ApiUrl) { "6" } else { "5" }
Write-Section "$secNum. Test Configuration"

function Test-SettingsFile([string]$filePath, [string]$label, [bool]$requireApiUrl) {
    $rel = $filePath.Replace($TargetPath, ".")
    Write-Pass "${label}: $rel"
    try {
        $ts = Get-Content $filePath -Raw | ConvertFrom-Json

        if ($ts.BaseUrl) {
            Write-Pass "BaseUrl: $($ts.BaseUrl)"
        } else {
            Write-Fail "BaseUrl missing from $label"
        }

        if ($requireApiUrl) {
            if ($ts.ApiUrl) {
                Write-Pass "ApiUrl: $($ts.ApiUrl)"
            } else {
                Write-Fail "ApiUrl missing from $label (required for E2E auth)"
            }
        }

        $pools = @($ts.Pools | Get-Member -MemberType NoteProperty -ErrorAction SilentlyContinue)
        if ($pools.Count -ge 2) {
            Write-Pass "$($pools.Count) credential pool(s) defined"
        } elseif ($pools.Count -eq 1) {
            Write-Warn "Only 1 pool defined -- add Pool2 to enable parallel test collections"
        } else {
            Write-Fail "No pools defined in $label > Pools"
        }

        foreach ($pool in $pools) {
            $poolObj  = $ts.Pools.$($pool.Name)
            $accounts = @($poolObj | Get-Member -MemberType NoteProperty -ErrorAction SilentlyContinue)
            foreach ($acct in $accounts) {
                $a       = $poolObj.$($acct.Name)
                $missing = @(@("Email","Password","DisplayName","Username") |
                    Where-Object { -not $a.$_ -or $a.$_ -eq "" })
                if ($missing.Count -eq 0) {
                    Write-Pass "Pool $($pool.Name) / $($acct.Name) -- all fields present"
                } else {
                    Write-Warn "Pool $($pool.Name) / $($acct.Name) -- missing: $($missing -join ', ')"
                }
            }
        }
    } catch {
        Write-Warn "Could not parse ${label}: $($_.Exception.Message)"
    }

    $dir       = Split-Path $filePath -Parent
    $base      = (Split-Path $filePath -Leaf) -replace "\.json$", ""
    $localPath = Join-Path $dir "$base.local.json"
    if (Test-Path $localPath) {
        Write-Pass "$base.local.json present (local overrides active)"
    } else {
        Write-Info "Tip: create $base.local.json (gitignored) to override settings locally"
    }
}

# E2E settings
$testSettingsFiles = @(Get-ChildItem -Path $TargetPath -Recurse -Filter "testsettings.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" })

if ($testSettingsFiles.Count -eq 0) {
    Write-Fail "testsettings.json not found -- required for E2E tests"
} else {
    foreach ($tsf in $testSettingsFiles) {
        Test-SettingsFile -filePath $tsf.FullName -label "testsettings.json" -requireApiUrl $true
    }
}

# Live API settings
$apiSettingsFiles = @(Get-ChildItem -Path $TargetPath -Recurse -Filter "apitestsettings.json" -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" })

if ($apiSettingsFiles.Count -gt 0) {
    foreach ($asf in $apiSettingsFiles) {
        Test-SettingsFile -filePath $asf.FullName -label "apitestsettings.json" -requireApiUrl $false
    }
} elseif ($testsApiLiveExists) {
    Write-Warn "apitestsettings.json not found in Tests.Api.Live -- required for live API tests"
}

# ===========================================================================
# 7. Visual Test Baselines
# ===========================================================================

$secNum = if ($ApiUrl) { "7" } else { "6" }
Write-Section "$secNum. Visual Test Baselines"

$baselineDirs = @(Get-ChildItem -Path $TargetPath -Recurse -Filter "Baselines" -Directory -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -notmatch "\\(obj|bin)\\" -and $_.FullName -notmatch "\.playwright" })

if ($baselineDirs.Count -eq 0) {
    Write-Warn "No Baselines directory found -- visual tests will fail until baselines are committed"
    Write-Info "Run with UPDATE_VISUAL_BASELINES=true, then copy images into Visual/Baselines/ and commit"
} else {
    foreach ($dir in $baselineDirs) {
        $pngs = @(Get-ChildItem -Path $dir.FullName -Filter "*.png" -ErrorAction SilentlyContinue)
        $rel  = $dir.FullName.Replace($TargetPath, ".")
        if ($pngs.Count -gt 0) {
            Write-Pass "$rel -- $($pngs.Count) baseline image(s)"
            foreach ($png in $pngs) { Write-Info "  $($png.Name)" }
        } else {
            Write-Warn "$rel exists but contains no .png files"
            Write-Info "Run tests with UPDATE_VISUAL_BASELINES=true to generate baselines"
        }
    }
}

# ===========================================================================
# Summary
# ===========================================================================

Write-Host ""
Write-Host "===========================================" -ForegroundColor White
Write-Host "Summary" -ForegroundColor White
Write-Host "===========================================" -ForegroundColor White
Write-Host "  Passed   : $script:PassCount" -ForegroundColor Green
Write-Host "  Warnings : $script:WarnCount" -ForegroundColor Yellow
Write-Host "  Failed   : $script:FailCount" -ForegroundColor Red
Write-Host ""

if ($script:FailCount -gt 0) {
    Write-Host "One or more checks FAILED. Resolve the issues above before running tests." -ForegroundColor Red
    exit 1
} elseif ($script:WarnCount -gt 0) {
    Write-Host "All critical checks passed with warnings. Review warnings before pushing to CI." -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "All checks passed. The project looks compatible with PlugIn-Automate-CSharp." -ForegroundColor Green
    exit 0
}
