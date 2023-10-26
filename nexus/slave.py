import time
import socket
from tkinter import *
from typing import List
from random import randint
from rx.subject import Subject

class ObservableObject:
    def __setattr__(self, name, value):
        super().__setattr__(name, value)
        if hasattr(self, 'subject'):
            self.subject.on_next(self)

class Slave(ObservableObject):
    sock: socket.socket

    def __init__(self, id, mac, ip="Unknown", port=7779, socket=None):
        from .interface import tree, update_tree

        # Required properties
        self.id = id
        self.mac = mac
        self.last_received = time.time() * 1000
        self.last_sent = time.time() * 1000
        self.subject = Subject()
        self.subject.subscribe(update_tree)

        # Optional properties
        self.sock = socket
        self.ip = ip
        self.port = port
        self.rssi = 0
        self.loop_duration = 0
        self.free_heap = 0
        self.cpu_freq = 0
        self.flash_size = 0

        self.loop_durations = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0]
        self.max_samples = 10
        self.min_duration = float('inf')
        self.max_duration = 0
        self.avg_duration = 0

    def update_loop_duration(self):
        self.loop_durations.append(self.loop_duration)

        if len(self.loop_durations) > self.max_samples:
            self.loop_durations.pop(0)

        self.min_duration = min(self.loop_durations)
        self.max_duration = max(self.loop_durations)
        self.avg_duration = sum(self.loop_durations) / len(self.loop_durations)

    def addr(self):
        return (self.ip, self.port)
    
    def update_from_json(self, json_dict):
        self.__dict__.update(json_dict)
        self.update_loop_duration()

    def rssi_to_text_bar(self, max_length=10):
        rssi_percent = 100 + self.rssi if self.rssi <= 0 else 100
        filled_length = int(max_length * rssi_percent // 100)
        bar = '█' * filled_length + '' * (max_length - filled_length)
        return f"{rssi_percent}% {bar}"
    
    def rssi_to_percent(self):
        rssi_percent = 100 + self.rssi if self.rssi <= 0 else 100
        return f"{rssi_percent}%"
    
    def loop_duration_string(self):
        return f"{self.avg_duration/1000:.2f} ms ({self.min_duration/1000:.2f} - {self.max_duration/1000:.2f})"

    def tree_values(self):
        rssi_percent = 100 + self.rssi if self.rssi <= 0 else 100
        free_heap_kB = self.free_heap / 1024
        loop_duration_ms = self.loop_duration / 1000
        flash_size_mb = self.flash_size / 1024 / 1024

        return (self.id, self.ip, self.mac, f"{self.rssi_to_text_bar()}", f"{self.loop_duration_string()} ms", f"{free_heap_kB:.1f} kB", f"{self.cpu_freq} MHz", f"{flash_size_mb} MB")
    
    # Add and remove
    def add_slave(self):
        from .interface import tree, update_tree

        slaves.append(self)
        tree.insert('', END, values=self.tree_values())

    def remove_slave(self):
        from .interface import tree

        mac_to_remove = self.mac
        slaves_to_remove = [slave for slave in slaves if slave.mac == mac_to_remove]

        for item in tree.get_children():
            values = tree.item(item, 'values')
            if values[2] == mac_to_remove:  # Assuming MAC is the 3rd value in the tree
                tree.delete(item)

        for slave in slaves_to_remove:
            slaves.remove(slave)

    def find_slave_by_mac(mac_address):
        for slave in slaves:
            if slave.mac == mac_address:
                return slave
        return None

    def remove_slave_by_mac(mac):
        from .interface import tree

        slaves_to_remove = [slave for slave in slaves if slave.mac == mac]
        for slave in slaves_to_remove:
            slave.remove_slave(tree)
    
    # Sample data
    def generate_sample_ip():
        return f"{randint(0, 255)}.{randint(0, 255)}.{randint(0, 255)}.{randint(0, 255)}"

    def generate_sample_mac():
        return f"{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}"
    
    def generate_sample_slave():
        return Slave(f"slave-{randint(0, 100)}", Slave.generate_sample_mac(), Slave.generate_sample_ip())
    
    def generate_sample_slaves():
        slaves = []
        
        for i in range(10):
            slaves.append(Slave.generate_sample_slave())
        
        return slaves

slaves: List[Slave] = []
