using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
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

            return matcher.GetResultsInFullPath(path)
                .Where(f => JsonNode.Parse(File.ReadAllText(f))["info"]?["x-typespec-generated"] != null)
                .Select(f => new SwaggerFile(f));
        }

        public string Path { get; private set; }

        public SwaggerFile(string path)
        {
            Path = path;
        }
    }
}
