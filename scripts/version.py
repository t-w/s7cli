import os
from argparse import ArgumentParser


def main():

    parser = ArgumentParser()
    parser.add_argument("-f", "--file", dest="filename",
                        help="path to sharedAssemblyInfo.cs file", metavar="FILE")
    args = parser.parse_args()

    version = "0.10.0.0"
    if os.environ.get("CI_COMMIT_TAG"):
        version = os.environ["CI_COMMIT_TAG"]
    elif os.environ.get("CI_JOB_ID"):
        version = f"0.10.0.{os.environ["CI_JOB_ID"]}"

    content = (
        f'using System.Reflection;\n\n'
        f'[assembly: AssemblyVersion("{version}")]\n'
        f'[assembly: AssemblyFileVersion("{version}")]'
    )

    with open(args.filename,'w') as out:
        out.write(content)


if __name__ == "__main__":
    main()
