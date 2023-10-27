import tkinter as tk
from tkinter import ttk
import io
import sys

class TeeOut:
    def __init__(self, out1, out2, callback=None):
        self.out1 = out1
        self.out2 = out2
        self.callback = callback

    def write(self, text):
        self.out1.write(text)
        self.out2.write(text)
        if self.callback:
            self.callback()

    def flush(self):
        self.out1.flush()
        self.out2.flush()

def log(text):
    from ..interface import root

    try:
        root.after(0, lambda: print(text))
    except:
        print(text)

class LogWindow:
    def __init__(self):
        self.old_stdout = sys.stdout
        self.new_stdout = io.StringIO()
        sys.stdout = TeeOut(self.new_stdout, self.old_stdout, self.update_text_widget)
        self.log_window = None
        self.frozen = False

    def update_text_widget(self):
        if self.frozen:
            return
        
        if self.log_window:
            try:
                text_widget = self.log_window.nametowidget("log_text")
            except:
                return
            
            self.new_stdout.seek(0)
            new_lines = self.new_stdout.read()
            self.new_stdout.truncate(0)
            self.new_stdout.seek(0)

            if new_lines and text_widget.winfo_exists():
                text_widget = self.log_window.nametowidget("log_text")
                text_widget.insert(tk.END, new_lines)
                text_widget.see(tk.END)  # Scroll to the end

    def show(self):
        self.log_window = tk.Toplevel()
        self.log_window.title("Log Window")
        self.log_window.protocol("WM_DELETE_WINDOW", self.on_closing_log_window)

        toolbar = ttk.Frame(self.log_window)
        toolbar.pack(side=tk.BOTTOM, fill=tk.X)

        separator = ttk.Separator(toolbar)
        separator.pack(side=tk.LEFT, expand=1)

        freeze_button = ttk.Button(toolbar, text="Freeze", command=self.freeze)
        freeze_button.pack(side=tk.LEFT, padx=2, pady=2)

        text_widget = tk.Text(self.log_window, wrap='none', font="Courier 8", name="log_text")
        text_widget.pack(side=tk.LEFT, expand=1, fill='both')

        scrollbar = ttk.Scrollbar(self.log_window, command=text_widget.yview)
        scrollbar.pack(side=tk.RIGHT, fill=tk.Y)

        text_widget.config(yscrollcommand=scrollbar.set)

    def freeze(self):
        self.frozen = not self.frozen

    def restore_stdout(self):
        sys.stdout = self.old_stdout

    def on_closing_log_window(self):  # <-- Add this method
        self.log_window.destroy()
        self.log_window = None