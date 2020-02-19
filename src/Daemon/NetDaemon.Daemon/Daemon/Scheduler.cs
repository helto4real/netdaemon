﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Common;

namespace JoySoftware.HomeAssistant.NetDaemon.Daemon
{


    /// <summary>
    ///     Interface to be able to mock the time
    /// </summary>
    public interface IManageTime
    {
        DateTime Current { get; }

        Task Delay(TimeSpan timeSpan, CancellationToken token);
    }

    public class Scheduler : IScheduler
    {
        private const int DefaultSchedulerTimeout = 100;

        /// <summary>
        ///     Used to cancel all running tasks
        /// </summary>
        private readonly CancellationTokenSource _cancelSource = new CancellationTokenSource();

        private readonly ConcurrentDictionary<int, Task> _scheduledTasks
                    = new ConcurrentDictionary<int, Task>();
        private readonly IManageTime? _timeManager;
        private Task _schedulerTask;
        public Scheduler(IManageTime? timerManager = null)
        {
            _timeManager = timerManager ?? new TimeManager();

            _schedulerTask = Task.Run(SchedulerLoop, _cancelSource.Token);
        }

        /// <summary>
        ///     Time when task was completed, these probably wont be used more than in tests
        /// </summary>
        public DateTime CompletedTime { get; } = DateTime.MaxValue;

        /// <summary>
        ///     Calculated start time, these probably wont be used more than in tests
        /// </summary>
        public DateTime StartTime { get; } = DateTime.MinValue;


        /// <inheritdoc/>
        public void RunEvery(int millisecondsDelay, Func<Task> func) => RunEveryAsync(millisecondsDelay, func);

        /// <inheritdoc/>
        public Task RunEveryAsync(int millisecondsDelay, Func<Task> func)
        {
            return RunEveryAsync(TimeSpan.FromMilliseconds(millisecondsDelay), func);
        }

        /// <inheritdoc/>
        public void RunEvery(TimeSpan timeSpan, Func<Task> func) => RunEveryAsync(timeSpan, func);

