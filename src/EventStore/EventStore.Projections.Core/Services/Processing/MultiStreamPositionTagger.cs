using System;
using System.Collections.Generic;
using EventStore.Core.Data;
using EventStore.Projections.Core.Messages;
using System.Linq;

namespace EventStore.Projections.Core.Services.Processing
{
    public class MultiStreamPositionTagger : PositionTagger
    {
        private readonly HashSet<string> _streams;

        public MultiStreamPositionTagger(string[] streams)
        {
            if (streams == null) throw new ArgumentNullException("streams");
            if (streams.Length == 0) throw new ArgumentException("streams");
            _streams = new HashSet<string>(streams);
        }

        public override bool IsMessageAfterCheckpointTag(CheckpointTag previous, ProjectionCoreServiceMessage.CommittedEventDistributed comittedEvent)
        {
            if (previous.GetMode() != CheckpointTag.Mode.MultiStream)
                throw new ArgumentException("Mode.MultiStream expected", "previous");
            return _streams.Contains(comittedEvent.PositionStreamId)
                   && comittedEvent.PositionSequenceNumber > previous.Streams[comittedEvent.PositionStreamId];
        }

        public override CheckpointTag MakeCheckpointTag(CheckpointTag previous, ProjectionCoreServiceMessage.CommittedEventDistributed comittedEvent)
        {
            if (!_streams.Contains(comittedEvent.PositionStreamId))
                throw new InvalidOperationException(string.Format("Invalid stream '{0}'", comittedEvent.EventStreamId));
            return previous.UpdateStreamPosition(comittedEvent.PositionStreamId, comittedEvent.PositionSequenceNumber);
        }

        public override CheckpointTag MakeZeroCheckpointTag()
        {
            return CheckpointTag.FromStreamPositions(_streams.ToDictionary(v => v, v => ExpectedVersion.NoStream));
        }

        public override bool IsCompatible(CheckpointTag checkpointTag)
        {
            //TODO: should Stream be supported here as well if in the set?
            return checkpointTag.GetMode() == CheckpointTag.Mode.MultiStream
                   && checkpointTag.Streams.All(v => _streams.Contains(v.Key));

        }
    }
}