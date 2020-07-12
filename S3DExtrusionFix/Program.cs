using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace S3DExtrusionFix
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!args.Any())
            {
                Console.WriteLine("The path to the gcode file to be fixed is required.");
                Console.ReadLine();
                return;
            }

            var path = args[0];

            var lines = File.ReadAllLines(path);

            double curx = 0, cury = 0, cure = 0;

            var values = new Dictionary<char, double>();

            int lineCount = 0;

            var movements = new List<(int lineNumber, double ratio, double distance)>();

            foreach (var line in lines)
            {
                lineCount++;
                values = ParseLine(line);
                if (values.ContainsKey('G'))
                {
                    switch (values['G'])
                    {
                        case 92:
                            cure = values['E'];
                            break;

                        case 28:
                            curx = cury = 0;
                            break;

                        case 1:
                            if (values.ContainsKey('X') && values.ContainsKey('Y'))
                            {
                                var dx = values['X'] - curx;
                                var dy = values['Y'] - cury;

                                curx = values['X'];
                                cury = values['Y'];

                                if (values.ContainsKey('E'))
                                {
                                    var de = values['E'] - cure;
                                    cure = values['E'];

                                    var dd = Math.Sqrt(dx * dx + dy * dy);
                                    if (dd != 0)
                                    {
                                        var x = de / dd;

                                        movements.Add((lineCount, x, dd));
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            var mean = movements.Average(m => m.ratio);
            var sd = Sd(movements.Select(m => m.ratio));

            Console.WriteLine($"Mean extrusion to travel ratio: {mean}");
            Console.WriteLine($"Std: {sd}");

            if (sd < mean)
            {
                Console.WriteLine("Clean file. No action taken.");
                Console.ReadLine();
                return;
            }

            var oddities = movements.Where(m => m.ratio > mean + sd);

            var list = oddities.ToList();

            if (list.Any())
                Console.WriteLine($"{list.Count()} outliers found:");

            list.ForEach(o =>
            {
                Console.WriteLine($"({o.lineNumber}) Extrusion ratio: {o.ratio}");

                // apply mean extrustion rate over distance
                var suggestedExtrusion = o.distance * mean;
                Console.WriteLine($"Current: {lines[o.lineNumber - 1]}");

                var tags = ParseLine(lines[o.lineNumber - 1]);

                var previousTags = ParseLine(lines[o.lineNumber - 2]);
                if (previousTags.ContainsKey('E'))
                {

                    tags['E'] = previousTags['E'] + suggestedExtrusion;

                    var bob = new StringBuilder();
                    bob.Append($"G{tags['G']:0} X{tags['X']:0.000} Y{tags['Y']:0.000} E{tags['E']:0.0000}");

                    if (tags.ContainsKey('F'))
                        bob.Append($" F{tags['F']:0}");

                    var newLine = bob.ToString();
                    Console.WriteLine($"Changed: {newLine}");

                    lines[o.lineNumber - 1] = newLine;
                }
            });

            var filename = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var dir = Path.GetDirectoryName(path);

            var newFilename = $"{dir}\\{filename}.s3dfix{extension}";

            File.WriteAllLines(newFilename, lines);

            Console.WriteLine($"Fixes written to: {newFilename}");
            Console.ReadLine();
        }

        static double Sd(IEnumerable<double> values)
        {
            var mean = values.Average();

            var sumOfSquares = values.Sum(v => (v - mean) * (v - mean));

            var sd = Math.Sqrt(sumOfSquares / (values.Count() - 1));

            return sd;
        }

        static Dictionary<char, double> ParseLine(string line)
        {
            var isParam = new Regex(@"[A-Z][0-9\.]+");
            var values = new Dictionary<char, double>();
            var parts = line.Split(' ');
            if (parts[0][0] == 'G')
            {
                foreach (var part in parts)
                {
                    if (isParam.IsMatch(part))
                    {
                        var tag = part[0];
                        if (double.TryParse(part.Substring(1, part.Length - 1), out var v))
                            values.Add(tag, v);
                    }
                }
            }

            return values;
        }
    }
}
