using System;

namespace TMDbLib.Utilities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NameAttribute : Attribute
    {
        public string Description { get; }

        public NameAttribute(string description)
        {
            Description = description;
        }
    }
}