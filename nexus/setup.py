import subprocess

try:
    import pkg_resources
except ImportError:
    subprocess.call(['pip', 'install', 'setuptools'])
    import pkg_resources

packages = ["zeroconf", "ttkthemes", "PyYAML", "rx"]

installed = {pkg.key for pkg in pkg_resources.working_set}

for package in packages:
    if package not in installed:
        print(f"Installing {package}...")
        subprocess.run(["pip", "install", package])