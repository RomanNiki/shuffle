using System;
using System.Collections;

namespace App.Scripts.Generator
{
    public static class ListExtension
    {
        public static void Shuffle(this IList list)
        {
            var random = new System.Random(DateTime.Now.Millisecond);

            for (int i = list.Count - 1; i >= 1; i--)
            {
                int j = random.Next(i + 1);
                (list[j], list[i]) = (list[i], list[j]);
            }
        }
    }
}