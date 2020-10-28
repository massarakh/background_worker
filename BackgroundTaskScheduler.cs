using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Background_Worker
{
    public class BackgroundTaskScheduler : IBackgroundWorkable
    {
        private Thread? _syncThread;
        private Thread? _asyncThread;
        private readonly CancellationTokenSource _cts;
        private BlockingCollection<Tuple<Action, Action?, Action?>> _syncTasks = new BlockingCollection<Tuple<Action, Action?, Action?>>();
        private BlockingCollection<Action> _asyncTasks = new BlockingCollection<Action>();

        public BackgroundTaskScheduler()
        {
            _cts = new CancellationTokenSource();
        }

        public void Start()
        {
            var stoppingToken = _cts.Token;
            _syncThread = new Thread(async () =>
            {
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {

                        var item = _syncTasks.Take(stoppingToken);
                        do
                        {
                            var main = item.Item1;
                            var failed = item.Item2;
                            var success = item.Item3;
                            stoppingToken.ThrowIfCancellationRequested();

                            try
                            {
                                await Task.Run(() => main(), stoppingToken).ContinueWith(res =>
                                {
                                    if (res.Status == TaskStatus.Faulted)
                                    {
                                        failed?.Invoke();
                                    }
                                    else
                                    {
                                        success?.Invoke();
                                    }
                                }, stoppingToken);
                            }

                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error executing tasks: {ex.Message}", ex);
                            }

                        } while (_syncTasks.TryTake(out item, 0, stoppingToken));
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Tasks have been cancelled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}", ex);
                }
            })
            { IsBackground = true };
            _syncThread.Start();

            _asyncThread = new Thread(() =>
            {
                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        Action item = _asyncTasks.Take(stoppingToken);
                        do
                        {
                            stoppingToken.ThrowIfCancellationRequested();

                            try
                            {
                                var action = item; 
                                Task.Factory.StartNew(() => action(),
                                    stoppingToken,
                                    TaskCreationOptions.LongRunning,
                                    TaskScheduler.Default);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error executing tasks: {ex.Message}", ex);
                            }

                        } while (_asyncTasks.TryTake(out item, 0, stoppingToken));
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Tasks have been cancelled");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}", ex);
                }
            })
            { IsBackground = true };
            _asyncThread.Start();
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        /// <inheritdoc />
        public void EnqueueSync(Action work, Action? failedResult = null, Action? successResult = null)
        {
            _syncTasks.Add(new Tuple<Action, Action?, Action?>(work, failedResult, successResult));
        }

        /// <inheritdoc />
        public void EnqueueAsync(Action work)
        {
            _asyncTasks.Add(work);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_cts.IsCancellationRequested)
                Stop();
            _asyncTasks.Dispose();
            _syncTasks.Dispose();
        }
    }
}
