namespace Axis.Pulsar.Core.XBNF.Parsers.Meta
{
    /// <summary>
    /// Experimental match status structure
    /// </summary>
    public readonly struct MatchStatus
    {
        private readonly Status _status;

        public MatchStatus(Status status)
        {
            _status = status;
        }

        public static MatchStatus Of(Status status) => new(status);

        public static implicit operator MatchStatus(Status status) => new(status);

        public static implicit operator Status(MatchStatus matchStatus) => matchStatus._status;

        public static implicit operator bool(MatchStatus matchStatus)
        {
            return matchStatus._status switch
            {
                Status.Failed => false,
                Status.Succeeded
                or Status.Unmatched => true,
                _ => throw new InvalidOperationException(
                    $"Invalid Status: {matchStatus._status}")
            };
        }

        #region Nested types
        public enum Status
        {
            Failed,
            Succeeded,
            Unmatched
        }
        #endregion
    }
}
