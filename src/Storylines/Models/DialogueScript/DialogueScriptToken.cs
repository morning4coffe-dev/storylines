#nullable enable annotations
namespace Storylines.Models.DialogueScript
{
    public enum DialogueLineType
    {
        Blank,
        Comment,
        NodeHeader,
        SpeakerLine,
        Choice,
        IndentedJump,
        Condition,
        SetAction,
        ClearAction,
        Unknown
    }

    public record DialogueScriptLine(
        int LineIndex,
        string Raw,
        DialogueLineType Type,
        string? NodeName = null,
        string[]? Tags = null,
        string? SpeakerName = null,
        string? SpeakerText = null,
        string? ChoiceText = null,
        string? ConditionFlag = null,
        string? TargetNodeId = null,
        string? ActionFlag = null,
        string? ActionValue = null,
        float PositionX = 0f,
        float PositionY = 0f
    );
}
