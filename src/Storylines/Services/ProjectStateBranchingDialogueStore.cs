using Storylines.Models;
using Storylines.Services.Interfaces;
using System.Collections.Generic;

namespace Storylines.Services
{
    public class ProjectStateBranchingDialogueStore : IBranchingDialogueStore
    {
        private readonly ProjectState _projectState;

        public ProjectStateBranchingDialogueStore(ProjectState projectState)
        {
            _projectState = projectState;
        }

        public List<BranchingDialogueGraphData> BranchingDialogues => _projectState.BranchingDialogues;

        public BranchingDialogueGraphData GetOrCreateGraph(string chapterId)
        {
            return _projectState.GetOrCreateBranchingDialogueForChapter(chapterId);
        }
    }
}