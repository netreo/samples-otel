# Distributed Tracing Example - From https://github.com/open-telemetry/opentelemetry-php
This example uses `docker-compose`, and illustrates the distributed tracing functionality of OpenTelemetry. An HTTP request to service-one will make multiple asynchronous HTTP requests, each of which is injected with a `traceparent` header.

All trace data is exported via grpc to an [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/), where they are forwarded to zipkin and jaeger.

The example is presented as a [slim framework](https://www.slimframework.com/) single-file application for simplicity, and uses Guzzle as an HTTP client. The same application source is used for all services.

## Running the example
```bash
$ docker-compose -f docker/docker-compose.yaml up -d
$ php GettingStarted.php
```
- Access Jaeger UI - http://localhost:16686
