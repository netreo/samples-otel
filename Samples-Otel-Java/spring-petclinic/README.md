# Application

Sample Otel application using the Spring Petclinic application

### Prerequisites

* Docker Desktop
* Java 9+

### To Run

1. Download the lastest OTel Java Agent
   from https://github.com/open-telemetry/opentelemetry-java-instrumentation/releases/latest/download/opentelemetry-javaagent.jar

```shell
~ > cd docker
~/docker > docker-compose up -d
~/docker > cd ..
~ > cd Samples-Otel-Java/spring-petclinic
~/Samples-Otel-Java/spring-petclinic > export OTEL_TRACES_SAMPLER=parentbased_always_on && export OTEL_TRACES_EXPORTER=otlp && export OTEL_EXPORTER_OTLP_TRACES_PROTOCOL=grpc && export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
~/Samples-Otel-Java/spring-petclinic > java -javaagent:<path_to_otel_java_agent> -jar spring-petclinic-2.4.5.jar
```
