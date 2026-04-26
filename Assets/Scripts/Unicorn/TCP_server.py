import socket
import time
import threading

HOST = "127.0.0.1"
PORT = 12345

SEND_RATE_HZ = 25   # 40 ms por paquete

vector = [0.0, 0.0]       # [x, y]
state_vector = [0]        # [state]
imaginary = 0             # 0,1,2

data_lock = threading.Lock()

#tcp server
def run():

    server_sock = socket.socket(
        socket.AF_INET,
        socket.SOCK_STREAM
    )

    server_sock.setsockopt(
        socket.SOL_SOCKET,
        socket.SO_REUSEADDR,
        1
    )

    server_sock.bind((HOST, PORT))
    server_sock.listen(1)

    print(f"[TCP] Escuchando en {HOST}:{PORT}...")

    conn, addr = server_sock.accept()

    print(f"[TCP] Conectado con {addr}")

    sleep_time = 1.0 / SEND_RATE_HZ

    try:
        while True:

            # tomar snapshot de variables
            with data_lock:
                x = vector[0]
                y = vector[1]
                state = state_vector[0]
                imag = imaginary

            # Formato:
            # x,y,state,imaginary
            message = (
                f"{x:.4f},"
                f"{y:.4f},"
                f"{state},"
                f"{imag}\n"
            )

            conn.sendall(
                message.encode("utf-8")
            )

            print("[TCP] enviado:",
                  message.strip())

            time.sleep(sleep_time)

    except (BrokenPipeError,
            ConnectionResetError):

        print("[TCP] Cliente desconectado.")

    except KeyboardInterrupt:
        print("[TCP] Servidor detenido.")

    finally:
        conn.close()
        server_sock.close()
        print("[TCP] Sockets cerrados.")


if __name__ == "__main__":
    run()