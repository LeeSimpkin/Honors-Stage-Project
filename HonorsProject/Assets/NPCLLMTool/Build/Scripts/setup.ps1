$ollama = "$PSScriptRoot..\ollama\OllamaSetup.exe"

Write-Host "Starting Ollama..."
Start-Process -FilePath $ollama -ArgumentList "serve" -WindowStyle Hidden

Start-Sleep -Seconds 5

Write-Host "Checking models..."
$models = & $ollama list

if ($models -notmatch "llama3.2") {
Write-Host "Downloading model..."
& $ollama pull llama3.2
}

if ($models -notmatch "Phi3") {
Write-Host "Downloading model..."
& $ollama pull Phi3
}

if ($models -notmatch "tinyllama") {
Write-Host "Downloading model..."
& $ollama pull tinyllama
}

Write-Host "Done."

$source = "$PSScriptRoot..\models"
$dest = "$env.ollama\models"

Write-Host "Copying models..."

New-Item -ItemType Directory -Force -Path $dest

Copy-Item -Path "$source*" -Destination $dest -Recurse -Force

Write-Host "Models copied"