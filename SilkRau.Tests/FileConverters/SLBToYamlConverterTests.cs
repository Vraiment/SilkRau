﻿/*
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using FluentAssertions;
using NSubstitute;
using NSubstitute.ClearExtensions;
using NUnit.Framework;
using SAGESharp.IO.Binary;
using SilkRau.FileConverters;
using System;
using System.IO;
using YamlDotNet.Serialization;

namespace SilkRau.Tests.FileConverters
{
    class SLBToYamlConverterTests
    {
        private readonly IBinarySerializer<object> slbSerializer
            = Substitute.For<IBinarySerializer<object>>();

        private readonly ISerializer yamlSerializer = Substitute.For<ISerializer>();

        private readonly SLBToYamlConverter<object>.IIO io
            = Substitute.For<SLBToYamlConverter<object>.IIO>();

        private readonly IFileConverter fileConverter;

        public SLBToYamlConverterTests()
        {
            fileConverter = new SLBToYamlConverter<object>(
                slbSerializer,
                yamlSerializer,
                io
            );
        }

        [TearDown]
        public void TearDown()
        {
            slbSerializer.ClearSubstitute();
            yamlSerializer.ClearSubstitute();
            io.ClearSubstitute();
        }

        [Test]
        public void Test_Converting_A_File_To_Another()
        {
            IBinaryReader binaryReader = Substitute.For<IBinaryReader>();
            object value = new object();
            string inputFileName = nameof(inputFileName);
            string outputFileName = nameof(outputFileName);
            string contents = nameof(contents);

            io.ReadBinaryFromFile(
                filePath: Arg.Is(inputFileName),
                function: Arg.Any<Func<IBinaryReader, object>>()
            ).Returns(callInfo => callInfo.Arg<Func<IBinaryReader, object>>().Invoke(binaryReader));
            slbSerializer.Read(binaryReader).Returns(value);
            yamlSerializer.Serialize(value).Returns(contents);

            fileConverter.Convert(inputFilePath: inputFileName, outputFilePath: outputFileName);

            io.Received().WriteTextToFile(filePath: outputFileName, contents: contents);
        }

        [Test]
        public void Test_Converting_A_File_With_A_Bad_Format()
        {
            IBinaryReader binaryReader = Substitute.For<IBinaryReader>();
            object value = new object();
            string inputFileName = nameof(inputFileName);
            string outputFileName = nameof(outputFileName);
            string contents = nameof(contents);
            EndOfStreamException cause = new EndOfStreamException();

            io.ReadBinaryFromFile(
                filePath: Arg.Is(inputFileName),
                function: Arg.Any<Func<IBinaryReader, object>>()
            ).Returns(callInfo => callInfo.Arg<Func<IBinaryReader, object>>().Invoke(binaryReader));
            slbSerializer.Read(binaryReader).Returns(_ => throw cause);

            Action action = () => fileConverter
                .Convert(inputFilePath: inputFileName, outputFilePath: outputFileName);

            action.Should()
                .ThrowExactly<BadFormatException>()
                .Which
                .InnerException
                .Should()
                .BeSameAs(cause);

            io.DidNotReceiveWithAnyArgs().WriteTextToFile(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
