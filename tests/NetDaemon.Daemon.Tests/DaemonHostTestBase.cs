using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;
using JoySoftware.HomeAssistant.NetDaemon.Daemon;
using Moq;

namespace NetDaemon.Daemon.Tests
{
    public partial class DaemonHostTestaBase
    {
        private readonly LoggerMock _loggerMock;
        private readonly HassClientMock _defaultHassClientMock;
        private readonly NetDaemonHost _defaultDaemonHost;
        private readonly NetDaemonHost _notConnectedDaemonHost;

        internal DaemonHostTestaBase()
        {
            _loggerMock = new LoggerMock();
            _defaultHassClientMock = HassClientMock.DefaultMock;
            _defaultDaemonHost = new NetDaemonHost(_defaultHassClientMock.Object, _loggerMock.LoggerFactory);
            _notConnectedDaemonHost = new NetDaemonHost(HassClientMock.MockConnectFalse.Object, _loggerMock.LoggerFactory);

        }

        public NetDaemonHost DefaultDaemonHost => _defaultDaemonHost;
        public NetDaemonHost NotConnectedDaemonHost => _notConnectedDaemonHost;

        public HassClientMock DefaultHassClientMock => _defaultHassClientMock;

        public LoggerMock LoggerMock => _loggerMock;

        public string HelloWorldData => "Hello world!";
        public dynamic GetDynamicDataObject(string testData = "testdata")
        {
            var expandoObject = new ExpandoObject();
            dynamic dynamicData = expandoObject;
            dynamicData.Test = testData;
            return dynamicData;
        }

        public (dynamic, ExpandoObject) GetDynamicObject(params (string, object)[] dynamicParameters)
        {
            var expandoObject = new ExpandoObject();
            var dict = expandoObject as IDictionary<string, object>;

            foreach (var (name, value) in dynamicParameters)
            {
                dict[name] = value;
            }
            return (expandoObject, expandoObject);
        }


        public async Task RunDefauldDaemonUntilCanceled(short milliSeconds = 100, bool overrideDebugNotCancel = false)
        {
            var cancelSource = Debugger.IsAttached && !overrideDebugNotCancel
                ? new CancellationTokenSource()
                : new CancellationTokenSource(milliSeconds);
            try
            {
                await _defaultDaemonHost.Run("host", 8123, false, "token", cancelSource.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected behaviour
            }
        }

        public (Task, CancellationTokenSource) ReturnRunningDefauldDaemonHostTask(short milliSeconds = 100, bool overrideDebugNotCancel = false)
        {
            var cancelSource = Debugger.IsAttached && !overrideDebugNotCancel
                ? new CancellationTokenSource()
                : new CancellationTokenSource(milliSeconds);
            return (_defaultDaemonHost.Run("host", 8123, false, "token", cancelSource.Token), cancelSource);
        }
        public (Task, CancellationTokenSource) ReturnRunningNotConnectedDaemonHostTask(short milliSeconds = 100, bool overrideDebugNotCancel = false)
        {
            var cancelSource = Debugger.IsAttached && !overrideDebugNotCancel
                ? new CancellationTokenSource()
                : new CancellationTokenSource(milliSeconds);
            return (_notConnectedDaemonHost.Run("host", 8123, false, "token", cancelSource.Token), cancelSource);
        }

        public async Task WaitUntilCanceled(Task task)
        {
            try
            {
                await task;
            }
            catch (TaskCanceledException)
            {
                // Expected behaviour
            }
        }


    }
}