using TMDbLib.Utilities;

namespace TMDbLib.Objects.General
{
    public enum SortOrder
    {
        Undefined = 0,
        [Name("asc")]
        Ascending = 1,
        [Name("desc")]
        Descending = 2
    }
}
