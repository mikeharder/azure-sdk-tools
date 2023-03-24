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

            return Task.CompletedTask;
        }

        private static async Task Run(Options options)
        {
            Console.WriteLine("Hello World!");
            await Task.CompletedTask;
        }
    }
}
