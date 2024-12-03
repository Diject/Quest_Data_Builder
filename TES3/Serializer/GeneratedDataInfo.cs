﻿using Luaon.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Serializer
{
    class GeneratedDataInfo
    {
        static readonly int version = 1;
        readonly SortedList<uint, string> files;

        public GeneratedDataInfo(SortedList<uint, string> files)
        {
            this.files = files;
        }

        public override string ToString()
        {
            var table = new LTable();

            table.Add("version", version);
            table.Add("time", (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds);

            var filesTable = new LTable();
            foreach (var file in files)
            {
                var filename = Path.GetFileName(file.Value);
                if (filename is null) continue;
                filesTable.Add(filename.ToLower());
            }
            table.Add("files", filesTable);

            return table.ToString();
        }
    }
}