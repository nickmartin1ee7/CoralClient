Basic Packet Structure

Both requests and responses are sent as TCP packets. Their payload follows the following basic structure:
Field 	Type 	Value
Size 	32-bit little-endian Signed Integer 	Varies, see below.
ID 	32-bit little-endian Signed Integer 	Varies, see below.
Type 	32-bit little-endian Signed Integer 	Varies, see below.
Body 	Null-terminated ASCII String 	Varies, see below.
Empty String 	Null-terminated ASCII String 	0x00
Packet Size

The packet size field is a 32-bit little endian integer, representing the length of the request in bytes. Note that the packet size field itself is not included when determining the size of the packet, so the value of this field is always 4 less than the packet's actual length. The minimum possible value for packet size is 10:
Size 	Containing
4 Bytes 	ID Field
4 Bytes 	Type Field
At least 1 Byte 	Packet body (potentially empty)
1 Bytes 	Empty string terminator

Since the only one of these values that can change in length is the body, an easy way to calculate the size of a packet is to find the byte-length of the packet body, then add 10 to it.

The maximum possible value of packet size is 4096. If the response is too large to fit into one packet, it will be split and sent as multiple packets. See "Multiple-packet Responses" below for more information on how to determine when a packet is split.
Packet ID

The packet id field is a 32-bit little endian integer chosen by the client for each request. It may be set to any positive integer. When the server responds to the request, the response packet will have the same packet id as the original request (unless it is a failed SERVERDATA_AUTH_RESPONSE packet - see below.) It need not be unique, but if a unique packet id is assigned, it can be used to match incoming responses to their corresponding requests.
Packet Type

The packet type field is a 32-bit little endian integer, which indicates the purpose of the packet. Its value will always be either 0, 2, or 3, depending on which of the following request/response types the packet represents:
Value 	String Descriptor
3 	SERVERDATA_AUTH
2 	SERVERDATA_AUTH_RESPONSE
2 	SERVERDATA_EXECCOMMAND
0 	SERVERDATA_RESPONSE_VALUE

Note that the repetition in the above table is not an error: SERVERDATA_AUTH_RESPONSE and SERVERDATA_EXECCOMMAND both have a numeric value of 2.

See "Requests and Responses" below for an explanation of each packet type.
Packet Body

The packet body field is a null-terminated string encoded in ASCII (i.e. ASCIIZ). Depending on the packet type, it may contain either the RCON password for the server, the command to be executed, or the server's response to a request. See "Requests and Responses" below for details on what each packet type's body contains.
Empty String

The end of a Source RCON Protocol packet is marked by an empty ASCIIZ string - essentially just 8 bits of 0. In languages that do not null-terminate strings automatically, you can simply write 16 bits of 0 immediately after the packet body (8 to terminate the packet body and 8 for the empty string afterwards).
Requests and Responses
SERVERDATA_AUTH

