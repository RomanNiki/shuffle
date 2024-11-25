using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            if (_configSorterGroups.shuffleDirections)
            {
                foreach (var item in items)
                {
                    item.Neighbors.Shuffle(_providerRandom);
                }
            }

            var targetGroupSize = _configSorterGroups.groupSize;
            var percent = _configSorterGroups.percent;
            
            _targetGroupsCount = (int)(items.Count * percent / targetGroupSize);
            _targetUseItems = _targetGroupsCount * targetGroupSize;
            
            var extraElementsCount = items.Count - _targetUseItems;
            List<GridItem> potencialStart = new List<GridItem>();
            List<GridItem> usedItems = new List<GridItem>();
            List<Color> generateColors = GenerateColors(_targetGroupsCount + 1);
            HashSet<GridItem> canNotUseAsStart = new HashSet<GridItem>();
            
            potencialStart.AddRange(items);

            for (int i = 0; i < extraElementsCount; i++)
            {
                var itemToRemove = items[i];
                usedItems.Add(itemToRemove);
                potencialStart.Remove(itemToRemove);
                itemToRemove.SetColor(Color.black);
            }

            if (_configSorterGroups.shuffleStartValue)
            {
                potencialStart.Shuffle(_providerRandom);
            }

            int err = 0;
            List<GridItem> island = new List<GridItem>();
            int islandCount = 0;
            var ind = 0;
            while (usedItems.Count < _targetUseItems && potencialStart.Count >= targetGroupSize)
            {
                if (err >= _targetUseItems * _targetUseItems + _targetUseItems)
                {
                    throw new StackOverflowException($"at index {island[0].gridPosition}");
                }

                err++;
                island.Clear();
                var currentItem = potencialStart[ind];

                bool lastPlace = false;
                foreach (var gridItem in potencialStart)
                {
                    if (canNotUseAsStart.Contains(gridItem))
                    {
                        continue;
                    }
                    island.Add(gridItem);
                    
                    bool willCreateEmpty =
                        await WillCreateEmptySpace(gridItem, items, usedItems, island, targetGroupSize);

                    if (willCreateEmpty)
                    {
                        canNotUseAsStart.Add(gridItem);
                    }

                    if (island.Count == targetGroupSize)
                    {
                        lastPlace = true;
                    }
                    else
                    {
                        island.Clear();
                    }

                    if (!willCreateEmpty)
                    {
                        currentItem = gridItem;
                        break;
                    }
                }

                if (!lastPlace)
                {
                    if (!await TryCreateIsland(currentItem, targetGroupSize, items, usedItems, island))
                    {
                        ind++;
                        potencialStart.Remove(currentItem);
                        continue;
                    }
                }

                ind = 0;

                var color = generateColors[islandCount];
                islandCount++;

                foreach (var islandItem in island)
                {
                    usedItems.Add(islandItem);
                    potencialStart.Remove(islandItem);
                    islandItem.SetColor(color);
                }

                await UniTask.WaitUntil(() => Input.anyKeyDown);
            }

            Debug.Log("Finished sorting");


            return potencialStart;
        }

        private async UniTask<bool> TryCreateIsland(GridItem currentItem, int targetGroupSize, List<GridItem> items,
            List<GridItem> usedItems, List<GridItem> island)
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

                    if (island.Count == targetGroupSize)
                    {
                        return true;
                    }
                }
            }

            return island.Count == targetGroupSize;
        }

        private async UniTask<bool> WillCreateEmptySpace(GridItem currentItem, List<GridItem> gridItems,
            List<GridItem> usedItems, List<GridItem> currentIsland, int targetGroupSize)
        {
            var simulatedUsedItems = new HashSet<GridItem>(usedItems);
            simulatedUsedItems.UnionWith(currentIsland);
            simulatedUsedItems.Add(currentItem);

            var remainingItems = gridItems.Except(simulatedUsedItems).ToList();
            var visited = new HashSet<GridItem>();

            var colr = currentItem.GetColor();
           // currentItem.SetColor(Color.blue);
            bool candidateToExit = false;
          //  await UniTask.WaitUntil(() => Input.anyKeyDown);

            List<GridItem> group = new List<GridItem>();
            List<GridItem> allGroups = new List<GridItem>();
            List<GridItem> itemsToGroup = new List<GridItem>();
            bool foundGroupToIsland = false;

            foreach (var item in currentItem.Neighbors)
            {
                if (visited.Contains(item) || !remainingItems.Contains(item)) continue;
                group.Clear();
                var groupSize = await GetConnectedGroupSize(item, remainingItems, visited, group);
                allGroups.AddRange(group);
            
                if (!candidateToExit && (groupSize + currentIsland.Count) % targetGroupSize != 0)
                {
                    currentItem.SetColor(colr);
                    candidateToExit = true;
                }

                if (!candidateToExit && !foundGroupToIsland && groupSize == targetGroupSize - currentIsland.Count)
                {
                    itemsToGroup.AddRange(allGroups);
                    foundGroupToIsland = true;
                }

                if (group.Count == 1 && currentIsland.Count + 1 < targetGroupSize)
                {
                    foundGroupToIsland = true;
                    currentIsland.AddRange(group);
                }
            }

            if (candidateToExit)
            {
                if (allGroups.Count == targetGroupSize - currentIsland.Count)
                {
                    Debug.Log("created island force big");
                    currentIsland.AddRange(allGroups);
                    usedItems.AddRange(allGroups);
                    return false;
                }

                return true;
            }

            if (itemsToGroup.Count > 0)
            {
                Debug.Log("created island force");
                currentIsland.AddRange(itemsToGroup);
                usedItems.AddRange(itemsToGroup);
                usedItems.Add(currentItem);
            }

            return false;
        }

        private static async Task DebugGroupSize(GridItem currentItem, List<GridItem> currentIsland,
            List<GridItem> group)
        {
            foreach (var i in currentIsland)
                i.SetColor(Color.red);
            currentItem.SetColor(Color.cyan);

            foreach (var i in group)
            {
                i.SetColor(Color.magenta);
            }

            await UniTask.WaitUntil(() => Input.anyKeyDown);

            foreach (var i in currentIsland)
                i.SetColor(Color.gray);
            currentItem.SetColor(Color.gray);

            foreach (var i in group)
            {
                i.SetColor(Color.gray);
            }
        }

        private async UniTask<int> GetConnectedGroupSize(GridItem startItem, List<GridItem> remainingItems,
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

            return size;
        }
    }

    [Serializable]
    public struct ConfigSorterGroups
    {
        public int groupSize;
        public float percent;
        public bool shuffleDirections;
        public bool shuffleStartValue;
    }
}