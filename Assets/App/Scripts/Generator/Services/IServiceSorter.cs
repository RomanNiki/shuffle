using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace App.Scripts.Generator.Services
{
    public interface IServiceSorter
    { 
        UniTask<List<GridItem>> Sort(List<GridItem> groups);
    }
}