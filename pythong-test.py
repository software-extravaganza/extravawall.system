from nfqueue import Queue

def cb(i, payload):
    print("Packet received")
    payload.set_verdict(nfqueue.NF_ACCEPT)

q = Queue()
q.bind(1, cb) # Bind to the same queue number as your .NET app
q.run()
