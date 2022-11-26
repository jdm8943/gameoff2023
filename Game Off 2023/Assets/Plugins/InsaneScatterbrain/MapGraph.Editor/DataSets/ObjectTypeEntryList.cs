using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InsaneScatterbrain.MapGraph.Editor
{
    /// <summary>
    /// Base class for editor list for displaying and editing object type entry lists.
    /// </summary>
    /// <typeparam name="TType">The type of object type.</typeparam>
    /// <typeparam name="TEntry">The entry type.</typeparam>
    /// <typeparam name="TObject">The entry's object type.</typeparam>
    public abstract class ObjectTypeEntryList<TType, TEntry, TObject> : ListBase
        where TType : IObjectType<TEntry, TObject>
        where TObject : Object
        where TEntry : IObjectTypeEntry<TObject>
    {
        private readonly ObjectTypeSetList<TType, TEntry, TObject> parentList;
        private readonly ReorderableList reorderableList;

        protected abstract string EntryTypeNamePlural { get; }
        protected abstract string EntryTypeNameSingular { get; }

        protected override ReorderableList ReorderableList => reorderableList;

        /// <summary>
        /// Draws list.
        /// </summary>
        public void DoLayoutList()
        {
            reorderableList.displayAdd = parentList.SelectedIndex > -1;
            reorderableList.displayRemove = parentList.SelectedIndex > -1;

            if (parentList.SelectedIndex > -1)
            {
                var typeId = parentList.OrderedIds[parentList.SelectedIndex];
                var typeName = parentList.ObjectTypeSet.GetName(typeId);

                EditorGUILayout.LabelField($"{typeName} {EntryTypeNamePlural}", DefaultHeaderStyle);
            }
            
            reorderableList.DoLayoutList();
        }

        /// <summary>
        /// Refreshes list entries.
        /// </summary>
        public void RefreshList()
        {
            reorderableList.index = -1;
            reorderableList.list = new int[GetEntryCount(parentList.SelectedIndex)];
        }

        private void ApplyChanges()
        {
            parentList.ApplyChanges();
        }

        protected ObjectTypeEntryList(ObjectTypeSetList<TType, TEntry, TObject> parentList)
        {
            Undo.undoRedoPerformed += RefreshList;
            
            this.parentList = parentList;
            
            var orderedIds = parentList.OrderedIds;
            var dataSet = parentList.ObjectTypeSet;

            reorderableList = new ReorderableList(Array.Empty<int>(), typeof(TEntry), true, false, false, false)
            {
                headerHeight = 4,
                drawElementCallback = (rect, i, active, focused) =>
                {
                    var entry = GetObject(i);

                    EditorGUI.BeginChangeCheck();
                    
                    // Give the text field a unique ID, consisting of the its index and the list's ID, so that it can be used
                    // to see if it's the currently focused control and select the associated item in the list. 
                    SetNextListItemControlName(i);
                    
                    var newEntryObject = (TObject) EditorGUI.ObjectField(
                        new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), entry,
                        typeof(TObject), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetObject(i, newEntryObject);
                        ApplyChanges();
                    }
                    
                    UpdateSelectedIndex();
                },
                drawNoneElementCallback = rect =>
                {
                    if (parentList.SelectedIndex < 0)
                    {
                        var style = new GUIStyle(EditorStyles.label) {alignment = TextAnchor.MiddleCenter};
                        EditorGUI.LabelField(rect, $"Select a {EntryTypeNameSingular} Type", style);
                    }
                    else
                    {
                        EditorGUI.LabelField(rect, "List is Empty");
                    }
                },
                onAddCallback = list =>
                {
                    var typeId = orderedIds[parentList.SelectedIndex];
                    dataSet.AddNewEntry(typeId);
                    ApplyChanges();
                    RefreshList();
                },
                onRemoveCallback = list =>
                {
                    if (parentList.DataObject != null)
                    {
                        Undo.RegisterCompleteObjectUndo(parentList.DataObject, $"Remove {EntryTypeNameSingular}");
                    }

                    var typeId = orderedIds[parentList.SelectedIndex];
                    dataSet.RemoveEntry(typeId, reorderableList.index);
                    ApplyChanges();
                    RefreshList();
                },
                onReorderCallbackWithDetails = (list, oldIndex, newIndex) =>
                {
                    var typeId = orderedIds[parentList.SelectedIndex];
                    dataSet.MoveEntry(typeId, oldIndex, newIndex);
                    ApplyChanges();
                }
            };
        }
        
        public void Dispose()
        {
            Undo.undoRedoPerformed -= RefreshList;
        }
        
        private TObject GetObject(int index)
        {
            var orderedIds = parentList.OrderedIds;
            var typeId = orderedIds[parentList.SelectedIndex];

            return parentList.ObjectTypeSet.GetObject(typeId, index);
        }

        private void SetObject(int index, TObject newObject)
        {
            var orderedIds = parentList.OrderedIds;
            var typeId = orderedIds[parentList.SelectedIndex];

            parentList.ObjectTypeSet.SetObject(typeId, index, newObject);
        }

        private int GetEntryCount(int index)
        {
            var id = parentList.OrderedIds[index];

            return parentList.ObjectTypeSet.GetEntryCount(id);
        }
    }
}