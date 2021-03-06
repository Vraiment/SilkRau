﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using CommandLine;
using Ninject;
using SilkRau.NinjectModules;
using SilkRau.Options;
using System;
using System.IO;

namespace SilkRau
{
    internal sealed class Program
    {
        private readonly IFileTypeRegistry fileTypeRegistry;

        private readonly IFileConverterFactory fileConverterFactory;

        private readonly TextWriter textWriter;

        private readonly IPathValidator pathValidator;

        public Program(
            IFileTypeRegistry fileTypeRegistry,
            IFileConverterFactory fileConverterFactory,
            TextWriter textWriter,
            IPathValidator pathValidator
        ) {
            this.fileTypeRegistry = fileTypeRegistry;
            this.fileConverterFactory = fileConverterFactory;
            this.textWriter = textWriter;
            this.pathValidator = pathValidator;
        }

        private static int Main(string[] args)
        {
            IKernel kernel = new StandardKernel(
                new CommandLineModule(),
                new SAGESharpModule(),
                new FileConvertersModule(),
                new ProgramModule()
            );

            Program program = kernel.Get<Program>();
            Parser parser = kernel.Get<Parser>();

            return parser.ParseArguments<ConvertOptions, PrintOptions>(args)
                .MapResult<ConvertOptions, PrintOptions, int>(
                    options => HandleErrors(options, program.Run),
                    options => HandleErrors(options, program.Run),
                    _ => 2 // This happens if bad arguments are passed
                );
        }

        private static int HandleErrors<T>(T options, Action<T> action)
        {
            try
            {
                action(options);
                return 0;
            }
            catch (SilkRauException exception)
            {
                Console.Error.WriteLine(exception.Message);

                if (exception is BadFormatException)
                {
                    DumpException(options, exception);
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Fatal, unexpected error: ${exception.Message}");
                Console.Error.WriteLine(exception.StackTrace);

                DumpException(options, exception);
            }

            return -1;
        }

        private static void DumpException<T>(T options, Exception exception)
        {
            string dumpFileName = $"SilkRau.dump.{DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss-ffff")}";

            Console.Error.WriteLine($"Created dump file {dumpFileName}");

            string[] contents = new string[]
            {
                $"Failed to execute: {options}",
                $"With message: {exception.Message}",
                exception.StackTrace
            };

            File.WriteAllLines(path: dumpFileName, contents: contents);
        }

        public void Run(ConvertOptions options)
        {
            string outputFile = options.OutputFile ??
                Path.ChangeExtension(options.InputFile, GetExtensionForFormat(options.OutputFormat));

            if (!options.Force)
            {
                pathValidator.ValidateFileDoesNotExist(outputFile);
            }

            IFileConverter fileConverter = fileConverterFactory.BuildFileConverter(
                fileType: fileTypeRegistry.GetTypeForFileType(options.FileType),
                fileConversion: new FileConversion(
                    inputFileFormat: options.InputFormat,
                    outputFileFormat: options.OutputFormat
                )
            );

            fileConverter.Convert(
                 inputFilePath: options.InputFile,
                 outputFilePath: outputFile
            );
        }

        private static string GetExtensionForFormat(FileFormat fileFormat)
        {
            if (FileFormat.SLB == fileFormat)
            {
                return "slb";
            }
            else if (FileFormat.Yaml == fileFormat)
            {
                return "yaml";
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        public void Run(PrintOptions options)
        {
            if (options.FileTypes)
            {
                foreach (string fileType in fileTypeRegistry.SupportedFileTypes)
                {
                    textWriter.WriteLine(fileType);
                }
            }
            else if (options.Conversions)
            {
                foreach (FileConversion fileConversion in fileConverterFactory.ValidConversions)
                {
                    textWriter.WriteLine($"{fileConversion.InputFileFormat} to {fileConversion.OutputFileFormat}");
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
