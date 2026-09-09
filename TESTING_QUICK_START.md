# Quick Start - Testing Commands

**Copy-paste these commands directly into PowerShell**

---

## Quick Setup (One Time)
```powershell
cd "c:\Users\U39004\Documents\Coding\Yaml\SimlifiezYaml"
dotnet restore
dotnet build
```

---

## Run All Tests (Verify Everything Works)
```powershell
dotnet test
```

**Expected:** ✅ ~95+ tests pass

---

## Quick Test by Phase

```powershell
# Phase 1 Tests (Critical Fixes)
dotnet test --filter "ClassName~ErrorPathTests"

# Phase 2 Tests (Code Quality & Error Handling)  
dotnet test --filter "ClassName~ErrorPathTests"

# Phase 3 Tests (Architecture Refactoring)
dotnet test --filter "ClassName~Phase3ArchitectureTests"

# Integration Tests
dotnet test --filter "ClassName~PipelineGeneratorTests"
```

---

## Start Web Application
```powershell
# Basic (stops on exit)
dotnet run --project src/SimlifiezYaml.Web

# With auto-reload (recommended for development)
dotnet watch --project src/SimlifiezYaml.Web run
```

**Then open:** https://localhost:5001

**Stop:** Press Ctrl+C

---

## Full Test Run with Coverage
```powershell
dotnet test --verbosity normal --logger "console;verbosity=normal"
```

---

## Single Test
```powershell
dotnet test --filter "FullyQualifiedName~SimlifiezYaml.Core.Tests.Phase3ArchitectureTests.YamlAssembler_BuildsCompleteYaml"
```

---

## Clean & Rebuild (If Issues)
```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
```

---

## Check Compilation
```powershell
dotnet build
```

**Expected:** ✅ Build succeeded (0 errors, 0 warnings)

---

## Most Common Issue Resolution

```powershell
# 1. Port already in use? Solution:
taskkill /FI "WINDOWSTITLE eq *dotnet*" /F

# 2. Package restore failed? Solution:
dotnet nuget locals all --clear
dotnet restore --force

# 3. Tests failing? Solution:
dotnet clean
dotnet build
dotnet test
```

---

**For detailed help:** See `LOCAL_TESTING_GUIDE.md` in project root
