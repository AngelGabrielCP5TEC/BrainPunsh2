import socket
import time

#Configuramos IP y Puerto, deben ser iguales a los que usa Unity
HOST = "127.0.0.1"
PORT = 12345

SEND_RATE_HZ = 25 #1 paquete cada 40ms, hay que ver si lo adaptamos a más o menos

#Variables compartidas con los otros scripts
vector = [0.0, 0.0] #vector para x e y
state_vector = [0]

#servidor TCP (transmission control protocol)
def run(): #corremos el servidor en un hilo aparte para no bloquear el resto del programa, y así poder actualizar los valores de x, y y concentración en tiempo real
    #socket del servidor (IPv4, TCP)
    server_sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1) #para reutilizar la dirección del socket

    #asociamos el socket a la dirección y puerto configurados
    server_sock.bind((HOST, PORT))

    #socket en modo escucha, con un backlog de 1 conexión
    server_sock.listen(1)
    print(f"[TCP] servidor escuchando en {HOST}:{PORT}...")

    #Bloqueante: pausa hasta que el script se conecte
    conn, addr = server_sock.accept()
    print(f"[TCP] conexión establecida con {addr}")

    #tiempo de espera entre paquetes según la frecuencia que queramos
    sleep_time = 1.0 / SEND_RATE_HZ

    try: 
        while True:
            #formateamos el mensaje con los valores actuales de x, y y concentración
            message = f"{x:.2f},{y:.2f},{state_vector:.2f}\n"
            conn.sendall(message.encode("utf-8")) #enviamos el mensaje al cliente (Unity) como bytes UTF-8 (8 bit Unicode Tranformation Format)

            print(f"[TCP] enviado: {message.strip()}")
            time.sleep(sleep_time) #esperamos antes de enviar el siguiente paquete

    except (BrokenPipeError, ConnectionResetError):
                print("[TCP] conexión estupida qlera pendeja.")
            
    except KeyboardInterrupt:
            print("[TCP] servidor detenido por el usuario.")
    finally:
        #cerramos los sockets al terminar
        conn.close()
        server_sock.close()
        print("[TCP] sockets cerrados, servidor apagado.")

if __name__ == "__main__":
    run()
