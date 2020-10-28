using System;

namespace Background_Worker
{
    public interface IBackgroundWorkable : IDisposable
    {
        /// <summary>
        /// Enqueueing task for synchronous execution
        /// </summary>
        /// <param name="work">Main job</param>
        /// <param name="failedResult">If failed</param>
        /// <param name="successResult">If succeeded</param>
        void EnqueueSync(Action work, Action? failedResult = null, Action? successResult = null);

        /// <summary>
        /// Enqueueing task for asynchronous execution
        /// </summary>
        /// <param name="work">Main job</param>
        void EnqueueAsync(Action work);

    }
}