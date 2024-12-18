using System;
using System.Collections.Generic;
using System.Linq;
using App.Scripts.Random.Providers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace App.Scripts.Generator.Handlers
{
    public class HandlerSorterGroups : IHandlerSorter
    {
        private readonly ConfigSorterGroups _configSorterGroups;
        private readonly IProviderRandom _providerRandom;
        private int _targetGroupsCount;
        private int _targetUseItems;
        private List<Color> _generateColors;

        public HandlerSorterGroups(ConfigSorterGroups configSorterGroups, IProviderRandom providerRandom)
        {
            _configSorterGroups = configSorterGroups;
            _providerRandom = providerRandom;
        }

        List<Color> GenerateColors(int count)
        {
            HashSet<Color> uniqueColors = new HashSet<Color>();

            while (uniqueColors.Count < count)
            {
                Color color = new Color(_providerRandom.Value, _providerRandom.Value, _providerRandom.Value);
                uniqueColors.Add(color);
            }

            return new List<Color>(uniqueColors);
        }

        public async UniTask<List<GridItem>> Sort(List<GridItem> items)
        {
            ShuffleDirections(items);

            var targetGroupSize = _configSorterGroups.groupSize;
            var percent = _configSorterGroups.percent;

            _targetGroupsCount = (int)(items.Count * percent / targetGroupSize);
            _targetUseItems = _targetGroupsCount * targetGroupSize;
            _generateColors = GenerateColors(_targetGroupsCount + 1);

            List<GridItem> potentialStart = new List<GridItem>();
            HashSet<GridItem> usedItems = new HashSet<GridItem>();
            HashSet<GridItem> canNotUseAsStart = new HashSet<GridItem>();
            List<GridItem> currentIsland = new List<GridItem>();

            potentialStart.AddRange(items);

            ExcludeExtraItems(items, usedItems, potentialStart);

            int islandCount = 0;

            while (usedItems.Count < _targetUseItems && potentialStart.Count > 0)
            {
                currentIsland.Clear();
                var currentItem = potentialStart[0];

                currentItem = await CanUseStartItem(items, potentialStart, canNotUseAsStart,
                    currentIsland,
                    usedItems, targetGroupSize, currentItem);

                if (currentIsland.Count != targetGroupSize)
                {
                    if (!await TryCreateIsland(currentItem, targetGroupSize, items, usedItems, currentIsland))
                    {
                        potentialStart.Remove(currentItem);
                        continue;
                    }
                }

                islandCount++;

                ApplyIslandToWorld(currentIsland, usedItems, potentialStart, islandCount);

              //  await UniTask.WaitUntil(() => Input.anyKeyDown);
            }

            Debug.Log("Finished sorting");


            return potentialStart;
        }

        private void ExcludeExtraItems(List<GridItem> items, HashSet<GridItem> usedItems, List<GridItem> potentialStart)
        {
            var extraElementsCount = items.Count - _targetUseItems;
            for (int i = 0; i < extraElementsCount; i++)
            {
                var itemToRemove = items[i];
                usedItems.Add(itemToRemove);
                potentialStart.Remove(itemToRemove);
            }
        }

        private void ShuffleDirections(List<GridItem> items)
        {
            if (_configSorterGroups.shuffleDirections)
            {
                foreach (var item in items)
                {
                    item.Neighbors.Shuffle(_providerRandom);
                }
            }
        }

        private async UniTask<GridItem> CanUseStartItem(List<GridItem> items,
            List<GridItem> potentialStart, HashSet<GridItem> canNotUseAsStart, List<GridItem> currentIsland,
            IReadOnlyCollection<GridItem> usedItems, int targetGroupSize, GridItem currentItem)
        {
            foreach (var gridItem in potentialStart)
            {
                if (canNotUseAsStart.Contains(gridItem))
                {
                    continue;
                }

                currentIsland.Add(gridItem);

                bool willCreateEmpty =
                    await WillCreateEmptySpace(gridItem, items, usedItems, currentIsland, targetGroupSize);

                if (willCreateEmpty)
                {
                    canNotUseAsStart.Add(gridItem);
                }

                if (currentIsland.Count == targetGroupSize)
                {
                    break;
                }

                currentIsland.Clear();

                if (!willCreateEmpty)
                {
                    currentItem = gridItem;
                    break;
                }
            }

            return currentItem;
        }

        private void ApplyIslandToWorld(List<GridItem> currentIsland, HashSet<GridItem> usedItems,
            List<GridItem> potentialStart, int i)
        {
            var color = _generateColors[i];
            foreach (var islandItem in currentIsland)
            {
                usedItems.Add(islandItem);
                potentialStart.Remove(islandItem);
                islandItem.SetColor(color);
            }
        }

        private async UniTask<bool> TryCreateIsland(GridItem currentItem, int targetGroupSize, List<GridItem> items,
            HashSet<GridItem> usedItems, List<GridItem> island)
        {
            Stack<GridItem> stack = new Stack<GridItem>();
            HashSet<GridItem> visited = new HashSet<GridItem> { currentItem };
            stack.Push(currentItem);
            island.Add(currentItem);

            while (stack.Count > 0 && island.Count < targetGroupSize)
            {
                var item = stack.Pop();

                foreach (var itemToPlace in item.Neighbors)
                {
                    if (!visited.Contains(itemToPlace) && !usedItems.Contains(itemToPlace))
                    {
                        await TrySetIslandItem(targetGroupSize, items, usedItems, island, itemToPlace, visited, stack);
                    }

                    if (island.Count == targetGroupSize)
                    {
                        return true;
                    }
                }
            }

            return island.Count == targetGroupSize;
        }

        private async UniTask TrySetIslandItem(int targetGroupSize, List<GridItem> items,
            IReadOnlyCollection<GridItem> usedItems,
            List<GridItem> island, GridItem itemToPlace,
            HashSet<GridItem> visited, Stack<GridItem> stack)
        {
            island.Add(itemToPlace);

            if (!await WillCreateEmptySpace(itemToPlace, items, usedItems, island, targetGroupSize))
            {
                visited.Add(itemToPlace);
                stack.Push(itemToPlace);
            }
            else
            {
                island.Remove(itemToPlace);
            }
        }

        private async UniTask<bool> WillCreateEmptySpace(GridItem currentItem, List<GridItem> gridItems,
            IReadOnlyCollection<GridItem> usedItems, List<GridItem> currentIsland, int targetGroupSize)
        {
            HashSet<GridItem> simulatedUsedItems = new HashSet<GridItem>(usedItems);
            simulatedUsedItems.UnionWith(currentIsland);
            simulatedUsedItems.Add(currentItem);
            
            currentItem.SetColor(Color.blue);

          //  await UniTask.WaitUntil(() => Input.anyKeyDown);

            List<GridItem> remainingItems = gridItems.Except(simulatedUsedItems).ToList();
            List<GridItem> group = new List<GridItem>();
            HashSet<GridItem> visited = new HashSet<GridItem>();
            HashSet<GridItem> allGroups = new HashSet<GridItem>();
            HashSet<GridItem> itemsToGroup = new HashSet<GridItem>();
            bool foundGroupToIsland = false;
            bool candidateToExit = false;

            foreach (var item in currentItem.Neighbors)
            {
                if (visited.Contains(item) || !remainingItems.Contains(item))
                    continue;
                
                
                group.Clear();
                var groupSize = await GetConnectedGroupSize(item, remainingItems, visited, group);
                allGroups.AddRange(group);

                if (!candidateToExit && (groupSize + currentIsland.Count) % targetGroupSize != 0)
                {
                    candidateToExit = true;
                }

                if (!candidateToExit && !foundGroupToIsland && groupSize == targetGroupSize - currentIsland.Count)
                {
                    itemsToGroup.AddRange(allGroups);
                    foundGroupToIsland = true;
                }

                if (!candidateToExit && !foundGroupToIsland && groupSize < targetGroupSize)
                {
                    return true;
                }
            }
            

            if (candidateToExit)
            {
                return TryConnectGroupsAround(currentIsland, targetGroupSize, allGroups);
            }

            TryAddGroupToIsland(currentItem, currentIsland, itemsToGroup);

            return false;
        }

        private static void TryAddGroupToIsland(GridItem currentItem,
            List<GridItem> currentIsland, HashSet<GridItem> itemsToGroup)
        {
            if (itemsToGroup.Count > 0)
            {
                currentIsland.AddRange(itemsToGroup);
                currentIsland.Add(currentItem);
            }
        }

        private static bool TryConnectGroupsAround(List<GridItem> currentIsland,
            int targetGroupSize, HashSet<GridItem> allGroups)
        {
            if (allGroups.Count == targetGroupSize - currentIsland.Count)
            {
                currentIsland.AddRange(allGroups);
                return false;
            }

            return true;
        }

        private UniTask<int> GetConnectedGroupSize(GridItem startItem, List<GridItem> remainingItems,
            HashSet<GridItem> visited,
            List<GridItem> island)
        {
            var stack = new Stack<GridItem>();
            stack.Push(startItem);
            visited.Add(startItem);

            int size = 0;

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                size++;
                island.Add(item);

                foreach (var neighbor in item.Neighbors)
                {
                    if (neighbor != null && remainingItems.Contains(neighbor) && visited.Add(neighbor))
                    {
                        stack.Push(neighbor);
                    }
                }
            }

            return UniTask.FromResult(size);
        }
    }

    [Serializable]
    public struct ConfigSorterGroups
    {
        public int groupSize;
        public float percent;
        public bool shuffleDirections;
    }
}