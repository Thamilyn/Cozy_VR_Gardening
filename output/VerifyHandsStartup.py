"""Verify the approved welcome using the existing offline speech recognizer."""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parent.parent
os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
sys.dont_write_bytecode = True
sys.path.insert(0, "C:/Users/Trole/Documents/generate_voice")

import check_audios
import numpy as np
import soundfile as sf
import torch
from scipy.signal import resample_poly
from transformers import WhisperForConditionalGeneration, WhisperProcessor

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--audio", type=Path, default=ROOT / "output/HandsStartupAudio_20261028/01_welcome.wav")
parser.add_argument("--report", type=Path, default=ROOT / "output/HandsStartupAudioQA.json")
args = parser.parse_args()
assert args.audio.resolve().is_relative_to(ROOT) and args.report.resolve().is_relative_to(ROOT)
entries = json.loads((ROOT / "Docs/GronnyHandsStartupDialogues.json").read_text(encoding="utf-8-sig"))
assert len(entries) == 1 and entries[0]["id"] == "01_welcome"
entry = entries[0]
path = args.audio.resolve()
audio, rate = sf.read(path, dtype="float32", always_2d=True)
assert audio.size and np.isfinite(audio).all()
mono = audio.mean(axis=1)
peak = float(np.max(np.abs(audio)))
clipped = float(np.mean(np.abs(audio) >= 0.999))
assert rate == 24000 and 0 < peak < 0.999 and clipped == 0

torch.set_num_threads(min(4, os.cpu_count() or 1))
cache = Path("C:/Users/Trole/Documents/generate_voice/models_verificacion")
model_id = "openai/whisper-base.en"
processor = WhisperProcessor.from_pretrained(model_id, cache_dir=cache, local_files_only=True)
model = WhisperForConditionalGeneration.from_pretrained(
    model_id, cache_dir=cache, local_files_only=True, use_safetensors=True
).eval()
divisor = math.gcd(rate, 16000)
resampled = resample_poly(mono, 16000 // divisor, rate // divisor)
inputs = processor(resampled, sampling_rate=16000, return_tensors="pt", return_attention_mask=True)
with torch.inference_mode():
    tokens = model.generate(**inputs, max_new_tokens=256, num_beams=3)
transcript = processor.batch_decode(tokens, skip_special_tokens=True)[0].strip()
def expand_you_are(text):
    return text.lower().replace("you’re", "you are").replace("you're", "you are")
differences = check_audios.compare(expand_you_are(entry["text"]), expand_you_are(transcript))
name_only = bool(differences) and all(
    item["operation"] == "replace" and item["expected"] == "gronny" and item["transcript"] == "granny"
    for item in differences
)
report = {
    "recognition_model": model_id,
    "expected": entry["text"], "transcript": transcript,
    "differences": differences, "words_match": not differences,
    "name_only_difference": name_only,
    "note": "Automatic transcription; any proper-name difference still needs human listening.",
    "duration_seconds": round(len(mono) / rate, 3), "sample_rate": rate,
    "peak": peak, "clipped_sample_fraction": clipped,
    "sha256": hashlib.sha256(path.read_bytes()).hexdigest().upper(),
}
args.report.resolve().write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps(report, indent=2), flush=True)
assert not differences or name_only, "Review transcription before replacing the welcome."
