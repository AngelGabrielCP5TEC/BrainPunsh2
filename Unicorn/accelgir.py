from pylsl import StreamInlet, resolve_stream
import numpy as np
<<<<<<< Updated upstream
import math 
import UnicornPy
=======
import os
import sys
import TCP_server
from scipy.signal import welch, butter, filtfilt
from collections import deque
import pandas as pd
from scipy.signal import iirnotch
import threading
import random
import time
>>>>>>> Stashed changes

##Bias del giroscopio: promediar muestras del sujeto quieto }
def calibrate_gyro(inlet, num_samples=100): #inlet es el input del stream de datos 
    print("Calibrando posición inicial.")
    gyro_bias = np.zeros(3) #vector de 3 dimenciones para x, y y z
    for _ in range(num_samples):
        sample, _ = inlet.pull_sample()
        gyro_bias += np.array([sample[gx_idx], sample[gy_idx], sample[gz_idx]])
    gyro_bias /= num_samples #el bias es el promedio de las muestras
    print(f"Bias del giroscopio: {gyro_bias}")
    return gyro_bias[0], gyro_bias[1], gyro_bias[2] #retorna el bias para x, y, z

def threshold_calibration():
    mu = mu = np.mean(magnitudes)
    sigma = np.std(magnitudes)
    threshold = mu + 3*sigma
    return threshold



treshold_mov = threshold #Sale del ruido basal del giroscopio, se puede sacar con magnitud de gx gy
gain = 0.2 #También hay que ajustarlo, pero es para hacerlo más o menos sensible, a mayor valor, más sensible será el sistema.

x_prev, y_prev = 0, 0 #inicializamos posición en 0 

while(1):
    sample,_ = inlet.pull_sample()
    #le quitamos el bias a los datos jalados del giroscopio
    gx = sample[gx_idx] - gyro_bias[0]
    gy = sample[gy_idx] - gyro_bias[1]

    #Comparamos la magnitud del movimiento con el treshold para detectar si hay movimiento significativo, *nota preguntar si lo normalizamos a binario o si lo dejamos como un valor continuo para controlar la velocidad del cursor*
    #Vamos a usar la magnitud considerando sólo ejes x y y, ignoramos z!!
    magnitude = math.sqrt(gx**2 + gy**2)
    if magnitude > treshold_mov:
        x_raw = gain * gy #sensibilidad del sistema por el valor de mov detectado por el girscopio
        y_raw = gain * gx

        #Saturamos para adecuarnos al rango que espera Unity de un joystick
        x = max(-1, min(1, x_prev)) 
        y = max(-1, min(1, y_prev))

        #suavizamos el movimiento usando un filtro de media móvil simple, para evitar movimientos bruscos del cursor
        x = 0.8*x_prev + 0.2*gx #ajustamos el valor del movimiento detectado con el valor suavizado
        y = 0.8*y_prev + 0.2*gy

        #guardamos los nuevos datos previos 
        x_prev = x
        y_prev = y

        vector = [x, y]

        print(f"Movimiento detectado: gx={gx}, gy={gy}, magnitud={magnitude}")
        print(f"Posición del cursor: x={x_prev}, y={y_prev}")
    else:
        x = 0
        y = 0
        print("No se detectó movimiento significativo.")





