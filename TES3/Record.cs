using Quest_Data_Builder.Core;
using System;
using System.Collections.Generic;

namespace Quest_Data_Builder.TES3
{
    public class Record
    {
        public readonly RecordData RecordInfo;

        public Record(RecordData recordData)
        {
            this.RecordInfo = recordData;
        }
    }
}
