$ollamaPath = $PSScriptRoot..ollamaollama.exe

Start-Process -FilePath $ollamaPath -ArgumentList serve -WindowStyle Hidden