using System;
using VContainer.Unity;

namespace App.Scripts.Random.Providers
{
    public class ProviderRandom : IProviderRandom, IInitializable
    {
        private readonly System.Random _random;

        public ProviderRandom(int seed = -1)
        {
            int seed1;
            if (seed == -1)
            {
                _random = new System.Random(DateTime.Now.Millisecond);
                return;
            }
          
            seed1 = seed;
            _random = new System.Random(seed1);
        }

        public float Value => (float)_random.NextDouble();
        
        public void Initialize()
        {
        }

        public int Range(int i, int usedItemsCount)
        {
            return _random.Next(i, usedItemsCount);
        }
    }
}