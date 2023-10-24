import threading
from tkinter import *
from tkinter import ttk

def master_loop():
    from .slave import Slave, slaves
    from .interface import root, tree
    from zeroconf import ServiceInfo, Zeroconf
    import socket
    import json
    import base64
    import select
    import time

    def get_ip_address():
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.connect(("8.8.8.8", 80))
        ip_address = s.getsockname()[0]
        s.close()
        return ip_address

    IP_ADDRESS = "0.0.0.0"
    UDP_PORT = 5010
    BUFFER_SIZE = 4096
    GROUP = "239.255.50.10"

    try:
        dcs_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        dcs_socket.bind((IP_ADDRESS, UDP_PORT))
        dcs_socket.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, socket.inet_aton(GROUP) + socket.inet_aton(IP_ADDRESS))
    except:
        print("Failed to bind to socket")
        return

    zeroconf = Zeroconf()
    ip_address = get_ip_address()

    SLAVE_PORT = 7779
    info = ServiceInfo(
        "_dcs-bios._tcp.local.",
        "DCS-BIOS Service._dcs-bios._tcp.local.",
        addresses=[socket.inet_aton(ip_address)],
        port=SLAVE_PORT,
    )

    zeroconf.register_service(info)
    
    slave_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    slave_socket.bind((IP_ADDRESS, SLAVE_PORT))
    slave_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
    slave_socket.setsockopt(socket.IPPROTO_TCP, socket.IP_TOS, 0x10)
    slave_socket.setsockopt(socket.IPPROTO_IP, socket.IP_TOS, 0x10)
    slave_socket.listen(5)

    print(f"Listening for connections on {ip_address}:{SLAVE_PORT}")

    try:
        slave_sockets = []

        while True:
            # Create a list of sockets to monitor
            readable_sockets, _, _ = select.select([dcs_socket, slave_socket] + slave_sockets, [], [], 0.1)
            
            for s in readable_sockets:
                if s is dcs_socket:
                    dcs_data, _ = dcs_socket.recvfrom(BUFFER_SIZE)
                    
                    for slave in slaves:
                        encoded_data = base64.b64encode(dcs_data).decode()
                        message = json.dumps({'type': 'message', 'data': encoded_data})
                        bytes = slave.sock.send(message.encode(), socket.MSG_OOB)
                        print(f"Sent {bytes} {message} to {slave.id} at address {slave.addr()}")

                elif s is slave_socket:
                    conn, addr = s.accept()
                    s.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
                    s.setsockopt(socket.IPPROTO_TCP, socket.IP_TOS, 0x10)
                    s.setsockopt(socket.IPPROTO_IP, socket.IP_TOS, 0x10)
                    slave_sockets.append(conn)

                else:
                    try:
                        raw_data = s.recv(BUFFER_SIZE)

                        command = json.loads(raw_data.decode())
                    
                        type = command.get('type', None)
                        slave_data = command.get('slave', None)

                        # Check if slave is already registered, otherwise add it
                        slave = Slave.find_slave_by_mac(slave_data['mac'])
                        if not slave:
                            ip, port = s.getpeername()
                            slave = Slave(slave_data['id'], slave_data['mac'], ip, port, s)
                            slave.update_from_json(slave_data)
                            slave.add_slave()

                        if type == "message":
                            data = base64.b64decode(command.get('data', None))
                        else:
                            data = command.get('data', None)

                        print(f"Received {type} ({data}) from {slave_data['id']} ({slave_data['mac']}) at address {slave.ip} rssi {slave_data['rssi']}")
                        
                        slave.update_from_json(slave_data)

                        slave.last_received = int(time.time() * 1000)
                    except:
                        print(f"Failed to receive data from {s.getpeername()}")

            # Check for stale slaves
            current_time = int(time.time() * 1000)

            # Find stale slaves
            stale_slaves = [slave for slave in slaves if current_time - slave.last_received > 3000]

            # Remove stale slaves and log their removal
            for stale_slave in stale_slaves:
                print(f"Removing stale slave {stale_slave.id} with MAC address {stale_slave.mac} ({slave_sockets.count} remaining)")
                slave_sockets.remove(stale_slave.sock)
                stale_slave.remove_slave()

            # Send keep-alive to all slaves
            message = json.dumps({"type": "check-in"})
            current_time = time.time() * 1000

            for slave in slaves:
                if current_time - slave.last_sent >= 1000:
                    slave.sock.send(message.encode())
                    slave.last_sent = current_time
                    print(f"Sent keep-alive to {slave.id} at address {slave.ip}")
            
    except KeyboardInterrupt:
        print("Kthxbai")
    finally:
        zeroconf.unregister_service(info)
        zeroconf.close()

thread = threading.Thread(target=master_loop, daemon=True)
thread.start()