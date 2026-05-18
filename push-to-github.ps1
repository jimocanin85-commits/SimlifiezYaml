# SimlifiezYaml - push to GitHub
# Run: powershell -ExecutionPolicy Bypass -File push-to-github.ps1

$ErrorActionPreference = "Continue"
$repoUrl = "https://github.com/jimocanin85-commits/SimlifiezYaml.git"
Set-Location $PSScriptRoot

function Invoke-Git {
    param([string[]]$Args)
    $out = & git @Args 2>&1
    $code = $LASTEXITCODE
    if ($out) { $out | ForEach-Object { Write-Host $_ } }
    return $code
}

# --- Require Git identity (repo-local or global) ---
$name = (git config user.name 2>$null)
$email = (git config user.email 2>$null)
if (-not $name -or -not $email) {
    Write-Host "`nGit needs your name and email before it can commit." -ForegroundColor Yellow
    Write-Host "Run these once (use your real name and GitHub email):" -ForegroundColor Yellow
    Write-Host '  git config user.name "Your Name"' -ForegroundColor Cyan
    Write-Host '  git config user.email "you@example.com"' -ForegroundColor Cyan
    Write-Host "`nGitHub noreply email example:" -ForegroundColor Yellow
    Write-Host '  git config user.email "jimocanin85-commits@users.noreply.github.com"' -ForegroundColor Cyan
    exit 1
}

Write-Host "=== 1. git init & remote ===" -ForegroundColor Cyan
Invoke-Git init | Out-Null
git remote remove origin 2>$null | Out-Null
Invoke-Git remote, add, origin, $repoUrl | Out-Null
Invoke-Git remote, -v | Out-Null

Write-Host "`n=== 2. git add & commit (must happen BEFORE pull) ===" -ForegroundColor Cyan
Invoke-Git add, . | Out-Null
$commitCode = Invoke-Git commit, -m, "Add enterprise SimlifiezYaml Azure DevOps pipeline builder"
if ($commitCode -ne 0) {
    $status = git status --porcelain 2>$null
    if (-not $status) {
        Write-Host "Nothing new to commit (working tree clean)." -ForegroundColor Yellow
    } else {
        Write-Host "Commit failed. Fix errors above and run this script again." -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n=== 3. git branch main ===" -ForegroundColor Cyan
Invoke-Git branch, -M, main | Out-Null

Write-Host "`n=== 4. git fetch ===" -ForegroundColor Cyan
Invoke-Git fetch, origin | Out-Null

$remoteMain = git ls-remote --heads origin main 2>$null
if ($remoteMain) {
    Write-Host "`n=== 5. merge remote main (e.g. empty GitHub README) ===" -ForegroundColor Cyan
    $pullCode = Invoke-Git pull, origin, main, --allow-unrelated-histories, --no-edit
    if ($pullCode -ne 0) {
        Write-Host "Merge conflict? Keep local README and complete merge:" -ForegroundColor Yellow
        Write-Host "  git checkout --ours README.md" -ForegroundColor Cyan
        Write-Host "  git add README.md" -ForegroundColor Cyan
        Write-Host '  git commit -m "Merge remote main, keep local project README"' -ForegroundColor Cyan
        exit 1
    }
}

Write-Host "`n=== 6. git push ===" -ForegroundColor Cyan
$pushCode = Invoke-Git push, -u, origin, main
if ($pushCode -eq 0) {
    Write-Host "`nDone: https://github.com/jimocanin85-commits/SimlifiezYaml" -ForegroundColor Green
} else {
    Write-Host "`nPush failed. Check auth: gh auth status" -ForegroundColor Red
    exit 1
}
