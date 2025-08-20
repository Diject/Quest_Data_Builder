using System.Collections.Concurrent;

namespace Quest_Data_Builder.Extentions
{
    public static class ConcurrentBagExtentions
    {
        // https://stackoverflow.com/a/31144811
        public static void AddRange<T>(this ConcurrentBag<T> @this, IEnumerable<T> toAdd)
        {
            foreach (var element in toAdd)
            {
                @this.Add(element);
            }
        }
    }
}
