"""Comprueba un lote con Whisper ya descargado; no instala ni descarga modelos."""

import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import runpy
import sys

ROOT = Path(__file__).resolve().parent.parent
EXISTING = Path("C:/Users/Trole/Documents/generate_voice")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dialogues", type=Path, required=True)
    parser.add_argument("--audio-dir", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    for path in (args.dialogues, args.audio_dir, args.report):
        if not path.resolve().is_relative_to(ROOT):
            parser.error("Todos los archivos del lote deben estar dentro del proyecto.")
    os.environ["HF_HUB_OFFLINE"] = "1"
    os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
    sys.dont_write_bytecode = True
    sys.path.insert(0, str(EXISTING))
    compare = runpy.run_path(str(EXISTING / "check_audios.py"), run_name="existing_audio_checks")["compare"]
    import numpy as np
    import soundfile as sf
    import torch
    from scipy.signal import resample_poly
    from transformers import WhisperForConditionalGeneration, WhisperProcessor

    torch.set_num_threads(min(4, os.cpu_count() or 1))
    cache = EXISTING / "models_verificacion"
    model_id = "openai/whisper-base.en"
    processor = WhisperProcessor.from_pretrained(model_id, cache_dir=cache, local_files_only=True)
    model = WhisperForConditionalGeneration.from_pretrained(
        model_id, cache_dir=cache, local_files_only=True, use_safetensors=True
    ).eval()
    report = {"recognition_model": model_id, "note": "Revisión automática; no sustituye escucha humana.", "files": []}
    for entry in json.loads(args.dialogues.read_text(encoding="utf-8-sig")):
        path = (args.audio_dir / entry.get("path", entry["id"] + ".wav")).resolve()
        if not path.is_relative_to(ROOT):
            raise ValueError(f"Audio fuera del proyecto: {path}")
        audio, rate = sf.read(path, dtype="float32", always_2d=True)
        if not audio.size or not np.isfinite(audio).all():
            raise ValueError(f"Audio vacío o inválido: {path}")
        mono = audio.mean(axis=1)
        divisor = math.gcd(rate, 16000)
        inputs = processor(resample_poly(mono, 16000 // divisor, rate // divisor),
                           sampling_rate=16000, return_tensors="pt", return_attention_mask=True)
        with torch.inference_mode():
            tokens = model.generate(**inputs, max_new_tokens=256, num_beams=3)
        heard = processor.batch_decode(tokens, skip_special_tokens=True)[0].strip()
        differences = compare(entry["text"], heard)
        result = {"id": entry["id"], "expected": entry["text"], "transcript": heard,
                  "differences": differences, "words_match": not differences,
                  "duration_seconds": round(len(mono) / rate, 3), "sample_rate": rate,
                  "peak": float(np.max(np.abs(mono))),
                  "clipped_sample_fraction": float(np.mean(np.abs(mono) >= 0.999)),
                  "sha256": hashlib.sha256(path.read_bytes()).hexdigest().upper()}
        report["files"].append(result)
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"{entry['id']}: {heard} | diferencias: {differences}", flush=True)


if __name__ == "__main__":
    main()
