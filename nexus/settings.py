import os
import yaml

def save_settings():
    from .interface import root

    settings = {
        'geometry': root.geometry(),
        # other settings
    }
    with open('settings.yaml', 'w') as f:
        yaml.dump(settings, f)

def load_settings():
    from .interface import root
    
    if os.path.exists('settings.yaml'):
        with open('settings.yaml', 'r') as f:
            settings = yaml.safe_load(f)
            root.geometry(settings.get('geometry', ''))