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
