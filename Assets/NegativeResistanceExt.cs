using System.Collections.Generic;
using System.Linq;

namespace negativeResSpace
{
    public static class NegativeResistanceExt
    {
        public static List<List<T>> Chunk<T>(this List<T> list, int chunkSize)
        {
            int taken = 0;
            List<List<T>> chunks = new List<List<T>>();
            while (taken < chunkSize)
            {
                chunks.Add(list.Skip(taken).Take(chunkSize).ToList());
                taken += chunkSize;
            }
            return chunks;
        }
    }
}