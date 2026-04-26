from pylsl import StreamInlet, resolve_stream
import numpy as np
import os
import sys
import TCP_server
from scipy.signal import welch, butter, filtfilt
from collections import deque
import pandas as pd
from scipy.signal import iirnotch

## PARAMETROS
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


BASE_DIR = os.path.dirname(os.path.abspath(__file__))
sys.path.append(BASE_DIR)

## FUNCIONES PARA MOTOR IMAGINATION
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

class BCIClassifier:
    def __init__(self, threshold=0.05):
        self.threshold = threshold
        self.last_raw_state = 0
        self.stable_state = 0
        self.counter = 0

    def classify(self, erd_c3, erd_c4):
        if np.isnan(erd_c3) or np.isnan(erd_c4):
            current = 0
        else:
            score = erd_c4 - erd_c3

            if score < -self.threshold:
                current = 1  # izquierda
            elif score > self.threshold:
                current = 2  # derecha
            else:
                current = 0

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
        return self.stable_state
        




#vars cindy sofi
# BANDAS
theta_band = (4.0, 6.39)
alpha_band = (8.0, 10.30)
beta_band  = (17.02, 23.02)

# VARIABLES
fs          = 250
window_size = 500

# FILTRO
FILTER_LOW   = 3.0
FILTER_HIGH  = 25.0
FILTER_ORDER = 4

columns    = ['Time', 'FZ', 'CZ', 'PZ']
total_data = {k: [] for k in columns}
history    = []

MIN_ENG = 0.2
MAX_ENG = 1.5

# índices Unicorn
ax_idx = 8
ay_idx = 9
gx_idx = 12
gy_idx = 11

#calibración 
calib_path = os.path.join(BASE_DIR,"calib_cursor.npy")

if not os.path.exists(calib_path):
    raise FileNotFoundError(
        f"No existe {calib_path}. Ejecuta primero calibración."
    )

params=np.load(calib_path)

(
acc_bias_x,
acc_bias_y,
gyro_bias_x,
gyro_bias_y,

acc_x_min,
acc_x_max,
acc_y_min,
acc_y_max,

gyro_x_min,
gyro_x_max,
gyro_y_min,
gyro_y_max

)=params

def prepare_filter(fs):
    nyq  = 0.5 * fs
    b, a = butter(FILTER_ORDER, [FILTER_LOW/nyq, FILTER_HIGH/nyq], btype='band')
    return b, a

b, a = prepare_filter(fs)

def apply_filter(data):
    return filtfilt(b, a, data)

def bandpower(psd, freqs, band):
    idx = (freqs >= band[0]) & (freqs <= band[1])
    return np.trapz(psd[idx], freqs[idx])


def normalize(v,vmin,vmax):

    if abs(vmax-vmin)<1e-9:
        return 0.0

    u=2*(v-vmin)/(vmax-vmin)-1

    return np.clip(u,-1.0,1.0)

# ---------------- STREAM LSL ----------------

streams=resolve_stream()

if not streams:
    raise RuntimeError(
      "No se encontraron streams LSL"
    )

inlet=StreamInlet(streams[0])

FZ_IDX = 0
CZ_IDX = 2
PZ_IDX = 4

# -------- suavizado ----------
x_prev=0.0
y_prev=0.0

alpha=0.8

# -------- pesos fusion sensor ----------
w_acc=0.7
w_gyro=0.3

# Inicialización para ERD
CALIBRATION_CSV = os.path.join(BASE_DIR, "raw_baseline_data.csv") #LYON, CORRIGE ESTO
ERD_THRESHOLD = 0.05

b_n, a_n, b_b, a_b = build_filters(fs)

ref_c3, ref_c4 = load_calibration_from_csv(
    CALIBRATION_CSV, fs, b_n, a_n, b_b, a_b
)

buffer_c3 = deque(maxlen=window_size)
buffer_c4 = deque(maxlen=window_size)

erd_classifier = BCIClassifier(threshold=ERD_THRESHOLD)

sample_counter = 0
STEP_SIZE = 25

