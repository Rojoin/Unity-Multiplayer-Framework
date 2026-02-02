# Unity Multiplayer Reflection Framework

Experimental multiplayer framework for **Unity** that synchronizes data over the network using **UDP** and **C# Reflection**.

The system uses reflection on the backend to detect variable types at runtime and send it using UDP. It supports both reference and value types without the need of custom messages.For Unity specfics structs and classes the serialization is handled through **extension methods**, allowing explicit control over how each type is serialized and deserialized.

This project started as a **2024 Image Campus project** and later evolved into an independent experiment.

---

## Features

- Reflection-based backend to inspect variable types
- Custom serialization for structs and classes via extension methods
- UDP transport for low-latency communication
- Packet checksum for data validation
- Automatic synchronization of supported variables

---

## Planned

- Cross-referenced objects
- Auth server
- Dictionary support
- TCP protocol support

---

## License

MIT
