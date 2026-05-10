using System;
using System.Linq;
using Storylines.Models.Dialogue;
using Storylines.Services.Interfaces;

namespace Storylines.Services
{
    public class GraphConsistencyService : IGraphConsistencyService
    {
        public void RemoveNode(DialogueGraph graph, string nodeId)
        {
            if (graph == null || string.IsNullOrEmpty(nodeId)) return;

            var nodeToRemove = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);
            if (nodeToRemove != null)
            {
                graph.Nodes.Remove(nodeToRemove);
            }

            // Remove all incoming and outgoing choices
            var choicesToRemove = graph.Choices
                .Where(c => c.SourceNodeId == nodeId || c.TargetNodeId == nodeId)
                .ToList();

            foreach (var choice in choicesToRemove)
            {
                graph.Choices.Remove(choice);
            }
        }
    }
}
