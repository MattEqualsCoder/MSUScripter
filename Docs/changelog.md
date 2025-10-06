# MSU Scripter 5.0.0

Release 5.0.0 introduces some major overhauls to the MSU Scripter in order to make the experience better for users.

## Updated UI

The UI has been completely redesigned to allow you to make things easier and more streamlined.

### Main Starting Window

<img width="1201" height="530" alt="image" src="https://github.com/user-attachments/assets/ed24deb2-d64d-4f2e-895d-7ee4b388cec3" />

The main starting window has been redone to have tabs for the creating new projects, opening projects, and changing settings.
The first time launching, it will default to the new project tab, which now allows you to populate additional fields before creating the MSU project than before. 
When launching the MSU Scripter after creating a project, it will default to the open project tab.

### MSU Project Window

<img width="1280" height="830" alt="image" src="https://github.com/user-attachments/assets/da70eea8-9c00-4034-8b92-0a25a57c625f" />

After opening a project, the list of all tracks and songs is now displayed on the left. 
Using this left panel, you can select the track you want to view, add songs to tracks, and even move songs around between different tracks.
You can easily search for specific tracks as well as customize the view to show additional icons to easily see the status of the project.

### Basic vs Advanced Song Views

<img width="2048" height="798" alt="image" src="https://github.com/user-attachments/assets/ce3e7ed8-a31e-4f7b-afcd-c164e4a4570b" />

When editing the details of a song, you can choose to either use the basic view with integrated PyMusicLooper that will only show the most commonly edited fields,
or you can use the advanced view which has all MsuPcm++ fields accessible. The advanced view has a panel similar to the track panel that allows you to add, copy,
and move sub tracks and sub channels easily. By default the MSU Scripter will ask each time which view you want to use, but you can select a default view if desired.

The buttons for playing the song and the audio controls are now always accessible at the bottom when viewing a song as well.

## Dependency Installation

<img width="557" height="260" alt="image" src="https://github.com/user-attachments/assets/ba8ef95d-0b0b-4b95-857d-0739cf847f7b" />

A common issue people have ran into has been getting some of the dependencies installed such as PyMusicLooper and the YouTube video creation application.
In order to help alleviate that, the MSU Scripter when first launching will check for dependencies and offer to install them for you. This will install
portable versions of Python and ffmpeg. If you'd like to avoid the extra space, you can still install the dependencies manually by following the
install documentation.

## Better Linux Support

Previously the Linux version of the MSU Scripter was limited in functionality. You were unable to jump to to specific parts of songs while playing them,
it did not provide any warnings regarding the sample rate, and you had to manually install dotnet to get it to run. Going forward the Linux version is
now being released as an AppImage file, so dotnet will no longer be a required pre-requisite to run the MSU Scripter.

The AppImage file has been tested onto Linux Mint 21 (based on Ubuntu 22.04), Linux Mint Debian Edition 6 (based on Debian Bookworm), EndeavourOS 
(based on Arch), and Fedora. When first starting, the application will offer to create a Desktop file to add it to your desktop environment's menu.

## Miscellaneous Changes and Fixes

- Pressing space bar after clicking the button to play a song will now pause playing songs.
- An additional track list format has been added. You can select "album - song (artist)", "song by artist (album)", and the "table" formats.
- Fixed an issue where packaging MSUs into a zip file was adding in files that were no longer selected to be added.
- Dither has been added as a per track option. If this is enabled, you will no longer be able to generate a tracks.json to send to other people to generate the MSU.
- Fixed an issue where the file inputs would allow you to type into the them.
- For non-looped tracks, the audio player will add a small pause before replaying from the beginning.
- Fixed an issue where clicking prev in the PyMusicLooper panel would prevent you from clicking next again.
- Fixed a crash that would occur when running PyMusicLooper and the starting samples would filter out all results.
- Fixed an issue where pausing, moving the play tracker location, and resuming play would play a few incorrect samples before playing from the correct location.
- Lowered the memory footprint that used to occur when changing tracks/songs (fixed by UI rewrite)
- Fixed an issue where sometimes you would scroll accidentally down the page after entering values (fixed by UI redesign)
- The MSU scripter sould now auto set itself as the default application for .msup (MSU Scripter Project) files