while True:

    sample, timestamp =inlet.pull_sample()
    buffer_c3.append(sample[1])  # C3 / Channel 2
    buffer_c4.append(sample[3])  # C4 / Channel 4
    sample_counter += 1
    total_data['Time'].append(timestamp)
    total_data['FZ'].append(sample[FZ_IDX])
    total_data['CZ'].append(sample[CZ_IDX])
    total_data['PZ'].append(sample[PZ_IDX])


    if len(total_data['Time']) >= window_size:
            theta = alpha_power = beta = 0

            for ch in columns[1:]:
                data = np.array(total_data[ch][-window_size:])
                data = apply_filter(data)

                freqs, psd = welch(data, fs, nperseg=250, noverlap=125)
                theta += bandpower(psd, freqs, theta_band)
                alpha_power += bandpower(psd, freqs, alpha_band)
                beta  += bandpower(psd, freqs, beta_band)

            n      = len(columns) - 1
            theta /= n
            alpha_power /= n
            beta  /= n

            # ENGAGEMENT ABSOLUTO — sin baseline, sin normalización
            engagement = beta / (alpha_power + theta + 1e-6)

            # SUAVIZADO con ventana más amplia (30 muestras)
            history.append(engagement)
            if len(history) > 30:
                history.pop(0)

            engagement_smooth = np.mean(history)

            engagement_scaled = (engagement_smooth - MIN_ENG) / (MAX_ENG - MIN_ENG)
            engagement_scaled = max(0, min(1, engagement_scaled))

            engagement_index = int(engagement_scaled * 100)

            # ========= OUTPUT =========
            print(f"\rEngagement Index: {engagement_index}", end='')

            # CLASIFICACIÓN directa sobre el ratio
            
            #STATE ES LO MAS IMPORTANTE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if engagement_smooth < 0.5:
                state = 0
            elif engagement_smooth < 1.2:
                state = 1
            else:
                state = 2

<<<<<<< HEAD
            print(f"\rEngagement: {engagement_smooth:.3f} | Estado: {engagement_index}", end='')
=======
            print(f"\rEngagement: {engagement_smooth:.3f} | Estado: {state}", end='')

            #MOTOR IMAGINATION: cálculo de ERD/ERS cada 0.1 s
            # 2) ERD C3/C4
            state_erd = erd_classifier.stable_state

            if len(buffer_c3) == window_size and sample_counter >= STEP_SIZE:
                sample_counter = 0

                p_c3 = get_band_power(np.array(buffer_c3), fs, b_n, a_n, b_b, a_b)
                p_c4 = get_band_power(np.array(buffer_c4), fs, b_n, a_n, b_b, a_b)

                erd_c3 = compute_erd(p_c3, ref_c3)
                erd_c4 = compute_erd(p_c4, ref_c4)

                state_erd = erd_classifier.classify(erd_c3, erd_c4)




>>>>>>> 99ed764182bbb9b30e0fe44620f8affcf8fcca1e
            # quitar bias
            ax=sample[ax_idx]-acc_bias_x
            ay=sample[ay_idx]-acc_bias_y

            gx=sample[gx_idx]-gyro_bias_x
            gy=sample[gy_idx]-gyro_bias_y

            # normalizar a [-1,1]
            x_acc=normalize(
                ax,
                acc_x_min,
                acc_x_max
            )

            y_acc=normalize(
                ay,
                acc_y_min,
                acc_y_max
            )


            x_gyro=normalize(
                gx,
                gyro_x_min,
                gyro_x_max
            )

            y_gyro=normalize(
                gy,
                gyro_y_min,
                gyro_y_max
            )

            # fusion sensores
            x=(w_acc*x_acc)+(w_gyro*x_gyro)
            y=(w_acc*y_acc)+(w_gyro*y_gyro)

            # suavizado exponencial
            x=alpha*x_prev+(1-alpha)*x
            y=alpha*y_prev+(1-alpha)*y

            x_prev=x
            y_prev=y

            estado = [int(state)]
            motor = [int(state_erd)]
            vector=[float(x),float(y)]
            print(f"Vector: {vector}")

            TCP_server.vector=vector
<<<<<<< HEAD
            TCP_server.focus=engagement_index
            
=======
            TCP_server.state_vector=estado
            TCP_server.imaginary=motor #agregar esto en TCP_server para enviar el estado de motor imagination a Unity
>>>>>>> 99ed764182bbb9b30e0fe44620f8affcf8fcca1e
            # debug opcional
            print(vector)
