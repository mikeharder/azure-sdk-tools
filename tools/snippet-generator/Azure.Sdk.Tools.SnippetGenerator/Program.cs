﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.CodeAnalysis.Options;

namespace Azure.Sdk.Tools.SnippetGenerator
{
    public class Program
    {
        [Option(ShortName = "b")] public string BasePath { get; set; }
        [Option(ShortName = "sm")] public bool StrictMode { get; set; }

        public async Task OnExecuteAsync()
        {
            List<string> unUsedSnippets = null;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var baseDirectory = new DirectoryInfo(BasePath).Name;
            if (baseDirectory.Equals("sdk"))
            {
                var tasks = new List<Task<IEnumerable<string>>>();
                foreach (var sdkDir in Directory.GetDirectories(BasePath))
                {
                    tasks.Add(new DirectoryProcessor(sdkDir).ProcessAsync());
                }

                await Task.WhenAll(tasks);
                unUsedSnippets = tasks
                    .SelectMany(t => t.Result)
                    .ToList();
            }
            else
            {
                unUsedSnippets = (await new DirectoryProcessor(BasePath).ProcessAsync()).ToList();
            }
            Console.WriteLine();
            if (unUsedSnippets.Any())
            {
                string message = $"Not all snippets were used.\n{string.Join(Environment.NewLine, unUsedSnippets)}";
                unUsedSnippets.Sort();
                if (StrictMode)
                {
                    throw new InvalidOperationException(message);
                }
                WriteError(message);
            }

            sw.Stop();
            Console.WriteLine($"SnippetGenerator completed in {sw.Elapsed}");
        }

        public static int Main(string[] args)
        {
            try
            {
                return CommandLineApplication.Execute<Program>(args);
            }
            catch (Exception e)
            {
                WriteError(e.ToString());
                return 1;
            }
        }

        private static void WriteError(string message, params object[] args)
        {
            ConsoleColor foreground = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(message, args);
            }
            finally
            {
                Console.ForegroundColor = foreground;
            }
        }
    }
}