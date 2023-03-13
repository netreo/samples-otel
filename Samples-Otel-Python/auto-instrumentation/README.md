# Python OTel Launcher README

## Setup

```bash
python3 -m venv .
source ./bin/activate

# Installs OTel libraries
pip install -r requirements.txt
opentelemetry-bootstrap -a install
```

## Running the OTel Collector

```bash
$ docker-compose -f ../../docker/docker-compose.yaml up -d
```

## Running the example

```bash
$ docker-compose up -d
$ curl localhost:8081/ping
```

## Accessing the Jaeger UI for the Logs

1. Open browser
2. Access Jaeger UI - http://localhost:16686

## Send data to OTel Collector (Manual)

> Note: This setup assumes that you have an OTel Collector running at `localhost:4317`. To run an OTel Collector instance locally.

```bash
# Enable Flask debugging
export FLASK_DEBUG=1

# gRPC debug flags
export GRPC_VERBOSITY=debug
export GRPC_TRACE=http,call_error,connectivity_state

# Run Python app with auto-instrumentation
opentelemetry-instrument \
    --traces_exporter console,otlp \
    --metrics_exporter console \
    --service_name test-py-auto-collector-server \
    python server.py
```

To send over HTTP, replace `otlp` with `otlp_proto_http` in the `--traces_exporter` line.

## Run the Client

In a separate terminal window:

```bash
source ./bin/activate

opentelemetry-instrument \
    --traces_exporter console,otlp \
    --metrics_exporter console,otlp \
    --service_name test-py-auto-client
    python client.py test
```

Where `test` is the parameter being passed to `client.py`.

## References

More info on `opentelemetry-instrument` [here](https://github.com/open-telemetry/opentelemetry-python-contrib/tree/main/opentelemetry-instrumentation).
- [OpenTelemetry Example Lightstep](https://github.com/lightstep/opentelemetry-examples)