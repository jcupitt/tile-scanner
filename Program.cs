using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
            using (BinaryWriter binWriter = new BinaryWriter(File.Open(outputFilename, FileMode.Create)))
            {
                CopyImage(connector, binWriter, rowMajor);
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
            var innerParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
            var stripLock = new object();
            var outerLength = rowMajor ? level.Rows : level.Columns;
            var innerLength = rowMajor ? level.Columns : level.Rows;

            Console.WriteLine($"copying: {level.Columns} x {level.Rows} tiles ...");    

            for (var outer = 0; outer < outerLength; outer++)
            {
                var strip = new Dictionary<(int, int), byte[]>();

                Parallel.For(0, innerLength, innerParallelOptions, (inner, innerLoopState) =>
                {
                    var column = rowMajor ? inner : outer;
                    var row = rowMajor ? outer : inner;

                    var tile = connector.GetTile(index, column, row);
                    if (tile != null)
                    {
                        lock (stripLock)
                        {
                            strip.Add((column, row), tile);
                        }
                    }
                });

                foreach (var entry in strip)
                {
                    binWriter.Write(entry.Value);
                }
            }

        }
    }
}
