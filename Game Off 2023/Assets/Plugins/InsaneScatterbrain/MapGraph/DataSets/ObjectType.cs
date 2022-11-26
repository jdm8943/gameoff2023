using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using InsaneScatterbrain.ScriptGraph;
using Object = UnityEngine.Object;

namespace InsaneScatterbrain.MapGraph
{
    /// <summary>
    /// Represents objects that consists of entries that represent a certain Object type.
    /// </summary>
    /// <typeparam name="TEntry">The entry type.</typeparam>
    /// <typeparam name="TObject">The object type.</typeparam>
    [Serializable]
    public abstract class ObjectType<TEntry, TObject> : DataSetItem, IObjectType<TEntry, TObject> 
        where TEntry : IObjectTypeEntry<TObject> 
        where TObject : Object
    {
        /// <summary>
        /// A less restricted collection of the entries for protected use.
        /// </summary>
        protected abstract List<TEntry> OpenEntries { get; }

        private ReadOnlyCollection<TEntry> readonlyEntries;
        /// <inheritdoc cref="IObjectType{TEntry,TObject}.Entries"/>
        public ReadOnlyCollection<TEntry> Entries => readonlyEntries ?? (readonlyEntries = OpenEntries.AsReadOnly());

        /// <summary>
        /// Creates and returns an entry for this object type.
        /// </summary>
        /// <returns>The new entry.</returns>
        protected abstract TEntry NewEntry();
        
        /// <inheritdoc cref="IObjectType{TEntry,TObject}.AddNewEntry"/>
        public void AddNewEntry()
        {
            OpenEntries.Add(NewEntry());
        }

        /// <inheritdoc cref="IObjectType{TEntry,TObject}.RemoveEntry"/>
        public void RemoveEntry(int index)
        {
            OpenEntries.RemoveAt(index);
        }

        public void MoveEntry(int oldIndex, int newIndex)
        {
            var entry = OpenEntries[oldIndex];
            
            OpenEntries.RemoveAt(oldIndex);
            OpenEntries.Insert(newIndex, entry);
        }

        /// <inheritdoc cref="IObjectType{TEntry,TObject}.Clean"/>
        public void Clean()
        {
            OpenEntries.RemoveAll(item => item.Value == null);
        }

        protected ObjectType(string name) : base(name) { }
    }
}