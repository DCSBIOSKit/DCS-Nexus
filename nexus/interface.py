from PyQt5.QtWidgets import QApplication, QMainWindow, QTableWidget, QTableWidgetItem, QToolBar, QAction, QVBoxLayout, QWidget, QHeaderView, QMenu
from PyQt5.QtCore import Qt
import sys
from .slave import Slave, slaves
from .settings import *
from .windows.log_window import LogWindow

class NexusMainWindow(QMainWindow):
    def __init__(self):
        super(NexusMainWindow, self).__init__()

        # Main UI setup
        self.setWindowTitle("Nexus")
        self.setGeometry(200, 200, 800, 600)

        # Create QTableWidget object
        self.tableWidget = QTableWidget()
        self.tableWidget.setColumnCount(8)
        self.tableWidget.setHorizontalHeaderLabels(['Identifier', 'IP', 'MAC', 'RSSI', 'Loop Time', 'Free Memory', 'CPU', 'Flash Size'])
        self.tableWidget.horizontalHeader().setSectionResizeMode(QHeaderView.Stretch)
        
        # Create Toolbar
        self.toolbar = self.addToolBar("Main Toolbar")
        log_action = QAction("Log", self)
        self.toolbar.addAction(log_action)
        settings_action = QAction("Settings", self)
        self.toolbar.addAction(settings_action)

        # Layouts
        layout = QVBoxLayout()
        layout.addWidget(self.tableWidget)
        container = QWidget()
        container.setLayout(layout)
        self.setCentralWidget(container)

        # Signals & Slots
        self.tableWidget.setContextMenuPolicy(Qt.CustomContextMenu)
        self.tableWidget.customContextMenuRequested.connect(self.show_context_menu)
        log_action.triggered.connect(self.log_button_clicked)
        settings_action.triggered.connect(self.settings_button_clicked)

        # Initialize table
        self.update_table()
        
    def show_context_menu(self, pos):
        context_menu = QMenu(self)
        restart_action = context_menu.addAction("Restart")
        restart_action.triggered.connect(self.restart_item)
        context_menu.exec_(self.tableWidget.mapToGlobal(pos))
        
    def restart_item(self):
        from .master import enqueue_slave_command, SlaveCommand

        selected_row = self.tableWidget.currentRow()
        if selected_row >= 0:
            mac_address = self.tableWidget.item(selected_row, 2).text()
            slave = Slave.find_slave_by_mac(mac_address)
            if slave:
                print(f"Restarting slave: {slave.id}")
                enqueue_slave_command(SlaveCommand("restart", slave.id))

    def log_button_clicked(self):
        print("Log Button Clicked")
        # Implement the log action here

    def settings_button_clicked(self):
        print("Settings Button Clicked")
        # Implement the settings action here

    def update_table(self):
        global slaves
        
        self.tableWidget.setRowCount(0)
        for slave in slaves:
            free_heap_kB = slave.free_heap / 1024
            loop_duration_ms = slave.loop_duration / 1000
            flash_size_mb = slave.flash_size / 1024 / 1024

            row_position = self.tableWidget.rowCount()
            self.tableWidget.insertRow(row_position)
            self.tableWidget.setItem(row_position, 0, QTableWidgetItem(str(slave.id)))
            self.tableWidget.setItem(row_position, 1, QTableWidgetItem(slave.ip))
            self.tableWidget.setItem(row_position, 2, QTableWidgetItem(slave.mac))
            self.tableWidget.setItem(row_position, 3, QTableWidgetItem(str(slave.rssi_to_text_bar())))
            self.tableWidget.setItem(row_position, 4, QTableWidgetItem(str(slave.loop_duration_string()) + " ms"))
            self.tableWidget.setItem(row_position, 5, QTableWidgetItem("{:.0f} kB".format(free_heap_kB)))
            self.tableWidget.setItem(row_position, 6, QTableWidgetItem(str(slave.cpu_freq) + " MHz"))
            self.tableWidget.setItem(row_position, 7, QTableWidgetItem(str(flash_size_mb) + " MB"))

            # (self.id, self.ip, self.mac, f"{self.rssi_to_text_bar()}", f"{self.loop_duration_string()} ms", f"{free_heap_kB:.1f} kB", f"{self.cpu_freq} MHz", f"{flash_size_mb} MB")

window = None

def create_app():
    global window

    app = QApplication(sys.argv)
    window = NexusMainWindow()
    window.show()
    sys.exit(app.exec_())