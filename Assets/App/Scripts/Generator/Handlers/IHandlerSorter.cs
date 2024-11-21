using System.Collections.Generic;

namespace App.Scripts.Generator.Handlers
{
    public interface IHandlerSorter
    {
        List<GridItem> Sort(List<GridItem> items);
    }
}