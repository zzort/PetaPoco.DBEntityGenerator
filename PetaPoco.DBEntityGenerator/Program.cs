﻿namespace PetaPoco.DBEntityGenerator
{
    using CommandLine;
    using MySql.Data.MySqlClient;
    using Newtonsoft.Json;
    using Npgsql;
    using Oracle.ManagedDataAccess.Client;
    using PetaPoco.DBEntityGenerator.Outputs;
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.SqlClient;
    using System.IO;

    class Program
    {
        static void Main(string[] args)
        {
#if NETCOREAPP
            DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);
            DbProviderFactories.RegisterFactory("SqlServer", SqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("MySql", MySqlClientFactory.Instance);
            DbProviderFactories.RegisterFactory("Oracle", OracleClientFactory.Instance);
#endif

            Parser.Default.ParseArguments<ProgramOptions>(args)
                .WithParsed<ProgramOptions>(opts => RunWithOptions(opts));
        }

        private static void RunWithOptions(ProgramOptions opts)
        {
            GenerateCommand generateCommand = null;
            if (!string.IsNullOrWhiteSpace(opts.FullConfig))
            {
                var configFileContent = opts.FullConfig;
                generateCommand = JsonConvert.DeserializeObject<GenerateCommand>(configFileContent);
            }
            else if (!string.IsNullOrWhiteSpace(opts.ConfigFile))
            {
                var configFileContent = string.Empty;
                using (var fs = new FileStream(opts.ConfigFile, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        configFileContent = sr.ReadToEnd();
                    }
                }

                generateCommand = JsonConvert.DeserializeObject<GenerateCommand>(configFileContent);
                
                // Allow the connection string from the command line to override the one in the config file.
                if (!string.IsNullOrWhiteSpace(opts.ConnectionString))
                {
                    generateCommand.ConnectionString = opts.ConnectionString;
                }
            }
            else
            {
                generateCommand = new GenerateCommand
                {
                    ConnectionString = opts.ConnectionString,
                    ProviderName = opts.ProviderName,
                    ExplicitColumns = opts.ExplicitColumns,
                    TrackModifiedColumns = opts.TrackModifiedColumns,
                    Namespace = opts.Namespace
                };
            }

            IOutput output = null;

            if (string.Equals("file", opts.Output, StringComparison.InvariantCultureIgnoreCase))
            {
                Console.WriteLine($"Writing to file '{opts.OutputFile}'");
                output = new FileOutput(opts.OutputFile);
            }
            else
            {
                output = new ConsoleOutput();
            }

            using (output)
            {
                var generator = new Generator(output);
                generator.Generate(generateCommand);
            }
        }
    }
}
