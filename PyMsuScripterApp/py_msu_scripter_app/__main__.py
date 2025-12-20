import logging
import argparse
import yaml
import os.path

from py_msu_scripter_app.py_music_looper_runner import PyMusicLooperRunner
from py_msu_scripter_app.sample_rate_analyzer import SampleRateAnalyzer
from video_creator import VideoCreator
from version import Version

def cli():
    try:
        parser = argparse.ArgumentParser("py_msu_scripter_app",
                                         description="Commandline Python application for the MSU Scripter app")
        parser.add_argument("-i", "--input", help="Input YAML file with a list of all PCM files to include", type=str)
        parser.add_argument("-o", "--output", help="Output YAML file with the results of running the application", type=str)
        parser.add_argument("-v", "--version", help="Get the version number", action='store_true')
        args = parser.parse_args()

        if args.version:
            print("py_msu_scripter_app v" + Version.name())
            exit(1)

        if not args.input:
            print("usage: py_msu_scripter_app [-i INPUT] [-o OUTPUT] [-v]")
            print("Error: you must include either the argument -i/--input")
            exit(1)

        if not args.output:
            print("usage: py_msu_scripter_app [-i INPUT] [-o OUTPUT] [-v]")
            print("Error: the following arguments are required: -o/--output")
            exit(1)

        if not os.path.isfile(args.input):
            print("Error: the input YAML file was not found")
            exit(1)

        with open(args.input, "r", encoding="utf-8") as stream:
            try:
                yaml_file = yaml.safe_load(stream)
            except yaml.YAMLError as exc:
                print("Error: could not parse input YAML file")
                exit(1)

        mode = yaml_file["Mode"]
        result = False

        if mode == "samples":
            samples = SampleRateAnalyzer(yaml_file, args.output)
            result = samples.run()
        elif mode == "create_video":
            video_creator = VideoCreator(yaml_file, args.output)
            result = video_creator.run()
        elif mode == "py_music_looper":
            py_music_looper = PyMusicLooperRunner(yaml_file, args.output)
            result = py_music_looper.run()

        if result:
            exit(0)
        else:
            exit(1)

    except Exception as e:
        logging.error(e)


if __name__ == "__main__":
    cli()