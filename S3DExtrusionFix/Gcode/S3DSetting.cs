using System.Collections.Generic;
using System.Linq;

namespace JSo.GCode
{
    public class S3DSetting
    {
        public string Key { get; set; }
        public S3DSettingType SettingType { get; set; }

        public string Text { get; set; }

        public List<int> IntValues { get; set; } = new List<int>();
        public double IntValue => IntValues == null || !IntValues.Any() ? -1 : IntValues[0];

        public List<double> DoubleValues { get; set; } = new List<double>();
        // Quick accessor for single value setting
        public double DoubleValue => DoubleValues == null || !DoubleValues.Any() ? -1 : DoubleValues[0];

        public override string ToString()  {
            switch (SettingType)
            {
                case S3DSettingType.Double:
                    return $";   {Key},{string.Join(",", DoubleValues.Select(s => s.ToString("0.##")))}";

                case S3DSettingType.Integer:
                    return $";   {Key},{string.Join(",", IntValues.Select(s => s.ToString()))}";

                case S3DSettingType.Text:
                    return $";   {Text}";

                case S3DSettingType.Empty:
                    return string.Empty;
            }

            return string.Empty;
        }
    }
}
