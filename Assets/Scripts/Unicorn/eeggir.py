import numpy as np
import math
import pylsl
import csv
import pyqtgraph as pg
from datetime import datetime
from typing import List
from pyqtgraph.Qt import QtGui, QtCore, QtWidgets



# Parámetros básicos para la ventana de visualización
plot_duration = 5  # cuántos segundos de datos mostrar
update_interval = 60  # ms entre actualizaciones de pantalla
pull_interval = 20  # ms entre cada operación de extracción de datos
sample_rate = 250  # Hz - frecuencia de muestreo para guardar datos

# Parámetros de visualización para los canales EEG
channel_names = ["Acc Y", "Acc X", "Gyro Y", "Gyro X" ]
channel_colors=[
'#FF0000',
'#00AA00',
'#0000FF',
'#AA00FF'
]

sensor_indices = [8, 9, 11, 12]

class Inlet:
    """Clase base para representar una entrada plottable"""
    def __init__(self, info: pylsl.StreamInfo):
        # Crear una entrada y conectarla a la salida que encontramos anteriormente.
        # max_buflen se configura para que los datos más antiguos que plot_duration se descarten
        # automáticamente y solo extraigamos datos lo suficientemente nuevos para mostrarlos

        # También, realizar sincronización de reloj en línea para que todos los flujos estén en el
        # mismo dominio de tiempo que el local lsl_clock()
        # (ver https://labstreaminglayer.readthedocs.io/projects/liblsl/ref/enums.html#_CPPv414proc_clocksync)
        # y eliminar el jitter de las marcas de tiempo
        self.inlet = pylsl.StreamInlet(info, max_buflen=plot_duration,
                                       processing_flags=pylsl.proc_clocksync | pylsl.proc_dejitter)
        # Almacenar el nombre y recuento de canales
        self.name = info.name()
        self.channel_count = info.channel_count()
       
    def pull_and_plot(self, plot_time: float, plots):
        # No sabemos qué hacer con una entrada genérica, así que la omitimos.
        pass


class DataInlet(Inlet):
    """Un DataInlet representa una entrada con datos continuos y multicanal que
    deben trazarse como múltiples líneas."""
    dtypes = [[], np.float32, np.float64, None, np.int32, np.int16, np.int8, np.int64]

    def __init__(self, info: pylsl.StreamInfo, plots):
        super().__init__(info)
        # Calcular el tamaño de nuestro buffer, es decir, dos veces los datos mostrados
        bufsize = (2 * math.ceil(info.nominal_srate() * plot_duration), info.channel_count())
        self.buffer = np.empty(bufsize, dtype=self.dtypes[info.channel_format()])
        empty = np.array([])
       
        # Asegurar que solo usamos hasta 8 canales
        self.num_channels = min(self.channel_count, 4)
       
        # Para guardar datos
        self.all_data = []
        self.all_timestamps = []
        self.is_recording = False
        self.csv_filename = None
        self.last_sample_time = 0
        self.sample_interval = 1.0 / sample_rate  # Intervalo entre muestras en segundos
       
        # Crear un objeto de curva para cada canal en su propio gráfico
        self.curves = []
        for i in range(self.num_channels):
            color = channel_colors[i % len(channel_colors)]
            channel_name = channel_names[i] if i < len(channel_names) else f"Canal {i+1}"
            curve = pg.PlotCurveItem(
                x=empty,
                y=empty,
                autoDownsample=True,
                pen=pg.mkPen(color=color, width=2),
                name=channel_name
            )
            plots[i].addItem(curve)
            self.curves.append(curve)

    def start_recording(self, filename):
        """Iniciar la grabación de datos"""
        self.all_data = []
        self.all_timestamps = []
        self.is_recording = True
        self.csv_filename = filename
        self.last_sample_time = 0
        print(f"Iniciando grabación en {filename}")

    def stop_recording(self):
        """Detener la grabación y guardar datos en CSV"""
        if not self.is_recording:
            return
           
        self.is_recording = False
       
        if not self.all_data or not self.all_timestamps:
            print("No hay datos para guardar")
            return
           
        # Convertir listas a arrays numpy para facilitar el procesamiento
        timestamps = np.array(self.all_timestamps)
        data = np.array(self.all_data)
       
        try:
            with open(self.csv_filename, 'w', newline='') as csvfile:
                writer = csv.writer(csvfile)
               
                # Escribir encabezados
                headers=[
                "Timestamp",
                "ACC_Y",
                "ACC_X",
                "GYRO_Y",
                "GYRO_X"
                ]
                writer.writerow(headers)
               
                # Escribir datos
                for i in range(len(timestamps)):
                    row = [timestamps[i]] + data[i].tolist()
                    writer.writerow(row)
                   
            print(f"Datos guardados en {self.csv_filename}")
            print(f"Total de muestras guardadas: {len(timestamps)}")
            self.csv_filename = None
           
        except Exception as e:
            print(f"Error al guardar los datos: {e}")

    def pull_and_plot(self, plot_time, plots):
        # Extraer los datos
        chunk, ts = self.inlet.pull_chunk(timeout=0.0,
                                      max_samples=self.buffer.shape[0],
                                      dest_obj=self.buffer)
        # ts estará vacío si no se extrajeron muestras, una lista de marcas de tiempo en caso contrario
        if ts:
            ts = np.asarray(ts)
            y = self.buffer[0:ts.size, :]
           
            # Guardar datos si estamos grabando
            if self.is_recording:
                # Guardar todas las muestras en el chunk
                for i in range(len(ts)):
                    self.all_timestamps.append(ts[i])
                    self.all_data.append(
    y[i, sensor_indices]
)
               
                # Opcional: imprimir detalles para depuración
                print(f"Guardadas {len(ts)} muestras. Último timestamp: {ts[-1]}")
           
            this_x = None
            old_offsets = [0] * self.num_channels
            new_offset = 0
           
            for ch_ix in range(self.num_channels):
                # No extraemos datos de toda una pantalla, por lo que tenemos que
                # recortar los datos antiguos y adjuntar los nuevos
                old_x, old_y = self.curves[ch_ix].getData()
               
                # Encontrar el índice de la primera muestra que aún es visible
                old_offsets[ch_ix] = old_x.searchsorted(plot_time) if len(old_x) > 0 else 0
               
                if ch_ix == 0:
                    # Lo mismo para los nuevos datos, en caso de que extraigamos más datos
                    # de los que se pueden mostrar a la vez
                    new_offset = ts.searchsorted(plot_time) if len(ts) > 0 else 0
               
                # Adjuntar nuevas marcas de tiempo a las marcas de tiempo antiguas recortadas
                this_x = np.hstack((old_x[old_offsets[ch_ix]:], ts[new_offset:]))
               
                # Adjuntar nuevos datos a los datos antiguos recortados (sin desplazamiento vertical)
                this_y = np.hstack((old_y[old_offsets[ch_ix]:], y[new_offset:, sensor_indices[ch_ix]]))
               
                # Reemplazar los datos antiguos
                self.curves[ch_ix].setData(this_x, this_y)


