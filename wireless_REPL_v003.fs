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
\      i.   https://github.com/rabbithat/nRF52_delay_functions
\      ii.  https://github.com/rabbithat/nRF52_essential_definitions
\ 2. Only after loading the above files, load this file.
\ 3. At the REPL prompt, type 'terminal' to create a terminal node, or 
\    'remote' to create a remote node. If you wish, you can setup the
\    remote node to start automatically as a remote node by compiling to \    flash memory and writing an init definition such as:
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
\ ( char -- ) for every ASCII_cr received, also emit 
\ an ASCII_lf immediately afterward

#251 CONSTANT _MAX_DATA_PAYLOAD_LENGTH  \ 251 bytes
\ #define MAX_PACKET_COUNTER_CHARACTERS 1
\ #define MAX_PAYLOAD_LENGTH (MAX_DATA_PAYLOAD_LENGTH + MAX_PACKET_COUNTER_CHARACTERS)  

\ Note: must be <= 255 and must be divisible evenly by 4 (for ease of memory addressing)  
\ 252 is the largest number of bytes to fit this criteria
#252 CONSTANT _MAX_PAYLOAD_LENGTH \ = MAX_DATA_PAYLOAD_LENGTH + MAX_PACKET_COUNTER_CHARACTERS

#256 CONSTANT _RADIO_BUFFER_SIZE
_RADIO_BUFFER_SIZE buffer: txRadioBuffer  \ this is buffer that the radio will for transmission
_RADIO_BUFFER_SIZE buffer: rxRadioBuffer  \ this is buffer that the radio will for receiving

$AA constant _target_prefixAddress \ prefix address of the other node
$DEADBEEF constant _target_baseAddress  \ base address of the other node

$AA constant _my_prefixAddress  \ prefix address of this node
$FEEDBEEF constant _my_baseAddress \ base address of this node

#3 constant _maxClockTicksToWaitForAck \ The maximum number of LFCLK clock ticks to wait for an ACK.  3 ticks = 94.5 microseconds

\ ---------------------------------------------------------------------
\ *** START OF VARIABLE DECLARATIONS ***

0 variable <bool_terminalNode?> 
\ SEMAPHORE: True iff the node is a transmitter node.  
\ Otherwise, it's a receiver node.
\ UN-INITIALIZED: no way to know if this is a terminal node or a 
\ receiver node until specified by the user.
  
0 variable <char_pressedKey>
\ CONTAINS: the ASCII key that was pressed
  
0 variable <2uByte_receivedPayload>  
\ CONTAINS: the 2 uByte payload of the packet just received
\ STRUCTURE:
\   uByte0: the payload counter
\   uByte1: the received char
\ UN-INITIALIZED: no value possible until a payload is received.
  
0 variable <1uByte_txPayloadCounter> 
\ CONTAINS: The 1uByte counter to be used when sending a new payload.
\ INITIALIZED: By convention, the payload counter is initialized to zero.

$FF variable <1uByte_previousReceivedCounter>
\ CONTAINS: The 1uByte counter received in the preceeding packet that
\ was received just prior to the most recently received packet.
\ NEED BECAUSE: If the packet just received has the same
\ counter as the preceeding packet, then the just received packet
\ is a duplicate and should be ignored.
\ INITIALIZED: $FF because by convention communication starts with a zero
\ counter.  Therefore, the first packet received will not be ignored.
  
0 variable <1uByte_receivedCounter>
\ CONTAINS: A copy of the 1uByte counter from the just received payload
\ after it is parsed.
 
0 variable <char_receivedChar>
\ CONTAINS: A copy of the ASCII char from the just received payload
\ after it is parsed.
 
false variable <bool_ackReceived?> 
\ SEMAPHORE: True iff an ACK was received

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

\ $40000000 constant _NRF_POWER
$40000578 constant _NRF_POWER__DCDCEN
: initializeHardware 1 _NRF_POWER__DCDCEN ! ;  \ enable the DCDC voltage regulator 

\ $40000000 CONSTANT NRF_CLOCK
$40000008 CONSTANT _NRF_CLOCK__LFCLKSTART
$4000000C CONSTANT _NRF_CLOCK__LFCLKSTOP
$40000104 CONSTANT _NRF_CLOCK__EVENTS_LFCLKSTARTED
$40000518 CONSTANT _NRF_CLOCK__LFCLKSRC \ should default to zero

\ 40000000 constant _NRF_CLOCK
$40000000 constant _NRF_CLOCK__TASKS_HFCLKSTART
$40000100 constant _NRF_CLOCK__EVENTS_HFCLKSTARTED
: initializeClocks 1 _NRF_CLOCK__TASKS_HFCLKSTART ! begin  _NRF_CLOCK__EVENTS_HFCLKSTARTED @ until ;
  
