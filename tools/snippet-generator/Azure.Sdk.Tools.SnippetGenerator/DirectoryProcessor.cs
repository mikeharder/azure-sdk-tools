// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Azure.Sdk.Tools.SnippetGenerator
{
    public class DirectoryProcessor
    {
        private const string _snippetPrefix = "Snippet:";
        private readonly string _directory;
        private readonly Lazy<Task<List<Snippet>>> _snippets;
        private static readonly Regex _markdownOnlyRegex = new Regex(
            @"(?<indent>\s*)//@@\s*(?<line>.*)",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
        private const string _codeOnlyPattern = "/*@@*/";
        private static readonly Regex _regionRegex = new Regex(
            @"^(?<indent>\s*)(#region|#endregion)\s*(?<line>.*)",
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private UTF8Encoding _utf8EncodingWithoutBOM;
        private static string[] _snippetPreprocessorSymbols = new [] {"SNIPPET"};

        public DirectoryProcessor(string directory)
        {
            _directory = directory;
            _snippets = new Lazy<Task<List<Snippet>>>(DiscoverSnippetsAsync);
        }

        public async Task<IEnumerable<string>> ProcessAsync(IEnumerable<string> files = null)
        {
            if (files is null)
            {
                List<string> discoveredFiles = new();
                discoveredFiles.AddRange(Directory.EnumerateFiles(_directory, "*.md", SearchOption.AllDirectories));
                discoveredFiles.AddRange(Directory.EnumerateFiles(_directory, "*.cs", SearchOption.AllDirectories));

                files = discoveredFiles;
            }

            foreach (var file in files)
            {
                async ValueTask<string> SnippetProvider(string s)
                {
                    var snippets = await _snippets.Value;
                    var selectedSnippets = snippets.Where(snip => snip.Name == s).ToArray();
                    if (selectedSnippets.Length > 1)
                    {
                        throw new InvalidOperationException($"Multiple snippets with the name '{s}' defined '{_directory}'");
                    }

                    if (selectedSnippets.Length == 0)
                    {
                        throw new InvalidOperationException($"Snippet '{s}' not found in directory '{_directory}'");
                    }

                    var selectedSnippet = selectedSnippets.Single();
                    selectedSnippet.IsUsed = true;
                    Console.WriteLine($"Replaced {selectedSnippet.Name} in {file}");
                    var result = FormatSnippet(selectedSnippet.Text);
                    if (string.IsNullOrWhiteSpace(result))
                    {
                        throw new InvalidOperationException($"Snippet '{s}' is empty. Did you remember to implement the snippet code or are preprocessor conditions excluding it?");
                    }

                    return result;
                }

                var originalText = await File.ReadAllTextAsync(file);

                string text;
                switch (Path.GetExtension(file))
                {
                    case ".md":
                        text = await MarkdownProcessor.ProcessAsync(originalText, SnippetProvider);
                        break;
                    case ".cs":
                        text = await CSharpProcessor.ProcessAsync(originalText, SnippetProvider);
                        break;
                    default:
                        throw new NotSupportedException(file);
                }

                if (text != originalText)
                {
                    _utf8EncodingWithoutBOM = new UTF8Encoding(false);
                    await File.WriteAllTextAsync(file, text, _utf8EncodingWithoutBOM);
                }
            }
            var snippets = await _snippets.Value;
            var unUsedSnippets = snippets
                .Where(s => !s.IsUsed)
                .Select(s => $"{GetServiceDirName(s.FilePath)}: {s.Name}");
            return unUsedSnippets;

        }

        private string GetServiceDirName(string path)
        {
            string sdk = $"{Path.DirectorySeparatorChar}sdk{Path.DirectorySeparatorChar}";
            int start = path.IndexOf(sdk) + sdk.Length;
            int end = path.IndexOf(Path.DirectorySeparatorChar, start);
            return path.Substring(start, end - start);
        }

        private async Task<List<Snippet>> DiscoverSnippetsAsync()
        {
            var snippets = await GetSnippetsInDirectoryAsync(_directory);
            if (snippets.Count == 0)
            {
                return snippets;
            }
            Console.WriteLine($"Discovered snippets:");
            foreach (var snippet in snippets)
            {
                Console.WriteLine($" {snippet.Name} in {snippet.FilePath}");
            }

            return snippets;
        }

        private string FormatSnippet(SourceText text)
        {
            int minIndent = int.MaxValue;
            int firstLine = 0;
            var lines = text.Lines.Select(l => l.ToString()).ToArray();

            int lastLine = lines.Length - 1;

            while (firstLine < lines.Length && string.IsNullOrWhiteSpace(lines[firstLine]))
            {
                firstLine++;
            }

            while (lastLine > 0 && string.IsNullOrWhiteSpace(lines[lastLine]))
            {
                lastLine--;
            }

            for (var index = firstLine; index <= lastLine; index++)
            {
                var textLine = lines[index];

                if (string.IsNullOrWhiteSpace(textLine))
                {
                    continue;
                }

                int i;
                for (i = 0; i < textLine.Length; i++)
                {
                    if (!char.IsWhiteSpace(textLine[i])) break;
                }

                minIndent = Math.Min(minIndent, i);
            }

            var stringBuilder = new StringBuilder();
            for (var index = firstLine; index <= lastLine; index++)
            {
                var line = lines[index];
                line = string.IsNullOrWhiteSpace(line) ? string.Empty : line.Substring(minIndent);
                line = RemoveMarkdownOnlyPrefix(line);
                if (!IsCodeOnlyLine(line) && !IsRegionLine(line))
                {
                    stringBuilder.AppendLine(line);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// There are occasions where we might want some explanatory code only
        /// appearing in the markdown document.  Comments like
        ///
        ///     //@@ bool onlyInMarkDown = true;
        ///
        /// will have the "//@@" stripped off by this method when generating
        /// our markdown text.
        /// </summary>
        /// <param name="line">The line of text.</param>
        /// <returns>
        /// The line of text with an optional "//@@" markdown only prefix
        /// removed.
        /// </returns>
        private static string RemoveMarkdownOnlyPrefix(string line) =>
            _markdownOnlyRegex.Replace(line, match =>
            {
                var indentGroup = match.Groups["indent"];
                var lineGroup = match.Groups["line"];
                if (indentGroup.Success && lineGroup.Success)
                {
                    return indentGroup.Value + lineGroup.Value;
                }
                return line;
            });

        /// <summary>
        /// There are occasions where we might want to keep a line of code out
        /// of the snippets.  Comments like
        ///
        ///     /*@@*/ bool onlyInCode = true;
        ///
        /// will be stripped out of the markdown text.
        /// </summary>
        /// <param name="line">The line of text.</param>
        /// <returns>
        /// Whether the line should be removed.
        /// </returns>
        private static bool IsCodeOnlyLine(string line) =>
            line.IndexOf(_codeOnlyPattern) >= 0;

        /// <summary>
        /// Detects whether the line being processed is actually the region header or footer.
        /// These lines should be stripped out in order to support nested regions.
        /// </summary>
        /// <param name="line">The line of text.</param>
        /// <returns>Whether this is a region line.</returns>
        private static bool IsRegionLine(string line) =>
            _regionRegex.IsMatch(line);

        private async Task<List<Snippet>> GetSnippetsInDirectoryAsync(string baseDirectory)
        {
            var list = new List<Snippet>();
            foreach (var file in Directory.GetFiles(baseDirectory, "*.cs", SearchOption.AllDirectories))
            {
                try
                {
                    var text = await File.ReadAllTextAsync(file);
                    if (!text.Contains($"#region {_snippetPrefix}"))
                    {
                        continue;
                    }

                    var syntaxTree = CSharpSyntaxTree.ParseText(
                        text,
                        new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: _snippetPreprocessorSymbols),
                        path: file);

                    list.AddRange(GetAllSnippets(syntaxTree));
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($"Failed to discover snippets from file {file}", e);
                }
            }

            return list;
        }

        private Snippet[] GetAllSnippets(SyntaxTree syntaxTree)
        {
            var snippets = new List<Snippet>();
            var newRoot = PreprocessorDirectiveRemover.Instance.Visit(syntaxTree.GetRoot());
            syntaxTree = syntaxTree.WithRootAndOptions(newRoot, syntaxTree.Options);

            var directiveWalker = new DirectiveWalker();
            directiveWalker.Visit(syntaxTree.GetRoot());

            foreach (var region in directiveWalker.Regions)
            {
                var leadingTrivia = region.Item1.EndOfDirectiveToken.LeadingTrivia;
                if (!leadingTrivia.Any())
                {
                    // Skip unnamed regions
                    continue;
                }
                var syntaxTrivia = leadingTrivia.First(t => t.IsKind(SyntaxKind.PreprocessingMessageTrivia));
                var fromBounds = TextSpan.FromBounds(
                    region.Item1.GetLocation().SourceSpan.End,
                    region.Item2.GetLocation().SourceSpan.Start);

                var regionName = syntaxTrivia.ToString();
                if (regionName.StartsWith(_snippetPrefix))
                {
                    snippets.Add(new Snippet(syntaxTrivia.ToString(), syntaxTree.GetText().GetSubText(fromBounds), syntaxTree.FilePath));
                }
            }

            return snippets.ToArray();
        }

        class PreprocessorDirectiveRemover : CSharpSyntaxRewriter
        {
            public static PreprocessorDirectiveRemover Instance = new PreprocessorDirectiveRemover();
            private PreprocessorDirectiveRemover() : base(visitIntoStructuredTrivia: true)
            {
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.DisabledTextTrivia))
                    return SyntaxFactory.Whitespace("");

                return base.VisitTrivia(trivia);
            }

            public override SyntaxNode VisitIfDirectiveTrivia(IfDirectiveTriviaSyntax node)
            {
                return null;
            }

            public override SyntaxNode VisitElifDirectiveTrivia(ElifDirectiveTriviaSyntax node)
            {
                return null;
            }

            public override SyntaxNode VisitElseDirectiveTrivia(ElseDirectiveTriviaSyntax node)
            {
                return null;
            }

            public override SyntaxNode VisitEndIfDirectiveTrivia(EndIfDirectiveTriviaSyntax node)
            {
                return null;
            }
        }
        class DirectiveWalker : CSharpSyntaxWalker
        {
            private Stack<RegionDirectiveTriviaSyntax> _regions = new Stack<RegionDirectiveTriviaSyntax>();
            public List<(RegionDirectiveTriviaSyntax, EndRegionDirectiveTriviaSyntax)> Regions { get; } = new List<(RegionDirectiveTriviaSyntax, EndRegionDirectiveTriviaSyntax)>();

            public DirectiveWalker() : base(SyntaxWalkerDepth.StructuredTrivia)
            {
            }

            public override void VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
            {
                base.VisitRegionDirectiveTrivia(node);
                _regions.Push(node);
            }

            public override void VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
            {
                base.VisitEndRegionDirectiveTrivia(node);
                Regions.Add((_regions.Pop(), node));
            }
        }
    }
}
