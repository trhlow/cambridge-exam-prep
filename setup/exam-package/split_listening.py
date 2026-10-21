"""Split one Cambridge Listening recording (one mp3 for the whole test) into one track per part.

Usage:
    python -I setup/exam-package/split_listening.py <recording.mp3> [--parts 5] [-o <folder>] [--dry-run]

The package format wants one audio track per part (the candidate screen chains them into one continuous
recording). Cambridge recordings mark the change of part the same way every time: "That is the end of
Part n" is followed by a 10-second pause, the announcement "Now look at Part n+1" and a 5-second pause.
A pause of about 10 seconds with a 5-second pause right after it is therefore a part boundary; the other
10-second pauses (time to read the questions) are not followed so closely. Each track is cut one second
before the end of the long pause, as in the tracks Cambridge supplies for A2 Key for Schools.

B1 Preliminary recordings have no announcement pause: there every 10-second pause ends a part except the last
one, which runs into the transfer time. A2 Key has 5 parts, B1 Preliminary 4. The tracks are written as P1_<name>.mp3 … into --output (default:
audio-parts/ next to the recording), stream-copied without re-encoding. Listen to the start and end of
every track before using them. ffmpeg comes from --ffmpeg, the FFMPEG variable, PATH or the
imageio-ffmpeg package. Keep everything under setup/exam-package/sources/ (git-ignored).
"""

import argparse
import os
import re
import shutil
import site
import subprocess
import sys
from pathlib import Path

SILENCE_END = re.compile(r"silence_end: ([0-9.]+) \| silence_duration: ([0-9.]+)")

# The pause after "That is the end of Part n" and the one after "Now look at Part n+1".
LONG_PAUSE = (9.0, 12.0)
SHORT_PAUSE = (4.0, 6.5)
# From the end of the long pause to the end of the short one (announcement + 5 s): 7–8 s in the samples.
ANNOUNCEMENT_WINDOW = 9.0
CUT_BEFORE_END = 1.0
TRANSFER_PAUSE = 60.0       # the minutes left to copy answers at the end (B1: 5 minutes, then 1)


def _announced_cuts(silences):
    """A2: a long pause with the short pause after "Now look at Part n" right behind it."""
    cuts = []
    for index, (end, duration) in enumerate(silences):
        following = silences[index + 1] if index + 1 < len(silences) else None
        if LONG_PAUSE[0] <= duration <= LONG_PAUSE[1] and following:
            next_end, next_duration = following
            if SHORT_PAUSE[0] <= next_duration <= SHORT_PAUSE[1] and next_end - end <= ANNOUNCEMENT_WINDOW:
                cuts.append(round(end - CUT_BEFORE_END, 3))
    return cuts


def _long_pause_cuts(silences):
    """B1: every long pause ends a part, except the last part's, which runs straight into the transfer time."""
    cuts = []
    for index, (end, duration) in enumerate(silences):
        following = silences[index + 1] if index + 1 < len(silences) else None
        transfer = following and following[1] >= TRANSFER_PAUSE and following[0] - following[1] - end <= ANNOUNCEMENT_WINDOW
        if LONG_PAUSE[0] <= duration <= LONG_PAUSE[1] and not transfer:
            cuts.append(round(end - CUT_BEFORE_END, 3))
    return cuts


def part_boundaries(silences, parts):
    """Cut times (seconds) between parts, from (end, duration) pairs of the pauses in the recording."""
    found = []
    for rule in (_announced_cuts, _long_pause_cuts):
        cuts = rule(silences)
        if len(cuts) == parts - 1:
            return cuts
        found.append(cuts)
    cuts = found[0]
    raise ValueError(
        f"found {len(cuts)} part boundaries, expected {parts - 1}: "
        + (", ".join(f"{cut:.1f} s" for cut in cuts) or "none")
        + ". Split this recording by hand.")


def find_ffmpeg(explicit):
    for candidate in (explicit, os.environ.get("FFMPEG"), shutil.which("ffmpeg")):
        if candidate:
            return candidate
    try:
        import imageio_ffmpeg
        return imageio_ffmpeg.get_ffmpeg_exe()
    except ImportError:
        pass
    # `python -I` leaves the user's site-packages off sys.path; look for imageio-ffmpeg's binary there.
    bundled = sorted((Path(site.getusersitepackages()) / "imageio_ffmpeg" / "binaries").glob("ffmpeg*"))
    if bundled:
        return str(bundled[-1])
    sys.exit("ffmpeg not found: install it, pass --ffmpeg, or `python -m pip install --user imageio-ffmpeg`.")


def detect_silences(ffmpeg, recording):
    result = subprocess.run(
        [ffmpeg, "-hide_banner", "-i", str(recording), "-af", "silencedetect=n=-40dB:d=3", "-f", "null", "-"],
        capture_output=True, text=True, encoding="utf-8", errors="replace", check=True)
    return [(float(end), float(duration)) for end, duration in SILENCE_END.findall(result.stderr)]


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("recording", type=Path, help="the whole Listening test as one mp3")
    parser.add_argument("--parts", type=int, default=5, help="number of parts (A2 Key 5, B1 Preliminary 4)")
    parser.add_argument("-o", "--output", type=Path, help="folder for the tracks (default: audio-parts/ next to the recording)")
    parser.add_argument("--ffmpeg", help="path to ffmpeg")
    parser.add_argument("--dry-run", action="store_true", help="only print where the parts start")
    args = parser.parse_args(argv)

    ffmpeg = find_ffmpeg(args.ffmpeg)
    try:
        cuts = part_boundaries(detect_silences(ffmpeg, args.recording), args.parts)
    except ValueError as error:
        sys.exit(str(error))

    starts = [0.0, *cuts]
    for number, start in enumerate(starts, start=1):
        print(f"Part {number}: starts at {int(start // 60)}:{start % 60:04.1f}")
    if args.dry_run:
        return

    output = args.output or args.recording.parent / "audio-parts"
    output.mkdir(parents=True, exist_ok=True)
    for number, start in enumerate(starts, start=1):
        end = starts[number] if number < len(starts) else None
        target = output / f"P{number}_{args.recording.name}"
        command = [ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-i", str(args.recording), "-ss", f"{start:.3f}"]
        if end is not None:
            command += ["-to", f"{end:.3f}"]
        subprocess.run([*command, "-c", "copy", str(target)], check=True)
    print(f"{len(starts)} tracks written to {output}. Listen to the start and end of each before importing.")


if __name__ == "__main__":
    main()
