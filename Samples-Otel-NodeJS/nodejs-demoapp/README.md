# Application

Sample Otel application using a sample NodeJS application by Ben Coleman https://github.com/benc-uk/nodejs-demoapp

### Prerequisites

* Docker Desktop
* NodeJS

### To Run

```shell
~ > cd docker
~/docker > docker-compose up -d
~/docker > cd ..
~ > cd Samples-Otel-NodeJS/nodejs-demoapp
~/Samples-Otel-NodeJS/nodejs-demoapp > export OTEL_TRACES_SAMPLER=parentbased_always_on && export OTEL_TRACES_EXPORTER=otlp && export OTEL_EXPORTER_OTLP_TRACES_PROTOCOL=grpc && export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
~/Samples-Otel-NodeJS/nodejs-demoapp > npm install
~/Samples-Otel-NodeJS/nodejs-demoapp > npm run start
```
