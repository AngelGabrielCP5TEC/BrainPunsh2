import numpy as np
import os
import pandas as pd

CSV_PATH = r"C:\Users\sophi\Desktop\brain punch\BrainPunsh2\Assets\Scripts\Unicorn\eeg_data_20260425_175854.csv"

BASE_DIR = os.path.dirname(CSV_PATH)

def calibrate_from_csv(csv_path):

    df = pd.read_csv(csv_path)

    print(df.columns)

    ax=df["ACC_Y"].values
    ay=df["ACC_X"].values
    gx=df["GYRO_X"].values
    gy=df["GYRO_Y"].values

    N=len(df)
    n0=int(0.25*N)

    acc_bias_x=np.median(ax[:n0])
    acc_bias_y=np.median(ay[:n0])

    gyro_bias_x=np.median(gx[:n0])
    gyro_bias_y=np.median(gy[:n0])

    ax-=acc_bias_x
    ay-=acc_bias_y
    gx-=gyro_bias_x
    gy-=gyro_bias_y

    params=np.array([
        acc_bias_x,acc_bias_y,
        gyro_bias_x,gyro_bias_y,

        np.percentile(ax,5),
        np.percentile(ax,95),

        np.percentile(ay,5),
        np.percentile(ay,95),

        np.percentile(gx,5),
        np.percentile(gx,95),

        np.percentile(gy,5),
        np.percentile(gy,95)
    ])

    out_path=os.path.join(BASE_DIR,"calib_cursor.npy")
    np.save(out_path,params)

    print("Guardado:",out_path)


if __name__=="__main__":
    calibrate_from_csv(CSV_PATH)