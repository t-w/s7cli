# S7 API

Set of utilities to interact with Siemens Step7 PLC projects.

 - S7Lib - C# library written on top of SimaticLib (and S7HCOM_XLib).
 - S7Cli - CLI wrapper of S7Lib.
 - Step7 gRPC Service and C# Server - Service specification and server implementation for operating on S7 projects.

Initially a fork of TE-CRG's [s7cli].

## Installation

Download the latest binaries from the [Releases] page.

## Documentation

The automatic documentation for the latest release is available in https://s7-api.docs.cern.ch/.

It can be generated with `Doxygen` as follows:
```
doxygen Doxyfile
```

[s7cli]: https://gitlab.cern.ch/cryo-controls/utils/s7cli
[Releases]: https://gitlab.cern.ch/industrial-controls/services/plc-automation/s7-api/-/releases
