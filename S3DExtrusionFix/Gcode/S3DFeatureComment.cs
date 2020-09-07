namespace JSo.GCode
{
    public class S3DFeatureComment
    {
        public string Name { get; set; }

        public override string ToString() => $"; feature {Name}";
    }
}
