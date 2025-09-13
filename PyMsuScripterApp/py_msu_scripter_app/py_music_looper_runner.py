import os

import yaml
from pymusiclooper.core import MusicLooper


class PyMusicLooperRunner:
    file: str
    output: str
    min_duration_multiplier: float = 0.35
    min_loop_duration: float | None = None
    max_loop_duration: float | None = None
    approx_loop_start: float | None = None
    approx_loop_end: float | None = None
    brute_force: bool = False
    disable_pruning: bool = False


    def __init__(self, yaml_data: dict, output_path: str):
        self.file = yaml_data["File"]
        self.output = output_path
        self.min_duration_multiplier = yaml_data["MinDurationMultiplier"]
        self.min_loop_duration = yaml_data["MinLoopDuration"]
        self.max_loop_duration = yaml_data["MaxLoopDuration"]
        self.approx_loop_start = yaml_data["ApproxLoopStart"]
        self.approx_loop_end = yaml_data["ApproxLoopEnd"]


    def run(self):
        if not self.file or not os.path.exists(self.file):
            return self.print_yaml(False, "No input file found")

        try:
            print("Running PyMusicLooper")
            looper = MusicLooper(self.file)
            pairs = looper.find_loop_pairs(
                self.min_duration_multiplier,
                self.min_loop_duration,
                self.max_loop_duration,
                self.approx_loop_start,
                self.approx_loop_end,
                self.brute_force,
                self.disable_pruning)
            pair_objects = []
            for pair in pairs:
                pair_dict = dict(
                    LoopStart=pair.loop_start,
                    LoopEnd=pair.loop_end,
                    Score=format(pair.score, '.10f'),
                    NoteDistance=format(pair.note_distance, '.10f'),
                    LoudnessDifference=format(pair.loudness_difference, '.10f')
                )
                pair_objects.append(pair_dict)
            return self.print_yaml(True, "", pair_objects)
        except Exception as e:
            print(f"Error running PyMusicLooper: {str(e)}")
            return self.print_yaml(False, f"Error running PyMusicLooper: {str(e)}")


    def print_yaml(self, successful: bool, error: str, pairs: list | None = None) -> bool:
        if not pairs:
            pairs = []

        data = dict(
            Successful=successful,
            Error=error,
            Pairs=pairs
        )

        try:
            with open(self.output, 'w') as outfile:
                yaml.dump(data, outfile, default_flow_style=False)
            return successful
        except Exception as e:
            print(e)
            return False