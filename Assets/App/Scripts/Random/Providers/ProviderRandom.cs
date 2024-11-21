using VContainer.Unity;

namespace App.Scripts.Random.Providers
{
    public class ProviderRandom : IProviderRandom, IInitializable
    {
        private readonly int _seed;

        public ProviderRandom(int seed = 0)
        {
            _seed = seed;
        }

        public float Value => UnityEngine.Random.value;
        
        public void Initialize()
        {
            UnityEngine.Random.InitState(_seed);
        }

        public int Range(int i, int usedItemsCount)
        {
            return UnityEngine.Random.Range(i, usedItemsCount);
        }
    }
}