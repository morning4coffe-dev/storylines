using System;
using Storylines.Models.Dialogue;

namespace Storylines.Services.Interfaces
{
    public interface IGraphConsistencyService
    {
        void RemoveNode(DialogueGraph graph, string nodeId);
    }
}
