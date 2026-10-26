"""Genera el lote nuevo con el generador local existente, sin instalar paquetes.

Ejecutar con el Python del entorno virtual de Documents/generate_voice.
El generador original se lee, pero no se modifica.
"""

import argparse
import json
import os
from pathlib import Path
import runpy
import sys


PROJECT_ROOT = Path(__file__).resolve().parent.parent
SOURCE_SCRIPT = Path("C:/Users/Trole/Documents/generate_voice/generate_voice.py")
DIALOGUES_FILE = PROJECT_ROOT / "Docs/GronnyMenuYAccesibilidad.json"
DEFAULT_OUTPUT = PROJECT_ROOT / "Assets/Audio/menu_accessibility"


def main():
    if not sys.flags.utf8_mode:
        raise SystemExit("Ejecuta Python con -X utf8 antes de la ruta de este archivo.")

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--limit", type=int, help="Generar solo las primeras N frases.")
    parser.add_argument(
        "--output-dir", type=Path, default=DEFAULT_OUTPUT,
        help="Carpeta nueva dentro del proyecto; no se sobrescriben WAV existentes.",
    )
    args = parser.parse_args()
    if args.limit is not None and args.limit < 1:
        parser.error("--limit debe ser mayor que cero.")

    output_dir = args.output_dir.resolve()
    if not output_dir.is_relative_to(PROJECT_ROOT):
        parser.error("La carpeta de salida debe estar dentro de Cozy_VR_Gardening.")
    if not SOURCE_SCRIPT.is_file():
        parser.error(f"No se encuentra el generador existente: {SOURCE_SCRIPT}")

    entries = json.loads(DIALOGUES_FILE.read_text(encoding="utf-8"))
    ids = [entry["id"] for entry in entries]
    if len(ids) != len(set(ids)):
        parser.error("El JSON contiene identificadores repetidos.")
    if any(not name or any(c not in "abcdefghijklmnopqrstuvwxyz0123456789_" for c in name)
           for name in ids):
        parser.error("Los identificadores solo deben contener letras minúsculas, números y _.")
    if any(not isinstance(entry.get("text"), str) or not entry["text"].strip()
           for entry in entries):
        parser.error("Cada diálogo necesita texto no vacío.")

    selected = entries[:args.limit]
    if not selected:
        parser.error("El lote está vacío.")
    existing = [output_dir / (entry["id"] + ".wav") for entry in selected
                if (output_dir / (entry["id"] + ".wav")).exists()]
    if existing:
        parser.error("Ya existen WAV del lote. Elige otra carpeta con --output-dir: "
                     + ", ".join(path.name for path in existing))

    # Reutilizar los modelos descargados y no crear bytecode junto al script ajeno.
    os.environ["HF_HUB_OFFLINE"] = "1"
    os.environ["HF_HUB_DISABLE_TELEMETRY"] = "1"
    sys.dont_write_bytecode = True

    loaded = runpy.run_path(str(SOURCE_SCRIPT), run_name="gronny_existing_generator")
    generate = loaded["main"]
    if generate.__globals__.get("MODEL_REVISION") != "v2":
        parser.error("La revisión del generador ha cambiado; revisar antes de generar.")
    generate.__globals__["dialogues_instructions"] = [
        {"filename": entry["id"] + ".wav", "text": entry["text"]}
        for entry in selected
    ]
    print(f"Guion: {DIALOGUES_FILE}", flush=True)
    print(f"Salida: {output_dir}", flush=True)
    # El generador existente conserva Kiki, expressive y temperature=1.1.
    sys.argv = [str(SOURCE_SCRIPT), "--output-dir", str(output_dir)]
    generate()


if __name__ == "__main__":
    main()
