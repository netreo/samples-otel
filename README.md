# samples-otel

Sample applications for Open Telemetry examples


## [Sample-OTel-DotNet-Core](./samples-otel-dotnet-core/)

DotNet Core sample application that generates log, cache, message (queue), db and http traffic.
Uses [Bacon Ipsum Api]("https://baconipsum.com/"), Redis, RabbitMq and Postgres db as its external resources.

### Prerequisites

* Docker Desktop
* .NET 7

### To Run

```shell
~ > cd docker
~/docker > docker-compose up -d
~/docker > cd ..
~ > dotnet restore
~ > cd samples-otel-dotnet-core
~/samples-otel-dotnet-core > dotnet run
```

## [Sample-OTel-PHP](./samples-otel-php/)

PHP Sample application that uses slim framework (for distributed tracing capability) and vanilla PHP on how to setup simple OTel Application

### Prerequisites (PHP)

* Docker Desktop
* PHP Runtime (for samples-otel-php/basic)

### To Run (PHP)

* `README.md` is located in the root directory for each sample application
  * `samples-otel-php/basic/README.md` for basic OTel application
  * `samples-otel-php/otel-official-php-example/README.md` for simple microservices setup for PHP