using Quest_Data_Builder.Core;

namespace Quest_Data_Builder.TES3
{
    public class TES3DataFile
    {
        private readonly RecordStruct _records;

        public RecordStruct Records => _records;

        public TES3DataFile(BetterBinaryReader reader)
        {
            var tes3 = new RecordData(reader);

            if (tes3.Type != "TES3")
                throw new Exception("That's not a Morrowind master file.");

            var mRecords = new RecordStruct(StringComparer.OrdinalIgnoreCase);
            RecordData mRecord;

            while (reader.Position < reader.Length)
            {
                mRecord = new RecordData(reader);

                if (!mRecords.ContainsKey(mRecord.Type))
                    mRecords.Add(mRecord.Type, new List<RecordData>());

                mRecords[mRecord.Type].Add(mRecord);
                if (mRecord.Type == "INFO")
                {
                    var lastDIALRecord = mRecords["DIAL"].Last();
                    lastDIALRecord.ChildRecords.Add(mRecord);
                    mRecord.ParentRecord = lastDIALRecord;
                }

            }

            _records = mRecords;
        }
    }

    public class RecordStruct : Dictionary<string, List<RecordData>>
    {
        public RecordStruct(StringComparer? comparer) : base(comparer) { }
    }
}
