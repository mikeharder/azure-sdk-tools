using System;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace Azure.Sdk.Tools.TypeSpecValidator
{
    public static class Program
    {
        public class Options
        {
            [Value(0, MetaName = "Path", Required = true, HelpText = "Validates TypeSpec projects under this path.")]
            public string Path { get; set; }

            [Option('d', "git-diff", HelpText = "Only validate TypeSpec projects changes relative to a git commit (or branch).")]
            public string GitDiff { get; set; }
        }

        public static async Task Main(string[] args)
        {
            var parser = new CommandLine.Parser(settings =>
            {
                settings.CaseSensitive = false;
                settings.CaseInsensitiveEnumValues = true;
                settings.HelpWriter = null;
            });

            var parserResult = parser.ParseArguments<Options>(args);

            await parserResult.MapResult(
                (Options options) => Run(options),
                errors => DisplayHelp(parserResult)
            );
        }

        static Task DisplayHelp<T>(ParserResult<T> result)
        {
            var helpText = HelpText.AutoBuild(result, settings =>
            {
                settings.AddEnumValuesToHelpText = true;
                return settings;
            });

            Console.Error.WriteLine(helpText);

            Console.Error.WriteLine("Examples");
            Console.Error.WriteLine(@"> typespec-validator specification\contosowidgetmanager");
            Console.Error.WriteLine();
            Console.Error.WriteLine(@"> typespec-validator --git-diff main specification\contosowidgetmanager");

            return Task.CompletedTask;
        }

        private static async Task Run(Options options)
        {
            // 1. Find all swaggers generated from TypeSpec
            // 2. Find all TypeSpec projects
            // 3. Filter based on git-diff (if specified)
            // 4. Foreach SwaggerFile, run all SwaggerFileRules, passing in all TypeSpecProjects
            // 5. Foreach TypeSpecProject, run all TypeSpecProjectRules, passing in all SwaggerFiles

            var swaggers = SwaggerFile.EnumerateSwaggerFilesGeneratedFromTypeSpec(options.Path);
            Console.WriteLine("Swagger Files Generated from TypeSpec");
            foreach (var s in swaggers)
            {
                Console.WriteLine($"- {s.Path}");
            }

            Console.WriteLine();

            var projects = TypeSpecProject.EnumerateProjects(options.Path);
            Console.WriteLine("TypeSpec Projects");
            foreach (var p in projects)
            {
                Console.WriteLine($"- {p.Path}");
            }

            await Task.CompletedTask;
        }
    }
}
