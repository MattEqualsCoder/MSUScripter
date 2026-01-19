// ReSharper disable ReplaceAutoPropertyWithComputedProperty

using System;

namespace MSUScripter.Text;

public class ApplicationText
{
    public static ApplicationText CurrentLanguageText { get; set; } = new();
    
    public static event EventHandler<ApplicationText>? LanguageChanged;

    public static void SetLanguage(ApplicationText newLanguage)
    {
        CurrentLanguageText = newLanguage;
        LanguageChanged?.Invoke(null, newLanguage);
    }

    public string MainWindowApplicationName { get; } = "MSU Scripter";
    public string MainWindowNewProject { get; } = "New Project";
    public string MainWindowOpenProject { get; } = "Open Project";
    public string MainWindowSettings { get; } = "Settings";
    public string MainWindowAbout { get; } = "About";
    
    public string ProjectWindowMainMenu { get; } = "Main Menu";
    public string ProjectWindowSaveProject { get; } = "Save Project";
    public string ProjectWindowOpenMsuFolder { get; } = "Open MSU Folder";
    public string ProjectWindowAnalyzeAudio { get; } = "Analyze Audio...";
    public string ProjectWindowExportProject { get; } = "Generate MSU...";
    public string ProjectWindowFilterOnlyTracksMissingSongs { get; } = "Show only tracks with no added songs";
    public string ProjectWindowFilterOnlyIncomplete { get; } = "Show only incomplete songs";
    public string ProjectWindowFilterOnlyMissingAudio { get; } = "Show only songs missing audio files";
    public string ProjectWindowFilterOnlyCopyrightUntested { get; } = "Show only songs untested for copyright strikes";
    public string ProjectWindowDisplayIsCompleteIcon { get; } = "Display song completed flag icon";
    public string ProjectWindowDisplayHasSongIcon { get; } = "Display has audio files icon";
    public string ProjectWindowDisplayCheckCopyrightIcon { get; } = "Display add to copyright test video icon";
    public string ProjectWindowDisplayCopyrightSafeIcon { get; } = "Display copyright strike status icon";

    public string MsuBasicInfoMsuDetailsHeader { get; } = "MSU Details";
    public string MsuBasicInfoGenerationSettingsHeader { get; } = "Generation Settings";
    public string MsuBasicInfoPackNameLabel { get; } = "Pack Name";
    public string MsuBasicInfoPackNameToolTip { get; } = "A friendly display name of the MSU pack. Added to the MSU Randomizer YAML file.";
    public string MsuBasicInfoPackCreatorLabel { get; } = "Pack Creator";
    public string MsuBasicInfoPackCreatorToolTip { get; } = "Who created the MSU pack. Added to the MSU Randomizer YAML file.";
    public string MsuBasicInfoMsuTypeLabel { get; } = "MSU Type";
    public string MsuBasicInfoMsuTypeToolTip { get; } = "The randomizer or game you are making to use this MSU with.";
    public string MsuBasicInfoMsuPathLabel { get; } = "MSU Path";
    public string MsuBasicInfoMsuPathToolTip { get; } = "The path the of the .msu file to generate and create the MSU Randomizer YAML file for.";
    public string MsuBasicInfoMsuProjectPathLabel { get; } = "MSU Project Path";
    public string MsuBasicInfoMsuProjectPathToolTip { get; } = "The path the of the .msup project file used by the MSU Scripter application.";

