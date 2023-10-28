using System.Collections.Concurrent;

namespace DCS_Nexus {
    public class MessageQueue<T> {
        private ConcurrentQueue<T> _messageQueue;
        private uint _limit;

        public MessageQueue(uint limit = 1000) {
            _messageQueue = new ConcurrentQueue<T>();
            _limit = limit;
        }

        public void Enqueue(T message) {
            if (_messageQueue.Count < _limit) {
                _messageQueue.Enqueue(message);
            }
        }

        public T? Dequeue() {
            if (_messageQueue.TryDequeue(out T? message)) {
                return message;
            }
            
            return default;
        }

        public bool IsEmpty {
            get => _messageQueue.IsEmpty;
        }

        public uint Count {
            get => (uint)_messageQueue.Count;
        }
    }
}