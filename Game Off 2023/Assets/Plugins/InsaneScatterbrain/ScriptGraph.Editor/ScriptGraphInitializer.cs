using System.Collections.Generic;
using InsaneScatterbrain.Editor.Services;
using UnityEditor;

namespace InsaneScatterbrain.ScriptGraph.Editor
{
    /// <summary>
    /// The ScriptGraphInitialize is run on load to run maintenance jobs on the existing script graphs, in case
    /// things have changed. For example, it removes nodes and ports that no longer exist.
    /// </summary>
    [InitializeOnLoad]
    public static class ScriptGraphInitializer
    {
        private static readonly HashSet<InPort> InactiveInPorts;
        private static readonly HashSet<OutPort> InactiveOutPorts;
        
        static ScriptGraphInitializer()
        {
            InactiveInPorts = new HashSet<InPort>();
            InactiveOutPorts = new HashSet<OutPort>();
            
            // Find all the existing script graphs.
            var graphs = Assets.Find<ScriptGraphGraph>();
            
            var removeNodes = new List<IScriptNode>();
            
            // Check all the graphs for deleted things.
            foreach (var graph in graphs) 
            {
                removeNodes.Clear();
                foreach (var node in graph.Nodes)
                {
                    // If the node is considered to be null, it's no longer valid and can be removed entirely.
                    if (node == null)
                    {
                        removeNodes.Add(node);
                        continue;
                    }
                    
                    // If this node has any in ports, remove any that no longer exist.
                    if (node is IConsumerNode consumerNode)
                    {
                        // Add all the ports to the inactive set.
                        InactiveInPorts.Clear();
                        foreach (var inPort in consumerNode.InPorts)
                        {
                            InactiveInPorts.Add(inPort);
                        }
                        
                        // Now load all the ports for that node and remove any port that is loaded from the inactive
                        // port set. Any port that remains in that set, is no longer used and can be removed.
                        consumerNode.OnInPortAdded += RemoveInactiveInPort;
                        consumerNode.OnLoadInputPorts();
                        consumerNode.OnInPortAdded -= RemoveInactiveInPort; 
                        
                        foreach (var inactiveInPort in InactiveInPorts)
                        {
                            inactiveInPort.Disconnect();    // Make sure the port is disconnected before removing it.
                            consumerNode.RemoveIn(inactiveInPort.Name); 
                        }
                    }
            
                    // If this node has any out ports, remove any that no longer exist.
                    if (node is IProviderNode providerNode)
                    {
                        // Add all the ports to the inactive set.
                        InactiveOutPorts.Clear();
                        foreach (var outPort in providerNode.OutPorts)
                        {
                            InactiveOutPorts.Add(outPort); 
                        }
                        
                        // Now load all the ports for that node and remove any port that is loaded from the inactive
                        // port set. Any port that remains in that set, is no longer used and can be removed.
                        providerNode.OnOutPortAdded += RemoveInactiveOutPort;
                        providerNode.OnLoadOutputPorts();
                        providerNode.OnOutPortAdded -= RemoveInactiveOutPort;

                        foreach (var inactiveOutPort in InactiveOutPorts) 
                        {
                            inactiveOutPort.DisconnectAll();    // Make sure the port is disconnected before removing it.
                            providerNode.RemoveOut(inactiveOutPort.Name); 
                        }
                    }
                }
                
                foreach (var removeNode in removeNodes)
                {
                    graph.Remove(removeNode);
                }
                
                EditorUtility.SetDirty(graph);
            }

            ScriptGraphDebugger.Initialize();
        }

        private static void RemoveInactiveInPort(InPort inPort)
        {
            InactiveInPorts.Remove(inPort);
        }
        
        private static void RemoveInactiveOutPort(OutPort outPort)
        {
            InactiveOutPorts.Remove(outPort);
        }
    }
}