    public string MsuBasicInfoImportJsonLabel { get; } = "Import MsuPcm++ JSON File (Optional)";
    public string MsuBasicInfoImportJsonToolTip { get; } = "If you are importing a previously created MSU, you can select the MsuPcm++ tracks JSON to automatically import some of the details from the JSON file.";
    public string MsuBasicInfoImportWorkingDirectoryLabel { get; } = "MsuPcm++ Working Directory (Optional)";
    public string MsuBasicInfoImportWorkingDirectoryToolTip { get; } = "If you are importing a previously created MSU and MsuPcm++ tracks JSON file and have relative file paths in the JSON file, you can specify the folder you ran MsuPcm++ from to determine the full file paths.";
    public string MsuBasicInfoPackVersionLabel { get; } = "Pack Version";
    public string MsuBasicInfoPackVersionToolTip { get; } = "Current version of the track. Used by the MSU Randomizer to recache MSU data.";
    public string MsuBasicInfoDefaultArtistLabel { get; } = "Default Artist";
    public string MsuBasicInfoDefaultArtistToolTip { get; } = "Name of the artist to use for songs where an artist is not entered.";
    public string MsuBasicInfoDefaultAlbumLabel { get; } = "Default Album";
    public string MsuBasicInfoDefaultAlbumToolTip { get; } = "Name of the album to use for songs where an album is not entered.";
    public string MsuBasicInfoDefaultUrlLabel { get; } = "Default URL";
    public string MsuBasicInfoDefaultUrlToolTip { get; } = "URL to retrieve the album or support the artist for songs where a URL is not entered.";
    public string MsuBasicInfoIsMsuPcmPackLabel { get; } = "Generate PCM Files via MsuPcm++";
    public string MsuBasicInfoIsMsuPcmPackToolTip { get; } = "Add additional fields to enter information to use for generating the PCM files via the MsuPcm++ application.";
    public string MsuBasicInfoMsuPcmNormalizationLabel { get; } = "Default Normalization";
    public string MsuBasicInfoMsuPcmNormalizationToolTip { get; } = "The default RMS normalization level, in dBFS (decibels), to be applied to the entire pack. Should be a negative value, typically less than -10 as values approaching 0 will likely cause audio clipping.";
    public string MsuBasicInfoMsuPcmDitherLabel { get; } = "Dither";
    public string MsuBasicInfoMsuPcmDitherToolTip { get; } = "Whether or not to apply audio dither to the final output";
    public string MsuBasicInfoWriteYamlLabel { get; } = "Create YAML File";
    public string MsuBasicInfoWriteYamlToolTip { get; } = "Generate a YAML with track information that can be read by a user, the MSU Randomizer application, or some other application that supports it.";
    public string MsuBasicInfoWriteTrackListLabel { get; } = "Track List Text File Type";
    public string MsuBasicInfoWriteTrackListToolTip { get; } = "Structure of the text file that lists basic information about the tracks and artists.";
    public string MsuBasicInfoIncludeJsonLabel { get; } = "Bundle MsuPcm++ tracks.json File";
    public string MsuBasicInfoIncludeJsonToolTip { get; } = "Generate a full tracks.json file that can be used by others to re-generate the MSU provided they have the correct input files.";
    public string MsuBasicInfoWriteAltSwapperLabel { get; } = "Create Alt Track Swapper Script";
    public string MsuBasicInfoWriteAltSwapperToolTip { get; } = "Generate a PowerShell script to swap between primary and the first alt tracks, if any alt tracks are available.";
    public string MsuBasicInfoCreateSplitSmz3MsuCheckbox { get; } = "";
    public string MsuBasicInfoCreateSplitSmz3Label { get; } = "Create Separate ALttP & SM MSUs";
    public string MsuBasicInfoCreateSplitSmz3ToolTip { get; } = "Create Separate A Link to the Past and Super Metroid MSUs and Create PowerShell Script to Split MSU.";
    public string MsuBasicInfoZeldaMsuPathLabel { get; } = "A Link to the Past MSU Path";
    public string MsuBasicInfoZeldaMsuPathToolTip { get; } = "The path to the separate ALttP MSU that users can split the SMZ3 MSU into.";
    public string MsuBasicInfoMetroidMsuPathLabel { get; } = "Super Metroid MSU Path";
    public string MsuBasicInfoMetroidMsuPathToolTip { get; } = "The path to the separate Super Metroid MSU that users can split the SMZ3 MSU into.";
    
    public string InputAudioFileLabel { get; } = "Input Audio File";
    public string InputAudioFileToolTip { get; } = "Input audio file used by MsuPcm++ to generate the PCM file.";
    public string InputAudioFileFilter { get; } = "Supported audio files:*.wav,*.mp3,*.flac,*.ogg;All files:*.*";

    public string OutputAudioFileLabel { get; } = "Output Audio File";
    public string OutputAudioFileToolTip { get; } = "Output PCM file generated by MsuPcm++. Can only be modified for alt songs.";
    public string OutputAudioFileFilter { get; } = "PCM audio files:*.pcm";

    public string MetadataSongNameLabel { get; } = "Song Name";
    public string MetadataSongNameToolTip { get; } = "Song name used in the track list and the MSU Randomizer YAML file. The MSU Scripter will attempt to populate this automatically when you select an input audio file.";
    public string MetadataArtistNameLabel { get; } = "Artist Name";
    public string MetadataArtistNameToolTip { get; } = "Artist name used in the track list and the MSU Randomizer YAML file. The MSU Scripter will attempt to populate this automatically when you select an input audio file.";
    public string MetadataAlbumNameLabel { get; } = "Album Name";
    public string MetadataAlbumNameToolTip { get; } = "Album name used in the track list and the MSU Randomizer YAML file. The MSU Scripter will attempt to populate this automatically when you select an input audio file.";
    public string MetadataUrlLabel { get; } = "Url";
    public string MetadataUrlToolTip { get; } = "Url to retrieve the song or view the artist's library. Added to the YAML file.";

