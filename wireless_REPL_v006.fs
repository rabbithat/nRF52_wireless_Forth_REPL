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

\ Notification Strings
: exitMsg c" Exiting program." ;
: msg_startingReceivePaste c" Ready to receive transmitted paste: " ;
: msg_finishedReceivingPaste c" Finished receiving transmitted paste." ;
: msg_finishedTransmittingPaste c" Finished transmitting paste." ;

\ \\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\


: remoteNodeGreeting CR CR ." Hello! I am the remote node." CR ;
: terminalNodeGreeting CR CR ." Hello! I am the terminal node." CR ;

: showKey dup ASCII_cr = if CR drop else emit then ;
\ ( char -- ) 
\ ACTION: Print char.  For every ASCII_cr received, also print 
\ an ASCII_lf immediately afterward


\ ---------------------------------------------------------------------
\ *** START OF VARIABLE DECLARATIONS ***
  
\ 0 variable <char_pressedKey>
\ CONTAINS: the ASCII key that was pressed
  
false variable <bool_endProgram?> 
\ SEMAPHORE:  True iff this program should stop and exit to the REPL

false variable <halt>
\ SEMAPHORE: True iff a transmission error has occured and so program execution should stop

false variable <bool_paste_mode?>
\ SEMAPHORE: true iff the remote node is in the midst of downloading or processing a paste

\ *** END OF VARIABLE DECLARATIONS ***
\ ---------------------------------------------------------------------

\ unique char value that signals to the remote that the end of the paste file has been reached
0 constant _END_OF_PASTE_CHAR 

\ $40002000 constant UART0
$40002000 constant _NRF_UARTO__TASKS_STARTRX
$40002008 constant _NRF_UART0__TASKS_STARTTX

$40002108 constant _NRF_UART0__EVENTS_RXDRDY
\ True iff UART0 has received a byte that hasn't yet been read  

$4000211C constant _NRF_UARTO__EVENTS_TXDRDY
$40002518 constant _NRF_UART0__RXD
$4000251C constant _NRF_UART0__TXD

 
: nonblocking_keypressReceived?  _NRF_UART0__EVENTS_RXDRDY @ ;
\ (  -- boolean )
\ Returns True if a key was pressed, otherwise False

: nonblocking_altGetKey _NRF_UART0__RXD @ 0 _NRF_UART0__EVENTS_RXDRDY ! ;
\ ( -- char )
\ Reads CHAR from UART0 input.  
\ PRE-CONDITION: a char is already know to be waiting to be read.

: blocking_altEmit begin _NRF_UARTO__EVENTS_TXDRDY @ until _NRF_UART0__TXD ! 1 _NRF_UART0__TASKS_STARTTX ! ;
\ ( char --  )
\ Same action as EMIT, but executes independently from the FORTH kernel.
\ Note: blocks until UART0 is ready to transmit.
\ Warning: no flow control (which is true for EMIT also)

\ Print the given counted string via the alternate 
( cString -- )
: altPrint count 0 do dup c@ blocking_altEmit 1+ loop drop ;

\ specialized emit routine used by the remote node
( char -- )
: remoteEmit dup sendChar <bool_paste_mode?> @ if drop else blocking_altEmit then  ;
<bool_terminalNode?>

: print count 0 do dup c@ emit 1+ loop drop ;

\ stub
: initializeSerialIo ." Listening..." CR CR 1 _NRF_UARTO__TASKS_STARTRX ! ; 
  
: initializeEverything  initializeClocks initializeRtc startRtc initializeSerialIo initializeHardware initializeRadio ;


\ (  --  )
\
: listenForKeyOrPacket startListeningForPackets begin packetReceived? nonblocking_keypressReceived? or until ;

\ set flag to end program if the given key is the program abort key.
\ ( key -- boolean )
: quitKey? ASCII_graveAccent = dup if true <bool_endProgram?> ! else false <bool_endProgram?> ! then ;


\ ( -- )
\
: processAltKeyPress nonblocking_altGetKey dup <char_receivedChar> !  dup quitKey? not if <bool_terminalNode?> @ if sendChar else drop then else drop then ; 

\ : processAltKeyPress nonblocking_altGetKey dup <char_receivedChar> ! dup quitKey? not if dup sendChar else then ;

\ : processAltKeyPress nonblocking_altGetKey dup <char_receivedChar> ! dup blocking_altEmit dup quitKey? not if dup sendChar else then ;

0 variable tx-puffer
0 variable rx-puffer

: hookReceive listenForKeyOrPacket nonblocking_keypressReceived?  if processAltKeyPress else manageReceivedPacket then <char_receivedChar> @ rx-puffer ! ;


