# Distributed Tracing Example - From https://github.com/open-telemetry/opentelemetry-php
This example uses `docker-compose`, and illustrates the distributed tracing functionality of OpenTelemetry. An HTTP request to service-one will make multiple asynchronous HTTP requests, each of which is injected with a `traceparent` header.

All trace data is exported via grpc to an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/), where they are forwarded to zipkin and jaeger.

The example is presented as a [slim framework](https://www.slimframework.com/) single-file application for simplicity, and uses Guzzle as an HTTP client. The same application source is used for all services.

## Running the OTel Collector

```bash
$ docker-compose -f ../docker/docker-compose.yaml up -d
```

## Running the example

```bash
$ docker-compose run service-one composer install
$ docker-compose up -d
$ curl localhost:8000/users/otel
```

## Accessing the Jaeger UI for the Logs

1. Open browser
2. Access Jaeger UI - http://localhost:16686