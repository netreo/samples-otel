# Sample Ruby on Rails app instrumented with OpenTelemetry

This is a ruby on rails sample application to demonstrate how to auto-instrument a rails application using opentelemetry.

* Ruby version: 3.1.2

* First, start docker with the commands below from the root directory

```shell
~ > cd docker
~/docker > docker-compose up -d
```

* To install dependencies run `bundle install`

* Next, migrate the database: `rails db:migrate`

* Start the
  application: `OTEL_EXPORTER=otlp && export OTEL_EXPORTER_OTLP_TRACES_PROTOCOL=grpc && OTEL_SERVICE_NAME=sampleRailsApp && OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 && OTEL_RESOURCE_ATTRIBUTES=application=sparkapp && ~~~~rails server`

  This runs the rails application at port 3000. Try accessing app at http://localhost:3000/

This sample app was inspired by [this](https://www.digitalocean.com/community/tutorials/how-to-build-a-ruby-on-rails-application) article on digital
ocean.
