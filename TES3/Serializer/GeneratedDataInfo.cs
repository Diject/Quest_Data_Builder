using Quest_Data_Builder.Config;

namespace Quest_Data_Builder.TES3.Serializer
{
    class GeneratedDataInfo
    {
        static readonly int version = 7;
        readonly List<string> files;
        readonly CustomSerializer serializer;

        public GeneratedDataInfo(List<string> files, SerializerType format = SerializerType.Json)
        {
            this.files = files;
            serializer = new CustomSerializer(format);
        }

        public override string ToString()
        {
            var table = serializer.NewTable();

            table.Add("version", version);
            table.Add("time", (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds);
            table.Add("format", MainConfig.OutputFileFormat);

            var filesTable = serializer.NewArray();
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file);
                if (filename is null) continue;
                filesTable.Add(filename.ToLower());
            }
            table.Add("files", filesTable);

            return serializer.GetResult(table);
        }
    }
}
