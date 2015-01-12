using System.Collections;
using System.Collections.Generic;

namespace NetDist.Core.Extensions
{
    public static class DictionaryExtensionMethods
    {
        public static object GetSyncRoot<T1,T2>(this IDictionary<T1,T2> dict)
        {
            return ((IDictionary)dict).SyncRoot;
        }
    }
}
