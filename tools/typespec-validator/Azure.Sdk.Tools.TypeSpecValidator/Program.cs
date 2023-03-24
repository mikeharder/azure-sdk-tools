using CommandLine;
using CommandLine.Text;
using System;
using System.Threading.Tasks;

namespace Azure.Sdk.Tools.TypeSpecValidation
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
            Console.WriteLine(options.Path);
            await Task.CompletedTask;
        }
    }
}
