using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Collections
{
    [Flags]
    public enum CollectionMethods
    {
        [Name("Undefined")]
        Undefined = 0,
        [Name("images")]
        Images = 1
    }
}