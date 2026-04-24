using Storylines.Models;
using System.Collections.Generic;

namespace Storylines.Services.Interfaces
{
    public interface IBranchingDialogueStore
    {
        List<BranchingDialogueGraphData> BranchingDialogues { get; }
        BranchingDialogueGraphData GetOrCreateGraph(string chapterId);
    }
}