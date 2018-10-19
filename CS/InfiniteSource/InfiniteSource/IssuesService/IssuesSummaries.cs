using System;

namespace InfiniteSource {
    public class IssuesSummaries {
        public IssuesSummaries(int count, DateTime? lastCreated) {
            Count = count;
            LastCreated = lastCreated;
        }

        public int Count { get; private set; }
        public DateTime? LastCreated { get; private set; }
    }
}
