using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;

using ImageSdk;
using ImageSdk.Contracts;
using ImageSdk.Common;

namespace tile_scanner
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("usage: tile-scanner slide-filename (row-major|column-major)");
                Console.WriteLine("    eg.: tile-scanner 9235.sys row-major");
                System.Environment.Exit(1);
            }

            var filename = args[0];
            var rowMajor = args[1] == "row-major";
            Console.WriteLine($"testing: filename = {filename}, row-major = {rowMajor}");

            var container = new ServiceCollection();
            var serviceProvider = container.BuildServiceProvider();
            var factory = new ImageConnectorFactory(serviceProvider);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            using (var connector = factory.Create(filename))
            {
                // not a useful thing to change because of disc caching
                const int iterations = 1;

                for (var loop = 0; loop < iterations; loop++)
                {
                    Console.WriteLine($"loop {loop} ...");
                    ScanImage(connector, rowMajor);
                }
            }

            stopWatch.Stop();

            TimeSpan ts = stopWatch.Elapsed;
            Console.WriteLine($"took: {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}");
        }

        private static void ScanImage(IImageConnector connector, bool rowMajor)
        {
            var zoomLevelInfo = connector.GetZoomLevels();
            var index = zoomLevelInfo.Length - 1;
            var level = zoomLevelInfo[index];

            if (rowMajor)
            {
                for (var row = 0; row < level.Rows; row++)
                {
                    for (var col = 0; col < level.Columns; col++)
                    {
                        var tile = connector.GetTile(index, col, row);
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
                    }
                }
            }
        }
    }
}
