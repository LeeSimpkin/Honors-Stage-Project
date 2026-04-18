$ollama = "$PSScriptRoot..\ollama\ollama.exe"

Write-Host "Starting Ollama..."
Start-Process -FilePath $ollama -ArgumentList "serve" -WindowStyle Hidden

Start-Sleep -Seconds 5

Write-Host "Checking models..."
$models = & $ollama list

if ($models -notmatch "llama3") {
Write-Host "Downloading model..."
& $ollama pull llama3
}

Write-Host "Done."
