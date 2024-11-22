using System;
using System.Collections.Generic;
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


        int GetIndex(int i, int j, int size)
        {
            return i * size + j;
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
            while (usedItems.Count < _targetUseItems && freeItems.Count >= targetGroupSize)
            {
                if (err >= _targetUseItems * _targetUseItems + _targetUseItems)
                {
                    throw new StackOverflowException($"at index {island[0].gridPosition}");
                }

                err++;
                island.Clear();
                if (_configSorterGroups.shuffleStartValue)
                {
                    freeItems.Shuffle(_providerRandom);
                }

                var currentItem = freeItems[0];

                if (!await TryCreateIsland(currentItem, targetGroupSize, items, usedItems, island))
                {
                    continue;
                }

                var color = generateColors[islandCount];
                islandCount++;
                
                foreach (var islandItem in island)
                {
                    //      
                    usedItems.Add(islandItem);
                    freeItems.Remove(islandItem);
                    islandItem.SetColor(color);
                }
                await UniTask.WaitUntil(() => Input.anyKeyDown);
            }

            Debug.Log("Finished sorting");


            return freeItems;
        }

        private async UniTask<bool> TryCreateIsland(GridItem currentItem, int targetGroupSize, List<GridItem> gridItems,
            List<GridItem> usedItems, List<GridItem> island)
        {
            Stack<GridItem> stack = new Stack<GridItem>();
            HashSet<GridItem> visited = new HashSet<GridItem> { currentItem };
            stack.Push(currentItem);
            island.Add(currentItem);

            while (stack.Count > 0 && island.Count < targetGroupSize)
            {
                var item = stack.Pop();
                Vector2Int itemPosition = item.gridPosition;

                if (_configSorterGroups.shuffleDirections)
                {
                    _directions.Shuffle(_providerRandom);
                }

                foreach (var direction in _directions)
                {
                    Vector2Int neighborPosition = itemPosition + direction;

                    if (IsWithinBounds(neighborPosition, (int)Mathf.Sqrt(gridItems.Count)))
                    {
                        GridItem neighborItem = gridItems.Find(i => i.gridPosition == neighborPosition);

                        if (neighborItem != null && !visited.Contains(neighborItem) &&
                            !usedItems.Contains(neighborItem))
                        {
                            visited.Add(neighborItem);
                            if (!await WillCreateEmptySpace(neighborItem, gridItems, usedItems, island,
                                    targetGroupSize))
                            {
                                stack.Push(neighborItem);
                                island.Add(neighborItem);
                            }
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
            List<GridItem> usedItems,
            List<GridItem> currentIsland, int targetGroupSize)
        {
            int i = 0;
            int currentIslandSize = currentIsland.Count;
            HashSet<GridItem> visitedLocal = new HashSet<GridItem>() { currentItem };
            visitedLocal.AddRange(currentIsland);
            int approvedDirections = 0;
            foreach (var direction in _directions)
            {
                var neighborPosition = currentItem.gridPosition + direction;

                if (IsWithinBounds(neighborPosition, (int)Mathf.Sqrt(gridItems.Count)))
                {
                    GridItem neighborItem = gridItems.Find(i => i.gridPosition == neighborPosition);

                    if (neighborItem != null && !usedItems.Contains(neighborItem) &&
                        visitedLocal.Add(neighborItem))
                    {
                        if (!await WillCreateEmptySpaceInternal(neighborItem, gridItems, usedItems, visitedLocal,
                                targetGroupSize, currentIslandSize))
                        {
                            Debug.Log("Dont create empty space");
                            currentIslandSize++;
                            approvedDirections++;
                        }
                    }
                    else if (visitedLocal.Contains(neighborItem) || usedItems.Contains(neighborItem))
                    {
                        Debug.Log("Was already visited");
                        approvedDirections++;
                    }
                }
                else
                {
                    Debug.Log("Out of bounds when finding empty space");
                    approvedDirections++;
                }
            }


            return approvedDirections < _directions.Length;
        }

        private async UniTask<bool> WillCreateEmptySpaceInternal(GridItem neighborItem, List<GridItem> gridItems,
            List<GridItem> usedItems, HashSet<GridItem> visited, int targetGroupSize, int islandSize)
        {
            Stack<GridItem> stack = new Stack<GridItem>();
            HashSet<GridItem> visitedLocal = new HashSet<GridItem>(){neighborItem};
            stack.Push(neighborItem);

            int island = 1;
            visitedLocal.AddRange(visited);
            while (stack.Count > 0)
            {
                var item = stack.Pop();
                Vector2Int itemPosition = item.gridPosition;

                foreach (var direction in _directions)
                {
                    Vector2Int neighborPosition = itemPosition + direction;

                    if (IsWithinBounds(neighborPosition, (int)Mathf.Sqrt(gridItems.Count)))
                    {
                        GridItem currentItem = gridItems.Find(i => i.gridPosition == neighborPosition);

                        if (currentItem != null && !visitedLocal.Contains(currentItem) &&
                            !usedItems.Contains(currentItem))
                        {
                            stack.Push(currentItem);
                            visitedLocal.Add(currentItem);
                            island++;
                        }
                    }
                }
            }

            if (island % targetGroupSize == 0)
            {
                Debug.Log("island % targetGroupSize == 0");
                return false;
            }
            
            if ((island + islandSize) % targetGroupSize == 0)
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

        private static Vector2Int[] _directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left,
        };

        static bool IsWithinBounds(Vector2Int pos, int matrixSize)
        {
            return pos.x >= 0 && pos.x < matrixSize && pos.y >= 0 && pos.y < matrixSize;
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