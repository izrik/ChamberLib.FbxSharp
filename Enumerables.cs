using System;
using System.Collections.Generic;

namespace ChamberLib.FbxSharp
{
    static class Enumerables
    {
        public static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}

