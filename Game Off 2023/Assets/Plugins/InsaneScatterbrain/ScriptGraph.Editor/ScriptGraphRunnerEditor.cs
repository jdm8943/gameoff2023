using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InsaneScatterbrain.ScriptGraph.Editor
{
    /// <summary>
    /// Custom inspector for the script graph runner.
    /// </summary>
    [CustomEditor(typeof(ScriptGraphRunner), true)]
    public class ScriptGraphRunnerEditor : UnityEditor.Editor
    {
        private bool showInputParameters = true;
        private Dictionary<Type, Func<string, Type, object, object>> fields;
        
        private SerializedProperty graphProcessorProp;
        private SerializedProperty runOnAwakeProp;
        private SerializedProperty runOnStartProp;
        private SerializedProperty runAsynchronously;
        private SerializedProperty enableMultiThreading;
        private SerializedProperty skipChecks;
        private SerializedProperty enableObjectPooling;
        private SerializedProperty graphProp;
        private SerializedProperty isSeedRandomProp;
        private SerializedProperty seedTypeProp;
        private SerializedProperty seedProp;
        private SerializedProperty seedGuidProp;
        private SerializedProperty processedProp;

        /// <summary>
        /// Attempts to convert the provided object to an object of the given type.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <typeparam name="T">The type to convert to.</typeparam>
        /// <returns>The converted value.</returns>
        private static T ConvertableValue<T>(object val)
        {
            return (T) Convert.ChangeType(val, typeof(T));
        }

        private void OnEnable()
        {
            graphProcessorProp = serializedObject.FindProperty("graphProcessor");
            runOnAwakeProp = serializedObject.FindProperty("runOnAwake");
            runOnStartProp = serializedObject.FindProperty("runOnStart");
            runAsynchronously = serializedObject.FindProperty("runAsynchronously");
            enableMultiThreading = serializedObject.FindProperty("enableMultiThreading");
            skipChecks = serializedObject.FindProperty("skipChecks");
            enableObjectPooling = serializedObject.FindProperty("enableObjectPooling");
            processedProp = serializedObject.FindProperty("processed");
            
            graphProp = graphProcessorProp.FindPropertyRelative("graph");
            isSeedRandomProp = graphProcessorProp.FindPropertyRelative("isSeedRandom");
            seedTypeProp = graphProcessorProp.FindPropertyRelative("seedType");
            seedProp = graphProcessorProp.FindPropertyRelative("seed");
            seedGuidProp = graphProcessorProp.FindPropertyRelative("seedGuid");

            // Define all the input fields for different possible input parameter types.
            fields = new Dictionary<Type, Func<string, Type, object, object>>();
            
            fields.Add(typeof(int), (valName, valType, val) => EditorGUILayout.IntField(valName, ConvertableValue<int>(val)));
            fields.Add(typeof(float), (valName, valType, val) => EditorGUILayout.FloatField(valName, ConvertableValue<float>(val)));
            fields.Add(typeof(bool), (valName, valType, val) => EditorGUILayout.Toggle(valName, ConvertableValue<bool>(val)));
            fields.Add(typeof(string), (valName, valType, val) => EditorGUILayout.TextField(valName, ConvertableValue<string>(val)));
            
            fields.Add(typeof(Color32), (valName, valType, val) =>
            {
                var color = EditorGUILayout.ColorField(new GUIContent(valName), (Color32) val, true, false, false);
                color.a = 255;    // Make sure the colors from a color field are always completely opaque.
                return (Color32) color;
            });
            fields.Add(typeof(Vector2), (valName, valType, val) => EditorGUILayout.Vector2Field(valName, (Vector2)val));
            fields.Add(typeof(Vector2Int), (valName, valType, val) => EditorGUILayout.Vector2IntField(valName, (Vector2Int)val));
            fields.Add(typeof(Vector3), (valName, valType, val) => EditorGUILayout.Vector3Field(valName, (Vector3)val));
            fields.Add(typeof(Vector3Int), (valName, valType, val) => EditorGUILayout.Vector3IntField(valName, (Vector3Int)val));
            fields.Add(typeof(Rect), (valName, valType, val) => EditorGUILayout.RectField(valName, (Rect)val));
            fields.Add(typeof(RectInt), (valName, valType, val) => EditorGUILayout.RectIntField(valName, (RectInt)val));
            fields.Add(typeof(Bounds), (valName, valType, val) => EditorGUILayout.BoundsField(valName, (Bounds)val));
            fields.Add(typeof(BoundsInt), (valName, valType, val) => EditorGUILayout.BoundsIntField(valName, (BoundsInt)val));
            fields.Add(typeof(Object), (valName, valType, val) => EditorGUILayout.ObjectField(valName, (Object)val, valType, true));

            ScriptGraphRunner.OnRun += graph => Repaint(); 
            ScriptGraphRunner.OnStop += graph => Repaint();
        }

        public override void OnInspectorGUI()
        {
            var scriptGraphRunner = target as ScriptGraphRunner;
            
            var graphProcessor = scriptGraphRunner.GraphProcessor;
            
            if (scriptGraphRunner.GraphProcessor.IsProcessing)
            {
                // If the graph is already being processed, disable all the controls (except the "Open Graph" button),
                // so no changes can be made halfway through and to avoid processing the same graph multiple times
                // at the same time. As these things can lead to unexpected results and errors.
                GUI.enabled = false;
            }
            
            GUILayout.Space(20);

            EditorGUILayout.PropertyField(graphProp, new GUIContent("Graph"));

            if (graphProcessor.Graph != null)
            {
                GUILayout.Space(20);


                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(runOnAwakeProp, new GUIContent("Run On Awake", "When enabled, the runner will start processing the graph when OnAwake is triggered."));
                if (EditorGUI.EndChangeCheck())
                {
                    if (runOnAwakeProp.boolValue)
                    {
                        runOnStartProp.boolValue = false;
                    }
                }
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(runOnStartProp, new GUIContent("Run On Start", "When enabled, the runner will start processing the graph when OnStart is triggered."));
                if (EditorGUI.EndChangeCheck())
                {
                    if (runOnStartProp.boolValue)
                    {
                        runOnAwakeProp.boolValue = false;
                    }
                }
                
                GUILayout.Space(20);
                
                EditorGUILayout.PropertyField(skipChecks, new GUIContent("Skip Checks", "When enabled, the graph runner won't check if all parameters are assigned and all the necessary ports are connected, before processing the graph."));
                
                GUILayout.Space(20);
                
                EditorGUILayout.PropertyField(runAsynchronously, new GUIContent("Run Asynchronously", "When enabled, the graph will run on a separate thread (where possible).\n\nThis avoids the game or editor freezing while it waits for the graph runner to complete.\n\nThe downside is that it might take longer to complete processing the graph.\n\nThis can be useful if you need to run a graph during critical gameplay."));

                if (!scriptGraphRunner.RunAsynchronously) GUI.enabled = false;
                EditorGUILayout.PropertyField(enableMultiThreading, new GUIContent("Enable Multi-threading", "When enabled, the graph will run nodes in parallel on separate threads (where possible).\n\nMulti-threading will mostly benefit \"tall\" graphs, meaning graphs that largely consist of nodes that depend on the output of multiple other nodes."));
                if (!scriptGraphRunner.RunAsynchronously) GUI.enabled = true;

                GUILayout.Space(20);
                
                EditorGUILayout.PropertyField(enableObjectPooling, new GUIContent("Enable Object Pooling", "When enabled, Map Graph will pool and reuse any object instances it creates so that they don't get picked up by the garbage collector (where possible).\n\nThis avoids triggering the garbage collector, which can cause stuttering.\n\nThe downside of object pooling is that it might result in Map Graph hogging a lot of memory that it's not actively using.\n\nThis can be useful if you need to run a graph during critical gameplay."));
                
                var graph = graphProcessor.Graph;
            
                GUILayout.Space(20);

                EditorGUILayout.PropertyField(isSeedRandomProp, new GUIContent("Use Random Seed", "When enabled, a different RNG seed is used each time the graph is processed.\n\nDisabling this allows you to set your own seed, resulting in the same output each time.\n\nThis can be useful for testing purposes or for creating daily challenges, for example, where the same level should be generated for everyone."));
                if (!graphProcessor.IsSeedRandom)
                {
                    EditorGUILayout.PropertyField(seedTypeProp, new GUIContent("Seed Type"));

                    if (graphProcessor.SeedType == SeedType.Int)
                    {
                        EditorGUILayout.PropertyField(seedProp, new GUIContent("Seed"));
                    }
                    else if (graphProcessor.SeedType == SeedType.Guid)
                    {
                        if (string.IsNullOrEmpty(graphProcessor.SeedGuid))
                        {
                            graphProcessor.SeedGuid = Guid.NewGuid().ToString();
                        }
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.PropertyField(seedGuidProp, new GUIContent("Seed"));
                        if (GUILayout.Button("New Seed", GUILayout.Width(80)))
                        {
                            graphProcessor.SeedGuid = Guid.NewGuid().ToString();
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                if (graph.InputParameters.OrderedIds.Count > 0)
                {
                    GUILayout.Space(20);
                
                    showInputParameters = EditorGUILayout.Foldout(showInputParameters, "Input Parameters");

                    if (showInputParameters)
                    {
                        // Generated input fields for the input parameters.
                        foreach (var paramId in graph.InputParameters.OrderedIds)
                        {
                            var inputName = graph.InputParameters.GetName(paramId);
                            var inputType = graph.InputParameters.GetType(paramId);

                            Func<string, Type, object, object> field = null;

                            if (fields.ContainsKey(inputType))
                            {
                                field = fields[inputType];
                            }
                            else
                            {
                                // If there's no direct match between the input parameter type and any of the types
                                // that have fields registered to them, loop through all the parent's types and 
                                // use the field for the closest parent's type.
                                var parentType = inputType.BaseType;
                                while (parentType != null)
                                {
                                    if (fields.ContainsKey(parentType))
                                    {
                                        field = fields[parentType];
                                        break;
                                    }
                                
                                    parentType = parentType.BaseType;
                                }

                                // If no field is available, just show a label to show that the input parameter exists,
                                // but is not assignable from the inspector.
                                if (field == null)
                                {
                                    EditorGUILayout.LabelField(inputName, inputType.Name);

                                    continue;
                                }
                            }
                        
                            // Get the field's current value from the graph.
                            var inputParamValue = scriptGraphRunner.GetIn(inputName);
                        
                            if (inputParamValue == null && inputType.IsValueType)
                            {
                                // If it's a value type, make sure that it has a default value if it isn't set yet.
                                inputParamValue = Activator.CreateInstance(inputType);
                            }

                            // Assign the fields new value to the graph.
                            EditorGUI.BeginChangeCheck();
                            inputParamValue = field(inputName, inputType, inputParamValue);

                            if (!EditorGUI.EndChangeCheck()) continue;
                            
                            scriptGraphRunner.SetInById(paramId, inputParamValue);
                            EditorUtility.SetDirty(scriptGraphRunner); 
                        }
                    }
                }

                GUILayout.Space(20);
                
                EditorGUILayout.PropertyField(processedProp);
                
                GUILayout.Space(20);

                GUILayout.BeginHorizontal();
                
                GUI.enabled = true;
                if (GUILayout.Button("Open Graph", GUILayout.Height(40)))
                {
                    var window = ScriptGraphViewWindow.CreateGraphViewWindow(graph);
                    window.Load(graph);
                }
                
                if (scriptGraphRunner.GraphProcessor.IsProcessing)
                {
                    GUI.enabled = false;
                }

                if (GUILayout.Button("Run Graph", GUILayout.Height(40)))
                {
                    // Make sure the inspector is repainted after the graph is processed, in case the GUI
                    // hasn't updated in the meantime, which can happen if the mouse hasn't been moved after
                    // pressing the Run button for example.
                    
                    // First remove unsubscribe the methods from the events in case they have been added before.
                    scriptGraphRunner.OnProcessed -= RepaintAfterProcessing;
                    scriptGraphRunner.OnProcessed += RepaintAfterProcessing;
                    scriptGraphRunner.OnError -= Repaint;
                    scriptGraphRunner.OnError += Repaint;
                    scriptGraphRunner.Run();
                }
                GUILayout.EndHorizontal();

                if (graphProcessor.LatestExecutionTime > -1)
                {
                    GUILayout.Space(20);
                    GUILayout.Label($"Latest execution time: {graphProcessor.LatestExecutionTime / 1000f}s");
                } 
                
                GUILayout.Space(20);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void RepaintAfterProcessing(IReadOnlyDictionary<string, object> result)
        {
            Repaint();
        } 
    }
}