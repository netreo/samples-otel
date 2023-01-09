using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Samples.Services;

public static class RabbitMqExtensions
{
    public static void Publish<T>(this IModel model, T value, string exchange,
        string routingKey = "default.key", IBasicProperties? properties = null)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        properties ??= model.CreateBasicProperties();
        properties.ContentType = "text/json";
        model.BasicPublish(exchange, routingKey, properties, bytes);
    }
}