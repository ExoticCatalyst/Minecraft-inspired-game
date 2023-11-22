#!/bin/bash
# uses Aseprite to downscale the upscaled textures back to 32x32
for filename in ./*.png; do
    aseprite -b "$filename" --shrink-to 32,32 --save-as "$filename"
done
