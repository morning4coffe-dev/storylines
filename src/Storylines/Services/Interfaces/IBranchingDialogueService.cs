using Storylines.Models;

namespace Storylines.Services.Interfaces
{
    public interface IBranchingDialogueService
    {
        BranchingDialogueGraphData GetOrCreateGraph(string chapterId);
        BranchingDialogueNodeData? CreateNode(string chapterId, string? title = null, string? speaker = null, string? text = null);
        bool RenameNode(string chapterId, string nodeId, string? newTitle);
        bool DeleteNode(string chapterId, string nodeId);

        BranchingDialogueChoiceData? AddChoice(string chapterId, string nodeId, string? text = null, string? targetNodeId = null);
        bool RemoveChoice(string chapterId, string nodeId, string choiceId);
        bool ReorderChoices(string chapterId, string nodeId, int fromIndex, int toIndex);
        bool SetChoiceTarget(string chapterId, string nodeId, string choiceId, string? targetNodeId);

        bool SetStartNode(string chapterId, string nodeId);
        void NotifyGraphChanged(string chapterId);
        BranchingDialogueValidationResult ValidateGraph(string chapterId);

        BranchingDialogueSimulationState? StartSimulation(string chapterId);
        BranchingDialogueSimulationState? ChooseChoice(string chapterId, string choiceId);
        BranchingDialogueSimulationState? RestartSimulation(string chapterId);
        void StopSimulation(string chapterId);
        BranchingDialogueSimulationState? GetSimulationState(string chapterId);
    }
}