    public string MsuPcmHeader { get; } = "MsuPcm++ Details";
    public string MsuPcmTrimStartLabel { get; } = "Trim Start";
    public string MsuPcmTrimStartToolTip { get; } = "Trim the start of the current track at the specified sample.";
    public string MsuPcmTrimEndLabel { get; } = "Trim End";
    public string MsuPcmTrimEndToolTip { get; } = "Trim the end of the current track at the specified sample.";
    public string MsuPcmLoopLabel { get; } = "Loop Point";
    public string MsuPcmLoopToolTip { get; } = "The loop point of the current track, relative to this track/sub-track/sub-channel, in samples.";
    public string MsuPcmNormalizationLabel { get; } = "Normalization";
    public string MsuPcmNormalizationToolTip { get; } = "Normalize the current track to the specified RMS level in dBFS (decibels). The value overrides the default global normalization value. Should be a negative value, typically less than -10 as values approaching 0 will likely cause audio clipping.";
    public string MsuPcmFadeInLabel { get; } = "Fade In";
    public string MsuPcmFadeInToolTip { get; } = "Apply a fade in effect to the current track lasting a specified number of samples.";
    public string MsuPcmFadeOutLabel { get; } = "Fade Out";
    public string MsuPcmFadeOutToolTip { get; } = "Apply a fade out effect to the current track lasting a specified number of samples.";
    public string MsuPcmCrossFadeLabel { get; } = "Cross Fade";
    public string MsuPcmCrossFadeToolTip { get; } = "Apply a cross fade effect from the end of the current track to its loop point lasting a specified number of samples.";
    public string MsuPcmPaddingStartLabel { get; } = "Padding Start";
    public string MsuPcmPaddingStartToolTip { get; } = "Pad the beginning of the current track with a specified number of silent samples.";
    public string MsuPcmPaddingEndLabel { get; } = "Padding End";
    public string MsuPcmPaddingEndToolTip { get; } = "Pad the end of the current track with a specified number of silent samples.";
    public string MsuPcmTempoLabel { get; } = "Tempo";
    public string MsuPcmTempoToolTip { get; } = "Alter the tempo of the current track by a specified ratio.";
    public string MsuPcmCompressionLabel { get; } = "Compression";
    public string MsuPcmCompressionToolTip { get; } = "Apply dynamic range compression to the current track. Helps to minimize very loud and very quiet portions of the track.";
    public string PostGenerationVolumeLabel { get; } = "Post Generation Volume Modifier";
    public string PostGenerationVolumeToolTip { get; } = "Alter the volume after MsuPcm++ has generated the PCM file. If set, anyone generating the PCM themselves with MsuPcm++ via the tracks json file will not see these changes.";
    public string MsuPcmDitherLabel { get; } = "Dither";
    public string MsuPcmDitherToolTip { get; } = "Whether or not to apply audio dither to the final output. If set, overrides the default value.";

    
    public string CheckCopyrightCheckedText { get; } = "Add to copyright test video";
    public string CheckCopyrightUncheckedText { get; } = "Do not add to copyright test video";
    public string CheckCopyrightToolTipText { get; } = "If this track will be added to the video to upload to YouTube to check for potential copyright strikes.";
    public string IsCopyrightSafeCheckedText { get; } = "Verified to be safe from copyright strikes";
    public string IsCopyrightSafeUncheckedText { get; } = "Verified to not be safe from copyright strikes";
    public string IsCopyrightSafeNullText { get; } = "Not tested for copyright strike safety";
    public string IsCopyrightSafeToolTipText { get; } = "If this song has been tested and is known to be safe from potential copyright strikes.";

    public string SongPanelBasicHeader { get; } = "Song Details";
    public string SongPanelAdvancedModeCheckBox { get; } = "Advanced View";
    public string SongPanelBasicModeCheckBox { get; } = "Advanced View";
    public string SongPanelAdvancedModeToolTip { get; } = "Toggle advanced mode where you can set all msupcm++ settings, including adding subtracks and subchannels.";
    public string SongPanelBasicMetadataHeader { get; } = "Song Metadata Details";
    
