"""Local word alignment for existing Gronny recordings; no downloads or audio edits."""
import argparse
import json
import math
import os
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[2]
REVIEW = ROOT / "output/GronnyInstructionReview"
os.environ["HF_HUB_OFFLINE"] = "1"
os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
sys.dont_write_bytecode = True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--first-only", action="store_true")
    args = parser.parse_args()
    import numpy as np
    import soundfile as sf
    import torch
    from scipy.signal import resample_poly
    from transformers import WhisperForConditionalGeneration, WhisperProcessor

    torch.set_num_threads(min(4, os.cpu_count() or 1))
    cache = Path("C:/Users/Trole/Documents/generate_voice/models_verificacion")
    processor = WhisperProcessor.from_pretrained("openai/whisper-base.en", cache_dir=cache, local_files_only=True)
    model = WhisperForConditionalGeneration.from_pretrained(
        "openai/whisper-base.en", cache_dir=cache, local_files_only=True,
        use_safetensors=True, attn_implementation="eager",
    ).eval()
    audit = json.loads((REVIEW / "StaticAudit.json").read_text(encoding="utf-8-sig"))
    qa = json.loads((REVIEW / "WhisperAudioQA.json").read_text(encoding="utf-8-sig"))
    mismatches = {item["id"] for item in qa["files"] if not item["words_match"]}
    entries = [item for item in audit["files"] if item["authored_visual_times"] or item["id"] in mismatches]
    if args.first_only:
        entries = [next(item for item in entries if item["id"] == "controls_grab_hands")]
    results = []
    for entry in entries:
        path = (ROOT / entry["path"]).resolve()
        if not path.is_relative_to(ROOT):
            raise ValueError("Audio must stay inside the project.")
        audio, rate = sf.read(path, dtype="float32", always_2d=True)
        divisor = math.gcd(rate, 16000)
        mono = resample_poly(audio.mean(axis=1), 16000 // divisor, rate // divisor)
        inputs = processor(mono, sampling_rate=16000, return_tensors="pt", return_attention_mask=True)
        with torch.inference_mode():
            generated = model.generate(
                **inputs, num_beams=1, max_new_tokens=256,
                return_timestamps=True, return_token_timestamps=True, return_dict_in_generate=True,
            )
        text, offsets = processor.tokenizer._decode_asr(
            [{"tokens": generated["sequences"], "token_timestamps": generated["token_timestamps"]}],
            return_timestamps="word", return_language=False, time_precision=0.02,
        )
        results.append({"id": entry["id"], "text": text, "words": offsets["chunks"]})
        output = "WordTimingProbe.json" if args.first_only else "WordTimings.json"
        (REVIEW / output).write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
        print(entry["id"] + ": " + text, flush=True)


if __name__ == "__main__":
    main()
