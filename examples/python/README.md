# Example Python Step7Service client

## Quickstart

TODO: Update when API is fully implemented and there is no longer need to regenerate gRPC python stubs  

## Generate gRPC code

```sh
./venv/Scripts/activate
cd ~/WORKSPACE/s7cli/examples/python
python -m grpc_tools.protoc ../../Service/protos/step7.proto -I ../../Service/protos --python_out=. --grpc_python_out=.
```
