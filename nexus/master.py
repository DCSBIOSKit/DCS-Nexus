import threading
from tkinter import *
from tkinter import ttk
from .interface import log
from queue import Queue
import time

IP_ADDRESS = "0.0.0.0"
UDP_PORT = 5010
BUFFER_SIZE = 4096
GROUP = "239.255.50.10"
SLAVE_PORT = 7779
SLAVE_SOCKET_PROTOCOL = "UDP"

slave_socket = None
dcs_socket = None

class DCSOutMessage:
    def __init__(self, slave_id, seq, data):
        self.slave_id = slave_id
        self.seq = seq
        self.data = data
        self.received_at = time.time() * 1000
        self.acknowledged = False

dcs_message_queue: Queue[DCSOutMessage] = Queue()

class SlaveCommand:
    data = None

    def __init__(self, type, slave_id=None):
        self.type = type
        self.slave_id = slave_id

slave_command_queue: Queue[SlaveCommand] = Queue()

def enqueue_slave_command(command):
    slave_command_queue.put(command)

def dcs_loop():
    global log_window
    global slave_socket
    global dcs_socket

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

    try:
        dcs_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        dcs_socket.bind((IP_ADDRESS, UDP_PORT))
        dcs_socket.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, socket.inet_aton(GROUP) + socket.inet_aton(IP_ADDRESS))
    except:
        log("Failed to bind to socket")
        return

    zeroconf = Zeroconf()
    ip_address = get_ip_address()

    info = ServiceInfo(
        "_dcs-bios._tcp.local." if SLAVE_SOCKET_PROTOCOL == "TCP" else "_dcs-bios._udp.local.",
        "DCS-BIOS Service._dcs-bios._tcp.local." if SLAVE_SOCKET_PROTOCOL == "TCP" else "DCS-BIOS Service._dcs-bios._udp.local.",
        addresses=[socket.inet_aton(ip_address)],
        port=SLAVE_PORT,
    )

    zeroconf.register_service(info)

    log(f"Listening for connections on {ip_address}:{SLAVE_PORT}")

    try:
        while True:
            dcs_data, _ = dcs_socket.recvfrom(BUFFER_SIZE)
            
            encoded_data = base64.b64encode(dcs_data).decode()
            message = json.dumps({'type': 'message', 'data': encoded_data})
            
            if SLAVE_SOCKET_PROTOCOL == 'TCP':
                for slave in slaves:
                    bytes = slave.sock.send(message.encode(), socket.MSG_OOB) if SLAVE_SOCKET_PROTOCOL == 'TCP' else slave.sock.sendto(message.encode(), (slave.ip, SLAVE_PORT))
                    log(f"Sent {bytes} {message} to {slave.id} at address {slave.addr()}")
            else:
                if slave_socket is not None:
                    bytes = slave_socket.sendto(message.encode(), ("232.0.1.3", SLAVE_PORT))
                    log(f"Sent {bytes} {message} to multicast group 232.0.1.3")

    except KeyboardInterrupt:
        log("Kthxbai")
    finally:
        if 'log_window' in globals():
            log_window.restore_stdout()
        zeroconf.unregister_service(info)
        zeroconf.close()

