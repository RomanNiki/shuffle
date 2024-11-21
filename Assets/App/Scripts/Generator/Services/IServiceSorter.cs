using System.Collections.Generic;

namespace App.Scripts.Generator.Services
{
    public interface IServiceSorter
    { 
        List<GridItem> Sort(List<GridItem> groups);
    }
}