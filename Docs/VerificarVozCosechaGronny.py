"""Check the three generated WAVs using the existing offline recognition model."""
import hashlib
import json
import math
import os
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "output/GronnyHarvestVoice_20261108"
os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
os.environ["TRANSFORMERS_OFFLINE"] = "1"
sys.dont_write_bytecode = True
for variable in ("TMP", "TEMP", "TMPDIR"):
    os.environ[variable] = str(OUTPUT / "temp")


def words(text):
    return re.findall(r"[a-z]+", text.lower().replace("you've", "you have").replace("you’ve", "you have"))


def main():
    import numpy as np
    import soundfile as sf
    import torch
    from scipy.signal import resample_poly
    from transformers import WhisperForConditionalGeneration, WhisperProcessor

    cache = "C:/Users/Trole/Documents/generate_voice/models_verificacion"
    processor = WhisperProcessor.from_pretrained("openai/whisper-base.en", cache_dir=cache, local_files_only=True)
    model = WhisperForConditionalGeneration.from_pretrained(
        "openai/whisper-base.en", cache_dir=cache, local_files_only=True, use_safetensors=True,
    ).eval()
    torch.set_num_threads(min(4, os.cpu_count() or 1))
    entries = json.loads((ROOT / "Docs/GronnyHarvestDialogues.json").read_text(encoding="utf-8-sig"))
    results = []
    for entry in entries:
        path = OUTPUT / (entry["id"] + ".wav")
        audio, rate = sf.read(path, dtype="float32", always_2d=True)
        if not audio.size or not np.isfinite(audio).all():
            raise RuntimeError("Invalid signal: " + entry["id"])
        mono = audio.mean(axis=1)
        divisor = math.gcd(rate, 16000)
        inputs = processor(resample_poly(mono, 16000 // divisor, rate // divisor),
            sampling_rate=16000, return_tensors="pt", return_attention_mask=True)
        with torch.inference_mode():
            tokens = model.generate(**inputs, max_new_tokens=192, num_beams=3)
        transcript = processor.batch_decode(tokens, skip_special_tokens=True)[0].strip()
        results.append(dict(id=entry["id"], expected=entry["text"], transcript=transcript,
            words_match=words(entry["text"]) == words(transcript), seconds=len(mono) / rate,
            sample_rate=rate, channels=audio.shape[1], peak=float(np.max(np.abs(mono))),
            clipped_samples=int(np.sum(np.abs(mono) >= 0.999)),
            sha256=hashlib.sha256(path.read_bytes()).hexdigest().upper()))
        print(json.dumps(results[-1]), flush=True)
    (OUTPUT / "AudioQA.json").write_text(json.dumps(dict(
        recognition="Whisper base.en offline", note="Automatic transcription does not evaluate naturalness.",
        recordings=results), indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
