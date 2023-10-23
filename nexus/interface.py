import os
import platform
from tkinter import *
from tkinter import ttk

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

    # Add some sample data
    for i in range(50):
        tree.insert('', END, values=(f'slave-{i+1}', f'10.0.0.{i+20}', f'00:01:02:03:04:05:06:07'))

    # Create a scrollbar
    scrollbar = ttk.Scrollbar(main_frame, orient=VERTICAL, command=tree.yview)
    tree.config(yscrollcommand=scrollbar.set)

    # Pack the Treeview and Scrollbar
    scrollbar.pack(side=RIGHT, fill=Y)
    tree.pack(side=LEFT, fill=BOTH, expand=1)

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

def create_interface():
    set_dpi_awareness()
    import_themes()
    configure_window()
    create_slave_list()
    create_bottom_toolbar()
    
    root.mainloop()