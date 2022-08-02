import os
from packaging.version import parse as parse_version
from argparse import ArgumentParser


def main():

    parser = ArgumentParser()
    parser.add_argument("-f", "--file", dest="filename",
                        help="path to sharedAssemblyInfo.cs file", metavar="FILE")
    args = parser.parse_args()

    version_str = os.environ.get("CI_COMMIT_TAG") or "1.0.0"
    version = parse_version(version_str)
    # See https://docs.microsoft.com/en-us/dotnet/standard/library-guidance/versioning
    # Only include major version in assembly version
    assembly_version = f"{version.major}.0.0.0"
    ci_id = os.environ.get("$CI_PIPELINE_IID") or 0
    assembly_file_version = f"{version.major}.{version.minor}.{version.micro}.{ci_id}"

    content = (
        f'using System.Reflection;\n\n'
        f'[assembly: AssemblyVersion("{assembly_version}")]\n'
        f'[assembly: AssemblyFileVersion("{assembly_file_version}")]'
    )

    with open(args.filename,'w') as out:
        out.write(content)


if __name__ == "__main__":
    main()
