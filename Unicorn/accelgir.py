from pylsl import StreamInlet, resolve_stream
import numpy as np
import math 
import UnicornPy

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