    public string PyMusicLooperHeader { get; } = "PyMusicLooper";
    public string PyMusicLooperMinDurationMultiplierLabel { get; } = "Min Length Mult.";
    public string PyMusicLooperMinDurationMultiplierToolTip { get; } = "Minimum length/duration multiplier, or the minimum percentage of the source audio's length that the duration of the looped portion should last. For example, a multiplier of 0.25 of a 60 second audio file would mean the looped portion would need to be at least 15 seconds long.";
    public string PyMusicLooperDurationLimitInSecondsLabel { get; } = "Duration Limit in Seconds";
    public string PyMusicLooperDurationLimitInSecondsToolTip { get; } = "Minimum and maximum lengths of the looped audio in seconds";
    public string PyMusicLooperApproximateTimesLabel { get; } = "Approximate Loop Time in Seconds";
    public string PyMusicLooperApproximateTimesToolTip { get; } = "Approximate times for the start and stop times of the looped audio within 2 seconds.";
    public string PyMusicLooperFilterSamplesLabel { get; } = "Filter Samples";
    public string PyMusicLooperFilterSamplesToolTip { get; } = "Sample values to filter out results. If entered, the trim start value will be used as the starting sample filter.";
    public string PyMusicLooperRunButton { get; } = "Run";
    public string PyMusicLooperAutoRunCheckBox { get; } = "Automatically Run PyMusicLooper After Selecting File";
    public string PyMusicLooperStopButton { get; } = "Stop PyMusicLooper";
    public string PyMusicLooperPrevButton { get; } = "Previous Page";
    public string PyMusicLooperNextButton { get; } = "Next Page";
    public string PyMusicLooperGridLoopStartHeader { get; } = "Loop Start Sample";
    public string PyMusicLooperGridLoopEndHeader { get; } = "Loop End Sample";
    public string PyMusicLooperGridScoreHeader { get; } = "Score";
    public string PyMusicLooperGridLoopDurationHeader { get; } = "Loop Duration";
    public string PyMusicLooperGridStatusHeader { get; } = "Status";
    public string PyMusicLooperOpenWindowButton { get; } = "Run PyMusicLooper Application";

    public string MenuItemCopyToClipboard { get; } = "Copy to Clipboard";
    public string MenuItemPasteFromClipboard { get; } = "Paste from Clipboard";
    public string MenuItemDuplicateMsuPcmDetails { get; } = "Duplicate MsuPcm++ Details";
    public string MenuItemDeleteMsuPcmDetails { get; } = "Delete MsuPcm++ Details";
    public string MenuItemNewProject { get; } = "_New Project...";
    public string MenuItemOpenProject { get; } = "_Open Project";
    public string MenuItemOpenFromFile { get; } = "From File...";
    public string MenuItemSaveProject { get; } = "_Save Project";
    public string MenuItemCloseProject { get; } = "_Close Project";
    public string MenuItemSettings { get; } = "Se_ttings...";
    public string MenuItemExitApp { get; } = "E_xit MSU Scripter";
    public string MenuItemDuplicateSongDetails { get; } = "Duplicate Song Details";
    public string MenuItemDeleteSongDetails { get; } = "Delete Song Details";
    
    public string MenuItemGenerateMsuLabel { get; } = "Generate MSU...";
    public string MenuItemGenerateMsuToolTip { get; } = "Generate all PCM files if needed along with all other files for the MSU and (optionally) package the files into a zip file.";
    public string MenuItemCreateYouTubeVideoDetailsLabel { get; } = "Create Copyright Test YouTube Video...";
    public string MenuItemCreateYouTubeVideoDetailsToolTip { get; } = "Create a video of all of the marked songs to compile into a single video that you can upload to YouTube to see if it'll get copyright strikes.";
    public string MenuItemCreateYamlLabel { get; } = "Create MSU YAML File";
    public string MenuItemCreateYamlToolTip { get; } = "Create a YAML file with all track details that can be read by users or applications like the MSU Randomizer. Must be re-generated when you change any track metadata or if you re-generate PCM files where you have alt tracks.";
    public string MenuItemCreateTrackListLabel { get; } = "Create Track List File";
    public string MenuItemCreateTrackListToolTip { get; } = "Create a track list text file with all track details that can be read by users.";
    public string MenuItemCreateSwapScriptsLabel { get; } = "Create Track Swapping Script File(s)";
    public string MenuItemCreateSwapScriptsToolTip { get; } = "Create a .bat script file for swapping between primary tracks and, if applicable, a file for switching between SMZ3 and split SM and ALttP MSUs.";
    public string MenuItemCreateTracksJsonLabel { get; } = "Create MsuPcm++ Tracks File";
    public string MenuItemCreateTracksJsonToolTip { get; } = "Create the MsuPcm++ tracks.json file that can be sent to others to generate the Msu themselves.";
    public string MsuGenerationSettingsGenerateButton { get; } = "Generate";
    public string MsuGenerationSettingsCancelButton { get; } = "Cancel";

    public string GenericErrorTitle { get; } =
        "Unexpected Error";
    public string GenericError { get; } =
        "There was an unexpected error, please try again. If the problem persists, please post an Issue on GitHub.";

}