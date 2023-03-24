using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Azure.Sdk.Tools.TypeSpecValidator
{
    internal class SwaggerFile
    {
        public static IEnumerable<SwaggerFile> EnumerateSwaggerFilesGeneratedFromTypeSpec(string path)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

            matcher.AddInclude("**/*.json");

            matcher.AddExclude("**/quickstart-templates/*.json");
            matcher.AddExclude("**/schema/*.json");
            matcher.AddExclude("**/ scenarios/**/*.json");
            matcher.AddExclude("**/examples/**/*.json");
            matcher.AddExclude("**/package.json");
            matcher.AddExclude("**/package-lock.json");
            // TODO: Remove or rename to typespec?
            matcher.AddExclude("**/cadl/**/*.json");

            foreach (var f in matcher.GetResultsInFullPath(path))
            {
                var json = JsonNode.Parse(File.ReadAllText(f));
                if (json["info"]?["x-typespec-generated"] != null)
                {
                    yield return new SwaggerFile(f, json);
                }
            }
        }

        public string Path { get; private set; }
        public JsonNode Json { get; private set; }

        public SwaggerFile(string path, JsonNode json)
        {
            Path = path;
            Json = json;
        }
    }
}
