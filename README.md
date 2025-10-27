# MSU Scripter

A cross platform application built for creating MSUs, PCMs, and related files for over [20 different randomizers and rom patches](https://github.com/MattEqualsCoder/MSURandomizer/tree/main/Docs/YamlTemplates). The application works as a wrapper around [msupcm++](https://github.com/qwertymodo/msupcmplusplus), creating the necessary JSON file and executing msupcm++ to generate pcm files. Furthermore, it also creates YAML files for the [MSU Randomizer](https://github.com/MattEqualsCoder/MSURandomizer) to help it identify MSUs and their tracks and display the current playing song.

<img width="1280" height="830" alt="image" src="https://github.com/user-attachments/assets/da70eea8-9c00-4034-8b92-0a25a57c625f" />

## Installation

- Download the latest release via the [GitHub Releases page](https://github.com/MattEqualsCoder/MSUScripter/releases)
    - Windows - Download and run MSUScripterSetupWin executable
    - Linux - Download MSUScripter.x86_64.AppImage file, place where you want, and make executable
- Install dependencies like MsuPcm++ in the dependency window
- For manual dependency installation, read the [install docs](Docs/install.md)
  
## Features

- Enter song details in the UI and have the MSU Scripter run MsuPcm++ to generate PCM files.
- Import audio metadata and update to provide additional song info for [MSU Randomizer YAML files](https://github.com/MattEqualsCoder/MSURandomizer) and track list text files.
- Automatically detect loop points by running PyMusicLooper.
- Run the Audio Analysis tool to compare volume levels between songs and even other MSUs.
- Create an mp4 video file with all of the songs which can be uploaded to YouTube to test for copyright strikes.
- Using the built in MSU Scripter audio player, listen to the generated PCM files.
- Add additional songs for tracks and the MSU Scripter will generate a bat file to swap to the alt track.
- For SMZ3 MSUs, it can create bat files to split into Super Metroid and A Link to the Past MSUs.
- Import previously created MSUs to be able to create the MSU Randomizer YAML files.

## Troubleshooting & Support

Having problems? Please feel free to [post an Issue on GitHub](https://github.com/MattEqualsCoder/MSUScripter/issues). You can also reach out on some of the main randomizer discords. If encountering a crash, please include the latest log file located at %localappdata%/MSUScripter on Windows or ~/.local/share/MSUScripter/Logs on Linux.

## Future

As of version 5.0.0, I do not predict making any future updates outside of bug fixes and upgrades to avoid things getting out of date. There are some new features in the [issues list](https://github.com/MattEqualsCoder/MSUScripter/issues) however if anyone is interested in making contributions.

## Credit & Thanks

- [qwertymodo](https://github.com/qwertymodo) for [msupcm++](https://github.com/qwertymodo/msupcmplusplus)
- [arkrow](https://github.com/arkrow) for [PyMusicLooper](https://github.com/arkrow/PyMusicLooper)
- [StructuralMike](https://www.twitch.tv/structuralmike) for the original Python code to create the Copyright Test Videos.
- [Vivelin](https://vivelin.net/) for the [SMZ3 Cas' Randomizer](https://github.com/Vivelin/SMZ3Randomizer), from which I borrowed some code snippets here and there.
- [Minnie Trethewey](https://github.com/miketrethewey) for the json files for the MSU types.
- [Astral](https://github.com/astral-sh) for creating the standalone Python package for making it easier to provide PyMusicLooper
- [VoidTear](https://github.com/Vo1dTear) for creating an AppImage version of msupcm++ to make it easier for Linux users
- [PinkKittyRose](https://www.twitch.tv/pinkkittyrose), Phiggle, and [codemann8](https://github.com/codemann8) for testing and providing feedback.
- All of the MSU creators for making it worth creating this!
