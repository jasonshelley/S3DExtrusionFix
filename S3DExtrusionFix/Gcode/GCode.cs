using System.Linq;
using System.Text.RegularExpressions;

namespace JSo.GCode
{
    public class GCode
    {
        public string Cmd { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double E { get; set; }
        public int F { get; set; }
        public int S { get; set; }
        public int T { get; set; }

        private static Regex GcodeCommandRegex = new Regex("[MGT][^ ]+");

        public static bool IsGcodeLine(string line) => GcodeCommandRegex.IsMatch(line);

        public static GCode ParseLine(string line)
        {
            var code = new GCode();

            var parts = line.Split(' ').ToList();

            code.Cmd = parts.First();
            parts.RemoveAt(0);

            foreach (var part in parts)
            {
                var cmd = new Regex("([A-Za-z]*)").Match(part);
                switch (cmd.Captures[0].Value)
                {
                    case "X":
                        code.X = double.Parse(part.Substring(1));
                        break;

                    case "Y":
                        code.Y = double.Parse(part.Substring(1));
                        break;

                    case "Z":
                        code.Z = double.Parse(part.Substring(1));
                        break;

                    case "E":
                        code.E = double.Parse(part.Substring(1));
                        break;

                    case "F":
                        code.F = int.Parse(part.Substring(1));
                        break;

                    case "S":
                        code.S = int.Parse(part.Substring(1));
                        break;

                    case "T":
                        code.T = int.Parse(part.Substring(1));
                        break;

                    default:
                        break;
                }
            }

            return code;
        }

    }
}
