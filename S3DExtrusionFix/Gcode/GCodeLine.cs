using System.Text.RegularExpressions;

namespace JSo.GCode
{
    public class GCodeLine
    {
        public string Raw { get; set; }

        public GCodeLineType LineType { get; set; }

        // set if LineType == S3DSetting
        public S3DSetting S3DSetting { get; set; }

        // set if LineType == Gcode
        public GCode GCode { get; set; }

        // set if LineType == Comment
        public string Comment { get; set; }

        public S3DFeatureComment S3DFeature { get; set; }

        public override string ToString()
        {
            switch (LineType)
            {
                case GCodeLineType.Comment:
                    return Comment;

                case GCodeLineType.S3DSetting:
                    return S3DSetting.ToString();

                case GCodeLineType.Gcode:
                    return GCode.ToString();

                case GCodeLineType.FeatureComment:
                    return S3DFeature.ToString();
            }

            return string.Empty;
        }
    }
}
