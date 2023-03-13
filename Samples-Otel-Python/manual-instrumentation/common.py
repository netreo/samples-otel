import os
import string

from opentelemetry import trace
from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
from opentelemetry.sdk.resources import SERVICE_NAME, Resource
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import BatchSpanProcessor

def get_otlp_exporter():
    return OTLPSpanExporter()


def get_tracer():
    span_exporter = get_otlp_exporter()
    
    provider = TracerProvider()
    if not os.environ.get("OTEL_RESOURCE_ATTRIBUTES"):        
        # Service name is required for most backends
        resource = Resource(attributes={
            SERVICE_NAME: "test-py-manual-otlp"
        })
        provider = TracerProvider(resource=resource)
        print("Using default service name")
        
    processor = BatchSpanProcessor(span_exporter)
    provider.add_span_processor(processor)
    trace.set_tracer_provider(provider)    

    return trace.get_tracer(__name__)
