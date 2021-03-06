﻿using CommandLine;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpenDirectoryDownloader
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static string ConsoleTitle { get; set; }
        private static CommandLineOptions CommandLineOptions { get; set; }

        static async Task<int> Main(string[] args)
        {
            SetConsoleTitle("OpenDirectoryDownloader");

            Console.WriteLine("Started");
            Logger.Info("Started");

            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithNotParsed(o =>
                {
                    List<Error> errors = o.ToList();

                    if (errors.Any())
                    {
                        foreach (Error error in errors)
                        {
                            Console.WriteLine($"Error command line parameter '{error.Tag}'");
                        }
                    }
                })
                .WithParsed(o => CommandLineOptions = o);

            if (CommandLineOptions.Threads < 1 || CommandLineOptions.Threads > 100)
            {
                Console.WriteLine("Threads must be between 1 and 100");
                return 1;
            }

            string url = CommandLineOptions.Url;

            if (string.IsNullOrWhiteSpace(url))
            {
                Console.WriteLine("Which URL do you want to index?");
                url = Console.ReadLine();
            }

            // Wait until this ticket is closed: https://github.com/dotnet/corefx/pull/37050
            //AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);

            OpenDirectoryIndexerSettings openDirectoryIndexerSettings = new OpenDirectoryIndexerSettings
            {
                CommandLineOptions = CommandLineOptions
            };

            if (File.Exists(url))
            {
                openDirectoryIndexerSettings.FileName = url;
            }
            else
            {
                Console.WriteLine($"URL specified: {url}");

                url = Library.FixUrl(url);
                Console.WriteLine($"URL fixed: {url}");

                openDirectoryIndexerSettings.Url = url;
            }

            openDirectoryIndexerSettings.Threads = openDirectoryIndexerSettings.CommandLineOptions.Threads;
            openDirectoryIndexerSettings.Timeout = openDirectoryIndexerSettings.CommandLineOptions.Timeout;
            openDirectoryIndexerSettings.Username = openDirectoryIndexerSettings.CommandLineOptions.Username;
            openDirectoryIndexerSettings.Password = openDirectoryIndexerSettings.CommandLineOptions.Password;

            // FTP
            // TODO: Make dynamic
            if (openDirectoryIndexerSettings.Url?.StartsWith("ftp") == true)
            {
                openDirectoryIndexerSettings.Threads = 6;
            }

            OpenDirectoryIndexer openDirectoryIndexer = new OpenDirectoryIndexer(openDirectoryIndexerSettings);

            SetConsoleTitle($"{new Uri(url).Host.Replace("www.", string.Empty)} - {ConsoleTitle}");

            openDirectoryIndexer.StartIndexingAsync();
            Console.WriteLine("Started indexing!");

            Command.ShowInfoAndCommands();
            Command.ProcessConsoleInput(openDirectoryIndexer);

            await openDirectoryIndexer.IndexingTask;

            if (!CommandLineOptions.Quit)
            {
                Console.WriteLine("Press ESC to exit");
                Console.ReadKey();
            }

            return 0;
        }

        public static void SetConsoleTitle(string title)
        {
            ConsoleTitle = title;

            Console.Title = title;
        }
    }
}
