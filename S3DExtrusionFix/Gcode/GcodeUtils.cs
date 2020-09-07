using System.Collections.Generic;
using System.Linq;

namespace JSo.GCode
{
    public static class GcodeUtils
    {
        public static List<GCodeLine> ParseLines(IEnumerable<string> lines)
        {
            var parsed = new List<GCodeLine>();

            foreach (var line in lines)
                parsed.Add(ParseLine(line));

            return parsed;
        }

        private static GCodeLine ParseLine(string line)
        {
            var gcodeLine = new GCodeLine { Raw = line };
            if (line.StartsWith(";"))
            {
                line = line.Substring(2);
                if (line.StartsWith("feature"))
                {
                    gcodeLine.LineType = GCodeLineType.FeatureComment;
                    gcodeLine.S3DFeature = new S3DFeatureComment { Name = line.Substring("feature".Length) };
                }
                else if (line.StartsWith("  "))
                {
                    // double space indicates an S3D setting
                    gcodeLine.LineType = GCodeLineType.S3DSetting;
                    line = line.Substring(2);
                    var parts = line.Split(',');

                    gcodeLine.S3DSetting = new S3DSetting { Key = parts[0] };

                    if (parts.Count() > 1)
                    {
                        var valueArray = parts[1]?.Split('|');
                        // more restrictive to less
                        // S3D settings are a mess
                        // they use both comma and pipe separated lists, and a mixture of both
                        // I'm only interested in the pipe separated lists for now, so that's what I'm using
                        // SETTING DATA WILL BE LOST
                        if (int.TryParse(valueArray[0], out var i))
                        {
                            gcodeLine.S3DSetting.SettingType = S3DSettingType.Integer;
                            gcodeLine.S3DSetting.IntValues.Add(i);
                        }
                        else if (double.TryParse(valueArray[0], out var d))
                        {
                            gcodeLine.S3DSetting.SettingType = S3DSettingType.Double;
                            gcodeLine.S3DSetting.DoubleValues.Add(d);
                        }
                        else
                        {
                            gcodeLine.S3DSetting.SettingType = S3DSettingType.Text;
                            gcodeLine.S3DSetting.Text = string.Join("|", valueArray);
                        }
                    } 
                    else
                    {
                        gcodeLine.S3DSetting.SettingType = S3DSettingType.Empty;                        
                    }
                }
            }
            else if (GCode.IsGcodeLine(line))
            {
                gcodeLine.GCode = GCode.ParseLine(line);
            }
            else
            {
                gcodeLine.LineType = GCodeLineType.Unknown;
            }

            return gcodeLine;
        }
    }
}
