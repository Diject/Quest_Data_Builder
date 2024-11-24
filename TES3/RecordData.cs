using Quest_Data_Builder.Core;

namespace Quest_Data_Builder.TES3
{
    public class RecordData
    {
        public RecordData? ParentRecord { get; set; } = null;
        public List<RecordData> ChildRecords { get; } = new List<RecordData>();

        public string Type { get; private set; }
        public byte[]? Data { get; private set; }

        protected uint dataSize;
        protected uint flags;
        protected uint unknow;
        public long Position { get; private set; }
        protected BetterReader _reader;

        public bool Blocked
        {
            get { return (flags & 0x00002000) != 0; }
        }

        public bool Persistant
        {
            get { return (flags & 0x00000400) != 0; }
        }

        public bool Deleted
        {
            get { return (flags & 0x00000020) != 0; }
        }

        public bool Disabled
        {
            get { return (flags & 0x00000800) != 0; }
        }

        public RecordData(BetterReader reader)
        {
            _reader = reader;
            Position = _reader.Position;
            Type = _reader.ReadString(4);
            Extract(false);
        }

        private void Extract(bool saveRecordData)
        {
            _reader.Position = Position + 4;
            dataSize = _reader.ReadUInt32();
            unknow = _reader.ReadUInt32();
            flags = _reader.ReadUInt32();
            if (saveRecordData)
            {
                Data = ExtractSubRecords(dataSize);
            }
            else
            {
                _reader.Position += dataSize;
            }
        }

        public virtual void DeserializeSubRecords()
        {
            Extract(true);
        }

        protected virtual byte[] ExtractSubRecords(uint size)
        {
            return _reader.ReadBytes((int)size);
        }

        public void DisposeSubData()
        {
            Data = null;
        }  
    }
}
