import os
import nexus

nexus.install_packages()

from tkinter import *
from tkinter import ttk

def calculate(*args):
    try:
        value = float(feet.get())
        meters.set(int(0.3048 * value * 10000.0 + 0.5)/10000.0)
    except ValueError:
        pass

import ctypes
ctypes.windll.shcore.SetProcessDpiAwareness(1)

root = Tk()
root.title("Feet to Meters")

s = ttk.Style()
s.theme_use('xpnative')

current_directory = os.getcwd()

# Form the full path
full_path = os.path.join(current_directory, 'assets/tkBreeze-master')

print(full_path)

# Now use full_path in your tk.call
root.tk.call('lappend', 'auto_path', full_path)
root.tk.call('package', 'require', 'ttk::theme::breeze')
root.tk.call('package', 'require', 'ttk::theme::breeze-dark')
root.tk.call('tk', 'scaling', '-displayof', '.', 2)


print(s.theme_names())
s.theme_use('breeze-dark')

mainframe = ttk.Frame(root, padding="3 3 12 12")
mainframe.grid(column=0, row=0, sticky=(N, W, E, S))
root.columnconfigure(0, weight=1)
root.rowconfigure(0, weight=1)

feet = StringVar()
feet_entry = ttk.Entry(mainframe, width=7, textvariable=feet)
feet_entry.grid(column=2, row=1, sticky=(W, E))

meters = StringVar()
ttk.Label(mainframe, textvariable=meters).grid(column=2, row=2, sticky=(W, E))

ttk.Button(mainframe, text="Calculate", command=calculate).grid(column=3, row=3, sticky=W)

ttk.Label(mainframe, text="feet").grid(column=3, row=1, sticky=W)
ttk.Label(mainframe, text="is equivalent to").grid(column=1, row=2, sticky=E)
ttk.Label(mainframe, text="meters").grid(column=3, row=2, sticky=W)

for child in mainframe.winfo_children(): 
    child.grid_configure(padx=5, pady=5)

feet_entry.focus()
root.bind("<Return>", calculate)

root.mainloop()