\ 40001000 constant _NRF_RADIO
$40001508 constant _NRF_RADIO__FREQUENCY
$40001518 constant _NRF_RADIO__PCNF1
$40001514 constant _NRF_RADIO__PCNF0
$40001510 constant _NRF_RADIO__MODE
$40001650 constant _NRF_RADIO__MODECNF0
$40001534 constant _NRF_RADIO__CRCCNF
$40001504 constant _NRF_RADIO__PACKETPTR
$40001530 constant _NRF_RADIO__RXADDRESSES
$4000150C constant _NRF_RADIO__TXPOWER
 
: initializeRadio  #98 _NRF_RADIO__FREQUENCY !  $00040200 _NRF_RADIO__PCNF1 !  $00000800 _NRF_RADIO__PCNF0 ! #1 _NRF_RADIO__MODE ! #1 _NRF_RADIO__MODECNF0 ! #3 _NRF_RADIO__CRCCNF ! #1 _NRF_RADIO__RXADDRESSES ! #8 _NRF_RADIO__TXPOWER ! ;


$4000151C constant _NRF_RADIO__BASE0
$40001524 constant _NRF_RADIO__PREFIX0
$40001010 constant _NRF_RADIO__TASKS_DISABLE
$40001110 constant _NRF_RADIO__EVENTS_DISABLED
$40001004 constant _NRF_RADIO__TASKS_RXEN
$40001000 constant _NRF_RADIO__TASKS_TXEN
$40001100 constant _NRF_RADIO__EVENTS_READY
$40001008 constant _NRF_RADIO__TASKS_START
$4000110C constant _NRF_RADIO__EVENTS_END
  
\ guarantee radio is disabled
: disableRadio  0 _NRF_RADIO__EVENTS_DISABLED !  1 _NRF_RADIO__TASKS_DISABLE ! begin _NRF_RADIO__EVENTS_DISABLED @ until ;  
    
: activateRxidleState 0 _NRF_RADIO__EVENTS_READY !  1 _NRF_RADIO__TASKS_RXEN !  begin _NRF_RADIO__EVENTS_READY  until ;  

: initializeRxAddress <bool_terminalNode?> @ if _my_baseAddress _NRF_RADIO__BASE0 ! _my_prefixAddress _NRF_RADIO__PREFIX0 ! else _target_baseAddress _NRF_RADIO__BASE0 ! _target_prefixAddress _NRF_RADIO__PREFIX0 ! then ;

: initializeTxAddress <bool_terminalNode?> @ if _target_baseAddress _NRF_RADIO__BASE0 ! _target_prefixAddress _NRF_RADIO__PREFIX0 ! else _my_baseAddress _NRF_RADIO__BASE0 ! _my_prefixAddress _NRF_RADIO__PREFIX0 ! then ;

: setupRxRole disableRadio rxRadioBuffer _NRF_RADIO__PACKETPTR ! initializeRxAddress ;

: initializeRxIdleMode activateRxidleState ;  
\ ASSERTION: now in RXIDLE state.  Ready to move into RX state.
    
\ turn on the radio receiver and shift into TXIDLE state
: activateTxidleState  0 _NRF_RADIO__EVENTS_READY !  1 _NRF_RADIO__TASKS_TXEN ! begin _NRF_RADIO__EVENTS_READY @ until ;  

: setupTxRole disableRadio txRadioBuffer _NRF_RADIO__PACKETPTR ! initializeTxAddress ;

: initializeTxIdleMode  activateTxidleState ; 
\ 
\ ASSERTION: now in TXIDLE state.  Ready to move into TX state.

: guaranteeClear_EVENTS_END_semaphore  0 _NRF_RADIO__EVENTS_END !  begin _NRF_RADIO__EVENTS_END @  not until ;

: guaranteedTxOrRx 
  1 _NRF_RADIO__TASKS_START ! begin _NRF_RADIO__EVENTS_END @  until ;
  
: txOrRxBuffer guaranteeClear_EVENTS_END_semaphore guaranteedTxOrRx ;

: nonBlocking_txOrRxBuffer guaranteeClear_EVENTS_END_semaphore 1 _NRF_RADIO__TASKS_START ! ;
  
: transmitTxBuffer setupTxRole initializeTxIdleMode txOrRxBuffer ;
  
: receiveIntoRxBuffer setupRxRole initializeRxIdleMode txOrRxBuffer ;

\ ( -- boolean) returns true iff the packet was sent or packet was received
\
: txOrRxAchieved? _NRF_RADIO__EVENTS_END @ ;

: nonBlocking_receiveIntoRxBuffer setupRxRole initializeRxIdleMode nonBlocking_txOrRxBuffer ;
  
: initializeEverything  initializeClocks initializeRtc startRtc initializeSerialIo initializeHardware initializeRadio ;


