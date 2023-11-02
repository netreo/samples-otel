const functions = require('@google-cloud/functions-framework');
const { NodeTracerProvider } = require('@opentelemetry/sdk-trace-node');
const { registerInstrumentations } = require('@opentelemetry/instrumentation');
const { HttpInstrumentation } = require('@opentelemetry/instrumentation-http');
const { SemanticResourceAttributes } = require('@opentelemetry/semantic-conventions');
const { OTLPTraceExporter } = require("@opentelemetry/exporter-trace-otlp-proto");
const { AlwaysOnSampler, ConsoleSpanExporter, SimpleSpanProcessor } = require("@opentelemetry/sdk-trace-base");
const { Resource } = require("@opentelemetry/resources");
const { diag, DiagConsoleLogger, DiagLogLevel } = require("@opentelemetry/api");
const { trace, context } = require("@opentelemetry/api");
const { detectResourcesSync } = require('@opentelemetry/resources');
const { gcpDetector } = require('@opentelemetry/resource-detector-gcp');
const fetch = require('node-fetch');
const { getNodeAutoInstrumentations } = require('@opentelemetry/auto-instrumentations-node');

const { SeverityNumber } = require('@opentelemetry/api-logs');
const LogsAPI = require('@opentelemetry/api-logs');
const { LoggerProvider, SimpleLogRecordProcessor } = require('@opentelemetry/sdk-logs');
const { OTLPLogExporter } = require('@opentelemetry/exporter-logs-otlp-proto');

diag.setLogger(new DiagConsoleLogger(), DiagLogLevel.ALL);

const resource = detectResourcesSync({
  detectors: [gcpDetector],
})

const res = new Resource({
  [SemanticResourceAttributes.SERVICE_NAME]: 'otel-app-gcp-function',
  [SemanticResourceAttributes.FAAS_NAME]: 'otel-app-gcp-function'
});

const finalResource = resource.merge(res);

function setupOtel(functionName) {


  // create tracer provider
  const provider = new NodeTracerProvider({
    resource: finalResource,
    sampler: new AlwaysOnSampler()
  });

  // resource: new Resource({
  //   [SemanticResourceAttributes.SERVICE_NAME]: functionName,
  // }),

  // add proto exporter
  const exporter = new OTLPTraceExporter({
    url: '<url-to-collector>/v1/traces'
  });
  const traceExporter = new ConsoleSpanExporter();
  provider.addSpanProcessor(new SimpleSpanProcessor(traceExporter));
  provider.addSpanProcessor(new SimpleSpanProcessor(exporter));

  // register globally
  provider.register();

  // add http automatic instrumentation
  registerInstrumentations({
    instrumentations: [
      new HttpInstrumentation()
    ],
  });

  return provider;
}

function setupOtelLogging() {
  const logProvider = new LoggerProvider({ resource: finalResource });
  const logExporter = new OTLPLogExporter({
    url: '<url-to-collector>/v1/logs'
  });
  logProvider.addLogRecordProcessor(new SimpleLogRecordProcessor(logExporter));
  LogsAPI.logs.setGlobalLoggerProvider(logProvider);
  return logProvider;
}

function instrumentHandler(handler, funcName) {
  setupOtel(funcName);
  setupOtelLogging();

  return (req, res) => {
    const span = trace.getSpan(context.active());

    if (span != null) {
      span.updateName(funcName);

    }

    handler(req, res);
    // span.end();
  };
}


async function myHandler(req, res) {
  let message = req.query.message || req.body.message || 'Hello World!';
  var result = await fetch('https://dummyjson.com/products/1');
  const body = await result.text();

  // console.log(body);
  const logger = LogsAPI.logs.getLogger('default', '1.0.0');
  logger.emit({
    severityNumber: SeverityNumber.INFO,
    body: "Get Done!: " + JSON.stringify(body)
  })

  var response = await getRequest();
  res.status(200).send(message);
};

const https = require('https');

function getRequest() {
  const url = 'https://opentelemetry.io/';

  return new Promise((resolve, reject) => {
    const req = https.get(url, (res) => {
      resolve(res.statusCode);
    });

    req.on('error', (err) => {
      reject(new Error(err));
    });
  });
}

// Register an HTTP function with the Functions Framework that will be executed
// when you make an HTTP request to the deployed function's endpoint.
functions.http('helloHttp', instrumentHandler(myHandler, "helloHttp"));

//required for http instrumentation to work
require("http");
require("https");