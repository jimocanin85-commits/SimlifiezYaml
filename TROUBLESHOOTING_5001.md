# Troubleshooting localhost:5001 Connection

## Step 1: Check if port is in use
```powershell
# Check what's listening on port 5001
netstat -ano | findstr :5001

# If something is listening, you'll see output like:
# TCP    127.0.0.1:5001         0.0.0.0:0         LISTENING    12345
```

## Step 2: Kill any existing process on port 5001
```powershell
# Find process using port 5001
$proc = Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Found process: $($proc.OwningProcess)"
    Stop-Process -Id $proc.OwningProcess -Force
    Write-Host "Killed process"
}
```

## Step 3: Clean and rebuild
```powershell
cd "c:\Users\U39004\Documents\Coding\Yaml\SimlifiezYaml"
dotnet clean
dotnet build
```

## Step 4: Start the application with verbose output
```powershell
# This will show exactly what port the app starts on
dotnet run --project src/SimlifiezYaml.Web

# LOOK FOR THIS OUTPUT:
# "Now listening on: https://localhost:5001"
# OR
# "Now listening on: https://localhost:5002"  (if 5001 is taken)
```

## Step 5: If it doesn't start
```powershell
# Run with more diagnostics
dotnet run --project src/SimlifiezYaml.Web --verbosity diagnostic 2>&1 | Tee-Object -FilePath debug.log

# Check debug.log for errors
Get-Content debug.log | Select-Object -Last 50
```

## Step 6: Verify application files exist
```powershell
# Check if Program.cs exists
Test-Path "src/SimlifiezYaml.Web/Program.cs"

# Check web project
Test-Path "src/SimlifiezYaml.Web/SimlifiezYaml.Web.csproj"

# List components
Get-ChildItem "src/SimlifiezYaml.Web/Components/" -Recurse
```

## Step 7: Try different port
```powershell
# Run on alternate port if 5001 is problematic
dotnet run --project src/SimlifiezYaml.Web -- --urls "https://localhost:5002"

# Then open: https://localhost:5002
```

## Step 8: Check HTTPS certificate
```powershell
# If you see certificate errors, run:
dotnet dev-certs https --clean
dotnet dev-certs https --trust

# Then start app again
dotnet run --project src/SimlifiezYaml.Web
```

## Common Solutions Checklist

- [ ] Port 5001 not in use? `netstat -ano | findstr :5001` should be empty
- [ ] Application started? Look for "Now listening on:" message
- [ ] Correct port? App might use 5002, 5003 if 5001 taken
- [ ] HTTPS warning? Click "Advanced" then "Proceed" in browser
- [ ] Wait 5 seconds? App needs time to start
- [ ] Try different browser? Try Chrome, Edge, Firefox
- [ ] Check Windows Firewall? Allow dotnet.exe
