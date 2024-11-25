using System;
using System.Collections;
using System.Collections.Generic;
using App.Scripts.Random.Providers;

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
        
        public static void Shuffle(this IList list, IProviderRandom providerRandom)
        {
            for (int i = list.Count - 1; i >= 1; i--)
            {
                int j = providerRandom.Range(0, i + 1);
                (list[j], list[i]) = (list[i], list[j]);
            }
        }

        public static void AddRange<T>(this ISet<T> hashSet, IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                hashSet.Add(item);
            }
        }
    }
}