: repl-key?  ( -- ? ) rx-puffer @ 0<> ;
: repl-key   ( -- c ) hookReceive rx-puffer @ 0 rx-puffer ! ;
: repl-emit? ( -- ? ) tx-puffer @ 0= ;
: repl-emit  ( c -- ) dup tx-puffer ! remoteEmit 0 tx-puffer ! ;


: setupHooks ['] repl-key? hook-key? ! ['] repl-key hook-key ! ['] repl-emit? hook-emit? ! ['] repl-emit hook-emit ! ;

: terminalSetup  installTerminalIdentity initializeEverything ;

: terminal false <bool_endProgram?> ! terminalNodeGreeting terminalSetup begin listenForKeyOrPacket nonblocking_keypressReceived? if processAltKeyPress else manageReceivedPacket then <bool_endProgram?> @ until exitMsg print CR ;

: remoteSetup installRemoteIdentity initializeEverything ;

: remote remoteNodeGreeting remoteSetup setupHooks ;

\ Type 'remote' if it's the remote node
\ Otherwise, type 'terminal' if it's the terminal node.


\ From over the terminal, download the entire file of new code.  Finished when a tilde is detected.
\ Note: In the download buffer, the tilde is replaced with an ASCII_cr
\ ( -- )
: uploadToBuffer dlBuf_i var0! begin key dup dlBuf_i dlBuf i++List! ASCII_tilde = until dlBuf_i var--! ASCII_cr dlBuf_i dlBuf i++List! ;

\ Prompt the user the upload the code file.  Download the file but do not evaluate it.
: ul CR ." Begin paste: " uploadToBuffer CR CR dlBuf_i @ dup dlBuf_numChars ! . ." characters uploaded." CR CR ;

: transmitBufferToRemoteNode dlBuf_i var0! begin dlBuf_i dlBuf i++List@b sendChar dlBuf_i @ dlBuf_numChars @  >= until _END_OF_PASTE_CHAR sendChar ;

: paste ul terminalSetup transmitBufferToRemoteNode terminal ; \ msg_finishedTransmittingPaste print ;

\ Note: Diagnostic only.
\ show all the code that was received into the download buffer 
\
: sul cr dlBuf_i var0! begin dlBuf_i dlBuf i++List@b showKey dlBuf_i @ dlBuf_numChars @ = until CR ;

\ Print the given ASCII character.  For every ASCII_cr, also emit an ASCII_lf immediately afterward
\ ( char -- )
: showKey dup emit ASCII_cr = if ASCII_lf emit then ;

\ PRE-CONDITION: evalString_i equals the next location after the ASCII_cr which ends the string
\ Result:  Change evalString[0] to equal the number of characters *before* the ASCII_cr 
\ that ends the string
\ ( -- )
: updateImpliedCharCount__evalString evalString_i @ 2 - 0 evalString ivList!b ;

\ Starting from the current indexed location of the download buffer, copy the next line into evalString
\ ( -- )
: copyFrom_dlBuf_into_evalString evalString_i var1! begin dlBuf_i dlBuf i++List@b dup evalString_i evalString i++List!b ASCII_cr = until updateImpliedCharCount__evalString ; 

\ Starting at the beginning of the download buffer, copy each line into evalString and 
\ then print and evaluate it. 
\ ( -- )
: evaluateEachLine dlBuf_i var0! begin copyFrom_dlBuf_into_evalString CR evalString count type evalString count evaluate dlBuf_i @ dlBuf_numChars @  >= until ;

(  -- char )
: blocking_receiveChar begin receiveIntoRxBuffer processReceivedPacket? until <char_receivedChar> @ ;

\ From over the terminal, download the entire file of new code.  Finished when a zero is received. 
\ Store number of chars received into dlBuf_numChars
\ Note: In the download buffer, the tilde (or zero) is replaced with an ASCII_cr
\ ( -- )
: receiveTransmittedFile dlBuf_i var0! begin blocking_receiveChar dup dlBuf_i dlBuf i++List! _END_OF_PASTE_CHAR = until dlBuf_i @ dlBuf_numChars ! dlBuf_i var--! ASCII_cr dlBuf_i dlBuf i++List! ;

: dl remoteSetup msg_startingReceivePaste print receiveTransmittedFile msg_finishedReceivingPaste print ;

\ Note: Diagnostic only.
\ show all the code that was received into the download buffer 
\
: sdl cr dlBuf_i var0! begin dlBuf_i dlBuf i++List@b showKey dlBuf_i @ dlBuf_numChars @ = until CR ;

: download true <bool_paste_mode?> ! dl evaluateEachLine  false <bool_paste_mode?> ! ; \ CR CR CR  ." Done!"  CR  CR

\ Type 'paste" from the REPL to paste a file into the terminal node.
\ Type 'download' to receive a file into the remote node.
\ Type 'remote' if it's the remote node
\ Otherwise, type 'terminal' if it's the terminal node.

 




