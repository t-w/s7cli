import grpc

from s7service.step7_pb2 import *
from s7service.step7_pb2_grpc import Step7Stub

import logging
logging.basicConfig(level=logging.DEBUG)
logging.getLogger("grpc").setLevel(logging.WARNING)


SERVICE_HOST = "localhost:50051"


def check_log(log):
    for line in log:
        logging.debug(line.rstrip('\n'))


def check_reply(reply):
    if isinstance(reply, StatusReply):
        logging.debug(f"Received {{ exitCode={reply.status.exitCode} }}")
        check_log(reply.log)

    if isinstance(reply, ListReply):
        logging.debug(f"Received {{ exitCode={reply.status.exitCode}, items={reply.items} }}")
        check_log(reply.status.log)


def run():
    with grpc.insecure_channel(SERVICE_HOST) as channel:
        stub = Step7Stub(channel)
        req = ListProjectsRequest()
        check_reply(stub.ListProjects(req))
        req = ListProgramsRequest(project="ZEn01_10_STEP7__Com_SFB")
        check_reply(stub.ListPrograms(req))
        req = ListContainersRequest(project="ZEn01_10_STEP7__Com_SFB")
        check_reply(stub.ListContainers(req))
        req = ListStationsRequest(project="ZEn01_10_STEP7__Com_SFB")
        check_reply(stub.ListStations(req))


if __name__ == '__main__':
    logging.basicConfig()
    run()