\ ( -- boolean ) True iff the amount of time an ACK should take has been exceeded
\
: AckTimeOut? NRF_RTC__COUNTER @  _maxClockTicksToWaitForAck >= ;

: guaranteedClearRtc 1 NRF_RTC__TASKS_CLEAR ! begin NRF_RTC__COUNTER @  0 = until ;

\ Waits for an ACK.  If ACK received, then returns true.
\ Otherwise, if ACK not received in a timely manner, returns false
\
: waitForAck  nonBlocking_receiveIntoRxBuffer guaranteedClearRtc begin txOrRxAchieved? dup AckTimeOut? or if true else drop false then until ;

\ ( counter -- )
\
: writeCounterToTxBuffer txRadioBuffer var!b ;

\ ( 16BitValue -- 16BitValue )
: 16BitIncrement 1+ ;

#100 constant _retransmissionLimit \ stop retransmitting if no ACK after this number of tries

\ only display on the terminal node.
: errorMsg_link <bool_terminalNode?> @ if CR ." ***Error in radio link!***  No acknowledgment from target device." CR then ;

\ ( numRetransmissions -- boolean )
: reachedRetransmissionLimit? _retransmissionLimit >= dup if errorMsg_link then ;

: transmitUntilAcked 0 begin 1+ transmitTxBuffer dup reachedRetransmissionLimit? waitForAck dup <bool_ackReceived?> ! or until drop ;

\ Print the payload counter followed by the payload alpha character
\ (  --  )
\
: print<2uByte_receivedPayload> <1uByte_receivedCounter> @ . <char_receivedChar> @ emit CR ;

: printReceivedChar <char_receivedChar> @ showKey ;

: myHookKey <char_receivedChar> @ ;

: parseReceivedPayload rxRadioBuffer listAddr@b0 <1uByte_receivedCounter> ! rxRadioBuffer listAddr@b1 <char_receivedChar> ! ;

\ send an ACK with the same payload as what was just received
: send_ACK <2uByte_receivedPayload> @ txRadioBuffer ! transmitTxBuffer ;

\ True iff the just received payload's counter is the same as the <1uByte_previousReceivedCounter>
\ (  --  )
: duplicateCounter? <1uByte_previousReceivedCounter> @ <1uByte_receivedCounter> @ = ;

\ Set the last received counter to equal the just received counter
\
: updatePreviousReceivedCounter <1uByte_receivedCounter> @ <1uByte_previousReceivedCounter> ! ;

: storeReceivedPayload rxRadioBuffer @ <2uByte_receivedPayload> ! ;

: receive begin receiveIntoRxBuffer storeReceivedPayload send_ACK parseReceivedPayload duplicateCounter? not if updatePreviousReceivedCounter then printReceivedChar again ;

: installTerminalIdentity true <bool_terminalNode?> ! ;

: installRemoteIdentity false <bool_terminalNode?> ! ;


: rx CR CR ." Hello! I am a Receiver node." CR installRemoteIdentity  initializeEverything receive ;

\ $40002000 constant UART0
$40002108 constant NRF_UART0__EVENTS_RXDRDY \ True iff UART0 has received a byte that hasn't yet been read

\ Returns True if a key was pressed, otherwise False
\ (  -- boolean )
: keypressReceived? NRF_UART0__EVENTS_RXDRDY @ ;

\ ( key -- )
\
: writeKeyToTxBuffer dup <char_pressedKey> ! txRadioBuffer 1+ var!b ;

\ (  --  )
\
: transmitPayload transmitUntilAcked ;

\ ( payloadCounter -- )
\
: writeCounterToTxBuffer dup <1uByte_txPayloadCounter> var!b txRadioBuffer var!b ;

: showKeyReceived rxRadioBuffer varAddr@b1 showKey ;

: showKeyIfReceived <bool_ackReceived?> if showKeyReceived then ;

\
\ (  --  )
: startListeningForPackets nonBlocking_receiveIntoRxBuffer ; 

\  True iff a packet has been received into the rxRadioBuffer
\ (  -- boolean )
: packetReceived? txOrRxAchieved? ;

\ (  --  )
\
: listenForKeyOrPacket startListeningForPackets begin packetReceived? keypressReceived? or until ;

\ set flag to end program if the given key is the program abort key.
\ ( key -- )
: quitKey? ASCII_graveAccent = if true <bool_endProgram?> ! else false <bool_endProgram?> ! then ;

\ ( -- )
\
: processKeyPress key dup quitKey? writeKeyToTxBuffer transmitPayload ; \ showKeyIfReceived ;

\ True iff the received packet is not a duplicate 
\ ( -- boolean )
: processReceivedPacket? send_ACK storeReceivedPayload  parseReceivedPayload duplicateCounter? not dup if updatePreviousReceivedCounter then ; 

\ ( -- )
: manageReceivedPacket processReceivedPacket? if showKeyReceived then ;

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


 




