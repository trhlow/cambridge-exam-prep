import sys
from pathlib import Path

# The converter lives next to this folder; tests run with `python -I -m pytest`, which keeps the cwd off sys.path.
sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
