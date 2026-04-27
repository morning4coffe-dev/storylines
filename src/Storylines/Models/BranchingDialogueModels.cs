using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Storylines.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ConditionOperator
    {
        Equals,
        NotEquals,
        IsSet,
        IsNotSet
    }

    public class BranchingDialogueGraphData
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("chapterId")]
        public string? ChapterId { get; set; }

        [JsonProperty("startNodeId", NullValueHandling = NullValueHandling.Ignore)]
        public string? StartNodeId { get; set; }

        [JsonProperty("nodes")]
        public List<BranchingDialogueNodeData> Nodes { get; set; } = new List<BranchingDialogueNodeData>();

        public void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            if (Nodes == null)
                Nodes = new List<BranchingDialogueNodeData>();

            foreach (var node in Nodes)
                node?.EnsureValid();

            if (Nodes.Count == 0)
            {
                var defaultNode = new BranchingDialogueNodeData
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Start",
                    Text = string.Empty,
                    Choices = new List<BranchingDialogueChoiceData>()
                };
                Nodes.Add(defaultNode);
                StartNodeId = defaultNode.Id;
                return;
            }

            if (string.IsNullOrWhiteSpace(StartNodeId) || Nodes.All(n => n.Id != StartNodeId))
                StartNodeId = Nodes[0].Id;
        }
    }

    public class BranchingDialogueNodeData
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string? Title { get; set; }

        [JsonProperty("speaker", NullValueHandling = NullValueHandling.Ignore)]
        public string? Speaker { get; set; }

        [JsonProperty("characterToken", NullValueHandling = NullValueHandling.Ignore)]
        public string? CharacterToken { get; set; }

        [JsonProperty("text")]
        public string? Text { get; set; }

        [JsonProperty("notes", NullValueHandling = NullValueHandling.Ignore)]
        public string? Notes { get; set; }

        [JsonProperty("choices")]
        public List<BranchingDialogueChoiceData> Choices { get; set; } = new List<BranchingDialogueChoiceData>();

        [JsonProperty("actions", NullValueHandling = NullValueHandling.Ignore)]
        public List<BranchingDialogueActionData> Actions { get; set; }

        [JsonProperty("positionX", NullValueHandling = NullValueHandling.Ignore)]
        public double? PositionX { get; set; }

        [JsonProperty("positionY", NullValueHandling = NullValueHandling.Ignore)]
        public double? PositionY { get; set; }

        [JsonProperty("tags", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Tags { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Metadata { get; set; }

        public void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Text ??= string.Empty;
            Choices ??= new List<BranchingDialogueChoiceData>();
            Actions ??= new List<BranchingDialogueActionData>();

            foreach (var choice in Choices)
                choice?.EnsureValid();
        }
    }

    public class BranchingDialogueChoiceData
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("text")]
        public string? Text { get; set; }

        [JsonProperty("targetNodeId", NullValueHandling = NullValueHandling.Ignore)]
        public string? TargetNodeId { get; set; }

        [JsonProperty("conditions", NullValueHandling = NullValueHandling.Ignore)]
        public List<BranchingDialogueConditionData> Conditions { get; set; }

        [JsonProperty("metadata", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> Metadata { get; set; }

        public void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
                Id = Guid.NewGuid().ToString();

            Text ??= string.Empty;
            Conditions ??= new List<BranchingDialogueConditionData>();
        }
    }

    public class BranchingDialogueConditionData
    {
        [JsonProperty("flag")]
        public string? Flag { get; set; }

        [JsonProperty("operator", NullValueHandling = NullValueHandling.Ignore)]
        public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string? Value { get; set; }

        public bool Evaluate(Dictionary<string, string> variables)
        {
            if (string.IsNullOrWhiteSpace(Flag))
                return true;

            string? current = null;
            var hasValue = variables != null && variables.TryGetValue(Flag, out current);

            switch (Operator)
            {
                case ConditionOperator.IsSet:
                    return hasValue;
                case ConditionOperator.IsNotSet:
                    return !hasValue;
                case ConditionOperator.NotEquals:
                    return !hasValue || !string.Equals(current, Value ?? string.Empty, StringComparison.Ordinal);
                case ConditionOperator.Equals:
                default:
                    return hasValue && string.Equals(current, Value ?? string.Empty, StringComparison.Ordinal);
            }
        }
    }

    public class BranchingDialogueActionData
    {
        [JsonProperty("flag")]
        public string? Flag { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string? Value { get; set; }

        public void Execute(Dictionary<string, string> variables)
        {
            if (string.IsNullOrWhiteSpace(Flag) || variables == null)
                return;

            if (Value == null)
                variables.Remove(Flag);
            else
                variables[Flag] = Value;
        }
    }

    public class BranchingDialogueValidationResult
    {
        public List<BranchingDialogueValidationIssue> MissingTargets { get; set; } = new List<BranchingDialogueValidationIssue>();
        public List<BranchingDialogueValidationIssue> UnreachableNodes { get; set; } = new List<BranchingDialogueValidationIssue>();
        public List<BranchingDialogueValidationIssue> EmptyChoiceText { get; set; } = new List<BranchingDialogueValidationIssue>();
        public List<BranchingDialogueValidationIssue> UnknownSpeakers { get; set; } = new List<BranchingDialogueValidationIssue>();
        public List<BranchingDialogueValidationIssue> OrphanedConditions { get; set; } = new List<BranchingDialogueValidationIssue>();

        public bool HasWarnings => MissingTargets.Count > 0 || UnreachableNodes.Count > 0
            || EmptyChoiceText.Count > 0 || UnknownSpeakers.Count > 0 || OrphanedConditions.Count > 0;
    }

    public class BranchingDialogueValidationIssue
    {
        public string? NodeId { get; set; }
        public string? ChoiceId { get; set; }
        public string? Message { get; set; }
    }

    public class BranchingDialogueSimulationState
    {
        public string? ChapterId { get; set; }
        public string? GraphId { get; set; }
        public string? CurrentNodeId { get; set; }
        public List<string> BreadcrumbNodeIds { get; set; } = new List<string>();
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        public bool IsDeadEnd { get; set; }
        public bool IsActive { get; set; }
    }
}