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
        const string FilamentDiameter = "filamentDiameters";
        const string NozzleDiameter = "extruderDiameter";
        const string LayerHeight = "layerHeight";
        const string LineWidth = "extruderWidth";
        const string MaxWidthPercentage = "singleExtrusionMaxPrintingWidthPercentage";
        const string FirstLayerWidthPercentage = "firstLayerWidthPercentage";
        const string ExtrusionMultiplier = "extrusionMultiplier";
        const string IsParam = "isParam";

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

            var values = new Dictionary<string, double>();
            var parameters = new Dictionary<string, double>();

            int lineCount = 0;
            int previousLineNumer = 0;

            var movements = new List<(int lineNumber, double lineWidth, double distance, double previousE)>();
            var multiplier = 1.0;

            var outlines = new List<string>();

            int outliers = 0;

            foreach (var line in lines)
            {
                lineCount++;
                var outLine = line;
                values = ParseLine(line);
                if (values.ContainsKey(IsParam))
                {
                    values.Remove(IsParam);
                    parameters.Add(values.Keys.First(), values.Values.First());

                    if (parameters.ContainsKey(MaxWidthPercentage) && parameters.ContainsKey(FirstLayerWidthPercentage))
                    {
                        multiplier = Math.Max(parameters[MaxWidthPercentage], parameters[FirstLayerWidthPercentage]) / 100.0;
                    }
                }
                else if (values.ContainsKey("G"))
                {
                    switch (values["G"])
                    {
                        case 92:
                            cure = values["E"];
                            break;

                        case 28:
                            curx = cury = 0;
                            break;

                        case 1:
                            var travel = 0.0;
                            if (values.ContainsKey("X") && values.ContainsKey("Y"))
                            {
                                var dx = values["X"] - curx;
                                var dy = values["Y"] - cury;
                                travel = Math.Sqrt(dx * dx + dy * dy);

                                curx = values["X"];
                                cury = values["Y"];
                            }
                            if (values.ContainsKey("E"))
                            {
                                var de = values["E"] - cure;
                                // volume extruded
                                var extrudedVolume = de * Math.PI * Math.Pow(parameters[FilamentDiameter] / 2, 2);

                                if (travel != 0)
                                {
                                    // for a given volume extruded, for a given distance
                                    // we can calculate the actual line width
                                    // width = volume / (dd * layer height)
                                    double lineWidth = extrudedVolume / (travel * parameters[LayerHeight]);
                                    // line width is greater than the indicated maximums
                                    if (lineWidth > parameters[LineWidth] * multiplier)
                                    {
                                        // we'll set the new extrusion to achieve the configured line width
                                        // this may be wrong as it may be a single line extrusion
                                        // TODO: Deal with single line extrusions
                                        var volumeToExtrude = travel * parameters[LineWidth] * parameters[LayerHeight] * parameters[ExtrusionMultiplier];
                                        var extrusionDistance = volumeToExtrude / (Math.PI * Math.Pow(parameters[FilamentDiameter] / 2, 2));

                                        values["E"] = cure + extrusionDistance;

                                        outLine = BuildNewLine(values);

                                        outliers++;
                                        Console.WriteLine($"({(lineCount == previousLineNumer + 1 ? "+" : "")}{lineCount}) Indicated Line Width: {lineWidth}");
                                        previousLineNumer = lineCount;
                                    }
                                    movements.Add((lineCount, lineWidth, travel, cure));
                                }
                                cure = values["E"];
                            }


                            break;
                    }
                }
                outlines.Add(outLine);   
            }

            var mean = movements.Average(m => m.lineWidth);
            var sd = Sd(movements.Select(m => m.lineWidth));

            Console.WriteLine($"Mean lineWidth: {mean}");
            Console.WriteLine($"Configured lineWidth (including extrusion multiplier): {parameters[LineWidth] * parameters[ExtrusionMultiplier]}");
            Console.WriteLine($"Std: {sd}");
            Console.WriteLine($"Total outliers (including waterfall modifications): {outliers}");

            if (outliers == 0)
            {
                Console.WriteLine("Clean file. No action taken.");
                Console.ReadLine();
                return;
            }

            var filename = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            var dir = Path.GetDirectoryName(path);

            var newFilename = $"{dir}\\{filename}.s3dfix{extension}";

            File.WriteAllLines(newFilename, outlines);

            Console.WriteLine($"Fixes written to: {newFilename}");
            Console.ReadLine();
        }

        private static string BuildNewLine(Dictionary<string, double> tags)
        {
            var bob = new StringBuilder();
            bob.Append($"G{tags["G"]:0} X{tags["X"]:0.000} Y{tags["Y"]:0.000} E{tags["E"]:0.0000}");

            if (tags.ContainsKey("F"))
                bob.Append($" F{tags["F"]:0}");

            var newLine = bob.ToString();
            return newLine;
        }

        static double Sd(IEnumerable<double> values)
        {
            var mean = values.Average();

            var sumOfSquares = values.Sum(v => (v - mean) * (v - mean));

            var sd = Math.Sqrt(sumOfSquares / (values.Count() - 1));

            return sd;
        }

        static Dictionary<string, double> ParseLine(string line)
        {
            var values = new Dictionary<string, double>();
            if (line.StartsWith(";"))
            {
                line = line.Substring(2);
                if (line.StartsWith("  "))
                {
                    line = line.Substring(2);
                    var parts = line.Split(',');
                    if (parts.Count() > 1)
                    {
                        var valueArray = parts[1]?.Split('|');
                        if (double.TryParse(valueArray[0], out var x))
                        {
                            values.Add(IsParam, 0);
                            values.Add(parts[0], x);
                        }
                    }
                }
            }
            else
            {

                var isParam = new Regex(@"[A-Z][0-9\.]+");
                var parts = line.Split(' ');
                if (parts[0][0] == 'G')
                {
                    foreach (var part in parts)
                    {
                        if (isParam.IsMatch(part))
                        {
                            var tag = part[0];
                            if (double.TryParse(part.Substring(1, part.Length - 1), out var v))
                                values.Add(new String(tag, 1), v);
                        }
                    }
                }

            }
            return values;
        }
    }
}