<<<<<<< Updated upstream
=======
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

        self.mode = "detect"   # detect, hold_motor, hold_idle
        self.current_state = 0
        self.until_time = 0

        self.last_raw_state = 0
        self.counter = 0

    def detect_raw_state(self, erd_c3, erd_c4):
        if np.isnan(erd_c3) or np.isnan(erd_c4):
            return 0, np.nan

        score = (erd_c4 - erd_c3) / (abs(erd_c4) + abs(erd_c3) + 1e-6)
        LEFT_THRESHOLD = 0.08
        RIGHT_THRESHOLD = 0.04

        if score < -LEFT_THRESHOLD:
            return 1, score
        elif score > RIGHT_THRESHOLD:
            return 2, score
        else:
            return 0, score

    def classify(self, erd_c3, erd_c4):
        now = time.time()

        # 1) Si estoy manteniendo 1 o 2, lo repito hasta que acabe el tiempo
        if self.mode == "hold_motor":
            if now < self.until_time:
                print(f"motor: {self.current_state}", flush=True)
                return self.current_state
            else:
                self.mode = "hold_idle"
                self.current_state = 0
                self.until_time = now + random.uniform(1.0, 3.0)

        # 2) Si estoy en idle, mando 0 hasta que acabe el tiempo
        if self.mode == "hold_idle":
            if now < self.until_time:
                print("motor: 0", flush=True)
                return 0  
            else:
                self.mode = "detect"

        # 3) Solo aquí vuelvo a detectar ERD
        raw_state, score = self.detect_raw_state(erd_c3, erd_c4)

        # Debounce
        if raw_state == self.last_raw_state:
            self.counter += 1
        else:
            self.last_raw_state = raw_state
            self.counter = 1

        # Solo entra a 1 o 2 si se repitió suficientes veces
        if self.counter >= DEBOUNCE_COUNT and raw_state in [1, 2]:
            self.current_state = raw_state
            self.mode = "hold_motor"
            self.until_time = now + random.uniform(1.0, 3.0)

            print(f"score: {score:.4f} | motor: {self.current_state}", flush=True)
            return self.current_state

        # Si aún no detecta algo estable, se queda en 0
        print(f"score: {score:.4f} | motor: 0", flush=True)
        return 0
        




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

# ---------------- TCP SERVER (start early so Unity can connect while we wait for LSL) ----------------
threading.Thread(target=TCP_server.run, daemon=True).start()
print("[accelgir] TCP server thread started — Unity can connect now.")

# ---------------- STREAM LSL ----------------

streams=resolve_streams()

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
w_acc=0.6
w_gyro=0.4

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

            engagement_index = int(engagement_scaled * 100)*0.1

            # ========= OUTPUT =========
            print(f"\rEngagement Index: {engagement_index}", end='')

            # CLASIFICACIÓN directa sobre el ratio
            
            #STATE ES LO MAS IMPORTANTE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            #if engagement_smooth < 0.5:
            #    state = 0
            #elif engagement_smooth < 1.2:
            #    state = 1
            #else:
            #    state = 2

            

            #MOTOR IMAGINATION: cálculo de ERD/ERS cada 0.1 s
            # 2) ERD C3/C4
            state_erd = erd_classifier.current_state

            if len(buffer_c3) == window_size and sample_counter >= STEP_SIZE:
                sample_counter = 0

                p_c3 = get_band_power(np.array(buffer_c3), fs, b_n, a_n, b_b, a_b)
                p_c4 = get_band_power(np.array(buffer_c4), fs, b_n, a_n, b_b, a_b)

                erd_c3 = compute_erd(p_c3, ref_c3)
                erd_c4 = compute_erd(p_c4, ref_c4)

                state_erd = erd_classifier.classify(erd_c3, erd_c4)
            else:
                state_erd = erd_classifier.current_state



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
            x=(w_acc*x_acc)+(w_gyro*x_gyro)*0.3
            y=(w_acc*y_acc)+(w_gyro*y_gyro)*0.3

            # suavizado exponencial
            x=alpha*x_prev+(1-alpha)*x
            y=alpha*y_prev+(1-alpha)*y

            x_prev=x
            y_prev=y

            #vector=[float(x),float(y)]
            #print(f"| Vector: {vector}<")

            #TCP_server.vector=vector
            #TCP_server.focus=engagement_index
            #TCP_server.imaginary=int(state_erd)

            full_vector = [
                float(x),
                float(y),
                float(engagement_index),
                float(state_erd)
            ]

            print(f"| Full Vector: {full_vector}<")

            TCP_server.vector = full_vector
>>>>>>> Stashed changes
