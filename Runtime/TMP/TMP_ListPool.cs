using System.Collections.Generic;


namespace TMPro
{
    internal static class TMP_ListPool<T>
    {
        private static readonly TMP_ObjectPool<List<T>> s_ListPool = new(null, l => l.Clear());

        public static List<T> Get()
        {
            return s_ListPool.Get();
        }

        public static void Release(List<T> toRelease)
        {
            s_ListPool.Release(toRelease);
        }
    }
}