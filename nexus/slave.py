from tkinter import *
from typing import List
from random import randint
from rx.subject import Subject

class ObservableObject:
    observable_properties = []

    def __setattr__(self, name, value):
        super().__setattr__(name, value)
        if hasattr(self, 'subject') and name in self.__class__.observable_properties:
            self.subject.on_next(self)

class Slave(ObservableObject):
    observable_properties = ["id", "ip"]

    def __init__(self, id, mac, ip="Unknown"):
        # Required properties
        self.id = id
        self.mac = mac
        self.subject = Subject()

        # Optional properties
        self.ip = ip
    
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
