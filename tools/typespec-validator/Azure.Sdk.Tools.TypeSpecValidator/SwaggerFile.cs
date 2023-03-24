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
        public static IEnumerable<SwaggerFile> EnumerateSwaggerFiles(string path)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);

            matcher.AddInclude("**/*.json");

            matcher.AddExclude("**/quickstart-templates/*.json");
            matcher.AddExclude("**/schema/*.json");
            matcher.AddExclude("**/scenarios/**/*.json");
            matcher.AddExclude("**/examples/**/*.json");
            matcher.AddExclude("**/package.json");
            matcher.AddExclude("**/package-lock.json");
            // TODO: Remove or rename to typespec?
            matcher.AddExclude("**/cadl/**/*.json");

            return matcher.GetResultsInFullPath(path).Select(f => new SwaggerFile(f));
        }

        public bool GeneratedFromTypeSpec => (Json["info"]?["x-typespec-generated"] != null);

        private Lazy<JsonNode> _json;
        public JsonNode Json => _json.Value;

        public string Path { get; private set; }

        public SwaggerFile(string path)
        {
            Path = path;
            _json = new Lazy<JsonNode>(() => JsonNode.Parse(File.ReadAllText(path)));
        }
    }
}
