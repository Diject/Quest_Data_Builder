using System.Collections;
using System.Text.RegularExpressions;

namespace Quest_Data_Builder.Extentions
{
    public partial class PairReplacer : IDictionary<string, string>
    {
        private readonly IDictionary<string, string> _dictionary;
        private int _counter = 0;

        /// <summary>
        /// A dictionary that returns the key itself if the key does not exist, or the value of the key if the key exists.
        /// </summary>
        public PairReplacer()
        {
            _dictionary = new Dictionary<string, string>();
        }

        public string this[string key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public ICollection<string> Keys => _dictionary.Keys;
        public ICollection<string> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => _dictionary.IsReadOnly;

        public void Add(string key, string value)
        {
            _dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _dictionary.Add(item);
        }

        /// <summary>
        /// Replaces all quoted words in the string with dummy ones and returns the replaced string. Also adds the replaced words to the dictionary.
        /// </summary>
        /// <param name="str"></param>
        public string AddQuotedString(string str)
        {
            var mathes = FindQuotedRegex().Matches(str);
            foreach (Match match in mathes)
            {
                var value = match.Groups[1].Value;
                var dummyStr = GetDummyString();
                _dictionary.Add(dummyStr, value);
                str = str.Replace(value, dummyStr);
            }
            return str;
        }

        /// <summary>
        /// Get the value of the key, if the key does not exist, return the key itself.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            if (TryGetValue(key, out var value))
            {
                return value;
            }
            return key;
        }

        private string GetDummyString()
        {
            return $"__dummy{_counter++}__";
        }

        public void Clear()
        {
            _dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _dictionary.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return _dictionary.Remove(item);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dictionary.TryGetValue(key, out value!);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        [GeneratedRegex("\"([^\"]*)\"")]
        private static partial Regex FindQuotedRegex();
    }
}
