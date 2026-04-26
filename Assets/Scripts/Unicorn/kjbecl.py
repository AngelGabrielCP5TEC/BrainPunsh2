from pylsl import StreamInlet, resolve_streams
import numpy as np
from scipy.signal import welch, butter, filtfilt

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

# STREAM
streams = resolve_streams()
inlet   = StreamInlet(streams[0])
print("Midiendo engagement...\n")

FZ_IDX = 0
CZ_IDX = 2
PZ_IDX = 4

try:
    while True:
        sample, timestamp = inlet.pull_sample()

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

            # CLASIFICACIÓN directa sobre el ratio
            
            #STATE ES LO MAS IMPORTANTE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if engagement_smooth < 0.5:
                state = 0
            elif engagement_smooth < 1.2:
                state = 1
            else:
                state = 2

            print(f"\rEngagement: {engagement_smooth:.3f} | Estado: {state}", end='')

except KeyboardInterrupt:
    print("\nMedición finalizada.")