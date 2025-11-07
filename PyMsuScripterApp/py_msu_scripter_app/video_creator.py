import threading
import time

import yaml
from proglog import ProgressBarLogger

try:
    import json
    import os
    import re
    import sys
    from pydub import AudioSegment
    from moviepy import AudioFileClip, ColorClip
except ImportError as e:
    print(e)
    print("Please install the following packages:")
    print("json")
    print("os")
    print("pydub")
    print("moviepy")
    exit()


class VideoCreator:
    files: list
    output_video: str
    output: str
    phase: int = 0
    phase_progress: float = 0
    progress_file: str

    def __init__(self, yaml_data, output):
        self.files = yaml_data["Files"]
        self.output_video = yaml_data["OutputVideo"]
        self.progress_file = yaml_data["ProgressFile"]
        self.output = output

    def run(self) -> bool:

        if not self.files:
            return self.print_yaml(False, "No tracks marked for inclusion")

        pattern = re.compile("\\.mp4$", re.IGNORECASE)

        if not self.output_video:
            return self.print_yaml(False, "Output video file missing")

        if not pattern.findall(self.output_video):
            return self.print_yaml(False, "Invalid output file")

        for track_file in self.files:
            if not os.path.exists(track_file):
                return self.print_yaml(False, f"Track file {track_file} does not exist. Exiting.")

        stop_event = threading.Event()
        t = threading.Thread(target=self.writer_thread, args=(self.progress_file, stop_event), daemon=True)

        try:
            t.start()

            output_mp4 = self.output_video
            output_wav = pattern.sub(".wav", output_mp4)

            print(f"Writing to {output_wav} and {output_mp4}")

            combined_track = AudioSegment.empty()
            for track_file in self.files:
                audio = AudioSegment.from_file(track_file, format="pcm", frame_rate=44100, channels=2, sample_width=2)
                combined_track += audio

            combined_track.export(output_wav, format="wav")

            logger = MyBarLogger(self)

            with AudioFileClip(output_wav) as audio_clip:
                with ColorClip(size=(720, 576), color=(0, 0, 0), duration=audio_clip.duration) as video_clip:
                    video_clip = video_clip.with_audio(audio_clip)
                    video_clip.write_videofile(output_mp4, fps=24, logger=logger)

            return self.print_yaml(True, "")
        except Exception as e:
            print(f"Error creating wav or mp4 file: {str(e)}", file=sys.stderr)
            return self.print_yaml(False, f"Error creating wav or mp4 file {str(e)}")
        finally:
            stop_event.set()
            t.join(timeout=2)
            try:
                audio_clip.close()
                video_clip.close()
            except Exception:
                pass

    def print_yaml(self, successful: bool, error: str) -> bool:
        data = dict(
            Successful=successful,
            Error=error,
        )

        try:
            with open(self.output, 'w') as outfile:
                yaml.dump(data, outfile, default_flow_style=False)
            return successful
        except Exception as e:
            print(e)
            return False

    def writer_thread(self, filename, stop_event):
        while not stop_event.is_set():
            try:
                with open(filename, "w") as f:
                    print(f"writer thread {self.phase} {self.phase_progress}/100")
                    f.write(f"{self.phase}|{self.phase_progress}\n")
                    f.flush()  # ensure immediate write
            except Exception:
                pass
            time.sleep(0.5)  # 500 ms


class MyBarLogger(ProgressBarLogger):
    source: VideoCreator

    def __init__(self, source: VideoCreator):
        super().__init__()
        self.source = source

    def bars_callback(self, bar, attr, value, old_value=None):
        percentage = (value / self.bars[bar]['total']) * 100
        if bar == "chunk":
            self.source.phase = 1
            self.source.phase_progress = percentage
            # print(f"Audio: {percentage}/100")
        elif bar == "frame_index":
            self.source.phase = 2
            self.source.phase_progress = percentage
            # print(f"Video: {percentage}/100")
