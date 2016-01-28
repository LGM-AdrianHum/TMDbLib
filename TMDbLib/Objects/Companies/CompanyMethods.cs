using System;
using TMDbLib.Utilities;

namespace TMDbLib.Objects.Companies
{
    [Flags]
    public enum CompanyMethods
    {
        [Name("Undefined")]
        Undefined = 0,
        [Name("movies")]
        Movies = 1
    }
}