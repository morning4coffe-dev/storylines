namespace Storylines.Models.DialogueScript
{
    internal static class DialogueScriptConstants
    {
        public const string NodeHeaderPrefix = "::";
        public const string ChoicePrefix = "->";
        public const string ConditionPrefix = "#if ";
        public const string SetActionPrefix = "@set ";
        public const string ClearActionPrefix = "@clear ";
        public const string CommentPrefix = "//";
        public const string PositionCommentMarker = "@pos";

        // Indentation used when emitting jump targets below a choice line.
        public const string IndentedJumpPrefix = "    :: ";
    }

    /// <summary>
    /// Shared constants for the branching dialogue graph model.
    /// Defined here so the parser (main project) and plugin can share the value
    /// without a circular dependency.
    /// </summary>
    public static class BranchingDialogueConstants
    {
        /// <summary>
        /// Sentinel target node ID that marks a terminal (dead-end) choice.
        /// Written in text as <c>:: [end]</c> or <c>-> [end]</c>.
        /// </summary>
        public const string EndNodeId = "[end]";
    }
}
