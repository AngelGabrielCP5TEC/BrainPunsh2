import os
import sys
import argparse
from collections import deque

import numpy as np
import pandas as pd
from scipy.signal import iirnotch, butter, filtfilt, welch

try:
    import pylsl
    from pylsl import StreamInlet
except ImportError:
    sys.exit("[ERROR] Instala pylsl: pip install pylsl")


# =========================
# PARÁMETROS
# =========================

FS = 250
WINDOW_SIZE = 500      # 2 segundos
STEP_SIZE = 25        # procesa cada 0.1 s
FMIN, FMAX = 8.0, 12.0
DEBOUNCE_COUNT = 3

# Python empieza en 0:
# Canal 2 = índice 1
# Canal 4 = índice 3
CH_C3 = 1
CH_C4 = 3


# =========================
# FILTROS Y POTENCIA
# =========================

def build_filters(fs, f_notch=60.0):
    nyq = fs / 2.0
    b_n, a_n = iirnotch(f_notch, 30.0, fs)
    b_b, a_b = butter(4, [FMIN / nyq, FMAX / nyq], btype="band")
    return b_n, a_n, b_b, a_b


def get_band_power(data, fs, b_n, a_n, b_b, a_b):
    data = np.asarray(data, dtype=float)

    if len(data) < 10:
        return np.nan

    sig = filtfilt(b_n, a_n, data)
    sig = filtfilt(b_b, a_b, sig)

    freqs, psd = welch(sig, fs=fs, nperseg=min(len(sig), fs))
    mask = (freqs >= FMIN) & (freqs <= FMAX)

    return float(np.trapezoid(psd[mask], freqs[mask]))


def compute_erd(power, baseline_power):
    if baseline_power <= 0 or np.isnan(baseline_power):
        return np.nan

    return (power - baseline_power) / baseline_power


# =========================
# CALIBRACIÓN
# =========================

def load_calibration_from_csv(path, fs, b_n, a_n, b_b, a_b):
    if not os.path.exists(path):
        sys.exit(f"[ERROR] No existe el archivo de calibración: {path}")

    df = pd.read_csv(path)
    df.columns = df.columns.str.strip()

    required_cols = ["Channel 2", "Channel 4"]

    for col in required_cols:
        if col not in df.columns:
            sys.exit(
                f"[ERROR] No encontré la columna '{col}'. "
                f"Columnas disponibles: {list(df.columns)}"
            )

        df[col] = pd.to_numeric(df[col], errors="coerce")

    df = df.dropna(subset=required_cols)

    c3 = df["Channel 2"].to_numpy(dtype=float)
    c4 = df["Channel 4"].to_numpy(dtype=float)

    ref_c3 = get_band_power(c3, fs, b_n, a_n, b_b, a_b)
    ref_c4 = get_band_power(c4, fs, b_n, a_n, b_b, a_b)

    if ref_c3 <= 0 or ref_c4 <= 0 or np.isnan(ref_c3) or np.isnan(ref_c4):
        sys.exit("[ERROR] La potencia de baseline salió 0, negativa o inválida.")

    print(f"[CALIB] Baseline C3 / Channel 2: {ref_c3:.6f}", file=sys.stderr)
    print(f"[CALIB] Baseline C4 / Channel 4: {ref_c4:.6f}", file=sys.stderr)

    return ref_c3, ref_c4


# =========================
# LSL
# =========================

def find_lsl_stream():
    print("[LSL] Buscando streams disponibles...", file=sys.stderr)

    streams = pylsl.resolve_streams()

    if not streams:
        sys.exit("[ERROR] No se detectó ningún stream LSL.")

    selected_stream = None

    for info in streams:
        print(
            f"[LSL] Stream encontrado: "
            f"name={info.name()}, "
            f"type={info.type()}, "
            f"channels={info.channel_count()}, "
            f"fs={info.nominal_srate()}, "
            f"format={info.channel_format()}",
            file=sys.stderr
        )

        if (
            info.nominal_srate() != pylsl.IRREGULAR_RATE
            and info.channel_format() != pylsl.cf_string
            and info.channel_count() >= 4
        ):
            selected_stream = info
            break

    if selected_stream is None:
        sys.exit("[ERROR] No encontré un stream numérico con al menos 4 canales.")

    print(f"[LSL] Usando stream: {selected_stream.name()}", file=sys.stderr)

    return StreamInlet(
        selected_stream,
        max_buflen=5,
        processing_flags=pylsl.proc_clocksync | pylsl.proc_dejitter
    )


# =========================
# CLASIFICADOR HEURÍSTICO
# =========================

class BCIClassifier:
    def __init__(self, threshold=0.05):
        self.threshold = threshold
        self.last_raw_state = "00"
        self.stable_state = "00"
        self.counter = 0

    def classify(self, erd_c3, erd_c4):
        if np.isnan(erd_c3) or np.isnan(erd_c4):
            current = "00"
        else:
            score = erd_c4 - erd_c3

            if score < -self.threshold:
                current = "01"  # izquierda
            elif score > self.threshold:
                current = "02"  # derecha
            else:
                current = "00"

        # Debounce: actualiza el estado estable solo si se repite
        if current == self.last_raw_state:
            self.counter += 1
            if self.counter >= DEBOUNCE_COUNT:
                self.stable_state = current
        else:
            self.last_raw_state = current
            self.counter = 1

        # Imprime SIEMPRE el estado estable
        print(self.stable_state, flush=True)
        


# =========================
# LOOP PRINCIPAL
# =========================

def run_detector(calibration_csv, threshold):
    b_n, a_n, b_b, a_b = build_filters(FS)

    ref_c3, ref_c4 = load_calibration_from_csv(
        calibration_csv, FS, b_n, a_n, b_b, a_b
    )

    inlet = find_lsl_stream()

    buffer_c3 = deque(maxlen=WINDOW_SIZE)
    buffer_c4 = deque(maxlen=WINDOW_SIZE)

    classifier = BCIClassifier(threshold=threshold)

    sample_counter = 0

    print("[BCI] Detector activo.", file=sys.stderr)
    print("[BCI] Salidas: 00=neutral, 01=left, 02=right", file=sys.stderr)

    try:
        while True:
            sample, _ = inlet.pull_sample()

            if sample is None:
                continue

            if len(sample) <= max(CH_C3, CH_C4):
                print("[ERROR] El stream tiene menos canales de los esperados.", file=sys.stderr)
                continue

            buffer_c3.append(sample[CH_C3])
            buffer_c4.append(sample[CH_C4])

            sample_counter += 1

            if len(buffer_c3) == WINDOW_SIZE and sample_counter >= STEP_SIZE:
                sample_counter = 0

                p_c3 = get_band_power(np.array(buffer_c3), FS, b_n, a_n, b_b, a_b)
                p_c4 = get_band_power(np.array(buffer_c4), FS, b_n, a_n, b_b, a_b)

                erd_c3 = compute_erd(p_c3, ref_c3)
                erd_c4 = compute_erd(p_c4, ref_c4)

                classifier.classify(erd_c3, erd_c4)

    except KeyboardInterrupt:
        print("\n[BCI] Detenido.", file=sys.stderr)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()

    parser.add_argument(
        "--calibration",
        required=True,
        help="Ruta del CSV de calibración con columnas Channel 2 y Channel 4"
    )

    parser.add_argument(
        "--threshold",
        type=float,
        default=0.05,
        help="Umbral para score = ERD_C4 - ERD_C3"
    )

    args = parser.parse_args()

    run_detector(args.calibration, args.threshold)