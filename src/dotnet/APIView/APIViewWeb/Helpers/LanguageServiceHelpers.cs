using System;
using System.Collections.Generic;
using System.Linq;

namespace APIViewWeb.Helpers
{
    public class LanguageServiceHelpers
    {
        public static string[] SupportedLanguages = new string[] { "C", "C#", "C++", "Go", "Java", "JavaScript", "Json", "Kotlin", "Python", "Rust", "Swagger", "Swift", "TypeSpec", "Xml" };

        public static IEnumerable<string> MapLanguageAliases(IEnumerable<string> languages)
        {
            HashSet<string> result = new HashSet<string>();

            foreach (var language in languages)
            {
                if (language.Equals("TypeSpec") || language.Equals("Cadl"))
                {
                    result.Add("Cadl");
                    result.Add("TypeSpec");
                }
                result.Add(language);
            }

            return result.ToList();
        }

        public static string MapLanguageAlias(string language)
        {
            if (language.Equals("net", StringComparison.OrdinalIgnoreCase) || language.Equals(".NET", StringComparison.OrdinalIgnoreCase))
                return "C#";

            if (language.Equals("cpp", StringComparison.OrdinalIgnoreCase))
                return "C++";

            if (language.Equals("js", StringComparison.OrdinalIgnoreCase))
                return "JavaScript";

            if (language.Equals("Cadl", StringComparison.OrdinalIgnoreCase))
                return "TypeSpec";

            return SupportedLanguages.Where(lang => lang.Equals(language, StringComparison.OrdinalIgnoreCase)).FirstOrDefault() ?? language;
        }

        public static string GetLanguageAliasForCopilotService(string language)
        {
            switch (language)
            {
                case "C":
                    return "clang";
                case "C#":
                    return "dotnet";
                case "C++":
                    return "cpp";
                case "JavaScript":
                    return "typescript";
                case "Swift":
                    return "ios";
                case "Go":
                    return "golang";
                default:
                    return language.ToLower();
            }
        }

        public static string GetLanguageFromRepoName(string repoName)
        {
            var result = String.Empty;

            if (repoName.EndsWith("-net"))
                result = "C#";
            if (repoName.EndsWith("-c"))
                result = "C";
            if (repoName.EndsWith("-cpp"))
                result = "C++";
            if (repoName.EndsWith("-go"))
                result = "Go";
            if (repoName.EndsWith("-java"))
                result = "Java";
            if (repoName.EndsWith("-js"))
                result = "JavaScript";
            if (repoName.EndsWith("-python"))
                result = "Python";
            if (repoName.EndsWith("-ios"))
                result = "Swift";
            if(repoName.EndsWith("-rust"))
                result = "Rust";

            return result;
        }

        public static LanguageService GetLanguageService(string language, IEnumerable<LanguageService> languageServices)
        {
            return languageServices.FirstOrDefault(service => service.Name.Equals(language, StringComparison.OrdinalIgnoreCase));
        }
    }
}
