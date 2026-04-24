$ollamaPath = $PSScriptRoot..ollama\ollamaSetup.exe

Start-Process -FilePath $ollamaPath -ArgumentList serve -WindowStyle Hidden