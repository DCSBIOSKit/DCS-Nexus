import subprocess
import pkg_resources

def install_packages():
    packages = ["ttkthemes"]

    installed = {pkg.key for pkg in pkg_resources.working_set}
    
    for package in packages:
        if package not in installed:
            print(f"Installing {package}...")
            subprocess.run(["pip", "install", package])
