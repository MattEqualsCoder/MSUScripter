# MSU Scripter

A cross platform application built for creating MSUs and related files. The application works as a wrapper around [msupcm++](https://github.com/qwertymodo/msupcmplusplus), creating the necessary JSON and executing it to generate pcm files. Furthermore, it also creates YAML files for the [MSU Randomizer](https://github.com/MattEqualsCoder/MSURandomizer) to help it identify MSUs and their tracks and display the current playing song.

![image](https://github.com/MattEqualsCoder/MSUScripter/assets/63823784/8aa52ce7-06e5-4405-aa83-33fbaba42e9d)

## Features

- **Converts audio files into pcm files** ([msupcm++](https://github.com/qwertymodo/msupcmplusplus) required) - Through the UI you can enter almost any field available to msupcm++ and generate either all pcm files for the MSU-1 or even just single files for testing. The MSU Scripter will even verify that created pcms are valid and can run multiple instances of msupcm++ simultaneously for faster generation of MSUs.
- **Create MSU YAML files with song information** - The MSU Scripter will write YAML files for the [MSU Randomizer](https://github.com/MattEqualsCoder/MSURandomizer) to pull information about the MSU and the songs such as the song name, artist, and album. These YAML files also double as generated user friendly track lists! [View more information about the MSU Randomizer YAML files here.](https://github.com/MattEqualsCoder/MSURandomizer/blob/main/Docs/yaml.md)
- **Play generated pcm files** - Built into the MSU Scripter is an audio player for playing pcm files, including an option to start playing near the end of the song to test loop points.
- **Audio balance analysis** - After creating your MSU, run the Audio Analysis to scan through all of the generated files to look for songs that have an average volume or peak that is louder than the overall averages of all of the songs.
- **Auto loop detection** ([PyMusicLooper](https://github.com/arkrow/PyMusicLooper) required) - By using the PyMusicLooper script, the MSU Scripter can attempt to find a loop point for you.
- **Import audio metadata** - Automatically pulls in mp3 and flac metadata for song names, artists, and playlists.
- **Alt file support** - Want to include different options for people for particular tracks? Add multiple songs to a track, and it will generate different pcm files for each track. Optionally, the MSU Scripter will even generate a .bat (batch) file for swapping between the original base file and the alt files.
- **Auto create split ALttPR and VARIA files for SMZ3 MSUs** - Creating an SMZ3 MSU and want to allow people to change it to work with ALttPR or VARIA randomizers? The MSU Scripter will create separate MSUs and their YAML files and then create a .bat file for swapping between combined SMZ3 and split ALttPR and VARIA MSUs.
- **Import previously created MSUs** - Already have an MSU you want to create YAML files for or make updates to the pcm files? Simply point the MSU Scripter to a previously created MSU and, if msupcm++ is being used, the msupcm++ json file and the directory you previously executed msupcm++ from. It'll pull in all the info it can from the files.
- **Create video files for copyright testing** ([MSU Test Video Creator](https://github.com/MattEqualsCoder/MSUTestVideoCreator) required) - Based on a Python code by [StructuralMike](https://github.com/StructuralMike), create an mp4 video file with all of the songs which can be uploaded to YouTube to test for copyright strikes.
- **Supports over 20 types of MSUs** - Thanks to JSON files created by [Minnie Trethewey](https://github.com/miketrethewey), the MSU Scripter supports a variety of MSU types, such as A Link to the Past, Super Metroid, SMZ3, Donkey Kong Country, Super Mario World, and others!

## Setup

- Download the latest release via the GitHub Releases page
    - Windows - Run the installer exe file to install all pre-requisites and the MSU Scripter
    - Linux - Make sure to have .net 8 installed. Either make the MSUScripter file executable and run it or run dotnet MSUScripter.dll
- Download [msupcm++](https://github.com/qwertymodo/msupcmplusplus) if generating pcms is desired
- Install [PyMusicLooper](https://github.com/arkrow/PyMusicLooper) if automatic loop point detection is desired (make sure you can run it via a PowerShell or Terminal window)
- Install [MSU Test Video Creator](https://github.com/MattEqualsCoder/MSUTestVideoCreator) for creating mp4 files to upload to YouTube to test for potential copyright strikes
- For Linux users, install [PCM Player](https://github.com/MattEqualsCoder/pcm_player) in order to be able to listen to pcm files
- Add in all details desired about the MSU and the tracks
- Click on export to generate the MSU and all related files

## Troubleshooting & Support

Having problems? Please feel free to [post an Issue on GitHub](). If encountering a crash, please include the latest log file located at %localappdata%/MSUScripter.

## Credit & Thanks

- [Vivelin](https://vivelin.net/) for the [SMZ3 Cas' Randomizer](https://github.com/Vivelin/SMZ3Randomizer), from which I borrowed some code snippets here and there.
- [PinkKittyRose](https://www.twitch.tv/pinkkittyrose) and Phiggle for testing this for me.
- [Minnie Trethewey](https://github.com/miketrethewey) for the json files for the MSU types.
- [qwertymodo](https://github.com/qwertymodo) for msupcm++ 
- [arkrow](https://github.com/arkrow) for PyMusicLooper
- [StructuralMike](https://github.com/StructuralMike) for the original Python code to create the Copyright Test Videos.
