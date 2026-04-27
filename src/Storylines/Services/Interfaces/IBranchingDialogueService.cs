using Storylines.Models;
using System.Collections.Generic;

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

        // Condition management
        BranchingDialogueConditionData AddCondition(string chapterId, string nodeId, string choiceId,
            string? flag = null, ConditionOperator op = ConditionOperator.Equals, string? value = null);
        bool RemoveCondition(string chapterId, string nodeId, string choiceId, int conditionIndex);

        // Action management (set/unset variables when entering a node)
        BranchingDialogueActionData AddAction(string chapterId, string nodeId,
            string? flag = null, string? value = null);
        bool RemoveAction(string chapterId, string nodeId, int actionIndex);

        bool SetStartNode(string chapterId, string nodeId);
        void NotifyGraphChanged(string chapterId);
        BranchingDialogueValidationResult ValidateGraph(string chapterId, IEnumerable<string> knownSpeakers = null);

        BranchingDialogueSimulationState? StartSimulation(string chapterId);
        BranchingDialogueSimulationState? ChooseChoice(string chapterId, string choiceId);
        BranchingDialogueSimulationState? RestartSimulation(string chapterId);
        void StopSimulation(string chapterId);
        BranchingDialogueSimulationState? GetSimulationState(string chapterId);
    }
}