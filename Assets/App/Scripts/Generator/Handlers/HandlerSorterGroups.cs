using System;
using System.Collections.Generic;
using System.Linq;
using App.Scripts.Random.Providers;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
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
            var targetGroupSize = _configSorterGroups.groupSize;
            var matrixSize = (int)Mathf.Sqrt(items.Count);
            var percent = _configSorterGroups.percent;
            _targetGroupsCount = (int)(items.Count * percent / targetGroupSize);
            _targetUseItems = _targetGroupsCount * targetGroupSize;

            List<GridItem> freeItems = new List<GridItem>();
            List<GridItem> usedItems = new List<GridItem>();
            List<Color> generateColors = GenerateColors(_targetGroupsCount + 1);
            freeItems.AddRange(items);

            int err = 0;
            List<GridItem> island = new List<GridItem>();
            int islandCount = 0;
            var ind = 0;
            while (usedItems.Count < _targetUseItems && freeItems.Count >= targetGroupSize)
            {
                if (err >= _targetUseItems * _targetUseItems + _targetUseItems)
                {
                    throw new StackOverflowException($"at index {island[0].gridPosition}");
                }

                err++;
                island.Clear();
                var currentItem = freeItems[ind];

                if (_configSorterGroups.shuffleStartValue)
                {
                    freeItems.Shuffle(_providerRandom);
                }

                bool lastPlace = false;
                foreach (var gridItem in freeItems)
                {
                    bool willCreateEmpty =
                        await WillCreateEmptySpace(gridItem, items, usedItems, island, targetGroupSize);
                    if (island.Count == targetGroupSize)
                    {
                        lastPlace = true;
                    }
                    else
                    {
                        island.Clear();
                    }

                    if (!willCreateEmpty || lastPlace)
                    {
                        currentItem = gridItem;
                        break;
                    }
                }

                if (lastPlace || !await TryCreateIsland(currentItem, targetGroupSize, items, usedItems, island))
                {
                    ind++;
                    continue;
                }

                ind = 0;

                var color = generateColors[islandCount];
                islandCount++;

                foreach (var islandItem in island)
                {
                    usedItems.Add(islandItem);
                    freeItems.Remove(islandItem);
                    islandItem.SetColor(color);
                }

                await UniTask.WaitUntil(() => Input.anyKeyDown);
            }

            Debug.Log("Finished sorting");


            return freeItems;
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

                if (_configSorterGroups.shuffleDirections)
                {
                    item.Neighbors.Shuffle(_providerRandom);
                }

                foreach (var itemToPlace in item.Neighbors)
                {
                    if (!visited.Contains(itemToPlace) && !usedItems.Contains(itemToPlace))
                    {
                        visited.Add(itemToPlace);
                        if (!await WillCreateEmptySpace(itemToPlace, items, usedItems, island, targetGroupSize))
                        {
                            stack.Push(itemToPlace);
                            island.Add(itemToPlace);
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

            int groupsSize = 0;
            List<GridItem> group = new List<GridItem>();
            foreach (var item in remainingItems)
            {
                if (visited.Contains(item)) continue;

                group.Clear();
                var groupSize = GetConnectedGroupSize(item, remainingItems, visited, group);
                groupsSize += groupSize;


                if (groupSize == targetGroupSize - currentIsland.Count - 1)
                {
                    currentIsland.AddRange(group);
                    usedItems.AddRange(group);
                    return false;
                }

                if (groupSize < targetGroupSize)
                {
                    return true;
                }
            }

            var totalIslands = groupsSize / targetGroupSize;

            var targetTotalIslands = (_targetUseItems - (usedItems.Count + groupsSize + 1)) / targetGroupSize;
            if (totalIslands < targetTotalIslands)
            {
                return true;
            }

            return false;
        }

        private int GetConnectedGroupSize(GridItem startItem, List<GridItem> remainingItems, HashSet<GridItem> visited,
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


        private async UniTask<bool> WillCreateEmptySpaceInternal(GridItem neighborItem, List<GridItem> gridItems,
            List<GridItem> usedItems, HashSet<GridItem> visited, int targetGroupSize, int islandSize)
        {
            Stack<GridItem> stack = new Stack<GridItem>();
            HashSet<GridItem> visitedLocal = new HashSet<GridItem>() { neighborItem };
            stack.Push(neighborItem);

            int island = 1;
            visitedLocal.AddRange(visited);
            while (stack.Count > 0)
            {
                var item = stack.Pop();

                foreach (var currentItem in item.Neighbors)
                {
                    if (currentItem != null && !visitedLocal.Contains(currentItem) &&
                        !usedItems.Contains(currentItem))
                    {
                        stack.Push(currentItem);
                        visitedLocal.Add(currentItem);
                        island++;
                    }
                }
            }

            if (island % targetGroupSize == 0)
            {
                Debug.Log("island % targetGroupSize == 0");
                return false;
            }

            var spaceLeft = targetGroupSize - islandSize;
            if (island == spaceLeft)
            {
                return false;
            }

            var totalIslands = island / targetGroupSize;

            var targetTotalIslands = (_targetUseItems - (usedItems.Count + island)) / targetGroupSize;
            if (totalIslands >= targetTotalIslands)
            {
                Debug.Log($"Total islands reached: {totalIslands}");
                return false;
            }

            return true;
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