Typically, the first packet sent by the client will be a SERVERDATA_AUTH packet, which is used to authenticate the connection with the server. The value of the packet's fields are as follows:
Field 	Contains
ID 	any positive integer, chosen by the client (will be mirrored back in the server's response)
Type 	3
Body 	the RCON password of the server (if this matches the server's rcon_password cvar, the auth will succeed)

If the rcon_password cvar is not set, or if it is set to empty string, all SERVERDATA_AUTH requests will be refused.
SERVERDATA_EXECCOMMAND

This packet type represents a command issued to the server by a client. This can be a ConCommand such as mp_switchteams or changelevel, a command to set a cvar such as sv_cheats 1, or a command to fetch the value of a cvar, such as sv_cheats. The response will vary depending on the command issued.
Field 	Contains
ID 	any positive integer, chosen by the client (will be mirrored back in the server's response)
Type 	2
Body 	the command to be executed on the server
SERVERDATA_AUTH_RESPONSE

This packet is a notification of the connection's current auth status. When the server receives an auth request, it will respond with an empty SERVERDATA_RESPONSE_VALUE, followed immediately by a SERVERDATA_AUTH_RESPONSE indicating whether authentication succeeded or failed. Note that the status code is returned in the packet id field, so when pairing the response with the original auth request, you may need to look at the packet id of the preceeding SERVERDATA_RESPONSE_VALUE.
Field 	Contains
ID 	If authentication was successful, the ID assigned by the request. If auth failed, -1 (0xFF FF FF FF)
Type 	2
Body 	Empty string (0x00)
SERVERDATA_RESPONSE_VALUE

A SERVERDATA_RESPONSE_VALUE packet is the response to a SERVERDATA_EXECCOMMAND request.
Field 	Contains
ID 	The ID assigned by the original request
Type 	0
Body 	The server's response to the original command. May be empty string (0x00) in some cases.

Note that particularly long responses may be sent in multiple SERVERDATA_RESPONSE_VALUE packets - see "Multiple-packet Responses" below.

Also note that requests executed asynchronously can possibly send their responses out of order[1] - using a unique ID to identify and associate the responses with their requests can circumvent this issue.
Multiple-packet Responses

Most responses are small enough to fit within the maximum possible packet size of 4096 bytes. However, a few commands such as cvarlist and, occasionally, status produce responses too long to be sent in one response packet. When this happens, the server will split the response into multiple SERVERDATA_RESPONSE_VALUE packets. Unfortunately, it can be difficult to accurately determine from the first packet alone whether the response has been split.

One common workaround is for the client to send an empty SERVERDATA_RESPONSE_VALUE packet after every SERVERDATA_EXECCOMMAND request. Rather than throwing out the erroneous request, SRCDS mirrors it back to the client, followed by another RESPONSE_VALUE packet containing 0x0000 0001 0000 0000 in the packet body field. Because SRCDS always responds to requests in the order it receives them, receiving a response packet with an empty packet body guarantees that all of the meaningful response packets have already been received. Then, the response bodies can simply be concatenated to build the full response.

Special thanks to Koraktor for discovering this trick.
Example Packets

The log below shows an example of communication between an RCON client and Source Dedicated Server. The client first sends an authentication request with password "passwrd", then executes commands "echo HLSW: Test", "log", and "status". Lines in red represent request packets, and lines in blue represent the server's responses.

00000000  11 00 00 00 00 00 00 00  03 00 00 00 70 61 73 73 ........ ....pass
00000010  77 72 64 00 00                                   wrd..
00000000  0a 00 00 00 00 00 00 00  00 00 00 00 00 00       ........ ......
0000000E  0a 00 00 00 00 00 00 00  02 00 00 00 00 00       ........ ......
00000015  19 00 00 00 00 00 00 00  02 00 00 00 65 63 68 6f ........ ....echo
00000025  20 48 4c 53 57 3a 20 54  65 73 74 00 00           HLSW: T est..
0000001C  17 00 00 00 00 00 00 00  00 00 00 00 48 4c 53 57 ........ ....HLSW
0000002C  20 3a 20 54 65 73 74 20  0a 00 00                 : Test  ...
00000032  0d 00 00 00 00 00 00 00  02 00 00 00 6c 6f 67 00 ........ ....log.
00000042  00                                               .
00000037  4c 00 00 00 00 00 00 00  00 00 00 00 55 73 61 67 L....... ....Usag
00000047  65 3a 20 20 6c 6f 67 20  3c 20 6f 6e 20 7c 20 6f e:  log  < on | o
00000057  66 66 20 3e 0a 63 75 72  72 65 6e 74 6c 79 20 6c ff >.cur rently l
00000067  6f 67 67 69 6e 67 20 74  6f 3a 20 66 69 6c 65 2c ogging t o: file,
00000077  20 63 6f 6e 73 6f 6c 65  2c 20 75 64 70 0a 00 00  console , udp...
000000D6  10 00 00 00 00 00 00 00  02 00 00 00 73 74 61 74 ........ ....stat
000000E6  75 73 00 00                                      us..
00000134  e4 00 00 00 00 00 00 00  00 00 00 00 68 6f 73 74 ........ ....host
00000144  6e 61 6d 65 3a 20 54 61  73 6b 66 6f 72 63 65 72 name: Ta skforcer
00000154  61 6e 67 65 72 2e 6e 65  74 20 54 46 32 20 2d 20 anger.ne t TF2 - 
00000164  54 65 61 6d 77 6f 72 6b  21 0a 76 65 72 73 69 6f Teamwork !.versio
00000174  6e 20 3a 20 31 2e 30 2e  31 2e 34 2f 31 34 20 33 n : 1.0. 1.4/14 3
00000184  33 34 35 20 73 65 63 75  72 65 20 0a 75 64 70 2f 345 secu re .udp/
00000194  69 70 20 20 3a 20 20 38  2e 32 2e 30 2e 32 38 3a ip  :  8 .2.0.28:
000001A4  32 37 30 31 35 0a 6d 61  70 20 20 20 20 20 3a 20 27015.ma p     : 
000001B4  74 63 5f 68 79 64 72 6f  20 61 74 3a 20 30 20 78 tc_hydro  at: 0 x
000001C4  2c 20 30 20 79 2c 20 30  20 7a 0a 70 6c 61 79 65 , 0 y, 0  z.playe
000001D4  72 73 20 3a 20 30 20 28  32 30 20 6d 61 78 29 0a rs : 0 ( 20 max).
000001E4  0a 23 20 75 73 65 72 69  64 20 6e 61 6d 65 20 75 .# useri d name u
000001F4  6e 69 71 75 65 69 64 20  63 6f 6e 6e 65 63 74 65 niqueid  connecte
00000204  64 20 70 69 6e 67 20 6c  6f 73 73 20 73 74 61 74 d ping l oss stat
00000214  65 20 61 64 72 0a 00 00                          e adr...

Packet Construction Examples
VB.Net

Written by Tuck and veN.

 Private Function RCON_Command(ByVal Command As String, ByVal ServerData As Integer) As Byte()
     Dim Packet As Byte() = New Byte(CByte((13 + Command.Length))) {}
     Packet(0) = Command.Length + 9       'Packet Size (Integer)
     Packet(4) = 0                        'Request Id (Integer)
     Packet(8) = ServerData               'SERVERDATA_EXECCOMMAND / SERVERDATA_AUTH (Integer)
     For X As Integer = 0 To Command.Length - 1
         Packet(12 + X) = System.Text.Encoding.Default.GetBytes(Command(X))(0)
     Next
     Return Packet
 End Function

C++

.h file code:

#include <bit>
#include <concepts>
#include <ostream>
#include <type_traits>

std::ostream &write_le_to(std::ostream &dest, std::integral auto value) {
  if constexpr (std::endian::native == std::endian::little) {
    dest.write(reinterpret_cast<const char *>(&value), sizeof(value));
  } else {
    for (size_t i = 0; i != sizeof(value); ++i) {
      dest.write(
          reinterpret_cast<const char *>(&value) + (sizeof(value) - 1 - i), 1);
    }
  }
  return dest;
}

static std::ostream &write_rcon_to(std::ostream &dest, int32_t type, int32_t id,
                                   std::string_view body) {
  const char nullbytes[] = {'\x00', '\x00'};
  const int32_t minsize = sizeof(id) + sizeof(type) + sizeof(nullbytes);
  const int32_t size = static_cast<int32_t>(body.size() + minsize);
  write_le_to(dest, size);
  write_le_to(dest, id);
  write_le_to(dest, type);
  dest.write(body.data(), body.size());
  dest.write(nullbytes, sizeof(nullbytes));
  return dest;
}

Node.js

Written by Speedhaxx, edited by 2006kiaoptima

function createRequest(type, id, body) {

	// Size, in bytes, of the whole packet. 
	let size = Buffer.byteLength(body) + 14;

	let buffer = Buffer.alloc(size);

	buffer.writeInt32LE(size - 4, 0);
	buffer.writeInt32LE(id,       4);
	buffer.writeInt32LE(type,     8);

	buffer.write(body, 12, size - 2, "ascii");

	// String terminator and 8 empty bits
	buffer.writeInt16LE(0, size - 2);

	return buffer;
};

function readResponse(buffer) {

	let response = {
		size: buffer.readInt32LE(0),
		id:   buffer.readInt32LE(4),
		type: buffer.readInt32LE(8),
		body: buffer.toString("ascii", 12, buffer.length - 2);
	}

	return response;
};

Haskell

import Data.Bytestring as B
import Data.Bytestring.Lazy as BL
import Data.Bytestring.UTF8 as UTF8
import Data.Binary
import Data.Int
createPackage :: String -> Int32 -> B.ByteString
createPackage command idInt =
  B.concat [encodeS sizeInt, id, reqType, body, null]
  where
    sizeInt :: Int32
    sizeInt = fromIntegral (B.length $ UTF8.fromString command) + (10 :: Int32)
    id = encodeS idInt
    reqType = encodeS (2 :: Int32)
    body = UTF8.fromString command
    null = UTF8.fromString "\0\0"
    encodeS :: Binary a => a -> B.ByteString
    encodeS = BL.toStrict . encode
