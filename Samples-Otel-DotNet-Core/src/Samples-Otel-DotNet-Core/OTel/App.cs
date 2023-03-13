using System.Diagnostics;
using System.Net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;

namespace Samples.OTel;

public static class App
{
    public static readonly ActivitySource Application = new ("Samples.OTel.DotNet.Core");
    public static readonly ActivitySource RabbitMq = new (nameof(RabbitMq));
    public static readonly ActivitySource Redis = new("OpenTelemetry.Instrumentation.StackExchangeRedis");

    public static void AddTags(this Activity? activity, IConnectionMultiplexer connection)
    {
        if (activity == null) return;
        var configuration = ConfigurationOptions.Parse(connection.Configuration);
        if (configuration.EndPoints.FirstOrDefault() is DnsEndPoint endpoint)
        {
            activity.AddTag(PropertyNames.NetPeerName, endpoint.Host);
            activity.AddTag(PropertyNames.NetPeerPort, endpoint.Port);
            activity.AddTag(PropertyNames.PeerService, $"{endpoint.Host}:{endpoint.Port}");
        }
    }

    public static void AddTags(this Activity? activity, IDatabase database, string key)
    {
        if (database == null) return;
        activity?.AddTag(PropertyNames.DbSystem, "redis");
        activity?.AddTag(PropertyNames.DbRedisDatabase_index, database.Database);
        var endpoint = database.IdentifyEndpoint(key);
        if (endpoint is DnsEndPoint npoint)
        {
            activity?.AddTag(PropertyNames.NetPeerName, npoint.Host);
            activity?.AddTag(PropertyNames.NetPeerPort, npoint.Port);
        }

        activity?.AddTag(PropertyNames.PeerService, endpoint.ToString());
    }

    public static void AddTags(this Activity? activity, ISubscriber? subscriber, string channel)
    {
        if (subscriber == null) return;
        activity?.AddTag(PropertyNames.DbSystem, "redis");
        var endpoint = subscriber.IdentifyEndpoint(channel);
        if (endpoint is DnsEndPoint npoint)
        {
            activity?.AddTag(PropertyNames.NetPeerName, npoint.Host);
            activity?.AddTag(PropertyNames.NetPeerPort, npoint.Port);
        }

        activity?.AddTag(PropertyNames.PeerService, endpoint.ToString());
        
    }

    public static string ToString(this RedisValue value, int maxLength)
    {
        if (maxLength < 10)
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, "Must be 10 or greater");
        if (value.IsNull) return "NULL";
        if (value.IsNullOrEmpty) return "";
        return value.Length() >= maxLength ? $"{value.ToString()[..(maxLength - 3)]}..." : value;
    }

    public static RedisValue ToRedisValue(this string? value)
    {
        if (value == null) return RedisValue.Null;
        if (string.IsNullOrWhiteSpace(value)) return RedisValue.EmptyString;
        return new RedisValue(value);
    }
    
    public static void AddTags(this Activity? activity, BasicDeliverEventArgs args)
    {
        activity?.AddTag(PropertyNames.MessagingSystem, "rabbitmq");
        activity?.AddTag(PropertyNames.MessagingOperation, "receive");
        activity?.AddTag(PropertyNames.MessagingRabbitmqRouting_key, args.RoutingKey);
        activity?.AddTag("messaging.rabbitmq.exchange", args.Exchange);
        activity?.AddTags(args.BasicProperties);
    }
    public static void AddTags(this Activity? activity, IBasicProperties properties)
    {
        activity?.AddTag(PropertyNames.MessagingMessageId, properties.MessageId);
        activity?.AddTag(PropertyNames.MessagingMessageConversation_id, properties.CorrelationId);
    }
    public static void AddTags(this Activity? activity, AmqpTcpEndpoint? endpoint)
    {
        if (activity == null || endpoint == null) return;
        activity.AddTag(PropertyNames.NetPeerName, endpoint.HostName);
        activity.AddTag(PropertyNames.NetPeerPort, endpoint.Port);
    }

    public static void AddTags(this Activity? activity, IConnection? connection)
    {
        activity?.AddTags(connection?.Endpoint);
    }
}