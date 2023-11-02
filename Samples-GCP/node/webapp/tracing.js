
const process = require('process');
const opentelemetry = require('@opentelemetry/sdk-node');
const { getNodeAutoInstrumentations } = require('@opentelemetry/auto-instrumentations-node');
const { ConsoleSpanExporter,AlwaysOnSampler,SimpleSpanProcessor } = require('@opentelemetry/sdk-trace-base');
const { Resource } = require('@opentelemetry/resources');
const { SemanticResourceAttributes } = require('@opentelemetry/semantic-conventions');
const { OTLPTraceExporter } = require('@opentelemetry/exporter-trace-otlp-proto');

const { detectResourcesSync } = require('@opentelemetry/resources');
const { gcpDetector } = require('@opentelemetry/resource-detector-gcp');

const { NodeTracerProvider } = require('@opentelemetry/sdk-trace-node');

const { SeverityNumber, logs } = require('@opentelemetry/api-logs');
const { LoggerProvider, SimpleLogRecordProcessor } = require('@opentelemetry/sdk-logs');
const { OTLPLogExporter } =  require('@opentelemetry/exporter-logs-otlp-proto');

const resource = detectResourcesSync({
    detectors: [gcpDetector],
  })


const res = new Resource({
    [SemanticResourceAttributes.SERVICE_NAME]: 'otel-app-compute-engine'
});

const finalResource = resource.merge(res);

const provider = new NodeTracerProvider({
    resource: finalResource,
    sampler: new AlwaysOnSampler(),
  });

const consoleExporter = new ConsoleSpanExporter();
const otlpExporter = new OTLPTraceExporter( {
    url: '<url-to-collector>/v1/traces'
});


// export spans to console (useful for debugging)
provider.addSpanProcessor(new SimpleSpanProcessor(consoleExporter));
// export spans to opentelemetry collector
provider.addSpanProcessor(new SimpleSpanProcessor(otlpExporter));

provider.register();

const logProvider = new LoggerProvider({resource: finalResource});
const logExporter = new OTLPLogExporter({
    url: '<url-to-collector>/v1/logs'
});
logProvider.addLogRecordProcessor(new SimpleLogRecordProcessor(logExporter));
logs.setGlobalLoggerProvider(logProvider);


const sdk = new opentelemetry.NodeSDK({
  resource: finalResource,
  traceExporter: otlpExporter,
  instrumentations: [getNodeAutoInstrumentations()]
});

// initialize the SDK and register with the OpenTelemetry API
// this enables the API to record telemetry
sdk.start();

// gracefully shut down the SDK on process exit
process.on('SIGTERM', () => {
  sdk.shutdown()
    .then(() => console.log('Tracing terminated'))
    .catch((error) => console.log('Error terminating tracing', error))
    .finally(() => process.exit(0));
});