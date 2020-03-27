import logging

import grpc

from step7_pb2 import *
from step7_pb2_grpc import Step7Stub

SERVICE_HOST = "localhost:50051"


def run():
    with grpc.insecure_channel(SERVICE_HOST) as channel:
        stub = Step7Stub(channel)
        req = ListProjectsRequest()
        res = stub.ListProjects(req)
        print(f'Received {res.status.exitCode}')
        for line in res.status.log:
            print(line.replace("\n", ""))
 

if __name__ == '__main__':
    logging.basicConfig()
    run()
