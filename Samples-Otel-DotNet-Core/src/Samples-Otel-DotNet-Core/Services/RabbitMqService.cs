using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Samples.Model;

namespace Samples.Services;

public class RabbitMqService : IDisposable
{
    public IConnection Connection { get; }
    public IModel Model { get; }

    public RabbitMqService(IAsyncConnectionFactory factory, ILogger<RabbitMqService> logger)
    {
        Connection = factory.CreateConnection();
        Model = Connection.CreateModel();
        Model.QueueDeclare(BlogPost.QueueName, true, false, false);
        Model.ExchangeDeclare(BlogPost.QueueName, ExchangeType.Fanout, true);
        Model.QueueBind(BlogPost.QueueName, BlogPost.QueueName, "*.key");
        Model.CallbackException += (sender, args) =>
            logger.LogError(args.Exception, $"From RabbitMq Details => {JsonConvert.SerializeObject(args.Detail)}");
    }


    public void Dispose()
    {
        Model.Dispose();
        Connection.Dispose();
    }
}