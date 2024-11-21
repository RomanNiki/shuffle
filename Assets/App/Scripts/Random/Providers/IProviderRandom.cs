namespace App.Scripts.Random.Providers
{
    public interface IProviderRandom
    {
        float Value { get; }
        int Range(int i, int usedItemsCount);
    }
}