using UnityEngine;

namespace InsaneScatterbrain.MapGraph
{
    /// <summary>
    /// Represents an entry for a certain object type.
    /// </summary>
    /// <typeparam name="T">The type of Object represented by this entry.</typeparam>
    public interface IObjectTypeEntry<T> where T : Object
    {
        /// <summary>
        /// Gets/sets the entry's value.
        /// </summary>
        T Value { get; set; }
    }
}