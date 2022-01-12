using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;

using ImageSdk;
using ImageSdk.Contracts;
using ImageSdk.Common;

namespace tile_scanner
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("usage: tile-scanner input-filename output-filename (row-major|column-major)");
                Console.WriteLine("  eg.: tile-scanner 9235.sys f:/tmp/x row-major");
                System.Environment.Exit(1);
            }

            var inputFilename = args[0];
            var outputFilename = args[1];
            var rowMajor = args[2] == "row-major";
            var length = new System.IO.FileInfo(inputFilename).Length;
            Console.WriteLine($"filename = {inputFilename}, outputFilename = {outputFilename}, row-major = {rowMajor}, {length / (1024 * 1024)} MB");

            var container = new ServiceCollection();
            var serviceProvider = container.BuildServiceProvider();
            var factory = new ImageConnectorFactory(serviceProvider);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            using (var connector = factory.Create(inputFilename))
            {
                // not a very useful thing to change because of disc caching
                const int iterations = 1;

                for (var loop = 0; loop < iterations; loop++)
                {
                    using (BinaryWriter binWriter = new BinaryWriter(File.Open(outputFilename, FileMode.Create)))
                    {
                        CopyImage(connector, binWriter, rowMajor);
                    }
                }
            }

            stopWatch.Stop();

            TimeSpan ts = stopWatch.Elapsed;
            Console.WriteLine($"took: {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");
        }

        private static void CopyImage(IImageConnector connector, BinaryWriter binWriter, bool rowMajor)
        {
            var zoomLevelInfo = connector.GetZoomLevels();
            var index = zoomLevelInfo.Length - 1;
            var level = zoomLevelInfo[index];

            Console.WriteLine($"copying: {level.Columns} x {level.Rows} tiles ...");    

            if (rowMajor)
            {
                for (var row = 0; row < level.Rows; row++)
                {
                    for (var col = 0; col < level.Columns; col++)
                    {
                        var tile = connector.GetTile(index, col, row);
                        if (tile != null)
                        {
                            binWriter.Write(tile);
                        }
                    }
                }

            }
            else
            {
                for (var col = 0; col < level.Columns; col++)
                {
                    for (var row = 0; row < level.Rows; row++)
                    {
                        var tile = connector.GetTile(index, col, row);
                        if (tile != null)
                        {
                            binWriter.Write(tile);
                        }
                    }
                }
            }
        }
    }
}
