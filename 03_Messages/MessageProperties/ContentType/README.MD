# RabbitMQ ContentType Messaging Demo

This repository contains a simple C# application that demonstrates messaging with RabbitMQ using multiple content types (JSON and XML). It shows how messages can be serialized and deserialized dynamically based on the `ContentType` header.

Let's assume our sensor data producers are diverse systems spread throughout a large facility. One modern device sends its sensor readings in JSON, while an older legacy system prefers XML. Both systems, despite their differences, channel their data into a single RabbitMQ queue. Picture a central control room where a consumer waits patiently, inspecting each incoming message’s `ContentType` header to decide the right way to interpret it—much like an interpreter fluently translating between languages on the fly.

This scenario is ideal for demonstrating content negotiation in messaging systems. It highlights how a unified consumer can seamlessly manage and process data from various sources and formats, addressing a common real-world challenge where different systems must communicate reliably despite their inherent differences.
