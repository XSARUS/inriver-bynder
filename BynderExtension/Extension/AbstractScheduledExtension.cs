using inRiver.Remoting.Exceptions;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using NCrontab;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bynder.Extension
{
    using Enums;
    using Utils.Helpers;

    public abstract class AbstractScheduledExtension : Extension, IScheduledExtension
    {
        #region Fields

        private const string _dateTimeFormat = "yyyyMMdd|HHmmss";

        private readonly System.Timers.Timer _crontabTimer;

        /// <summary>
        /// Id which contains the year, month, day, hour, minute, second
        /// when the Scheduler class has been instantiated.
        /// We could also use this id to see since when this Scheduled extension is initialized for the first time.
        ///
        /// Format is yyyyMMdd|HHmmss in UTC
        /// </summary>
        private readonly string _instanceId;

        private string _cronExpression = "";

        private CrontabSchedule _crontabScheduler;

        private SchedulerStatus _status;

        #endregion Fields

        #region Properties

        public override Dictionary<string, string> DefaultSettings
        {
            get
            {
                var settings = base.DefaultSettings;
                settings.Add(Config.Settings.CronExpression, "* * * * *");
                return settings;
            }
        }

        protected DateTime LastStartDateTimeOfProcess { get; set; }

        #endregion Properties

        #region Constructors

        protected AbstractScheduledExtension()
        {
            _instanceId = DateTime.UtcNow.ToString(_dateTimeFormat);

            _crontabTimer = new System.Timers.Timer();
            _crontabTimer.Elapsed += delegate { CronTabTimerElapsed(); };
        }

        #endregion Constructors

        #region Methods

        public virtual void Execute(bool force)
        {
            try
            {
                if (_status == SchedulerStatus.Idle) InitializeScheduler();
                if (_status == SchedulerStatus.Active && force) ExecuteForceCommand();
                if (_status == SchedulerStatus.Busy)
                    Context.Log(LogLevel.Information, $"{_instanceId}: Force run is canceled because there is still a process busy.");
            }
            catch (Exception e)
            {
                Context.Log(LogLevel.Error, $"Error while executing extension!", e);
            }
        }

        public override string Test()
        {
            var sb = new StringBuilder();

            try
            {
                sb.AppendLine(base.Test());

                sb.AppendLine($"Status is {_status}");
                switch (_status)
                {
                    case SchedulerStatus.Idle:
                        sb.AppendLine("Scheduler is not started. The scheduler can be started by pushing the 'run' button");
                        break;

                    case SchedulerStatus.Active:
                        sb.AppendLine($"Scheduler is started. The next run shall be on {_crontabScheduler.GetNextOccurrence(DateTime.UtcNow)} UTC");
                        break;

                    case SchedulerStatus.Busy:
                        sb.AppendLine("Scheduler is busy with a process");
                        break;
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine(ex.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Implementation of what the scheduled extension must do
        /// </summary>
        protected abstract void Execute();

        private void CronTabTimerElapsed() => StartProcess();

        private void ExecuteForceCommand()
        {
            var cronExpression = SettingHelper.GetCronExpression(DefaultSettings, Context.Logger);
            if (Equals(_cronExpression, cronExpression))
            {
                Context.Log(LogLevel.Information, $"{_instanceId}: Force run is executed. The process will be started.");

                StartProcess();
            }
            else
            {
                Context.Log(LogLevel.Information, $"{_instanceId}: Force run is executed. Settings are changed the scheduler will reschedule.");

                _crontabTimer.Stop();

                _cronExpression = cronExpression;

                _crontabScheduler = CrontabSchedule.Parse(cronExpression);
                InitializeScheduledTimer();
            }
        }

        private void InitializeScheduledTimer()
        {
            DateTime nextOccurrence = _crontabScheduler.GetNextOccurrence(DateTime.UtcNow);

            _crontabTimer.Interval = (nextOccurrence - DateTime.UtcNow).TotalMilliseconds;
            _crontabTimer.Start();

            Context.Log(LogLevel.Verbose, $"{_instanceId} - Scheduled for {nextOccurrence} UTC");
        }

        private void InitializeScheduler()
        {
            _cronExpression = SettingHelper.GetCronExpression(DefaultSettings, Context.Logger); 
            _crontabScheduler = CrontabSchedule.Parse(_cronExpression);
            InitializeScheduledTimer();

            _status = SchedulerStatus.Active;
        }

        private void StartProcess()
        {
            if (_status == SchedulerStatus.Busy)
            {
                Context.Log(LogLevel.Information, $"{_instanceId}: The is still a process running. The current running process has been started on '{LastStartDateTimeOfProcess}'. To stop the current process press the restart service button.");
            }
            else
            {
                try
                {
                    _crontabTimer.Stop();
                    _status = SchedulerStatus.Busy;

                    Context.Log(LogLevel.Verbose, $"{_instanceId}: CronTabTimer Elapsed, start process");

                    LastStartDateTimeOfProcess = DateTime.UtcNow;

                    // run code in separate task
                    Task.Run(() => Execute()).GetAwaiter().GetResult();
                }
                // thrown when using multiple threads and one of them throws an exception
                catch (AggregateException ex)
                {
                    foreach (var iEx in ex.InnerExceptions)
                    {
                        Context.Log(LogLevel.Error, $"{_instanceId}: ({iEx.GetType().FullName}) {iEx.GetBaseException().Message}", iEx);
                    }
                }
                // thrown when inRiver can't get data (from the database)
                catch (PersistanceException ex)
                {
                    Context.Log(LogLevel.Error, $"{_instanceId}: ({ex.GetType().FullName}) {ex.GetBaseException().Message} \r\n Detailed Message: {ex.DetailedMessage}", ex);
                }
                // catch all other exceptions
                catch (Exception ex)
                {
                    Context.Log(LogLevel.Error, $"{_instanceId}: ({ex.GetType().FullName}) {ex.GetBaseException().Message}", ex);
                }
                finally
                {
                    Context.Log(LogLevel.Verbose, $"{_instanceId}: CronTabTimer Elapsed, end process");

                    _status = SchedulerStatus.Active;

                    InitializeScheduledTimer();
                }
            }
        }

        #endregion Methods
    }
}
