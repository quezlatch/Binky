using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Binky
{
    public static class Runner
    {
        static void QueueBackgroundWorkItem(Func<CancellationToken, Task> workItem)
        {
            var cancellationToken = new CancellationToken();
            Task.Run(() => workItem(cancellationToken), cancellationToken);
        }
        static Action<Func<CancellationToken, Task>> HostingEnvironmentQueueBackgroundWorkItem = HostingEnvironment.QueueBackgroundWorkItem;
        public static Action<Func<CancellationToken, Task>> Enqueue = HostingEnvironment.IsHosted ? HostingEnvironmentQueueBackgroundWorkItem : QueueBackgroundWorkItem;
    }
}
