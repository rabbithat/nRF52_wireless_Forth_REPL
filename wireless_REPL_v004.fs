\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
\
\ DESCRIPTION: this nRF52 Forth code provides a wirless Forth REPL
\ between a remote node and a terminal node connected to your computer.
\ This means that what you type and see on your terminal screen when 
\ the two nodes are radio linked is exactly what you would see if your 
\ terminal were directly connected to the remote node.
\ 
\ An ACK protocol is used to ensure that every character arrives as 
\ intended, and a 3 byte CRC is used for error checking.  Since
\ packets with errors won't be ACKed, a new packet will be sent
\ until a packet which passes the 3 byte CRC is both received and
\ and ACKed. Any redundant packets received will be ignored. Moreover,
\ a character is shown on your terminal screen only if it has been
\ received as valid and ACKed.  As further assurance of correctness,
\ the character displayed on your terminal is the one included in the 
\ ACK reponse packet.  If no ACK is received, then no character will
\ be displayed *and* a hard to ignore radio link error message will 
\ be displayed instead.  Thus the veracity of communications is both
\ reliable and easy to verify visually.
\
\
\ DIRECTIONS: 
\ 1. Load the current versions of the following files:
\     i.    https://github.com/rabbithat/nRF52_delay_functions
\     ii.   https://github.com/rabbithat/nRF52_essential_definitions
\     iii.  https://github.com/rabbithat/nRF52_osiLayers3and4
\ 2. Only after loading the above files, load this file.
\ 3. At the REPL prompt, type 'terminal' to create a terminal node, or 
\    'remote' to create a remote node. If you wish, you can setup the
\    remote node to start automatically as a remote node by compiling to
\    flash memory and writing an init definition such as:
\    : init remote ;
\    at the end of the file.
\
\ For a proper demonstration, you will need both a remote node and a 
\ terminal node.
\
\ This Forth code is written in mecrisp-stellaris: 
\ https://sourceforge.net/projects/mecrisp/
\ 
\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\

: remoteNodeGreeting CR CR ." Hello! I am the remote node." CR ;
: terminalNodeGreeting CR CR ." Hello! I am the terminal node." CR ;

: showKey dup ASCII_cr = if CR drop else emit then ;
\ ( char -- ) 
\ ACTION: Print char.  For every ASCII_cr received, also print 
\ an ASCII_lf immediately afterward


\ ---------------------------------------------------------------------
\ *** START OF VARIABLE DECLARATIONS ***
  
0 variable <char_pressedKey>
\ CONTAINS: the ASCII key that was pressed
  
false variable <bool_endProgram?> 
\ SEMAPHORE:  True iff this program should stop and exit to the REPL

\ *** END OF VARIABLE DECLARATIONS ***
\ ---------------------------------------------------------------------


$40002008 constant NRF_UART0__TASKS_STARTTX
$4000251C constant NRF_UART0__TXD

\ ( key --  )
: myEmit NRF_UART0__TXD ! 1 NRF_UART0__TASKS_STARTTX ! ;


\ stub
: initializeSerialIo ." Listening..." CR CR ; 

  
: initializeEverything  initializeClocks initializeRtc startRtc initializeSerialIo initializeHardware initializeRadio ;



\ $40002000 constant UART0
$40002108 constant NRF_UART0__EVENTS_RXDRDY \ True iff UART0 has received a byte that hasn't yet been read

\ Returns True if a key was pressed, otherwise False
\ (  -- boolean )
: keypressReceived? NRF_UART0__EVENTS_RXDRDY @ ;


\ (  --  )
\
: listenForKeyOrPacket startListeningForPackets begin packetReceived? keypressReceived? or until ;

\ set flag to end program if the given key is the program abort key.
\ ( key -- )
: quitKey? ASCII_graveAccent = if true <bool_endProgram?> ! else false <bool_endProgram?> ! then ;

\ ( -- )
\
: processKeyPress key dup quitKey? writeKeyToTxBuffer transmitPayload ; 

0 variable tx-puffer
0 variable rx-puffer

: hookReceive begin receiveIntoRxBuffer processReceivedPacket? until <char_receivedChar> @ rx-puffer ! ; 

\ ( char -- )
: sendChar <1uByte_txPayloadCounter> ++var@b writeCounterToTxBuffer writeKeyToTxBuffer transmitPayload ;


: repl-key?  ( -- ? ) rx-puffer @ 0<> ;
: repl-key   ( -- c ) hookReceive rx-puffer @ 0 rx-puffer ! ;
: repl-emit? ( -- ? ) tx-puffer @ 0= ;
: repl-emit  ( c -- ) dup tx-puffer ! dup myEmit sendChar 0 tx-puffer ! ;


: setupHooks ['] repl-key? hook-key? ! ['] repl-key hook-key ! ['] repl-emit? hook-emit? ! ['] repl-emit hook-emit ! ;

\ (  --  )
\
: terminal terminalNodeGreeting installTerminalIdentity initializeEverything 0 begin 1+ value_b dup  writeCounterToTxBuffer listenForKeyOrPacket keypressReceived? if processKeyPress else manageReceivedPacket then   <bool_endProgram?> @ until ;

: remote remoteNodeGreeting installRemoteIdentity initializeEverything  setupHooks ;

\ Type 'remote' if it's the remote node
\ Otherwise, type 'terminal' if it's the terminal node.




 




