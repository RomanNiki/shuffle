using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace App.Scripts.Generator.Handlers
{
    public interface IHandlerSorter
    {
        UniTask<List<GridItem>> Sort(List<GridItem> items);
    }
}