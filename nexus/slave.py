from random import randint
from tkinter import *

class Slave:
    def __init__(self, id, ip, mac, observer):
        self._observers = [observer]
        self.id = id
        self.ip = ip
        self.mac = mac

    @property
    def id(self):
        return self._id

    @id.setter
    def id(self, value):
        self._id = value
        self._notify_observers()

    @property
    def ip(self):
        return self._ip

    @ip.setter
    def ip(self, value):
        self._ip = value
        self._notify_observers()

    @property
    def mac(self):
        return self._mac

    @mac.setter
    def mac(self, value):
        self._mac = value
        self._notify_observers()

    def add_observer(self, observer):
        self._observers.append(observer)

    def _notify_observers(self):
        for observer in self._observers:
            observer.update(self)
    
    # Sample data
    def generate_sample_ip():
        return f"{randint(0, 255)}.{randint(0, 255)}.{randint(0, 255)}.{randint(0, 255)}"

    def generate_sample_mac():
        return f"{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}:{randint(0x00, 0xFF):02X}"
    
    def generate_sample_slave(observer):
        return Slave(f"slave-{randint(0, 100)}", Slave.generate_sample_ip(), Slave.generate_sample_mac(), observer)
    
    def generate_sample_slaves(observer):
        slaves = []
        
        for i in range(10):
            slaves.append(Slave.generate_sample_slave(observer))
        
        return slaves
    
class SlaveListObserver:
    def __init__(self, tree):
        self.tree = tree

    def update(self, slave_list):
        for item in self.tree.get_children():
            self.tree.delete(item)
        for slave in slave_list:
            self.tree.insert('', END, values=(slave.id, slave.ip, slave.mac))

class SlaveObserver:
    def __init__(self, tree):
        self.tree = tree

    def update(self, slave):
        for item in self.tree.get_children():
            if self.tree.item(item, "values")[0] == slave.id:
                self.tree.item(item, values=(slave.id, slave.ip, slave.mac))
                break

slaves = [Slave]
