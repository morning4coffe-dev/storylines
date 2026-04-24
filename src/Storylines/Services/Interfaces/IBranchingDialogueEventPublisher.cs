using Storylines.Models;

namespace Storylines.Services.Interfaces
{
    public interface IBranchingDialogueEventPublisher
    {
        void PublishGraphChanged(string chapterId, string? graphId);
        void PublishSimulationStateChanged(string chapterId, BranchingDialogueSimulationState? state);
    }
}