namespace MassTransitBenchmark.Latency
{
    using System;
    using System.Threading.Tasks;
    using MassTransit;
    using MassTransit.Azure.ServiceBus.Core;
    using MassTransit.Util;


    public class ServiceBusMessageLatencyTransport :
        IMessageLatencyTransport
    {
        readonly ServiceBusHostSettings _hostSettings;
        readonly IMessageLatencySettings _settings;
        Uri _targetAddress;
        Task<ISendEndpoint> _targetEndpoint;

        public ServiceBusMessageLatencyTransport(ServiceBusHostSettings hostSettings, IMessageLatencySettings settings)
        {
            _hostSettings = hostSettings;
            _settings = settings;
        }

        public Task<ISendEndpoint> TargetEndpoint => _targetEndpoint;

        public IBusControl GetBusControl(Action<IReceiveEndpointConfigurator> callback)
        {
            IBusControl busControl = Bus.Factory.CreateUsingAzureServiceBus(x =>
            {
                x.Host(_hostSettings);

                x.ReceiveEndpoint("latency_consumer" + (_settings.Durable ? "" : "_express"), e =>
                {
                    e.PrefetchCount = _settings.PrefetchCount;
                    if (_settings.ConcurrencyLimit > 0)
                        e.MaxConcurrentCalls = _settings.ConcurrencyLimit;

                    callback(e);

                    _targetAddress = e.InputAddress;
                });
            });

            TaskUtil.Await(() => busControl.StartAsync());

            _targetEndpoint = busControl.GetSendEndpoint(_targetAddress);

            return busControl;
        }
    }
}