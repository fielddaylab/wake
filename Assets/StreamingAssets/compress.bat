@echo off

set root=%cd%
for /d /r %%s in (*) do (
    cd %%s
    
    set processDir="%%s"
    echo processing: %processDir%
    
    magick mogrify -resize "1024x1024>" -verbose *.png
    pngquant *.png --ext .png --verbose -f --skip-if-larger
    
    for %%a in (*.mp3) do (
        ffmpeg -i "%%a" -ab 96k "temp-%%a"
        del "%%a"
        ren "temp-%%a" "%%a"
    )
    
    cd %root%
)

magick mogrify -resize "1024x1024>" -verbose *.png
pngquant *.png --ext .png --verbose -f --skip-if-larger