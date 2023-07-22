﻿//----------------------
// <auto-generated>
//     Generated using the NJsonSchema v10.9.0.0 (Newtonsoft.Json v9.0.0.0) (http://NJsonSchema.org)
// </auto-generated>
//----------------------


namespace MSUScripter.Configs
{
    #pragma warning disable // Disable all warnings

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Track : Track_base
    {
        [Newtonsoft.Json.JsonProperty("track_number", Required = Newtonsoft.Json.Required.Always)]
        public int Track_number { get; set; }

        [Newtonsoft.Json.JsonProperty("title", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Title { get; set; }

        /// <summary>
        /// Files which will be mixed together to form the input to the parent track
        /// </summary>
        [Newtonsoft.Json.JsonProperty("sub_channels", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.MinLength(1)]
        public System.Collections.Generic.ICollection<Sub_channel> Sub_channels { get; set; }

        /// <summary>
        /// Files which will be concatenated together to form the input to the parent track
        /// </summary>
        [Newtonsoft.Json.JsonProperty("sub_tracks", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.MinLength(1)]
        public System.Collections.Generic.ICollection<Sub_track> Sub_tracks { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Sub_track : Track_base
    {
        /// <summary>
        /// Files which will be mixed together to form the input to the parent sub-track
        /// </summary>
        [Newtonsoft.Json.JsonProperty("sub_channels", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.MinLength(1)]
        public System.Collections.Generic.ICollection<Sub_channel> Sub_channels { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Sub_channel : Track_base
    {
        /// <summary>
        /// Files which will be concatenated together to form the input to the parent sub-channel
        /// </summary>
        [Newtonsoft.Json.JsonProperty("sub_tracks", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.MinLength(1)]
        public System.Collections.Generic.ICollection<Sub_track> Sub_tracks { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Track_base
    {
        /// <summary>
        /// The file to be used as the input for this track/sub-track/sub-channel
        /// </summary>
        [Newtonsoft.Json.JsonProperty("file", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string File { get; set; }

        /// <summary>
        /// The final output filename, overrides output_prefix
        /// </summary>
        [Newtonsoft.Json.JsonProperty("output", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Output { get; set; }

        /// <summary>
        /// The loop point of the current track, relative to this track/sub-track/sub-channel, in samples
        /// </summary>
        [Newtonsoft.Json.JsonProperty("loop", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Loop { get; set; }

        /// <summary>
        /// Trim the start of the current track at the specified sample
        /// </summary>
        [Newtonsoft.Json.JsonProperty("trim_start", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Trim_start { get; set; }

        /// <summary>
        /// Trim the end of the current track at the specified sample
        /// </summary>
        [Newtonsoft.Json.JsonProperty("trim_end", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Trim_end { get; set; }

        /// <summary>
        /// Apply a fade in effect to the current track lasting a specified number of samples
        /// </summary>
        [Newtonsoft.Json.JsonProperty("fade_in", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Fade_in { get; set; }

        /// <summary>
        /// Apply a fade out effect to the current track lasting a specified number of samples
        /// </summary>
        [Newtonsoft.Json.JsonProperty("fade_out", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Fade_out { get; set; }

        /// <summary>
        /// Apply a cross fade effect from the end of the current track to its loop point lasting a specified number of samples
        /// </summary>
        [Newtonsoft.Json.JsonProperty("cross_fade", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Cross_fade { get; set; }

        /// <summary>
        /// Pad the beginning of the current track with a specified number of silent samples
        /// </summary>
        [Newtonsoft.Json.JsonProperty("pad_start", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Pad_start { get; set; }

        /// <summary>
        /// Pad the end of the current track with a specified number of silent samples
        /// </summary>
        [Newtonsoft.Json.JsonProperty("pad_end", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Pad_end { get; set; }

        /// <summary>
        /// Alter the tempo of the current track by a specified ratio
        /// </summary>
        [Newtonsoft.Json.JsonProperty("tempo", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double Tempo { get; set; }

        /// <summary>
        /// Normalize the current track to the specified RMS level, overrides the global normalization value
        /// </summary>
        [Newtonsoft.Json.JsonProperty("normalization", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double Normalization { get; set; }

        /// <summary>
        /// Apply dynamic range compression to the current track
        /// </summary>
        [Newtonsoft.Json.JsonProperty("compression", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Compression { get; set; }

        [Newtonsoft.Json.JsonProperty("use_option", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Use_option { get; set; }

        [Newtonsoft.Json.JsonProperty("options", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        [System.ComponentModel.DataAnnotations.MinLength(1)]
        public System.Collections.Generic.ICollection<Options> Options { get; set; }



        private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Track_option : Track
    {
        [Newtonsoft.Json.JsonProperty("option", Required = Newtonsoft.Json.Required.Always)]
        public int Option { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Sub_track_option : Sub_track
    {
        [Newtonsoft.Json.JsonProperty("option", Required = Newtonsoft.Json.Required.Always)]
        public int Option { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Sub_channel_option : Sub_channel
    {
        [Newtonsoft.Json.JsonProperty("option", Required = Newtonsoft.Json.Required.Always)]
        public int Option { get; set; }


    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Base_option : Track_base
    {
        [Newtonsoft.Json.JsonProperty("option", Required = Newtonsoft.Json.Required.Always)]
        public int Option { get; set; }


    }

    /// <summary>
    /// Configuration file schema for msupcm++
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class MsuPcmPlusPlusConfig
    {
        /// <summary>
        /// The SNES game this audio pack is intended for
        /// </summary>
        [Newtonsoft.Json.JsonProperty("game", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Game { get; set; }

        /// <summary>
        /// The name of this audio pack
        /// </summary>
        [Newtonsoft.Json.JsonProperty("pack", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Pack { get; set; }

        /// <summary>
        /// The original artist of the audio files used in this pack
        /// </summary>
        [Newtonsoft.Json.JsonProperty("artist", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Artist { get; set; }

        /// <summary>
        /// The location where the original audio files can be found
        /// </summary>
        [Newtonsoft.Json.JsonProperty("url", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public System.Uri Url { get; set; }

        /// <summary>
        /// The prefix used for the final output files, followed by the track number
        /// </summary>
        [Newtonsoft.Json.JsonProperty("output_prefix", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public string Output_prefix { get; set; }

        /// <summary>
        /// The default RMS normalization level, in dBFS, to be applied to the entire pack
        /// </summary>
        [Newtonsoft.Json.JsonProperty("normalization", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public double Normalization { get; set; }

        /// <summary>
        /// Whether or not to apply audio dither to the final output
        /// </summary>
        [Newtonsoft.Json.JsonProperty("dither", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Dither { get; set; }

        /// <summary>
        /// Sets the verbosity level of the application during processing
        /// </summary>
        [Newtonsoft.Json.JsonProperty("verbosity", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Verbosity { get; set; }

        /// <summary>
        /// Whether or not to keep temporary files generated during processing
        /// </summary>
        [Newtonsoft.Json.JsonProperty("keep_temps", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool Keep_temps { get; set; }

        /// <summary>
        /// Any track number less than this will not be processed
        /// </summary>
        [Newtonsoft.Json.JsonProperty("first_track", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int First_track { get; set; }

        /// <summary>
        /// Any track number greater than this will not be processed
        /// </summary>
        [Newtonsoft.Json.JsonProperty("last_track", Required = Newtonsoft.Json.Required.DisallowNull, NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
        public int Last_track { get; set; }

        [Newtonsoft.Json.JsonProperty("tracks", Required = Newtonsoft.Json.Required.Always)]
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(1)]
        public System.Collections.Generic.ICollection<Track> Tracks { get; set; } = new System.Collections.ObjectModel.Collection<Track>();



        private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }

    [System.CodeDom.Compiler.GeneratedCode("NJsonSchema", "10.9.0.0 (Newtonsoft.Json v9.0.0.0)")]
    public partial class Options
    {


        private System.Collections.Generic.IDictionary<string, object> _additionalProperties;

        [Newtonsoft.Json.JsonExtensionData]
        public System.Collections.Generic.IDictionary<string, object> AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new System.Collections.Generic.Dictionary<string, object>()); }
            set { _additionalProperties = value; }
        }

    }
}