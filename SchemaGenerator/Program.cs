// See https://aka.ms/new-console-template for more information

using MSUScripter.Configs;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

var outputPath = GetOutputPath();
if (!Directory.Exists(outputPath))
{
    Directory.CreateDirectory(outputPath);
}

var settings = new NewtonsoftJsonSchemaGeneratorSettings()
{
    DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.Null,
    DefaultDictionaryValueReferenceTypeNullHandling = ReferenceTypeNullHandling.Null,
    
};
var generator = new JsonSchemaGenerator(settings);
CreateSchema(typeof(MsuSongInfo));
CreateSchema(typeof(MsuSongMsuPcmInfo));

void CreateSchema(Type type)
{
    var schema = generator.Generate(type);
    var text = schema.ToJson();
    File.WriteAllText(Path.Combine(outputPath, $"{type.Name}.json"), text);
}

string GetOutputPath()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory != null && !directory.GetFiles("*.sln").Any())
    {
        directory = directory.Parent;
    }

    return Path.Combine(directory!.FullName, "Schemas");
}