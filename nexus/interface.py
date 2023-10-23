import os
import platform
from tkinter import *
from tkinter import ttk
from .slave import *
from .settings import *

root = Tk()
main_frame = ttk.Frame(root)
style = ttk.Style()

def set_dpi_awareness():
    if platform.system() == 'Windows':
        import ctypes
        ctypes.windll.shcore.SetProcessDpiAwareness(1)

def import_themes():
    current_directory = os.getcwd()

    # Form the full path
    full_path = os.path.join(current_directory, 'assets/tkBreeze-master')

    # Now use full_path in your tk.call
    root.tk.call('lappend', 'auto_path', full_path)
    root.tk.call('package', 'require', 'ttk::theme::breeze')
    root.tk.call('package', 'require', 'ttk::theme::breeze-dark')
    root.tk.call('tk', 'scaling', '-displayof', '.', 2)

def configure_window():
    root.title("Nexus")
    
    style.theme_use('breeze-dark')
    
    main_frame.pack(fill=BOTH, expand=1)

def create_slave_list():
    def treeview_sort_column(tv, col, reverse):
        l = [(tv.set(k, col), k) for k in tv.get_children('')]
        l.sort(reverse=reverse)

        # rearrange items in sorted positions
        for index, (val, k) in enumerate(l):
            tv.move(k, '', index)

        # reverse sort next time
        tv.heading(col, command=lambda: treeview_sort_column(tv, col, not reverse))

    # Create a Treeview widget
    tree = ttk.Treeview(main_frame, columns=('Identifier', 'IP', 'MAC'), show='headings')
    tree.heading('Identifier', text='Identifier', command=lambda: treeview_sort_column(tree, 'Identifier', False))
    tree.heading('IP', text='IP', command=lambda: treeview_sort_column(tree, 'IP', False))
    tree.heading('MAC', text='MAC', command=lambda: treeview_sort_column(tree, 'MAC', False))

    # Create observer instances
    slave_list_observer = SlaveListObserver(tree)
    slave_observer = SlaveObserver(tree)

    slaves = Slave.generate_sample_slaves(slave_observer)
    slave_list_observer.update(slaves)

    def on_treeview_select(event):
        selected_item = tree.selection()[0]
        selected_values = tree.item(selected_item)['values']
        selected_mac = selected_values[2]
        selected_slave = next((slave for slave in slaves if slave.mac == selected_mac), None)
        print(f"Selected slave: {selected_slave.mac}")
    
    tree.bind('<<TreeviewSelect>>', on_treeview_select)

    # Create a scrollbar
    scrollbar = ttk.Scrollbar(main_frame, orient=VERTICAL, command=tree.yview)
    tree.config(yscrollcommand=scrollbar.set)

    # Pack the Treeview and Scrollbar
    scrollbar.pack(side=RIGHT, fill=Y)
    tree.pack(side=LEFT, fill=BOTH, expand=1)

    # Bind CMD+Q to save settings and quit
    root.createcommand("::tk::mac::Quit", lambda: (
        save_settings(),
        root.destroy()
    ))

def toggle_style():
    if style.theme_use() == 'breeze-dark':
        style.theme_use('breeze')
    else:
        style.theme_use('breeze-dark')

def create_bottom_toolbar():
    toolbar = ttk.Frame(root)
    toolbar.pack(side=BOTTOM, fill=X)

    appearance_button = ttk.Button(toolbar, text="Appearance", command=toggle_style)
    appearance_button.pack(side=LEFT, padx=2, pady=2)

    separator = ttk.Separator(toolbar)
    separator.pack(side=LEFT, expand=1)

    settings_button = ttk.Button(toolbar, text="Settings")
    settings_button.pack(side=LEFT, padx=2, pady=2)

def center_window():
    root.update_idletasks()
    width = root.winfo_width()
    height = root.winfo_height()
    x = (root.winfo_screenwidth() // 2) - (width // 2)
    y = (root.winfo_screenheight() // 2) - (height // 2)
    root.geometry(f'{width}x{height}+{x}+{y}')

def create_interface():
    set_dpi_awareness()
    import_themes()
    configure_window()
    create_slave_list()
    create_bottom_toolbar()
    center_window()
    load_settings()
    
    root.protocol("WM_DELETE_WINDOW", lambda: (save_settings(), root.destroy()))

    root.mainloop()