        /// <inheritdoc/>
        public Task RunEveryAsync(TimeSpan timeSpan, Func<Task> func)
        {
            var stopWatch = new Stopwatch();

            var task = Task.Run(async () =>
            {
                while (!_cancelSource.IsCancellationRequested)
                {
                    stopWatch.Start();
                    await func.Invoke();
                    stopWatch.Stop();

                    // If less time spent in func that duration delay the remainder
                    if (timeSpan > stopWatch.Elapsed)
                    {
                        var diff = timeSpan.Subtract(stopWatch.Elapsed);
                        await _timeManager!.Delay(diff, _cancelSource.Token);
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                    stopWatch.Reset();
                }
            }, _cancelSource.Token);

            ScheduleTask(task);

            return task;
        }

        internal TimeSpan CalculateDailyTimeBetweenNowAndTargetTime(DateTime targetTime)
        {
            var now = _timeManager!.Current;

            var timeToTrigger = new DateTime(now.Year, now.Month, now.Day, targetTime.Hour, targetTime.Minute, targetTime.Second);

            if (now > timeToTrigger)
            {
                timeToTrigger = timeToTrigger.AddDays(1);
            }
            return timeToTrigger.Subtract(now);
        }

        internal TimeSpan CalculateEveryMinuteTimeBetweenNowAndTargetTime(short second)
        {
            var now = _timeManager!.Current;
            if (now.Second > second)
            {
                return TimeSpan.FromSeconds(60 - now.Second + second);
            }
            return TimeSpan.FromSeconds(second - now.Second);
        }

        /// <summary>
        ///     Run daily tasks
        /// </summary>
        /// <param name="time">The time in the format HH:mm:ss</param>
        /// <param name="func">The action to run</param>
        /// <remarks>
        ///     It is safe to supress the task since it is handled internally in the scheduler
        /// </remarks>
        public void RunDaily(string time, Func<Task> func) => RunDailyAsync(time, func);


        /// <summary>
        ///     Run daily tasks
        /// </summary>
        /// <param name="time">The time in the format HH:mm:ss</param>
        /// <param name="func">The action to run</param>
        /// <returns></returns>
        public Task RunDailyAsync(string time, Func<Task> func)
        {
            DateTime timeOfDayToTrigger;
            if (!DateTime.TryParseExact(time, "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out timeOfDayToTrigger))
            {
                throw new FormatException($"{time} is not a valid time for the current locale");
            }


            var task = Task.Run(async () =>
           {
               while (!_cancelSource.IsCancellationRequested)
               {

                   var diff = CalculateDailyTimeBetweenNowAndTargetTime(timeOfDayToTrigger);
                   await _timeManager!.Delay(diff, _cancelSource.Token);
                   await func.Invoke();
               }
           }, _cancelSource.Token);

            ScheduleTask(task);

            return task;
        }

        /// <summary>
        ///      Run task every minute at given second
        /// </summary>
        /// <param name="second">The second in a minute to start (0-59)</param>
        /// <param name="func">The task to run</param>
        /// <remarks>
        ///     It is safe to supress the task since it is handled internally in the scheduler
        /// </remarks>

        public void RunEveryMinute(short second, Func<Task> func) => RunEveryMinuteAsync(second, func);

        /// <summary>
        ///     Run task every minute at given second
        /// </summary>
        /// <param name="second">The second in a minute to start (0-59)</param>
        /// <param name="func">The task to run</param>
        /// <returns></returns>
        public Task RunEveryMinuteAsync(short second, Func<Task> func)
        {
            var task = Task.Run(async () =>
          {
              while (!_cancelSource.IsCancellationRequested)
              {
                  var now = _timeManager.Current;
                  var diff = CalculateEveryMinuteTimeBetweenNowAndTargetTime(second);
                  await _timeManager!.Delay(diff, _cancelSource.Token);
                  await func.Invoke();

              }
          }, _cancelSource.Token);

            ScheduleTask(task);

            return task;
        }



        /// <inheritdoc/>
        public void RunIn(int millisecondsDelay, Func<Task> func) => RunInAsync(millisecondsDelay, func);

        /// <inheritdoc/>
        public Task RunInAsync(int millisecondsDelay, Func<Task> func)
        {
            return RunInAsync(TimeSpan.FromMilliseconds(millisecondsDelay), func);
        }

        /// <inheritdoc/>
        public void RunIn(TimeSpan timeSpan, Func<Task> func) => RunInAsync(timeSpan, func);

        /// <inheritdoc/>
        public Task RunInAsync(TimeSpan timeSpan, Func<Task> func)
        {
            var task = Task.Run(async () =>
            {
                await _timeManager!.Delay(timeSpan, _cancelSource.Token);
                await func.Invoke();
            }, _cancelSource.Token);

            ScheduleTask(task);

            return task;
        }

        /// <summary>
        ///     Stops the scheduler
        /// </summary>
        public async Task Stop()
        {
            _cancelSource.Cancel();

            // Make sure we are waiting for the scheduler task as well
            _scheduledTasks[_schedulerTask.Id] = _schedulerTask;

            var taskResult = await Task.WhenAny(
                Task.WhenAll(_scheduledTasks.Values.ToArray()), Task.Delay(1000));

            if (_scheduledTasks.Values.Count(n => n.IsCompleted == false) > 0)
                // Todo: Some kind of logging have to be done here to tell user which task caused timeout
                throw new ApplicationException("Failed to cancel all tasks");
        }

        private async Task SchedulerLoop()
        {
            try
            {
                while (!_cancelSource.Token.IsCancellationRequested)
                    if (_scheduledTasks.Count > 0)
                    {
                        // Make sure we do cleaning and handle new task every 100 ms
                        ScheduleTask(Task.Delay(DefaultSchedulerTimeout,
                            _cancelSource.Token)); // Todo: Work out a proper timing

                        var task = await Task.WhenAny(_scheduledTasks.Values.ToArray())
                            .ConfigureAwait(false);

                        // Todo: handle errors here if not removing
                        _scheduledTasks.TryRemove(task.Id, out _);
                    }
                    else
                    {
                        await Task.Delay(DefaultSchedulerTimeout, _cancelSource.Token);
                    }
            }
            catch (OperationCanceledException)
            {// Normal, just ignore
            }

        }

        private void ScheduleTask(Task addedTask)
        {
            _scheduledTasks[addedTask.Id] = addedTask;
        }
    }

    /// <summary>
    ///     Abstract time functions to be able to mock
    /// </summary>
    public class TimeManager : IManageTime
    {
        /// <summary>
        ///     Returns current local time
        /// </summary>
        /// <value></value>
        public DateTime Current { get; }

        /// <summary>
        ///     Delays a given timespan time
        /// </summary>
        /// <param name="timeSpan">Timespan to delay</param>
        /// <param name="token">Cancelation token to cancel delay</param>
        public async Task Delay(TimeSpan timeSpan, CancellationToken token)
        {
            await Task.Delay(timeSpan, token);
        }
    }
}