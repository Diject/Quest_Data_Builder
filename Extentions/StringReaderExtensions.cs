using System.Reflection;

namespace Quest_Data_Builder.Extentions
{
    public static class StringReaderExtensions
    {
        public static void SetPosition(this StringReader reader, int pos)
        {
            reader.GetType()
                .GetField("_pos", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(reader, pos);
        }

        public static int GetPosition(this StringReader reader)
        {
            return (int)reader.GetType()
                .GetField("_pos", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(reader)!;
        }
    }
}
