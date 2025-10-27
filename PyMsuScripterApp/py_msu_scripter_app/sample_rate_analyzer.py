import os
import time

import librosa
import yaml
from pymusiclooper.audio import MLAudio
from pydub.utils import mediainfo

class SampleRateAnalyzer:
    file: str
    output: str


    def __init__(self, yaml_data: dict, output_path: str):

        self.file = yaml_data["File"]
        self.output = output_path


    def run(self) -> bool:

        print("Running sample rate analysis")

        start_time = time.time()

        from pydub import AudioSegment
        AudioSegment.converter = "/home/matt/.local/share/MSUScripter/dependencies/ffmpeg/bin/ffmpeg"
        AudioSegment.ffmpeg = "/home/matt/.local/share/MSUScripter/dependencies/ffmpeg/bin/ffmpeg"
        AudioSegment.ffprobe = "/home/matt/.local/share/MSUScripter/dependencies/ffmpeg/bin/ffprobe"

        # audio = AudioSegment.from_file(self.file)
        # sr = audio.frame_rate

        os.chdir("/home/matt/.local/share/MSUScripter/dependencies/ffmpeg/bin/")

        try:
            data = mediainfo(self.file)
            sample_rate = int(data['sample_rate'])
            duration = float(data['duration'])
            return self.print_yaml(True, "", sample_rate, duration)
        except Exception as e:
            print(str(e))
            return self.print_yaml(False, f"Error analyzing audio {str(e)}", 0, 0)


    def print_yaml(self, successful: bool, error: str, sample_rate: int, duration: float) -> bool:
        data = dict(
            Successful=successful,
            Error=error,
            SampleRate=sample_rate,
            Duration=duration
        )

        try:
            with open(self.output, 'w') as outfile:
                yaml.dump(data, outfile, default_flow_style=False)
            return successful
        except Exception as e:
            print(e)
            return False