def slave_loop():
    global slave_socket
    global dcs_socket

    from .slave import Slave, slaves
    from .interface import root, tree
    from zeroconf import ServiceInfo, Zeroconf
    import socket
    import json
    import base64
    import select
    import time

    if SLAVE_SOCKET_PROTOCOL == 'TCP':
        slave_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        slave_socket.bind((IP_ADDRESS, SLAVE_PORT))
        slave_socket.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        slave_socket.setsockopt(socket.IPPROTO_TCP, socket.IP_TOS, 0x10)
        slave_socket.setsockopt(socket.IPPROTO_IP, socket.IP_TOS, 0x10)
        slave_socket.listen(5)
    else:
        slave_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        slave_socket.bind(('10.0.0.10', SLAVE_PORT))
        slave_socket.setsockopt(socket.IPPROTO_IP, socket.IP_TOS, 0x10)
        slave_socket.setsockopt(socket.IPPROTO_IP, socket.IP_MULTICAST_TTL, 1)
        slave_socket.setsockopt(socket.IPPROTO_IP, socket.IP_MULTICAST_LOOP, 0)
        slave_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

    slave_sockets = []

    while True:
        # Create a list of sockets to monitor
        if SLAVE_SOCKET_PROTOCOL == 'TCP':
            readable_sockets, _, _ = select.select([slave_socket] + slave_sockets, [], [], 0)
            
            for s in readable_sockets:
                if s is slave_socket:
                    if SLAVE_SOCKET_PROTOCOL == 'TCP':
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
                            message = base64.b64decode(command.get('data', None))

                            if message:
                                dcs_socket.sendto(message, ('localhost', 7778))
                        # Disabled at the moment due to drawing performance issues blocking the dcs_thread due to the GIL
                        #if type == "check-in":
                            #slave.update_from_json(slave_data)
                        else:
                            message = command.get('data', None)

                        log(f"Received {type} ({message}) from {slave_data['id']} ({slave_data['mac']}) at address {slave.ip} rssi {slave_data['rssi']}")
                        
                        slave.last_received = int(time.time() * 1000)
                    except:
                        log(f"Failed to receive data from {s.getpeername()}")
        else:
            readable_sockets, _, _ = select.select([slave_socket], [], [], 0.001)
            
            for s in readable_sockets:
                if s is slave_socket:
                    try:
                        raw_data, slave_addr = slave_socket.recvfrom(BUFFER_SIZE)

                        command = json.loads(raw_data.decode())
                            
                        type = command.get('type', None)
                        slave_data = command.get('slave', None)

                        # Check if slave is already registered, otherwise add it
                        slave = Slave.find_slave_by_mac(slave_data['mac'])
                        if not slave:
                            if SLAVE_SOCKET_PROTOCOL == 'TCP':
                                ip, port = s.getpeername()
                            else:
                                ip, port = slave_addr
                            slave = Slave(slave_data['id'], slave_data['mac'], ip, port, s)
                            slave.update_from_json(slave_data)
                            slave.add_slave()

                        message = None

                        if type == "message":
                            message = base64.b64decode(command.get('data', None))

                            if message is not None:
                                is_duplicate = False

                                # Check if the queue already contains a message with the same slave_id and seq
                                for existing_message in list(dcs_message_queue.queue):
                                    if existing_message.slave_id == slave_data['id'] and existing_message.seq == command['seq']:
                                        is_duplicate = True
                                        break

                                # If not a duplicate, add to the queue
                                if not is_duplicate:
                                    dcs_message_queue.put(DCSOutMessage(slave_data['id'], command['seq'], message))
                                # log(f"Enqueued message to DCS-BIOS: {message}")
                        # Disabled at the moment due to drawing performance issues blocking the dcs_thread due to the GIL
                        #if type == "check-in":
                            #slave.update_from_json(slave_data)
                        else:
                            message = command.get('data', None)

                        if message is not None:
                            log(f"Received {type} ({message}) from {slave_data['id']} ({slave_data['mac']}) at address {slave.ip} rssi {slave_data['rssi']}")
                        else:
                            log(f"Received {type} from {slave_data['id']} ({slave_data['mac']}) at address {slave.ip} rssi {slave_data['rssi']}")
                        
                        slave.last_received = int(time.time() * 1000)
                    except Exception as e:
                        log(f"Failed to receive data from {slave_addr}: {e}")

        items_to_remove = []
        for message in list(dcs_message_queue.queue):
            
            # Check if the item is old, messages are saved for 60 seconds in case they are retranmitted (so they can be ignored).
            if time.time() * 1000 - message.received_at >= 10000:
                items_to_remove.append(message)
                continue
            
            if not message.acknowledged:
                message.acknowledged = True
                dcs_socket.sendto(message.data, ('localhost', 7778))
                log(f"Forwarded message to DCS-BIOS: {message.data} (seq {message.seq})")
                
                ack = json.dumps({'type': 'ack', 'id': message.slave_id, 'seq': message.seq})
                
                if SLAVE_SOCKET_PROTOCOL == 'TCP':
                    for slave in slaves:
                        if slave.id == message.slave_id:
                            slave.sock.send(ack.encode(), socket.MSG_OOB)
                            bytes_sent = slave.sock.send(ack.encode(), socket.MSG_OOB)
                            #log(f"Sent {bytes_sent} {ack} to {slave.id} at address {slave.addr()}")
                else:
                    if slave_socket is not None:
                        slave_socket.sendto(ack.encode(), ("232.0.1.3", SLAVE_PORT))
                        bytes_sent = slave_socket.sendto(ack.encode(), ("232.0.1.3", SLAVE_PORT))
                        log(f"Sent {bytes_sent} {ack} to multicast group 232.0.1.3")

        # Remove items that are old
        for item in items_to_remove:
            dcs_message_queue.queue.remove(item)

        # Send out slave commands
        while not slave_command_queue.empty():
            command = slave_command_queue.get()

            payload = json.dumps({"type": command.type, "id": command.slave_id, "data": command.data}).encode()

            if command.type == "restart-all":
                for slave in slaves:  # Assuming 'slaves' is the list containing all Slave objects
                    slave.sock.send(payload) if SLAVE_SOCKET_PROTOCOL == 'TCP' else slave_socket.sendto(payload, ("232.0.1.3", SLAVE_PORT))
                    log(f"Sent restart command to all slaves")

            elif command.type == "restart":
                slave = Slave.find_slave_by_id(command.slave_id)
                if slave:
                    slave.sock.send(payload) if SLAVE_SOCKET_PROTOCOL == 'TCP' else slave_socket.sendto(payload, ("232.0.1.3", SLAVE_PORT))
                    log(f"Sent restart command to {slave.id} ")
                else:
                    log(f"Failed to send restart command to {command.slave_id} because it is not registered")

        # Check for stale slaves
        current_time = int(time.time() * 1000)

        # Find stale slaves
        stale_slaves = [slave for slave in slaves if current_time - slave.last_received > 3000]

        # Remove stale slaves and log their removal
        for stale_slave in stale_slaves:
            log(f"Removing stale slave {stale_slave.id} with MAC address {stale_slave.mac} ({len(slave_sockets)} remaining)")
            if slave_socket in slave_sockets:
                slave_sockets.remove(stale_slave.sock)
            stale_slave.remove_slave()

        # Send keep-alive to all slaves
        message = json.dumps({"type": "check-in"})
        current_time = time.time() * 1000

        for slave in slaves:
            if current_time - slave.last_sent >= 1000:
                slave.sock.send(message.encode()) if SLAVE_SOCKET_PROTOCOL == 'TCP' else slave.sock.sendto(message.encode(), (slave.ip, SLAVE_PORT))
                slave.last_sent = current_time
                log(f"Sent keep-alive to {slave.id} at address {slave.ip}")

slave_thread = threading.Thread(target=slave_loop, daemon=True)
slave_thread.start()

dcs_thread = threading.Thread(target=dcs_loop, daemon=True)
dcs_thread.start()