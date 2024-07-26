namespace MSUScripter.Models;

public class GeneratePcmFileResponse(bool successful, bool generatedPcmFile, string? message, string? outputPath)
{
    public bool Successful => successful;
    public bool GeneratedPcmFile => generatedPcmFile;
    public string? Message => message;
    public string? OutputPath => outputPath;
}