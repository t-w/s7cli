# coding=utf-8
from setuptools import setup, find_packages
import os

version = "0.10.0.0"
if os.environ.get("CI_COMMIT_TAG"):
    version = os.environ["CI_COMMIT_TAG"]


setup(
    name='s7service',
    version=version,
    description='Python gRPC stubs for Step 7 Service',
    url='',
    author='João Borrego',
    author_email='joao.borrego@cern.ch',
    license='CERN',
    packages=find_packages(),
)
