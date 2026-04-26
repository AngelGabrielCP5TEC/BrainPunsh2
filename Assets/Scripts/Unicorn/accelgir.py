from pylsl import StreamInlet, resolve_stream
import numpy as np
import os
import sys
import TCP_server
from scipy.signal import welch, butter, filtfilt

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
sys.path.append(BASE_DIR)

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


while True:

    sample, timestamp =inlet.pull_sample()

    total_data['Time'].append(timestamp)
    total_data['FZ'].append(sample[FZ_IDX])
    total_data['CZ'].append(sample[CZ_IDX])
    total_data['PZ'].append(sample[PZ_IDX])


if len(total_data['Time']) >= window_size:
            theta = alpha = beta = 0

            for ch in columns[1:]:
                data = np.array(total_data[ch][-window_size:])
                data = apply_filter(data)

                freqs, psd = welch(data, fs, nperseg=250, noverlap=125)
                theta += bandpower(psd, freqs, theta_band)
                alpha += bandpower(psd, freqs, alpha_band)
                beta  += bandpower(psd, freqs, beta_band)

            n      = len(columns) - 1
            theta /= n
            alpha /= n
            beta  /= n

            # ENGAGEMENT ABSOLUTO — sin baseline, sin normalización
            engagement = beta / (alpha + theta + 1e-6)

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

            print(f"\rEngagement: {engagement_smooth:.3f} | Estado: {engagement_index}", end='')
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

            estado = [state]
            vector=[float(x),float(y)]
            print(f"Vector: {vector}")

            TCP_server.vector=vector
            TCP_server.focus=engagement_index
            
            # debug opcional
            print(vector)
