namespace MateHR.Domain.Recruitment.Exceptions
{
    public sealed class JobAlreadyClosedException : InvalidOperationException
    {
        public JobAlreadyClosedException(Guid jobId)
            : base($"El job {jobId} ya esta cerrado.")
        {
            JobId = jobId;
        }

        public Guid JobId { get; }
    }
}
