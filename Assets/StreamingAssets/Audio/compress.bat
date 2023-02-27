@echo off

set root=%cd%
for /d /r %%s in (*) do (
    cd %%s
    
    set processDir="%%s"
    echo processing: %processDir%
    
    for %%a in (*.mp3) do (
        ffmpeg -i "%%a" -ab 96k "temp-%%a.ogg"
        del "%%a.ogg"
        ren "temp-%%a.ogg" "%%a.ogg"
    
        ffmpeg -i "%%a" -ab 96k "temp-%%a"
        del "%%a"
        ren "temp-%%a" "%%a"
    )
    
    cd %root%
)