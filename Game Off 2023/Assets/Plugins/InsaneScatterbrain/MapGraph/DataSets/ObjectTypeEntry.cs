using UnityEngine;

namespace InsaneScatterbrain.MapGraph
{
    /// <summary>
    /// Represents an entry for a certain object type.
    /// </summary>
    /// <typeparam name="T">The type of Object represented by this entry.</typeparam>
    public abstract class ObjectTypeEntry<T> : IObjectTypeEntry<T> where T : Object
    {
        /// <inheritdoc cref="IObjectTypeEntry{T}.Value"/>
        public abstract T Value { get; set; }
    }
}