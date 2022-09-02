@echo off

set root=%cd%
for /d /r %%s in (*) do (
    cd %%s
    set processDir="%%s"
    echo processing: %processDir%
    magick mogrify -resize "1024x1024>" -verbose *.png
    pngquant *.png --ext .png --verbose -f --skip-if-larger
    cd %root%
)

magick mogrify -resize "1024x1024>" -verbose *.png
pngquant *.png --ext .png --verbose -f --skip-if-larger