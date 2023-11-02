const express = require('express');
const app = express();
const fetch = require('node-fetch');
const opentelemetry = require("@opentelemetry/api");

const { SeverityNumber, logs } = require('@opentelemetry/api-logs');

require("http");
require("https");
require("./tracing");

const loggerProvider = logs.getLoggerProvider();
const logger = loggerProvider.getLogger('default', "1.0.0");

app.get('/', async (req, res) => {

    var span = opentelemetry.trace.getActiveSpan();

    span.addEvent("Call http");
    var result = await fetch('https://dummyjson.com/products/1');
    const body = await result.text();

    logger.emit({
        severityNumber: SeverityNumber.INFO,
        body: "Get Done!: "+ JSON.stringify(body)
    })

    span.end();

    res.send('GCP App Engine!');
});

const PORT = process.env.PORT || 8080;

app.listen(PORT, () => {
    console.log(`Server listening on port ${PORT}...`);
});