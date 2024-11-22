using System;
using System.Collections.Generic;
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

            return UniTask.FromResult(groups) ;
        }

        public void Initialize()
        {
            
        }

        public async UniTask StartAsync(CancellationToken cancellation = new CancellationToken())
        {
            var list = new List<GridItem>();
            var gridItemPrefab = _configServiceSorter.prefabItem;
            var grid = _configServiceSorter.grid;
            
            for (int i = 0; i < _configServiceSorter.gridSize; i++)
            {
                for (int j = 0; j <  _configServiceSorter.gridSize; j++)
                {
                    
                    var gridItem = Object.Instantiate(gridItemPrefab, grid);
                    list.Add(gridItem);
                    gridItem.Setup(_configServiceSorter.defaultColor, new Vector2Int(i, j));
                }
            }
            await Sort(list);
        }
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