using System;
using InsaneScatterbrain.ScriptGraph;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace InsaneScatterbrain.MapGraph
{
    /// <summary>
    /// Object type set stored as a scriptable object.
    /// </summary>
    /// <typeparam name="TType">The type of the object type.</typeparam>
    /// <typeparam name="TEntry">The type of the entries.</typeparam>
    /// <typeparam name="TObject">The type of the object in the entries.</typeparam>
    [Serializable]
    public abstract class ObjectTypeSetScriptableObject<TType, TEntry, TObject> : 
        DataSetScriptableObject<TType, OpenObjectTypeSet<TType, TEntry, TObject>>, 
        IObjectTypeSet<TType, TObject>, IPreparable
        where TType : IObjectType<TEntry, TObject>
        where TEntry : IObjectTypeEntry<TObject>
        where TObject : Object
    {
        public event Action<string, int, TObject> OnEntryRemoved
        {
            add => OpenSet.OnEntryRemoved += value;
            remove => OpenSet.OnEntryRemoved -= value;
        }
        
        public event Action<string, int, TObject, TObject> OnEntrySet
        {
            add => OpenSet.OnEntrySet += value;
            remove => OpenSet.OnEntrySet -= value;
        }

        /// <inheritdoc cref="IObjectTypeSet{TType,TObject}.AddNewEntry"/>
        public void AddNewEntry(string typeId)
        {
            OpenSet.AddNewEntry(typeId);
        }

        /// <inheritdoc cref="IObjectTypeSet{TType,TObject}.RemoveEntry"/>
        public void RemoveEntry(string typeId, int entryIndex)
        {
            OpenSet.RemoveEntry(typeId, entryIndex);
        }

        /// <inheritdoc cref="IObjectTypeSet{TType,TObject}.GetObject"/>
        public TObject GetObject(string typeId, int entryIndex)
        {
            return OpenSet.GetObject(typeId, entryIndex);
        }

        /// <inheritdoc cref="IObjectTypeSet{TType,TObject}.SetObject"/>
        public void SetObject(string typeId, int entryIndex, TObject obj)
        {
            OpenSet.SetObject(typeId, entryIndex, obj);
        }

        /// <inheritdoc cref="IObjectTypeSet{TType,TObject}.GetEntryCount"/>
        public int GetEntryCount(string typeId)
        {
            return OpenSet.GetEntryCount(typeId);
        }

        /// <inheritdoc cref="IObjectTypeSet{TType,TObject}.MoveEntry"/>
        public void MoveEntry(string typeId, int oldIndex, int newIndex)
        {
            OpenSet.MoveEntry(typeId, oldIndex, newIndex);
        }

        /// <inheritdoc cref="IObjectTypeSet{TType,TObject}.Clean"/>
        public void Clean()
        {
            OpenSet.Clean();
        }

        /// <summary>
        /// Gets a random entry of the given type.
        /// </summary>
        /// <param name="typeName">The type's name.</param>
        /// <param name="random">The random instance used.</param>
        /// <returns>A random entry.</returns>
        protected TEntry GetRandomEntry(string typeName, Random random)
        {
            var type = GetByName(typeName);
            var entries = type.Entries;
            if (entries.Count < 1)
            {
                Debug.LogError($"No entries for type: {typeName}", this);
            }
            
            var entryIndex = random.Next(0, entries.Count);
            var entry = entries[entryIndex];

            return entry;
        }
        
        /// <summary>
        /// Gets the object for a random entry of the given type.
        /// </summary>
        /// <param name="typeName">The type's name.</param>
        /// <param name="random">The random instance used.</param>
        /// <returns>A random object.</returns>
        public TObject GetRandomObject(string typeName, Random random)
        {
            return GetRandomEntry(typeName, random).Value;
        }

        public virtual void Prepare()
        {
            // Check if all entries have been assigned a value.
            foreach (var typeId in OrderedIds)
            {
                var type = Get(typeId);
                foreach (var entry in type.Entries)
                {
                    if (entry.Value != null) continue;

                    Debug.LogWarning($"An entry of type {type.Name} on {name} hasn't been assigned a value.", this);
                }
            }
        }
    }
}
