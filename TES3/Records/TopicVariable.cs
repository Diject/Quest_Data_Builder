using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Quest_Data_Builder.TES3.Records
{
    class SCVRVariable
    {
        public readonly uint Index;
        public readonly SCVRType Type;
        public readonly string DetailsValue = "";
        public readonly SCVROperator Operator;
        public readonly string Name = "";
        public float? FLTV; //Needs to set manually
        public UInt32? INTV; //Needs to set manually

        public float? FloatValue { get { return FLTV; } }
        public float? IntValue { get { return INTV; } }

        public double Value {
            get { return INTV ?? FLTV ?? 0; }
        }

        public SCVRVariable(string scvr)
        {
            Index = Convert.ToUInt32(scvr[0]);
            Type = (SCVRType)scvr[1];
            DetailsValue = scvr.Substring(2, 2);
            Operator = (SCVROperator)scvr[4];
            if (scvr.Length > 5) 
                Name = scvr.Substring(5);
        }
    }

    enum SCVRType
    {
        Function = '1',
        Global = '2',
        Local = '3',
        Journal = '4',
        Item = '5',
        Dead = '6',
        NotID = '7',
        NotFaction = '8',
        NotClass = '9',
        NotRace = 'A',
        NotCell = 'B',
        NotLocal = 'C',
    }

    enum SCVROperator
    {
        Equal = '0',
        NotEqual = '1',
        Greater = '2',
        GreaterOrEqual = '3',
        Less = '4',
        LessOrEqual = '5',
    }
}
