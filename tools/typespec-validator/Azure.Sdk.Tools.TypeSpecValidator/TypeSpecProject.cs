using System.Collections.Generic;
using System.IO;

namespace Azure.Sdk.Tools.TypeSpecValidator
{
    internal class TypeSpecProject
    {
        public static IEnumerable<TypeSpecProject> EnumerateProjects(string path)
        {
            var tspconfigs = new DirectoryInfo(path).EnumerateFiles("tspconfig.yaml", SearchOption.AllDirectories);
            foreach (var tspconfig in tspconfigs)
            {
                yield return new TypeSpecProject(tspconfig.Directory.FullName);
            }
        }

        public string Path { get; private set; }

        public TypeSpecProject(string path)
        {
            Path = path;
        }
    }
}
