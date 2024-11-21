using System;
using System.Collections.Generic;
using App.Scripts.Generator.Handlers;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace App.Scripts.Generator.Services
{
    public class ServiceSorter : IServiceSorter, IInitializable
    {
        private readonly IEnumerable<IHandlerSorter> _handlerSorters;
        private readonly ConfigServiceSorter _configServiceSorter;

        public ServiceSorter(IEnumerable<IHandlerSorter> handlerSorters, ConfigServiceSorter configServiceSorter)
        {
            _handlerSorters = handlerSorters;
            _configServiceSorter = configServiceSorter;
        }

        public List<GridItem> Sort(List<GridItem> groups)
        {
            foreach (var handlerSorter in _handlerSorters)
            {
                return handlerSorter.Sort(groups);
            }

            return groups;
        }

        public void Initialize()
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
            
            Sort(list);
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