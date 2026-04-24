using Storylines.Models;
using Storylines.Services.Interfaces;

namespace Storylines.Services
{
    public class BranchingDialogueEventPublisher : IBranchingDialogueEventPublisher
    {
        private readonly EventAggregator _events;

        public BranchingDialogueEventPublisher(EventAggregator events)
        {
            _events = events;
        }

        public void PublishGraphChanged(string chapterId, string graphId)
        {
            _events.Publish(new BranchingDialogueGraphChangedEvent
            {
                ChapterId = chapterId,
                GraphId = graphId
            });
        }

        public void PublishSimulationStateChanged(string chapterId, BranchingDialogueSimulationState state)
        {
            _events.Publish(new BranchingDialogueSimulationStateChangedEvent
            {
                ChapterId = chapterId,
                State = state
            });
        }
    }
}