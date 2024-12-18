using System.Collections.Generic;
using App.Scripts.Random.Providers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Scripts.Generator.Handlers
{
    public class HandlerSorterNotMatch : IHandlerSorter
    {
        public bool Shuffle(List<GridItem> items, int index)
        {
            var providerRandom = new ProviderRandom(index);
            providerRandom.Initialize();
            items.Shuffle(providerRandom);

            var result = new List<GridItem>();
            if (BuildSolution(items, result))
            {
                ApplyColors(result);
                items.AddRange(result);
                return true;
            }

            return false;
        }
        
        private static void ApplyColors(List<GridItem> result)
        {
            byte i = 0;
            foreach (var item in result)
            {
                i++;
                item.SetColor(new Color32(i, i, i, 255));
            }
        }

        private bool BuildSolution(List<GridItem> remainingItems, List<GridItem> currentResult)
        {
            if (remainingItems.Count == 0)
            {
                return true;
            }
            
            for (int i = 0; i < remainingItems.Count; i++)
            {
                var candidate = remainingItems[i];

                if (IsSatisfied(currentResult, candidate))
                {
                    currentResult.Add(candidate);
                    var nextRemaining = new List<GridItem>(remainingItems);
                    nextRemaining.RemoveAt(i);
                    
                    if (BuildSolution(nextRemaining, currentResult))
                    {
                        return true;
                    }
                    
                    currentResult.Remove(candidate);
                }
            }

            return false;
        }
        
        private static bool IsSatisfied(List<GridItem> result, GridItem candidate)
        {
            if (result.Count > 0)
            {
                GridItem lastAdded = result[^1];
                var neighbours = candidate.Neighbors;
                if (neighbours.Contains(lastAdded))
                {
                    return false;
                }
            }

            return true;
        }

        public async UniTask<List<GridItem>> Sort(List<GridItem> items)
        {
            for (int i = 0; i < 10000; i++)
            {
                await UniTask.NextFrame();
                await UniTask.NextFrame();
                Debug.Log(i);
                Shuffle(new List<GridItem>(items), i);
            }

           
            return items;
        }
    }
}