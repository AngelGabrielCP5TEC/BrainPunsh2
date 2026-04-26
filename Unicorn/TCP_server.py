import socket
import sys
import time
import threading
import traceback

HOST = "127.0.0.1"
PORT = 1234

SEND_RATE_HZ = 25   # 40 ms por paquete

vector = [0.0, 0.0]       # [x, y]
focus = 0.0  # nivel de concentración flotante
imaginary = 0             # 0,1,2

data_lock = threading.Lock()


def _log(msg):
    """Print to stderr so it doesn't get overwritten by accelgir's \\r-heavy stdout."""
    print(f"[TCP] {msg}", file=sys.stderr, flush=True)


def run():
    """Bind once, then loop accept() so Unity can reconnect repeatedly."""
    try:
        server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        server_sock.bind((HOST, PORT))
        server_sock.listen(1)
        _log(f"Escuchando en {HOST}:{PORT}...")
    except Exception as e:
        _log(f"BIND/LISTEN FAILED: {e}")
        traceback.print_exc(file=sys.stderr)
        return

    sleep_time = 1.0 / SEND_RATE_HZ

    try:
        while True:
            try:
                conn, addr = server_sock.accept()
                _log(f"Conectado con {addr}")
            except Exception as e:
                _log(f"accept() failed: {e}")
                time.sleep(0.5)
                continue

            try:
                while True:
                    with data_lock:
                        x = vector[0]
                        y = vector[1]
                        foc = focus
                        imag = imaginary

                    # Format: x,y,focus,imaginary\n
                    message = f"{x:.4f},{y:.4f},{foc},{imag}\n"
                    conn.sendall(message.encode("utf-8"))
                    time.sleep(sleep_time)

            except (BrokenPipeError, ConnectionResetError, OSError) as e:
                _log(f"Cliente desconectado ({e}). Esperando nueva conexión...")
            finally:
                try:
                    conn.close()
                except Exception:
                    pass
            # back to outer loop: accept() again

    except KeyboardInterrupt:
        _log("Servidor detenido (Ctrl+C).")
    finally:
        try:
            server_sock.close()
        except Exception:
            pass
        _log("Socket cerrado.")


if __name__ == "__main__":
    run()
