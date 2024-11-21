using System;
using System.Collections.Generic;
using App.Scripts.Random.Providers;
using Unity.VisualScripting;
using UnityEngine;

namespace App.Scripts.Generator.Handlers
{
    public class HandlerSorterGroups : IHandlerSorter
    {
        private readonly ConfigSorterGroups _configSorterGroups;
        private readonly IProviderRandom _providerRandom;
        private int _targetGroupsCount;

        public HandlerSorterGroups(ConfigSorterGroups configSorterGroups, IProviderRandom providerRandom)
        {
            _configSorterGroups = configSorterGroups;
            _providerRandom = providerRandom;
        }

        // var index = i * matrixSize + j;

        int GetIndex(int i, int j, int size)
        {
            return i * size + j;
        }

        public List<GridItem> Sort(List<GridItem> items)
        {
            var targetGroupSize = _configSorterGroups.groupSize;
            var matrixSize = (int)Mathf.Sqrt(items.Count);
            var percent = _configSorterGroups.percent;
            _targetGroupsCount = (int)(items.Count * percent / targetGroupSize);
            var targetUseItems = _targetGroupsCount * targetGroupSize;

            List<GridItem> freeItems = new List<GridItem>();
            List<GridItem> usedItems = new List<GridItem>();
            freeItems.AddRange(items);

            int islandCount = 0;
            int err = 0;
            List<GridItem> island = new List<GridItem>();
            while (usedItems.Count < targetUseItems && freeItems.Count > targetGroupSize)
            {
                if (err >= targetUseItems * targetUseItems + targetUseItems)
                {
                    throw new StackOverflowException($"at index {island[0].gridPosition}");
                }

                err++;
                island.Clear();
                freeItems.Shuffle();
                var currentItem = freeItems[0];

                if (!TryCreateIsland(currentItem, targetGroupSize, items, usedItems, island))
                {
                    continue;
                }

                var color = new Color(_providerRandom.Value, _providerRandom.Value, _providerRandom.Value, 1);

                islandCount++;
                foreach (var islandItem in island)
                {
                    usedItems.Add(islandItem);
                    freeItems.Remove(islandItem);
                    islandItem.SetColor(color);
                }
            }


            return freeItems;
        }

        private bool TryCreateIsland(GridItem currentItem, int targetGroupSize, List<GridItem> gridItems,
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

                _directions.Shuffle();

                foreach (var direction in _directions)
                {
                    Vector2Int neighborPosition = itemPosition + direction;

                    if (IsWithinBounds(neighborPosition, (int)Mathf.Sqrt(gridItems.Count)))
                    {
                        GridItem neighborItem = gridItems.Find(i => i.gridPosition == neighborPosition);

                        if (neighborItem != null && !visited.Contains(neighborItem) &&
                            !usedItems.Contains(neighborItem))
                        {
                            if (!WillCreateEmptySpace(neighborItem, gridItems, usedItems, visited, targetGroupSize))
                            {
                                stack.Push(neighborItem);
                                island.Add(neighborItem);
                                visited.Add(neighborItem);
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

        private bool WillCreateEmptySpace(GridItem neighborItem, List<GridItem> gridItems, List<GridItem> usedItems,
            HashSet<GridItem> visited, int targetGroupSize)
        {
            HashSet<GridItem> visitedLocal = new HashSet<GridItem> { neighborItem };
            visitedLocal.AddRange(visited);
            int c = 0;
            foreach (var direction in _directions)
            {
                var neighborPosition = neighborItem.gridPosition + direction;

                if (IsWithinBounds(neighborPosition, (int)Mathf.Sqrt(gridItems.Count)))
                {
                    GridItem target = gridItems.Find(i => i.gridPosition == neighborPosition);

                    if (target != null && (!usedItems.Contains(target) || usedItems.Count <= targetGroupSize) &&
                        !visitedLocal.Contains(target))
                    {
                        if (!WillCreateEmptySpaceInternal(target, gridItems, usedItems, visitedLocal, targetGroupSize))
                        {
                            c++;
                        }
                    }
                    else if (visitedLocal.Contains(target) || (usedItems.Contains(target) || usedItems.Count <= targetGroupSize))
                    {
                        c++;
                    }
                }
                else
                {
                    c++;
                }
            }

            return c < _directions.Length;
        }

        private bool WillCreateEmptySpaceInternal(GridItem neighborItem, List<GridItem> gridItems,
            List<GridItem> usedItems, HashSet<GridItem> visited, int targetGroupSize)
        {
            Stack<GridItem> stack = new Stack<GridItem>();
            stack.Push(neighborItem);

            int island = 1;

            HashSet<GridItem> visitedLocal = new HashSet<GridItem> { neighborItem };
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
                            (!usedItems.Contains(currentItem) || usedItems.Count <= targetGroupSize))
                        {
                            stack.Push(currentItem);
                            visitedLocal.Add(currentItem);
                            island++;
                        }
                    }
                }
            }

            if (island / targetGroupSize > 1)
            {
                return false;
            }

            if (island % targetGroupSize < _targetGroupsCount)
            {
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
    }
}