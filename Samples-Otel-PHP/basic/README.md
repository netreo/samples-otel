# Distributed Tracing Example - From https://github.com/open-telemetry/opentelemetry-php
This example uses `docker-compose`, and illustrates the open telemetry capability on a simple service.

## Prerequisites

- Docker
- PHP Runtime

## Running the OTel Collector

```bash
$ docker-compose -f ../docker/docker-compose.yaml up -d
```

## Running the example

```bash
$ composer install
$ php GettingStarted.php
```

## Accessing the Jaeger UI for the Logs

1. Open browser
2. Access Jaeger UI - http://localhost:16686