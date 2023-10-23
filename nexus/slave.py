import time
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
    def __init__(self, id, mac, ip="Unknown", port=7779):
        from .interface import tree, update_tree

        # Required properties
        self.id = id
        self.mac = mac
        self.last_received = time.time() * 1000
        self.last_sent = time.time() * 1000
        self.subject = Subject()
        self.subject.subscribe(update_tree)

        # Optional properties
        self.ip = ip
        self.port = port
        self.rssi = 0
        self.cpu_freq = 0
        self.free_heap = 0

    def addr(self):
        return (self.ip, self.port)
    
    def update_from_json(self, json_dict):
        self.__dict__.update(json_dict)

    # Add and remove
    def add_slave(self):
        from .interface import tree, update_tree

        slaves.append(self)
        tree.insert('', END, values=(self.id, self.ip, self.mac, self.rssi, self.cpu_freq, self.free_heap))

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
