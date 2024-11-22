using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using App.Scripts.Generator.Handlers;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace App.Scripts.Generator.Services
{
    public class ServiceSorter : IServiceSorter, IAsyncStartable
    {
        private readonly IEnumerable<IHandlerSorter> _handlerSorters;
        private readonly ConfigServiceSorter _configServiceSorter;

        public ServiceSorter(IEnumerable<IHandlerSorter> handlerSorters, ConfigServiceSorter configServiceSorter)
        {
            _handlerSorters = handlerSorters;
            _configServiceSorter = configServiceSorter;
        }

        public UniTask<List<GridItem>> Sort(List<GridItem> groups)
        {
            foreach (var handlerSorter in _handlerSorters)
            {
                return handlerSorter.Sort(groups);
            }

            return UniTask.FromResult(groups);
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            var list = new List<GridItem>();
            var gridItemPrefab = _configServiceSorter.prefabItem;
            var grid = _configServiceSorter.grid;

            for (int i = 0; i < _configServiceSorter.gridSize; i++)
            {
                for (int j = 0; j < _configServiceSorter.gridSize; j++)
                {
                    var gridItem = Object.Instantiate(gridItemPrefab, grid);
                    list.Add(gridItem);
                    gridItem.Setup(_configServiceSorter.defaultColor, new Vector2Int(i, j));
                }
            }

            foreach (var gridItem in list)
            {
                gridItem.Neighbors = GetNeighbors(gridItem, list).ToList();
            }

            await Sort(list);
        }

        private IEnumerable<GridItem> GetNeighbors(GridItem item, List<GridItem> items)
        {
            foreach (var direction in Directions)
            {
                var itemGridPosition = item.gridPosition;
                var neighborPosition = itemGridPosition + direction;
                if (IsWithinBounds(neighborPosition, (int)Mathf.Sqrt(items.Count)))
                {
                    GridItem itemToPlace = items.Find(i => i.gridPosition == neighborPosition);
                    yield return itemToPlace;
                }
            }
        }

        static bool IsWithinBounds(Vector2Int pos, int matrixSize)
        {
            return pos.x >= 0 && pos.x < matrixSize && pos.y >= 0 && pos.y < matrixSize;
        }

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left,
        };
    }

    [Serializable]
    public struct ConfigServiceSorter
    {
        public GridItem prefabItem;
        public RectTransform grid;
        public Color defaultColor;
        public int gridSize;
    }
}