# S7 API

Refactoring [s7cli] into a layered API.
The main objectives include

- Creating a stateless high-level interface to Siemens' Simatic API for STEP 7 projects
- Decoupling core logic from application
- Creating a proper way to programmatically interface with the C# API (as opposed to spawning s7cli processes)
- Language-agnostic RPC specification, from which we can auto-generate client-side libraries in Python, Java, ...

[s7cli]: https://gitlab.cern.ch/jpechirr/s7cli/-/tree/master
