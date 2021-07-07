# from socketserver import TCPServer, BaseRequestHandler
import socket
import selectors
import json
from typing import Dict

from commands import *
from room import Room, MAX_PLAYERS_IN_GAME as MPIG


def get_random_room(rooms) -> str:
    pass


class PokerServer:
    def __init__(self, host: str, port: int):
        self.listener = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.listener.bind((host, port))
        self.listener.setblocking(False)
        self.listener.listen(10)
        self.sel = selectors.DefaultSelector()
        self.sel.register(self.listener, selectors.EVENT_READ, self.accept)
        self.players = {}
        self.rooms = {}

    def accept(self, _):
        conn, _client = self.listener.accept()
        self.players[conn] = ""
        self.sel.register(conn, selectors.EVENT_READ, self.handle)

    def _handle(self, conn):
        data = conn.recv(16192).strip()
        payload = json.loads(data)
        username = payload["username"]
        kind = payload["kind"]
        payload = payload["payload"]
        if kind == "JOIN":
            cmd = JoinCmd(payload)
            assert self.players[conn] == ""
            assert cmd.room in self.rooms
            assert len(self.rooms[cmd.room].players) < MPIG
            assert not self.rooms[cmd.room].started
            self.rooms[cmd.room].players.append((conn, username))
            self.players[conn] = cmd.room
        elif payload["kind"] == "CREATE":
            cmd = CreateCmd(payload)
            assert self.players[conn] == ""
            new_room = get_random_room(self.rooms.keys())
            new_state = Room()
            new_state.players.append((conn, username))
            self.rooms[new_room] = new_state
            self.players[conn] = new_room
        else:
            cmd = {
                "RAISE": RaiseCmd,
                "FOLD": FoldCmd,
            }[kind]
            cmd = cmd(**payload)
            room = cmd.room
            self.rooms[room] = self.rooms[room].update(cmd)
            # self.request.sendall(self.rooms[room] )
        # self.request.sendall(self.data.upper())

    def handle(self, conn):
        try:
            self._handle(conn)
        except Exception as e:
            pass

    def serve(self):
        while True:
            events = self.sel.select()
            for key, _ in events:
                callback = key.data
                callback(key.fileobj)


# class PokerHandler(BaseRequestHandler):
#     def __init__(self, rooms: Dict[str, Room]):
#         self.rooms = rooms

#     def handle(self):
#         self.data = self.request.recv(16192).strip()
#         payload = json.loads(self.data)
#         if payload["kind"] == "JOIN":
#             cmd = JoinCmd(payload["payload"])
#             assert (
#                 len(self.rooms[cmd.room].players) < MPIG
#                 and not self.rooms[cmd.room].started
#             )
#             self.rooms[cmd.room].players.append((self.client_address, cmd.username))
#         elif payload["kind"] == "CREATE":
#             cmd = CreateCmd(payload["payload"])
#             pass
#         else:
#             cmd = {
#                 "RAISE": RaiseCmd,
#                 "FOLD": FoldCmd,
#             }[payload["kind"]]
#             cmd = cmd(**payload["cmd"])
#             room = cmd.room
#             self.rooms[room] = self.rooms[room].update(cmd)
#             # self.request.sendall(self.rooms[room])
#         # self.request.sendall(self.data.upper())


if __name__ == "__main__":
    PORT = 12345

    server = PokerServer("", PORT)
    server.serve()
    # with TCPServer(("", PORT), PokerHandler(rooms)) as server:
    #     server.serve_forever()
