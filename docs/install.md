# Installation Instructions

## Basic Installation

### Windows

1. Download the MSUScripterSetupWin file from the [latest release](https://github.com/MattEqualsCoder/MSUScripter/releases)
2. Run the executable to install
3. Install all dependencies in the dependency installer window

### Linux

1. Install [.NET 9.0](https://learn.microsoft.com/en-us/dotnet/core/install/linux)
   - Make sure that dotnet is in the PATH
   - If you need multiple versions of .NET installed, you may want to use te [scripted install](https://learn.microsoft.com/en-us/dotnet/core/install/linux-scripted-manual#scripted-install) 
2. Download the MSUScripterLinux tar file from the [latest release](https://github.com/MattEqualsCoder/MSUScripter/releases)
3. Make the MSUScripter file executable to run or run the command `dotnet MSUScripter.dll`
4. Install all dependencies in the dependency installer window

## Manual Dependency Installation

With the exception of msupcm++, you can install the dependencies manually. This can be useful to preserve space if you are going to use FFmpeg or Python for other things.

### FFmpeg

FFmpeg is used by the Python Companion App to create YouTube videos for testing for copyright strikes.

1. Download [FFmpeg](https://www.ffmpeg.org/download.html)
2. Make sure FFmpeg is available in your PATH
3. To verify you should be able to open up a new terminal/command prompt window and simply type in `ffmpeg -version` to get a response

### Python Companion App

1. Download and install [Python](https://www.python.org/downloads/) (not needed on Linux)
2. Download and install [pipx](https://pipx.pypa.io/stable/installation/)
   - Make sure to run `pipx ensurepath`
3. Run the command `pipx install py-msu-scripter-app`
   - If preferred, you can also install pip and install via `python -m pip install py-msu-scripter-app` or `pip install py-msu-scripter-app` based on your environment
4. Verify it's installed by running either `py-msu-scripter-app --version` or `python -m py-msu-scripter-app --version`
