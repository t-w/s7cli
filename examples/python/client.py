import logging

import grpc

from step7_pb2 import CreateProjectRequest
from step7_pb2_grpc import Step7Stub

SERVICE_HOST = "localhost:50051"
PROJECT_NAME = "Step7ProjectName"
PROJECT_DIR = "C:/jpechirr/Workspace/newProj"


def run():
    with grpc.insecure_channel(SERVICE_HOST) as channel:
        stub = Step7Stub(channel)
        request = CreateProjectRequest(projectName=PROJECT_NAME, projectDir=PROJECT_DIR)
        response = stub.CreateProject(request)
    print("Example client received: " + str(response.exitCode))


if __name__ == '__main__':
    logging.basicConfig()
    run()
