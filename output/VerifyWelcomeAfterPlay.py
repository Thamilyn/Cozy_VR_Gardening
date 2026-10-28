"""Check the new welcome with the existing offline recognizer."""
import json
import os
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parent.parent
os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
sys.dont_write_bytecode = True
sys.path.insert(0, "C:/Users/Trole/Documents/generate_voice")
import check_audios

entries = json.loads((ROOT / "Docs/GronnyWelcomeDialogues.json").read_text(encoding="utf-8-sig"))
check_audios.dialogues_instructions = [
    {"filename": str(ROOT / "output/WelcomeAfterPlayFinalAudio" / (entry["id"] + ".wav")), "text": entry["text"]}
    for entry in entries
]
check_audios.REPORT = ROOT / "output/WelcomeAfterPlayVoiceVerification.json"
check_audios.main()
report = json.loads(check_audios.REPORT.read_text(encoding="utf-8"))
assert len(report["files"]) == 1
assert all(item["words_match"] and item["peak"] > 0 and item["clipped_sample_fraction"] == 0 for item in report["files"])
