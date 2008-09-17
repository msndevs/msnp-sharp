using System;
using System.Collections;
using System.Collections.Generic;

namespace MSNPSharp
{
    /*
     * Those classes are needed because Dictionary/Hashtable doesn't guarantee
     * that the Add order will be the same loop order (at least on mono)
     */

    public class StrKeyValuePair
    {
        string key;
        string val;

        public StrKeyValuePair(string key, string val)
        {
            this.key = key;
            this.val = val;
        }

        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                key = value;
            }
        }

        public string Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
            }
        }
    }

    public class StrDictionary : IEnumerable
    {
        List<StrKeyValuePair> content;

        public StrDictionary()
        {
            content = new List<StrKeyValuePair>();
        }

        /*
         * I know, foreach sux, but for this use case it's ok since
         * we will only have no more than 10 items
         */
        public string this[string key]
        {
            get
            {
                foreach (StrKeyValuePair kvp in content)
                    if (kvp.Key == key)
                        return kvp.Value;

                return null;
            }
            set
            {
                bool found = false;

                foreach (StrKeyValuePair kvp in content)
                {
                    if (kvp.Key == key)
                    {
                        kvp.Value = value;
                        found = true;
                    }
                }

                if (!found)
                    Add(key, value);
            }
        }

        public void Add(string key, string val)
        {
            StrKeyValuePair kvp = new StrKeyValuePair(key, val);

            content.Add(kvp);
        }

        public bool ContainsKey(string key)
        {
            foreach (StrKeyValuePair kvp in content)
                if (kvp.Key == key)
                    return true;

            return false;
        }

        public void Clear()
        {
            content.Clear();
        }

        public IEnumerator GetEnumerator()
        {
            return content.GetEnumerator();
        }
    }
};