class MarkerInlet(Inlet):
    """Un MarkerInlet muestra eventos que ocurren esporádicamente como líneas verticales"""
    def __init__(self, info: pylsl.StreamInfo):
        super().__init__(info)

    def pull_and_plot(self, plot_time, plots):
        # No implementamos marcadores en esta versión para simplificar
        pass


class EEGInterface(QtWidgets.QMainWindow):
    def __init__(self):
        super().__init__()
       
        self.setWindowTitle('Visualización de ejes X y Y en el giroscopio')
        self.resize(1200, 900)
       
        # Widget central
        central_widget = QtWidgets.QWidget()
        self.setCentralWidget(central_widget)
       
        # Layout principal
        main_layout = QtWidgets.QVBoxLayout(central_widget)
       
        # Área de gráficos - Usar GridLayout para organizar los plots
        self.graph_layout = QtWidgets.QGridLayout()
        main_layout.addLayout(self.graph_layout, stretch=1)
       
        # Crear 4 gráficos separados (4x2 grid)
        self.graphs = []
        self.plots = []
       
        for i in range(4):
            graph_widget = pg.GraphicsLayoutWidget()
            plot = graph_widget.addPlot(title=f'Canal {i+1}')
            plot.setLabel('left', 'Amplitud')
            plot.setLabel('bottom', 'Tiempo (s)')
            plot.enableAutoRange(x=False, y=True)
            plot.showGrid(x=True, y=True)
           
            # Añadir al grid - 4 canales por fila, 2 filas
            row = i // 4
            col = i % 4
            self.graph_layout.addWidget(graph_widget, row, col)
           
            self.graphs.append(graph_widget)
            self.plots.append(plot)
       
        # Panel de control
        control_panel = QtWidgets.QHBoxLayout()
        main_layout.addLayout(control_panel)
       
        # Campo de nombre de archivo
        self.filename_label = QtWidgets.QLabel("Nombre del archivo:")
        control_panel.addWidget(self.filename_label)
       
        self.filename_input = QtWidgets.QLineEdit()
        self.filename_input.setText(f"eeg_data_{datetime.now().strftime('%Y%m%d_%H%M%S')}.csv")
        control_panel.addWidget(self.filename_input, stretch=1)
       
        # Botón para explorar
        self.browse_button = QtWidgets.QPushButton("Explorar...")
        self.browse_button.clicked.connect(self.browse_file)
        control_panel.addWidget(self.browse_button)
       
        # Botón para iniciar/detener grabación
        self.record_button = QtWidgets.QPushButton("Iniciar Grabación")
        self.record_button.setCheckable(True)
        self.record_button.clicked.connect(self.toggle_recording)
        control_panel.addWidget(self.record_button)
       
        # Indicador de estado
        self.status_label = QtWidgets.QLabel("Estado: Esperando")
        control_panel.addWidget(self.status_label)
       
        # Indicador de grabación (LED)
        self.recording_indicator = QtWidgets.QLabel("⚪")  # Círculo blanco
        self.recording_indicator.setStyleSheet("font-size: 20px;")
        control_panel.addWidget(self.recording_indicator)
       
        # Información de frecuencia de muestreo
        self.sample_rate_label = QtWidgets.QLabel(f"Frecuencia de muestreo: {sample_rate} Hz")
        control_panel.addWidget(self.sample_rate_label)
       
        # Inicializar temporizadores
        self.update_timer = QtCore.QTimer(self)
        self.update_timer.timeout.connect(self.scroll)
        self.update_timer.start(update_interval)
       
        self.pull_timer = QtCore.QTimer(self)
        self.pull_timer.timeout.connect(self.update)
        self.pull_timer.start(pull_interval)
       
        # Barra de estado
        self.statusBar().showMessage("Listo")
       
        # Buscar flujos de datos
        self.inlets = []
        self.data_inlet = None  # Referencia al inlet de datos principal
        self.find_streams()
   
    def find_streams(self):
        self.statusBar().showMessage("Buscando flujos de datos...")
        streams = pylsl.resolve_streams()
       
        if not streams:
            self.statusBar().showMessage("No se encontraron flujos de datos")
            return
           
        for info in streams:
            if info.nominal_srate() != pylsl.IRREGULAR_RATE \
                    and info.channel_format() != pylsl.cf_string:
                print('Añadiendo entrada de datos: ' + info.name())
                inlet = DataInlet(info, self.plots)
                self.inlets.append(inlet)
                self.data_inlet = inlet  # Guardar referencia al inlet de datos para la grabación
            else:
                print('No sé qué hacer con el flujo ' + info.name())
       
        num_inlets = len(self.inlets)
        self.statusBar().showMessage(f"Se encontraron {num_inlets} flujos de datos")
   
    def scroll(self):
        """Mover la vista para que los datos parezcan desplazarse en todos los gráficos"""
        fudge_factor = pull_interval * .002
        plot_time = pylsl.local_clock()
        for plot in self.plots:
            plot.setXRange(plot_time - plot_duration + fudge_factor, plot_time - fudge_factor)
   
    def update(self):
        """Actualizar datos y gráficos"""
        mintime = pylsl.local_clock() - plot_duration
        for inlet in self.inlets:
            inlet.pull_and_plot(mintime, self.plots)
           
        # Parpadear el indicador si estamos grabando
        if self.record_button.isChecked():
            current_time = pylsl.local_clock()
            if int(current_time * 2) % 2 == 0:
                self.recording_indicator.setText("🔴")  # Círculo rojo
            else:
                self.recording_indicator.setText("⚪")  # Círculo blanco
   
    def browse_file(self):
        """Abrir diálogo para seleccionar ubicación y nombre de archivo"""
        options = QtWidgets.QFileDialog.Options()
        filename, _ = QtWidgets.QFileDialog.getSaveFileName(
            self,
            "Guardar datos EEG",
            self.filename_input.text(),
            "Archivos CSV (*.csv);;Todos los archivos (*)",
            options=options
        )
        if filename:
            if not filename.endswith('.csv'):
                filename += '.csv'
            self.filename_input.setText(filename)
   
    def toggle_recording(self):
        """Iniciar o detener la grabación de datos"""
        if not self.data_inlet:
            QtWidgets.QMessageBox.warning(self, "Error", "No hay flujos de datos disponibles para grabar")
            self.record_button.setChecked(False)
            return
           
        if self.record_button.isChecked():
            # Iniciar grabación
            filename = self.filename_input.text()
            if not filename:
                QtWidgets.QMessageBox.warning(self, "Error", "Por favor, especifica un nombre de archivo")
                self.record_button.setChecked(False)
                return
               
            if not filename.endswith('.csv'):
                filename += '.csv'
                self.filename_input.setText(filename)
               
            self.data_inlet.start_recording(filename)
            self.record_button.setText("Detener Grabación")
            self.status_label.setText("Estado: Grabando")
            self.filename_input.setEnabled(False)
            self.browse_button.setEnabled(False)
            self.statusBar().showMessage(f"Grabando datos en {filename} a {sample_rate} Hz")
        else:
            # Detener grabación
            self.data_inlet.stop_recording()
            self.record_button.setText("Iniciar Grabación")
            self.status_label.setText("Estado: Detenido")
            self.recording_indicator.setText("⚪")  # Círculo blanco
            self.filename_input.setEnabled(True)
            self.browse_button.setEnabled(True)
            self.statusBar().showMessage("Grabación detenida")


def main():
    app = QtWidgets.QApplication([])
    window = EEGInterface()
    window.show()
    app.exec_()


if __name__ == '__main__':
    main()

